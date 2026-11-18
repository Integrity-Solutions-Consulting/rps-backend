using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Liquidaciones.Application;
using rps_backend.Features.Prestamos.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Prestamos.Application;

// Reglas de negocio de Préstamos. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
//   - Se pueden registrar varios préstamos en el mismo mes y el monto puede ser mayor que el honorario.
//   - El plan de pagos se arma a mano: cada cobro es la quincena (el 15) o el fin de mes (último día)
//     de un mes desde el siguiente a la entrega, y la suma tiene que dar exactamente el monto.
//   - Sin tope por cobro, pero el pago nunca queda negativo: lo que no alcanza en un corte se cobra en
//     el siguiente pago hasta que la deuda quede en 0 (PrestamosEnCortes).
//   - Los cobros cuya fecha ya llegó quedan fijos; los que vienen se pueden cambiar.
public class PrestamosService(RpsDbContext db)
{
    private const int LargoMaximoObservaciones = 500;

    private static int Orden(TipoCorte tipo) => tipo == TipoCorte.Quincena ? 0 : 1;

    public async Task<Resultado<List<PrestamoResponse>>> ListarAsync(Guid colaboradorId, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return PrestamoErrores.ColaboradorNoEncontrado(colaboradorId);

        return (await EstadoAsync(colaboradorId, ct))
            .OrderByDescending(p => p.Fecha).ThenByDescending(p => p.CreadoEn)
            .ToList();
    }

    public async Task<Resultado<PrestamoResponse>> ObtenerAsync(Guid colaboradorId, Guid prestamoId, CancellationToken ct)
    {
        var prestamo = (await EstadoAsync(colaboradorId, ct)).FirstOrDefault(p => p.Id == prestamoId);
        if (prestamo is null)
            return PrestamoErrores.NoEncontrado(prestamoId);
        return prestamo;
    }

    public async Task<Resultado<ResumenPrestamosResponse>> ObtenerResumenAsync(Guid colaboradorId, CancellationToken ct)
    {
        var colaborador = await db.ColaboradoresRps.AsNoTracking().FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return PrestamoErrores.ColaboradorNoEncontrado(colaboradorId);

        var hoy = HoraEcuador.Hoy;
        var primerMes = HoraEcuador.MesActual.AddMonths(1);
        var prestamos = await EstadoAsync(colaboradorId, ct);
        var cuotas = prestamos.SelectMany(p => p.Cuotas).ToList();

        // Próximo cobro: el siguiente pago con algo programado, más lo que haya quedado pendiente de
        // pagos anteriores (se cobra en ese mismo pago).
        var proximaFecha = cuotas.Where(c => c.FechaCobro > hoy).Select(c => (DateOnly?)c.FechaCobro).Min();
        var atrasado = cuotas.Where(c => c.FechaCobro <= hoy).Sum(c => c.Importe - c.Cobrado);
        var proximoImporte = proximaFecha is { } f
            ? cuotas.Where(c => c.FechaCobro == f).Sum(c => c.Importe - c.Cobrado) + atrasado
            : atrasado;

        return new ResumenPrestamosResponse(
            colaboradorId, colaborador.Activo,
            await FrecuenciaAsync(colaboradorId, primerMes, ct) == Frecuencia.DosPagos,
            primerMes,
            prestamos.Count(p => !p.Pagado),
            prestamos.Sum(p => p.Monto),
            prestamos.Sum(p => p.Cobrado),
            prestamos.Sum(p => p.Saldo),
            proximaFecha, proximoImporte);
    }

    public async Task<Resultado<PrestamoResponse>> RegistrarAsync(
        Guid colaboradorId, CrearPrestamoRequest request, Guid usuarioId, CancellationToken ct)
    {
        if (!MontoValido(request.Monto))
            return PrestamoErrores.MontoInvalido;

        var observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim();
        if (observaciones?.Length > LargoMaximoObservaciones)
            return PrestamoErrores.ObservacionesLargas;

        var fecha = request.Fecha ?? HoraEcuador.Hoy;

        // RC 04: se serializa por colaborador, igual que Anticipos, porque al guardar se recalculan
        // sus liquidaciones.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM rps.colaborador_rps WHERE id = {colaboradorId} FOR UPDATE", ct);

        var colaborador = await db.ColaboradoresRps.AsNoTracking().FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return PrestamoErrores.ColaboradorNoEncontrado(colaboradorId);
        if (!colaborador.Activo)
            return PrestamoErrores.ColaboradorDesactivado;

        var error = await ValidarPlanAsync(colaboradorId, fecha, request.Monto!.Value, request.Cuotas, [], ct);
        if (error is not null)
            return error;

        var prestamo = new Prestamo
        {
            ColaboradorId = colaboradorId,
            Monto = request.Monto.Value,
            Fecha = fecha,
            Observaciones = observaciones,
            CreadoPor = usuarioId
        };
        db.Prestamos.Add(prestamo);
        db.CuotasPrestamo.AddRange(request.Cuotas!.Select(c => NuevaCuota(prestamo.Id, c)));
        await db.SaveChangesAsync(ct);

        // Si ya hay liquidaciones generadas de esos meses, su pago tiene que reflejar el préstamo.
        await PrestamosEnCortes.RecalcularAsync(db, colaboradorId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await ObtenerAsync(colaboradorId, prestamo.Id, ct)).Valor!;
    }

