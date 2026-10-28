using rps_backend.Features.Colaboradores.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Colaboradores.Api;

// Solo HTTP: recibe la petición, llama al servicio y traduce el Resultado a status code.
public static class ColaboradoresEndpoints
{
    public static IEndpointRouteBuilder MapColaboradoresEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/colaboradores").WithTags("Colaboradores");

        // GET /api/colaboradores?estado=Pendiente&activo=true
        group.MapGet("/", async (EstadoConfiguracion? estado, bool? activo, ColaboradoresService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(estado, activo, ct)));

        group.MapGet("/{id:guid}", async (Guid id, ColaboradoresService service, CancellationToken ct) =>
                (await service.ObtenerAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<ColaboradorDetalleResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CrearColaboradorRequest request, ColaboradoresService service, CancellationToken ct) =>
                (await service.CrearAsync(request, ct)).AHttp(c => TypedResults.Created($"/api/colaboradores/{c.Id}", c)))
            .Produces<ColaboradorDetalleResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/clasificacion", async (Guid id, ActualizarClasificacionRequest request, ColaboradoresService service, CancellationToken ct) =>
                (await service.ActualizarClasificacionAsync(id, request, ct)).AHttp(TypedResults.Ok))
            .Produces<ColaboradorDetalleResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/desactivar", async (Guid id, ColaboradoresService service, CancellationToken ct) =>
                (await service.CambiarActivoAsync(id, activo: false, ct)).AHttp(TypedResults.Ok))
            .Produces<ColaboradorDetalleResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/activar", async (Guid id, ColaboradoresService service, CancellationToken ct) =>
                (await service.CambiarActivoAsync(id, activo: true, ct)).AHttp(TypedResults.Ok))
            .Produces<ColaboradorDetalleResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/frecuencias", async (Guid id, ColaboradoresService service, CancellationToken ct) =>
                (await service.ListarFrecuenciasAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<List<FrecuenciaResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/frecuencias", async (Guid id, RegistrarFrecuenciaRequest request, ColaboradoresService service, CancellationToken ct) =>
                (await service.RegistrarFrecuenciaAsync(id, request, ct)).AHttp(f => TypedResults.Created($"/api/colaboradores/{id}/frecuencias", f)))
            .Produces<FrecuenciaResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Solo frecuencias de meses que aún no empiezan.
        group.MapPut("/{id:guid}/frecuencias/{frecuenciaId:guid}", async (Guid id, Guid frecuenciaId, RegistrarFrecuenciaRequest request, ColaboradoresService service, CancellationToken ct) =>
                (await service.EditarFrecuenciaAsync(id, frecuenciaId, request, ct)).AHttp(TypedResults.Ok))
            .Produces<FrecuenciaResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}/frecuencias/{frecuenciaId:guid}", async (Guid id, Guid frecuenciaId, ColaboradoresService service, CancellationToken ct) =>
                (await service.EliminarFrecuenciaAsync(id, frecuenciaId, ct)).AHttp(_ => TypedResults.NoContent()))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Reemplaza la cuenta activa; la anterior queda como historial.
        group.MapPut("/{id:guid}/cuenta-bancaria", async (Guid id, RegistrarCuentaBancariaRequest request, ColaboradoresService service, CancellationToken ct) =>
                (await service.RegistrarCuentaBancariaAsync(id, request, ct)).AHttp(TypedResults.Ok))
            .Produces<CuentaBancariaResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
