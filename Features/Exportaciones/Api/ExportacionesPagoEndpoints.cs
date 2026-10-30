using rps_backend.Features.Exportaciones.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Exportaciones.Api;

// Archivo de pago por mes y corte, con versiones. Conviven con los CSV de ExportacionesEndpoints
// (/honorarios, /liquidaciones, /colaboradores), que no cambian.
public static class ExportacionesPagoEndpoints
{
    public static IEndpointRouteBuilder MapExportacionesPagoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exportaciones").WithTags("Exportaciones");

        // GET /api/exportaciones/preparacion?periodo=2026-10&corte=Quincena
        group.MapGet("/preparacion", async (string? periodo, TipoCorte? corte, ExportacionesPagoService service, CancellationToken ct) =>
                (await service.PrepararAsync(periodo, corte, ct)).AHttp(TypedResults.Ok))
            .Produces<PreparacionExportacionResponse>()
            .ProducesValidationProblem();

        // GET /api/exportaciones?periodo=2026-10 (sin período: todas)
        group.MapGet("/", async (string? periodo, ExportacionesPagoService service, CancellationToken ct) =>
                (await service.ListarAsync(periodo, ct)).AHttp(TypedResults.Ok))
            .Produces<List<ExportacionResponse>>()
            .ProducesValidationProblem();

        group.MapPost("/", async (GenerarExportacionRequest request, ExportacionesPagoService service, CancellationToken ct) =>
                (await service.GenerarAsync(request, ct)).AHttp(e => TypedResults.Created($"/api/exportaciones/{e.Id}", e)))
            .Produces<ExportacionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}/archivo", async (Guid id, ExportacionesPagoService service, CancellationToken ct) =>
                (await service.ArchivoAsync(id, ct)).AHttp(a => TypedResults.File(a.Contenido, "text/csv", a.NombreArchivo)))
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
