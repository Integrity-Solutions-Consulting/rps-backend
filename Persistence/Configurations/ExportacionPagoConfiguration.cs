using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Exportaciones.Domain;
using rps_backend.Features.Liquidaciones.Domain;

namespace rps_backend.Persistence.Configurations;

public class ExportacionPagoConfiguration : IEntityTypeConfiguration<ExportacionPago>
{
    public void Configure(EntityTypeBuilder<ExportacionPago> builder)
    {
        builder.ToTable("exportacion_pago");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Corte).HasConversion<string>().HasMaxLength(10);
        builder.Property(e => e.Usuario).HasMaxLength(100);
        builder.Property(e => e.NombreArchivo).HasMaxLength(200);
        builder.Property(e => e.Total).HasColumnType("numeric(12,2)");

        // Una sola versión N por mes y corte.
        builder.HasIndex(e => new { e.Periodo, e.Corte, e.Version }).IsUnique();
    }
}

public class ExportacionPagoDetalleConfiguration : IEntityTypeConfiguration<ExportacionPagoDetalle>
{
    public void Configure(EntityTypeBuilder<ExportacionPagoDetalle> builder)
    {
        builder.ToTable("exportacion_pago_detalle");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.TmrColaboradorId).HasMaxLength(50);
        builder.Property(d => d.Valor).HasColumnType("numeric(12,2)");
        builder.Property(d => d.Banco).HasMaxLength(100);
        builder.Property(d => d.CuentaEnmascarada).HasMaxLength(40);

        builder.HasOne<ExportacionPago>().WithMany().HasForeignKey(d => d.ExportacionPagoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Liquidacion>().WithMany().HasForeignKey(d => d.LiquidacionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ColaboradorRps>().WithMany().HasForeignKey(d => d.ColaboradorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.ExportacionPagoId);
        builder.HasIndex(d => d.ColaboradorId);
    }
}
