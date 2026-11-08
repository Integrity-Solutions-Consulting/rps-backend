using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Contratos.Domain;
using rps_backend.Persistence;

namespace rps_backend.Features.Dashboard.Application;

// Una fila de "Vencimientos de contratos" en Inicio. ContratoId sirve para abrir el contrato en la
// pantalla Contratos; Nombres/Apellidos vienen de la copia de TMR (nulos si el colaborador se creó a mano).
public record VencimientoContratoResponse(
    Guid ContratoId,
    Guid ColaboradorId,
    string TmrColaboradorId,
    string? Nombres,
    string? Apellidos,
    Guid ProyectoId,
    string ProyectoNombre,
    DateOnly FechaFinVigente,
    int DiasRestantes,
    bool AsignacionActiva);

// Contratos que hay que mirar en Inicio (RN 13, RN 14, RN 15):
//  - por vencer: fin vigente (con adendas) entre hoy y hoy + "dias";
//  - vencidos con la asignación todavía activa y sin otro contrato vigente para el mismo proyecto.
// Un contrato puede cubrir varios proyectos (o ninguno): se avisa por cada par contrato-proyecto,
// porque un proyecto puede necesitar revisión mientras otro del mismo contrato sigue sin problema.
// Solo informa: el vencimiento no bloquea la liquidación.
public class VencimientosContratosService(RpsDbContext db)
{
    public const int DiasPorDefecto = 30;

    public async Task<List<VencimientoContratoResponse>> ListarAsync(int? dias, CancellationToken ct)
    {
        var plazo = dias is > 0 and <= 365 ? dias.Value : DiasPorDefecto;
        var hoy = HoyEnEcuador();
        var limite = hoy.AddDays(plazo);

        var contratos = await db.Contratos.AsNoTracking().ToListAsync(ct);
        if (contratos.Count == 0)
            return [];

        var contratoIds = contratos.Select(c => c.Id).ToList();

        var proyectosPorContrato = (await db.ContratosProyectos.AsNoTracking()
                .Where(cp => contratoIds.Contains(cp.ContratoId))
                .ToListAsync(ct))
            .GroupBy(cp => cp.ContratoId)
            .ToDictionary(g => g.Key, g => g.Select(cp => cp.ProyectoId).ToList());

        var maxAdenda = (await db.Adendas.AsNoTracking()
                .Where(a => contratoIds.Contains(a.ContratoId))
                .Select(a => new { a.ContratoId, a.NuevaFechaFin })
                .ToListAsync(ct))
            .GroupBy(a => a.ContratoId)
            .ToDictionary(g => g.Key, g => g.Max(a => a.NuevaFechaFin));

        // Indefinido (FechaFin null) nunca vence: Fin queda null y no participa de este aviso.
        DateOnly? FinVigente(Contrato c) =>
            c.FechaFin is null ? null : (maxAdenda.TryGetValue(c.Id, out var f) && f > c.FechaFin ? f : c.FechaFin);

        // Un contrato sin proyecto (ej. personal de RR. HH.) no participa de este aviso: no hay
        // asignación de TMR contra la que revisar su vigencia.
        var conProyecto = contratos
            .SelectMany(c => proyectosPorContrato.GetValueOrDefault(c.Id, []).Select(proyectoId => (Contrato: c, ProyectoId: proyectoId)))
            .Select(x => (x.Contrato, x.ProyectoId, Fin: FinVigente(x.Contrato)))
            .ToList();

        if (conProyecto.Count == 0)
            return [];

        // Asignaciones activas hoy, por colaborador y proyecto. Las que ya no vienen de TMR no cuentan (RN 22).
        var activas = (await db.Asignaciones.AsNoTracking()
                .Where(a => !a.EliminadaEnOrigen && a.FechaInicio <= hoy && (a.FechaSalida == null || a.FechaSalida >= hoy))
                .Select(a => new { a.ColaboradorId, a.ProyectoId })
                .ToListAsync(ct))
            .Select(a => (a.ColaboradorId, a.ProyectoId))
            .ToHashSet();

        // Pares colaborador-proyecto que ya tienen un contrato vigente (renovado, o indefinido): sus
        // contratos viejos no se avisan.
        var conContratoVigente = conProyecto
            .Where(x => x.Fin is null || x.Fin >= hoy)
            .Select(x => (x.Contrato.ColaboradorId, x.ProyectoId))
            .ToHashSet();

        var elegidos = conProyecto.Where(x =>
        {
            var par = (x.Contrato.ColaboradorId, x.ProyectoId);
            var porVencer = x.Fin is not null && x.Fin >= hoy && x.Fin <= limite;
            var vencidoConAsignacion = x.Fin is not null && x.Fin < hoy && activas.Contains(par) && !conContratoVigente.Contains(par);
            return porVencer || vencidoConAsignacion;
        }).ToList();

        if (elegidos.Count == 0)
            return [];

        var colaboradorIds = elegidos.Select(x => x.Contrato.ColaboradorId).Distinct().ToList();
        var proyectoIds = elegidos.Select(x => x.ProyectoId).Distinct().ToList();

        var colaboradores = await db.ColaboradoresRps.AsNoTracking()
            .Where(c => colaboradorIds.Contains(c.Id))
            .Select(c => new { c.Id, c.TmrColaboradorId, c.Nombres, c.Apellidos })
            .ToDictionaryAsync(c => c.Id, ct);
        var proyectos = await db.Proyectos.AsNoTracking()
            .Where(p => proyectoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);

        return elegidos
            .Select(x =>
            {
                var c = colaboradores.GetValueOrDefault(x.Contrato.ColaboradorId);
                return new VencimientoContratoResponse(
                    x.Contrato.Id,
                    x.Contrato.ColaboradorId,
                    c?.TmrColaboradorId ?? string.Empty,
                    c?.Nombres,
                    c?.Apellidos,
                    x.ProyectoId,
                    proyectos.GetValueOrDefault(x.ProyectoId, string.Empty),
                    x.Fin!.Value,
                    x.Fin.Value.DayNumber - hoy.DayNumber,
                    activas.Contains((x.Contrato.ColaboradorId, x.ProyectoId)));
            })
            .OrderBy(v => v.DiasRestantes)
            .ToList();
    }

    // "Hoy" en Ecuador: con UtcNow, después de las 19:00 ya sería el día siguiente.
    private static DateOnly HoyEnEcuador()
    {
        try
        {
            var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona));
        }
        catch (TimeZoneNotFoundException)
        {
            return DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
        }
    }
}
