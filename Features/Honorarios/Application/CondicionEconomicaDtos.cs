using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// ===== Requests =====

public record CrearCondicionEconomicaRequest(
    Guid? ProyectoId, Guid? ContratoId, decimal? ImporteMensual, Modalidad? Modalidad, DateOnly? MesInicioVigencia, string? Observaciones);

// ===== Responses =====

public record CondicionEconomicaResponse(
    Guid Id, Guid ColaboradorId, Guid ProyectoId, string ProyectoNombre, Guid ContratoId,
    decimal ImporteMensual, Modalidad Modalidad, DateOnly MesInicioVigencia, string? Observaciones);
