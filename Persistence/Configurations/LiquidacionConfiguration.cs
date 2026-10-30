using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Liquidaciones.Domain;

namespace rps_backend.Persistence.Configurations;

public class LiquidacionConfiguration : IEntityTypeConfiguration<Liquidacion>
{
    public void Configure(EntityTypeBuilder<Liquidacion> builder)
    {
        builder.ToTable("liquidacion");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.NetoConsolidado)
            .HasColumnType("numeric(12,2)");

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(l => l.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.ColaboradorId, l.Periodo });
    }
}
