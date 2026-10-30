using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Feriados.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Feriados.Application;

// Reglas de negocio de Feriados. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
public class FeriadosService(RpsDbContext db, IHttpClientFactory httpClientFactory)
{
    // Fuente pública de feriados de Ecuador (gratuita, sin clave) — misma que ya usa TMR
    // (Features/Configuracion/DiasFestivos). No incluye traslados por decreto: por eso
    // la importación es una base y CrearAsync permite corregir/agregar a mano.
    private const string UrlFeriadosEcuador = "https://date.nager.at/api/v3/PublicHolidays/{0}/EC";

    // La fuente devuelve algunos nombres en inglés: se traducen igual que en TMR.
    private static readonly Dictionary<string, string> NombresFeriados = new(StringComparer.OrdinalIgnoreCase)
    {
        ["New Year's Day"] = "Año Nuevo",
        ["Carnival"] = "Carnaval",
        ["Good Friday"] = "Viernes Santo",
        ["International Workers' Day"] = "Día del Trabajo",
        ["The Battle of Pichincha"] = "Batalla de Pichincha",
        ["Declaration of Independence of Quito"] = "Primer Grito de Independencia",
        ["Independence of Guayaquil"] = "Independencia de Guayaquil",
        ["All Souls' Day"] = "Día de los Difuntos",
        ["Independence of Cuenca"] = "Independencia de Cuenca",
        ["Christmas Day"] = "Navidad",
    };

    private sealed record FeriadoExterno(DateOnly date, string localName, string name);

    public async Task<List<FeriadoResponse>> ListarAsync(CancellationToken ct)
    {
        return await db.Feriados.AsNoTracking()
            .OrderBy(f => f.Fecha)
            .Select(f => new FeriadoResponse(f.Id, f.Fecha, f.Nombre, f.CreadoEn))
            .ToListAsync(ct);
    }

    public async Task<Resultado<FeriadoResponse>> CrearAsync(CrearFeriadoRequest request, CancellationToken ct)
    {
        if (request.Fecha is null)
            return FeriadoErrores.FechaInvalida;

        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre) || nombre.Length > 200)
            return FeriadoErrores.NombreInvalido;

        if (await db.Feriados.AnyAsync(f => f.Fecha == request.Fecha, ct))
            return FeriadoErrores.FechaDuplicada(request.Fecha.Value);

        var feriado = new Feriado { Fecha = request.Fecha.Value, Nombre = nombre };

        db.Feriados.Add(feriado);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return FeriadoErrores.FechaDuplicada(request.Fecha.Value);
        }

        return new FeriadoResponse(feriado.Id, feriado.Fecha, feriado.Nombre, feriado.CreadoEn);
    }

    public async Task<Resultado<bool>> EliminarAsync(Guid id, CancellationToken ct)
    {
        var feriado = await db.Feriados.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feriado is null)
            return FeriadoErrores.NoEncontrado(id);

        // Si ya se generaron honorarios del período que contiene esta fecha, borrarlo dejaría
        // ese cálculo inconsistente contra una futura corrida del mismo período.
        var mesFeriado = new DateOnly(feriado.Fecha.Year, feriado.Fecha.Month, 1);
        if (await db.Honorarios.AnyAsync(h => h.Periodo == mesFeriado, ct))
            return FeriadoErrores.YaUsadoEnHonorarios(feriado.Fecha);

        db.Feriados.Remove(feriado);
        await db.SaveChangesAsync(ct);

        return true;
    }

    // Idempotente: no duplica un feriado que ya existe en esa fecha.
    public async Task<Resultado<ImportarFeriadosResponse>> ImportarAsync(int anio, CancellationToken ct)
    {
        if (anio is < 2000 or > 2100)
            return FeriadoErrores.AnioInvalido;

        List<FeriadoExterno>? externos;
        try
        {
            var cliente = httpClientFactory.CreateClient();
            cliente.Timeout = TimeSpan.FromSeconds(20);
            externos = await cliente.GetFromJsonAsync<List<FeriadoExterno>>(string.Format(UrlFeriadosEcuador, anio), ct);
        }
        // TaskCanceledException solo se trata como "fuente no disponible" si vino del timeout
        // del HttpClient; si el que canceló fue el propio ct del llamador, se deja propagar.
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException
            || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            return FeriadoErrores.FuenteNoDisponible;
        }

        var fechasExistentes = (await db.Feriados.AsNoTracking()
                .Where(f => f.Fecha.Year == anio)
                .Select(f => f.Fecha)
                .ToListAsync(ct))
            .ToHashSet();

        var nuevos = new List<Feriado>();
        var omitidos = 0;

        foreach (var externo in externos ?? [])
        {
            if (fechasExistentes.Contains(externo.date))
            {
                omitidos++;
                continue;
            }

            var nombre = NombresFeriados.TryGetValue(externo.name, out var traducido) ? traducido : externo.localName;
            // Carnaval son dos días con el mismo nombre: se distinguen por el día de la semana.
            if (nombre == "Carnaval")
                nombre = externo.date.DayOfWeek == DayOfWeek.Monday ? "Carnaval - Lunes" : "Carnaval - Martes";

            nuevos.Add(new Feriado { Fecha = externo.date, Nombre = nombre });
            fechasExistentes.Add(externo.date);
        }

        if (nuevos.Count > 0)
        {
            db.Feriados.AddRange(nuevos);
            await db.SaveChangesAsync(ct);
        }

        return new ImportarFeriadosResponse(anio, nuevos.Count, omitidos);
    }
}
