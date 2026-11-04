using rps_backend.Features.Dashboard.Application;

namespace rps_backend.Features.Dashboard.Api;

public static class VencimientosContratosEndpoints
{
    public static IEndpointRouteBuilder MapVencimientosContratosEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/dashboard/vencimientos?dias=30 (dias opcional, 30 por defecto). Lo usa Inicio.
        app.MapGet("/api/dashboard/vencimientos", async (int? dias, VencimientosContratosService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(dias, ct)))
            .WithTags("Dashboard")
            .Produces<List<VencimientoContratoResponse>>();

        return app;
    }
}
