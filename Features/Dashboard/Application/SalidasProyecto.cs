using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Honorarios.Application;
using rps_backend.Persistence;

namespace rps_backend.Features.Dashboard.Application;

// Una fila de "Salidas de proyecto" en Inicio: alguien sale de un proyecto que se le paga por
// proyecto, así que ese honorario deja de pagarse desde el día siguiente a la salida.
public record SalidaProyectoResponse(
    Guid ColaboradorId,
    string TmrColaboradorId,
    string? Nombres,
    string? Apellidos,
    Guid ContratoId,
    Guid ProyectoId,
    string ProyectoNombre,
    DateOnly FechaSalida,
    int DiasRestantes);

// Salidas de proyecto (fecha de salida de la asignación en TMR) entre hoy y hoy + "dias" (sin
// "dias": el mismo plazo que el aviso de vencimiento de contratos). Solo de proyectos que se pagan
// POR PROYECTO: si el contrato se paga por contrato, salir de un proyecto no cambia el pago.
public class SalidasProyectoService(RpsDbContext db)
{
    public async Task<List<SalidaProyectoResponse>> ListarAsync(int? dias, CancellationToken ct)
    {
        var plazo = await VencimientosContratosService.PlazoAsync(db, dias, ct);
        var hoy = VencimientosContratosService.HoyEnEcuador();
        var limite = hoy.AddDays(plazo);
        var mesActual = new DateOnly(hoy.Year, hoy.Month, 1);

        var salidas = await db.Asignaciones.AsNoTracking()
            .Where(a => !a.EliminadaEnOrigen && a.FechaSalida != null && a.FechaSalida >= hoy && a.FechaSalida <= limite)
            .Select(a => new { a.ColaboradorId, a.ProyectoId, FechaSalida = a.FechaSalida!.Value })
            .ToListAsync(ct);
        if (salidas.Count == 0)
            return [];

        var colaboradorIds = salidas.Select(s => s.ColaboradorId).Distinct().ToList();

        // Lo que rige hoy: solo cuentan los proyectos con honorario propio (pago por proyecto).
        var porProyecto = CondicionesVigentes.Seleccionar(
                await db.CondicionesEconomicas.AsNoTracking()
                    .Where(c => colaboradorIds.Contains(c.ColaboradorId) && c.MesInicioVigencia <= mesActual)
                    .ToListAsync(ct),
                mesActual)
            .Where(c => c.ProyectoId is not null)
            .GroupBy(c => (c.ColaboradorId, c.ProyectoId!.Value))
            .ToDictionary(g => g.Key, g => g.First().ContratoId);

        var elegidas = salidas.Where(s => porProyecto.ContainsKey((s.ColaboradorId, s.ProyectoId))).ToList();
        if (elegidas.Count == 0)
            return [];

        var colaboradores = await db.ColaboradoresRps.AsNoTracking()
            .Where(c => colaboradorIds.Contains(c.Id))
            .Select(c => new { c.Id, c.TmrColaboradorId, c.Nombres, c.Apellidos })
            .ToDictionaryAsync(c => c.Id, ct);
        var proyectoIds = elegidas.Select(s => s.ProyectoId).Distinct().ToList();
        var nombres = await db.Proyectos.AsNoTracking()
            .Where(p => proyectoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);

        return elegidas
            .Select(s =>
            {
                var c = colaboradores.GetValueOrDefault(s.ColaboradorId);
                return new SalidaProyectoResponse(
                    s.ColaboradorId, c?.TmrColaboradorId ?? string.Empty, c?.Nombres, c?.Apellidos,
                    porProyecto[(s.ColaboradorId, s.ProyectoId)], s.ProyectoId, nombres.GetValueOrDefault(s.ProyectoId, "Proyecto"),
                    s.FechaSalida, s.FechaSalida.DayNumber - hoy.DayNumber);
            })
            .OrderBy(s => s.DiasRestantes)
            .ToList();
    }
}
