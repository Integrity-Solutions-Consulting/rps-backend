using rps_backend.Shared;

namespace rps_backend.Features.Exportaciones.Application;

public static class ExportacionPagoErrores
{
    public static readonly Error PeriodoInvalido =
        Error.Validacion("ExportacionPago.PeriodoInvalido", "periodo", "Es obligatorio y debe ser el primer día del mes (ej. 2026-11-01).");

    public static readonly Error CorteRequerido =
        Error.Validacion("ExportacionPago.CorteRequerido", "corte", "Es obligatorio (Quincena o FinDeMes).");

    public static readonly Error SinColaboradores =
        Error.Validacion("ExportacionPago.SinColaboradores", "colaboradorIds", "Selecciona al menos un colaborador.");

    public static Error ColaboradorSinCorte(Guid colaboradorId) =>
        Error.Validacion("ExportacionPago.ColaboradorSinCorte", "colaboradorIds",
            $"El colaborador '{colaboradorId}' no tiene una liquidación con ese corte en ese período.");

    public static Error NoEncontrada(Guid id) =>
        Error.NoEncontrado("ExportacionPago.NoEncontrada", $"No existe una exportación con id '{id}'.");
}
