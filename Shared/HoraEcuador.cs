namespace rps_backend.Shared;

// Fechas de negocio en hora de Ecuador (UTC-5 fijo: Ecuador no usa horario de verano).
// Con DateTime.UtcNow, después de las 19:00 ya sería el día siguiente; con DateTime.Now, depende
// de la zona horaria del servidor. Las marcas de tiempo que se guardan (CreadoEn, tokens,
// auditoría) siguen en UTC: son instantes, no fechas del calendario.
public static class HoraEcuador
{
    private static readonly TimeSpan Desfase = TimeSpan.FromHours(-5);

    public static DateTime Ahora => DateTime.SpecifyKind(DateTime.UtcNow + Desfase, DateTimeKind.Unspecified);

    public static DateOnly Hoy => DateOnly.FromDateTime(Ahora);

    // Día 1 del mes actual.
    public static DateOnly MesActual => new(Hoy.Year, Hoy.Month, 1);

    // Un instante guardado en UTC, mostrado en hora de Ecuador (para textos de PDF/Excel).
    public static DateTime DesdeUtc(DateTime utc) =>
        DateTime.SpecifyKind((utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : utc) + Desfase, DateTimeKind.Unspecified);
}
