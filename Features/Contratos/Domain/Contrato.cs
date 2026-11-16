namespace rps_backend.Features.Contratos.Domain;

// Un contrato es un documento administrativo de Contabilidad para un colaborador: puede cubrir
// uno, varios o ningún proyecto (ver ContratoProyecto) — por ejemplo, personal de RR. HH. sin
// proyecto en TMR todavía tiene contrato. Las fechas son las del documento legal; los días
// reconocidos para el cálculo siguen saliendo de la Asignación de TMR de cada proyecto, no de aquí.
public class Contrato
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public DateOnly FechaInicio { get; set; }

    // Fecha original acordada: una adenda la extiende pero no la reemplaza (ver Adenda.NuevaFechaFin).
    // Null = contrato indefinido (sin fecha de fin); nunca vence y no admite adendas.
    public DateOnly? FechaFin { get; set; }

    public string? Referencia { get; set; }

    public string? Observaciones { get; set; }

    // Terminado antes de su fin porque el colaborador se desactivó: el contrato rige hasta este día
    // inclusive (ese mes se paga hasta aquí) y ya no admite adendas. Si se reactiva, necesita un
    // contrato nuevo. FechaFin y las adendas no se tocan (RN 12). Ver VigenciaContrato.
    public DateOnly? FechaTerminacion { get; set; }

    public string? MotivoTerminacion { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}

// Hasta cuándo rige un contrato: el fin original extendido por la adenda más lejana (null =
// indefinido) y, si se terminó antes (desactivación), no más allá de la terminación. Todo cálculo de
// "fin vigente" pasa por aquí, para que pago, estado, avisos y anticipos digan lo mismo.
public static class VigenciaContrato
{
    public static DateOnly? FinEfectivo(DateOnly? fechaFin, DateOnly? maxAdenda, DateOnly? fechaTerminacion)
    {
        DateOnly? fin = fechaFin is null ? null : (maxAdenda is { } a && a > fechaFin ? a : fechaFin);
        if (fechaTerminacion is { } t && (fin is null || t < fin))
            fin = t;
        return fin;
    }
}
