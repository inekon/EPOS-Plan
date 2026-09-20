using System.Globalization;
using System.Text;

namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// Das FLACHE Abbild der Ansicht „Simulation" für den Hilfe-Assistenten (Auftrag #221,
/// sechste Deklaration des Dialogkatalogs).
///
/// <para><b>Warum die Ansicht überhaupt Felder anmeldet.</b> Bis hierher unterschied
/// sich der Assistent der Simulationsansicht von dem des Hauptfensters in genau einer
/// Zeichenkette — dem Bereich aus dem Hilfeschlüssel. Der Anwender hat das am
/// 11.09.2026 benannt: „Die KI-Buttons haben keine unterschiedliche Funktion im
/// Kontext." Ein Kontext, der nur aus einem Namen besteht, ist keiner; er wird einer,
/// sobald der Assistent die Kaskade, die fünf Laufparameter und die Kennzahlen des
/// Laufs LESEN kann.</para>
///
/// <para><b>Warum ein eigenes Sichtmodell und nicht die DTO selbst</b> (Muster
/// <c>StromspeicherKiSicht</c>, #200): Ein Maskenfeld des Katalogs ist genau ZWEI
/// Stufen tief (<c>Typ.Eigenschaft</c>, <c>KiEigenschaftspfad</c>); was die Ansicht
/// zeigt, liegt drei bis vier Stufen tief
/// (<c>Ergebnis.Kennzahlen.WaermebedarfGesamtMwh</c>) und verteilt über VIER Stände —
/// die Konfiguration, die fünf Laufparameter, das Ergebnis und die Ansicht selbst
/// (Schritt und Reiter). Diese Klasse löst die Ketten an EINER benannten Stelle auf.</para>
///
/// <para><b>Sie hält keinen Zustand.</b> Jede Eigenschaft rechnet bei jedem Zugriff aus
/// den Delegaten; die Ansicht tauscht ihre Stände bei jedem Auffrischen aus, und ein
/// festgehaltenes Objekt zeigte dem Assistenten den Stand von vorhin.</para>
///
/// <para><b>Elf Eigenschaften sind ABGELEITET und ohne Setzer</b> — die Kaskade, der
/// Schritt, der Reiter, die sieben Kennzahlen des Laufs und die Laufhinweise. Das ist
/// die Aussage, nicht eine Nachlässigkeit: Sie lassen sich nicht eingeben, und
/// <c>feld_setzen</c> lehnt sie deshalb ab (<c>KiFeldzugang.Setzbar</c> folgt der
/// Schreibbarkeit der Eigenschaft). Setzbar sind genau die FÜNF Laufparameter — und
/// sie gehen denselben Weg wie das Feld auf dem Bildschirm
/// (<see cref="SimulationParameterDienste"/>, jedes Feld schreibt sofort).</para>
/// </summary>
public sealed class SimulationKiSicht
{
    private readonly Func<SimulationKonfigDaten?> _konfiguration;
    private readonly Func<ParameterDaten?> _laufparameter;
    private readonly Func<SimulationParameterDienste?> _schreibwege;
    private readonly Func<SimulationErgebnisDaten?> _ergebnis;
    private readonly Func<string> _schritt;
    private readonly Func<string> _reiter;
    private readonly Func<SimulationErgebnisDienste?>? _speicherwege;
    private readonly Func<SimulationKonfigDienste?>? _konfigwege;

