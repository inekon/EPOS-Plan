using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die fuenf Proben des PROJEKTTRANSFERS (iU9-W15a.0j, Befund W15a-B34).
    ///
    /// <para><b>Warum es sie gibt.</b> Der einzige jemals gelaufene Export/Import-Nachweis
    /// (<c>kd1runner transfer</c>, „17/17 PASS", <c>Konzept_Projekttransfer_EPOS-Plan.md:192</c>)
    /// lag in einem Scratchpad und ist verloren. Bis zu dieser Welle rief KEIN Test
    /// <c>Exportieren</c> oder <c>Importieren</c> auch nur auf — und genau dieser
    /// Controller (1 278 Zeilen) zieht mit iU9-W15a in den Kern um. Die Proben entstehen
    /// deshalb VOR dem Umzug und laufen danach unveraendert erneut (Risiko R-W15a-2).</para>
    ///
    /// <para><b>Warum nicht „bitgleich".</b> Ein Paket ist als GANZES nicht reproduzierbar:
    /// <c>exportedUtc</c> im Manifest, die Eintragszeitstempel des ZIP und — der
    /// eigentliche Grund — der Import vergibt bewusst NEUE Ids (Befund W15a-B33). Was
    /// bitgleich sein KANN, sind die JSON-Eintraege selbst; alles Uebrige wird ueber die
    /// Kriterien des Konzept-Pruefstands geprueft: Zeilenzahlen, FK-Integritaet,
    /// Variantenverknuepfung, Versionsabweisung.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Probe.</b> Alle fuenf schreiben (schon der Export
    /// oeffnet einen Vorgang); geteilt wuerde ein Fall den naechsten sehen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjekttransferTests
    {
        /// <summary>Das Regressionsprojekt der Referenzlaeufe (Id 1030).</summary>
        private const string PROJEKT = "Referenz BHKW-Kaskade (Regressionstest)";

        /// <summary>Ein Stammprojekt mit ZWEI Varianten im Testbestand (Id 1019).</summary>
        private const string STAMM = "Wöhler";
        private const string VARIANTE_1 = "Wöhler - Test1";
        private const string VARIANTE_2 = "Wöhler - Test2";

        // =============================================================================
        //  P1 — Determinismus des Pakets
        // =============================================================================
        [Fact]
        public void P1_Zwei_Exporte_desselben_Projekts_liefern_dasselbe_Paket()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            string a = ordner.Datei("a.wpx");
            string b = ordner.Datei("b.wpx");

            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, a));
            Assert.True(io.Exportieren(PROJEKT, b));

            Dictionary<string, byte[]> ea = Eintraege(a);
            Dictionary<string, byte[]> eb = Eintraege(b);

            // Gleiche Eintragsnamen, und jeder ausser dem Manifest byteweise gleich.
            Assert.Equal(ea.Keys.OrderBy(k => k, StringComparer.Ordinal),
                         eb.Keys.OrderBy(k => k, StringComparer.Ordinal));
            Assert.Contains("manifest.json", ea.Keys);
            Assert.True(ea.Count > 1, "Das Paket traegt ausser dem Manifest keine Daten.");

            foreach (string name in ea.Keys.Where(k => k != "manifest.json"))
                Assert.True(ea[name].SequenceEqual(eb[name]),
                            "Eintrag " + name + " unterscheidet sich zwischen zwei Exporten.");

            // Das Manifest unterscheidet sich NUR im Ausgabezeitpunkt.
            Assert.Equal(OhneZeitstempel(ea["manifest.json"]), OhneZeitstempel(eb["manifest.json"]));
        }

        // =============================================================================
        //  P2 — Rundreise-Zaehlung (Pruefstand-Punkt 2)
        // =============================================================================
        [Fact]
        public void P2_Nach_der_Rundreise_stimmen_die_Zeilenzahlen_je_Pakettabelle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            string paket = ordner.Datei("rund.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Assert.True(quelle > 0);

            int neu = io.Importieren(paket, "Rundreise P2", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            Assert.NotEqual(quelle, neu);

            Dictionary<string, int> imPaket = PaketZeilen(paket);
            Assert.NotEmpty(imPaket);

            var plan = Plan();
            foreach (var kvp in imPaket)
            {
                Assert.True(plan.ContainsKey(kvp.Key), "Pakettabelle " + kvp.Key + " steht nicht im Plan.");
                int imZiel = Zaehle(plan[kvp.Key], neu);
                Assert.True(kvp.Value == imZiel,
                            kvp.Key + ": " + kvp.Value + " Zeilen im Paket, " + imZiel + " im Ziel.");

                // Quelle == Ziel. Einzige Ausnahme ist Tab_ProjektWerte: der Export laesst
                // Kostenpositionen ohne gueltige Anlagenzuordnung bewusst zurueck (T6).
                int inQuelle = Zaehle(plan[kvp.Key], quelle);
                if (kvp.Key.Equals("Tab_ProjektWerte", StringComparison.OrdinalIgnoreCase))
                    Assert.True(inQuelle >= imZiel, "Tab_ProjektWerte: Ziel hat mehr Zeilen als die Quelle.");
                else
                    Assert.True(inQuelle == imZiel,
                                kvp.Key + ": Quelle " + inQuelle + ", Ziel " + imZiel + ".");
            }
        }

        // =============================================================================
        //  P3 — Rundreise-Integritaet (Pruefstand-Punkt 2, FK-Seite)
        // =============================================================================
        [Fact]
        public void P3_Nach_der_Rundreise_gibt_es_keine_verwaisten_Verweise()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            string paket = ordner.Datei("integritaet.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            int neu = io.Importieren(paket, "Rundreise P3", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            var plan = Plan();
            var waisen = new List<string>();
            int geprueft = 0;

            foreach (string tabelle in PaketZeilen(paket).Keys)
            {
                string filter = string.Format(plan[tabelle].Filter, neu);
                foreach ((string spalte, string zielTab, string zielSpalte) in Fremdschluessel(tabelle))
                {
                    geprueft++;
                    object o = DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM [" + tabelle + "] AS k WHERE (" + filter + ") " +
                        "AND k.[" + spalte + "] IS NOT NULL AND k.[" + spalte + "] <> 0 " +
                        "AND NOT EXISTS (SELECT 1 FROM [" + zielTab + "] AS e " +
                        "WHERE e.[" + zielSpalte + "] = k.[" + spalte + "])");
                    int zahl = o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
                    if (zahl > 0) waisen.Add(tabelle + "." + spalte + " -> " + zielTab + ": " + zahl);
                }
            }

            Assert.True(geprueft > 0, "Es wurde kein einziger Fremdschluessel geprueft.");
            Assert.True(waisen.Count == 0, "Verwaiste Verweise: " + string.Join(", ", waisen));

            // Ae24: keine Kostenposition, die auf eine Anlage eines FREMDEN Projekts zeigt.
            object lose = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte AS p WHERE p.ProjektID = " + neu +
                " AND p.ID_Anlage IS NOT NULL AND p.ID_Anlage <> 0 " +
                "AND NOT EXISTS (SELECT 1 FROM Tab_Energieanlagen AS a " +
                "WHERE a.ID = p.ID_Anlage AND a.ID_Projekt = " + neu + ")");
            Assert.Equal(0, lose == null || lose == DBNull.Value ? 0 : Convert.ToInt32(lose));
        }

        // =============================================================================
        //  P4 — Variantenpaket (T3, Pruefstand-Punkt 4)
        // =============================================================================
        [Fact]
        public void P4_Ein_Variantenpaket_verknuepft_die_importierten_Projekte_neu()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            string paket = ordner.Datei("varianten.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(STAMM, new List<string> { VARIANTE_1, VARIANTE_2 }, paket));

            // Das Manifest fuehrt zwei Varianten-Baeume und zwei Verknuepfungen.
            JsonElement man = Manifest(paket);
            Assert.Equal(2, man.GetProperty("formatVersion").GetInt32());
            Assert.Equal(2, man.GetProperty("variants").GetArrayLength());
            Assert.Equal(2, man.GetProperty("variantLinks").GetArrayLength());

            int neu = io.Importieren(paket, "Wöhler P4", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            // Tab_Variante zeigt auf die IMPORTIERTEN Projekte, nicht auf die Quelle.
            DataTable dt = DataRepository.GetDataTable(
                "SELECT v.ID_Projekt, v.Variantenname, p.Projektname " +
                "FROM Tab_Variante AS v INNER JOIN Tab_Projekt AS p ON v.ID_Projekt = p.ID " +
                "WHERE v.ID_ProjektRef = " + neu + " ORDER BY v.Variantenname");
            Assert.NotNull(dt);
            Assert.Equal(2, dt.Rows.Count);

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(STAMM);
            foreach (DataRow r in dt.Rows)
            {
                int idVariante = Convert.ToInt32(r["ID_Projekt"]);
                Assert.NotEqual(quelle, idVariante);
                Assert.True(idVariante > 0);
                // Die importierte Variante ist ein EIGENES Projekt und traegt Zeilen.
                Assert.True(Convert.ToInt32(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Projekt WHERE ID = " + idVariante)) == 1);
            }
            Assert.Equal(new[] { "Test1", "Test2" },
                         dt.Rows.Cast<DataRow>().Select(r => Convert.ToString(r["Variantenname"])).ToArray());
        }

        // =============================================================================
        //  P5 — Versions-Ablehnung (B2/TF4)
        // =============================================================================
        [Fact]
        public void P5_Ein_Paket_mit_fremdem_Schemastand_wird_abgelehnt_ein_Altpaket_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            string paket = ordner.Datei("version.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            // Das Paket traegt den echten Migrationsstand.
            Assert.Equal(SchemaStand.Zielversion, Manifest(paket).GetProperty("schemaVersion").GetInt32());

            string fremd = ordner.Datei("fremd.wpx");
            SchreibeMitSchemastand(paket, fremd, SchemaStand.Zielversion - 1);
            int abgelehnt = io.Importieren(fremd, "Version P5a",
                                           ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
            Assert.Equal(-1, abgelehnt);
            Assert.Contains("Schemastand " + (SchemaStand.Zielversion - 1), fehler, StringComparison.Ordinal);
            Assert.Contains("Stand " + SchemaStand.Zielversion, fehler, StringComparison.Ordinal);

            // schemaVersion 0 ist ein V1-Altpaket (vor T2 exportiert) und bleibt zugelassen.
            string alt = ordner.Datei("alt.wpx");
            SchreibeMitSchemastand(paket, alt, 0);
            int angenommen = io.Importieren(alt, "Version P5b",
                                            ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler2);
            Assert.True(angenommen > 0, "Altpaket abgelehnt: " + fehler2);
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        /// <summary>Ein Ordner fuer die Paketdateien einer Probe; er raeumt sich selbst auf.</summary>
        private sealed class Arbeitsordner : IDisposable
        {
            private readonly string _pfad;

            public Arbeitsordner()
            {
                _pfad = Path.Combine(Path.GetTempPath(),
                                     "epos-transfer-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(_pfad);
            }

            public string Datei(string name) => Path.Combine(_pfad, name);

            public void Dispose()
            {
                try { Directory.Delete(_pfad, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        private static Dictionary<string, byte[]> Eintraege(string paket)
        {
            var map = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            using var zip = ZipFile.OpenRead(paket);
            foreach (ZipArchiveEntry e in zip.Entries)
            {
                using var s = e.Open();
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                map[e.FullName] = ms.ToArray();
            }
            return map;
        }

        private static string Text(byte[] roh) => new UTF8Encoding(false).GetString(roh);

        /// <summary>Das Manifest ohne die Zeile <c>exportedUtc</c> — alles Uebrige ist bestimmt.</summary>
        private static string OhneZeitstempel(byte[] manifest) =>
            string.Join("\n", Text(manifest).Split('\n')
                       .Where(z => !z.Contains("\"exportedUtc\"")));

        private static JsonElement Manifest(string paket)
        {
            using var doc = JsonDocument.Parse(Text(Eintraege(paket)["manifest.json"]));
            return doc.RootElement.Clone();
        }

        /// <summary>Tabellenname -> Zeilenzahl im Stammbaum <c>data/</c> des Pakets.</summary>
        private static Dictionary<string, int> PaketZeilen(string paket)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, byte[]> eintraege = Eintraege(paket);
            foreach (JsonElement t in Manifest(paket).GetProperty("tables").EnumerateArray())
            {
                string name = t.GetProperty("name").GetString();
                using var doc = JsonDocument.Parse(Text(eintraege["data/" + name + ".json"]));
                map[name] = doc.RootElement.GetArrayLength();
            }
            return map;
        }

        /// <summary>Schreibt das Paket neu und setzt dabei den Schemastand im Manifest.</summary>
        private static void SchreibeMitSchemastand(string quelle, string ziel, int stand)
        {
            Dictionary<string, byte[]> eintraege = Eintraege(quelle);
            string manifest = Text(eintraege["manifest.json"]);
            using (var doc = JsonDocument.Parse(manifest))
            {
                int alt = doc.RootElement.GetProperty("schemaVersion").GetInt32();
                manifest = manifest.Replace("\"schemaVersion\": " + alt, "\"schemaVersion\": " + stand);
            }

            using var stream = new FileStream(ziel, FileMode.Create);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
            foreach (var kvp in eintraege)
            {
                using var s = zip.CreateEntry(kvp.Key, CompressionLevel.Optimal).Open();
                byte[] roh = kvp.Key == "manifest.json" ? new UTF8Encoding(false).GetBytes(manifest) : kvp.Value;
                s.Write(roh, 0, roh.Length);
            }
        }

        /// <summary>Der Kopierplan, nach Tabellenname greifbar.</summary>
        private static Dictionary<string, ProjektDuplizierenCtrl.Spec> Plan()
        {
            var map = new Dictionary<string, ProjektDuplizierenCtrl.Spec>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in new ProjektDuplizierenCtrl().ErmittlePlan()) map[s.Tabelle] = s;
            return map;
        }

        private static int Zaehle(ProjektDuplizierenCtrl.Spec spec, int projektId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + spec.Tabelle + "] WHERE " + string.Format(spec.Filter, projektId));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        /// <summary>Die erzwungenen Fremdschluessel einer Tabelle (Spalte, Zieltabelle, Zielspalte).</summary>
        private static List<(string, string, string)> Fremdschluessel(string tabelle)
        {
            var liste = new List<(string, string, string)>();
            DataTable dt = DataRepository.GetDataTable("PRAGMA foreign_key_list('" + tabelle + "')");
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
            {
                string spalte = Convert.ToString(r["from"]);
                string zielTab = Convert.ToString(r["table"]);
                string zielSpalte = r["to"] == DBNull.Value ? null : Convert.ToString(r["to"]);
                if (string.IsNullOrEmpty(zielSpalte)) zielSpalte = "ID";
                if (string.IsNullOrEmpty(spalte) || string.IsNullOrEmpty(zielTab)) continue;
                liste.Add((spalte, zielTab, zielSpalte));
            }
            return liste;
        }
    }
}
