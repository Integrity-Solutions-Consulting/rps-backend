using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Proyectos.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Proyectos.Application;

// Reglas de negocio de Asignaciones. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
public class AsignacionesService(RpsDbContext db)
{
    public async Task<Resultado<List<AsignacionResponse>>> ListarAsync(Guid colaboradorId, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return AsignacionErrores.ColaboradorNoEncontrado(colaboradorId);

        return await db.Asignaciones.AsNoTracking()
            .Where(a => a.ColaboradorId == colaboradorId)
            .Join(db.Proyectos.AsNoTracking(), a => a.ProyectoId, p => p.Id, (a, p) => new AsignacionResponse(
                a.Id, a.ColaboradorId, a.ProyectoId, p.Nombre, a.FechaInicio, a.FechaSalida))
            .OrderByDescending(a => a.FechaInicio)
            .ToListAsync(ct);
    }

    public async Task<Resultado<AsignacionResponse>> CrearAsync(Guid colaboradorId, CrearAsignacionRequest request, CancellationToken ct)
    {
        if (request.ProyectoId is null)
            return AsignacionErrores.ProyectoRequerido;

        if (request.FechaInicio is null)
            return AsignacionErrores.FechaInicioInvalida;

        if (request.FechaSalida is not null && request.FechaSalida < request.FechaInicio)
            return AsignacionErrores.FechaSalidaInvalida;

        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return AsignacionErrores.ColaboradorNoEncontrado(colaboradorId);

        var proyecto = await db.Proyectos.FirstOrDefaultAsync(p => p.Id == request.ProyectoId, ct);
        if (proyecto is null)
            return AsignacionErrores.ProyectoNoEncontrado(request.ProyectoId.Value);

        if (!proyecto.Activo)
            return AsignacionErrores.ProyectoInactivo;

        // No se duplican días dentro del mismo proyecto (RN 25): rechaza si se superpone con otra
        // asignación del mismo colaborador al mismo proyecto.
        var seSuperpone = await db.Asignaciones.AnyAsync(a =>
            a.ColaboradorId == colaboradorId && a.ProyectoId == request.ProyectoId &&
            request.FechaInicio <= (a.FechaSalida ?? DateOnly.MaxValue) &&
            (request.FechaSalida ?? DateOnly.MaxValue) >= a.FechaInicio, ct);
        if (seSuperpone)
            return AsignacionErrores.Superpuesta(request.ProyectoId.Value);

        var asignacion = new Asignacion
        {
            ColaboradorId = colaboradorId,
            ProyectoId = request.ProyectoId.Value,
            FechaInicio = request.FechaInicio.Value,
            FechaSalida = request.FechaSalida
        };

        db.Asignaciones.Add(asignacion);
        await db.SaveChangesAsync(ct);

        return new AsignacionResponse(asignacion.Id, colaboradorId, proyecto.Id, proyecto.Nombre, asignacion.FechaInicio, asignacion.FechaSalida);
    }

    public async Task<Resultado<AsignacionResponse>> FinalizarAsync(
        Guid colaboradorId, Guid asignacionId, FinalizarAsignacionRequest request, CancellationToken ct)
    {
        if (request.FechaSalida is null)
            return AsignacionErrores.FechaSalidaInvalida;

        var asignacion = await db.Asignaciones.FirstOrDefaultAsync(a => a.Id == asignacionId && a.ColaboradorId == colaboradorId, ct);
        if (asignacion is null)
            return AsignacionErrores.NoEncontrado(asignacionId);

        if (request.FechaSalida < asignacion.FechaInicio)
            return AsignacionErrores.FechaSalidaInvalida;

        asignacion.FechaSalida = request.FechaSalida;
        await db.SaveChangesAsync(ct);

        var proyecto = await db.Proyectos.AsNoTracking().FirstAsync(p => p.Id == asignacion.ProyectoId, ct);
        return new AsignacionResponse(asignacion.Id, colaboradorId, proyecto.Id, proyecto.Nombre, asignacion.FechaInicio, asignacion.FechaSalida);
    }

    public async Task<Resultado<bool>> EliminarAsync(Guid colaboradorId, Guid asignacionId, CancellationToken ct)
    {
        var asignacion = await db.Asignaciones.FirstOrDefaultAsync(a => a.Id == asignacionId && a.ColaboradorId == colaboradorId, ct);
        if (asignacion is null)
            return AsignacionErrores.NoEncontrado(asignacionId);

        if (asignacion.FechaInicio <= DateOnly.FromDateTime(DateTime.UtcNow))
            return AsignacionErrores.NoEliminable;

        db.Asignaciones.Remove(asignacion);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
