using Microsoft.EntityFrameworkCore;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Notificaciones.Application;

// Bandeja de notificaciones de la cabecera del frontend (campana). No guarda nada: cada vez arma
// la lista leyendo las tablas de los demás módulos. Los campos coinciden con
// rps-frontend/src/app/features/notificaciones/models/notificacion.models.ts.

public record NotificacionResponse(
    string Id,
    // Configuracion | Contrato | Documento | Liquidacion | Tmr | Anticipo
    string Tipo,
    // Critica | Advertencia | Info
    string Severidad,
    Guid? ColaboradorId,
    string? TmrColaboradorId,
    // Hasta la integración con TMR el backend no conoce nombres: va "ID TMR x" y el frontend lo reemplaza.
    string? Colaborador,
    string Titulo,
    string Mensaje,
    DateTime Fecha,
    // Pantalla del frontend donde se resuelve.
    string Ruta,
    Dictionary<string, string>? Query);

public static class NotificacionErrores
{
    public static readonly Error PeriodoInvalido =
        Error.Validacion("Notificacion.PeriodoInvalido", "periodo", "Debe tener formato yyyy-MM.");
}

public class NotificacionesService(RpsDbContext db)
{
    // Plazo de "contrato por vencer" (RN 13). La configuración todavía no guarda los días de alerta.
    private const int DiasAlerta = 30;

