using rps_backend.Features.Configuracion.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Configuracion.Api;

// Solo HTTP: recibe la petición, llama al servicio y traduce el Resultado a status code.
public static class ConfiguracionEndpoints
{
    public static IEndpointRouteBuilder MapConfiguracionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/configuracion").WithTags("Configuracion");

        group.MapGet("/", async (ConfiguracionService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ObtenerAsync(ct)))
            .Produces<ConfiguracionResponse>();

        group.MapPut("/", async (ActualizarConfiguracionRequest request, ConfiguracionService service, CancellationToken ct) =>
                (await service.ActualizarAsync(request, ct)).AHttp(TypedResults.Ok))
            .Produces<ConfiguracionResponse>()
            .ProducesValidationProblem();

        return app;
    }
}
