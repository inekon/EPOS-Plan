// Die sprachneutralen KENNUNGEN der Meldungen, die sich vom Assistenten erklaeren
// lassen (Auftrag #199, Stufe S1, Weg 2).
//
// WARUM SIE AN EINER STELLE STEHEN. Eine Kennung ist DREIMAL dieselbe Zeichenkette:
// am Banner ("erklaeren lassen" haengt sie an), im Aufrufkontext (sie geht als
// Suchbegriff in den Chat) und im Aktionswissen (HilfeWissen fuehrt je Kennung einen
// Abschnitt). Stuende sie dreimal getippt da, liefe sie beim ersten Umbenennen
// auseinander - und das Ergebnis waere kein Fehler, sondern ein Abschnitt, den
// niemand mehr findet. Deshalb: EINE Tabelle, drei Leser.
//
// SIE SIND NICHT NEU ERFUNDEN. Zwei der drei Familien gab es schon:
//   * LAUF_W_* sind woertlich die Kriteriumsschluessel aus SimulationLaufCtrl (#190).
//   * FLOTTE_* sind die Aufzaehlung FlottenHinweisKennung (#183) in Textform; die
//     Umsetzung steht in Fuer(FlottenHinweisKennung) und an keiner zweiten Stelle.
//   * PV_STRANG_P1..P8 sind die acht Regeln der Strangampel aus
//     Konzept_Wechselrichter_EPOS-Plan.md 4.2 (StrangPlausibilitaet).
//
// SCHREIBWEISE: GROSSBUCHSTABEN, Unterstriche, ASCII - dieselbe Drei-Schichten-Regel
// wie bei Masken und Seitenschluessel. Sie sind zugleich der NAMENSTEIL der Ressource
// KI_FRAGE_<KENNUNG>; wer eine Kennung aendert, aendert den Ressourcenschluessel mit.

