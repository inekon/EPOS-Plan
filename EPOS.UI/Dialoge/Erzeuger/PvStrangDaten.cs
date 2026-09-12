using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Die Farbe einer Ampelzeile im Abschnitt „Wechselrichter und Stränge"
/// (<c>Konzept_Wechselrichter_EPOS-Plan.md</c> 4.2, Stufe S2).
///
/// <para><b>Warum ein eigener Aufzählungstyp und nicht der des Kerns.</b>
/// <c>StrangPlausibilitaet.Ampel</c> hängt an einer Prüfklasse mit
/// Kern-Fachmodellen; die Oberfläche bekommt ihre Daten „ausschliesslich als
/// <c>[Parameter]</c>" (<c>EPOS.UI/CLAUDE.md</c>). Drei Werte und eine
/// Abbildungszeile in der Hülle sind der billigere Preis als eine Fachklasse im
/// Markup.</para>
/// </summary>
public enum Ampelfarbe
{
    /// <summary>Alle anwendbaren Prüfungen bestanden.</summary>
    Gruen,

    /// <summary>Eine weiche Prüfung verletzt oder eine Angabe fehlt.</summary>
    Gelb,

    /// <summary>P1, P2 oder P4 verletzt — die Auslegung ist so nicht zulässig.</summary>
    Rot
}

/// <summary>
/// EINE Zeile der Strangtabelle im PV-Dialog (Stufe S2, Konzept 3.4 und 7).
///
/// <para><b>Sie wird IN PLACE geändert</b> — wie <see cref="ErzeugerZeile"/>, zu der
/// sie gehört. Die Hülle hält die Liste und schreibt sie beim Übernehmen in
/// <c>Z_AnlageStrang</c>.</para>
///
/// <para><b>Der Wechselrichter steht als PROJEKTKOPIE darin</b>
/// (<see cref="WechselrichterId"/>), nicht als Katalogsatz: „Projekte KOPIEREN
/// Katalogsätze, alle persistierten Verweise zeigen auf die Projektkopie"
/// (<c>KatalogRegistry</c>). Die Klappliste zeigt den KATALOG; das Kopieren erledigt
/// die Hülle beim Übernehmen (<c>WechselrichterCtrl.CopyFromStamm</c>) und gibt
/// Id und Namen zurück. <see cref="WechselrichterName"/> ist dabei nicht nur Anzeige,
/// sondern das Band zur Klappliste: Katalogsatz und Projektkopie tragen denselben
/// <c>Bezeichner</c>, und genau darüber findet <c>CopyFromStamm</c> eine vorhandene
/// Kopie wieder.</para>
///
/// <para><b>Nullable, wo NULL etwas heisst</b> (Konzept 3.4): kein Gerät, Gerät 1,
/// MPPT 1, ein Strang parallel — und bei Neigung/Azimut „der Anlagenwert". Dort ist
/// die Unterscheidung tragend: <b>Azimut 0 ist eine GÜLTIGE Ausrichtung</b> (Süden).
/// Die Maske zeigt einen geerbten Wert deshalb in KLAMMERN und schreibt ihn nicht.</para>
/// </summary>
public sealed class StrangZeile
{
    /// <summary>Reihenfolge in der Tabelle, 1…n. Beim Speichern neu vergeben.</summary>
    public int Rang { get; set; }

    /// <summary>Freitext („Dach Süd"); leer = der Rang als Anzeige.</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Die PROJEKTKOPIE des Geräts (<c>Tab_Wechselrichter.ID</c>); 0 = keins.</summary>
    public int WechselrichterId { get; set; }

    /// <summary>Anzeigename des Geräts; leer = keins zugeordnet.</summary>
    public string WechselrichterName { get; set; } = "";

