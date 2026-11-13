namespace rps_backend.Persistence;

// Marca una entidad que AuditoriaInterceptor no registra en rps.auditoria, porque ella misma ya es
// un registro de quién hizo qué y cuándo (duplicarla solo agregaría volumen). Hoy: Exportacion.
public interface ISinAuditoria;
