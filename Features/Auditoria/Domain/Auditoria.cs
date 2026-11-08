namespace rps_backend.Features.Auditoria.Domain;

// Registro genérico de cambios, capturado automáticamente por AuditoriaInterceptor en cada
// SaveChanges (RN 48, RC 03) — ningún servicio de negocio escribe aquí a mano.
// UsuarioId es null cuando el cambio no vino de una petición HTTP autenticada (ej. el job de
// sincronización con TMR, que corre sin usuario): no se inventa un autor que no hubo (RC 05).
public class Auditoria
{
    public long Id { get; set; }

    // Nombre de la clase de dominio (ej. "Liquidacion", "Contrato"), no el nombre de la tabla.
    public string Entidad { get; set; } = string.Empty;

    // Clave primaria de la fila afectada, como texto (soporta claves compuestas: "a,b").
    public string EntidadId { get; set; } = string.Empty;

    // Null en alta o baja completa (el cambio es la fila entera, no un campo).
    public string? Campo { get; set; }

    public string? ValorAnterior { get; set; }

    public string? ValorNuevo { get; set; }

    public Guid? UsuarioId { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public string? Motivo { get; set; }
}
