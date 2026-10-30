using rps_backend.Shared;

namespace rps_backend.Features.Exportaciones.Domain;

// Una versión del archivo de pago de un mes y corte (RN 52-55). Se guarda el archivo tal cual se
// generó para poder descargarlo otra vez sin crear otra versión. Exportar no marca nada como
// pagado ni da por recuperados los anticipos (RN 56).
public class ExportacionPago
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    // Primer día del mes.
    public DateOnly Periodo { get; set; }

    public TipoCorte Corte { get; set; }

    // 1, 2, 3… por cada (Periodo, Corte).
    public int Version { get; set; }

    public DateTime GeneradaEn { get; set; } = DateTime.UtcNow;

    // Sin autenticación todavía: siempre "Contabilidad" (RC 01 pendiente).
    public string Usuario { get; set; } = "Contabilidad";

    public decimal Total { get; set; }

    public string NombreArchivo { get; set; } = string.Empty;

    public byte[] Contenido { get; set; } = [];
}
