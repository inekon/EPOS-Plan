using System;
using System.Collections.Generic;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Waermepumpe;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// Die sprachneutralen Schlüssel der DREI Schritte des Simulationsablaufs
/// (Auftrag #207, Anwenderentscheid <b>SIM‑Q1</b>).
///
/// <para><b>Zwei Blätter und ein Knopf.</b> ① und ③ sind Blätter der
/// <c>Ablaufleiste</c> und stehen deshalb hier; ② ist der RECHENKNOPF der Leiste
/// und hat keinen Blattschlüssel — ein Lauf ist kein Blatt, das man ansieht
/// (dasselbe Muster wie Schritt 4 der Stromspeicher-Auslegung).</para>
/// </summary>
public static class SimulationSchritt
{
    /// <summary>Schritt ① — die <see cref="SimulationKonfigSeite"/>.</summary>
    public const string Konfiguration = "KONFIGURATION";

    /// <summary>Schritt ③ — die <see cref="SimulationErgebnisSeite"/>.</summary>
    public const string Ergebnis = "ERGEBNIS";
}

/// <summary>
/// Der ANZEIGENAME eines Reiterblatts von Schritt ③ (Auftrag #221).
///
/// <para><b>Wozu.</b> Die Ansicht meldet ihren Hilfekontext nach oben — Name, Schritt
/// und offenes Reiterblatt —, und der Assistent nennt ihn in seiner Kontextzeile. Was
/// die Ergebnisseite dabei herausgibt, ist der sprachneutrale SCHLÜSSEL
/// (<c>STROMSPEICHER</c>); was der Anwender liest, ist die Beschriftung des Reiters.</para>
///
/// <para><b>Die Beschriftungen sind dieselben Ressourcen</b>, die
/// <c>SimulationErgebnisSeite</c> an ihre neun <c>Reiterblatt</c> gibt — und deshalb
/// hält ein Wächter (<c>EPOS.UI.Tests</c>) beide Listen gegeneinander: Jeder Schlüssel
/// aus <c>SimulationErgebnisSeite.Blatt</c> muss hier einen Namen haben.</para>
/// </summary>
public static class SimulationBlattnamen
{
    /// <summary>Der Anzeigename zum Blattschlüssel; ein unbekannter liefert Leer.</summary>
    public static string Text(string? schluessel) => schluessel switch
    {
        SimulationErgebnisSeite.Blatt.Uebersicht    => Resource.SIMERG_TAB_UEBERSICHT,
        SimulationErgebnisSeite.Blatt.Bedarf        => Resource.SIMERG_TAB_BEDARF,
        SimulationErgebnisSeite.Blatt.Waermepumpe   => Resource.SIM_ERZEUGERNAME_WAERMEPUMPE,
        SimulationErgebnisSeite.Blatt.Heizkessel    => Resource.SIM_ERZEUGERNAME_HEIZKESSEL,
        SimulationErgebnisSeite.Blatt.Solarthermie  => Resource.SIM_ERZEUGERNAME_SOLARTHERMIE,
        SimulationErgebnisSeite.Blatt.Bhkw          => Resource.SIM_ERZEUGERNAME_BHKW,
        SimulationErgebnisSeite.Blatt.Photovoltaik  => Resource.SIM_PHOTOVOLTAIK,
        SimulationErgebnisSeite.Blatt.Stromspeicher => Resource.SIM_STROMSPEICHER,
        SimulationErgebnisSeite.Blatt.Ergebnis      => Resource.SIM_ERGEBNIS,
        _                                           => ""
    };
}

