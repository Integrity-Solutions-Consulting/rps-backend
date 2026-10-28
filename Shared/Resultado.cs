namespace rps_backend.Shared;

public enum TipoError
{
    NoEncontrado,
    Validacion,
    Conflicto
}

public record Error(TipoError Tipo, string Mensaje, string? Campo = null)
{
    public static Error NoEncontrado(string mensaje) => new(TipoError.NoEncontrado, mensaje);

    public static Error Validacion(string campo, string mensaje) => new(TipoError.Validacion, mensaje, campo);

    public static Error Conflicto(string mensaje) => new(TipoError.Conflicto, mensaje);
}

// Lo que devuelven los servicios de Application: un valor o un error, sin saber nada de HTTP.
// La capa Api lo traduce a 200/201/400/404/409.
public class Resultado<T>
{
    public T? Valor { get; }

    public Error? Error { get; }

    public bool EsExito => Error is null;

    private Resultado(T? valor, Error? error)
    {
        Valor = valor;
        Error = error;
    }

    public static implicit operator Resultado<T>(T valor) => new(valor, null);

    public static implicit operator Resultado<T>(Error error) => new(default, error);
}
