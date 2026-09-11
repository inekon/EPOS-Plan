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

    /// <summary>Legt die Sicht über die vier lebenden Stände der Ansicht.</summary>
    /// <param name="konfiguration">Der Stand von Schritt ①; <c>null</c> = ① steht nicht.</param>
    /// <param name="laufparameter">Der Stand der fünf Laufparameter; <c>null</c> = keiner.</param>
    /// <param name="schreibwege">Die fünf Schreibwege; <c>null</c> = nichts ist setzbar.</param>
    /// <param name="ergebnis">Der Stand von Schritt ③; <c>null</c> = es wurde nicht gerechnet.</param>
    /// <param name="schritt">Der Name des stehenden Schritts, in der Oberflächensprache.</param>
    /// <param name="reiter">Der Name des offenen Reiterblatts; leer in ①.</param>
    public SimulationKiSicht(Func<SimulationKonfigDaten?> konfiguration,
                             Func<ParameterDaten?> laufparameter,
                             Func<SimulationParameterDienste?> schreibwege,
                             Func<SimulationErgebnisDaten?> ergebnis,
                             Func<string> schritt,
                             Func<string> reiter)
    {
        _konfiguration = konfiguration ?? throw new ArgumentNullException(nameof(konfiguration));
        _laufparameter = laufparameter ?? throw new ArgumentNullException(nameof(laufparameter));
        _schreibwege = schreibwege ?? throw new ArgumentNullException(nameof(schreibwege));
        _ergebnis = ergebnis ?? throw new ArgumentNullException(nameof(ergebnis));
        _schritt = schritt ?? throw new ArgumentNullException(nameof(schritt));
        _reiter = reiter ?? throw new ArgumentNullException(nameof(reiter));
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

    /// <summary>Rechnet die Wärmepumpe mit Heizstab?</summary>
    public bool WpHeizstab
    {
        get => Parameter?.Heizstab ?? false;
        set
        {
            ParameterDaten? p = Parameter;
            if (p is null) return;
            p.Heizstab = value;
            Wege?.HeizstabSchreiben?.Invoke(value);
        }
    }

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
}
