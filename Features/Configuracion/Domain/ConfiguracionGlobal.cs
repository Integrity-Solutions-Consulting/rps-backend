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

    public decimal TopeMaximoAnticipo { get; set; }

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
