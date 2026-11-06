namespace rps_backend.Features.Configuracion.Application;

// ===== Requests =====

public record ActualizarConfiguracionRequest(
    string? RazonSocial,
    string? Ruc,
    string? Moneda,
    int? DiaPagoFinDeMes,
    int? DiaPagoDosPagos1,
    int? DiaPagoDosPagos2,
    decimal? PorcentajeMaximoAnticipo,
    decimal? PorcentajeIva,
    decimal? PorcentajeRetencionFuente,
    decimal? PorcentajeRetencionIvaProfesional,
    decimal? PorcentajeRetencionIvaJunior);

// ===== Responses =====

public record ConfiguracionResponse(
    Guid Id,
    string RazonSocial,
    string Ruc,
    string Moneda,
    int DiaPagoFinDeMes,
    int DiaPagoDosPagos1,
    int DiaPagoDosPagos2,
    decimal PorcentajeMaximoAnticipo,
    decimal PorcentajeIva,
    decimal PorcentajeRetencionFuente,
    decimal PorcentajeRetencionIvaProfesional,
    decimal PorcentajeRetencionIvaJunior,
    DateTime ActualizadoEn);
