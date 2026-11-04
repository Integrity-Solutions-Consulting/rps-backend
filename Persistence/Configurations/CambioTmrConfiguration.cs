using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.IntegracionTmr.Domain;
using rps_backend.Features.Proyectos.Domain;

namespace rps_backend.Persistence.Configurations;

public class CambioTmrConfiguration : IEntityTypeConfiguration<CambioTmr>
{
    public void Configure(EntityTypeBuilder<CambioTmr> builder)
    {
        builder.ToTable("cambio_tmr");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Tipo)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(c => c.ValorAnterior)
            .HasMaxLength(100);

        builder.Property(c => c.ValorNuevo)
            .HasMaxLength(100);

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(c => c.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Proyecto>()
            .WithMany()
            .HasForeignKey(c => c.ProyectoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.Revisado);
    }
}
