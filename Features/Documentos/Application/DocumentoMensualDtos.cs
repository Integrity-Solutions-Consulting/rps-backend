using rps_backend.Shared;

namespace rps_backend.Features.Documentos.Application;

// ===== Requests =====

public record RegistrarDocumentoMensualRequest(DateOnly? Periodo, TipoDocumento? Tipo, string? Numero, DateOnly? FechaEmision);

public record EditarDocumentoMensualRequest(TipoDocumento? Tipo, string? Numero, DateOnly? FechaEmision);

// ===== Responses =====

public record DocumentoMensualResponse(
    Guid Id,
    Guid ColaboradorId,
    DateOnly Periodo,
    TipoDocumento Tipo,
    string Numero,
    DateOnly FechaEmision,
    DateTime MarcadoEn);
