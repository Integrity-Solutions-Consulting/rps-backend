using rps_backend.Shared;

namespace rps_backend.Features.Colaboradores.Application;

// Catálogo de errores de la feature Colaboradores.
public static class ColaboradorErrores
{
    // ===== No encontrado =====

    public static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Colaborador.NoEncontrado", $"No existe un colaborador con id '{id}'.");

    public static Error FrecuenciaNoEncontrada(Guid frecuenciaId) =>
        Error.NoEncontrado("Colaborador.FrecuenciaNoEncontrada", $"El colaborador no tiene una frecuencia con id '{frecuenciaId}'.");

    // ===== Conflictos =====

    public static Error TmrDuplicado(string tmrColaboradorId) =>
        Error.Conflicto("Colaborador.TmrDuplicado", $"Ya existe un colaborador con TmrColaboradorId '{tmrColaboradorId}'.");

    public static readonly Error Desactivado =
        Error.Conflicto("Colaborador.Desactivado", "El colaborador está desactivado. Actívalo antes de modificar su configuración.");

    public static Error FrecuenciaDuplicada(DateOnly vigenteDesdeMes) =>
        Error.Conflicto("Colaborador.FrecuenciaDuplicada", $"El colaborador ya tiene una frecuencia registrada desde {vigenteDesdeMes:yyyy-MM-dd}.");

    public static readonly Error FrecuenciaNoModificable =
        Error.Conflicto("Colaborador.FrecuenciaNoModificable", "Solo se pueden editar o eliminar frecuencias de meses que aún no empiezan.");

    public static readonly Error TieneAnticiposAprobadosPendientes =
        Error.Conflicto(
            "Colaborador.TieneAnticiposAprobadosPendientes",
            "El colaborador tiene anticipos aprobados sin descontar. Anúlalos o espera a que se descuenten antes de desactivarlo.");

    // ===== Validaciones =====

    public static readonly Error TmrIdInvalido =
        Error.Validacion("Colaborador.TmrIdInvalido", "tmrColaboradorId", "Es obligatorio y debe tener máximo 50 caracteres.");

    public static readonly Error FrecuenciaRequerida =
        Error.Validacion("Colaborador.FrecuenciaRequerida", "frecuencia", "Es obligatoria (DosPagos o FinDeMes).");

    public static readonly Error VigenteDesdeMesInvalido =
        Error.Validacion("Colaborador.VigenteDesdeMesInvalido", "vigenteDesdeMes", "Es obligatoria y debe ser el primer día del mes (ej. 2026-11-01).");

    public static readonly Error VigenteDesdeMesNoFuturo =
        Error.Validacion("Colaborador.VigenteDesdeMesNoFuturo", "vigenteDesdeMes", "Al editar, el nuevo mes debe ser posterior al mes actual.");

    public static readonly Error NumeroCuentaInvalido =
        Error.Validacion("Colaborador.NumeroCuentaInvalido", "numeroCuenta", "Es obligatorio, solo dígitos y máximo 30 caracteres.");

    public static readonly Error BancoInvalido =
        Error.Validacion("Colaborador.BancoInvalido", "banco", "Debe tener máximo 100 caracteres.");

    public static readonly Error TipoCuentaInvalido =
        Error.Validacion("Colaborador.TipoCuentaInvalido", "tipoCuenta", "Debe tener máximo 20 caracteres.");
}
