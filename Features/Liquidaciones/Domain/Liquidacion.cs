using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Domain;

public class Liquidacion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public DateOnly Periodo { get; set; }

    public decimal MontoTotal { get; set; }

    public EstadoLiquidacion Estado { get; set; } = EstadoLiquidacion.Generada;

    public DateTime? PagadaEn { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
