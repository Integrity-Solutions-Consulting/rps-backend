using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Domain;

// Cómo se reparte el pago del mes entre los cortes. Lo usan Liquidaciones (para generar los
// cortes) y Anticipos (para el tope de cada cuota), así los dos aplican exactamente la misma regla.
//
// Con DosPagos:
//   - Quincena  = 50% del SUBTOTAL mensual completo (sin IVA ni retenciones, sin prorratear).
//   - Fin de mes = neto del mes - la quincena: ahí se reflejan el IVA y las retenciones.
// Con FinDeMes: un solo pago por el neto completo.
public static class DistribucionCortes
{
    // El centavo impar se queda en la quincena (RN 39).
    public static decimal Quincena(decimal subtotalMensualCompleto) =>
        Math.Round(subtotalMensualCompleto / 2, 2, MidpointRounding.AwayFromZero);

    // Valor base de cada corte para un mes completo. Sirve de tope para lo que se le puede
    // descontar en ese corte: no se puede descontar más de lo que se le paga.
    public static IReadOnlyDictionary<TipoCorte, decimal> MesCompleto(
        Frecuencia frecuencia, decimal subtotalMensualCompleto, decimal netoMensualCompleto)
    {
        if (frecuencia != Frecuencia.DosPagos)
            return new Dictionary<TipoCorte, decimal> { [TipoCorte.FinDeMes] = netoMensualCompleto };

        var quincena = Quincena(subtotalMensualCompleto);
        return new Dictionary<TipoCorte, decimal>
        {
            [TipoCorte.Quincena] = quincena,
            [TipoCorte.FinDeMes] = Math.Max(netoMensualCompleto - quincena, 0m)
        };
    }
}
