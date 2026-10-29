using rps_backend.Shared;

namespace rps_backend.Features.Anticipos.Domain;

public class Anticipo
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public decimal Monto { get; set; }

    public DateOnly FechaSolicitud { get; set; }

    public EstadoAnticipo Estado { get; set; } = EstadoAnticipo.Pendiente;

    // Honorario que lo descontó; null hasta que queda Descontado.
    public Guid? HonorarioId { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
