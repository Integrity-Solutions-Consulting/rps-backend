namespace rps_backend.Features.Dashboard.Application;

public record ColaboradoresResumen(int Configurados, int Pendientes, int Activos, int Inactivos);

public record HonorariosResumen(int Cantidad, decimal NetoTotal);

public record LiquidacionesResumen(int Revisadas, int SinRevisar);

public record AnticiposResumen(int ConSaldoPendiente, decimal MontoPendiente);

// RN 13: contratos por vencer (fecha fin vigente, considerando adendas), a 30/15/7 días.
public record ContratosPorVencerResumen(int En30Dias, int En15Dias, int En7Dias);

public record DashboardResponse(
    DateOnly? Periodo,
    ColaboradoresResumen Colaboradores,
    HonorariosResumen Honorarios,
    LiquidacionesResumen Liquidaciones,
    AnticiposResumen Anticipos,
    ContratosPorVencerResumen ContratosPorVencer);
