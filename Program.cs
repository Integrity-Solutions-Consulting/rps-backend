using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using rps_backend.Features.Anticipos.Api;
using rps_backend.Features.Anticipos.Application;
using rps_backend.Features.Auditoria.Api;
using rps_backend.Features.Auditoria.Application;
using rps_backend.Features.Auth.Api;
using rps_backend.Features.Auth.Application;
using rps_backend.Features.Colaboradores.Api;
using rps_backend.Features.Colaboradores.Application;
using rps_backend.Features.Configuracion.Api;
using rps_backend.Features.Configuracion.Application;
using rps_backend.Features.Contratos.Api;
using rps_backend.Features.Contratos.Application;
using rps_backend.Features.Dashboard.Api;
using rps_backend.Features.Dashboard.Application;
using rps_backend.Features.Exportaciones.Api;
using rps_backend.Features.Exportaciones.Application;
using rps_backend.Features.Feriados.Api;
using rps_backend.Features.Feriados.Application;
using rps_backend.Features.Historial.Api;
using rps_backend.Features.Historial.Application;
using rps_backend.Features.Honorarios.Api;
using rps_backend.Features.Honorarios.Application;
using rps_backend.Features.IntegracionTmr.Api;
using rps_backend.Features.IntegracionTmr.Application;
using rps_backend.Features.Liquidaciones.Api;
using rps_backend.Features.Liquidaciones.Application;
using rps_backend.Features.Proyectos.Api;
using rps_backend.Features.Proyectos.Application;
using rps_backend.Integrations.Tmr;
using rps_backend.Persistence;

// QuestPDF (PDF de liquidaciones): licencia Community, gratuita para empresas con ingresos anuales
// menores a 1 millón de USD. Si la empresa supera ese límite, cambiar a la licencia que corresponda.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditoriaInterceptor>();
builder.Services.AddDbContext<RpsDbContext>((sp, options) =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseSnakeCaseNamingConvention()
    .AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>()));

// ── Autenticación JWT ──
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) || jwtSettings.SecretKey.Length < 32)
    throw new InvalidOperationException("Jwt:SecretKey debe configurarse (variable de entorno Jwt__SecretKey) y tener al menos 32 caracteres.");

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization(options =>
    // Cualquier endpoint sin AllowAnonymous explícito exige estar autenticado (igual que tmr-backend).
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddRateLimiter(options =>
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 10;
        limiter.QueueLimit = 0;
    }));

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
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<VencimientosContratosService>();
builder.Services.AddScoped<ExportacionesService>();
builder.Services.AddScoped<ExportacionPagoService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<FeriadosService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UsuariosService>();
builder.Services.AddScoped<RolesService>();
// Sin Tmr:BaseUrl la app arranca igual: solo la sincronización responde que TMR no está disponible.
builder.Services.AddHttpClient<ITmrGateway, TmrHttpGateway>(client =>
{
    if (Uri.TryCreate(builder.Configuration["Tmr:BaseUrl"], UriKind.Absolute, out var baseUrl))
        client.BaseAddress = baseUrl;
    client.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["Tmr:ApiKey"]);
});
builder.Services.AddScoped<SincronizacionTmrService>();
builder.Services.AddScoped<CambiosTmrService>();

var sincronizacionTmr = builder.Configuration.GetSection("Tmr:Sincronizacion").Get<SincronizacionTmrOpciones>() ?? new SincronizacionTmrOpciones();
builder.Services.AddSingleton(sincronizacionTmr);
if (sincronizacionTmr.Habilitada && sincronizacionTmr.IntervaloMinutos < 1)
    throw new InvalidOperationException("Tmr:Sincronizacion:IntervaloMinutos debe ser de al menos 1 minuto.");

