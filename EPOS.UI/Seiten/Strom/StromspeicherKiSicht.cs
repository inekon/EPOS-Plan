using System.Globalization;
using System.Text;
using KiKern;
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
    private readonly Func<string>? _schritt;

    /// <summary>Legt die Sicht über die lebenden Stände der Ansicht.</summary>
    /// <param name="eingaben">Der Arbeitsstand (Flotte, Betriebsführung).</param>
    /// <param name="ergebnis">Das Ergebnis des letzten Flottenlaufs; <c>null</c> = keines.</param>
    /// <param name="hinweise">Die Hinweise der Vorprüfung; nie <c>null</c>.</param>
    /// <param name="schritt">
    /// Der Name des stehenden Schritts der Ablaufleiste (Auftrag #224); <c>null</c> =
    /// die Ansicht meldet ihn nicht, das Feld bleibt leer. Er ist WAHLFREI, damit ein
    /// Prüfstand die Sicht weiterhin mit drei Delegaten anlegen kann.
    /// </param>
    public StromspeicherKiSicht(Func<SpeicherOptimierungEingaben?> eingaben,
                                Func<SpeicherFlottenErgebnis?> ergebnis,
                                Func<IReadOnlyList<FlottenHinweis>> hinweise,
                                Func<string>? schritt = null)
    {
        _eingaben = eingaben ?? throw new ArgumentNullException(nameof(eingaben));
        _ergebnis = ergebnis ?? throw new ArgumentNullException(nameof(ergebnis));
        _hinweise = hinweise ?? throw new ArgumentNullException(nameof(hinweise));
        _schritt = schritt;
    }

    // =====================================================================
    //  Wo der Anwender steht (Auftrag #224)
    // =====================================================================

    /// <summary>
    /// Der Name des stehenden Schritts der Ablaufleiste — „Speicher", „Daten &amp;
    /// Kosten", „Betriebsführung", „Optimierung", „Ergebnis".
    /// </summary>
    /// <remarks>
    /// Ohne ihn beantwortet der Assistent eine Frage wie „was soll ich hier tun?" ins
    /// Blaue: Die Ansicht führt fünf Blätter mit ganz verschiedenen Feldern. Er ist
    /// ABGELEITET und ohne Setzer — ein Schrittwechsel ist eine Bedienhandlung und kein
    /// Feldwert.
    /// </remarks>
    public string Schritt => _schritt?.Invoke() ?? "";

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

    /// <summary>
    /// Die fünf Betriebsziele, die die Maske anbietet (KI-F1b, KI-D-Q6) — der
    /// Schlüssel ist der Name des Aufzählungswertes, den <see cref="Betriebsziel"/>
    /// führt.
    /// </summary>
    /// <remarks>
    /// <b>Die Liste steht an EINER Stelle</b> (<c>SpeicherFlottenBetriebEditor</c>):
    /// Klappliste und Assistentenauswahl zeigen dieselben fünf Ziele. Die Anmeldung
    /// findet diese Eigenschaft über die Namenskonvention <c>&lt;Eigenschaft&gt;Wahl</c>.
    /// </remarks>
    public IReadOnlyList<KiWahleintrag> BetriebszielWahl
    {
        get
        {
            var liste = new List<KiWahleintrag>();
            foreach ((int id, string text) in
                     EPOS.UI.Dialoge.Strom.SpeicherFlottenBetriebEditor.Betriebsziele)
                liste.Add(new KiWahleintrag(((FlottenBetriebsziel)id).ToString(), text));
            return liste;
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
    //  Die Optimierung — Station 4 (Auftrag #224)
    // =====================================================================

    /// <summary>
    /// Sucht der nächste Lauf die wirtschaftlich beste Größe, oder bewertet er nur die
    /// eingestellte Flotte?
    /// </summary>
    public bool GroessenOptimieren
    {
        get => _eingaben()?.Auslegung?.FlottenGroessenOptimieren == true;
        set
        {
            SpeicherAuslegungKonfiguration? a = _eingaben()?.Auslegung;
            if (a is not null) a.FlottenGroessenOptimieren = value;
        }
    }

    /// <summary>
    /// WAS der nächste Lauf variiert (Auftrag #247, SD‑E‑10): „Nur bewerten",
    /// „Größe suchen" oder „Stückzahl suchen".
    /// </summary>
    /// <remarks>
    /// <b>Nur lesend.</b> Die Methode hängt an ZWEI Feldern desselben Standes
    /// (<c>FlottenAuslegungEingang.Suchmethode</c> und
    /// <c>SpeicherAuslegungKonfiguration.FlottenGroessenOptimieren</c>) und an der Frage,
    /// ob überhaupt eine Einheit „variieren" trägt; sie zu SETZEN ist die Aufgabe der
    /// Optionsgruppe in Station 4, die beides gleichzieht und die Sperre kennt.
    /// </remarks>
    public string Suchmethode
    {
        get
        {
            if (!GroessenOptimieren) return WindowsFormsApplication1.MyResource.Resource.FLOTTE_OPT_SUCHE_AUS;
            return Auslegung?.Suchmethode == FlottenSuchmethode.Stueckzahl
                ? WindowsFormsApplication1.MyResource.Resource.FLOTTE_OPT_METHODE_STUECKZAHL
                : WindowsFormsApplication1.MyResource.Resource.FLOTTE_OPT_METHODE_GROESSE;
        }
    }

    /// <summary>
    /// Läuft nach dem Grobraster die zweite Phase — das Feinraster um das Grob-Optimum
    /// (SD‑Q10)?
    /// </summary>
    public bool Feinraster
    {
        get => Auslegung?.Feinraster == true;
        set { FlottenAuslegungEingang? a = Auslegung; if (a is not null) a.Feinraster = value; }
    }

    /// <summary>Obergrenze der Kandidatenzahl über BEIDE Phasen.</summary>
    public int MaximaleKandidaten
    {
        get => Auslegung?.MaximaleKandidaten ?? 0;
        set { FlottenAuslegungEingang? a = Auslegung; if (a is not null) a.MaximaleKandidaten = value; }
    }

    /// <summary>
    /// Wie viele Kandidaten der eingestellte Suchraum ergibt — Grobraster plus
    /// Feinraster-Obergrenze.
    /// </summary>
    /// <remarks>
    /// Die Zahl kommt aus <see cref="FlottenOptimierer.Kandidatenzahl"/>, also aus
    /// derselben Rechnung, mit der der Lauf sein Raster annimmt oder abweist; ein
    /// unbrauchbarer Suchraum liefert 0 statt einer erfundenen Zahl. Sie ist
    /// ABGELEITET — eingeben lässt sich nur der Bereich, aus dem sie folgt.
    /// </remarks>
    public int Kandidatenzahl
    {
        get
        {
            FlottenStudieKonfiguration? f = Flotte;
            if (f is null) return 0;
            FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(f);
            return zahl.Gueltig ? (int)Math.Min(int.MaxValue, zahl.Gesamt) : 0;
        }
    }

    /// <summary>
    /// Kapitalwert [€] des besten Kandidaten der letzten Suche; <c>null</c> = keine
    /// Suche gerechnet oder die Nullvariante hat gewonnen.
    /// </summary>
    public double? BesterKapitalwertEuro => BesterKandidat?.KapitalwertEuro;

    /// <summary>Kapazität [kWh] des besten Kandidaten; <c>null</c> wie oben.</summary>
    public double? BesteKapazitaetKWh => BesterKandidat?.KapazitaetKWh;

    /// <summary>
    /// Jährliche Betriebsersparnis [€/a] des besten Kandidaten, OHNE Kapitaldienst
    /// (SD‑Q11 — die Zahl, mit der sich die Mappe V7 vergleichen lässt).
    /// </summary>
    public double? BesteErsparnisEuroJahr => BesterKandidat?.ErsparnisEuroJahr;

    /// <summary>
    /// Aus welcher Phase der beste Kandidat stammt — „Grob" oder „Fein"; leer ohne
    /// Suche.
    /// </summary>
    public string BestePhase => BesterKandidat is { } k ? k.Phase.ToString() : "";

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
    //  Station 1 — die EINHEITEN der Flotte als SPALTEN (Welle KI-F6)
    // =====================================================================

    /// <summary>
    /// Je Speichereinheit eine Zeile mit den Feldern, die der Einheiteneditor zeigt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Eine SAMMLUNG und keine zwanzig Einzelfelder</b> (<c>KiEigenschaftspfad</c>,
    /// Spaltenform): Wie viele Einheiten eine Flotte führt, steht erst zur Laufzeit
    /// fest — dieselbe Lage wie bei den Positionen der Kostenverwaltung. Die
    /// Aufstellung <see cref="EinheitenListe"/> bleibt daneben stehen: Sie beantwortet
    /// „wie sieht die Flotte aus?" in einem Satz, die Spalten beantworten „setze bei
    /// Speicher 2 die Kapazität auf 500 kWh".
    /// </para>
    /// <para>
    /// <b>Die Zeile ist eine HÜLLE und nicht <c>FlottenEinheit</c> selbst.</b> Fünf
    /// Werte stehen in der Engine als Anteil 0…1 und auf der Maske in PROZENT
    /// (Lade- und Entladewirkungsgrad, die drei SoC-Grenzen). Ein unmittelbar
    /// angemeldetes Modell böte an, 95 zu setzen, wo der Anwender 95 % liest und die
    /// Engine 0,95 rechnet — genau die stille Setzung, die Fachkonzept 11.6
    /// ausschließt. Die Hülle rechnet an EINER Stelle um.
    /// </para>
    /// <para>
    /// <b>Die RAINFLOW-Kurve bleibt draußen.</b> Sie ist eine Tabelle IN der Zeile —
    /// eine zweite Sammlungsstufe, die der Eigenschaftspfad nicht kennt — und trägt
    /// ihren eigenen Editor samt „Punkt hinzufügen/entfernen" (KI‑D‑Q6).
    /// </para>
    /// </remarks>
    public IReadOnlyList<FlottenEinheitKiZeile> Einheitenzeilen
    {
        get
        {
            IReadOnlyList<FlottenEinheit> einheiten = Einheiten;
            var zeilen = new List<FlottenEinheitKiZeile>(einheiten.Count);
            for (int i = 0; i < einheiten.Count; i++)
                zeilen.Add(new FlottenEinheitKiZeile(einheiten[i], i));
            return zeilen;
        }
    }

    // =====================================================================
    //  Station 2 — Daten und Kosten (Welle KI-F6)
    // =====================================================================

    /// <summary>Die zwei Quellen des Lastgangs.</summary>
    public IReadOnlyList<KiWahleintrag> LastquelleWahl
        => Quellenwahl(EPOS.UI.Dialoge.Strom.SpeicherAuslegungEditor.LastQuellen);

    /// <summary>Woher der Strombedarf kommt — aus EPOS oder aus einer CSV-Datei.</summary>
    public SpeicherAuslegungQuelle Lastquelle
    {
        get => Konfiguration?.Lastquelle ?? SpeicherAuslegungQuelle.Epos;
        set { SpeicherAuslegungKonfiguration? k = Konfiguration; if (k is not null) k.Lastquelle = value; }
    }

    /// <summary>Die drei Quellen der PV-Erzeugung.</summary>
    public IReadOnlyList<KiWahleintrag> PvQuelleWahl
        => Quellenwahl(EPOS.UI.Dialoge.Strom.SpeicherAuslegungEditor.PvQuellen);

    /// <summary>Woher die PV-Erzeugung kommt.</summary>
    public SpeicherAuslegungQuelle PvQuelle
    {
        get => Konfiguration?.PvQuelle ?? SpeicherAuslegungQuelle.Epos;
        set { SpeicherAuslegungKonfiguration? k = Konfiguration; if (k is not null) k.PvQuelle = value; }
    }

    /// <summary>Die drei Quellen des Bezugspreises.</summary>
    public IReadOnlyList<KiWahleintrag> PreisquelleWahl
        => Quellenwahl(EPOS.UI.Dialoge.Strom.SpeicherAuslegungEditor.PreisQuellen);

    /// <summary>Woher der Bezugspreis kommt.</summary>
    public SpeicherAuslegungQuelle Preisquelle
    {
        get => Konfiguration?.Preisquelle ?? SpeicherAuslegungQuelle.Epos;
        set { SpeicherAuslegungKonfiguration? k = Konfiguration; if (k is not null) k.Preisquelle = value; }
    }

    /// <summary>
    /// Ordnet die CSV-Zeitachse dem EPOS-Modelljahr zu — nötig, sobald eine Datei
    /// neben einer EPOS-Reihe steht.
    /// </summary>
    public bool EposModelljahrZuordnen
    {
        get => Konfiguration?.EposModelljahrZuordnen == true;
        set { SpeicherAuslegungKonfiguration? k = Konfiguration; if (k is not null) k.EposModelljahrZuordnen = value; }
    }

    /// <summary>Die zwei Quellen der Kostensätze.</summary>
    public IReadOnlyList<KiWahleintrag> InvestitionsquelleWahl
        => Quellenwahl(EPOS.UI.Dialoge.Strom.SpeicherAuslegungEditor.KostenQuellen);

    /// <summary>Woher die Investitionskosten kommen — aus dem Dialog oder dem Kostenmodul.</summary>
    public SpeicherKostenQuelle Investitionsquelle
    {
        get => Konfiguration?.Investitionsquelle ?? SpeicherKostenQuelle.Dialog;
        set { SpeicherAuslegungKonfiguration? k = Konfiguration; if (k is not null) k.Investitionsquelle = value; }
    }

    /// <summary>Die zwei Quellen der Kostensätze.</summary>
    public IReadOnlyList<KiWahleintrag> BetriebsquelleWahl
        => Quellenwahl(EPOS.UI.Dialoge.Strom.SpeicherAuslegungEditor.KostenQuellen);

    /// <summary>Woher die Betriebskosten kommen.</summary>
    public SpeicherKostenQuelle Betriebsquelle
    {
        get => Konfiguration?.Betriebsquelle ?? SpeicherKostenQuelle.Dialog;
        set { SpeicherAuslegungKonfiguration? k = Konfiguration; if (k is not null) k.Betriebsquelle = value; }
    }

    /// <summary>
    /// Leistungsbezogene Investition [€/kW] des Dialogsatzes.
    /// </summary>
    /// <remarks>
    /// <b>Gelesen wird, was die Maske ZEIGT</b> — steht die Quelle auf Kostenmodul,
    /// ist das der Modulsatz; geschrieben wird immer in den DIALOGSATZ, weil allein
    /// er eingebbar ist. Genau so verhält sich das Eingabefeld: Es ist bei
    /// Kostenmodul gesperrt.
    /// </remarks>
    public double InvestitionProKw
    {
        get => InvestAnzeige?.InvestEurProKw ?? 0.0;
        set => KostenSetzen(k => k.InvestEurProKw = value, invest: true);
    }

    /// <summary>Kapazitätsbezogene Investition [€/kWh].</summary>
    public double InvestitionProKWh
    {
        get => InvestAnzeige?.InvestEurProKwh ?? 0.0;
        set => KostenSetzen(k => k.InvestEurProKwh = value, invest: true);
    }

    /// <summary>Leistungsbezogene Betriebskosten [€/(kW·a)].</summary>
    public double BetriebProKw
    {
        get => BetriebAnzeige?.BetriebEurProKwJahr ?? 0.0;
        set => KostenSetzen(k => k.BetriebEurProKwJahr = value, invest: false);
    }

    /// <summary>Kapazitätsbezogene Betriebskosten [€/(kWh·a)].</summary>
    public double BetriebProKWh
    {
        get => BetriebAnzeige?.BetriebEurProKwhJahr ?? 0.0;
        set => KostenSetzen(k => k.BetriebEurProKwhJahr = value, invest: false);
    }

    /// <summary>Betriebskosten je entladener Energie [€/kWh].</summary>
    public double BetriebProKWhEntladen
    {
        get => BetriebAnzeige?.BetriebEurProKwhEntladen ?? 0.0;
        set => KostenSetzen(k => k.BetriebEurProKwhEntladen = value, invest: false);
    }

    /// <summary>Der Leistungspreis L_P des Netzanschlusses [€/(kW·a)].</summary>
    /// <remarks>
    /// <b>Er steht an ZWEI Orten und wird an beiden geschrieben</b> — im
    /// Optimierungsstand und im Tarif der Flotte; genau so tut es der Wirt, wenn der
    /// Anwender das Feld verlässt. Ein Setzer, der nur einen Ort träfe, ließe die
    /// Wirtschaftlichkeit mit der alten Zahl rechnen.
    /// </remarks>
    public double Leistungspreis
    {
        get => _eingaben()?.LeistungspreisEurProKwA ?? 0.0;
        set
        {
            SpeicherOptimierungEingaben? e = _eingaben();
            if (e is null) return;
            e.LeistungspreisEurProKwA = value;

            FlottenStudieKonfiguration? f = Flotte;
            if (f is not null) (f.Tarif ??= new FlottenTarif()).LeistungspreisEuroProKw = value;
        }
    }

    /// <summary>Energie-Ausgleichswert [€/kWh gespeichert]; <c>null</c> = keiner.</summary>
    public double? EnergieAusgleich
    {
        get => Optionen?.EnergieAusgleichEuroProKWh;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.EnergieAusgleichEuroProKWh = value; }
    }

    /// <summary>Kalkulationszins der Studie [%].</summary>
    /// <remarks>Die Engine führt ihn als Anteil 0…1, die Maske zeigt Prozent.</remarks>
    public double KalkulationszinsProzent
    {
        get => (Wirtschaft?.Kalkulationszins ?? 0.0) * 100.0;
        set { FlottenWirtschaftlichkeitEingang? w = Wirtschaft; if (w is not null) w.Kalkulationszins = value / 100.0; }
    }

    /// <summary>Die zwei Jahresprojektionen.</summary>
    public IReadOnlyList<KiWahleintrag> JahresprojektionWahl
        => Quellenwahl(EPOS.UI.Dialoge.Strom.SpeicherFlottenWirtschaftBlock.Projektionsarten);

    /// <summary>
    /// 0 = eingelesene Jahre einzeln bewerten, 1 = Referenzjahr ausdrücklich
    /// wiederholen.
    /// </summary>
    public int Jahresprojektion
    {
        get => Wirtschaft?.ReferenzjahrExplizitWiederholen == true ? 1 : 0;
        set { FlottenWirtschaftlichkeitEingang? w = Wirtschaft; if (w is not null) w.ReferenzjahrExplizitWiederholen = value == 1; }
    }

    /// <summary>Projektlaufzeit bei wiederholtem Referenzjahr [a].</summary>
    public int Projektjahre
    {
        get => Wirtschaft?.ProjektjahreBeiWiederholung ?? 0;
        set { FlottenWirtschaftlichkeitEingang? w = Wirtschaft; if (w is not null) w.ProjektjahreBeiWiederholung = value; }
    }

    /// <summary>Zusätzlicher Restwert der Studie [€] — über die Restwerte der Einheiten hinaus.</summary>
    public double RestwertStudieEuro
    {
        get => Wirtschaft?.RestwertEuro ?? 0.0;
        set { FlottenWirtschaftlichkeitEingang? w = Wirtschaft; if (w is not null) w.RestwertEuro = value; }
    }

    // =====================================================================
    //  Station 3 — Betriebsführung und Netz (Welle KI-F6)
    // =====================================================================

    /// <summary>Die drei Verteilungsregeln der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> VerteilungWahl
        => Aufzaehlungswahl<FlottenVerteilung>(
               EPOS.UI.Dialoge.Strom.SpeicherFlottenBetriebEditor.Verteilungen);

    /// <summary>Wie sich die Leistung auf die Einheiten verteilt.</summary>
    public FlottenVerteilung Verteilung
    {
        get => Optionen?.Verteilung ?? FlottenVerteilung.KapazitaetsProportional;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.Verteilung = value; }
    }

    /// <summary>Die zwei Erzeugerreihenfolgen der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> ErzeugerPrioritaetWahl
        => Aufzaehlungswahl<FlottenErzeugerPrioritaet>(
               EPOS.UI.Dialoge.Strom.SpeicherFlottenBetriebEditor.ErzeugerPrioritaeten);

    /// <summary>Welcher Erzeuger den Speicher zuerst lädt.</summary>
    public FlottenErzeugerPrioritaet ErzeugerPrioritaet
    {
        get => Optionen?.ErzeugerPrioritaet ?? FlottenErzeugerPrioritaet.PvVorBhkw;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.ErzeugerPrioritaet = value; }
    }

    /// <summary>Ist die Einspeisung aus der Batterie ins Netz freigegeben?</summary>
    public bool BatterieexportErlaubt
    {
        get => Optionen?.BatterieexportErlaubt == true;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.BatterieexportErlaubt = value; }
    }

    /// <summary>Harte Bezugsgrenze am Netzanschluss [kW]; <c>null</c> = keine.</summary>
    public double? NetzbezugGrenzeKw
    {
        get => Optionen?.NetzbezugGrenzeKw;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.NetzbezugGrenzeKw = value; }
    }

    /// <summary>Harte Einspeisegrenze am Netzanschluss [kW]; <c>null</c> = keine.</summary>
    public double? NetzeinspeisungGrenzeKw
    {
        get => Optionen?.NetzeinspeisungGrenzeKw;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.NetzeinspeisungGrenzeKw = value; }
    }

    /// <summary>Die zwei Informationsstände der Planung.</summary>
    public IReadOnlyList<KiWahleintrag> InformationsstandWahl
        => Aufzaehlungswahl<PrognoseArt>(
               EPOS.UI.Dialoge.Strom.SpeicherFlottenNetzBlock.Prognosearten);

    /// <summary>Mit welchem Wissen der Fahrplaner rechnet.</summary>
    public PrognoseArt Informationsstand
    {
        get => Optionen?.PrognoseArt ?? PrognoseArt.VerifiziertBekannt;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.PrognoseArt = value; }
    }

    /// <summary>Planungshorizont in Intervallen.</summary>
    public int PlanungshorizontIntervalle
    {
        get => Optionen?.PlanungshorizontIntervalle ?? 0;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.PlanungshorizontIntervalle = value; }
    }

    /// <summary>Abstand zweier Planungsläufe in Intervallen.</summary>
    public int NeuplanungAlleIntervalle
    {
        get => Optionen?.NeuplanungAlleIntervalle ?? 0;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.NeuplanungAlleIntervalle = value; }
    }

    /// <summary>Die drei Horizont-Endbedingungen.</summary>
    public IReadOnlyList<KiWahleintrag> EndbedingungWahl
        => Aufzaehlungswahl<FlottenEndbedingung>(
               EPOS.UI.Dialoge.Strom.SpeicherFlottenNetzBlock.Endbedingungen);

    /// <summary>Was am Ende des Planungshorizonts gelten soll.</summary>
    public FlottenEndbedingung Endbedingung
    {
        get => Optionen?.Endbedingung ?? FlottenEndbedingung.KeineVorgabe;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.Endbedingung = value; }
    }

    /// <summary>Darf der Lauf auf die reaktive Regel zurückfallen, wenn der Plan scheitert?</summary>
    public bool PrognoseFallbackErlaubt
    {
        get => Optionen?.PrognoseFallbackErlaubt == true;
        set { FlottenSimulationOptionen? o = Optionen; if (o is not null) o.PrognoseFallbackErlaubt = value; }
    }

    // =====================================================================
    //  Station 4 — der SUCHRAUM als Spalten (Welle KI-F6)
    // =====================================================================

    /// <summary>
    /// Die zwei Gerätequellen der Suchkarte — Begleiteigenschaft der Spalte
    /// <c>Suchachsen[].Quelle</c>.
    /// </summary>
    /// <remarks>
    /// <b>Sie steht HIER und nicht an der Zeile:</b> Die Maskenbrücke sucht die
    /// Begleiteigenschaft <c>&lt;Eigenschaft&gt;Wahl</c> immer am Daten-Objekt der
    /// Maske (<c>KiMaskenanmeldung.Wahlquelle</c>), auch für eine Spalte.
    /// </remarks>
    public IReadOnlyList<KiWahleintrag> QuelleWahl
        => Aufzaehlungswahl<FlottenKandidatenquelle>(
               EPOS.UI.Seiten.Strom.OptimierungBlock.Quellen);

    /// <summary>
    /// Je Suchachse eine Zeile — die Karte, die Station 4 für jede Einheit zeigt.
    /// </summary>
    /// <remarks>
    /// <b>Eine Achse je Einheit</b>: <c>SpeicherFlottenEditor.Normalisieren</c> hält
    /// das so, und die Karte trägt den Namen ihrer Einheit. Die GERÄTETABELLE der
    /// Karte bleibt draußen — sie ist eine gerechnete Anzeige
    /// (<c>FlottenGeraetewahl.Waehle</c>) und kein Eingabefeld.
    /// </remarks>
    public IReadOnlyList<FlottenAchseKiZeile> Suchachsen
    {
        get
        {
            List<FlottenAuslegungsAchse>? achsen = Auslegung?.Achsen;
            if (achsen is null) return Array.Empty<FlottenAchseKiZeile>();

            IReadOnlyList<FlottenEinheit> einheiten = Einheiten;
            var zeilen = new List<FlottenAchseKiZeile>(achsen.Count);
            for (int i = 0; i < achsen.Count; i++)
            {
                FlottenAuslegungsAchse a = achsen[i];
                string name = "";
                foreach (FlottenEinheit e in einheiten)
                    if (e.Id == a.ErsetztEinheitId) { name = e.Name; break; }

                zeilen.Add(new FlottenAchseKiZeile(a, i, name));
            }
            return zeilen;
        }
    }

    // =====================================================================
    //  Die Ketten — einmal hier und nirgends sonst
    // =====================================================================

    private SpeicherAuslegungKonfiguration? Konfiguration => _eingaben()?.Auslegung;

    private FlottenStudieKonfiguration? Flotte => _eingaben()?.Auslegung?.Flotte;

    private FlottenSimulationOptionen? Optionen => Flotte?.Optionen;

    private IReadOnlyList<FlottenEinheit> Einheiten
        => (IReadOnlyList<FlottenEinheit>?)Flotte?.Einheiten ?? Array.Empty<FlottenEinheit>();

    private FlottenAuslegungEingang? Auslegung => Flotte?.Auslegung;

    private FlottenKandidatZusammenfassung? BesterKandidat
        => _ergebnis()?.Auslegung?.BesterKandidat;

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

    private FlottenWirtschaftlichkeitEingang? Wirtschaft
    {
        get
        {
            FlottenStudieKonfiguration? f = Flotte;
            return f is null ? null : f.Wirtschaftlichkeit ??= new FlottenWirtschaftlichkeitEingang();
        }
    }

    /// <summary>Der Kostensatz, den die Maske im Investitionsblock ZEIGT.</summary>
    private SpeicherKostensaetze? InvestAnzeige
        => Konfiguration is not { } k
               ? null
               : k.Investitionsquelle == SpeicherKostenQuelle.Kostenmodul
                     ? k.VerwendeteKosten
                     : k.DirekteKosten;

    /// <summary>Der Kostensatz, den die Maske im Betriebsblock ZEIGT.</summary>
    private SpeicherKostensaetze? BetriebAnzeige
        => Konfiguration is not { } k
               ? null
               : k.Betriebsquelle == SpeicherKostenQuelle.Kostenmodul
                     ? k.VerwendeteKosten
                     : k.DirekteKosten;

    /// <summary>
    /// Schreibt in den DIALOGSATZ und setzt die Vorhandenmarke — wörtlich wie
    /// <c>SpeicherAuslegungEditor.InvestitionSetzen</c> bzw. <c>BetriebSetzen</c>.
    /// </summary>
    private void KostenSetzen(Action<SpeicherKostensaetze> schreiben, bool invest)
    {
        SpeicherAuslegungKonfiguration? k = Konfiguration;
        if (k is null) return;

        SpeicherKostensaetze satz = k.DirekteKosten ??= new SpeicherKostensaetze();
        schreiben(satz);
        if (invest) satz.InvestVorhanden = true; else satz.BetriebVorhanden = true;
    }

    /// <summary>
    /// Eine feste Klappliste der Maske als Wahleinträge — Schlüssel ist die Id, die
    /// die Maske führt.
    /// </summary>
    private static IReadOnlyList<KiWahleintrag> Quellenwahl(
        IReadOnlyList<(int Id, string Text)> liste)
    {
        var eintraege = new List<KiWahleintrag>(liste.Count);
        foreach ((int id, string text) in liste)
            eintraege.Add(new KiWahleintrag(id.ToString(CultureInfo.InvariantCulture), text));
        return eintraege;
    }

    /// <summary>
    /// Dasselbe für eine Klappliste, deren Zielfeld eine AUFZÄHLUNG ist: Der
    /// Schlüssel ist dann der NAME des Aufzählungswertes und nicht seine Zahl —
    /// dieselbe Bauart wie bei <see cref="BetriebszielWahl"/>.
    /// </summary>
    private static IReadOnlyList<KiWahleintrag> Aufzaehlungswahl<T>(
        IReadOnlyList<(int Id, string Text)> liste) where T : struct, Enum
    {
        var eintraege = new List<KiWahleintrag>(liste.Count);
        foreach ((int id, string text) in liste)
            eintraege.Add(new KiWahleintrag(((T)(object)id).ToString() ?? "", text));
        return eintraege;
    }

    private double Summe(Func<FlottenEinheit, double> welche)
    {
        double summe = 0.0;
        foreach (FlottenEinheit e in Einheiten) summe += welche(e);
        return summe;
    }
}

