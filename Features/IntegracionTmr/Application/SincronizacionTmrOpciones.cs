namespace rps_backend.Features.IntegracionTmr.Application;

// Sección Tmr:Sincronizacion de la configuración. Apagada por defecto: cada entorno decide si lee TMR solo.
public class SincronizacionTmrOpciones
{
    public bool Habilitada { get; set; }

    public int IntervaloMinutos { get; set; } = 30;
}
