namespace rps_backend.Features.Anticipos.Domain;

// "Anticipo adicional" (RN 41-47): valor extraordinario que se recupera con un plan de cuotas
// en el mismo mes. El "anticipo ordinario" (el 50% de quincena) no es una deuda y no se modela
// aquí: es simplemente el Corte Quincena de la Liquidación.
public class AnticipoAdicional
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public decimal Monto { get; set; }

    public DateOnly Fecha { get; set; }

    public string? Observaciones { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
