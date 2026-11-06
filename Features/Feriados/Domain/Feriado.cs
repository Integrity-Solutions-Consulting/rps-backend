namespace rps_backend.Features.Feriados.Domain;

public class Feriado
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateOnly Fecha { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
