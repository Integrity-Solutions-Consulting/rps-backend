using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Anticipos.Domain;
using rps_backend.Features.Honorarios.Application;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Anticipos.Application;

// Reglas de negocio de Anticipos adicionales y su plan de recuperación. No conoce HTTP: devuelve
// Resultado y la capa Api decide el status code.
public class AnticiposService(RpsDbContext db)
{
    public async Task<Resultado<List<AnticipoAdicionalResponse>>> ListarAsync(Guid colaboradorId, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return AnticipoErrores.ColaboradorNoEncontrado(colaboradorId);

        var anticipos = await db.AnticiposAdicionales.AsNoTracking()
            .Where(a => a.ColaboradorId == colaboradorId)
            .OrderByDescending(a => a.Fecha)
            .ToListAsync(ct);

        var respuestas = new List<AnticipoAdicionalResponse>();
        foreach (var anticipo in anticipos)
            respuestas.Add(await AResponseAsync(anticipo, ct));

        return respuestas;
    }

    public async Task<Resultado<AnticipoAdicionalResponse>> ObtenerAsync(Guid colaboradorId, Guid anticipoId, CancellationToken ct)
    {
        var anticipo = await db.AnticiposAdicionales.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == anticipoId && a.ColaboradorId == colaboradorId, ct);
        if (anticipo is null)
            return AnticipoErrores.NoEncontrado(anticipoId);

