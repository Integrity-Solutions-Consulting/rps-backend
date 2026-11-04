using Microsoft.EntityFrameworkCore;
using rps_backend.Features.IntegracionTmr.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.IntegracionTmr.Application;

// Consulta y revisión de los avisos de cambios de TMR (RN 50). Revisar un aviso no recalcula nada:
// Contabilidad decide si recalcula la liquidación afectada.
public class CambiosTmrService(RpsDbContext db)
{
    public async Task<List<CambioTmrResponse>> ListarAsync(bool? revisado, Guid? colaboradorId, CancellationToken ct)
    {
        var query = db.CambiosTmr.AsNoTracking();

        if (revisado is not null)
            query = query.Where(c => c.Revisado == revisado);

        if (colaboradorId is not null)
            query = query.Where(c => c.ColaboradorId == colaboradorId);

        var cambios = await query.OrderByDescending(c => c.DetectadoEn).ThenBy(c => c.Id).ToListAsync(ct);

        return await AResponsesAsync(cambios, ct);
    }

    public async Task<Resultado<CambioTmrResponse>> MarcarRevisadoAsync(Guid id, Guid usuarioId, CancellationToken ct)
    {
        var cambio = await db.CambiosTmr.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cambio is null)
            return CambioTmrErrores.NoEncontrado(id);

        // Repetir la acción no reemplaza quién lo revisó primero ni cuándo (RC 03).
        if (!cambio.Revisado)
        {
            cambio.Revisado = true;
            cambio.RevisadoEn = DateTime.UtcNow;
            cambio.RevisadoPorUsuarioId = usuarioId;
            await db.SaveChangesAsync(ct);
        }

        return (await AResponsesAsync([cambio], ct))[0];
    }

    private async Task<List<CambioTmrResponse>> AResponsesAsync(List<CambioTmr> cambios, CancellationToken ct)
    {
        var colaboradorIds = cambios.Select(c => c.ColaboradorId).Distinct().ToList();
        var proyectoIds = cambios.Where(c => c.ProyectoId is not null).Select(c => c.ProyectoId!.Value).Distinct().ToList();

        var colaboradores = await db.ColaboradoresRps.AsNoTracking()
            .Where(c => colaboradorIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Nombres, c.Apellidos })
            .ToDictionaryAsync(c => c.Id, ct);

        var proyectos = await db.Proyectos.AsNoTracking()
            .Where(p => proyectoIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Nombre })
            .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);

        var liquidaciones = (await db.Liquidaciones.AsNoTracking()
                .Where(l => colaboradorIds.Contains(l.ColaboradorId))
                .Select(l => new { l.ColaboradorId, l.Periodo, l.Revisado })
                .ToListAsync(ct))
            .ToLookup(l => l.ColaboradorId);

        return cambios.Select(c =>
        {
            var colaborador = colaboradores[c.ColaboradorId];
            var afectadas = c.PeriodoDesde is null
                ? []
                : liquidaciones[c.ColaboradorId].Where(l => l.Periodo >= c.PeriodoDesde).ToList();

            return new CambioTmrResponse(
                c.Id, c.ColaboradorId, colaborador.Nombres, colaborador.Apellidos,
                c.ProyectoId, c.ProyectoId is null ? null : proyectos[c.ProyectoId.Value],
                c.Tipo, c.ValorAnterior, c.ValorNuevo, c.PeriodoDesde, c.DetectadoEn,
                afectadas.Count > 0, afectadas.Any(l => l.Revisado),
                c.Revisado, c.RevisadoEn, c.RevisadoPorUsuarioId);
        }).ToList();
    }
}