/// <summary>
/// Die MARKE einer Simulationsansicht — ein kurzer Text der Form
/// <c>"schritt=3;blatt=STROMSPEICHER"</c> (Konzept „Simulationsablauf" 2.1).
///
/// <para><b>Warum ein Text und kein Datensatz.</b> Die Marke geht durch den
/// Rückwegstapel der <c>AppWurzel</c> und durch
/// <c>Dienste.Navigation.OeffneMaske(Masken.Simulation, marke)</c> — beide
/// tragen Zeichenketten. Mehr als Schritt und Reiterblatt stellt die Wurzel
/// ohnehin nicht wieder her (Regel aus #199: „die Wurzel stellt die Ansicht
/// wieder her, nicht den inneren Zustand einer Komponente").</para>
/// </summary>
public static class SimulationMarke
{
    /// <summary>Der Schritt ① als Marke.</summary>
    public const string SCHRITT_KONFIGURATION = "schritt=1";

    /// <summary>
    /// Der Schritt ② als Marke — <b>„öffnen UND rechnen"</b> (Anwenderentscheid
    /// <b>SIM‑E‑1</b> vom 11.09.2026, Windows-Abnahme #216).
    /// </summary>
    /// <remarks>
    /// ② ist kein Blatt, sondern der Rechenknopf; eine Marke darauf heisst deshalb
    /// nicht „zeige Schritt 2", sondern „tu, was der Knopf tut". Die Ansicht prüft
    /// dabei DIESELBE Sperre wie der Knopf: Ist sie gesetzt, bleibt sie bei ① und
    /// nennt den Grund — eine Marke ist ein WUNSCH, kein Befehl.
    /// </remarks>
    public const string SCHRITT_LAUF = "schritt=2";

    /// <summary>Der Schritt ③ als Marke.</summary>
    public const string SCHRITT_ERGEBNIS = "schritt=3";

    /// <summary>
    /// Die WIRTKENNUNG des Startseiten-Reiters „Simulation" (Auftrag <b>#220</b>,
    /// Anwenderentscheid <b>SIM‑E‑2</b>, Punkt 4).
    /// </summary>
    /// <remarks>
    /// <para>Seit #220 zeigt derselbe Schritt ③ an ZWEI Stellen — als Blatt der
    /// Ansicht <c>SIMULATION</c> und in der rechten Spalte des Startseiten-Reiters.
    /// Der Rückwegstapel muss beide auseinanderhalten können: Wer die
    /// Stromspeicher-Auslegung aus dem REITER heraus geöffnet hat, will in den Reiter
    /// zurück und nicht in die Ansicht.</para>
    /// <para>Deshalb trägt die Marke der Startseite zusätzlich
    /// <c>wirt=START</c> — <c>"wirt=START;schritt=3;blatt=STROMSPEICHER"</c>. Ohne
    /// Wirtkennung meint eine Marke wie bisher die Ansicht.</para>
    /// </remarks>
    public const string WIRT_START = "START";

    /// <summary>
    /// Liest Schritt und Reiterblatt aus einer Marke. Unbekanntes bleibt leer
    /// bzw. 0 — eine Marke ist ein WUNSCH, kein Befehl, und die Seite entscheidet
    /// selbst, ob sie ihn erfüllen kann (ohne Ergebnis fällt sie auf ① zurück).
    /// </summary>
    public static (int Schritt, string Blatt) Lesen(string? marke)
    {
        int schritt = 0;
        string blatt = "";
        if (string.IsNullOrWhiteSpace(marke)) return (schritt, blatt);

        foreach (string stueck in marke.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int gleich = stueck.IndexOf('=');
            if (gleich <= 0) continue;

            string name = stueck.Substring(0, gleich).Trim();
            string wert = stueck.Substring(gleich + 1).Trim();

            if (string.Equals(name, "schritt", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(wert, out int zahl)) schritt = zahl;
            }
            else if (string.Equals(name, "blatt", StringComparison.OrdinalIgnoreCase))
            {
                blatt = wert;
            }
        }

        return (schritt, blatt);
    }

    /// <summary>
    /// Liest die WIRTKENNUNG einer Marke; leer = keine, dann meint sie die Ansicht
    /// (Auftrag #220).
    /// </summary>
    public static string WirtLesen(string? marke)
    {
        if (string.IsNullOrWhiteSpace(marke)) return "";

        foreach (string stueck in marke.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int gleich = stueck.IndexOf('=');
            if (gleich <= 0) continue;

            if (string.Equals(stueck.Substring(0, gleich).Trim(), "wirt",
                              StringComparison.OrdinalIgnoreCase))
                return stueck.Substring(gleich + 1).Trim();
        }

        return "";
    }

