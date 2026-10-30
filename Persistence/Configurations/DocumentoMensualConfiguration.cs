using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Documentos.Domain;

namespace rps_backend.Persistence.Configurations;

public class DocumentoMensualConfiguration : IEntityTypeConfiguration<DocumentoMensual>
{
    public void Configure(EntityTypeBuilder<DocumentoMensual> builder)
    {
        builder.ToTable("documento_mensual");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Tipo)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Numero)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(d => d.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un documento mensual por colaborador y período (RN 32: dos pagos no originan dos documentos).
        builder.HasIndex(d => new { d.ColaboradorId, d.Periodo })
            .IsUnique();
    }
}
