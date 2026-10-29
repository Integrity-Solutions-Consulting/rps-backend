using rps_backend.Shared;

namespace rps_backend.Features.Contratos.Application;

// Catálogo de errores de la feature Contratos.
public static class ContratoErrores
{
    // ===== No encontrado =====

    public static Error ColaboradorNoEncontrado(Guid id) =>
        Error.NoEncontrado("Contrato.ColaboradorNoEncontrado", $"No existe un colaborador con id '{id}'.");

    public static Error NoEncontrado(Guid contratoId) =>
        Error.NoEncontrado("Contrato.NoEncontrado", $"El colaborador no tiene un contrato con id '{contratoId}'.");

    // ===== Conflictos =====

    public static readonly Error ColaboradorDesactivado =
        Error.Conflicto("Contrato.ColaboradorDesactivado", "El colaborador está desactivado. Actívalo antes de modificar sus contratos.");

    public static readonly Error NoEliminable =
        Error.Conflicto("Contrato.NoEliminable", "Solo se pueden eliminar contratos que aún no han empezado. Para terminar uno vigente, edita su fecha de fin.");

    // ===== Validaciones =====

    public static readonly Error TipoServicioInvalido =
        Error.Validacion("Contrato.TipoServicioInvalido", "tipoServicio", "Es obligatorio y debe tener máximo 100 caracteres.");

    public static readonly Error TarifaInvalida =
        Error.Validacion("Contrato.TarifaInvalida", "tarifa", "Es obligatoria y debe ser mayor a 0.");

    public static readonly Error FechaInicioInvalida =
        Error.Validacion("Contrato.FechaInicioInvalida", "fechaInicio", "Es obligatoria.");

    public static readonly Error FechaFinInvalida =
        Error.Validacion("Contrato.FechaFinInvalida", "fechaFin", "Debe ser igual o posterior a la fecha de inicio.");
}
