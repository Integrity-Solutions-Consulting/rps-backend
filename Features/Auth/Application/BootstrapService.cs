using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Auth.Domain;
using rps_backend.Persistence;

namespace rps_backend.Features.Auth.Application;

// Se ejecuta una sola vez, fuera del pipeline web normal (ver "--initialize-data" en Program.cs),
// contra una base de datos recién creada: siembra los roles y crea el primer usuario Administrador
// a partir de variables de entorno. Nunca pisa datos existentes.
public static class BootstrapService
{
    public static readonly string[] RolesIniciales = ["Administrador", "Administrador Contable", "Asistente Contable"];

    public static async Task EjecutarAsync(RpsDbContext db, IConfiguration configuracion)
    {
        if (await db.Usuarios.AnyAsync())
        {
            Console.WriteLine("Bootstrap: ya existe al menos un usuario; no se hace nada.");
            return;
        }

        foreach (var nombreRol in RolesIniciales)
        {
            if (!await db.Roles.AnyAsync(r => r.Nombre == nombreRol))
                db.Roles.Add(new Rol { Nombre = nombreRol });
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"Bootstrap: roles sembrados ({string.Join(", ", RolesIniciales)}).");

        var email = configuracion["Bootstrap:AdminEmail"];
        var password = configuracion["Bootstrap:AdminPassword"];
        var nombre = configuracion["Bootstrap:AdminNombre"];

        var algunaDefinida = !string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(password) || !string.IsNullOrWhiteSpace(nombre);
        var todasDefinidas = !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(nombre);

        if (!algunaDefinida)
        {
            Console.WriteLine("Bootstrap: no se definieron Bootstrap__AdminEmail/AdminPassword/AdminNombre; no se crea ningún usuario todavía.");
            return;
        }

        if (!todasDefinidas)
            throw new InvalidOperationException("Bootstrap__AdminEmail, Bootstrap__AdminPassword y Bootstrap__AdminNombre deben definirse las tres juntas.");

        if (password!.Length < 12)
            throw new InvalidOperationException("Bootstrap__AdminPassword debe tener al menos 12 caracteres.");

        var rolAdministrador = await db.Roles.FirstAsync(r => r.Nombre == "Administrador");

        var usuario = new Usuario
        {
            Email = email!.Trim().ToLowerInvariant(),
            Nombre = nombre!.Trim(),
            HashPassword = BCrypt.Net.BCrypt.HashPassword(password),
        };
        db.Usuarios.Add(usuario);
        db.UsuariosRoles.Add(new UsuarioRol { UsuarioId = usuario.Id, RolId = rolAdministrador.Id });

        await db.SaveChangesAsync();
        Console.WriteLine($"Bootstrap: usuario Administrador creado ({usuario.Email}).");
    }
}
