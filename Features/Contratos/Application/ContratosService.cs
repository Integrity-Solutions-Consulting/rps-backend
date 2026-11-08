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
        var validacion = Validar(request.FechaInicio, request.FechaFin, request.Referencia, request.Observaciones,
            out var referencia, out var observaciones);
        if (validacion is not null)
            return validacion;

        var proyectoIds = (request.ProyectoIds ?? []).Distinct().ToList();
        if (proyectoIds.Count != (request.ProyectoIds?.Count ?? 0))
            return ContratoErrores.ProyectosDuplicados;

        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return ContratoErrores.ColaboradorNoEncontrado(colaboradorId);

        if (!colaborador.Activo)
            return ContratoErrores.ColaboradorDesactivado;

        // Un contrato nuevo solo tiene sentido si la persona sigue activa en TMR (no solo activa
        // en RPS, que es un interruptor independiente). Si todavía no sincronizó, no se asume
        // ninguna de las dos cosas (RC 05): se avisa que falta confirmar el dato.
        if (colaborador.ActivoEnTmr is not true)
            return ContratoErrores.NoActivoEnTmr;

        List<Proyectos.Domain.Proyecto> proyectos = [];
        if (proyectoIds.Count > 0)
        {
            proyectos = await db.Proyectos.Where(p => proyectoIds.Contains(p.Id)).ToListAsync(ct);

            var faltante = proyectoIds.FirstOrDefault(id => !proyectos.Any(p => p.Id == id));
            if (faltante != default)
                return ContratoErrores.ProyectoNoEncontrado(faltante);

            var inactivo = proyectos.FirstOrDefault(p => !p.Activo);
            if (inactivo is not null)
                return ContratoErrores.ProyectoInactivo;

            // Evita doble reconocimiento de importes: no se permite otro contrato de este
            // colaborador que ya cubra el mismo proyecto y se superponga en fechas.
            var superpuesto = await ProyectoSuperpuestoAsync(colaboradorId, proyectoIds, request.FechaInicio!.Value, request.FechaFin!.Value, ct);
            if (superpuesto is not null)
                return ContratoErrores.Superpuesto(superpuesto.Value);
        }

        var contrato = new Contrato
        {
            ColaboradorId = colaboradorId,
            FechaInicio = request.FechaInicio!.Value,
            FechaFin = request.FechaFin!.Value,
            Referencia = referencia,
            Observaciones = observaciones
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.Contratos.Add(contrato);
        db.ContratosProyectos.AddRange(proyectoIds.Select(id => new ContratoProyecto { ContratoId = contrato.Id, ProyectoId = id }));
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

        // FechaFin no se toca aquí a propósito (RN 12): la fecha original no se edita ni se
        // reemplaza. Para extender la vigencia del contrato se usa RegistrarAdendaAsync. La lista
        // de proyectos tampoco se edita: para cubrir uno nuevo se registra otro contrato.
        contrato.Referencia = string.IsNullOrEmpty(referencia) ? null : referencia;
        contrato.Observaciones = string.IsNullOrEmpty(observaciones) ? null : observaciones;
        contrato.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return (await ARespuestasAsync([contrato], ct))[0];
    }

    // Solo se pueden eliminar contratos que aún no han empezado: uno ya iniciado se finaliza
    // con una adenda, porque pudo haberse usado ya para calcular honorarios.
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

        var proyectosDelContrato = await db.ContratosProyectos.Where(cp => cp.ContratoId == contratoId).ToListAsync(ct);
        db.ContratosProyectos.RemoveRange(proyectosDelContrato);
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

    // Busca, entre los proyectos pedidos, alguno que ya esté cubierto por otro contrato vigente
    // de este colaborador en un rango que se superpone con [fechaInicio, fechaFin].
    private async Task<Guid?> ProyectoSuperpuestoAsync(
        Guid colaboradorId, List<Guid> proyectoIds, DateOnly fechaInicio, DateOnly fechaFin, CancellationToken ct)
    {
        var candidatos = await db.ContratosProyectos.AsNoTracking()
            .Where(cp => proyectoIds.Contains(cp.ProyectoId))
            .Join(db.Contratos.AsNoTracking().Where(c => c.ColaboradorId == colaboradorId), cp => cp.ContratoId, c => c.Id,
                (cp, c) => new { cp.ProyectoId, Contrato = c })
            .ToListAsync(ct);

        if (candidatos.Count == 0)
            return null;

        var contratoIds = candidatos.Select(x => x.Contrato.Id).Distinct().ToList();
        var maxAdendaPorContrato = (await db.Adendas.AsNoTracking()
                .Where(a => contratoIds.Contains(a.ContratoId))
                .ToListAsync(ct))
            .GroupBy(a => a.ContratoId)
            .ToDictionary(g => g.Key, g => g.Max(a => a.NuevaFechaFin));

        foreach (var x in candidatos)
        {
            var maxAdenda = maxAdendaPorContrato.GetValueOrDefault(x.Contrato.Id);
            var finVigente = maxAdenda > x.Contrato.FechaFin ? maxAdenda : x.Contrato.FechaFin;
            if (fechaInicio <= finVigente && fechaFin >= x.Contrato.FechaInicio)
                return x.ProyectoId;
        }

        return null;
    }

    private async Task<List<ContratoResponse>> ARespuestasAsync(List<Contrato> contratos, CancellationToken ct)
    {
        var hoy = Hoy();
        var contratoIds = contratos.Select(c => c.Id).ToList();

        var proyectosPorContrato = (await db.ContratosProyectos.AsNoTracking()
                .Where(cp => contratoIds.Contains(cp.ContratoId))
                .Join(db.Proyectos.AsNoTracking(), cp => cp.ProyectoId, p => p.Id,
                    (cp, p) => new { cp.ContratoId, ProyectoDeContratoResponse = new ProyectoDeContratoResponse(p.Id, p.Nombre) })
                .ToListAsync(ct))
            .GroupBy(x => x.ContratoId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ProyectoDeContratoResponse).ToList());

        var maxAdendaPorContrato = (await db.Adendas.AsNoTracking()
                .Where(a => contratoIds.Contains(a.ContratoId))
                .GroupBy(a => a.ContratoId)
                .Select(g => new { ContratoId = g.Key, MaxNuevaFechaFin = g.Max(a => a.NuevaFechaFin) })
                .ToListAsync(ct))
            .ToDictionary(x => x.ContratoId, x => x.MaxNuevaFechaFin);

        return contratos.Select(c =>
        {
            var fechaFinVigente = maxAdendaPorContrato.TryGetValue(c.Id, out var maxAdenda) && maxAdenda > c.FechaFin
                ? maxAdenda
                : c.FechaFin;

            return new ContratoResponse(
                c.Id, c.ColaboradorId, proyectosPorContrato.GetValueOrDefault(c.Id, []),
                c.FechaInicio, c.FechaFin, fechaFinVigente, c.Referencia, c.Observaciones,
                Vigente: c.FechaInicio <= hoy && fechaFinVigente >= hoy);
        }).ToList();
    }

    private static Error? Validar(
        DateOnly? fechaInicio, DateOnly? fechaFin, string? referenciaInput, string? observacionesInput,
        out string? referencia, out string? observaciones)
    {
        referencia = string.IsNullOrWhiteSpace(referenciaInput) ? null : referenciaInput.Trim();
        observaciones = string.IsNullOrWhiteSpace(observacionesInput) ? null : observacionesInput.Trim();

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
