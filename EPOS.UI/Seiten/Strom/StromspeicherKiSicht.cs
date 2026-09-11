using System.Globalization;
using System.Text;
using SpeicherEngine;
using WindowsFormsApplication1;

namespace EPOS.UI.Seiten.Strom;

/// <summary>
/// Das FLACHE Abbild der Stromspeicher-Ansicht für den Hilfe-Assistenten (Auftrag #200,
/// fünfte Deklaration des Dialogkatalogs).
///
/// <para><b>Warum ein eigenes Sichtmodell und nicht die Eingaben selbst.</b> Ein
/// Maskenfeld des Katalogs ist genau ZWEI Stufen tief (<c>Typ.Eigenschaft</c>,
/// <c>KiEigenschaftspfad</c>); was die Ansicht zeigt, liegt aber drei bis fünf Stufen tief
/// (<c>Eingaben.Auslegung.Flotte.Optionen.Betriebsziel</c>) und verteilt über drei
/// Objekte — Eingaben, Ergebnis und Vorprüfung. Diese Klasse löst die Ketten an EINER
/// benannten Stelle auf; jede <c>null</c>-Stufe darin ist begründet und nicht eine stille
/// Fehlerquelle im Getter der Brücke.</para>
///
/// <para><b>Sie hält KEINEN Zustand.</b> Alle Eigenschaften rechnen bei jedem Zugriff aus
/// den drei Delegaten — die Ansicht ersetzt ihren Eingabestand bei jeder Änderung durch
/// eine Kopie (<c>_eingaben = e.Kopie()</c>), ein festgehaltenes Objekt zeigte dem
/// Assistenten den Stand von vorhin.</para>
///
/// <para><b>Sechs Eigenschaften sind ABGELEITET und ohne Setzer</b> — die Diagnose und die
/// drei Ergebniswerte des letzten Laufs. Das ist keine Nachlässigkeit, sondern die Aussage:
/// Sie lassen sich nicht eingeben, und die Stufe S3 wird ein <c>feld_setzen</c> darauf
/// deshalb ablehnen (<c>KiFeldzugang.Setzbar</c> folgt der Schreibbarkeit der
/// Eigenschaft).</para>
///
/// <para><b>Die zehn übrigen tragen einen Setzer</b>, der in den lebenden Eingabestand
/// schreibt. Benutzt wird er erst in S3; er entsteht hier, damit Lese- und Schreibseite
/// dieselbe Kette benutzen und nicht zwei verschiedene.</para>
/// </summary>
public sealed class StromspeicherKiSicht
{
    private readonly Func<SpeicherOptimierungEingaben?> _eingaben;
    private readonly Func<SpeicherFlottenErgebnis?> _ergebnis;
    private readonly Func<IReadOnlyList<FlottenHinweis>> _hinweise;

    /// <summary>Legt die Sicht über die drei lebenden Stände der Ansicht.</summary>
    /// <param name="eingaben">Der Arbeitsstand (Flotte, Betriebsführung).</param>
    /// <param name="ergebnis">Das Ergebnis des letzten Flottenlaufs; <c>null</c> = keines.</param>
    /// <param name="hinweise">Die Hinweise der Vorprüfung; nie <c>null</c>.</param>
    public StromspeicherKiSicht(Func<SpeicherOptimierungEingaben?> eingaben,
                                Func<SpeicherFlottenErgebnis?> ergebnis,
                                Func<IReadOnlyList<FlottenHinweis>> hinweise)
    {
        _eingaben = eingaben ?? throw new ArgumentNullException(nameof(eingaben));
        _ergebnis = ergebnis ?? throw new ArgumentNullException(nameof(ergebnis));
        _hinweise = hinweise ?? throw new ArgumentNullException(nameof(hinweise));
    }

    // =====================================================================
    //  Die Flotte
    // =====================================================================

    /// <summary>Zahl der gleichzeitig betriebenen Speichereinheiten.</summary>
    public int Einheitenzahl => Einheiten.Count;

