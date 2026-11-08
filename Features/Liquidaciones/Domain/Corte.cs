using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Domain;

// Distribución del neto consolidado de una Liquidación según la Frecuencia del colaborador
// (RN 36-40): uno (FinDeMes) o dos (DosPagos: Quincena + FinDeMes).
public class Corte
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid LiquidacionId { get; set; }

    public TipoCorte Tipo { get; set; }

    // Antes de recuperar anticipos adicionales (RN 38).
    public decimal ValorBase { get; set; }

    public decimal RecuperacionAdicional { get; set; }

    // ValorBase - RecuperacionAdicional, nunca negativo (RN 47).
    public decimal ValorPreparado { get; set; }

    // Max(RecuperacionAdicional - ValorBase, 0): cuánto de la recuperación programada no cupo en
    // este corte. "Saldo insuficiente por salida": se muestra, nunca se oculta ni se da por
    // recuperado (no afecta el saldo pendiente del Anticipo, que sigue siendo responsabilidad de
    // Contabilidad resolver).
    public decimal RecuperacionNoAplicada { get; set; }

    // Solo en el corte Quincena: cuánto se le restó de su base ordinaria (mitad del neto mensual
    // completo) porque, con los días reales del período, el corte FinDeMes habría quedado
    // negativo (RN 27 — salida en la segunda mitad del mes). 0 si no aplicó.
    public decimal AjustePorProrrateo { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