    /// <summary>Legt die Sicht über die lebenden Stände der Ansicht.</summary>
    /// <param name="konfiguration">Der Stand von Schritt ①; <c>null</c> = ① steht nicht.</param>
    /// <param name="laufparameter">Der Stand der vier Laufparameter; <c>null</c> = keiner.</param>
    /// <param name="schreibwege">Die vier Schreibwege; <c>null</c> = nichts ist setzbar.</param>
    /// <param name="ergebnis">Der Stand von Schritt ③; <c>null</c> = es wurde nicht gerechnet.</param>
    /// <param name="schritt">Der Name des stehenden Schritts, in der Oberflächensprache.</param>
    /// <param name="reiter">Der Name des offenen Reiterblatts; leer in ①.</param>
    /// <param name="speicherwege">
    /// Der Schreibdienst des Reiters „Stromspeicher" (Welle KI‑F2). <c>null</c> = das Blatt
    /// steht nicht oder die Plattform stellt ihn nicht; dann bleiben die einundzwanzig
    /// Speicherfelder lesbar, und eine Setzung läuft ins Leere statt in die Datenbank.
    /// </param>
    /// <param name="konfigwege">
    /// Der Schreibdienst von Schritt ① (Welle KI‑F2) — er trägt den Lesepunkt des
    /// Wärmepumpen-Kennfelds. <c>null</c> = ① steht nicht.
    /// </param>
    public SimulationKiSicht(Func<SimulationKonfigDaten?> konfiguration,
                             Func<ParameterDaten?> laufparameter,
                             Func<SimulationParameterDienste?> schreibwege,
                             Func<SimulationErgebnisDaten?> ergebnis,
                             Func<string> schritt,
                             Func<string> reiter,
                             Func<SimulationErgebnisDienste?>? speicherwege = null,
                             Func<SimulationKonfigDienste?>? konfigwege = null)
    {
        _konfiguration = konfiguration ?? throw new ArgumentNullException(nameof(konfiguration));
        _laufparameter = laufparameter ?? throw new ArgumentNullException(nameof(laufparameter));
        _schreibwege = schreibwege ?? throw new ArgumentNullException(nameof(schreibwege));
        _ergebnis = ergebnis ?? throw new ArgumentNullException(nameof(ergebnis));
        _schritt = schritt ?? throw new ArgumentNullException(nameof(schritt));
        _reiter = reiter ?? throw new ArgumentNullException(nameof(reiter));
        _speicherwege = speicherwege;
        _konfigwege = konfigwege;
    }

    // =====================================================================
    //  Wo der Anwender steht (nur lesend)
    // =====================================================================

    /// <summary>Der stehende Schritt der Ablaufleiste — „Konfiguration" oder „Ergebnis".</summary>
    public string Ansichtsschritt => _schritt() ?? "";

    /// <summary>
    /// Das offene Reiterblatt von Schritt ③ („Übersicht", „Stromspeicher", …); leer,
    /// solange ① vorn steht.
    /// </summary>
    public string Reiter => _reiter() ?? "";

    // =====================================================================
    //  Schritt ① — Kaskade und Reihenfolge (nur lesend)
    // =====================================================================

