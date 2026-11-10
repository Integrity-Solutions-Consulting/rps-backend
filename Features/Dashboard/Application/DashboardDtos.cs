namespace rps_backend.Features.Dashboard.Application;

public record ColaboradoresResumen(int Configurados, int Pendientes, int Activos, int Inactivos);

public record HonorariosResumen(int Cantidad, decimal NetoTotal);

public record LiquidacionesResumen(int Revisadas, int SinRevisar);

public record AnticiposResumen(int ConSaldoPendiente, decimal MontoPendiente);

// RN 13: contratos por vencer (fecha fin vigente, considerando adendas) en cada plazo de aviso.
// Los plazos son configurables (ConfiguracionGlobal.DiasAvisoVencimiento1..3; por defecto 30/15/7)
// y viajan junto a los conteos para que Inicio muestre los que están vigentes.
public record ContratosPorVencerResumen(
    int DiasAviso1, int DiasAviso2, int DiasAviso3,
    int EnAviso1, int EnAviso2, int EnAviso3);

public record DashboardResponse(
    DateOnly? Periodo,
    ColaboradoresResumen Colaboradores,
    HonorariosResumen Honorarios,
    LiquidacionesResumen Liquidaciones,
    AnticiposResumen Anticipos,
    ContratosPorVencerResumen ContratosPorVencer);