/// <summary>
/// EINE Speichereinheit der Flotte, wie der Einheiteneditor sie zeigt (Welle KI‑F6).
///
/// <para><b>Eine Hülle und kein zweites Modell.</b> Jede Eigenschaft schreibt
/// unmittelbar in die <see cref="FlottenEinheit"/> der offenen Maske; gehalten wird
/// nichts. Fünf Werte rechnet sie um: Die Engine führt Wirkungsgrade und SoC-Grenzen
/// als Anteil 0…1, die Maske zeigt Prozent.</para>
/// </summary>
public sealed class FlottenEinheitKiZeile
{
    private readonly FlottenEinheit _einheit;

    /// <summary>Legt die Zeile über die Einheit; <paramref name="platz"/> ist ihre Nummer.</summary>
    public FlottenEinheitKiZeile(FlottenEinheit einheit, int platz)
    {
        _einheit = einheit ?? throw new ArgumentNullException(nameof(einheit));
        Platz = platz;
    }

    /// <summary>Der Platz der Einheit in der Flotte (0-basiert).</summary>
    public int Platz { get; }

    /// <summary>
    /// Der Klartextname der Zeile — er steht im Feldblock statt „Kapazität 2".
    /// </summary>
    /// <remarks>
    /// Eine Einheit ohne Bezeichnung fällt auf ihre Nummer zurück, genau wie die
    /// Aufstellung der Ansicht (<see cref="StromspeicherKiSicht.EinheitenListe"/>).
    /// </remarks>
    public string Name
    {
        get => string.IsNullOrWhiteSpace(_einheit.Name)
                   ? (Platz + 1).ToString(CultureInfo.CurrentCulture)
                   : _einheit.Name;
        set => _einheit.Name = value ?? "";
    }