    /// <summary>
    /// Der ABWEICHENDE Modultyp dieses Strangs als PROJEKTKOPIE
    /// (<c>Tab_PV.ID</c> → <c>Z_AnlageStrang.ID_PV</c>); <b>0 = das Modul der
    /// Anlage</b> (Anwenderentscheid <b>W6‑O‑6</b> vom 06.09.2026: „jeder Strang mit
    /// nur einem Modultyp, unterschiedliche Stränge können jeweils einen anderen
    /// Modultyp haben").
    ///
    /// <para>Dieselbe Bauart wie <see cref="WechselrichterId"/>: Die Klappliste zeigt
    /// den KATALOG, die Zeile trägt die Projektkopie, und das Band zwischen beiden ist
    /// der <see cref="ModulName"/>.</para>
    /// </summary>
    public int ModulId { get; set; }

    /// <summary>Anzeigename des Moduls; leer = das Modul der Anlage.</summary>
    public string ModulName { get; set; } = "";

    /// <summary>Welches physische Gerät dieses Typs; <c>null</c> = 1.</summary>
    public int? Geraetenummer { get; set; }

    /// <summary>MPPT-Eingang dieses Geräts; <c>null</c> = 1.</summary>
    public int? Mppt { get; set; }

    /// <summary>Module in Reihe; <c>null</c> = noch nicht angegeben.</summary>
    public int? ModuleReihe { get; set; }

    /// <summary>Parallel geschaltete Stränge; <c>null</c> = 1.</summary>
    public int? StraengeParallel { get; set; }

    /// <summary>Neigung dieses Teilfelds [°]; <c>null</c> = der Anlagenwert.</summary>
    public int? Neigung { get; set; }

    /// <summary>Azimut dieses Teilfelds [°]; <c>null</c> = der Anlagenwert.</summary>
    public int? Azimut { get; set; }

    /// <summary>Module dieses Strangs (Reihe × parallel); 0 ohne Reihe.</summary>
    public int Modulzahl => (ModuleReihe ?? 0) <= 0 ? 0 : ModuleReihe!.Value * (StraengeParallel ?? 1);
}

/// <summary>
/// Eine Zeile der Ampel — Farbe und Satz, fertig formuliert (Konzept 4.2).
/// </summary>
/// <param name="Farbe">Grün, gelb oder rot.</param>
/// <param name="Satz">
/// Der Satz mit Zahlen. Er kommt FERTIG aus dem Kern
/// (<c>StrangPlausibilitaet</c>); die Komponente rechnet und formatiert nicht.
/// </param>
public sealed record Ampelzeile(Ampelfarbe Farbe, string Satz);

/// <summary>
/// Was der Prüfstand des Kerns zum aktuellen Stand der Tabelle sagt — das Ergebnis
/// des Delegaten <c>PvStraengeFelder.Pruefen</c>.
/// </summary>
/// <param name="Straenge">Je Strangzeile eine Ampelzeile, in Rangfolge.</param>
/// <param name="Geraete">
/// Je physischem Gerät eine Ampelzeile für den Kopf des Abschnitts — dort steht das
/// DC/AC-Verhältnis (Konzept 7).
/// </param>
/// <param name="Modulsumme">
/// Σ (Reihe × parallel) über alle Stränge — die ABGELEITETE „Anzahl Module"
/// (Entscheidungsfrage <b>Q9</b>).
/// </param>
/// <param name="Werkzeugtipp">
/// Die benannte Näherung von P2/P3 (<c>beta_OC</c> statt eines eigenen
/// MPP-Koeffizienten) als <c>title</c> der Ampel; leer = keiner.
/// </param>
public sealed record StrangBefund(
    IReadOnlyList<Ampelzeile> Straenge,
    IReadOnlyList<Ampelzeile> Geraete,
    int Modulsumme,
    string Werkzeugtipp = "")
{
    /// <summary>Der leere Befund — ohne Strangzeile gibt es nichts zu melden.</summary>
    public static readonly StrangBefund Leer =
        new(Array.Empty<Ampelzeile>(), Array.Empty<Ampelzeile>(), 0);
}

