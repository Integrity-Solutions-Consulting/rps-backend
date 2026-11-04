using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Proyectos.Domain;
using rps_backend.Integrations.Tmr;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.IntegracionTmr.Application;

// Copia a RPS los colaboradores, proyectos y asignaciones de TMR. RPS nunca escribe en TMR (RN 01) y
// lo que desaparece de TMR se marca, no se borra (RN 07, RN 22). Se aplica todo o nada.
public class SincronizacionTmrService(RpsDbContext db, ITmrGateway tmr, ILogger<SincronizacionTmrService> logger)
{
    // Evita que dos sincronizaciones simultáneas creen los mismos registros (RC 04).
    private const long LlaveBloqueo = 7_342_001;

    public async Task<Resultado<SincronizacionTmrResponse>> SincronizarAsync(CancellationToken ct)
    {
        // Se descarga todo antes de tocar la base: si TMR falla no se interpreta como que no hay datos (RC 05).
        List<TmrColaboradorDto> colaboradores;
        List<TmrProyectoDto> proyectos;
        List<TmrAsignacionDto> asignaciones;
        try
        {
            colaboradores = await tmr.ObtenerColaboradoresAsync(ct);
            proyectos = await tmr.ObtenerProyectosAsync(ct);
            asignaciones = await tmr.ObtenerAsignacionesAsync(ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested
            && ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            logger.LogWarning(ex, "No se pudo leer TMR durante la sincronización");
            return SincronizacionTmrErrores.TmrNoDisponible;
        }

        // Una lista vacía marcaría todo como eliminado en origen.
        if (colaboradores.Count == 0 || proyectos.Count == 0 || asignaciones.Count == 0)
            return SincronizacionTmrErrores.RespuestaVacia;

        var ahora = DateTime.UtcNow;

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({LlaveBloqueo})", ct);

        var (conteoProyectos, proyectoIds) = await SincronizarProyectosAsync(proyectos, ahora, ct);
        var (conteoColaboradores, colaboradorIds) = await SincronizarColaboradoresAsync(colaboradores, ahora, ct);
        var (conteoAsignaciones, omitidas) = await SincronizarAsignacionesAsync(asignaciones, colaboradorIds, proyectoIds, ahora, ct);

        try
        {
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "No se pudieron guardar los datos sincronizados de TMR");
            return SincronizacionTmrErrores.NoGuardado;
        }

        return new SincronizacionTmrResponse(ahora, conteoProyectos, conteoColaboradores, conteoAsignaciones, omitidas);
    }

    private async Task<(ConteoSincronizacion Conteo, Dictionary<int, Guid> Ids)> SincronizarProyectosAsync(
        List<TmrProyectoDto> origen, DateTime ahora, CancellationToken ct)
    {
        var existentes = await db.Proyectos
            .Where(p => p.TmrProyectoId != null)
            .ToDictionaryAsync(p => p.TmrProyectoId!, ct);

        var ids = new Dictionary<int, Guid>();
        var vistos = new HashSet<string>();
        int creados = 0, actualizados = 0, marcados = 0;

        foreach (var dto in origen)
        {
            var clave = dto.Id.ToString(CultureInfo.InvariantCulture);
            vistos.Add(clave);
            var nombre = dto.Nombre.Trim();
            var cliente = dto.Cliente?.Trim();

            if (!existentes.TryGetValue(clave, out var proyecto))
            {
                proyecto = new Proyecto { TmrProyectoId = clave, Nombre = nombre, Cliente = cliente, Activo = dto.Activo, SincronizadoEn = ahora };
                db.Proyectos.Add(proyecto);
                creados++;
            }
            else
            {
                if (proyecto.Nombre != nombre || proyecto.Cliente != cliente || proyecto.Activo != dto.Activo || proyecto.EliminadoEnOrigen)
                    actualizados++;

                proyecto.Nombre = nombre;
                proyecto.Cliente = cliente;
                proyecto.Activo = dto.Activo;
                proyecto.EliminadoEnOrigen = false;
                proyecto.SincronizadoEn = ahora;
            }

            ids[dto.Id] = proyecto.Id;
        }

        foreach (var (clave, proyecto) in existentes)
        {
            if (!vistos.Contains(clave) && !proyecto.EliminadoEnOrigen)
            {
                proyecto.EliminadoEnOrigen = true;
                marcados++;
            }
        }

        return (new ConteoSincronizacion(creados, actualizados, marcados), ids);
    }

