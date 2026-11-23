namespace rps_backend.Features.Auth.Application;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "rps-backend";

    public string Audience { get; set; } = "rps-frontend";

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 7;

    // Límites de la sesión (no del token): aunque el refresh token siga vigente, la sesión se
    // cierra si pasa este tiempo sin usarse o si supera la duración máxima desde el login.
    public int SesionInactividadMinutos { get; set; } = 60;

    public int SesionDuracionMaximaHoras { get; set; } = 8;
}
