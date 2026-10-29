using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Contratos.Domain;
using rps_backend.Features.Documentos.Domain;
using rps_backend.Features.Liquidaciones.Domain;
using rps_backend.Shared;

namespace rps_backend.Persistence.Configurations;

public class DocumentoConfiguration : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> builder)
    {
        builder.ToTable("documento", t =>
            t.HasCheckConstraint(
                "ck_documento_una_referencia",
                "(colaborador_id IS NOT NULL)::int + (contrato_id IS NOT NULL)::int + (liquidacion_id IS NOT NULL)::int = 1"));

        builder.HasKey(d => d.Id);

        builder.Property(d => d.NombreOriginal)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.RutaArchivo)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.Origen)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(OrigenDocumento.Subido);

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(d => d.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Contrato>()
            .WithMany()
            .HasForeignKey(d => d.ContratoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Liquidacion>()
            .WithMany()
            .HasForeignKey(d => d.LiquidacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
