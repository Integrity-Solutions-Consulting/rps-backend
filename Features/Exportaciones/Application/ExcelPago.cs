using ClosedXML.Excel;
using rps_backend.Shared;

namespace rps_backend.Features.Exportaciones.Application;

public record FilaExcelPago(
    string Colaborador, string? Identificacion, string? Banco, string? TipoCuenta, string? NumeroCuenta, decimal Importe);

// Genera el Excel de pago (RN 52). El formato de columnas es provisional: la plantilla real del
// banco todavía no está definida (PA 02 del documento de requisitos); cuando se confirme, este es
// el único lugar que cambia — las reglas económicas que calculan Importe no dependen de esto.
public static class ExcelPago
{
    public static byte[] Generar(
        string razonSocial, string ruc, string moneda, DateOnly periodo, TipoCorte corte, int version,
        List<FilaExcelPago> filas)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Pago");

        hoja.Cell(1, 1).Value = razonSocial;
        hoja.Cell(2, 1).Value = $"RUC {ruc}";
        hoja.Cell(3, 1).Value = $"Período {periodo:yyyy-MM} · Corte {(corte == TipoCorte.Quincena ? "Quincena" : "Fin de mes")} · Versión {version}";
        hoja.Cell(4, 1).Value = $"Generado {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
        hoja.Range(1, 1, 4, 1).Style.Font.SetBold();

        var filaEncabezado = 6;
        string[] encabezados = ["Colaborador", "Identificación", "Banco", "Tipo de cuenta", "Número de cuenta", $"Importe ({moneda})"];
        for (var i = 0; i < encabezados.Length; i++)
            hoja.Cell(filaEncabezado, i + 1).Value = encabezados[i];
        hoja.Row(filaEncabezado).Style.Font.SetBold();

        var fila = filaEncabezado + 1;
        foreach (var f in filas)
        {
            hoja.Cell(fila, 1).Value = f.Colaborador;
            hoja.Cell(fila, 2).Value = f.Identificacion ?? "";
            hoja.Cell(fila, 3).Value = f.Banco ?? "";
            hoja.Cell(fila, 4).Value = f.TipoCuenta ?? "";
            hoja.Cell(fila, 5).Value = f.NumeroCuenta ?? "";
            hoja.Cell(fila, 6).Value = f.Importe;
            hoja.Cell(fila, 6).Style.NumberFormat.Format = "#,##0.00";
            fila++;
        }

        hoja.Cell(fila, 5).Value = "Total";
        hoja.Cell(fila, 5).Style.Font.SetBold();
        hoja.Cell(fila, 6).Value = filas.Sum(f => f.Importe);
        hoja.Cell(fila, 6).Style.Font.SetBold();
        hoja.Cell(fila, 6).Style.NumberFormat.Format = "#,##0.00";

        hoja.Columns(1, 6).AdjustToContents();

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }
}
