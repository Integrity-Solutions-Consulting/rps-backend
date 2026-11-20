namespace rps_backend.Features.Auth.Application;

// Módulos de RPS que se pueden permitir por rol. Las claves viajan tal cual al frontend.
public static class PermisosModulo
{
    public const string RolAdministrador = "Administrador";
    public const string RolAdministradorContable = "Administrador Contable";
    public const string RolAsistenteContable = "Asistente Contable";

    // "configuracion": la pantalla Configuración entera (datos de la empresa, feriados, auditoría,
    // usuarios y roles). Sin él, la cuenta no la ve.
    public static readonly string[] Todos =
        ["colaboradores", "contratos", "anticipos", "prestamos", "liquidaciones", "exportaciones", "configuracion"];

    public static IReadOnlyList<string> PorDefecto(string rol) => rol switch
    {
        RolAdministrador or RolAdministradorContable => Todos,
        RolAsistenteContable => ["colaboradores", "liquidaciones"],
        _ => [],
    };

    // Deja solo claves conocidas, sin repetir, en el orden de Todos.
    public static List<string> Normalizar(IEnumerable<string>? permisos)
    {
        var pedidos = new HashSet<string>((permisos ?? []).Select(p => p.Trim().ToLowerInvariant()));
        return Todos.Where(pedidos.Contains).ToList();
    }

    public static bool SonValidos(IEnumerable<string>? permisos) =>
        (permisos ?? []).All(p => Todos.Contains(p.Trim().ToLowerInvariant()));
}
