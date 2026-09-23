using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Eine leere Datenbank für die Tww-Controller</b> (Umsetzungskonzept
    /// Zapfprofilgenerator, Stufe Z0, Posten P5–P8).
    ///
    /// <para><b>Warum nicht die Testdatenbank.</b> <c>Referenzlaeufe/Kenndaten_Test.sqlite</c>
    /// trägt die zehn Tww-Tabellen erst mit Posten P4; bis dahin — und auch danach, weil ihr
    /// fiktiver Katalog kein Gegenstand dieser Fälle ist — legt jeder Fall eine eigene, leere
    /// Datei im Temp-Ordner an: die zehn Tabellen aus <see cref="TwwSchema"/> (dieselbe Quelle
    /// wie Migration und Werkzeug) und die zwei Elterntabellen <c>Tab_Projekt</c> und
    /// <c>Tab_Gebaeude</c>, auf die die Fremdschlüssel zeigen — nur mit ihrer Spalte <c>ID</c>.
    /// Die Testdatenbank selbst wird nie verändert.</para>
    ///
    /// <para><b>Nur erfundene Werte</b> (Konzept Kapitel 6 (a)): runde Zahlen, Herkunftsart
    /// <c>FIKTIV</c>, Quelle „Testkatalog (fiktiv)". Keine Normzahl, kein Produkt.</para>
    ///
    /// <para>Die Klasse setzt <see cref="DataRepository.PfadUeberschreibung"/> (statisch) —
    /// jede nutzende Testklasse trägt deshalb <c>[Collection("Testdatenbank")]</c>.</para>
    /// </summary>
    public sealed class TwwTestdatenbank : IDisposable
    {
        internal const string QUELLE = "Testkatalog (fiktiv)";

        private readonly string _vorher;
        private readonly Func<bool> _schreibrechtVorher;
        private readonly string _ordner;

        public TwwTestdatenbank(bool mitTwwSchema = true)
        {
            _vorher = DataRepository.PfadUeberschreibung;
            _schreibrechtVorher = Schreibnaht.Schreibrecht;
            Schreibnaht.WerkzeugFreigabe("EPOS.Kern.Tests (leere Tww-Datenbank)");

            _ordner = Path.Combine(Path.GetTempPath(),
                                   "epos-twwtest-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_ordner);
            DataRepository.PfadUeberschreibung = Path.Combine(_ordner, "Kenndaten.sqlite");

            DataRepository.ExecuteNonQuery("CREATE TABLE \"Tab_Projekt\" (\"ID\" INTEGER PRIMARY KEY)");
            DataRepository.ExecuteNonQuery("CREATE TABLE \"Tab_Gebaeude\" (\"ID\" INTEGER PRIMARY KEY)");
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_Projekt\" (\"ID\") VALUES (1), (2)");

            if (mitTwwSchema) SchemaAnlegen();
        }

        /// <summary>Die zehn Tabellen und ihre Indizes — genau wie Schemaschritt T1.</summary>
        public static void SchemaAnlegen()
        {
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value);
            foreach (KeyValuePair<string, string> i in TwwSchema.Indizes)
                DataRepository.ExecuteNonQuery(i.Value);
        }

        public void Dispose()
        {
            DataRepository.PfadUeberschreibung = _vorher;
            Schreibnaht.Schreibrecht = _schreibrechtVorher;
            try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { /* wie TestDatenbank */ }
            try { Directory.Delete(_ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
        }

        // =================================================================================
        // Erfundene Katalogzeilen
        // =================================================================================

        /// <summary>Eine Parameterzeile mit erfundenem Schlüssel und Wert.</summary>
        internal static int ParameterAnlegen(string schluessel, double wert, string version,
                                             string einheit = null, string herkunft = TwwSchema.HERKUNFT_FIKTIV)
        {
            return DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO \"Tab_TwwParameter_STAMM\" (\"Schluessel\", \"Wert\", \"Einheit\", \"Katalogversion\", " +
                "\"Quelle\", \"Ausgabe\", \"Version\", \"Herkunftsart\", \"Status\", \"Beleg\", \"ReadOnly\") " +
                "VALUES (?, ?, ?, ?, ?, NULL, ?, ?, 'EIGEN', 'intern', 0)",
                new[]
                {
                    new DbParam("@s", schluessel), new DbParam("@w", wert), new DbParam("@e", einheit),
                    new DbParam("@k", version), new DbParam("@q", QUELLE), new DbParam("@v", version),
                    new DbParam("@h", herkunft)
                });
        }

        /// <summary>
        /// Ein Tagesgangsatz mit den Tagtypen aus <paramref name="tagtypen"/>; jeder Tagesgang
        /// legt je die Hälfte auf die Stunden 7 und 19 (erfunden, Σ 1).
        /// </summary>
        internal static int TagesgangsatzAnlegen(string bezeichner, string version, bool readOnly = false,
                                                 params int[] tagtypen)
        {
            int id = DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO \"Tab_TwwTagesgangsatz_STAMM\" (\"Bezeichner\", \"Katalogversion\", \"Status\", \"ReadOnly\") " +
                "VALUES (?, ?, 'EIGEN', ?)",
                new[] { new DbParam("@b", bezeichner), new DbParam("@k", version), new DbParam("@r", readOnly ? 1 : 0) });

            if (tagtypen == null || tagtypen.Length == 0) tagtypen = new[] { 1, 2, 3, 4 };
            string spalten = string.Join(", ", Enumerable.Range(1, 24).Select(h => "\"Anteil_" + h.ToString("00") + "\""));
            string werte = string.Join(", ", Enumerable.Range(1, 24).Select(h => h == 7 || h == 19 ? "0.5" : "0.0"));
            foreach (int t in tagtypen)
                DataRepository.ExecuteNonQuery(
                    "INSERT INTO \"Tab_TwwTagesgang_STAMM\" (\"ID_Tagesgangsatz\", \"Tagtyp\", " + spalten +
                    ", \"Quelle\", \"Ausgabe\", \"Version\", \"Herkunftsart\") VALUES (?, ?, " + werte + ", ?, NULL, ?, 'FIKTIV')",
                    new DbParam("@id", id), new DbParam("@t", t), new DbParam("@q", QUELLE), new DbParam("@v", version));
            return id;
        }

        /// <summary>
        /// Eine Nutzungsart unmittelbar per SQL — auch mit Status und ReadOnly, die
        /// <c>TwwNutzungsartCtrl.Neu</c> nie vergibt. Erfundene Werte: Bedarf 1/2/3,
        /// Bezugstemperaturen 50/10, Monatsfaktoren 1, Wochenfaktoren Mo–Fr 0,2.
        /// </summary>
        internal static int NutzungsartAnlegen(string bezeichner, string version, int idTagesgangsatz,
                                               string status = TwwSchema.STATUS_EIGEN, bool readOnly = false,
                                               string beleg = null)
        {
            var spalten = new List<string>
            {
                "Bezeichner", "Katalogversion", "Bezugsart", "Bedarf_Niedrig", "Bedarf_Mittel", "Bedarf_Hoch",
                "Bedarf_Mittel_Min", "Bedarf_Mittel_Max",
                "Bedarf_Quelle", "Bedarf_Version", "Bedarf_Herkunftsart",
                "Bezug_Zapftemperatur", "Bezug_Kaltwasser", "Bilanzgrenze", "Kalenderart",
                "Jahresgang_Quelle", "Jahresgang_Version", "Jahresgang_Herkunftsart",
                "Wochengang_Quelle", "Wochengang_Version", "Wochengang_Herkunftsart",
                "ID_Tagesgangsatz", "Status", "Beleg", "ReadOnly"
            };
            var werte = new List<object>
            {
                bezeichner, version, 1, 1.0, 2.0, 3.0,
                1.5, 2.5,
                QUELLE, version, TwwSchema.HERKUNFT_FIKTIV,
                50.0, 10.0, 1, 1,
                QUELLE, version, TwwSchema.HERKUNFT_FIKTIV,
                QUELLE, version, TwwSchema.HERKUNFT_FIKTIV,
                idTagesgangsatz, status, beleg, readOnly ? 1 : 0
            };
            for (int m = 1; m <= 12; m++) { spalten.Add("Monat_" + m); werte.Add(1.0); }
            for (int w = 1; w <= 7; w++) { spalten.Add("Woche_" + w); werte.Add(w <= 5 ? 0.2 : 0.0); }

            string sql = "INSERT INTO \"Tab_TwwNutzungsart_STAMM\" (" +
                         string.Join(", ", spalten.Select(s => "\"" + s + "\"")) + ") VALUES (" +
                         string.Join(", ", spalten.Select(_ => "?")) + ")";
            return DataRepository.ExecuteInsertAndGetId(sql,
                werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray());
        }

        /// <summary>Eine Zone eines Projekts auf eine Nutzungsart.</summary>
        internal static int ZoneAnlegen(int idProjekt, int idNutzungsart, string name, double bezugsmenge,
                                        int reihenfolge = 1)
        {
            return DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO \"Tab_TwwZone\" (\"ID_Projekt\", \"ID_Nutzungsart\", \"Reihenfolge\", \"Name\", \"Bezugsmenge\") " +
                "VALUES (?, ?, ?, ?, ?)",
                new[]
                {
                    new DbParam("@p", idProjekt), new DbParam("@n", idNutzungsart), new DbParam("@r", reihenfolge),
                    new DbParam("@name", name), new DbParam("@b", bezugsmenge)
                });
        }
    }
}
