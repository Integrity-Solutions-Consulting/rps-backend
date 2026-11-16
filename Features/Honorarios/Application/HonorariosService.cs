using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Colaboradores.Application;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Honorarios.Domain;
using rps_backend.Features.Proyectos.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

// Reglas de negocio de Honorarios. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
public class HonorariosService(RpsDbContext db)
{
    // Un honorario por contrato (pago por contrato) o por proyecto del contrato (pago por proyecto)
    // con al menos un día reconocido en el período (RN 17-25). Idempotente: se omite si ya existe.
    public async Task<Resultado<GenerarHonorariosResponse>> GenerarAsync(GenerarHonorariosRequest request, CancellationToken ct)
    {
        if (request.Periodo is not { Day: 1 } periodo)
            return HonorarioErrores.PeriodoInvalido;

        var finPeriodo = periodo.AddMonths(1).AddDays(-1);
        var diasReales = finPeriodo.Day;

        // Activos e inactivos: quien salió de TMR a mitad de mes (y por eso quedó inactivo en RPS)
        // cobra los días que trabajó. Los días salen de las fechas de sus asignaciones, así que a
        // quien ya no trabajó en el período no se le genera nada (diasReconocidos == 0).
        // Configurado se evalúa para el período, no para hoy: el contrato pudo terminar ese mes.
        var candidatos = await db.ColaboradoresRps.AsNoTracking()
            .Where(c => c.Clasificacion != null)
            .ToListAsync(ct);

        // También quien ya tenía honorarios en el período aunque hoy no esté configurado: se le
        // vuelven a contar los días con lo que rige (si ya no le corresponde nada, se retiran) en vez
        // de dejar un cálculo viejo.
        var conHonorariosEnPeriodo = (await db.Honorarios.AsNoTracking()
                .Where(h => h.Periodo == periodo).Select(h => h.ColaboradorId).Distinct().ToListAsync(ct))
            .ToHashSet();

        var colaboradores = new List<ColaboradorRps>();
        foreach (var candidato in candidatos)
            if (conHonorariosEnPeriodo.Contains(candidato.Id)
                || await ColaboradorEstado.CalcularParaPeriodoAsync(candidato, db, periodo, ct) == EstadoConfiguracion.Configurado)
                colaboradores.Add(candidato);

        if (colaboradores.Count == 0)
            return new GenerarHonorariosResponse(0, 0, 0);

        var colaboradorIds = colaboradores.Select(c => c.Id).ToList();
        var clasificacionPorColaborador = colaboradores.ToDictionary(c => c.Id, c => c.Clasificacion!.Value);

        // Lo que rige cada día del período en cada contrato: un solo importe (pago por contrato) o uno
        // por proyecto (pago por proyecto). Si cambia a mitad de mes, cada condición se lleva sus
        // días (un honorario por tramo). Ver CondicionesVigentes.
        var tramos = CondicionesVigentes.TramosDelPeriodo(
            await db.CondicionesEconomicas.AsNoTracking()
                .Where(c => colaboradorIds.Contains(c.ColaboradorId) && c.FechaInicioVigencia <= finPeriodo)
                .ToListAsync(ct),
            periodo, finPeriodo);
        var condicionesVigentes = tramos.Keys.ToList();

        var asignaciones = await db.Asignaciones.AsNoTracking()
            .Where(a => colaboradorIds.Contains(a.ColaboradorId)
                && !a.EliminadaEnOrigen
                && a.FechaInicio <= finPeriodo
                && (a.FechaSalida == null || a.FechaSalida >= periodo))
            .ToListAsync(ct);

        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);

        // Los que ya existen se vuelven a contar: una adenda (o una salida en TMR) pudo cambiar los
        // días o el monto del período después del primer cálculo.
        var existentes = (await db.Honorarios
                .Where(h => h.Periodo == periodo && colaboradorIds.Contains(h.ColaboradorId))
                .ToListAsync(ct))
            .GroupBy(h => (h.ContratoId, h.ProyectoId, h.CondicionEconomicaId))
            .ToDictionary(g => g.Key, g => g.First());

        var nuevos = new List<Honorario>();
        var omitidos = 0;
        var recalculados = 0;

