using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Anticipos.Domain;
using rps_backend.Features.Colaboradores.Domain;

namespace rps_backend.Persistence.Configurations;

public class AnticipoAdicionalConfiguration : IEntityTypeConfiguration<AnticipoAdicional>
{
    public void Configure(EntityTypeBuilder<AnticipoAdicional> builder)
    {
        builder.ToTable("anticipo_adicional");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Monto).HasColumnType("numeric(12,2)");
        builder.Property(a => a.Observaciones).HasMaxLength(500);

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(a => a.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.ColaboradorId);
    }
}

public class CuotaRecuperacionConfiguration : IEntityTypeConfiguration<CuotaRecuperacion>
{
    public void Configure(EntityTypeBuilder<CuotaRecuperacion> builder)
    {
        builder.ToTable("cuota_recuperacion", t =>
            t.HasCheckConstraint("ck_cuota_recuperacion_importe", "importe > 0"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Importe).HasColumnType("numeric(12,2)");
        builder.Property(c => c.Corte).HasConversion<string>().HasMaxLength(10);

        builder.HasOne<AnticipoAdicional>()
            .WithMany()
            .HasForeignKey(c => c.AnticipoAdicionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.AnticipoAdicionalId, c.Periodo, c.Corte }).IsUnique();
    }
}
