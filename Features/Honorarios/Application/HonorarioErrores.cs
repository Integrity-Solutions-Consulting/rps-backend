using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// Catálogo de errores de la feature Honorarios.
public static class HonorarioErrores
{
    public static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Honorario.NoEncontrado", $"No existe un honorario con id '{id}'.");

    public static readonly Error YaConsolidado =
        Error.Conflicto("Honorario.YaConsolidado", "Este honorario ya está consolidado en una liquidación. Corrígelo recalculando el período, no eliminándolo suelto.");

    public static readonly Error PeriodoInvalido =
        Error.Validacion("Honorario.PeriodoInvalido", "periodo", "Es obligatorio y debe ser el primer día del mes (ej. 2026-11-01).");
}
