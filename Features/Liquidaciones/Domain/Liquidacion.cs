namespace rps_backend.Features.Liquidaciones.Domain;

// Consolida los Honorario de todos los proyectos reconocidos de un colaborador en un período
// (RN 30-31), antes de anticipos adicionales. No existe estado "Pagada": el pago bancario está
// fuera de alcance (RN 56); "Revisado" es solo el control interno previo a exportar (RN 49).
public class Liquidacion
{
    public const int LargoMaximoMotivo = 1000;

    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public DateOnly Periodo { get; set; }

    public decimal NetoConsolidado { get; set; }

    public bool Revisado { get; set; }

    public DateTime? RevisadoEn { get; set; }

    // Quién marcó "Revisado para envío" (RC 03). Null si nunca se ha revisado.
    public Guid? RevisadoPorUsuarioId { get; set; }

    // Aviso de que la liquidación cambió DESPUÉS de calculada (anticipo nuevo, honorarios que
    // llegaron tarde...). Queda visible hasta que se vuelva a marcar revisada. Null = sin cambios
    // pendientes de revisar.
    public DateTime? ModificadaEn { get; set; }

    public string? MotivoModificacion { get; set; }

    // true si estaba "Revisada" cuando cambió y por eso volvió a "Por revisar".
    public bool RevisionAnulada { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    // Registra un cambio posterior al cálculo: deja el motivo a la vista y, si ya estaba revisada,
    // la devuelve a "Por revisar" (lo que se revisó ya no es lo que se va a pagar). Si hay varios
    // cambios antes de la siguiente revisión, los motivos se acumulan.
    public void RegistrarModificacion(string motivo)
    {
        var motivos = ModificadaEn is not null && !string.IsNullOrEmpty(MotivoModificacion)
            ? $"{MotivoModificacion} · {motivo}"
            : motivo;
        MotivoModificacion = motivos.Length > LargoMaximoMotivo ? motivos[^LargoMaximoMotivo..] : motivos;
        ModificadaEn = DateTime.UtcNow;

        if (Revisado)
        {
            RevisionAnulada = true;
            Revisado = false;
            RevisadoEn = null;
            RevisadoPorUsuarioId = null;
        }
    }

    // "Revisado para envío": también da por visto el aviso de cambios.
    public void MarcarRevisada(Guid usuarioId)
    {
        Revisado = true;
        RevisadoEn = DateTime.UtcNow;
        RevisadoPorUsuarioId = usuarioId;
        ModificadaEn = null;
        MotivoModificacion = null;
        RevisionAnulada = false;
    }
}
