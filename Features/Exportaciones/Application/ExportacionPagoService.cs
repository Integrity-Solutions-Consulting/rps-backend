using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Colaboradores.Application;
using rps_backend.Features.Exportaciones.Domain;
using rps_backend.Features.Liquidaciones.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Exportaciones.Application;

// El Excel de pago por corte, con selección, previsualización, versiones y descarga (RN 52-57).
// No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
public class ExportacionPagoService(RpsDbContext db, CuentaBancariaProtector protector)
{
    public async Task<Resultado<PreviewExportacionPagoResponse>> PrevisualizarAsync(
        DateOnly? periodo, TipoCorte? corte, CancellationToken ct)
    {
        if (periodo is not { Day: 1 } p)
            return ExportacionPagoErrores.PeriodoInvalido;
        if (corte is not { } c)
            return ExportacionPagoErrores.CorteRequerido;

        var candidatos = await CandidatosAsync(p, c, ct);
        var version = await SiguienteVersionAsync(p, c, ct);

        var items = candidatos.Select(x => new ItemPreviewResponse(
            x.ColaboradorId, x.Colaborador, x.Importe, x.TieneCuentaBancaria, x.Advertencias)).ToList();

        return new PreviewExportacionPagoResponse(p, c, version, items, items.Sum(i => i.Importe));
    }

    public async Task<Resultado<ExportacionPagoResponse>> GenerarAsync(
        GenerarExportacionPagoRequest request, Guid usuarioId, CancellationToken ct)
    {
        if (request.Periodo is not { Day: 1 } periodo)
            return ExportacionPagoErrores.PeriodoInvalido;
        if (request.Corte is not { } corte)
            return ExportacionPagoErrores.CorteRequerido;
        if (request.ColaboradorIds is null || request.ColaboradorIds.Count == 0)
            return ExportacionPagoErrores.SinColaboradores;

        var candidatos = await CandidatosAsync(periodo, corte, ct);
        var porColaborador = candidatos.ToDictionary(x => x.ColaboradorId);

        var seleccionados = new List<Candidato>();
        foreach (var id in request.ColaboradorIds.Distinct())
        {
            if (!porColaborador.TryGetValue(id, out var candidato))
                return ExportacionPagoErrores.ColaboradorSinCorte(id);
            seleccionados.Add(candidato);
        }

        var version = await SiguienteVersionAsync(periodo, corte, ct);
        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);

        var filasExcel = seleccionados.Select(x => new FilaExcelPago(
            x.Colaborador, x.Identificacion, x.Banco, x.TipoCuenta,
            x.NumeroCuentaCifrado is null ? null : protector.Descifrar(x.NumeroCuentaCifrado),
            x.Importe)).ToList();

        var contenido = ExcelPago.Generar(config.RazonSocial, config.Ruc, config.Moneda, periodo, corte, version, filasExcel);
        var nombreArchivo = $"pago_{periodo:yyyy-MM}_{(corte == TipoCorte.Quincena ? "quincena" : "fin-de-mes")}_v{version}.xlsx";

        var exportacion = new Exportacion
        {
            Periodo = periodo,
            Corte = corte,
            Version = version,
            GeneradoPor = usuarioId,
            NombreArchivo = nombreArchivo,
            Contenido = contenido,
            Items = seleccionados.Select(x => new ExportacionItem
            {
                ColaboradorId = x.ColaboradorId,
                LiquidacionId = x.LiquidacionId,
                Importe = x.Importe
            }).ToList()
        };

        db.Exportaciones.Add(exportacion);
        await db.SaveChangesAsync(ct);

