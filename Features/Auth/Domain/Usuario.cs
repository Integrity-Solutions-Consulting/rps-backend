namespace rps_backend.Features.Auth.Domain;

public class Usuario
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Email { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string HashPassword { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public int IntentosFallidos { get; set; }

    public DateTime? BloqueadoHasta { get; set; }

    // Fuerza un cambio de contraseña en el próximo login (ej. contraseña temporal del bootstrap).
    public bool DebeCambiarPassword { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;

    public List<UsuarioRol> Roles { get; set; } = [];
}