    /// <summary>Schreibt eine Marke; ein leeres Blatt bleibt weg.</summary>
    public static string Schreiben(int schritt, string? blatt)
        => string.IsNullOrEmpty(blatt) ? "schritt=" + schritt
                                       : "schritt=" + schritt + ";blatt=" + blatt;

    /// <summary>
    /// Schreibt eine Marke MIT Wirtkennung (Auftrag #220); ein leerer Wirt schreibt
    /// dieselbe Marke wie <see cref="Schreiben(int, string?)"/>.
    /// </summary>
    public static string Schreiben(string? wirt, int schritt, string? blatt)
        => string.IsNullOrEmpty(wirt) ? Schreiben(schritt, blatt)
                                      : "wirt=" + wirt + ";" + Schreiben(schritt, blatt);
}

/// <summary>
/// Die DATENSEITE der Ansicht „Simulation" (Auftrag #207, Stufe S1).
///
/// <para><b>Sie trägt keine eigenen Daten</b>, sondern die zwei fertigen
/// Parametersätze der bestehenden Seiten und zwei Auskünfte, die die Ablaufleiste
/// braucht. Die Fachwege selbst bleiben, wo sie sind: in
/// <c>SimulationKonfigDienste</c> und <c>SimulationErgebnisDienste</c>. Unter
/// Windows legt <c>Views/Simulation/SimulationHuelle.cs</c> das Bündel an und hält
/// die zwei Hülleninstanzen zwischen zwei Besuchen — der gerechnete Lauf, die
/// Bilder und die Gültigkeitsmarke überleben damit einen Ansichtswechsel.</para>
///
/// <para><b>Kein Delegat ist kein Knopf</b> (Hausregel der Auslegungsansicht):
/// Fehlt ein Eintrag, blendet die Ansicht die zugehörige Bedienung aus, statt sie
/// gesperrt oder wirkungslos zu zeigen.</para>
/// </summary>
public sealed class SimulationAnsichtDienste
{
    /// <summary>
    /// Der Schlüssel, unter dem die Quelle dieses Bündel in ihren Parametersatz legt
    /// (<c>SimulationAnsichtQuelle.AnsichtGaben</c>).
    /// </summary>
    public const string PARAMETER = "Dienste";

    /// <summary>
    /// Liest das Bündel aus einem Parametersatz der Simulation (Auftrag <b>#220</b>);
    /// <c>null</c> = der Satz fehlt oder führt keines.
    /// </summary>
    /// <remarks>
    /// <b>Wozu.</b> Seit #220 bedient sich ein ZWEITER Wirt aus demselben Satz — der
    /// Startseiten-Reiter „Simulation" (SIM‑E‑2, Punkt 3). Die Ansicht bekommt ihn als
    /// <c>@attributes</c> und damit über den Blazor-Verteiler; der Reiter braucht nur
    /// das Bündel. Damit der Schlüsselname an EINER Stelle steht, liest ihn diese
    /// Methode — nicht die <c>Startseite</c>.
    /// </remarks>
    public static SimulationAnsichtDienste? Aus(IReadOnlyDictionary<string, object>? gaben)
    {
        if (gaben is null) return null;
        return gaben.TryGetValue(PARAMETER, out object? wert)
                   ? wert as SimulationAnsichtDienste
                   : null;
    }

    /// <summary>
    /// Der Parametersatz von Schritt ① (<see cref="SimulationKonfigSeite"/>);
    /// <c>null</c> = die Plattform bietet ihn nicht an.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Konfiguration;

    /// <summary>
    /// Der Parametersatz von Schritt ③ (<see cref="SimulationErgebnisSeite"/>);
    /// <c>null</c> = die Plattform bietet ihn nicht an.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Ergebnis;

