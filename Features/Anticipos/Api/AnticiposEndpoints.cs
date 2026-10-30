using rps_backend.Features.Anticipos.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Anticipos.Api;

// Solo HTTP: recibe la petición, llama al servicio y traduce el Resultado a status code.
public static class AnticiposEndpoints
{
    public static IEndpointRouteBuilder MapAnticiposEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/colaboradores/{id:guid}/anticipos").WithTags("Anticipos");

        group.MapGet("/", async (Guid id, AnticiposService service, CancellationToken ct) =>
                (await service.ListarAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<List<AnticipoAdicionalResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{anticipoId:guid}", async (Guid id, Guid anticipoId, AnticiposService service, CancellationToken ct) =>
                (await service.ObtenerAsync(id, anticipoId, ct)).AHttp(TypedResults.Ok))
            .Produces<AnticipoAdicionalResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (Guid id, CrearAnticipoAdicionalRequest request, AnticiposService service, CancellationToken ct) =>
                (await service.RegistrarAsync(id, request, ct)).AHttp(a => TypedResults.Created($"/api/colaboradores/{id}/anticipos/{a.Id}", a)))
            .Produces<AnticipoAdicionalResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Reemplaza el plan de recuperación por completo (RN 44-47).
        group.MapPut("/{anticipoId:guid}/plan", async (Guid id, Guid anticipoId, RegistrarPlanRequest request, AnticiposService service, CancellationToken ct) =>
                (await service.RegistrarPlanAsync(id, anticipoId, request, ct)).AHttp(TypedResults.Ok))
            .Produces<AnticipoAdicionalResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{anticipoId:guid}", async (Guid id, Guid anticipoId, AnticiposService service, CancellationToken ct) =>
                (await service.EliminarAsync(id, anticipoId, ct)).AHttp(_ => TypedResults.NoContent()))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
