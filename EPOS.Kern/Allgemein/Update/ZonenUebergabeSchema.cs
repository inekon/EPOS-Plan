using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // AK1z - DIE WAERMEUEBERGABE JE ZONE (Entscheid E63).
    //
    // WOZU. Die Zone traegt schon Art, Exponent und Nennleistung der Waermeuebergabe. Damit jede Zone ihren
    // eigenen Heizkreis rechnen kann, kommen der Auslegungspunkt (Vorlauf, Ruecklauf, Raumtemperatur) und das
    // Proportionalband des Reglers dazu; das Ergebnis der Zone nimmt die mittleren Kreistemperaturen und die
    // Stunden mit begrenzter Uebergabe auf:
    //
    //   Tab_Zone          Auslegung_Vorlauf, Auslegung_Ruecklauf, Auslegung_Raumtemperatur   REAL   NULL = wie Gebaeude
    //                     Regler_Proportionalband                                         REAL >= 0
    //   Tab_ErgebnisZone  Vorlauf_Mittel_C, Ruecklauf_Mittel_C                            REAL
    //                     Uebergabe_Begrenzt_H                                            REAL 0..8760
    //
    // Auslegungsaussentemperatur, Heizkreisschalter, Heizkurve und Sollwertprofil bleiben allein am Gebaeude.
    // Vorlauf > Ruecklauf prueft der Eingang, nicht das DDL (die Gebaeudespalten tragen ebenfalls keine Klausel).
    //
    // KEIN DML, KEIN SICHTNEUBAU. Jede Bestandszeile steht danach auf NULL - jede Zone rechnet wie ihr Gebaeude,
    // der Referenzlauf bleibt byte-gleich. Alles in EINEM Vorgang; der Schritt ist wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>AK1z</b> — die Wärmeübergabe je Zone (E63): EINE Quelle für Migration, Werkzeug, Testkopie und
    /// Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class ZonenUebergabeSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der Schritt hinter der
        /// zuletzt auf <c>origin</c> liegenden Klasse.
        /// </summary>
        public const int SCHRITT = ErdreichVorgabeSchema.SCHRITT + 1;

        /// <summary>Die Zonentabelle.</summary>
        public const string TAB_ZONE = ZonenSchema.TAB_ZONE;

        /// <summary>Die Ergebnistabelle der Zonen.</summary>
        public const string TAB_ERGEBNIS_ZONE = ZonenkopplungSchema.TAB_ERGEBNIS;

        /// <summary>Auslegungsvorlauf der Zone [°C]; NULL = wie Gebäude.</summary>
        public const string SPALTE_AUSLEGUNG_VORLAUF = GebaeudeSchema.SPALTE_AUSLEGUNG_VORLAUF;

        /// <summary>Auslegungsrücklauf der Zone [°C]; NULL = wie Gebäude.</summary>
        public const string SPALTE_AUSLEGUNG_RUECKLAUF = GebaeudeSchema.SPALTE_AUSLEGUNG_RUECKLAUF;

        /// <summary>Auslegungsraumtemperatur der Zone [°C]; NULL = wie Gebäude, sonst Tag-Sollwert der Zone.</summary>
        public const string SPALTE_AUSLEGUNG_RAUMTEMPERATUR = GebaeudeSchema.SPALTE_AUSLEGUNG_RAUMTEMPERATUR;

        /// <summary>Proportionalband des Reglers der Zone [K]; NULL = wie Gebäude.</summary>
        public const string SPALTE_REGLER_PROPORTIONALBAND = GebaeudeSchema.SPALTE_REGLER_PROPORTIONALBAND;

        /// <summary>Mittlerer Vorlauf des Zonenkreises [°C] im Ergebnis.</summary>
        public const string SPALTE_VORLAUF_MITTEL = "Vorlauf_Mittel_C";

        /// <summary>Mittlerer Rücklauf des Zonenkreises [°C] im Ergebnis.</summary>
        public const string SPALTE_RUECKLAUF_MITTEL = "Ruecklauf_Mittel_C";

        /// <summary>Stunden, in denen die Übergabe der Zone begrenzt hat [h], im Ergebnis.</summary>
        public const string SPALTE_UEBERGABE_BEGRENZT = "Uebergabe_Begrenzt_H";

        /// <summary>Spaltenzahl von <c>Tab_ErgebnisZone</c> nach dem Schritt: die von KP-S4 und die drei Kreisspalten.</summary>
        public const int SPALTENZAHL_ERGEBNIS_ZONE = AufheizManuellSchema.SPALTENZAHL_ERGEBNIS_ZONE + 3;

        /// <summary>Die vier Eingabespalten an <c>Tab_Zone</c>, in Schrittfolge.</summary>
        public static readonly string[] SPALTEN_ZONE =
        {
            SPALTE_AUSLEGUNG_VORLAUF, SPALTE_AUSLEGUNG_RUECKLAUF, SPALTE_AUSLEGUNG_RAUMTEMPERATUR,
            SPALTE_REGLER_PROPORTIONALBAND,
        };

        /// <summary>Die sieben Spalten des Schritts: Tabelle, Spalte, Typ samt Prüfklausel (STRICT, nullbar).</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_ZONE, SPALTE_AUSLEGUNG_VORLAUF, "REAL"),
            (TAB_ZONE, SPALTE_AUSLEGUNG_RUECKLAUF, "REAL"),
            (TAB_ZONE, SPALTE_AUSLEGUNG_RAUMTEMPERATUR, "REAL"),
            (TAB_ZONE, SPALTE_REGLER_PROPORTIONALBAND,
                "REAL CHECK (\"" + SPALTE_REGLER_PROPORTIONALBAND + "\" IS NULL OR \"" + SPALTE_REGLER_PROPORTIONALBAND + "\" >= 0)"),
            (TAB_ERGEBNIS_ZONE, SPALTE_VORLAUF_MITTEL, "REAL"),
            (TAB_ERGEBNIS_ZONE, SPALTE_RUECKLAUF_MITTEL, "REAL"),
            (TAB_ERGEBNIS_ZONE, SPALTE_UEBERGABE_BEGRENZT,
                "REAL CHECK (\"" + SPALTE_UEBERGABE_BEGRENZT + "\" BETWEEN 0 AND 8760)"),
        };

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_ZONE, TAB_ERGEBNIS_ZONE };

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Steht der Schritt? Alle sieben Spalten.</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Tragen die Zonen die vier Eingabespalten (für Leser, die auf einem älteren Stand weiterlaufen)?</summary>
        public static bool ZoneVorhanden()
            => DataRepository.TabelleVorhanden(TAB_ZONE)
               && SPALTEN_ZONE.All(s => DataRepository.SpalteVorhanden(TAB_ZONE, s));

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang mit abgeschalteten Fremdschlüsseln aus — für die Migration der
        /// Schale, <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>. <b>Wiederholbar:</b> Eine
        /// stehende Spalte wird übergangen; steht alles, öffnet er keinen Vorgang. <b>Kein DML, kein Sichtneubau.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 7).</returns>
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
                bericht?.Add("steht bereits - Uebergabespalten an Tab_Zone und Tab_ErgebnisZone; nichts zu tun");
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
            GebaeudeZonenanschluss.ProbeVerwerfen();
            if (bericht != null)
                foreach (string z in zeilen) bericht.Add(z);
            bericht?.Add("KEIN DML an Bestandsdaten, der Referenzlauf bleibt byte-gleich");
            return angelegt;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
