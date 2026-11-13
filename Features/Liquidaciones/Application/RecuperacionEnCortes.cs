using Microsoft.EntityFrameworkCore;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Application;

// Aplica a los Corte de una liquidación lo que las cuotas de anticipos adicionales tienen
// programado para ese período (RN 38, 47). Lo usa Liquidaciones al generar y Anticipos cuando
// se registra o cambia un plan DESPUÉS de generada la liquidación: así el pago siempre refleja
// los anticipos del mes, aunque la liquidación ya se hubiera revisado.
public static class RecuperacionEnCortes
{
    // RN 47 / "saldo insuficiente por salida": la recuperación nunca deja el corte negativo, pero
    // lo que no cupo se registra en RecuperacionNoAplicada en vez de desaparecer.
    public static (decimal Preparado, decimal NoAplicada) Aplicar(decimal valorBase, decimal recuperacion) =>
        (Math.Max(valorBase - recuperacion, 0m), Math.Max(recuperacion - valorBase, 0m));

    private static string Dinero(decimal valor) =>
        valor.ToString("C2", new System.Globalization.CultureInfo("es-EC"));

    // Recalcula los cortes de la liquidación (colaborador, período), si existe. Si algo cambió,
    // queda el aviso con el motivo y, si estaba "Revisado", vuelve a "Por revisar".
    // No guarda: el que llama hace SaveChanges dentro de su transacción.
    public static async Task RecalcularAsync(RpsDbContext db, Guid colaboradorId, DateOnly periodo, CancellationToken ct)
    {
        var liquidacion = await db.Liquidaciones.FirstOrDefaultAsync(l => l.ColaboradorId == colaboradorId && l.Periodo == periodo, ct);
        if (liquidacion is null)
            return;

        var recuperacionPorCorte = (await db.CuotasRecuperacion.AsNoTracking()
                .Where(c => c.Periodo == periodo)
                .Join(db.AnticiposAdicionales.AsNoTracking(), c => c.AnticipoAdicionalId, a => a.Id,
                    (c, a) => new { a.ColaboradorId, c.Corte, c.Importe })
                .Where(x => x.ColaboradorId == colaboradorId)
                .ToListAsync(ct))
            .GroupBy(x => x.Corte)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Importe));

        var cortes = await db.Cortes.Where(c => c.LiquidacionId == liquidacion.Id).ToListAsync(ct);

        var detalles = new List<string>();
        foreach (var corte in cortes.OrderBy(c => c.Tipo == TipoCorte.Quincena ? 0 : 1))
        {
            var recuperacion = recuperacionPorCorte.GetValueOrDefault(corte.Tipo, 0m);
            if (corte.RecuperacionAdicional == recuperacion)
                continue;

            var nombre = corte.Tipo == TipoCorte.Quincena ? "quincena" : "fin de mes";
            detalles.Add($"{nombre} {Dinero(corte.RecuperacionAdicional)} → {Dinero(recuperacion)}");
            corte.RecuperacionAdicional = recuperacion;
            (corte.ValorPreparado, corte.RecuperacionNoAplicada) = Aplicar(corte.ValorBase, recuperacion);
        }

        if (detalles.Count > 0)
            liquidacion.RegistrarModificacion($"Cambió el descuento por anticipos ({string.Join(", ", detalles)})");
    }
}
