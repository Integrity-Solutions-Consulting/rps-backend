using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Auth.Domain;
using rps_backend.Features.Exportaciones.Domain;

namespace rps_backend.Persistence.Configurations;

public class ExportacionConfiguration : IEntityTypeConfiguration<Exportacion>
{
    public void Configure(EntityTypeBuilder<Exportacion> builder)
    {
        // "exportacion_reporte" y no "exportacion": ese nombre lo usó el antiguo Excel de pago (con archivo
        // guardado y versiones), que se quitó en la migración QuitarExcelDePago. Son conceptos distintos.
        builder.ToTable("exportacion_reporte");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Vista).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Formato).HasConversion<string>().HasMaxLength(10);
        builder.Property(e => e.NombreArchivo).HasMaxLength(150).IsRequired();
        builder.Property(e => e.TotalNeto).HasPrecision(14, 2);
        builder.Property(e => e.HuellaDatos).HasMaxLength(64).IsFixedLength().IsRequired();

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(e => e.GeneradoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // El historial se lista por mes, de lo más nuevo a lo más antiguo.
        builder.HasIndex(e => new { e.Periodo, e.GeneradoEn });
        builder.HasIndex(e => e.GeneradoEn);
    }
}
