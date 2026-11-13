namespace rps_backend.Features.Exportaciones.Application;

// POST /api/exportaciones/{honorarios|liquidaciones}[/pdf]
//   periodo: primer día del mes ("2026-11-01"); null = todos los meses.
//   vista (solo liquidaciones): "consolidado" (por defecto) o "porColaborador".
public record ExportarReporteRequest(DateOnly? Periodo, string? Vista);

// Una fila del historial "Exportaciones recientes". Tipo, Vista y Formato con los mismos valores que el frontend.
// DatosCambiaron: true si los datos del reporte cambiaron desde esta exportación, false si son iguales,
// null si no se pudo comprobar (no se afirma nada).
public record ExportacionResponse(
    Guid Id, string Tipo, string? Vista, string Formato, DateOnly? Periodo, string NombreArchivo,
    int Registros, decimal? TotalNeto, string? Usuario, DateTime GeneradoEn, bool? DatosCambiaron);

public record PaginaExportacionesResponse(List<ExportacionResponse> Items, int Total, int Pagina, int Tamano);

public record UltimaExportacionResponse(string Tipo, string? Vista, string Formato, DateTime GeneradoEn, string? Usuario);
