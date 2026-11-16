using rps_backend.Features.Honorarios.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Application;

// ===== Requests =====

public record GenerarLiquidacionesRequest(DateOnly? Periodo);

// ===== Responses =====

// Actualizadas: colaboradores que ya tenían Liquidacion de este período y se recalcularon (honorarios
// nuevos o con días/monto distintos); nunca se crea una segunda (RN 30).
// Eliminadas: se quedaron sin honorarios en el período (ya no les corresponde ningún día).
public record GenerarLiquidacionesResponse(int Generadas, int Actualizadas, int HonorariosIncluidos, int Eliminadas = 0);

// RecuperacionNoAplicada > 0 o AjustePorProrrateo > 0: el corte quedó por debajo de lo programado
// (anticipo o quincena). Es una inconsistencia para que Contabilidad revise, no un error silencioso.
public record CorteResponse(
    Guid Id, TipoCorte Tipo, decimal ValorBase, decimal RecuperacionAdicional, decimal ValorPreparado,
    decimal RecuperacionNoAplicada, decimal AjustePorProrrateo);

public record LiquidacionResponse(
    Guid Id,
    Guid ColaboradorId,
    DateOnly Periodo,
    decimal NetoConsolidado,
    bool Revisado,
    DateTime? RevisadoEn,
    Guid? RevisadoPorUsuarioId,
    DateTime CreadoEn,
    List<CorteResponse> Cortes,
    // Cambió después de calculada (aviso hasta la próxima revisión). Ver Liquidacion.RegistrarModificacion.
    DateTime? ModificadaEn,
    string? MotivoModificacion,
    bool RevisionAnulada);

public record LiquidacionDetalleResponse(
    Guid Id,
    Guid ColaboradorId,
    DateOnly Periodo,
    decimal NetoConsolidado,
    bool Revisado,
    DateTime? RevisadoEn,
    Guid? RevisadoPorUsuarioId,
    DateTime CreadoEn,
    List<CorteResponse> Cortes,
    List<HonorarioResponse> Honorarios,
    DateTime? ModificadaEn,
    string? MotivoModificacion,
    bool RevisionAnulada);
