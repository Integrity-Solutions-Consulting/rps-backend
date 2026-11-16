using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Honorarios.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// Reglas de negocio de Condiciones Económicas: el honorario mensual fijo de un contrato, que se
// paga por contrato (un solo importe, sin proyecto) o por proyecto (un importe por cada proyecto).
public class CondicionesEconomicasService(RpsDbContext db)
{
    public async Task<Resultado<List<CondicionEconomicaResponse>>> ListarAsync(Guid colaboradorId, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return CondicionEconomicaErrores.ColaboradorNoEncontrado(colaboradorId);

        var condiciones = await db.CondicionesEconomicas.AsNoTracking()
            .Where(c => c.ColaboradorId == colaboradorId)
            .OrderByDescending(c => c.FechaInicioVigencia)
            .ToListAsync(ct);

        var proyectoIds = condiciones.Where(c => c.ProyectoId is not null).Select(c => c.ProyectoId!.Value).Distinct().ToList();
        var nombres = await db.Proyectos.AsNoTracking()
            .Where(p => proyectoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);

        return condiciones.Select(c => AResponse(c, c.ProyectoId is { } id ? nombres.GetValueOrDefault(id) : null)).ToList();
    }

    // ProyectoId null = pago por contrato: un solo importe fijo para todo el contrato.
    public async Task<Resultado<CondicionEconomicaResponse>> CrearAsync(
        Guid colaboradorId, CrearCondicionEconomicaRequest request, CancellationToken ct)
    {
        if (request.ContratoId is null)
            return CondicionEconomicaErrores.ContratoRequerido;

        if (request.ImporteMensual is null || request.ImporteMensual <= 0)
            return CondicionEconomicaErrores.ImporteMensualInvalido;

        if (request.Modalidad is null)
            return CondicionEconomicaErrores.ModalidadInvalida;

        if (request.FechaInicioVigencia is not { } fechaInicioVigencia)
            return CondicionEconomicaErrores.FechaInicioVigenciaInvalida;

        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return CondicionEconomicaErrores.ColaboradorNoEncontrado(colaboradorId);

        if (!await db.Contratos.AnyAsync(c => c.Id == request.ContratoId && c.ColaboradorId == colaboradorId, ct))
            return CondicionEconomicaErrores.ContratoNoEncontrado(request.ContratoId.Value);

        if (request.ProyectoId is { } proyectoId)
        {
            if (!await db.Proyectos.AnyAsync(p => p.Id == proyectoId, ct))
                return CondicionEconomicaErrores.ProyectoNoEncontrado(proyectoId);

            // Pago por proyecto: solo para un proyecto que ese contrato incluya, o que va a incluir
            // por una adenda programada (sus proyectos se aplican el día en que rige).
            var enContrato = await db.ContratosProyectos.AnyAsync(cp => cp.ContratoId == request.ContratoId && cp.ProyectoId == proyectoId, ct)
                || await db.Adendas.AnyAsync(a => a.ContratoId == request.ContratoId && !a.ProyectosAplicados
                    && a.ProyectosNuevos != null && a.ProyectosNuevos.Contains(proyectoId), ct);
            if (!enContrato)
                return CondicionEconomicaErrores.ContratoNoEncontrado(request.ContratoId.Value);
        }

        // La adenda que registra este cambio: al corregirla se corrigen también sus honorarios.
        if (request.AdendaId is { } adendaId
            && !await db.Adendas.AnyAsync(a => a.Id == adendaId && a.ContratoId == request.ContratoId, ct))
            return CondicionEconomicaErrores.AdendaNoEncontrada(adendaId);

        var existente = request.ProyectoId is null
            ? await db.CondicionesEconomicas.FirstOrDefaultAsync(c =>
                c.ContratoId == request.ContratoId && c.ProyectoId == null && c.FechaInicioVigencia == fechaInicioVigencia, ct)
            : await db.CondicionesEconomicas.FirstOrDefaultAsync(c =>
                c.ColaboradorId == colaboradorId && c.ProyectoId == request.ProyectoId && c.FechaInicioVigencia == fechaInicioVigencia, ct);

        var observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim();

        if (existente is not null)
        {
            if (!request.Reemplazar)
                return CondicionEconomicaErrores.FechaInicioVigenciaDuplicada(fechaInicioVigencia);

            // Si ya se usó en un cálculo, se corrige igual: al volver a calcular el período,
            // HonorariosService detecta que el monto cambió, recalcula ese honorario y la
            // liquidación queda marcada como modificada.
            existente.ContratoId = request.ContratoId.Value;
            existente.ImporteMensual = request.ImporteMensual.Value;
            existente.Modalidad = request.Modalidad.Value;
            if (request.AdendaId is not null)
                existente.AdendaId = request.AdendaId;
            if (request.Observaciones is not null)
                existente.Observaciones = observaciones;
            await db.SaveChangesAsync(ct);

            return AResponse(existente, await NombreProyectoAsync(existente.ProyectoId, ct));
        }

        var condicion = new CondicionEconomica
        {
            ColaboradorId = colaboradorId,
            ProyectoId = request.ProyectoId,
            ContratoId = request.ContratoId.Value,
            ImporteMensual = request.ImporteMensual.Value,
            Modalidad = request.Modalidad.Value,
            FechaInicioVigencia = fechaInicioVigencia,
            Observaciones = observaciones,
            AdendaId = request.AdendaId
        };

        db.CondicionesEconomicas.Add(condicion);
        await db.SaveChangesAsync(ct);

        return AResponse(condicion, await NombreProyectoAsync(condicion.ProyectoId, ct));
    }

    private async Task<string?> NombreProyectoAsync(Guid? proyectoId, CancellationToken ct) =>
        proyectoId is null
            ? null
            : await db.Proyectos.AsNoTracking().Where(p => p.Id == proyectoId).Select(p => p.Nombre).FirstOrDefaultAsync(ct);

    private static CondicionEconomicaResponse AResponse(CondicionEconomica c, string? proyectoNombre) => new(
        c.Id, c.ColaboradorId, c.ProyectoId, proyectoNombre, c.ContratoId,
        c.ImporteMensual, c.Modalidad, c.FechaInicioVigencia, c.Observaciones, c.AdendaId);
}
