using rps_backend.Shared;

namespace rps_backend.Features.IntegracionTmr.Application;

public static class CambioTmrErrores
{
    public static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("CambioTmr.NoEncontrado", $"No existe un cambio de TMR con id '{id}'.");
}
