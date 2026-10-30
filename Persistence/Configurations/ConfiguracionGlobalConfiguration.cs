using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Configuracion.Domain;

namespace rps_backend.Persistence.Configurations;

public class ConfiguracionGlobalConfiguration : IEntityTypeConfiguration<ConfiguracionGlobal>
{
    // Fila única: la tabla nace sembrada con este Id y nunca se crea/elimina otra por API.
    public static readonly Guid IdUnico = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public void Configure(EntityTypeBuilder<ConfiguracionGlobal> builder)
    {
        builder.ToTable("configuracion_global");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.RazonSocial)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Ruc)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Moneda)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(c => c.PorcentajeMaximoAnticipo).HasColumnType("numeric(5,2)");
        builder.Property(c => c.PorcentajeIva).HasColumnType("numeric(5,2)");
        builder.Property(c => c.PorcentajeRetencionFuente).HasColumnType("numeric(5,2)");
        builder.Property(c => c.PorcentajeRetencionIvaProfesional).HasColumnType("numeric(5,2)");
        builder.Property(c => c.PorcentajeRetencionIvaJunior).HasColumnType("numeric(5,2)");

        builder.HasData(new ConfiguracionGlobal
        {
            Id = IdUnico,
            RazonSocial = string.Empty,
            Ruc = string.Empty,
            Moneda = "USD",
            DiaPagoFinDeMes = 30,
            DiaPagoDosPagos1 = 15,
            DiaPagoDosPagos2 = 30,
            PorcentajeMaximoAnticipo = 100m,
            PorcentajeIva = 15m,
            PorcentajeRetencionFuente = 10m,
            PorcentajeRetencionIvaProfesional = 100m,
            PorcentajeRetencionIvaJunior = 70m,
            ActualizadoEn = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