    private async Task<(ConteoSincronizacion Conteo, Dictionary<int, Guid> Ids)> SincronizarColaboradoresAsync(
        List<TmrColaboradorDto> origen, DateTime ahora, CancellationToken ct)
    {
        var existentes = await db.ColaboradoresRps.ToDictionaryAsync(c => c.TmrColaboradorId, ct);

        var ids = new Dictionary<int, Guid>();
        var vistos = new HashSet<string>();
        int creados = 0, actualizados = 0, marcados = 0;

        foreach (var dto in origen)
        {
            var clave = dto.Id.ToString(CultureInfo.InvariantCulture);
            vistos.Add(clave);
            var nombres = dto.Nombres?.Trim();
            var apellidos = dto.Apellidos?.Trim();
            var identificacion = dto.Identificacion?.Trim();
            var correo = dto.Correo?.Trim();

            if (!existentes.TryGetValue(clave, out var colaborador))
            {
                // Nace Pendiente aunque aún no tenga contratos ni honorarios (RN 06).
                colaborador = new ColaboradorRps { TmrColaboradorId = clave };
                db.ColaboradoresRps.Add(colaborador);
                creados++;
            }
            else if (colaborador.Nombres != nombres || colaborador.Apellidos != apellidos
                || colaborador.Identificacion != identificacion || colaborador.Correo != correo
                || colaborador.ActivoEnTmr != dto.Activo || colaborador.EliminadoEnOrigen)
            {
                colaborador.ActualizadoEn = ahora;
                actualizados++;
            }

            // Activo no se toca: lo administra Contabilidad en RPS. El estado de TMR va en ActivoEnTmr.
            colaborador.Nombres = nombres;
            colaborador.Apellidos = apellidos;
            colaborador.Identificacion = identificacion;
            colaborador.Correo = correo;
            colaborador.ActivoEnTmr = dto.Activo;
            colaborador.EliminadoEnOrigen = false;
            colaborador.SincronizadoEn = ahora;

            ids[dto.Id] = colaborador.Id;
        }

        // Los creados a mano (SincronizadoEn nulo) no vienen de TMR: no se marcan.
        foreach (var (clave, colaborador) in existentes)
        {
            if (!vistos.Contains(clave) && colaborador.SincronizadoEn is not null && !colaborador.EliminadoEnOrigen)
            {
                colaborador.EliminadoEnOrigen = true;
                colaborador.ActualizadoEn = ahora;
                marcados++;
            }
        }

        return (new ConteoSincronizacion(creados, actualizados, marcados), ids);
    }

