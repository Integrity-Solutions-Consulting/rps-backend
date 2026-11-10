using rps_backend.Shared;

namespace rps_backend.Features.Configuracion.Application;

// Catálogo de errores de la feature Configuracion.
public static class ConfiguracionErrores
{
    public static readonly Error RazonSocialInvalida =
        Error.Validacion("Configuracion.RazonSocialInvalida", "razonSocial", "Es obligatoria y debe tener máximo 200 caracteres.");

    public static readonly Error RucInvalido =
        Error.Validacion("Configuracion.RucInvalido", "ruc", "Debe tener exactamente 13 dígitos numéricos.");

    public static readonly Error MonedaInvalida =
        Error.Validacion("Configuracion.MonedaInvalida", "moneda", "Es obligatoria y debe ser un código de 3 letras (ej. USD).");

    public static Error DiaPagoInvalido(string campo) =>
        Error.Validacion("Configuracion.DiaPagoInvalido", campo, "Debe ser un día válido del mes (1 a 31).");

    public static Error DiasAvisoInvalido(string campo) =>
        Error.Validacion("Configuracion.DiasAvisoInvalido", campo, "Debe ser un número de días entre 1 y 365.");

    public static Error DiasAvisoFueraDeOrden(string campo) =>
        Error.Validacion("Configuracion.DiasAvisoFueraDeOrden", campo, "Debe ser menor que el aviso anterior: los avisos van de mayor a menor.");

    public static Error PorcentajeInvalido(string campo) =>
        Error.Validacion("Configuracion.PorcentajeInvalido", campo, "Es obligatorio y debe estar entre 0 y 100.");
}