/// <summary>
/// EINE Zeile, die die Auslegungshilfe vorschlägt (Anwenderwunsch 08.09.2026,
/// <b>W6‑B‑8</b>) — die vier Zahlen, die eine <see cref="StrangZeile"/> ausmachen.
///
/// <para><b>Warum nicht gleich eine <see cref="StrangZeile"/>.</b> Der Vorschlag kommt
/// aus dem KERN (<c>StrangAuslegung.Aufteilen</c>) und weiß nichts von Projektkopien:
/// Welches Gerät die Zeilen tragen, entscheidet die Komponente beim Übernehmen des
/// Katalogsatzes, und Neigung und Azimut bleiben leer — sie heißen dann „der
/// Anlagenwert".</para>
/// </summary>
/// <param name="Geraetenummer">Welches physische Gerät, 1…n.</param>
/// <param name="Mppt">MPP-Tracker dieses Geräts, 1…m.</param>
/// <param name="ModuleReihe">Module in Reihe.</param>
/// <param name="StraengeParallel">Parallel geschaltete Stränge an diesem Tracker.</param>
public sealed record StrangVorgabe(int Geraetenummer, int Mppt, int ModuleReihe,
                                   int StraengeParallel);

/// <summary>
/// Was die Auslegungshilfe zu einem Katalogsatz sagt (<b>W6‑B‑8</b>) — das Ergebnis des
/// Delegaten <c>PvStraengeFelder.AuslegungVorschlagen</c>.
///
/// <para><b>Gerechnet hat der Kern</b> (<c>StrangAuslegung.Vorschlagen</c> und
/// <c>Aufteilen</c>), <b>formuliert die Hülle</b>: Die Komponente ersetzt bei
/// <paramref name="Moeglich"/> ihre Tabelle durch die <paramref name="Zeilen"/> und
/// zeigt den <paramref name="Satz"/>; sonst bleibt die Tabelle stehen, und der Satz
/// nennt den Grund.</para>
/// </summary>
/// <param name="Moeglich">Es gibt eine Aufteilung.</param>
/// <param name="Zeilen">Die vorgeschlagene Strangtabelle; leer, wenn nicht möglich.</param>
/// <param name="Satz">Der fertig formulierte Satz — Vorschlag oder Grund.</param>
public sealed record StrangVorschlag(bool Moeglich, IReadOnlyList<StrangVorgabe> Zeilen,
                                     string Satz)
{
    /// <summary>Kein Vorschlag und kein Satz — der Stand ohne Delegat.</summary>
    public static readonly StrangVorschlag Leer =
        new(false, Array.Empty<StrangVorgabe>(), "");
}

/// <summary>
/// Was der Knopf „Vorschlag aus Klimadaten übernehmen" anbietet (<b>W6‑B‑11</b>,
/// Anwenderentscheid vom 09.09.2026) — das Ergebnis des Delegaten
/// <c>PvStraengeFelder.TemperaturenVorschlagen</c>.
///
/// <para><b>Gerechnet hat der KERN</b> (<c>AuslegungstemperaturVorschlag.Fuer</c>):
/// kalt = Jahresminimum der Außentemperatur des Klimadatensatzes, heiß =
/// Zelltemperatur bei 1 000 W/m² aus dem Jahresmaximum und <c>T_NOCT</c> des
/// Anlagenmoduls. Die Komponente trägt die zwei Zahlen in ihre Felder und zeigt den
/// <paramref name="Satz"/> als Herleitung — sie rechnet nichts.</para>
///
/// <para><b>Übernommen wird nur per Knopf</b>: Die Auslegungstemperatur ist eine
/// Entscheidung des Planers, keine Ableitung (Begründung im Kern).</para>
/// </summary>
/// <param name="Moeglich">Die Klimadaten lieferten eine Reihe.</param>
/// <param name="Kalt">Vorgeschlagene Auslegungstemperatur kalt [°C].</param>
/// <param name="Heiss">Vorgeschlagene Auslegungstemperatur heiß [°C].</param>
/// <param name="Satz">Die Herleitung — oder der Grund, wenn es keinen Vorschlag gibt.</param>
public sealed record Temperaturvorschlag(bool Moeglich, double Kalt, double Heiss, string Satz)
{
    /// <summary>Kein Vorschlag und kein Satz — der Stand ohne Delegat.</summary>
    public static readonly Temperaturvorschlag Leer = new(false, 0.0, 0.0, "");
}

