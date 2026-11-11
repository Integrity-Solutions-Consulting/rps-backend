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
        colaborador.EstadoConfiguracion = await CalcularAsync(colaborador, db, ct);
        colaborador.ActualizadoEn = DateTime.UtcNow;
    }

    // El estado depende de la fecha (un contrato vence, una frecuencia programada entra en
    // vigencia), no solo de las ediciones: esto lo pone al día para todos. Solo toca a los que
    // cambian, para no ensuciar ActualizadoEn ni la auditoría. Guarda los cambios y devuelve cuántos.
    public static async Task<int> RecalcularTodosAsync(RpsDbContext db, CancellationToken ct)
    {
        var colaboradores = await db.ColaboradoresRps.ToListAsync(ct);
        var cambiados = 0;

        foreach (var colaborador in colaboradores)
        {
            var estado = await CalcularAsync(colaborador, db, ct);
            if (estado == colaborador.EstadoConfiguracion)
                continue;

            colaborador.EstadoConfiguracion = estado;
            colaborador.ActualizadoEn = DateTime.UtcNow;
            cambiados++;
        }

        if (cambiados > 0)
            await db.SaveChangesAsync(ct);

        return cambiados;
    }

    private static Task<EstadoConfiguracion> CalcularAsync(ColaboradorRps colaborador, RpsDbContext db, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        return CalcularEntreAsync(colaborador, db, hoy, hoy, ct);
    }

    // Configurado para un período ya transcurrido o en curso (cálculo de honorarios): no el estado
    // de hoy, sino si en ese mes tenía todo. Así quien salió a mitad de mes, con un contrato que
    // terminó ese mismo mes, igual entra al cálculo de los días que trabajó.
    public static Task<EstadoConfiguracion> CalcularParaPeriodoAsync(
        ColaboradorRps colaborador, RpsDbContext db, DateOnly periodo, CancellationToken ct) =>
        CalcularEntreAsync(colaborador, db, periodo, periodo.AddMonths(1).AddDays(-1), ct);

    // Contrato vigente en algún día de [desde, hasta]; frecuencia que ya rige en el mes de "desde".
    private static async Task<EstadoConfiguracion> CalcularEntreAsync(
        ColaboradorRps colaborador, RpsDbContext db, DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var mes = new DateOnly(desde.Year, desde.Month, 1);

        // Vigente, no "alguna vez registrada": una frecuencia con VigenteDesdeMes futuro no
        // cuenta todavía (mismo criterio que ColaboradoresService.ConstruirDetalleAsync).
        var tieneFrecuencia = await db.ColaboradoresFrecuencia
            .AnyAsync(f => f.ColaboradorId == colaborador.Id && f.VigenteDesdeMes <= mes, ct);

        var tieneCuentaActiva = await db.CuentasBancarias
            .AnyAsync(c => c.ColaboradorId == colaborador.Id && c.Activa, ct);

        // Considera adendas: un contrato cuya FechaFin original ya pasó sigue vigente si una
        // adenda extendió su fecha fin (RN 10-12).
        var contratosIniciados = await db.Contratos
            .Where(c => c.ColaboradorId == colaborador.Id && c.FechaInicio <= hasta)
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
                // Indefinido (sin fecha de fin): siempre vigente una vez iniciado.
                if (c.FechaFin is null) return true;
                var maxAdenda = maxAdendaPorContrato.GetValueOrDefault(c.Id);
                var fechaFinVigente = maxAdenda > c.FechaFin ? maxAdenda : c.FechaFin.Value;
                return fechaFinVigente >= desde;
            });
        }

        return colaborador.Clasificacion is not null && tieneFrecuencia && tieneCuentaActiva && tieneContratoVigente
            ? EstadoConfiguracion.Configurado
            : EstadoConfiguracion.Pendiente;
    }
}
