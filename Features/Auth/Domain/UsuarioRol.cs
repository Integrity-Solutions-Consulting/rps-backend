namespace rps_backend.Features.Auth.Domain;

// Tabla puente usuario-rol: permite que un usuario tenga varios roles y que agregar roles
// nuevos sea solo insertar filas, sin migrar el esquema de Usuario.
public class UsuarioRol
{
    public Guid UsuarioId { get; set; }

    public Guid RolId { get; set; }

    public DateTime AsignadoEn { get; set; } = DateTime.UtcNow;

    public Usuario? Usuario { get; set; }

    public Rol? Rol { get; set; }
}
