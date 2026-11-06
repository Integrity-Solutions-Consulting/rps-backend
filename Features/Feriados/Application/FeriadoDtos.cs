namespace rps_backend.Features.Feriados.Application;

// ===== Requests =====

public record CrearFeriadoRequest(DateOnly? Fecha, string? Nombre);

// ===== Responses =====

public record FeriadoResponse(Guid Id, DateOnly Fecha, string Nombre, DateTime CreadoEn);

public record ImportarFeriadosResponse(int Anio, int Creados, int Omitidos);
