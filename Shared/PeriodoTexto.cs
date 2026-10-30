using System.Globalization;

namespace rps_backend.Shared;

// Convierte el mes "yyyy-MM" que envía el frontend en el primer día del mes (DateOnly), y al revés.
// Los endpoints nuevos (exportaciones por corte, notificaciones) reciben el período así.
public static class PeriodoTexto
{
    public static bool TryParse(string? texto, out DateOnly primerDia)
    {
        primerDia = default;
        if (string.IsNullOrWhiteSpace(texto))
            return false;
        if (!DateOnly.TryParseExact(texto + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            return false;
        primerDia = fecha;
        return true;
    }

    public static string Formato(DateOnly fecha) => fecha.ToString("yyyy-MM", CultureInfo.InvariantCulture);
}
