using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Contratos.Domain;

namespace rps_backend.Persistence.Configurations;

public class ContratoConfiguration : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> builder)
    {
        builder.ToTable("contrato", t =>
            t.HasCheckConstraint(
                "ck_contrato_fecha_fin",
                "fecha_fin IS NULL OR fecha_fin >= fecha_inicio"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TipoServicio)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Tarifa)
            .HasColumnType("numeric(12,2)");

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(c => c.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Varios contratos pueden estar vigentes a la vez para el mismo colaborador.
        builder.HasIndex(c => c.ColaboradorId);
    }
}
