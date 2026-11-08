namespace rps_backend.Features.Exportaciones.Domain;

// Un colaborador dentro de una Exportacion, con su importe consolidado de ese corte (RN 54). Se
// ancla a la Liquidacion, no al detalle por proyecto, para no duplicar la obligación. Un
// colaborador no se repite dentro de la misma Exportacion.
public class ExportacionItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ExportacionId { get; set; }

    public Guid ColaboradorId { get; set; }

    public Guid LiquidacionId { get; set; }

    public decimal Importe { get; set; }
}
