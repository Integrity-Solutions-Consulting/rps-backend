using rps_backend.Shared;

namespace rps_backend.Features.Colaboradores.Domain;

public class ColaboradorFrecuencia
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public Frecuencia Frecuencia { get; set; }

    public DateOnly VigenteDesdeMes { get; set; }
}
