using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Contratos.Domain;
using rps_backend.Features.Honorarios.Domain;
using rps_backend.Features.Proyectos.Domain;

namespace rps_backend.Persistence.Configurations;

public class CondicionEconomicaConfiguration : IEntityTypeConfiguration<CondicionEconomica>
{
    public void Configure(EntityTypeBuilder<CondicionEconomica> builder)
    {
        builder.ToTable("condicion_economica");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ImporteMensual).HasColumnType("numeric(12,2)");
        builder.Property(c => c.Modalidad).HasConversion<string>().HasMaxLength(10);
        builder.Property(c => c.Observaciones).HasMaxLength(500);

        builder.HasOne<ColaboradorRps>().WithMany().HasForeignKey(c => c.ColaboradorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Proyecto>().WithMany().HasForeignKey(c => c.ProyectoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Contrato>().WithMany().HasForeignKey(c => c.ContratoId).OnDelete(DeleteBehavior.Restrict);

        // No se repite el mismo mes de vigencia dos veces para la misma relación colaborador-proyecto.
        builder.HasIndex(c => new { c.ColaboradorId, c.ProyectoId, c.MesInicioVigencia }).IsUnique();
    }
}
