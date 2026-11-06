using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using rps_backend.Features.Auth.Domain;

namespace rps_backend.Features.Auth.Application;

// Sin estado y sin dependencias de BD: genera y valida la forma criptográfica de los tokens.
// AuthService es quien decide cuándo emitir uno y qué hacer con la fila de BD correspondiente.
public class TokenService(IOptions<JwtSettings> jwtOptions)
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public (string Token, string Jti, DateTime ExpiraEn) GenerarAccessToken(Usuario usuario, IReadOnlyList<string> roles)
    {
        var jti = Guid.NewGuid().ToString();
        var expiraEn = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.Nombre),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiraEn,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), jti, expiraEn);
    }

    // 64 bytes aleatorios en base64: esto es lo que recibe el cliente. Solo se guarda su hash (ver HashToken).
    public (string Crudo, DateTime ExpiraEn) GenerarRefreshTokenCrudo()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var expiraEn = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays);
        return (Convert.ToBase64String(bytes), expiraEn);
    }

    // SHA256 simple (no BCrypt): el refresh token ya es alta entropía aleatoria, no una contraseña
    // elegida por una persona, así que no necesita un hash lento — y permite buscarlo por igualdad en BD.
    public static string HashToken(string tokenCrudo)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(tokenCrudo));
        return Convert.ToHexStringLower(hash);
    }
}
