using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Domain;

public class Honorario
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public Guid ContratoId { get; set; }

    public DateOnly Periodo { get; set; }

    public int NumeroPago { get; set; }

    // Neto a pagar (bruto - Descuento).
    public decimal Monto { get; set; }

    // Suma de anticipos descontados al generar este honorario.
    public decimal Descuento { get; set; } = 0;

    public EstadoHonorario Estado { get; set; } = EstadoHonorario.Pendiente;

    // Null = aún no incluido en ninguna liquidación.
    public Guid? LiquidacionId { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
