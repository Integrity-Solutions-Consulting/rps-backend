namespace rps_backend.Features.IntegracionTmr.Application;

public record ConteoSincronizacion(int Creados, int Actualizados, int MarcadosEliminados);

public record AsignacionOmitida(int IdEmpleado, int IdProyecto, string Motivo);

public record SincronizacionTmrResponse(
    DateTime SincronizadoEn,
    ConteoSincronizacion Proyectos,
    ConteoSincronizacion Colaboradores,
    ConteoSincronizacion Asignaciones,
    List<AsignacionOmitida> AsignacionesOmitidas);
