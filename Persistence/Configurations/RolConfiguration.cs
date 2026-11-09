using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Auth.Domain;

namespace rps_backend.Persistence.Configurations;

public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("rol");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Nombre)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(r => r.Nombre)
            .IsUnique();

        builder.Property(r => r.Descripcion)
            .HasMaxLength(200);

        builder.Property(r => r.Activo)
            .HasDefaultValue(true)
            .HasSentinel(true);

        builder.Property(r => r.Permisos)
            .HasColumnType("text[]")
            .IsRequired();
    }
}
