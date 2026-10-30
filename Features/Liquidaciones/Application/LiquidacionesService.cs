using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Honorarios.Application;
using rps_backend.Features.Liquidaciones.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Application;

// Reglas de negocio de Liquidaciones. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
public class LiquidacionesService(RpsDbContext db)
{
    // Una liquidación por colaborador agrupa sus honorarios Aprobado sin liquidar todavía del período.
    // Un honorario ya asignado a una liquidación (LiquidacionId != null) no se vuelve a tomar: idempotente.
    public async Task<Resultado<GenerarLiquidacionesResponse>> GenerarAsync(GenerarLiquidacionesRequest request, CancellationToken ct)
    {
        if (request.Periodo is not { Day: 1 } periodo)
            return LiquidacionErrores.PeriodoInvalido;

        var honorarios = await db.Honorarios
            .Where(h => h.Periodo == periodo && h.Estado == EstadoHonorario.Aprobado && h.LiquidacionId == null)
            .ToListAsync(ct);

        if (honorarios.Count == 0)
            return new GenerarLiquidacionesResponse(0, 0);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var generadas = 0;
        foreach (var grupo in honorarios.GroupBy(h => h.ColaboradorId))
        {
            var liquidacion = new Liquidacion
            {
                ColaboradorId = grupo.Key,
                Periodo = periodo,
                MontoTotal = grupo.Sum(h => h.Monto)
            };
            db.Liquidaciones.Add(liquidacion);

            foreach (var honorario in grupo)
                honorario.LiquidacionId = liquidacion.Id;

            generadas++;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new GenerarLiquidacionesResponse(generadas, honorarios.Count);
    }

    public async Task<List<LiquidacionResponse>> ListarAsync(
        DateOnly? periodo, Guid? colaboradorId, EstadoLiquidacion? estado, CancellationToken ct)
    {
        var query = db.Liquidaciones.AsNoTracking();

        if (periodo is not null)
            query = query.Where(l => l.Periodo == periodo);

        if (colaboradorId is not null)
            query = query.Where(l => l.ColaboradorId == colaboradorId);

        if (estado is not null)
            query = query.Where(l => l.Estado == estado);

        return await query
            .OrderByDescending(l => l.Periodo)
            .Select(l => new LiquidacionResponse(
                l.Id, l.ColaboradorId, l.Periodo, l.MontoTotal, l.Estado, l.PagadaEn, l.CreadoEn, l.ActualizadoEn))
            .ToListAsync(ct);
    }

    public async Task<Resultado<LiquidacionDetalleResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var liquidacion = await db.Liquidaciones.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (liquidacion is null)
            return LiquidacionErrores.NoEncontrado(id);

        var honorarios = await db.Honorarios.AsNoTracking()
            .Where(h => h.LiquidacionId == id)
            .OrderBy(h => h.NumeroPago)
            .Select(h => new HonorarioResponse(
                h.Id, h.ColaboradorId, h.ContratoId, h.Periodo, h.NumeroPago, h.Monto, h.Descuento, h.Estado, h.CreadoEn, h.ActualizadoEn))
            .ToListAsync(ct);

        return new LiquidacionDetalleResponse(
            liquidacion.Id, liquidacion.ColaboradorId, liquidacion.Periodo, liquidacion.MontoTotal,
            liquidacion.Estado, liquidacion.PagadaEn, liquidacion.CreadoEn, liquidacion.ActualizadoEn, honorarios);
    }

    public async Task<Resultado<LiquidacionResponse>> MarcarPagadaAsync(Guid id, CancellationToken ct)
    {
        var liquidacion = await db.Liquidaciones.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (liquidacion is null)
            return LiquidacionErrores.NoEncontrado(id);

        if (liquidacion.Estado != EstadoLiquidacion.Generada)
            return LiquidacionErrores.TransicionInvalida(liquidacion.Estado, "pagar");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Solo los que siguen Aprobado: uno que se anuló después de generar la liquidación
        // (vía HonorariosService.AnularAsync) no debe resucitar a Pagado.
        var honorarios = await db.Honorarios
            .Where(h => h.LiquidacionId == id && h.Estado == EstadoHonorario.Aprobado)
            .ToListAsync(ct);
        foreach (var honorario in honorarios)
        {
            honorario.Estado = EstadoHonorario.Pagado;
            honorario.ActualizadoEn = DateTime.UtcNow;
        }

        liquidacion.Estado = EstadoLiquidacion.Pagada;
        liquidacion.PagadaEn = DateTime.UtcNow;
        liquidacion.ActualizadoEn = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return AResponse(liquidacion);
    }

    private static LiquidacionResponse AResponse(Liquidacion l) => new(
        l.Id, l.ColaboradorId, l.Periodo, l.MontoTotal, l.Estado, l.PagadaEn, l.CreadoEn, l.ActualizadoEn);
}
