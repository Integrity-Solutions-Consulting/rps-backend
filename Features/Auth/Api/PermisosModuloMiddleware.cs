using System.IdentityModel.Tokens.Jwt;
using System.Text.RegularExpressions;
using rps_backend.Features.Auth.Application;

namespace rps_backend.Features.Auth.Api;

// Exige el permiso del módulo (Configuración > Roles) antes de llegar al endpoint. Va después de
// UseAuthorization: para entonces la petición ya tiene una sesión válida.
//
// Regla general: LEER datos compartidos (colaboradores, contratos, proyectos, configuración...) está
// permitido a cualquier usuario con sesión, porque varias pantallas los muestran (Inicio, el menú,
// Rol a pagar). MODIFICAR algo exige el permiso del módulo. Anticipos, Préstamos, Exportaciones y
// lo de Configuración (auditoría, usuarios, roles) piden el permiso incluso para leer.
// Si un usuario tiene varios roles, basta con que uno de ellos tenga el permiso.
// El Administrador pasa siempre (PermisosUsuarioService le da todos los módulos).
public class PermisosModuloMiddleware(RequestDelegate next)
{
    private sealed record Regla(Regex Ruta, bool SoloEscritura, string[] Modulos);

    // La primera regla que coincide decide: las más específicas van primero.
    private static readonly Regla[] Reglas =
    [
        Nueva(@"^/api/colaboradores/[^/]+/anticipos(/|$)", soloEscritura: false, "anticipos"),
        Nueva(@"^/api/colaboradores/[^/]+/prestamos(/|$)", soloEscritura: false, "prestamos"),
        Nueva(@"^/api/colaboradores/[^/]+/contratos(/|$)", soloEscritura: true, "contratos"),
        // Condiciones económicas, asignaciones y cuenta bancaria se editan desde la ficha del colaborador
        // y también desde el registro de contratos.
        Nueva(@"^/api/colaboradores/[^/]+/(condiciones-economicas|asignaciones|cuenta-bancaria)(/|$)", soloEscritura: true, "colaboradores", "contratos"),
        Nueva(@"^/api/colaboradores(/|$)", soloEscritura: true, "colaboradores"),
        Nueva(@"^/api/proyectos(/|$)", soloEscritura: true, "colaboradores", "contratos"),
        Nueva(@"^/api/integracion-tmr/sincronizar(/|$)", soloEscritura: true, "colaboradores"),
        Nueva(@"^/api/(honorarios|liquidaciones)(/|$)", soloEscritura: true, "liquidaciones"),
        Nueva(@"^/api/exportaciones(/|$)", soloEscritura: false, "exportaciones"),
        Nueva(@"^/api/(configuracion|feriados)(/|$)", soloEscritura: true, "configuracion"),
        Nueva(@"^/api/(auditoria|usuarios|roles)(/|$)", soloEscritura: false, "configuracion"),
    ];

    private static readonly Dictionary<string, string> Etiquetas = new()
    {
        ["colaboradores"] = "Colaboradores",
        ["contratos"] = "Contratos",
        ["anticipos"] = "Anticipos",
        ["prestamos"] = "Préstamos",
        ["liquidaciones"] = "Rol a pagar",
        ["exportaciones"] = "Exportaciones",
        ["configuracion"] = "Configuración",
    };

    public async Task InvokeAsync(HttpContext context, PermisosUsuarioService permisosUsuario)
    {
        var ruta = context.Request.Path.Value ?? string.Empty;
        var regla = Array.Find(Reglas, r => r.Ruta.IsMatch(ruta));

        // Sin regla, o una lectura donde la regla solo controla cambios: sigue igual que antes.
        if (regla is null || (regla.SoloEscritura && EsLectura(context.Request.Method)))
        {
            await next(context);
            return;
        }

        // Sin usuario: UseAuthorization ya respondió 401 si el endpoint lo pedía; aquí no se decide nada.
        var sub = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(sub, out var usuarioId))
        {
            await next(context);
            return;
        }

        var permisos = await permisosUsuario.ObtenerAsync(usuarioId, context.RequestAborted);
        if (regla.Modulos.Any(permisos.Contains))
        {
            await next(context);
            return;
        }

        var modulos = string.Join(" o ", regla.Modulos.Select(m => Etiquetas.GetValueOrDefault(m, m)));
        await TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Sin permiso",
                detail: $"Tu rol no tiene permiso para el módulo {modulos}.",
                extensions: new Dictionary<string, object?> { ["codigo"] = "Permisos.SinAcceso" })
            .ExecuteAsync(context);
    }

    private static bool EsLectura(string metodo) =>
        HttpMethods.IsGet(metodo) || HttpMethods.IsHead(metodo) || HttpMethods.IsOptions(metodo);

    private static Regla Nueva(string patron, bool soloEscritura, params string[] modulos) =>
        new(new Regex(patron, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled), soloEscritura, modulos);
}
