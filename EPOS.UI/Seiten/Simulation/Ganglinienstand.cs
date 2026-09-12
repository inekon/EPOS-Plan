namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// <b>Die Schalterstellung EINES Ganglinienreiters über die Sitzung</b>
/// (Anwenderrückmeldung 12.09.2026, Auftrag #234: „Die Grafik ist zu Beginn leer …
/// es sollte der Bedarf mit select als default ausgewählt werden — oder die Auswahl
/// des Benutzers gespeichert werden").
///
/// <para><b>Warum es das braucht.</b> <c>Bausteine/Reiterblatt.razor</c> zeichnet
/// seinen Inhalt <c>@if (Sichtbar)</c>: Ein nicht gewähltes Blatt gibt GAR NICHTS
/// aus, und beim Wechsel Autarkie → Wärme → Strom → Wärme entsteht der Reiter
/// vollständig NEU. Ohne Gedächtnis fällt damit jede Anwenderwahl auf die Vorgabe
/// zurück — wer sich seine vier Erzeuger zusammengestellt hat, findet nach einem
/// Blick auf das Nachbarblatt wieder den Ausgangszustand vor.</para>
///
/// <para><b>Die Reihen stehen als SCHLÜSSEL, nicht als Index.</b> Welche Reihen ein
/// Reiter führt, entscheidet der LAUF (<c>Ganglinienreihe.Vorhanden</c>): Ein Projekt
/// ohne Solarthermie zeigt sie gar nicht. Ein gemerkter Index 3 zeigte nach dem
/// nächsten Lauf auf eine andere Reihe. Ein Schlüssel, den der neue Bestand nicht
/// kennt, fällt STILL weg; eine neu hinzugekommene Reihe ist beim nächsten Aufbau
/// NICHT von selbst an — nur ein Stand, der noch gar nichts gemerkt hat
/// (<see cref="Belegt"/> = <c>false</c>), bekommt die Vorbelegung des Reiters.</para>
///
/// <para><b>Sitzung, nicht dauerhaft</b> — dieselbe Bauart und dieselbe Begründung
/// wie beim Filterstand je Katalog (Stufe S2.5,
/// <c>EPOS.Kern/Allgemein/Katalog/Katalogfilterregister.cs</c>): Es wird NICHTS
/// geschrieben, kein <c>Dienste.Einstellungen</c>, keine Datei, keine Datenbank. Der
/// Stand lebt so lange wie der Prozess. Der Anwender hat beide Wege angeboten
/// („oder die Auswahl des Benutzers gespeichert werden"); über die Sitzung genügt für
/// den gemeldeten Fall — den Blattwechsel — und bleibt plattformfrei.</para>
///
/// <para><b>Der DATENZOOM wird ausdrücklich NICHT gemerkt.</b> Ein aufgezogener
/// Ausschnitt ist eine Frage an EINE Stelle des Jahres; ihn beim nächsten Betreten
/// wiederherzustellen, zeigte ein Bild, das niemand angefordert hat.</para>
/// </summary>
public sealed class Ganglinienstand
{
    /// <summary>Ist überhaupt schon etwas gemerkt? Bis dahin gilt die Vorbelegung.</summary>
    public bool Belegt { get; private set; }

    /// <summary>Die gewählte Bedarfsart; −1 = Gesamt/Produktion.</summary>
    public int Bedarfsart { get; private set; } = -1;

    /// <summary>Steht die Dauerlinie vorn?</summary>
    public bool Sortiert { get; private set; }

    /// <summary>Ist die Bedarfslinie eingeblendet?</summary>
    public bool Bedarfslinie { get; private set; }

    /// <summary>Die gewählten Reihen als sprachneutrale Schlüssel.</summary>
    public IReadOnlyList<string> Reihen { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// Schreibt die Schalterstellung fort. Die Schlüssel werden KOPIERT — der Reiter
    /// baut seine Liste bei jeder Änderung neu, und ein gemerkter Verweis darauf
    /// wanderte mit.
    /// </summary>
    public void Merken(int bedarfsart, bool sortiert, bool bedarfslinie,
                       IEnumerable<string>? reihen)
    {
        Bedarfsart = bedarfsart;
        Sortiert = sortiert;
        Bedarfslinie = bedarfslinie;
        Reihen = reihen is null ? Array.Empty<string>() : new List<string>(reihen);
        Belegt = true;
    }

    /// <summary>Wirft das Gemerkte weg — der nächste Aufbau belegt wieder vor.</summary>
    public void Zuruecksetzen()
    {
        Belegt = false;
        Bedarfsart = -1;
        Sortiert = false;
        Bedarfslinie = false;
        Reihen = Array.Empty<string>();
    }
}

/// <summary>
/// <b>Der Ganglinienstand JE REITER über die Sitzung</b> — ein Halter, zwei Wirte:
/// Der Wärmegang und der Stromgang haben je einen eigenen Stand, und beide behalten
/// ihn, gleich WO die Ergebnisseite ihren Reiter gerade aufbaut (Ergebnisblatt der
/// Simulationsseite, Startseiten-Reiter „Simulation", Ansicht <c>SIMULATION</c>).
///
/// <para><b>Für Proben ist der Stand tauschbar.</b> bunit fährt Testklassen
/// nebeneinander, und ein prozessweiter Halter, den ein Fall zurücksetzt, während der
/// nächste liest, ist kein Prüfstand, sondern eine Verabredung zum Zufall. Jeder der
/// zwei Reiter führt deshalb den Parameter <c>Gedaechtnis</c> — genau wie die neun
/// Katalogdialoge ihr <c>Filterstandvorgabe</c> (S2.5): Ein Fall gibt seinen EIGENEN
/// <see cref="Ganglinienstand"/> herein und teilt mit niemandem. <see cref="Leeren"/>
/// bleibt für den Fall gedacht, dass ein Programmteil bewusst von vorn anfangen
/// will.</para>
/// </summary>
public static class Ganglinienregister
{
    /// <summary>Schlüssel des Wärmegang-Reiters.</summary>
    public const string WAERMEGANG = "WAERMEGANG";

    /// <summary>Schlüssel des Stromgang-Reiters.</summary>
    public const string STROMGANG = "STROMGANG";

    private static readonly object _schloss = new object();

    private static readonly Dictionary<string, Ganglinienstand> _staende =
        new Dictionary<string, Ganglinienstand>(StringComparer.Ordinal);

    /// <summary>
    /// Der Stand dieses Reiters — beim ersten Zugriff ein frischer, danach DIESELBE
    /// Instanz; genau daran hängt das Gedächtnis über den Blattwechsel.
    /// </summary>
    public static Ganglinienstand Stand(string reiter)
    {
        string schluessel = reiter ?? "";

        lock (_schloss)
        {
            if (!_staende.TryGetValue(schluessel, out Ganglinienstand? s))
            {
                s = new Ganglinienstand();
                _staende[schluessel] = s;
            }
            return s;
        }
    }

    /// <summary>Führt dieser Reiter schon einen Stand? (Prüfhilfe — sie legt keinen an.)</summary>
    public static bool Bekannt(string reiter)
    {
        lock (_schloss) { return _staende.ContainsKey(reiter ?? ""); }
    }

    /// <summary>Wirft ALLE gemerkten Stände weg.</summary>
    public static void Leeren()
    {
        lock (_schloss) { _staende.Clear(); }
    }
}
