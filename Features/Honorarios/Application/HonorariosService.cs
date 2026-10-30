using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Anticipos.Domain;
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

        // Orden determinístico: con varios contratos simultáneos del mismo colaborador, decide
        // de forma estable cuál honorario absorbe primero un anticipo pendiente.
        var contratosVigentes = await db.Contratos.AsNoTracking()
            .Where(c => colaboradorIds.Contains(c.ColaboradorId)
                && c.FechaInicio <= finPeriodo
                && (c.FechaFin == null || c.FechaFin >= periodo))
            .OrderBy(c => c.ColaboradorId).ThenBy(c => c.FechaInicio).ThenBy(c => c.Id)
            .ToListAsync(ct);

        var frecuenciaPorColaborador = (await db.ColaboradoresFrecuencia.AsNoTracking()
                .Where(f => colaboradorIds.Contains(f.ColaboradorId) && f.VigenteDesdeMes <= periodo)
                .ToListAsync(ct))
            .GroupBy(f => f.ColaboradorId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.VigenteDesdeMes).First().Frecuencia);

        // Anticipos Aprobado pendientes de descontar, más antiguo primero. Tracked (no AsNoTracking):
        // se modifican más abajo al aplicarse.
        var anticiposPorColaborador = (await db.Anticipos
                .Where(a => colaboradorIds.Contains(a.ColaboradorId) && a.Estado == EstadoAnticipo.Aprobado)
                .ToListAsync(ct))
            .GroupBy(a => a.ColaboradorId)
            .ToDictionary(g => g.Key, g => new Queue<Anticipo>(g.OrderBy(a => a.FechaSolicitud)));

        var existentes = (await db.Honorarios.AsNoTracking()
                .Where(h => h.Periodo == periodo && colaboradorIds.Contains(h.ColaboradorId))
                .Select(h => new { h.ColaboradorId, h.ContratoId, h.NumeroPago })
                .ToListAsync(ct))
            .Select(h => (h.ColaboradorId, h.ContratoId, h.NumeroPago))
            .ToHashSet();

        // Feriados del período, para contar días hábiles al prorratear contratos parciales.
        var feriados = (await db.Feriados.AsNoTracking()
                .Where(f => f.Fecha >= periodo && f.Fecha <= finPeriodo)
                .Select(f => f.Fecha)
                .ToListAsync(ct))
            .ToHashSet();

        var nuevos = new List<Honorario>();
        var omitidos = 0;

        foreach (var contrato in contratosVigentes)
        {
            // Sin frecuencia vigente a ese período no se puede determinar el reparto: se omite.
            if (!frecuenciaPorColaborador.TryGetValue(contrato.ColaboradorId, out var frecuencia))
                continue;

            // Prorratea por días hábiles (lunes-viernes, sin feriados) cuando el contrato no
            // cubre la ventana completa; si la cubre, el factor es 1 y el monto sale igual que
            // antes. DosPagos prorratea cada quincena por separado (día 1-15 y 16-fin de mes);
            // el primer pago se redondea y el segundo absorbe el resto para que, cuando el
            // contrato cubre todo el mes, la suma sea exactamente la Tarifa.
            List<(int NumeroPago, decimal Monto)> pagos;
            if (frecuencia == Frecuencia.DosPagos)
            {
                var mitadBase = Math.Round(contrato.Tarifa / 2, 2, MidpointRounding.AwayFromZero);
                var finQuincena1 = new DateOnly(periodo.Year, periodo.Month, 15);
                var inicioQuincena2 = finQuincena1.AddDays(1);

                var pago1 = CalcularMontoProrrateado(mitadBase, periodo, finQuincena1, contrato.FechaInicio, contrato.FechaFin, feriados);
                var pago2 = CalcularMontoProrrateado(contrato.Tarifa - mitadBase, inicioQuincena2, finPeriodo, contrato.FechaInicio, contrato.FechaFin, feriados);
                pagos = [(1, pago1), (2, pago2)];
            }
            else
            {
                var monto = CalcularMontoProrrateado(contrato.Tarifa, periodo, finPeriodo, contrato.FechaInicio, contrato.FechaFin, feriados);
                pagos = [(1, monto)];
            }

            foreach (var (numeroPago, montoBruto) in pagos)
            {
                if (existentes.Contains((contrato.ColaboradorId, contrato.Id, numeroPago)))
                {
                    omitidos++;
                    continue;
                }

                var honorario = new Honorario
                {
                    ColaboradorId = contrato.ColaboradorId,
                    ContratoId = contrato.Id,
                    Periodo = periodo,
                    NumeroPago = numeroPago,
                    Monto = montoBruto
                };

                // Descuenta anticipos completos mientras alcancen; uno que no alcanza se deja
                // para el próximo honorario (de este u otro período) sin fraccionarlo.
                if (anticiposPorColaborador.TryGetValue(contrato.ColaboradorId, out var pendientes))
                {
                    while (pendientes.Count > 0 && pendientes.Peek().Monto <= honorario.Monto)
                    {
                        var anticipo = pendientes.Dequeue();
                        honorario.Monto -= anticipo.Monto;
                        honorario.Descuento += anticipo.Monto;

                        anticipo.Estado = EstadoAnticipo.Descontado;
                        anticipo.HonorarioId = honorario.Id;
                        anticipo.ActualizadoEn = DateTime.UtcNow;
                    }
                }

                nuevos.Add(honorario);
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
                h.Id, h.ColaboradorId, h.ContratoId, h.Periodo, h.NumeroPago, h.Monto, h.Descuento, h.Estado, h.CreadoEn, h.ActualizadoEn))
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

        // Si ya está agrupado en una liquidación, se paga pagando esa liquidación, no aquí
        // directo: evita que la liquidación quede Generada con un honorario ya Pagado por fuera.
        if (honorario.LiquidacionId is not null)
            return HonorarioErrores.PerteneceALiquidacion;

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

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Los anticipos que este honorario había descontado vuelven a Aprobado: el adelanto
        // sigue pendiente de cobrarse, no se pierde porque se anuló el honorario que lo cubría.
        var anticiposDescontados = await db.Anticipos
            .Where(a => a.HonorarioId == honorario.Id)
            .ToListAsync(ct);

        foreach (var anticipo in anticiposDescontados)
        {
            anticipo.Estado = EstadoAnticipo.Aprobado;
            anticipo.HonorarioId = null;
            anticipo.ActualizadoEn = DateTime.UtcNow;
        }

        honorario.Estado = EstadoHonorario.Anulado;
        honorario.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return AResponse(honorario);
    }

    private static HonorarioResponse AResponse(Honorario h) => new(
        h.Id, h.ColaboradorId, h.ContratoId, h.Periodo, h.NumeroPago, h.Monto, h.Descuento, h.Estado, h.CreadoEn, h.ActualizadoEn);

    // montoBase prorrateado por la fracción de días hábiles de [ventanaInicio, ventanaFin] que el
    // contrato efectivamente cubre. Si el contrato cubre toda la ventana, el factor es 1 y el
    // monto sale igual a montoBase (sin cambio de comportamiento para el caso ya existente).
    private static decimal CalcularMontoProrrateado(
        decimal montoBase, DateOnly ventanaInicio, DateOnly ventanaFin,
        DateOnly contratoInicio, DateOnly? contratoFin, HashSet<DateOnly> feriados)
    {
        var totalHabiles = ContarDiasHabiles(ventanaInicio, ventanaFin, feriados);
        if (totalHabiles == 0)
            return 0m;

        var trabajadoInicio = contratoInicio > ventanaInicio ? contratoInicio : ventanaInicio;
        var finEfectivo = contratoFin ?? ventanaFin;
        var trabajadoFin = finEfectivo < ventanaFin ? finEfectivo : ventanaFin;

        var diasTrabajados = ContarDiasHabiles(trabajadoInicio, trabajadoFin, feriados);

        return Math.Round(montoBase * diasTrabajados / totalHabiles, 2, MidpointRounding.AwayFromZero);
    }

    private static int ContarDiasHabiles(DateOnly desde, DateOnly hasta, HashSet<DateOnly> feriados)
    {
        if (hasta < desde)
            return 0;

        var contador = 0;
        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
        {
            if (fecha.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            if (feriados.Contains(fecha))
                continue;

            contador++;
        }

        return contador;
    }
}
