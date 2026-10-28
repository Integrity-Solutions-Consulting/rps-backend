using Microsoft.AspNetCore.DataProtection;

namespace rps_backend.Features.Colaboradores.Application;

// Cifra y descifra el número de cuenta con ASP.NET Core Data Protection.
// Si se pierden las llaves de Data Protection, los números guardados ya no se pueden descifrar.
public class CuentaBancariaProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector =
        provider.CreateProtector("rps-backend.CuentaBancaria.NumeroCuenta");

    public string Cifrar(string numeroCuenta) => _protector.Protect(numeroCuenta);

    public string Descifrar(string numeroCuentaCifrado) => _protector.Unprotect(numeroCuentaCifrado);

    public string Enmascarar(string numeroCuentaCifrado)
    {
        var numero = Descifrar(numeroCuentaCifrado);
        return numero.Length <= 4
            ? new string('*', numero.Length)
            : "****" + numero[^4..];
    }
}
