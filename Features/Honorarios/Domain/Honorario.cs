using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Domain;

// Detalle tributario de un proyecto de un colaborador en un período (RN 28). Inmutable una vez
// generado: si cambian los parámetros o la condición económica, se recalcula, no se edita in situ.
public class Honorario
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public Guid ProyectoId { get; set; }

    public Guid ContratoId { get; set; }

    public Guid CondicionEconomicaId { get; set; }

    public DateOnly Periodo { get; set; }

    public int DiasReales { get; set; }

    public int DiasReconocidos { get; set; }

    public Modalidad Modalidad { get; set; }

    // Snapshot de los parámetros usados (RN 29): un cambio posterior en ConfiguracionGlobal
    // no altera silenciosamente este cálculo ya generado.
    public decimal PorcentajeIva { get; set; }

    public decimal PorcentajeRetencionFuente { get; set; }

    public decimal PorcentajeRetencionIva { get; set; }

    public decimal ProporcionalAcordado { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Iva { get; set; }

    public decimal TotalDocumento { get; set; }

    public decimal RetencionFuente { get; set; }

    public decimal RetencionIva { get; set; }

    public decimal TotalRetenciones { get; set; }

    public decimal Neto { get; set; }

    // Null hasta que se consolida en una Liquidación del período.
    public Guid? LiquidacionId { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
