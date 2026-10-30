namespace rps_backend.Features.Proyectos.Application;

// ===== Requests =====

public record CrearAsignacionRequest(Guid? ProyectoId, DateOnly? FechaInicio, DateOnly? FechaSalida);

public record FinalizarAsignacionRequest(DateOnly? FechaSalida);

// ===== Responses =====

public record AsignacionResponse(
    Guid Id, Guid ColaboradorId, Guid ProyectoId, string ProyectoNombre, DateOnly FechaInicio, DateOnly? FechaSalida);
