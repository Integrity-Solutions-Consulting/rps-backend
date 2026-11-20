using Microsoft.EntityFrameworkCore;
using rps_backend.Persistence;

namespace rps_backend.Features.Auth.Application;

// Módulos que puede usar un usuario: la unión de los permisos de sus roles activos.
// El Administrador siempre tiene todos. Se lee de la base en cada petición (no del token), así que un
// cambio de permisos, de rol o una cuenta desactivada se aplica en la siguiente petición, sin esperar
// a que el access token expire.
public class PermisosUsuarioService(RpsDbContext db)
{
    private Guid _usuarioEnCache;
    private IReadOnlySet<string>? _cache;

    public async Task<IReadOnlySet<string>> ObtenerAsync(Guid usuarioId, CancellationToken ct)
    {
        if (_cache is not null && _usuarioEnCache == usuarioId)
            return _cache;

        HashSet<string> permisos = [];
        var activo = await db.Usuarios.AsNoTracking().AnyAsync(u => u.Id == usuarioId && u.Activo, ct);
        if (activo)
        {
            var roles = await db.UsuariosRoles.AsNoTracking()
                .Where(ur => ur.UsuarioId == usuarioId)
                .Join(db.Roles.Where(r => r.Activo), ur => ur.RolId, r => r.Id, (ur, r) => new { r.Nombre, r.Permisos })
                .ToListAsync(ct);

            if (roles.Any(r => r.Nombre == PermisosModulo.RolAdministrador))
                permisos = [.. PermisosModulo.Todos];
            else
                permisos = [.. PermisosModulo.Normalizar(roles.SelectMany(r => r.Permisos))];
        }

        _usuarioEnCache = usuarioId;
        _cache = permisos;
        return permisos;
    }

    // Misma lista, en el orden de PermisosModulo.Todos (la que se envía al frontend).
    public async Task<List<string>> ListarAsync(Guid usuarioId, CancellationToken ct)
    {
        var permisos = await ObtenerAsync(usuarioId, ct);
        return PermisosModulo.Todos.Where(permisos.Contains).ToList();
    }
}
