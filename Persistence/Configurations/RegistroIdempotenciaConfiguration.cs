using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Persistence;

namespace rps_backend.Persistence.Configurations;

public class RegistroIdempotenciaConfiguration : IEntityTypeConfiguration<RegistroIdempotencia>
{
    public void Configure(EntityTypeBuilder<RegistroIdempotencia> builder)
    {
        builder.ToTable("idempotencia");

        builder.HasKey(r => r.Clave);
        builder.Property(r => r.Clave).HasMaxLength(100);
        builder.Property(r => r.ContentType).HasMaxLength(100);
        builder.Property(r => r.Cuerpo).HasColumnType("bytea");

        builder.HasIndex(r => r.CreadoEn);
    }
}
