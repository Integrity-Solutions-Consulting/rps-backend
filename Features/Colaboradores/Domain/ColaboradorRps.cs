using rps_backend.Shared;

namespace rps_backend.Features.Colaboradores.Domain;

public class ColaboradorRps
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string TmrColaboradorId { get; set; } = string.Empty;

    // Copia de TMR, actualizada por la sincronización. Nulos solo antes de la primera sincronización.
    public string? Nombres { get; set; }

    public string? Apellidos { get; set; }

    public string? Identificacion { get; set; }

    public string? Correo { get; set; }

    public bool? ActivoEnTmr { get; set; }

    public DateTime? SincronizadoEn { get; set; }

    public bool EliminadoEnOrigen { get; set; }

    // Código del tipo de contrato en TMR (catálogo TCT). Ver TiposContratoTmr.
    public string? TipoContratoTmr { get; set; }

    // CON/PAS: la fija la sincronización desde TMR. MT/COS: la elige Contabilidad en RPS.
    public Clasificacion? Clasificacion { get; set; }

    public EstadoConfiguracion EstadoConfiguracion { get; set; } = EstadoConfiguracion.Pendiente;

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}