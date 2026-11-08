using rps_backend.Shared;

namespace rps_backend.Features.Exportaciones.Domain;

// El archivo de pago generado para un (Periodo, Corte) (RN 52-55). Inmutable una vez creada:
// volver a generar para el mismo (Periodo, Corte) crea una versión nueva, nunca la reemplaza.
// El contenido se guarda junto con los metadatos para poder descargarlo de nuevo sin
// reconstruirlo (RN 55 — descargar no regenera).
public class Exportacion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateOnly Periodo { get; set; }

    public TipoCorte Corte { get; set; }

    public int Version { get; set; }

    public Guid GeneradoPor { get; set; }

    public DateTime GeneradoEn { get; set; } = DateTime.UtcNow;

    public string NombreArchivo { get; set; } = string.Empty;

    public byte[] Contenido { get; set; } = [];

    public List<ExportacionItem> Items { get; set; } = [];
}
