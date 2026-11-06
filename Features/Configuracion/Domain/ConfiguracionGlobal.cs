namespace rps_backend.Features.Configuracion.Domain;

public class ConfiguracionGlobal
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string RazonSocial { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public string Moneda { get; set; } = "USD";

    public int DiaPagoFinDeMes { get; set; } = 30;

    public int DiaPagoDosPagos1 { get; set; } = 15;

    public int DiaPagoDosPagos2 { get; set; } = 30;

    // Todos los porcentajes se guardan en puntos (15 = 15%), no como fracción.

    // Límite de un Anticipo adicional: % del neto mensual completo consolidado (RN 42-43).
    public decimal PorcentajeMaximoAnticipo { get; set; } = 100m;

    // Parámetros tributarios de la sección 7 del documento de requisitos. Honorario los
    // snapshotea al calcularse (RN 29): cambiarlos aquí no altera cálculos ya generados.
    public decimal PorcentajeIva { get; set; } = 15m;

    public decimal PorcentajeRetencionFuente { get; set; } = 10m;

    public decimal PorcentajeRetencionIvaProfesional { get; set; } = 100m;

    public decimal PorcentajeRetencionIvaJunior { get; set; } = 70m;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