    /// <summary>
    /// Je Einheit Kapazität, Lade- und Entladeleistung — eine Zeile je Einheit.
    /// </summary>
    /// <remarks>
    /// <b>Ein Maskenfeld trägt EINEN Wert</b> (<c>KiDialogFeld</c>), eine Flotte aber
    /// beliebig viele Einheiten, deren Zahl erst zur Laufzeit feststeht. Die Aufstellung
    /// geht deshalb als TEXT hinaus — genau so, wie die Ansicht sie zeigt.
    /// </remarks>
    public string EinheitenListe
    {
        get
        {
            IReadOnlyList<FlottenEinheit> einheiten = Einheiten;
            if (einheiten.Count == 0) return "";

            var sb = new StringBuilder();
            var k = CultureInfo.CurrentCulture;

            for (int i = 0; i < einheiten.Count; i++)
            {
                FlottenEinheit e = einheiten[i];
                if (sb.Length > 0) sb.Append("; ");

                string name = string.IsNullOrWhiteSpace(e.Name)
                    ? (i + 1).ToString(k)
                    : e.Name;

                sb.Append(name).Append(": ")
                  .Append(e.KapazitaetKWh.ToString("0.###", k)).Append(" kWh, ")
                  .Append(e.LadeleistungKw.ToString("0.###", k)).Append(" / ")
                  .Append(e.EntladeleistungKw.ToString("0.###", k)).Append(" kW");
            }

            return sb.ToString();
        }
    }

    /// <summary>Summe der Vollbereichskapazitäten [kWh].</summary>
    public double KapazitaetGesamtKWh => Summe(e => e.KapazitaetKWh);

    /// <summary>Summe der höchsten Ladeleistungen [kW AC].</summary>
    public double LadeleistungGesamtKw => Summe(e => e.LadeleistungKw);

    /// <summary>Summe der höchsten Entladeleistungen [kW AC].</summary>
    public double EntladeleistungGesamtKw => Summe(e => e.EntladeleistungKw);

    // =====================================================================
    //  Die Betriebsführung
    // =====================================================================

    /// <summary>Das gefahrene Betriebsziel.</summary>
    public string Betriebsziel
    {
        get => Optionen?.Betriebsziel.ToString() ?? "";
        set
        {
            FlottenSimulationOptionen? o = Optionen;
            if (o is null || string.IsNullOrWhiteSpace(value)) return;
            if (Enum.TryParse(value, true, out FlottenBetriebsziel ziel)) o.Betriebsziel = ziel;
        }
    }