    public async Task<Resultado<PrestamoResponse>> ActualizarAsync(
        Guid colaboradorId, Guid prestamoId, ActualizarPrestamoRequest request, CancellationToken ct)
    {
        if (!MontoValido(request.Monto))
            return PrestamoErrores.MontoInvalido;

        var observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim();
        if (observaciones?.Length > LargoMaximoObservaciones)
            return PrestamoErrores.ObservacionesLargas;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM rps.colaborador_rps WHERE id = {colaboradorId} FOR UPDATE", ct);

        var prestamo = await db.Prestamos.FirstOrDefaultAsync(p => p.Id == prestamoId && p.ColaboradorId == colaboradorId, ct);
        if (prestamo is null)
            return PrestamoErrores.NoEncontrado(prestamoId);

        var hoy = HoraEcuador.Hoy;
        var existentes = await db.CuotasPrestamo.Where(c => c.PrestamoId == prestamoId).ToListAsync(ct);
        var bloqueadas = existentes.Where(c => CuotaPrestamo.FechaDeCobro(c.Periodo, c.Corte) <= hoy).ToList();

        var error = await ValidarPlanAsync(colaboradorId, prestamo.Fecha, request.Monto!.Value, request.Cuotas, bloqueadas, ct);
        if (error is not null)
            return error;

        prestamo.Monto = request.Monto.Value;
        prestamo.Observaciones = observaciones;
        prestamo.ActualizadoEn = DateTime.UtcNow;

        // Las cuotas bloqueadas se quedan como están; las demás se reemplazan por las del pedido. En
        // dos pasos para no chocar con el índice único (préstamo, mes, corte).
        db.CuotasPrestamo.RemoveRange(existentes.Except(bloqueadas));
        await db.SaveChangesAsync(ct);

        db.CuotasPrestamo.AddRange(request.Cuotas!
            .Where(c => !bloqueadas.Any(b => b.Periodo == c.Periodo && b.Corte == c.Corte))
            .Select(c => NuevaCuota(prestamoId, c)));
        await db.SaveChangesAsync(ct);

        await PrestamosEnCortes.RecalcularAsync(db, colaboradorId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await ObtenerAsync(colaboradorId, prestamoId, ct)).Valor!;
    }

