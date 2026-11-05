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
    List<TipoCorte> CortesPermitidos);

public record AnticipoAdicionalResponse(
    Guid Id, Guid ColaboradorId, decimal Monto, DateOnly Fecha, string? Observaciones,
    decimal SaldoPendiente, List<CuotaRecuperacionResponse> Cuotas, DateTime CreadoEn);
