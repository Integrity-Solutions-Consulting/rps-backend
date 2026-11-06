namespace rps_backend.Features.Auth.Domain;

public class Rol
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}
