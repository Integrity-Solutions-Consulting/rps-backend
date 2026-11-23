using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Liquidaciones.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Application;

// Rol a pagar > factura del mes, días descontados y observaciones de cada colaborador.
// Las reglas de plazos están en PlazoFactura; este servicio solo registra. El efecto en la quincena
// lo aplica LiquidacionesService al calcular el período. No conoce HTTP: devuelve Resultado.
public class FacturacionService(RpsDbContext db)
{
    public async Task<Resultado<FacturacionPeriodoResponse>> ObtenerAsync(DateOnly? periodoPedido, CancellationToken ct)
    {
        if (periodoPedido is not { Day: 1 } periodo)
            return LiquidacionErrores.PeriodoInvalido;

        var ahora = HoraEcuador.Ahora;

        var liquidaciones = await db.Liquidaciones.AsNoTracking()
            .Where(l => l.Periodo == periodo)
            .Select(l => new { l.Id, l.ColaboradorId })
            .ToListAsync(ct);
        var idsLiquidacion = liquidaciones.Select(l => l.Id).ToList();
        var conQuincena = (await db.Cortes.AsNoTracking()
                .Where(c => idsLiquidacion.Contains(c.LiquidacionId) && c.Tipo == TipoCorte.Quincena)
                .Select(c => c.LiquidacionId)
                .ToListAsync(ct))
            .ToHashSet();
        var colaboradoresConQuincena = liquidaciones.Where(l => conQuincena.Contains(l.Id)).Select(l => l.ColaboradorId).ToHashSet();

        var controles = await db.ControlesMensuales.AsNoTracking()
            .Where(c => c.Periodo == periodo)
            .ToDictionaryAsync(c => c.ColaboradorId, ct);

        var colaboradorIds = liquidaciones.Select(l => l.ColaboradorId).Concat(controles.Keys).Distinct().ToList();
        var frecuencias = await FrecuenciasAsync(colaboradorIds, periodo, ct);
        var nombres = await NombresUsuariosAsync(controles.Values, ct);

        var filas = colaboradorIds.Select(id => AFila(
                id, periodo, controles.GetValueOrDefault(id), frecuencias.GetValueOrDefault(id, Frecuencia.FinDeMes),
                colaboradoresConQuincena.Contains(id), nombres, ahora))
            .ToList();

        return new FacturacionPeriodoResponse(
            periodo, Fase(periodo, ahora), PlazoFactura.CierreATiempo(periodo), PlazoFactura.CierreGracia(periodo),
            filas.Count(f => f.QuincenaPorAjustar), filas);
    }

    public async Task<Resultado<ControlMensualResponse>> MarcarFacturaAsync(
        Guid colaboradorId, MarcarFacturaRequest request, Guid usuarioId, CancellationToken ct)
    {
        if (request.Periodo is not { Day: 1 } periodo)
            return LiquidacionErrores.PeriodoInvalido;
        if (!PlazoFactura.Aplica(periodo))
            return FacturacionErrores.NoAplica(periodo);
        if (HoraEcuador.Ahora >= PlazoFactura.CierreGracia(periodo))
            return FacturacionErrores.PlazoCerrado;
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return FacturacionErrores.ColaboradorNoEncontrado(colaboradorId);

        var control = await ObtenerOCrearAsync(colaboradorId, periodo, ct);
        // Marcar dos veces no cambia la hora de la primera (doble clic, dos personas a la vez).
        if (control.FacturaRecibidaEn is null)
        {
            control.FacturaRecibidaEn = DateTime.UtcNow;
            control.FacturaMarcadaPorUsuarioId = usuarioId;
            await db.SaveChangesAsync(ct);
        }
        return await FilaAsync(control, ct);
    }