/// <summary>
/// Das Ergebnis des Übernehmens eines Katalogsatzes in das Projekt
/// (<c>WechselrichterCtrl.CopyFromStamm</c>).
/// </summary>
/// <param name="Id">Die Projektkopie; 0 = nicht übernommen.</param>
/// <param name="Name">Ihr Bezeichner.</param>
public sealed record GeraetWahl(int Id, string Name);

/// <summary>
/// Die Beschriftungen des Abschnitts „Wechselrichter und Stränge" — EIN Bündel statt
/// dreissig <c>[Parameter] string</c> (Hausregel <c>EPOS.UI/CLAUDE.md</c>: „ab etwa
/// zehn Anzeigetexten ein BÜNDEL").
///
/// <para><b>Es füllt sich SELBST aus <c>MyResource</c></b> — Bauart
/// <c>LizenzTexte</c>: Es sind reine Katalogeinträge ohne Fallunterscheidung. Ein
/// fehlender Schlüssel fällt auf den deutschen Wortlaut zurück, damit die Komponente
/// auch ohne Ressourcen zeichnet (Regel für Kachel- und Menüziele).</para>
/// </summary>
public sealed class PvStrangTexte
{
    private static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); } catch { /* ohne Katalog */ }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    /// <summary>Überschrift des Abschnitts — <c>PVS_ABSCHNITT</c>.</summary>
    public string Abschnitt { get; set; } = T("PVS_ABSCHNITT", "Wechselrichter und Stränge");

    /// <summary>Titel der Optionsgruppe — <c>PVS_WAHL</c>.</summary>
    public string Wahl { get; set; } = T("PVS_WAHL", "Wechselrichter");

    /// <summary>Option „vereinfacht" — <c>PVS_OPT_VEREINFACHT</c>.</summary>
    public string OptionVereinfacht { get; set; } =
        T("PVS_OPT_VEREINFACHT", "vereinfacht — Pauschalen ohne Wechselrichter");

    /// <summary>Option „mit Wechselrichter" — <c>PVS_OPT_KATALOG</c>.</summary>
    public string OptionKatalog { get; set; } =
        T("PVS_OPT_KATALOG", "mit Wechselrichter — Katalog, Stränge, Kennlinie, Clipping");

    /// <summary>Die Zeile im Weg „vereinfacht" — <c>PVS_HINWEIS_VEREINFACHT</c>, mit {0} = Wirkungsgrad.</summary>
    public string HinweisVereinfacht { get; set; } =
        T("PVS_HINWEIS_VEREINFACHT", "Die Anlage rechnet mit dem Wirkungsgrad {0} und ohne Clipping.");

    /// <summary>
    /// Die Zeile, wenn „mit Wechselrichter" gewählt, aber kein Strang angelegt ist —
    /// <c>PVS_HINWEIS_OHNE_STRANG</c>. Seit <b>W6‑B‑3</b> nennt sie den nächsten
    /// Handgriff, statt nur einen Mangel zu melden: Der Anwender steht in genau diesem
    /// Zustand, sobald er den Weg wählt.
    /// </summary>
    public string HinweisOhneStrang { get; set; } =
        T("PVS_HINWEIS_OHNE_STRANG",
          "Noch kein Strang: Gerät oben wählen und „Strang anlegen“ drücken.");

    /// <summary>
    /// Beschriftung der KATALOGWAHL über der Tabelle — <c>PVS_LBL_GERAETEWAHL</c>
    /// (<b>W6‑B‑3</b>). Sie gilt dem nächsten Strang; jede bestehende Zeile hat ihre
    /// eigene Klappliste in der Spalte „Wechselrichter".
    /// </summary>
    public string LabelGeraetewahl { get; set; } =
        T("PVS_LBL_GERAETEWAHL", "Wechselrichter aus dem Katalog:");

    /// <summary>Die Herleitung unter der Katalogwahl — <c>PVS_HERLEITUNG_GERAETEWAHL</c>.</summary>
    public string HerleitungGeraetewahl { get; set; } =
        T("PVS_HERLEITUNG_GERAETEWAHL",
          "Das gewählte Gerät bekommt der nächste Strang. In der Tabelle lässt es sich je Strang ändern. Gerät und MPPT sind Nummern, leer heißt 1: Stränge mit derselben Gerätenummer hängen an EINEM Wechselrichter; ein zweiter Wechselrichter desselben Typs bekommt Gerät 2.");

    /// <summary>
    /// Der Weg zum Import, wenn der Wechselrichterkatalog LEER ist —
    /// <c>PVS_HINWEIS_KATALOG_LEER</c> (<b>W6‑B‑3</b>). Der Auslieferungskatalog ist
    /// leer (W6‑O‑3); ohne diesen Satz stünde der Abschnitt stumm da.
    /// </summary>
    public string HinweisKatalogLeer { get; set; } =
        T("PVS_HINWEIS_KATALOG_LEER",
          "Der Wechselrichterkatalog ist leer. Geräte einlesen über: " +
          "Administration → Daten & Import → Photovoltaik → „Wechselrichter (CEC, OND)…“.");

    /// <summary>
    /// Die Zeile neben dem Rückfallknopf im Weg „mit Wechselrichter" —
    /// <c>PVS_HERLEITUNG_RUECKFALL</c> (<b>W6‑B‑3</b>). Sie sagt, dass hinter dem Knopf
    /// die PAUSCHALEN stehen und nicht die Katalogwahl.
    /// </summary>
    public string HerleitungRueckfall { get; set; } =
        T("PVS_HERLEITUNG_RUECKFALL",
          "Die Anlagenwerte sind der Rückfall des vereinfachten Weges — sie rechnen nur ohne Strangzuordnung.");

    /// <summary>Spaltenkopf „Rang" — <c>PVS_SP_RANG</c>.</summary>
    public string SpalteRang { get; set; } = T("PVS_SP_RANG", "Rang");

    /// <summary>Spaltenkopf „Bezeichner" — <c>PVS_SP_BEZEICHNER</c>.</summary>
    public string SpalteBezeichner { get; set; } = T("PVS_SP_BEZEICHNER", "Bezeichner");

    /// <summary>Spaltenkopf „Wechselrichter" — <c>PVS_SP_WECHSELRICHTER</c>.</summary>
    public string SpalteWechselrichter { get; set; } = T("PVS_SP_WECHSELRICHTER", "Wechselrichter");

    /// <summary>Spaltenkopf „Modul" — <c>PVS_SP_MODUL</c> (W6‑O‑6).</summary>
    public string SpalteModul { get; set; } = T("PVS_SP_MODUL", "Modul");

    /// <summary>Klapplisteneintrag „(Modul der Anlage)" — <c>PVS_MODUL_ANLAGE</c>.</summary>
    public string ModulDerAnlage { get; set; } = T("PVS_MODUL_ANLAGE", "(Modul der Anlage)");

    /// <summary>Die Herleitung unter der Tabelle — <c>PVS_HERLEITUNG_MODUL</c>.</summary>
    public string HerleitungModul { get; set; } =
        T("PVS_HERLEITUNG_MODUL",
          "Leer heisst: der Strang rechnet mit dem Modul der Anlage. Ein eigener Modultyp gilt nur für diesen Strang.");

    /// <summary>Spaltenkopf „Gerät" — <c>PVS_SP_GERAET</c>.</summary>
    public string SpalteGeraet { get; set; } = T("PVS_SP_GERAET", "Gerät");

    /// <summary>Spaltenkopf „MPPT" — <c>PVS_SP_MPPT</c>.</summary>
    public string SpalteMppt { get; set; } = T("PVS_SP_MPPT", "MPPT");

    /// <summary>Spaltenkopf „Module in Reihe" — <c>PVS_SP_REIHE</c>.</summary>
    public string SpalteReihe { get; set; } = T("PVS_SP_REIHE", "Module in Reihe");

    /// <summary>Spaltenkopf „Stränge parallel" — <c>PVS_SP_PARALLEL</c>.</summary>
    public string SpalteParallel { get; set; } = T("PVS_SP_PARALLEL", "Stränge parallel");

    /// <summary>Spaltenkopf „Neigung [°]" — <c>PVS_SP_NEIGUNG</c>.</summary>
    public string SpalteNeigung { get; set; } = T("PVS_SP_NEIGUNG", "Neigung [°]");

    /// <summary>Spaltenkopf „Azimut [°]" — <c>PVS_SP_AZIMUT</c>.</summary>
    public string SpalteAzimut { get; set; } = T("PVS_SP_AZIMUT", "Azimut [°]");

    /// <summary>Klapplisteneintrag „(kein Gerät)" — <c>PVS_KEIN_GERAET_WAHL</c>.</summary>
    public string KeinGeraet { get; set; } = T("PVS_KEIN_GERAET_WAHL", "(kein Gerät)");

    /// <summary>
    /// Filterzeile ÜBER der Strangtabelle — <c>PVS_FILTER_HERSTELLER</c>
    /// (Anwenderentscheid <b>W6‑O‑4</b> vom 06.09.2026: „Hersteller kann vom Modul
    /// verschieden sein. Herstellerfilter etc. wie in Modulliste einfügen").
    /// </summary>
    public string FilterHersteller { get; set; } =
        T("PVS_FILTER_HERSTELLER", "Filtern nach Hersteller:");

    /// <summary>
    /// Die Herleitung unter der Filterzeile, wenn die Vorauswahl GETROFFEN hat —
    /// <c>PVS_HERLEITUNG_FILTER</c>, mit {0} = Hersteller des Anlagenmoduls
    /// (Anwenderentscheid <b>W6‑E‑6</b> vom 07.09.2026).
    /// </summary>
    public string HerleitungFilter { get; set; } =
        T("PVS_HERLEITUNG_FILTER",
          "Vorauswahl: Hersteller des Moduls ({0}). Jeder Hersteller ist wählbar.");

    /// <summary>
    /// Die Herleitung, wenn der Wechselrichterkatalog KEIN Gerät des Modulherstellers
    /// führt — <c>PVS_HERLEITUNG_FILTER_OHNE</c>, mit {0} = Hersteller des Anlagenmoduls.
    /// </summary>
    public string HerleitungFilterOhne { get; set; } =
        T("PVS_HERLEITUNG_FILTER_OHNE",
          "Kein Wechselrichter des Modulherstellers ({0}) im Katalog — der Filter steht auf „Alle“. Jeder Hersteller ist wählbar.");

    /// <summary>
    /// Die Herleitung ohne bekannten Modulhersteller — <c>PVS_HERLEITUNG_FILTER_FREI</c>.
    /// Es bleibt der Satz, der ohnehin der wichtigere ist: Der Filter sperrt nichts.
    /// </summary>
    public string HerleitungFilterFrei { get; set; } =
        T("PVS_HERLEITUNG_FILTER_FREI", "Jeder Hersteller ist wählbar.");

    /// <summary>
    /// Beschriftung der Zeile „Auslegungstemperaturen" — <c>PVS_LBL_TEMPERATUREN</c>
    /// (<b>W6‑B‑11</b>).
    /// </summary>
    public string LabelTemperaturen { get; set; } =
        T("PVS_LBL_TEMPERATUREN", "Auslegungstemperaturen:");

    /// <summary>Beschriftung des kalten Falls — <c>PVS_LBL_T_KALT</c>.</summary>
    public string LabelTKalt { get; set; } = T("PVS_LBL_T_KALT", "kalt [°C]");

    /// <summary>Beschriftung des heißen Falls — <c>PVS_LBL_T_HEISS</c>.</summary>
    public string LabelTHeiss { get; set; } = T("PVS_LBL_T_HEISS", "heiß [°C]");

    /// <summary>
    /// Werkzeugtipp der Temperaturzeile — <c>PVS_TIP_TEMPERATUREN</c>: was die zwei
    /// Zahlen bewirken und was ein leeres Feld heißt.
    /// </summary>
    public string TipTemperaturen { get; set; } =
        T("PVS_TIP_TEMPERATUREN",
          "Der kalte Fall bestimmt die höchste Strangspannung (P1, P3), der heiße Fall die niedrigste MPP-Spannung und den höchsten Strom (P2, P4). Leer heißt: −10 °C und +70 °C Zelltemperatur.");

    /// <summary>
    /// Knopf „Vorschlag aus Klimadaten übernehmen" — <c>PVS_BTN_TVORSCHLAG</c>
    /// (<b>W6‑B‑11</b>). Ohne Delegat gibt es ihn NICHT.
    /// </summary>
    public string BtnTemperaturvorschlag { get; set; } =
        T("PVS_BTN_TVORSCHLAG", "Vorschlag aus Klimadaten übernehmen");

    /// <summary>
    /// Die Zeile unter den zwei Feldern, solange kein Vorschlag gerechnet wurde —
    /// <c>PVS_TEMP_GELTEND</c> mit {0} kalt, {1} heiß, {2} und {3} den Vorgaben.
    /// </summary>
    public string HerleitungTemperaturen { get; set; } =
        T("PVS_TEMP_GELTEND",
          "Es gelten {0} °C kalt und {1} °C heiß (leer = Vorgabe {2} °C / {3} °C).");

    /// <summary>Knopf „Strang anlegen" — <c>PVS_BTN_ANLEGEN</c>.</summary>
    public string BtnAnlegen { get; set; } = T("PVS_BTN_ANLEGEN", "Strang anlegen");

    /// <summary>
    /// Knopf „Auslegung vorschlagen" — <c>PVS_BTN_VORSCHLAG</c> (Anwenderwunsch
    /// 08.09.2026, <b>W6‑B‑8</b>). Er füllt die Strangtabelle aus dem Vorschlag des
    /// Kerns (<c>StrangAuslegung.Vorschlagen</c>).
    /// </summary>
    public string BtnVorschlag { get; set; } = T("PVS_BTN_VORSCHLAG", "Auslegung vorschlagen");

    /// <summary>Knopf „Entfernen" — <c>PVS_BTN_ENTFERNEN</c>.</summary>
    public string BtnEntfernen { get; set; } = T("PVS_BTN_ENTFERNEN", "Entfernen");

    /// <summary>Knopf „Wechselrichter der Anlage…" — <c>PVS_BTN_ANLAGE</c>.</summary>
    public string BtnAnlage { get; set; } = T("PVS_BTN_ANLAGE", "Wechselrichter der Anlage…");

    /// <summary>Zeilenwahl-Kurztext der Tabelle — <c>KFAK_SP_WAHL</c>.</summary>
    public string SpalteWahl { get; set; } = T("KFAK_SP_WAHL", "Wahl");
}
