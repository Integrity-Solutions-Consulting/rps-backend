namespace rps_backend.Features.Colaboradores.Domain;

public class CuentaBancaria
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public string? Banco { get; set; }

    public string? TipoCuenta { get; set; }

    public string NumeroCuentaCifrado { get; set; } = string.Empty;

    public bool Activa { get; set; } = true;
}
