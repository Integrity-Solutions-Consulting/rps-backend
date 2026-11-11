using Quartz;
using rps_backend.Persistence;

namespace rps_backend.Features.Colaboradores.Application;

// Pone al día el estado Configurado/Pendiente de todos los colaboradores: un contrato que vence o
// una frecuencia programada que entra en vigencia cambian el estado sin que nadie edite nada.
[DisallowConcurrentExecution]
public class RecalculoEstadoJob(RpsDbContext db, ILogger<RecalculoEstadoJob> logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        try
        {
            var cambiados = await ColaboradorEstado.RecalcularTodosAsync(db, cancellationToken);
            logger.LogInformation("Recálculo diario del estado de colaboradores: {Cambiados} cambiaron de estado", cambiados);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error en el recálculo diario del estado de colaboradores");
        }
    }
}
