using rps_backend.Shared;

namespace rps_backend.Features.Auth.Application;

// Catálogo de errores de la administración de roles.
public static class RolesErrores
{
    public static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Roles.NoEncontrado", $"No existe el rol '{id}'.");

    public static readonly Error NombreRequerido =
        Error.Validacion("Roles.NombreRequerido", "nombre", "Escribe el nombre del rol.");

    public static readonly Error NombreMuyLargo =
        Error.Validacion("Roles.NombreMuyLargo", "nombre", "El nombre del rol admite hasta 50 caracteres.");

    public static readonly Error DescripcionMuyLarga =
        Error.Validacion("Roles.DescripcionMuyLarga", "descripcion", "La descripción admite hasta 200 caracteres.");

    public static readonly Error PermisoInvalido =
        Error.Validacion("Roles.PermisoInvalido", "permisos", "Uno de los permisos indicados no existe.");

    public static Error NombreDuplicado(string nombre) =>
        Error.Conflicto("Roles.NombreDuplicado", $"Ya existe un rol llamado '{nombre}'.");

    public static readonly Error RolAdministradorProtegido =
        Error.Conflicto("Roles.AdministradorProtegido", "El rol Administrador no se puede desactivar ni cambiar sus permisos.");

    public static readonly Error SinPermisoSobreRol =
        Error.Conflicto("Roles.SinPermisoSobreRol", "Tu rol no puede administrar este rol.");
}
