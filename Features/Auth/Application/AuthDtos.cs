namespace rps_backend.Features.Auth.Application;

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiraEn,
    UsuarioResponse Usuario);

public record RefreshTokenRequest(string RefreshToken);

public record LogoutRequest(string? RefreshToken);

public record RegisterRequest(string Email, string Nombre, string Password, string Rol);

public record ChangePasswordRequest(string PasswordActual, string PasswordNueva);

public record UsuarioResponse(Guid Id, string Email, string Nombre, IReadOnlyList<string> Roles);