    private async Task<(ConteoSincronizacion Conteo, List<AsignacionOmitida> Omitidas)> SincronizarAsignacionesAsync(
        List<TmrAsignacionDto> origen, Dictionary<int, Guid> colaboradorIds, Dictionary<int, Guid> proyectoIds,
        DateTime ahora, CancellationToken ct)
    {
        var omitidas = new List<AsignacionOmitida>();
        // Fila inválida con inicio conocido: la asignación que RPS ya tenga se deja como está hasta corregir TMR.
        var clavesInvalidas = new HashSet<(Guid, Guid, DateOnly)>();
        // TMR puede repetir colaborador, proyecto e inicio (una fila por líder): si alguna sigue abierta, queda abierta.
        var validas = new Dictionary<(Guid ColaboradorId, Guid ProyectoId, DateOnly FechaInicio), DateOnly?>();

        foreach (var dto in origen)
        {
            if (!colaboradorIds.TryGetValue(dto.IdEmpleado, out var colaboradorId))
            {
                omitidas.Add(new AsignacionOmitida(dto.IdEmpleado, dto.IdProyecto, "El colaborador no llegó en la lista de TMR."));
                continue;
            }

            if (!proyectoIds.TryGetValue(dto.IdProyecto, out var proyectoId))
            {
                omitidas.Add(new AsignacionOmitida(dto.IdEmpleado, dto.IdProyecto, "El proyecto no llegó en la lista de TMR."));
                continue;
            }

            if (!IntentarLeerFecha(dto.FechaInicio, out var fechaInicio))
            {
                omitidas.Add(new AsignacionOmitida(dto.IdEmpleado, dto.IdProyecto, "No tiene una fecha de inicio válida."));
                continue;
            }

            var clave = (colaboradorId, proyectoId, fechaInicio);

            DateOnly? fechaSalida = null;
            if (!string.IsNullOrWhiteSpace(dto.FechaSalida))
            {
                if (!IntentarLeerFecha(dto.FechaSalida, out var salida) || salida < fechaInicio)
                {
                    clavesInvalidas.Add(clave);
                    omitidas.Add(new AsignacionOmitida(dto.IdEmpleado, dto.IdProyecto, "La fecha de salida no es válida."));
                    continue;
                }

                fechaSalida = salida;
            }
            else if (!dto.EstadoAsignacion)
            {
                clavesInvalidas.Add(clave);
                omitidas.Add(new AsignacionOmitida(dto.IdEmpleado, dto.IdProyecto, "Está inactiva y no tiene fecha de salida."));
                continue;
            }

            if (validas.TryGetValue(clave, out var salidaPrevia))
                validas[clave] = salidaPrevia is null || fechaSalida is null
                    ? null
                    : salidaPrevia > fechaSalida ? salidaPrevia : fechaSalida;
            else
                validas[clave] = fechaSalida;
        }

        var existentes = await db.Asignaciones.ToListAsync(ct);
        var porClave = existentes
            .GroupBy(a => (a.ColaboradorId, a.ProyectoId, a.FechaInicio))
            .ToDictionary(g => g.Key, g => g.First());

        var vistas = new HashSet<Guid>();
        int creadas = 0, actualizadas = 0, marcadas = 0;

        foreach (var (clave, fechaSalida) in validas)
        {
            if (!porClave.TryGetValue(clave, out var asignacion))
            {
                asignacion = new Asignacion
                {
                    ColaboradorId = clave.ColaboradorId,
                    ProyectoId = clave.ProyectoId,
                    FechaInicio = clave.FechaInicio,
                    FechaSalida = fechaSalida,
                    SincronizadaEn = ahora
                };
                db.Asignaciones.Add(asignacion);
                creadas++;
            }
            else
            {
                if (asignacion.FechaSalida != fechaSalida || asignacion.EliminadaEnOrigen)
                    actualizadas++;

                asignacion.FechaSalida = fechaSalida;
                asignacion.EliminadaEnOrigen = false;
                asignacion.SincronizadaEn = ahora;
            }

            vistas.Add(asignacion.Id);
        }

        // Las creadas a mano (SincronizadaEn nula) no vienen de TMR: no se marcan.
        foreach (var asignacion in existentes)
        {
            if (!vistas.Contains(asignacion.Id) && asignacion.SincronizadaEn is not null && !asignacion.EliminadaEnOrigen
                && !clavesInvalidas.Contains((asignacion.ColaboradorId, asignacion.ProyectoId, asignacion.FechaInicio)))
            {
                asignacion.EliminadaEnOrigen = true;
                marcadas++;
            }
        }

        return (new ConteoSincronizacion(creadas, actualizadas, marcadas), omitidas);
    }

    private static bool IntentarLeerFecha(string? valor, out DateOnly fecha) =>
        DateOnly.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
}
