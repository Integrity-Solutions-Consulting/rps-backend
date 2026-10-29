using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Honorarios.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// Reglas de negocio de Honorarios. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
public class HonorariosService(RpsDbContext db)
{
    // Un honorario por contrato vigente durante el período; la Frecuencia vigente al período
    // decide si se divide en 1 (FinDeMes) o 2 (DosPagos) pagos. Idempotente: si ya existe la
    // combinación colaborador+contrato+periodo+numeroPago, se omite en vez de duplicar.
    public async Task<Resultado<GenerarHonorariosResponse>> GenerarAsync(GenerarHonorariosRequest request, CancellationToken ct)
    {
        if (request.Periodo is not { Day: 1 } periodo)
            return HonorarioErrores.PeriodoInvalido;

        var finPeriodo = periodo.AddMonths(1).AddDays(-1);

        var colaboradorIds = await db.ColaboradoresRps.AsNoTracking()
            .Where(c => c.Activo && c.EstadoConfiguracion == EstadoConfiguracion.Configurado)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (colaboradorIds.Count == 0)
            return new GenerarHonorariosResponse(0, 0);

        var contratosVigentes = await db.Contratos.AsNoTracking()
            .Where(c => colaboradorIds.Contains(c.ColaboradorId)
                && c.FechaInicio <= finPeriodo
                && (c.FechaFin == null || c.FechaFin >= periodo))
            .ToListAsync(ct);

        var frecuenciaPorColaborador = (await db.ColaboradoresFrecuencia.AsNoTracking()
                .Where(f => colaboradorIds.Contains(f.ColaboradorId) && f.VigenteDesdeMes <= periodo)
                .ToListAsync(ct))
            .GroupBy(f => f.ColaboradorId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.VigenteDesdeMes).First().Frecuencia);

        var existentes = (await db.Honorarios.AsNoTracking()
                .Where(h => h.Periodo == periodo && colaboradorIds.Contains(h.ColaboradorId))
                .Select(h => new { h.ColaboradorId, h.ContratoId, h.NumeroPago })
                .ToListAsync(ct))
            .Select(h => (h.ColaboradorId, h.ContratoId, h.NumeroPago))
            .ToHashSet();

        var nuevos = new List<Honorario>();
        var omitidos = 0;

        foreach (var contrato in contratosVigentes)
        {
            // Sin frecuencia vigente a ese período no se puede determinar el reparto: se omite.
            if (!frecuenciaPorColaborador.TryGetValue(contrato.ColaboradorId, out var frecuencia))
                continue;

            List<(int NumeroPago, decimal Monto)> pagos = frecuencia == Frecuencia.DosPagos
                ? [(1, contrato.Tarifa / 2), (2, contrato.Tarifa / 2)]
                : [(1, contrato.Tarifa)];

            foreach (var (numeroPago, monto) in pagos)
            {
                if (existentes.Contains((contrato.ColaboradorId, contrato.Id, numeroPago)))
                {
                    omitidos++;
                    continue;
                }

                nuevos.Add(new Honorario
                {
                    ColaboradorId = contrato.ColaboradorId,
                    ContratoId = contrato.Id,
                    Periodo = periodo,
                    NumeroPago = numeroPago,
                    Monto = monto
                });
            }
        }

        if (nuevos.Count > 0)
        {
            db.Honorarios.AddRange(nuevos);
            await db.SaveChangesAsync(ct);
        }

        return new GenerarHonorariosResponse(nuevos.Count, omitidos);
    }

    public async Task<List<HonorarioResponse>> ListarAsync(
        DateOnly? periodo, Guid? colaboradorId, EstadoHonorario? estado, CancellationToken ct)
    {
        var query = db.Honorarios.AsNoTracking();

        if (periodo is not null)
            query = query.Where(h => h.Periodo == periodo);

        if (colaboradorId is not null)
            query = query.Where(h => h.ColaboradorId == colaboradorId);

        if (estado is not null)
            query = query.Where(h => h.Estado == estado);

        return await query
            .OrderByDescending(h => h.Periodo)
            .ThenBy(h => h.NumeroPago)
            .Select(h => new HonorarioResponse(
                h.Id, h.ColaboradorId, h.ContratoId, h.Periodo, h.NumeroPago, h.Monto, h.Estado, h.CreadoEn, h.ActualizadoEn))
            .ToListAsync(ct);
    }

    public async Task<Resultado<HonorarioResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var honorario = await db.Honorarios.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id, ct);
        if (honorario is null)
            return HonorarioErrores.NoEncontrado(id);

        return AResponse(honorario);
    }

    public async Task<Resultado<HonorarioResponse>> AprobarAsync(Guid id, CancellationToken ct)
    {
        var honorario = await db.Honorarios.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (honorario is null)
            return HonorarioErrores.NoEncontrado(id);

        if (honorario.Estado != EstadoHonorario.Pendiente)
            return HonorarioErrores.TransicionInvalida(honorario.Estado, "aprobar");

        honorario.Estado = EstadoHonorario.Aprobado;
        honorario.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return AResponse(honorario);
    }

    public async Task<Resultado<HonorarioResponse>> MarcarPagadoAsync(Guid id, CancellationToken ct)
    {
        var honorario = await db.Honorarios.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (honorario is null)
            return HonorarioErrores.NoEncontrado(id);

        if (honorario.Estado != EstadoHonorario.Aprobado)
            return HonorarioErrores.TransicionInvalida(honorario.Estado, "marcar como pagado");

        honorario.Estado = EstadoHonorario.Pagado;
        honorario.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return AResponse(honorario);
    }

    public async Task<Resultado<HonorarioResponse>> AnularAsync(Guid id, CancellationToken ct)
    {
        var honorario = await db.Honorarios.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (honorario is null)
            return HonorarioErrores.NoEncontrado(id);

        if (honorario.Estado is not (EstadoHonorario.Pendiente or EstadoHonorario.Aprobado))
            return HonorarioErrores.TransicionInvalida(honorario.Estado, "anular");

        honorario.Estado = EstadoHonorario.Anulado;
        honorario.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return AResponse(honorario);
    }

    private static HonorarioResponse AResponse(Honorario h) => new(
        h.Id, h.ColaboradorId, h.ContratoId, h.Periodo, h.NumeroPago, h.Monto, h.Estado, h.CreadoEn, h.ActualizadoEn);
}
