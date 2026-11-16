using rps_backend.Features.Honorarios.Domain;

namespace rps_backend.Features.Honorarios.Application;

// Qué honorarios rigen en una fecha. Cada contrato se paga de UNA de dos formas:
//  - por contrato: un solo importe fijo (CondicionEconomica sin proyecto). Los proyectos no
//    importan: se paga mientras el contrato esté vigente, tenga 0, 1 o muchos proyectos;
//  - por proyecto: un importe por cada proyecto, que se paga solo los días de la asignación.
// Si un contrato cambió de forma de pago, rige la que se registró con la fecha más reciente.
public static class CondicionesVigentes
{
    public static List<CondicionEconomica> Seleccionar(IEnumerable<CondicionEconomica> condiciones, DateOnly fecha)
    {
        var resultado = new List<CondicionEconomica>();

        foreach (var delContrato in condiciones.Where(c => c.FechaInicioVigencia <= fecha).GroupBy(c => (c.ColaboradorId, c.ContratoId)))
        {
            var porContrato = delContrato.Where(c => c.PorContrato).MaxBy(c => c.FechaInicioVigencia);
            var porProyecto = delContrato
                .Where(c => !c.PorContrato && (porContrato is null || c.FechaInicioVigencia > porContrato.FechaInicioVigencia))
                .GroupBy(c => c.ProyectoId)
                .Select(g => g.MaxBy(c => c.FechaInicioVigencia)!)
                .ToList();

            if (porProyecto.Count > 0)
                resultado.AddRange(porProyecto);
            else if (porContrato is not null)
                resultado.Add(porContrato);
        }

        return resultado;
    }

    // Los días del período que rige cada condición. Si el monto (o la forma de pago) cambia a mitad
    // de mes, ese mes queda en tramos: cada condición se lleva solo sus días.
    public static Dictionary<CondicionEconomica, HashSet<DateOnly>> TramosDelPeriodo(
        IEnumerable<CondicionEconomica> condiciones, DateOnly periodo, DateOnly finPeriodo)
    {
        var lista = condiciones.ToList();
        var tramos = new Dictionary<CondicionEconomica, HashSet<DateOnly>>();

        for (var dia = periodo; dia <= finPeriodo; dia = dia.AddDays(1))
            foreach (var condicion in Seleccionar(lista, dia))
            {
                if (!tramos.TryGetValue(condicion, out var dias))
                    tramos[condicion] = dias = [];
                dias.Add(dia);
            }

        return tramos;
    }
}
