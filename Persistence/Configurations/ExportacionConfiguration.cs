using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Exportaciones.Domain;

namespace rps_backend.Persistence.Configurations;

public class ExportacionConfiguration : IEntityTypeConfiguration<Exportacion>
{
    public void Configure(EntityTypeBuilder<Exportacion> builder)
    {
        builder.ToTable("exportacion");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Corte).HasConversion<string>().HasMaxLength(10);
        builder.Property(e => e.NombreArchivo).HasMaxLength(200);
        builder.Property(e => e.Contenido).HasColumnType("bytea");

        builder.HasMany(e => e.Items)
            .WithOne()
            .HasForeignKey(i => i.ExportacionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cada corrida para el mismo (Periodo, Corte) es una versión nueva (RN 55).
        builder.HasIndex(e => new { e.Periodo, e.Corte, e.Version }).IsUnique();
    }
}
