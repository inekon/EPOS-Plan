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

        /// <summary>
        /// Alle Kennungen dieser Klasse — für den Nachweis, dass jede einen
        /// Wissensabschnitt und eine Ressource <c>KI_FRAGE_&lt;Kennung&gt;</c> hat.
        /// </summary>
        public static readonly string[] Alle =
        {
            FLOTTE_PEAKZIEL_UNTER_TAGESMINIMUM, FLOTTE_PEAKZIEL_UEBER_REFERENZSPITZE,
            FLOTTE_BETRIEBSKOSTEN_SEHR_NIEDRIG, FLOTTE_START_SOC_AUF_MINIMUM,
            FLOTTE_ARBEITSLOS,
            LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ, LAUF_W_ERZEUGER_OHNE_STROMPLATZ,
            PV_STRANG_P1, PV_STRANG_P2, PV_STRANG_P3, PV_STRANG_P4,
            PV_STRANG_P5, PV_STRANG_P6, PV_STRANG_P7, PV_STRANG_P8
        };

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
