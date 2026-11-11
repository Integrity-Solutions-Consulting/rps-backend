using Microsoft.EntityFrameworkCore;
using rps_backend.Persistence;

namespace rps_backend.Features.Colaboradores.Application;

public static class ColaboradorAnticipos
{
    // Un anticipo adicional con saldo pendiente solo se recupera con las cuotas de sus liquidaciones:
    // desactivar al colaborador mientras tanto lo dejaría varado.
    public static async Task<bool> TieneSaldoPendienteAsync(RpsDbContext db, Guid colaboradorId, CancellationToken ct)
    {
        var anticipoIds = await db.AnticiposAdicionales.Where(a => a.ColaboradorId == colaboradorId).Select(a => a.Id).ToListAsync(ct);
        if (anticipoIds.Count == 0)
            return false;

        var montoTotal = await db.AnticiposAdicionales.Where(a => anticipoIds.Contains(a.Id)).SumAsync(a => a.Monto, ct);
        var cuotasAplicadas = await db.CuotasRecuperacion.Where(c => anticipoIds.Contains(c.AnticipoAdicionalId)).SumAsync(c => c.Importe, ct);
        return montoTotal - cuotasAplicadas > 0;
    }
}