        return await AResponseAsync(anticipo, ct);
    }

    // Datos para la pantalla de Anticipos. No agrega reglas: reutiliza los mismos cálculos que validan
    // RegistrarAsync (neto completo, límite, saldo pendiente) y RegistrarPlanAsync (cortes por frecuencia).
    public async Task<Resultado<ResumenAnticiposResponse>> ObtenerResumenAsync(Guid colaboradorId, DateOnly? periodo, CancellationToken ct)
    {
        var colaborador = await db.ColaboradoresRps.AsNoTracking().FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return AnticipoErrores.ColaboradorNoEncontrado(colaboradorId);

        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);
        var netoMensualCompleto = await NetoMensualCompletoAsync(colaborador, config, ct);
        var limite = Math.Round(netoMensualCompleto * config.PorcentajeMaximoAnticipo / 100, 2, MidpointRounding.AwayFromZero);
        var saldoPendiente = await SaldoPendienteColaboradorAsync(colaboradorId, ct);

        var mes = periodo is { Day: 1 } p ? p : new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var topes = await TopesPorCorteAsync(colaborador, config, mes, excluirAnticipoId: null, ct);

        var quincenaCerrada = QuincenaCerrada(mes);
        return new ResumenAnticiposResponse(
            colaboradorId, colaborador.Activo, netoMensualCompleto, config.PorcentajeMaximoAnticipo,
            limite, saldoPendiente, Math.Max(0m, limite - saldoPendiente), mes,
            topes.Where(t => !(quincenaCerrada && t.Corte == TipoCorte.Quincena)).Select(t => t.Corte).ToList(),
            topes, quincenaCerrada);
    }

    // RN 21, 42-43: el límite es un % (configurable) del neto mensual completo SIN prorratear
    // (todas las condiciones económicas vigentes del colaborador, a tarifa completa).
    public async Task<Resultado<AnticipoAdicionalResponse>> RegistrarAsync(
        Guid colaboradorId, CrearAnticipoAdicionalRequest request, Guid usuarioId, CancellationToken ct)
    {
        if (request.Monto is null || request.Monto <= 0)
            return AnticipoErrores.MontoInvalido;

        // RC 04: dos anticipos nuevos para el mismo colaborador pueden llegar casi juntos y los
        // dos leer el mismo saldo pendiente "de antes" — sin un bloqueo, ambos pasarían el
        // límite aunque juntos lo superen. Se serializa por colaborador (no bloquea a los demás)
        // reteniendo la fila hasta el commit.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM rps.colaborador_rps WHERE id = {colaboradorId} FOR UPDATE", ct);

        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return AnticipoErrores.ColaboradorNoEncontrado(colaboradorId);

        if (!colaborador.Activo)
            return AnticipoErrores.ColaboradorDesactivado;

        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);
        var netoMensualCompleto = await NetoMensualCompletoAsync(colaborador, config, ct);
        var limite = Math.Round(netoMensualCompleto * config.PorcentajeMaximoAnticipo / 100, 2, MidpointRounding.AwayFromZero);

        var saldoPendiente = await SaldoPendienteColaboradorAsync(colaboradorId, ct);
        if (saldoPendiente + request.Monto > limite)
            return AnticipoErrores.ExcedeLimite(limite);

        var observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim();

        var anticipo = new AnticipoAdicional
        {
            ColaboradorId = colaboradorId,
            Monto = request.Monto.Value,
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow),
            Observaciones = observaciones,
            CreadoPor = usuarioId
        };

        db.AnticiposAdicionales.Add(anticipo);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await AResponseAsync(anticipo, ct);
    }

    // RN 44-47: el plan reemplaza por completo al anterior (si existía); valida que la suma
    // coincida exacto con el Monto, cada cuota sea positiva y todas sean de un mismo mes. Además,
    // ninguna cuota (sumada a las de sus otros anticipos en ese mes y corte) puede superar lo que
    // se le paga en ese corte: quincena = 50% del subtotal, fin de mes = neto - quincena.
    public async Task<Resultado<AnticipoAdicionalResponse>> RegistrarPlanAsync(
        Guid colaboradorId, Guid anticipoId, RegistrarPlanRequest request, CancellationToken ct)
    {
        if (request.Cuotas is null || request.Cuotas.Count == 0)
            return AnticipoErrores.CuotasRequeridas;

        foreach (var cuota in request.Cuotas)
        {
            if (cuota.Periodo is not { Day: 1 } || cuota.Corte is null || cuota.Importe is null || cuota.Importe <= 0)
                return AnticipoErrores.CuotaInvalida;
        }

        if (request.Cuotas.Select(c => c.Periodo!.Value).Distinct().Count() > 1)
            return AnticipoErrores.CuotasDeMesesDistintos;

        // Una sola cuota por corte: dos en el mismo corte se suman en una.
        var corteRepetido = request.Cuotas.GroupBy(c => c.Corte!.Value).FirstOrDefault(g => g.Count() > 1);
        if (corteRepetido is not null)
            return AnticipoErrores.CorteRepetido(corteRepetido.Key);

        var anticipo = await db.AnticiposAdicionales.FirstOrDefaultAsync(a => a.Id == anticipoId && a.ColaboradorId == colaboradorId, ct);
        if (anticipo is null)
            return AnticipoErrores.NoEncontrado(anticipoId);

        var sumaCuotas = request.Cuotas.Sum(c => c.Importe!.Value);
        if (sumaCuotas != anticipo.Monto)
            return AnticipoErrores.SumaCuotasNoCoincide(anticipo.Monto);

        // Una cuota de un corte que no se va a generar nunca se aplicaría (LiquidacionesService
        // solo crea Corte.Quincena cuando la frecuencia es DosPagos): el plan quedaría registrado
        // pero el saldo pendiente nunca se recuperaría de verdad.
        var periodoCuotas = request.Cuotas[0].Periodo!.Value;
        var frecuenciaVigente = await FrecuenciaParaPlanAsync(colaboradorId, periodoCuotas, ct);

        if (frecuenciaVigente != Frecuencia.DosPagos && request.Cuotas.Any(c => c.Corte != TipoCorte.FinDeMes))
            return AnticipoErrores.CorteIncompatibleConFrecuencia;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Dos planes del mismo colaborador guardados casi a la vez podrían leer el mismo
        // "comprometido" y juntos pasarse del tope del corte: se serializa por colaborador (RC 04).
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM rps.colaborador_rps WHERE id = {colaboradorId} FOR UPDATE", ct);

        var colaborador = await db.ColaboradoresRps.AsNoTracking().FirstAsync(c => c.Id == colaboradorId, ct);
        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);
        var topes = await TopesPorCorteAsync(colaborador, config, periodoCuotas, excluirAnticipoId: anticipoId, ct);
        var existentes = await db.CuotasRecuperacion.Where(c => c.AnticipoAdicionalId == anticipoId).ToListAsync(ct);

        foreach (var cuota in request.Cuotas)
        {
            // Pasado el día 15 la quincena ya se pagó: no se le puede descontar ahí, solo de lo que
            // le queda (fin de mes). La cuota de quincena que este anticipo ya tenía se puede dejar
            // tal cual (ya se descontó), pero no cambiar.
            if (cuota.Corte == TipoCorte.Quincena && QuincenaCerrada(periodoCuotas))
            {
                var yaDescontada = existentes.FirstOrDefault(c => c.Periodo == periodoCuotas && c.Corte == TipoCorte.Quincena);
                if (yaDescontada is not null && yaDescontada.Importe == cuota.Importe)
                    continue;
                return AnticipoErrores.QuincenaYaPagada(periodoCuotas);
            }

            // Sin ese corte en el mes (p. ej. la liquidación salió solo con fin de mes, RN 26).
            var tope = topes.FirstOrDefault(t => t.Corte == cuota.Corte!.Value);
            if (tope is null)
                return AnticipoErrores.CorteIncompatibleConFrecuencia;
            if (cuota.Importe!.Value > tope.Disponible)
                return AnticipoErrores.CuotaExcedeCorte(tope.Corte, periodoCuotas, tope.ValorBase, tope.Disponible);
        }

        // Y al revés: una cuota de quincena ya pagada no se puede quitar del plan (ni moverla a
        // otro mes), porque esa plata ya se descontó.
        var quincenaPagadaQuitada = existentes.Any(e => e.Corte == TipoCorte.Quincena && QuincenaCerrada(e.Periodo)
            && !request.Cuotas.Any(c => c.Periodo == e.Periodo && c.Corte == TipoCorte.Quincena && c.Importe == e.Importe));
        if (quincenaPagadaQuitada)
            return AnticipoErrores.QuincenaPagadaNoSeQuita;

        var periodosAfectados = existentes.Select(c => c.Periodo).Append(periodoCuotas).Distinct().ToList();
        db.CuotasRecuperacion.RemoveRange(existentes);
        await db.SaveChangesAsync(ct);

        var nuevas = request.Cuotas.Select(c => new CuotaRecuperacion
        {
            AnticipoAdicionalId = anticipoId,
            Periodo = c.Periodo!.Value,
            Corte = c.Corte!.Value,
            Importe = c.Importe!.Value
        }).ToList();
        db.CuotasRecuperacion.AddRange(nuevas);
        await db.SaveChangesAsync(ct);

        // Si la liquidación del mes ya se generó (aunque ya esté revisada), el pago tiene que
        // reflejar este anticipo: se recalculan sus cortes. También el mes del plan anterior, si
        // el plan se movió de mes.
        foreach (var periodoAfectado in periodosAfectados)
            await rps_backend.Features.Liquidaciones.Application.RecuperacionEnCortes.RecalcularAsync(db, colaboradorId, periodoAfectado, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return await AResponseAsync(anticipo, ct);
    }

    public async Task<Resultado<bool>> EliminarAsync(Guid colaboradorId, Guid anticipoId, CancellationToken ct)
    {
        var anticipo = await db.AnticiposAdicionales.FirstOrDefaultAsync(a => a.Id == anticipoId && a.ColaboradorId == colaboradorId, ct);
        if (anticipo is null)
            return AnticipoErrores.NoEncontrado(anticipoId);

        if (await db.CuotasRecuperacion.AnyAsync(c => c.AnticipoAdicionalId == anticipoId, ct))
            return AnticipoErrores.TieneCuotas;

        db.AnticiposAdicionales.Remove(anticipo);
        await db.SaveChangesAsync(ct);

        return true;
    }

    // Frecuencia con la que se valida un plan de ese mes. Misma consulta que antes estaba dentro de
    // RegistrarPlanAsync; se separó para que el resumen use exactamente la misma regla.
    // Sin frecuencia registrada se paga solo a fin de mes (igual que LiquidacionesService): no se
    // puede tomar la quincena, porque ese corte nunca se generaría.
    private async Task<Frecuencia> FrecuenciaParaPlanAsync(Guid colaboradorId, DateOnly periodo, CancellationToken ct) =>
        await db.ColaboradoresFrecuencia.AsNoTracking()
            .Where(f => f.ColaboradorId == colaboradorId && f.VigenteDesdeMes <= periodo)
            .OrderByDescending(f => f.VigenteDesdeMes)
            .Select(f => (Frecuencia?)f.Frecuencia)
            .FirstOrDefaultAsync(ct) ?? Frecuencia.FinDeMes;

    // La quincena de `mes` se paga el día 15: desde el 16 (hora de Ecuador) ya no se le puede
    // descontar nada ahí. Un mes anterior al actual también la tiene cerrada.
    private static bool QuincenaCerrada(DateOnly mes) =>
        rps_backend.Features.Dashboard.Application.VencimientosContratosService.HoyEnEcuador() > new DateOnly(mes.Year, mes.Month, 15);

    private async Task<decimal> SaldoPendienteColaboradorAsync(Guid colaboradorId, CancellationToken ct)
    {
        var anticipoIds = await db.AnticiposAdicionales.AsNoTracking()
            .Where(a => a.ColaboradorId == colaboradorId)
            .Select(a => a.Id)
            .ToListAsync(ct);

        if (anticipoIds.Count == 0)
            return 0m;

        var montoTotal = await db.AnticiposAdicionales.AsNoTracking()
            .Where(a => anticipoIds.Contains(a.Id))
            .SumAsync(a => a.Monto, ct);

        var cuotasAplicadas = await db.CuotasRecuperacion.AsNoTracking()
            .Where(c => anticipoIds.Contains(c.AnticipoAdicionalId))
            .SumAsync(c => c.Importe, ct);

        return montoTotal - cuotasAplicadas;
    }

    // Para el límite (RN 21): el neto mensual completo de lo que rige hoy.
    private async Task<decimal> NetoMensualCompletoAsync(
        rps_backend.Features.Colaboradores.Domain.ColaboradorRps colaborador,
        rps_backend.Features.Configuracion.Domain.ConfiguracionGlobal config, CancellationToken ct)
    {
        var mesActual = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        return (await MensualCompletoAsync(colaborador, config, mesActual, hoy, hoy, ct)).Neto;
    }

    // Subtotal y neto mensuales COMPLETOS (sin prorratear) de las condiciones que rigen en `mes`,
    // solo de contratos vigentes en algún momento entre `desde` y `hasta`: un solo importe por
    // contrato (pago por contrato) o uno por proyecto.
    private async Task<(decimal Subtotal, decimal Neto)> MensualCompletoAsync(
        rps_backend.Features.Colaboradores.Domain.ColaboradorRps colaborador,
        rps_backend.Features.Configuracion.Domain.ConfiguracionGlobal config,
        DateOnly mes, DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var contratosVigentes = await ContratosVigentesAsync(colaborador.Id, desde, hasta, ct);
        var condicionesVigentes = rps_backend.Features.Honorarios.Application.CondicionesVigentes.Seleccionar(
                await db.CondicionesEconomicas.AsNoTracking()
                    .Where(c => c.ColaboradorId == colaborador.Id && c.MesInicioVigencia <= mes)
                    .ToListAsync(ct),
                mes)
            .Where(c => contratosVigentes.Contains(c.ContratoId))
            .ToList();

        if (condicionesVigentes.Count == 0)
            return (0m, 0m);

        var pctRetencionIva = colaborador.Clasificacion == Clasificacion.Profesional
            ? config.PorcentajeRetencionIvaProfesional
            : config.PorcentajeRetencionIvaJunior;

        var calculos = condicionesVigentes.Select(c => CalculoTributario.Calcular(
            c.ImporteMensual, c.Modalidad, config.PorcentajeIva, config.PorcentajeRetencionFuente, pctRetencionIva)).ToList();
        return (calculos.Sum(r => r.Subtotal), calculos.Sum(r => r.Neto));
    }

    // Tope de descuento por corte del mes (misma regla que LiquidacionesService vía
    // DistribucionCortes), menos lo ya programado por sus otros anticipos en ese mes y corte.
    // `excluirAnticipoId`: el anticipo cuyo plan se está reemplazando no cuenta como comprometido.
    private async Task<List<TopeCorteResponse>> TopesPorCorteAsync(
        rps_backend.Features.Colaboradores.Domain.ColaboradorRps colaborador,
        rps_backend.Features.Configuracion.Domain.ConfiguracionGlobal config,
        DateOnly mes, Guid? excluirAnticipoId, CancellationToken ct)
    {
        // Si la liquidación del mes ya existe, se descuenta de lo que de verdad se le va a pagar en
        // cada corte (con prorrateo por días reales); si no, de la estimación del mes completo.
        var cortesLiquidacion = await db.Liquidaciones.AsNoTracking()
            .Where(l => l.ColaboradorId == colaborador.Id && l.Periodo == mes)
            .Join(db.Cortes.AsNoTracking(), l => l.Id, c => c.LiquidacionId, (l, c) => new { c.Tipo, c.ValorBase })
            .ToListAsync(ct);

        IReadOnlyDictionary<TipoCorte, decimal> valores;
        if (cortesLiquidacion.Count > 0)
        {
            valores = cortesLiquidacion.GroupBy(c => c.Tipo).ToDictionary(g => g.Key, g => g.Sum(c => c.ValorBase));
        }
        else
        {
            var frecuencia = await FrecuenciaParaPlanAsync(colaborador.Id, mes, ct);
            var (subtotal, neto) = await MensualCompletoAsync(colaborador, config, mes, mes, mes.AddMonths(1).AddDays(-1), ct);
            valores = rps_backend.Features.Liquidaciones.Domain.DistribucionCortes.MesCompleto(frecuencia, subtotal, neto);
        }

        var quincenaCerrada = QuincenaCerrada(mes);

        var comprometido = (await db.CuotasRecuperacion.AsNoTracking()
                .Where(c => c.Periodo == mes)
                .Join(db.AnticiposAdicionales.AsNoTracking(), c => c.AnticipoAdicionalId, a => a.Id,
                    (c, a) => new { a.ColaboradorId, c.AnticipoAdicionalId, c.Corte, c.Importe })
                .Where(x => x.ColaboradorId == colaborador.Id && x.AnticipoAdicionalId != excluirAnticipoId)
                .ToListAsync(ct))
            .GroupBy(x => x.Corte)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Importe));

        return valores
            .OrderBy(v => v.Key == TipoCorte.Quincena ? 0 : 1)
            .Select(v =>
            {
                var usado = comprometido.GetValueOrDefault(v.Key, 0m);
                var disponible = quincenaCerrada && v.Key == TipoCorte.Quincena ? 0m : Math.Max(v.Value - usado, 0m);
                return new TopeCorteResponse(v.Key, v.Value, usado, disponible);
            })
            .ToList();
    }

    // Contratos del colaborador vigentes en algún momento entre `desde` y `hasta` (con adendas;
    // sin fecha de fin = indefinido). Con desde == hasta es "vigentes en esa fecha".
    private async Task<HashSet<Guid>> ContratosVigentesAsync(Guid colaboradorId, DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var contratos = await db.Contratos.AsNoTracking()
            .Where(c => c.ColaboradorId == colaboradorId && c.FechaInicio <= hasta)
            .Select(c => new { c.Id, c.FechaFin })
            .ToListAsync(ct);
        var ids = contratos.Select(c => c.Id).ToList();
        var maxAdenda = (await db.Adendas.AsNoTracking().Where(a => ids.Contains(a.ContratoId)).ToListAsync(ct))
            .GroupBy(a => a.ContratoId)
            .ToDictionary(g => g.Key, g => g.Max(a => a.NuevaFechaFin));

        return contratos
            .Where(c => c.FechaFin is null || (maxAdenda.TryGetValue(c.Id, out var f) && f > c.FechaFin ? f : c.FechaFin.Value) >= desde)
            .Select(c => c.Id)
            .ToHashSet();
    }

    private async Task<AnticipoAdicionalResponse> AResponseAsync(AnticipoAdicional anticipo, CancellationToken ct)
    {
        var cuotas = await db.CuotasRecuperacion.AsNoTracking()
            .Where(c => c.AnticipoAdicionalId == anticipo.Id)
            .Select(c => new CuotaRecuperacionResponse(c.Id, c.Periodo, c.Corte, c.Importe))
            .ToListAsync(ct);

        var saldoPendiente = anticipo.Monto - cuotas.Sum(c => c.Importe);

        return new AnticipoAdicionalResponse(
            anticipo.Id, anticipo.ColaboradorId, anticipo.Monto, anticipo.Fecha, anticipo.Observaciones,
            saldoPendiente, cuotas, anticipo.CreadoPor, anticipo.CreadoEn);
    }
}
