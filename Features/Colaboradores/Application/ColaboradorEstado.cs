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

        var tieneContratoVigente = await db.Contratos
            .AnyAsync(c => c.ColaboradorId == colaborador.Id
                && c.FechaInicio <= hoy
                && (c.FechaFin == null || c.FechaFin >= hoy), ct);

        colaborador.EstadoConfiguracion =
            colaborador.Clasificacion is not null && tieneFrecuencia && tieneCuentaActiva && tieneContratoVigente
                ? EstadoConfiguracion.Configurado
                : EstadoConfiguracion.Pendiente;

        colaborador.ActualizadoEn = DateTime.UtcNow;
    }
}
