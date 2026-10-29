using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Documentos.Domain;
using rps_backend.Features.Liquidaciones.Application;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Documentos.Application;

// Reglas de negocio de Documentos. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
public class DocumentosService(RpsDbContext db, IWebHostEnvironment env, LiquidacionesService liquidacionesService)
{
    private const long TamanoMaximoBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "text/plain",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private string CarpetaSubida
    {
        get
        {
            var carpeta = Path.Combine(env.ContentRootPath, "uploads");
            Directory.CreateDirectory(carpeta);
            return carpeta;
        }
    }

    public async Task<Resultado<DocumentoResponse>> SubirAsync(
        IFormFile archivo, Guid? colaboradorId, Guid? contratoId, Guid? liquidacionId, CancellationToken ct)
    {
        var referencias = new[] { colaboradorId, contratoId, liquidacionId }.Count(r => r is not null);
        if (referencias != 1)
            return DocumentoErrores.ReferenciaInvalida;

        if (archivo.Length == 0)
            return DocumentoErrores.ArchivoVacio;

        if (archivo.Length > TamanoMaximoBytes)
            return DocumentoErrores.ArchivoDemasiadoGrande;

        if (!TiposPermitidos.Contains(archivo.ContentType))
            return DocumentoErrores.TipoArchivoNoPermitido;

        if (colaboradorId is not null && !await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return DocumentoErrores.ColaboradorNoEncontrado(colaboradorId.Value);

        if (contratoId is not null && !await db.Contratos.AnyAsync(c => c.Id == contratoId, ct))
            return DocumentoErrores.ContratoNoEncontrado(contratoId.Value);

        if (liquidacionId is not null && !await db.Liquidaciones.AnyAsync(l => l.Id == liquidacionId, ct))
            return DocumentoErrores.LiquidacionNoEncontrado(liquidacionId.Value);

        var documento = new Documento
        {
            ColaboradorId = colaboradorId,
            ContratoId = contratoId,
            LiquidacionId = liquidacionId,
            NombreOriginal = archivo.FileName,
            ContentType = archivo.ContentType,
            TamanoBytes = archivo.Length,
            Origen = OrigenDocumento.Subido
        };

        var rutaFisica = Path.Combine(CarpetaSubida, documento.Id + Path.GetExtension(archivo.FileName));
        await using (var destino = File.Create(rutaFisica))
            await archivo.CopyToAsync(destino, ct);

        documento.RutaArchivo = Path.Combine("uploads", Path.GetFileName(rutaFisica));

        db.Documentos.Add(documento);
        await db.SaveChangesAsync(ct);

        return AResponse(documento);
    }

    public async Task<List<DocumentoResponse>> ListarAsync(
        Guid? colaboradorId, Guid? contratoId, Guid? liquidacionId, CancellationToken ct)
    {
        var query = db.Documentos.AsNoTracking();

        if (colaboradorId is not null)
            query = query.Where(d => d.ColaboradorId == colaboradorId);

        if (contratoId is not null)
            query = query.Where(d => d.ContratoId == contratoId);

        if (liquidacionId is not null)
            query = query.Where(d => d.LiquidacionId == liquidacionId);

        return await query
            .OrderByDescending(d => d.CreadoEn)
            .Select(d => new DocumentoResponse(
                d.Id, d.ColaboradorId, d.ContratoId, d.LiquidacionId, d.NombreOriginal, d.ContentType, d.TamanoBytes, d.Origen, d.CreadoEn))
            .ToListAsync(ct);
    }

    public async Task<Resultado<DocumentoResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var documento = await db.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (documento is null)
            return DocumentoErrores.NoEncontrado(id);

        return AResponse(documento);
    }

    public async Task<Resultado<(string RutaFisica, string ContentType, string NombreOriginal)>> ObtenerArchivoAsync(
        Guid id, CancellationToken ct)
    {
        var documento = await db.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (documento is null)
            return DocumentoErrores.NoEncontrado(id);

        var rutaFisica = Path.Combine(env.ContentRootPath, documento.RutaArchivo);
        return (rutaFisica, documento.ContentType, documento.NombreOriginal);
    }

    public async Task<Resultado<bool>> EliminarAsync(Guid id, CancellationToken ct)
    {
        var documento = await db.Documentos.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (documento is null)
            return DocumentoErrores.NoEncontrado(id);

        var rutaFisica = Path.Combine(env.ContentRootPath, documento.RutaArchivo);
        if (File.Exists(rutaFisica))
            File.Delete(rutaFisica);

        db.Documentos.Remove(documento);
        await db.SaveChangesAsync(ct);

        return true;
    }

    // Genera un comprobante .txt a partir del detalle de una Liquidación; sin librería de PDF por ahora.
    public async Task<Resultado<DocumentoResponse>> GenerarComprobanteLiquidacionAsync(Guid liquidacionId, CancellationToken ct)
    {
        var detalleResultado = await liquidacionesService.ObtenerAsync(liquidacionId, ct);
        if (!detalleResultado.EsExito)
            return detalleResultado.Error!;

        var detalle = detalleResultado.Valor!;

        var contenido = new System.Text.StringBuilder()
            .AppendLine("COMPROBANTE DE PAGO")
            .AppendLine($"Colaborador: {detalle.ColaboradorId}")
            .AppendLine($"Periodo: {detalle.Periodo:yyyy-MM}")
            .AppendLine($"Estado: {detalle.Estado}")
            .AppendLine($"Monto total: {detalle.MontoTotal:F2}")
            .AppendLine("----------------------")
            .AppendJoin(Environment.NewLine, detalle.Honorarios.Select(h =>
                $"Honorario {h.NumeroPago} (periodo {h.Periodo:yyyy-MM}): {h.Monto:F2}" +
                (h.Descuento > 0 ? $" (descuento anticipos: {h.Descuento:F2})" : string.Empty)))
            .AppendLine()
            .ToString();

        var documento = new Documento
        {
            LiquidacionId = liquidacionId,
            NombreOriginal = $"comprobante-liquidacion-{liquidacionId}.txt",
            ContentType = "text/plain",
            Origen = OrigenDocumento.Generado
        };

        var rutaFisica = Path.Combine(CarpetaSubida, documento.Id + ".txt");
        var bytes = System.Text.Encoding.UTF8.GetBytes(contenido);
        await File.WriteAllBytesAsync(rutaFisica, bytes, ct);

        documento.RutaArchivo = Path.Combine("uploads", Path.GetFileName(rutaFisica));
        documento.TamanoBytes = bytes.Length;

        db.Documentos.Add(documento);
        await db.SaveChangesAsync(ct);

        return AResponse(documento);
    }

    private static DocumentoResponse AResponse(Documento d) => new(
        d.Id, d.ColaboradorId, d.ContratoId, d.LiquidacionId, d.NombreOriginal, d.ContentType, d.TamanoBytes, d.Origen, d.CreadoEn);
}
