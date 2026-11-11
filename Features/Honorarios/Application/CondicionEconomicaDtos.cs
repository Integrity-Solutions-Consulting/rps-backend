using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// ===== Requests =====

// Reemplazar: si ya hay una condición de ese proyecto que empieza el mismo mes, se sobrescribe en
// vez de rechazarla (el formulario de contratos, al cambiar un honorario que rige desde este mes).
public record CrearCondicionEconomicaRequest(
    Guid? ProyectoId, Guid? ContratoId, decimal? ImporteMensual, Modalidad? Modalidad, DateOnly? MesInicioVigencia, string? Observaciones,
    bool Reemplazar = false);

// ===== Responses =====

public record CondicionEconomicaResponse(
    Guid Id, Guid ColaboradorId, Guid ProyectoId, string ProyectoNombre, Guid ContratoId,
    decimal ImporteMensual, Modalidad Modalidad, DateOnly MesInicioVigencia, string? Observaciones);
