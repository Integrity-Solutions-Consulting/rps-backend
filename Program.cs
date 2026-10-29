using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Colaboradores.Api;
using rps_backend.Features.Colaboradores.Application;
using rps_backend.Features.Contratos.Api;
using rps_backend.Features.Contratos.Application;
using rps_backend.Features.Honorarios.Api;
using rps_backend.Features.Honorarios.Application;
using rps_backend.Features.Liquidaciones.Api;
using rps_backend.Features.Liquidaciones.Application;
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
builder.Services.AddDataProtection().SetApplicationName("rps-backend");
builder.Services.AddSingleton<CuentaBancariaProtector>();
builder.Services.AddScoped<ColaboradoresService>();
builder.Services.AddScoped<ContratosService>();
builder.Services.AddScoped<HonorariosService>();
builder.Services.AddScoped<LiquidacionesService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapColaboradoresEndpoints();
app.MapContratosEndpoints();
app.MapHonorariosEndpoints();
app.MapLiquidacionesEndpoints();

app.Run();
