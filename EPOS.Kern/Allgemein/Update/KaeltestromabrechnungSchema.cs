using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KU3-4d - DIE KAELTESTROMABRECHNUNG DER KAELTEMASCHINE (Kuehlkonzept 6.1-6.3; Plan G7b bis KU3
    // Abschnitt 6, Zeile KU3-4d; Entscheide E33-E35, E67/E68).
    //
    // WAS.
    //   Tab_ErgebnisKaeltemaschine  Kaeltestrom_Netzbezug_MWh (der Netzbezug des Kaeltestroms der Maschine),
    //                               Kuehl_ID_Carrier und Kuehl_EigenerZaehler (ein ABWEICHENDER Kuehltraeger
    //                               samt Abrechnungsart, wie die Modulzeile der Waermepumpe) und Stromspitze_kW
    //                               (die Spitze ihres Kaeltestroms je Stunde - der Leistungspreis eines eigenen
    //                               Zaehlers, auch ohne Zeitreihen des Laufs)
    //   Trigger                     trg_Kostenstempel_Tab_Energieanlagen_U neu: seine Spaltenliste fuehrt
    //                               Kaeltemaschine_Anzahl und ID_Kaeltemaschine (KostenStempelSchema.
    //                               SPALTEN_ENERGIEANLAGEN). DROP und CREATE nur, wenn der stehende Trigger die
    //                               Liste nicht deckt - wiederholbar.
    //
    // KEIN TRIGGER AN Tab_Kaeltemaschine: Die Geraetetabellen stempeln nicht (auch Tab_WP nicht) - sie wirken
    // ueber die Simulation, deren Lauf ErgebnisAktuell schon prueft (KostenStempelSchema, Kopf).
    //
    // ERGEBNISNEUTRAL. Kein Referenzprojekt fuehrt eine Kaeltemaschine; die Spalten entstehen leer. Der
    // Referenzlauf bleibt byte-gleich.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>Schemaschritt der Kältestromabrechnung der Kältemaschine</b> (KU3-4d): vier Ergebnisspalten je Maschine
    /// und der erneuerte Stempeltrigger der Anlagenzeile. Wiederholbar und ergebnisneutral.
    /// </summary>
    public static class KaeltestromabrechnungSchema
    {
        /// <summary>Die Nummer des Schritts — hängt an der Vorgängerklasse.</summary>
        public const int SCHRITT = KaeltemaschineAnlageSchema.SCHRITT + 1;

        /// <summary>Die Ergebnistabelle je Maschine.</summary>
        public const string TAB_ERGEBNIS = KaeltemaschineAnlageSchema.TAB_ERGEBNIS;

        /// <summary>Der Netzbezug des Kältestroms der Maschine [MWh/a]; NULL = vor dem Schritt gerechnet.</summary>
        public const string SPALTE_NETZBEZUG = "Kaeltestrom_Netzbezug_MWh";

        /// <summary>Der abweichende Kühlträger der Maschine (<c>energy_carrier.id</c>); NULL = Träger des Projekts.</summary>
        public const string SPALTE_KUEHL_ID_CARRIER = KuehlungSchema.SPALTE_KUEHL_ID_CARRIER;

        /// <summary>Die Abrechnungsart (0/1); NULL = anteilig bzw. kein abweichender Träger.</summary>
        public const string SPALTE_KUEHL_EIGENER_ZAEHLER = KuehlungSchema.SPALTE_KUEHL_EIGENER_ZAEHLER;

        /// <summary>Die Spitze des Kältestroms der Maschine je Stunde [kW].</summary>
        public const string SPALTE_STROMSPITZE = "Stromspitze_kW";

        /// <summary>Die Spalten des Schritts.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_ERGEBNIS, SPALTE_NETZBEZUG, "REAL"),
            (TAB_ERGEBNIS, SPALTE_KUEHL_ID_CARRIER, "INTEGER"),
            (TAB_ERGEBNIS, SPALTE_KUEHL_EIGENER_ZAEHLER,
             "INTEGER CHECK (\"" + SPALTE_KUEHL_EIGENER_ZAEHLER + "\" IN (0,1))"),
            (TAB_ERGEBNIS, SPALTE_STROMSPITZE, "REAL"),
        };

        /// <summary>Der Name des erneuerten Triggers.</summary>
        public static string TriggerName => AnlagenTrigger().Name;

        /// <summary>Was der Schritt voraussetzt.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[]
        {
            TAB_ERGEBNIS, KostenStempelSchema.TAB_ENERGIEANLAGEN, KostenStempelSchema.TAB_PROJEKT
        };

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Der Update-Trigger der Anlagenzeile aus <see cref="KostenStempelSchema.Trigger"/>.</summary>
        public static KostenStempelSchema.Stempeltrigger AnlagenTrigger() => KostenStempelSchema.Trigger.Single(t =>
            t.Tabelle == KostenStempelSchema.TAB_ENERGIEANLAGEN && t.Ereignis == "UPDATE");

        /// <summary>
        /// Deckt der stehende Update-Trigger der Anlagenzeile jede Spalte von
        /// <see cref="KostenStempelSchema.SPALTEN_ENERGIEANLAGEN"/>? <c>false</c> auch, wenn er fehlt.
        /// </summary>
        public static bool TriggerAktuell()
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = ?", new DbParam("?", TriggerName));
            string sql = o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(sql)) return false;
            return KostenStempelSchema.SPALTEN_ENERGIEANLAGEN.All(s => sql.Contains("\"" + s + "\"", StringComparison.Ordinal));
        }

        /// <summary>Stehen Spalten und Trigger?</summary>
        public static bool Vollstaendig() =>
            DataRepository.TabelleVorhanden(TAB_ERGEBNIS)
            && SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte))
            && TriggerAktuell();

        /// <summary>
        /// Legt fehlende Spalten an und erneuert den Trigger, wenn er die Spaltenliste nicht deckt — in EINEM
        /// Vorgang. Wiederholbar; liefert die Zahl der Anweisungen.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            bool triggerNeu = !TriggerAktuell();
            int n = 0;
            if (offen.Count > 0 || triggerNeu)
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                        {
                            v.Ausfuehren(Anlegen(s));
                            bericht?.Add(s.Tabelle + "." + s.Spalte + " angelegt");
                            n++;
                        }
                        if (triggerNeu)
                        {
                            KostenStempelSchema.Stempeltrigger t = AnlagenTrigger();
                            v.Ausfuehren("DROP TRIGGER IF EXISTS \"" + t.Name + "\"");
                            v.Ausfuehren(t.Sql);
                            bericht?.Add("Trigger " + t.Name + " erneuert (Spaltenliste mit " +
                                         KaeltemaschineAnlageSchema.SPALTE_ANZAHL + " und " +
                                         KaeltemaschineAnlageSchema.SPALTE_ID_KAELTEMASCHINE + ")");
                            n += 2;
                        }
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
            }
            else bericht?.Add("Spalten und Trigger der Kaeltestromabrechnung vorhanden");
            bericht?.Add("KEIN Rechenergebnis aendert sich, der Referenzlauf bleibt byte-gleich");
            return n;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
