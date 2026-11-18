using rps_backend.Shared;

namespace rps_backend.Features.Prestamos.Domain;

// Un pago del plan de un Prestamo: se cobra siempre en un corte (la quincena, el 15, o el fin de
// mes, el último día) de un mes posterior al del préstamo. Se descuenta en la liquidación de ese mes;
// si el pago de ese corte no alcanza, lo que falta pasa al siguiente pago (ver PrestamosEnCortes).
public class CuotaPrestamo
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid PrestamoId { get; set; }

    // Primer día del mes en que se cobra.
    public DateOnly Periodo { get; set; }

    public TipoCorte Corte { get; set; }

    public decimal Importe { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    // Día en que se paga ese corte: el 15 (quincena) o el último día del mes (fin de mes).
    public static DateOnly FechaDeCobro(DateOnly periodo, TipoCorte corte) =>
        corte == TipoCorte.Quincena
            ? new DateOnly(periodo.Year, periodo.Month, 15)
            : new DateOnly(periodo.Year, periodo.Month, 1).AddMonths(1).AddDays(-1);
}
