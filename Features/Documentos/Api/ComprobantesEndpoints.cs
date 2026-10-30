using rps_backend.Features.Documentos.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Documentos.Api;

// Genera un Documento (comprobante .txt) a partir de una Liquidación existente.
public static class ComprobantesEndpoints
{
    public static IEndpointRouteBuilder MapComprobantesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/liquidaciones/{id:guid}/comprobante", async (Guid id, DocumentosService service, CancellationToken ct) =>
                (await service.GenerarComprobanteLiquidacionAsync(id, ct)).AHttp(d => TypedResults.Created($"/api/documentos/{d.Id}", d)))
            .WithTags("Documentos")
            .Produces<DocumentoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
