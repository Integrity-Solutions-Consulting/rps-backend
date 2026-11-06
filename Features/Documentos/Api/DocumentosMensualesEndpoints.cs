using rps_backend.Features.Documentos.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Documentos.Api;

// Solo HTTP: recibe la petición, llama al servicio y traduce el Resultado a status code.
public static class DocumentosMensualesEndpoints
{
    public static IEndpointRouteBuilder MapDocumentosMensualesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/colaboradores/{id:guid}/documentos-mensuales").WithTags("Documentos");

        // GET /api/colaboradores/{id}/documentos-mensuales?periodo=2026-11-01
        group.MapGet("/", async (Guid id, DateOnly? periodo, DocumentosMensualesService service, CancellationToken ct) =>
                (await service.ListarAsync(id, periodo, ct)).AHttp(TypedResults.Ok))
            .Produces<List<DocumentoMensualResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (Guid id, RegistrarDocumentoMensualRequest request, DocumentosMensualesService service, CancellationToken ct) =>
                (await service.RegistrarAsync(id, request, ct)).AHttp(d => TypedResults.Created($"/api/colaboradores/{id}/documentos-mensuales/{d.Id}", d)))
            .Produces<DocumentoMensualResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{documentoMensualId:guid}", async (Guid id, Guid documentoMensualId, EditarDocumentoMensualRequest request, DocumentosMensualesService service, CancellationToken ct) =>
                (await service.EditarAsync(id, documentoMensualId, request, ct)).AHttp(TypedResults.Ok))
            .Produces<DocumentoMensualResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{documentoMensualId:guid}", async (Guid id, Guid documentoMensualId, DocumentosMensualesService service, CancellationToken ct) =>
                (await service.EliminarAsync(id, documentoMensualId, ct)).AHttp(_ => TypedResults.NoContent()))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
