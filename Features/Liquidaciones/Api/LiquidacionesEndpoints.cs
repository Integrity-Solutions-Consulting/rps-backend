using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using rps_backend.Features.Liquidaciones.Application;
using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Api;

// Solo HTTP: recibe la petición, llama al servicio y traduce el Resultado a status code.
public static class LiquidacionesEndpoints
{
    public static IEndpointRouteBuilder MapLiquidacionesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/liquidaciones").WithTags("Liquidaciones");

        group.MapPost("/generar", async (GenerarLiquidacionesRequest request, LiquidacionesService service, CancellationToken ct) =>
                (await service.GenerarAsync(request, ct)).AHttp(TypedResults.Ok))
            .Produces<GenerarLiquidacionesResponse>()
            .ProducesValidationProblem();

        // GET /api/liquidaciones?periodo=2026-11-01&colaboradorId=...&revisado=false
        group.MapGet("/", async (DateOnly? periodo, Guid? colaboradorId, bool? revisado, LiquidacionesService service, CancellationToken ct) =>
                TypedResults.Ok(await service.ListarAsync(periodo, colaboradorId, revisado, ct)));

        group.MapGet("/{id:guid}", async (Guid id, LiquidacionesService service, CancellationToken ct) =>
                (await service.ObtenerAsync(id, ct)).AHttp(TypedResults.Ok))
            .Produces<LiquidacionDetalleResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Control interno previo a exportar (RN 49); no equivale a pago realizado.
        group.MapPost("/{id:guid}/revisar", async (Guid id, ClaimsPrincipal user, LiquidacionesService service, CancellationToken ct) =>
                (await service.MarcarRevisadaAsync(id, Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!), ct)).AHttp(TypedResults.Ok))
            .Produces<LiquidacionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ── Rol a pagar: factura del mes, días descontados y observaciones (ver PlazoFactura) ──
        // GET /api/liquidaciones/facturacion?periodo=2026-12-01
        group.MapGet("/facturacion", async (DateOnly? periodo, FacturacionService service, CancellationToken ct) =>
                (await service.ObtenerAsync(periodo, ct)).AHttp(TypedResults.Ok))
            .Produces<FacturacionPeriodoResponse>()
            .ProducesValidationProblem();

        group.MapPost("/facturacion/{colaboradorId:guid}/factura", async (Guid colaboradorId, MarcarFacturaRequest request, ClaimsPrincipal user, FacturacionService service, CancellationToken ct) =>
                (await service.MarcarFacturaAsync(colaboradorId, request, Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!), ct)).AHttp(TypedResults.Ok))
            .Produces<ControlMensualResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        // DELETE /api/liquidaciones/facturacion/{colaboradorId}/factura?periodo=2026-12-01
        group.MapDelete("/facturacion/{colaboradorId:guid}/factura", async (Guid colaboradorId, DateOnly? periodo, FacturacionService service, CancellationToken ct) =>
                (await service.QuitarFacturaAsync(colaboradorId, periodo, ct)).AHttp(TypedResults.Ok))
            .Produces<ControlMensualResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/facturacion/{colaboradorId:guid}/dias", async (Guid colaboradorId, GuardarDiasRequest request, ClaimsPrincipal user, FacturacionService service, CancellationToken ct) =>
                (await service.GuardarDiasAsync(colaboradorId, request, Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!), ct)).AHttp(TypedResults.Ok))
            .Produces<ControlMensualResponse>()
            .ProducesValidationProblem();

        return app;
    }
}
