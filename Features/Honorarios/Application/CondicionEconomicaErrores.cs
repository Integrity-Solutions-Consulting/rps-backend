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

    public static readonly Error ContratoRequerido =
        Error.Validacion("CondicionEconomica.ContratoRequerido", "contratoId", "Es obligatorio.");

    public static readonly Error ImporteMensualInvalido =
        Error.Validacion("CondicionEconomica.ImporteMensualInvalido", "importeMensual", "Es obligatorio y debe ser mayor a 0.");

    public static readonly Error ModalidadInvalida =
        Error.Validacion("CondicionEconomica.ModalidadInvalida", "modalidad", "Es obligatoria (Neto o MasIva).");

    public static readonly Error FechaInicioVigenciaInvalida =
        Error.Validacion("CondicionEconomica.FechaInicioVigenciaInvalida", "fechaInicioVigencia", "Es obligatoria.");

    public static Error YaUsadaEnHonorarios(DateOnly fecha) =>
        Error.Conflicto("CondicionEconomica.YaUsadaEnHonorarios",
            $"El honorario vigente desde el {fecha:dd/MM/yyyy} ya se usó en un cálculo de honorarios generado. Elimina ese cálculo (si aún no está liquidado) para poder cambiarlo.");

    public static Error AdendaNoEncontrada(Guid id) =>
        Error.NoEncontrado("CondicionEconomica.AdendaNoEncontrada", $"No existe una adenda con id '{id}' para ese contrato.");

    public static Error FechaInicioVigenciaDuplicada(DateOnly fecha) =>
        Error.Conflicto("CondicionEconomica.FechaInicioVigenciaDuplicada", $"Ya existe una condición económica de ese proyecto vigente desde el {fecha:dd/MM/yyyy}.");
}
