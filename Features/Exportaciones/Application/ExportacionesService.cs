using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Exportaciones.Application;

// Exportaciones de solo lectura (CSV y el PDF de liquidaciones). No hay forma de fallar: sin Resultado<T>.
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
                h.Id, h.ColaboradorId, c.TmrColaboradorId, h.ProyectoId, h.Periodo, h.Modalidad,
                h.DiasReales, h.DiasReconocidos, h.Subtotal, h.Iva, h.RetencionFuente, h.RetencionIva, h.Neto
            })
            .OrderBy(f => f.Periodo).ThenBy(f => f.TmrColaboradorId)
            .ToListAsync(ct);

        var encabezados = new[]
        {
            "id", "colaborador_id", "tmr_colaborador_id", "proyecto_id", "periodo", "modalidad",
            "dias_reales", "dias_reconocidos", "subtotal", "iva", "retencion_fuente", "retencion_iva", "neto"
        };
        var contenido = ConstruirCsv(encabezados, filas.Select(f => new[]
        {
            f.Id.ToString(), f.ColaboradorId.ToString(), f.TmrColaboradorId, f.ProyectoId.ToString(), FormatFecha(f.Periodo), f.Modalidad.ToString(),
            f.DiasReales.ToString(CultureInfo.InvariantCulture), f.DiasReconocidos.ToString(CultureInfo.InvariantCulture),
            FormatMonto(f.Subtotal), FormatMonto(f.Iva), FormatMonto(f.RetencionFuente), FormatMonto(f.RetencionIva), FormatMonto(f.Neto)
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
                l.Id, l.ColaboradorId, c.TmrColaboradorId, l.Periodo, l.NetoConsolidado, l.Revisado
            })
            .OrderBy(f => f.Periodo).ThenBy(f => f.TmrColaboradorId)
            .ToListAsync(ct);

        var encabezados = new[] { "id", "colaborador_id", "tmr_colaborador_id", "periodo", "neto_consolidado", "revisado" };
        var contenido = ConstruirCsv(encabezados, filas.Select(f => new[]
        {
            f.Id.ToString(), f.ColaboradorId.ToString(), f.TmrColaboradorId, FormatFecha(f.Periodo),
            FormatMonto(f.NetoConsolidado), f.Revisado ? "true" : "false"
        }));

        var nombreArchivo = periodo is null ? "liquidaciones_todas.csv" : $"liquidaciones_{periodo:yyyy-MM}.csv";
        return (contenido, nombreArchivo);
    }

    // Mismo filtro que el CSV de liquidaciones, con nombres, cortes y totales para un PDF formal.
    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarLiquidacionesPdfAsync(DateOnly? periodo, CancellationToken ct)
    {
        var query = db.Liquidaciones.AsNoTracking().AsQueryable();
        if (periodo is not null)
            query = query.Where(l => l.Periodo == periodo);

        var liquidaciones = await query
            .Join(db.ColaboradoresRps.AsNoTracking(), l => l.ColaboradorId, c => c.Id, (l, c) => new
            {
                l.Id, l.Periodo, l.NetoConsolidado, l.Revisado,
                c.TmrColaboradorId, c.Nombres, c.Apellidos, c.Identificacion, c.Clasificacion
            })
            .OrderBy(f => f.Periodo).ThenBy(f => f.Apellidos).ThenBy(f => f.TmrColaboradorId)
            .ToListAsync(ct);

        var ids = liquidaciones.Select(l => l.Id).ToList();
        var cortes = (await db.Cortes.AsNoTracking()
                .Where(c => ids.Contains(c.LiquidacionId))
                .ToListAsync(ct))
            .GroupBy(c => c.LiquidacionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var filas = liquidaciones.Select(l =>
        {
            var deLaLiquidacion = cortes.GetValueOrDefault(l.Id) ?? [];
            var nombre = $"{l.Nombres} {l.Apellidos}".Trim();
            return new FilaLiquidacionPdf(
                string.IsNullOrEmpty(nombre) ? $"ID TMR {l.TmrColaboradorId}" : nombre,
                l.Identificacion,
                l.Clasificacion?.ToString(),
                l.Periodo,
                l.NetoConsolidado,
                deLaLiquidacion.Where(c => c.Tipo == TipoCorte.Quincena).Sum(c => c.ValorPreparado),
                deLaLiquidacion.Where(c => c.Tipo == TipoCorte.FinDeMes).Sum(c => c.ValorPreparado),
                deLaLiquidacion.Sum(c => c.RecuperacionAdicional),
                deLaLiquidacion.Count > 0 ? deLaLiquidacion.Sum(c => c.ValorPreparado) : l.NetoConsolidado,
                l.Revisado);
        }).ToList();

        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);
        var contenido = LiquidacionesPdf.Generar(new ReporteLiquidacionesPdf(
            config.RazonSocial, config.Ruc, config.Moneda, periodo, DateTime.UtcNow, filas));

        var nombreArchivo = periodo is null ? "liquidaciones_todas.pdf" : $"liquidaciones_{periodo:yyyy-MM}.pdf";
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
