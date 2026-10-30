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
    // Vinculación | Frecuencia | Contrato | Adenda | Honorario | Liquidación | Anticipo | Documento | Exportación
    string Tipo,
    string Descripcion,
    string Usuario,
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
    // Sin autenticación todavía: todos los movimientos se atribuyen a Contabilidad (RC 01 pendiente).
    private const string Usuario = "Contabilidad";

    public async Task<Resultado<List<EventoHistorialResponse>>> ListarAsync(Guid colaboradorId, CancellationToken ct)
    {
        var colaborador = await db.ColaboradoresRps.AsNoTracking().FirstOrDefaultAsync(c => c.Id == colaboradorId, ct);
        if (colaborador is null)
            return HistorialErrores.ColaboradorNoEncontrado(colaboradorId);

        var eventos = new List<EventoHistorialResponse>();
        var ficha = $"/colaboradores/{colaboradorId}";
        EnlaceEvento Condiciones() => new("Ver condiciones", ficha, new() { ["tab"] = "condiciones" });

        eventos.Add(new(colaborador.CreadoEn, "Vinculación", $"Vinculado en RPS con el ID de TMR {colaborador.TmrColaboradorId}.",
            Usuario, null, null, "Registrado", null));

        // Frecuencias: no guardan fecha de registro; se ubican en el mes desde el que rigen.
        var frecuencias = await db.ColaboradoresFrecuencia.AsNoTracking().Where(f => f.ColaboradorId == colaboradorId).ToListAsync(ct);
        foreach (var f in frecuencias)
            eventos.Add(new(f.VigenteDesdeMes.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Frecuencia",
                $"Frecuencia \"{(f.Frecuencia == Frecuencia.DosPagos ? "Quincena y fin de mes" : "Solo fin de mes")}\" desde {PeriodoTexto.Formato(f.VigenteDesdeMes)}.",
                Usuario, PeriodoTexto.Formato(f.VigenteDesdeMes), null, "Registrado", Condiciones()));

        // Contratos y adendas.
        var proyectos = await db.Proyectos.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);
        var contratos = await db.Contratos.AsNoTracking().Where(c => c.ColaboradorId == colaboradorId).ToListAsync(ct);
        var contratoIds = contratos.Select(c => c.Id).ToList();
        var adendas = await db.Adendas.AsNoTracking().Where(a => contratoIds.Contains(a.ContratoId)).ToListAsync(ct);
        foreach (var c in contratos)
        {
            var enlace = new EnlaceEvento("Ver contrato", "/contratos", new() { ["contrato"] = c.Id.ToString() });
            var proyecto = proyectos.GetValueOrDefault(c.ProyectoId, "Proyecto");
            eventos.Add(new(c.CreadoEn, "Contrato", $"Contrato {c.Referencia} en {proyecto}: del {c.FechaInicio:yyyy-MM-dd} al {c.FechaFin:yyyy-MM-dd}.",
                Usuario, PeriodoTexto.Formato(c.FechaInicio), null, "Registrado", enlace));
            var numero = 1;
            foreach (var a in adendas.Where(x => x.ContratoId == c.Id).OrderBy(x => x.CreadoEn))
                eventos.Add(new(a.CreadoEn, "Adenda", $"Adenda {numero++} en {proyecto}: nuevo fin {a.NuevaFechaFin:yyyy-MM-dd}.",
                    Usuario, PeriodoTexto.Formato(a.FechaRegistro), null, "Registrada", enlace));
        }

        // Honorarios (condiciones económicas), también los cancelados, y sus cancelaciones o restauraciones.
        var mesActual = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1);
        var condiciones = await db.CondicionesEconomicas.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.ColaboradorId == colaboradorId).ToListAsync(ct);
        var condicionIds = condiciones.Select(c => c.Id).ToList();
        var movimientos = await db.CondicionesEconomicasMovimientos.AsNoTracking()
            .Where(m => condicionIds.Contains(m.CondicionEconomicaId)).ToListAsync(ct);
        foreach (var c in condiciones)
        {
            var proyecto = proyectos.GetValueOrDefault(c.ProyectoId, "Proyecto");
            var modalidad = c.Modalidad == Modalidad.Neto ? "neto" : "más IVA";
            eventos.Add(new(c.CreadoEn, "Honorario", $"Cambio de honorario · {proyecto}: ${c.ImporteMensual:0.00} {modalidad} desde {PeriodoTexto.Formato(c.MesInicioVigencia)}.",
                Usuario, PeriodoTexto.Formato(c.MesInicioVigencia), c.ImporteMensual,
                c.MesInicioVigencia > mesActual ? "Programado" : "Registrado", Condiciones()));
            foreach (var m in movimientos.Where(x => x.CondicionEconomicaId == c.Id))
                eventos.Add(new(m.Fecha, "Honorario",
                    m.Tipo == "Cancelado"
                        ? $"Se canceló el cambio programado de {proyecto}: ${c.ImporteMensual:0.00} desde {PeriodoTexto.Formato(c.MesInicioVigencia)}."
                        : $"Se deshizo la cancelación: {proyecto} vuelve a tener ${c.ImporteMensual:0.00} desde {PeriodoTexto.Formato(c.MesInicioVigencia)}.",
                    m.Usuario, PeriodoTexto.Formato(c.MesInicioVigencia), c.ImporteMensual, m.Tipo, Condiciones()));
        }

        // Liquidaciones: cálculo y revisión (RN 49: revisar no es pagar).
        var liquidaciones = await db.Liquidaciones.AsNoTracking().Where(l => l.ColaboradorId == colaboradorId).ToListAsync(ct);
        foreach (var l in liquidaciones)
        {
            var mes = PeriodoTexto.Formato(l.Periodo);
            var enlace = new EnlaceEvento("Ver detalle", "/liquidaciones", new() { ["periodo"] = mes, ["liquidacion"] = l.Id.ToString() });
            eventos.Add(new(l.CreadoEn, "Liquidación", $"Liquidación de {mes} calculada.", Usuario, mes, l.NetoConsolidado, "Calculada", enlace));
            if (l.RevisadoEn is not null)
                eventos.Add(new(l.RevisadoEn.Value, "Liquidación", $"Liquidación de {mes} marcada como revisada para envío. No confirma una transferencia.",
                    Usuario, mes, l.NetoConsolidado, "Revisada", enlace));
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
                Usuario, plan.Count > 0 ? PeriodoTexto.Formato(plan[0].Periodo) : PeriodoTexto.Formato(a.Fecha), a.Monto, "Registrado",
                new("Ver plan", "/anticipos", new() { ["colaborador"] = colaboradorId.ToString() })));
        }

        // Documentos tributarios.
        var documentos = await db.DocumentosMensuales.AsNoTracking().Where(d => d.ColaboradorId == colaboradorId).ToListAsync(ct);
        foreach (var d in documentos)
        {
            var mes = PeriodoTexto.Formato(d.Periodo);
            eventos.Add(new(d.MarcadoEn, "Documento", $"{(d.Tipo == TipoDocumento.Factura ? "Factura" : "Nota de venta")} {d.Numero} de {mes}.",
                Usuario, mes, null, "Registrado",
                new("Ver documento", "/documentos", new() { ["periodo"] = mes, ["colaborador"] = colaboradorId.ToString() })));
        }

        // Exportaciones en las que salió (RN 56: exportado no es pagado).
        var exportaciones = await db.ExportacionesPagoDetalle.AsNoTracking()
            .Where(d => d.ColaboradorId == colaboradorId)
            .Join(db.ExportacionesPago.AsNoTracking(), d => d.ExportacionPagoId, e => e.Id, (d, e) => new { e.Periodo, e.Corte, e.Version, e.GeneradaEn, e.Usuario, d.Valor })
            .ToListAsync(ct);
        foreach (var e in exportaciones)
        {
            var mes = PeriodoTexto.Formato(e.Periodo);
            var corte = e.Corte == TipoCorte.Quincena ? "quincena" : "fin de mes";
            eventos.Add(new(e.GeneradaEn, "Exportación", $"Exportación {corte} de {mes} (versión {e.Version}). Archivo preparado; no confirma la transferencia.",
                e.Usuario, mes, e.Valor, $"Exportado v{e.Version}",
                new("Ver versión", "/exportaciones", new() { ["periodo"] = mes, ["corte"] = e.Corte.ToString() })));
        }

        return eventos.OrderByDescending(e => e.Fecha).ToList();
    }
}
