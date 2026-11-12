using ClosedXML.Excel;

namespace rps_backend.Features.Exportaciones.Application;

// Excel de liquidaciones "por colaborador": una sola hoja con un bloque por colaborador, igual que
// el PDF por colaborador — encabezado (nombre, identificación, clasificación, neto consolidado,
// revisión), tabla de honorarios con impuestos y retenciones, y tabla de cortes con anticipos.
public static class LiquidacionesExcel
{
    private const string FormatoMonto = "#,##0.00";
    private static readonly XLColor Marino = XLColor.FromHtml("#13263D");
    private static readonly XLColor Verde = XLColor.FromHtml("#0F766E");
    private static readonly XLColor FondoCabecera = XLColor.FromHtml("#F1F5F9");

    private static readonly string[] EncabezadosHonorarios =
        ["Proyecto", "Modalidad", "Días", "Proporcional", "Subtotal", "IVA", "Total doc.", "Ret. fuente", "Ret. IVA", "Total ret.", "Neto"];

    private static readonly string[] EncabezadosCortes = ["Corte", "Días", "Base", "Cuotas anticipos", "Valor preparado"];

    public static byte[] PorColaborador(
        string razonSocial, string ruc, string subtitulo, IReadOnlyList<FilaLiquidacionPdf> filas, bool porMes)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Por colaborador");

        hoja.Cell(1, 1).Value = string.IsNullOrWhiteSpace(razonSocial) ? "Empresa sin configurar" : razonSocial;
        hoja.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(13).Font.SetFontColor(Marino);
        hoja.Cell(2, 1).Value = $"RUC {(string.IsNullOrWhiteSpace(ruc) ? "—" : ruc)}";
        hoja.Cell(3, 1).Value = "Liquidaciones de honorarios · Por colaborador";
        hoja.Cell(3, 1).Style.Font.SetBold().Font.SetFontSize(12);
        hoja.Cell(4, 1).Value = subtitulo;
        hoja.Cell(5, 1).Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm} · Documento de control interno; no es comprobante de pago.";
        hoja.Cell(5, 1).Style.Font.SetItalic().Font.SetFontColor(XLColor.Gray);

        var fila = 7;
        if (filas.Count == 0)
        {
            hoja.Cell(fila, 1).Value = "No hay liquidaciones para este período.";
            hoja.Cell(fila, 1).Style.Font.SetItalic();
        }

        foreach (var f in filas)
        {
            // Encabezado del colaborador.
            var datos = $"{f.Identificacion ?? "—"} · {f.Clasificacion ?? "Sin clasificar"}{(porMes ? " · " + ReportesPdf.NombreMes(f.Periodo) : "")}";
            hoja.Cell(fila, 1).Value = f.Colaborador;
            hoja.Cell(fila, 1).Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(Marino);
            hoja.Cell(fila, 3).Value = datos;
            hoja.Cell(fila, 3).Style.Font.SetFontColor(XLColor.Gray);
            hoja.Cell(fila, 8).Value = "Neto consolidado";
            hoja.Cell(fila, 10).Value = f.NetoConsolidado;
            hoja.Cell(fila, 10).Style.NumberFormat.Format = FormatoMonto;
            hoja.Cell(fila, 11).Value = f.Revisado ? "Revisada" : "Pendiente de revisión";
            var cabeceraColaborador = hoja.Range(fila, 1, fila, EncabezadosHonorarios.Length);
            cabeceraColaborador.Style.Border.SetBottomBorder(XLBorderStyleValues.Medium).Border.SetBottomBorderColor(Marino);
            hoja.Range(fila, 8, fila, 11).Style.Font.SetBold().Font.SetFontColor(Verde);
            fila += 2;

            // Honorarios.
            Cabecera(hoja, fila++, EncabezadosHonorarios);
            foreach (var h in f.Honorarios)
            {
                Escribir(hoja, fila++, [
                    h.Proyecto, h.Modalidad, $"{h.DiasReconocidos}/{h.DiasReales}", h.ProporcionalAcordado, h.Subtotal, h.Iva,
                    h.TotalDocumento, -h.RetencionFuente, -h.RetencionIva, -h.TotalRetenciones, h.Neto
                ]);
                hoja.Cell(fila - 1, EncabezadosHonorarios.Length).Style.Font.SetBold();
            }
            fila++;

            // Cortes.
            if (f.Cortes.Count > 0)
            {
                Cabecera(hoja, fila++, EncabezadosCortes);
                foreach (var k in f.Cortes)
                {
                    Escribir(hoja, fila++, [
                        k.Corte, k.Rango, k.ValorBase, k.CuotasAnticipos == 0 ? null : -k.CuotasAnticipos, k.ValorPreparado
                    ]);
                    hoja.Cell(fila - 1, EncabezadosCortes.Length).Style.Font.SetBold();
                }
                Escribir(hoja, fila, ["A pagar", null, null, null, f.APagar]);
                var total = hoja.Range(fila, 1, fila, EncabezadosCortes.Length);
                total.Style.Font.SetBold().Fill.SetBackgroundColor(FondoCabecera);
                total.Style.Border.SetTopBorder(XLBorderStyleValues.Medium).Border.SetTopBorderColor(Marino);
                fila++;
            }

            fila += 2; // espacio antes del siguiente colaborador
        }

        hoja.Columns(1, EncabezadosHonorarios.Length).AdjustToContents(7, Math.Max(fila, 7));
        foreach (var columna in hoja.Columns(1, EncabezadosHonorarios.Length))
        {
            if (columna.Width > 45) columna.Width = 45;
            if (columna.Width < 12) columna.Width = 12;
        }

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void Cabecera(IXLWorksheet hoja, int fila, string[] encabezados)
    {
        for (var i = 0; i < encabezados.Length; i++)
            hoja.Cell(fila, i + 1).Value = encabezados[i];
        var rango = hoja.Range(fila, 1, fila, encabezados.Length);
        rango.Style.Font.SetBold().Font.SetFontColor(Marino).Fill.SetBackgroundColor(FondoCabecera);
        rango.Style.Border.SetBottomBorder(XLBorderStyleValues.Thin).Border.SetBottomBorderColor(Marino);
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
                default:
                    celda.Value = valores[i]!.ToString();
                    break;
            }
        }
    }
}
