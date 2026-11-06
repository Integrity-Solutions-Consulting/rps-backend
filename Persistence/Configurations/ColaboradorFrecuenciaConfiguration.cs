using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;

namespace rps_backend.Persistence.Configurations;

public class ColaboradorFrecuenciaConfiguration : IEntityTypeConfiguration<ColaboradorFrecuencia>
{
    public void Configure(EntityTypeBuilder<ColaboradorFrecuencia> builder)
    {
        builder.ToTable("colaborador_frecuencia", t =>
        {
            t.HasCheckConstraint(
                "ck_colaborador_frecuencia_frecuencia",
                "frecuencia IN ('DosPagos', 'FinDeMes')");
            t.HasCheckConstraint(
                "ck_colaborador_frecuencia_primer_dia_mes",
                "EXTRACT(DAY FROM vigente_desde_mes) = 1");
        });

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Frecuencia)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(f => f.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.ColaboradorId, f.VigenteDesdeMes })
            .IsUnique();
    }
}
