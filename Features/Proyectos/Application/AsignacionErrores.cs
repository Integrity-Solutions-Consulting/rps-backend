using rps_backend.Shared;

namespace rps_backend.Features.Proyectos.Application;

public static class AsignacionErrores
{
    public static Error ColaboradorNoEncontrado(Guid id) =>
        Error.NoEncontrado("Asignacion.ColaboradorNoEncontrado", $"No existe un colaborador con id '{id}'.");

    public static Error ProyectoNoEncontrado(Guid id) =>
        Error.NoEncontrado("Asignacion.ProyectoNoEncontrado", $"No existe un proyecto con id '{id}'.");

    public static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Asignacion.NoEncontrado", $"El colaborador no tiene una asignación con id '{id}'.");

    public static readonly Error ProyectoInactivo =
        Error.Conflicto("Asignacion.ProyectoInactivo", "El proyecto está inactivo. Actívalo antes de asignar colaboradores.");

    public static Error Superpuesta(Guid proyectoId) =>
        Error.Conflicto("Asignacion.Superpuesta", $"Ya existe una asignación vigente de este colaborador al proyecto '{proyectoId}' en ese rango de fechas.");

    public static readonly Error NoEliminable =
        Error.Conflicto("Asignacion.NoEliminable", "Solo se pueden eliminar asignaciones que aún no han empezado.");

    public static readonly Error FechaInicioInvalida =
        Error.Validacion("Asignacion.FechaInicioInvalida", "fechaInicio", "Es obligatoria.");

    public static readonly Error FechaSalidaInvalida =
        Error.Validacion("Asignacion.FechaSalidaInvalida", "fechaSalida", "Debe ser igual o posterior a la fecha de inicio.");

    public static readonly Error ProyectoRequerido =
        Error.Validacion("Asignacion.ProyectoRequerido", "proyectoId", "Es obligatorio.");
}
