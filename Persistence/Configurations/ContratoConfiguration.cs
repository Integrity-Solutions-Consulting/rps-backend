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

        // Varios contratos pueden estar vigentes a la vez para el mismo colaborador (distintos
        // proyectos, o incluso un contrato sin ningún proyecto — ver ContratoProyecto).
        builder.HasIndex(c => c.ColaboradorId);
    }
}

public class ContratoProyectoConfiguration : IEntityTypeConfiguration<ContratoProyecto>
{
    public void Configure(EntityTypeBuilder<ContratoProyecto> builder)
    {
        builder.ToTable("contrato_proyecto");

        builder.HasKey(cp => cp.Id);

        builder.HasOne<Contrato>()
            .WithMany()
            .HasForeignKey(cp => cp.ContratoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<rps_backend.Features.Proyectos.Domain.Proyecto>()
            .WithMany()
            .HasForeignKey(cp => cp.ProyectoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un proyecto no se repite dentro del mismo contrato.
        builder.HasIndex(cp => new { cp.ContratoId, cp.ProyectoId }).IsUnique();
        builder.HasIndex(cp => cp.ProyectoId);
    }
}
