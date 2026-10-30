using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Documentos.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Documentos.Application;

// Reglas de negocio de Documentos (documento mensual). No conoce HTTP: devuelve Resultado y la
// capa Api decide el status code. Sin archivo adjunto: es un check manual (tipo/número/fecha) de
// si el colaborador emitió su factura/nota de venta ese mes; su ausencia solo advierte, no bloquea.
public class DocumentosMensualesService(RpsDbContext db)
{
    public async Task<Resultado<List<DocumentoMensualResponse>>> ListarAsync(Guid colaboradorId, DateOnly? periodo, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return DocumentoMensualErrores.ColaboradorNoEncontrado(colaboradorId);

        var query = db.DocumentosMensuales.AsNoTracking().Where(d => d.ColaboradorId == colaboradorId);

        if (periodo is not null)
            query = query.Where(d => d.Periodo == periodo);

        return await query
            .OrderByDescending(d => d.Periodo)
            .Select(d => new DocumentoMensualResponse(
                d.Id, d.ColaboradorId, d.Periodo, d.Tipo, d.Numero, d.FechaEmision, d.MarcadoEn))
            .ToListAsync(ct);
    }

    public async Task<Resultado<DocumentoMensualResponse>> RegistrarAsync(
        Guid colaboradorId, RegistrarDocumentoMensualRequest request, CancellationToken ct)
    {
        if (request.Periodo is not { Day: 1 } periodo)
            return DocumentoMensualErrores.PeriodoInvalido;

        if (request.Tipo is null)
            return DocumentoMensualErrores.TipoInvalido;

        var numero = request.Numero?.Trim();
        if (string.IsNullOrEmpty(numero) || numero.Length > 50)
            return DocumentoMensualErrores.NumeroInvalido;

        if (request.FechaEmision is null)
            return DocumentoMensualErrores.FechaEmisionInvalida;

        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return DocumentoMensualErrores.ColaboradorNoEncontrado(colaboradorId);

        if (await db.DocumentosMensuales.AnyAsync(d => d.ColaboradorId == colaboradorId && d.Periodo == periodo, ct))
            return DocumentoMensualErrores.YaRegistrado(periodo);

        var documento = new DocumentoMensual
        {
            ColaboradorId = colaboradorId,
            Periodo = periodo,
            Tipo = request.Tipo.Value,
            Numero = numero,
            FechaEmision = request.FechaEmision.Value
        };

        db.DocumentosMensuales.Add(documento);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return DocumentoMensualErrores.YaRegistrado(periodo);
        }

        return AResponse(documento);
    }

    public async Task<Resultado<DocumentoMensualResponse>> EditarAsync(
        Guid colaboradorId, Guid documentoMensualId, EditarDocumentoMensualRequest request, CancellationToken ct)
    {
        if (request.Tipo is null)
            return DocumentoMensualErrores.TipoInvalido;

        var numero = request.Numero?.Trim();
        if (string.IsNullOrEmpty(numero) || numero.Length > 50)
            return DocumentoMensualErrores.NumeroInvalido;

        if (request.FechaEmision is null)
            return DocumentoMensualErrores.FechaEmisionInvalida;

        var documento = await db.DocumentosMensuales
            .FirstOrDefaultAsync(d => d.Id == documentoMensualId && d.ColaboradorId == colaboradorId, ct);
        if (documento is null)
            return DocumentoMensualErrores.NoEncontrado(documentoMensualId);

        documento.Tipo = request.Tipo.Value;
        documento.Numero = numero;
        documento.FechaEmision = request.FechaEmision.Value;
        documento.MarcadoEn = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return AResponse(documento);
    }

    public async Task<Resultado<bool>> EliminarAsync(Guid colaboradorId, Guid documentoMensualId, CancellationToken ct)
    {
        var documento = await db.DocumentosMensuales
            .FirstOrDefaultAsync(d => d.Id == documentoMensualId && d.ColaboradorId == colaboradorId, ct);
        if (documento is null)
            return DocumentoMensualErrores.NoEncontrado(documentoMensualId);

        db.DocumentosMensuales.Remove(documento);
        await db.SaveChangesAsync(ct);

        return true;
    }

    private static DocumentoMensualResponse AResponse(DocumentoMensual d) => new(
        d.Id, d.ColaboradorId, d.Periodo, d.Tipo, d.Numero, d.FechaEmision, d.MarcadoEn);
}
