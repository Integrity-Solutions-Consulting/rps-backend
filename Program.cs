using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Anticipos.Api;
using rps_backend.Features.Anticipos.Application;
using rps_backend.Features.Colaboradores.Api;
using rps_backend.Features.Colaboradores.Application;
using rps_backend.Features.Configuracion.Api;
using rps_backend.Features.Configuracion.Application;
using rps_backend.Features.Contratos.Api;
using rps_backend.Features.Contratos.Application;
using rps_backend.Features.Dashboard.Api;
using rps_backend.Features.Dashboard.Application;
using rps_backend.Features.Documentos.Api;
using rps_backend.Features.Documentos.Application;
using rps_backend.Features.Exportaciones.Api;
using rps_backend.Features.Exportaciones.Application;
using rps_backend.Features.Feriados.Api;
using rps_backend.Features.Feriados.Application;
using rps_backend.Features.Historial.Api;
using rps_backend.Features.Historial.Application;
using rps_backend.Features.Honorarios.Api;
using rps_backend.Features.Honorarios.Application;
using rps_backend.Features.Liquidaciones.Api;
using rps_backend.Features.Liquidaciones.Application;
using rps_backend.Features.Notificaciones.Api;
using rps_backend.Features.Notificaciones.Application;
using rps_backend.Features.Proyectos.Api;
using rps_backend.Features.Proyectos.Application;
using rps_backend.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<RpsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseSnakeCaseNamingConvention());

// Enums como texto en el JSON ("Profesional", "DosPagos") en vez de números.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
// Llaves persistidas en la misma base (no en un llavero efímero): sobreviven a redeploys y a
// contenedores nuevos, que de otro modo dejarían indescifrables los números de cuenta ya guardados.
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<RpsDbContext>()
    .SetApplicationName("rps-backend");
builder.Services.AddSingleton<CuentaBancariaProtector>();
builder.Services.AddScoped<ColaboradoresService>();
builder.Services.AddScoped<ContratosService>();
builder.Services.AddScoped<ProyectosService>();
builder.Services.AddScoped<AsignacionesService>();
builder.Services.AddScoped<HonorariosService>();
builder.Services.AddScoped<CondicionesEconomicasService>();
builder.Services.AddScoped<LiquidacionesService>();
builder.Services.AddScoped<AnticiposService>();
builder.Services.AddScoped<ConfiguracionService>();
builder.Services.AddScoped<DocumentosMensualesService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ExportacionesService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<FeriadosService>();
// Agregados para conectar el frontend: cancelar/restaurar honorarios, exportación por corte,
// notificaciones e historial.
builder.Services.AddScoped<CancelacionCondicionesService>();
builder.Services.AddScoped<ExportacionesPagoService>();
builder.Services.AddScoped<NotificacionesService>();
builder.Services.AddScoped<HistorialService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapColaboradoresEndpoints();
app.MapContratosEndpoints();
app.MapProyectosEndpoints();
app.MapAsignacionesEndpoints();
app.MapHonorariosEndpoints();
app.MapCondicionesEconomicasEndpoints();
app.MapLiquidacionesEndpoints();
app.MapAnticiposEndpoints();
app.MapConfiguracionEndpoints();
app.MapDocumentosMensualesEndpoints();
app.MapDashboardEndpoints();
app.MapExportacionesEndpoints();
app.MapFeriadosEndpoints();
app.MapCancelacionCondicionesEndpoints();
app.MapExportacionesPagoEndpoints();
app.MapNotificacionesEndpoints();
app.MapHistorialEndpoints();

app.Run();
