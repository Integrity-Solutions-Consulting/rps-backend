namespace rps_backend.Features.Contratos.Application;

// ===== Requests =====

// ProyectoIds puede ser null o vacío: un contrato puede cubrir uno, varios o ningún proyecto
// (personal de RR. HH. sin proyecto en TMR todavía tiene contrato).
// Indefinido = true ignora FechaFin: el contrato queda sin fecha de fin (nunca vence, sin adendas).
public record CrearContratoRequest(
    List<Guid>? ProyectoIds, DateOnly? FechaInicio, DateOnly? FechaFin, string? Referencia, string? Observaciones,
    bool Indefinido = false);

// Sin FechaFin ni Indefinido: esa decisión no se edita después de creada (RN 12). Para extender la
// vigencia se usa una Adenda. El resto (proyectos, fecha de inicio, referencia, observaciones) sí
// se puede corregir.
public record EditarContratoRequest(List<Guid>? ProyectoIds, DateOnly? FechaInicio, string? Referencia, string? Observaciones);

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
