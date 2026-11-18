using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Liquidaciones.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Liquidaciones.Application;

// Descuenta en los Corte de las liquidaciones lo que los préstamos del colaborador tienen programado.
// Se aplica DESPUÉS de los anticipos (RecuperacionEnCortes) y con dos reglas:
//   1. Nunca deja un pago negativo: en cada corte se descuenta como máximo lo que queda después de
//      los anticipos (ValorBase - RecuperacionAdicional).
//   2. La deuda siempre se termina de cobrar: lo que no alcanzó en un corte pasa al siguiente pago,
//      y así hasta que el préstamo quede en 0. Por eso se recorren todos los cortes del colaborador
//      en orden (mes, quincena antes que fin de mes) llevando lo ya descontado.
// Una cuota de un mes sin liquidación (o de la quincena de alguien que cobra solo a fin de mes) se
// cobra en el siguiente corte que exista.
// Lo usan Liquidaciones (al generar), Anticipos (al cambiar un plan) y Préstamos (al registrar,
// editar o eliminar). No guarda: el que llama hace SaveChanges dentro de su transacción.
public static class PrestamosEnCortes
{
    private static int Orden(TipoCorte tipo) => tipo == TipoCorte.Quincena ? 0 : 1;

    private static string Dinero(decimal valor) =>
        valor.ToString("C2", new System.Globalization.CultureInfo("es-EC"));

    // `sinAviso`: liquidaciones recién creadas en esta misma operación (no tiene sentido avisar que
    // "cambiaron" si se acaban de calcular).
    public static async Task RecalcularAsync(
        RpsDbContext db, Guid colaboradorId, CancellationToken ct, IReadOnlySet<Guid>? sinAviso = null)
    {
        var liquidaciones = await db.Liquidaciones
            .Where(l => l.ColaboradorId == colaboradorId)
            .ToListAsync(ct);
        if (liquidaciones.Count == 0)
            return;

        var periodoPorLiquidacion = liquidaciones.ToDictionary(l => l.Id, l => l.Periodo);
        var ids = liquidaciones.Select(l => l.Id).ToList();
        var cortes = await db.Cortes.Where(c => ids.Contains(c.LiquidacionId)).ToListAsync(ct);

        var cuotas = await db.CuotasPrestamo.AsNoTracking()
            .Join(db.Prestamos.AsNoTracking(), c => c.PrestamoId, p => p.Id,
                (c, p) => new { p.ColaboradorId, c.Periodo, c.Corte, c.Importe })
            .Where(x => x.ColaboradorId == colaboradorId)
            .ToListAsync(ct);

        var descontadoAntes = 0m;
        var cambios = new Dictionary<Guid, List<string>>();

        foreach (var corte in cortes
                     .OrderBy(c => periodoPorLiquidacion[c.LiquidacionId])
                     .ThenBy(c => Orden(c.Tipo)))
        {
            var periodo = periodoPorLiquidacion[corte.LiquidacionId];

            // Todo lo programado hasta este corte (incluido), menos lo que ya se descontó antes.
            var programado = cuotas
                .Where(q => q.Periodo < periodo || (q.Periodo == periodo && Orden(q.Corte) <= Orden(corte.Tipo)))
                .Sum(q => q.Importe);
            var pendiente = Math.Max(programado - descontadoAntes, 0m);

            var despuesDeAnticipos = Math.Max(corte.ValorBase - corte.RecuperacionAdicional, 0m);
            var descuento = Math.Min(pendiente, despuesDeAnticipos);
            descontadoAntes += descuento;

            if (corte.DescuentoPrestamos != descuento)
            {
                var nombre = corte.Tipo == TipoCorte.Quincena ? "quincena" : "fin de mes";
                if (!cambios.TryGetValue(corte.LiquidacionId, out var detalles))
                    cambios[corte.LiquidacionId] = detalles = [];
                detalles.Add($"{nombre} {Dinero(corte.DescuentoPrestamos)} → {Dinero(descuento)}");
                corte.DescuentoPrestamos = descuento;
            }

            corte.ValorPreparado = despuesDeAnticipos - descuento;
        }

        foreach (var (liquidacionId, detalles) in cambios)
        {
            if (sinAviso?.Contains(liquidacionId) == true)
                continue;
            liquidaciones.First(l => l.Id == liquidacionId)
                .RegistrarModificacion($"Cambió el descuento por préstamos ({string.Join(", ", detalles)})");
        }
    }

    // Cuánto se le ha descontado ya en total por préstamos (en las liquidaciones generadas).
    public static async Task<decimal> DescontadoAsync(RpsDbContext db, Guid colaboradorId, CancellationToken ct) =>
        await db.Liquidaciones.AsNoTracking()
            .Where(l => l.ColaboradorId == colaboradorId)
            .Join(db.Cortes.AsNoTracking(), l => l.Id, c => c.LiquidacionId, (l, c) => c.DescuentoPrestamos)
            .SumAsync(ct);
}
