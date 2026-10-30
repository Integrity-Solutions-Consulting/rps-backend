using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Contratos.Domain;

namespace rps_backend.Persistence.Configurations;

public class AdendaConfiguration : IEntityTypeConfiguration<Adenda>
{
    public void Configure(EntityTypeBuilder<Adenda> builder)
    {
        builder.ToTable("adenda");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Observaciones)
            .HasMaxLength(500);

        builder.HasOne<Contrato>()
            .WithMany()
            .HasForeignKey(a => a.ContratoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.ContratoId);
    }
}
