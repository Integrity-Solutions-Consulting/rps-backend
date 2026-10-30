using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Proyectos.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Proyectos.Application;

// Reglas de negocio de Proyectos. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
// CRUD manual mientras no se integra TMR (fuente real de proyectos a futuro).
public class ProyectosService(RpsDbContext db)
{
    public async Task<List<ProyectoResponse>> ListarAsync(bool? activo, CancellationToken ct)
    {
        var query = db.Proyectos.AsNoTracking();

        if (activo is not null)
            query = query.Where(p => p.Activo == activo);

        return await query
            .OrderBy(p => p.Nombre)
            .Select(p => new ProyectoResponse(p.Id, p.Nombre, p.Cliente, p.Activo, p.CreadoEn))
            .ToListAsync(ct);
    }

    public async Task<Resultado<ProyectoResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var proyecto = await db.Proyectos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (proyecto is null)
            return ProyectoErrores.NoEncontrado(id);

        return AResponse(proyecto);
    }

    public async Task<Resultado<ProyectoResponse>> CrearAsync(CrearProyectoRequest request, CancellationToken ct)
    {
        var validacion = Validar(request.Nombre, request.Cliente, out var nombre, out var cliente);
        if (validacion is not null)
            return validacion;

        var proyecto = new Proyecto { Nombre = nombre, Cliente = cliente };

        db.Proyectos.Add(proyecto);
        await db.SaveChangesAsync(ct);

        return AResponse(proyecto);
    }

    public async Task<Resultado<ProyectoResponse>> EditarAsync(Guid id, EditarProyectoRequest request, CancellationToken ct)
    {
        var validacion = Validar(request.Nombre, request.Cliente, out var nombre, out var cliente);
        if (validacion is not null)
            return validacion;

        var proyecto = await db.Proyectos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (proyecto is null)
            return ProyectoErrores.NoEncontrado(id);

        proyecto.Nombre = nombre;
        proyecto.Cliente = cliente;
        await db.SaveChangesAsync(ct);

        return AResponse(proyecto);
    }

    public async Task<Resultado<ProyectoResponse>> CambiarActivoAsync(Guid id, bool activo, CancellationToken ct)
    {
        var proyecto = await db.Proyectos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (proyecto is null)
            return ProyectoErrores.NoEncontrado(id);

        if (proyecto.Activo != activo)
        {
            proyecto.Activo = activo;
            await db.SaveChangesAsync(ct);
        }

        return AResponse(proyecto);
    }

    private static Error? Validar(string? nombreInput, string? clienteInput, out string nombre, out string? cliente)
    {
        nombre = nombreInput?.Trim() ?? string.Empty;
        cliente = string.IsNullOrWhiteSpace(clienteInput) ? null : clienteInput.Trim();

        if (string.IsNullOrEmpty(nombre) || nombre.Length > 200)
            return ProyectoErrores.NombreInvalido;

        if (cliente?.Length > 200)
            return ProyectoErrores.ClienteInvalido;

        return null;
    }

    private static ProyectoResponse AResponse(Proyecto p) => new(p.Id, p.Nombre, p.Cliente, p.Activo, p.CreadoEn);
}
