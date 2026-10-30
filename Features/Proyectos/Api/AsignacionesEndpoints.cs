using rps_backend.Features.Proyectos.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Proyectos.Api;

public static class AsignacionesEndpoints
{
    public static IEndpointRouteBuilder MapAsignacionesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/colaboradores/{id:guid}/asignaciones").WithTags("Asignaciones");

        group.MapGet("/", async (Guid id, AsignacionesService service, CancellationToken ct) =>
                (await service.ListarAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<List<AsignacionResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (Guid id, CrearAsignacionRequest request, AsignacionesService service, CancellationToken ct) =>
                (await service.CrearAsync(id, request, ct)).AHttp(a => TypedResults.Created($"/api/colaboradores/{id}/asignaciones/{a.Id}", a)))
            .Produces<AsignacionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{asignacionId:guid}/finalizar", async (Guid id, Guid asignacionId, FinalizarAsignacionRequest request, AsignacionesService service, CancellationToken ct) =>
                (await service.FinalizarAsync(id, asignacionId, request, ct)).AHttp(TypedResults.Ok))
            .Produces<AsignacionResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{asignacionId:guid}", async (Guid id, Guid asignacionId, AsignacionesService service, CancellationToken ct) =>
                (await service.EliminarAsync(id, asignacionId, ct)).AHttp(_ => TypedResults.NoContent()))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