    public async Task<Resultado<bool>> EliminarAsync(Guid colaboradorId, Guid prestamoId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM rps.colaborador_rps WHERE id = {colaboradorId} FOR UPDATE", ct);

        var prestamo = await db.Prestamos.FirstOrDefaultAsync(p => p.Id == prestamoId && p.ColaboradorId == colaboradorId, ct);
        if (prestamo is null)
            return PrestamoErrores.NoEncontrado(prestamoId);

        var estado = (await EstadoAsync(colaboradorId, ct)).First(p => p.Id == prestamoId);
        if (estado.Cobrado > 0 || estado.Cuotas.Any(c => c.Bloqueada))
            return PrestamoErrores.TieneCobros;

        db.CuotasPrestamo.RemoveRange(await db.CuotasPrestamo.Where(c => c.PrestamoId == prestamoId).ToListAsync(ct));
        await db.SaveChangesAsync(ct);
        db.Prestamos.Remove(prestamo);
        await db.SaveChangesAsync(ct);

        await PrestamosEnCortes.RecalcularAsync(db, colaboradorId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return true;
    }

    // Valida el plan de pagos completo. `bloqueadas`: cuotas ya guardadas cuya fecha llegó; tienen
    // que venir iguales (mismo mes, corte e importe) y no se revalidan sus fechas.
    private async Task<Error?> ValidarPlanAsync(
        Guid colaboradorId, DateOnly fechaPrestamo, decimal monto, List<CuotaPrestamoInput>? cuotas,
        List<CuotaPrestamo> bloqueadas, CancellationToken ct)
    {
        if (cuotas is null || cuotas.Count == 0)
            return PrestamoErrores.CuotasRequeridas;

        foreach (var c in cuotas)
        {
            if (c.Periodo is not { Day: 1 } || c.Corte is null || !Enum.IsDefined(c.Corte.Value) || !MontoValido(c.Importe))
                return PrestamoErrores.CuotaInvalida;
        }

        var repetida = cuotas.GroupBy(c => (c.Periodo!.Value, c.Corte!.Value)).FirstOrDefault(g => g.Count() > 1);
        if (repetida is not null)
            return PrestamoErrores.CuotaRepetida(repetida.Key.Item1, repetida.Key.Item2);

        foreach (var b in bloqueadas)
        {
            if (!cuotas.Any(c => c.Periodo == b.Periodo && c.Corte == b.Corte && c.Importe == b.Importe))
                return PrestamoErrores.CuotaCobradaNoSeCambia;
        }

        var suma = cuotas.Sum(c => c.Importe!.Value);
        if (suma != monto)
            return PrestamoErrores.SumaNoCoincide(monto, suma);

        var hoy = HoraEcuador.Hoy;
        var primerMes = new DateOnly(fechaPrestamo.Year, fechaPrestamo.Month, 1).AddMonths(1);

        foreach (var c in cuotas.OrderBy(c => c.Periodo).ThenBy(c => Orden(c.Corte!.Value)))
        {
            var periodo = c.Periodo!.Value;
            var corte = c.Corte!.Value;
            if (bloqueadas.Any(b => b.Periodo == periodo && b.Corte == corte))
                continue;

            if (periodo < primerMes)
                return PrestamoErrores.AntesDelMesSiguiente(primerMes);

            var fechaCobro = CuotaPrestamo.FechaDeCobro(periodo, corte);
            if (fechaCobro <= hoy)
                return PrestamoErrores.FechaYaPaso(fechaCobro);

            // Una quincena de alguien que ese mes cobra solo a fin de mes nunca tendría corte.
            if (corte == TipoCorte.Quincena && await FrecuenciaAsync(colaboradorId, periodo, ct) != Frecuencia.DosPagos)
                return PrestamoErrores.CorteIncompatibleConFrecuencia(periodo);
        }

        return null;
    }

    private static bool MontoValido(decimal? monto) =>
        monto is { } m && m > 0 && decimal.Round(m, 2) == m;

    private static CuotaPrestamo NuevaCuota(Guid prestamoId, CuotaPrestamoInput c) => new()
    {
        PrestamoId = prestamoId,
        Periodo = c.Periodo!.Value,
        Corte = c.Corte!.Value,
        Importe = c.Importe!.Value
    };

    // Misma regla que LiquidacionesService y Anticipos: la frecuencia registrada más reciente que
    // rige en ese mes; sin ninguna, se paga solo a fin de mes.
    private async Task<Frecuencia> FrecuenciaAsync(Guid colaboradorId, DateOnly periodo, CancellationToken ct) =>
        await db.ColaboradoresFrecuencia.AsNoTracking()
            .Where(f => f.ColaboradorId == colaboradorId && f.VigenteDesdeMes <= periodo)
            .OrderByDescending(f => f.VigenteDesdeMes)
            .Select(f => (Frecuencia?)f.Frecuencia)
            .FirstOrDefaultAsync(ct) ?? Frecuencia.FinDeMes;

    // Todos los préstamos del colaborador con lo cobrado de cada cuota. Lo descontado en las
    // liquidaciones se reparte entre las cuotas en orden de fecha de cobro (las más antiguas primero),
    // que es el mismo orden en que PrestamosEnCortes las va cobrando.
    private async Task<List<PrestamoResponse>> EstadoAsync(Guid colaboradorId, CancellationToken ct)
    {
        var prestamos = await db.Prestamos.AsNoTracking()
            .Where(p => p.ColaboradorId == colaboradorId)
            .ToListAsync(ct);
        if (prestamos.Count == 0)
            return [];

        var ids = prestamos.Select(p => p.Id).ToList();
        var cuotas = await db.CuotasPrestamo.AsNoTracking()
            .Where(c => ids.Contains(c.PrestamoId))
            .ToListAsync(ct);

        var prestamoPorId = prestamos.ToDictionary(p => p.Id);
        var porRepartir = await PrestamosEnCortes.DescontadoAsync(db, colaboradorId, ct);
        var cobradoPorCuota = new Dictionary<Guid, decimal>();
        foreach (var c in cuotas
                     .OrderBy(c => c.Periodo).ThenBy(c => Orden(c.Corte))
                     .ThenBy(c => prestamoPorId[c.PrestamoId].Fecha).ThenBy(c => prestamoPorId[c.PrestamoId].CreadoEn)
                     .ThenBy(c => c.Id))
        {
            var cobrado = Math.Min(c.Importe, porRepartir);
            cobradoPorCuota[c.Id] = cobrado;
            porRepartir -= cobrado;
        }

        var hoy = HoraEcuador.Hoy;
        return prestamos.Select(p =>
        {
            var respuestas = cuotas
                .Where(c => c.PrestamoId == p.Id)
                .OrderBy(c => c.Periodo).ThenBy(c => Orden(c.Corte))
                .Select(c =>
                {
                    var fechaCobro = CuotaPrestamo.FechaDeCobro(c.Periodo, c.Corte);
                    return new CuotaPrestamoResponse(
                        c.Id, c.Periodo, c.Corte, fechaCobro, c.Importe, cobradoPorCuota.GetValueOrDefault(c.Id), fechaCobro <= hoy);
                })
                .ToList();
            var cobrado = respuestas.Sum(c => c.Cobrado);
            var saldo = p.Monto - cobrado;
            var proximo = respuestas.Where(c => c.FechaCobro > hoy).Select(c => (DateOnly?)c.FechaCobro).FirstOrDefault();

            return new PrestamoResponse(
                p.Id, p.ColaboradorId, p.Monto, p.Fecha, p.Observaciones,
                cobrado, saldo, saldo <= 0, proximo,
                respuestas, p.CreadoPor, p.CreadoEn, p.ActualizadoEn);
        }).ToList();
    }
}