    /// <summary>
    /// Der Grund, aus dem der Lauf gar nicht erst starten kann — die ROTE
    /// Vorprüfung. Leer = frei. Unter Windows ist das die Sperre aus ADR‑001
    /// (nicht abgeschlossene Schema-Migration); der Text steht am gesperrten
    /// Rechenknopf als <c>title</c>.
    /// </summary>
    public Func<string>? Sperrgrund;

    /// <summary>
    /// Liegt ein gerechneter Lauf vor? Ohne ihn ist Schritt ③ gesperrt.
    /// </summary>
    /// <remarks>
    /// <b>Nicht dasselbe wie „das Ergebnis ist gültig".</b> Wer aus der
    /// Stromspeicher-Auslegung zurückkommt, hat einen gerechneten Lauf UND ein
    /// veraltetes Ergebnis (der Nachzug setzt <c>_ergebnisGueltig = false</c>) —
    /// er soll ③ betreten dürfen und dort das Banner „Flotte geändert" lesen.
    /// Deshalb fragt diese Auskunft nach dem LAUF und nicht nach der Gültigkeit.
    /// </remarks>
    public Func<bool>? ErgebnisVorhanden;

    /// <summary>
    /// Die fünf LAUFPARAMETER, die seit Auftrag <b>#216</b> in Schritt ① stehen;
    /// <c>null</c> = die Plattform bietet sie nicht an, dann zeigt ① sie nicht.
    /// </summary>
    public SimulationParameterDienste? Parameter;

    /// <summary>
    /// Der ZULETZT GELADENE Stand von Schritt ③ — das, was die Ergebnisseite gerade
    /// zeigt; <c>null</c> = sie hat noch nichts geladen (Auftrag #221).
    /// </summary>
    /// <remarks>
    /// <para><b>Wozu.</b> Die Ansicht meldet ihre Felder beim Hilfe-Assistenten an
    /// (KI‑D‑E‑1), und zu ihnen gehören die Kennzahlen des Laufs. Sie stehen fertig in
    /// <see cref="SimulationErgebnisDaten"/>; ein eigener Ladeweg dafür wäre ein
    /// ZWEITER Stand derselben Zahlen — und jede Leseanfrage des Assistenten ein
    /// weiterer Datenbankzugriff.</para>
    /// <para><b>Es ist kein neuer Datenweg</b>, sondern derselbe: Die Hülle gibt her,
    /// was sie beim letzten <c>SimulationErgebnisDienste.Laden</c> ohnehin gebaut hat.</para>
    /// </remarks>
    public Func<SimulationErgebnisDaten?>? Ergebnisstand;

    /// <summary>
    /// Die gemeinsame Sperre <b>„ein Lauf zur Zeit"</b> (Auftrag <b>#220</b>,
    /// Anwenderentscheid <b>SIM‑E‑2</b>, Punkt 5); <c>null</c> = keine Sperre.
    /// </summary>
    /// <remarks>
    /// Seit #220 stoßen ZWEI Wirte denselben Lauf an — Schritt ② der Ansicht und die
    /// Kachel des Startseiten-Reiters. Die Quelle legt EINE Sperre je Projekt an und
    /// gibt sie jedem Parametersatz mit; damit sperren sich die beiden gegenseitig,
    /// ohne voneinander zu wissen.
    /// </remarks>
    public SimulationLaufsperre? Laufsperre;
}

