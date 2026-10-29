using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Contratos.Domain;
using rps_backend.Features.Honorarios.Domain;
using rps_backend.Features.Liquidaciones.Domain;
using rps_backend.Shared;

namespace rps_backend.Persistence.Configurations;

public class HonorarioConfiguration : IEntityTypeConfiguration<Honorario>
{
    public void Configure(EntityTypeBuilder<Honorario> builder)
    {
        builder.ToTable("honorario", t =>
        {
            t.HasCheckConstraint("ck_honorario_numero_pago", "numero_pago IN (1, 2)");
            t.HasCheckConstraint(
                "ck_honorario_estado",
                "estado IN ('Pendiente', 'Aprobado', 'Pagado', 'Anulado')");
        });

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Monto)
            .HasColumnType("numeric(12,2)");

        builder.Property(h => h.Estado)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EstadoHonorario.Pendiente);

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(h => h.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Contrato>()
            .WithMany()
            .HasForeignKey(h => h.ContratoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Liquidacion>()
            .WithMany()
            .HasForeignKey(h => h.LiquidacionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Hace que generar un período sea idempotente: no se puede duplicar el mismo pago.
        builder.HasIndex(h => new { h.ColaboradorId, h.ContratoId, h.Periodo, h.NumeroPago })
            .IsUnique();
    }
}
