using rps_backend.Shared;

namespace rps_backend.Features.Auth.Application;

// Catálogo de errores de la feature Auth.
public static class AuthErrores
{
    // ===== Validaciones =====

    public static readonly Error CredencialesInvalidas =
        Error.Validacion("Auth.CredencialesInvalidas", "password", "Email o contraseña incorrectos.");

    public static Error CuentaBloqueada(DateTime hasta) =>
        Error.Validacion("Auth.CuentaBloqueada", "email", $"Cuenta bloqueada por intentos fallidos hasta {hasta:yyyy-MM-dd HH:mm} UTC.");

    public static readonly Error CuentaInactiva =
        Error.Validacion("Auth.CuentaInactiva", "email", "La cuenta está desactivada.");

    public static readonly Error TokenInvalido =
        Error.Validacion("Auth.TokenInvalido", "refreshToken", "El refresh token es inválido, expiró o ya fue revocado.");

    public static readonly Error PasswordActualIncorrecta =
        Error.Validacion("Auth.PasswordActualIncorrecta", "passwordActual", "La contraseña actual no es correcta.");

    public static readonly Error PasswordInvalida =
        Error.Validacion("Auth.PasswordInvalida", "password", "La contraseña debe tener al menos 10 caracteres, con letras y números.");

    public static readonly Error RolInvalido =
        Error.Validacion("Auth.RolInvalido", "rol", "El rol indicado no existe.");

    // ===== Conflictos =====

    public static Error EmailDuplicado(string email) =>
        Error.Conflicto("Auth.EmailDuplicado", $"Ya existe un usuario con el correo '{email}'.");
}