    public async Task<Resultado<ControlMensualResponse>> QuitarFacturaAsync(
        Guid colaboradorId, DateOnly? periodoPedido, CancellationToken ct)
    {
        if (periodoPedido is not { Day: 1 } periodo)
            return LiquidacionErrores.PeriodoInvalido;

        var control = await db.ControlesMensuales.FirstOrDefaultAsync(c => c.ColaboradorId == colaboradorId && c.Periodo == periodo, ct);
        if (control?.FacturaRecibidaEn is null)
            return FacturacionErrores.SinFactura;

        var ahora = HoraEcuador.Ahora;
        if (ahora >= PlazoFactura.CierreGracia(periodo))
            return FacturacionErrores.PlazoCerrado;
        // Una factura a tiempo, quitada después del 7, ya no se podría volver a marcar a tiempo:
        // se perdería la quincena por un clic. Solo se corrige mientras el plazo sigue abierto.
        if (PlazoFactura.EsATiempo(periodo, control.FacturaRecibidaEn) && ahora >= PlazoFactura.CierreATiempo(periodo))
            return FacturacionErrores.ATiempoNoSeQuita;

        control.FacturaRecibidaEn = null;
        control.FacturaMarcadaPorUsuarioId = null;
        await db.SaveChangesAsync(ct);
        return await FilaAsync(control, ct);
    }

    // Días descontados (solo se registran; no cambian el valor a pagar) y observaciones del mes.
    public async Task<Resultado<ControlMensualResponse>> GuardarDiasAsync(
        Guid colaboradorId, GuardarDiasRequest request, Guid usuarioId, CancellationToken ct)
    {
        if (request.Periodo is not { Day: 1 } periodo)
            return LiquidacionErrores.PeriodoInvalido;

        var diasDelMes = periodo.AddMonths(1).AddDays(-1).Day;
        var dias = request.DiasDescontados ?? 0;
        if (dias < 0 || dias > diasDelMes)
            return FacturacionErrores.DiasInvalidos(diasDelMes);

        var observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim();
        if (observaciones is { Length: > ControlMensual.LargoMaximoObservaciones })
            return FacturacionErrores.ObservacionesMuyLargas;

        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == colaboradorId, ct))
            return FacturacionErrores.ColaboradorNoEncontrado(colaboradorId);

        var control = await ObtenerOCrearAsync(colaboradorId, periodo, ct);
        if (control.DiasDescontados != dias || control.Observaciones != observaciones)
        {
            control.DiasDescontados = dias;
            control.Observaciones = observaciones;
            control.DiasActualizadoEn = DateTime.UtcNow;
            control.DiasActualizadoPorUsuarioId = usuarioId;
            await db.SaveChangesAsync(ct);
        }
        return await FilaAsync(control, ct);
    }

    private static FasePlazoFactura Fase(DateOnly periodo, DateTime ahora) =>
        !PlazoFactura.Aplica(periodo) ? FasePlazoFactura.NoAplica
        : ahora < PlazoFactura.CierreATiempo(periodo) ? FasePlazoFactura.Abierto
        : ahora < PlazoFactura.CierreGracia(periodo) ? FasePlazoFactura.Gracia
        : FasePlazoFactura.Cerrado;

    private async Task<ControlMensual> ObtenerOCrearAsync(Guid colaboradorId, DateOnly periodo, CancellationToken ct)
    {
        var control = await db.ControlesMensuales.FirstOrDefaultAsync(c => c.ColaboradorId == colaboradorId && c.Periodo == periodo, ct);
        if (control is null)
        {
            control = new ControlMensual { ColaboradorId = colaboradorId, Periodo = periodo };
            db.ControlesMensuales.Add(control);
        }
        return control;
    }

    private async Task<ControlMensualResponse> FilaAsync(ControlMensual control, CancellationToken ct)
    {
        var periodo = control.Periodo;
        var frecuencias = await FrecuenciasAsync([control.ColaboradorId], periodo, ct);
        var tieneQuincena = await db.Liquidaciones.AsNoTracking()
            .Where(l => l.Periodo == periodo && l.ColaboradorId == control.ColaboradorId)
            .Join(db.Cortes.AsNoTracking().Where(c => c.Tipo == TipoCorte.Quincena), l => l.Id, c => c.LiquidacionId, (l, c) => c.Id)
            .AnyAsync(ct);
        var nombres = await NombresUsuariosAsync([control], ct);
        return AFila(control.ColaboradorId, periodo, control, frecuencias.GetValueOrDefault(control.ColaboradorId, Frecuencia.FinDeMes),
            tieneQuincena, nombres, HoraEcuador.Ahora);
    }

    private static ControlMensualResponse AFila(
        Guid colaboradorId, DateOnly periodo, ControlMensual? control, Frecuencia frecuencia, bool tieneQuincena,
        IReadOnlyDictionary<Guid, string> nombres, DateTime ahora)
    {
        var recibida = control?.FacturaRecibidaEn;
        var estado = PlazoFactura.Estado(periodo, recibida, ahora);
        var antesDeGracia = ahora < PlazoFactura.CierreGracia(periodo);
        var puedeMarcar = PlazoFactura.Aplica(periodo) && antesDeGracia && recibida is null;
        var puedeQuitar = recibida is not null && antesDeGracia
            && !(estado == EstadoFactura.ATiempo && ahora >= PlazoFactura.CierreATiempo(periodo));

        return new ControlMensualResponse(
            colaboradorId, frecuencia, estado, recibida,
            control?.FacturaMarcadaPorUsuarioId is { } m ? nombres.GetValueOrDefault(m) : null,
            puedeMarcar, puedeQuitar,
            control?.DiasDescontados ?? 0, control?.Observaciones, control?.DiasActualizadoEn,
            control?.DiasActualizadoPorUsuarioId is { } d ? nombres.GetValueOrDefault(d) : null,
            tieneQuincena && PlazoFactura.SinQuincena(periodo, recibida, ahora));
    }

    // Misma regla que LiquidacionesService: la frecuencia vigente más reciente que empieza hasta ese mes.
    private async Task<Dictionary<Guid, Frecuencia>> FrecuenciasAsync(List<Guid> colaboradorIds, DateOnly periodo, CancellationToken ct) =>
        (await db.ColaboradoresFrecuencia.AsNoTracking()
            .Where(f => colaboradorIds.Contains(f.ColaboradorId) && f.VigenteDesdeMes <= periodo)
            .ToListAsync(ct))
        .GroupBy(f => f.ColaboradorId)
        .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.VigenteDesdeMes).First().Frecuencia);

    private async Task<Dictionary<Guid, string>> NombresUsuariosAsync(IEnumerable<ControlMensual> controles, CancellationToken ct)
    {
        var ids = controles
            .SelectMany(c => new[] { c.FacturaMarcadaPorUsuarioId, c.DiasActualizadoPorUsuarioId })
            .Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();
        return await db.Usuarios.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nombre, ct);
    }
}

