using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using rps_backend.Persistence;

namespace rps_backend.Features.Exportaciones.Application;

// Exportaciones CSV de solo lectura. No hay forma de fallar: sin Resultado<T>.
// Todo número/fecha se formatea con InvariantCulture: el locale del servidor usa coma decimal,
// y una coma rompería las columnas del CSV.
public class ExportacionesService(RpsDbContext db)
{
    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarHonorariosAsync(DateOnly? periodo, CancellationToken ct)
    {
        var query = db.Honorarios.AsNoTracking().AsQueryable();
        if (periodo is not null)
            query = query.Where(h => h.Periodo == periodo);

        var filas = await query
            .Join(db.ColaboradoresRps.AsNoTracking(), h => h.ColaboradorId, c => c.Id, (h, c) => new
            {
                h.Id, h.ColaboradorId, c.TmrColaboradorId, h.Periodo, h.NumeroPago, h.Monto, h.Descuento, h.Estado
            })
            .OrderBy(f => f.Periodo).ThenBy(f => f.TmrColaboradorId).ThenBy(f => f.NumeroPago)
            .ToListAsync(ct);

        var encabezados = new[] { "id", "colaborador_id", "tmr_colaborador_id", "periodo", "numero_pago", "monto", "descuento", "estado" };
        var contenido = ConstruirCsv(encabezados, filas.Select(f => new[]
        {
            f.Id.ToString(), f.ColaboradorId.ToString(), f.TmrColaboradorId, FormatFecha(f.Periodo),
            f.NumeroPago.ToString(CultureInfo.InvariantCulture), FormatMonto(f.Monto), FormatMonto(f.Descuento), f.Estado.ToString()
        }));

        var nombreArchivo = periodo is null ? "honorarios_todos.csv" : $"honorarios_{periodo:yyyy-MM}.csv";
        return (contenido, nombreArchivo);
    }

    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarLiquidacionesAsync(DateOnly? periodo, CancellationToken ct)
    {
        var query = db.Liquidaciones.AsNoTracking().AsQueryable();
        if (periodo is not null)
            query = query.Where(l => l.Periodo == periodo);

        var filas = await query
            .Join(db.ColaboradoresRps.AsNoTracking(), l => l.ColaboradorId, c => c.Id, (l, c) => new
            {
                l.Id, l.ColaboradorId, c.TmrColaboradorId, l.Periodo, l.MontoTotal, l.Estado, l.PagadaEn
            })
            .OrderBy(f => f.Periodo).ThenBy(f => f.TmrColaboradorId)
            .ToListAsync(ct);

        var encabezados = new[] { "id", "colaborador_id", "tmr_colaborador_id", "periodo", "monto_total", "estado", "pagada_en" };
        var contenido = ConstruirCsv(encabezados, filas.Select(f => new[]
        {
            f.Id.ToString(), f.ColaboradorId.ToString(), f.TmrColaboradorId, FormatFecha(f.Periodo),
            FormatMonto(f.MontoTotal), f.Estado.ToString(), FormatFechaHora(f.PagadaEn)
        }));

        var nombreArchivo = periodo is null ? "liquidaciones_todas.csv" : $"liquidaciones_{periodo:yyyy-MM}.csv";
        return (contenido, nombreArchivo);
    }

    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarColaboradoresAsync(CancellationToken ct)
    {
        var filas = await db.ColaboradoresRps.AsNoTracking()
            .OrderBy(c => c.TmrColaboradorId)
            .ToListAsync(ct);

        var encabezados = new[] { "id", "tmr_colaborador_id", "clasificacion", "estado_configuracion", "activo", "creado_en" };
        var contenido = ConstruirCsv(encabezados, filas.Select(c => new[]
        {
            c.Id.ToString(), c.TmrColaboradorId, c.Clasificacion?.ToString() ?? string.Empty, c.EstadoConfiguracion.ToString(),
            c.Activo ? "true" : "false", FormatFechaHora(c.CreadoEn)
        }));

        return (contenido, "colaboradores.csv");
    }

    private static string FormatMonto(decimal valor) => valor.ToString("F2", CultureInfo.InvariantCulture);

    private static string FormatFecha(DateOnly valor) => valor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatFechaHora(DateTime? valor) =>
        valor?.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) ?? string.Empty;

    private static byte[] ConstruirCsv(IEnumerable<string> encabezados, IEnumerable<IEnumerable<string?>> filas)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', encabezados.Select(EscaparCsv)));

        foreach (var fila in filas)
            sb.AppendLine(string.Join(',', fila.Select(EscaparCsv)));

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscaparCsv(string? valor)
    {
        valor ??= string.Empty;
        return valor.Contains(',') || valor.Contains('"') || valor.Contains('\n')
            ? "\"" + valor.Replace("\"", "\"\"") + "\""
            : valor;
    }
}
