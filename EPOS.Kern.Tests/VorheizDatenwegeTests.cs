using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Datenwege der Vorheizvorgabe</b> (AH-V6): Duplizieren, Variante, Projektpaket (Export und Import) und die
    /// Übernahme aus dem Zonenkatalog nehmen die sieben Spalten <c>Tab_Einstellungen.Aufheiz_*</c> des Schritts
    /// <see cref="VorheizenSchema.SCHRITT"/> und <c>Vorheizzeit_H</c> der Zonen mit. Alle vier Wege kopieren schemagetrieben
    /// (Spaltenliste aus der Tabelle bzw. Schnittmenge beider Zonentabellen); diese Probe hält das an den neuen Spalten fest.
    /// Projekt 1052 der Testdatenbank (drei Zonen) trägt die Vorgabe nur in der Arbeitskopie.
    /// </summary>
    [Collection("Testdatenbank")]
    public class VorheizDatenwegeTests : IDisposable
    {
        private const int QUELLE = 1052;
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static readonly string[] EINSTELLUNG =
        {
            VorheizenSchema.SPALTE_VERFAHREN, VorheizenSchema.SPALTE_VORHEIZZEIT, VorheizenSchema.SPALTE_TOLERANZ_ART,
            VorheizenSchema.SPALTE_TOLERANZ, VorheizenSchema.SPALTE_REGELGENAUIGKEIT, VorheizenSchema.SPALTE_GELTUNG,
            VorheizenSchema.SPALTE_HEIZKREIS,
        };

        /// <summary>Setzt in der Arbeitskopie eine vollständige Vorgabe an 1052 und je Zone eine eigene Vorheizzeit.</summary>
        private static void VorgabeSetzen()
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE \"Tab_Einstellungen\" SET \"Aufheiz_Verfahren\" = 'VORGABE', \"Aufheiz_Vorheizzeit_H\" = 5, " +
                "\"Aufheiz_Toleranz_Art\" = 'KW', \"Aufheiz_Toleranz\" = 2.5, \"Aufheiz_Regelgenauigkeit_K\" = 0.5, " +
                "\"Aufheiz_Geltung\" = 'ZONE', \"Aufheiz_Heizkreis_Einbeziehen\" = 0 WHERE \"ID_Projekt\" = ?",
                new DbParam("@p", QUELLE)));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE \"Tab_Zone\" SET \"Vorheizzeit_H\" = 2 + \"Rang\" WHERE \"ID_Gebaeude\" IN " +
                "(SELECT \"ID\" FROM \"Tab_Gebaeude\" WHERE \"ID_Projekt\" = ?)", new DbParam("@p", QUELLE)));
        }

        private static string Einstellung(int projekt)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT " + string.Join(", ", EINSTELLUNG.Select(s => "\"" + s + "\"")) + " FROM \"Tab_Einstellungen\" WHERE \"ID_Projekt\" = ?",
                new DbParam("@p", projekt));
            Assert.Single(t.Rows.Cast<DataRow>());
            return string.Join("|", t.Rows[0].ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture)));
        }

        private static string Zonen(int projekt)
            => string.Join("|", DataRepository.GetDataTable(
                    "SELECT z.\"Bezeichner\", z.\"Vorheizzeit_H\" FROM \"Tab_Zone\" z INNER JOIN \"Tab_Gebaeude\" g ON g.\"ID\" = z.\"ID_Gebaeude\" " +
                    "WHERE g.\"ID_Projekt\" = ? ORDER BY z.\"Rang\", z.\"Bezeichner\"", new DbParam("@p", projekt))
                .Rows.Cast<DataRow>().Select(r => r[0] + "=" + Convert.ToString(r[1], CultureInfo.InvariantCulture)));

        private static string Name(int id)
            => Convert.ToString(DataRepository.ExecuteScalar("SELECT \"Projektname\" FROM \"Tab_Projekt\" WHERE \"ID\" = ?",
                                                             new DbParam("@p", id)), CultureInfo.InvariantCulture);

        private static void PruefeKopie(int kopie)
        {
            Assert.True(kopie > 0);
            Assert.NotEqual(QUELLE, kopie);
            Assert.Equal("VORGABE|5|KW|2.5|0.5|ZONE|0", Einstellung(kopie));
            string zonen = Zonen(kopie);
            Assert.Equal(Zonen(QUELLE), zonen);
            Assert.Contains("=3", zonen);
        }

        [Fact]
        public void Duplizieren_nimmt_Vorgabe_und_Zonen_Vorheizzeit_mit()
        {
            if (!_db.Vorhanden) return;
            VorgabeSetzen();
            PruefeKopie(new ProjektDuplizierenCtrl().Duplizieren(Name(QUELLE), "Vorheiz Duplikat"));
        }

        [Fact]
        public void Die_Variante_nimmt_Vorgabe_und_Zonen_Vorheizzeit_mit()
        {
            if (!_db.Vorhanden) return;
            VorgabeSetzen();
            int v = new VariantenCtrl().AnlegenAusStamm(QUELLE, Name(QUELLE), "Vorheizprobe", out string fehler);
            Assert.True(v > 0, fehler);
            PruefeKopie(v);
        }

        [Fact]
        public void Das_Projektpaket_nimmt_Vorgabe_und_Zonen_Vorheizzeit_mit()
        {
            if (!_db.Vorhanden) return;
            VorgabeSetzen();
            string ordner = Path.Combine(Path.GetTempPath(), "epos-vorheizpaket-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "vorheiz.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(Name(QUELLE), paket));
                int neu = io.Importieren(paket, "Vorheiz Paket", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeKopie(neu);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch (IOException) { }
            }
        }

        [Fact]
        public void Der_Zonenkatalog_reicht_die_Vorheizzeit_hin_und_zurueck()
        {
            if (!_db.Vorhanden || !ZonenKatalogSchema.Lesbar()) return;
            long quelle = Zahl("SELECT MIN(\"ID\") FROM \"Tab_Gebaeude_STAMM\"");
            Katalogkopie.Ergebnis k = GebaeudeStammCtrl.Duplizieren((int)quelle, "Vorheiz Katalogprobe");
            Assert.True(k.Ok, k.Meldung);
            long stamm = k.Id;
            DataRepository.ExecuteSQL("DELETE FROM \"Tab_Zone_STAMM\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@s", stamm));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Zone_STAMM\" (\"ID_Gebaeude\", \"Rang\", \"Bezeichner\", \"Nutzflaeche\", \"Raumhoehe\", \"IstBeheizt\", " +
                "\"Raumsolltemperatur_Tag\", \"Vorheizzeit_H\") VALUES (?, 1, 'Ost', 100.0, 3.0, 1, 21.0, 7), " +
                "(?, 2, 'West', 80.0, 3.0, 1, 21.0, NULL)", new DbParam("@a", stamm), new DbParam("@b", stamm)));

            // Katalog → Projekt (Übernahme über eine neue Zuordnungszeile, wie der Assistent).
            long projekt = Zahl("SELECT MIN(\"ID\") FROM \"Tab_Projekt\"");
            DataRepository.ExecuteSQL(
                "INSERT INTO \"Z_ProjektGebaeude\" (\"ID_Projekt\", \"Wohnflaeche_Waermebedarf\", " +
                "\"Einheit_Waermebedarf_Wohnflaeche\", \"Jahresnutzungsgrad\", \"dezWarmwasserbereitung\") VALUES (?, 100.0, ?, 0.9, 0)",
                new DbParam("@p", projekt), new DbParam("@e", "m2"));
            long z = Zahl("SELECT MAX(\"ID\") FROM \"Z_ProjektGebaeude\"");
            int idGebaeude = new GebaeudeStammCtrl().CopyFromStamm((int)stamm, "Vorheiz Katalogprobe", (int)projekt, (int)z);
            Assert.True(idGebaeude > 0);
            Assert.Equal("Ost=7|West=", Vorheizzeiten("Tab_Zone", idGebaeude));
            Assert.Equal(7, new GebaeudeZonenCtrl().LesenJeGebaeude(idGebaeude)[0].Vorheizzeit_H);

            // Projekt → Katalog („In DB übernehmen“).
            GebaeudeStammCtrl.ProjektuebernahmeErgebnis e = GebaeudeStammCtrl.AusProjektUebernehmen((int)z, "Vorheiz Katalogprobe (2)");
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal("Ost=7|West=", Vorheizzeiten("Tab_Zone_STAMM", e.Id));
        }

        private static string Vorheizzeiten(string tabelle, long idGebaeude)
            => string.Join("|", DataRepository.GetDataTable(
                    "SELECT \"Bezeichner\", \"Vorheizzeit_H\" FROM \"" + tabelle + "\" WHERE \"ID_Gebaeude\" = ? ORDER BY \"Rang\"",
                    new DbParam("@g", idGebaeude))
                .Rows.Cast<DataRow>().Select(r => r[0] + "=" + Convert.ToString(r[1], CultureInfo.InvariantCulture)));

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);
    }
}
