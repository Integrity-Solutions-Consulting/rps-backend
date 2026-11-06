namespace rps_backend.Features.Contratos.Domain;

public class Contrato
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public Guid ProyectoId { get; set; }

    public DateOnly FechaInicio { get; set; }

    // Fecha original acordada: una adenda la extiende pero no la reemplaza (ver Adenda.NuevaFechaFin).
    public DateOnly FechaFin { get; set; }

    public string? Referencia { get; set; }

    public string? Observaciones { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
