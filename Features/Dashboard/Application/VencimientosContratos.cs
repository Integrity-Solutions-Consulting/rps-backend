using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Contratos.Domain;
using rps_backend.Persistence;

namespace rps_backend.Features.Dashboard.Application;

// Una fila de "Vencimientos de contratos" en Inicio: UNA por contrato (el contrato es lo que se
// vence, no cada proyecto). ContratoId sirve para abrir el contrato en la pantalla Contratos;
// Nombres/Apellidos vienen de la copia de TMR. ProyectoId es siempre null (se conserva por
// compatibilidad); ProyectoNombre lista los proyectos del contrato, o "Sin proyectos".
public record VencimientoContratoResponse(
    Guid ContratoId,
    Guid ColaboradorId,
    string TmrColaboradorId,
    string? Nombres,
    string? Apellidos,
    Guid? ProyectoId,
    string ProyectoNombre,
    DateOnly FechaFinVigente,
    int DiasRestantes,
    bool AsignacionActiva);

// Contratos que hay que mirar en Inicio (RN 13, RN 14, RN 15), de todos los colaboradores y con o
// sin proyectos:
//  - por vencer: fin vigente (con adendas) entre hoy y hoy + "dias" (sin "dias": el primer aviso configurado, RN 13);
//  - vencidos sin otro contrato vigente para la persona y que todavía trabaja: alguna asignación
//    activa en sus proyectos o, si el contrato no tiene proyectos, el colaborador sigue activo.
// Solo informa: el vencimiento no bloquea la liquidación.
public class VencimientosContratosService(RpsDbContext db)
{
    // Solo si no existe la fila de configuración (mismo valor por defecto que ConfiguracionGlobal).
    public const int DiasPorDefecto = 30;

    public async Task<List<VencimientoContratoResponse>> ListarAsync(int? dias, CancellationToken ct)
    {
        var plazo = await PlazoAsync(db, dias, ct);
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

        var conFin = contratos.Select(c => (Contrato: c, Fin: FinVigente(c))).ToList();

        // Asignaciones activas hoy, por colaborador y proyecto. Las que ya no vienen de TMR no cuentan (RN 22).
        var activas = (await db.Asignaciones.AsNoTracking()
                .Where(a => !a.EliminadaEnOrigen && a.FechaInicio <= hoy && (a.FechaSalida == null || a.FechaSalida >= hoy))
                .Select(a => new { a.ColaboradorId, a.ProyectoId })
                .ToListAsync(ct))
            .Select(a => (a.ColaboradorId, a.ProyectoId))
            .ToHashSet();

        // Colaboradores con algún contrato vigente hoy (renovado, o indefinido): sus contratos viejos no se avisan.
        var conContratoVigente = conFin
            .Where(x => x.Contrato.FechaInicio <= hoy && (x.Fin is null || x.Fin >= hoy))
            .Select(x => x.Contrato.ColaboradorId)
            .ToHashSet();

        var colaboradores = await db.ColaboradoresRps.AsNoTracking()
            .Select(c => new { c.Id, c.TmrColaboradorId, c.Nombres, c.Apellidos, c.Activo })
            .ToDictionaryAsync(c => c.Id, ct);

        bool SigueTrabajando(Contrato c)
        {
            var proyectos = proyectosPorContrato.GetValueOrDefault(c.Id, []);
            return proyectos.Count > 0
                ? proyectos.Any(p => activas.Contains((c.ColaboradorId, p)))
                : colaboradores.GetValueOrDefault(c.ColaboradorId)?.Activo == true;
        }

        var elegidos = conFin.Where(x =>
        {
            var porVencer = x.Fin is not null && x.Fin >= hoy && x.Fin <= limite;
            var vencidoTrabajando = x.Fin is not null && x.Fin < hoy
                && !conContratoVigente.Contains(x.Contrato.ColaboradorId) && SigueTrabajando(x.Contrato);
            return porVencer || vencidoTrabajando;
        }).ToList();

        if (elegidos.Count == 0)
            return [];

        var proyectoIds = elegidos.SelectMany(x => proyectosPorContrato.GetValueOrDefault(x.Contrato.Id, [])).Distinct().ToList();
        var nombresProyecto = await db.Proyectos.AsNoTracking()
            .Where(p => proyectoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);

        return elegidos
            .Select(x =>
            {
                var c = colaboradores.GetValueOrDefault(x.Contrato.ColaboradorId);
                var proyectos = proyectosPorContrato.GetValueOrDefault(x.Contrato.Id, [])
                    .Select(p => nombresProyecto.GetValueOrDefault(p, "Proyecto"))
                    .ToList();
                return new VencimientoContratoResponse(
                    x.Contrato.Id,
                    x.Contrato.ColaboradorId,
                    c?.TmrColaboradorId ?? string.Empty,
                    c?.Nombres,
                    c?.Apellidos,
                    null,
                    proyectos.Count > 0 ? string.Join(", ", proyectos) : "Sin proyectos",
                    x.Fin!.Value,
                    x.Fin.Value.DayNumber - hoy.DayNumber,
                    SigueTrabajando(x.Contrato));
            })
            .OrderBy(v => v.DiasRestantes)
            .ToList();
    }

    // El plazo del aviso: el que pide la pantalla, o el primer aviso configurado (RN 13).
    internal static async Task<int> PlazoAsync(RpsDbContext db, int? dias, CancellationToken ct) =>
        dias is > 0 and <= 365
            ? dias.Value
            : (await db.ConfiguracionGlobal.AsNoTracking().Select(c => (int?)c.DiasAvisoVencimiento1).FirstOrDefaultAsync(ct)) ?? DiasPorDefecto;

    // "Hoy" en Ecuador: con UtcNow, después de las 19:00 ya sería el día siguiente.
    internal static DateOnly HoyEnEcuador()
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
