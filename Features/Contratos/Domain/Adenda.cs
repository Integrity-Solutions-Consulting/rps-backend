namespace rps_backend.Features.Contratos.Domain;

// Extensión de un Contrato: no crea otro proyecto ni otro honorario, solo empuja la fecha de fin
// vigente hacia adelante. El Contrato.FechaFin original nunca se borra (RN 12).
public class Adenda
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ContratoId { get; set; }

    public DateOnly NuevaFechaFin { get; set; }

    public DateOnly FechaRegistro { get; set; }

    public string? Observaciones { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
