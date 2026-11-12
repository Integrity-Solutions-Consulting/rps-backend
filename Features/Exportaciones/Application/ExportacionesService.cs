using System.Globalization;
using Microsoft.EntityFrameworkCore;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Exportaciones.Application;

// Exportaciones de solo lectura en Excel y PDF (liquidaciones, honorarios y colaboradores).
// No hay forma de fallar: sin Resultado<T>.
public class ExportacionesService(RpsDbContext db)
{
    // Excel de honorarios: una fila por honorario (proyecto, o el contrato si se paga por contrato).
    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarHonorariosAsync(DateOnly? periodo, CancellationToken ct)
    {
        var filas = await FilasHonorariosAsync(periodo, ct);
        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);

        var hoja = new HojaExcel(
            "Honorarios", "Honorarios por proyecto",
            $"{(periodo is { } mes ? ReportesPdf.NombreMes(mes) : "Todos los períodos")} · {filas.Count} honorarios · Moneda {config.Moneda}",
            ["Colaborador", "Identificación", "Proyecto", "Período", "Modalidad", "Días reconocidos", "Días del mes",
             "Proporcional acordado", "Subtotal", "IVA", "Total del documento", "Retención en la fuente", "Retención de IVA",
             "Total de retenciones", "Neto"],
            filas.Select(f => new object?[]
            {
                NombreColaborador(f.Nombres, f.Apellidos, f.TmrColaboradorId), f.Identificacion, f.Proyecto, ReportesPdf.NombreMes(f.Periodo),
                f.Modalidad == Modalidad.MasIva ? "Más IVA" : "Neto", f.DiasReconocidos, f.DiasReales,
                f.ProporcionalAcordado, f.Subtotal, f.Iva, f.TotalDocumento, f.RetencionFuente, f.RetencionIva, f.TotalRetenciones, f.Neto
            }).ToList(),
            ["Totales", null, null, null, null, null, null,
             filas.Sum(f => f.ProporcionalAcordado), filas.Sum(f => f.Subtotal), filas.Sum(f => f.Iva), filas.Sum(f => f.TotalDocumento),
             filas.Sum(f => f.RetencionFuente), filas.Sum(f => f.RetencionIva), filas.Sum(f => f.TotalRetenciones), filas.Sum(f => f.Neto)],
            "No hay honorarios calculados para este período.");

