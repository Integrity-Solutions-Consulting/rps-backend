using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Domain;

// Honorario mensual fijo acordado (RN 17-20). Cambia desde un mes hacia adelante; se conserva el
// historial via MesInicioVigencia. Dos modos de pago por contrato (ver CondicionesVigentes):
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

    public DateOnly MesInicioVigencia { get; set; }

    public string? Observaciones { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public bool PorContrato => ProyectoId is null;
}
