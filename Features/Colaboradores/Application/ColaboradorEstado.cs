using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Colaboradores.Application;

public static class ColaboradorEstado
{
    // Configurado = tiene clasificación + al menos una frecuencia + una cuenta bancaria activa.
    // Consulta la base, así que se llama después de guardar los cambios de frecuencia/cuenta.
    public static async Task RecalcularAsync(ColaboradorRps colaborador, RpsDbContext db, CancellationToken ct)
    {
        var tieneFrecuencia = await db.ColaboradoresFrecuencia
            .AnyAsync(f => f.ColaboradorId == colaborador.Id, ct);

        var tieneCuentaActiva = await db.CuentasBancarias
            .AnyAsync(c => c.ColaboradorId == colaborador.Id && c.Activa, ct);

        colaborador.EstadoConfiguracion =
            colaborador.Clasificacion is not null && tieneFrecuencia && tieneCuentaActiva
                ? EstadoConfiguracion.Configurado
                : EstadoConfiguracion.Pendiente;

        colaborador.ActualizadoEn = DateTime.UtcNow;
    }
}
