using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Anticipos.Domain;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Configuracion.Domain;
using rps_backend.Features.Contratos.Domain;
using rps_backend.Features.Honorarios.Domain;
using rps_backend.Features.Liquidaciones.Domain;
namespace rps_backend.Persistence;

public class RpsDbContext : DbContext
{
    public RpsDbContext(DbContextOptions<RpsDbContext> options) : base(options)
    {
    }

    public DbSet<ColaboradorRps> ColaboradoresRps => Set<ColaboradorRps>();
    public DbSet<ColaboradorFrecuencia> ColaboradoresFrecuencia => Set<ColaboradorFrecuencia>();
    public DbSet<CuentaBancaria> CuentasBancarias => Set<CuentaBancaria>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Honorario> Honorarios => Set<Honorario>();
    public DbSet<Liquidacion> Liquidaciones => Set<Liquidacion>();
    public DbSet<Anticipo> Anticipos => Set<Anticipo>();
    public DbSet<ConfiguracionGlobal> ConfiguracionGlobal => Set<ConfiguracionGlobal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rps");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RpsDbContext).Assembly);
    }
    
}