namespace rps_backend.Features.Configuracion.Application;

// ===== Requests =====

public record ActualizarConfiguracionRequest(
    string? RazonSocial,
    string? Ruc,
    string? Moneda,
    int? DiaPagoFinDeMes,
    int? DiaPagoDosPagos1,
    int? DiaPagoDosPagos2,
    decimal? TopeMaximoAnticipo);

// ===== Responses =====

public record ConfiguracionResponse(
    Guid Id,
    string RazonSocial,
    string Ruc,
    string Moneda,
    int DiaPagoFinDeMes,
    int DiaPagoDosPagos1,
    int DiaPagoDosPagos2,
    decimal TopeMaximoAnticipo,
    DateTime ActualizadoEn);
