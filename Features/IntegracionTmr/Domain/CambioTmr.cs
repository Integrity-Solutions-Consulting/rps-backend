namespace rps_backend.Features.IntegracionTmr.Domain;

public enum TipoCambioTmr
{
    AsignacionNueva,
    AsignacionEliminada,
    AsignacionRestaurada,
    FechaSalida,
    EstadoColaborador,
    ColaboradorEliminado,
    ColaboradorRestaurado,
    // Inactivo en TMR pero sigue activo en RPS porque tiene un anticipo adicional con saldo por
    // recuperar. Se desactiva solo en la sincronización siguiente a que el saldo llegue a cero.
    DesactivacionRetenida
}

// Aviso de un cambio relevante detectado en TMR (RN 50). No modifica liquidaciones: queda visible hasta
// que Contabilidad lo revisa. No indica quién hizo el cambio porque TMR no entrega ese dato.
public class CambioTmr
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    // Nulo cuando el cambio es del colaborador y no de una asignación.
    public Guid? ProyectoId { get; set; }

    public TipoCambioTmr Tipo { get; set; }

    public string? ValorAnterior { get; set; }

    public string? ValorNuevo { get; set; }

    // Primer mes potencialmente afectado. Nulo si el cambio no trae una fecha de la que deducirlo.
    public DateOnly? PeriodoDesde { get; set; }

    public DateTime DetectadoEn { get; set; } = DateTime.UtcNow;

    public bool Revisado { get; set; }

    public DateTime? RevisadoEn { get; set; }

    public Guid? RevisadoPorUsuarioId { get; set; }
}
