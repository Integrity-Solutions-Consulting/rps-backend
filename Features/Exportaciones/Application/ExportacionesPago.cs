using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Colaboradores.Application;
using rps_backend.Features.Exportaciones.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Exportaciones.Application;

// Archivo de pago por mes y corte, con versiones (sección 12 del documento de requisitos).
// Los nombres de los campos coinciden con rps-frontend/src/app/features/exportaciones/models/exportacion.models.ts.

// ===== Requests =====

public record GenerarExportacionRequest(string? Periodo, TipoCorte? Corte, List<Guid>? LiquidacionIds);

// ===== Responses =====

public record CandidatoExportacionResponse(
    Guid LiquidacionId, Guid ColaboradorId, string TmrColaboradorId,
    // Hasta que llegue la integración con TMR, el backend no conoce nombres: va el ID de TMR.
    string Colaborador,
    decimal Valor, string? Banco, string? Cuenta, bool Revisado, bool DocumentoRegistrado,
    // El backend actual no recalcula liquidaciones: siempre false.
    bool RequiereRecalculo,
    int? VersionExportada, List<string> Advertencias);

public record PreparacionExportacionResponse(
    string Periodo, TipoCorte Corte, bool Calculado, List<CandidatoExportacionResponse> Candidatos, int SiguienteVersion);

public record FilaExportadaResponse(Guid ColaboradorId, string TmrColaboradorId, string Colaborador, decimal Valor, string? Banco, string? Cuenta);

public record ExportacionResponse(
    Guid Id, string Periodo, TipoCorte Corte, int Version, DateTime GeneradaEn, string Usuario,
    List<FilaExportadaResponse> Filas, decimal Total, bool Vigente, bool Desactualizada, string NombreArchivo);

// ===== Errores =====

public static class ExportacionPagoErrores
{
    public static readonly Error PeriodoInvalido =
        Error.Validacion("Exportacion.PeriodoInvalido", "periodo", "Es obligatorio, con formato yyyy-MM.");

    public static readonly Error CorteInvalido =
        Error.Validacion("Exportacion.CorteInvalido", "corte", "Es obligatorio (Quincena o FinDeMes).");

    public static readonly Error SinColaboradores =
        Error.Validacion("Exportacion.SinColaboradores", "liquidacionIds", "Selecciona al menos un colaborador.");

    public static readonly Error SeleccionInvalida =
        Error.Validacion("Exportacion.SeleccionInvalida", "liquidacionIds", "Algún colaborador no tiene valor para este corte. Vuelve a cargar la lista.");

    public static Error NoEncontrada(Guid id) =>
        Error.NoEncontrado("Exportacion.NoEncontrada", $"No existe una exportación con id '{id}'.");
}

// ===== Servicio =====

public class ExportacionesPagoService(RpsDbContext db, CuentaBancariaProtector protector)
{
    // Lo que se va a incluir antes de generar: valores, cuentas y advertencias (RN 53).
    public async Task<Resultado<PreparacionExportacionResponse>> PrepararAsync(string? periodoTexto, TipoCorte? corte, CancellationToken ct)
    {
        if (!PeriodoTexto.TryParse(periodoTexto, out var periodo))
            return ExportacionPagoErrores.PeriodoInvalido;
        if (corte is null)
            return ExportacionPagoErrores.CorteInvalido;

        var liquidaciones = await db.Liquidaciones.AsNoTracking().Where(l => l.Periodo == periodo).ToListAsync(ct);
        var filas = await FilasDelCorteAsync(periodo, corte.Value, liquidaciones.Select(l => l.Id).ToList(), ct);
        var ultimas = await UltimaVersionPorColaboradorAsync(periodo, corte.Value, ct);
        var conDocumento = (await db.DocumentosMensuales.AsNoTracking()
                .Where(d => d.Periodo == periodo).Select(d => d.ColaboradorId).ToListAsync(ct)).ToHashSet();

        var candidatos = filas.Select(f =>
        {
            var liquidacion = liquidaciones.First(l => l.Id == f.LiquidacionId);
            int? version = ultimas.TryGetValue(f.ColaboradorId, out var v) ? v : null;
            var advertencias = new List<string>();
            if (version is not null) advertencias.Add($"Ya salió en la versión {version}. Si lo incluyes, se repite en el archivo nuevo.");
            if (!liquidacion.Revisado) advertencias.Add("No está marcada como \"Revisado para envío\".");
            if (!conDocumento.Contains(f.ColaboradorId)) advertencias.Add("Falta el documento tributario. No impide exportar (RN 57).");
            if (f.Cuenta is null) advertencias.Add("No tiene cuenta bancaria registrada.");
            return new CandidatoExportacionResponse(
                f.LiquidacionId, f.ColaboradorId, f.TmrColaboradorId, $"ID TMR {f.TmrColaboradorId}", f.Valor, f.Banco, f.CuentaEnmascarada,
                liquidacion.Revisado, conDocumento.Contains(f.ColaboradorId), false, version, advertencias);
        }).OrderBy(c => c.TmrColaboradorId).ToList();

        return new PreparacionExportacionResponse(
            PeriodoTexto.Formato(periodo), corte.Value, liquidaciones.Count > 0, candidatos,
            await UltimaVersionAsync(periodo, corte.Value, ct) + 1);
    }