builder.Services.AddQuartz(q =>
{
    if (sincronizacionTmr.Habilitada)
    {
        var jobKey = new JobKey(nameof(SincronizacionTmrJob));
        q.AddJob<SincronizacionTmrJob>(j => j.WithIdentity(jobKey));
        // Si una ejecución se retrasa no se acumulan repeticiones: espera a la siguiente programada.
        q.AddTrigger(t => t
            .ForJob(jobKey)
            .StartNow()
            .WithSimpleSchedule(s => s
                .WithInterval(TimeSpan.FromMinutes(sincronizacionTmr.IntervaloMinutos))
                .RepeatForever()
                .WithMisfireInstruction(SimpleTriggerMisfireInstruction.NextWithRemainingCount)));
    }

    // Estado Configurado/Pendiente: al arrancar y todos los días a las 05:05 UTC (00:05 en Ecuador),
    // cuando ya cambió la fecha con la que se evalúan contratos y frecuencias.
    var recalculoKey = new JobKey(nameof(RecalculoEstadoJob));
    q.AddJob<RecalculoEstadoJob>(j => j.WithIdentity(recalculoKey));
    q.AddTrigger(t => t.ForJob(recalculoKey).StartNow());
    q.AddTrigger(t => t
        .ForJob(recalculoKey)
        .WithCronSchedule("0 5 5 * * ?", c => c.InTimeZone(TimeZoneInfo.Utc)));
});
builder.Services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);
// Historial del colaborador: solo lee lo que guardan los demás módulos.
builder.Services.AddScoped<HistorialService>();

var app = builder.Build();

// "dotnet rps-backend.dll --migrate": aplica las migraciones pendientes y termina. Sirve en el
// contenedor desplegado, que no trae dotnet-ef; las migraciones siguen sin aplicarse al arrancar.
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RpsDbContext>();
    var pendientes = (await db.Database.GetPendingMigrationsAsync()).ToList();
    await db.Database.MigrateAsync();
    Console.WriteLine(pendientes.Count == 0
        ? "Migraciones: la base ya estaba al día."
        : $"Migraciones aplicadas: {string.Join(", ", pendientes)}.");
    return;
}

// "dotnet run -- --initialize-data": siembra los roles y crea el primer usuario Administrador,
// luego termina sin levantar el servidor web. Ver BootstrapService para el detalle.
if (args.Contains("--initialize-data"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RpsDbContext>();
    await BootstrapService.EjecutarAsync(db, builder.Configuration);
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// La más externa: convierte un DbUpdateConcurrencyException (RC 04 — alguien más guardó este
// mismo registro primero) en 409, en vez de un 500 sin explicación.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (DbUpdateConcurrencyException)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            title = "Conflicto",
            status = 409,
            detail = "El registro fue modificado por otra persona mientras se guardaba. Vuelve a cargarlo e intenta de nuevo.",
            codigo = "Concurrencia.Conflicto"
        });
    }
});

app.UseHttpsRedirection();
app.UseRateLimiter();

app.UseAuthentication();

// Un access token puede seguir siendo válido (no expiró) pero haber sido invalidado por /logout;
// se corta aquí, antes de que llegue a UseAuthorization/al endpoint.
app.Use(async (context, next) =>
{
    var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    if (jti is not null)
    {
        var db = context.RequestServices.GetRequiredService<RpsDbContext>();
        if (await db.TokenBlacklist.AnyAsync(b => b.Jti == jti))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
    }

    await next();
});

app.UseAuthorization();

// Después de autenticar/autorizar: solo las peticiones que ya pasaron esos filtros pueden
// quedar guardadas como respuesta "repetible" (RC 04 — doble clic / reintento de red).
app.UseMiddleware<IdempotenciaMiddleware>();

app.MapAuthEndpoints();
app.MapUsuariosEndpoints();
app.MapRolesEndpoints();
app.MapColaboradoresEndpoints();
app.MapContratosEndpoints();
app.MapProyectosEndpoints();
app.MapAsignacionesEndpoints();
app.MapHonorariosEndpoints();
app.MapCondicionesEconomicasEndpoints();
app.MapLiquidacionesEndpoints();
app.MapAnticiposEndpoints();
app.MapConfiguracionEndpoints();
app.MapDashboardEndpoints();
app.MapVencimientosContratosEndpoints();
app.MapExportacionesEndpoints();
app.MapExportacionPagoEndpoints();
app.MapFeriadosEndpoints();
app.MapIntegracionTmrEndpoints();
app.MapHistorialEndpoints();
app.MapAuditoriaEndpoints();

app.Run();
