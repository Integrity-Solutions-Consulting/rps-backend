namespace rps_backend.Features.Proyectos.Domain;

public class Proyecto
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    // Id en TMR. Nulo si el proyecto se creó a mano.
    public string? TmrProyectoId { get; set; }

    public DateTime? SincronizadoEn { get; set; }

    public bool EliminadoEnOrigen { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Cliente { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
