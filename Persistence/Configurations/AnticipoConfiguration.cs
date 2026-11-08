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

        // RC 04: protege ediciones/bajas concurrentes del MISMO anticipo (ej. dos personas lo
        // eliminan a la vez). No alcanza por sí solo para el límite de dos anticipos NUEVOS
        // simultáneos — eso se resuelve con el bloqueo de fila en AnticiposService.RegistrarAsync.
        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsRowVersion();
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