    // Genera una versión nueva con los colaboradores elegidos y guarda el archivo (RN 54, RN 55).
    public async Task<Resultado<ExportacionResponse>> GenerarAsync(GenerarExportacionRequest request, CancellationToken ct)
    {
        if (!PeriodoTexto.TryParse(request.Periodo, out var periodo))
            return ExportacionPagoErrores.PeriodoInvalido;
        if (request.Corte is null)
            return ExportacionPagoErrores.CorteInvalido;
        if (request.LiquidacionIds is null || request.LiquidacionIds.Count == 0)
            return ExportacionPagoErrores.SinColaboradores;

        var ids = request.LiquidacionIds.Distinct().ToList();
        var validas = await db.Liquidaciones.AsNoTracking()
            .Where(l => ids.Contains(l.Id) && l.Periodo == periodo).Select(l => l.Id).ToListAsync(ct);
        var filas = await FilasDelCorteAsync(periodo, request.Corte.Value, validas, ct);
        if (filas.Count != ids.Count)
            return ExportacionPagoErrores.SeleccionInvalida;

        var exportacion = new ExportacionPago
        {
            Periodo = periodo,
            Corte = request.Corte.Value,
            Version = await UltimaVersionAsync(periodo, request.Corte.Value, ct) + 1,
            Total = filas.Sum(f => f.Valor),
        };
        exportacion.NombreArchivo = $"RPS_{PeriodoTexto.Formato(periodo)}_{(exportacion.Corte == TipoCorte.Quincena ? "quincena" : "fin-de-mes")}_v{exportacion.Version}.csv";
        exportacion.Contenido = ConstruirArchivo(filas, exportacion.Total);

        db.ExportacionesPago.Add(exportacion);
        db.ExportacionesPagoDetalle.AddRange(filas.Select(f => new ExportacionPagoDetalle
        {
            ExportacionPagoId = exportacion.Id,
            LiquidacionId = f.LiquidacionId,
            ColaboradorId = f.ColaboradorId,
            TmrColaboradorId = f.TmrColaboradorId,
            Valor = f.Valor,
            Banco = f.Banco,
            CuentaEnmascarada = f.CuentaEnmascarada,
        }));
        await db.SaveChangesAsync(ct);

        return (await ListarAsync(PeriodoTexto.Formato(periodo), ct)).Valor!.First(e => e.Id == exportacion.Id);
    }

    // Versiones generadas, de la más reciente a la más antigua. Sin período: todas.
    public async Task<Resultado<List<ExportacionResponse>>> ListarAsync(string? periodoTexto, CancellationToken ct)
    {
        var query = db.ExportacionesPago.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(periodoTexto))
        {
            if (!PeriodoTexto.TryParse(periodoTexto, out var periodo))
                return ExportacionPagoErrores.PeriodoInvalido;
            query = query.Where(e => e.Periodo == periodo);
        }

        var exportaciones = await query.OrderByDescending(e => e.GeneradaEn).ToListAsync(ct);
        var ids = exportaciones.Select(e => e.Id).ToList();
        var detalles = await db.ExportacionesPagoDetalle.AsNoTracking().Where(d => ids.Contains(d.ExportacionPagoId)).ToListAsync(ct);

        // Valor actual de cada corte, para marcar versiones desactualizadas (control propuesto, sección 12).
        var liquidacionIds = detalles.Select(d => d.LiquidacionId).Distinct().ToList();
        var cortesActuales = await db.Cortes.AsNoTracking()
            .Where(c => liquidacionIds.Contains(c.LiquidacionId))
            .Select(c => new { c.LiquidacionId, c.Tipo, c.ValorPreparado })
            .ToListAsync(ct);

        var ultimas = exportaciones.GroupBy(e => (e.Periodo, e.Corte)).ToDictionary(g => g.Key, g => g.Max(e => e.Version));

