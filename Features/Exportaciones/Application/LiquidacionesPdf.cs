using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace rps_backend.Features.Exportaciones.Application;

// Datos que necesita el reporte. Los arma ExportacionesService; aquí solo se dibuja.
public record FilaLiquidacionPdf(
    string Colaborador, string? Identificacion, string? Clasificacion, DateOnly Periodo,
    decimal NetoConsolidado, decimal Quincena, decimal FinDeMes, decimal RecuperacionAnticipos, decimal APagar, bool Revisado);

public record ReporteLiquidacionesPdf(
    string RazonSocial, string Ruc, string Moneda, DateOnly? Periodo, DateTime GeneradoEn, IReadOnlyList<FilaLiquidacionPdf> Filas);

// PDF formal de liquidaciones: logo, encabezado con datos de la empresa, tabla y totales.
// Logo: si existe Assets/logo.png junto al ejecutable se usa ese archivo; si no, se dibuja el distintivo "RPS".
public static class LiquidacionesPdf
{
    private const string Marino = "#13263D";
    private const string Verde = "#0F766E";
    private const string Gris = "#64748B";
    private const string Borde = "#E2E8F0";
    private const string FondoCabecera = "#F1F5F9";

    // Formato de Ecuador sin depender de ICU (en contenedores con globalización invariante no hay "es-EC").
    private static readonly NumberFormatInfo Numeros = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = ".", NumberDecimalDigits = 2 };

    private static readonly string[] Meses =
        ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    public static byte[] Generar(ReporteLiquidacionesPdf r)
    {
        var porMes = r.Periodo is null;
        var revisadas = r.Filas.Count(f => f.Revisado);

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(28);
            page.DefaultTextStyle(t => t.FontSize(9).FontColor(Colors.Black));

            page.Header().Element(c => Encabezado(c, r, revisadas));
            page.Content().PaddingVertical(12).Element(c => Tabla(c, r, porMes));
            page.Footer().Row(row =>
            {
                row.RelativeItem().Text($"Generado el {r.GeneradoEn.ToLocalTime():dd/MM/yyyy HH:mm} · Documento de control interno; no es comprobante de pago.")
                    .FontSize(7.5f).FontColor(Gris);
                row.AutoItem().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(7.5f).FontColor(Gris));
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
            });
        })).GeneratePdf();
    }

    private static void Encabezado(IContainer c, ReporteLiquidacionesPdf r, int revisadas)
    {
        c.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(52).Height(52).Element(Logo);
                row.ConstantItem(12);
                row.RelativeItem().AlignMiddle().Column(empresa =>
                {
                    empresa.Item().Text(string.IsNullOrWhiteSpace(r.RazonSocial) ? "Empresa sin configurar" : r.RazonSocial)
                        .FontSize(14).Bold().FontColor(Marino);
                    empresa.Item().Text($"RUC {(string.IsNullOrWhiteSpace(r.Ruc) ? "—" : r.Ruc)}").FontColor(Gris);
                });
                row.AutoItem().AlignMiddle().AlignRight().Column(titulo =>
                {
                    titulo.Item().AlignRight().Text("Liquidaciones de honorarios").FontSize(15).Bold().FontColor(Marino);
                    titulo.Item().AlignRight().Text(r.Periodo is { } p ? NombreMes(p) : "Todos los períodos").FontSize(11).FontColor(Verde);
                    titulo.Item().AlignRight().Text($"{revisadas} de {r.Filas.Count} revisadas · Moneda {r.Moneda}").FontColor(Gris);
                });
            });
            col.Item().PaddingTop(10).LineHorizontal(1.5f).LineColor(Marino);
        });
    }

    private static void Logo(IContainer c)
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");
        if (File.Exists(ruta))
        {
            c.Image(ruta).FitArea();
            return;
        }
        c.Background(Marino).AlignCenter().AlignMiddle().Text("RPS").FontSize(16).Bold().FontColor(Colors.White);
    }

    private static void Tabla(IContainer c, ReporteLiquidacionesPdf r, bool porMes)
    {
        if (r.Filas.Count == 0)
        {
            c.PaddingTop(30).AlignCenter().Text("No hay liquidaciones para este período.").FontSize(11).FontColor(Gris);
            return;
        }

        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(24);       // N.º
                cols.RelativeColumn(3);        // Colaborador
                cols.RelativeColumn(1.6f);     // Identificación
                cols.RelativeColumn(1.3f);     // Clasificación
                if (porMes) cols.RelativeColumn(1.4f); // Período
                cols.RelativeColumn(1.5f);     // Neto consolidado
                cols.RelativeColumn(1.3f);     // Quincena
                cols.RelativeColumn(1.3f);     // Fin de mes
                cols.RelativeColumn(1.5f);     // Recuperación
                cols.RelativeColumn(1.5f);     // A pagar
                cols.RelativeColumn(1.1f);     // Revisado
            });

            t.Header(h =>
            {
                Cabecera(h.Cell(), "N.º");
                Cabecera(h.Cell(), "Colaborador");
                Cabecera(h.Cell(), "Identificación");
                Cabecera(h.Cell(), "Clasificación");
                if (porMes) Cabecera(h.Cell(), "Período");
                Cabecera(h.Cell(), "Neto consolidado", derecha: true);
                Cabecera(h.Cell(), "Quincena", derecha: true);
                Cabecera(h.Cell(), "Fin de mes", derecha: true);
                Cabecera(h.Cell(), "Recup. anticipos", derecha: true);
                Cabecera(h.Cell(), "A pagar", derecha: true);
                Cabecera(h.Cell(), "Revisado");
            });

            var n = 0;
            foreach (var f in r.Filas)
            {
                n++;
                Celda(t.Cell(), n.ToString(CultureInfo.InvariantCulture));
                Celda(t.Cell(), f.Colaborador);
                Celda(t.Cell(), f.Identificacion ?? "—");
                Celda(t.Cell(), f.Clasificacion ?? "—");
                if (porMes) Celda(t.Cell(), NombreMes(f.Periodo));
                Celda(t.Cell(), Monto(f.NetoConsolidado), derecha: true);
                Celda(t.Cell(), f.Quincena == 0 ? "—" : Monto(f.Quincena), derecha: true);
                Celda(t.Cell(), Monto(f.FinDeMes), derecha: true);
                Celda(t.Cell(), f.RecuperacionAnticipos == 0 ? "—" : "-" + Monto(f.RecuperacionAnticipos), derecha: true);
                Celda(t.Cell(), Monto(f.APagar), derecha: true, negrita: true);
                Celda(t.Cell(), f.Revisado ? "Sí" : "Pendiente");
            }

            // Totales
            t.Cell().ColumnSpan(porMes ? 5u : 4u).Element(Total).Text($"Totales ({r.Filas.Count} colaboradores)").Bold();
            t.Cell().Element(Total).AlignRight().Text(Monto(r.Filas.Sum(f => f.NetoConsolidado))).Bold();
            t.Cell().Element(Total).AlignRight().Text(Monto(r.Filas.Sum(f => f.Quincena))).Bold();
            t.Cell().Element(Total).AlignRight().Text(Monto(r.Filas.Sum(f => f.FinDeMes))).Bold();
            t.Cell().Element(Total).AlignRight().Text("-" + Monto(r.Filas.Sum(f => f.RecuperacionAnticipos))).Bold();
            t.Cell().Element(Total).AlignRight().Text(Monto(r.Filas.Sum(f => f.APagar))).Bold().FontColor(Verde);
            t.Cell().Element(Total);
        });
    }

    private static void Cabecera(IContainer c, string texto, bool derecha = false)
    {
        var celda = c.Background(FondoCabecera).BorderBottom(1).BorderColor(Marino).PaddingVertical(5).PaddingHorizontal(4);
        if (derecha) celda = celda.AlignRight();
        celda.Text(texto).FontSize(8.5f).Bold().FontColor(Marino);
    }

    private static void Celda(IContainer c, string texto, bool derecha = false, bool negrita = false)
    {
        var celda = c.BorderBottom(0.5f).BorderColor(Borde).PaddingVertical(4).PaddingHorizontal(4);
        if (derecha) celda = celda.AlignRight();
        var t = celda.Text(texto);
        if (negrita) t.Bold();
    }

    private static IContainer Total(IContainer c) =>
        c.BorderTop(1.5f).BorderColor(Marino).Background(FondoCabecera).PaddingVertical(5).PaddingHorizontal(4);

    private static string Monto(decimal valor) => "$" + valor.ToString("N2", Numeros);

    private static string NombreMes(DateOnly p) => $"{Meses[p.Month - 1]} de {p.Year}";
}