        // Vigencia del contrato de cada condición, con adendas (RN 10-12) y terminación: fin = la
        // mayor entre la fecha fin original y las adendas (null = indefinido), pero nunca después del
        // día en que se terminó por desactivación (ese día sí se paga).
        var contratoIds = condicionesVigentes.Select(c => c.ContratoId).Distinct().ToList();
        var contratos = await db.Contratos.AsNoTracking()
            .Where(c => contratoIds.Contains(c.Id))
            .Select(c => new { c.Id, c.FechaInicio, c.FechaFin, c.FechaTerminacion })
            .ToListAsync(ct);
        var maxAdenda = (await db.Adendas.AsNoTracking()
                .Where(a => contratoIds.Contains(a.ContratoId))
                .ToListAsync(ct))
            .GroupBy(a => a.ContratoId)
            .ToDictionary(g => g.Key, g => g.Max(a => a.NuevaFechaFin));
        var vigenciaContrato = contratos.ToDictionary(c => c.Id, c => (
            Inicio: c.FechaInicio,
            Fin: Contratos.Domain.VigenciaContrato.FinEfectivo(c.FechaFin, maxAdenda.GetValueOrDefault(c.Id), c.FechaTerminacion)));

        // Días reconocidos de cada condición: los de su tramo, siempre dentro de la vigencia de su
        // contrato (si el contrato empieza o termina a mitad de mes, se paga el proporcional):
        //  - pago por contrato: todos los días del contrato, sin importar los proyectos;
        //  - pago por proyecto: solo los días de la asignación en ese proyecto (desde que entra
        //    hasta que sale), aunque el contrato siga.
        var diasPorCondicion = condicionesVigentes.ToDictionary(
            c => c.Id,
            c =>
            {
                var dias = c.PorContrato
                    ? Rango(periodo, finPeriodo)
                    : DiasDeAsignaciones(
                        asignaciones.Where(a => a.ColaboradorId == c.ColaboradorId && a.ProyectoId == c.ProyectoId), periodo, finPeriodo);
                dias.IntersectWith(tramos[c]);
                if (vigenciaContrato.TryGetValue(c.ContratoId, out var v))
                    dias.RemoveWhere(d => d < v.Inicio || (v.Fin is { } fin && d > fin));
                return dias;
            });

        // Honorarios ya generados cuyo tramo ya no rige (ej. se reemplazó la condición): se retiran
        // y LiquidacionesService recalcula su liquidación porque el total ya no cuadra.
        var vigentesIds = condicionesVigentes.Select(c => c.Id).ToHashSet();
        foreach (var huerfano in existentes.Values.Where(h => !vigentesIds.Contains(h.CondicionEconomicaId)))
        {
            db.Honorarios.Remove(huerfano);
            recalculados++;
        }

        foreach (var condicion in condicionesVigentes)
        {
            var diasReconocidos = diasPorCondicion[condicion.Id].Count;
            var proporcionalAcordado = Math.Round(
                condicion.ImporteMensual / diasReales * diasReconocidos, 2, MidpointRounding.AwayFromZero);
            var generaHonorario = diasReconocidos > 0 && proporcionalAcordado > 0;

            if (existentes.TryGetValue((condicion.ContratoId, condicion.ProyectoId, condicion.Id), out var existente))
            {
                // Mismos días, mismo monto y misma modalidad en el mismo tramo: se deja tal cual
                // (conserva los porcentajes con que se calculó, RN 29). Si se corrigió el monto de la
                // condición (adenda corregida o reemplazada), el proporcional ya no coincide.
                if (generaHonorario && existente.DiasReconocidos == diasReconocidos && existente.DiasReales == diasReales
                    && existente.ProporcionalAcordado == proporcionalAcordado && existente.Modalidad == condicion.Modalidad)
                {
                    omitidos++;
                    continue;
                }

                // Ya no le corresponden días en el período: se retira. LiquidacionesService
                // recalcula su liquidación porque el total ya no cuadra.
                if (!generaHonorario)
                {
                    db.Honorarios.Remove(existente);
                    recalculados++;
                    continue;
                }

                // Cambiaron los días o el monto del tramo: se recalcula y se suelta de la liquidación para
                // que el siguiente paso la vuelva a consolidar con el valor nuevo.
                Aplicar(existente, condicion, diasReconocidos, proporcionalAcordado);
                existente.LiquidacionId = null;
                recalculados++;
                continue;
            }

            // Pago por proyecto: un día sin asignación en ese proyecto no genera honorario (RN 24).
            // Pago por contrato: solo cuenta la vigencia del contrato.
            if (!generaHonorario)
                continue;

            var nuevo = new Honorario
            {
                ColaboradorId = condicion.ColaboradorId,
                ProyectoId = condicion.ProyectoId,
                ContratoId = condicion.ContratoId,
                Periodo = periodo,
            };
            Aplicar(nuevo, condicion, diasReconocidos, proporcionalAcordado);
            nuevos.Add(nuevo);
        }

