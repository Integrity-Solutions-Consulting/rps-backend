namespace rps_backend.Shared;

public static class ResultadoHttp
{
    // Traduce un Resultado de Application a una respuesta HTTP.
    public static IResult AHttp<T>(this Resultado<T> resultado, Func<T, IResult> siEsExito)
    {
        if (resultado.EsExito)
            return siEsExito(resultado.Valor!);

        var error = resultado.Error!;
        return error.Tipo switch
        {
            TipoError.NoEncontrado => TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound, title: "No encontrado", detail: error.Mensaje),
            TipoError.Validacion => TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { [error.Campo ?? "general"] = [error.Mensaje] }),
            TipoError.Conflicto => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict, title: "Conflicto", detail: error.Mensaje),
            _ => throw new InvalidOperationException($"TipoError no soportado: {error.Tipo}")
        };
    }
}
