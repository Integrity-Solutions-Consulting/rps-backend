using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Colaboradores.Application;
using rps_backend.Features.Contratos.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Contratos.Application;

// Reglas de negocio de Contratos. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
public class ContratosService(RpsDbContext db)
{
    public async Task<Resultado<List<ContratoResponse>>> ListarAsync(Guid colaboradorId, bool? vigente, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return ContratoErrores.ColaboradorNoEncontrado(colaboradorId);

        var hoy = Hoy();

        var contratos = await db.Contratos.AsNoTracking()
            .Where(c => c.ColaboradorId == colaboradorId)
            .OrderByDescending(c => c.FechaInicio)
            .ToListAsync(ct);

        var respuestas = contratos.Select(c => AResponse(c, hoy)).ToList();

        if (vigente is not null)
            respuestas = respuestas.Where(c => c.Vigente == vigente).ToList();

        return respuestas;
    }

    public async Task<Resultado<ContratoResponse>> ObtenerAsync(Guid colaboradorId, Guid contratoId, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return ContratoErrores.ColaboradorNoEncontrado(colaboradorId);

        var contrato = await db.Contratos.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contratoId && c.ColaboradorId == colaboradorId, ct);
        if (contrato is null)
            return ContratoErrores.NoEncontrado(contratoId);

        return AResponse(contrato, Hoy());
    }

    public async Task<Resultado<ContratoResponse>> CrearAsync(Guid colaboradorId, CrearContratoRequest request, CancellationToken ct)
    {
        var validacion = Validar(request.TipoServicio, request.Tarifa, request.FechaInicio, request.FechaFin);
        if (validacion is not null)
            return validacion;

        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return ContratoErrores.ColaboradorNoEncontrado(colaboradorId);

        if (!colaborador.Activo)
            return ContratoErrores.ColaboradorDesactivado;

        var contrato = new Contrato
        {
            ColaboradorId = colaboradorId,
            TipoServicio = request.TipoServicio!.Trim(),
            Tarifa = request.Tarifa!.Value,
            FechaInicio = request.FechaInicio!.Value,
            FechaFin = request.FechaFin
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.Contratos.Add(contrato);
        await db.SaveChangesAsync(ct);

        await ColaboradorEstado.RecalcularAsync(colaborador, db, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return AResponse(contrato, Hoy());
    }

    public async Task<Resultado<ContratoResponse>> EditarAsync(
        Guid colaboradorId, Guid contratoId, EditarContratoRequest request, CancellationToken ct)
    {
        var validacion = Validar(request.TipoServicio, request.Tarifa, request.FechaInicio, request.FechaFin);
        if (validacion is not null)
            return validacion;

        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return ContratoErrores.ColaboradorNoEncontrado(colaboradorId);

        if (!colaborador.Activo)
            return ContratoErrores.ColaboradorDesactivado;

        var contrato = await db.Contratos.FirstOrDefaultAsync(c => c.Id == contratoId && c.ColaboradorId == colaboradorId, ct);
        if (contrato is null)
            return ContratoErrores.NoEncontrado(contratoId);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        contrato.TipoServicio = request.TipoServicio!.Trim();
        contrato.Tarifa = request.Tarifa!.Value;
        contrato.FechaInicio = request.FechaInicio!.Value;
        contrato.FechaFin = request.FechaFin;
        contrato.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await ColaboradorEstado.RecalcularAsync(colaborador, db, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return AResponse(contrato, Hoy());
    }

    // Solo se pueden eliminar contratos que aún no han empezado: uno ya iniciado se finaliza
    // editando su fecha de fin, porque pudo haberse usado ya para calcular honorarios.
    public async Task<Resultado<bool>> EliminarAsync(Guid colaboradorId, Guid contratoId, CancellationToken ct)
    {
        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return ContratoErrores.ColaboradorNoEncontrado(colaboradorId);

        if (!colaborador.Activo)
            return ContratoErrores.ColaboradorDesactivado;

        var contrato = await db.Contratos.FirstOrDefaultAsync(c => c.Id == contratoId && c.ColaboradorId == colaboradorId, ct);
        if (contrato is null)
            return ContratoErrores.NoEncontrado(contratoId);

        if (contrato.FechaInicio <= Hoy())
            return ContratoErrores.NoEliminable;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.Contratos.Remove(contrato);
        await db.SaveChangesAsync(ct);

        await ColaboradorEstado.RecalcularAsync(colaborador, db, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return true;
    }

    private static Error? Validar(string? tipoServicio, decimal? tarifa, DateOnly? fechaInicio, DateOnly? fechaFin)
    {
        var tipo = tipoServicio?.Trim();
        if (string.IsNullOrEmpty(tipo) || tipo.Length > 100)
            return ContratoErrores.TipoServicioInvalido;

        if (tarifa is null || tarifa <= 0)
            return ContratoErrores.TarifaInvalida;

        if (fechaInicio is null)
            return ContratoErrores.FechaInicioInvalida;

        if (fechaFin is not null && fechaFin < fechaInicio)
            return ContratoErrores.FechaFinInvalida;

        return null;
    }

    private static ContratoResponse AResponse(Contrato contrato, DateOnly hoy) => new(
        contrato.Id,
        contrato.ColaboradorId,
        contrato.TipoServicio,
        contrato.Tarifa,
        contrato.FechaInicio,
        contrato.FechaFin,
        Vigente: contrato.FechaInicio <= hoy && (contrato.FechaFin is null || contrato.FechaFin >= hoy));

    private static DateOnly Hoy() => DateOnly.FromDateTime(DateTime.UtcNow);
}
