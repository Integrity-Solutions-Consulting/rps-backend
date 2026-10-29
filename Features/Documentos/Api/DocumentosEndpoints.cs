using Microsoft.AspNetCore.Mvc;
using rps_backend.Features.Documentos.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Documentos.Api;

// Solo HTTP: recibe la petición, llama al servicio y traduce el Resultado a status code.
public static class DocumentosEndpoints
{
    public static IEndpointRouteBuilder MapDocumentosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documentos").WithTags("Documentos");

        // multipart/form-data: archivo + exactamente una de colaboradorId/contratoId/liquidacionId.
        group.MapPost("/", async (
                IFormFile archivo,
                [FromForm] Guid? colaboradorId,
                [FromForm] Guid? contratoId,
                [FromForm] Guid? liquidacionId,
                DocumentosService service,
                CancellationToken ct) =>
                (await service.SubirAsync(archivo, colaboradorId, contratoId, liquidacionId, ct))
                    .AHttp(d => TypedResults.Created($"/api/documentos/{d.Id}", d)))
            .DisableAntiforgery()
            .Produces<DocumentoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // GET /api/documentos?colaboradorId=&contratoId=&liquidacionId=
        group.MapGet("/", async (Guid? colaboradorId, Guid? contratoId, Guid? liquidacionId, DocumentosService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(colaboradorId, contratoId, liquidacionId, ct)));

        group.MapGet("/{id:guid}", async (Guid id, DocumentosService service, CancellationToken ct) =>
                (await service.ObtenerAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<DocumentoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/descargar", async (Guid id, DocumentosService service, CancellationToken ct) =>
                (await service.ObtenerArchivoAsync(id, ct)).AHttp(a =>
                    TypedResults.PhysicalFile(a.RutaFisica, a.ContentType, a.NombreOriginal)))
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, DocumentosService service, CancellationToken ct) =>
                (await service.EliminarAsync(id, ct)).AHttp(_ => TypedResults.NoContent()))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