    /// <summary>Nennkapazität [kWh].</summary>
    public double Kapazitaet
    {
        get => _einheit.KapazitaetKWh;
        set => _einheit.KapazitaetKWh = value;
    }

    /// <summary>Höchste Ladeleistung [kW].</summary>
    public double Ladeleistung
    {
        get => _einheit.LadeleistungKw;
        set => _einheit.LadeleistungKw = value;
    }

    /// <summary>Höchste Entladeleistung [kW].</summary>
    public double Entladeleistung
    {
        get => _einheit.EntladeleistungKw;
        set => _einheit.EntladeleistungKw = value;
    }

    /// <summary>Ladewirkungsgrad [%].</summary>
    public double Ladewirkungsgrad
    {
        get => _einheit.Ladewirkungsgrad * 100.0;
        set => _einheit.Ladewirkungsgrad = value / 100.0;
    }

    /// <summary>Entladewirkungsgrad [%].</summary>
    public double Entladewirkungsgrad
    {
        get => _einheit.Entladewirkungsgrad * 100.0;
        set => _einheit.Entladewirkungsgrad = value / 100.0;
    }

    /// <summary>Untere Grenze des Ladezustands [%].</summary>
    public double SocMin
    {
        get => _einheit.SocMin * 100.0;
        set => _einheit.SocMin = value / 100.0;
    }

