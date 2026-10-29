namespace rps_backend.Features.Contratos.Domain;

public class Contrato
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public string TipoServicio { get; set; } = string.Empty;

    public decimal Tarifa { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
