using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Application;

// Catálogo de errores de la feature Liquidaciones.
public static class LiquidacionErrores
{
    // ===== No encontrado =====

    public static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Liquidacion.NoEncontrado", $"No existe una liquidación con id '{id}'.");

    // ===== Conflictos =====

    public static Error TransicionInvalida(EstadoLiquidacion estadoActual, string accion) =>
        Error.Conflicto(
            "Liquidacion.TransicionInvalida",
            $"No se puede {accion} una liquidación en estado '{estadoActual}'.");

    // ===== Validaciones =====

    public static readonly Error PeriodoInvalido =
        Error.Validacion("Liquidacion.PeriodoInvalido", "periodo", "Es obligatorio y debe ser el primer día del mes (ej. 2026-11-01).");
}