    /// <summary>Obere Grenze des Ladezustands [%].</summary>
    public double SocMax
    {
        get => _einheit.SocMax * 100.0;
        set => _einheit.SocMax = value / 100.0;
    }

    /// <summary>Ladezustand zu Beginn [%].</summary>
    public double SocStart
    {
        get => _einheit.SocStart * 100.0;
        set => _einheit.SocStart = value / 100.0;
    }

    /// <summary>Geschützte Peak-Reserve [kWh].</summary>
    public double PeakReserve
    {
        get => _einheit.PeakReserveKWh;
        set => _einheit.PeakReserveKWh = value;
    }

    /// <summary>AC-Hilfsverbrauch [kW].</summary>
    public double Hilfsverbrauch
    {
        get => _einheit.HilfsverbrauchKw;
        set => _einheit.HilfsverbrauchKw = value;
    }

    /// <summary>Marginale Verschleißkosten der Entladung [€/kWh].</summary>
    public double Grenzverschleiss
    {
        get => _einheit.GrenzverschleissEuroProKWhEntladung;
        set => _einheit.GrenzverschleissEuroProKWhEntladung = value;
    }

    /// <summary>Rechnet diese Einheit mit eigenen Kostensätzen?</summary>
    public bool EigeneKosten
    {
        get => _einheit.EigeneKosten;
        set => _einheit.EigeneKosten = value;
    }

