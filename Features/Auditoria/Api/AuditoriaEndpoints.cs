using rps_backend.Features.Auditoria.Application;

namespace rps_backend.Features.Auditoria.Api;

public static class AuditoriaEndpoints
{
    public static IEndpointRouteBuilder MapAuditoriaEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/auditoria?entidad=Liquidacion&entidadId=...&usuarioId=...&limite=200
        app.MapGet("/api/auditoria", async (
                string? entidad, string? entidadId, Guid? usuarioId, int limite, AuditoriaService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(entidad, entidadId, usuarioId, limite, ct)))
            .WithTags("Auditoría")
            .Produces<List<AuditoriaResponse>>();

        return app;
    }
}
