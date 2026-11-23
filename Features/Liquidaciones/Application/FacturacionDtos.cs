using rps_backend.Features.Liquidaciones.Domain;
using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Application;

// ===== Requests =====

public record MarcarFacturaRequest(DateOnly? Periodo);

public record GuardarDiasRequest(DateOnly? Periodo, int? DiasDescontados, string? Observaciones);

// ===== Responses =====

// Fase del plazo de facturas del mes, según la hora actual de Ecuador.
public enum FasePlazoFactura
{
    NoAplica,   // mes anterior al control de facturas
    Abierto,    // hasta el día 7, 23:59
    Gracia,     // del 8 al 14, 23:59
    Cerrado,    // desde el 15, 00:00
}

// Estado de facturación de todo el mes: plazos y una fila por colaborador con liquidación o con
// algo registrado. CierreATiempo y CierreGracia van en hora de Ecuador (sin zona).
public record FacturacionPeriodoResponse(
    DateOnly Periodo,
    FasePlazoFactura Fase,
    DateTime CierreATiempo,
    DateTime CierreGracia,
    // Liquidaciones que todavía tienen quincena aunque ya no les corresponde (pasó el día 7 sin
    // factura a tiempo): se ajustan al volver a "Calcular período".
    int QuincenasPorAjustar,
    List<ControlMensualResponse> Colaboradores);

public record ControlMensualResponse(
    Guid ColaboradorId,
    Frecuencia Frecuencia,
    EstadoFactura EstadoFactura,
    DateTime? FacturaRecibidaEn,
    string? FacturaMarcadaPor,
    // ¿Se puede marcar o quitar ahora? (plazo y gracia; una factura a tiempo solo se quita hasta el 7)
    bool PuedeMarcarFactura,
    bool PuedeQuitarFactura,
    int DiasDescontados,
    string? Observaciones,
    DateTime? DiasActualizadoEn,
    string? DiasActualizadoPor,
    bool QuincenaPorAjustar);
