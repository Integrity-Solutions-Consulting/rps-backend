using Microsoft.EntityFrameworkCore;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Historial.Application;

// Historial por colaborador (sección 12, "Historial por colaborador"; RN 48). No guarda nada:
// junta los registros de los demás módulos, del más reciente al más antiguo.
// Los campos coinciden con EventoHistorial en rps-frontend/src/app/features/colaboradores/models/colaborador.models.ts.

public record EnlaceEvento(string Texto, string Ruta, Dictionary<string, string>? Query);

public record EventoHistorialResponse(
    DateTime Fecha,
    // Vinculación | Frecuencia | Contrato | Adenda | Honorario | Liquidación | Anticipo
    string Tipo,
    string Descripcion,
    // Null cuando el módulo de origen todavía no captura quién hizo el cambio (RC 03 parcial:
    // por ahora solo Liquidación.RevisadoPor y Anticipo.CreadoPor tienen autor real). No se
    // inventa "Contabilidad" para lo que no se sabe (RC 05).
    string? Usuario,
    // Mes "yyyy-MM" al que corresponde, si aplica.
    string? Periodo,
    decimal? Importe,
    // Lo que pasó, con palabras exactas: un cambio de honorario no es una exportación ni una exportación un pago.
    string? Estado,
    EnlaceEvento? Enlace);

public static class HistorialErrores
{
    public static Error ColaboradorNoEncontrado(Guid id) =>
        Error.NoEncontrado("Colaborador.NoEncontrado", $"No existe un colaborador con id '{id}'.");
}

