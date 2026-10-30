using Microsoft.EntityFrameworkCore;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Configuracion.Application;

// Reglas de negocio de Configuracion. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
// Es un singleton: la fila la siembra la migración, esta capa solo lee/actualiza, nunca crea ni elimina.
public class ConfiguracionService(RpsDbContext db)
{
    public async Task<ConfiguracionResponse> ObtenerAsync(CancellationToken ct)
    {
        var config = await ObtenerOCrearAsync(ct);
        return AResponse(config);
    }

    public async Task<Resultado<ConfiguracionResponse>> ActualizarAsync(ActualizarConfiguracionRequest request, CancellationToken ct)
    {
        var razonSocial = request.RazonSocial?.Trim();
        if (string.IsNullOrEmpty(razonSocial) || razonSocial.Length > 200)
            return ConfiguracionErrores.RazonSocialInvalida;

        var ruc = request.Ruc?.Trim();
        if (string.IsNullOrEmpty(ruc) || ruc.Length > 20)
            return ConfiguracionErrores.RucInvalido;

        var moneda = request.Moneda?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(moneda) || moneda.Length != 3)
            return ConfiguracionErrores.MonedaInvalida;

        if (request.DiaPagoFinDeMes is not (>= 1 and <= 31))
            return ConfiguracionErrores.DiaPagoInvalido("diaPagoFinDeMes");

        if (request.DiaPagoDosPagos1 is not (>= 1 and <= 31))
            return ConfiguracionErrores.DiaPagoInvalido("diaPagoDosPagos1");

        if (request.DiaPagoDosPagos2 is not (>= 1 and <= 31))
            return ConfiguracionErrores.DiaPagoInvalido("diaPagoDosPagos2");

        if (request.PorcentajeMaximoAnticipo is not (>= 0 and <= 100))
            return ConfiguracionErrores.PorcentajeInvalido("porcentajeMaximoAnticipo");

        if (request.PorcentajeIva is not (>= 0 and <= 100))
            return ConfiguracionErrores.PorcentajeInvalido("porcentajeIva");

        if (request.PorcentajeRetencionFuente is not (>= 0 and <= 100))
            return ConfiguracionErrores.PorcentajeInvalido("porcentajeRetencionFuente");

        if (request.PorcentajeRetencionIvaProfesional is not (>= 0 and <= 100))
            return ConfiguracionErrores.PorcentajeInvalido("porcentajeRetencionIvaProfesional");

        if (request.PorcentajeRetencionIvaJunior is not (>= 0 and <= 100))
            return ConfiguracionErrores.PorcentajeInvalido("porcentajeRetencionIvaJunior");

        var config = await ObtenerOCrearAsync(ct);

        config.RazonSocial = razonSocial;
        config.Ruc = ruc;
        config.Moneda = moneda;
        config.DiaPagoFinDeMes = request.DiaPagoFinDeMes.Value;
        config.DiaPagoDosPagos1 = request.DiaPagoDosPagos1.Value;
        config.DiaPagoDosPagos2 = request.DiaPagoDosPagos2.Value;
        config.PorcentajeMaximoAnticipo = request.PorcentajeMaximoAnticipo.Value;
        config.PorcentajeIva = request.PorcentajeIva.Value;
        config.PorcentajeRetencionFuente = request.PorcentajeRetencionFuente.Value;
        config.PorcentajeRetencionIvaProfesional = request.PorcentajeRetencionIvaProfesional.Value;
        config.PorcentajeRetencionIvaJunior = request.PorcentajeRetencionIvaJunior.Value;
        config.ActualizadoEn = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return AResponse(config);
    }

    // Ambiente sin la fila semilla (migración no aplicada todavía): se autogenera con los
    // valores por defecto en vez de que GET/PUT /api/configuracion truene con un 500.
    private async Task<Domain.ConfiguracionGlobal> ObtenerOCrearAsync(CancellationToken ct)
    {
        var config = await db.ConfiguracionGlobal.FirstOrDefaultAsync(ct);
        if (config is not null)
            return config;

        config = new Domain.ConfiguracionGlobal();
        db.ConfiguracionGlobal.Add(config);
        await db.SaveChangesAsync(ct);

        return config;
    }

    private static ConfiguracionResponse AResponse(Domain.ConfiguracionGlobal c) => new(
        c.Id, c.RazonSocial, c.Ruc, c.Moneda, c.DiaPagoFinDeMes, c.DiaPagoDosPagos1, c.DiaPagoDosPagos2,
        c.PorcentajeMaximoAnticipo, c.PorcentajeIva, c.PorcentajeRetencionFuente,
        c.PorcentajeRetencionIvaProfesional, c.PorcentajeRetencionIvaJunior, c.ActualizadoEn);
}
