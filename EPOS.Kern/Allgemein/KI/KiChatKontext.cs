// Der Bedienkontext des KI-Assistenten - plattformfrei (iU9-W15b.0f, Entscheid E-9).
//
// WARUM ES DIESE DATEI GIBT. HilfeKontext.Beschreibung() ermittelt den Bereich, in dem
// der Anwender arbeitet, ueber Form.ActiveForm und ActiveMdiChild
// (WindowsFormsApplication1\Allgemein\KI\HilfeKontext.cs). Auf iOS gibt es beides nicht -
// dort bliebe der Bereich fuer immer "Unbekannter Bereich" (Befund W15b-B19). Der
// Assistent antwortete dann zwar weiterhin, aber ohne zu wissen, wovon der Anwender
// spricht.
//
// Getrennt wird deshalb zwischen ZUORDNUNG und ERMITTLUNG:
//   * Die ZUORDNUNG - Positivliste, drei Nachschlagetabellen, die Freigabeschranke -
//     ist reine Zeichenarbeit und steht hier. HilfeKontext reicht sie durch.
//   * Die ERMITTLUNG des aktiven Fensters bleibt in der Huelle und kommt als
//     Func<string> AktiverBereich herein: Windows belegt sie mit Form.ActiveForm,
//     iOS mit dem Seitenschluessel der offenen Razor-Seite. Ohne Huelle bleibt es bei
//     BEREICH_UNBEKANNT.
//
// DER DATENSCHUTZGRUND BLEIBT DERSELBE (HilfeKontext, Klassenkopf): Der Kontext
// verlaesst den Rechner und darf deshalb ausschliesslich generische
// Bereichsbezeichnungen enthalten. Was nicht in der Positivliste steht, wird zu
// BEREICH_UNBEKANNT - hier wie dort.

