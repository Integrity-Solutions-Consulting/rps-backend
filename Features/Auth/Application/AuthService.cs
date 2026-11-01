using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Auth.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Auth.Application;

// Reglas de autenticación. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
public class AuthService(RpsDbContext db, TokenService tokenService)
{
    private const int MaxIntentosFallidos = 5;
    private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    public async Task<Resultado<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (usuario is null || !usuario.Activo)
            return AuthErrores.CredencialesInvalidas;

        if (usuario.BloqueadoHasta is { } bloqueadoHasta && bloqueadoHasta > DateTime.UtcNow)
            return AuthErrores.CuentaBloqueada(bloqueadoHasta);

        if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.HashPassword))
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= MaxIntentosFallidos)
                usuario.BloqueadoHasta = DateTime.UtcNow.Add(DuracionBloqueo);

            await db.SaveChangesAsync(ct);
            return AuthErrores.CredencialesInvalidas;
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;

        var roles = await ObtenerNombresRolesAsync(usuario.Id, ct);

        var sesion = new Sesion { UsuarioId = usuario.Id };
        db.Sesiones.Add(sesion);

        var respuesta = EmitirTokens(usuario, sesion.Id, familiaToken: Guid.NewGuid(), roles);

        await db.SaveChangesAsync(ct);
        return respuesta;
    }

    public async Task<Resultado<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var hash = TokenService.HashToken(request.RefreshToken);
        var tokenActual = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

        if (tokenActual is null || tokenActual.Revocado)
            return AuthErrores.TokenInvalido;

        if (tokenActual.Usado)
        {
            // El token ya se usó antes: alguien más lo tiene. Se revoca toda la cadena de rotaciones.
            await db.RefreshTokens
                .Where(rt => rt.FamiliaToken == tokenActual.FamiliaToken)
                .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.Revocado, true), ct);
            return AuthErrores.TokenInvalido;
        }

        if (tokenActual.ExpiraEn < DateTime.UtcNow)
            return AuthErrores.TokenInvalido;

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == tokenActual.UsuarioId, ct);
        if (usuario is null || !usuario.Activo)
            return AuthErrores.TokenInvalido;

        tokenActual.Usado = true;

        var sesion = await db.Sesiones.FirstAsync(s => s.Id == tokenActual.SesionId, ct);
        sesion.UltimaActividad = DateTime.UtcNow;

        var roles = await ObtenerNombresRolesAsync(usuario.Id, ct);
        var respuesta = EmitirTokens(usuario, sesion.Id, tokenActual.FamiliaToken, roles);

        await db.SaveChangesAsync(ct);
        return respuesta;
    }

    public async Task<Resultado<bool>> LogoutAsync(Guid usuarioId, string jti, DateTime accessTokenExpiraEn, string? refreshTokenCrudo, CancellationToken ct)
    {
        db.TokenBlacklist.Add(new Domain.TokenBlacklist { Jti = jti, ExpiraEn = accessTokenExpiraEn });

        if (!string.IsNullOrWhiteSpace(refreshTokenCrudo))
        {
            var hash = TokenService.HashToken(refreshTokenCrudo);
            var token = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash && rt.UsuarioId == usuarioId, ct);
            if (token is not null)
            {
                token.Revocado = true;
                var sesion = await db.Sesiones.FirstOrDefaultAsync(s => s.Id == token.SesionId, ct);
                if (sesion is not null)
                    sesion.Activa = false;
            }
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Resultado<UsuarioResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Usuarios.AnyAsync(u => u.Email == email, ct))
            return AuthErrores.EmailDuplicado(email);

        if (!EsPasswordValida(request.Password))
            return AuthErrores.PasswordInvalida;

        var rol = await db.Roles.FirstOrDefaultAsync(r => r.Nombre == request.Rol && r.Activo, ct);
        if (rol is null)
            return AuthErrores.RolInvalido;

        var usuario = new Usuario
        {
            Email = email,
            Nombre = request.Nombre.Trim(),
            HashPassword = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };
        db.Usuarios.Add(usuario);
        db.UsuariosRoles.Add(new UsuarioRol { UsuarioId = usuario.Id, RolId = rol.Id });

        await db.SaveChangesAsync(ct);
        return new UsuarioResponse(usuario.Id, usuario.Email, usuario.Nombre, [rol.Nombre]);
    }

    public async Task<Resultado<bool>> ChangePasswordAsync(Guid usuarioId, ChangePasswordRequest request, CancellationToken ct)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        if (usuario is null)
            return AuthErrores.CredencialesInvalidas;

        if (!BCrypt.Net.BCrypt.Verify(request.PasswordActual, usuario.HashPassword))
            return AuthErrores.PasswordActualIncorrecta;

        if (!EsPasswordValida(request.PasswordNueva))
            return AuthErrores.PasswordInvalida;

        usuario.HashPassword = BCrypt.Net.BCrypt.HashPassword(request.PasswordNueva);
        usuario.DebeCambiarPassword = false;
        usuario.ActualizadoEn = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Resultado<UsuarioResponse>> ObtenerPerfilAsync(Guid usuarioId, CancellationToken ct)
    {
        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        if (usuario is null)
            return AuthErrores.CredencialesInvalidas;

        var roles = await ObtenerNombresRolesAsync(usuario.Id, ct);
        return new UsuarioResponse(usuario.Id, usuario.Email, usuario.Nombre, roles);
    }

    // El "jti" del access token no se guarda aquí: solo se necesita en logout, y ahí se lee
    // directamente del claim del token vigente (ver AuthEndpoints), no de la emisión original.
    private AuthResponse EmitirTokens(Usuario usuario, Guid sesionId, Guid familiaToken, IReadOnlyList<string> roles)
    {
        var (accessToken, _, accessExpiraEn) = tokenService.GenerarAccessToken(usuario, roles);
        var (refreshCrudo, refreshExpiraEn) = tokenService.GenerarRefreshTokenCrudo();

        db.RefreshTokens.Add(new RefreshToken
        {
            SesionId = sesionId,
            UsuarioId = usuario.Id,
            TokenHash = TokenService.HashToken(refreshCrudo),
            FamiliaToken = familiaToken,
            ExpiraEn = refreshExpiraEn,
        });

        return new AuthResponse(accessToken, refreshCrudo, accessExpiraEn, new UsuarioResponse(usuario.Id, usuario.Email, usuario.Nombre, roles));
    }

    private async Task<List<string>> ObtenerNombresRolesAsync(Guid usuarioId, CancellationToken ct) =>
        await db.UsuariosRoles
            .Where(ur => ur.UsuarioId == usuarioId)
            .Join(db.Roles.Where(r => r.Activo), ur => ur.RolId, r => r.Id, (ur, r) => r.Nombre)
            .ToListAsync(ct);

    private static bool EsPasswordValida(string password) =>
        password.Length >= 10 && password.Any(char.IsLetter) && password.Any(char.IsDigit);
}
