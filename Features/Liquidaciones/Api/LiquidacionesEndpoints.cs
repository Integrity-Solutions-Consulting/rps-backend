using rps_backend.Features.Liquidaciones.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Api;

// Solo HTTP: recibe la petición, llama al servicio y traduce el Resultado a status code.
public static class LiquidacionesEndpoints
{
    public static IEndpointRouteBuilder MapLiquidacionesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/liquidaciones").WithTags("Liquidaciones");

        group.MapPost("/generar", async (GenerarLiquidacionesRequest request, LiquidacionesService service, CancellationToken ct) =>
                (await service.GenerarAsync(request, ct)).AHttp(TypedResults.Ok))
            .Produces<GenerarLiquidacionesResponse>()
            .ProducesValidationProblem();

        // GET /api/liquidaciones?periodo=2026-11-01&colaboradorId=...&revisado=false
        group.MapGet("/", async (DateOnly? periodo, Guid? colaboradorId, bool? revisado, LiquidacionesService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(periodo, colaboradorId, revisado, ct)));

        group.MapGet("/{id:guid}", async (Guid id, LiquidacionesService service, CancellationToken ct) =>
                (await service.ObtenerAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<LiquidacionDetalleResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Control interno previo a exportar (RN 49); no equivale a pago realizado.
        group.MapPost("/{id:guid}/revisar", async (Guid id, LiquidacionesService service, CancellationToken ct) =>
                (await service.MarcarRevisadaAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<LiquidacionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
