using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // UB - UEBERGABEGRENZE UND BIVALENZ DER WAERMEPUMPE (Umsetzungskonzept Uebergabegrenze und Bivalenz,
    // Abschnitt 4.1/4.2; Etappe UB-E2, Welle E2-a; Schemaschritt 203, haengt an 202 KuehlkurveSchema).
    //
    // WOZU. Die Waermepumpe bekommt hydraulische Grenzen (Spreizung, Mindestvolumenstrom, Ruecklauf),
    // das BHKW eine Ruecklaufgrenze, die Anlage ihre Einbindung und den Vorwaermbetrieb; das Ergebnis
    // je Modul und je Projekt fuehrt die Betriebsbereiche, die Zaehler der Grenzen und die
    // Bivalenzpunkte. Ein Schritt fuer alle 43 Spalten (auch die Geraetespalten von UB-E3):
    //
    //   Tab_WP, Tab_WP_STAMM            Spreizung_Auslegung_K           REAL 3..8      NULL = 5 K
    //                                   Spreizung_Max_K                 REAL 5..40     NULL = 10 K
    //                                   Spreizung_Min_K                 REAL 0..8      NULL = 3 K
    //                                   Mindestvolumenstrom_Prozent     REAL 20..100   NULL = 60 %
    //                                   Ruecklauf_Max                   REAL 20..70    NULL = abgeleitet
    //                                   Ruecklauf_Bezug                 REAL 20..40    NULL = 30 °C, wenn abgewertet
    //                                   Ruecklauf_Abwertung_ProzentJeK  REAL 0..5      NULL = keine Abwertung
    //                                   Kaeltemittel                    TEXT           Werteliste im Kern (U-2)
    //   Tab_BHKW, Tab_BHKW_STAMM        Ruecklauf_Max                   REAL 40..90    NULL = keine Grenze
    //   Tab_Energieanlagen              Einbindung                      TEXT IN ('DIREKT','PUFFER','WEICHE'); NULL = Bestandsweg (U-1)
    //                                   Vorwaermbetrieb                 INTEGER 0/1    NULL = 0
    //   Tab_ErgebnisWaermepumpeModul    Bereich_{WpAllein,Parallel,Vorwaermung,NurKessel}_h    INTEGER 0..8760
    //                                   Bereich_{WpAllein,Parallel,Vorwaermung,NurKessel}_MWh  REAL >= 0
    //                                   Spreizung_Unterschritten_h, Ruecklauf_Ueberschritten_h INTEGER 0..8760
    //                                   Bivalenzpunkt_1, Bivalenzpunkt_2  REAL
    //                                   Uebergabe_Max_kW                REAL >= 0
    //   Tab_ErgebnisWaermepumpe         die vier Stunden-, vier Waerme- und zwei Zaehlerspalten wie oben
    //
    // KEIN DML AN BESTANDSDATEN. Jede Bestandszeile steht danach auf NULL - jedes Projekt rechnet wie
    // vorher (U-1). Alles in EINEM Vorgang mit abgeschalteten Fremdschluesseln; wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>UB</b> — Grenzen der Übergabe an Wärmepumpe und BHKW, Einbindung und Vorwärmbetrieb der Anlage, Betriebsbereiche
    /// und Zähler im Ergebnis (Umsetzungskonzept Übergabegrenze und Bivalenz, Abschnitt 4): EINE Quelle für Migration,
    /// Werkzeug, Testkopie, Controller und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class UebergabegrenzeSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> (Schritt 203) — die EINE Stelle, an der sie steht: der Schritt hinter der
        /// zuletzt gebauten Klasse <see cref="KuehlkurveSchema"/> (Schritt 202).
        /// </summary>
        public const int SCHRITT = KuehlkurveSchema.SCHRITT + 1;

        /// <summary>Die Wärmepumpe im Projekt.</summary>
        public const string TAB_WP = ErzeugerTeillastSchema.TAB_WP;

        /// <summary>Der Wärmepumpenkatalog.</summary>
        public const string TAB_WP_STAMM = ErzeugerTeillastSchema.TAB_WP_STAMM;

        /// <summary>Das BHKW im Projekt.</summary>
        public const string TAB_BHKW = ErzeugerTeillastSchema.TAB_BHKW;

        /// <summary>Der BHKW-Katalog.</summary>
        public const string TAB_BHKW_STAMM = ErzeugerTeillastSchema.TAB_BHKW_STAMM;

        /// <summary>Die Anlagen des Projekts.</summary>
        public const string TAB_ANLAGEN = SchemaKatalog.TAB_ENERGIEANLAGEN;

        /// <summary>Das Ergebnis je Wärmepumpenmodul.</summary>
        public const string TAB_ERGEBNIS_MODUL = KuehlungSchema.TAB_ERGEBNIS_WP_MODUL;

        /// <summary>Das Ergebnis der Wärmepumpen je Projekt.</summary>
        public const string TAB_ERGEBNIS = SchemaKatalog.TAB_ERGEBNISWAERMEPUMPE;

        // ---- Gerätespalten der Wärmepumpe ----

        /// <summary>Auslegungsspreizung [K]; NULL = 5 K.</summary>
        public const string SPALTE_SPREIZUNG_AUSLEGUNG = "Spreizung_Auslegung_K";

        /// <summary>Größte zulässige Spreizung [K]; NULL = 10 K.</summary>
        public const string SPALTE_SPREIZUNG_MAX = "Spreizung_Max_K";

        /// <summary>Kleinste zulässige Spreizung [K]; NULL = 3 K.</summary>
        public const string SPALTE_SPREIZUNG_MIN = "Spreizung_Min_K";

        /// <summary>Mindestvolumenstrom [% des Nennvolumenstroms]; NULL = 60 %.</summary>
        public const string SPALTE_MINDESTVOLUMENSTROM = "Mindestvolumenstrom_Prozent";

        /// <summary>Größter zulässiger Rücklauf [°C] (auch am BHKW); NULL = abgeleitet bzw. keine Grenze.</summary>
        public const string SPALTE_RUECKLAUF_MAX = "Ruecklauf_Max";

        /// <summary>Bezugsrücklauf der Abwertung [°C]; NULL = 30 °C, wenn abgewertet.</summary>
        public const string SPALTE_RUECKLAUF_BEZUG = "Ruecklauf_Bezug";

        /// <summary>Abwertung der Leistungszahl je Kelvin Rücklauf über dem Bezug [%/K]; NULL = keine.</summary>
        public const string SPALTE_RUECKLAUF_ABWERTUNG = "Ruecklauf_Abwertung_ProzentJeK";

        /// <summary>Kältemittel (Werteliste im Kern, U-2); NULL = allgemeine Vorgaben.</summary>
        public const string SPALTE_KAELTEMITTEL = "Kaeltemittel";

        // ---- Anlagenspalten ----

        /// <summary>Hydraulische Einbindung der Wärmepumpe; NULL = Bestandsweg (U-1).</summary>
        public const string SPALTE_EINBINDUNG = "Einbindung";

        /// <summary>Vorwärmbetrieb (0/1); NULL = 0.</summary>
        public const string SPALTE_VORWAERMBETRIEB = "Vorwaermbetrieb";

        /// <summary>Die drei Einbindungen als SQL-Literal (Quelle des <c>CHECK</c>).</summary>
        public const string WERTE_EINBINDUNG = "'DIREKT','PUFFER','WEICHE'";

        // ---- Ergebnisspalten ----

        public const string SPALTE_BEREICH_WPALLEIN_H = "Bereich_WpAllein_h";
        public const string SPALTE_BEREICH_PARALLEL_H = "Bereich_Parallel_h";
        public const string SPALTE_BEREICH_VORWAERMUNG_H = "Bereich_Vorwaermung_h";
        public const string SPALTE_BEREICH_NURKESSEL_H = "Bereich_NurKessel_h";
        public const string SPALTE_BEREICH_WPALLEIN_MWH = "Bereich_WpAllein_MWh";
        public const string SPALTE_BEREICH_PARALLEL_MWH = "Bereich_Parallel_MWh";
        public const string SPALTE_BEREICH_VORWAERMUNG_MWH = "Bereich_Vorwaermung_MWh";
        public const string SPALTE_BEREICH_NURKESSEL_MWH = "Bereich_NurKessel_MWh";
        public const string SPALTE_SPREIZUNG_UNTERSCHRITTEN_H = "Spreizung_Unterschritten_h";
        public const string SPALTE_RUECKLAUF_UEBERSCHRITTEN_H = "Ruecklauf_Ueberschritten_h";
        public const string SPALTE_BIVALENZPUNKT_1 = "Bivalenzpunkt_1";
        public const string SPALTE_BIVALENZPUNKT_2 = "Bivalenzpunkt_2";
        public const string SPALTE_UEBERGABE_MAX = "Uebergabe_Max_kW";

        private static string Q(string s) => "\"" + s + "\"";

        private static string Z(double x) => x.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Bereich(string s, double min, double max)
            => "REAL CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " BETWEEN " + Z(min) + " AND " + Z(max) + ")";

        /// <summary>Die acht Gerätespalten der Wärmepumpe samt Typ und Prüfklausel, in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> WP_SPALTEN = new[]
        {
            (SPALTE_SPREIZUNG_AUSLEGUNG, Bereich(SPALTE_SPREIZUNG_AUSLEGUNG, 3, 8)),
            (SPALTE_SPREIZUNG_MAX, Bereich(SPALTE_SPREIZUNG_MAX, 5, 40)),
            (SPALTE_SPREIZUNG_MIN, Bereich(SPALTE_SPREIZUNG_MIN, 0, 8)),
            (SPALTE_MINDESTVOLUMENSTROM, Bereich(SPALTE_MINDESTVOLUMENSTROM, 20, 100)),
            (SPALTE_RUECKLAUF_MAX, Bereich(SPALTE_RUECKLAUF_MAX, 20, 70)),
            (SPALTE_RUECKLAUF_BEZUG, Bereich(SPALTE_RUECKLAUF_BEZUG, 20, 40)),
            (SPALTE_RUECKLAUF_ABWERTUNG, Bereich(SPALTE_RUECKLAUF_ABWERTUNG, 0, 5)),
            (SPALTE_KAELTEMITTEL, "TEXT"),
        };

        /// <summary>Die Rücklaufgrenze des BHKW samt Prüfklausel.</summary>
        public static readonly (string Spalte, string Typ) BHKW_SPALTE =
            (SPALTE_RUECKLAUF_MAX, Bereich(SPALTE_RUECKLAUF_MAX, 40, 90));

        /// <summary>Die zwei Anlagenspalten.</summary>
        public static readonly IReadOnlyList<string> SPALTEN_ANLAGE = new[] { SPALTE_EINBINDUNG, SPALTE_VORWAERMBETRIEB };

        /// <summary>Die zehn Ergebnisspalten, die Modul- und Projektergebnis gemeinsam führen, in Schreibreihenfolge.</summary>
        public static readonly IReadOnlyList<string> SPALTEN_ERGEBNIS = new[]
        {
            SPALTE_BEREICH_WPALLEIN_H, SPALTE_BEREICH_PARALLEL_H, SPALTE_BEREICH_VORWAERMUNG_H, SPALTE_BEREICH_NURKESSEL_H,
            SPALTE_BEREICH_WPALLEIN_MWH, SPALTE_BEREICH_PARALLEL_MWH, SPALTE_BEREICH_VORWAERMUNG_MWH, SPALTE_BEREICH_NURKESSEL_MWH,
            SPALTE_SPREIZUNG_UNTERSCHRITTEN_H, SPALTE_RUECKLAUF_UEBERSCHRITTEN_H,
        };

        /// <summary>Die dreizehn Ergebnisspalten je Modul: die zehn gemeinsamen, die Bivalenzpunkte, die größte Übergabe.</summary>
        public static readonly IReadOnlyList<string> SPALTEN_ERGEBNIS_MODUL =
            SPALTEN_ERGEBNIS.Concat(new[] { SPALTE_BIVALENZPUNKT_1, SPALTE_BIVALENZPUNKT_2, SPALTE_UEBERGABE_MAX }).ToArray();

        /// <summary>Typ samt Prüfklausel einer Ergebnisspalte.</summary>
        private static string ErgebnisTyp(string s)
        {
            if (s == SPALTE_BIVALENZPUNKT_1 || s == SPALTE_BIVALENZPUNKT_2) return "REAL";
            if (s.EndsWith("_h", StringComparison.Ordinal)) return AnlagenfahrplanSchema.Stunden(s);
            return AnlagenfahrplanSchema.NichtNegativ(s);
        }

        /// <summary>Alle 43 Spalten des Schritts: Tabelle, Spalte, Typ samt Prüfklausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN =
            WP_SPALTEN.Select(s => (TAB_WP, s.Spalte, s.Typ))
                .Concat(WP_SPALTEN.Select(s => (TAB_WP_STAMM, s.Spalte, s.Typ)))
                .Concat(new[]
                {
                    (TAB_BHKW, BHKW_SPALTE.Spalte, BHKW_SPALTE.Typ),
                    (TAB_BHKW_STAMM, BHKW_SPALTE.Spalte, BHKW_SPALTE.Typ),
                    (TAB_ANLAGEN, SPALTE_EINBINDUNG,
                     "TEXT CHECK (" + Q(SPALTE_EINBINDUNG) + " IS NULL OR " + Q(SPALTE_EINBINDUNG) + " IN (" + WERTE_EINBINDUNG + "))"),
                    (TAB_ANLAGEN, SPALTE_VORWAERMBETRIEB,
                     "INTEGER CHECK (" + Q(SPALTE_VORWAERMBETRIEB) + " IS NULL OR " + Q(SPALTE_VORWAERMBETRIEB) + " IN (0,1))"),
                })
                .Concat(SPALTEN_ERGEBNIS_MODUL.Select(s => (TAB_ERGEBNIS_MODUL, s, ErgebnisTyp(s))))
                .Concat(SPALTEN_ERGEBNIS.Select(s => (TAB_ERGEBNIS, s, ErgebnisTyp(s))))
                .ToArray();

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IReadOnlyList<string> Voraussetzungen()
            => new[] { TAB_WP, TAB_WP_STAMM, TAB_BHKW, TAB_BHKW_STAMM, TAB_ANLAGEN, TAB_ERGEBNIS_MODUL, TAB_ERGEBNIS };

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Stehen alle 43 Spalten?</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Stehen die acht Gerätespalten an der Wärmepumpentabelle (für Leser auf einem älteren Stand)?</summary>
        public static bool GeraetespaltenVorhanden(string tabelle)
            => WP_SPALTEN.All(s => DataRepository.SpalteVorhanden(tabelle, s.Spalte));

        /// <summary>Stehen die zwei Spalten an der Anlage?</summary>
        public static bool AnlagenspaltenVorhanden()
            => SPALTEN_ANLAGE.All(s => DataRepository.SpalteVorhanden(TAB_ANLAGEN, s));

        /// <summary>Stehen alle Ergebnisspalten an Modul- und Projektergebnis (Wächter für <c>ErgebnisCtrl</c>)?</summary>
        public static bool ErgebnisspaltenVorhanden()
            => SPALTEN_ERGEBNIS_MODUL.All(s => DataRepository.SpalteVorhanden(TAB_ERGEBNIS_MODUL, s))
               && SPALTEN_ERGEBNIS.All(s => DataRepository.SpalteVorhanden(TAB_ERGEBNIS, s));

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang mit abgeschalteten Fremdschlüsseln aus — für die Migration der Schale,
        /// <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>. <b>Wiederholbar:</b> Eine stehende Spalte wird
        /// übergangen; steht alles, öffnet er keinen Vorgang. <b>Kein DML an Bestandsdaten.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 43).</returns>
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
                bericht?.Add("steht bereits - Grenzen an Waermepumpe und BHKW, Einbindung und Vorwaermbetrieb an " + TAB_ANLAGEN +
                             ", Bereiche und Zaehler im Ergebnis; nichts zu tun");
                return 0;
            }

            int angelegt = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                        angelegt++;
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
                foreach (string z in zeilen) bericht.Add(z);
            bericht?.Add("KEIN DML an Bestandsdaten, der Referenzlauf bleibt byte-gleich");
            return angelegt;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
