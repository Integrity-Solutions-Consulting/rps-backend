using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Contratos.Domain;
using rps_backend.Features.Honorarios.Domain;
using rps_backend.Features.Liquidaciones.Domain;
using rps_backend.Features.Proyectos.Domain;
using rps_backend.Shared;

namespace rps_backend.Persistence.Configurations;

public class HonorarioConfiguration : IEntityTypeConfiguration<Honorario>
{
    public void Configure(EntityTypeBuilder<Honorario> builder)
    {
        builder.ToTable("honorario");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Modalidad).HasConversion<string>().HasMaxLength(10);

        builder.Property(h => h.ProporcionalAcordado).HasColumnType("numeric(12,2)");
        builder.Property(h => h.Subtotal).HasColumnType("numeric(12,2)");
        builder.Property(h => h.Iva).HasColumnType("numeric(12,2)");
        builder.Property(h => h.TotalDocumento).HasColumnType("numeric(12,2)");
        builder.Property(h => h.RetencionFuente).HasColumnType("numeric(12,2)");
        builder.Property(h => h.RetencionIva).HasColumnType("numeric(12,2)");
        builder.Property(h => h.TotalRetenciones).HasColumnType("numeric(12,2)");
        builder.Property(h => h.Neto).HasColumnType("numeric(12,2)");

        builder.Property(h => h.PorcentajeIva).HasColumnType("numeric(5,2)");
        builder.Property(h => h.PorcentajeRetencionFuente).HasColumnType("numeric(5,2)");
        builder.Property(h => h.PorcentajeRetencionIva).HasColumnType("numeric(5,2)");

        builder.HasOne<ColaboradorRps>().WithMany().HasForeignKey(h => h.ColaboradorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Proyecto>().WithMany().HasForeignKey(h => h.ProyectoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Contrato>().WithMany().HasForeignKey(h => h.ContratoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CondicionEconomica>().WithMany().HasForeignKey(h => h.CondicionEconomicaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Liquidacion>().WithMany().HasForeignKey(h => h.LiquidacionId).OnDelete(DeleteBehavior.Restrict);

        // Hace que generar un período sea idempotente: no se puede duplicar el honorario de un proyecto.
        builder.HasIndex(h => new { h.ColaboradorId, h.ProyectoId, h.Periodo }).IsUnique();
    }
}
