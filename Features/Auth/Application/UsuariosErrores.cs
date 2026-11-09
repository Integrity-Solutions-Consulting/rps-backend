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

    public static readonly Error PropioRol =
        Error.Conflicto("Usuarios.PropioRol", "No puedes cambiar el rol de tu propia cuenta.");

    public static readonly Error RolInvalido =
        Error.Validacion("Usuarios.RolInvalido", "rol", "El rol indicado no existe o está desactivado.");

    public static readonly Error SinPermisoParaCambiarRol =
        Error.Conflicto("Usuarios.SinPermisoParaCambiarRol",
            "Tu rol no puede cambiar el rol de esta cuenta ni asignar los roles Administrador o Administrador Contable.");

    public static readonly Error UltimoAdministradorRol =
        Error.Conflicto("Usuarios.UltimoAdministradorRol", "No se puede quitar el rol Administrador al último Administrador activo.");
}
