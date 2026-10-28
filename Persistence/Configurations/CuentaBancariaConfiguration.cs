using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;

namespace rps_backend.Persistence.Configurations;

public class CuentaBancariaConfiguration : IEntityTypeConfiguration<CuentaBancaria>
{
    public void Configure(EntityTypeBuilder<CuentaBancaria> builder)
    {
        builder.ToTable("cuenta_bancaria");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Banco)
            .HasMaxLength(100);

        builder.Property(c => c.TipoCuenta)
            .HasMaxLength(20);

        builder.Property(c => c.NumeroCuentaCifrado)
            .HasMaxLength(200)
            .IsRequired();

        // Sentinel true: si Activa = false, EF lo envía y no lo pisa el default de la base.
        builder.Property(c => c.Activa)
            .HasDefaultValue(true)
            .HasSentinel(true);

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(c => c.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.ColaboradorId)
            .IsUnique()
            .HasFilter("activa = true");
    }
}
