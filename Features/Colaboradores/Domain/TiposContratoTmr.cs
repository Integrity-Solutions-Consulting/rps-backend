using rps_backend.Shared;

namespace rps_backend.Features.Colaboradores.Domain;

// Tipo de contrato de TMR (catálogo TCT, por código; los nombres del catálogo pueden cambiar).
// RPS es para quienes facturan: los afiliados (FJ) y los que no tienen tipo no entran.
public static class TiposContratoTmr
{
    public const string ServiciosProfesionales = "CON";
    public const string ServiciosJuniors = "PAS";
    public const string MedioTiempo = "MT";
    public const string ConsultorExterno = "COS";
    public const string Afiliado = "FJ";

    public static bool Factura(string? codigo) =>
        codigo is ServiciosProfesionales or ServiciosJuniors or MedioTiempo or ConsultorExterno;

    // CON y PAS ya dicen si es Profesional o Junior. MT y COS no: lo elige Contabilidad en RPS.
    public static Clasificacion? ClasificacionFija(string? codigo) => codigo switch
    {
        ServiciosProfesionales => Clasificacion.Profesional,
        ServiciosJuniors => Clasificacion.Junior,
        _ => null
    };

    public static string Describir(string? codigo) => codigo switch
    {
        ServiciosProfesionales => "Servicios profesionales",
        ServiciosJuniors => "Servicios juniors",
        MedioTiempo => "Medio tiempo",
        ConsultorExterno => "Consultor externo",
        Afiliado => "Afiliado",
        null => "Sin tipo de contrato",
        _ => codigo
    };
}
