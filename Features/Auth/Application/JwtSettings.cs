namespace rps_backend.Features.Auth.Application;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "rps-backend";

    public string Audience { get; set; } = "rps-frontend";

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 7;
}
