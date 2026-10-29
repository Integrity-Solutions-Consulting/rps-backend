namespace rps_backend.Features.Dashboard.Application;

public record ColaboradoresResumen(int Configurados, int Pendientes, int Activos, int Inactivos);

public record HonorariosResumen(int Pendientes, int Aprobados, int Pagados, int Anulados, decimal MontoNetoTotal);

public record LiquidacionesResumen(int Generadas, int Pagadas);

public record AnticiposResumen(int AprobadosPendientes, decimal MontoPendiente);

public record DashboardResponse(
    DateOnly? Periodo,
    ColaboradoresResumen Colaboradores,
    HonorariosResumen Honorarios,
    LiquidacionesResumen Liquidaciones,
    AnticiposResumen Anticipos);
