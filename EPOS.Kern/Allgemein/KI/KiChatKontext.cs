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
        //  Zuordnung nach HILFESCHLUESSEL (Auftrag #199, Stufe S1, Weg 1)
        //
        //  WARUM DER HILFESCHLUESSEL. Jeder Dialog traegt ihn ohnehin - am
        //  InfoKnopf, als dieselbe Zeichenkette, die links in help_mapping.txt
        //  steht. Er ist damit die EINZIGE Stelle, die einen Dialog fachlich
        //  benennt (Konzept "Der Hilfe-Assistent im Dialog", 1). Wer daraus den
        //  Bereich ableitet, braucht in 88 Dialogen keine einzige neue Zeile.
        //
        //  PRAEFIX UND NICHT VOLLTREFFER. Ein Schluessel ist
        //  "<Maske>.<Steuerelement>" ("Form_Gebaeude2.groupBox5"), und
        //  help_mapping.txt fuehrt 201 davon auf 103 Masken. Zugeordnet wird
        //  deshalb das MASKENPRAEFIX, und unter mehreren passenden gewinnt das
        //  LAENGSTE: "Form_Stromspeicher" und "Form_Stromverbraucher" sind zwei
        //  Bereiche, "Form_BHKW" deckt "Form_BHKWAdmin" und "Form_BHKWEing"
        //  gemeinsam ab.
        //
        //  DIE SCHRANKE BLEIBT. Jeder Treffer geht durch Freigegeben() - auch
        //  ein Tippfehler in dieser Tabelle kann keinen freien Text in den
        //  Prompt bringen.
        // ------------------------------------------------------------------

        /// <summary>
        /// Zuordnung Maskenpräfix eines Hilfeschlüssels -&gt; Bereich. Gepflegt gegen
        /// <c>WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt</c>; der
        /// Wächter <c>KiBereichstabelleTests</c> hält beide gegeneinander.
        /// </summary>
        private static readonly Dictionary<string, string> BEREICH_JE_HILFEPRAEFIX =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "AssistentSeite",                B_ASSISTENT },
            { "Form_AdminPV",                  B_PHOTOVOLTAIK },
            { "Form_AdminSettings",            B_ADMIN },
            { "Form_AdminStromspeicher",       B_STROMSPEICHER },
            { "Form_AdminWaermeeinlesen",      B_WAERMEBEDARF },
            { "Form_AdminWechselrichter",      B_PHOTOVOLTAIK },
            { "Form_BHKW",                     B_BHKW },
            { "Form_Betriebsmodus",            B_WAERMEPUMPE },
            { "Form_BkUebernahme",             B_VARIANTEN },
            { "Form_Brauchwasser",             B_BRAUCHWASSER },
            { "Form_CaseEingabe",              B_KOSTEN },
            { "Form_DBBHKW",                   B_BHKW },
            { "Form_EingBrauchwasserTyp",      B_BRAUCHWASSER },
            { "Form_EingDBStromverbraucher",   B_STROMVERBRAUCHER },
            { "Form_EingGebTyp",               B_GEBAEUDE },
            { "Form_EingProzTyp",              B_PROZESSWAERME },
            { "Form_EingStromTyp",             B_STROMVERBRAUCHER },
            { "Form_Emissionskatalog",         B_KOSTEN },
            { "Form_Energietraeger",           B_KOSTEN },
            { "Form_ErgBrauchwasserwaerme",    B_BRAUCHWASSER },
            { "Form_ErgProzesswaerme",         B_PROZESSWAERME },
            { "Form_ErgStromverbraucher",      B_STROMVERBRAUCHER },
            { "Form_GanglinieImportOptionen",  B_STROMVERBRAUCHER },
            { "Form_Gebaeude",                 B_GEBAEUDE },
            { "Form_Gesetzesparameter",        B_ADMIN },
            { "Form_Heizkessel",               B_HEIZKESSEL },
            { "Form_ImportKonflikte",          B_PROJEKT },
            { "Form_KatalogDubletten",         B_ADMIN },
            { "Form_KiChat",                   B_HILFE },
            { "Form_KiEinstellungen",          B_HILFE },
            { "Form_Klimadaten",               B_KLIMADATEN },
            { "Form_Klimazonenkarte",          B_QUELLE_ERDREICH },
            { "Form_Kosten",                   B_KOSTEN },
            { "Form_LeistungspreisReihe",      B_KOSTEN },
            { "Form_Lizenz",                   B_LIZENZ },
            { "Form_PV",                       B_PHOTOVOLTAIK },
            { "Form_PeakShaving",              B_STROMSPEICHER },
            { "Form_PhotovoltaikVerguetung",   B_KOSTEN },
            { "Form_Projekt",                  B_PROJEKT },
            { "Form_Prozesswaerme",            B_PROZESSWAERME },
            { "Form_PufferSp",                 B_PUFFERSPEICHER },
            { "Form_QuelleErdreich",           B_QUELLE_ERDREICH },
            { "Form_QuellePufferspeicher",     B_SIM_KONFIG },
            { "Form_Quellprofil",              B_SIM_KONFIG },
            { "Form_Simulation_Config",        B_SIM_KONFIG },
            { "Form_Simulation_Detail",        B_SIM_DETAIL },
            { "Form_Solar",                    B_SOLARTHERMIE },
            { "Form_SpeicherOptimierung",      B_STROMSPEICHER },
            { "Form_SpeicherVariantenVergleich", B_STROMSPEICHER },
            { "Form_SpotpreisImport",          B_KOSTEN },
            { "Form_Start",                    B_HAUPTFENSTER },
            { "Form_Stromganglinie",           B_STROMVERBRAUCHER },
            { "Form_Stromspeicher",            B_STROMSPEICHER },
            { "Form_Stromverbraucher",         B_STROMVERBRAUCHER },
            { "Form_Tarifstruktur",            B_KOSTEN },
            { "Form_Vorlagen",                 B_KOSTEN },
            { "Form_WP",                       B_WAERMEPUMPE },
            { "Form_Waermebedarf",             B_WAERMEBEDARF },
            { "Form_Waermesenke",              B_SIM_KONFIG },
            { "Form_Wirtschaftlichkeit",       B_WIRTSCHAFT },
            { "Hauptfenster",                  B_HAUPTFENSTER },
            { "Kenndaten",                     B_WAERMEPUMPE },
            { "KiWerkzeugliste",               B_HILFE },
            { "Main_PV_Test",                  B_PHOTOVOLTAIK },
            { "UcBericht",                     B_BERICHT },
            { "UcBkKosten",                    B_KOSTEN },
            { "UcBkUebersicht",                B_VARIANTEN },
            { "UcWirtschaftlichkeit",          B_WIRTSCHAFT },
            { "Wizard_WPItem",                 B_WAERMEPUMPE }
        };

        /// <summary>Die Präfixtabelle zum Nachlesen — eine Kopie, keine Handhabe.</summary>
        public static IReadOnlyDictionary<string, string> Hilfepraefixe =>
            new Dictionary<string, string>(BEREICH_JE_HILFEPRAEFIX, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Der Bereich zu einem Hilfeschlüssel (<c>Form_Heizkessel.btn_Help</c>).
        /// Ein unbekanntes Präfix liefert <see cref="BEREICH_UNBEKANNT"/>.
        /// </summary>
        public static string BereichFuerHilfeschluessel(string hilfeschluessel)
        {
            if (string.IsNullOrWhiteSpace(hilfeschluessel)) return BEREICH_UNBEKANNT;

            string schluessel = hilfeschluessel.Trim();

            // Das Maskenpraefix ist alles vor dem ersten Punkt; ein Schluessel ohne
            // Punkt gilt ganz.
            int punkt = schluessel.IndexOf('.');
            string maske = punkt < 0 ? schluessel : schluessel.Substring(0, punkt);
            if (maske.Length == 0) return BEREICH_UNBEKANNT;

            string treffer;
            if (BEREICH_JE_HILFEPRAEFIX.TryGetValue(maske, out treffer)) return Freigegeben(treffer);

            // Unter mehreren passenden Praefixen gewinnt das laengste.
            string bestes = null;
            foreach (KeyValuePair<string, string> paar in BEREICH_JE_HILFEPRAEFIX)
            {
                if (!maske.StartsWith(paar.Key, StringComparison.OrdinalIgnoreCase)) continue;
                if (bestes != null && paar.Key.Length <= bestes.Length) continue;
                bestes = paar.Key;
                treffer = paar.Value;
            }

            return bestes == null ? BEREICH_UNBEKANNT : Freigegeben(treffer);
        }

        // ------------------------------------------------------------------
        //  Der AUFRUF aus einem Dialog (Auftrag #199, Stufe S1)
        // ------------------------------------------------------------------

        /// <summary>
        /// Der zuletzt gemeldete Aufruf aus einem Dialog; <c>null</c> = der Assistent
        /// wurde nicht aus einer Maske heraus gerufen.
        /// </summary>
        public static KiAufrufkontext Aufruf { get; private set; }

        /// <summary>
        /// Meldet den Aufruf aus einem Dialog. <c>null</c> löscht ihn — so meldet sich
        /// der Menüweg und das Schliessen des Chatfensters.
        /// </summary>
        /// <remarks>
        /// <b>Der Aufruf ist der ERSTE Lieferant des Hakens</b>
        /// <see cref="AktiverBereich"/>, die Windows-<c>HilfeKontext</c> der zweite
        /// (Konzept 3.1). Der Haken selbst bleibt unangetastet: Er wird einmal beim
        /// Programmstart belegt, und ein Dialogaufruf, der ihn überschriebe, liesse den
        /// Bereich nach dem Schliessen des Dialogs stehen. Statt dessen fragt
        /// <see cref="AktuellerBereich"/> zuerst hier und fällt dann auf den Haken
        /// zurück — auf iOS, wo kein Haken eingehängt ist, trägt der Aufruf damit
        /// allein.
        /// </remarks>
        public static void AufrufMelden(KiAufrufkontext aufruf)
        {
            Aufruf = aufruf;
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
            // Der Aufruf aus einem Dialog ist der erste Lieferant (S1, #199): Er weiss
            // genauer, worum es geht, als jede Fensterermittlung - und auf iOS ist er
            // der einzige.
            KiAufrufkontext aufruf = Aufruf;
            if (aufruf != null)
            {
                string ausDemAufruf = Freigegeben(aufruf.Bereich);
                if (!string.Equals(ausDemAufruf, BEREICH_UNBEKANNT, StringComparison.Ordinal))
                    return ausDemAufruf;
            }

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
