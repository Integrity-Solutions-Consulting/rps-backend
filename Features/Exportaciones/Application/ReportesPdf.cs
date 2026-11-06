using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace rps_backend.Features.Exportaciones.Application;

// Columna de un reporte PDF. Ancho relativo; Derecha = montos y números.
public record ColumnaPdf(string Titulo, float Ancho, bool Derecha = false);

// Reporte tabular genérico (Honorarios, Colaboradores). Mismo diseño que LiquidacionesPdf:
// logo, encabezado con datos de la empresa, tabla, fila de totales y pie con página.
// Totales: un valor por columna (null = celda vacía); el primero suele ser la etiqueta "Totales".
public record ReporteTablaPdf(
    string RazonSocial,
    string Ruc,
    string Titulo,
    string Subtitulo,
    string Resumen,
    DateTime GeneradoEn,
    IReadOnlyList<ColumnaPdf> Columnas,
    IReadOnlyList<string[]> Filas,
    string?[]? Totales,
    string MensajeVacio);

public static class ReportesPdf
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

    public static string Monto(decimal valor) => (valor < 0 ? "-$" : "$") + Math.Abs(valor).ToString("N2", Numeros);

    public static string NombreMes(DateOnly p) => $"{Meses[p.Month - 1]} de {p.Year}";

    public static byte[] Generar(ReporteTablaPdf r) =>
        Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(28);
            page.DefaultTextStyle(t => t.FontSize(9).FontColor(Colors.Black));

            page.Header().Element(c => Encabezado(c, r));
            page.Content().PaddingVertical(12).Element(c => Tabla(c, r));
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

    private static void Encabezado(IContainer c, ReporteTablaPdf r)
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
                    titulo.Item().AlignRight().Text(r.Titulo).FontSize(15).Bold().FontColor(Marino);
                    titulo.Item().AlignRight().Text(r.Subtitulo).FontSize(11).FontColor(Verde);
                    titulo.Item().AlignRight().Text(r.Resumen).FontColor(Gris);
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

    private static void Tabla(IContainer c, ReporteTablaPdf r)
    {
        if (r.Filas.Count == 0)
        {
            c.PaddingTop(30).AlignCenter().Text(r.MensajeVacio).FontSize(11).FontColor(Gris);
            return;
        }

        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(24); // N.º
                foreach (var col in r.Columnas) cols.RelativeColumn(col.Ancho);
            });

            t.Header(h =>
            {
                Cabecera(h.Cell(), "N.º", derecha: false);
                foreach (var col in r.Columnas) Cabecera(h.Cell(), col.Titulo, col.Derecha);
            });

            var n = 0;
            foreach (var fila in r.Filas)
            {
                n++;
                Celda(t.Cell(), n.ToString(CultureInfo.InvariantCulture), derecha: false);
                for (var i = 0; i < r.Columnas.Count; i++)
                    Celda(t.Cell(), i < fila.Length ? fila[i] : string.Empty, r.Columnas[i].Derecha);
            }

            if (r.Totales is null) return;
            t.Cell().Element(Total);
            for (var i = 0; i < r.Columnas.Count; i++)
            {
                var celda = t.Cell().Element(Total);
                if (r.Columnas[i].Derecha) celda = celda.AlignRight();
                var valor = i < r.Totales.Length ? r.Totales[i] : null;
                celda.Text(valor ?? string.Empty).Bold().FontColor(i == r.Columnas.Count - 1 ? Verde : Colors.Black);
            }
        });
    }

    private static void Cabecera(IContainer c, string texto, bool derecha)
    {
        var celda = c.Background(FondoCabecera).BorderBottom(1).BorderColor(Marino).PaddingVertical(5).PaddingHorizontal(4);
        if (derecha) celda = celda.AlignRight();
        celda.Text(texto).FontSize(8.5f).Bold().FontColor(Marino);
    }

    private static void Celda(IContainer c, string texto, bool derecha)
    {
        var celda = c.BorderBottom(0.5f).BorderColor(Borde).PaddingVertical(4).PaddingHorizontal(4);
        if (derecha) celda = celda.AlignRight();
        celda.Text(texto);
    }

    private static IContainer Total(IContainer c) =>
        c.BorderTop(1.5f).BorderColor(Marino).Background(FondoCabecera).PaddingVertical(5).PaddingHorizontal(4);
}