    /// <summary>Feste Investition [€].</summary>
    public double InvestitionFix
    {
        get => _einheit.InvestitionEuro;
        set => _einheit.InvestitionEuro = value;
    }

    /// <summary>Kapazitätsbezogene Investition [€/kWh].</summary>
    public double InvestitionProKWh
    {
        get => _einheit.InvestitionEuroProKWh;
        set => _einheit.InvestitionEuroProKWh = value;
    }

    /// <summary>Leistungsbezogene Investition [€/kW].</summary>
    public double InvestitionProKw
    {
        get => _einheit.InvestitionEuroProKw;
        set => _einheit.InvestitionEuroProKw = value;
    }

    /// <summary>Feste Betriebskosten [€/a].</summary>
    public double BetriebFix
    {
        get => _einheit.JaehrlicheFixeOpexEuro;
        set => _einheit.JaehrlicheFixeOpexEuro = value;
    }

    /// <summary>Kapazitätsbezogene Betriebskosten [€/(kWh·a)].</summary>
    public double BetriebProKWh
    {
        get => _einheit.JaehrlicheOpexEuroProKWhKapazitaet;
        set => _einheit.JaehrlicheOpexEuroProKWhKapazitaet = value;
    }

    /// <summary>Leistungsbezogene Betriebskosten [€/(kW·a)].</summary>
    public double BetriebProKw
    {
        get => _einheit.JaehrlicheOpexEuroProKw;
        set => _einheit.JaehrlicheOpexEuroProKw = value;
    }

