using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using rps_backend.Features.Exportaciones.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Exportaciones.Api;

// El Excel de pago por corte (RN 52-57): previsualizar, generar una versión y descargarla de
// nuevo sin regenerar. Separado de ExportacionesEndpoints, que son reportes de consulta.
public static class ExportacionPagoEndpoints
{
    public static IEndpointRouteBuilder MapExportacionPagoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exportaciones/pago").WithTags("Exportación de pago");

        // GET /api/exportaciones/pago/preview?periodo=2026-11-01&corte=Quincena
        group.MapGet("/preview", async (DateOnly? periodo, TipoCorte? corte, ExportacionPagoService service, CancellationToken ct) =>
                (await service.PrevisualizarAsync(periodo, corte, ct)).AHttp(TypedResults.Ok))
            .Produces<PreviewExportacionPagoResponse>()
            .ProducesValidationProblem();

        group.MapPost("/", async (GenerarExportacionPagoRequest request, ClaimsPrincipal user, ExportacionPagoService service, CancellationToken ct) =>
                (await service.GenerarAsync(request, Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!), ct))
                    .AHttp(e => TypedResults.Created($"/api/exportaciones/pago/{e.Id}", e)))
            .Produces<ExportacionPagoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        // GET /api/exportaciones/pago?periodo=2026-11-01&corte=Quincena (ambos opcionales: historial completo).
        group.MapGet("/", async (DateOnly? periodo, TipoCorte? corte, ExportacionPagoService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(periodo, corte, ct)));

        // Descarga: lee el archivo ya generado, nunca lo regenera (RN 55).
        group.MapGet("/{id:guid}/descargar", async (Guid id, ExportacionPagoService service, CancellationToken ct) =>
        {
            var resultado = await service.DescargarAsync(id, ct);
            return resultado.AHttp(r => TypedResults.File(
                r.Contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", r.NombreArchivo));
        }).ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
