using rps_backend.Features.IntegracionTmr.Domain;

namespace rps_backend.Features.IntegracionTmr.Application;

public record ConteoSincronizacion(int Creados, int Actualizados, int MarcadosEliminados);

public record AsignacionOmitida(int IdEmpleado, int IdProyecto, string Motivo);

public record SincronizacionTmrResponse(
    DateTime SincronizadoEn,
    ConteoSincronizacion Proyectos,
    ConteoSincronizacion Colaboradores,
    ConteoSincronizacion Asignaciones,
    int CambiosDetectados,
    List<AsignacionOmitida> AsignacionesOmitidas);

// TieneLiquidacion y TieneLiquidacionRevisada se refieren a liquidaciones de PeriodoDesde en adelante.
public record CambioTmrResponse(
    Guid Id,
    Guid ColaboradorId,
    string? Nombres,
    string? Apellidos,
    Guid? ProyectoId,
    string? ProyectoNombre,
    TipoCambioTmr Tipo,
    string? ValorAnterior,
    string? ValorNuevo,
    DateOnly? PeriodoDesde,
    DateTime DetectadoEn,
    bool TieneLiquidacion,
    bool TieneLiquidacionRevisada,
    bool Revisado,
    DateTime? RevisadoEn,
    Guid? RevisadoPorUsuarioId);
