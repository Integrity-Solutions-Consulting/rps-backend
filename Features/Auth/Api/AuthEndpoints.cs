using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using rps_backend.Features.Auth.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Auth.Api;

// Solo HTTP: recibe la petición, llama al servicio y traduce el Resultado a status code.
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, AuthService service, CancellationToken ct) =>
                (await service.LoginAsync(request, ct)).AHttp(TypedResults.Ok))
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .Produces<AuthResponse>()
            .ProducesValidationProblem();

        group.MapPost("/refresh-token", async (RefreshTokenRequest request, AuthService service, CancellationToken ct) =>
                (await service.RefreshTokenAsync(request, ct)).AHttp(TypedResults.Ok))
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .Produces<AuthResponse>()
            .ProducesValidationProblem();

        group.MapPost("/logout", async (LogoutRequest? request, ClaimsPrincipal user, AuthService service, CancellationToken ct) =>
            {
                var usuarioId = ObtenerUsuarioId(user);
                var jti = user.FindFirstValue(JwtRegisteredClaimNames.Jti)!;
                var expiraEn = DateTimeOffset.FromUnixTimeSeconds(long.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Exp)!)).UtcDateTime;

                return (await service.LogoutAsync(usuarioId, jti, expiraEn, request?.RefreshToken, ct))
                    .AHttp(_ => TypedResults.NoContent());
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/change-password", async (ChangePasswordRequest request, ClaimsPrincipal user, AuthService service, CancellationToken ct) =>
                (await service.ChangePasswordAsync(ObtenerUsuarioId(user), request, ct)).AHttp(_ => TypedResults.Ok()))
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        // Técnico, no contable: solo quien administra el sistema crea cuentas nuevas.
        group.MapPost("/register", async (RegisterRequest request, AuthService service, CancellationToken ct) =>
                (await service.RegisterAsync(request, ct)).AHttp(u => TypedResults.Created($"/api/auth/usuarios/{u.Id}", u)))
            .RequireAuthorization(policy => policy.RequireRole("Administrador"))
            .Produces<UsuarioResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/me", async (ClaimsPrincipal user, AuthService service, CancellationToken ct) =>
                (await service.ObtenerPerfilAsync(ObtenerUsuarioId(user), ct)).AHttp(TypedResults.Ok))
            .RequireAuthorization()
            .Produces<UsuarioResponse>();

        return app;
    }

    private static Guid ObtenerUsuarioId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
