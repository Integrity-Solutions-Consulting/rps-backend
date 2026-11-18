using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Prestamos.Domain;

namespace rps_backend.Persistence.Configurations;

public class PrestamoConfiguration : IEntityTypeConfiguration<Prestamo>
{
    public void Configure(EntityTypeBuilder<Prestamo> builder)
    {
        builder.ToTable("prestamo", t =>
            t.HasCheckConstraint("ck_prestamo_monto", "monto > 0"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Monto).HasColumnType("numeric(12,2)");
        builder.Property(p => p.Observaciones).HasMaxLength(500);

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(p => p.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.ColaboradorId);

        // RC 04: dos personas editando el mismo préstamo a la vez.
        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsRowVersion();
    }
}

public class CuotaPrestamoConfiguration : IEntityTypeConfiguration<CuotaPrestamo>
{
    public void Configure(EntityTypeBuilder<CuotaPrestamo> builder)
    {
        builder.ToTable("cuota_prestamo", t =>
            t.HasCheckConstraint("ck_cuota_prestamo_importe", "importe > 0"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Importe).HasColumnType("numeric(12,2)");
        builder.Property(c => c.Corte).HasConversion<string>().HasMaxLength(10);

        builder.HasOne<Prestamo>()
            .WithMany()
            .HasForeignKey(c => c.PrestamoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un solo pago por corte en cada préstamo: dos en el mismo corte se suman en uno.
        builder.HasIndex(c => new { c.PrestamoId, c.Periodo, c.Corte }).IsUnique();
    }
}