public static class FacturacionErrores
{
    public static Error NoAplica(DateOnly periodo) =>
        Error.Validacion("Facturacion.NoAplica", "periodo",
            $"El control de facturas empieza en {PlazoFactura.InicioControl:MM/yyyy}; {periodo:MM/yyyy} se paga como antes.");

    public static readonly Error PlazoCerrado =
        Error.Conflicto("Facturacion.PlazoCerrado",
            $"Ya pasaron los días de gracia (hasta el día {PlazoFactura.DiaFinGracia}, 23:59): la factura de este mes ya no se puede marcar ni quitar.");

    public static readonly Error SinFactura =
        Error.Conflicto("Facturacion.SinFactura", "Este colaborador no tiene una factura marcada en ese mes.");

    public static readonly Error ATiempoNoSeQuita =
        Error.Conflicto("Facturacion.ATiempoNoSeQuita",
            $"La factura llegó a tiempo y el plazo (día {PlazoFactura.DiaLimite}) ya venció: si se quita, se perdería la quincena. Ya no se puede quitar.");

    public static Error DiasInvalidos(int diasDelMes) =>
        Error.Validacion("Facturacion.DiasInvalidos", "diasDescontados", $"Los días a descontar van de 0 a {diasDelMes}.");

    public static readonly Error ObservacionesMuyLargas =
        Error.Validacion("Facturacion.ObservacionesMuyLargas", "observaciones",
            $"Las observaciones admiten hasta {ControlMensual.LargoMaximoObservaciones} caracteres.");

    public static Error ColaboradorNoEncontrado(Guid id) =>
        Error.NoEncontrado("Facturacion.ColaboradorNoEncontrado", $"No existe un colaborador con id '{id}'.");
}
