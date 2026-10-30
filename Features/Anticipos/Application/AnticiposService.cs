using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Anticipos.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Anticipos.Application;

// Reglas de negocio de Anticipos. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
// El descuento automático contra Honorarios vive en HonorariosService.GenerarAsync, no aquí.
public class AnticiposService(RpsDbContext db)
{
    public async Task<Resultado<List<AnticipoResponse>>> ListarAsync(Guid colaboradorId, EstadoAnticipo? estado, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return AnticipoErrores.ColaboradorNoEncontrado(colaboradorId);

        var query = db.Anticipos.AsNoTracking().Where(a => a.ColaboradorId == colaboradorId);

        if (estado is not null)
            query = query.Where(a => a.Estado == estado);

        return await query
            .OrderByDescending(a => a.FechaSolicitud)
            .Select(a => new AnticipoResponse(
                a.Id, a.ColaboradorId, a.Monto, a.FechaSolicitud, a.Estado, a.HonorarioId, a.CreadoEn, a.ActualizadoEn))
            .ToListAsync(ct);
    }

    public async Task<Resultado<AnticipoResponse>> ObtenerAsync(Guid colaboradorId, Guid anticipoId, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return AnticipoErrores.ColaboradorNoEncontrado(colaboradorId);

        var anticipo = await db.Anticipos.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == anticipoId && a.ColaboradorId == colaboradorId, ct);
        if (anticipo is null)
            return AnticipoErrores.NoEncontrado(anticipoId);

        return AResponse(anticipo);
    }

    public async Task<Resultado<AnticipoResponse>> CrearAsync(Guid colaboradorId, CrearAnticipoRequest request, CancellationToken ct)
    {
        if (request.Monto is null || request.Monto <= 0)
            return AnticipoErrores.MontoInvalido;

        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return AnticipoErrores.ColaboradorNoEncontrado(colaboradorId);

        if (!colaborador.Activo)
            return AnticipoErrores.ColaboradorDesactivado;

        // Si por algún motivo la fila semilla de configuración no existe (ambiente mal migrado),
        // no se bloquea la creación de anticipos: se omite el tope en vez de tirar un 500.
        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstOrDefaultAsync(ct);
        if (config is not null && request.Monto > config.TopeMaximoAnticipo)
            return AnticipoErrores.MontoExcedeTope(config.TopeMaximoAnticipo);

        var anticipo = new Anticipo
        {
            ColaboradorId = colaboradorId,
            Monto = request.Monto.Value,
            FechaSolicitud = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        db.Anticipos.Add(anticipo);
        await db.SaveChangesAsync(ct);

        return AResponse(anticipo);
    }

    public async Task<Resultado<AnticipoResponse>> AprobarAsync(Guid colaboradorId, Guid anticipoId, CancellationToken ct)
    {
        var colaborador = await db.ColaboradoresRps.AsNoTracking().FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return AnticipoErrores.ColaboradorNoEncontrado(colaboradorId);

        // Un anticipo Aprobado de un colaborador inactivo nunca se descuenta (GenerarAsync solo
        // considera colaboradores activos): aprobarlo lo dejaría pendiente para siempre.
        if (!colaborador.Activo)
            return AnticipoErrores.ColaboradorDesactivado;

        var anticipo = await db.Anticipos.FirstOrDefaultAsync(a => a.Id == anticipoId && a.ColaboradorId == colaboradorId, ct);
        if (anticipo is null)
            return AnticipoErrores.NoEncontrado(anticipoId);

        if (anticipo.Estado != EstadoAnticipo.Pendiente)
            return AnticipoErrores.TransicionInvalida(anticipo.Estado, "aprobar");

        anticipo.Estado = EstadoAnticipo.Aprobado;
        anticipo.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return AResponse(anticipo);
    }

    public async Task<Resultado<AnticipoResponse>> AnularAsync(Guid colaboradorId, Guid anticipoId, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return AnticipoErrores.ColaboradorNoEncontrado(colaboradorId);

        var anticipo = await db.Anticipos.FirstOrDefaultAsync(a => a.Id == anticipoId && a.ColaboradorId == colaboradorId, ct);
        if (anticipo is null)
            return AnticipoErrores.NoEncontrado(anticipoId);

        if (anticipo.Estado is not (EstadoAnticipo.Pendiente or EstadoAnticipo.Aprobado))
            return AnticipoErrores.TransicionInvalida(anticipo.Estado, "anular");

        anticipo.Estado = EstadoAnticipo.Anulado;
        anticipo.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return AResponse(anticipo);
    }

    private static AnticipoResponse AResponse(Anticipo a) => new(
        a.Id, a.ColaboradorId, a.Monto, a.FechaSolicitud, a.Estado, a.HonorarioId, a.CreadoEn, a.ActualizadoEn);
}
