namespace rps_backend.Features.Auth.Domain;

public class Sesion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UsuarioId { get; set; }

    public bool Activa { get; set; } = true;

    public DateTime UltimaActividad { get; set; } = DateTime.UtcNow;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
