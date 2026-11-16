using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Contratos.Domain;
using rps_backend.Persistence;

namespace rps_backend.Features.Contratos.Application;

// Un colaborador desactivado no puede tener contratos vigentes (ni por proyecto ni por contrato):
// al desactivarse, todos los que siguen vigentes (o todavía no empiezan) terminan ese día. Ese día
// todavía se paga, así que el mes de la salida entra al cálculo hasta esa fecha; desde el mes
// siguiente, no. Si después se reactiva, necesita un contrato nuevo: el terminado no se reabre.
public static class TerminacionContratos
{
    public const string MotivoDesactivacion = "Colaborador desactivado";

    // Devuelve cuántos contratos terminó.
    public static async Task<int> TerminarPorDesactivacionAsync(RpsDbContext db, Guid colaboradorId, DateOnly fecha, CancellationToken ct)
    {
        var contratos = await db.Contratos
            .Where(c => c.ColaboradorId == colaboradorId && c.FechaTerminacion == null)
            .ToListAsync(ct);
        if (contratos.Count == 0)
            return 0;

        var ids = contratos.Select(c => c.Id).ToList();
        var maxAdenda = (await db.Adendas.AsNoTracking().Where(a => ids.Contains(a.ContratoId)).ToListAsync(ct))
            .GroupBy(a => a.ContratoId)
            .ToDictionary(g => g.Key, g => g.Max(a => a.NuevaFechaFin));

        var terminados = 0;
        foreach (var contrato in contratos)
        {
            // Uno que ya venció antes de la desactivación no se toca: ya no estaba vigente.
            var fin = VigenciaContrato.FinEfectivo(contrato.FechaFin, maxAdenda.GetValueOrDefault(contrato.Id), null);
            if (fin is { } f && f < fecha)
                continue;

            contrato.FechaTerminacion = fecha;
            contrato.MotivoTerminacion = MotivoDesactivacion;
            contrato.ActualizadoEn = DateTime.UtcNow;
            terminados++;
        }

        return terminados;
    }
}