using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Bereichszuordnung des KI-Assistenten: Klassenname, Fenstertitel oder
    /// Seitenschluessel in einen der 27 freigegebenen Bereichsnamen.
    /// </summary>
    public static class KiChatKontext
    {
        // ------------------------------------------------------------------
        //  Positivliste der Bereichsbezeichnungen (aus HilfeKontext.cs:33-59)
        // ------------------------------------------------------------------

        /// <summary>Ersatzwert, wenn sich der Bereich nicht sicher zuordnen laesst.</summary>
        public const string BEREICH_UNBEKANNT = "Unbekannter Bereich";

        /// <summary>Administration.</summary>
        public const string B_ADMIN = "Administration";

        // E5 (Projektdialoge, 29.08.2026): der Bereich heisst wie das Fenster
        // "Projektassistent". Der Wert ist zugleich Schluessel in
        // WikiWissen.SEITE_JE_BEREICH - beide Stellen nur gemeinsam aendern.
        /// <summary>Projektassistent.</summary>
        public const string B_ASSISTENT = "Projektassistent";

        /// <summary>Bericht.</summary>
        public const string B_BERICHT = "Bericht";
        /// <summary>BHKW.</summary>
        public const string B_BHKW = "BHKW";
        /// <summary>Brauchwasser.</summary>
        public const string B_BRAUCHWASSER = "Brauchwasser";
        /// <summary>Gebaeude.</summary>
        public const string B_GEBAEUDE = "Gebäude";
        /// <summary>Hauptfenster.</summary>
        public const string B_HAUPTFENSTER = "Hauptfenster";
        /// <summary>Heizkessel.</summary>
        public const string B_HEIZKESSEL = "Heizkessel";
        /// <summary>Hilfe.</summary>
        public const string B_HILFE = "Hilfe";
        /// <summary>Klimadaten.</summary>
        public const string B_KLIMADATEN = "Klimadaten";
        /// <summary>Kosten und Preise.</summary>
        public const string B_KOSTEN = "Kosten und Preise";
        /// <summary>Lizenz.</summary>
        public const string B_LIZENZ = "Lizenz";
        /// <summary>Photovoltaik.</summary>
        public const string B_PHOTOVOLTAIK = "Photovoltaik";
        /// <summary>Projektverwaltung.</summary>
        public const string B_PROJEKT = "Projektverwaltung";
        /// <summary>Prozesswaerme.</summary>
        public const string B_PROZESSWAERME = "Prozesswärme";
        /// <summary>Pufferspeicher.</summary>
        public const string B_PUFFERSPEICHER = "Pufferspeicher";
        /// <summary>Simulation.</summary>
        public const string B_SIMULATION = "Simulation";
        /// <summary>Solarthermie.</summary>
        public const string B_SOLARTHERMIE = "Solarthermie";
        /// <summary>Stromspeicher.</summary>
        public const string B_STROMSPEICHER = "Stromspeicher";
        /// <summary>Stromverbraucher.</summary>
        public const string B_STROMVERBRAUCHER = "Stromverbraucher";
        /// <summary>Varianten.</summary>
        public const string B_VARIANTEN = "Varianten";
        /// <summary>Waermebedarf.</summary>
        public const string B_WAERMEBEDARF = "Wärmebedarf";
        /// <summary>Waermepumpe.</summary>
        public const string B_WAERMEPUMPE = "Wärmepumpe";
        /// <summary>Wirtschaftlichkeit.</summary>
        public const string B_WIRTSCHAFT = "Wirtschaftlichkeit";

        // Von Masken ueber SetzeBereich() gesetzte, bewusst feinere Bezeichnungen.
        // Sie enthalten nur Fach- und Bedienbegriffe, keine Projektdaten.

        /// <summary>Waermequelle Erdreich - die feinere Bezeichnung des Quellendialogs.</summary>
        public const string B_QUELLE_ERDREICH =
            "Wärmequelle Erdreich (Quellsystem, Bodentyp, Auslegungsprüfung VDI 4640)";

        /// <summary>Simulationskonfiguration - die feinere Bezeichnung der Konfigurationsseite.</summary>
        public const string B_SIM_KONFIG =
            "Simulation Konfiguration (Erzeuger definieren, Pufferspeicher zuordnen)";

        /// <summary>Detaillierte Simulation - die feinere Bezeichnung der Ergebnisseite.</summary>
        public const string B_SIM_DETAIL = "Detaillierte Simulation";

        /// <summary>
        /// Alle Zeichenketten, die als Bereichsangabe den Rechner verlassen duerfen.
        /// Was hier nicht steht, wird zu <see cref="BEREICH_UNBEKANNT"/>.
        /// </summary>
        private static readonly HashSet<string> POSITIVLISTE = new HashSet<string>(StringComparer.Ordinal)
        {
            BEREICH_UNBEKANNT,
            B_ADMIN, B_ASSISTENT, B_BERICHT, B_BHKW, B_BRAUCHWASSER, B_GEBAEUDE,
            B_HAUPTFENSTER, B_HEIZKESSEL, B_HILFE, B_KLIMADATEN, B_KOSTEN, B_LIZENZ,
            B_PHOTOVOLTAIK, B_PROJEKT, B_PROZESSWAERME, B_PUFFERSPEICHER, B_SIMULATION,
            B_SOLARTHERMIE, B_STROMSPEICHER, B_STROMVERBRAUCHER, B_VARIANTEN,
            B_WAERMEBEDARF, B_WAERMEPUMPE, B_WIRTSCHAFT,
            B_QUELLE_ERDREICH, B_SIM_KONFIG, B_SIM_DETAIL
        };

        /// <summary>Die Positivliste zum Nachlesen - eine Kopie, keine Handhabe.</summary>
        public static IReadOnlyCollection<string> Bereiche => new List<string>(POSITIVLISTE);

        // ------------------------------------------------------------------
        //  Zuordnung nach Seitenschluessel (iOS, iU9-W15b.0f)
        // ------------------------------------------------------------------

        /// <summary>
        /// Zuordnung <c>EPOS.UI.Seiten.Seitenschluessel</c> -&gt; Bereich. Die Schluessel
        /// stehen hier als Zeichenkette, weil der Kern die Oberflaechenbibliothek nicht
        /// kennt - es sind dieselben sprachneutralen ASCII-Werte.
        /// </summary>
        private static readonly Dictionary<string, string> BEREICH_JE_SEITE =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "PROJEKTLISTE",             B_PROJEKT },
            { "ENERGIETRAEGER_VARIANTE",  B_KOSTEN },
            { "BHKW_WIRTSCHAFTLICHKEIT",  B_BHKW },
            { "SIMULATION_KONFIGURATION", B_SIM_KONFIG },
            { "SIMULATION_ERGEBNIS",      B_SIM_DETAIL },
            { "KI_ASSISTENT",             B_HILFE }
        };

        /// <summary>
        /// Bereich zu einem Seitenschluessel; ein unbekannter Schluessel liefert
        /// <see cref="BEREICH_UNBEKANNT"/>.
        /// </summary>
        public static string BereichFuerSeite(string seitenschluessel)
        {
            if (string.IsNullOrWhiteSpace(seitenschluessel)) return BEREICH_UNBEKANNT;
            string wert;
            return BEREICH_JE_SEITE.TryGetValue(seitenschluessel.Trim(), out wert)
                ? Freigegeben(wert)
                : BEREICH_UNBEKANNT;
        }

        // ------------------------------------------------------------------
        //  Die Ermittlung - Sache der Huelle
        // ------------------------------------------------------------------

        /// <summary>
        /// Woher der aktuelle Bereich kommt. Windows belegt den Haken ueber
        /// <c>HilfeKontext</c> mit <c>Form.ActiveForm</c>/<c>ActiveMdiChild</c>, iOS mit
        /// dem Seitenschluessel der offenen Razor-Seite (<c>AppWurzel</c>).
        /// </summary>
        /// <remarks>
        /// Bleibt der Haken leer, liefert <see cref="AktuellerBereich"/> stets
        /// <see cref="BEREICH_UNBEKANNT"/>. Der Assistent antwortet dann ohne
        /// Bereichsangabe - das funktioniert, ist nur unschaerfer. Was der Haken
        /// liefert, geht IN JEDEM FALL noch durch <see cref="Freigegeben"/>: Auch eine
        /// fehlerhafte Huelle kann so keinen freien Text in den Prompt bringen.
        /// </remarks>
        public static Func<string> AktiverBereich { get; set; }

        /// <summary>
        /// Der Bereich, in dem der Anwender gerade arbeitet - immer ein Eintrag der
        /// Positivliste.
        /// </summary>
        public static string AktuellerBereich()
        {
            Func<string> haken = AktiverBereich;
            if (haken == null) return BEREICH_UNBEKANNT;

            try { return Freigegeben(haken()); }
            catch { return BEREICH_UNBEKANNT; }
        }

        // ------------------------------------------------------------------
        //  Die Schranke
        // ------------------------------------------------------------------

        /// <summary>
        /// Letzte Schranke: nur Eintraege der Positivliste duerfen hinaus. Alles andere
        /// wird zu <see cref="BEREICH_UNBEKANNT"/>.
        /// </summary>
        public static string Freigegeben(string bereich)
        {
            if (string.IsNullOrWhiteSpace(bereich)) return BEREICH_UNBEKANNT;
            string b = bereich.Trim();
            return POSITIVLISTE.Contains(b) ? b : BEREICH_UNBEKANNT;
        }
    }
}
