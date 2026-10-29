using rps_backend.Shared;

namespace rps_backend.Features.Documentos.Application;

// Catálogo de errores de la feature Documentos.
public static class DocumentoErrores
{
    // ===== No encontrado =====

    public static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Documento.NoEncontrado", $"No existe un documento con id '{id}'.");

    public static Error ColaboradorNoEncontrado(Guid id) =>
        Error.NoEncontrado("Documento.ColaboradorNoEncontrado", $"No existe un colaborador con id '{id}'.");

    public static Error ContratoNoEncontrado(Guid id) =>
        Error.NoEncontrado("Documento.ContratoNoEncontrado", $"No existe un contrato con id '{id}'.");

    public static Error LiquidacionNoEncontrado(Guid id) =>
        Error.NoEncontrado("Documento.LiquidacionNoEncontrado", $"No existe una liquidación con id '{id}'.");

    // ===== Validaciones =====

    public static readonly Error ReferenciaInvalida =
        Error.Validacion("Documento.ReferenciaInvalida", "referencia", "Debe venir exactamente una de: colaboradorId, contratoId o liquidacionId.");

    public static readonly Error ArchivoVacio =
        Error.Validacion("Documento.ArchivoVacio", "archivo", "Es obligatorio y no puede estar vacío.");

    public static readonly Error ArchivoDemasiadoGrande =
        Error.Validacion("Documento.ArchivoDemasiadoGrande", "archivo", "No puede superar los 10 MB.");

    public static readonly Error TipoArchivoNoPermitido =
        Error.Validacion("Documento.TipoArchivoNoPermitido", "archivo", "Tipo de archivo no permitido (solo PDF, JPG, PNG, TXT o Word).");
}
