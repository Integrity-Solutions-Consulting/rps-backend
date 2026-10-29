using rps_backend.Features.Honorarios.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Application;

// ===== Requests =====

public record GenerarLiquidacionesRequest(DateOnly? Periodo);

// ===== Responses =====

public record GenerarLiquidacionesResponse(int Generadas, int HonorariosIncluidos);

public record LiquidacionResponse(
    Guid Id,
    Guid ColaboradorId,
    DateOnly Periodo,
    decimal MontoTotal,
    EstadoLiquidacion Estado,
    DateTime? PagadaEn,
    DateTime CreadoEn,
    DateTime ActualizadoEn);

public record LiquidacionDetalleResponse(
    Guid Id,
    Guid ColaboradorId,
    DateOnly Periodo,
    decimal MontoTotal,
    EstadoLiquidacion Estado,
    DateTime? PagadaEn,
    DateTime CreadoEn,
    DateTime ActualizadoEn,
    List<HonorarioResponse> Honorarios);
