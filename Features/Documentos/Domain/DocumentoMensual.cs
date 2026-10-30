using rps_backend.Shared;

namespace rps_backend.Features.Documentos.Domain;

public class DocumentoMensual
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ColaboradorId { get; set; }

    public DateOnly Periodo { get; set; }

    public TipoDocumento Tipo { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateOnly FechaEmision { get; set; }

    public DateTime MarcadoEn { get; set; } = DateTime.UtcNow;
}
