namespace rps_backend.Integrations.Tmr;

// Lectura de TMR. Si TMR no responde o la respuesta no es válida, lanza una excepción: nunca devuelve
// una lista vacía en su lugar, para no confundir "no se pudo verificar" con "no hay datos" (RC 05).
public interface ITmrGateway
{
    Task<List<TmrColaboradorDto>> ObtenerColaboradoresAsync(CancellationToken ct);

    Task<List<TmrProyectoDto>> ObtenerProyectosAsync(CancellationToken ct);

    Task<List<TmrAsignacionDto>> ObtenerAsignacionesAsync(CancellationToken ct);
}
