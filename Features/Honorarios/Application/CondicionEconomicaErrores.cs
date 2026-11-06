using rps_backend.Shared;

namespace rps_backend.Features.Honorarios.Application;

public static class CondicionEconomicaErrores
{
    public static Error ColaboradorNoEncontrado(Guid id) =>
        Error.NoEncontrado("CondicionEconomica.ColaboradorNoEncontrado", $"No existe un colaborador con id '{id}'.");

    public static Error ProyectoNoEncontrado(Guid id) =>
        Error.NoEncontrado("CondicionEconomica.ProyectoNoEncontrado", $"No existe un proyecto con id '{id}'.");

    public static Error ContratoNoEncontrado(Guid id) =>
        Error.NoEncontrado("CondicionEconomica.ContratoNoEncontrado", $"No existe un contrato con id '{id}' para ese colaborador y proyecto.");

    public static readonly Error ProyectoRequerido =
        Error.Validacion("CondicionEconomica.ProyectoRequerido", "proyectoId", "Es obligatorio.");

    public static readonly Error ContratoRequerido =
        Error.Validacion("CondicionEconomica.ContratoRequerido", "contratoId", "Es obligatorio.");

    public static readonly Error ImporteMensualInvalido =
        Error.Validacion("CondicionEconomica.ImporteMensualInvalido", "importeMensual", "Es obligatorio y debe ser mayor a 0.");

    public static readonly Error ModalidadInvalida =
        Error.Validacion("CondicionEconomica.ModalidadInvalida", "modalidad", "Es obligatoria (Neto o MasIva).");

    public static readonly Error MesInicioVigenciaInvalido =
        Error.Validacion("CondicionEconomica.MesInicioVigenciaInvalido", "mesInicioVigencia", "Es obligatorio y debe ser el primer día del mes.");

    public static Error MesInicioVigenciaDuplicado(DateOnly mes) =>
        Error.Conflicto("CondicionEconomica.MesInicioVigenciaDuplicado", $"Ya existe una condición económica de ese proyecto vigente desde {mes:yyyy-MM-dd}.");
}
