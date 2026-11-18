namespace rps_backend.Features.Prestamos.Domain;

// Préstamo al colaborador: se le entrega el monto completo (puede ser mayor que su honorario) y lo
// devuelve con un plan de pagos armado a mano (CuotaPrestamo). A diferencia del anticipo adicional,
// se pueden registrar varios en el mismo mes y se cobran desde el mes siguiente.
public class Prestamo
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public decimal Monto { get; set; }

    // Día en que se le entregó el dinero. Las cuotas van desde el mes siguiente a esta fecha.
    public DateOnly Fecha { get; set; }

    public string? Observaciones { get; set; }

    // Quién lo registró (RC 03).
    public Guid CreadoPor { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime? ActualizadoEn { get; set; }
}
