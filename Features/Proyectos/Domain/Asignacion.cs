namespace rps_backend.Features.Proyectos.Domain;

// Relación colaborador-proyecto con sus fechas; determina los "días reconocidos" para Honorarios.
// Hoy se administra a mano en RPS; a futuro la alimentará TMR sin cambiar el resto de la lógica.
public class Asignacion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public Guid ProyectoId { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaSalida { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
