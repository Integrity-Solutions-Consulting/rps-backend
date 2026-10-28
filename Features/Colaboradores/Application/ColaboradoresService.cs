using Microsoft.EntityFrameworkCore;
using rps_backend.Features.Colaboradores.Domain;
using rps_backend.Persistence;
using rps_backend.Shared;

namespace rps_backend.Features.Colaboradores.Application;

// Reglas de negocio de Colaboradores. No conoce HTTP: devuelve Resultado y la capa Api decide el status code.
public class ColaboradoresService(RpsDbContext db, CuentaBancariaProtector protector)
{
    public async Task<List<ColaboradorResumenResponse>> ListarAsync(
        EstadoConfiguracion? estado, bool? activo, CancellationToken ct)
    {
        var query = db.ColaboradoresRps.AsNoTracking();

        if (estado is not null)
            query = query.Where(c => c.EstadoConfiguracion == estado);

        if (activo is not null)
            query = query.Where(c => c.Activo == activo);

        return await query
            .OrderBy(c => c.CreadoEn)
            .Select(c => new ColaboradorResumenResponse(
                c.Id, c.TmrColaboradorId, c.Clasificacion, c.EstadoConfiguracion, c.Activo, c.CreadoEn, c.ActualizadoEn))
            .ToListAsync(ct);
    }

    public async Task<Resultado<ColaboradorDetalleResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var colaborador = await db.ColaboradoresRps.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (colaborador is null)
            return NoEncontrado(id);

