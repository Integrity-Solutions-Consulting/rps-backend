using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using rps_backend.Features.Auth.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Auth.Api;

// Administración de cuentas (Configuración > Roles / Usuarios). Solo HTTP: las reglas están en UsuariosService.
public static class UsuariosEndpoints
{
    public static IEndpointRouteBuilder MapUsuariosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usuarios").WithTags("Usuarios")
            .RequireAuthorization(policy => policy.RequireRole("Administrador", "Administrador Contable"));

        group.MapGet("/", async (UsuariosService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(ct)))
            .Produces<List<UsuarioAdminResponse>>();

        group.MapPatch("/{id:guid}/estado", async (Guid id, CambiarEstadoUsuarioRequest request, ClaimsPrincipal user, UsuariosService service, CancellationToken ct) =>
                (await service.CambiarEstadoAsync(
                    Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!),
                    user.IsInRole("Administrador"),
                    id, request.Activo, ct)).AHttp(TypedResults.Ok))
            .Produces<UsuarioAdminResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Administrador y Administrador Contable (este último con límites, ver UsuariosService.CambiarRolAsync).
        group.MapPut("/{id:guid}/rol", async (Guid id, CambiarRolUsuarioRequest request, ClaimsPrincipal user, UsuariosService service, CancellationToken ct) =>
                (await service.CambiarRolAsync(
                    Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!),
                    user.IsInRole("Administrador"),
                    id, request.Rol, ct)).AHttp(TypedResults.Ok))
            .Produces<UsuarioAdminResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
