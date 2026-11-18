using rps_backend.Shared;

namespace rps_backend.Features.Prestamos.Application;

// ===== Requests =====

// Periodo: primer día del mes en que se cobra ("2026-12-01"). Corte: Quincena (el 15) o FinDeMes
// (el último día). Importe: lo que se cobra en ese pago.
public record CuotaPrestamoInput(DateOnly? Periodo, TipoCorte? Corte, decimal? Importe);

// Fecha: día en que se entrega el dinero (sin fecha: hoy, hora de Ecuador).
public record CrearPrestamoRequest(
    decimal? Monto, DateOnly? Fecha, string? Observaciones, List<CuotaPrestamoInput>? Cuotas);

// Reemplaza el monto, las observaciones y el plan. Las cuotas ya cobradas tienen que venir iguales.
public record ActualizarPrestamoRequest(
    decimal? Monto, string? Observaciones, List<CuotaPrestamoInput>? Cuotas);

// ===== Responses =====

// FechaCobro: el 15 o el último día del mes. Cobrado: lo que ya se descontó de esta cuota en las
// liquidaciones generadas (los descuentos se reparten en orden de fecha entre todas las cuotas del
// colaborador). Bloqueada: su fecha ya llegó, así que no se puede cambiar ni quitar.
public record CuotaPrestamoResponse(
    Guid Id, DateOnly Periodo, TipoCorte Corte, DateOnly FechaCobro, decimal Importe, decimal Cobrado, bool Bloqueada);

public record PrestamoResponse(
    Guid Id, Guid ColaboradorId, decimal Monto, DateOnly Fecha, string? Observaciones,
    decimal Cobrado, decimal Saldo, bool Pagado, DateOnly? ProximoCobro,
    List<CuotaPrestamoResponse> Cuotas, Guid CreadoPor, DateTime CreadoEn, DateTime? ActualizadoEn);

// Lo que la pantalla de Préstamos necesita del colaborador. PrimerMesDeCobro: el mes desde el que se
// pueden poner cuotas en un préstamo que se registra hoy. CobraEnQuincena: su frecuencia vigente es
// de dos pagos (si no, solo se le puede cobrar a fin de mes).
public record ResumenPrestamosResponse(
    Guid ColaboradorId, bool Activo, bool CobraEnQuincena, DateOnly PrimerMesDeCobro,
    int PrestamosActivos, decimal TotalPrestado, decimal Cobrado, decimal SaldoPendiente,
    DateOnly? ProximoCobroFecha, decimal ProximoCobroImporte);
