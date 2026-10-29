using rps_backend.Shared;

namespace rps_backend.Features.Anticipos.Application;

// ===== Requests =====

public record CrearAnticipoRequest(decimal? Monto);

// ===== Responses =====

public record AnticipoResponse(
    Guid Id,
    Guid ColaboradorId,
    decimal Monto,
    DateOnly FechaSolicitud,
    EstadoAnticipo Estado,
    Guid? HonorarioId,
    DateTime CreadoEn,
    DateTime ActualizadoEn);
