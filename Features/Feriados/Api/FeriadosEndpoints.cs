using rps_backend.Features.Feriados.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Feriados.Api;

// Solo HTTP: recibe la petición, llama al servicio y traduce el Resultado a status code.
public static class FeriadosEndpoints
{
    public static IEndpointRouteBuilder MapFeriadosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/feriados").WithTags("Feriados");

        group.MapGet("/", async (FeriadosService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(ct)))
            .Produces<List<FeriadoResponse>>();

        group.MapPost("/", async (CrearFeriadoRequest request, FeriadosService service, CancellationToken ct) =>
                (await service.CrearAsync(request, ct)).AHttp(f => TypedResults.Created($"/api/feriados/{f.Id}", f)))
            .Produces<FeriadoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Importa los feriados nacionales de Ecuador del año indicado desde Nager.Date (idempotente).
        group.MapPost("/importar/{anio:int}", async (int anio, FeriadosService service, CancellationToken ct) =>
                (await service.ImportarAsync(anio, ct)).AHttp(TypedResults.Ok))
            .Produces<ImportarFeriadosResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", async (Guid id, FeriadosService service, CancellationToken ct) =>
                (await service.EliminarAsync(id, ct)).AHttp(_ => TypedResults.NoContent()))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
