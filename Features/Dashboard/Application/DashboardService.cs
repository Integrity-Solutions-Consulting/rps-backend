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

        return new DashboardResponse(periodo, colaboradores, honorarios, liquidaciones, anticipos);
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

        var pendientes = await query.CountAsync(h => h.Estado == EstadoHonorario.Pendiente, ct);
        var aprobados = await query.CountAsync(h => h.Estado == EstadoHonorario.Aprobado, ct);
        var pagados = await query.CountAsync(h => h.Estado == EstadoHonorario.Pagado, ct);
        var anulados = await query.CountAsync(h => h.Estado == EstadoHonorario.Anulado, ct);
        var montoNetoTotal = await query.Where(h => h.Estado != EstadoHonorario.Anulado).SumAsync(h => h.Monto, ct);

        return new HonorariosResumen(pendientes, aprobados, pagados, anulados, montoNetoTotal);
    }

    private async Task<LiquidacionesResumen> ObtenerLiquidacionesResumenAsync(DateOnly? periodo, CancellationToken ct)
    {
        var query = db.Liquidaciones.AsQueryable();
        if (periodo is not null)
            query = query.Where(l => l.Periodo == periodo);

        var generadas = await query.CountAsync(l => l.Estado == EstadoLiquidacion.Generada, ct);
        var pagadas = await query.CountAsync(l => l.Estado == EstadoLiquidacion.Pagada, ct);

        return new LiquidacionesResumen(generadas, pagadas);
    }

    private async Task<AnticiposResumen> ObtenerAnticiposResumenAsync(CancellationToken ct)
    {
        var aprobados = db.Anticipos.Where(a => a.Estado == EstadoAnticipo.Aprobado);

        var cantidad = await aprobados.CountAsync(ct);
        var monto = await aprobados.SumAsync(a => a.Monto, ct);

        return new AnticiposResumen(cantidad, monto);
    }
}
