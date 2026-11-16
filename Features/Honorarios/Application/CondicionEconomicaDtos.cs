using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// ===== Requests =====

// ProyectoId null: pago por contrato (un solo importe fijo para todo el contrato, tenga los
// proyectos que tenga). Con valor: pago por proyecto, el importe de ese proyecto del contrato.
// FechaInicioVigencia: cualquier día; si cae a mitad de mes, ese mes se paga en tramos.
// Reemplazar: si ya hay una para la misma fecha, se sobrescribe en vez de rechazarla (el formulario
// de contratos, al cambiar un honorario).
public record CrearCondicionEconomicaRequest(
    Guid? ProyectoId, Guid? ContratoId, decimal? ImporteMensual, Modalidad? Modalidad, DateOnly? FechaInicioVigencia, string? Observaciones,
    bool Reemplazar = false);

// ===== Responses =====

public record CondicionEconomicaResponse(
    Guid Id, Guid ColaboradorId, Guid? ProyectoId, string? ProyectoNombre, Guid ContratoId,
    decimal ImporteMensual, Modalidad Modalidad, DateOnly FechaInicioVigencia, string? Observaciones);
