using rps_backend.Shared;

namespace rps_backend.Features.Configuracion.Application;

// Catálogo de errores de la feature Configuracion.
public static class ConfiguracionErrores
{
    public static readonly Error RazonSocialInvalida =
        Error.Validacion("Configuracion.RazonSocialInvalida", "razonSocial", "Es obligatoria y debe tener máximo 200 caracteres.");

    public static readonly Error RucInvalido =
        Error.Validacion("Configuracion.RucInvalido", "ruc", "Es obligatorio y debe tener máximo 20 caracteres.");

    public static readonly Error MonedaInvalida =
        Error.Validacion("Configuracion.MonedaInvalida", "moneda", "Es obligatoria y debe ser un código de 3 letras (ej. USD).");

    public static Error DiaPagoInvalido(string campo) =>
        Error.Validacion("Configuracion.DiaPagoInvalido", campo, "Debe ser un día válido del mes (1 a 31).");

    public static readonly Error TopeMaximoAnticipoInvalido =
        Error.Validacion("Configuracion.TopeMaximoAnticipoInvalido", "topeMaximoAnticipo", "Es obligatorio y debe ser mayor a 0.");
}
