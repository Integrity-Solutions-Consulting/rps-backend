using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Domain;

public class Honorario
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public Guid ContratoId { get; set; }

    public DateOnly Periodo { get; set; }

    public int NumeroPago { get; set; }

    public decimal Monto { get; set; }

    public EstadoHonorario Estado { get; set; } = EstadoHonorario.Pendiente;

    // Null = aún no incluido en ninguna liquidación.
    public Guid? LiquidacionId { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
