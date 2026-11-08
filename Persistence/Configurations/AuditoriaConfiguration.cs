using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Auditoria.Domain;

namespace rps_backend.Persistence.Configurations;

public class AuditoriaConfiguration : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        builder.ToTable("auditoria");

        // bigint identity: es la tabla de mayor volumen de escritura (se llena en cada SaveChanges).
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseIdentityAlwaysColumn();

        builder.Property(a => a.Entidad).HasMaxLength(100);
        builder.Property(a => a.EntidadId).HasMaxLength(200);
        builder.Property(a => a.Campo).HasMaxLength(100);

        builder.HasIndex(a => new { a.Entidad, a.EntidadId });
        builder.HasIndex(a => a.Fecha);
    }
}
