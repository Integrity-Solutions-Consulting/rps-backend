using rps_backend.Persistence;

namespace rps_backend.Features.Exportaciones.Domain;

public enum TipoReporte { Honorarios, Liquidaciones, Colaboradores }

public enum FormatoReporte { Excel, Pdf }

// Un archivo que RPS generó y entregó (historial "Exportaciones recientes"). Solo se crea cuando el
// archivo se generó bien y nunca se edita ni se borra. No guarda el archivo: "volver a generar" lo
// arma de nuevo con los datos actuales. Exportar no cambia ninguna liquidación (RN 49, RN 56).
// No pasa por rps.auditoria (ISinAuditoria): ya es en sí un registro de quién, qué y cuándo.
public class Exportacion : ISinAuditoria
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public TipoReporte Tipo { get; set; }

    // Solo liquidaciones: Consolidado o PorColaborador.
    public Application.VistaLiquidaciones? Vista { get; set; }

    public FormatoReporte Formato { get; set; }

    // Primer día del mes exportado; null = todos los meses (y siempre null en colaboradores).
    public DateOnly? Periodo { get; set; }

    public string NombreArchivo { get; set; } = string.Empty;

    // Filas del reporte (honorarios, liquidaciones o colaboradores) y su neto total (null en colaboradores).
    public int Registros { get; set; }

    public decimal? TotalNeto { get; set; }

    // SHA-256 (hex) de los datos que muestra el reporte, no de los bytes del archivo (el PDF lleva la
    // hora de generación). Si hoy da otro valor, los datos cambiaron desde esta exportación.
    public string HuellaDatos { get; set; } = string.Empty;

    public Guid GeneradoPorUsuarioId { get; set; }

    public DateTime GeneradoEn { get; set; } = DateTime.UtcNow;
}