    /// <summary>
    /// Die AUFGENOMMENEN Erzeuger in Kaskadenreihenfolge, je Gruppe eine Aufzählung
    /// („Wärmeerzeuger: 1. BHKW 1, 2. Heizkessel"); leer, solange nichts aufgenommen ist.
    /// </summary>
    /// <remarks>
    /// <para><b>Ein Maskenfeld trägt EINEN Wert</b> (<c>KiDialogFeld</c>), eine Kaskade
    /// aber beliebig viele Plätze, deren Zahl erst zur Laufzeit feststeht. Die
    /// Aufstellung geht deshalb als TEXT hinaus — genau so, wie die Ansicht sie zeigt;
    /// dieselbe Regel wie bei <c>StromspeicherKiSicht.EinheitenListe</c>.</para>
    /// <para><b>Nicht aufgenommene Karten stehen NICHT darin.</b> Sie sind der Grund
    /// vieler Fragen („warum liefert mein Kessel nichts?"), aber sie sind keine
    /// Kaskade; sie stehen in <see cref="NichtAufgenommen"/>.</para>
    /// </remarks>
    public string Kaskade
    {
        get
        {
            SimulationKonfigDaten? d = _konfiguration();
            if (d is null) return "";

            var sb = new StringBuilder();

            foreach (KachelGruppe gruppe in d.Gruppen)
            {
                var teile = new List<string>();

                foreach (ErzeugerZeile zeile in gruppe.Zeilen)
                {
                    if (zeile.Verfuegbar) continue;

                    string rang = (zeile.Kachel.Rang ?? "").Trim();
                    string name = string.IsNullOrWhiteSpace(zeile.Kachel.Titel)
                        ? zeile.DbWert
                        : zeile.Kachel.Titel;

                    teile.Add(rang.Length == 0 ? name : rang + " " + name);
                }

                if (teile.Count == 0) continue;

                if (sb.Length > 0) sb.Append("; ");
                sb.Append(gruppe.Titel).Append(": ").Append(string.Join(", ", teile));
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Die Erzeuger, die das Projekt FÜHRT, die aber auf keinem Platz der Simulation
    /// stehen; leer = keine.
    /// </summary>
    /// <remarks>
    /// Sie beantworten die häufigste Rückfrage zu einem Lauf: Eine angelegte Anlage
    /// ohne Kaskadenplatz rechnet nicht, und die Übersicht zeigt dafür 0,00 (#190).
    /// </remarks>
    public string NichtAufgenommen
    {
        get
        {
            SimulationKonfigDaten? d = _konfiguration();
            if (d is null) return "";

            var teile = new List<string>();

            foreach (KachelGruppe gruppe in d.Gruppen)
                foreach (ErzeugerZeile zeile in gruppe.Zeilen)
                {
                    if (!zeile.Verfuegbar || !zeile.HatAnlage) continue;

                    teile.Add(string.IsNullOrWhiteSpace(zeile.Bezeichner)
                                  ? zeile.DbWert
                                  : zeile.Bezeichner);
                }

            return string.Join(", ", teile);
        }
    }

    // =====================================================================
    //  Die FÜNF Laufparameter (lesbar und setzbar)
    // =====================================================================

    /// <summary>Die Netzwärmeverluste [%] — sie wirken nur bei vorhandenem Wärmebedarf.</summary>
    public double Netzverluste
    {
        get => Parameter?.Netzverluste ?? 0.0;
        set
        {
            ParameterDaten? p = Parameter;
            if (p is null) return;
            p.Netzverluste = value;
            Wege?.NetzverlusteSchreiben?.Invoke(value, p.NetzverlusteEinheit);
        }
    }

    /// <summary>Die Betriebsart der BHKW: 0 = wärmegeführt, 1 = stromgeführt, 2 = ohne Einspeisung.</summary>
    public int BhkwBetriebsart
    {
        get => Parameter?.Betriebsart ?? 0;
        set
        {
            ParameterDaten? p = Parameter;
            if (p is null) return;
            p.Betriebsart = value;
            Wege?.BetriebsartSchreiben?.Invoke(value);
        }
    }

    /// <summary>Die projektweite untere Modulationsgrenze der BHKW-Module [%].</summary>
    public int BhkwLeistungsgrenze
    {
        get => Parameter?.UntersteLeistungsgrenze ?? 0;
        set
        {
            ParameterDaten? p = Parameter;
            if (p is null) return;
            p.UntersteLeistungsgrenze = value;
            Wege?.LeistungsgrenzeSchreiben?.Invoke(value);
        }
    }

    // 16.09.2026 (Auftrag #299): Hier stand "WpHeizstab" - das KI-Feld wp_heizstab der
    // Simulationsmaske. Der Heizstab ist kein Laufparameter des Projekts mehr, sondern
    // ein Feld JE WÄRMEPUMPE (Tab_Energieanlagen.Heizstab); die Maske führt ihn deshalb
    // nicht mehr, und KiDialoge.Katalog nennt ihn nicht mehr.

    /// <summary>Die Betriebsbereitschaft des Heizkessels [h/a].</summary>
    public double KesselBereitschaft
    {
        get => Parameter?.Bereitschaft ?? 0.0;
        set
        {
            ParameterDaten? p = Parameter;
            if (p is null) return;
            p.Bereitschaft = value;
            Wege?.BereitschaftSchreiben?.Invoke(value);
        }
    }

    /// <summary>
    /// Der Lesepunkt des Wärmepumpen-Kennfelds: <c>true</c> = vor dem Speicher ablesen
    /// (Welle KI‑F2).
    /// </summary>
    /// <remarks>
    /// Der Schalter steht in der Fußzeile von Schritt ① und nur dort, wo er überhaupt
    /// etwas bedeutet (<c>BoosterSichtbar</c>). Sein Schreibweg ist derselbe wie am
    /// Schalter: <c>SimulationKonfigDienste.LesepunktSchreiben</c>; er meldet, ob die
    /// Einstellung angekommen ist, und nur dann zieht der Stand nach.
    /// </remarks>
    public bool LesepunktDavor
    {
        get => _konfiguration()?.BoosterDavor ?? false;
        set
        {
            SimulationKonfigDaten? d = _konfiguration();
            if (d is null) return;
            if (_konfigwege?.Invoke()?.LesepunktSchreiben?.Invoke(value) == true)
                d.BoosterDavor = value;
        }
    }

    // =====================================================================
    //  Der Reiter „Stromspeicher" — die Parameter der aktiven Variante
    //  (Welle KI-F2)
    // =====================================================================
    //
    // WARUM SIE HIER STEHEN UND NICHT IN EINER EIGENEN MASKE. Der Block ist ein
    // BLATT der Ansicht "Simulation" und kein Dialog: Er geht nicht auf, er steht
    // auf dem Reiter "Stromspeicher" von Schritt ③. Eine Maske ist, was offen ist -
    // und offen ist die Simulationsansicht.
    //
    // JEDES FELD SCHREIBT SOFORT, ueber denselben Weg wie das Feld auf dem
    // Bildschirm (SpeicherParameterBlock.Schreiben -> SpeicherfeldSchreiben je
    // Feldschluessel). Erst merken, dann schreiben - dieselbe Reihenfolge; sonst
    // stuende in der Maske eine andere Zahl als in der Datenbank.

    /// <summary>Der untere Ladezustand des Bandes [%].</summary>
    public double SpeicherSocMin
    {
        get => Speicher?.SoCMinProzent ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.SoCMin, value, s => s.SoCMinProzent = value);
    }

    /// <summary>Der obere Ladezustand des Bandes [%].</summary>
    public double SpeicherSocMax
    {
        get => Speicher?.SoCMaxProzent ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.SoCMax, value, s => s.SoCMaxProzent = value);
    }

    /// <summary>Lade- und Entladeleistung der Speicheranlage [kW].</summary>
    public double SpeicherLadeleistung
    {
        get => Speicher?.LadeleistungKw ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Leistung, value, s => s.LadeleistungKw = value);
    }

