using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace rps_backend.Persistence.Interno;

// ════════════════════════════════════════════════════════════════════════════════════════════
// [RESPALDO DE LLAVES · TEMPORAL]  Para quitarlo: docs/RESPALDO-LLAVES-CIFRADO.md (carpeta RPS).
//
// QUÉ HACE
//   Las llaves de ASP.NET Data Protection (con las que CuentaBancariaProtector cifra los números
//   de cuenta) viven en la tabla rps.data_protection_keys. Si en una migración de datos en Dokploy
//   esa tabla no se copia, se vacía o la base se recrea, los números de cuenta quedan ilegibles.
//
//   Este archivo pone un "espejo" delante del repositorio de llaves de la base:
//     1. Cada llave que se guarda en la base se copia también como archivo .xml en una carpeta
//        del servidor (un volumen persistente de Dokploy, fuera de la base de datos).
//     2. Al arrancar (y cada vez que Data Protection lee el llavero), compara base y carpeta:
//          - llave que está en la base y no en la carpeta  -> la copia a la carpeta;
//          - llave que está en la carpeta y no en la base  -> la vuelve a insertar en la base.
//        Así, si la base llega sin llaves tras una migración, se restauran solas y las cuentas
//        cifradas se siguen leyendo.
//
// CÓMO SE ACTIVA
//   Solo si existe la variable de entorno RPS_ESTADO_INTERNO_DIR con la ruta de la carpeta.
//   Sin esa variable no hace nada y el backend funciona exactamente igual que antes.
//   En Dokploy: montar un volumen (p. ej. /app/estado-interno) y poner
//   RPS_ESTADO_INTERNO_DIR=/app/estado-interno en las variables del servicio del backend.
//
// SEGURIDAD
//   Los archivos de la carpeta contienen las llaves SIN cifrar: quien los tenga puede descifrar
//   las cuentas de la base. Por eso:
//     - la carpeta NUNCA debe estar dentro del repositorio ni subirse a git;
//     - debe ser un volumen del servidor con acceso restringido (los archivos se crean con
//       permisos 600 en Linux);
//     - si se copia a otro servidor, hacerlo por un canal privado y borrar la copia temporal.
//   Que el nombre del archivo no sea obvio NO es una protección: lo que protege es que la
//   carpeta esté fuera del código y con acceso restringido.
// ════════════════════════════════════════════════════════════════════════════════════════════
internal static class EstadoInternoExtensions
{
    // Variable de entorno con la carpeta del espejo. Vacía o ausente = desactivado.
    public const string VariableCarpeta = "RPS_ESTADO_INTERNO_DIR";

    public static IServiceCollection AddEstadoInterno(this IServiceCollection services, IConfiguration configuration)
    {
        var carpeta = configuration[VariableCarpeta];
        if (string.IsNullOrWhiteSpace(carpeta))
            return services;

        // PostConfigure corre después de PersistKeysToDbContext, así que aquí el XmlRepository ya es
        // el de la base y solo lo envolvemos; no se cambia cómo se cifra ni dónde se guardan las llaves.
        services.AddOptions<KeyManagementOptions>()
            .PostConfigure<ILoggerFactory>((opciones, loggerFactory) =>
            {
                if (opciones.XmlRepository is null || opciones.XmlRepository is EspejoEstadoInterno)
                    return;

                opciones.XmlRepository = new EspejoEstadoInterno(
                    opciones.XmlRepository,
                    carpeta,
                    loggerFactory.CreateLogger<EspejoEstadoInterno>());
            });

        return services;
    }
}

