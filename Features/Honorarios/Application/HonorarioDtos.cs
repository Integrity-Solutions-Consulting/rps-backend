using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// ===== Requests =====

public record GenerarHonorariosRequest(DateOnly? Periodo);

// ===== Responses =====

// Recalculados: ya existían y cambiaron sus días o su condición (ej. una adenda después del primer cálculo).
public record GenerarHonorariosResponse(int Generados, int Omitidos, int Recalculados);

public record HonorarioResponse(
    Guid Id,
    Guid ColaboradorId,
    // Null: honorario de un contrato que se paga por contrato (sin proyecto).
    Guid? ProyectoId,
    Guid ContratoId,
    DateOnly Periodo,
    int DiasReales,
    int DiasReconocidos,
    Modalidad Modalidad,
    decimal ProporcionalAcordado,
    decimal Subtotal,
    decimal Iva,
    decimal TotalDocumento,
    decimal RetencionFuente,
    decimal RetencionIva,
    decimal TotalRetenciones,
    decimal Neto,
    Guid? LiquidacionId,
    DateTime CreadoEn);
