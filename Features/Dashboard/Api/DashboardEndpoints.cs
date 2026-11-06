using rps_backend.Features.Dashboard.Application;

namespace rps_backend.Features.Dashboard.Api;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

        // GET /api/dashboard?periodo=2026-11-01 (periodo opcional; filtra solo Honorarios/Liquidaciones)
        group.MapGet("/", async (DateOnly? periodo, DashboardService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ObtenerAsync(periodo, ct)))
            .Produces<DashboardResponse>();

        return app;
    }
}
