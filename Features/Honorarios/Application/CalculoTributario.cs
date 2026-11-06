using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// Fórmulas de la sección 7 del documento de requisitos (RN 28). Se reutiliza tanto al generar
// Honorarios (sobre el proporcional del período) como en Anticipos para el límite (sobre el
// importe mensual completo, sin prorratear) — mismas fórmulas, distinto monto de entrada.
public static class CalculoTributario
{
    public record Resultado(
        decimal Subtotal, decimal Iva, decimal TotalDocumento,
        decimal RetencionFuente, decimal RetencionIva, decimal TotalRetenciones, decimal Neto);

    public static Resultado Calcular(
        decimal montoBase, Modalidad modalidad, decimal pctIva, decimal pctRetencionFuente, decimal pctRetencionIva)
    {
        decimal subtotal;
        if (modalidad == Modalidad.Neto)
        {
            // Factor derivado de los mismos parámetros (para profesional 100%/15%/10% da 0.90,
            // para junior 70% da 0.945 — verificado contra el ejemplo del documento).
            var factor = 1 + pctIva / 100 - pctRetencionFuente / 100 - (pctIva / 100) * (pctRetencionIva / 100);
            subtotal = Math.Round(montoBase / factor, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            subtotal = montoBase;
        }

        var iva = Math.Round(subtotal * pctIva / 100, 2, MidpointRounding.AwayFromZero);
        var totalDocumento = subtotal + iva;
        var retencionFuente = Math.Round(subtotal * pctRetencionFuente / 100, 2, MidpointRounding.AwayFromZero);
        var retencionIva = Math.Round(iva * pctRetencionIva / 100, 2, MidpointRounding.AwayFromZero);
        var totalRetenciones = retencionFuente + retencionIva;
        var neto = totalDocumento - totalRetenciones;

        return new Resultado(subtotal, iva, totalDocumento, retencionFuente, retencionIva, totalRetenciones, neto);
    }
}
