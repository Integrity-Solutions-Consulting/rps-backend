using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Honorarios.Application;
using rps_backend.Features.Honorarios.Domain;
using rps_backend.Features.Liquidaciones.Domain;
using rps_backend.Features.Proyectos.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Application;

// Reglas de negocio de Liquidaciones. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
public class LiquidacionesService(RpsDbContext db)
{
    // Consolida los honorarios sin liquidar todavía de un período por colaborador (RN 30-31) y
    // distribuye el neto en 1 o 2 Corte según su Frecuencia (RN 36-40). Idempotente: un honorario
    // ya asignado a una liquidación (LiquidacionId != null) no se vuelve a tomar.
    //
    // Si el colaborador ya tiene Liquidacion de este período (ej. llegó un contrato tardío y se
    // generaron honorarios nuevos después de la primera corrida), los honorarios nuevos se suman
    // a esa misma Liquidacion y sus Corte se recalculan con el total — nunca se crea una segunda
    // Liquidacion para el mismo (colaborador, período), porque eso volvía a aplicar la recuperación
    // de anticipos de ese período una segunda vez.
    public async Task<Resultado<GenerarLiquidacionesResponse>> GenerarAsync(GenerarLiquidacionesRequest request, CancellationToken ct)
    {
        if (request.Periodo is not { Day: 1 } periodo)
            return LiquidacionErrores.PeriodoInvalido;

        var finPeriodo = periodo.AddMonths(1).AddDays(-1);
        var dia15 = new DateOnly(periodo.Year, periodo.Month, 15);

        var honorariosNuevos = await db.Honorarios
            .Where(h => h.Periodo == periodo && h.LiquidacionId == null)
            .ToListAsync(ct);

        if (honorariosNuevos.Count == 0)
            return new GenerarLiquidacionesResponse(0, 0, 0);

        var colaboradorIds = honorariosNuevos.Select(h => h.ColaboradorId).Distinct().ToList();

        var liquidacionesExistentes = await db.Liquidaciones
            .Where(l => l.Periodo == periodo && colaboradorIds.Contains(l.ColaboradorId))
            .ToDictionaryAsync(l => l.ColaboradorId, ct);

        var honorariosPrevios = liquidacionesExistentes.Count == 0
            ? []
            : await db.Honorarios.AsNoTracking()
                .Where(h => h.Periodo == periodo && h.LiquidacionId != null && colaboradorIds.Contains(h.ColaboradorId))
                .ToListAsync(ct);

        var frecuenciaPorColaborador = (await db.ColaboradoresFrecuencia.AsNoTracking()
                .Where(f => colaboradorIds.Contains(f.ColaboradorId) && f.VigenteDesdeMes <= periodo)
                .ToListAsync(ct))
            .GroupBy(f => f.ColaboradorId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.VigenteDesdeMes).First().Frecuencia);

        var condicionesPorId = (await db.CondicionesEconomicas.AsNoTracking()
                .Where(c => colaboradorIds.Contains(c.ColaboradorId))
                .ToListAsync(ct))
            .ToDictionary(c => c.Id);

        var proyectoIds = honorariosNuevos.Concat(honorariosPrevios).Where(h => h.ProyectoId is not null).Select(h => h.ProyectoId!.Value).Distinct().ToList();
        var asignaciones = await db.Asignaciones.AsNoTracking()
            .Where(a => colaboradorIds.Contains(a.ColaboradorId) && proyectoIds.Contains(a.ProyectoId)
                && !a.EliminadaEnOrigen
                && a.FechaInicio <= finPeriodo && (a.FechaSalida == null || a.FechaSalida >= periodo))
            .ToListAsync(ct);

        var contratoIdsHonorarios = honorariosNuevos.Concat(honorariosPrevios).Select(h => h.ContratoId).Distinct().ToList();
        var inicioContrato = await db.Contratos.AsNoTracking()
            .Where(c => contratoIdsHonorarios.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.FechaInicio, ct);

