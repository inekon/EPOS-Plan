using System;
using System.Threading.Tasks;

using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// Die SPERRE „ein Lauf zur Zeit" (Anwenderentscheid <b>SIM‑E‑2</b>, Punkt 5,
/// 11.09.2026).
///
/// <para><b>Wozu.</b> Seit #220 gibt es ZWEI Stellen, an denen derselbe Lauf
/// angestoßen wird: Schritt ② der Ansicht <c>SIMULATION</c> und die Kachel
/// „Simulation starten" des Startseiten-Reiters. Beide bedienen denselben Kern und
/// dieselbe Hülle; zwei gleichzeitige Läufe wären zwei Schreiber auf einem Stand.</para>
///
/// <para><b>Sie lebt in der QUELLE, nicht in einer Komponente</b>
/// (<c>SimulationAnsichtQuelle</c> legt EINE je Projekt an und gibt sie jedem
/// Parametersatz mit). Eine Komponente hält sie nicht: Ansicht und Reiter sind zwei
/// Komponenten mit zwei Lebensdauern, und die Wurzel verwirft die eine, wenn sie die
/// andere zeigt. <c>null</c> heißt „diese Plattform führt keine Sperre" — dann bleibt
/// alles, wie es vor #220 war.</para>
///
/// <para><b>Kein Sperrobjekt und kein Faden.</b> Angemeldet wird ausschließlich auf
/// dem Bedienfaden (Blazor zeichnet und behandelt Ereignisse auf einem Faden); der
/// Lauf selbst liegt im Hintergrund, aber sein Start und sein Ende nicht.</para>
/// </summary>
public sealed class SimulationLaufsperre
{
    /// <summary>Wer den Lauf angemeldet hat; <c>null</c> = frei.</summary>
    private object? _inhaber;

    /// <summary>Läuft gerade ein Lauf — gleich welcher Wirt ihn angestoßen hat?</summary>
    public bool Belegt => _inhaber is not null;

    /// <summary>Läuft ein Lauf, den ein ANDERER Wirt angestoßen hat?</summary>
    public bool Fremd(object wer)
        => _inhaber is not null && !ReferenceEquals(_inhaber, wer);

    /// <summary>
    /// Meldet einen Lauf an. <c>false</c> = ein anderer Wirt rechnet schon; dann
    /// geschieht nichts.
    /// </summary>
    public bool Anmelden(object wer)
    {
        if (wer is null) return false;
        if (_inhaber is not null) return ReferenceEquals(_inhaber, wer);

        _inhaber = wer;
        return true;
    }

    /// <summary>Gibt die Sperre frei — nur der Inhaber kann das.</summary>
    public void Abmelden(object wer)
    {
        if (ReferenceEquals(_inhaber, wer)) _inhaber = null;
    }
}

/// <summary>
/// Die GEMEINSAME LAUFSTEUERUNG der zwei Wirte des Simulationslaufs (Auftrag
/// <b>#220</b>, Anwenderentscheid <b>SIM‑E‑2</b>): der Ansicht
/// <c>SimulationSeite</c> (Schritt ②) und des Startseiten-Reiters
/// <c>SimulationReiter</c> (Kachel „Simulation starten").
///
/// <para><b>Eine Wahrheit.</b> Beide beantworten dieselben drei Fragen — „warum ist
/// der Lauf gesperrt?", „darf er starten?" und „wie startet er?" —, und sie müssen
/// dieselben Antworten geben: derselbe Sperrgrund in derselben Reihenfolge, dieselbe
/// Rücksicht auf einen fremden Lauf, derselbe Weg über
/// <see cref="SimulationErgebnisSeite.LaufStarten"/>. Kopiert wäre das zweimal
/// derselbe Zustandsautomat.</para>
///
/// <para><b>Sie rechnet nicht und lädt nicht.</b> Der Lauf gehört unverändert der
/// Ergebnisseite: Sie führt Fortschrittsbalken, Abbruch, das Neuladen danach und die
/// Meldung an ihren Wirt. Hier steht nur, WANN er starten darf und wer die Sperre
/// hält.</para>
/// </summary>
public sealed class SimulationLaufsteuerung
{
    private readonly object _wirt;
    private readonly Func<SimulationAnsichtDienste?> _dienste;
    private readonly Func<bool> _konfigUngespeichert;
    private readonly Func<SimulationErgebnisSeite?> _blatt;

