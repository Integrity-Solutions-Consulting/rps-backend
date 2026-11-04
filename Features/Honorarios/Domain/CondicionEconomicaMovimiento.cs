namespace rps_backend.Features.Honorarios.Domain;

// Registro de cada cancelación o restauración de una CondicionEconomica programada, para el
// historial del colaborador (RN 48: conservar autor, fecha y valores).
public class CondicionEconomicaMovimiento
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid CondicionEconomicaId { get; set; }

    // "Cancelado" o "Restaurado".
    public string Tipo { get; set; } = string.Empty;

    // Sin autenticación todavía: siempre "Contabilidad" (RC 01 pendiente).
    public string Usuario { get; set; } = "Contabilidad";

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
