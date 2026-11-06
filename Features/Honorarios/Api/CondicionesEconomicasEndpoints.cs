using rps_backend.Features.Honorarios.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Api;

public static class CondicionesEconomicasEndpoints
{
    public static IEndpointRouteBuilder MapCondicionesEconomicasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/colaboradores/{id:guid}/condiciones-economicas").WithTags("Honorarios");

        group.MapGet("/", async (Guid id, CondicionesEconomicasService service, CancellationToken ct) =>
                (await service.ListarAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<List<CondicionEconomicaResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (Guid id, CrearCondicionEconomicaRequest request, CondicionesEconomicasService service, CancellationToken ct) =>
                (await service.CrearAsync(id, request, ct)).AHttp(c => TypedResults.Created($"/api/colaboradores/{id}/condiciones-economicas/{c.Id}", c)))
            .Produces<CondicionEconomicaResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
