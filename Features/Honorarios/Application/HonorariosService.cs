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

        var colaboradores = new List<ColaboradorRps>();
        foreach (var candidato in candidatos)
            if (await ColaboradorEstado.CalcularParaPeriodoAsync(candidato, db, periodo, ct) == EstadoConfiguracion.Configurado)
                colaboradores.Add(candidato);

        if (colaboradores.Count == 0)
            return new GenerarHonorariosResponse(0, 0, 0);

        var colaboradorIds = colaboradores.Select(c => c.Id).ToList();
        var clasificacionPorColaborador = colaboradores.ToDictionary(c => c.Id, c => c.Clasificacion!.Value);

        // Lo que rige en el período en cada contrato: un solo importe (pago por contrato) o uno por
        // proyecto (pago por proyecto). Ver CondicionesVigentes.
        var condicionesVigentes = CondicionesVigentes.Seleccionar(
            await db.CondicionesEconomicas.AsNoTracking()
                .Where(c => colaboradorIds.Contains(c.ColaboradorId) && c.MesInicioVigencia <= periodo)
                .ToListAsync(ct),
            periodo);

        if (condicionesVigentes.Count == 0)
            return new GenerarHonorariosResponse(0, 0, 0);

        var asignaciones = await db.Asignaciones.AsNoTracking()
            .Where(a => colaboradorIds.Contains(a.ColaboradorId)
                && !a.EliminadaEnOrigen
                && a.FechaInicio <= finPeriodo
                && (a.FechaSalida == null || a.FechaSalida >= periodo))
            .ToListAsync(ct);

        var config = await db.ConfiguracionGlobal.AsNoTracking().FirstAsync(ct);

        // Los que ya existen se vuelven a contar: una adenda (o una salida en TMR) pudo cambiar los
        // días del período después del primer cálculo.
        var existentes = (await db.Honorarios
                .Where(h => h.Periodo == periodo && colaboradorIds.Contains(h.ColaboradorId))
                .ToListAsync(ct))
            .GroupBy(h => (h.ContratoId, h.ProyectoId))
            .ToDictionary(g => g.Key, g => g.First());

        var nuevos = new List<Honorario>();
        var omitidos = 0;
        var recalculados = 0;

        // Vigencia del contrato de cada condición, con adendas (RN 10-12): fin = la mayor entre la
        // fecha fin original y las adendas; null = indefinido.
        var contratoIds = condicionesVigentes.Select(c => c.ContratoId).Distinct().ToList();
        var contratos = await db.Contratos.AsNoTracking()
            .Where(c => contratoIds.Contains(c.Id))
            .Select(c => new { c.Id, c.FechaInicio, c.FechaFin })
            .ToListAsync(ct);
        var maxAdenda = (await db.Adendas.AsNoTracking()
                .Where(a => contratoIds.Contains(a.ContratoId))
                .ToListAsync(ct))
            .GroupBy(a => a.ContratoId)
            .ToDictionary(g => g.Key, g => g.Max(a => a.NuevaFechaFin));
        var vigenciaContrato = contratos.ToDictionary(c => c.Id, c =>
        {
            DateOnly? fin = c.FechaFin is null ? null
                : maxAdenda.TryGetValue(c.Id, out var adenda) && adenda > c.FechaFin ? adenda : c.FechaFin;
            return (Inicio: c.FechaInicio, Fin: fin);
        });

        // Días reconocidos de cada condición, siempre dentro de la vigencia de su contrato (si el
        // contrato empieza o termina a mitad de mes, se paga el proporcional):
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
                if (vigenciaContrato.TryGetValue(c.ContratoId, out var v))
                    dias.RemoveWhere(d => d < v.Inicio || (v.Fin is { } fin && d > fin));
                return dias;
            });

        foreach (var condicion in condicionesVigentes)
        {
            var diasReconocidos = diasPorCondicion[condicion.Id].Count;
            var proporcionalAcordado = Math.Round(
                condicion.ImporteMensual / diasReales * diasReconocidos, 2, MidpointRounding.AwayFromZero);
            var generaHonorario = diasReconocidos > 0 && proporcionalAcordado > 0;

            if (existentes.TryGetValue((condicion.ContratoId, condicion.ProyectoId), out var existente))
            {
                // Mismos días y misma condición: se deja tal cual (conserva los porcentajes con que
                // se calculó, RN 29).
                if (generaHonorario && existente.DiasReconocidos == diasReconocidos
                    && existente.CondicionEconomicaId == condicion.Id && existente.DiasReales == diasReales)
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

                // Cambiaron los días o la condición: se recalcula y se suelta de la liquidación para
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
