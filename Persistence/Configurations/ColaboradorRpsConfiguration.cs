using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Shared;

namespace rps_backend.Persistence.Configurations;

public class ColaboradorRpsConfiguration : IEntityTypeConfiguration<ColaboradorRps>
{
    public void Configure(EntityTypeBuilder<ColaboradorRps> builder)
    {
        builder.ToTable("colaborador_rps", t =>
            t.HasCheckConstraint(
                "ck_colaborador_rps_clasificacion",
                "clasificacion IN ('Profesional', 'Junior')"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TmrColaboradorId)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(c => c.TmrColaboradorId)
            .IsUnique();

        builder.Property(c => c.Clasificacion)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.EstadoConfiguracion)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EstadoConfiguracion.Pendiente);
    }
}