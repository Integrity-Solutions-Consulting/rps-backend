using rps_backend.Shared;

namespace rps_backend.Features.Anticipos.Application;

// ===== Requests =====

public record CrearAnticipoAdicionalRequest(decimal? Monto, string? Observaciones);

public record CuotaInput(DateOnly? Periodo, TipoCorte? Corte, decimal? Importe);

public record RegistrarPlanRequest(List<CuotaInput>? Cuotas);

// ===== Responses =====

public record CuotaRecuperacionResponse(Guid Id, DateOnly Periodo, TipoCorte Corte, decimal Importe);

// Lo que la pantalla de Anticipos necesita para registrar uno, calculado con las mismas reglas que
// RegistrarAsync (límite y saldo) y RegistrarPlanAsync (cortes según la frecuencia).
public record ResumenAnticiposResponse(
    Guid ColaboradorId,
    bool Activo,
    decimal NetoMensualCompleto,
    decimal PorcentajeMaximoAnticipo,
    decimal Limite,
    decimal SaldoPendiente,
    decimal Disponible,
    DateOnly Periodo,
    List<TipoCorte> CortesPermitidos,
    List<TopeCorteResponse> TopesPorCorte);

// Cuánto se le puede descontar en un corte del mes: lo que se le paga en ese corte (ValorBase:
// quincena = 50% del subtotal, fin de mes = neto - quincena) menos lo que ya tienen programado
// las cuotas de sus anticipos para ese mismo mes y corte (Comprometido).
public record TopeCorteResponse(TipoCorte Corte, decimal ValorBase, decimal Comprometido, decimal Disponible);

public record AnticipoAdicionalResponse(
    Guid Id, Guid ColaboradorId, decimal Monto, DateOnly Fecha, string? Observaciones,
    decimal SaldoPendiente, List<CuotaRecuperacionResponse> Cuotas, Guid CreadoPor, DateTime CreadoEn);