    /// <summary>Das weiche wirtschaftliche Peak-Ziel [kW]; <c>null</c> = keines.</summary>
    public double? PeakZielKw
    {
        get => Optionen?.WirtschaftlicherPeakZielwertKw;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.WirtschaftlicherPeakZielwertKw = value; }
    }

    /// <summary>
    /// Läuft die Entladeschwelle als kausale Ratsche (Spezifikation 5.1.1) oder steht sie
    /// den ganzen Zeitraum fest?
    /// </summary>
    /// <remarks>
    /// Bei <c>true</c> ist <see cref="PeakZielKw"/> der STARTWERT H₀, und die Schwelle
    /// wird im Lauf nachgezogen, sobald die Flotte eine Spitze nicht halten kann.
    /// </remarks>
    public bool PeakZielAdaptiv
    {
        get => Optionen?.PeakZielAdaptiv == true;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.PeakZielAdaptiv = value; }
    }

    /// <summary>Ist das Laden aus dem Netz freigegeben?</summary>
    public bool NetzladungErlaubt
    {
        get => Optionen?.NetzladungErlaubt == true;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.NetzladungErlaubt = value; }
    }

    /// <summary>
    /// Der Start-Ladezustand [%], gemittelt über die Einheiten.
    /// </summary>
    /// <remarks>
    /// <b>Ein Mittelwert und keine Liste</b> — aus demselben Grund wie bei den drei
    /// Summen: Das Feld trägt einen Wert. Beim Setzen bekommen ALLE Einheiten denselben
    /// Start-SoC; das ist die einzige Deutung, die aus einer Zahl folgt.
    /// </remarks>
    public double StartSocProzent
    {
        get
        {
            IReadOnlyList<FlottenEinheit> einheiten = Einheiten;
            if (einheiten.Count == 0) return 0.0;

            double summe = 0.0;
            foreach (FlottenEinheit e in einheiten) summe += e.SocStart;
            return summe / einheiten.Count * 100.0;
        }
        set
        {
            foreach (FlottenEinheit e in Einheiten) e.SocStart = value / 100.0;
        }
    }

    /// <summary>Geschützte Peak-Reserve [kWh], summiert über die Flotte.</summary>
    public double PeakReserveKWh
    {
        get => Summe(e => e.PeakReserveKWh);
        set
        {
            IReadOnlyList<FlottenEinheit> einheiten = Einheiten;
            if (einheiten.Count == 0) return;

            double jeEinheit = value / einheiten.Count;
            foreach (FlottenEinheit e in einheiten) e.PeakReserveKWh = jeEinheit;
        }
    }

    // =====================================================================
    //  Die Diagnose (nur lesend)
    // =====================================================================

    /// <summary>Hat die Flotte im ganzen Zeitraum weder geladen noch entladen?</summary>
    public bool Arbeitslos => Diagnose?.Arbeitslos == true;

    /// <summary>
    /// Die gezählten Gründe als EIN Satz — gebaut im Kern
    /// (<see cref="FlottenPlausibilitaet.Gruende"/>), damit Assistent und Diagnosebanner
    /// denselben Wortlaut führen.
    /// </summary>
    public string DiagnoseGruende
    {
        get
        {
            FlottenDiagnose? d = Diagnose;
            return d is null ? "" : FlottenPlausibilitaet.Gruende(d, CultureInfo.CurrentCulture);
        }
    }

    /// <summary>
    /// Die Prüfhinweise — die des letzten Laufs, sonst die der Vorprüfung —, untereinander
    /// als Text.
    /// </summary>
    public string Pruefhinweise
    {
        get
        {
            IReadOnlyList<FlottenHinweis> hinweise = Hinweisliste;
            if (hinweise.Count == 0) return "";

            var sb = new StringBuilder();
            foreach (FlottenHinweis h in hinweise)
            {
                if (h is null || string.IsNullOrWhiteSpace(h.Text)) continue;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(h.Text);
            }
            return sb.ToString();
        }
    }

    // =====================================================================
    //  Das Ergebnis der letzten Bewertung (nur lesend)
    // =====================================================================

    /// <summary>Höchster Netzbezug [kW] im Lauf mit Flotte; <c>null</c> = kein Lauf.</summary>
    public double? BezugsspitzeKw => Variante?.MaximalerNetzbezugKw;

    /// <summary>Netzbezug [kWh] im Lauf mit Flotte; <c>null</c> = kein Lauf.</summary>
    public double? NetzbezugKWh => Variante?.NetzbezugKWh;

    /// <summary>
    /// Kapitalwert [€] gegenüber der Variante ohne Zusatzspeicher; <c>null</c> = nicht
    /// bewertet (ohne Jahreskonten gibt es keinen).
    /// </summary>
    public double? KapitalwertEuro => _ergebnis()?.Studie?.Wirtschaftlichkeit?.KapitalwertEuro;

    // =====================================================================
    //  Die Ketten — einmal hier und nirgends sonst
    // =====================================================================

    private FlottenStudieKonfiguration? Flotte => _eingaben()?.Auslegung?.Flotte;

    private FlottenSimulationOptionen? Optionen => Flotte?.Optionen;

    private IReadOnlyList<FlottenEinheit> Einheiten
        => (IReadOnlyList<FlottenEinheit>?)Flotte?.Einheiten ?? Array.Empty<FlottenEinheit>();

    private FlottenSimulationErgebnis? Variante => _ergebnis()?.Studie?.Variante;

    private FlottenDiagnose? Diagnose => Variante?.Diagnose;

    /// <summary>
    /// Die Prüfhinweise des LETZTEN LAUFS haben Vorrang vor denen der Vorprüfung: Nach
    /// einem Lauf beantworten sie die Frage „warum sieht das Ergebnis so aus?", davor
    /// kann es nur die Vorprüfung.
    /// </summary>
    private IReadOnlyList<FlottenHinweis> Hinweisliste
    {
        get
        {
            List<FlottenHinweis>? ausLauf = _ergebnis()?.Pruefhinweise;
            if (ausLauf is { Count: > 0 }) return ausLauf;
            return _hinweise() ?? Array.Empty<FlottenHinweis>();
        }
    }

    private double Summe(Func<FlottenEinheit, double> welche)
    {
        double summe = 0.0;
        foreach (FlottenEinheit e in Einheiten) summe += welche(e);
        return summe;
    }
}
