namespace rps_backend.Features.Auth.Application;

public record RolResponse(
    Guid Id,
    string Nombre,
    string? Descripcion,
    bool Activo,
    IReadOnlyList<string> Permisos,
    int Usuarios);

public record CrearRolRequest(string Nombre, string? Descripcion, List<string>? Permisos);

public record ActualizarPermisosRolRequest(List<string>? Permisos);

public record CambiarEstadoRolRequest(bool Activo);
