namespace rps_backend.Features.Contratos.Domain;

// Cambio de un Contrato: rige desde FechaVigencia (hoy o una fecha futura, aunque se registre hoy)
// hasta la nueva fecha de fin. Puede cambiar la fecha de fin, los proyectos y el honorario (forma de
// pago, monto, modalidad); los cambios de honorario se guardan como CondicionEconomica vigente desde
// esa misma fecha. El Contrato.FechaFin original nunca se borra (RN 12).
public class Adenda
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ContratoId { get; set; }

    // Fin del contrato desde esta adenda. Null solo en contratos indefinidos (no tienen fin).
    public DateOnly? NuevaFechaFin { get; set; }

    // Día en que se registró.
    public DateOnly FechaRegistro { get; set; }

    // Desde cuándo rigen sus cambios: el día de registro o uno posterior.
    public DateOnly FechaVigencia { get; set; }

    // Proyectos del contrato desde FechaVigencia (lista completa). Null: la adenda no cambia proyectos.
    // Si FechaVigencia es futura, se aplican ese día (ver AdendasProgramadas).
    public List<Guid>? ProyectosNuevos { get; set; }

    // Los que tenía el contrato justo antes de aplicar ProyectosNuevos: permiten deshacerlo si al
    // corregir la adenda se mueve su fecha al futuro.
    public List<Guid>? ProyectosAnteriores { get; set; }

    // ProyectosNuevos ya se aplicó al contrato.
    public bool ProyectosAplicados { get; set; }

    // Resumen legible de lo que cambió (proyectos, forma de pago, monto, fecha de fin).
    public string? Cambios { get; set; }

    public string? Observaciones { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
