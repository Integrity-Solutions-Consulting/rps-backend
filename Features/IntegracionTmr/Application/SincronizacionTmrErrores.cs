using rps_backend.Shared;

namespace rps_backend.Features.IntegracionTmr.Application;

public static class SincronizacionTmrErrores
{
    public static readonly Error TmrNoDisponible =
        Error.ServicioNoDisponible("IntegracionTmr.NoDisponible", "No se pudo verificar la información de TMR. No se modificó ningún dato.");

    public static readonly Error RespuestaVacia =
        Error.ServicioNoDisponible("IntegracionTmr.RespuestaVacia", "TMR devolvió listas vacías. Por seguridad no se modificó ningún dato.");

    public static readonly Error SinTipoContrato =
        Error.ServicioNoDisponible("IntegracionTmr.SinTipoContrato",
            "TMR no envió el tipo de contrato de ningún colaborador (¿falta actualizar TMR?). Por seguridad no se modificó ningún dato.");

    public static readonly Error NoGuardado =
        Error.Conflicto("IntegracionTmr.NoGuardado", "Los datos de TMR no se pudieron guardar. No se modificó ningún dato.");
}
