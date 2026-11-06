using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Auth.Domain;

namespace rps_backend.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuario");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.Nombre)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(u => u.HashPassword)
            .HasMaxLength(200)
            .IsRequired();

        // Sentinel true: si Activo = false, EF lo envía y no lo pisa el default de la base.
        builder.Property(u => u.Activo)
            .HasDefaultValue(true)
            .HasSentinel(true);

        builder.Property(u => u.IntentosFallidos)
            .HasDefaultValue(0);

        builder.HasMany(u => u.Roles)
            .WithOne(ur => ur.Usuario)
            .HasForeignKey(ur => ur.UsuarioId);
    }
}
