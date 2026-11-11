namespace rps_backend.Features.Contratos.Domain;

// Cambio de un Contrato: rige desde FechaRegistro (el día en que se registra) hasta la nueva fecha
// de fin. Puede cambiar la fecha de fin, los proyectos y el honorario (forma de pago, monto,
// modalidad); los cambios de honorario se guardan como CondicionEconomica (RN 19: desde el mes
// siguiente). El Contrato.FechaFin original nunca se borra (RN 12).
public class Adenda
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ContratoId { get; set; }

    // Fin del contrato desde esta adenda. Null solo en contratos indefinidos (no tienen fin).
    public DateOnly? NuevaFechaFin { get; set; }

    // Desde cuándo rige: el día en que se registró.
    public DateOnly FechaRegistro { get; set; }

    // Resumen legible de lo que cambió (proyectos, forma de pago, monto, fecha de fin).
    public string? Cambios { get; set; }

    public string? Observaciones { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