        var contenido = ReportesExcel.Generar(config.RazonSocial, config.Ruc, [hoja]);
        var nombreArchivo = periodo is null ? "honorarios_todos.xlsx" : $"honorarios_{periodo:yyyy-MM}.xlsx";
        return (contenido, nombreArchivo);
    }

    // PDF de honorarios: mismos datos que el Excel, con totales.
    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarHonorariosPdfAsync(DateOnly? periodo, CancellationToken ct)
    {
        var filas = await FilasHonorariosAsync(periodo, ct);

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

    // Excel de liquidaciones.
    //   Consolidado: una hoja con una fila por colaborador (neto, quincena, fin de mes, cuotas, a pagar) y totales.
    //   PorColaborador: una hoja con un bloque por colaborador (honorarios con impuestos y retenciones, y cortes).
    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarLiquidacionesAsync(
        DateOnly? periodo, VistaLiquidaciones vista, CancellationToken ct)
    {
        var liquidaciones = await FilasLiquidacionesAsync(periodo, ct);
        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);
        var subtitulo = $"{(periodo is { } mes ? ReportesPdf.NombreMes(mes) : "Todos los períodos")} · {liquidaciones.Count} liquidaciones · Moneda {config.Moneda}";

        byte[] contenido;
        if (vista == VistaLiquidaciones.PorColaborador)
        {
            contenido = LiquidacionesExcel.PorColaborador(config.RazonSocial, config.Ruc, subtitulo, liquidaciones, porMes: periodo is null);
        }
        else
        {
            var resumen = new HojaExcel(
                "Consolidado", "Liquidaciones de honorarios · Consolidado", subtitulo,
                ["Colaborador", "Identificación", "Clasificación", "Período", "Neto consolidado", "Quincena", "Fin de mes",
                 "Cuotas de anticipos", "A pagar", "Revisada"],
                liquidaciones.Select(l => new object?[]
                {
                    l.Colaborador, l.Identificacion, l.Clasificacion ?? "Sin clasificar", ReportesPdf.NombreMes(l.Periodo),
                    l.NetoConsolidado, l.Quincena, l.FinDeMes, l.RecuperacionAnticipos, l.APagar, l.Revisado ? "Sí" : "Pendiente"
                }).ToList(),
                ["Totales", null, null, null, liquidaciones.Sum(l => l.NetoConsolidado), liquidaciones.Sum(l => l.Quincena),
                 liquidaciones.Sum(l => l.FinDeMes), liquidaciones.Sum(l => l.RecuperacionAnticipos), liquidaciones.Sum(l => l.APagar), null],
                "No hay liquidaciones para este período.");
            contenido = ReportesExcel.Generar(config.RazonSocial, config.Ruc, [resumen]);
        }

        return (contenido, NombreArchivoLiquidaciones(periodo, vista, "xlsx"));
    }

    // PDF formal de liquidaciones: el consolidado (tabla con todos y totales) o el detalle por
    // colaborador (sus honorarios con impuestos y retenciones, y sus cortes con anticipos).
    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarLiquidacionesPdfAsync(
        DateOnly? periodo, VistaLiquidaciones vista, CancellationToken ct)
    {
        var filas = await FilasLiquidacionesAsync(periodo, ct);
        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);
        var contenido = LiquidacionesPdf.Generar(new ReporteLiquidacionesPdf(
            config.RazonSocial, config.Ruc, config.Moneda, periodo, DateTime.UtcNow, filas, vista));

        return (contenido, NombreArchivoLiquidaciones(periodo, vista, "pdf"));
    }

    // liquidaciones_consolidado_2026-11.pdf · liquidaciones_por_colaborador_todas.xlsx
    private static string NombreArchivoLiquidaciones(DateOnly? periodo, VistaLiquidaciones vista, string extension) =>
        $"liquidaciones_{(vista == VistaLiquidaciones.PorColaborador ? "por_colaborador" : "consolidado")}_{(periodo is null ? "todas" : periodo.Value.ToString("yyyy-MM"))}.{extension}";

    // Excel de colaboradores: las mismas columnas que el PDF. Sin cuentas bancarias (están cifradas).
    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarColaboradoresAsync(CancellationToken ct)
    {
        var (titulo, subtitulo, encabezados, filas) = await DatosColaboradoresAsync(ct);
        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);
        var contenido = ReportesExcel.Generar(config.RazonSocial, config.Ruc,
            [new HojaExcel("Colaboradores", titulo, subtitulo, encabezados, filas.Select(f => f.Cast<object?>().ToArray()).ToList(),
                Vacio: "No hay colaboradores registrados.")]);
        return (contenido, "colaboradores.xlsx");
    }

    // PDF de colaboradores: lista de RPS con nombre, identificación, clasificación, frecuencia vigente y estado.
    // No incluye cuentas bancarias (están cifradas y no deben circular en un PDF).
    public async Task<(byte[] Contenido, string NombreArchivo)> ExportarColaboradoresPdfAsync(CancellationToken ct)
    {
        var (titulo, subtitulo, encabezados, filas) = await DatosColaboradoresAsync(ct);
        var anchos = new float[] { 3, 1.6f, 3, 1, 1.3f, 1.5f, 1.4f, 1.2f };
        var columnas = encabezados.Select((e, i) => new ColumnaPdf(e, anchos[i])).ToList();
        var activos = filas.Count(f => f[7] == "Activo");
        var configurados = filas.Count(f => f[6] == "Configurado");

        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);
        var contenido = ReportesPdf.Generar(new ReporteTablaPdf(
            config.RazonSocial, config.Ruc, titulo, $"Al {DateOnly.FromDateTime(DateTime.UtcNow):dd/MM/yyyy}", subtitulo,
            DateTime.UtcNow, columnas, filas,
            [$"Total: {filas.Count}", null, null, null, null, null, $"{configurados} configurados", $"{activos} activos"],
            "No hay colaboradores registrados."));

        return (contenido, "colaboradores.pdf");
    }

    private async Task<(string Titulo, string Subtitulo, string[] Encabezados, List<string[]> Filas)> DatosColaboradoresAsync(CancellationToken ct)
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

        var filas = colaboradores.Select(c => new[]
        {
            NombreColaborador(c.Nombres, c.Apellidos, c.TmrColaboradorId),
            c.Identificacion ?? "—",
            c.Correo ?? "—",
            c.TmrColaboradorId,
            c.Clasificacion?.ToString() ?? "Sin clasificar",
            frecuencias.TryGetValue(c.Id, out var frec) ? (frec == Frecuencia.DosPagos ? "Quincena y fin de mes" : "Fin de mes") : "—",
            c.EstadoConfiguracion == EstadoConfiguracion.Configurado ? "Configurado" : "Pendiente",
            c.Activo ? "Activo" : "Inactivo"
        }).ToList();

        var activos = colaboradores.Count(c => c.Activo);
        var configurados = colaboradores.Count(c => c.EstadoConfiguracion == EstadoConfiguracion.Configurado);
        return ("Colaboradores registrados",
            $"{colaboradores.Count} en total · {activos} activos · {configurados} configurados",
            ["Colaborador", "Identificación", "Correo", "ID TMR", "Clasificación", "Frecuencia", "Configuración", "Estado"],
            filas);
    }

    private record FilaHonorario(
        DateOnly Periodo, int DiasReales, int DiasReconocidos, Modalidad Modalidad, decimal ProporcionalAcordado,
        decimal Subtotal, decimal Iva, decimal TotalDocumento, decimal RetencionFuente, decimal RetencionIva,
        decimal TotalRetenciones, decimal Neto, string TmrColaboradorId, string? Nombres, string? Apellidos,
        string? Identificacion, string Proyecto);

    // Honorarios con nombre de colaborador y de proyecto. Left join: el de un contrato que se paga
    // por contrato no tiene proyecto.
    private async Task<List<FilaHonorario>> FilasHonorariosAsync(DateOnly? periodo, CancellationToken ct)
    {
        var query = db.Honorarios.AsNoTracking().AsQueryable();
        if (periodo is not null)
            query = query.Where(h => h.Periodo == periodo);

        return await query
            .Join(db.ColaboradoresRps.AsNoTracking(), h => h.ColaboradorId, c => c.Id, (h, c) => new { h, c })
            .GroupJoin(db.Proyectos.AsNoTracking(), x => x.h.ProyectoId, p => (Guid?)p.Id, (x, ps) => new { x.h, x.c, ps })
            .SelectMany(x => x.ps.DefaultIfEmpty(), (x, p) => new FilaHonorario(
                x.h.Periodo, x.h.DiasReales, x.h.DiasReconocidos, x.h.Modalidad, x.h.ProporcionalAcordado,
                x.h.Subtotal, x.h.Iva, x.h.TotalDocumento, x.h.RetencionFuente, x.h.RetencionIva,
                x.h.TotalRetenciones, x.h.Neto, x.c.TmrColaboradorId, x.c.Nombres, x.c.Apellidos,
                x.c.Identificacion, p != null ? p.Nombre : "Pago por contrato"))
            .OrderBy(f => f.Periodo).ThenBy(f => f.Apellidos).ThenBy(f => f.TmrColaboradorId).ThenBy(f => f.Proyecto)
            .ToListAsync(ct);
    }

    private static readonly string[] MesesCortos = ["ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic"];

    // Liquidaciones con todo su detalle (lo mismo que se ve en la pantalla Liquidaciones).
    private async Task<List<FilaLiquidacionPdf>> FilasLiquidacionesAsync(DateOnly? periodo, CancellationToken ct)
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
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.Tipo == TipoCorte.Quincena ? 0 : 1).ToList());

        var honorarios = (await db.Honorarios.AsNoTracking()
                .Where(h => h.LiquidacionId != null && ids.Contains(h.LiquidacionId!.Value))
                .GroupJoin(db.Proyectos.AsNoTracking(), h => h.ProyectoId, p => (Guid?)p.Id, (h, ps) => new { h, ps })
                .SelectMany(x => x.ps.DefaultIfEmpty(), (x, p) => new { x.h, Proyecto = p != null ? p.Nombre : "Pago por contrato" })
                .ToListAsync(ct))
            .GroupBy(x => x.h.LiquidacionId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Proyecto).Select(x => new DetalleHonorarioPdf(
                x.Proyecto, x.h.Modalidad == Modalidad.MasIva ? "Más IVA" : "Neto", x.h.DiasReconocidos, x.h.DiasReales,
                x.h.ProporcionalAcordado, x.h.Subtotal, x.h.Iva, x.h.TotalDocumento, x.h.RetencionFuente, x.h.RetencionIva,
                x.h.TotalRetenciones, x.h.Neto)).ToList());

        return liquidaciones.Select(l =>
        {
            var deLaLiquidacion = cortes.GetValueOrDefault(l.Id) ?? [];
            var nombre = $"{l.Nombres} {l.Apellidos}".Trim();
            var finPeriodo = l.Periodo.AddMonths(1).AddDays(-1);
            var mes = MesesCortos[l.Periodo.Month - 1];
            var tieneQuincena = deLaLiquidacion.Any(c => c.Tipo == TipoCorte.Quincena);
            var detalleCortes = deLaLiquidacion.Select(c => new DetalleCortePdf(
                c.Tipo == TipoCorte.Quincena ? "Quincena" : "Fin de mes",
                c.Tipo == TipoCorte.Quincena ? $"1 – 15 {mes}" : $"{(tieneQuincena ? 16 : 1)} – {finPeriodo.Day} {mes}",
                c.ValorBase, c.RecuperacionAdicional, c.ValorPreparado)).ToList();

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
                l.Revisado,
                honorarios.GetValueOrDefault(l.Id) ?? [],
                detalleCortes);
        }).ToList();
    }

    private static string NombreColaborador(string? nombres, string? apellidos, string tmrColaboradorId)
    {
        var nombre = $"{nombres} {apellidos}".Trim();
        return string.IsNullOrEmpty(nombre) ? $"ID TMR {tmrColaboradorId}" : nombre;
    }
}
