using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Contratos.Domain;
using rps_backend.Features.Proyectos.Domain;

namespace rps_backend.Persistence.Configurations;

public class ContratoConfiguration : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> builder)
    {
        builder.ToTable("contrato", t =>
            t.HasCheckConstraint(
                "ck_contrato_fecha_fin",
                "fecha_fin >= fecha_inicio"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Referencia)
            .HasMaxLength(100);

        builder.Property(c => c.Observaciones)
            .HasMaxLength(500);

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(c => c.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Proyecto>()
            .WithMany()
            .HasForeignKey(c => c.ProyectoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Varios contratos pueden estar vigentes a la vez para el mismo colaborador (distintos proyectos).
        builder.HasIndex(c => new { c.ColaboradorId, c.ProyectoId });
    }
}
