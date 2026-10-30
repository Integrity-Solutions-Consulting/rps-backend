using rps_backend.Features.Notificaciones.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Notificaciones.Api;

public static class NotificacionesEndpoints
{
    public static IEndpointRouteBuilder MapNotificacionesEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/notificaciones?periodo=2026-10 (sin período: el mes en curso)
        app.MapGet("/api/notificaciones", async (string? periodo, NotificacionesService service, CancellationToken ct) =>
                (await service.ListarAsync(periodo, ct)).AHttp(TypedResults.Ok))
            .WithTags("Notificaciones")
            .Produces<List<NotificacionResponse>>()
            .ProducesValidationProblem();

        return app;
    }
}
