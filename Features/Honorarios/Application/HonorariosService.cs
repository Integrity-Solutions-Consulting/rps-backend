using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Honorarios.Domain;
using rps_backend.Features.Proyectos.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// Reglas de negocio de Honorarios. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
public class HonorariosService(RpsDbContext db)
{
    // Un honorario por (colaborador, proyecto) con condición económica vigente y al menos un día
    // reconocido en el período (RN 17-25). Idempotente: se omite si ya existe para ese período.
    public async Task<Resultado<GenerarHonorariosResponse>> GenerarAsync(GenerarHonorariosRequest request, CancellationToken ct)
    {
        if (request.Periodo is not { Day: 1 } periodo)
            return HonorarioErrores.PeriodoInvalido;

        var finPeriodo = periodo.AddMonths(1).AddDays(-1);
        var diasReales = finPeriodo.Day;

        var colaboradores = await db.ColaboradoresRps.AsNoTracking()
            .Where(c => c.Activo && c.EstadoConfiguracion == EstadoConfiguracion.Configurado)
            .ToListAsync(ct);

        if (colaboradores.Count == 0)
            return new GenerarHonorariosResponse(0, 0);

        var colaboradorIds = colaboradores.Select(c => c.Id).ToList();
        var clasificacionPorColaborador = colaboradores.ToDictionary(c => c.Id, c => c.Clasificacion!.Value);

        // Condición económica vigente = la más reciente cuyo MesInicioVigencia ya empezó.
        var condicionesVigentes = (await db.CondicionesEconomicas.AsNoTracking()
                .Where(c => colaboradorIds.Contains(c.ColaboradorId) && c.MesInicioVigencia <= periodo)
                .ToListAsync(ct))
            .GroupBy(c => (c.ColaboradorId, c.ProyectoId))
            .Select(g => g.OrderByDescending(c => c.MesInicioVigencia).First())
            .ToList();

        if (condicionesVigentes.Count == 0)
            return new GenerarHonorariosResponse(0, 0);

        var asignaciones = await db.Asignaciones.AsNoTracking()
            .Where(a => colaboradorIds.Contains(a.ColaboradorId)
                && a.FechaInicio <= finPeriodo
                && (a.FechaSalida == null || a.FechaSalida >= periodo))
            .ToListAsync(ct);

        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);

        var existentes = (await db.Honorarios.AsNoTracking()
                .Where(h => h.Periodo == periodo && colaboradorIds.Contains(h.ColaboradorId))
                .Select(h => new { h.ColaboradorId, h.ProyectoId })
                .ToListAsync(ct))
            .Select(h => (h.ColaboradorId, h.ProyectoId))
            .ToHashSet();

        var nuevos = new List<Honorario>();
        var omitidos = 0;

        foreach (var condicion in condicionesVigentes)
        {
            if (existentes.Contains((condicion.ColaboradorId, condicion.ProyectoId)))
            {
                omitidos++;
                continue;
            }

            var diasReconocidos = ContarDiasReconocidos(
                asignaciones.Where(a => a.ColaboradorId == condicion.ColaboradorId && a.ProyectoId == condicion.ProyectoId),
                periodo, finPeriodo);

            // Un día sin ningún proyecto activo no genera honorarios por asignación (RN 24).
            if (diasReconocidos == 0)
                continue;

            var pctRetencionIva = clasificacionPorColaborador[condicion.ColaboradorId] == Clasificacion.Profesional
                ? config.PorcentajeRetencionIvaProfesional
                : config.PorcentajeRetencionIvaJunior;

            var proporcionalAcordado = Math.Round(
                condicion.ImporteMensual / diasReales * diasReconocidos, 2, MidpointRounding.AwayFromZero);

            var tributos = CalculoTributario.Calcular(
                proporcionalAcordado, condicion.Modalidad, config.PorcentajeIva, config.PorcentajeRetencionFuente, pctRetencionIva);

            nuevos.Add(new Honorario
            {
                ColaboradorId = condicion.ColaboradorId,
                ProyectoId = condicion.ProyectoId,
                ContratoId = condicion.ContratoId,
                CondicionEconomicaId = condicion.Id,
                Periodo = periodo,
                DiasReales = diasReales,
                DiasReconocidos = diasReconocidos,
                Modalidad = condicion.Modalidad,
                PorcentajeIva = config.PorcentajeIva,
                PorcentajeRetencionFuente = config.PorcentajeRetencionFuente,
                PorcentajeRetencionIva = pctRetencionIva,
                ProporcionalAcordado = proporcionalAcordado,
                Subtotal = tributos.Subtotal,
                Iva = tributos.Iva,
                TotalDocumento = tributos.TotalDocumento,
                RetencionFuente = tributos.RetencionFuente,
                RetencionIva = tributos.RetencionIva,
                TotalRetenciones = tributos.TotalRetenciones,
                Neto = tributos.Neto
            });
        }

