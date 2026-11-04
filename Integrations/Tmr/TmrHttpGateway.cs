namespace rps_backend.Integrations.Tmr;

// La URL base y la API key se configuran al registrar el HttpClient en Program.cs.
public class TmrHttpGateway(HttpClient http) : ITmrGateway
{
    public Task<List<TmrColaboradorDto>> ObtenerColaboradoresAsync(CancellationToken ct) =>
        ObtenerAsync<TmrColaboradorDto>("/api/integracion/rps/colaboradores", ct);

    public Task<List<TmrProyectoDto>> ObtenerProyectosAsync(CancellationToken ct) =>
        ObtenerAsync<TmrProyectoDto>("/api/integracion/rps/proyectos", ct);

    public Task<List<TmrAsignacionDto>> ObtenerAsignacionesAsync(CancellationToken ct) =>
        ObtenerAsync<TmrAsignacionDto>("/api/integracion/rps/asignaciones", ct);

    private async Task<List<T>> ObtenerAsync<T>(string ruta, CancellationToken ct) =>
        await http.GetFromJsonAsync<List<T>>(ruta, ct)
            ?? throw new InvalidOperationException($"TMR devolvió una respuesta vacía en {ruta}.");
}
