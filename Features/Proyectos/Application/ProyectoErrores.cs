using rps_backend.Shared;

namespace rps_backend.Features.Proyectos.Application;

public static class ProyectoErrores
{
    public static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Proyecto.NoEncontrado", $"No existe un proyecto con id '{id}'.");

    public static readonly Error NombreInvalido =
        Error.Validacion("Proyecto.NombreInvalido", "nombre", "Es obligatorio y debe tener máximo 200 caracteres.");

    public static readonly Error ClienteInvalido =
        Error.Validacion("Proyecto.ClienteInvalido", "cliente", "Debe tener máximo 200 caracteres.");
}
