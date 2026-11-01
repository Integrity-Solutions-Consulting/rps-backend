using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Auth.Domain;

namespace rps_backend.Persistence.Configurations;

public class UsuarioRolConfiguration : IEntityTypeConfiguration<UsuarioRol>
{
    public void Configure(EntityTypeBuilder<UsuarioRol> builder)
    {
        builder.ToTable("usuario_rol");

        builder.HasKey(ur => new { ur.UsuarioId, ur.RolId });

        builder.HasOne(ur => ur.Rol)
            .WithMany()
            .HasForeignKey(ur => ur.RolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
