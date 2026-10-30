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
    public async Task<Resultado<GenerarLiquidacionesResponse>> GenerarAsync(GenerarLiquidacionesRequest request, CancellationToken ct)
    {
        if (request.Periodo is not { Day: 1 } periodo)
            return LiquidacionErrores.PeriodoInvalido;

        var finPeriodo = periodo.AddMonths(1).AddDays(-1);
        var dia15 = new DateOnly(periodo.Year, periodo.Month, 15);

        var honorarios = await db.Honorarios
            .Where(h => h.Periodo == periodo && h.LiquidacionId == null)
            .ToListAsync(ct);

        if (honorarios.Count == 0)
            return new GenerarLiquidacionesResponse(0, 0);

        var colaboradorIds = honorarios.Select(h => h.ColaboradorId).Distinct().ToList();

        var frecuenciaPorColaborador = (await db.ColaboradoresFrecuencia.AsNoTracking()
                .Where(f => colaboradorIds.Contains(f.ColaboradorId) && f.VigenteDesdeMes <= periodo)
                .ToListAsync(ct))
            .GroupBy(f => f.ColaboradorId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.VigenteDesdeMes).First().Frecuencia);

        var condicionesPorId = (await db.CondicionesEconomicas.AsNoTracking()
                .Where(c => colaboradorIds.Contains(c.ColaboradorId))
                .ToListAsync(ct))
            .ToDictionary(c => c.Id);

        var proyectoIds = honorarios.Select(h => h.ProyectoId).Distinct().ToList();
        var asignaciones = await db.Asignaciones.AsNoTracking()
            .Where(a => colaboradorIds.Contains(a.ColaboradorId) && proyectoIds.Contains(a.ProyectoId)
                && a.FechaInicio <= finPeriodo && (a.FechaSalida == null || a.FechaSalida >= periodo))
            .ToListAsync(ct);

        // Cuotas de recuperación de Anticipos adicionales programadas para este período (RN 38).
        var recuperacionPorColaboradorYCorte = (await db.CuotasRecuperacion.AsNoTracking()
                .Where(c => c.Periodo == periodo)
                .Join(db.AnticiposAdicionales.AsNoTracking(), c => c.AnticipoAdicionalId, a => a.Id,
                    (c, a) => new { a.ColaboradorId, c.Corte, c.Importe })
                .Where(x => colaboradorIds.Contains(x.ColaboradorId))
                .ToListAsync(ct))
            .GroupBy(x => (x.ColaboradorId, x.Corte))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Importe));

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var generadas = 0;
        foreach (var grupo in honorarios.GroupBy(h => h.ColaboradorId))
        {
            var netoConsolidado = grupo.Sum(h => h.Neto);

            var netoMensualCompleto = grupo.Sum(h =>
            {
                var condicion = condicionesPorId[h.CondicionEconomicaId];
                return CalculoTributario.Calcular(
                    condicion.ImporteMensual, h.Modalidad, h.PorcentajeIva, h.PorcentajeRetencionFuente, h.PorcentajeRetencionIva).Neto;
            });

            var liquidacion = new Liquidacion
            {
                ColaboradorId = grupo.Key,
                Periodo = periodo,
                NetoConsolidado = netoConsolidado
            };
            db.Liquidaciones.Add(liquidacion);

            foreach (var honorario in grupo)
                honorario.LiquidacionId = liquidacion.Id;

            var frecuencia = frecuenciaPorColaborador.GetValueOrDefault(grupo.Key, Frecuencia.FinDeMes);
            var asignacionesColaborador = asignaciones
                .Where(a => a.ColaboradorId == grupo.Key && grupo.Any(h => h.ProyectoId == a.ProyectoId))
                .ToList();

            foreach (var corte in ConstruirCortes(frecuencia, netoConsolidado, netoMensualCompleto, asignacionesColaborador, periodo, finPeriodo, dia15))
            {
                corte.LiquidacionId = liquidacion.Id;
                corte.RecuperacionAdicional = recuperacionPorColaboradorYCorte.GetValueOrDefault((grupo.Key, corte.Tipo), 0m);
                corte.ValorPreparado = Math.Max(corte.ValorBase - corte.RecuperacionAdicional, 0m);
                db.Cortes.Add(corte);
            }

            generadas++;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new GenerarLiquidacionesResponse(generadas, honorarios.Count);
    }

    private static List<Corte> ConstruirCortes(
        Frecuencia frecuencia, decimal netoConsolidado, decimal netoMensualCompleto,
        List<Asignacion> asignaciones, DateOnly periodo, DateOnly finPeriodo, DateOnly dia15)
    {
        if (frecuencia == Frecuencia.FinDeMes || asignaciones.Count == 0)
            return [NuevoCorte(TipoCorte.FinDeMes, netoConsolidado)];

        var fechaInicioEfectiva = asignaciones.Min(a => a.FechaInicio > periodo ? a.FechaInicio : periodo);

        // RN 26: ingreso después del día 15 con DosPagos -> todo el proporcional se prepara al
        // fin de mes, sin corte de quincena ese período.
        if (fechaInicioEfectiva > dia15)
            return [NuevoCorte(TipoCorte.FinDeMes, netoConsolidado)];

        // Quincena ordinaria = mitad del neto mensual COMPLETO (sin prorratear): se asume que la
        // primera quincena se trabajó completa salvo que la entrada fuera después del día 15
        // (caso ya cubierto arriba). El centavo impar se asigna a quincena (RN 39).
        var quincena = Math.Round(netoMensualCompleto / 2, 2, MidpointRounding.AwayFromZero);

        // RN 27 / convención confirmada para la segunda mitad: el corte de fin de mes es el
        // total mensual reconocido (ya prorateado por días reales trabajados en todo el mes)
        // menos lo ya preparado en quincena — no una base propia de la segunda quincena.
        var finDeMes = netoConsolidado - quincena;
        if (finDeMes < 0)
        {
            quincena += finDeMes; // absorbe el faltante; finDeMes vuelve a 0.
            finDeMes = 0;
        }

        return [NuevoCorte(TipoCorte.Quincena, quincena), NuevoCorte(TipoCorte.FinDeMes, finDeMes)];
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

        var cortes = await db.Cortes.AsNoTracking().Where(c => c.LiquidacionId == id)
            .Select(c => new CorteResponse(c.Id, c.Tipo, c.ValorBase, c.RecuperacionAdicional, c.ValorPreparado))
            .ToListAsync(ct);

        var honorarios = await db.Honorarios.AsNoTracking()
            .Where(h => h.LiquidacionId == id)
            .Select(h => new HonorarioResponse(
                h.Id, h.ColaboradorId, h.ProyectoId, h.ContratoId, h.Periodo, h.DiasReales, h.DiasReconocidos, h.Modalidad,
                h.ProporcionalAcordado, h.Subtotal, h.Iva, h.TotalDocumento, h.RetencionFuente, h.RetencionIva,
                h.TotalRetenciones, h.Neto, h.LiquidacionId, h.CreadoEn))
            .ToListAsync(ct);

        return new LiquidacionDetalleResponse(
            liquidacion.Id, liquidacion.ColaboradorId, liquidacion.Periodo, liquidacion.NetoConsolidado,
            liquidacion.Revisado, liquidacion.RevisadoEn, liquidacion.CreadoEn, cortes, honorarios);
    }

    // Control interno de Contabilidad previo a exportar (RN 49); no equivale a pago realizado.
    public async Task<Resultado<LiquidacionResponse>> MarcarRevisadaAsync(Guid id, CancellationToken ct)
    {
        var liquidacion = await db.Liquidaciones.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (liquidacion is null)
            return LiquidacionErrores.NoEncontrado(id);

        liquidacion.Revisado = true;
        liquidacion.RevisadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var cortes = await db.Cortes.AsNoTracking().Where(c => c.LiquidacionId == id)
            .Select(c => new CorteResponse(c.Id, c.Tipo, c.ValorBase, c.RecuperacionAdicional, c.ValorPreparado))
            .ToListAsync(ct);
        return AResponse(liquidacion, cortes);
    }

    private static CorteResponse ACorteResponse(Corte c) => new(c.Id, c.Tipo, c.ValorBase, c.RecuperacionAdicional, c.ValorPreparado);

    private static LiquidacionResponse AResponse(Liquidacion l, List<CorteResponse> cortes) => new(
        l.Id, l.ColaboradorId, l.Periodo, l.NetoConsolidado, l.Revisado, l.RevisadoEn, l.CreadoEn, cortes);
}