    public async Task<Resultado<List<NotificacionResponse>>> ListarAsync(string? periodoTexto, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        DateOnly periodo;
        if (string.IsNullOrWhiteSpace(periodoTexto))
            periodo = new DateOnly(hoy.Year, hoy.Month, 1);
        else if (!PeriodoTexto.TryParse(periodoTexto, out periodo))
            return NotificacionErrores.PeriodoInvalido;
        var mes = PeriodoTexto.Formato(periodo);

        var lista = new List<NotificacionResponse>();
        void Agregar(string id, string tipo, string severidad, Guid? colaboradorId, string? tmrId, string titulo, string mensaje,
            string ruta, Dictionary<string, string>? query = null) =>
            lista.Add(new NotificacionResponse(id, tipo, severidad, colaboradorId, tmrId, tmrId is null ? null : $"ID TMR {tmrId}",
                titulo, mensaje, DateTime.UtcNow, ruta, query));

        var colaboradores = await db.ColaboradoresRps.AsNoTracking().Where(c => c.Activo).ToListAsync(ct);
        var ids = colaboradores.Select(c => c.Id).ToList();
        var conFrecuencia = (await db.ColaboradoresFrecuencia.AsNoTracking()
            .Where(f => ids.Contains(f.ColaboradorId) && f.VigenteDesdeMes <= periodo)
            .Select(f => f.ColaboradorId).ToListAsync(ct)).ToHashSet();
        var conCuenta = (await db.CuentasBancarias.AsNoTracking()
            .Where(c => ids.Contains(c.ColaboradorId) && c.Activa)
            .Select(c => c.ColaboradorId).ToListAsync(ct)).ToHashSet();

        // 1. Configuración incompleta del colaborador (RN 06).
        foreach (var c in colaboradores)
        {
            var faltan = new List<string>();
            if (c.Clasificacion is null) faltan.Add("tipo de servicio");
            if (!conFrecuencia.Contains(c.Id)) faltan.Add("frecuencia de pago");
            if (!conCuenta.Contains(c.Id)) faltan.Add("cuenta bancaria");
            if (faltan.Count == 0) continue;
            Agregar($"config-{c.Id}", "Configuracion",
                faltan.Contains("tipo de servicio") || faltan.Contains("cuenta bancaria") ? "Critica" : "Advertencia",
                c.Id, c.TmrColaboradorId, "Configuración incompleta",
                $"Falta {string.Join(", ", faltan)}. Sin eso no se puede calcular o exportar su pago.",
                $"/colaboradores/{c.Id}", new() { ["tab"] = faltan.Count == 1 && faltan[0] == "cuenta bancaria" ? "bancarios" : "condiciones" });
        }

        // 2. Asignaciones activas: sin honorario (condición económica) o sin contrato, y contratos por vencer.
        var finPeriodo = periodo.AddMonths(1).AddDays(-1);
        var asignaciones = await db.Asignaciones.AsNoTracking()
            .Where(a => ids.Contains(a.ColaboradorId) && a.FechaInicio <= finPeriodo && (a.FechaSalida == null || a.FechaSalida >= hoy))
            .Join(db.Proyectos.AsNoTracking(), a => a.ProyectoId, p => p.Id, (a, p) => new { a.ColaboradorId, a.ProyectoId, Proyecto = p.Nombre })
            .ToListAsync(ct);
        var condiciones = await db.CondicionesEconomicas.AsNoTracking()
            .Where(c => ids.Contains(c.ColaboradorId) && c.MesInicioVigencia <= periodo)
            .Select(c => new { c.ColaboradorId, c.ProyectoId }).ToListAsync(ct);
        var contratos = await db.Contratos.AsNoTracking().Where(c => ids.Contains(c.ColaboradorId)).ToListAsync(ct);
        var contratoIds = contratos.Select(c => c.Id).ToList();
        var maxAdenda = await db.Adendas.AsNoTracking()
            .Where(a => contratoIds.Contains(a.ContratoId))
            .GroupBy(a => a.ContratoId)
            .Select(g => new { ContratoId = g.Key, Fin = g.Max(a => a.NuevaFechaFin) })
            .ToDictionaryAsync(x => x.ContratoId, x => x.Fin, ct);
        var tmrPorId = colaboradores.ToDictionary(c => c.Id, c => c.TmrColaboradorId);

        foreach (var a in asignaciones)
        {
            var tmrId = tmrPorId[a.ColaboradorId];
            if (!condiciones.Any(c => c.ColaboradorId == a.ColaboradorId && c.ProyectoId == a.ProyectoId))
                Agregar($"honorario-{a.ColaboradorId}-{a.ProyectoId}", "Configuracion", "Advertencia", a.ColaboradorId, tmrId,
                    "Proyecto sin honorario", $"{a.Proyecto} está asignado pero no tiene honorario para {mes}.",
                    $"/colaboradores/{a.ColaboradorId}", new() { ["tab"] = "condiciones" });

            var contrato = contratos
                .Where(c => c.ColaboradorId == a.ColaboradorId && c.ProyectoId == a.ProyectoId)
                .Select(c => new { c.Id, Fin = maxAdenda.TryGetValue(c.Id, out var f) && f > c.FechaFin ? f : c.FechaFin })
                .OrderByDescending(c => c.Fin)
                .FirstOrDefault();
            if (contrato is null)
            {
                Agregar($"sin-contrato-{a.ColaboradorId}-{a.ProyectoId}", "Contrato", "Advertencia", a.ColaboradorId, tmrId,
                    "Proyecto sin contrato", $"{a.Proyecto} no tiene contrato registrado.", "/contratos", new() { ["colaborador"] = tmrId });
                continue;
            }
            var dias = contrato.Fin.DayNumber - hoy.DayNumber;
            if (dias < 0)
                Agregar($"contrato-{contrato.Id}", "Contrato", "Critica", a.ColaboradorId, tmrId, "Contrato vencido con asignación activa",
                    $"{a.Proyecto}: venció el {contrato.Fin:yyyy-MM-dd}. No bloquea el cálculo; revisar si corresponde una adenda (RN 15).",
                    "/contratos", new() { ["contrato"] = contrato.Id.ToString() });
            else if (dias <= DiasAlerta)
                Agregar($"contrato-{contrato.Id}", "Contrato", "Advertencia", a.ColaboradorId, tmrId, "Contrato por vencer",
                    $"{a.Proyecto}: vence el {contrato.Fin:yyyy-MM-dd} (en {dias} días).",
                    "/contratos", new() { ["contrato"] = contrato.Id.ToString() });
        }

        // 3. Liquidaciones del mes sin documento tributario (RN 35) o sin revisar (RN 49).
        var liquidaciones = await db.Liquidaciones.AsNoTracking().Where(l => l.Periodo == periodo).ToListAsync(ct);
        var conDocumento = (await db.DocumentosMensuales.AsNoTracking()
            .Where(d => d.Periodo == periodo).Select(d => d.ColaboradorId).ToListAsync(ct)).ToHashSet();
        var tmrTodos = await db.ColaboradoresRps.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.TmrColaboradorId, ct);
        foreach (var l in liquidaciones)
        {
            var tmrId = tmrTodos.GetValueOrDefault(l.ColaboradorId);
            if (!conDocumento.Contains(l.ColaboradorId))
                Agregar($"documento-{l.Id}", "Documento", "Info", l.ColaboradorId, tmrId, "Documento tributario pendiente",
                    $"Falta el documento de {mes}. No impide exportar (RN 35).",
                    "/documentos", new() { ["periodo"] = mes, ["colaborador"] = l.ColaboradorId.ToString() });
            if (!l.Revisado)
                Agregar($"revision-{l.Id}", "Liquidacion", "Info", l.ColaboradorId, tmrId, "Liquidación sin revisar",
                    $"La liquidación de {mes} no está marcada como \"Revisado para envío\".",
                    "/liquidaciones", new() { ["periodo"] = mes, ["liquidacion"] = l.Id.ToString() });
        }

        return lista;
    }
}
