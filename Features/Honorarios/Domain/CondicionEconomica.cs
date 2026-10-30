using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Domain;

// Honorario mensual fijo acordado para un colaborador en un proyecto (RN 17-20). Cambia desde
// un mes hacia adelante; se conserva el historial via MesInicioVigencia.
public class CondicionEconomica
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public Guid ProyectoId { get; set; }

    public Guid ContratoId { get; set; }

    public decimal ImporteMensual { get; set; }

    public Modalidad Modalidad { get; set; }

    public DateOnly MesInicioVigencia { get; set; }

    public string? Observaciones { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    // Si un cambio programado (mes que aún no empieza) se cancela, no se borra: se marca aquí.
    // Las consultas normales lo ignoran (filtro global en CondicionEconomicaConfiguration) y se
    // puede restaurar. Cada cancelación y restauración queda en CondicionEconomicaMovimiento (RN 48).
    public DateTime? CanceladoEn { get; set; }
}
