using rps_backend.Shared;

namespace rps_backend.Features.Anticipos.Application;

// Catálogo de errores de la feature Anticipos.
public static class AnticipoErrores
{
    public static Error ColaboradorNoEncontrado(Guid id) =>
        Error.NoEncontrado("Anticipo.ColaboradorNoEncontrado", $"No existe un colaborador con id '{id}'.");

    public static Error NoEncontrado(Guid anticipoId) =>
        Error.NoEncontrado("Anticipo.NoEncontrado", $"El colaborador no tiene un anticipo adicional con id '{anticipoId}'.");

    public static readonly Error ColaboradorDesactivado =
        Error.Conflicto("Anticipo.ColaboradorDesactivado", "El colaborador está desactivado. Actívalo antes de registrar anticipos.");

    public static Error ExcedeLimite(decimal limite) =>
        Error.Conflicto("Anticipo.ExcedeLimite", $"El saldo pendiente más este anticipo superaría el límite permitido ({limite:F2}).");

    public static readonly Error TieneCuotas =
        Error.Conflicto("Anticipo.TieneCuotas", "No se puede eliminar un anticipo que ya tiene un plan de recuperación registrado.");

    public static readonly Error MontoInvalido =
        Error.Validacion("Anticipo.MontoInvalido", "monto", "Es obligatorio y debe ser mayor a 0.");

    public static readonly Error CuotasRequeridas =
        Error.Validacion("Anticipo.CuotasRequeridas", "cuotas", "Es obligatorio incluir al menos una cuota.");

    public static readonly Error CuotaInvalida =
        Error.Validacion("Anticipo.CuotaInvalida", "cuotas", "Cada cuota necesita período (primer día del mes), corte e importe mayor a 0.");

    public static readonly Error CuotasDeMesesDistintos =
        Error.Validacion("Anticipo.CuotasDeMesesDistintos", "cuotas", "Todas las cuotas deben ser del mismo mes: se recupera completo en un solo período.");

    public static Error SumaCuotasNoCoincide(decimal monto) =>
        Error.Validacion("Anticipo.SumaCuotasNoCoincide", "cuotas", $"La suma de las cuotas debe ser exactamente {monto:F2}.");

    public static readonly Error CorteIncompatibleConFrecuencia =
        Error.Validacion(
            "Anticipo.CorteIncompatibleConFrecuencia", "cuotas",
            "El tipo de corte de cada cuota debe existir para la frecuencia de pago del colaborador en ese período " +
            "(FinDeMes solo acepta cuotas de corte FinDeMes).");
}
