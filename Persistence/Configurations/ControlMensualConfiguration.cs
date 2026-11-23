using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Liquidaciones.Domain;

namespace rps_backend.Persistence.Configurations;

public class ControlMensualConfiguration : IEntityTypeConfiguration<ControlMensual>
{
    public void Configure(EntityTypeBuilder<ControlMensual> builder)
    {
        builder.ToTable("control_mensual", t =>
            t.HasCheckConstraint("ck_control_mensual_dias_descontados", "dias_descontados >= 0 AND dias_descontados <= 31"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Observaciones).HasMaxLength(ControlMensual.LargoMaximoObservaciones);

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(c => c.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Una sola fila por colaborador y mes.
        builder.HasIndex(c => new { c.ColaboradorId, c.Periodo }).IsUnique();
    }
}