    /// <summary>Kosten je entladener Energie [€/kWh].</summary>
    public double Durchsatzkosten
    {
        get => _einheit.DurchsatzkostenEuroProKWhEntladung;
        set => _einheit.DurchsatzkostenEuroProKWhEntladung = value;
    }

    /// <summary>Ersatzkosten [€].</summary>
    public double Ersatzkosten
    {
        get => _einheit.ErsatzkostenEuro;
        set => _einheit.ErsatzkostenEuro = value;
    }

    /// <summary>Ersatzintervall [a].</summary>
    public int Ersatzintervall
    {
        get => _einheit.ErsatzintervallJahre;
        set => _einheit.ErsatzintervallJahre = value;
    }

    /// <summary>Restwert der Einheit am Ende der Laufzeit [€].</summary>
    public double Restwert
    {
        get => _einheit.RestwertEuro;
        set => _einheit.RestwertEuro = value;
    }
}

/// <summary>
/// EINE Suchachse der Station „Optimierung" (Welle KI‑F6) — die Karte, die dort je
/// Einheit steht.
///
/// <para><b>Eine Hülle wie <see cref="FlottenEinheitKiZeile"/>.</b> Sie schreibt
/// unmittelbar in die <see cref="FlottenAuslegungsAchse"/> der offenen Maske; den
/// KARTENNAMEN bekommt sie von der Einheit, die die Achse ersetzt — genau so
/// beschriftet der Block seine Karten.</para>
/// </summary>
public sealed class FlottenAchseKiZeile
{
    private readonly FlottenAuslegungsAchse _achse;
    private readonly string _einheitenname;

