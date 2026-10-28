namespace rps_backend.Shared;

public static class ResultadoHttp
{
    // Traduce un Resultado de Application a una respuesta HTTP.
    // El "codigo" del error viaja en el JSON de la respuesta (ProblemDetails).
    public static IResult AHttp<T>(this Resultado<T> resultado, Func<T, IResult> siEsExito)
    {
        if (resultado.EsExito)
            return siEsExito(resultado.Valor!);

        var error = resultado.Error!;
        var extensiones = new Dictionary<string, object?> { ["codigo"] = error.Codigo };

        return error.Tipo switch
        {
            TipoError.NoEncontrado => TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound, title: "No encontrado", detail: error.Mensaje, extensions: extensiones),
            TipoError.Validacion => TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { [error.Campo ?? "general"] = [error.Mensaje] }, extensions: extensiones),
            TipoError.Conflicto => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict, title: "Conflicto", detail: error.Mensaje, extensions: extensiones),
            _ => throw new InvalidOperationException($"TipoError no soportado: {error.Tipo}")
        };
    }
}
