using Quartz;

namespace rps_backend.Features.IntegracionTmr.Application;

// Ejecuta la misma sincronización que el endpoint manual. Un fallo se registra y se reintenta en el
// siguiente intervalo; el retraso queda visible en GET /api/integracion-tmr/estado (RC 05).
[DisallowConcurrentExecution]
public class SincronizacionTmrJob(SincronizacionTmrService service, ILogger<SincronizacionTmrJob> logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await service.SincronizarAsync(cancellationToken);

            if (resultado.EsExito)
                logger.LogInformation(
                    "Sincronización automática con TMR completada: {CambiosDetectados} cambios, {AsignacionesOmitidas} asignaciones omitidas",
                    resultado.Valor!.CambiosDetectados, resultado.Valor.AsignacionesOmitidas.Count);
            else
                logger.LogWarning("Sincronización automática con TMR no aplicada: {Codigo}", resultado.Error!.Codigo);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error en la sincronización automática con TMR");
        }
    }
}
