using Microsoft.EntityFrameworkCore;
using rps_backend.Persistence;

namespace rps_backend.Features.Auditoria.Application;

public record AuditoriaResponse(
    long Id, string Entidad, string EntidadId, string? Campo, string? ValorAnterior, string? ValorNuevo,
    Guid? UsuarioId, DateTime Fecha, string? Motivo);

// Solo lectura: el interceptor es el único que escribe en rps.auditoria.
public class AuditoriaService(RpsDbContext db)
{
    public async Task<List<AuditoriaResponse>> ListarAsync(
        string? entidad, string? entidadId, Guid? usuarioId, int limite, CancellationToken ct)
    {
        var query = db.Set<Domain.Auditoria>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entidad))
            query = query.Where(a => a.Entidad == entidad);
        if (!string.IsNullOrWhiteSpace(entidadId))
            query = query.Where(a => a.EntidadId == entidadId);
        if (usuarioId is not null)
            query = query.Where(a => a.UsuarioId == usuarioId);

        return await query
            .OrderByDescending(a => a.Fecha)
            .Take(Math.Clamp(limite <= 0 ? 200 : limite, 1, 1000))
            .Select(a => new AuditoriaResponse(
                a.Id, a.Entidad, a.EntidadId, a.Campo, a.ValorAnterior, a.ValorNuevo, a.UsuarioId, a.Fecha, a.Motivo))
            .ToListAsync(ct);
    }
}
