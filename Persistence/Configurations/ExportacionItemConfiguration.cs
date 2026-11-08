using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Exportaciones.Domain;
using rps_backend.Features.Liquidaciones.Domain;

namespace rps_backend.Persistence.Configurations;

public class ExportacionItemConfiguration : IEntityTypeConfiguration<ExportacionItem>
{
    public void Configure(EntityTypeBuilder<ExportacionItem> builder)
    {
        builder.ToTable("exportacion_item");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Importe).HasColumnType("numeric(12,2)");

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(i => i.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Liquidacion>()
            .WithMany()
            .HasForeignKey(i => i.LiquidacionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un colaborador no se repite dentro de la misma Exportacion (RN 54).
        builder.HasIndex(i => new { i.ExportacionId, i.ColaboradorId }).IsUnique();
    }
}
