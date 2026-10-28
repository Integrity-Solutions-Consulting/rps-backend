using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Colaboradores.Domain;
namespace rps_backend.Persistence;

public class RpsDbContext : DbContext
{
    public RpsDbContext(DbContextOptions<RpsDbContext> options) : base(options)
    {
    }

    public DbSet<ColaboradorRps> ColaboradoresRps => Set<ColaboradorRps>();
    public DbSet<ColaboradorFrecuencia> ColaboradoresFrecuencia => Set<ColaboradorFrecuencia>();
    public DbSet<CuentaBancaria> CuentasBancarias => Set<CuentaBancaria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rps");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RpsDbContext).Assembly);
    }
    
}