public class HistorialService(RpsDbContext db)
{
    public async Task<Resultado<List<EventoHistorialResponse>>> ListarAsync(Guid colaboradorId, CancellationToken ct)
    {
        var colaborador = await db.ColaboradoresRps.AsNoTracking().FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return HistorialErrores.ColaboradorNoEncontrado(colaboradorId);

        // Solo estos dos campos tienen autor real capturado hoy; el resto de los eventos de abajo
        // no afirma un usuario que no se registró (RC 05).
        var revisadoPorIds = await db.Liquidaciones.AsNoTracking()
            .Where(l => l.ColaboradorId == colaboradorId && l.RevisadoPorUsuarioId != null)
            .Select(l => l.RevisadoPorUsuarioId!.Value).Distinct().ToListAsync(ct);
        var creadoPorIds = await db.AnticiposAdicionales.AsNoTracking()
            .Where(a => a.ColaboradorId == colaboradorId).Select(a => a.CreadoPor).Distinct().ToListAsync(ct);
        var nombresPorUsuarioId = await db.Usuarios.AsNoTracking()
            .Where(u => revisadoPorIds.Contains(u.Id) || creadoPorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nombre, ct);

        var eventos = new List<EventoHistorialResponse>();
        var ficha = $"/colaboradores/{colaboradorId}";
        EnlaceEvento Condiciones() => new("Ver condiciones", ficha, new() { ["tab"] = "condiciones" });

        eventos.Add(new(colaborador.CreadoEn, "Vinculación", $"Vinculado en RPS con el ID de TMR {colaborador.TmrColaboradorId}.",
            null, null, null, "Registrado", null));

        // Frecuencias: no guardan fecha de registro; se ubican en el mes desde el que rigen.
        var frecuencias = await db.ColaboradoresFrecuencia.AsNoTracking().Where(f => f.ColaboradorId == colaboradorId).ToListAsync(ct);
        foreach (var f in frecuencias)
            eventos.Add(new(f.VigenteDesdeMes.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Frecuencia",
                $"Frecuencia \"{(f.Frecuencia == Frecuencia.DosPagos ? "Quincena y fin de mes" : "Solo fin de mes")}\" desde {PeriodoTexto.Formato(f.VigenteDesdeMes)}.",
                null, PeriodoTexto.Formato(f.VigenteDesdeMes), null, "Registrado", Condiciones()));

        // Contratos y adendas.
        var proyectos = await db.Proyectos.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);
        var contratos = await db.Contratos.AsNoTracking().Where(c => c.ColaboradorId == colaboradorId).ToListAsync(ct);
        var contratoIds = contratos.Select(c => c.Id).ToList();
        var adendas = await db.Adendas.AsNoTracking().Where(a => contratoIds.Contains(a.ContratoId)).ToListAsync(ct);
        var proyectosPorContrato = (await db.ContratosProyectos.AsNoTracking().Where(cp => contratoIds.Contains(cp.ContratoId)).ToListAsync(ct))
            .GroupBy(cp => cp.ContratoId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(cp => proyectos.GetValueOrDefault(cp.ProyectoId, "Proyecto"))));
        foreach (var c in contratos)
        {
            var enlace = new EnlaceEvento("Ver contrato", "/contratos", new() { ["contrato"] = c.Id.ToString() });
            var proyecto = proyectosPorContrato.GetValueOrDefault(c.Id, "sin proyecto");
            var finTexto = c.FechaFin.HasValue ? c.FechaFin.Value.ToString("yyyy-MM-dd") : "indefinido";
            eventos.Add(new(c.CreadoEn, "Contrato", $"Contrato {c.Referencia} en {proyecto}: del {c.FechaInicio:yyyy-MM-dd} al {finTexto}.",
                null, PeriodoTexto.Formato(c.FechaInicio), null, "Registrado", enlace));
            var numero = 1;
            foreach (var a in adendas.Where(x => x.ContratoId == c.Id).OrderBy(x => x.CreadoEn))
                eventos.Add(new(a.CreadoEn, "Adenda", $"Adenda {numero++} en {proyecto}: nuevo fin {a.NuevaFechaFin:yyyy-MM-dd}.",
                    null, PeriodoTexto.Formato(a.FechaRegistro), null, "Registrada", enlace));
        }

        // Honorarios (condiciones económicas).
        var mesActual = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1);
        var condiciones = await db.CondicionesEconomicas.AsNoTracking()
            .Where(c => c.ColaboradorId == colaboradorId).ToListAsync(ct);
        foreach (var c in condiciones)
        {
            var proyecto = proyectos.GetValueOrDefault(c.ProyectoId, "Proyecto");
            var modalidad = c.Modalidad == Modalidad.Neto ? "neto" : "más IVA";
            eventos.Add(new(c.CreadoEn, "Honorario", $"Cambio de honorario · {proyecto}: ${c.ImporteMensual:0.00} {modalidad} desde {PeriodoTexto.Formato(c.MesInicioVigencia)}.",
                null, PeriodoTexto.Formato(c.MesInicioVigencia), c.ImporteMensual,
                c.MesInicioVigencia > mesActual ? "Programado" : "Registrado", Condiciones()));
        }

        // Liquidaciones: cálculo y revisión (RN 49: revisar no es pagar).
        var liquidaciones = await db.Liquidaciones.AsNoTracking().Where(l => l.ColaboradorId == colaboradorId).ToListAsync(ct);
        foreach (var l in liquidaciones)
        {
            var mes = PeriodoTexto.Formato(l.Periodo);
            var enlace = new EnlaceEvento("Ver detalle", "/liquidaciones", new() { ["periodo"] = mes, ["liquidacion"] = l.Id.ToString() });
            eventos.Add(new(l.CreadoEn, "Liquidación", $"Liquidación de {mes} calculada.", null, mes, l.NetoConsolidado, "Calculada", enlace));
            if (l.RevisadoEn is not null)
                eventos.Add(new(l.RevisadoEn.Value, "Liquidación", $"Liquidación de {mes} marcada como revisada para envío. No confirma una transferencia.",
                    l.RevisadoPorUsuarioId is { } rid ? nombresPorUsuarioId.GetValueOrDefault(rid) : null,
                    mes, l.NetoConsolidado, "Revisada", enlace));
        }

        // Anticipos adicionales.
        var anticipos = await db.AnticiposAdicionales.AsNoTracking().Where(a => a.ColaboradorId == colaboradorId).ToListAsync(ct);
        var anticipoIds = anticipos.Select(a => a.Id).ToList();
        var cuotas = await db.CuotasRecuperacion.AsNoTracking().Where(c => anticipoIds.Contains(c.AnticipoAdicionalId)).ToListAsync(ct);
        foreach (var a in anticipos)
        {
            var plan = cuotas.Where(c => c.AnticipoAdicionalId == a.Id).ToList();
            var detalle = plan.Count == 0
                ? "Sin plan de recuperación todavía."
                : "Recuperación: " + string.Join(", ", plan.Select(c => $"{(c.Corte == TipoCorte.Quincena ? "quincena" : "fin de mes")} ${c.Importe:0.00}")) + ".";
            eventos.Add(new(a.CreadoEn, "Anticipo", $"Anticipo adicional de ${a.Monto:0.00}. {detalle}",
                nombresPorUsuarioId.GetValueOrDefault(a.CreadoPor), plan.Count > 0 ? PeriodoTexto.Formato(plan[0].Periodo) : PeriodoTexto.Formato(a.Fecha),
                a.Monto, "Registrado", new("Ver plan", "/anticipos", new() { ["colaborador"] = colaboradorId.ToString() })));
        }

        return eventos.OrderByDescending(e => e.Fecha).ToList();
    }
}
