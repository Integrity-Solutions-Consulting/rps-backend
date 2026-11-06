using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Proyectos.Domain;

namespace rps_backend.Persistence.Configurations;

public class AsignacionConfiguration : IEntityTypeConfiguration<Asignacion>
{
    public void Configure(EntityTypeBuilder<Asignacion> builder)
    {
        builder.ToTable("asignacion", t =>
            t.HasCheckConstraint("ck_asignacion_fecha_salida", "fecha_salida IS NULL OR fecha_salida >= fecha_inicio"));

        builder.HasKey(a => a.Id);

        builder.HasOne<ColaboradorRps>()
            .WithMany()
            .HasForeignKey(a => a.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Proyecto>()
            .WithMany()
            .HasForeignKey(a => a.ProyectoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.ColaboradorId, a.ProyectoId });
    }
}
