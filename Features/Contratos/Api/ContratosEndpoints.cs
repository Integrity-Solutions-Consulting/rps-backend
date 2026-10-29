using rps_backend.Features.Contratos.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Contratos.Api;

// Solo HTTP: recibe la petición, llama al servicio y traduce el Resultado a status code.
public static class ContratosEndpoints
{
    public static IEndpointRouteBuilder MapContratosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/colaboradores/{id:guid}/contratos").WithTags("Contratos");

        // GET /api/colaboradores/{id}/contratos?vigente=true
        group.MapGet("/", async (Guid id, bool? vigente, ContratosService service, CancellationToken ct) =>
                (await service.ListarAsync(id, vigente, ct)).AHttp(TypedResults.Ok))
            .Produces<List<ContratoResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{contratoId:guid}", async (Guid id, Guid contratoId, ContratosService service, CancellationToken ct) =>
                (await service.ObtenerAsync(id, contratoId, ct)).AHttp(TypedResults.Ok))
            .Produces<ContratoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (Guid id, CrearContratoRequest request, ContratosService service, CancellationToken ct) =>
                (await service.CrearAsync(id, request, ct)).AHttp(c => TypedResults.Created($"/api/colaboradores/{id}/contratos/{c.Id}", c)))
            .Produces<ContratoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{contratoId:guid}", async (Guid id, Guid contratoId, EditarContratoRequest request, ContratosService service, CancellationToken ct) =>
                (await service.EditarAsync(id, contratoId, request, ct)).AHttp(TypedResults.Ok))
            .Produces<ContratoResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Solo contratos que aún no han empezado; uno vigente se finaliza editando su fecha de fin.
        group.MapDelete("/{contratoId:guid}", async (Guid id, Guid contratoId, ContratosService service, CancellationToken ct) =>
                (await service.EliminarAsync(id, contratoId, ct)).AHttp(_ => TypedResults.NoContent()))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
