using rps_backend.Features.Honorarios.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Api;

// Endpoints nuevos junto a los de Condiciones Económicas: ver todas (con canceladas), cancelar y restaurar.
public static class CancelacionCondicionesEndpoints
{
    public static IEndpointRouteBuilder MapCancelacionCondicionesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/colaboradores/{id:guid}/condiciones-economicas").WithTags("Honorarios");

        group.MapGet("/historial", async (Guid id, CancelacionCondicionesService service, CancellationToken ct) =>
                (await service.ListarConCanceladasAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<List<CondicionEconomicaConEstadoResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{condicionId:guid}/cancelar", async (Guid id, Guid condicionId, CancelacionCondicionesService service, CancellationToken ct) =>
                (await service.CancelarAsync(id, condicionId, ct)).AHttp(TypedResults.Ok))
            .Produces<CondicionEconomicaConEstadoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{condicionId:guid}/restaurar", async (Guid id, Guid condicionId, CancelacionCondicionesService service, CancellationToken ct) =>
                (await service.RestaurarAsync(id, condicionId, ct)).AHttp(TypedResults.Ok))
            .Produces<CondicionEconomicaConEstadoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
