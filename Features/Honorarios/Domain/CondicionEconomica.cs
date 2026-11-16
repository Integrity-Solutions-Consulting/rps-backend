using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Domain;

// Honorario mensual fijo acordado (RN 17-20). Rige desde FechaInicioVigencia (cualquier día) hasta
// que empieza la siguiente; se conserva el historial. Si cambia a mitad de mes, ese mes se paga en
// tramos: cada uno con sus días y su importe (ver CondicionesVigentes.TramosDelPeriodo).
// Dos modos de pago por contrato (ver CondicionesVigentes):
//  - por proyecto: ProyectoId con valor, un importe por cada proyecto del contrato;
//  - por contrato: ProyectoId null, un solo importe fijo para todo el contrato, tenga los
//    proyectos que tenga (o ninguno).
public class CondicionEconomica
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public Guid? ProyectoId { get; set; }

    public Guid ContratoId { get; set; }

    public decimal ImporteMensual { get; set; }

    public Modalidad Modalidad { get; set; }

    public DateOnly FechaInicioVigencia { get; set; }

    public string? Observaciones { get; set; }

    // Adenda que registró este cambio de honorario (null: configuración inicial del contrato).
    // Al corregir la adenda se corrigen también sus honorarios (monto, modalidad, fecha).
    public Guid? AdendaId { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public bool PorContrato => ProyectoId is null;
}
