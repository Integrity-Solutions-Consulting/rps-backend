using Microsoft.EntityFrameworkCore;

namespace rps_backend.Persistence;

// Lee el header Idempotency-Key en POST/PUT; si ya se procesó esa clave, devuelve la respuesta
// guardada en vez de repetir la acción. Protege contra doble clic o un reintento de red.
// Genérico: no sabe qué endpoint es, aplica igual a cualquiera que reciba el header.
public class IdempotenciaMiddleware(RequestDelegate next)
{
    private const string Header = "Idempotency-Key";

    public async Task InvokeAsync(HttpContext context, RpsDbContext db)
    {
        var metodo = context.Request.Method;
        if (!HttpMethods.IsPost(metodo) && !HttpMethods.IsPut(metodo))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(Header, out var valores) || string.IsNullOrWhiteSpace(valores.ToString()))
        {
            await next(context);
            return;
        }

        var clave = valores.ToString();
        if (clave.Length > 100)
        {
            await next(context);
            return;
        }

        var existente = await db.RegistrosIdempotencia.AsNoTracking().FirstOrDefaultAsync(r => r.Clave == clave);
        if (existente is not null)
        {
            context.Response.StatusCode = existente.StatusCode;
            if (existente.ContentType is not null)
                context.Response.ContentType = existente.ContentType;
            await context.Response.Body.WriteAsync(existente.Cuerpo);
            return;
        }

        var cuerpoOriginal = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = cuerpoOriginal;
        }

        var bytes = buffer.ToArray();

        // Solo se guarda un éxito: un error no debe "congelarse" como si fuera la respuesta
        // definitiva (RN 47 — reintentar tras un error de validación debe poder volver a intentarlo).
        if (context.Response.StatusCode is >= 200 and < 300)
        {
            db.RegistrosIdempotencia.Add(new RegistroIdempotencia
            {
                Clave = clave, StatusCode = context.Response.StatusCode, ContentType = context.Response.ContentType, Cuerpo = bytes
            });

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Dos peticiones con la misma clave llegaron casi juntas: la otra ya guardó el
                // registro. No pasa nada — cada una ya tiene su propia respuesta correcta.
            }
        }

        await cuerpoOriginal.WriteAsync(bytes);
    }
}