    /// <summary>Die Nennkapazität der Speicheranlage [kWh].</summary>
    public double SpeicherKapazitaet
    {
        get => Speicher?.KapazitaetKwh ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Kapazitaet, value, s => s.KapazitaetKwh = value);
    }

    /// <summary>Die Preisschwelle, unter der geladen wird [ct/kWh].</summary>
    public double SpeicherLadeschwelle
    {
        get => Speicher?.Ladeschwellwert ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Ladeschwelle, value, s => s.Ladeschwellwert = value);
    }

    /// <summary>Der Steuerwert der Betriebsart (nicht ihr Anzeigetext).</summary>
    public string SpeicherBetriebsart
    {
        get => Speicher?.Betriebsart ?? "";
        set => SpeicherWahl(SpeicherFeld.Betriebsart, value, s => s.Betriebsart = value ?? "");
    }

    /// <summary>Der Steuerwert der Berechnungsart (nicht ihr Anzeigetext).</summary>
    public string SpeicherBerechnungsart
    {
        get => Speicher?.Berechnungsart ?? "";
        set => SpeicherWahl(SpeicherFeld.Berechnungsart, value, s => s.Berechnungsart = value ?? "");
    }

    /// <summary>Die Zielschwelle der Lastspitzenkappung [kW]; 0 heißt „nicht gepflegt".</summary>
    public double SpeicherPeakZiel
    {
        get => Speicher?.PeakZiel ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.PeakZiel, value, s => s.PeakZiel = value);
    }

    /// <summary>Zieht die Zielschwelle sich selbst nach?</summary>
    public bool SpeicherPeakZielAdaptiv
    {
        get => Speicher?.PeakZielAdaptiv ?? false;
        set => SpeicherSchalter(SpeicherFeld.PeakZielAdaptiv, value, s => s.PeakZielAdaptiv = value);
    }

    /// <summary>Der Kompatibilitätsmodus der Dauernutzung.</summary>
    public bool SpeicherKompatibilitaet
    {
        get => Speicher?.Kompatibilitaet ?? false;
        set => SpeicherSchalter(SpeicherFeld.Kompatibilitaet, value, s => s.Kompatibilitaet = value);
    }

    /// <summary>Laden aus dem Überschuss der Photovoltaik.</summary>
    public bool SpeicherLadenAusPv
    {
        get => Speicher?.LadenAusPv ?? false;
        set => SpeicherSchalter(SpeicherFeld.LadenPv, value, s => s.LadenAusPv = value);
    }

    /// <summary>Laden aus dem Überschuss der BHKW.</summary>
    public bool SpeicherLadenAusBhkw
    {
        get => Speicher?.LadenAusBhkw ?? false;
        set => SpeicherSchalter(SpeicherFeld.LadenBhkw, value, s => s.LadenAusBhkw = value);
    }

    /// <summary>Entladen in das Netz erlaubt.</summary>
    public bool SpeicherNetzentladung
    {
        get => Speicher?.Netzentladung ?? false;
        set => SpeicherSchalter(SpeicherFeld.Netzentladung, value, s => s.Netzentladung = value);
    }

    /// <summary>
    /// Stromgeführter BHKW-Betrieb — sichtbar, aber dauerhaft gesperrt (Ausbaustufe 11).
    /// </summary>
    public bool SpeicherBhkwStromgefuehrt => Speicher?.BhkwStromgefuehrt ?? false;

    /// <summary>Der Kalkulationszins der Speicherwirtschaftlichkeit [%].</summary>
    public double SpeicherKapitalzins
    {
        get => Speicher?.Kapitalzins ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Kapitalzins, value, s => s.Kapitalzins = value);
    }

    /// <summary>Die Nutzungsdauer der Speicheranlage [a].</summary>
    public double SpeicherNutzungsdauer
    {
        get => Speicher?.Nutzungsdauer ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Nutzungsdauer, value, s => s.Nutzungsdauer = value);
    }

    /// <summary>Der Leistungspreis des Netzbetreibers [€/(kW·a)].</summary>
    public double SpeicherLeistungspreis
    {
        get => Speicher?.Leistungspreis ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Leistungspreis, value, s => s.Leistungspreis = value);
    }

    /// <summary>Der Aufschlag auf netzgeladene Energie [ct/kWh].</summary>
    public double SpeicherNetzladeaufschlag
    {
        get => Speicher?.Netzladeaufschlag ?? 0.0;
        set => SpeicherZahl(SpeicherFeld.Netzladeaufschlag, value, s => s.Netzladeaufschlag = value);
    }

    /// <summary>Der Steuerwert der Preisquelle (nicht ihr Anzeigetext).</summary>
    public string SpeicherPreisquelle
    {
        get => Speicher?.Preisquelle ?? "";
        set => SpeicherWahl(SpeicherFeld.Preisquelle, value, s => s.Preisquelle = value ?? "");
    }

    /// <summary>Den Aufschlag auf die Preisreihe anwenden.</summary>
    public bool SpeicherAufschlag
    {
        get => Speicher?.Aufschlag ?? false;
        set => SpeicherSchalter(SpeicherFeld.Aufschlag, value, s => s.Aufschlag = value);
    }

    // =====================================================================
    //  Schritt ③ — die Kennzahlen des Laufs (nur lesend)
    // =====================================================================

    /// <summary>Der Wärmebedarf des Projekts [MWh/a].</summary>
    public double WaermebedarfMwh => Kennzahlen?.WaermebedarfGesamtMwh ?? 0.0;

    /// <summary>Die Wärmedeckung des Laufs [%] — der Ring der Übersicht.</summary>
    public double WaermedeckungProzent => Uebersicht?.WaermedeckungProzent ?? 0.0;

    /// <summary>Der REST, der nach allen Erzeugern ungedeckt bleibt [MWh/a].</summary>
    public double RestwaermeMwh => Kennzahlen?.RestwaermeMwh ?? 0.0;

    /// <summary>Der Strombedarf des Projekts [MWh/a].</summary>
    public double StrombedarfMwh => Kennzahlen?.StrombedarfGesamtMwh ?? 0.0;

    /// <summary>Die Stromdeckung des Laufs [%] — der zweite Ring der Übersicht.</summary>
    public double StromdeckungProzent => Uebersicht?.StromdeckungProzent ?? 0.0;

    /// <summary>Der ungedeckte Reststrombedarf [MWh/a].</summary>
    public double ReststromMwh => Kennzahlen?.ReststromMwh ?? 0.0;

    /// <summary>Die Entladung des Stromspeichers im Lauf [MWh/a]; 0 ohne Speicher.</summary>
    public double SpeicherentladungMwh => Kennzahlen?.StromspeicherEntladungMwh ?? 0.0;

    /// <summary>
    /// Das SoC-Band des Stromspeichers („20 – 90 %"); leer, wenn das Projekt keine
    /// aktive Speichervariante führt.
    /// </summary>
    public string SpeicherSocBand
    {
        get
        {
            SpeicherParameterDaten? s = _ergebnis()?.Parameter?.Speicher;
            if (s is null || !s.VarianteVorhanden) return "";

            var k = CultureInfo.CurrentCulture;
            return s.SoCMinProzent.ToString("0.#", k) + " – " +
                   s.SoCMaxProzent.ToString("0.#", k) + " %";
        }
    }

    /// <summary>
    /// Die Warnungen und Hinweise des letzten Laufs; leer = keine.
    /// </summary>
    /// <remarks>
    /// Sie tragen die Kennungen, zu denen <c>HilfeWissen</c> seinen Aktionswissen-
    /// Abschnitt führt (<c>LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ</c>, …) — der Assistent
    /// findet die Erklärung damit auch ohne Modell.
    /// </remarks>
    public string Laufhinweise => _ergebnis()?.Laufmeldungen ?? "";

    // =====================================================================
    //  Die Auflösung der Ketten — jede null-Stufe hat einen Grund
    // =====================================================================

    /// <summary>
    /// Der Stand der fünf Laufparameter. <c>null</c> = die Plattform bietet sie nicht
    /// an; dann bleiben die fünf Felder bei ihren Vorgaben und sind nicht setzbar.
    /// </summary>
    private ParameterDaten? Parameter => _laufparameter();

    /// <summary>Die fünf Schreibwege; <c>null</c> = die Plattform schreibt sie nicht.</summary>
    private SimulationParameterDienste? Wege => _schreibwege();

    /// <summary>
    /// Die 13 Zahlen und sechs Summen des Laufs; <c>null</c> = es wurde nicht
    /// gerechnet, und die sieben Kennzahlen stehen auf 0.
    /// </summary>
    private WindowsFormsApplication1.SimulationErgebnisCtrl.UebersichtKennzahlen? Kennzahlen
        => _ergebnis()?.Kennzahlen;

    /// <summary>Präsenz und Ringmittelwerte; <c>null</c> = es wurde nicht gerechnet.</summary>
    private UebersichtDaten? Uebersicht => _ergebnis()?.Uebersicht;

    /// <summary>
    /// Der Stand des Reiters „Stromspeicher" — dasselbe Objekt, das
    /// <c>SpeicherParameterBlock</c> bearbeitet; <c>null</c> = es wurde nicht geladen.
    /// </summary>
    private SpeicherParameterDaten? Speicher => _ergebnis()?.Parameter?.Speicher;

    /// <summary>
    /// Merkt eine Speicherzahl im Stand und schreibt sie — in dieser Reihenfolge und
    /// INVARIANT, wie <c>SpeicherParameterBlock.Zahl</c>.
    /// </summary>
    private void SpeicherZahl(string feld, double wert, Action<SpeicherParameterDaten> merken)
        => SpeicherSetzen(feld, wert.ToString(CultureInfo.InvariantCulture), merken);

    /// <summary>Merkt einen Speicherschalter und schreibt ihn als 0/1.</summary>
    private void SpeicherSchalter(string feld, bool wert, Action<SpeicherParameterDaten> merken)
        => SpeicherSetzen(feld, wert ? "1" : "0", merken);

    /// <summary>
    /// Merkt einen Steuerwert und schreibt ihn wörtlich. <b>Ein Wert, den die Liste der
    /// Maske nicht führt, wird abgewiesen</b> — die Klappliste kennt genau diese
    /// Steuerwerte, und ein erfundener stünde danach in der Datenbank, ohne dass ihn
    /// jemand wieder auswählen könnte.
    /// </summary>
    private void SpeicherWahl(string feld, string? wert, Action<SpeicherParameterDaten> merken)
    {
        SpeicherParameterDaten? s = Speicher;
        if (s is null || wert is null) return;

        IReadOnlyList<Steuerwahl> liste = feld switch
        {
            SpeicherFeld.Betriebsart => s.Betriebsarten,
            SpeicherFeld.Berechnungsart => s.Berechnungsarten,
            _ => s.Preisquellen
        };

        bool bekannt = false;
        foreach (Steuerwahl wahl in liste)
            if (string.Equals(wahl.Wert, wert, StringComparison.Ordinal)) bekannt = true;

        if (!bekannt) return;

        SpeicherSetzen(feld, wert, merken);
    }

    private void SpeicherSetzen(string feld, string wert, Action<SpeicherParameterDaten> merken)
    {
        SpeicherParameterDaten? s = Speicher;
        if (s is null) return;

        merken(s);
        _speicherwege?.Invoke()?.SpeicherfeldSchreiben?.Invoke(feld, wert);
    }
}
