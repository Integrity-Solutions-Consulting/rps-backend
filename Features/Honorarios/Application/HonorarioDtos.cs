using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// ===== Requests =====

public record GenerarHonorariosRequest(DateOnly? Periodo);

// ===== Responses =====

public record GenerarHonorariosResponse(int Generados, int Omitidos);

public record HonorarioResponse(
    Guid Id,
    Guid ColaboradorId,
    Guid ProyectoId,
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
