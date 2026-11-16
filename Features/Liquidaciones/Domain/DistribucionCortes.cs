using rps_backend.Features.Honorarios.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Domain;

// Cómo se reparte el pago del mes entre los cortes. Lo usan Liquidaciones (para generar los
// cortes) y Anticipos (para el tope de cada cuota), así los dos aplican exactamente la misma regla.
//
// Con DosPagos:
//   - Quincena  = 50% de la base de quincena mensual completa (sin prorratear):
//       · modalidad Neto   -> 50% del NETO acordado (ej. pasante de $200 neto: $100 y $100).
//       · modalidad MasIva -> 50% del SUBTOTAL (sin IVA ni retenciones).
//   - Fin de mes = neto del mes - la quincena: ahí se reflejan el IVA y las retenciones.
// Con FinDeMes: un solo pago por el neto completo.
public static class DistribucionCortes
{
    // Lo que cuenta para la quincena de una condición, según su modalidad.
    public static decimal BaseQuincena(Modalidad modalidad, CalculoTributario.Resultado mensualCompleto) =>
        modalidad == Modalidad.Neto ? mensualCompleto.Neto : mensualCompleto.Subtotal;

    // El centavo impar se queda en la quincena (RN 39).
    public static decimal Quincena(decimal baseQuincenaMensualCompleta) =>
        Math.Round(baseQuincenaMensualCompleta / 2, 2, MidpointRounding.AwayFromZero);

    // Valor base de cada corte para un mes completo. Sirve de tope para lo que se le puede
    // descontar en ese corte: no se puede descontar más de lo que se le paga.
    public static IReadOnlyDictionary<TipoCorte, decimal> MesCompleto(
        Frecuencia frecuencia, decimal baseQuincenaMensualCompleta, decimal netoMensualCompleto)
    {
        if (frecuencia != Frecuencia.DosPagos)
            return new Dictionary<TipoCorte, decimal> { [TipoCorte.FinDeMes] = netoMensualCompleto };

        var quincena = Quincena(baseQuincenaMensualCompleta);
        return new Dictionary<TipoCorte, decimal>
        {
            [TipoCorte.Quincena] = quincena,
            [TipoCorte.FinDeMes] = Math.Max(netoMensualCompleto - quincena, 0m)
        };
    }
}
