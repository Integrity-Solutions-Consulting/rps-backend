using System.Security.Claims;
using rps_backend.Features.Auth.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Auth.Api;

// Administración de roles (Configuración > Roles). Solo HTTP: las reglas están en RolesService.
public static class RolesEndpoints
{
    public static IEndpointRouteBuilder MapRolesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles").WithTags("Roles")
            .RequireAuthorization(policy => policy.RequireRole(PermisosModulo.RolAdministrador, PermisosModulo.RolAdministradorContable));

        group.MapGet("/", async (ClaimsPrincipal user, RolesService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(EsAdministrador(user), ct)))
            .Produces<List<RolResponse>>();

        group.MapGet("/asignables", async (RolesService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsignablesAsync(ct)))
            .Produces<List<string>>();

        group.MapPost("/", async (CrearRolRequest request, RolesService service, CancellationToken ct) =>
                (await service.CrearAsync(request, ct)).AHttp(r => TypedResults.Created($"/api/roles/{r.Id}", r)))
            .RequireAuthorization(policy => policy.RequireRole(PermisosModulo.RolAdministrador))
            .Produces<RolResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/permisos", async (Guid id, ActualizarPermisosRolRequest request, ClaimsPrincipal user, RolesService service, CancellationToken ct) =>
                (await service.ActualizarPermisosAsync(EsAdministrador(user), id, request, ct)).AHttp(TypedResults.Ok))
            .Produces<RolResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{id:guid}/estado", async (Guid id, CambiarEstadoRolRequest request, ClaimsPrincipal user, RolesService service, CancellationToken ct) =>
                (await service.CambiarEstadoAsync(EsAdministrador(user), id, request.Activo, ct)).AHttp(TypedResults.Ok))
            .Produces<RolResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static bool EsAdministrador(ClaimsPrincipal user) => user.IsInRole(PermisosModulo.RolAdministrador);
}
