namespace rps_backend.Features.Auth.Application;

// Módulos de RPS que se pueden permitir por rol. Las claves viajan tal cual al frontend.
public static class PermisosModulo
{
    public const string RolAdministrador = "Administrador";
    public const string RolAdministradorContable = "Administrador Contable";
    public const string RolAsistenteContable = "Asistente Contable";

    public static readonly string[] Todos =
        ["colaboradores", "contratos", "anticipos", "liquidaciones", "documentos", "exportaciones"];

    public static IReadOnlyList<string> PorDefecto(string rol) => rol switch
    {
        RolAdministrador or RolAdministradorContable => Todos,
        RolAsistenteContable => ["colaboradores", "liquidaciones", "documentos"],
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
