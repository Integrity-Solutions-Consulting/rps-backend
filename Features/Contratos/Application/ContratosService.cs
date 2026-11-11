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
        var validacion = Validar(request.FechaInicio, request.FechaFin, request.Indefinido, request.Referencia, request.Observaciones,
            out var fechaFin, out var referencia, out var observaciones);
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
            var superpuesto = await ProyectoSuperpuestoAsync(colaboradorId, proyectoIds, request.FechaInicio!.Value, fechaFin, ct);
            if (superpuesto is not null)
                return ContratoErrores.Superpuesto(superpuesto.Value);
        }

        var contrato = new Contrato
        {
            ColaboradorId = colaboradorId,
            FechaInicio = request.FechaInicio!.Value,
            FechaFin = fechaFin,
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

        if (request.FechaInicio is null)
            return ContratoErrores.FechaInicioInvalida;

        var proyectoIds = (request.ProyectoIds ?? []).Distinct().ToList();
        if (proyectoIds.Count != (request.ProyectoIds?.Count ?? 0))
            return ContratoErrores.ProyectosDuplicados;

        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return ContratoErrores.ColaboradorNoEncontrado(colaboradorId);

        if (!colaborador.Activo)
            return ContratoErrores.ColaboradorDesactivado;

        var contrato = await db.Contratos.FirstOrDefaultAsync(c => c.Id == contratoId && c.ColaboradorId == colaboradorId, ct);
        if (contrato is null)
            return ContratoErrores.NoEncontrado(contratoId);

        // FechaFin (y si es indefinido) no se tocan aquí a propósito (RN 12): esa decisión no se
        // edita ni se reemplaza. Para extender la vigencia del contrato se usa RegistrarAdendaAsync.
        if (contrato.FechaFin is not null && request.FechaInicio > contrato.FechaFin)
            return ContratoErrores.FechaInicioPosteriorAFin;

        // Editar corrige; no cambia el contrato. Proyectos: solo agregar, y solo si se paga por
        // contrato (agregar uno no cambia el pago). Quitar uno, o agregarlo a un contrato que se paga
        // por proyecto, cambia lo que se paga: eso es una adenda.
        var actualesIds = await db.ContratosProyectos.AsNoTracking()
            .Where(cp => cp.ContratoId == contratoId).Select(cp => cp.ProyectoId).ToListAsync(ct);
        var quitaProyectos = actualesIds.Any(id => !proyectoIds.Contains(id));
        var agregaProyectos = proyectoIds.Any(id => !actualesIds.Contains(id));
        if (quitaProyectos || (agregaProyectos && !await SePagaPorContratoAsync(contratoId, ct)))
            return ContratoErrores.CambioRequiereAdenda;

        if (agregaProyectos)
        {
            var error = await ValidarProyectosAsync(colaboradorId, contratoId, proyectoIds, request.FechaInicio.Value, contrato.FechaFin, ct);
            if (error is not null)
                return error;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await ReemplazarProyectosAsync(contratoId, proyectoIds, ct);

        contrato.FechaInicio = request.FechaInicio.Value;
        contrato.Referencia = string.IsNullOrEmpty(referencia) ? null : referencia;
        contrato.Observaciones = string.IsNullOrEmpty(observaciones) ? null : observaciones;
        contrato.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await ColaboradorEstado.RecalcularAsync(colaborador, db, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

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
            .Select(a => new AdendaResponse(a.Id, a.ContratoId, a.NuevaFechaFin, a.FechaRegistro, a.Observaciones, a.Cambios))
            .ToListAsync(ct);
    }

    // Una adenda registra un cambio del contrato que rige desde hoy hasta la nueva fecha de fin:
    // la fecha de fin (igual o posterior a la vigente; null en indefinidos), los proyectos (lista
    // completa nueva) y un resumen de lo que cambió. Los cambios de honorario los registra el
    // formulario como CondicionEconomica (RN 19: desde el mes siguiente).
    public async Task<Resultado<AdendaResponse>> RegistrarAdendaAsync(
        Guid colaboradorId, Guid contratoId, CrearAdendaRequest request, CancellationToken ct)
    {
        var observaciones = request.Observaciones?.Trim();
        if (observaciones?.Length > 500)
            return ContratoErrores.ObservacionesInvalidas;

        var cambios = request.Cambios?.Trim();
        if (cambios?.Length > 1000)
            return ContratoErrores.CambiosInvalidos;

        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return ContratoErrores.ColaboradorNoEncontrado(colaboradorId);

        var contrato = await db.Contratos.FirstOrDefaultAsync(c => c.Id == contratoId && c.ColaboradorId == colaboradorId, ct);
        if (contrato is null)
            return ContratoErrores.NoEncontrado(contratoId);

        DateOnly? nuevaFechaFin = null;
        if (contrato.FechaFin is null)
        {
            if (request.NuevaFechaFin is not null)
                return ContratoErrores.ContratoIndefinido;
        }
        else
        {
            var fechaFinVigente = await FechaFinVigenteAsync(contrato, ct);
            if (request.NuevaFechaFin is null || request.NuevaFechaFin < fechaFinVigente)
                return ContratoErrores.NuevaFechaFinInvalida;
            if (!FechaEnRango(request.NuevaFechaFin.Value))
                return ContratoErrores.FechaFueraDeRango("nuevaFechaFin");
            nuevaFechaFin = request.NuevaFechaFin;
        }

        List<Guid>? proyectoIds = null;
        if (request.ProyectoIds is not null)
        {
            proyectoIds = request.ProyectoIds.Distinct().ToList();
            if (proyectoIds.Count != request.ProyectoIds.Count)
                return ContratoErrores.ProyectosDuplicados;

            var error = await ValidarProyectosAsync(colaboradorId, contratoId, proyectoIds, contrato.FechaInicio, nuevaFechaFin ?? contrato.FechaFin, ct);
            if (error is not null)
                return error;
        }

        var adenda = new Adenda
        {
            ContratoId = contratoId,
            NuevaFechaFin = nuevaFechaFin,
            FechaRegistro = Hoy(),
            Cambios = string.IsNullOrEmpty(cambios) ? null : cambios,
            Observaciones = string.IsNullOrEmpty(observaciones) ? null : observaciones
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.Adendas.Add(adenda);
        if (proyectoIds is not null)
            await ReemplazarProyectosAsync(contratoId, proyectoIds, ct);
        contrato.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        // Puede revivir la vigencia del contrato si su FechaFin original ya había pasado.
        await ColaboradorEstado.RecalcularAsync(colaborador, db, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return new AdendaResponse(adenda.Id, adenda.ContratoId, adenda.NuevaFechaFin, adenda.FechaRegistro, adenda.Observaciones, adenda.Cambios);
    }

    // Permite corregir una adenda ya registrada (ej. se escribió mal la fecha): la nueva fecha debe
    // seguir siendo posterior a la que quedaría vigente sin contar esta misma adenda.
    public async Task<Resultado<AdendaResponse>> EditarAdendaAsync(
        Guid colaboradorId, Guid contratoId, Guid adendaId, CrearAdendaRequest request, CancellationToken ct)
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

        var adenda = await db.Adendas.FirstOrDefaultAsync(a => a.Id == adendaId && a.ContratoId == contratoId, ct);
        if (adenda is null)
            return ContratoErrores.AdendaNoEncontrada(adendaId);

        if (contrato.FechaFin is null)
        {
            if (request.NuevaFechaFin is not null)
                return ContratoErrores.ContratoIndefinido;
        }
        else
        {
            var fechaFinVigenteSinEsta = await FechaFinVigenteAsync(contrato, ct, excluirAdendaId: adendaId);
            if (request.NuevaFechaFin is null || request.NuevaFechaFin < fechaFinVigenteSinEsta)
                return ContratoErrores.NuevaFechaFinInvalida;
            if (!FechaEnRango(request.NuevaFechaFin.Value))
                return ContratoErrores.FechaFueraDeRango("nuevaFechaFin");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        adenda.NuevaFechaFin = request.NuevaFechaFin;
        adenda.Observaciones = string.IsNullOrEmpty(observaciones) ? null : observaciones;
        await db.SaveChangesAsync(ct);

        // Puede revivir o cerrar la vigencia del contrato según hacia dónde se movió la fecha.
        await ColaboradorEstado.RecalcularAsync(colaborador, db, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return new AdendaResponse(adenda.Id, adenda.ContratoId, adenda.NuevaFechaFin, adenda.FechaRegistro, adenda.Observaciones, adenda.Cambios);
    }

    // Proyectos que existen, están activos y no los cubre otro contrato del colaborador en fechas superpuestas.
    private async Task<Error?> ValidarProyectosAsync(
        Guid colaboradorId, Guid contratoId, List<Guid> proyectoIds, DateOnly fechaInicio, DateOnly? fechaFin, CancellationToken ct)
    {
        if (proyectoIds.Count == 0)
            return null;

        var proyectos = await db.Proyectos.Where(p => proyectoIds.Contains(p.Id)).ToListAsync(ct);

        var faltante = proyectoIds.FirstOrDefault(id => !proyectos.Any(p => p.Id == id));
        if (faltante != default)
            return ContratoErrores.ProyectoNoEncontrado(faltante);

        // Uno que ya estaba en el contrato puede seguir aunque se haya desactivado; uno nuevo, no.
        var actuales = await db.ContratosProyectos.AsNoTracking()
            .Where(cp => cp.ContratoId == contratoId).Select(cp => cp.ProyectoId).ToListAsync(ct);
        if (proyectos.Any(p => !p.Activo && !actuales.Contains(p.Id)))
            return ContratoErrores.ProyectoInactivo;

        var superpuesto = await ProyectoSuperpuestoAsync(colaboradorId, proyectoIds, fechaInicio, fechaFin, ct, excluirContratoId: contratoId);
        return superpuesto is null ? null : ContratoErrores.Superpuesto(superpuesto.Value);
    }

    // Deja en el contrato exactamente esos proyectos.
    private async Task ReemplazarProyectosAsync(Guid contratoId, List<Guid> proyectoIds, CancellationToken ct)
    {
        var actuales = await db.ContratosProyectos.Where(cp => cp.ContratoId == contratoId).ToListAsync(ct);
        db.ContratosProyectos.RemoveRange(actuales.Where(cp => !proyectoIds.Contains(cp.ProyectoId)));
        db.ContratosProyectos.AddRange(proyectoIds.Where(id => !actuales.Any(cp => cp.ProyectoId == id))
            .Select(id => new ContratoProyecto { ContratoId = contratoId, ProyectoId = id }));
    }

    // Cómo se paga el contrato según lo último registrado (incluido un cambio programado): true si
    // es un solo monto por contrato. Sin honorarios todavía se trata como por contrato (aún no hay
    // un pago por proyecto que cambiar).
    private async Task<bool> SePagaPorContratoAsync(Guid contratoId, CancellationToken ct)
    {
        var condiciones = await db.CondicionesEconomicas.AsNoTracking().Where(c => c.ContratoId == contratoId).ToListAsync(ct);
        if (condiciones.Count == 0)
            return true;

        var ultimoMes = condiciones.Max(c => c.MesInicioVigencia);
        return Honorarios.Application.CondicionesVigentes.Seleccionar(condiciones, ultimoMes).Any(c => c.PorContrato);
    }

    // Solo se llama con contratos que no son indefinidos (se valida antes en Registrar/EditarAdendaAsync).
    // excluirAdendaId: al editar una adenda, no se compara contra sí misma.
    private async Task<DateOnly> FechaFinVigenteAsync(Contrato contrato, CancellationToken ct, Guid? excluirAdendaId = null)
    {
        var fechaFin = contrato.FechaFin!.Value;
        var maxAdenda = await db.Adendas.AsNoTracking()
            .Where(a => a.ContratoId == contrato.Id && a.Id != excluirAdendaId && a.NuevaFechaFin != null)
            .Select(a => a.NuevaFechaFin)
            .OrderByDescending(f => f)
            .FirstOrDefaultAsync(ct);

        return maxAdenda is { } f && f > fechaFin ? f : fechaFin;
    }

    // Busca, entre los proyectos pedidos, alguno que ya esté cubierto por otro contrato vigente
    // de este colaborador en un rango que se superpone con [fechaInicio, fechaFin]. fechaFin null
    // (indefinido) se trata como "sin límite": siempre se superpone hacia adelante. excluirContratoId
    // se usa al editar: el contrato no se compara contra sí mismo.
    private async Task<Guid?> ProyectoSuperpuestoAsync(
        Guid colaboradorId, List<Guid> proyectoIds, DateOnly fechaInicio, DateOnly? fechaFin, CancellationToken ct,
        Guid? excluirContratoId = null)
    {
        var candidatos = await db.ContratosProyectos.AsNoTracking()
            .Where(cp => proyectoIds.Contains(cp.ProyectoId) && cp.ContratoId != excluirContratoId)
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
            DateOnly? finVigente = null;
            if (x.Contrato.FechaFin is not null)
            {
                var maxAdenda = maxAdendaPorContrato.GetValueOrDefault(x.Contrato.Id);
                finVigente = maxAdenda > x.Contrato.FechaFin ? maxAdenda : x.Contrato.FechaFin;
            }

            var seSuperponeInicio = finVigente is null || fechaInicio <= finVigente;
            var seSuperponeFin = fechaFin is null || fechaFin >= x.Contrato.FechaInicio;
            if (seSuperponeInicio && seSuperponeFin)
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
            DateOnly? fechaFinVigente = null;
            if (c.FechaFin is not null)
            {
                fechaFinVigente = maxAdendaPorContrato.TryGetValue(c.Id, out var maxAdenda) && maxAdenda > c.FechaFin
                    ? maxAdenda
                    : c.FechaFin;
            }

            var vigente = c.FechaInicio <= hoy && (fechaFinVigente is null || fechaFinVigente >= hoy);

            return new ContratoResponse(
                c.Id, c.ColaboradorId, proyectosPorContrato.GetValueOrDefault(c.Id, []),
                c.FechaInicio, c.FechaFin, fechaFinVigente, c.Referencia, c.Observaciones,
                Vigente: vigente);
        }).ToList();
    }

    private static Error? Validar(
        DateOnly? fechaInicio, DateOnly? fechaFin, bool indefinido, string? referenciaInput, string? observacionesInput,
        out DateOnly? fechaFinValidada, out string? referencia, out string? observaciones)
    {
        referencia = string.IsNullOrWhiteSpace(referenciaInput) ? null : referenciaInput.Trim();
        observaciones = string.IsNullOrWhiteSpace(observacionesInput) ? null : observacionesInput.Trim();
        fechaFinValidada = null;

        if (fechaInicio is null)
            return ContratoErrores.FechaInicioInvalida;

        if (!FechaEnRango(fechaInicio.Value))
            return ContratoErrores.FechaFueraDeRango("fechaInicio");

        // Indefinido gana sobre cualquier fechaFin que haya llegado en la solicitud.
        if (!indefinido)
        {
            if (fechaFin is null || fechaFin < fechaInicio)
                return ContratoErrores.FechaFinInvalida;
            if (!FechaEnRango(fechaFin.Value))
                return ContratoErrores.FechaFueraDeRango("fechaFin");
            fechaFinValidada = fechaFin;
        }

        if (referencia?.Length > 100)
            return ContratoErrores.ReferenciaInvalida;

        if (observaciones?.Length > 500)
            return ContratoErrores.ObservacionesInvalidas;

        return null;
    }

    private static DateOnly Hoy() => DateOnly.FromDateTime(DateTime.UtcNow);

    // Año desde 2000 y de 4 dígitos (DateOnly ya no admite más de 9999).
    private static bool FechaEnRango(DateOnly fecha) => fecha.Year >= 2000;
}