        return exportaciones.Select(e =>
        {
            var filas = detalles.Where(d => d.ExportacionPagoId == e.Id).ToList();
            var desactualizada = filas.Any(d =>
                cortesActuales.FirstOrDefault(c => c.LiquidacionId == d.LiquidacionId && c.Tipo == e.Corte)?.ValorPreparado != d.Valor);
            return new ExportacionResponse(
                e.Id, PeriodoTexto.Formato(e.Periodo), e.Corte, e.Version, e.GeneradaEn, e.Usuario,
                filas.Select(d => new FilaExportadaResponse(d.ColaboradorId, d.TmrColaboradorId, $"ID TMR {d.TmrColaboradorId}", d.Valor, d.Banco, d.CuentaEnmascarada)).ToList(),
                e.Total, e.Version == ultimas[(e.Periodo, e.Corte)], desactualizada, e.NombreArchivo);
        }).ToList();
    }

    // El mismo archivo que se generó; no crea otra versión.
    public async Task<Resultado<(byte[] Contenido, string NombreArchivo)>> ArchivoAsync(Guid id, CancellationToken ct)
    {
        var e = await db.ExportacionesPago.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null)
            return ExportacionPagoErrores.NoEncontrada(id);
        return (e.Contenido, e.NombreArchivo);
    }

    // ===== Internos =====

    private record FilaCorte(
        Guid LiquidacionId, Guid ColaboradorId, string TmrColaboradorId, decimal Valor,
        string? Banco, string? TipoCuenta, string? Cuenta, string? CuentaEnmascarada);

    // Colaboradores con valor en ese corte (ValorPreparado > 0), con su cuenta activa.
    private async Task<List<FilaCorte>> FilasDelCorteAsync(DateOnly periodo, TipoCorte corte, List<Guid> liquidacionIds, CancellationToken ct)
    {
        var filas = await db.Cortes.AsNoTracking()
            .Where(c => liquidacionIds.Contains(c.LiquidacionId) && c.Tipo == corte && c.ValorPreparado > 0)
            .Join(db.Liquidaciones.AsNoTracking(), c => c.LiquidacionId, l => l.Id, (c, l) => new { c.LiquidacionId, l.ColaboradorId, c.ValorPreparado })
            .Join(db.ColaboradoresRps.AsNoTracking(), x => x.ColaboradorId, col => col.Id, (x, col) => new { x.LiquidacionId, x.ColaboradorId, col.TmrColaboradorId, x.ValorPreparado })
            .ToListAsync(ct);

        var colaboradorIds = filas.Select(f => f.ColaboradorId).ToList();
        var cuentas = await db.CuentasBancarias.AsNoTracking()
            .Where(c => colaboradorIds.Contains(c.ColaboradorId) && c.Activa).ToListAsync(ct);

        return filas.Select(f =>
        {
            var cuenta = cuentas.FirstOrDefault(c => c.ColaboradorId == f.ColaboradorId);
            return new FilaCorte(
                f.LiquidacionId, f.ColaboradorId, f.TmrColaboradorId, f.ValorPreparado,
                cuenta?.Banco, cuenta?.TipoCuenta,
                cuenta is null ? null : protector.Descifrar(cuenta.NumeroCuentaCifrado),
                cuenta is null ? null : protector.Enmascarar(cuenta.NumeroCuentaCifrado));
        }).ToList();
    }

    private async Task<int> UltimaVersionAsync(DateOnly periodo, TipoCorte corte, CancellationToken ct) =>
        await db.ExportacionesPago.Where(e => e.Periodo == periodo && e.Corte == corte).MaxAsync(e => (int?)e.Version, ct) ?? 0;

    private async Task<Dictionary<Guid, int>> UltimaVersionPorColaboradorAsync(DateOnly periodo, TipoCorte corte, CancellationToken ct) =>
        (await db.ExportacionesPagoDetalle.AsNoTracking()
            .Join(db.ExportacionesPago.AsNoTracking().Where(e => e.Periodo == periodo && e.Corte == corte),
                d => d.ExportacionPagoId, e => e.Id, (d, e) => new { d.ColaboradorId, e.Version })
            .ToListAsync(ct))
        .GroupBy(x => x.ColaboradorId)
        .ToDictionary(g => g.Key, g => g.Max(x => x.Version));

    // CSV de ejemplo hasta tener la plantilla del banco (PA 02). Separador ";" y coma decimal para
    // que Excel en español lo abra en columnas; BOM para las tildes.
    private static byte[] ConstruirArchivo(List<FilaCorte> filas, decimal total)
    {
        var es = CultureInfo.GetCultureInfo("es-EC");
        var sb = new StringBuilder();
        sb.AppendLine("ID TMR;Banco;Tipo de cuenta;Número de cuenta;Valor");
        foreach (var f in filas.OrderBy(f => f.TmrColaboradorId))
            sb.AppendLine(string.Join(';', f.TmrColaboradorId, f.Banco ?? "", f.TipoCuenta ?? "", f.Cuenta ?? "", f.Valor.ToString("0.00", es)));
        sb.AppendLine($"Total;;;;{total.ToString("0.00", es)}");
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(sb.ToString())];
    }
}
