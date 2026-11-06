using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Domain;

// Distribución del neto consolidado de una Liquidación según la Frecuencia del colaborador
// (RN 36-40): uno (FinDeMes) o dos (DosPagos: Quincena + FinDeMes).
public class Corte
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid LiquidacionId { get; set; }

    public TipoCorte Tipo { get; set; }

    // Antes de recuperar anticipos adicionales (RN 38).
    public decimal ValorBase { get; set; }

    public decimal RecuperacionAdicional { get; set; }

    // ValorBase - RecuperacionAdicional.
    public decimal ValorPreparado { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
