using rps_backend.Shared;

namespace rps_backend.Features.Colaboradores.Domain;

public class ColaboradorRps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string TmrColaboradorId { get; set; } = string.Empty;

    // Copia de TMR, actualizada por la sincronización. Nulos si el registro se creó a mano.
    public string? Nombres { get; set; }

    public string? Apellidos { get; set; }

    public string? Identificacion { get; set; }

    public string? Correo { get; set; }

    public bool? ActivoEnTmr { get; set; }

    public DateTime? SincronizadoEn { get; set; }

    public bool EliminadoEnOrigen { get; set; }

    public Clasificacion? Clasificacion { get; set; }

    public EstadoConfiguracion EstadoConfiguracion { get; set; } = EstadoConfiguracion.Pendiente;

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}