/// <summary>
/// Die fünf Laufparameter des gefallenen Reiters „Parameter" — Netzverluste,
/// BHKW-Betriebsart, untere Leistungsgrenze, Heizstab und Betriebsbereitschaft
/// (Windows-Abnahme <b>#216</b> vom 11.09.2026, Punkt 3: „Nimm Parameter heraus").
///
/// <para><b>Kein zweiter Schreibweg.</b> Die fünf Delegaten sind DIESELBEN, die
/// bis #216 der Reiter „Parameter" von <c>SimulationErgebnisDienste</c> bekam —
/// unter Windows legt sie <c>SimulationHuelle</c> aus der
/// <c>SimulationErgebnisHuelle</c> ein (<c>KonfigSchreiben</c>,
/// <c>BetriebsartSchreiben</c>). Die Werte landen damit in denselben Spalten von
/// <c>Tab_Einstellungen</c>, und die Hülle, die den Lauf bestückt, kennt sie
/// unverändert. Ein eigener Weg über die Konfigurationshülle wäre ein zweiter
/// Stand derselben Zahlen.</para>
///
/// <para><b>Die Netzverluste schreiben sofort</b> — dieselbe Hausregel wie im
/// abgelösten Reiter (W11b‑B‑29). Die DREI ERZEUGERWERTE schreiben seit dem
/// Anwenderwunsch vom 16.09.2026 im OK-Weg des
/// <c>KomponentenKonfigurationDialog</c>: An der Erzeugerkarte steht nur noch sein
/// Knopf, und ein Dialog mit Abbrechen darf nichts vorher geschrieben haben.
/// Dieselben Delegaten, derselbe Zielort — nur ein anderer Auslöser.</para>
/// </summary>
public sealed class SimulationParameterDienste
{
    /// <summary>Liest den Stand der fünf Werte; <c>null</c> = keiner.</summary>
    public Func<ParameterDaten>? Laden;

    /// <summary>Wert und Einheit der Netzwärmeverluste.</summary>
    public Action<double, string>? NetzverlusteSchreiben;

    /// <summary>0 = wärmegeführt, 1 = stromgeführt, 2 = ohne Einspeisung.</summary>
    public Action<int>? BetriebsartSchreiben;

    /// <summary>Die projektweite untere Modulationsgrenze der BHKW-Module [%].</summary>
    public Action<int>? LeistungsgrenzeSchreiben;

    // 16.09.2026 (Auftrag #299): Hier stand "HeizstabSchreiben" - der Schreibweg des
    // PROJEKTweiten Heizstabschalters. Der Heizstab gehört seither der WÄRMEPUMPE; er
    // geht über WaermepumpeKonfigurationSpeichern mit den übrigen Anlagenfeldern.

    /// <summary>Die Betriebsbereitschaft des Heizkessels [h/a].</summary>
    public Action<double>? BereitschaftSchreiben;

    /// <summary>
    /// Die Projekteinstellung „Kühlung rechnen" (Stufe KU1, Kühlkonzept 8.3) — schreibt
    /// SOFORT, wie die Netzverluste, und meldet, ob danach der gewünschte Wert steht
    /// (<c>KonfigurationCtrl.KuehlbetriebSetzen</c>). <c>null</c> = die Plattform bietet
    /// den Schalter nicht an; dann steht der Abschnitt „Kühlung" nicht da.
    /// </summary>
    public Func<bool, bool>? KuehlbetriebSchreiben;

    /// <summary>
    /// Die Projekteinstellung „Anlagenkopplung" (Konzept Anlagenkopplung 9.4, AK1 Welle 3) —
    /// schreibt SOFORT wie der Kühlschalter und meldet, ob danach die gewünschte Stufe steht
    /// (<c>KonfigurationCtrl.AnlagenkopplungSetzen</c>); der Parameter ist der Steuerwert,
    /// <c>null</c> = aus. <c>null</c> als Delegat = die Plattform bietet die Wahl nicht an; dann
    /// steht der Abschnitt „Anlagenkopplung" nicht da.
    /// </summary>
    public Func<string?, bool>? AnlagenkopplungSchreiben;

    // =====================================================================
    //  Die Konfiguration EINER Wärmepumpen-Anlage (Anwenderwunsch 16.09.2026)
    // =====================================================================
    //
    // Der Konfigurationsdialog der Karte zeigt bei der Wärmepumpe nicht nur die
    // projektweite Heizstab-Einstellung, sondern die Konfiguration DIESER Anlage:
    // elektrische Nachheizung, Sperrzeit, bivalenter Betrieb, Energieträger. Sie
    // steht in Tab_Energieanlagen und nicht in Tab_Einstellungen — deshalb ein
    // eigener Lese- und Schreibweg statt eines weiteren Schalters.
    //
    // DIE DREI SIND OPTIONAL. Wo sie fehlen (iOS, Proben), zeigt der Dialog bei der
    // Wärmepumpe allein die PROJEKTEINSTELLUNG; der Knopf bleibt stehen, denn die
    // gibt es überall. Still fällt nichts aus — der Dialog sagt an seiner
    // Herleitungszeile, was er zeigt.

