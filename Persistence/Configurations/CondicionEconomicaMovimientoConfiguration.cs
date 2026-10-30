using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Honorarios.Domain;

namespace rps_backend.Persistence.Configurations;

public class CondicionEconomicaMovimientoConfiguration : IEntityTypeConfiguration<CondicionEconomicaMovimiento>
{
    public void Configure(EntityTypeBuilder<CondicionEconomicaMovimiento> builder)
    {
        builder.ToTable("condicion_economica_movimiento");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Tipo).HasMaxLength(20);
        builder.Property(m => m.Usuario).HasMaxLength(100);

        builder.HasOne<CondicionEconomica>().WithMany().HasForeignKey(m => m.CondicionEconomicaId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.CondicionEconomicaId);
    }
}