using System;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Kennungen der erklärbaren Meldungen — Bindeglied zwischen Banner,
    /// <see cref="KiAufrufkontext"/> und dem Aktionswissen in <see cref="HilfeWissen"/>.
    /// </summary>
    public static class KiMeldungskennung
    {
        // ------------------------------------------------------------------
        //  Speicherflotte (FlottenHinweisKennung, #183; Diagnosebanner, #192)
        // ------------------------------------------------------------------

        /// <summary>Peak-Ziel unter dem Maximum der Tagesminima, Netzladung verboten.</summary>
        public const string FLOTTE_PEAKZIEL_UNTER_TAGESMINIMUM = "FLOTTE_PEAKZIEL_UNTER_TAGESMINIMUM";

        /// <summary>Peak-Ziel über der Referenzspitze — die Kappung bleibt wirkungslos.</summary>
        public const string FLOTTE_PEAKZIEL_UEBER_REFERENZSPITZE = "FLOTTE_PEAKZIEL_UEBER_REFERENZSPITZE";

        /// <summary>Jährlicher Betriebsaufwand unter einem Tausendstel der Investition.</summary>
        public const string FLOTTE_BETRIEBSKOSTEN_SEHR_NIEDRIG = "FLOTTE_BETRIEBSKOSTEN_SEHR_NIEDRIG";

        /// <summary>Start-Ladezustand auf dem SoC-Minimum.</summary>
        public const string FLOTTE_START_SOC_AUF_MINIMUM = "FLOTTE_START_SOC_AUF_MINIMUM";

        /// <summary>Die Flotte hat im ganzen Zeitraum weder geladen noch entladen.</summary>
        public const string FLOTTE_ARBEITSLOS = "FLOTTE_ARBEITSLOS";

        /// <summary>Planendes Betriebsziel ohne geladenen Prognose-Snapshot.</summary>
        public const string FLOTTE_PROGNOSE_FEHLT = "FLOTTE_PROGNOSE_FEHLT";

        /// <summary>Ein Punkt der Lebensdauerkurve, mit dem die Rainflow-Auswertung nicht rechnen kann.</summary>
        public const string FLOTTE_RAINFLOW_UNGUELTIG = "FLOTTE_RAINFLOW_UNGUELTIG";

        // ------------------------------------------------------------------
        //  Simulationslauf (SimulationLaufCtrl, #190)
        // ------------------------------------------------------------------

        /// <summary>Wärmeerzeuger angelegt, aber in keinem Kaskadenplatz.</summary>
        public const string LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ =
            SimulationLaufCtrl.KRIT_ERZEUGER_OHNE_KASKADENPLATZ;

        /// <summary>Stromerzeuger bzw. Energiespeicher angelegt, aber nicht auf seinem Platz.</summary>
        public const string LAUF_W_ERZEUGER_OHNE_STROMPLATZ =
            SimulationLaufCtrl.KRIT_ERZEUGER_OHNE_STROMPLATZ;

        // ------------------------------------------------------------------
        //  Strangampel P1 bis P8 (StrangPlausibilitaet)
        // ------------------------------------------------------------------

        /// <summary>P1: Leerlaufspannung des Strangs im kalten Fall über der DC-Grenze.</summary>
        public const string PV_STRANG_P1 = "PV_STRANG_P1";

        /// <summary>P2: MPP-Spannung im heissen Fall unter dem MPP-Fenster.</summary>
        public const string PV_STRANG_P2 = "PV_STRANG_P2";

        /// <summary>P3: MPP-Spannung im kalten Fall über dem MPP-Fenster.</summary>
        public const string PV_STRANG_P3 = "PV_STRANG_P3";

        /// <summary>P4: Eingangsstrom je MPPT über dem zulässigen Wert.</summary>
        public const string PV_STRANG_P4 = "PV_STRANG_P4";

        /// <summary>P5: mehr Stränge an einem MPPT, als das Gerät führt.</summary>
        public const string PV_STRANG_P5 = "PV_STRANG_P5";

        /// <summary>P6: DC/AC-Verhältnis ausserhalb des empfohlenen Bandes.</summary>
        public const string PV_STRANG_P6 = "PV_STRANG_P6";

        /// <summary>P7: DC-Eingangsleistung über der Herstellergrenze.</summary>
        public const string PV_STRANG_P7 = "PV_STRANG_P7";

        /// <summary>P8: Modulsumme der Stränge weicht von der „Anzahl Module" der Anlage ab.</summary>
        public const string PV_STRANG_P8 = "PV_STRANG_P8";

        // ------------------------------------------------------------------
        //  Klimadaten: die vier TRY-Meldungen (Auftrag KI-1)
        // ------------------------------------------------------------------
        //
        // SIE HEISSEN WIE IHR RESSOURCENSCHLUESSEL. Die Meldungstexte des
        // Klimaimports liegen unter genau diesen Namen in Resource.resx
        // (KlimaImportAblauf, TryPaketLeser); dieselbe Zeichenkette ist hier die
        // Kennung und im Aktionswissen der Abschnitt. Wer sie aus einer Meldung
        // oder einem Protokoll abschreibt, findet die Erklaerung.

        /// <summary>Die Adresse der TRY-Regionaldaten erlaubt keine Teilabrufe.</summary>
        public const string KLIMA_TRY_KEIN_BEREICH = "KLIMA_TRY_KEIN_BEREICH";

        /// <summary>Der Standort liegt weiter als 300 km von der naechsten TRY-Region.</summary>
        public const string KLIMA_TRY_AUSSERHALB = "KLIMA_TRY_AUSSERHALB";

        /// <summary>Die TRY-Daten sind nicht lesbar (Zeile, Spaltenzahl, Feldwert).</summary>
        public const string KLIMA_TRY_FORMATFEHLER = "KLIMA_TRY_FORMATFEHLER";

        /// <summary>Der Kopf der TRY-Datei nennt keinen lesbaren Standort.</summary>
        public const string KLIMA_TRY_STANDORT_UNLESBAR = "KLIMA_TRY_STANDORT_UNLESBAR";

        // ------------------------------------------------------------------
        //  Berichtsvorlagen: Vorpruefung, Vorlagenwahl, Laufmeldung (BV-E1)
        // ------------------------------------------------------------------
        //
        // SIE HEISSEN WIE IHR RESSOURCENSCHLUESSEL - wie KLIMA_TRY_*. Eine
        // Pruefmeldung traegt die Kennung schon (Pruefmeldung.Kennung ist der
        // Ressourcenschluessel ihres Texts, VF_PRUEF_* und BV_VORLAGEN_*), die
        // Laufmeldung und die Rueckfrage vor dem Start ebenso (Berichtsmeldung.
        // Kennung, BV_LAUF_* und BV_START_*). Die Liste Berichtsvorlagen haelt der
        // Waechter BerichtKiKennungenTests gegen den Quelltext des Pruefers.

        /// <summary>Die Vorlage kann nicht gelesen werden.</summary>
        public const string VF_PRUEF_UNLESBAR = nameof(MyResource.Resource.VF_PRUEF_UNLESBAR);

        /// <summary>Das Dateiformat der Vorlage wird nicht unterstützt.</summary>
        public const string VF_PRUEF_FORMAT = nameof(MyResource.Resource.VF_PRUEF_FORMAT);

        /// <summary>Die Vorlage ist zu groß.</summary>
        public const string VF_PRUEF_GROESSE = nameof(MyResource.Resource.VF_PRUEF_GROESSE);

        /// <summary>Die Vorlage ist entpackt zu groß.</summary>
        public const string VF_PRUEF_GROESSE_ENTPACKT = nameof(MyResource.Resource.VF_PRUEF_GROESSE_ENTPACKT);

        /// <summary>Die Vorlage enthält Makros.</summary>
        public const string VF_PRUEF_MAKROS = nameof(MyResource.Resource.VF_PRUEF_MAKROS);

        /// <summary>Nachverfolgte Änderungen in der Vorlage.</summary>
        public const string VF_PRUEF_AENDERUNGEN = nameof(MyResource.Resource.VF_PRUEF_AENDERUNGEN);

        /// <summary>Der Verweis auf die Dokumentvorlage wird entfernt.</summary>
        public const string VF_PRUEF_VORLAGENVERWEIS = nameof(MyResource.Resource.VF_PRUEF_VORLAGENVERWEIS);

        /// <summary>Verknüpfte Inhalte werden entfernt.</summary>
        public const string VF_PRUEF_EXTERN = nameof(MyResource.Resource.VF_PRUEF_EXTERN);

        /// <summary>Überschriftenstile fehlen oder tragen keine Gliederungsebene.</summary>
        public const string VF_PRUEF_UEBERSCHRIFTEN = nameof(MyResource.Resource.VF_PRUEF_UEBERSCHRIFTEN);

        /// <summary>Platzhalter nicht erkannt.</summary>
        public const string VF_PRUEF_KLAMMER_OFFEN = nameof(MyResource.Resource.VF_PRUEF_KLAMMER_OFFEN);

        /// <summary>Unbekannte Marke in doppelten Klammern.</summary>
        public const string VF_PRUEF_MARKE_UNBEKANNT = nameof(MyResource.Resource.VF_PRUEF_MARKE_UNBEKANNT);

        /// <summary>Unbekannter Platzhalter.</summary>
        public const string VF_PRUEF_UNBEKANNT = nameof(MyResource.Resource.VF_PRUEF_UNBEKANNT);

        /// <summary>Platzhalter in abweichender Schreibweise.</summary>
        public const string VF_PRUEF_NORMALFORM = nameof(MyResource.Resource.VF_PRUEF_NORMALFORM);

        /// <summary>Wert je Variante wird noch nicht gefüllt.</summary>
        public const string VF_PRUEF_KONTEXT_STAND = nameof(MyResource.Resource.VF_PRUEF_KONTEXT_STAND);

        /// <summary>Wert je Gebäude wird noch nicht gefüllt.</summary>
        public const string VF_PRUEF_KONTEXT_GEBAEUDE = nameof(MyResource.Resource.VF_PRUEF_KONTEXT_GEBAEUDE);

        /// <summary>Platzhalter an einer Stelle, an der er nicht stehen kann.</summary>
        public const string VF_PRUEF_ORT = nameof(MyResource.Resource.VF_PRUEF_ORT);

        /// <summary>Unbekannte Formatangabe.</summary>
        public const string VF_PRUEF_ANGABE_UNBEKANNT = nameof(MyResource.Resource.VF_PRUEF_ANGABE_UNBEKANNT);

        /// <summary>Formatangabe passt nicht zur Art des Platzhalters.</summary>
        public const string VF_PRUEF_ANGABE_UNPASSEND = nameof(MyResource.Resource.VF_PRUEF_ANGABE_UNPASSEND);

        /// <summary>Unbekannter Wiederholbereich.</summary>
        public const string VF_PRUEF_BLOCK_BEREICH = nameof(MyResource.Resource.VF_PRUEF_BLOCK_BEREICH);

        /// <summary>Blockmarke als Inhaltssteuerelement, das die Engine nicht auswertet (Ende, im Satz, um eine Zelle).</summary>
        public const string VF_PRUEF_BLOCK_NICHT_UNTERSTUETZT = nameof(MyResource.Resource.VF_PRUEF_BLOCK_NICHT_UNTERSTUETZT);

        /// <summary>Paarvergleich (stand.a/stand.b) in Sicht 1 mit mehr als einer Variante.</summary>
        public const string VF_PRUEF_PAARSICHT = nameof(MyResource.Resource.VF_PRUEF_PAARSICHT);

        /// <summary>Positionsadressierung (stand.&lt;n&gt;, variante.&lt;n&gt;) über der Zahl der gewählten Stände (BV-E9).</summary>
        public const string VF_PRUEF_POSITION = nameof(MyResource.Resource.VF_PRUEF_POSITION);

        /// <summary>Mustertabelle ohne erkennbare Rolle (BV-E5).</summary>
        public const string VF_PRUEF_MUSTER_OHNE_ROLLEN = nameof(MyResource.Resource.VF_PRUEF_MUSTER_OHNE_ROLLEN);

        /// <summary>Bedingung ohne Schalter.</summary>
        public const string VF_PRUEF_WENN_OHNE_SCHALTER = nameof(MyResource.Resource.VF_PRUEF_WENN_OHNE_SCHALTER);

        /// <summary>Bedingung mit einem Wert statt eines Schalters.</summary>
        public const string VF_PRUEF_WENN_KEIN_SCHALTER = nameof(MyResource.Resource.VF_PRUEF_WENN_KEIN_SCHALTER);

        /// <summary>Blockmarke im Satz.</summary>
        public const string VF_PRUEF_BLOCK_ALLEIN = nameof(MyResource.Resource.VF_PRUEF_BLOCK_ALLEIN);

        /// <summary>Block in der dritten Ebene.</summary>
        public const string VF_PRUEF_BLOCK_TIEFE = nameof(MyResource.Resource.VF_PRUEF_BLOCK_TIEFE);

        /// <summary>Blockende ohne Anfang.</summary>
        public const string VF_PRUEF_BLOCK_ENDE = nameof(MyResource.Resource.VF_PRUEF_BLOCK_ENDE);

        /// <summary>Block wird nicht geschlossen.</summary>
        public const string VF_PRUEF_BLOCK_OFFEN = nameof(MyResource.Resource.VF_PRUEF_BLOCK_OFFEN);

        /// <summary>Verbundene Zellen in der Wiederholzeile.</summary>
        public const string VF_PRUEF_BLOCK_VERBUNDEN = nameof(MyResource.Resource.VF_PRUEF_BLOCK_VERBUNDEN);

        /// <summary>Block reicht über eine Tabellengrenze.</summary>
        public const string VF_PRUEF_BLOCK_TABELLE = nameof(MyResource.Resource.VF_PRUEF_BLOCK_TABELLE);

        /// <summary>Datumsfeld zeigt das Datum des Öffnens.</summary>
        public const string VF_PRUEF_DATUMSFELD = nameof(MyResource.Resource.VF_PRUEF_DATUMSFELD);

        /// <summary>Kommentare der Vorlage.</summary>
        public const string VF_PRUEF_KOMMENTARE = nameof(MyResource.Resource.VF_PRUEF_KOMMENTARE);

        /// <summary>Vorlage ohne Platzhalter.</summary>
        public const string VF_PRUEF_OHNE_PLATZHALTER = nameof(MyResource.Resource.VF_PRUEF_OHNE_PLATZHALTER);

        /// <summary>Die Sprache der Vorlage weicht ab.</summary>
        public const string VF_PRUEF_SPRACHE = nameof(MyResource.Resource.VF_PRUEF_SPRACHE);

        /// <summary>Vorlage aus einer älteren Katalogfassung.</summary>
        public const string VF_PRUEF_FASSUNG_ALT = nameof(MyResource.Resource.VF_PRUEF_FASSUNG_ALT);

        /// <summary>Neues Kapitel, in der Vorlage nicht enthalten.</summary>
        public const string VF_PRUEF_KAPITEL_NEU = nameof(MyResource.Resource.VF_PRUEF_KAPITEL_NEU);

        /// <summary>Vorlage aus einer neueren Katalogfassung.</summary>
        public const string VF_PRUEF_FASSUNG_NEU = nameof(MyResource.Resource.VF_PRUEF_FASSUNG_NEU);

        /// <summary>Gültigkeitshinweise fehlen.</summary>
        public const string VF_PRUEF_GUELTIGKEIT = nameof(MyResource.Resource.VF_PRUEF_GUELTIGKEIT);

        /// <summary>Ein Kapitel steht mehrfach in der Vorlage (BV-E2).</summary>
        public const string VF_PRUEF_KAPITEL_DOPPELT = nameof(MyResource.Resource.VF_PRUEF_KAPITEL_DOPPELT);

        /// <summary>Ein Platzhalter wirkt erst in einer späteren Programmfassung (Schalter, Bild als Text; BV-E2).</summary>
        public const string VF_PRUEF_SPAETER = nameof(MyResource.Resource.VF_PRUEF_SPAETER);

        /// <summary>Die Anhang-E-Checkliste findet ein Kapitel nicht in der Vorlage (BV-E2).</summary>
        public const string VF_PRUEF_ANHANG_E_STELLE = nameof(MyResource.Resource.VF_PRUEF_ANHANG_E_STELLE);

        /// <summary>Die Vorlagendatei kann nicht gelesen werden.</summary>
        public const string BV_VORLAGEN_NICHT_LESBAR = nameof(MyResource.Resource.BV_VORLAGEN_NICHT_LESBAR);

        /// <summary>Die Vorlage ist nicht vorhanden.</summary>
        public const string BV_VORLAGEN_FEHLT = nameof(MyResource.Resource.BV_VORLAGEN_FEHLT);

        /// <summary>Die Vorlage ist in Word geöffnet.</summary>
        public const string BV_VORLAGEN_IN_WORD = nameof(MyResource.Resource.BV_VORLAGEN_IN_WORD);

        /// <summary>Welche Word-Vorlage der Bericht nimmt.</summary>
        public const string BV_LAUF_VORLAGE = nameof(MyResource.Resource.BV_LAUF_VORLAGE);

        /// <summary>Rückfall bei der Vorlagenwahl.</summary>
        public const string BV_LAUF_RUECKFALL = nameof(MyResource.Resource.BV_LAUF_RUECKFALL);

        /// <summary>Nicht ersetzte Platzhalter, gelb markiert.</summary>
        public const string BV_LAUF_UNBEKANNT = nameof(MyResource.Resource.BV_LAUF_UNBEKANNT);

        /// <summary>Platzhalter ohne Wert.</summary>
        public const string BV_LAUF_LEER = nameof(MyResource.Resource.BV_LAUF_LEER);

        /// <summary>Kommentare der Vorlage entfernt.</summary>
        public const string BV_LAUF_KOMMENTARE = nameof(MyResource.Resource.BV_LAUF_KOMMENTARE);

        /// <summary>Warnungen beim Füllen der Vorlage.</summary>
        public const string BV_LAUF_WARNUNGEN = nameof(MyResource.Resource.BV_LAUF_WARNUNGEN);

        /// <summary>Die Vorlage nutzt den Paarvergleich, gewählt ist Sicht 1.</summary>
        public const string BV_START_SICHT = nameof(MyResource.Resource.BV_START_SICHT);

        /// <summary>Vorlage ohne Wirtschaftlichkeit.</summary>
        public const string BV_START_OHNE_WIRTSCHAFT = nameof(MyResource.Resource.BV_START_OHNE_WIRTSCHAFT);

        /// <summary>
        /// Die Kennungen der Berichtsvorlagen: die Regeln des Vorlagenprüfers, die
        /// Prüfmeldungen des Vorlagen-Controllers, die Abschnitte der Laufmeldung und die
        /// Befunde der Rückfrage vor dem Start. Steht VOR <see cref="Alle"/> — statische
        /// Felder werden in der Folge des Quelltexts belegt.
        /// </summary>
        public static readonly string[] Berichtsvorlagen =
        {
            VF_PRUEF_UNLESBAR, VF_PRUEF_FORMAT, VF_PRUEF_GROESSE, VF_PRUEF_GROESSE_ENTPACKT, VF_PRUEF_MAKROS,
            VF_PRUEF_AENDERUNGEN, VF_PRUEF_VORLAGENVERWEIS, VF_PRUEF_EXTERN, VF_PRUEF_UEBERSCHRIFTEN,
            VF_PRUEF_KLAMMER_OFFEN, VF_PRUEF_MARKE_UNBEKANNT, VF_PRUEF_UNBEKANNT, VF_PRUEF_NORMALFORM,
            VF_PRUEF_KONTEXT_STAND, VF_PRUEF_KONTEXT_GEBAEUDE, VF_PRUEF_ORT, VF_PRUEF_ANGABE_UNBEKANNT,
            VF_PRUEF_ANGABE_UNPASSEND, VF_PRUEF_BLOCK_BEREICH, VF_PRUEF_BLOCK_NICHT_UNTERSTUETZT, VF_PRUEF_PAARSICHT,
            VF_PRUEF_POSITION, VF_PRUEF_MUSTER_OHNE_ROLLEN,
            VF_PRUEF_WENN_OHNE_SCHALTER, VF_PRUEF_WENN_KEIN_SCHALTER, VF_PRUEF_BLOCK_ALLEIN,
            VF_PRUEF_BLOCK_TIEFE, VF_PRUEF_BLOCK_ENDE, VF_PRUEF_BLOCK_OFFEN, VF_PRUEF_BLOCK_VERBUNDEN,
            VF_PRUEF_BLOCK_TABELLE, VF_PRUEF_DATUMSFELD, VF_PRUEF_KOMMENTARE, VF_PRUEF_OHNE_PLATZHALTER,
            VF_PRUEF_SPRACHE, VF_PRUEF_FASSUNG_ALT, VF_PRUEF_KAPITEL_NEU, VF_PRUEF_FASSUNG_NEU,
            VF_PRUEF_GUELTIGKEIT, VF_PRUEF_KAPITEL_DOPPELT, VF_PRUEF_SPAETER, VF_PRUEF_ANHANG_E_STELLE,
            BV_VORLAGEN_NICHT_LESBAR, BV_VORLAGEN_FEHLT, BV_VORLAGEN_IN_WORD,
            BV_LAUF_VORLAGE, BV_LAUF_RUECKFALL, BV_LAUF_UNBEKANNT, BV_LAUF_LEER, BV_LAUF_KOMMENTARE,
            BV_LAUF_WARNUNGEN, BV_START_SICHT, BV_START_OHNE_WIRTSCHAFT
        };

        /// <summary>
        /// Alle Kennungen dieser Klasse — für den Nachweis, dass jede einen
        /// Wissensabschnitt und eine Ressource <c>KI_FRAGE_&lt;Kennung&gt;</c> hat.
        /// </summary>
        public static readonly string[] Alle = new[]
        {
            FLOTTE_PEAKZIEL_UNTER_TAGESMINIMUM, FLOTTE_PEAKZIEL_UEBER_REFERENZSPITZE,
            FLOTTE_BETRIEBSKOSTEN_SEHR_NIEDRIG, FLOTTE_START_SOC_AUF_MINIMUM,
            FLOTTE_ARBEITSLOS, FLOTTE_PROGNOSE_FEHLT, FLOTTE_RAINFLOW_UNGUELTIG,
            LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ, LAUF_W_ERZEUGER_OHNE_STROMPLATZ,
            PV_STRANG_P1, PV_STRANG_P2, PV_STRANG_P3, PV_STRANG_P4,
            PV_STRANG_P5, PV_STRANG_P6, PV_STRANG_P7, PV_STRANG_P8,
            KLIMA_TRY_KEIN_BEREICH, KLIMA_TRY_AUSSERHALB, KLIMA_TRY_FORMATFEHLER,
            KLIMA_TRY_STANDORT_UNLESBAR
        }.Concat(Berichtsvorlagen).ToArray();

        /// <summary>
        /// Die Kennung zu einem Prüfhinweis der Speicherflotte. Eine unbekannte
        /// Aufzählungsmarke liefert eine leere Zeichenkette — dann zeigt das Banner
        /// keinen Erklärlink, statt auf einen Abschnitt zu zeigen, den es nicht gibt.
        /// </summary>
        public static string Fuer(FlottenHinweisKennung kennung)
        {
            switch (kennung)
            {
                case FlottenHinweisKennung.PeakZielUnterTagesminimum:
                    return FLOTTE_PEAKZIEL_UNTER_TAGESMINIMUM;
                case FlottenHinweisKennung.PeakZielUeberReferenzspitze:
                    return FLOTTE_PEAKZIEL_UEBER_REFERENZSPITZE;
                case FlottenHinweisKennung.BetriebskostenSehrNiedrig:
                    return FLOTTE_BETRIEBSKOSTEN_SEHR_NIEDRIG;
                case FlottenHinweisKennung.StartSoCAufMinimum:
                    return FLOTTE_START_SOC_AUF_MINIMUM;
                case FlottenHinweisKennung.FlotteArbeitslos:
                    return FLOTTE_ARBEITSLOS;
                case FlottenHinweisKennung.PrognoseFehlt:
                    return FLOTTE_PROGNOSE_FEHLT;
                case FlottenHinweisKennung.LebensdauerkurveUngueltig:
                    return FLOTTE_RAINFLOW_UNGUELTIG;
                default:
                    return "";
            }
        }

        /// <summary>
        /// Die Kennung zur Regelnummer der Strangampel (1 bis 8); ausserhalb dieses
        /// Bereichs eine leere Zeichenkette.
        /// </summary>
        public static string FuerStrangregel(int regel)
        {
            if (regel < 1 || regel > 8) return "";
            return "PV_STRANG_P" + regel.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Die vorbelegte Frage zu einer Meldung: die Ressource
        /// <c>KI_FRAGE_&lt;Kennung&gt;</c>, wenn es sie gibt — sonst der allgemeine Satz
        /// <c>KI_FRAGE_ALLGEMEIN</c> mit dem Bannertext (Konzept 3.2).
        /// </summary>
        /// <param name="kennung">Die Meldungskennung; leer liefert den allgemeinen Satz.</param>
        /// <param name="meldungstext">Der Bannertext für den allgemeinen Satz.</param>
        /// <remarks>
        /// <b>Sie steht im Kern und nicht in der Oberfläche</b>, weil hier der
        /// Ressourcenkatalog liegt und weil beide Plattformen dieselbe Frage stellen
        /// sollen. Fehlt beides — Ressource und Bannertext —, bleibt die Eingabezeile
        /// leer; eine erfundene Frage wäre schlechter als keine.
        /// </remarks>
        public static string Frage(string kennung, string meldungstext)
        {
            string besondere = Ressource("KI_FRAGE_" + (kennung ?? "").Trim());
            if (!string.IsNullOrEmpty(besondere)) return besondere;

            string text = (meldungstext ?? "").Trim();
            if (text.Length == 0) return "";

            string vorlage = Ressource("KI_FRAGE_ALLGEMEIN");
            if (string.IsNullOrEmpty(vorlage)) return text;

            try
            {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, vorlage, text);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        /// <summary>Ein Ressourcentext, oder leer — ein fehlender Schlüssel ist kein Fehler.</summary>
        private static string Ressource(string schluessel)
        {
            if (string.IsNullOrWhiteSpace(schluessel)) return "";
            try { return MyResource.Resource.ResourceManager.GetString(schluessel) ?? ""; }
            catch (Exception) { return ""; }
        }
    }
}
