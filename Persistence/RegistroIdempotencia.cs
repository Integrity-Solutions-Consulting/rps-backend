namespace rps_backend.Persistence;

// La respuesta guardada de una petición que llegó con header Idempotency-Key. Si la misma clave
// vuelve a llegar, IdempotenciaMiddleware devuelve esto en vez de repetir la acción (ej. un
// doble clic en "Registrar anticipo" no debe crear dos anticipos). Solo se guardan éxitos
// (2xx): un error no debe "congelarse" como si fuera la respuesta definitiva.
public class RegistroIdempotencia
{
    public string Clave { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string? ContentType { get; set; }

    public byte[] Cuerpo { get; set; } = [];

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
