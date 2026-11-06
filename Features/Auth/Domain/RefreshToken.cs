namespace rps_backend.Features.Auth.Domain;

// Un token por cada login/refresh (rotación). FamiliaToken enlaza toda la cadena de rotaciones
// de una misma sesión: si un token ya marcado Usado vuelve a presentarse, es señal de robo y se
// revoca la familia completa (RefreshToken.Revocado = true en todos sus eslabones).
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid SesionId { get; set; }

    public Guid UsuarioId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public Guid FamiliaToken { get; set; }

    public bool Usado { get; set; }

    public bool Revocado { get; set; }

    public DateTime ExpiraEn { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
