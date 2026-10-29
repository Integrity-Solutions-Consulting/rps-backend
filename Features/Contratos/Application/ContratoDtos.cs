namespace rps_backend.Features.Contratos.Application;

// ===== Requests =====

public record CrearContratoRequest(string? TipoServicio, decimal? Tarifa, DateOnly? FechaInicio, DateOnly? FechaFin);

public record EditarContratoRequest(string? TipoServicio, decimal? Tarifa, DateOnly? FechaInicio, DateOnly? FechaFin);

// ===== Responses =====

public record ContratoResponse(
    Guid Id,
    Guid ColaboradorId,
    string TipoServicio,
    decimal Tarifa,
    DateOnly FechaInicio,
    DateOnly? FechaFin,
    bool Vigente);