        void Aplicar(Honorario h, CondicionEconomica condicion, int diasReconocidos, decimal proporcionalAcordado)
        {
            var pctRetencionIva = clasificacionPorColaborador[condicion.ColaboradorId] == Clasificacion.Profesional
                ? config.PorcentajeRetencionIvaProfesional
                : config.PorcentajeRetencionIvaJunior;

            var tributos = CalculoTributario.Calcular(
                proporcionalAcordado, condicion.Modalidad, config.PorcentajeIva, config.PorcentajeRetencionFuente, pctRetencionIva);

            h.CondicionEconomicaId = condicion.Id;
            h.DiasReales = diasReales;
            h.DiasReconocidos = diasReconocidos;
            h.Modalidad = condicion.Modalidad;
            h.PorcentajeIva = config.PorcentajeIva;
            h.PorcentajeRetencionFuente = config.PorcentajeRetencionFuente;
            h.PorcentajeRetencionIva = pctRetencionIva;
            h.ProporcionalAcordado = proporcionalAcordado;
            h.Subtotal = tributos.Subtotal;
            h.Iva = tributos.Iva;
            h.TotalDocumento = tributos.TotalDocumento;
            h.RetencionFuente = tributos.RetencionFuente;
            h.RetencionIva = tributos.RetencionIva;
            h.TotalRetenciones = tributos.TotalRetenciones;
            h.Neto = tributos.Neto;
        }

        db.Honorarios.AddRange(nuevos);
        if (nuevos.Count > 0 || recalculados > 0)
            await db.SaveChangesAsync(ct);

        return new GenerarHonorariosResponse(nuevos.Count, omitidos, recalculados);
    }

    public async Task<List<HonorarioResponse>> ListarAsync(DateOnly? periodo, Guid? colaboradorId, CancellationToken ct)
    {
        var query = db.Honorarios.AsNoTracking();

        if (periodo is not null)
            query = query.Where(h => h.Periodo == periodo);

        if (colaboradorId is not null)
            query = query.Where(h => h.ColaboradorId == colaboradorId);

        return await query
            .OrderByDescending(h => h.Periodo)
            .Select(h => new HonorarioResponse(
                h.Id, h.ColaboradorId, h.ProyectoId, h.ContratoId, h.Periodo, h.DiasReales, h.DiasReconocidos, h.Modalidad,
                h.ProporcionalAcordado, h.Subtotal, h.Iva, h.TotalDocumento, h.RetencionFuente, h.RetencionIva,
                h.TotalRetenciones, h.Neto, h.LiquidacionId, h.CreadoEn))
            .ToListAsync(ct);
    }

    public async Task<Resultado<HonorarioResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var honorario = await db.Honorarios.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id, ct);
        if (honorario is null)
            return HonorarioErrores.NoEncontrado(id);

        return AResponse(honorario);
    }

    // Solo si todavía no se consolidó en una Liquidación: una vez agrupado, se corrige
    // recalculando el período, no borrando honorarios sueltos (RN 48, RN 51).
    public async Task<Resultado<bool>> EliminarAsync(Guid id, CancellationToken ct)
    {
        var honorario = await db.Honorarios.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (honorario is null)
            return HonorarioErrores.NoEncontrado(id);

        if (honorario.LiquidacionId is not null)
            return HonorarioErrores.YaConsolidado;

        db.Honorarios.Remove(honorario);
        await db.SaveChangesAsync(ct);

        return true;
    }

    private static HashSet<DateOnly> Rango(DateOnly desde, DateOnly hasta)
    {
        var dias = new HashSet<DateOnly>();
        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
            dias.Add(fecha);
        return dias;
    }

    private static HashSet<DateOnly> DiasDeAsignaciones(IEnumerable<Asignacion> asignaciones, DateOnly periodo, DateOnly finPeriodo)
    {
        var dias = new HashSet<DateOnly>();
        foreach (var asignacion in asignaciones)
        {
            var inicio = asignacion.FechaInicio > periodo ? asignacion.FechaInicio : periodo;
            var finAsignacion = asignacion.FechaSalida ?? finPeriodo;
            var fin = finAsignacion < finPeriodo ? finAsignacion : finPeriodo;
            dias.UnionWith(Rango(inicio, fin));
        }
        return dias;
    }

    private static HonorarioResponse AResponse(Honorario h) => new(
        h.Id, h.ColaboradorId, h.ProyectoId, h.ContratoId, h.Periodo, h.DiasReales, h.DiasReconocidos, h.Modalidad,
        h.ProporcionalAcordado, h.Subtotal, h.Iva, h.TotalDocumento, h.RetencionFuente, h.RetencionIva,
        h.TotalRetenciones, h.Neto, h.LiquidacionId, h.CreadoEn);
}
