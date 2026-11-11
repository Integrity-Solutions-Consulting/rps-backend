using rps_backend.Features.Exportaciones.Application;

namespace rps_backend.Features.Exportaciones.Api;

public static class ExportacionesEndpoints
{
    public static IEndpointRouteBuilder MapExportacionesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exportaciones").WithTags("Exportaciones");

        group.MapGet("/honorarios", async (DateOnly? periodo, ExportacionesService service, CancellationToken ct) =>
        {
            var (contenido, nombreArchivo) = await service.ExportarHonorariosAsync(periodo, ct);
            return TypedResults.File(contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        });

        // GET /api/exportaciones/honorarios/pdf?periodo=2026-11-01 (sin periodo: todos los meses).
        group.MapGet("/honorarios/pdf", async (DateOnly? periodo, ExportacionesService service, CancellationToken ct) =>
        {
            var (contenido, nombreArchivo) = await service.ExportarHonorariosPdfAsync(periodo, ct);
            return TypedResults.File(contenido, "application/pdf", nombreArchivo);
        });

        group.MapGet("/liquidaciones", async (DateOnly? periodo, ExportacionesService service, CancellationToken ct) =>
        {
            var (contenido, nombreArchivo) = await service.ExportarLiquidacionesAsync(periodo, ct);
            return TypedResults.File(contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        });

        // GET /api/exportaciones/liquidaciones/pdf?periodo=2026-11-01 (sin periodo: todos los meses).
        group.MapGet("/liquidaciones/pdf", async (DateOnly? periodo, ExportacionesService service, CancellationToken ct) =>
        {
            var (contenido, nombreArchivo) = await service.ExportarLiquidacionesPdfAsync(periodo, ct);
            return TypedResults.File(contenido, "application/pdf", nombreArchivo);
        });

        group.MapGet("/colaboradores", async (ExportacionesService service, CancellationToken ct) =>
        {
            var (contenido, nombreArchivo) = await service.ExportarColaboradoresAsync(ct);
            return TypedResults.File(contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        });

        // GET /api/exportaciones/colaboradores/pdf
        group.MapGet("/colaboradores/pdf", async (ExportacionesService service, CancellationToken ct) =>
        {
            var (contenido, nombreArchivo) = await service.ExportarColaboradoresPdfAsync(ct);
            return TypedResults.File(contenido, "application/pdf", nombreArchivo);
        });

        return app;
    }
}
