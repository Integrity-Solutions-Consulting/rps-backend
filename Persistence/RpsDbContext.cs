using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Anticipos.Domain;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Features.Configuracion.Domain;
using rps_backend.Features.Contratos.Domain;
using rps_backend.Features.Documentos.Domain;
using rps_backend.Features.Feriados.Domain;
using rps_backend.Features.Honorarios.Domain;
using rps_backend.Features.Liquidaciones.Domain;
using rps_backend.Features.Proyectos.Domain;
namespace rps_backend.Persistence;

// IDataProtectionKeyContext: las llaves de cifrado de CuentaBancariaProtector se guardan en la
// misma base (persisten entre redeploys/contenedores nuevos), no en un llavero efímero en memoria.
public class RpsDbContext : DbContext, IDataProtectionKeyContext
{
    public RpsDbContext(DbContextOptions<RpsDbContext> options) : base(options)
    {
    }

    public DbSet<ColaboradorRps> ColaboradoresRps => Set<ColaboradorRps>();
    public DbSet<ColaboradorFrecuencia> ColaboradoresFrecuencia => Set<ColaboradorFrecuencia>();
    public DbSet<CuentaBancaria> CuentasBancarias => Set<CuentaBancaria>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<rps_backend.Features.Contratos.Domain.Adenda> Adendas => Set<rps_backend.Features.Contratos.Domain.Adenda>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<Asignacion> Asignaciones => Set<Asignacion>();
    public DbSet<CondicionEconomica> CondicionesEconomicas => Set<CondicionEconomica>();
    public DbSet<Honorario> Honorarios => Set<Honorario>();
    public DbSet<Liquidacion> Liquidaciones => Set<Liquidacion>();
    public DbSet<rps_backend.Features.Liquidaciones.Domain.Corte> Cortes => Set<rps_backend.Features.Liquidaciones.Domain.Corte>();
    public DbSet<AnticipoAdicional> AnticiposAdicionales => Set<AnticipoAdicional>();
    public DbSet<CuotaRecuperacion> CuotasRecuperacion => Set<CuotaRecuperacion>();
    public DbSet<ConfiguracionGlobal> ConfiguracionGlobal => Set<ConfiguracionGlobal>();
    public DbSet<DocumentoMensual> DocumentosMensuales => Set<DocumentoMensual>();
    public DbSet<Feriado> Feriados => Set<Feriado>();
    public DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey> DataProtectionKeys
        => Set<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rps");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RpsDbContext).Assembly);
    }
    
}