using System.Globalization;
using rps_backend.Shared;

namespace rps_backend.Features.Prestamos.Application;

public static class PrestamoErrores
{
    private static readonly CultureInfo Ecuador = new("es-EC");

    private static string Mes(DateOnly periodo) => periodo.ToString("MMMM yyyy", Ecuador);

    private static string Dia(DateOnly fecha) => fecha.ToString("dd 'de' MMMM yyyy", Ecuador);

    public static Error ColaboradorNoEncontrado(Guid id) =>
        Error.NoEncontrado("Prestamo.ColaboradorNoEncontrado", $"No existe un colaborador con id '{id}'.");

    public static Error NoEncontrado(Guid prestamoId) =>
        Error.NoEncontrado("Prestamo.NoEncontrado", $"El colaborador no tiene un préstamo con id '{prestamoId}'.");

    public static readonly Error ColaboradorDesactivado =
        Error.Conflicto("Prestamo.ColaboradorDesactivado", "El colaborador está desactivado. Actívalo antes de registrarle un préstamo.");

    public static readonly Error MontoInvalido =
        Error.Validacion("Prestamo.MontoInvalido", "monto", "Es obligatorio, mayor a 0 y con máximo 2 decimales.");

    public static readonly Error ObservacionesLargas =
        Error.Validacion("Prestamo.ObservacionesLargas", "observaciones", "Máximo 500 caracteres.");

    public static readonly Error CuotasRequeridas =
        Error.Validacion("Prestamo.CuotasRequeridas", "cuotas", "Agrega al menos una fecha de cobro al plan de pagos.");

    public static readonly Error CuotaInvalida =
        Error.Validacion("Prestamo.CuotaInvalida", "cuotas",
            "Cada fecha de cobro necesita mes, corte (quincena o fin de mes) y un valor mayor a 0 con máximo 2 decimales.");

    public static Error CuotaRepetida(DateOnly periodo, TipoCorte corte) =>
        Error.Validacion("Prestamo.CuotaRepetida", "cuotas",
            $"Hay dos cobros en {(corte == TipoCorte.Quincena ? "la quincena" : "el fin de mes")} de {Mes(periodo)}. Deja uno solo con el valor total.");

    public static Error SumaNoCoincide(decimal monto, decimal suma) =>
        Error.Validacion("Prestamo.SumaNoCoincide", "cuotas", suma < monto
            ? $"Faltan {monto - suma:F2} para completar el préstamo de {monto:F2}. El plan de pagos tiene que sumar exactamente el monto prestado."
            : $"El plan de pagos suma {suma - monto:F2} más que el préstamo de {monto:F2}. Tiene que sumar exactamente el monto prestado.");

    public static Error AntesDelMesSiguiente(DateOnly primerMes) =>
        Error.Validacion("Prestamo.AntesDelMesSiguiente", "cuotas",
            $"Los cobros van desde {Mes(primerMes)}, el mes siguiente a la entrega del préstamo.");

    public static Error FechaYaPaso(DateOnly fecha) =>
        Error.Validacion("Prestamo.FechaYaPaso", "cuotas",
            $"El pago del {Dia(fecha)} ya se hizo: elige una fecha de cobro posterior a hoy.");

    public static Error CorteIncompatibleConFrecuencia(DateOnly periodo) =>
        Error.Validacion("Prestamo.CorteIncompatibleConFrecuencia", "cuotas",
            $"En {Mes(periodo)} este colaborador cobra solo a fin de mes: no se le puede cobrar en la quincena porque ese día no se le paga.");

    public static readonly Error CuotaCobradaNoSeCambia =
        Error.Validacion("Prestamo.CuotaCobradaNoSeCambia", "cuotas",
            "Los cobros cuya fecha ya llegó no se pueden cambiar ni quitar: déjalos igual y ajusta solo los que vienen.");

    public static readonly Error TieneCobros =
        Error.Conflicto("Prestamo.TieneCobros",
            "Este préstamo ya empezó a cobrarse: no se puede eliminar. Si hace falta, corrige los cobros que todavía no llegan.");
}
