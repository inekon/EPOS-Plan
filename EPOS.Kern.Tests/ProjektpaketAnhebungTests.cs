using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ein älteres Projektpaket wird beim Import angehoben</b> (Konzept
    /// <c>Dokumentation/aktuell/Konzept_Projektpaket_Migration_EPOS-Plan.md</c>, Prüfweg).
    ///
    /// <para>Alte Pakete liegen nicht im Repositorium. Die Fälle exportieren deshalb aus der
    /// Testdatenbank und bauen das Paket auf den Stand 93 zurück: Schemastand im Manifest,
    /// Spaltenbild und Werte, wie sie vor den umrechnenden Schritten standen. Nach dem
    /// Import müssen die Werte der Quelle wieder dastehen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektpaketAnhebungTests
    {
        /// <summary>Projekt 1049: BHKW, Gebäude und Kollektorfeld.</summary>
        private const string SOLAR = "Referenzprojekt Solarthermie";

        /// <summary>Projekt 1017: Wirtschaftlichkeit, drei Trägerzeilen.</summary>
        private const string WP = "WP_PV-Speicher";

        private const int ALTSTAND = 93;

        // =============================================================================
        //  Das Register
        // =============================================================================

        [Fact]
        public void Jeder_Schemaschritt_bis_zum_Zielstand_nennt_seine_Paketwirkung()
        {
            var nummern = Paketanhebung.Stufen.Select(s => s.Nr).ToList();
            var erwartet = Enumerable.Range(Paketanhebung.UNTERE_GRENZE + 1,
                                            SchemaStand.Zielversion - Paketanhebung.UNTERE_GRENZE).ToList();
            Assert.Equal(erwartet, nummern);

            foreach (Paketanhebung.Stufe s in Paketanhebung.Stufen)
            {
                Assert.False(string.IsNullOrWhiteSpace(s.Text), "Schritt " + s.Nr + " ohne Text.");
                Assert.True(s.Wirkung == Paketanhebung.Art.Umformung
                            ? s.Umformung != null : s.Umformung == null,
                            "Schritt " + s.Nr + ": Art und Umformung passen nicht zusammen.");
            }
        }

        [Fact]
        public void Die_Vorschau_zaehlt_Schritte_und_Umformungen()
        {
            Paketanhebung.Vorschau v = Paketanhebung.Vorschauen(ALTSTAND);
            Assert.True(v.Noetig);
            Assert.False(v.Neuer);
            Assert.False(v.UnterGrenze);
            Assert.Equal(SchemaStand.Zielversion - ALTSTAND, v.Schritte);
            Assert.Equal(Paketanhebung.Stufen.Count(s => s.Nr > ALTSTAND && s.Wirkung == Paketanhebung.Art.Umformung),
                         v.Umformungen);

            Assert.False(Paketanhebung.Vorschauen(SchemaStand.Zielversion).Noetig);
            Assert.False(Paketanhebung.Vorschauen(0).Noetig);
            Assert.True(Paketanhebung.Vorschauen(SchemaStand.Zielversion + 1).Neuer);
            Assert.True(Paketanhebung.Vorschauen(61).UnterGrenze);
        }

        // =============================================================================
        //  Rückgebautes Paket: BHKW, Gebäude, Kollektor (Schritte 98, 99, 101, 148, 150)
        // =============================================================================

        [Fact]
        public void Ein_Paket_auf_Stand_93_wird_gehoben_und_traegt_die_Werte_der_Quelle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = Id(SOLAR);
            Assert.True(quelle > 0);
            DataTable bhkw = DataRepository.GetDataTable(
                "SELECT Bezeichner, Wirkungsgrad, Wirkungsgrad_el, Wirkungsgrad_th FROM Tab_BHKW WHERE ID_Projekt = ? ORDER BY Bezeichner",
                new DbParam("@p", quelle));
            DataTable gebaeude = DataRepository.GetDataTable(
                "SELECT Gebaeudename, Nutzflaeche FROM Tab_Gebaeude WHERE ID_Projekt = ? ORDER BY Gebaeudename",
                new DbParam("@p", quelle));
            Assert.True(bhkw.Rows.Count > 0 && gebaeude.Rows.Count > 0);

            string paket = ordner.Datei("neu.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(SOLAR, paket));

            string alt = ordner.Datei("alt.wpx");
            Umbauen(paket, alt, ALTSTAND, (tabelle, zeile) =>
            {
                if (tabelle == "Tab_BHKW")
                {
                    // Vor 98 stand dort der ELEKTRISCHE Wirkungsgrad in Prozent.
                    double w = Zahl(zeile["Wirkungsgrad"]), pel = Zahl(zeile["Pel"]), pth = Zahl(zeile["Ptherm"]);
                    zeile["Wirkungsgrad"] = Math.Round(w * pel / (pel + pth) * 100.0, 6);
                    zeile.Remove("Wirkungsgrad_el");
                    zeile.Remove("Wirkungsgrad_th");
                }
                else if (tabelle == "Tab_Gebaeude")
                {
                    zeile["Wohnflaeche"] = zeile["Nutzflaeche"]?.DeepClone();
                    zeile.Remove("Nutzflaeche");
                    zeile.Remove("Baujahr");
                    zeile.Remove("Energiestandard");
                    zeile["Baualtersklasse"] = "I";       // alte Klasse I: Niedrigenergiebauweise
                }
                else if (tabelle == "Tab_Solarkollektoren")
                {
                    zeile["Vorlauf"] = 60;                 // vor 150 führte der Kollektor sie
                    zeile["Ruecklauf"] = 30;
                }
            });

            int neu = io.Importieren(alt, "Anhebung 93", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            Assert.Contains(io.LetzterBericht, z => z.Contains(ALTSTAND.ToString(CultureInfo.InvariantCulture))
                                                    && z.Contains(SchemaStand.Zielversion.ToString(CultureInfo.InvariantCulture)));
            Assert.Contains(io.LetzterBericht, z => z.StartsWith("Schritt 98:", StringComparison.Ordinal));
            Assert.Contains(io.LetzterBericht, z => z.StartsWith("Schritt 148:", StringComparison.Ordinal));

            DataTable bhkwNeu = DataRepository.GetDataTable(
                "SELECT Bezeichner, Wirkungsgrad, Wirkungsgrad_el, Wirkungsgrad_th FROM Tab_BHKW WHERE ID_Projekt = ? ORDER BY Bezeichner",
                new DbParam("@p", neu));
            Assert.Equal(bhkw.Rows.Count, bhkwNeu.Rows.Count);
            for (int i = 0; i < bhkw.Rows.Count; i++)
                foreach (string s in new[] { "Wirkungsgrad", "Wirkungsgrad_el", "Wirkungsgrad_th" })
                    Assert.True(Math.Abs(Zahl(bhkw.Rows[i][s]) - Zahl(bhkwNeu.Rows[i][s])) < 1e-3,
                                "Tab_BHKW." + s + ": Quelle " + bhkw.Rows[i][s] + ", nach der Anhebung " + bhkwNeu.Rows[i][s]);

            DataTable gebNeu = DataRepository.GetDataTable(
                "SELECT Gebaeudename, Nutzflaeche, Baualtersklasse, Energiestandard FROM Tab_Gebaeude WHERE ID_Projekt = ? ORDER BY Gebaeudename",
                new DbParam("@p", neu));
            Assert.Equal(gebaeude.Rows.Count, gebNeu.Rows.Count);
            for (int i = 0; i < gebaeude.Rows.Count; i++)
            {
                Assert.Equal(Zahl(gebaeude.Rows[i]["Nutzflaeche"]), Zahl(gebNeu.Rows[i]["Nutzflaeche"]), 6);
                Assert.Equal("J", Convert.ToString(gebNeu.Rows[i]["Baualtersklasse"], CultureInfo.InvariantCulture));
                Assert.Equal(Energiestandard.NIEDRIGENERGIE, Convert.ToString(gebNeu.Rows[i]["Energiestandard"], CultureInfo.InvariantCulture));
            }

            object kollektor = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Solarkollektoren WHERE ID_Projekt = ?", new DbParam("@p", neu));
            Assert.Equal(1L, Convert.ToInt64(kollektor, CultureInfo.InvariantCulture));
        }

        // =============================================================================
        //  Rückgebautes Paket: Wirtschaftlichkeit und Trägerkarte (Schritte 102, 112, 127)
        // =============================================================================

        [Fact]
        public void Freitext_Preisbasis_und_Anlagenart_werden_beim_Heben_nachgezogen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = Id(WP);
            Assert.True(quelle > 0);
            const string FREITEXT = "Imagegewinn beim Betreiber";
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWirtschaftlichkeit SET Nicht_Monetaer = ? WHERE ID_Projekt = ?",
                new DbParam("@t", FREITEXT), new DbParam("@p", quelle)));
            List<string> basis = Preisbasen(quelle);
            Assert.NotEmpty(basis);

            string paket = ordner.Datei("neu.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(WP, paket));

            string alt = ordner.Datei("alt.wpx");
            Umbauen(paket, alt, ALTSTAND, (tabelle, zeile) =>
            {
                if (tabelle == "energy_project_settings") zeile.Remove("Preisbasis");
                else if (tabelle == "Tab_Energieanlagen") zeile["KWKG_Anlagenart"] = "";
            });

            int neu = io.Importieren(alt, "Anhebung 93b", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            Assert.Equal(basis, Preisbasen(neu));

            object leer = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND KWKG_Anlagenart = ''",
                new DbParam("@p", neu));
            Assert.Equal(0L, Convert.ToInt64(leer, CultureInfo.InvariantCulture));

            DataTable wirkung = DataRepository.GetDataTable(
                "SELECT Kategorie, Beschreibung FROM Tab_ProjektWirkung WHERE ID_Projekt = ?", new DbParam("@p", neu));
            Assert.Equal(1, wirkung.Rows.Count);
            Assert.Equal(ProjektWirkungSchema.KATEGORIE_UEBERNAHME, Convert.ToString(wirkung.Rows[0]["Kategorie"], CultureInfo.InvariantCulture));
            Assert.Equal(FREITEXT, Convert.ToString(wirkung.Rows[0]["Beschreibung"], CultureInfo.InvariantCulture));
        }

        // =============================================================================
        //  Zielstand unverändert, Fehler lässt die Datenbank unberührt
        // =============================================================================

        [Fact]
        public void Ein_Paket_auf_Zielstand_wird_nicht_angefasst()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            string paket = ordner.Datei("ziel.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(SOLAR, paket));

            int neu = io.Importieren(paket, "Zielstand", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            Assert.DoesNotContain(io.LetzterBericht, z => z.StartsWith("Schritt ", StringComparison.Ordinal));
        }

        [Fact]
        public void Scheitert_eine_Stufe_bricht_der_Import_ab_und_die_Datenbank_bleibt_unberuehrt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = Id(WP);
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWirtschaftlichkeit SET Nicht_Monetaer = ? WHERE ID_Projekt = ?",
                new DbParam("@t", "Freitext"), new DbParam("@p", quelle)));

            string paket = ordner.Datei("neu.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(WP, paket));

            // Eine Projekt-Id als Text: Die STRICT-Tabelle der Wirkungen (Schritt 127)
            // nimmt sie nicht an — die Stufe scheitert.
            string kaputt = ordner.Datei("kaputt.wpx");
            Umbauen(paket, kaputt, ALTSTAND, (tabelle, zeile) =>
            {
                if (tabelle == "Tab_Projekt") zeile["ID"] = "x" + zeile["ID"];
                else if (tabelle == "Tab_ProjektWirtschaftlichkeit") zeile["ID_Projekt"] = "x" + zeile["ID_Projekt"];
            });

            long vorher = Anzahl("Tab_Projekt");
            int neu = io.Importieren(kaputt, "Anhebung kaputt", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.Equal(-1, neu);
            Assert.Contains("127", fehler, StringComparison.Ordinal);
            Assert.Equal(vorher, Anzahl("Tab_Projekt"));
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        private static int Id(string projektname) => new ProjektDuplizierenCtrl().GetProjektId(projektname);

        private static long Anzahl(string tabelle) =>
            Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM [" + tabelle + "]"), CultureInfo.InvariantCulture);

        private static List<string> Preisbasen(int projekt)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Preisbasis FROM energy_project_settings WHERE ID_Projekt = ? ORDER BY Preisbasis",
                new DbParam("@p", projekt));
            return dt.Rows.Cast<DataRow>()
                     .Select(r => r[0] == DBNull.Value ? "" : Convert.ToString(r[0], CultureInfo.InvariantCulture))
                     .ToList();
        }

        private static double Zahl(object o)
        {
            if (o is JsonNode n) return n.GetValue<double>();
            return o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Schreibt das Paket mit anderem Schemastand neu und lässt jede Zeile jedes
        /// Projektbaums (<c>data/</c>, <c>projects/*/data/</c>) umbauen.
        /// </summary>
        private static void Umbauen(string quelle, string ziel, int stand, Action<string, JsonObject> zeile)
        {
            var eintraege = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            using (ZipArchive zip = ZipFile.OpenRead(quelle))
                foreach (ZipArchiveEntry e in zip.Entries)
                {
                    using Stream s = e.Open();
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    eintraege[e.FullName] = ms.ToArray();
                }

            var utf8 = new UTF8Encoding(false);
            using var stream = new FileStream(ziel, FileMode.Create);
            using var aus = new ZipArchive(stream, ZipArchiveMode.Create);
            foreach (KeyValuePair<string, byte[]> kvp in eintraege)
            {
                byte[] roh = kvp.Value;
                if (kvp.Key == "manifest.json")
                {
                    JsonObject m = JsonNode.Parse(utf8.GetString(roh)).AsObject();
                    m["schemaVersion"] = stand;
                    roh = utf8.GetBytes(m.ToJsonString());
                }
                else if (kvp.Key.StartsWith("data/", StringComparison.Ordinal) ||
                         (kvp.Key.StartsWith("projects/", StringComparison.Ordinal) && kvp.Key.Contains("/data/")))
                {
                    string tabelle = Path.GetFileNameWithoutExtension(kvp.Key);
                    JsonArray zeilen = JsonNode.Parse(utf8.GetString(roh)).AsArray();
                    foreach (JsonNode z in zeilen) zeile(tabelle, z.AsObject());
                    roh = utf8.GetBytes(zeilen.ToJsonString());
                }
                using Stream s = aus.CreateEntry(kvp.Key, CompressionLevel.Optimal).Open();
                s.Write(roh, 0, roh.Length);
            }
        }

        /// <summary>Ein Ordner für die Paketdateien eines Falls; er räumt sich selbst auf.</summary>
        private sealed class Arbeitsordner : IDisposable
        {
            private readonly string _pfad = Path.Combine(Path.GetTempPath(),
                "epos-anhebung-" + Guid.NewGuid().ToString("N").Substring(0, 8));

            public Arbeitsordner() => Directory.CreateDirectory(_pfad);

            public string Datei(string name) => Path.Combine(_pfad, name);

            public void Dispose()
            {
                try { Directory.Delete(_pfad, true); } catch { /* Aufräumen darf nicht scheitern */ }
            }
        }
    }
}
