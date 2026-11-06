namespace rps_backend.Features.Contratos.Application;

// ===== Requests =====

public record CrearContratoRequest(Guid? ProyectoId, DateOnly? FechaInicio, DateOnly? FechaFin, string? Referencia, string? Observaciones);

public record EditarContratoRequest(DateOnly? FechaFin, string? Referencia, string? Observaciones);

public record CrearAdendaRequest(DateOnly? NuevaFechaFin, string? Observaciones);

// ===== Responses =====

public record ContratoResponse(
    Guid Id,
    Guid ColaboradorId,
    Guid ProyectoId,
    string ProyectoNombre,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    DateOnly FechaFinVigente,
    string? Referencia,
    string? Observaciones,
    bool Vigente);

public record AdendaResponse(Guid Id, Guid ContratoId, DateOnly NuevaFechaFin, DateOnly FechaRegistro, string? Observaciones);
