using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wiederimport des Bauteilkatalogs konvergiert</b> (BK-3d): Nach EINER Übernahme legt ein erneuter Lauf derselben
    /// Quelle nichts mehr an — auch wenn ein Satz der Quelle per Dublettenregel an einen Importsatz bindet, den ein anderer
    /// Satz derselben Quelle per Quellkennung im selben Lauf aktualisiert. Dazu die Dublettenzählung der Vorschau.
    /// Ausschließlich synthetische Dateien im Format des Exports.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BauteilkatalogWiederimportTests : IDisposable
    {
        private readonly ITestOutputHelper _aus;
        private readonly List<string> _ordner = new List<string>();

        public BauteilkatalogWiederimportTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose()
        {
            foreach (string o in _ordner)
                try { Directory.Delete(o, true); } catch (IOException) { }
        }

        private const string KOPF_B = "UId;ShortDesc;LongDesc;InfoDesc;HeatConductivity;Density;HeatCapacity;MaterialGroupType;MaterialType;IsCapillary;MoistureContent;StampEdit";
        private const string KOPF_A = "UId;BasisUId;ShortDesc;LongDesc;InfoDesc;Thickness;UValue;RValue;InternalCoefficientOfHeatTransfer;ExternalCoefficientOfHeatTransfer;FractionOfFrame;GValue;StampEdit";
        private const string KOPF_S = "UId;DimensionUId;LayerSectionUId;SeqNum;ShortDesc;LongDesc;Thickness;ThermalConductivity;Density;HeatCapacity;MaterialGroupType;RValue";
        private const string KOPF_X = "UId;DimensionUId;SectionNum;SectionShareValve;LongDesc";
        private const string KOPF_F = "UId;DimensionUId;GlazingUValue;GlazingGValue";
        private const string KOPF_Z = "UId;GroupType;SubType;MaterialUId";
        private const string ST = "2025-03-14T10:20:30";

        private static string G(int n) => "30000000-0000-0000-0000-" + n.ToString("D12", CultureInfo.InvariantCulture);
        private static string Z(double d) => d.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>Eine Baustoffzeile (Code 4 = Mauerwerk).</summary>
        private static string[] Stoff(int n, string name, double lambda, double rho, double cp) =>
            new[] { G(n), name, "", "", Z(lambda), Z(rho), Z(cp), "4", "", "", "", ST };

        /// <summary>Schreibt einen Ordner im Format des Exports; nur Baustoffe, die übrigen Dateien mit Kopf.</summary>
        private string Ordner(IEnumerable<string[]> baustoffe)
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-bk3d-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(o);
            _ordner.Add(o);
            var utf8 = new UTF8Encoding(false);
            var sb = new StringBuilder(KOPF_B).Append("\r\n");
            foreach (string[] z in baustoffe) sb.Append(string.Join(";", z)).Append("\r\n");
            File.WriteAllText(Path.Combine(o, "Baustoffe.csv"), sb.ToString(), utf8);
            File.WriteAllText(Path.Combine(o, "Aufbauten.csv"), KOPF_A + "\r\n", utf8);
            File.WriteAllText(Path.Combine(o, "Schichten.csv"), KOPF_S + "\r\n", utf8);
            File.WriteAllText(Path.Combine(o, "Schichten_Abschnitte.csv"), KOPF_X + "\r\n", utf8);
            File.WriteAllText(Path.Combine(o, "Fenster.csv"), KOPF_F + "\r\n", utf8);
            File.WriteAllText(Path.Combine(o, "Basis_Auszug.csv"), KOPF_Z + "\r\n", utf8);
            return o;
        }

        private static long Zahl(string sql, params DbParam[] p) =>
            Convert.ToInt64(DataRepository.ExecuteScalar(sql, p) ?? 0L, CultureInfo.InvariantCulture);

        private static long Baustoffe() => Zahl("SELECT COUNT(*) FROM \"Tab_Baustoff_STAMM\"");

        /// <summary>Ein Auslieferungssatz (VORGABE) der Testdatenbank nach Namen: (ID, λ, ρ, c).</summary>
        private static (long Id, double L, double R, double C) Vorgabe(string name)
        {
            var t = DataRepository.GetDataTable("SELECT \"ID\", \"Lambda\", \"Rho\", \"cp\" FROM \"Tab_Baustoff_STAMM\" " +
                                                "WHERE \"Herkunft\" = 'VORGABE' AND \"Bezeichner\" = ? AND \"Gruppe_Code\" = 'MAUERWERK'",
                                                new DbParam("@n", name));
            Assert.Equal(1, t.Rows.Count);
            var r = t.Rows[0];
            return (Convert.ToInt64(r["ID"]), Convert.ToDouble(r["Lambda"]), Convert.ToDouble(r["Rho"]), Convert.ToDouble(r["cp"]));
        }

        private void Zeige(string was, BauteilkatalogVorschau v) =>
            _aus.WriteLine(was + ": Neu " + v.Baustoffe.Neu + " / MitKatalog " + v.Baustoffe.MitKatalog + " / InQuelle " +
                           v.Baustoffe.InQuelle + " / Wiedererkannt " + v.Baustoffe.Wiedererkannt + " / Abgelehnt " +
                           v.Baustoffe.Abgelehnt + " / Dubletten " + v.DublettenImKatalog);

        /// <summary>Dubletten über Auslieferung und Import: gleicher Name und gleiche Gruppe, Werte nach der Dublettenregel gleich.</summary>
        private static int DublettenNachRegel()
        {
            var t = DataRepository.GetDataTable("SELECT \"Bezeichner\", \"Gruppe_Code\", \"Lambda\", \"Rho\", \"cp\", \"Herkunft\" FROM \"Tab_Baustoff_STAMM\"");
            var zeilen = t.Rows.Cast<System.Data.DataRow>().Select(r => (N: r["Bezeichner"] as string, G: r["Gruppe_Code"] as string,
                L: r["Lambda"] as double?, R: r["Rho"] as double?, C: r["cp"] as double?, H: r["Herkunft"] as string)).ToList();
            int n = 0;
            foreach (var i in zeilen.Where(x => x.H == "IMPORT"))
                if (zeilen.Any(x => x.H == "VORGABE" && x.N == i.N && x.G == i.G &&
                                    BauteilkatalogImport.WerteGleich(x.L, x.R, x.C, i.L, i.R, i.C))) n++;
            return n;
        }

        [Fact]
        public void Wiederimport_konvergiert_nach_einer_Uebernahme()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var a = Vorgabe("Vollziegel 1800");
            var b = Vorgabe("Kalksandstein 1400");
            var c = Vorgabe("Hochlochziegel HLzW 700");

            // Importstand eines früheren Laufs: Satz 15 mit seinen damaligen Werten, Satz 17 fehlt in der neuen Quelle.
            string frueher = Ordner(new[] { Stoff(15, "Probestein Z", 0.40, 1500, 1000), Stoff(17, "Probestein W", 0.30, 900, 1000) });
            Assert.True(BauteilkatalogImport.Uebernehmen(frueher).Geschrieben);
            long vor = Baustoffe();

            string o = Ordner(new[]
            {
                Stoff(11, "Vollziegel 1800", a.L, a.R, a.C),                          // (a) trifft die Auslieferung
                Stoff(12, "Kalksandstein 1400", b.L, b.R, b.C),                       // (b) zwei Zeilen, in der Quelle zusammengeführt,
                Stoff(13, "Kalksandstein 1400", b.L * 1.005, b.R, b.C),               //     beide treffen die Auslieferung
                Stoff(14, "Hochlochziegel HLzW 700", c.L * 1000.0, c.R, c.C),         // (c) λ-Skalenfehler, trifft die Auslieferung
                Stoff(15, "Probestein Z", 0.50, 1500, 1000),                          // (d) wiedererkannt, λ geändert
                Stoff(16, "Probestein Z", 0.40, 1500, 1000),                          //     gleichnamig mit den alten Werten von 15
                Stoff(18, "Probestein W", 0.30, 900, 1000),                           // (e) trifft den Importsatz 17 anderer Quellkennung
            });

            BauteilkatalogVorschau v1 = BauteilkatalogImport.Vorschau(o);
            Zeige("Vorschau 1", v1);
            Assert.Equal(1, v1.Baustoffe.InQuelle);
            Assert.Equal(1, v1.LambdaSkaliert);
            Assert.Equal(4, v1.Baustoffe.MitKatalog);           // a, b, c, e — nicht 16: die Werte von 15 ändern sich im selben Lauf
            Assert.Equal(1, v1.Baustoffe.Wiedererkannt);        // 15
            Assert.Equal(1, v1.Baustoffe.Neu);                  // 16
            Assert.Equal((0, 0), (v1.DublettenImKatalog, v1.NamensgleicheImporte));

            BauteilkatalogVorschau u1 = BauteilkatalogImport.Uebernehmen(o);
            Zeige("Übernahme 1", u1);
            Assert.Null(u1.Fehler);
            Assert.Equal(vor + 1, Baustoffe());

            // Die Zusammenführung mit dem Katalog ist persistiert: Quellkennung am Auslieferungssatz.
            Assert.Equal(G(11), DataRepository.ExecuteScalar("SELECT \"Quellkennung\" FROM \"Tab_Baustoff_STAMM\" WHERE \"ID\" = ?", new DbParam("@i", a.Id)));
            Assert.Equal(G(12), DataRepository.ExecuteScalar("SELECT \"Quellkennung\" FROM \"Tab_Baustoff_STAMM\" WHERE \"ID\" = ?", new DbParam("@i", b.Id)));
            Assert.Equal(G(14), DataRepository.ExecuteScalar("SELECT \"Quellkennung\" FROM \"Tab_Baustoff_STAMM\" WHERE \"ID\" = ?", new DbParam("@i", c.Id)));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoff_STAMM\" WHERE \"Herkunft\" = 'VORGABE' AND \"Lambda\" <> ? AND \"ID\" = ?",
                                 new DbParam("@l", c.L), new DbParam("@i", c.Id)));

            BauteilkatalogVorschau v2 = BauteilkatalogImport.Vorschau(o);
            Zeige("Vorschau 2", v2);
            Assert.Equal(0, v2.Baustoffe.Neu);
            Assert.Equal(1, v2.Baustoffe.MitKatalog);           // e bleibt am Importsatz 17 (dessen Quellkennung bleibt)
            Assert.Equal(5, v2.Baustoffe.Wiedererkannt);

            BauteilkatalogVorschau u2 = BauteilkatalogImport.Uebernehmen(o);
            Assert.Null(u2.Fehler);
            Assert.Equal(0, u2.Baustoffe.Neu);
            Assert.Equal(vor + 1, Baustoffe());

            BauteilkatalogVorschau v3 = BauteilkatalogImport.Vorschau(o);
            Zeige("Vorschau 3", v3);
            Assert.Equal((0, 1, 1, 5), (v3.Baustoffe.Neu, v3.Baustoffe.MitKatalog, v3.Baustoffe.InQuelle, v3.Baustoffe.Wiedererkannt));
            Assert.Equal((0, 2), (v3.DublettenImKatalog, v3.NamensgleicheImporte));   // 15 und 16: gleicher Name, andere Werte
            Assert.Equal(0, DublettenNachRegel());
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoff_STAMM\" WHERE \"Quellkennung\" = ?", new DbParam("@k", G(16))));
        }

        [Fact]
        public void Ein_geaenderter_Lambda_in_der_Toleranz_fuehrt_mit_dem_Katalog_zusammen_nicht_neu()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var a = Vorgabe("Vollziegel 1800");
            string o = Ordner(new[] { Stoff(21, "Vollziegel 1800", a.L * 1.015, a.R, a.C) });
            long vor = Baustoffe();

            BauteilkatalogVorschau v1 = BauteilkatalogImport.Vorschau(o);
            Assert.Equal((0, 1), (v1.Baustoffe.Neu, v1.Baustoffe.MitKatalog));
            Assert.True(BauteilkatalogImport.Uebernehmen(o).Geschrieben);
            Assert.Equal(vor, Baustoffe());
            Assert.Equal(Z(a.L), Z(Convert.ToDouble(DataRepository.ExecuteScalar("SELECT \"Lambda\" FROM \"Tab_Baustoff_STAMM\" WHERE \"ID\" = ?",
                                                                                   new DbParam("@i", a.Id)), CultureInfo.InvariantCulture)));

            // Danach weicht die Quelle über die Toleranz hinaus ab: wiedererkannt, der Auslieferungssatz bleibt, nichts neu.
            string o2 = Ordner(new[] { Stoff(21, "Vollziegel 1800", a.L * 1.5, a.R, a.C) });
            BauteilkatalogVorschau v2 = BauteilkatalogImport.Vorschau(o2);
            Assert.Equal((0, 0, 1), (v2.Baustoffe.Neu, v2.Baustoffe.MitKatalog, v2.Baustoffe.Wiedererkannt));
            Assert.True(BauteilkatalogImport.Uebernehmen(o2).Geschrieben);
            Assert.Equal(vor, Baustoffe());
        }

        [Fact]
        public void Die_Vorschau_zaehlt_Importsaetze_mit_Name_und_Gruppe_eines_Auslieferungssatzes()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var a = Vorgabe("Vollziegel 1800");

            // Gegenprobe: sauberer Katalog.
            string leer = Ordner(new[] { Stoff(31, "Probestein Y", 0.3, 900, 1000) });
            BauteilkatalogVorschau v0 = BauteilkatalogImport.Vorschau(leer);
            Assert.Equal((0, 0), (v0.DublettenImKatalog, v0.NamensgleicheImporte));
            Assert.DoesNotContain(v0.Protokoll, z => z.StartsWith("Hinweis: Importsätze", StringComparison.Ordinal));

            // Ein Importsatz gleichen Namens und gleicher Gruppe (λ weit weg, darum neu angelegt).
            string o = Ordner(new[] { Stoff(32, "Vollziegel 1800", a.L * 2.0, a.R, a.C) });
            Assert.Equal(1, BauteilkatalogImport.Vorschau(o).Baustoffe.Neu);
            Assert.True(BauteilkatalogImport.Uebernehmen(o).Geschrieben);

            BauteilkatalogVorschau v = BauteilkatalogImport.Vorschau(o);
            Assert.Equal((1, 0), (v.DublettenImKatalog, v.NamensgleicheImporte));
            Assert.Equal(1, v.Baustoffe.Wiedererkannt);
            Assert.Contains(v.Protokoll, z => z.StartsWith("Hinweis: Importsätze mit Name und Gruppe eines Auslieferungssatzes: 1", StringComparison.Ordinal));
        }
    }
}
