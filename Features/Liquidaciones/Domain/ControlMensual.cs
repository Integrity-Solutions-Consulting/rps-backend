using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Domain;

// Lo que Contabilidad registra a mano de cada colaborador en un mes de "Rol a pagar":
//  - si entregó su factura (y cuándo), que decide si cobra la quincena (ver PlazoFactura);
//  - días descontados del pago por contrato y observaciones.
// Una fila por (colaborador, período). Va aparte de Liquidacion porque se puede registrar antes de
// calcular el período y no debe perderse si la liquidación se vuelve a calcular o se elimina.
// Los días descontados, por ahora, solo se registran: no cambian el valor a pagar (ver el bloque
// comentado en HonorariosService) ni salen en las exportaciones.
public class ControlMensual
{
    public const int LargoMaximoObservaciones = 1000;
    public const int MaximoDiasDescontados = 31;

    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    // Día 1 del mes.
    public DateOnly Periodo { get; set; }

    // Instante (UTC) en que se marcó la factura como recibida. Null = no entregada.
    public DateTime? FacturaRecibidaEn { get; set; }

    public Guid? FacturaMarcadaPorUsuarioId { get; set; }

    public int DiasDescontados { get; set; }

    public string? Observaciones { get; set; }

    // Último cambio de días u observaciones.
    public DateTime? DiasActualizadoEn { get; set; }

    public Guid? DiasActualizadoPorUsuarioId { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public enum EstadoFactura
{
    // El mes es anterior al control de facturas: se paga como siempre.
    NoAplica,
    // Todavía no llega y aún no vence el plazo (hasta el día 7, 23:59).
    Pendiente,
    // Ya venció el plazo y corren los días de gracia (del 8 al 14, 23:59): aún se puede marcar.
    PendienteEnGracia,
    // Pasaron los días de gracia sin factura.
    NoEntregada,
    // Llegó hasta el día 7, 23:59: cobra quincena.
    ATiempo,
    // Llegó en los días de gracia: queda registrada, pero el mes se paga a fin de mes.
    EnGracia,
}

// Plazos de la factura mensual, en hora de Ecuador:
//  - hasta el día 7, 23:59 → a tiempo: cobra la quincena (si su frecuencia es Dos pagos);
//  - del 8 al 14, 23:59 → días de gracia: se puede marcar, pero ese mes se paga todo a fin de mes;
//  - desde el 15, 00:00 → ya no se puede marcar ni quitar.
// Mientras el plazo está abierto la quincena se mantiene (en espera); al vencer el día 7, quien no
// la entregó a tiempo pasa a fin de mes la próxima vez que se calcula el período.
public static class PlazoFactura
{
    public const int DiaLimite = 7;
    public const int DiaFinGracia = 14;

    // Primer "Rol a pagar" en el que se exige la factura. Los meses anteriores se pagan como antes,
    // así volver a calcular un mes viejo no le quita la quincena a nadie.
    public static readonly DateOnly InicioControl = new(2026, 12, 1);

    public static bool Aplica(DateOnly periodo) => periodo >= InicioControl;

    // Primer instante (hora de Ecuador) en que la factura ya no es "a tiempo": día 8, 00:00.
    public static DateTime CierreATiempo(DateOnly periodo) => new(periodo.Year, periodo.Month, DiaLimite + 1, 0, 0, 0);

    // Primer instante (hora de Ecuador) en que ya no se puede marcar: día 15, 00:00.
    public static DateTime CierreGracia(DateOnly periodo) => new(periodo.Year, periodo.Month, DiaFinGracia + 1, 0, 0, 0);

    public static bool EsATiempo(DateOnly periodo, DateTime? recibidaEnUtc) =>
        recibidaEnUtc is { } r && HoraEcuador.DesdeUtc(r) < CierreATiempo(periodo);

    public static EstadoFactura Estado(DateOnly periodo, DateTime? recibidaEnUtc, DateTime ahoraEcuador)
    {
        if (!Aplica(periodo))
            return EstadoFactura.NoAplica;
        if (recibidaEnUtc is not null)
            return EsATiempo(periodo, recibidaEnUtc) ? EstadoFactura.ATiempo : EstadoFactura.EnGracia;
        if (ahoraEcuador < CierreATiempo(periodo))
            return EstadoFactura.Pendiente;
        return ahoraEcuador < CierreGracia(periodo) ? EstadoFactura.PendienteEnGracia : EstadoFactura.NoEntregada;
    }

    // ¿Se le quita la quincena de este mes? Solo cuando el control aplica, ya venció el día 7 y la
    // factura no llegó a tiempo (la de los días de gracia tampoco habilita la quincena).
    public static bool SinQuincena(DateOnly periodo, DateTime? recibidaEnUtc, DateTime ahoraEcuador) =>
        Aplica(periodo) && ahoraEcuador >= CierreATiempo(periodo) && !EsATiempo(periodo, recibidaEnUtc);
}
