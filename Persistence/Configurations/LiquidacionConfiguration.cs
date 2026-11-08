using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Liquidaciones.Domain;

namespace rps_backend.Persistence.Configurations;

public class LiquidacionConfiguration : IEntityTypeConfiguration<Liquidacion>
{
    public void Configure(EntityTypeBuilder<Liquidacion> builder)
    {
        builder.ToTable("liquidacion");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.NetoConsolidado)
            .HasColumnType("numeric(12,2)");

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(l => l.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Una sola Liquidacion por colaborador y período (RN 30): sin esto, dos corridas de
        // GenerarAsync para el mismo mes podían crear una segunda liquidación y recuperar la
        // misma cuota de anticipo dos veces.
        builder.HasIndex(l => new { l.ColaboradorId, l.Periodo }).IsUnique();

        // Token de concurrencia nativo de PostgreSQL (RC 04): dos "Calcular"/"Revisar" simultáneos
        // sobre la misma liquidación ya no se pisan en silencio — el segundo guardado falla con 409.
        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsRowVersion();
    }
}
