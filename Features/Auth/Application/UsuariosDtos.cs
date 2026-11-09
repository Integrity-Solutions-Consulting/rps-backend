namespace rps_backend.Features.Auth.Application;

// Usuario tal como lo ve quien administra cuentas (pantalla Configuración > Usuarios / Roles).
public record UsuarioAdminResponse(
    Guid Id,
    string Email,
    string Nombre,
    bool Activo,
    DateTime CreadoEn,
    IReadOnlyList<string> Roles);

public record CambiarEstadoUsuarioRequest(bool Activo);

public record CambiarRolUsuarioRequest(string Rol);
