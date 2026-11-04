using rps_backend.Shared;

namespace rps_backend.Features.Colaboradores.Application;

// ===== Requests =====

public record CrearColaboradorRequest(string? TmrColaboradorId, Clasificacion? Clasificacion);

public record ActualizarClasificacionRequest(Clasificacion? Clasificacion);

public record RegistrarFrecuenciaRequest(Frecuencia? Frecuencia, DateOnly? VigenteDesdeMes);

public record RegistrarCuentaBancariaRequest(string? Banco, string? TipoCuenta, string? NumeroCuenta);

// ===== Responses =====

// Nombres, Apellidos, Identificacion, Correo, ActivoEnTmr, SincronizadoEn y EliminadoEnOrigen son información
// de origen TMR (solo lectura en RPS); nulos si el registro se creó a mano.
public record ColaboradorResumenResponse(
    Guid Id,
    string TmrColaboradorId,
    string? Nombres,
    string? Apellidos,
    string? Identificacion,
    string? Correo,
    bool? ActivoEnTmr,
    DateTime? SincronizadoEn,
    bool EliminadoEnOrigen,
    Clasificacion? Clasificacion,
    EstadoConfiguracion EstadoConfiguracion,
    bool Activo,
    DateTime CreadoEn,
    DateTime ActualizadoEn);

public record ColaboradorDetalleResponse(
    Guid Id,
    string TmrColaboradorId,
    string? Nombres,
    string? Apellidos,
    string? Identificacion,
    string? Correo,
    bool? ActivoEnTmr,
    DateTime? SincronizadoEn,
    bool EliminadoEnOrigen,
    Clasificacion? Clasificacion,
    EstadoConfiguracion EstadoConfiguracion,
    bool Activo,
    DateTime CreadoEn,
    DateTime ActualizadoEn,
    FrecuenciaResponse? FrecuenciaVigente,
    CuentaBancariaResponse? CuentaBancaria);

public record FrecuenciaResponse(Guid Id, Frecuencia Frecuencia, DateOnly VigenteDesdeMes);

// El número de cuenta nunca se devuelve completo, solo enmascarado (****1234).
public record CuentaBancariaResponse(Guid Id, string? Banco, string? TipoCuenta, string NumeroCuentaEnmascarado);
