using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // AH-V4 - DAS VORHEIZEN DER AUFHEIZOPTIMIERUNG, FASSUNG 2: EINSTELLUNGEN UND ERGEBNISSE
    // (Entwurf Vorheizrampe Fassung 2, Abschnitt 4 „Folgen“, 2.9; Entscheide E122, E124, E125).
    //
    // WAS.
    //   Tab_Einstellungen     sieben Spalten der Projekteinstellung: Verfahren, Vorheizzeit, Toleranz (Art, Wert),
    //                         Regelgenauigkeit, Geltung, Heizkreis-Schalter
    //   Tab_Zone              Vorheizzeit_H (NULL = erbt vom Gebaeude bzw. Projekt), ebenso am Katalogzwilling Tab_Zone_STAMM
    //   Tab_ErgebnisGebaeude  achtzehn Ergebnisgroessen des Vorheizens (Vorheizgebaeude des Kerns)
    //   Tab_ErgebnisZone      dieselben achtzehn je Zone
    //
    // Tab_Gebaeude.Aufheizzeit_Manuell_H bleibt (Schritt KP-S4) und traegt im Verfahren VORGABE die Vorheizzeit des
    // Gebaeudes. Keine Spalte fuer die Art „fest/taeglich“ (E124 F13: nur „fest“), keine fuer den Ankunftsbezug und keine fuer
    // den Quantilanteil (E125: Kern-Konstanten).
    //
    // ALLE NULLBAR, mit Pruefklausel. NULL heisst Vorgabe: Verfahren RAMPE (Bestand), Toleranz 20 % (Kern-Konstante, F14),
    // Regelgenauigkeit 1 K (F15), Geltung GEBAEUDE, Heizkreis einbezogen. Die Ergebnisspalten stehen auf NULL, solange das
    // Verfahren die Sollwertrampe ist.
    //
    // ERGEBNISNEUTRAL. KEIN DML, KEIN TABELLENNEUBAU: Jede Bestandszeile steht danach auf NULL, und NULL rechnet bitgleich
    // den Bestand. Der Schritt ist wiederholbar.
    //
    // DREI LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema und die Testvorrichtung in EPOS.Kern.Tests;
    // dazu KonfigurationCtrl (Einstellung), GebaeudeZonenCtrl (Zone) und ErgebnisCtrl (Ergebnis) nach Vorhandensein.
    // ====================================================================================

    /// <summary>
    /// <b>AH-V4</b> — das Vorheizen der Aufheizoptimierung (Fassung 2): sieben Einstellungsspalten an <c>Tab_Einstellungen</c>,
    /// die Vorheizzeit je Zone an <c>Tab_Zone</c> (und dem Katalogzwilling <c>Tab_Zone_STAMM</c>) und achtzehn Ergebnisspalten je Gebäude und je Zone. EINE Quelle für
    /// Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class VorheizenSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (215).</summary>
        // Hängt an Schritt 214 (K-F1, RueckkuehlwerkSchema).
        public const int SCHRITT = RueckkuehlwerkSchema.SCHRITT + 1;

        /// <summary>Die Projekteinstellungen.</summary>
        public const string TAB_EINSTELLUNGEN = SchemaKatalog.TAB_EINSTELLUNGEN;

        /// <summary>Die Zonen.</summary>
        public const string TAB_ZONE = SchemaKatalog.TAB_ZONE;

        /// <summary>Der Zwilling der Zonen im Gebäudekatalog (führt jede Fachspalte der Projekttabelle).</summary>
        public const string TAB_ZONE_STAMM = SchemaKatalog.TAB_ZONE_STAMM;

        /// <summary>Das Ergebnis je Gebäude.</summary>
        public const string TAB_ERGEBNIS_GEBAEUDE = AufheizErgebnisSchema.TAB_GEBAEUDE;

        /// <summary>Das Ergebnis je Zone.</summary>
        public const string TAB_ERGEBNIS_ZONE = AufheizErgebnisSchema.TAB_ZONE;

        // ---- Einstellung (Tab_Einstellungen) ----

        public const string SPALTE_VERFAHREN = "Aufheiz_Verfahren";
        public const string SPALTE_VORHEIZZEIT = "Aufheiz_Vorheizzeit_H";
        public const string SPALTE_TOLERANZ_ART = "Aufheiz_Toleranz_Art";
        public const string SPALTE_TOLERANZ = "Aufheiz_Toleranz";
        public const string SPALTE_REGELGENAUIGKEIT = "Aufheiz_Regelgenauigkeit_K";
        public const string SPALTE_GELTUNG = "Aufheiz_Geltung";
        public const string SPALTE_HEIZKREIS = "Aufheiz_Heizkreis_Einbeziehen";

        // ---- Zone (Tab_Zone) ----

        /// <summary>Die Vorheizzeit der Zone [h], 1 bis 47; NULL = erbt (Gebäude, dann Projekt).</summary>
        public const string SPALTE_ZONE_VORHEIZZEIT = "Vorheizzeit_H";

        // ---- Persistenzwerte (deutsch, eingefroren) ----

        public const string VERFAHREN_RAMPE = "RAMPE";
        public const string VERFAHREN_VORGABE = "VORGABE";
        public const string VERFAHREN_BERECHNET = "BERECHNET";

        /// <summary>Die Verfahren in fester Folge; leer = <see cref="VERFAHREN_RAMPE"/>.</summary>
        public static readonly IReadOnlyList<string> VERFAHREN = new[] { VERFAHREN_RAMPE, VERFAHREN_VORGABE, VERFAHREN_BERECHNET };

        public const string TOLERANZ_PROZENT = "PROZENT";
        public const string TOLERANZ_KW = "KW";

        /// <summary>Die Arten der Toleranz; leer = <see cref="TOLERANZ_PROZENT"/>.</summary>
        public static readonly IReadOnlyList<string> TOLERANZARTEN = new[] { TOLERANZ_PROZENT, TOLERANZ_KW };

        public const string GELTUNG_GEBAEUDE = "GEBAEUDE";
        public const string GELTUNG_ZONE = "ZONE";

        /// <summary>Die Geltungen; leer = <see cref="GELTUNG_GEBAEUDE"/>.</summary>
        public static readonly IReadOnlyList<string> GELTUNGEN = new[] { GELTUNG_GEBAEUDE, GELTUNG_ZONE };

        // ---- Eingabegrenzen (Hauswerte; sie stehen im CHECK und im Record Vorheizvorgabe) ----

        /// <summary>Die kleinste Vorheizzeit [h].</summary>
        public const int VORHEIZZEIT_MIN_H = Vorheizvorgabe.VORHEIZZEIT_MIN_H;

        /// <summary>Die größte Vorheizzeit [h].</summary>
        public const int VORHEIZZEIT_MAX_H = Vorheizvorgabe.VORHEIZZEIT_MAX_H;

        /// <summary>Die größte Regelgenauigkeit ε [K].</summary>
        public const double REGELGENAUIGKEIT_MAX_K = 5.0;

        // ---- Ergebnis (Tab_ErgebnisGebaeude, Tab_ErgebnisZone) ----

        public const string SPALTE_ERG_VERFAHREN = "Aufheiz_Verfahren";
        public const string SPALTE_ERG_VORHEIZZEIT = "Vorheizzeit_H";
        public const string SPALTE_ERG_BEDARF_MAX = "Vorheizzeit_Bedarf_Max_H";
        public const string SPALTE_ERG_BEZUG = "Vorheiz_Bezug_Kw";
        public const string SPALTE_ERG_DECKEL = "Vorheiz_Deckel_Kw";
        public const string SPALTE_ERG_LEISTUNG = "Vorheiz_Leistung_Kw";
        public const string SPALTE_ERG_SPRUNG_VORLAUF = "Vorheiz_Sprung_Vorlauf_Max_Kw";
        public const string SPALTE_ERG_SPRUNG_LAUF = "Vorheiz_Sprung_Lauf_Max_Kw";
        public const string SPALTE_ERG_TAGE_VERFEHLT = "Vorheiz_Tage_Verfehlt";
        public const string SPALTE_ERG_TAGE_UEBER = "Vorheiz_Tage_Ueber_Vorheizzeit";
        public const string SPALTE_ERG_UNTERSCHREITUNG_MAX = "Vorheiz_Unterschreitung_Max_K";
        public const string SPALTE_ERG_UNTERSCHREITUNG_H = "Vorheiz_Unterschreitung_H";
        public const string SPALTE_ERG_DECKELSTUNDEN = "Vorheiz_Deckelstunden_H";
        public const string SPALTE_ERG_FLOORSTUNDEN = "Vorheiz_Floorstunden_H";
        public const string SPALTE_ERG_NAECHTE = "Vorheiz_Naechte_Ohne_Absenkung";
        public const string SPALTE_ERG_UNERREICHBAR = "Vorheiz_Unerreichbar_Tage";
        public const string SPALTE_ERG_SPERRZEIT = "Vorheiz_Sperrzeit_Tage";

        /// <summary>Die Mehrwärme des Vorheizens — eine Jahressumme, also MWh (Einheitenregel des Kerns, Regel 2).</summary>
        public const string SPALTE_ERG_MEHRWAERME = "Vorheiz_Mehrwaerme_Mwh";

        private static string Q(string s) => "\"" + s + "\"";

        private static string Liste(IEnumerable<string> werte) => string.Join(",", werte.Select(w => "'" + w + "'"));

        private static string Text(string s, IEnumerable<string> werte)
            => "TEXT CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " IN (" + Liste(werte) + "))";

        private static string Ganz(string s, int von, int bis)
            => "INTEGER CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " BETWEEN " + Zahl(von) + " AND " + Zahl(bis) + ")";

        private static string GanzNichtNegativ(string s) => "INTEGER CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " >= 0)";

        private static string NichtNegativ(string s) => "REAL CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " >= 0)";

        private static string Zahl(double d) => d.ToString(CultureInfo.InvariantCulture);

        /// <summary>Die sieben Spalten der Projekteinstellung in Tabellenfolge — Name und Typ samt Klausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN_EINSTELLUNG = new[]
        {
            (TAB_EINSTELLUNGEN, SPALTE_VERFAHREN, Text(SPALTE_VERFAHREN, VERFAHREN)),
            (TAB_EINSTELLUNGEN, SPALTE_VORHEIZZEIT, Ganz(SPALTE_VORHEIZZEIT, VORHEIZZEIT_MIN_H, VORHEIZZEIT_MAX_H)),
            (TAB_EINSTELLUNGEN, SPALTE_TOLERANZ_ART, Text(SPALTE_TOLERANZ_ART, TOLERANZARTEN)),
            (TAB_EINSTELLUNGEN, SPALTE_TOLERANZ, NichtNegativ(SPALTE_TOLERANZ)),
            (TAB_EINSTELLUNGEN, SPALTE_REGELGENAUIGKEIT,
                "REAL CHECK (" + Q(SPALTE_REGELGENAUIGKEIT) + " IS NULL OR (" + Q(SPALTE_REGELGENAUIGKEIT) + " > 0 AND " +
                Q(SPALTE_REGELGENAUIGKEIT) + " <= " + Zahl(REGELGENAUIGKEIT_MAX_K) + "))"),
            (TAB_EINSTELLUNGEN, SPALTE_GELTUNG, Text(SPALTE_GELTUNG, GELTUNGEN)),
            (TAB_EINSTELLUNGEN, SPALTE_HEIZKREIS,
                "INTEGER CHECK (" + Q(SPALTE_HEIZKREIS) + " IS NULL OR " + Q(SPALTE_HEIZKREIS) + " IN (0,1))"),
        };

        /// <summary>Die Spalte der Zone — an der Projekttabelle und ihrem Katalogzwilling.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN_ZONE = new[]
        {
            (TAB_ZONE, SPALTE_ZONE_VORHEIZZEIT, Ganz(SPALTE_ZONE_VORHEIZZEIT, VORHEIZZEIT_MIN_H, VORHEIZZEIT_MAX_H)),
            (TAB_ZONE_STAMM, SPALTE_ZONE_VORHEIZZEIT, Ganz(SPALTE_ZONE_VORHEIZZEIT, VORHEIZZEIT_MIN_H, VORHEIZZEIT_MAX_H)),
        };

        /// <summary>
        /// Die achtzehn Ergebnisgrößen in Tabellenfolge — Name und Typ, dieselben an Gebäude und Zone. Tage (Vereinigungen)
        /// 0 bis 365; Stunden und Nächte sind am Gebäude Summen über die Zonen und deshalb nur nicht negativ; die Sprünge
        /// ohne Bereich (eine Differenz), die Mehrwärme ohne Bereich (Lauf minus Vorlauf).
        /// </summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> ERGEBNISSPALTEN = new[]
        {
            (SPALTE_ERG_VERFAHREN, Text(SPALTE_ERG_VERFAHREN, new[] { VERFAHREN_VORGABE, VERFAHREN_BERECHNET })),
            (SPALTE_ERG_VORHEIZZEIT, Ganz(SPALTE_ERG_VORHEIZZEIT, 0, VORHEIZZEIT_MAX_H)),
            (SPALTE_ERG_BEDARF_MAX, GanzNichtNegativ(SPALTE_ERG_BEDARF_MAX)),
            (SPALTE_ERG_BEZUG, NichtNegativ(SPALTE_ERG_BEZUG)),
            (SPALTE_ERG_DECKEL, NichtNegativ(SPALTE_ERG_DECKEL)),
            (SPALTE_ERG_LEISTUNG, NichtNegativ(SPALTE_ERG_LEISTUNG)),
            (SPALTE_ERG_SPRUNG_VORLAUF, "REAL"),
            (SPALTE_ERG_SPRUNG_LAUF, "REAL"),
            (SPALTE_ERG_TAGE_VERFEHLT, Ganz(SPALTE_ERG_TAGE_VERFEHLT, 0, 365)),
            (SPALTE_ERG_TAGE_UEBER, Ganz(SPALTE_ERG_TAGE_UEBER, 0, 365)),
            (SPALTE_ERG_UNTERSCHREITUNG_MAX, NichtNegativ(SPALTE_ERG_UNTERSCHREITUNG_MAX)),
            (SPALTE_ERG_UNTERSCHREITUNG_H, GanzNichtNegativ(SPALTE_ERG_UNTERSCHREITUNG_H)),
            (SPALTE_ERG_DECKELSTUNDEN, GanzNichtNegativ(SPALTE_ERG_DECKELSTUNDEN)),
            (SPALTE_ERG_FLOORSTUNDEN, GanzNichtNegativ(SPALTE_ERG_FLOORSTUNDEN)),
            (SPALTE_ERG_NAECHTE, GanzNichtNegativ(SPALTE_ERG_NAECHTE)),
            (SPALTE_ERG_UNERREICHBAR, Ganz(SPALTE_ERG_UNERREICHBAR, 0, 365)),
            (SPALTE_ERG_SPERRZEIT, Ganz(SPALTE_ERG_SPERRZEIT, 0, 365)),
            (SPALTE_ERG_MEHRWAERME, "REAL"),
        };

        /// <summary>Die Ergebnisspalten an beiden Tabellen (Gebäude vor Zone).</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN_ERGEBNIS =
            ERGEBNISSPALTEN.Select(s => (TAB_ERGEBNIS_GEBAEUDE, s.Spalte, s.Typ))
                           .Concat(ERGEBNISSPALTEN.Select(s => (TAB_ERGEBNIS_ZONE, s.Spalte, s.Typ))).ToArray();

        /// <summary>Alle Spalten, die der Schritt an bestehende Tabellen anhängt.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN =
            SPALTEN_EINSTELLUNG.Concat(SPALTEN_ZONE).Concat(SPALTEN_ERGEBNIS).ToArray();

        /// <summary>Spalten von <c>Tab_ErgebnisGebaeude</c> nach dem Schritt (Vorgänger <see cref="AufheizAufschlagErgebnisSchema"/>).</summary>
        public const int SPALTENZAHL_ERGEBNIS_GEBAEUDE = AufheizAufschlagErgebnisSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE + 18;

        /// <summary>Spalten von <c>Tab_ErgebnisZone</c> nach dem Schritt (Vorgänger <see cref="ZonenKaeltespitzeSchema"/>).</summary>
        public const int SPALTENZAHL_ERGEBNIS_ZONE = ZonenKaeltespitzeSchema.SPALTENZAHL_ERGEBNIS_ZONE + 18;

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() =>
            new[] { TAB_EINSTELLUNGEN, TAB_ZONE, TAB_ZONE_STAMM, TAB_ERGEBNIS_GEBAEUDE, TAB_ERGEBNIS_ZONE };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Steht der Schritt — alle Spalten? (Wächter der Leser, etwa für einen älteren Stand auf iOS.)</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Wie viele Einstellungszeilen tragen ein Verfahren außer der Rampe? (Nachweis: nach dem Schritt 0.)</summary>
        public static int ZeilenMitVerfahren()
        {
            if (!DataRepository.SpalteVorhanden(TAB_EINSTELLUNGEN, SPALTE_VERFAHREN)) return 0;
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + Q(TAB_EINSTELLUNGEN) + " WHERE " + Q(SPALTE_VERFAHREN) + " IS NOT NULL AND " +
                Q(SPALTE_VERFAHREN) + " <> 'RAMPE'");
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus (wiederholbar); Rückgabe = Zahl der angelegten Spalten. Fehlt eine
        /// vorausgesetzte Tabelle, wirft er benannt. <b>Kein DML.</b>
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - Vorheizen (Einstellung, Zone, Ergebnis); nichts zu tun");
                return 0;
            }
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            if (bericht != null)
            {
                foreach (string z in zeilen) bericht.Add(z);
                bericht.Add("KEIN DML an Bestandsdaten, der Referenzlauf bleibt byte-gleich");
            }
            return offen.Count;
        }

        // ---- Übersetzung Persistenzwert <-> Kern ----

        /// <summary>Das Verfahren aus dem Persistenzwert; leer und unbekannt = Sollwertrampe.</summary>
        public static Aufheizverfahren VerfahrenAus(string wert) => wert switch
        {
            VERFAHREN_VORGABE => Aufheizverfahren.Vorgabe,
            VERFAHREN_BERECHNET => Aufheizverfahren.Berechnet,
            _ => Aufheizverfahren.Sollwertrampe,
        };

        /// <summary>Der Persistenzwert des Verfahrens; die Sollwertrampe (Vorgabe) als <c>null</c>.</summary>
        public static string VerfahrenWert(Aufheizverfahren v) => v switch
        {
            Aufheizverfahren.Vorgabe => VERFAHREN_VORGABE,
            Aufheizverfahren.Berechnet => VERFAHREN_BERECHNET,
            _ => null,
        };

        /// <summary>Die Toleranzart aus dem Persistenzwert; leer = Prozent.</summary>
        public static Vorheiztoleranzart ToleranzartAus(string wert)
            => wert == TOLERANZ_KW ? Vorheiztoleranzart.Kilowatt : Vorheiztoleranzart.Prozent;

        /// <summary>Der Persistenzwert der Toleranzart; Prozent (Vorgabe) als <c>null</c>.</summary>
        public static string ToleranzartWert(Vorheiztoleranzart a) => a == Vorheiztoleranzart.Kilowatt ? TOLERANZ_KW : null;

        /// <summary>Die Geltung aus dem Persistenzwert; leer = Gebäude.</summary>
        public static Vorheizgeltung GeltungAus(string wert) => wert == GELTUNG_ZONE ? Vorheizgeltung.Zone : Vorheizgeltung.Gebaeude;

        /// <summary>Der Persistenzwert der Geltung; Gebäude (Vorgabe) als <c>null</c>.</summary>
        public static string GeltungWert(Vorheizgeltung g) => g == Vorheizgeltung.Zone ? GELTUNG_ZONE : null;

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
