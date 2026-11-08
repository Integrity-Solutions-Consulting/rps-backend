using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using rps_backend.Features.Auditoria.Domain;

namespace rps_backend.Persistence;

// Registra en rps.auditoria qué cambió, quién y cuándo, en cada SaveChanges de cualquier entidad
// (RN 48, RC 03). No conoce ningún módulo de negocio: recorre el ChangeTracker genéricamente, así
// que cubre automáticamente entidades nuevas sin tocar este archivo.
public class AuditoriaInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    // El cambio sí se registra; el valor en texto plano, no (son datos sensibles, no tributarios).
    private static readonly HashSet<string> CamposSensibles = new(StringComparer.OrdinalIgnoreCase)
    {
        "HashPassword", "NumeroCuentaCifrado"
    };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            RegistrarCambios(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            RegistrarCambios(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void RegistrarCambios(DbContext context)
    {
        var usuarioId = ObtenerUsuarioId();
        var ahora = DateTime.UtcNow;
        var nuevas = new List<Auditoria>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // La auditoría no se audita a sí misma, y las entidades sin cambios reales no generan ruido.
            if (entry.Entity is Auditoria) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            var entidad = entry.Metadata.ClrType.Name;
            var entidadId = ClavePrimaria(entry);

            switch (entry.State)
            {
                case EntityState.Added:
                    nuevas.Add(Nueva(entidad, entidadId, null, null, "(creado)", usuarioId, ahora));
                    break;

                case EntityState.Deleted:
                    nuevas.Add(Nueva(entidad, entidadId, null, "(eliminado)", null, usuarioId, ahora));
                    break;

                case EntityState.Modified:
                    foreach (var prop in entry.Properties)
                    {
                        if (!prop.IsModified) continue;

                        var campo = prop.Metadata.Name;
                        var anterior = Formatear(campo, prop.OriginalValue);
                        var nuevo = Formatear(campo, prop.CurrentValue);
                        if (anterior == nuevo) continue;

                        nuevas.Add(Nueva(entidad, entidadId, campo, anterior, nuevo, usuarioId, ahora));
                    }
                    break;
            }
        }

        if (nuevas.Count > 0)
            context.Set<Auditoria>().AddRange(nuevas);
    }

    private static Auditoria Nueva(
        string entidad, string entidadId, string? campo, string? anterior, string? nuevo, Guid? usuarioId, DateTime fecha) => new()
    {
        Entidad = entidad, EntidadId = entidadId, Campo = campo,
        ValorAnterior = anterior, ValorNuevo = nuevo, UsuarioId = usuarioId, Fecha = fecha
    };

    private static string? Formatear(string campo, object? valor)
    {
        if (valor is null) return null;
        if (CamposSensibles.Contains(campo)) return "(valor sensible)";

        return valor switch
        {
            DateTime dt => dt.ToString("O"),
            DateOnly d => d.ToString("yyyy-MM-dd"),
            decimal m => m.ToString("0.00"),
            byte[] => "(binario)",
            _ => valor.ToString()
        };
    }

    private static string ClavePrimaria(EntityEntry entry)
    {
        var claves = entry.Metadata.FindPrimaryKey()?.Properties;
        if (claves is null || claves.Count == 0) return string.Empty;

        return string.Join(",", claves.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? ""));
    }

    private Guid? ObtenerUsuarioId()
    {
        var sub = httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return sub is not null && Guid.TryParse(sub, out var id) ? id : null;
    }
}
