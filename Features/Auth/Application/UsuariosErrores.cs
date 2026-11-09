using rps_backend.Shared;

namespace rps_backend.Features.Auth.Application;

// Catálogo de errores de la administración de usuarios.
public static class UsuariosErrores
{
    public static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Usuarios.NoEncontrado", $"No existe el usuario '{id}'.");

    public static readonly Error PropioEstado =
        Error.Conflicto("Usuarios.PropioEstado", "No puedes activar ni desactivar tu propia cuenta.");

    public static readonly Error SinPermisoSobreRol =
        Error.Conflicto("Usuarios.SinPermisoSobreRol", "Solo puedes activar o desactivar cuentas de Asistente Contable.");

    public static readonly Error UltimoAdministrador =
        Error.Conflicto("Usuarios.UltimoAdministrador", "No se puede desactivar al último Administrador activo.");
}
