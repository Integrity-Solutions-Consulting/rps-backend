using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using rps_backend.Features.Exportaciones.Application;
using rps_backend.Features.Exportaciones.Domain;

namespace rps_backend.Features.Exportaciones.Api;

// Las descargas son POST: además de devolver el archivo, registran la exportación en el historial
// ("Exportaciones recientes"). Un GET no debe escribir: lo repetirían precargas del navegador o reintentos.
public static class ExportacionesEndpoints
{
    private const string Excel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string Pdf = "application/pdf";

    public static IEndpointRouteBuilder MapExportacionesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exportaciones").WithTags("Exportaciones");

        // POST /api/exportaciones/honorarios · { "periodo": "2026-11-01" } (sin periodo: todos los meses).
        group.MapPost("/honorarios", async (ExportarReporteRequest? request, ClaimsPrincipal user, ExportacionesService service, CancellationToken ct) =>
        {
            var (contenido, nombreArchivo) = await service.ExportarHonorariosAsync(request?.Periodo, UsuarioId(user), ct);
            return TypedResults.File(contenido, Excel, nombreArchivo);
        });

        group.MapPost("/honorarios/pdf", async (ExportarReporteRequest? request, ClaimsPrincipal user, ExportacionesService service, CancellationToken ct) =>
        {
            var (contenido, nombreArchivo) = await service.ExportarHonorariosPdfAsync(request?.Periodo, UsuarioId(user), ct);
            return TypedResults.File(contenido, Pdf, nombreArchivo);
        });

        // POST /api/exportaciones/liquidaciones · { "periodo": "2026-11-01", "vista": "consolidado|porColaborador" }
        // (sin vista: consolidado).
        group.MapPost("/liquidaciones", async (ExportarReporteRequest? request, ClaimsPrincipal user, ExportacionesService service, CancellationToken ct) =>
        {
            var (contenido, nombreArchivo) = await service.ExportarLiquidacionesAsync(request?.Periodo, Vista(request?.Vista), UsuarioId(user), ct);
            return TypedResults.File(contenido, Excel, nombreArchivo);
        });

        group.MapPost("/liquidaciones/pdf", async (ExportarReporteRequest? request, ClaimsPrincipal user, ExportacionesService service, CancellationToken ct) =>
        {
            var (contenido, nombreArchivo) = await service.ExportarLiquidacionesPdfAsync(request?.Periodo, Vista(request?.Vista), UsuarioId(user), ct);
            return TypedResults.File(contenido, Pdf, nombreArchivo);
        });

        group.MapPost("/colaboradores", async (ClaimsPrincipal user, ExportacionesService service, CancellationToken ct) =>
        {
            var (contenido, nombreArchivo) = await service.ExportarColaboradoresAsync(UsuarioId(user), ct);
            return TypedResults.File(contenido, Excel, nombreArchivo);
        });

        group.MapPost("/colaboradores/pdf", async (ClaimsPrincipal user, ExportacionesService service, CancellationToken ct) =>
        {
            var (contenido, nombreArchivo) = await service.ExportarColaboradoresPdfAsync(UsuarioId(user), ct);
            return TypedResults.File(contenido, Pdf, nombreArchivo);
        });

        // GET /api/exportaciones/historial?periodo=2026-11-01&tipo=liquidaciones&pagina=1&tamano=20
        //   sin periodo: las de "todos los meses" y colaboradores · todos=true: sin filtro de mes.
        group.MapGet("/historial", async (DateOnly? periodo, bool? todos, string? tipo, int? pagina, int? tamano,
            ExportacionesService service, CancellationToken ct) =>
        {
            TipoReporte? filtro = null;
            if (!string.IsNullOrWhiteSpace(tipo))
            {
                if (!Enum.TryParse<TipoReporte>(tipo, ignoreCase: true, out var t) || !Enum.IsDefined(t))
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                        { ["tipo"] = ["Usa honorarios, liquidaciones o colaboradores."] });
                filtro = t;
            }
            return Results.Ok(await service.HistorialAsync(periodo, todos ?? false, filtro, pagina ?? 1, tamano ?? 20, ct));
        })
        .Produces<PaginaExportacionesResponse>()
        .ProducesValidationProblem();

        // GET /api/exportaciones/ultimas?periodo=2026-11-01 · la última de cada reporte (sin periodo: "todos los meses").
        group.MapGet("/ultimas", async (DateOnly? periodo, ExportacionesService service, CancellationToken ct) =>
                TypedResults.Ok(await service.UltimasAsync(periodo, ct)))
            .Produces<List<UltimaExportacionResponse>>();

        return app;
    }

    private static Guid UsuarioId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static VistaLiquidaciones Vista(string? vista) =>
        string.Equals(vista, "porColaborador", StringComparison.OrdinalIgnoreCase)
            ? VistaLiquidaciones.PorColaborador
            : VistaLiquidaciones.Consolidado;
}
