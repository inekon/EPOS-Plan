using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KK - DIE RAUMGEFUEHRTE KUEHLKURVE (Entwurf KK, Festlegungen 1, 3, 7, 12, 15; Anwenderentscheide
    // E106 und E107; Schemaschritt S1 = 202 des Wellenplans, Welle KK4; haengt an 201 Ak3KSchema).
    //
    // WOZU. Der Kuehlvorlauf des Gebaeudes gleitet nach einer Zwei-Punkt-Kurve ueber die
    // Aussentemperatur (Fusspunkt, Auslegungspunkt) und - auf AK3 - mit einem Raumeinfluss. Die
    // Eingaben stehen am Gebaeude, die Kennzahlen am Projektergebnis:
    //
    //   Tab_Gebaeude, Tab_Gebaeude_STAMM  Kuehlkurve_Aktiv             INTEGER 0/1        NULL = aus (fester Vorlauf)
    //                                     Kuehlkurve_Fusspunkt         REAL 4..22 [°C]    NULL = Auslegungsruecklauf
    //                                     Kuehlkurve_Raumeinfluss      REAL 0..10 [K/K]   NULL oder 0 = aus
    //                                     Kuehlkurve_Auslegung_Weg     TEXT IN (stunde, tagesmittel, eingabe); NULL = tagesmittel
    //                                     Kuehlkurve_Auslegung_Aussen  REAL 0..60 [°C]    nur mit dem Weg eingabe
    //   Tab_ErgebnisEnergiebedarf         Kuehlkurve_Vorlauf_Mittel_C  REAL [°C]          mittlerer Kuehlvorlauf der Kuehlstunden
    //                                     Kuehlkurve_Absenkung_Kh      REAL >= 0 [Kh]     Summe der Absenkung durch den Raumeinfluss
    //                                     Kuehlkurve_Vorlaufgrenze_Stunden INTEGER 0..8760 Stunden an der Vorlaufgrenze
    //
    // KEINE ZONENSPALTE (Festlegung 15): Die Kuehluebergabe je Zone nimmt die drei Spalten aus
    // Schritt 137; Auslegungspunkt, Vorlaufgrenze und Kuehlkurve stehen allein am Gebaeude.
    //
    // DER ELFTE SICHTNEUBAU. Der Lauf liest das Gebaeude ueber Abfrage_Projektgebaeude; die fuenf
    // Spalten stehen dort HINTER dem Raumeinfluss der Heizkurve (110 Spalten,
    // GebaeudeSchema.SQL_VIEW_KUEHLKURVE). Der Schritt laeuft in Migration, Werkzeug und Testkopie
    // ZULETZT und baut die Sicht, sobald sie nicht seine Form hat.
    //
    // KEIN DML AN BESTANDSDATEN. Jede Bestandszeile steht danach auf NULL - jedes Projekt rechnet wie
    // vorher (Festlegung 1). Alles in EINEM Vorgang mit abgeschalteten Fremdschluesseln; der Schritt
    // ist wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>KK</b> — die Eingaben der Kühlkurve je Gebäude (Entwurf KK, Festlegungen 1, 3, 7) und ihre Kennzahlen im
    /// Projektergebnis (Festlegung 12): EINE Quelle für Migration, Werkzeug, Testkopie, Controller und Nachweis
    /// (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class KuehlkurveSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> (Schritt 202) — die EINE Stelle, an der sie steht: der Schritt hinter der
        /// zuletzt gebauten Klasse <see cref="Ak3KSchema"/> (Schritt 201).
        /// </summary>
        public const int SCHRITT = Ak3KSchema.SCHRITT + 1;

        /// <summary>Der Fußpunkt: untere Grenze [°C] (Festlegung 7, Bereich der Vorlaufgrenze).</summary>
        public const double FUSSPUNKT_MIN = 4.0;

        /// <summary>Der Fußpunkt: obere Grenze [°C] (Festlegung 7).</summary>
        public const double FUSSPUNKT_MAX = 22.0;

        /// <summary>Der Raumeinfluss: obere Grenze [K/K] (Festlegung 7, wie <c>Heizkurve_Raumeinfluss</c>).</summary>
        public const double RAUMEINFLUSS_MAX = 10.0;

        /// <summary>Die Auslegungs-Außentemperatur: untere Grenze der Prüfklausel [°C].</summary>
        public const double AUSSEN_MIN = 0.0;

        /// <summary>Die Auslegungs-Außentemperatur: obere Grenze der Prüfklausel [°C].</summary>
        public const double AUSSEN_MAX = 60.0;

        /// <summary>Die drei Auslegungswege als SQL-Literal (Quelle des <c>CHECK</c>; Werte aus <see cref="DbWerte"/>).</summary>
        public const string WERTE_AUSLEGUNG_WEG = "'" + DbWerte.KUEHLKURVE_AUSLEGUNG_STUNDE + "', '" +
                                                  DbWerte.KUEHLKURVE_AUSLEGUNG_TAGESMITTEL + "', '" +
                                                  DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE + "'";

        /// <summary>Die Ergebnistabelle des Projekts (Energiebedarf; Festlegung 12).</summary>
        public const string TAB_ERGEBNIS = SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF;

        /// <summary>Mittlerer Kühlvorlauf der Kühlstunden [°C].</summary>
        public const string SPALTE_VORLAUF_MITTEL = "Kuehlkurve_Vorlauf_Mittel_C";

        /// <summary>Summe der Absenkung durch den Raumeinfluss [Kh].</summary>
        public const string SPALTE_ABSENKUNG_KH = "Kuehlkurve_Absenkung_Kh";

        /// <summary>Stunden am unteren Rand (Vorlaufgrenze) [h].</summary>
        public const string SPALTE_VORLAUFGRENZE_STUNDEN = "Kuehlkurve_Vorlaufgrenze_Stunden";

        /// <summary>Die Ergebnisspalten in Schreibreihenfolge.</summary>
        public static readonly IReadOnlyList<string> SPALTEN_ERGEBNIS = new[]
        {
            SPALTE_VORLAUF_MITTEL, SPALTE_ABSENKUNG_KH, SPALTE_VORLAUFGRENZE_STUNDEN,
        };

        private static string Q(string s) => "\"" + s + "\"";

        private static string Bereich(string s, double min, double max)
            => "REAL CHECK (" + Q(s) + " IS NULL OR (" + Q(s) + " >= " + Z(min) + " AND " + Q(s) + " <= " + Z(max) + "))";

        private static string Z(double x) => x.ToString("0.0###", CultureInfo.InvariantCulture);

        /// <summary>Die fünf Gebäudespalten samt Typ und Prüfklausel, in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> GEBAEUDESPALTEN = new[]
        {
            (GebaeudeSchema.SPALTE_KUEHLKURVE_AKTIV,
             "INTEGER CHECK (" + Q(GebaeudeSchema.SPALTE_KUEHLKURVE_AKTIV) + " IN (0,1))"),
            (GebaeudeSchema.SPALTE_KUEHLKURVE_FUSSPUNKT,
             Bereich(GebaeudeSchema.SPALTE_KUEHLKURVE_FUSSPUNKT, FUSSPUNKT_MIN, FUSSPUNKT_MAX)),
            (GebaeudeSchema.SPALTE_KUEHLKURVE_RAUMEINFLUSS,
             Bereich(GebaeudeSchema.SPALTE_KUEHLKURVE_RAUMEINFLUSS, 0.0, RAUMEINFLUSS_MAX)),
            (GebaeudeSchema.SPALTE_KUEHLKURVE_AUSLEGUNG_WEG,
             "TEXT CHECK (" + Q(GebaeudeSchema.SPALTE_KUEHLKURVE_AUSLEGUNG_WEG) + " IN (" + WERTE_AUSLEGUNG_WEG + "))"),
            (GebaeudeSchema.SPALTE_KUEHLKURVE_AUSLEGUNG_AUSSEN,
             Bereich(GebaeudeSchema.SPALTE_KUEHLKURVE_AUSLEGUNG_AUSSEN, AUSSEN_MIN, AUSSEN_MAX)),
        };

        /// <summary>Alle dreizehn Spalten des Schritts: Tabelle, Spalte, Typ samt Prüfklausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN =
            GEBAEUDESPALTEN.Select(s => (GebaeudeSchema.TAB_GEBAEUDE, s.Spalte, s.Typ))
                .Concat(GEBAEUDESPALTEN.Select(s => (GebaeudeSchema.TAB_GEBAEUDE_STAMM, s.Spalte, s.Typ)))
                .Concat(new[]
                {
                    (TAB_ERGEBNIS, SPALTE_VORLAUF_MITTEL, "REAL"),
                    (TAB_ERGEBNIS, SPALTE_ABSENKUNG_KH,
                     "REAL CHECK (" + Q(SPALTE_ABSENKUNG_KH) + " IS NULL OR " + Q(SPALTE_ABSENKUNG_KH) + " >= 0)"),
                    (TAB_ERGEBNIS, SPALTE_VORLAUFGRENZE_STUNDEN,
                     "INTEGER CHECK (" + Q(SPALTE_VORLAUFGRENZE_STUNDEN) + " IS NULL OR " + Q(SPALTE_VORLAUFGRENZE_STUNDEN) +
                     " BETWEEN 0 AND 8760)"),
                })
                .ToArray();

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IReadOnlyList<string> Voraussetzungen()
            => new[] { GebaeudeSchema.TAB_GEBAEUDE, GebaeudeSchema.TAB_GEBAEUDE_STAMM, TAB_ERGEBNIS };

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Steht der Schritt? Alle Spalten und die Sicht in der Form des elften Durchgangs.</summary>
        public static bool Vollstaendig() =>
            SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)) && SichtSteht();

        /// <summary>Stehen die Gebäudespalten an der Tabelle (für Leser, die auf einem älteren Stand weiterlaufen)?</summary>
        public static bool Vorhanden(string tabelle)
            => GEBAEUDESPALTEN.All(s => DataRepository.SpalteVorhanden(tabelle, s.Spalte));

        /// <summary>Stehen alle Ergebnisspalten an <see cref="TAB_ERGEBNIS"/>?</summary>
        public static bool ErgebnisspaltenVorhanden()
            => SPALTEN_ERGEBNIS.All(s => DataRepository.SpalteVorhanden(TAB_ERGEBNIS, s));

        /// <summary>Beginnt die Spaltenfolge der Sicht mit <see cref="GebaeudeSchema.SICHT_KUEHLKURVE"/>?</summary>
        public static bool SichtSteht()
        {
            List<string> ist = GebaeudeSchema.SichtSpalten();
            string[] soll = GebaeudeSchema.SICHT_KUEHLKURVE;
            return ist.Count >= soll.Length && ist.Take(soll.Length).SequenceEqual(soll, StringComparer.Ordinal);
        }

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang mit abgeschalteten Fremdschlüsseln aus — für die Migration der
        /// Schale, <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>. <b>Wiederholbar:</b> Eine
        /// stehende Spalte wird übergangen, die Sicht nur neu gebaut, wenn sie nicht die Form des elften
        /// Durchgangs hat; steht alles, öffnet er keinen Vorgang. <b>Kein DML an Bestandsdaten.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 13); den Sichtneubau nennt der Bericht.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            bool sicht = !SichtSteht();
            if (offen.Count == 0 && !sicht)
            {
                bericht?.Add("steht bereits - die Kuehlkurve an beiden Gebaeudetabellen, die Sicht und die Kennzahlen an " +
                             TAB_ERGEBNIS + "; nichts zu tun");
                return 0;
            }

            int angelegt = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    if (sicht) v.Ausfuehren(GebaeudeSchema.SQL_VIEW_DROP);
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                        angelegt++;
                    }
                    if (sicht)
                    {
                        v.Ausfuehren(GebaeudeSchema.SQL_VIEW_KUEHLKURVE);
                        zeilen.Add("Sicht " + GebaeudeSchema.VIEW + " neu gebaut (" +
                                   GebaeudeSchema.SICHT_KUEHLKURVE.Length.ToString(CultureInfo.InvariantCulture) + " Spalten)");
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