        return await ConstruirDetalleAsync(colaborador, ct);
    }

    public async Task<Resultado<ColaboradorDetalleResponse>> CrearAsync(CrearColaboradorRequest request, CancellationToken ct)
    {
        var tmrColaboradorId = request.TmrColaboradorId?.Trim();
        if (string.IsNullOrEmpty(tmrColaboradorId) || tmrColaboradorId.Length > 50)
            return Error.Validacion("tmrColaboradorId", "Es obligatorio y debe tener máximo 50 caracteres.");

        if (await db.ColaboradoresRps.AnyAsync(c => c.TmrColaboradorId == tmrColaboradorId, ct))
            return Error.Conflicto($"Ya existe un colaborador con TmrColaboradorId '{tmrColaboradorId}'.");

        // Un colaborador nuevo siempre empieza Pendiente: aún no tiene frecuencia ni cuenta.
        var colaborador = new ColaboradorRps
        {
            TmrColaboradorId = tmrColaboradorId,
            Clasificacion = request.Clasificacion
        };

        db.ColaboradoresRps.Add(colaborador);
        await db.SaveChangesAsync(ct);

        return await ConstruirDetalleAsync(colaborador, ct);
    }

    public async Task<Resultado<ColaboradorDetalleResponse>> ActualizarClasificacionAsync(
        Guid id, ActualizarClasificacionRequest request, CancellationToken ct)
    {
        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (colaborador is null)
            return NoEncontrado(id);

        if (!colaborador.Activo)
            return Desactivado();

        colaborador.Clasificacion = request.Clasificacion;
        await ColaboradorEstado.RecalcularAsync(colaborador, db, ct);
        await db.SaveChangesAsync(ct);

        return await ConstruirDetalleAsync(colaborador, ct);
    }

    // Desactivar no borra nada: el colaborador y su historial se conservan y se puede volver a activar.
    public async Task<Resultado<ColaboradorDetalleResponse>> CambiarActivoAsync(Guid id, bool activo, CancellationToken ct)
    {
        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (colaborador is null)
            return NoEncontrado(id);

        if (colaborador.Activo != activo)
        {
            colaborador.Activo = activo;
            colaborador.ActualizadoEn = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return await ConstruirDetalleAsync(colaborador, ct);
    }

    public async Task<Resultado<List<FrecuenciaResponse>>> ListarFrecuenciasAsync(Guid id, CancellationToken ct)
    {
        if (!await db.ColaboradoresRps.AnyAsync(c => c.Id == id, ct))
            return NoEncontrado(id);

        return await db.ColaboradoresFrecuencia.AsNoTracking()
            .Where(f => f.ColaboradorId == id)
            .OrderByDescending(f => f.VigenteDesdeMes)
            .Select(f => new FrecuenciaResponse(f.Id, f.Frecuencia, f.VigenteDesdeMes))
            .ToListAsync(ct);
    }

    public async Task<Resultado<FrecuenciaResponse>> RegistrarFrecuenciaAsync(
        Guid id, RegistrarFrecuenciaRequest request, CancellationToken ct)
    {
        if (request.Frecuencia is null)
            return Error.Validacion("frecuencia", "Es obligatoria (DosPagos o FinDeMes).");

        if (request.VigenteDesdeMes is not { Day: 1 } vigenteDesdeMes)
            return Error.Validacion("vigenteDesdeMes", "Es obligatoria y debe ser el primer día del mes (ej. 2026-11-01).");

        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (colaborador is null)
            return NoEncontrado(id);

        if (!colaborador.Activo)
            return Desactivado();

        if (await db.ColaboradoresFrecuencia.AnyAsync(f => f.ColaboradorId == id && f.VigenteDesdeMes == vigenteDesdeMes, ct))
            return Error.Conflicto($"El colaborador ya tiene una frecuencia registrada desde {vigenteDesdeMes:yyyy-MM-dd}.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var frecuencia = new ColaboradorFrecuencia
        {
            ColaboradorId = id,
            Frecuencia = request.Frecuencia.Value,
            VigenteDesdeMes = vigenteDesdeMes
        };
        db.ColaboradoresFrecuencia.Add(frecuencia);
        await db.SaveChangesAsync(ct);

        await ColaboradorEstado.RecalcularAsync(colaborador, db, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return new FrecuenciaResponse(frecuencia.Id, frecuencia.Frecuencia, frecuencia.VigenteDesdeMes);
    }

    // Reemplaza la cuenta activa: la anterior queda con Activa = false como historial.
    public async Task<Resultado<CuentaBancariaResponse>> RegistrarCuentaBancariaAsync(
        Guid id, RegistrarCuentaBancariaRequest request, CancellationToken ct)
    {
        var numeroCuenta = request.NumeroCuenta?.Trim();
        if (string.IsNullOrEmpty(numeroCuenta) || numeroCuenta.Length > 30 || !numeroCuenta.All(char.IsAsciiDigit))
            return Error.Validacion("numeroCuenta", "Es obligatorio, solo dígitos y máximo 30 caracteres.");

        var banco = request.Banco?.Trim();
        if (banco?.Length > 100)
            return Error.Validacion("banco", "Debe tener máximo 100 caracteres.");

        var tipoCuenta = request.TipoCuenta?.Trim();
        if (tipoCuenta?.Length > 20)
            return Error.Validacion("tipoCuenta", "Debe tener máximo 20 caracteres.");

        var colaborador = await db.ColaboradoresRps.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (colaborador is null)
            return NoEncontrado(id);

        if (!colaborador.Activo)
            return Desactivado();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Se guarda primero la desactivación para no chocar con el índice único de cuenta activa.
        var cuentaAnterior = await db.CuentasBancarias.FirstOrDefaultAsync(c => c.ColaboradorId == id && c.Activa, ct);
        if (cuentaAnterior is not null)
        {
            cuentaAnterior.Activa = false;
            await db.SaveChangesAsync(ct);
        }

        var cuenta = new CuentaBancaria
        {
            ColaboradorId = id,
            Banco = string.IsNullOrEmpty(banco) ? null : banco,
            TipoCuenta = string.IsNullOrEmpty(tipoCuenta) ? null : tipoCuenta,
            NumeroCuentaCifrado = protector.Cifrar(numeroCuenta)
        };
        db.CuentasBancarias.Add(cuenta);
        await db.SaveChangesAsync(ct);

        await ColaboradorEstado.RecalcularAsync(colaborador, db, ct);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return ACuentaResponse(cuenta);
    }

    private async Task<ColaboradorDetalleResponse> ConstruirDetalleAsync(ColaboradorRps colaborador, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var mesActual = new DateOnly(hoy.Year, hoy.Month, 1);

        // Vigente = la más reciente que ya empezó (las de meses futuros no cuentan todavía).
        var frecuenciaVigente = await db.ColaboradoresFrecuencia.AsNoTracking()
            .Where(f => f.ColaboradorId == colaborador.Id && f.VigenteDesdeMes <= mesActual)
            .OrderByDescending(f => f.VigenteDesdeMes)
            .Select(f => new FrecuenciaResponse(f.Id, f.Frecuencia, f.VigenteDesdeMes))
            .FirstOrDefaultAsync(ct);

        var cuenta = await db.CuentasBancarias.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ColaboradorId == colaborador.Id && c.Activa, ct);

        return new ColaboradorDetalleResponse(
            colaborador.Id,
            colaborador.TmrColaboradorId,
            colaborador.Clasificacion,
            colaborador.EstadoConfiguracion,
            colaborador.Activo,
            colaborador.CreadoEn,
            colaborador.ActualizadoEn,
            frecuenciaVigente,
            cuenta is null ? null : ACuentaResponse(cuenta));
    }

    private CuentaBancariaResponse ACuentaResponse(CuentaBancaria cuenta) =>
        new(cuenta.Id, cuenta.Banco, cuenta.TipoCuenta, protector.Enmascarar(cuenta.NumeroCuentaCifrado));

    private static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado($"No existe un colaborador con id '{id}'.");

    private static Error Desactivado() =>
        Error.Conflicto("El colaborador está desactivado. Actívalo antes de modificar su configuración.");
}
