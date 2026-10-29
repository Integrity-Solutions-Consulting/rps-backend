using rps_backend.Shared;

namespace rps_backend.Features.Anticipos.Application;

// Catálogo de errores de la feature Anticipos.
public static class AnticipoErrores
{
    // ===== No encontrado =====

    public static Error ColaboradorNoEncontrado(Guid id) =>
        Error.NoEncontrado("Anticipo.ColaboradorNoEncontrado", $"No existe un colaborador con id '{id}'.");

    public static Error NoEncontrado(Guid anticipoId) =>
        Error.NoEncontrado("Anticipo.NoEncontrado", $"El colaborador no tiene un anticipo con id '{anticipoId}'.");

    // ===== Conflictos =====

    public static readonly Error ColaboradorDesactivado =
        Error.Conflicto("Anticipo.ColaboradorDesactivado", "El colaborador está desactivado. Actívalo antes de registrar anticipos.");

    public static Error TransicionInvalida(EstadoAnticipo estadoActual, string accion) =>
        Error.Conflicto(
            "Anticipo.TransicionInvalida",
            $"No se puede {accion} un anticipo en estado '{estadoActual}'.");

    // ===== Validaciones =====

    public static readonly Error MontoInvalido =
        Error.Validacion("Anticipo.MontoInvalido", "monto", "Es obligatorio y debe ser mayor a 0.");
}
