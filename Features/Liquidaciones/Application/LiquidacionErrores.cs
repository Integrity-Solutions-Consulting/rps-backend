using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Application;

// Catálogo de errores de la feature Liquidaciones.
public static class LiquidacionErrores
{
    public static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Liquidacion.NoEncontrado", $"No existe una liquidación con id '{id}'.");

    public static readonly Error PeriodoInvalido =
        Error.Validacion("Liquidacion.PeriodoInvalido", "periodo", "Es obligatorio y debe ser el primer día del mes (ej. 2026-11-01).");
}
