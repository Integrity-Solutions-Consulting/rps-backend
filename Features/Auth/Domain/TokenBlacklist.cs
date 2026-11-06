namespace rps_backend.Features.Auth.Domain;

// Permite invalidar un access token antes de que expire solo (logout). Se identifica por su
// "jti" (claim único por token); el middleware de JWT rechaza cualquier token cuyo jti esté aquí.
public class TokenBlacklist
{
    public string Jti { get; set; } = string.Empty;

    public DateTime ExpiraEn { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