    /// <summary>Legt die Zeile über die Achse.</summary>
    public FlottenAchseKiZeile(FlottenAuslegungsAchse achse, int platz, string einheitenname)
    {
        _achse = achse ?? throw new ArgumentNullException(nameof(achse));
        _einheitenname = einheitenname ?? "";
        Platz = platz;
    }

    /// <summary>Der Platz der Achse (0-basiert).</summary>
    public int Platz { get; }

    /// <summary>Der Klartextname der Karte — Anzeige, kein Eingabefeld.</summary>
    public string Achse => string.IsNullOrWhiteSpace(_einheitenname)
                               ? (Platz + 1).ToString(CultureInfo.CurrentCulture)
                               : _einheitenname;

    /// <summary>Nimmt diese Achse an der Suche teil?</summary>
    public bool Variieren
    {
        get => _achse.Aktiv;
        set => _achse.Aktiv = value;
    }

    /// <summary>Woher die Geräte kommen, unter denen die Größensuche wählt.</summary>
    /// <remarks>
    /// Die Einträge dazu stehen an der SICHT (<c>StromspeicherKiSicht.QuelleWahl</c>)
    /// und nicht hier: Die Maskenbrücke sucht die Begleiteigenschaft eines Wahlfeldes
    /// immer am Daten-Objekt der Maske, auch bei einer Spalte.
    /// </remarks>
    public FlottenKandidatenquelle Quelle
    {
        get => _achse.Quelle;
        set => _achse.Quelle = value;
    }

