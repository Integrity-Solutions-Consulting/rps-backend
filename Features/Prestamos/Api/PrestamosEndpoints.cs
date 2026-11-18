using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using rps_backend.Features.Prestamos.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Prestamos.Api;

// Solo HTTP: recibe la petición, llama al servicio y traduce el Resultado a status code.
public static class PrestamosEndpoints
{
    public static IEndpointRouteBuilder MapPrestamosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/colaboradores/{id:guid}/prestamos").WithTags("Préstamos");

        group.MapGet("/", async (Guid id, PrestamosService service, CancellationToken ct) =>
                (await service.ListarAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<List<PrestamoResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/resumen", async (Guid id, PrestamosService service, CancellationToken ct) =>
                (await service.ObtenerResumenAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<ResumenPrestamosResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{prestamoId:guid}", async (Guid id, Guid prestamoId, PrestamosService service, CancellationToken ct) =>
                (await service.ObtenerAsync(id, prestamoId, ct)).AHttp(TypedResults.Ok))
            .Produces<PrestamoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (Guid id, CrearPrestamoRequest request, ClaimsPrincipal user, PrestamosService service, CancellationToken ct) =>
                (await service.RegistrarAsync(id, request, Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!), ct))
                    .AHttp(p => TypedResults.Created($"/api/colaboradores/{id}/prestamos/{p.Id}", p)))
            .Produces<PrestamoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Reemplaza monto, observaciones y plan de pagos (los cobros cuya fecha ya llegó tienen que venir iguales).
        group.MapPut("/{prestamoId:guid}", async (Guid id, Guid prestamoId, ActualizarPrestamoRequest request, PrestamosService service, CancellationToken ct) =>
                (await service.ActualizarAsync(id, prestamoId, request, ct)).AHttp(TypedResults.Ok))
            .Produces<PrestamoResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{prestamoId:guid}", async (Guid id, Guid prestamoId, PrestamosService service, CancellationToken ct) =>
                (await service.EliminarAsync(id, prestamoId, ct)).AHttp(_ => TypedResults.NoContent()))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
