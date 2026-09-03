using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die MAUI-Anwendung. Sie hat genau ein Fenster und darin genau eine Seite -
/// mehr braucht eine Huelle nicht, deren Oberflaeche in Blazor liegt.
///
/// <para><b>CreateWindow statt MainPage.</b> <c>Application.MainPage</c> ist seit
/// .NET 9 abgekuendigt; der gestuetzte Weg ist das Ueberschreiben von
/// <see cref="CreateWindow"/>. Fachlich ist es dasselbe: ein Fenster, eine
/// Seite.</para>
///
/// <para><b>Der Pruefmodus laeuft NEBEN der Oberflaeche</b> (iU10-6). Er wird
/// im Hintergrund angestossen, damit das Fenster trotzdem aufgeht: Der CI-Job
/// macht am Ende einen Bildschirmabzug, und ein weisses Bild waere kein
/// Nachweis. Gerechnet wird auf einem Hintergrundfaden - eine 8760-Stunden-
/// Simulation auf dem Zeichenfaden liesse die App von iOS als „reagiert nicht"
/// abschiessen.</para>
/// </summary>
public sealed class App : Application
{
    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? aktivierung)
    {
        if (Prueflauf.Angefordert) PruefmodusStarten();

        return new Window(new HauptSeite()) { Title = "EPOS-Plan" };
    }

    /// <summary>
    /// Stoesst den Pruefmodus im Hintergrund an. Eine Ausnahme darf den Start
    /// der Anwendung nicht mitreissen - sie steht dafuer im Protokoll, und die
    /// Fertigmarke bleibt aus; genau daran erkennt der CI-Job den Fehlschlag.
    /// </summary>
    private static void PruefmodusStarten()
    {
        _ = Task.Run(() =>
        {
            try
            {
                Prueflauf.Ausfuehren(Dienste.Pfade.Dokumente);
            }
            catch (Exception ex)
            {
                try { Console.WriteLine("Pruefmodus FEHLER: " + ex); } catch { }
            }
        });
    }
}
