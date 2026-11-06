using rps_backend.Features.Proyectos.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Proyectos.Api;

public static class ProyectosEndpoints
{
    public static IEndpointRouteBuilder MapProyectosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proyectos").WithTags("Proyectos");

        group.MapGet("/", async (bool? activo, ProyectosService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(activo, ct)));

        group.MapGet("/{id:guid}", async (Guid id, ProyectosService service, CancellationToken ct) =>
                (await service.ObtenerAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<ProyectoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CrearProyectoRequest request, ProyectosService service, CancellationToken ct) =>
                (await service.CrearAsync(request, ct)).AHttp(p => TypedResults.Created($"/api/proyectos/{p.Id}", p)))
            .Produces<ProyectoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", async (Guid id, EditarProyectoRequest request, ProyectosService service, CancellationToken ct) =>
                (await service.EditarAsync(id, request, ct)).AHttp(TypedResults.Ok))
            .Produces<ProyectoResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/desactivar", async (Guid id, ProyectosService service, CancellationToken ct) =>
                (await service.CambiarActivoAsync(id, false, ct)).AHttp(TypedResults.Ok))
            .Produces<ProyectoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/activar", async (Guid id, ProyectosService service, CancellationToken ct) =>
                (await service.CambiarActivoAsync(id, true, ct)).AHttp(TypedResults.Ok))
            .Produces<ProyectoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
