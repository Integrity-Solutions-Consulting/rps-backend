using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Liquidaciones.Domain;
using rps_backend.Shared;

namespace rps_backend.Persistence.Configurations;

public class LiquidacionConfiguration : IEntityTypeConfiguration<Liquidacion>
{
    public void Configure(EntityTypeBuilder<Liquidacion> builder)
    {
        builder.ToTable("liquidacion", t =>
            t.HasCheckConstraint("ck_liquidacion_estado", "estado IN ('Generada', 'Pagada')"));

        builder.HasKey(l => l.Id);

        builder.Property(l => l.MontoTotal)
            .HasColumnType("numeric(12,2)");

        builder.Property(l => l.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EstadoLiquidacion.Generada);

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(l => l.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.ColaboradorId, l.Periodo });
    }
}
