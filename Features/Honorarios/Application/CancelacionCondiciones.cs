using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Honorarios.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// ===== Responses =====

// Igual que CondicionEconomicaResponse, más cuándo se registró y si está cancelada.
public record CondicionEconomicaConEstadoResponse(
    Guid Id, Guid ColaboradorId, Guid ProyectoId, string ProyectoNombre, Guid ContratoId,
    decimal ImporteMensual, Modalidad Modalidad, DateOnly MesInicioVigencia, string? Observaciones,
    DateTime CreadoEn, DateTime? CanceladoEn);

// ===== Errores =====

public static class CancelacionCondicionErrores
{
    public static Error NoEncontrada(Guid id) =>
        Error.NoEncontrado("CondicionEconomica.NoEncontrada", $"No existe una condición económica con id '{id}' para ese colaborador.");

    public static readonly Error NoModificable =
        Error.Conflicto("CondicionEconomica.NoModificable", "Solo se pueden cancelar o restaurar cambios de meses que aún no empiezan.");

    public static readonly Error YaCancelada =
        Error.Conflicto("CondicionEconomica.YaCancelada", "Este cambio ya estaba cancelado.");

    public static readonly Error NoCancelada =
        Error.Conflicto("CondicionEconomica.NoCancelada", "Este cambio no está cancelado.");

    public static readonly Error ConHonorarios =
        Error.Conflicto("CondicionEconomica.ConHonorarios", "Ya se generaron honorarios con esta condición: corrígela registrando un cambio nuevo.");

    public static Error Duplicada(DateOnly mes) =>
        Error.Conflicto("CondicionEconomica.MesInicioVigenciaDuplicado", $"Ya hay otra condición vigente desde {mes:yyyy-MM-dd} para ese proyecto.");
}

// ===== Servicio =====

// Cancelar y restaurar cambios programados de honorario (botón "Deshacer" del frontend).
// Cancelar no borra: marca CanceladoEn y deja un movimiento; así el historial no pierde nada (RN 48).
public class CancelacionCondicionesService(RpsDbContext db)
{
    // Todas las condiciones del colaborador, también las canceladas (para el historial por proyecto).
    public async Task<Resultado<List<CondicionEconomicaConEstadoResponse>>> ListarConCanceladasAsync(Guid colaboradorId, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return CondicionEconomicaErrores.ColaboradorNoEncontrado(colaboradorId);

        return await db.CondicionesEconomicas.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.ColaboradorId == colaboradorId)
            .Join(db.Proyectos.AsNoTracking(), c => c.ProyectoId, p => p.Id, (c, p) => new CondicionEconomicaConEstadoResponse(
                c.Id, c.ColaboradorId, c.ProyectoId, p.Nombre, c.ContratoId, c.ImporteMensual, c.Modalidad,
                c.MesInicioVigencia, c.Observaciones, c.CreadoEn, c.CanceladoEn))
            .OrderByDescending(c => c.MesInicioVigencia)
            .ToListAsync(ct);
    }

    public async Task<Resultado<CondicionEconomicaConEstadoResponse>> CancelarAsync(Guid colaboradorId, Guid condicionId, CancellationToken ct)
    {
        var condicion = await db.CondicionesEconomicas.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == condicionId && c.ColaboradorId == colaboradorId, ct);
        if (condicion is null)
            return CancelacionCondicionErrores.NoEncontrada(condicionId);
        if (condicion.CanceladoEn is not null)
            return CancelacionCondicionErrores.YaCancelada;
        if (condicion.MesInicioVigencia <= MesActual())
            return CancelacionCondicionErrores.NoModificable;
        if (await db.Honorarios.AnyAsync(h => h.CondicionEconomicaId == condicionId, ct))
            return CancelacionCondicionErrores.ConHonorarios;

        condicion.CanceladoEn = DateTime.UtcNow;
        db.CondicionesEconomicasMovimientos.Add(new CondicionEconomicaMovimiento { CondicionEconomicaId = condicionId, Tipo = "Cancelado" });
        await db.SaveChangesAsync(ct);

        return await AResponseAsync(condicion, ct);
    }

    public async Task<Resultado<CondicionEconomicaConEstadoResponse>> RestaurarAsync(Guid colaboradorId, Guid condicionId, CancellationToken ct)
    {
        var condicion = await db.CondicionesEconomicas.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == condicionId && c.ColaboradorId == colaboradorId, ct);
        if (condicion is null)
            return CancelacionCondicionErrores.NoEncontrada(condicionId);
        if (condicion.CanceladoEn is null)
            return CancelacionCondicionErrores.NoCancelada;
        if (condicion.MesInicioVigencia <= MesActual())
            return CancelacionCondicionErrores.NoModificable;

        // El filtro global ya excluye canceladas: si encuentra una, es una activa para ese mismo mes.
        var ocupado = await db.CondicionesEconomicas.AnyAsync(c =>
            c.ColaboradorId == colaboradorId && c.ProyectoId == condicion.ProyectoId && c.MesInicioVigencia == condicion.MesInicioVigencia, ct);
        if (ocupado)
            return CancelacionCondicionErrores.Duplicada(condicion.MesInicioVigencia);

        condicion.CanceladoEn = null;
        db.CondicionesEconomicasMovimientos.Add(new CondicionEconomicaMovimiento { CondicionEconomicaId = condicionId, Tipo = "Restaurado" });
        await db.SaveChangesAsync(ct);

        return await AResponseAsync(condicion, ct);
    }

    private static DateOnly MesActual()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        return new DateOnly(hoy.Year, hoy.Month, 1);
    }

    private async Task<CondicionEconomicaConEstadoResponse> AResponseAsync(CondicionEconomica c, CancellationToken ct)
    {
        var proyectoNombre = await db.Proyectos.AsNoTracking().Where(p => p.Id == c.ProyectoId).Select(p => p.Nombre).FirstAsync(ct);
        return new CondicionEconomicaConEstadoResponse(
            c.Id, c.ColaboradorId, c.ProyectoId, proyectoNombre, c.ContratoId, c.ImporteMensual, c.Modalidad,
            c.MesInicioVigencia, c.Observaciones, c.CreadoEn, c.CanceladoEn);
    }
}