        return AResponse(exportacion, seleccionados.ToDictionary(x => x.ColaboradorId, x => x.Colaborador));
    }

    public async Task<List<ExportacionPagoResponse>> ListarAsync(DateOnly? periodo, TipoCorte? corte, CancellationToken ct)
    {
        var query = db.Exportaciones.AsNoTracking().Include(e => e.Items).AsQueryable();
        if (periodo is not null)
            query = query.Where(e => e.Periodo == periodo);
        if (corte is not null)
            query = query.Where(e => e.Corte == corte);

        var exportaciones = await query.OrderByDescending(e => e.GeneradoEn).ToListAsync(ct);

        var colaboradorIds = exportaciones.SelectMany(e => e.Items).Select(i => i.ColaboradorId).Distinct().ToList();
        var nombres = await NombresPorColaboradorAsync(colaboradorIds, ct);

        return exportaciones.Select(e => AResponse(e, nombres)).ToList();
    }

    // Descarga: lee el archivo guardado, nunca lo regenera (RN 55).
    public async Task<Resultado<(byte[] Contenido, string NombreArchivo)>> DescargarAsync(Guid id, CancellationToken ct)
    {
        var exportacion = await db.Exportaciones.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exportacion is null)
            return ExportacionPagoErrores.NoEncontrada(id);

        return (exportacion.Contenido, exportacion.NombreArchivo);
    }

    private async Task<int> SiguienteVersionAsync(DateOnly periodo, TipoCorte corte, CancellationToken ct)
    {
        var maxima = await db.Exportaciones.AsNoTracking()
            .Where(e => e.Periodo == periodo && e.Corte == corte)
            .Select(e => e.Version)
            .DefaultIfEmpty(0)
            .MaxAsync(ct);
        return maxima + 1;
    }

    private record Candidato(
        Guid ColaboradorId, string Colaborador, string? Identificacion, Guid LiquidacionId, decimal Importe,
        bool TieneCuentaBancaria, string? Banco, string? TipoCuenta, string? NumeroCuentaCifrado, List<string> Advertencias);

    // Un colaborador por fila, con su Corte.ValorPreparado del período (RN 54) y las advertencias
    // que Contabilidad debe ver antes de generar (RN 53) — ninguna bloquea la exportación (RN 57).
    private async Task<List<Candidato>> CandidatosAsync(DateOnly periodo, TipoCorte corte, CancellationToken ct)
    {
        var filas = await db.Liquidaciones.AsNoTracking()
            .Where(l => l.Periodo == periodo)
            .Join(db.Cortes.AsNoTracking().Where(c => c.Tipo == corte), l => l.Id, c => c.LiquidacionId, (l, c) => new { l, c })
            .Join(db.ColaboradoresRps.AsNoTracking(), x => x.l.ColaboradorId, co => co.Id, (x, co) => new { x.l, x.c, co })
            .ToListAsync(ct);

        if (filas.Count == 0)
            return [];

        var colaboradorIds = filas.Select(f => f.co.Id).ToList();

        var cuentas = await db.CuentasBancarias.AsNoTracking()
            .Where(cb => colaboradorIds.Contains(cb.ColaboradorId) && cb.Activa)
            .ToDictionaryAsync(cb => cb.ColaboradorId, ct);

        // Versiones anteriores de este mismo (Periodo, Corte) que ya incluyeron a cada colaborador (RN 55:
        // "identificar registros previamente exportados y advertir sobre su repetición").
        var versionesPorColaborador = (await db.Exportaciones.AsNoTracking()
                .Where(e => e.Periodo == periodo && e.Corte == corte)
                .SelectMany(e => e.Items.Select(i => new { i.ColaboradorId, e.Version }))
                .Where(x => colaboradorIds.Contains(x.ColaboradorId))
                .ToListAsync(ct))
            .GroupBy(x => x.ColaboradorId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Version).OrderBy(v => v).ToList());

        return filas.Select(f =>
        {
            var cuenta = cuentas.GetValueOrDefault(f.co.Id);
            var nombre = $"{f.co.Nombres} {f.co.Apellidos}".Trim();
            if (string.IsNullOrEmpty(nombre)) nombre = $"ID TMR {f.co.TmrColaboradorId}";

            var advertencias = new List<string>();
            if (cuenta is null)
                advertencias.Add("Sin datos bancarios registrados.");
            if (f.c.RecuperacionNoAplicada > 0)
                advertencias.Add($"El corte no alcanzó para descontar {f.c.RecuperacionNoAplicada:0.00} de la cuota de anticipo programada.");
            if (f.c.AjustePorProrrateo > 0)
                advertencias.Add($"La quincena se redujo {f.c.AjustePorProrrateo:0.00} por una salida en el mes.");
            if (versionesPorColaborador.TryGetValue(f.co.Id, out var versiones))
                advertencias.Add($"Ya se exportó en la versión {string.Join(", ", versiones)} de este período y corte.");

            return new Candidato(
                f.co.Id, nombre, f.co.Identificacion, f.l.Id, f.c.ValorPreparado,
                cuenta is not null, cuenta?.Banco, cuenta?.TipoCuenta, cuenta?.NumeroCuentaCifrado, advertencias);
        }).OrderBy(x => x.Colaborador).ToList();
    }

    private async Task<Dictionary<Guid, string>> NombresPorColaboradorAsync(List<Guid> colaboradorIds, CancellationToken ct)
    {
        if (colaboradorIds.Count == 0)
            return [];

        return await db.ColaboradoresRps.AsNoTracking()
            .Where(c => colaboradorIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c =>
            {
                var nombre = $"{c.Nombres} {c.Apellidos}".Trim();
                return string.IsNullOrEmpty(nombre) ? $"ID TMR {c.TmrColaboradorId}" : nombre;
            }, ct);
    }

    private static ExportacionPagoResponse AResponse(Exportacion e, Dictionary<Guid, string> nombres) => new(
        e.Id, e.Periodo, e.Corte, e.Version, e.GeneradoPor, e.GeneradoEn, e.NombreArchivo,
        e.Items.Sum(i => i.Importe),
        e.Items.Select(i => new ExportacionPagoItemResponse(
            i.ColaboradorId, nombres.GetValueOrDefault(i.ColaboradorId, "Colaborador"), i.Importe)).ToList());
}
