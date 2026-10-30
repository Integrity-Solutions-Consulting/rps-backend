using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// Catálogo de errores de la feature Honorarios.
public static class HonorarioErrores
{
    // ===== No encontrado =====

    public static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Honorario.NoEncontrado", $"No existe un honorario con id '{id}'.");

    // ===== Conflictos =====

    public static Error TransicionInvalida(EstadoHonorario estadoActual, string accion) =>
        Error.Conflicto(
            "Honorario.TransicionInvalida",
            $"No se puede {accion} un honorario en estado '{estadoActual}'.");

    public static Error PerteneceALiquidacion(string accion) =>
        Error.Conflicto(
            "Honorario.PerteneceALiquidacion",
            $"Este honorario ya está agrupado en una liquidación: no se puede {accion} directamente, hazlo a través de esa liquidación.");

    // ===== Validaciones =====

    public static readonly Error PeriodoInvalido =
        Error.Validacion("Honorario.PeriodoInvalido", "periodo", "Es obligatorio y debe ser el primer día del mes (ej. 2026-11-01).");
}
