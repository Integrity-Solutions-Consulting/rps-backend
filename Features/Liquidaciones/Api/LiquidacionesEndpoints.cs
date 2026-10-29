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

        // GET /api/liquidaciones?periodo=2026-11-01&colaboradorId=...&estado=Generada
        group.MapGet("/", async (DateOnly? periodo, Guid? colaboradorId, EstadoLiquidacion? estado, LiquidacionesService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(periodo, colaboradorId, estado, ct)));

        group.MapGet("/{id:guid}", async (Guid id, LiquidacionesService service, CancellationToken ct) =>
                (await service.ObtenerAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<LiquidacionDetalleResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Marca la liquidación como Pagada y propaga el estado a los honorarios que agrupa.
        group.MapPost("/{id:guid}/pagar", async (Guid id, LiquidacionesService service, CancellationToken ct) =>
                (await service.MarcarPagadaAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<LiquidacionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
