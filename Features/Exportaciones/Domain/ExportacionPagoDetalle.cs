namespace rps_backend.Features.Exportaciones.Domain;

// Un colaborador dentro de una versión de exportación, con los valores tal como salieron (RN 55).
public class ExportacionPagoDetalle
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ExportacionPagoId { get; set; }

    public Guid LiquidacionId { get; set; }

    public Guid ColaboradorId { get; set; }

    public string TmrColaboradorId { get; set; } = string.Empty;

    // Valor del corte (ValorPreparado) en el momento de exportar.
    public decimal Valor { get; set; }

    public string? Banco { get; set; }

    // Solo enmascarada (****1234); el número completo va únicamente dentro del archivo.
    public string? CuentaEnmascarada { get; set; }
}
