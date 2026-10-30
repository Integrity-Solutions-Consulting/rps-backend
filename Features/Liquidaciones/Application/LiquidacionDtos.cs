using rps_backend.Features.Honorarios.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Application;

// ===== Requests =====

public record GenerarLiquidacionesRequest(DateOnly? Periodo);

// ===== Responses =====

public record GenerarLiquidacionesResponse(int Generadas, int HonorariosIncluidos);

public record CorteResponse(Guid Id, TipoCorte Tipo, decimal ValorBase, decimal RecuperacionAdicional, decimal ValorPreparado);

public record LiquidacionResponse(
    Guid Id,
    Guid ColaboradorId,
    DateOnly Periodo,
    decimal NetoConsolidado,
    bool Revisado,
    DateTime? RevisadoEn,
    DateTime CreadoEn,
    List<CorteResponse> Cortes);

public record LiquidacionDetalleResponse(
    Guid Id,
    Guid ColaboradorId,
    DateOnly Periodo,
    decimal NetoConsolidado,
    bool Revisado,
    DateTime? RevisadoEn,
    DateTime CreadoEn,
    List<CorteResponse> Cortes,
    List<HonorarioResponse> Honorarios);
