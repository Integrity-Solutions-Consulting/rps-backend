namespace rps_backend.Features.Proyectos.Domain;

// Relación colaborador-proyecto con sus fechas; determina los "días reconocidos" para Honorarios.
// La alimenta la sincronización con TMR; las de SincronizadaEn nula se crearon a mano en RPS.
public class Asignacion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public Guid ProyectoId { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaSalida { get; set; }

    public DateTime? SincronizadaEn { get; set; }

    // Ya no viene de TMR: se conserva para explicar períodos anteriores, pero no cuenta en cálculos nuevos (RN 22).
    public bool EliminadaEnOrigen { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
