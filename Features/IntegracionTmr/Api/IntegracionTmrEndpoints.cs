using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using rps_backend.Features.IntegracionTmr.Application;
using rps_backend.Shared;

namespace rps_backend.Features.IntegracionTmr.Api;

public static class IntegracionTmrEndpoints
{
    public static IEndpointRouteBuilder MapIntegracionTmrEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/integracion-tmr").WithTags("Integración TMR");

        group.MapPost("/sincronizar", async (SincronizacionTmrService service, CancellationToken ct) =>
                (await service.SincronizarAsync(ct)).AHttp(TypedResults.Ok))
            .Produces<SincronizacionTmrResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        // GET /api/integracion-tmr/cambios?revisado=false&colaboradorId=...
        group.MapGet("/cambios", async (bool? revisado, Guid? colaboradorId, CambiosTmrService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(revisado, colaboradorId, ct)));

        group.MapPost("/cambios/{id:guid}/revisar", async (Guid id, ClaimsPrincipal user, CambiosTmrService service, CancellationToken ct) =>
                (await service.MarcarRevisadoAsync(id, Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!), ct)).AHttp(TypedResults.Ok))
            .Produces<CambioTmrResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
