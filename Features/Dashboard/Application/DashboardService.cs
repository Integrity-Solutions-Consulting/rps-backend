using Microsoft.EntityFrameworkCore;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Dashboard.Application;

// Agregados de solo lectura sobre datos ya existentes. No hay forma de fallar: sin Resultado<T>.
public class DashboardService(RpsDbContext db)
{
    public async Task<DashboardResponse> ObtenerAsync(DateOnly? periodo, CancellationToken ct)
    {
        var colaboradores = await ObtenerColaboradoresResumenAsync(ct);
        var honorarios = await ObtenerHonorariosResumenAsync(periodo, ct);
        var liquidaciones = await ObtenerLiquidacionesResumenAsync(periodo, ct);
        var anticipos = await ObtenerAnticiposResumenAsync(ct);
        var contratosPorVencer = await ObtenerContratosPorVencerAsync(ct);

        return new DashboardResponse(periodo, colaboradores, honorarios, liquidaciones, anticipos, contratosPorVencer);
    }

    private async Task<ColaboradoresResumen> ObtenerColaboradoresResumenAsync(CancellationToken ct)
    {
        var configurados = await db.ColaboradoresRps.CountAsync(c => c.EstadoConfiguracion == EstadoConfiguracion.Configurado, ct);
        var pendientes = await db.ColaboradoresRps.CountAsync(c => c.EstadoConfiguracion == EstadoConfiguracion.Pendiente, ct);
        var activos = await db.ColaboradoresRps.CountAsync(c => c.Activo, ct);
        var inactivos = await db.ColaboradoresRps.CountAsync(c => !c.Activo, ct);

        return new ColaboradoresResumen(configurados, pendientes, activos, inactivos);
    }

    private async Task<HonorariosResumen> ObtenerHonorariosResumenAsync(DateOnly? periodo, CancellationToken ct)
    {
        var query = db.Honorarios.AsQueryable();
        if (periodo is not null)
            query = query.Where(h => h.Periodo == periodo);

        var cantidad = await query.CountAsync(ct);
        var netoTotal = await query.SumAsync(h => h.Neto, ct);

        return new HonorariosResumen(cantidad, netoTotal);
    }

    private async Task<LiquidacionesResumen> ObtenerLiquidacionesResumenAsync(DateOnly? periodo, CancellationToken ct)
    {
        var query = db.Liquidaciones.AsQueryable();
        if (periodo is not null)
            query = query.Where(l => l.Periodo == periodo);

        var revisadas = await query.CountAsync(l => l.Revisado, ct);
        var sinRevisar = await query.CountAsync(l => !l.Revisado, ct);

        return new LiquidacionesResumen(revisadas, sinRevisar);
    }

    private async Task<AnticiposResumen> ObtenerAnticiposResumenAsync(CancellationToken ct)
    {
        var anticipos = await db.AnticiposAdicionales.AsNoTracking().ToListAsync(ct);
        if (anticipos.Count == 0)
            return new AnticiposResumen(0, 0m);

        var anticipoIds = anticipos.Select(a => a.Id).ToList();
        var cuotasPorAnticipo = (await db.CuotasRecuperacion.AsNoTracking()
                .Where(c => anticipoIds.Contains(c.AnticipoAdicionalId))
                .ToListAsync(ct))
            .GroupBy(c => c.AnticipoAdicionalId)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Importe));

        var saldos = anticipos.Select(a => a.Monto - cuotasPorAnticipo.GetValueOrDefault(a.Id, 0m)).Where(s => s > 0).ToList();

        return new AnticiposResumen(saldos.Count, saldos.Sum());
    }

    private async Task<ContratosPorVencerResumen> ObtenerContratosPorVencerAsync(CancellationToken ct)
    {
        var contratos = await db.Contratos.AsNoTracking().ToListAsync(ct);
        var contratoIds = contratos.Select(c => c.Id).ToList();

        var maxAdendaPorContrato = (await db.Adendas.AsNoTracking()
                .Where(a => contratoIds.Contains(a.ContratoId))
                .ToListAsync(ct))
            .GroupBy(a => a.ContratoId)
            .ToDictionary(g => g.Key, g => g.Max(a => a.NuevaFechaFin));

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var fechasFinVigente = contratos.Select(c =>
        {
            var maxAdenda = maxAdendaPorContrato.GetValueOrDefault(c.Id);
            return maxAdenda > c.FechaFin ? maxAdenda : c.FechaFin;
        }).ToList();

        int ContarHasta(int dias) => fechasFinVigente.Count(f => f >= hoy && f <= hoy.AddDays(dias));

        return new ContratosPorVencerResumen(ContarHasta(30), ContarHasta(15), ContarHasta(7));
    }
}
