using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Exportaciones.Application;

// Exportaciones de solo lectura (CSV y PDF de liquidaciones, honorarios y colaboradores). No hay forma de fallar: sin Resultado<T>.
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

    // PDF de honorarios: mismo filtro que el CSV, con nombres de colaborador y proyecto y totales.
    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarHonorariosPdfAsync(DateOnly? periodo, CancellationToken ct)
    {
        var query = db.Honorarios.AsNoTracking().AsQueryable();
        if (periodo is not null)
            query = query.Where(h => h.Periodo == periodo);

        var filas = await query
            .Join(db.ColaboradoresRps.AsNoTracking(), h => h.ColaboradorId, c => c.Id, (h, c) => new { h, c })
            // Left join: el honorario de un contrato que se paga por contrato no tiene proyecto.
            .GroupJoin(db.Proyectos.AsNoTracking(), x => x.h.ProyectoId, p => (Guid?)p.Id, (x, ps) => new { x.h, x.c, ps })
            .SelectMany(x => x.ps.DefaultIfEmpty(), (x, p) => new
            {
                x.h.Periodo, x.h.DiasReales, x.h.DiasReconocidos, x.h.Modalidad,
                x.h.Subtotal, x.h.Iva, x.h.RetencionFuente, x.h.RetencionIva, x.h.Neto,
                x.c.TmrColaboradorId, x.c.Nombres, x.c.Apellidos, Proyecto = p != null ? p.Nombre : "Pago por contrato"
            })
            .OrderBy(f => f.Periodo).ThenBy(f => f.Apellidos).ThenBy(f => f.TmrColaboradorId).ThenBy(f => f.Proyecto)
            .ToListAsync(ct);

        var todos = periodo is null;
        var columnas = new List<ColumnaPdf> { new("Colaborador", 3), new("Proyecto", 2.6f) };
        if (todos) columnas.Add(new("Período", 1.6f));
        columnas.AddRange([
            new("Días", 1), new("Modalidad", 1.2f), new("Subtotal", 1.4f, true), new("IVA", 1.2f, true),
            new("Ret. fuente", 1.3f, true), new("Ret. IVA", 1.3f, true), new("Neto", 1.4f, true)
        ]);

        var filasPdf = filas.Select(f =>
        {
            var celdas = new List<string> { NombreColaborador(f.Nombres, f.Apellidos, f.TmrColaboradorId), f.Proyecto };
            if (todos) celdas.Add(ReportesPdf.NombreMes(f.Periodo));
            celdas.AddRange([
                $"{f.DiasReconocidos}/{f.DiasReales}", f.Modalidad == Modalidad.MasIva ? "Más IVA" : "Neto",
                ReportesPdf.Monto(f.Subtotal), ReportesPdf.Monto(f.Iva), ReportesPdf.Monto(f.RetencionFuente),
                ReportesPdf.Monto(f.RetencionIva), ReportesPdf.Monto(f.Neto)
            ]);
            return celdas.ToArray();
        }).ToList();

        var totales = new List<string?> { $"Totales ({filas.Count} honorarios)", null };
        if (todos) totales.Add(null);
        totales.AddRange([
            null, null, ReportesPdf.Monto(filas.Sum(f => f.Subtotal)), ReportesPdf.Monto(filas.Sum(f => f.Iva)),
            ReportesPdf.Monto(filas.Sum(f => f.RetencionFuente)), ReportesPdf.Monto(filas.Sum(f => f.RetencionIva)),
            ReportesPdf.Monto(filas.Sum(f => f.Neto))
        ]);

        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);
        var personas = filas.Select(f => f.TmrColaboradorId).Distinct().Count();
        var contenido = ReportesPdf.Generar(new ReporteTablaPdf(
            config.RazonSocial, config.Ruc, "Honorarios por proyecto",
            periodo is { } mes ? ReportesPdf.NombreMes(mes) : "Todos los períodos",
            $"{personas} colaboradores · {filas.Count} honorarios · Moneda {config.Moneda}",
            DateTime.UtcNow, columnas, filasPdf, totales.ToArray(),
            "No hay honorarios calculados para este período."));

        var nombreArchivo = periodo is null ? "honorarios_todos.pdf" : $"honorarios_{periodo:yyyy-MM}.pdf";
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

    // PDF de colaboradores: lista de RPS con nombre, identificación, clasificación, frecuencia vigente y estado.
    // No incluye cuentas bancarias (están cifradas y no deben circular en un PDF).
    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarColaboradoresPdfAsync(CancellationToken ct)
    {
        var colaboradores = await db.ColaboradoresRps.AsNoTracking()
            .OrderBy(c => c.Apellidos).ThenBy(c => c.TmrColaboradorId)
            .ToListAsync(ct);

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var mesActual = new DateOnly(hoy.Year, hoy.Month, 1);
        var frecuencias = (await db.ColaboradoresFrecuencia.AsNoTracking()
                .Where(f => f.VigenteDesdeMes <= mesActual)
                .ToListAsync(ct))
            .GroupBy(f => f.ColaboradorId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.VigenteDesdeMes).First().Frecuencia);

        var columnas = new List<ColumnaPdf>
        {
            new("Colaborador", 3), new("Identificación", 1.6f), new("Correo", 3), new("ID TMR", 1),
            new("Clasificación", 1.3f), new("Frecuencia", 1.5f), new("Configuración", 1.4f), new("Estado", 1.2f)
        };

        var filasPdf = colaboradores.Select(c => new[]
        {
            NombreColaborador(c.Nombres, c.Apellidos, c.TmrColaboradorId),
            c.Identificacion ?? "—",
            c.Correo ?? "—",
            c.TmrColaboradorId,
            c.Clasificacion?.ToString() ?? "Sin clasificar",
            frecuencias.TryGetValue(c.Id, out var frec) ? (frec == Frecuencia.DosPagos ? "Quincena y fin de mes" : "Fin de mes") : "—",
            c.EstadoConfiguracion == EstadoConfiguracion.Configurado ? "Configurado" : "Pendiente",
            c.Activo ? "Activo" : "Desactivado"
        }).ToList();

        var activos = colaboradores.Count(c => c.Activo);
        var configurados = colaboradores.Count(c => c.EstadoConfiguracion == EstadoConfiguracion.Configurado);
        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);
        var contenido = ReportesPdf.Generar(new ReporteTablaPdf(
            config.RazonSocial, config.Ruc, "Colaboradores registrados",
            $"Al {hoy:dd/MM/yyyy}",
            $"{colaboradores.Count} en total · {activos} activos · {configurados} configurados",
            DateTime.UtcNow, columnas, filasPdf,
            [$"Total: {colaboradores.Count}", null, null, null, null, null, $"{configurados} configurados", $"{activos} activos"],
            "No hay colaboradores registrados."));

        return (contenido, "colaboradores.pdf");
    }

    private static string NombreColaborador(string? nombres, string? apellidos, string tmrColaboradorId)
    {
        var nombre = $"{nombres} {apellidos}".Trim();
        return string.IsNullOrEmpty(nombre) ? $"ID TMR {tmrColaboradorId}" : nombre;
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
