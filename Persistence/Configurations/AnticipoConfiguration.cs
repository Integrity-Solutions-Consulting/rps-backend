using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Anticipos.Domain;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Honorarios.Domain;
using rps_backend.Shared;

namespace rps_backend.Persistence.Configurations;

public class AnticipoConfiguration : IEntityTypeConfiguration<Anticipo>
{
    public void Configure(EntityTypeBuilder<Anticipo> builder)
    {
        builder.ToTable("anticipo", t =>
            t.HasCheckConstraint(
                "ck_anticipo_estado",
                "estado IN ('Pendiente', 'Aprobado', 'Descontado', 'Anulado')"));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Monto)
            .HasColumnType("numeric(12,2)");

        builder.Property(a => a.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EstadoAnticipo.Pendiente);

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(a => a.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Honorario>()
            .WithMany()
            .HasForeignKey(a => a.HonorarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