    /// <summary>Kleinste Kapazität des Suchbereichs [kWh].</summary>
    public double KapazitaetVon
    {
        get => _achse.KapazitaetVonKWh;
        set => _achse.KapazitaetVonKWh = value;
    }

    /// <summary>Größte Kapazität des Suchbereichs [kWh].</summary>
    public double KapazitaetBis
    {
        get => _achse.KapazitaetBisKWh;
        set => _achse.KapazitaetBisKWh = value;
    }

    /// <summary>Kleinste Leistung des Suchbereichs [kW].</summary>
    public double LeistungVon
    {
        get => _achse.LeistungVonKw;
        set => _achse.LeistungVonKw = value;
    }

    /// <summary>Größte Leistung des Suchbereichs [kW].</summary>
    public double LeistungBis
    {
        get => _achse.LeistungBisKw;
        set => _achse.LeistungBisKw = value;
    }

    /// <summary>Kleinste Stückzahl der Achse.</summary>
    public int AnzahlVon
    {
        get => _achse.AnzahlVon;
        set => _achse.AnzahlVon = value;
    }

    /// <summary>Größte Stückzahl der Achse.</summary>
    public int AnzahlBis
    {
        get => _achse.AnzahlBis;
        set => _achse.AnzahlBis = value;
    }
}