    /// <param name="wirt">
    /// Der Wirt — die Komponente, die den Lauf anstößt. Sie ist der Inhaber der
    /// <see cref="SimulationLaufsperre"/> und erkennt daran ihren EIGENEN Lauf.
    /// </param>
    /// <param name="dienste">Die Datenseite der Ansicht; <c>null</c> = nichts geht.</param>
    /// <param name="konfigUngespeichert">
    /// Hat Schritt ① ungespeicherte Änderungen? Der Reiter kennt keinen Schritt ①
    /// und meldet deshalb immer <c>false</c>.
    /// </param>
    /// <param name="blatt">Die stehende Ergebnisseite; <c>null</c> = sie zeichnet noch.</param>
    public SimulationLaufsteuerung(object wirt,
                                   Func<SimulationAnsichtDienste?> dienste,
                                   Func<bool> konfigUngespeichert,
                                   Func<SimulationErgebnisSeite?> blatt)
    {
        _wirt = wirt ?? throw new ArgumentNullException(nameof(wirt));
        _dienste = dienste ?? throw new ArgumentNullException(nameof(dienste));
        _konfigUngespeichert = konfigUngespeichert ?? (() => false);
        _blatt = blatt ?? throw new ArgumentNullException(nameof(blatt));
    }

    private SimulationAnsichtDienste? Dienste => _dienste();

    /// <summary>
    /// Der Grund, aus dem der Lauf gesperrt ist; leer = frei.
    /// </summary>
    /// <remarks>
    /// Die Reihenfolge ist die der Ansicht seit #207, um einen Fall erweitert:
    /// (0) es rechnet schon jemand anders (SIM‑E‑2, Punkt 5), (1) die ROTE
    /// Vorprüfung — ohne sie läuft gar nichts —, (2) die ungespeicherte
    /// Konfiguration; der Lauf läse sonst den alten Stand aus der Datenbank.
    /// </remarks>
    public string Sperrgrund
    {
        get
        {
            SimulationAnsichtDienste? dienste = Dienste;
            if (dienste?.Ergebnis is null) return "";

            if (dienste.Laufsperre?.Fremd(_wirt) ?? false) return Resource.SIM_LAUF_ANDERSWO;

            string sperrgrund = dienste.Sperrgrund?.Invoke() ?? "";
            if (sperrgrund.Length > 0) return sperrgrund;

            return _konfigUngespeichert() ? Resource.SIM_ANSICHT_LAUF_GESPERRT_KONFIG : "";
        }
    }

    /// <summary>Darf der Lauf starten?</summary>
    public bool Frei => Dienste?.Ergebnis is not null && Sperrgrund.Length == 0;

    /// <summary>Rechnet die Ergebnisseite dieses Wirts gerade?</summary>
    public bool Laeuft => _blatt()?.Laeuft ?? false;

    /// <summary>Der Fortschritt des eigenen Laufs; <c>null</c> = unbestimmt.</summary>
    public double? Anteil => _blatt()?.Anteil;

    /// <summary>Die Phasenmeldung des eigenen Laufs; leer = die Vorgabe.</summary>
    public string Fortschrittstext => _blatt()?.Fortschrittstext ?? "";

    /// <summary>Nimmt die Ergebnisseite einen Abbruch entgegen?</summary>
    public bool AbbruchMoeglich => _blatt()?.AbbruchMoeglich ?? false;

    /// <summary>Bricht den laufenden Lauf ab — der Weg der Ergebnisseite.</summary>
    public void Abbrechen() => _blatt()?.LaufAbbrechen();

    /// <summary>
    /// Startet den Lauf: Sperre anmelden, den Weg der Ergebnisseite gehen, Sperre
    /// freigeben. Gesperrt oder ohne stehende Ergebnisseite geschieht nichts.
    /// </summary>
    public async Task Starten()
    {
        if (!Frei) return;

        SimulationErgebnisSeite? seite = _blatt();
        if (seite is null) return;

        SimulationLaufsperre? sperre = Dienste?.Laufsperre;
        if (sperre is not null && !sperre.Anmelden(_wirt)) return;

        try
        {
            await seite.LaufStarten();
        }
        finally
        {
            sperre?.Abmelden(_wirt);
        }
    }
}
