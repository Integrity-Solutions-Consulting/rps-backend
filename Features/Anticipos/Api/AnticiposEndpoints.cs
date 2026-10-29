using rps_backend.Features.Anticipos.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Anticipos.Api;

// Solo HTTP: recibe la petición, llama al servicio y traduce el Resultado a status code.
public static class AnticiposEndpoints
{
    public static IEndpointRouteBuilder MapAnticiposEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/colaboradores/{id:guid}/anticipos").WithTags("Anticipos");

        // GET /api/colaboradores/{id}/anticipos?estado=Pendiente
        group.MapGet("/", async (Guid id, EstadoAnticipo? estado, AnticiposService service, CancellationToken ct) =>
                (await service.ListarAsync(id, estado, ct)).AHttp(TypedResults.Ok))
            .Produces<List<AnticipoResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{anticipoId:guid}", async (Guid id, Guid anticipoId, AnticiposService service, CancellationToken ct) =>
                (await service.ObtenerAsync(id, anticipoId, ct)).AHttp(TypedResults.Ok))
            .Produces<AnticipoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (Guid id, CrearAnticipoRequest request, AnticiposService service, CancellationToken ct) =>
                (await service.CrearAsync(id, request, ct)).AHttp(a => TypedResults.Created($"/api/colaboradores/{id}/anticipos/{a.Id}", a)))
            .Produces<AnticipoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{anticipoId:guid}/aprobar", async (Guid id, Guid anticipoId, AnticiposService service, CancellationToken ct) =>
                (await service.AprobarAsync(id, anticipoId, ct)).AHttp(TypedResults.Ok))
            .Produces<AnticipoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{anticipoId:guid}/anular", async (Guid id, Guid anticipoId, AnticiposService service, CancellationToken ct) =>
                (await service.AnularAsync(id, anticipoId, ct)).AHttp(TypedResults.Ok))
            .Produces<AnticipoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
