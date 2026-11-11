using rps_backend.Shared;

namespace rps_backend.Features.Contratos.Application;

// Catálogo de errores de la feature Contratos.
public static class ContratoErrores
{
    // ===== No encontrado =====

    public static Error ColaboradorNoEncontrado(Guid id) =>
        Error.NoEncontrado("Contrato.ColaboradorNoEncontrado", $"No existe un colaborador con id '{id}'.");

    public static Error ProyectoNoEncontrado(Guid id) =>
        Error.NoEncontrado("Contrato.ProyectoNoEncontrado", $"No existe un proyecto con id '{id}'.");

    public static Error NoEncontrado(Guid contratoId) =>
        Error.NoEncontrado("Contrato.NoEncontrado", $"El colaborador no tiene un contrato con id '{contratoId}'.");

    public static Error AdendaNoEncontrada(Guid adendaId) =>
        Error.NoEncontrado("Contrato.AdendaNoEncontrada", $"El contrato no tiene una adenda con id '{adendaId}'.");

    // ===== Conflictos =====

    public static readonly Error ColaboradorDesactivado =
        Error.Conflicto("Contrato.ColaboradorDesactivado", "El colaborador está desactivado. Actívalo antes de modificar sus contratos.");

    public static readonly Error ProyectoInactivo =
        Error.Conflicto("Contrato.ProyectoInactivo", "El proyecto está inactivo. Actívalo antes de registrar contratos.");

    public static readonly Error NoActivoEnTmr =
        Error.Conflicto("Contrato.NoActivoEnTmr",
            "El colaborador no está activo en TMR (o todavía no se ha confirmado). Solo se registran contratos para personas activas en TMR.");

    public static Error Superpuesto(Guid proyectoId) =>
        Error.Conflicto("Contrato.Superpuesto", $"Ya existe un contrato de este colaborador con el proyecto '{proyectoId}' que se superpone en fechas.");

    public static readonly Error NoEliminable =
        Error.Conflicto("Contrato.NoEliminable", "Solo se pueden eliminar contratos que aún no han empezado. Para terminar uno vigente, registra una adenda.");

    public static readonly Error ContratoIndefinido =
        Error.Conflicto("Contrato.Indefinido", "Este contrato es indefinido (sin fecha de fin): no admite adendas.");

    // ===== Validaciones =====

    public static readonly Error ProyectosDuplicados =
        Error.Validacion("Contrato.ProyectosDuplicados", "proyectoIds", "No se puede repetir el mismo proyecto dos veces en un contrato.");

    public static readonly Error FechaInicioInvalida =
        Error.Validacion("Contrato.FechaInicioInvalida", "fechaInicio", "Es obligatoria.");

    public static readonly Error FechaFinInvalida =
        Error.Validacion("Contrato.FechaFinInvalida", "fechaFin",
            "Es obligatoria y debe ser igual o posterior a la fecha de inicio, salvo que el contrato sea indefinido.");

    public static readonly Error FechaInicioPosteriorAFin =
        Error.Validacion("Contrato.FechaInicioPosteriorAFin", "fechaInicio", "No puede ser posterior a la fecha de fin del contrato.");

    public static readonly Error ReferenciaInvalida =
        Error.Validacion("Contrato.ReferenciaInvalida", "referencia", "Debe tener máximo 100 caracteres.");

    public static readonly Error ObservacionesInvalidas =
        Error.Validacion("Contrato.ObservacionesInvalidas", "observaciones", "Debe tener máximo 500 caracteres.");

    // Fechas de contrato y adenda: desde el año 2000 y con año de 4 dígitos (evita 20266 o 0202).
    public static Error FechaFueraDeRango(string campo) =>
        Error.Validacion("Contrato.FechaFueraDeRango", campo, "La fecha debe ser desde el año 2000 y el año de 4 dígitos.");

    public static readonly Error NuevaFechaFinInvalida =
        Error.Validacion("Contrato.NuevaFechaFinInvalida", "nuevaFechaFin", "Es obligatoria y debe ser posterior a la fecha de fin vigente actual.");
}
