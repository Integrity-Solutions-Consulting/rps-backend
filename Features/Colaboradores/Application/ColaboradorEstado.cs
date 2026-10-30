using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Colaboradores.Application;

public static class ColaboradorEstado
{
    // Configurado = tiene clasificación + al menos una frecuencia + una cuenta bancaria activa + un contrato vigente.
    // Consulta la base, así que se llama después de guardar los cambios de frecuencia/cuenta/contrato.
    public static async Task RecalcularAsync(ColaboradorRps colaborador, RpsDbContext db, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var mesActual = new DateOnly(hoy.Year, hoy.Month, 1);

        // Vigente, no "alguna vez registrada": una frecuencia con VigenteDesdeMes futuro no
        // cuenta todavía (mismo criterio que ColaboradoresService.ConstruirDetalleAsync).
        var tieneFrecuencia = await db.ColaboradoresFrecuencia
            .AnyAsync(f => f.ColaboradorId == colaborador.Id && f.VigenteDesdeMes <= mesActual, ct);

        var tieneCuentaActiva = await db.CuentasBancarias
            .AnyAsync(c => c.ColaboradorId == colaborador.Id && c.Activa, ct);

        // Considera adendas: un contrato cuya FechaFin original ya pasó sigue vigente si una
        // adenda extendió su fecha fin más allá de hoy (RN 10-12).
        var contratosIniciados = await db.Contratos
            .Where(c => c.ColaboradorId == colaborador.Id && c.FechaInicio <= hoy)
            .Select(c => new { c.Id, c.FechaFin })
            .ToListAsync(ct);

        var tieneContratoVigente = false;
        if (contratosIniciados.Count > 0)
        {
            var contratoIds = contratosIniciados.Select(c => c.Id).ToList();
            var maxAdendaPorContrato = (await db.Adendas
                    .Where(a => contratoIds.Contains(a.ContratoId))
                    .ToListAsync(ct))
                .GroupBy(a => a.ContratoId)
                .ToDictionary(g => g.Key, g => g.Max(a => a.NuevaFechaFin));

            tieneContratoVigente = contratosIniciados.Any(c =>
            {
                var maxAdenda = maxAdendaPorContrato.GetValueOrDefault(c.Id);
                var fechaFinVigente = maxAdenda > c.FechaFin ? maxAdenda : c.FechaFin;
                return fechaFinVigente >= hoy;
            });
        }

        colaborador.EstadoConfiguracion =
            colaborador.Clasificacion is not null && tieneFrecuencia && tieneCuentaActiva && tieneContratoVigente
                ? EstadoConfiguracion.Configurado
                : EstadoConfiguracion.Pendiente;

        colaborador.ActualizadoEn = DateTime.UtcNow;
    }
}
