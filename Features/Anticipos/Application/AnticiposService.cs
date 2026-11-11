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
        var cortes = await FrecuenciaParaPlanAsync(colaboradorId, mes, ct) == Frecuencia.DosPagos
            ? new List<TipoCorte> { TipoCorte.Quincena, TipoCorte.FinDeMes }
            : new List<TipoCorte> { TipoCorte.FinDeMes };

        return new ResumenAnticiposResponse(
            colaboradorId, colaborador.Activo, netoMensualCompleto, config.PorcentajeMaximoAnticipo,
            limite, saldoPendiente, Math.Max(0m, limite - saldoPendiente), mes, cortes);
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
    // coincida exacto con el Monto, cada cuota sea positiva y todas sean de un mismo mes.
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

        var existentes = await db.CuotasRecuperacion.Where(c => c.AnticipoAdicionalId == anticipoId).ToListAsync(ct);
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

    private async Task<decimal> NetoMensualCompletoAsync(
        rps_backend.Features.Colaboradores.Domain.ColaboradorRps colaborador,
        rps_backend.Features.Configuracion.Domain.ConfiguracionGlobal config, CancellationToken ct)
    {
        var mesActual = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        // Lo que rige este mes: un solo importe por contrato (pago por contrato) o uno por proyecto.
        // Solo de contratos vigentes hoy: el de un contrato ya terminado no es parte del neto mensual.
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var contratosVigentes = await ContratosVigentesAsync(colaborador.Id, hoy, ct);
        var condicionesVigentes = rps_backend.Features.Honorarios.Application.CondicionesVigentes.Seleccionar(
                await db.CondicionesEconomicas.AsNoTracking()
                    .Where(c => c.ColaboradorId == colaborador.Id && c.MesInicioVigencia <= mesActual)
                    .ToListAsync(ct),
                mesActual)
            .Where(c => contratosVigentes.Contains(c.ContratoId))
            .ToList();

        if (condicionesVigentes.Count == 0)
            return 0m;

        var pctRetencionIva = colaborador.Clasificacion == Clasificacion.Profesional
            ? config.PorcentajeRetencionIvaProfesional
            : config.PorcentajeRetencionIvaJunior;

        return condicionesVigentes.Sum(c => CalculoTributario.Calcular(
            c.ImporteMensual, c.Modalidad, config.PorcentajeIva, config.PorcentajeRetencionFuente, pctRetencionIva).Neto);
    }

    // Contratos del colaborador vigentes en la fecha (con adendas; sin fecha de fin = indefinido).
    private async Task<HashSet<Guid>> ContratosVigentesAsync(Guid colaboradorId, DateOnly fecha, CancellationToken ct)
    {
        var contratos = await db.Contratos.AsNoTracking()
            .Where(c => c.ColaboradorId == colaboradorId && c.FechaInicio <= fecha)
            .Select(c => new { c.Id, c.FechaFin })
            .ToListAsync(ct);
        var ids = contratos.Select(c => c.Id).ToList();
        var maxAdenda = (await db.Adendas.AsNoTracking().Where(a => ids.Contains(a.ContratoId)).ToListAsync(ct))
            .GroupBy(a => a.ContratoId)
            .ToDictionary(g => g.Key, g => g.Max(a => a.NuevaFechaFin));

        return contratos
            .Where(c => c.FechaFin is null || (maxAdenda.TryGetValue(c.Id, out var f) && f > c.FechaFin ? f : c.FechaFin.Value) >= fecha)
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
