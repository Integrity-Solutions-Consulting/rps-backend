namespace rps_backend.Features.Contratos.Domain;

// Un proyecto cubierto por un Contrato (relación de varios a varios: RN 08 permite un contrato
// por proyecto o un contrato para varios proyectos del mismo colaborador). Sin fechas propias:
// los días reconocidos de cada proyecto siguen saliendo de su Asignación en TMR, no de aquí.
public class ContratoProyecto
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ContratoId { get; set; }

    public Guid ProyectoId { get; set; }
}