        if (nuevos.Count > 0)
        {
            db.Honorarios.AddRange(nuevos);
            await db.SaveChangesAsync(ct);
        }

        return new GenerarHonorariosResponse(nuevos.Count, omitidos);
    }

    public async Task<List<HonorarioResponse>> ListarAsync(DateOnly? periodo, Guid? colaboradorId, CancellationToken ct)
    {
        var query = db.Honorarios.AsNoTracking();

        if (periodo is not null)
            query = query.Where(h => h.Periodo == periodo);

        if (colaboradorId is not null)
            query = query.Where(h => h.ColaboradorId == colaboradorId);

        return await query
            .OrderByDescending(h => h.Periodo)
            .Select(h => new HonorarioResponse(
                h.Id, h.ColaboradorId, h.ProyectoId, h.ContratoId, h.Periodo, h.DiasReales, h.DiasReconocidos, h.Modalidad,
                h.ProporcionalAcordado, h.Subtotal, h.Iva, h.TotalDocumento, h.RetencionFuente, h.RetencionIva,
                h.TotalRetenciones, h.Neto, h.LiquidacionId, h.CreadoEn))
            .ToListAsync(ct);
    }

    public async Task<Resultado<HonorarioResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var honorario = await db.Honorarios.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id, ct);
        if (honorario is null)
            return HonorarioErrores.NoEncontrado(id);

        return AResponse(honorario);
    }

    // Solo si todavía no se consolidó en una Liquidación: una vez agrupado, se corrige
    // recalculando el período, no borrando honorarios sueltos (RN 48, RN 51).
    public async Task<Resultado<bool>> EliminarAsync(Guid id, CancellationToken ct)
    {
        var honorario = await db.Honorarios.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (honorario is null)
            return HonorarioErrores.NoEncontrado(id);

        if (honorario.LiquidacionId is not null)
            return HonorarioErrores.YaConsolidado;

        db.Honorarios.Remove(honorario);
        await db.SaveChangesAsync(ct);

        return true;
    }

    private static int ContarDiasReconocidos(IEnumerable<Asignacion> asignaciones, DateOnly periodo, DateOnly finPeriodo)
    {
        var dias = new HashSet<DateOnly>();

        foreach (var asignacion in asignaciones)
        {
            var inicio = asignacion.FechaInicio > periodo ? asignacion.FechaInicio : periodo;
            var finAsignacion = asignacion.FechaSalida ?? finPeriodo;
            var fin = finAsignacion < finPeriodo ? finAsignacion : finPeriodo;

            for (var fecha = inicio; fecha <= fin; fecha = fecha.AddDays(1))
                dias.Add(fecha);
        }

        return dias.Count;
    }

    private static HonorarioResponse AResponse(Honorario h) => new(
        h.Id, h.ColaboradorId, h.ProyectoId, h.ContratoId, h.Periodo, h.DiasReales, h.DiasReconocidos, h.Modalidad,
        h.ProporcionalAcordado, h.Subtotal, h.Iva, h.TotalDocumento, h.RetencionFuente, h.RetencionIva,
        h.TotalRetenciones, h.Neto, h.LiquidacionId, h.CreadoEn);
}
