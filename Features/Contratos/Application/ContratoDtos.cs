namespace rps_backend.Features.Contratos.Application;

// ===== Requests =====

// ProyectoIds puede ser null o vacío: un contrato puede cubrir uno, varios o ningún proyecto
// (personal de RR. HH. sin proyecto en TMR todavía tiene contrato).
// Indefinido = true ignora FechaFin: el contrato queda sin fecha de fin (nunca vence, sin adendas).
public record CrearContratoRequest(
    List<Guid>? ProyectoIds, DateOnly? FechaInicio, DateOnly? FechaFin, string? Referencia, string? Observaciones,
    bool Indefinido = false);

// Sin FechaFin: la fecha de fin original no se edita (RN 12). Para extenderla se usa una Adenda.
// Sin ProyectoIds: la lista de proyectos de un contrato no se edita después de creado — para
// cubrir un proyecto nuevo se registra otro contrato.
public record EditarContratoRequest(string? Referencia, string? Observaciones);

public record CrearAdendaRequest(DateOnly? NuevaFechaFin, string? Observaciones);

// ===== Responses =====

public record ProyectoDeContratoResponse(Guid ProyectoId, string ProyectoNombre);

public record ContratoResponse(
    Guid Id,
    Guid ColaboradorId,
    List<ProyectoDeContratoResponse> Proyectos,
    DateOnly FechaInicio,
    // Null = indefinido (sin fecha de fin).
    DateOnly? FechaFin,
    DateOnly? FechaFinVigente,
    string? Referencia,
    string? Observaciones,
    bool Vigente);

public record AdendaResponse(Guid Id, Guid ContratoId, DateOnly NuevaFechaFin, DateOnly FechaRegistro, string? Observaciones);
