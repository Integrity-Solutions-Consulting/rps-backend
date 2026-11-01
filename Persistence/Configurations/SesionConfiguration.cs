using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Auth.Domain;

namespace rps_backend.Persistence.Configurations;

public class SesionConfiguration : IEntityTypeConfiguration<Sesion>
{
    public void Configure(EntityTypeBuilder<Sesion> builder)
    {
        builder.ToTable("sesion");

        builder.HasKey(s => s.Id);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(s => s.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.UsuarioId);
    }
}
