namespace rps_backend.Features.Proyectos.Domain;

public class Proyecto
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Nombre { get; set; } = string.Empty;

    public string? Cliente { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