    /// <summary>
    /// Die Anlagendaten EINER Wärmepumpe (<c>Tab_Energieanlagen.ID</c> der
    /// Kartenzeile) als frische ARBEITSKOPIE; <c>null</c> = kein Weg oder keine
    /// Anlage dieser Id.
    /// </summary>
    public Func<int, WaermepumpeAnlageDaten?>? WaermepumpeKonfigurationLaden;

    /// <summary>
    /// Schreibt die Konfigurationsfelder der Anlage zurück
    /// (<c>Tab_Energieanlagen.ID</c>, Feldsatz) und meldet das Ergebnis BENANNT.
    /// </summary>
    /// <remarks>
    /// <para><b>Warum nicht <c>bool</c></b> (bis 16.09.2026): Der Kern lehnt einen
    /// Schreibversuch mit einem SATZ ab — „Die Anlage 42 wurde nicht gefunden",
    /// „Die Konfiguration der Anlage konnte nicht gespeichert werden" —, und ein
    /// <c>false</c> warf ihn weg. Die Seite zeigte dafür ihren eigenen Allgemeinplatz,
    /// und der Grund stand nirgends. Jetzt reicht die Naht den Wortlaut des Kerns
    /// durch, und die Seite meldet ihn.</para>
    /// </remarks>
    public Func<int, WaermepumpeAnlageDaten, AnlagenkonfigErgebnis>? WaermepumpeKonfigurationSpeichern;

    /// <summary>
    /// Der Trägerkatalog der Energieträgerwahl; <c>null</c> = keine Wahl. Er wird
    /// beim Öffnen des Dialogs EINMAL gerufen, nicht je Zeichenlauf.
    /// </summary>
    public Func<IReadOnlyList<EnergietraegerWahl.Eintrag>>? WaermepumpeTraegerkatalog;

    /// <summary>
    /// Die Kühlgaben der Wärmepumpen-Konfiguration (Stufe KU2 Welle 3; Kühlkonzept 8.2) —
    /// Stützstellen, Sperrgrund und die Stromträger des Projekts; <c>null</c> = keine Gruppe
    /// „Kühlbetrieb". Wie der Trägerkatalog beim Öffnen EINMAL gerufen.
    /// </summary>
    public Func<EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlGaben?>? WaermepumpeKuehlGaben;

    // KEIN Weg fuer die TEXTE: Das Buendel WaermepumpeKonfigurationTexte fuellt sich
    // selbst aus MyResource in der Oberflaechensprache - die Huelle muss nichts
    // beisteuern, und ein Delegat dafuer waere eine Naht ohne Gegenueber.
}

/// <summary>
/// Was ein Schreibversuch an der Anlagenkonfiguration ergeben hat — das
/// plattformfreie Abbild von <c>WErzeugerCtrl.SpeicherErgebnis</c> (16.09.2026).
/// </summary>
/// <remarks>
/// Eine Razor-Seite kennt die Fachklassen des Kerns nicht; der Record trägt deshalb
/// genau die zwei Angaben, die sie braucht. Den <c>Name</c> des Kern-Ergebnisses führt
/// er nicht: Der Bezeichner ändert sich auf diesem Weg nie, und die Karte, an der die
/// Meldung erscheint, trägt ihn ohnehin.
/// </remarks>
/// <param name="Ok">Wurde geschrieben?</param>
/// <param name="Meldung">Der Grund im Klartext, bereits lokalisiert.</param>
public sealed record AnlagenkonfigErgebnis(bool Ok, string Meldung);
