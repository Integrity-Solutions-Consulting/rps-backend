using rps_backend.Features.Historial.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Historial.Api;

public static class HistorialEndpoints
{
    public static IEndpointRouteBuilder MapHistorialEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/colaboradores/{id}/historial
        app.MapGet("/api/colaboradores/{id:guid}/historial", async (Guid id, HistorialService service, CancellationToken ct) =>
                (await service.ListarAsync(id, ct)).AHttp(TypedResults.Ok))
            .WithTags("Historial")
            .Produces<List<EventoHistorialResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
