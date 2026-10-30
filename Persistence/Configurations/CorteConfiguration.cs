using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Liquidaciones.Domain;

namespace rps_backend.Persistence.Configurations;

public class CorteConfiguration : IEntityTypeConfiguration<Corte>
{
    public void Configure(EntityTypeBuilder<Corte> builder)
    {
        builder.ToTable("corte");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(10);
        builder.Property(c => c.ValorBase).HasColumnType("numeric(12,2)");
        builder.Property(c => c.RecuperacionAdicional).HasColumnType("numeric(12,2)");
        builder.Property(c => c.ValorPreparado).HasColumnType("numeric(12,2)");

        builder.HasOne<Liquidacion>()
            .WithMany()
            .HasForeignKey(c => c.LiquidacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.LiquidacionId, c.Tipo }).IsUnique();
    }
}
