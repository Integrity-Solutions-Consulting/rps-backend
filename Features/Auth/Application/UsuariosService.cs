using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Auth.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Auth.Application;

// Administración de cuentas: listar usuarios, activarlos o desactivarlos y cambiarles el rol.
// La creación sigue en AuthService.RegisterAsync (POST /api/auth/register, solo Administrador).
// Reglas:
//  - Nadie cambia el estado de su propia cuenta.
//  - El Administrador puede activar/desactivar cualquier cuenta; el Administrador Contable solo las de Asistente Contable.
//  - Siempre queda al menos un Administrador activo.
//  - Al desactivar se revocan los refresh tokens y se cierran las sesiones: no puede renovar ni volver a entrar.
//  - Cambiar rol: nunca sobre la propia cuenta, solo a un rol activo y sin dejar el sistema sin Administradores activos.
//    El Administrador cambia cualquier cuenta a cualquier rol. El Administrador Contable solo cuentas que no sean
//    Administrador ni Administrador Contable, y solo hacia roles que tampoco lo sean (misma regla que RolesService). El usuario queda con ese único rol; el cambio llega a su
//    sesión en la próxima renovación del token (AuthService relee los roles) o al volver a entrar.
public class UsuariosService(RpsDbContext db)
{
    private const string RolAdministrador = "Administrador";
    private const string RolAsistente = "Asistente Contable";

    public async Task<List<UsuarioAdminResponse>> ListarAsync(CancellationToken ct)
    {
        var usuarios = await db.Usuarios.AsNoTracking()
            .OrderBy(u => u.Nombre)
            .Select(u => new { u.Id, u.Email, u.Nombre, u.Activo, u.CreadoEn })
            .ToListAsync(ct);

        var roles = await RolesPorUsuarioAsync(null, ct);

        return usuarios
            .Select(u => new UsuarioAdminResponse(u.Id, u.Email, u.Nombre, u.Activo, u.CreadoEn,
                roles.TryGetValue(u.Id, out var r) ? r : []))
            .ToList();
    }

    public async Task<Resultado<UsuarioAdminResponse>> CambiarEstadoAsync(
        Guid actorId, bool actorEsAdministrador, Guid usuarioId, bool activo, CancellationToken ct)
    {
        if (usuarioId == actorId)
            return UsuariosErrores.PropioEstado;

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        if (usuario is null)
            return UsuariosErrores.NoEncontrado(usuarioId);

        var roles = (await RolesPorUsuarioAsync(usuarioId, ct)).GetValueOrDefault(usuarioId) ?? [];

        if (!actorEsAdministrador && !(roles.Count > 0 && roles.All(r => r == RolAsistente)))
            return UsuariosErrores.SinPermisoSobreRol;

        if (usuario.Activo != activo)
        {
            if (!activo && roles.Contains(RolAdministrador) && !await HayOtroAdministradorActivoAsync(usuarioId, ct))
                return UsuariosErrores.UltimoAdministrador;

            await using var transaccion = await db.Database.BeginTransactionAsync(ct);

            usuario.Activo = activo;
            usuario.ActualizadoEn = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            if (!activo)
            {
                await db.RefreshTokens
                    .Where(rt => rt.UsuarioId == usuarioId && !rt.Revocado)
                    .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.Revocado, true), ct);
                await db.Sesiones
                    .Where(s => s.UsuarioId == usuarioId && s.Activa)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Activa, false), ct);
            }

            await transaccion.CommitAsync(ct);
        }

        return new UsuarioAdminResponse(usuario.Id, usuario.Email, usuario.Nombre, usuario.Activo, usuario.CreadoEn, roles);
    }

    public async Task<Resultado<UsuarioAdminResponse>> CambiarRolAsync(
        Guid actorId, bool actorEsAdministrador, Guid usuarioId, string nombreRol, CancellationToken ct)
    {
        if (usuarioId == actorId)
            return UsuariosErrores.PropioRol;

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        if (usuario is null)
            return UsuariosErrores.NoEncontrado(usuarioId);

        var nombre = (nombreRol ?? string.Empty).Trim();
        var rolNuevo = await db.Roles.FirstOrDefaultAsync(r => r.Nombre == nombre && r.Activo, ct);
        if (rolNuevo is null)
            return UsuariosErrores.RolInvalido;

        var rolesActuales = (await RolesPorUsuarioAsync(usuarioId, ct)).GetValueOrDefault(usuarioId) ?? [];

        if (!actorEsAdministrador &&
            (EsRolDeAdministracion(rolNuevo.Nombre) || rolesActuales.Any(EsRolDeAdministracion)))
            return UsuariosErrores.SinPermisoParaCambiarRol;
        if (rolesActuales.Count == 1 && rolesActuales[0] == rolNuevo.Nombre)
            return new UsuarioAdminResponse(usuario.Id, usuario.Email, usuario.Nombre, usuario.Activo, usuario.CreadoEn, rolesActuales);

        if (usuario.Activo && rolesActuales.Contains(RolAdministrador) && rolNuevo.Nombre != RolAdministrador
            && !await HayOtroAdministradorActivoAsync(usuarioId, ct))
            return UsuariosErrores.UltimoAdministradorRol;

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        await db.UsuariosRoles.Where(ur => ur.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);
        db.UsuariosRoles.Add(new UsuarioRol { UsuarioId = usuarioId, RolId = rolNuevo.Id });
        usuario.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await transaccion.CommitAsync(ct);

        return new UsuarioAdminResponse(usuario.Id, usuario.Email, usuario.Nombre, usuario.Activo, usuario.CreadoEn, [rolNuevo.Nombre]);
    }

    private static bool EsRolDeAdministracion(string rol) =>
        rol is RolAdministrador or PermisosModulo.RolAdministradorContable;

    private async Task<bool> HayOtroAdministradorActivoAsync(Guid excluirId, CancellationToken ct) =>
        await db.UsuariosRoles
            .Where(ur => ur.UsuarioId != excluirId)
            .Join(db.Roles.Where(r => r.Nombre == RolAdministrador), ur => ur.RolId, r => r.Id, (ur, r) => ur.UsuarioId)
            .Join(db.Usuarios.Where(u => u.Activo), id => id, u => u.Id, (id, u) => u.Id)
            .AnyAsync(ct);

    private async Task<Dictionary<Guid, IReadOnlyList<string>>> RolesPorUsuarioAsync(Guid? usuarioId, CancellationToken ct)
    {
        var consulta = db.UsuariosRoles.AsNoTracking();
        if (usuarioId is { } id)
            consulta = consulta.Where(ur => ur.UsuarioId == id);

        var filas = await consulta
            .Join(db.Roles.Where(r => r.Activo), ur => ur.RolId, r => r.Id, (ur, r) => new { ur.UsuarioId, r.Nombre })
            .ToListAsync(ct);

        return filas
            .GroupBy(f => f.UsuarioId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(f => f.Nombre).ToList());
    }
}
