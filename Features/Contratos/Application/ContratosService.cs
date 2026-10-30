using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Colaboradores.Application;
using rps_backend.Features.Contratos.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Contratos.Application;

// Reglas de negocio de Contratos y sus Adendas. No conoce HTTP: devuelve Resultado y la capa Api
// decide el status code.
public class ContratosService(RpsDbContext db)
{
    public async Task<Resultado<List<ContratoResponse>>> ListarAsync(Guid colaboradorId, bool? vigente, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return ContratoErrores.ColaboradorNoEncontrado(colaboradorId);

        var contratos = await db.Contratos.AsNoTracking()
            .Where(c => c.ColaboradorId == colaboradorId)
            .OrderByDescending(c => c.FechaInicio)
            .ToListAsync(ct);

        var respuestas = await ARespuestasAsync(contratos, ct);

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

        return (await ARespuestasAsync([contrato], ct))[0];
    }

    public async Task<Resultado<ContratoResponse>> CrearAsync(Guid colaboradorId, CrearContratoRequest request, CancellationToken ct)
    {
        var validacion = Validar(request.ProyectoId, request.FechaInicio, request.FechaFin, request.Referencia, request.Observaciones,
            out var referencia, out var observaciones);
        if (validacion is not null)
            return validacion;

        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return ContratoErrores.ColaboradorNoEncontrado(colaboradorId);

        if (!colaborador.Activo)
            return ContratoErrores.ColaboradorDesactivado;

        var proyecto = await db.Proyectos.FirstOrDefaultAsync(p => p.Id == request.ProyectoId, ct);
        if (proyecto is null)
            return ContratoErrores.ProyectoNoEncontrado(request.ProyectoId!.Value);

        if (!proyecto.Activo)
            return ContratoErrores.ProyectoInactivo;

        // Evita doble reconocimiento de importes: no se permite otro contrato de la misma
        // relación colaborador-proyecto que se superponga en fechas.
        var seSuperpone = await db.Contratos.AnyAsync(c =>
            c.ColaboradorId == colaboradorId && c.ProyectoId == request.ProyectoId &&
            request.FechaInicio <= c.FechaFin && request.FechaFin >= c.FechaInicio, ct);
        if (seSuperpone)
            return ContratoErrores.Superpuesto(request.ProyectoId!.Value);

        var contrato = new Contrato
        {
            ColaboradorId = colaboradorId,
            ProyectoId = request.ProyectoId!.Value,
            FechaInicio = request.FechaInicio!.Value,
            FechaFin = request.FechaFin!.Value,
            Referencia = referencia,
            Observaciones = observaciones
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.Contratos.Add(contrato);
        await db.SaveChangesAsync(ct);

        await ColaboradorEstado.RecalcularAsync(colaborador, db, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return (await ARespuestasAsync([contrato], ct))[0];
    }

    public async Task<Resultado<ContratoResponse>> EditarAsync(
        Guid colaboradorId, Guid contratoId, EditarContratoRequest request, CancellationToken ct)
    {
        var referencia = request.Referencia?.Trim();
        if (referencia?.Length > 100)
            return ContratoErrores.ReferenciaInvalida;

        var observaciones = request.Observaciones?.Trim();
        if (observaciones?.Length > 500)
            return ContratoErrores.ObservacionesInvalidas;

        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return ContratoErrores.ColaboradorNoEncontrado(colaboradorId);

        if (!colaborador.Activo)
            return ContratoErrores.ColaboradorDesactivado;

        var contrato = await db.Contratos.FirstOrDefaultAsync(c => c.Id == contratoId && c.ColaboradorId == colaboradorId, ct);
        if (contrato is null)
            return ContratoErrores.NoEncontrado(contratoId);

        if (request.FechaFin is null || request.FechaFin < contrato.FechaInicio)
            return ContratoErrores.FechaFinInvalida;

        contrato.FechaFin = request.FechaFin.Value;
        contrato.Referencia = string.IsNullOrEmpty(referencia) ? null : referencia;
        contrato.Observaciones = string.IsNullOrEmpty(observaciones) ? null : observaciones;
        contrato.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return (await ARespuestasAsync([contrato], ct))[0];
    }

    // Solo se pueden eliminar contratos que aún no han empezado: uno ya iniciado se finaliza
    // con una adenda o editando su fecha de fin, porque pudo haberse usado ya para calcular honorarios.
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

    public async Task<Resultado<List<AdendaResponse>>> ListarAdendasAsync(Guid colaboradorId, Guid contratoId, CancellationToken ct)
    {
        var contrato = await db.Contratos.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contratoId && c.ColaboradorId == colaboradorId, ct);
        if (contrato is null)
            return ContratoErrores.NoEncontrado(contratoId);

        return await db.Adendas.AsNoTracking()
            .Where(a => a.ContratoId == contratoId)
            .OrderByDescending(a => a.FechaRegistro)
            .Select(a => new AdendaResponse(a.Id, a.ContratoId, a.NuevaFechaFin, a.FechaRegistro, a.Observaciones))
            .ToListAsync(ct);
    }

    public async Task<Resultado<AdendaResponse>> RegistrarAdendaAsync(
        Guid colaboradorId, Guid contratoId, CrearAdendaRequest request, CancellationToken ct)
    {
        var observaciones = request.Observaciones?.Trim();
        if (observaciones?.Length > 500)
            return ContratoErrores.ObservacionesInvalidas;

        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return ContratoErrores.ColaboradorNoEncontrado(colaboradorId);

        var contrato = await db.Contratos.FirstOrDefaultAsync(c => c.Id == contratoId && c.ColaboradorId == colaboradorId, ct);
        if (contrato is null)
            return ContratoErrores.NoEncontrado(contratoId);

        var fechaFinVigente = await FechaFinVigenteAsync(contrato, ct);

        if (request.NuevaFechaFin is null || request.NuevaFechaFin <= fechaFinVigente)
            return ContratoErrores.NuevaFechaFinInvalida;

        var adenda = new Adenda
        {
            ContratoId = contratoId,
            NuevaFechaFin = request.NuevaFechaFin.Value,
            FechaRegistro = Hoy(),
            Observaciones = string.IsNullOrEmpty(observaciones) ? null : observaciones
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.Adendas.Add(adenda);
        await db.SaveChangesAsync(ct);

        // Puede revivir la vigencia del contrato si su FechaFin original ya había pasado.
        await ColaboradorEstado.RecalcularAsync(colaborador, db, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return new AdendaResponse(adenda.Id, adenda.ContratoId, adenda.NuevaFechaFin, adenda.FechaRegistro, adenda.Observaciones);
    }

    private async Task<DateOnly> FechaFinVigenteAsync(Contrato contrato, CancellationToken ct)
    {
        var maxAdenda = await db.Adendas.AsNoTracking()
            .Where(a => a.ContratoId == contrato.Id)
            .Select(a => a.NuevaFechaFin)
            .OrderByDescending(f => f)
            .FirstOrDefaultAsync(ct);

        return maxAdenda == default ? contrato.FechaFin : (maxAdenda > contrato.FechaFin ? maxAdenda : contrato.FechaFin);
    }

    private async Task<List<ContratoResponse>> ARespuestasAsync(List<Contrato> contratos, CancellationToken ct)
    {
        var hoy = Hoy();
        var proyectoIds = contratos.Select(c => c.ProyectoId).Distinct().ToList();
        var proyectos = await db.Proyectos.AsNoTracking()
            .Where(p => proyectoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);

        var contratoIds = contratos.Select(c => c.Id).ToList();
        var maxAdendaPorContrato = await db.Adendas.AsNoTracking()
            .Where(a => contratoIds.Contains(a.ContratoId))
            .GroupBy(a => a.ContratoId)
            .Select(g => new { ContratoId = g.Key, MaxNuevaFechaFin = g.Max(a => a.NuevaFechaFin) })
            .ToDictionaryAsync(x => x.ContratoId, x => x.MaxNuevaFechaFin, ct);

        return contratos.Select(c =>
        {
            var fechaFinVigente = maxAdendaPorContrato.TryGetValue(c.Id, out var maxAdenda) && maxAdenda > c.FechaFin
                ? maxAdenda
                : c.FechaFin;

            return new ContratoResponse(
                c.Id, c.ColaboradorId, c.ProyectoId, proyectos.GetValueOrDefault(c.ProyectoId, "?"),
                c.FechaInicio, c.FechaFin, fechaFinVigente, c.Referencia, c.Observaciones,
                Vigente: c.FechaInicio <= hoy && fechaFinVigente >= hoy);
        }).ToList();
    }

    private static Error? Validar(
        Guid? proyectoId, DateOnly? fechaInicio, DateOnly? fechaFin, string? referenciaInput, string? observacionesInput,
        out string? referencia, out string? observaciones)
    {
        referencia = string.IsNullOrWhiteSpace(referenciaInput) ? null : referenciaInput.Trim();
        observaciones = string.IsNullOrWhiteSpace(observacionesInput) ? null : observacionesInput.Trim();

        if (proyectoId is null)
            return ContratoErrores.ProyectoRequerido;

        if (fechaInicio is null)
            return ContratoErrores.FechaInicioInvalida;

        if (fechaFin is null || fechaFin < fechaInicio)
            return ContratoErrores.FechaFinInvalida;

        if (referencia?.Length > 100)
            return ContratoErrores.ReferenciaInvalida;

        if (observaciones?.Length > 500)
            return ContratoErrores.ObservacionesInvalidas;

        return null;
    }

    private static DateOnly Hoy() => DateOnly.FromDateTime(DateTime.UtcNow);
}
