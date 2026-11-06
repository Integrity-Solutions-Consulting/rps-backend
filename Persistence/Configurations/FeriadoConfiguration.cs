using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Feriados.Domain;

namespace rps_backend.Persistence.Configurations;

public class FeriadoConfiguration : IEntityTypeConfiguration<Feriado>
{
    public void Configure(EntityTypeBuilder<Feriado> builder)
    {
        builder.ToTable("feriado");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Nombre)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(f => f.Fecha)
            .IsUnique();
    }
}
