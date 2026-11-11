namespace rps_backend.Features.Contratos.Application;

// ===== Requests =====

// ProyectoIds puede ser null o vacío: un contrato puede cubrir uno, varios o ningún proyecto
// (personal de RR. HH. sin proyecto en TMR todavía tiene contrato).
// Indefinido = true ignora FechaFin: el contrato queda sin fecha de fin (nunca vence; sus adendas no llevan fecha de fin).
public record CrearContratoRequest(
    List<Guid>? ProyectoIds, DateOnly? FechaInicio, DateOnly? FechaFin, string? Referencia, string? Observaciones,
    bool Indefinido = false);

// Editar es para corregir (referencia, observaciones, fecha de inicio mal escrita). Los proyectos
// solo se pueden AGREGAR, y solo si el contrato se paga por contrato; cualquier otro cambio del
// contrato (fin, quitar proyectos, honorario) es una Adenda. FechaFin/Indefinido no se tocan (RN 12).
public record EditarContratoRequest(List<Guid>? ProyectoIds, DateOnly? FechaInicio, string? Referencia, string? Observaciones);

// NuevaFechaFin: obligatoria (igual o posterior al fin vigente) salvo en contratos indefinidos,
// donde va null. ProyectoIds: null = los proyectos no cambian; con valor, la lista completa nueva.
// Cambios: resumen legible de lo que cambia (lo arma el formulario).
public record CrearAdendaRequest(DateOnly? NuevaFechaFin, string? Observaciones, List<Guid>? ProyectoIds = null, string? Cambios = null);

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

public record AdendaResponse(Guid Id, Guid ContratoId, DateOnly? NuevaFechaFin, DateOnly FechaRegistro, string? Observaciones, string? Cambios);
