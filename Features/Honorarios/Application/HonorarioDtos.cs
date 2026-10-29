using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// ===== Requests =====

public record GenerarHonorariosRequest(DateOnly? Periodo);

// ===== Responses =====

public record GenerarHonorariosResponse(int Generados, int Omitidos);

public record HonorarioResponse(
    Guid Id,
    Guid ColaboradorId,
    Guid ContratoId,
    DateOnly Periodo,
    int NumeroPago,
    decimal Monto,
    decimal Descuento,
    EstadoHonorario Estado,
    DateTime CreadoEn,
    DateTime ActualizadoEn);
