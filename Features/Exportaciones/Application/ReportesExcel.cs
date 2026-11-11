using ClosedXML.Excel;

namespace rps_backend.Features.Exportaciones.Application;

// Una hoja del Excel: título, encabezados y filas. Los valores decimal se escriben como número con
// formato de moneda (se pueden sumar en Excel); null queda vacío. Totales: la fila final, opcional.
public record HojaExcel(
    string Nombre, string Titulo, string Subtitulo, string[] Encabezados, IReadOnlyList<object?[]> Filas,
    object?[]? Totales = null, string Vacio = "Sin datos para este reporte.");

// Excel de los reportes (reemplaza a los CSV): una o varias hojas con el mismo encabezado de
// empresa, fila de títulos fija, filtros y columnas ajustadas.
public static class ReportesExcel
{
    private const string FormatoMonto = "#,##0.00";
    private static readonly XLColor Marino = XLColor.FromHtml("#13263D");
    private static readonly XLColor FondoCabecera = XLColor.FromHtml("#F1F5F9");

    public static byte[] Generar(string razonSocial, string ruc, IEnumerable<HojaExcel> hojas)
    {
        using var libro = new XLWorkbook();

        foreach (var h in hojas)
        {
            var hoja = libro.Worksheets.Add(h.Nombre.Length > 31 ? h.Nombre[..31] : h.Nombre);

            hoja.Cell(1, 1).Value = string.IsNullOrWhiteSpace(razonSocial) ? "Empresa sin configurar" : razonSocial;
            hoja.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(13).Font.SetFontColor(Marino);
            hoja.Cell(2, 1).Value = $"RUC {(string.IsNullOrWhiteSpace(ruc) ? "—" : ruc)}";
            hoja.Cell(3, 1).Value = h.Titulo;
            hoja.Cell(3, 1).Style.Font.SetBold().Font.SetFontSize(12);
            hoja.Cell(4, 1).Value = h.Subtitulo;
            hoja.Cell(5, 1).Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm} · Documento de control interno; no es comprobante de pago.";
            hoja.Cell(5, 1).Style.Font.SetItalic().Font.SetFontColor(XLColor.Gray);

            const int filaEncabezado = 7;
            for (var i = 0; i < h.Encabezados.Length; i++)
                hoja.Cell(filaEncabezado, i + 1).Value = h.Encabezados[i];
            var cabecera = hoja.Range(filaEncabezado, 1, filaEncabezado, h.Encabezados.Length);
            cabecera.Style.Font.SetBold().Font.SetFontColor(Marino).Fill.SetBackgroundColor(FondoCabecera);
            cabecera.Style.Border.SetBottomBorder(XLBorderStyleValues.Medium).Border.SetBottomBorderColor(Marino);

            var fila = filaEncabezado + 1;
            if (h.Filas.Count == 0)
            {
                hoja.Cell(fila, 1).Value = h.Vacio;
                hoja.Cell(fila, 1).Style.Font.SetItalic();
            }
            foreach (var valores in h.Filas)
                Escribir(hoja, fila++, valores);

            if (h.Filas.Count > 0)
            {
                hoja.Range(filaEncabezado, 1, fila - 1, h.Encabezados.Length).SetAutoFilter();
                if (h.Totales is not null)
                {
                    Escribir(hoja, fila, h.Totales);
                    var total = hoja.Range(fila, 1, fila, h.Encabezados.Length);
                    total.Style.Font.SetBold().Fill.SetBackgroundColor(FondoCabecera);
                    total.Style.Border.SetTopBorder(XLBorderStyleValues.Medium).Border.SetTopBorderColor(Marino);
                }
            }

            hoja.SheetView.FreezeRows(filaEncabezado);
            hoja.Columns(1, h.Encabezados.Length).AdjustToContents(filaEncabezado, fila);
            foreach (var columna in hoja.Columns(1, h.Encabezados.Length))
                if (columna.Width > 60) columna.Width = 60;
        }

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void Escribir(IXLWorksheet hoja, int fila, object?[] valores)
    {
        for (var i = 0; i < valores.Length; i++)
        {
            var celda = hoja.Cell(fila, i + 1);
            switch (valores[i])
            {
                case null:
                    break;
                case decimal d:
                    celda.Value = d;
                    celda.Style.NumberFormat.Format = FormatoMonto;
                    break;
                case int n:
                    celda.Value = n;
                    break;
                case DateOnly f:
                    celda.Value = f.ToDateTime(TimeOnly.MinValue);
                    celda.Style.DateFormat.Format = "dd/MM/yyyy";
                    break;
                default:
                    celda.Value = valores[i]!.ToString();
                    break;
            }
        }
    }
}
