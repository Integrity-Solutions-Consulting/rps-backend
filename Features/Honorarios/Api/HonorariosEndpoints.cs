using rps_backend.Features.Honorarios.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Api;

// Solo HTTP: recibe la petición, llama al servicio y traduce el Resultado a status code.
public static class HonorariosEndpoints
{
    public static IEndpointRouteBuilder MapHonorariosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/honorarios").WithTags("Honorarios");

        group.MapPost("/generar", async (GenerarHonorariosRequest request, HonorariosService service, CancellationToken ct) =>
                (await service.GenerarAsync(request, ct)).AHttp(TypedResults.Ok))
            .Produces<GenerarHonorariosResponse>()
            .ProducesValidationProblem();

        // GET /api/honorarios?periodo=2026-11-01&colaboradorId=...&estado=Pendiente
        group.MapGet("/", async (DateOnly? periodo, Guid? colaboradorId, EstadoHonorario? estado, HonorariosService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(periodo, colaboradorId, estado, ct)));

        group.MapGet("/{id:guid}", async (Guid id, HonorariosService service, CancellationToken ct) =>
                (await service.ObtenerAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<HonorarioResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/aprobar", async (Guid id, HonorariosService service, CancellationToken ct) =>
                (await service.AprobarAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<HonorarioResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/pagar", async (Guid id, HonorariosService service, CancellationToken ct) =>
                (await service.MarcarPagadoAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<HonorarioResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/anular", async (Guid id, HonorariosService service, CancellationToken ct) =>
                (await service.AnularAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<HonorarioResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
