using rps_backend.Shared;

namespace rps_backend.Features.Feriados.Application;

// Catálogo de errores de la feature Feriados.
public static class FeriadoErrores
{
    // ===== No encontrado =====

    public static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Feriado.NoEncontrado", $"No existe un feriado con id '{id}'.");

    // ===== Conflictos =====

    public static Error FechaDuplicada(DateOnly fecha) =>
        Error.Conflicto("Feriado.FechaDuplicada", $"Ya existe un feriado registrado el {fecha:yyyy-MM-dd}.");

    public static Error YaUsadoEnHonorarios(DateOnly fecha) =>
        Error.Conflicto(
            "Feriado.YaUsadoEnHonorarios",
            $"No se puede eliminar: ya se generaron honorarios del período de {fecha:yyyy-MM-dd}, que pudo haber usado este feriado para calcular días hábiles.");

    public static readonly Error FuenteNoDisponible =
        Error.Conflicto("Feriado.FuenteNoDisponible", "No se pudo consultar la fuente de feriados (Nager.Date). Intenta más tarde o regístralos manualmente.");

    // ===== Validaciones =====

    public static readonly Error FechaInvalida =
        Error.Validacion("Feriado.FechaInvalida", "fecha", "Es obligatoria.");

    public static readonly Error NombreInvalido =
        Error.Validacion("Feriado.NombreInvalido", "nombre", "Es obligatorio y debe tener máximo 200 caracteres.");

    public static readonly Error AnioInvalido =
        Error.Validacion("Feriado.AnioInvalido", "anio", "Debe ser un año entre 2000 y 2100.");
}
