using rps_backend.Shared;

namespace rps_backend.Features.Colaboradores.Domain;

public class ColaboradorRps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string TmrColaboradorId { get; set; } = string.Empty;

    public Clasificacion? Clasificacion { get; set; }

    public EstadoConfiguracion EstadoConfiguracion { get; set; } = EstadoConfiguracion.Pendiente;

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}