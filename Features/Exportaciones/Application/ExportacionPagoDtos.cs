using rps_backend.Shared;

namespace rps_backend.Features.Exportaciones.Application;

// ===== El Excel de pago por corte (RN 52-57) =====
// Independiente de los reportes CSV/PDF de ExportacionesService (esos son reportes de consulta;
// este es el archivo que Contabilidad lleva al banco, con trazabilidad e historial de versiones).

// ===== Requests =====

public record GenerarExportacionPagoRequest(DateOnly? Periodo, TipoCorte? Corte, List<Guid>? ColaboradorIds);

// ===== Responses =====

public record ItemPreviewResponse(
    Guid ColaboradorId, string Colaborador, decimal Importe, bool TieneCuentaBancaria, List<string> Advertencias);

// Lo que la pantalla muestra antes de generar: valores, advertencias y total (RN 53).
public record PreviewExportacionPagoResponse(
    DateOnly Periodo, TipoCorte Corte, int VersionSiguiente, List<ItemPreviewResponse> Items, decimal Total);

public record ExportacionPagoItemResponse(Guid ColaboradorId, string Colaborador, decimal Importe);

public record ExportacionPagoResponse(
    Guid Id, DateOnly Periodo, TipoCorte Corte, int Version, Guid GeneradoPor, DateTime GeneradoEn,
    string NombreArchivo, decimal Total, List<ExportacionPagoItemResponse> Items);
