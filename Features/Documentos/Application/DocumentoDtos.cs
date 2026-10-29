using rps_backend.Shared;

namespace rps_backend.Features.Documentos.Application;

// ===== Responses =====

public record DocumentoResponse(
    Guid Id,
    Guid? ColaboradorId,
    Guid? ContratoId,
    Guid? LiquidacionId,
    string NombreOriginal,
    string ContentType,
    long TamanoBytes,
    OrigenDocumento Origen,
    DateTime CreadoEn);
