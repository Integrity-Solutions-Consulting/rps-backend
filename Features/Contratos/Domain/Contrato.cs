namespace rps_backend.Features.Contratos.Domain;

// Un contrato es un documento administrativo de Contabilidad para un colaborador: puede cubrir
// uno, varios o ningún proyecto (ver ContratoProyecto) — por ejemplo, personal de RR. HH. sin
// proyecto en TMR todavía tiene contrato. Las fechas son las del documento legal; los días
// reconocidos para el cálculo siguen saliendo de la Asignación de TMR de cada proyecto, no de aquí.
public class Contrato
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public DateOnly FechaInicio { get; set; }

    // Fecha original acordada: una adenda la extiende pero no la reemplaza (ver Adenda.NuevaFechaFin).
    public DateOnly FechaFin { get; set; }

    public string? Referencia { get; set; }

    public string? Observaciones { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