// Envuelve el repositorio de llaves de la base (EntityFrameworkCoreXmlRepository) y mantiene una
// copia en archivos. La base sigue siendo la fuente principal; la carpeta solo respalda y restaura.
internal sealed class EspejoEstadoInterno(IXmlRepository principal, string carpeta, ILogger<EspejoEstadoInterno> log)
    : IXmlRepository
{
    private readonly DirectoryInfo _carpeta = new(carpeta);
    private readonly Lock _candado = new();

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        lock (_candado)
        {
            var enBase = principal.GetAllElements().ToList();

            List<(string Nombre, XElement Elemento)> enEspejo;
            try
            {
                enEspejo = LeerEspejo();
            }
            catch (Exception ex)
            {
                // Si la carpeta falla, el backend sigue con lo que hay en la base.
                log.LogWarning(ex, "No se pudo leer la carpeta de respaldo de llaves {Carpeta}.", _carpeta.FullName);
                return enBase;
            }

            // 1) Llaves que están en la carpeta pero faltan en la base: se restauran en la base.
            var firmasBase = enBase.Select(Firma).ToHashSet();
            var restauradas = new List<XElement>();
            foreach (var (nombre, elemento) in enEspejo)
            {
                if (firmasBase.Contains(Firma(elemento)))
                    continue;

                try
                {
                    principal.StoreElement(elemento, nombre);
                }
                catch (Exception ex)
                {
                    // Aunque no se pueda insertar, se devuelve igual para que el descifrado funcione.
                    log.LogWarning(ex, "No se pudo restaurar en la base la llave {Nombre}.", nombre);
                }

                restauradas.Add(elemento);
                firmasBase.Add(Firma(elemento));
            }

            if (restauradas.Count > 0)
                log.LogWarning("Se restauraron {Cantidad} llave(s) de cifrado desde la carpeta de respaldo.", restauradas.Count);

            // 2) Llaves que están en la base pero faltan en la carpeta: se copian a la carpeta.
            var firmasEspejo = enEspejo.Select(e => Firma(e.Elemento)).ToHashSet();
            foreach (var elemento in enBase)
            {
                if (!firmasEspejo.Contains(Firma(elemento)))
                    EscribirEnEspejo(elemento, NombrePara(elemento));
            }

            return enBase.Concat(restauradas).ToList();
        }
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        // Primero la base (comportamiento original); después la copia.
        principal.StoreElement(element, friendlyName);

        lock (_candado)
            EscribirEnEspejo(element, string.IsNullOrWhiteSpace(friendlyName) ? NombrePara(element) : friendlyName);
    }

    private List<(string Nombre, XElement Elemento)> LeerEspejo()
    {
        if (!_carpeta.Exists)
            return [];

        var resultado = new List<(string, XElement)>();
        foreach (var archivo in _carpeta.EnumerateFiles("*.xml"))
        {
            try
            {
                using var stream = archivo.OpenRead();
                resultado.Add((Path.GetFileNameWithoutExtension(archivo.Name), XElement.Load(stream)));
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Archivo de respaldo de llave ilegible: {Archivo}.", archivo.Name);
            }
        }

        return resultado;
    }

    private void EscribirEnEspejo(XElement elemento, string nombre)
    {
        try
        {
            _carpeta.Create();

            var destino = Path.Combine(_carpeta.FullName, NombreSeguro(nombre) + ".xml");
            if (File.Exists(destino))
                return;

            // Escritura atómica: archivo temporal y luego renombrar.
            var temporal = destino + ".tmp";
            File.WriteAllText(temporal, elemento.ToString(SaveOptions.DisableFormatting), Encoding.UTF8);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(temporal, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.Move(temporal, destino, overwrite: false);
        }
        catch (Exception ex)
        {
            // Nunca tumbar el backend por el respaldo: la llave ya quedó en la base.
            log.LogWarning(ex, "No se pudo copiar la llave {Nombre} a la carpeta de respaldo.", nombre);
        }
    }

    // Identifica una llave por su id (<key id="...">); otros elementos (revocaciones) por su contenido.
    private static string Firma(XElement elemento) =>
        elemento.Name.LocalName == "key" && elemento.Attribute("id")?.Value is { Length: > 0 } id
            ? "key:" + id.ToLowerInvariant()
            : "xml:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(elemento.ToString(SaveOptions.DisableFormatting))));

    private static string NombrePara(XElement elemento) =>
        elemento.Name.LocalName == "key" && elemento.Attribute("id")?.Value is { Length: > 0 } id
            ? "key-" + id
            : "elemento-" + Firma(elemento)[4..20];

    private static string NombreSeguro(string nombre)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        return new string(nombre.Select(c => invalidos.Contains(c) ? '_' : c).ToArray());
    }
}
