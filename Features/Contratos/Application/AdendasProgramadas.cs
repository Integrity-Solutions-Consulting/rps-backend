using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Colaboradores.Application;
using rps_backend.Features.Contratos.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Contratos.Application;

// Proyectos de una adenda que rige desde una fecha futura: no se aplican al registrarla sino el día
// en que empieza a regir. Lo ejecutan AdendasProgramadasJob (al arrancar y cada hora) y la lista de
// contratos, para que nunca se vea un cambio antes de tiempo ni se quede sin aplicar.
// (El pago por proyecto no depende de esto: sale de las fechas de asignación en TMR.)
public static class AdendasProgramadas
{
    // Aplica los proyectos de todas las adendas que ya empezaron a regir. Devuelve cuántas aplicó.
    public static async Task<int> AplicarVencidasAsync(RpsDbContext db, CancellationToken ct)
    {
        var hoy = HoraEcuador.Hoy;
        var pendientes = await db.Adendas
            .Where(a => a.ProyectosNuevos != null && !a.ProyectosAplicados && a.FechaVigencia <= hoy)
            .OrderBy(a => a.FechaVigencia).ThenBy(a => a.CreadoEn)
            .ToListAsync(ct);
        if (pendientes.Count == 0)
            return 0;

        foreach (var adenda in pendientes)
        {
            await AplicarProyectosAsync(db, adenda, ct);
            await db.SaveChangesAsync(ct);
        }

        var contratoIds = pendientes.Select(a => a.ContratoId).Distinct().ToList();
        var colaboradorIds = await db.Contratos.Where(c => contratoIds.Contains(c.Id)).Select(c => c.ColaboradorId).Distinct().ToListAsync(ct);
        foreach (var colaborador in await db.ColaboradoresRps.Where(c => colaboradorIds.Contains(c.Id)).ToListAsync(ct))
            await ColaboradorEstado.RecalcularAsync(colaborador, db, ct);
        await db.SaveChangesAsync(ct);

        return pendientes.Count;
    }

    // Deja en el contrato los proyectos de la adenda y guarda los que tenía, para poder deshacerlo.
    public static async Task AplicarProyectosAsync(RpsDbContext db, Adenda adenda, CancellationToken ct)
    {
        if (adenda.ProyectosNuevos is null || adenda.ProyectosAplicados)
            return;

        adenda.ProyectosAnteriores = await db.ContratosProyectos
            .Where(cp => cp.ContratoId == adenda.ContratoId).Select(cp => cp.ProyectoId).ToListAsync(ct);
        await ReemplazarAsync(db, adenda.ContratoId, adenda.ProyectosNuevos, ct);
        adenda.ProyectosAplicados = true;
    }

    // Vuelve a los proyectos de antes de la adenda (al corregirla y mover su fecha al futuro).
    public static async Task DeshacerProyectosAsync(RpsDbContext db, Adenda adenda, CancellationToken ct)
    {
        if (!adenda.ProyectosAplicados || adenda.ProyectosAnteriores is null)
            return;

        await ReemplazarAsync(db, adenda.ContratoId, adenda.ProyectosAnteriores, ct);
        adenda.ProyectosAplicados = false;
    }

    // Deja en el contrato exactamente esos proyectos.
    public static async Task ReemplazarAsync(RpsDbContext db, Guid contratoId, List<Guid> proyectoIds, CancellationToken ct)
    {
        var actuales = await db.ContratosProyectos.Where(cp => cp.ContratoId == contratoId).ToListAsync(ct);
        db.ContratosProyectos.RemoveRange(actuales.Where(cp => !proyectoIds.Contains(cp.ProyectoId)));
        db.ContratosProyectos.AddRange(proyectoIds.Where(id => !actuales.Any(cp => cp.ProyectoId == id))
            .Select(id => new ContratoProyecto { ContratoId = contratoId, ProyectoId = id }));
    }
}

// Aplica las adendas programadas al arrancar y después cada hora (el cambio de día en Ecuador cae
// dentro de esa hora).
public class AdendasProgramadasJob(IServiceScopeFactory scopes, ILogger<AdendasProgramadasJob> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var reloj = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<RpsDbContext>();
                var aplicadas = await AdendasProgramadas.AplicarVencidasAsync(db, stoppingToken);
                if (aplicadas > 0)
                    log.LogInformation("Adendas programadas aplicadas: {Cantidad}", aplicadas);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "No se pudieron aplicar las adendas programadas");
            }
        }
        while (await reloj.WaitForNextTickAsync(stoppingToken));
    }
}