        // Cuotas de recuperación de Anticipos adicionales programadas para este período (RN 38).
        var recuperacionPorColaboradorYCorte = (await db.CuotasRecuperacion.AsNoTracking()
                .Where(c => c.Periodo == periodo)
                .Join(db.AnticiposAdicionales.AsNoTracking(), c => c.AnticipoAdicionalId, a => a.Id,
                    (c, a) => new { a.ColaboradorId, c.Corte, c.Importe })
                .Where(x => colaboradorIds.Contains(x.ColaboradorId))
                .ToListAsync(ct))
            .GroupBy(x => (x.ColaboradorId, x.Corte))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Importe));

        // Cada honorario sale de una condición: la del contrato (pago por contrato, una sola) o la
        // de cada proyecto (pago por proyecto).
        // Cálculo tributario del importe mensual COMPLETO (sin prorratear) de cada condición.
        List<CalculoTributario.Resultado> MensualCompletoDe(IEnumerable<Honorario> grupoHonorarios) => grupoHonorarios
            .DistinctBy(h => h.CondicionEconomicaId)
            .Select(h =>
            {
                var condicion = condicionesPorId[h.CondicionEconomicaId];
                return CalculoTributario.Calcular(
                    condicion.ImporteMensual, h.Modalidad, h.PorcentajeIva, h.PorcentajeRetencionFuente, h.PorcentajeRetencionIva);
            })
            .ToList();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var generadas = 0;
        var actualizadas = 0;
        foreach (var grupo in honorariosNuevos.GroupBy(h => h.ColaboradorId))
        {
            var previosDelColaborador = honorariosPrevios.Where(h => h.ColaboradorId == grupo.Key).ToList();
            var yaExistia = liquidacionesExistentes.TryGetValue(grupo.Key, out var liquidacion);

            var netoConsolidado = previosDelColaborador.Sum(h => h.Neto) + grupo.Sum(h => h.Neto);
            var subtotalMensualCompleto = MensualCompletoDe(previosDelColaborador.Concat(grupo)).Sum(r => r.Subtotal);

            if (!yaExistia)
            {
                liquidacion = new Liquidacion { ColaboradorId = grupo.Key, Periodo = periodo, NetoConsolidado = netoConsolidado };
                db.Liquidaciones.Add(liquidacion);
                generadas++;
            }
            else
            {
                var netoAnterior = liquidacion!.NetoConsolidado;
                liquidacion.NetoConsolidado = netoConsolidado;
                // Un honorario nuevo cambia el total: la revisión anterior ya no describe esta
                // liquidación (equivalente de RN 49 para este caso).
                var cultura = new System.Globalization.CultureInfo("es-EC");
                liquidacion.RegistrarModificacion(
                    $"Se {(grupo.Count() == 1 ? "sumó 1 honorario nuevo" : $"sumaron {grupo.Count()} honorarios nuevos")} " +
                    $"(neto {netoAnterior.ToString("C2", cultura)} → {netoConsolidado.ToString("C2", cultura)})");
                actualizadas++;
            }

            foreach (var honorario in grupo)
                honorario.LiquidacionId = liquidacion!.Id;

            var frecuencia = frecuenciaPorColaborador.GetValueOrDefault(grupo.Key, Frecuencia.FinDeMes);
            // Desde cuándo trabajó en el período: por proyecto, la entrada a la asignación; por
            // contrato, el inicio del contrato (los proyectos no cuentan). Sirve para RN 26.
            var honorariosDelColaborador = previosDelColaborador.Concat(grupo).ToList();
            var proyectosDelColaborador = honorariosDelColaborador.Where(h => h.ProyectoId is not null).Select(h => h.ProyectoId!.Value).ToHashSet();
            var inicios = asignaciones
                .Where(a => a.ColaboradorId == grupo.Key && proyectosDelColaborador.Contains(a.ProyectoId))
                .Select(a => a.FechaInicio)
                .Concat(honorariosDelColaborador.Where(h => h.ProyectoId is null)
                    .Select(h => inicioContrato.GetValueOrDefault(h.ContratoId, periodo)))
                .Select(f => f > periodo ? f : periodo)
                .ToList();
            DateOnly? inicioEfectivo = inicios.Count > 0 ? inicios.Min() : null;

            var cortesCalculados = ConstruirCortes(frecuencia, netoConsolidado, subtotalMensualCompleto, inicioEfectivo, dia15);

            foreach (var corte in cortesCalculados)
                corte.RecuperacionAdicional = recuperacionPorColaboradorYCorte.GetValueOrDefault((grupo.Key, corte.Tipo), 0m);

            if (!yaExistia)
            {
                foreach (var corte in cortesCalculados)
                {
                    corte.LiquidacionId = liquidacion!.Id;
                    (corte.ValorPreparado, corte.RecuperacionNoAplicada) = RecuperacionEnCortes.Aplicar(corte.ValorBase, corte.RecuperacionAdicional);
                    db.Cortes.Add(corte);
                }
            }
            else
            {
                var cortesExistentes = await db.Cortes.Where(c => c.LiquidacionId == liquidacion!.Id).ToListAsync(ct);

                foreach (var calculado in cortesCalculados)
                {
                    var existente = cortesExistentes.FirstOrDefault(c => c.Tipo == calculado.Tipo);
                    if (existente is null)
                    {
                        calculado.LiquidacionId = liquidacion!.Id;
                        (calculado.ValorPreparado, calculado.RecuperacionNoAplicada) = RecuperacionEnCortes.Aplicar(calculado.ValorBase, calculado.RecuperacionAdicional);
                        db.Cortes.Add(calculado);
                    }
                    else
                    {
                        // Se sobrescribe con el total recalculado; RecuperacionAdicional no se
                        // suma a lo que ya tenía, para no aplicar la misma cuota dos veces.
                        existente.ValorBase = calculado.ValorBase;
                        existente.RecuperacionAdicional = calculado.RecuperacionAdicional;
                        existente.AjustePorProrrateo = calculado.AjustePorProrrateo;
                        (existente.ValorPreparado, existente.RecuperacionNoAplicada) = RecuperacionEnCortes.Aplicar(existente.ValorBase, existente.RecuperacionAdicional);
                    }
                }

                // Si la frecuencia o el momento de ingreso cambiaron el número de cortes (caso raro
                // dentro del mismo período), se retira el corte que ya no corresponde.
                foreach (var sobrante in cortesExistentes.Where(c => cortesCalculados.TrueForAll(n => n.Tipo != c.Tipo)))
                    db.Cortes.Remove(sobrante);
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new GenerarLiquidacionesResponse(generadas, actualizadas, honorariosNuevos.Count);
    }

    private static List<Corte> ConstruirCortes(
        Frecuencia frecuencia, decimal netoConsolidado, decimal subtotalMensualCompleto, DateOnly? inicioEfectivo, DateOnly dia15)
    {
        if (frecuencia == Frecuencia.FinDeMes || inicioEfectivo is not { } fechaInicioEfectiva)
            return [NuevoCorte(TipoCorte.FinDeMes, netoConsolidado)];

        // RN 26: ingreso después del día 15 con DosPagos -> todo el proporcional se prepara al
        // fin de mes, sin corte de quincena ese período.
        if (fechaInicioEfectiva > dia15)
            return [NuevoCorte(TipoCorte.FinDeMes, netoConsolidado)];

        // Quincena ordinaria = 50% del SUBTOTAL mensual COMPLETO (sin IVA ni retenciones, sin
        // prorratear): se asume que la primera quincena se trabajó completa salvo que la entrada
        // fuera después del día 15 (caso ya cubierto arriba). Ver DistribucionCortes.
        var quincena = DistribucionCortes.Quincena(subtotalMensualCompleto);

        // RN 27 / fin de mes = lo que queda: el neto reconocido en el mes (ya prorrateado por días
        // reales, con IVA y retenciones) menos el valor base de la quincena. Se resta el valor
        // base, no lo pagado tras descontar anticipos, para que la cuota de quincena no se
        // "devuelva" a fin de mes.
        var finDeMes = netoConsolidado - quincena;
        var ajustePorProrrateo = 0m;
        if (finDeMes < 0)
        {
            // Salida en el período: lo reconocido en todo el mes no alcanza la mitad ordinaria.
            // Se muestra cuánto se le restó a la quincena en vez de ocultarlo (ver "Saldo
            // insuficiente por salida" del documento de requisitos).
            ajustePorProrrateo = -finDeMes;
            quincena += finDeMes; // absorbe el faltante; finDeMes vuelve a 0.
            finDeMes = 0;
        }

        var corteQuincena = NuevoCorte(TipoCorte.Quincena, quincena);
        corteQuincena.AjustePorProrrateo = ajustePorProrrateo;

        return [corteQuincena, NuevoCorte(TipoCorte.FinDeMes, finDeMes)];
    }

    private static Corte NuevoCorte(TipoCorte tipo, decimal valorBase) => new()
    {
        Tipo = tipo,
        ValorBase = valorBase,
        RecuperacionAdicional = 0,
        ValorPreparado = valorBase
    };

    public async Task<List<LiquidacionResponse>> ListarAsync(
        DateOnly? periodo, Guid? colaboradorId, bool? revisado, CancellationToken ct)
    {
        var query = db.Liquidaciones.AsNoTracking();

        if (periodo is not null)
            query = query.Where(l => l.Periodo == periodo);

        if (colaboradorId is not null)
            query = query.Where(l => l.ColaboradorId == colaboradorId);

        if (revisado is not null)
            query = query.Where(l => l.Revisado == revisado);

        var liquidaciones = await query.OrderByDescending(l => l.Periodo).ToListAsync(ct);
        var ids = liquidaciones.Select(l => l.Id).ToList();

        var cortesPorLiquidacion = (await db.Cortes.AsNoTracking()
                .Where(c => ids.Contains(c.LiquidacionId))
                .ToListAsync(ct))
            .GroupBy(c => c.LiquidacionId)
            .ToDictionary(g => g.Key, g => g.Select(ACorteResponse).ToList());

        return liquidaciones.Select(l => AResponse(l, cortesPorLiquidacion.GetValueOrDefault(l.Id, []))).ToList();
    }

    public async Task<Resultado<LiquidacionDetalleResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var liquidacion = await db.Liquidaciones.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (liquidacion is null)
            return LiquidacionErrores.NoEncontrado(id);

        var cortes = (await db.Cortes.AsNoTracking().Where(c => c.LiquidacionId == id).ToListAsync(ct))
            .Select(ACorteResponse).ToList();

        var honorarios = await db.Honorarios.AsNoTracking()
            .Where(h => h.LiquidacionId == id)
            .Select(h => new HonorarioResponse(
                h.Id, h.ColaboradorId, h.ProyectoId, h.ContratoId, h.Periodo, h.DiasReales, h.DiasReconocidos, h.Modalidad,
                h.ProporcionalAcordado, h.Subtotal, h.Iva, h.TotalDocumento, h.RetencionFuente, h.RetencionIva,
                h.TotalRetenciones, h.Neto, h.LiquidacionId, h.CreadoEn))
            .ToListAsync(ct);

        return new LiquidacionDetalleResponse(
            liquidacion.Id, liquidacion.ColaboradorId, liquidacion.Periodo, liquidacion.NetoConsolidado,
            liquidacion.Revisado, liquidacion.RevisadoEn, liquidacion.RevisadoPorUsuarioId, liquidacion.CreadoEn, cortes, honorarios,
            liquidacion.ModificadaEn, liquidacion.MotivoModificacion, liquidacion.RevisionAnulada);
    }

    // Control interno de Contabilidad previo a exportar (RN 49); no equivale a pago realizado.
    public async Task<Resultado<LiquidacionResponse>> MarcarRevisadaAsync(Guid id, Guid usuarioId, CancellationToken ct)
    {
        var liquidacion = await db.Liquidaciones.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (liquidacion is null)
            return LiquidacionErrores.NoEncontrado(id);

        liquidacion.MarcarRevisada(usuarioId);
        await db.SaveChangesAsync(ct);

        var cortes = (await db.Cortes.AsNoTracking().Where(c => c.LiquidacionId == id).ToListAsync(ct))
            .Select(ACorteResponse).ToList();
        return AResponse(liquidacion, cortes);
    }

    private static CorteResponse ACorteResponse(Corte c) => new(
        c.Id, c.Tipo, c.ValorBase, c.RecuperacionAdicional, c.ValorPreparado, c.RecuperacionNoAplicada, c.AjustePorProrrateo);

    private static LiquidacionResponse AResponse(Liquidacion l, List<CorteResponse> cortes) => new(
        l.Id, l.ColaboradorId, l.Periodo, l.NetoConsolidado, l.Revisado, l.RevisadoEn, l.RevisadoPorUsuarioId, l.CreadoEn, cortes,
        l.ModificadaEn, l.MotivoModificacion, l.RevisionAnulada);
}
