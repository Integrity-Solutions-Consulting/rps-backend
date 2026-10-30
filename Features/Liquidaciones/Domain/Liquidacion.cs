namespace rps_backend.Features.Liquidaciones.Domain;

// Consolida los Honorario de todos los proyectos reconocidos de un colaborador en un período
// (RN 30-31), antes de anticipos adicionales. No existe estado "Pagada": el pago bancario está
// fuera de alcance (RN 56); "Revisado" es solo el control interno previo a exportar (RN 49).
public class Liquidacion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public DateOnly Periodo { get; set; }

    public decimal NetoConsolidado { get; set; }

    public bool Revisado { get; set; }

    public DateTime? RevisadoEn { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
