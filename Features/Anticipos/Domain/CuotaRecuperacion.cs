using rps_backend.Shared;

namespace rps_backend.Features.Anticipos.Domain;

// Detalle del plan de recuperación de un AnticipoAdicional: mes, corte e importe (RN 44, 46).
public class CuotaRecuperacion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid AnticipoAdicionalId { get; set; }

    public DateOnly Periodo { get; set; }

    public TipoCorte Corte { get; set; }

    public decimal Importe { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
