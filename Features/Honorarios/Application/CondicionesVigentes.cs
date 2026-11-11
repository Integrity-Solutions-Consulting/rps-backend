using rps_backend.Features.Honorarios.Domain;

namespace rps_backend.Features.Honorarios.Application;

// Qué honorarios rigen en un mes. Cada contrato se paga de UNA de dos formas:
//  - por contrato: un solo importe fijo (CondicionEconomica sin proyecto). Los proyectos no
//    importan: se paga mientras el contrato esté vigente, tenga 0, 1 o muchos proyectos;
//  - por proyecto: un importe por cada proyecto, que se paga solo los días de la asignación.
// Si un contrato cambió de forma de pago, rige la que se registró para el mes más reciente.
public static class CondicionesVigentes
{
    public static List<CondicionEconomica> Seleccionar(IEnumerable<CondicionEconomica> condiciones, DateOnly mes)
    {
        var resultado = new List<CondicionEconomica>();

        foreach (var delContrato in condiciones.Where(c => c.MesInicioVigencia <= mes).GroupBy(c => (c.ColaboradorId, c.ContratoId)))
        {
            var porContrato = delContrato.Where(c => c.PorContrato).MaxBy(c => c.MesInicioVigencia);
            var porProyecto = delContrato
                .Where(c => !c.PorContrato && (porContrato is null || c.MesInicioVigencia > porContrato.MesInicioVigencia))
                .GroupBy(c => c.ProyectoId)
                .Select(g => g.MaxBy(c => c.MesInicioVigencia)!)
                .ToList();

            if (porProyecto.Count > 0)
                resultado.AddRange(porProyecto);
            else if (porContrato is not null)
                resultado.Add(porContrato);
        }

        return resultado;
    }
}
