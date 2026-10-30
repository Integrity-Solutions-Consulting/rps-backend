namespace rps_backend.Features.Proyectos.Application;

// ===== Requests =====

public record CrearProyectoRequest(string? Nombre, string? Cliente);

public record EditarProyectoRequest(string? Nombre, string? Cliente);

// ===== Responses =====

public record ProyectoResponse(Guid Id, string Nombre, string? Cliente, bool Activo, DateTime CreadoEn);
