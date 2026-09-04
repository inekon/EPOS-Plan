using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="IProjektKontext"/>: das gerade geoeffnete
/// Projekt — seit dem <b>Anwenderentscheid W16b-O-3 vom 04.09.2026</b> nur noch
/// eine duenne WEITERLEITUNG auf
/// <see cref="ProjektKontextCtrl"/> im Kern.
///
/// <para><b>Warum sie ueberhaupt noch da ist.</b> Bis hierher fuehrte diese Klasse
/// eine EIGENE Umsetzung: Projekt lesen, Klimaregion nachschlagen,
/// <c>Tab_Applikation</c> fortschreiben. Damit gab es zwei Fassungen derselben
/// Zusage — und sie liefen in EINEM Punkt auseinander (Befund W16b-B2): Die
/// Klimazone las Windows aus der PROJEKTKOPIE, iOS aus dem STAMM. Der Entscheid
/// nimmt die iOS-Lesart, aber nur EINMAL: <c>ProjektKontextCtrl</c> liest jetzt
/// den Stammnamen (mit Rueckfall auf die Projektkopie), und diese Klasse ruft
/// ihn. <b>Dieselbe Klasse, dieselbe Antwort</b> — unter Windows legt
/// <c>Program.Main</c> denselben Typ in <c>Dienste.Projekt</c>, hier tut es
/// <c>MauiProgram.DiensteBelegen</c>.</para>
///
/// <para><b>Was NICHT weitergereicht, sondern hier bleibt: die zwei
/// Schutznetze.</b> Der Kern laesst eine Ausnahme aus dem Datenzugriff nach
/// aussen durch; unter Windows landet sie in der Fehlerbehandlung der Anwendung.
/// Auf iOS liegt die Datenbank in der Sandbox und wird beim Erststart erst
/// kopiert — eine durchgereichte Ausnahme aus einem Projektwechsel nimmt die App
/// mit. Deshalb faengt <see cref="Uebernehmen"/> hier wie bisher ab und
/// antwortet <c>false</c>, statt zu werfen. Alles Uebrige — die Reihenfolge
/// „Name zuerst, Id als Rueckfall", das Fortschreiben von <c>Tab_Applikation</c>
/// und das Ereignis <see cref="Gewechselt"/> — macht der Kern.</para>
///
/// <para><b><see cref="Vorhanden"/> ist <c>true</c>, sobald die Anwendung
/// laeuft.</b> Das ist die Aussage der Schnittstelle: „Es gibt einen fuehrenden
/// Kontext" - auch dann, wenn gerade kein Projekt offen ist. Erst ohne
/// Oberflaeche (Referenzlauf, Konsolenwerkzeug) duerfen Aufrufer ersatzweise
/// <c>Tab_Applikation</c> lesen. Genau diese Fallgabelung trifft
/// <c>KiAktionenProjekt.AktivesProjektErmitteln</c>. Der Kern antwortet
/// gleichlautend; die Weiterleitung aendert daran nichts.</para>
///
/// <para>Diese Datei kennt keine iOS-API und laesst sich ohne Mac pruefen.</para>
/// </summary>
public sealed class IosProjektKontext : IProjektKontext
{
    private readonly ProjektKontextCtrl _kern = new ProjektKontextCtrl();

    public IosProjektKontext()
    {
        // Der Kern meldet den Wechsel, diese Huelle reicht ihn an ihre eigenen
        // Empfaenger weiter - sie haengen an DIESER Instanz, nicht am Kern.
        _kern.Gewechselt += () =>
        {
            Action? h = Gewechselt;
            h?.Invoke();
        };
    }

    /// <inheritdoc/>
    public bool Vorhanden => _kern.Vorhanden;

    /// <inheritdoc/>
    public int Id => _kern.Id;

    /// <inheritdoc/>
    public string Name => _kern.Name;

    /// <inheritdoc/>
    public string Klimazone => _kern.Klimazone;

    /// <inheritdoc/>
    public bool Uebernehmen(int id, string name)
    {
        try
        {
            return _kern.Uebernehmen(id, name);
        }
        catch
        {
            // Auf iOS darf ein misslungener Projektwechsel die App nicht mitnehmen;
            // der Aufrufer erkennt am false, dass er keine Erfolgsmeldung zeigt.
            return false;
        }
    }

    /// <inheritdoc/>
    public event Action? Gewechselt;
}
