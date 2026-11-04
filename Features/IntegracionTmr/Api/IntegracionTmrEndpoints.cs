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

        return app;
    }
}
