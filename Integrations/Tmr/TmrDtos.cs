namespace rps_backend.Integrations.Tmr;

// Lo que entrega TMR en /api/integracion/rps. Las fechas llegan como texto yyyy-MM-dd y pueden venir vacías.
// TipoContrato: código del catálogo TCT de TMR (CON, PAS, FJ, MT, COS) o null si no tiene.
public record TmrColaboradorDto(int Id, string? Nombres, string? Apellidos, string? Identificacion, string? Correo, bool Activo,
    string? TipoContrato = null);

public record TmrProyectoDto(int Id, string Nombre, string? Cliente, bool Activo);

public record TmrAsignacionDto(int IdEmpleado, int IdProyecto, string? FechaInicio, string? FechaSalida, bool EstadoAsignacion);
