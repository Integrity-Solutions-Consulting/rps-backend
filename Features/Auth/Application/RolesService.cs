using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Auth.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;
using static rps_backend.Features.Auth.Application.PermisosModulo;

namespace rps_backend.Features.Auth.Application;

// Administración de roles (Configuración > Roles).
// Reglas:
//  - Solo el Administrador crea roles.
//  - El rol Administrador no se lista para administrar: no se desactiva ni cambia permisos (siempre tiene todos).
//  - El Administrador administra los demás roles; el Administrador Contable solo los que no son
//    Administrador ni Administrador Contable (Asistente Contable y los roles nuevos).
//  - Desactivar un rol lo quita de los tokens de sus usuarios en el próximo login o renovación
//    (AuthService ya filtra roles inactivos) e impide asignarlo a usuarios nuevos.
public class RolesService(RpsDbContext db)
{
    private const int MaxNombre = 50;
    private const int MaxDescripcion = 200;

    public async Task<List<RolResponse>> ListarAsync(bool actorEsAdministrador, CancellationToken ct)
    {
        var roles = await db.Roles.AsNoTracking().OrderBy(r => r.Nombre).ToListAsync(ct);
        var conteo = await db.UsuariosRoles.AsNoTracking()
            .GroupBy(ur => ur.RolId)
            .Select(g => new { RolId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.RolId, x => x.Total, ct);

        return roles
            .Where(r => PuedeAdministrar(actorEsAdministrador, r.Nombre))
            .Select(r => ARespuesta(r, conteo.GetValueOrDefault(r.Id)))
            .ToList();
    }

    // Roles activos que se pueden asignar a un usuario nuevo (incluye Administrador).
    public async Task<List<string>> ListarAsignablesAsync(CancellationToken ct) =>
        await db.Roles.AsNoTracking().Where(r => r.Activo).OrderBy(r => r.Nombre).Select(r => r.Nombre).ToListAsync(ct);

    public async Task<Resultado<RolResponse>> CrearAsync(CrearRolRequest request, CancellationToken ct)
    {
        var nombre = (request.Nombre ?? string.Empty).Trim();
        var descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();

        if (nombre.Length == 0)
            return RolesErrores.NombreRequerido;
        if (nombre.Length > MaxNombre)
            return RolesErrores.NombreMuyLargo;
        if (descripcion is { Length: > MaxDescripcion })
            return RolesErrores.DescripcionMuyLarga;
        if (!SonValidos(request.Permisos))
            return RolesErrores.PermisoInvalido;

        var nombreMinusculas = nombre.ToLower();
        if (await db.Roles.AnyAsync(r => r.Nombre.ToLower() == nombreMinusculas, ct))
            return RolesErrores.NombreDuplicado(nombre);

        var rol = new Rol { Nombre = nombre, Descripcion = descripcion, Permisos = Normalizar(request.Permisos) };
        db.Roles.Add(rol);
        await db.SaveChangesAsync(ct);
        return ARespuesta(rol, 0);
    }

    public async Task<Resultado<RolResponse>> ActualizarPermisosAsync(
        bool actorEsAdministrador, Guid id, ActualizarPermisosRolRequest request, CancellationToken ct)
    {
        if (!SonValidos(request.Permisos))
            return RolesErrores.PermisoInvalido;

        var resultado = await ObtenerAdministrableAsync(actorEsAdministrador, id, ct);
        if (resultado.Error is { } error)
            return error;
        var rol = resultado.Valor!;

        rol.Permisos = Normalizar(request.Permisos);
        await db.SaveChangesAsync(ct);
        return ARespuesta(rol, await ContarUsuariosAsync(rol.Id, ct));
    }

    public async Task<Resultado<RolResponse>> CambiarEstadoAsync(bool actorEsAdministrador, Guid id, bool activo, CancellationToken ct)
    {
        var resultado = await ObtenerAdministrableAsync(actorEsAdministrador, id, ct);
        if (resultado.Error is { } error)
            return error;
        var rol = resultado.Valor!;

        rol.Activo = activo;
        await db.SaveChangesAsync(ct);
        return ARespuesta(rol, await ContarUsuariosAsync(rol.Id, ct));
    }

    private async Task<Resultado<Rol>> ObtenerAdministrableAsync(bool actorEsAdministrador, Guid id, CancellationToken ct)
    {
        var rol = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (rol is null)
            return RolesErrores.NoEncontrado(id);
        if (rol.Nombre == RolAdministrador)
            return RolesErrores.RolAdministradorProtegido;
        if (!PuedeAdministrar(actorEsAdministrador, rol.Nombre))
            return RolesErrores.SinPermisoSobreRol;
        return rol;
    }

    private static bool PuedeAdministrar(bool actorEsAdministrador, string rol) =>
        rol != RolAdministrador && (actorEsAdministrador || rol != RolAdministradorContable);

    private Task<int> ContarUsuariosAsync(Guid rolId, CancellationToken ct) =>
        db.UsuariosRoles.CountAsync(ur => ur.RolId == rolId, ct);

    private static RolResponse ARespuesta(Rol r, int usuarios) =>
        new(r.Id, r.Nombre, r.Descripcion, r.Activo, r.Permisos, usuarios);
}
