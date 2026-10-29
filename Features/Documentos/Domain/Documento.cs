using rps_backend.Shared;

namespace rps_backend.Features.Documentos.Domain;

public class Documento
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid? ColaboradorId { get; set; }

    public Guid? ContratoId { get; set; }

    public Guid? LiquidacionId { get; set; }

    public string NombreOriginal { get; set; } = string.Empty;

    // Relativa a la carpeta de subida (p.ej. "uploads/{id}.pdf").
    public string RutaArchivo { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long TamanoBytes { get; set; }

    public OrigenDocumento Origen { get; set; } = OrigenDocumento.Subido;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
