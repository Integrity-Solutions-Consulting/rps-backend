using rps_backend.Shared;

namespace rps_backend.Features.Documentos.Application;

// Catálogo de errores de la feature Documentos (documento mensual: factura/nota de venta).
public static class DocumentoMensualErrores
{
    // ===== No encontrado =====

    public static Error ColaboradorNoEncontrado(Guid id) =>
        Error.NoEncontrado("DocumentoMensual.ColaboradorNoEncontrado", $"No existe un colaborador con id '{id}'.");

    public static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("DocumentoMensual.NoEncontrado", $"El colaborador no tiene un documento mensual con id '{id}'.");

    // ===== Conflictos =====

    public static Error YaRegistrado(DateOnly periodo) =>
        Error.Conflicto(
            "DocumentoMensual.YaRegistrado",
            $"Ya existe un documento mensual registrado para el período {periodo:yyyy-MM-dd}. Usa editar para corregirlo.");

    // ===== Validaciones =====

    public static readonly Error PeriodoInvalido =
        Error.Validacion("DocumentoMensual.PeriodoInvalido", "periodo", "Es obligatorio y debe ser el primer día del mes (ej. 2026-11-01).");

    public static readonly Error TipoInvalido =
        Error.Validacion("DocumentoMensual.TipoInvalido", "tipo", "Es obligatorio (Factura o NotaDeVenta).");

    public static readonly Error NumeroInvalido =
        Error.Validacion("DocumentoMensual.NumeroInvalido", "numero", "Es obligatorio y debe tener máximo 50 caracteres.");

    public static readonly Error FechaEmisionInvalida =
        Error.Validacion("DocumentoMensual.FechaEmisionInvalida", "fechaEmision", "Es obligatoria.");
}
