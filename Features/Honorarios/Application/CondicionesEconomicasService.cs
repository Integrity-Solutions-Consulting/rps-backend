using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Honorarios.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// Reglas de negocio de Condiciones Económicas (honorario mensual fijo por colaborador+proyecto).
public class CondicionesEconomicasService(RpsDbContext db)
{
    public async Task<Resultado<List<CondicionEconomicaResponse>>> ListarAsync(Guid colaboradorId, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return CondicionEconomicaErrores.ColaboradorNoEncontrado(colaboradorId);

        return await db.CondicionesEconomicas.AsNoTracking()
            .Where(c => c.ColaboradorId == colaboradorId)
            .Join(db.Proyectos.AsNoTracking(), c => c.ProyectoId, p => p.Id, (c, p) => new CondicionEconomicaResponse(
                c.Id, c.ColaboradorId, c.ProyectoId, p.Nombre, c.ContratoId, c.ImporteMensual, c.Modalidad, c.MesInicioVigencia, c.Observaciones))
            .OrderByDescending(c => c.MesInicioVigencia)
            .ToListAsync(ct);
    }

    public async Task<Resultado<CondicionEconomicaResponse>> CrearAsync(
        Guid colaboradorId, CrearCondicionEconomicaRequest request, CancellationToken ct)
    {
        if (request.ProyectoId is null)
            return CondicionEconomicaErrores.ProyectoRequerido;

        if (request.ContratoId is null)
            return CondicionEconomicaErrores.ContratoRequerido;

        if (request.ImporteMensual is null || request.ImporteMensual <= 0)
            return CondicionEconomicaErrores.ImporteMensualInvalido;

        if (request.Modalidad is null)
            return CondicionEconomicaErrores.ModalidadInvalida;

        if (request.MesInicioVigencia is not { Day: 1 } mesInicioVigencia)
            return CondicionEconomicaErrores.MesInicioVigenciaInvalido;

        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return CondicionEconomicaErrores.ColaboradorNoEncontrado(colaboradorId);

        if (!await db.Proyectos.AnyAsync(p => p.Id == request.ProyectoId, ct))
            return CondicionEconomicaErrores.ProyectoNoEncontrado(request.ProyectoId.Value);

        var contratoValido = await db.Contratos.AnyAsync(c =>
            c.Id == request.ContratoId && c.ColaboradorId == colaboradorId && c.ProyectoId == request.ProyectoId, ct);
        if (!contratoValido)
            return CondicionEconomicaErrores.ContratoNoEncontrado(request.ContratoId.Value);

        if (await db.CondicionesEconomicas.AnyAsync(c =>
                c.ColaboradorId == colaboradorId && c.ProyectoId == request.ProyectoId && c.MesInicioVigencia == mesInicioVigencia, ct))
            return CondicionEconomicaErrores.MesInicioVigenciaDuplicado(mesInicioVigencia);

        var observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim();

        var condicion = new CondicionEconomica
        {
            ColaboradorId = colaboradorId,
            ProyectoId = request.ProyectoId.Value,
            ContratoId = request.ContratoId.Value,
            ImporteMensual = request.ImporteMensual.Value,
            Modalidad = request.Modalidad.Value,
            MesInicioVigencia = mesInicioVigencia,
            Observaciones = observaciones
        };

        db.CondicionesEconomicas.Add(condicion);
        await db.SaveChangesAsync(ct);

        var proyectoNombre = await db.Proyectos.AsNoTracking().Where(p => p.Id == condicion.ProyectoId).Select(p => p.Nombre).FirstAsync(ct);

        return new CondicionEconomicaResponse(
            condicion.Id, condicion.ColaboradorId, condicion.ProyectoId, proyectoNombre, condicion.ContratoId,
            condicion.ImporteMensual, condicion.Modalidad, condicion.MesInicioVigencia, condicion.Observaciones);
    }
}
