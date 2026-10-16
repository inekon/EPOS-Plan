using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
    /// <b>Bauteilkatalog einlesen</b> (BK-3): Leser des CSV-Exports, Regeln 1–12, Zusammenführung, Wiederimport,
    /// Transaktion mit Rückweg und Leistung — ausschließlich an SYNTHETISCHEN Dateien im exakten Format des
    /// Exports (eingebettete LF, Quoting mit "", Platzhalter, Exponenten, Leer-GUID, c in kJ und J).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BauteilkatalogImportTests : IDisposable
    {
        private readonly ITestOutputHelper _aus;
        private readonly List<string> _ordner = new List<string>();

        public BauteilkatalogImportTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose()
        {
            BauteilkatalogImport.Schreibhaken = _ => { };
            foreach (string o in _ordner)
                try { Directory.Delete(o, true); } catch (IOException) { }
        }

        // ---- Probe im Format des Exports ---------------------------------------------------

        private static string G(int n) => "10000000-0000-0000-0000-" + n.ToString("D12", CultureInfo.InvariantCulture);

        private static string Feld(string s)
        {
            if (s == null) return "";
            return s.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }

        private static string Csv(string kopf, IEnumerable<string[]> zeilen)
        {
            var sb = new StringBuilder(kopf).Append("\r\n");
            foreach (string[] z in zeilen) sb.Append(string.Join(";", z.Select(Feld))).Append("\r\n");
            return sb.ToString();
        }

        private const string KOPF_B = "UId;ShortDesc;LongDesc;InfoDesc;HeatConductivity;Density;HeatCapacity;MaterialGroupType;MaterialType;IsCapillary;MoistureContent;StampEdit";
        private const string KOPF_A = "UId;BasisUId;ShortDesc;LongDesc;InfoDesc;Thickness;UValue;RValue;InternalCoefficientOfHeatTransfer;ExternalCoefficientOfHeatTransfer;FractionOfFrame;GValue;StampEdit";
        private const string KOPF_S = "UId;DimensionUId;LayerSectionUId;SeqNum;ShortDesc;LongDesc;Thickness;ThermalConductivity;Density;HeatCapacity;MaterialGroupType;RValue";
        private const string KOPF_X = "UId;DimensionUId;SectionNum;SectionShareValve;LongDesc";
        private const string KOPF_F = "UId;DimensionUId;GlazingUValue;GlazingGValue";
        private const string KOPF_Z = "UId;GroupType;SubType;MaterialUId";

        private sealed class Probe
        {
            public List<string[]> B = new List<string[]>(), A = new List<string[]>(), S = new List<string[]>(),
                                  X = new List<string[]>(), F = new List<string[]>(), Z = new List<string[]>();
            public string KopfB = KOPF_B;
            // BK-7: Teilkataloge; null = Datei fehlt.
            public string KopfK = KOPF_K, KopfI = KOPF_I;
            public List<string[]> K, I;
            public string Unterordner;
            // BK-7b: Katalog_Zuordnung.csv; null = Datei fehlt. ZuordnungUnterordner: Unterordner des gelesenen Ordners.
            public string KopfKZ = KOPF_KZ;
            public List<string[]> KZ;
            public string ZuordnungUnterordner;
        }

        // BK-7: erfundene Spaltennamen der Zuordnung - der Leser haengt an keiner Verweisspalte.
        private const string KOPF_K = "UId;ShortDesc;Bemerkung";
        private const string KOPF_I = "UId;KnotenUId;ZielUId;Sortierung";
        // BK-7b: der Kopf von Katalog_Zuordnung.csv laut Vertrag des Exports.
        private const string KOPF_KZ = "UId;CatalogContentUId;CatalogUId;BasisUId;DimensionUId;StateType;SortNum";

        private string Schreiben(Probe p)
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-bk3-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(o);
            _ordner.Add(o);
            if (p.Unterordner != null) { o = Path.Combine(o, p.Unterordner); Directory.CreateDirectory(o); }
            var utf8 = new UTF8Encoding(false);
            if (p.K != null) File.WriteAllText(Path.Combine(o, "Katalog.csv"), Csv(p.KopfK, p.K), utf8);
            if (p.I != null) File.WriteAllText(Path.Combine(o, "Katalog_Inhalt.csv"), Csv(p.KopfI, p.I), utf8);
            if (p.KZ != null)
            {
                string oz = p.ZuordnungUnterordner == null ? o : Path.Combine(o, p.ZuordnungUnterordner);
                Directory.CreateDirectory(oz);
                File.WriteAllText(Path.Combine(oz, "Katalog_Zuordnung.csv"), Csv(p.KopfKZ, p.KZ), utf8);
            }
            File.WriteAllText(Path.Combine(o, "Baustoffe.csv"), Csv(p.KopfB, p.B), utf8);
            File.WriteAllText(Path.Combine(o, "Aufbauten.csv"), Csv(KOPF_A, p.A), utf8);
            File.WriteAllText(Path.Combine(o, "Schichten.csv"), Csv(KOPF_S, p.S), utf8);
            File.WriteAllText(Path.Combine(o, "Schichten_Abschnitte.csv"), Csv(KOPF_X, p.X), utf8);
            File.WriteAllText(Path.Combine(o, "Fenster.csv"), Csv(KOPF_F, p.F), utf8);
            File.WriteAllText(Path.Combine(o, "Basis_Auszug.csv"), Csv(KOPF_Z, p.Z), utf8);
            return o;
        }

        private const string ST = "2025-03-14T10:20:30";

        /// <summary>Die Hauptprobe; <paramref name="katalog"/> = (Name, Code, λ, ρ, c) eines vorhandenen Katalogsatzes.</summary>
        private static Probe Hauptprobe((string Name, int Code, double L, double R, double C)? katalog = null)
        {
            var p = new Probe();
            string Z(double d) => d.ToString("R", CultureInfo.InvariantCulture);
            // B1: eingebettetes LF, verdoppeltes Anführungszeichen und ";" im Langtext; c in kJ.
            p.B.Add(new[] { G(1), "Probemauerstein A", "Langtext \"mit\" Zitat;\nzweite Zeile", "", "0.5", "1400", "1", "4", "3", "1", "2.5", "2024-01-02T03:04:05" });
            // B2: λ in Exponentenschreibweise, c in J.
            p.B.Add(new[] { G(2), "Probedämmung B", "Dämmprobe", "", "3.5E-2", "30", "1450", "5", "", "0", "", ST });
            // B3: Dublette zu B2 aus einem anderen Teilkatalog (λ +0,3 %, ρ +3 %, c −3 %).
            p.B.Add(new[] { G(3), "Probedämmung B", "", "Info nur hier", "0.0351", "31", "1400", "5", "", "", "", ST });
            // B4: Platzhalter, Stoffgruppe nur über Basis_Auszug (3 = Platten), c leer.
            p.B.Add(new[] { G(4), "Probeplatte C", "", "", "-987654321.99", "-987654.32", "", "", "", "", "", ST });
            // B5: Leer-GUID → abgelehnt.
            p.B.Add(new[] { BauteilkatalogQuelle.LEER_GUID, "Ohne Kennung", "", "", "1", "1000", "1", "4", "", "", "", ST });
            // B6: λ > 10 außerhalb der Metalle → abgelehnt.
            p.B.Add(new[] { G(6), "Probestoff D", "", "", "15", "2000", "1", "2", "", "", "", ST });
            // B8: ungedeuteter Code → Sonstige.
            p.B.Add(new[] { G(8), "Probestoff E", "", "", "0.2", "500", "0", "12", "", "", "", ST });
            if (katalog.HasValue)
            {
                var k = katalog.Value;
                p.B.Add(new[] { G(7), k.Name, "", "", Z(k.L * 1.01), Z(k.R), Z(k.C), k.Code.ToString(CultureInfo.InvariantCulture), "", "", "", ST });
            }
            p.Z.Add(new[] { G(904), "3", "0", G(4) });
            p.Z.Add(new[] { G(905), "7", "0", G(4) });
            p.Z.Add(new[] { G(906), "3", "0", G(4) });

            // A1 Außenwand mit Lücke in SeqNum; Schicht 2 ohne λ, gefüllt über den Namen.
            p.Z.Add(new[] { G(901), "1", "0", "" });
            p.A.Add(new[] { G(101), G(901), "Probewand 1", "Wand\nzweizeilig", "", "0.34", "0.3", "", "0.13", "0.04", "", "", ST });
            p.S.Add(new[] { G(201), G(101), "", "1", "Probemauerstein A", "", "0.24", "0.5", "1400", "1", "4", "" });
            p.S.Add(new[] { G(202), G(101), "", "3", "Probedämmung B", "", "0.1", "", "", "", "5", "" });
            // A2 Fenster ohne Schicht, mit U, Rahmenanteil in %, Verglasung aus Fenster.csv.
            p.Z.Add(new[] { G(902), "3", "0", "" });
            p.A.Add(new[] { G(102), G(902), "Probefenster", "", "", "", "1.1", "", "", "", "30", "", ST });
            p.F.Add(new[] { G(301), G(102), "0.7", "0.5" });
            // A3 Dach ohne Schicht → gekennzeichnet.
            p.Z.Add(new[] { G(903), "5", "0", "" });
            p.A.Add(new[] { G(103), G(903), "Probedach", "", "", "", "", "", "", "", "", "", ST });
            // A4 ungedeuteter Typ 14; Schicht ohne λ und ohne Treffer → gekennzeichnet.
            p.Z.Add(new[] { G(907), "14", "0", "" });
            p.A.Add(new[] { G(104), G(907), "Probebauteil", "", "", "", "", "", "", "", "", "", ST });
            p.S.Add(new[] { G(204), G(104), "", "1", "Unbekannte Schicht", "", "0.02", "", "", "", "0", "0.1" });
            // A5 inhomogene Wand mit zwei Abschnitten.
            p.A.Add(new[] { G(105), G(901), "Probewand inhomogen", "", "", "0.2", "0.4", "", "", "", "", "", "2025-06-30T00:00:00" });
            p.X.Add(new[] { G(401), G(105), "1", "80", "Gefach" });
            p.X.Add(new[] { G(402), G(105), "2", "20", "Ständer" });
            p.S.Add(new[] { G(205), G(105), G(401), "1", "Probedämmung B", "", "0.2", "0.035", "30", "1450", "5", "" });
            p.S.Add(new[] { G(206), G(105), G(402), "1", "Probeholz", "", "0.2", "0.13", "500", "1.6", "6", "" });
            // Schicht zu unbekanntem Aufbau → abgelehnt.
            p.S.Add(new[] { G(207), G(199), "", "1", "Waise", "", "0.1", "0.5", "", "", "4", "" });
            return p;
        }

        [Fact]
        public void Ein_Aufbau_mit_falscher_Anteilsumme_wird_benannt_verworfen()
        {
            // BK-5: dieselbe Probe, die Anteile der inhomogenen Wand A5 ergeben 70 % + 20 % = 0,9 statt 1 ± 0,01.
            Probe probe = Hauptprobe();
            probe.X[0] = new[] { G(401), G(105), "1", "70", "Gefach" };
            BauteilkatalogImport.Plan p = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(probe)), new BauteilkatalogStand());
            BauteilkatalogVorschau v = p.Vorschau;
            Assert.Null(v.Abgelehnt);
            Assert.Equal(4, v.Aufbauten.Neu);
            Assert.Equal(1, v.Aufbauten.Gruende["inhomogen: Flächenanteile ergeben nicht 1 ± 0,01"]);
            Assert.DoesNotContain(p.Aufbauten, a => a.Kennung == G(105));
            Assert.Empty(p.Abschnitte);
            Assert.Contains(v.Protokoll, z => z.StartsWith("Aufbau verworfen (inhomogen", StringComparison.Ordinal));
        }

        // ---- Leser -------------------------------------------------------------------------

        [Fact]
        public void Der_Leser_zerlegt_nach_RFC_4180_mit_eingebettetem_LF_und_verdoppeltem_Anfuehrungszeichen()
        {
            List<string[]> z = BauteilkatalogQuelle.Zerlegen("a;b;c\r\n1;\"x;\"\"y\"\"\nz\";\r\n\"\";2;3\r\n", out string fehler);
            Assert.Null(fehler);
            Assert.Equal(3, z.Count);
            Assert.Equal(new[] { "1", "x;\"y\"\nz", "" }, z[1]);
            Assert.Equal(new[] { "", "2", "3" }, z[2]);
            Assert.Null(BauteilkatalogQuelle.Zerlegen("a\r\n\"offen", out fehler));
            Assert.Contains("nicht geschlossen", fehler);
        }

        [Fact]
        public void Zahlen_und_Kennungen_folgen_Regel_1()
        {
            Assert.Null(BauteilkatalogQuelle.Zahl("-987654321.99"));
            Assert.Null(BauteilkatalogQuelle.Zahl("-987654.32"));
            Assert.Null(BauteilkatalogQuelle.Zahl("2E8"));
            Assert.Null(BauteilkatalogQuelle.Zahl(""));
            Assert.Equal(0.035, BauteilkatalogQuelle.Zahl("3.5E-2"));
            Assert.Equal(1.25, BauteilkatalogQuelle.Zahl("1.25"));
            Assert.Null(BauteilkatalogQuelle.Kennung(BauteilkatalogQuelle.LEER_GUID));
            Assert.Null(BauteilkatalogQuelle.Kennung("00000000-0000-0000-0000-000000000000"));
            Assert.Equal("ABCDEF00-0000-0000-0000-000000000001", BauteilkatalogQuelle.Kennung("abcdef00-0000-0000-0000-000000000001"));
            Assert.Equal(1000.0, BauteilkatalogImport.CpNormieren(1.0));
            Assert.Equal(1450.0, BauteilkatalogImport.CpNormieren(1450));
            Assert.Null(BauteilkatalogImport.CpNormieren(0));
            Assert.Equal("MINERALWOLLE", BauteilkatalogImport.KlasseAusName("mineralwolle 035", "DAEMMSTOFFE"));
            Assert.Null(BauteilkatalogImport.KlasseAusName("mineralwolle 035", "BETON"));
        }

        [Fact]
        public void Eine_fehlende_Pflichtspalte_lehnt_die_Quelle_benannt_ab()
        {
            Probe p = Hauptprobe();
            p.KopfB = KOPF_B.Replace("HeatConductivity;", "Waermeleitung;");
            BauteilkatalogVorschau v = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(p)), null).Vorschau;
            Assert.Contains("Baustoffe.csv: Pflichtspalte fehlt (HeatConductivity)", v.Abgelehnt);

            string o = Schreiben(Hauptprobe());
            File.Delete(Path.Combine(o, "Basis_Auszug.csv"));
            Assert.Equal("Datei fehlt: Basis_Auszug.csv", BauteilkatalogQuelle.Lesen(o).Abgelehnt);

            o = Schreiben(Hauptprobe());
            File.AppendAllText(Path.Combine(o, "Aufbauten.csv"), "nur;drei;Felder\r\n");
            Assert.Contains("statt 13 Felder", BauteilkatalogQuelle.Lesen(Path.Combine(o, "Baustoffe.csv")).Abgelehnt);

            o = Schreiben(Hauptprobe());
            File.Delete(Path.Combine(o, "Fenster.csv"));
            BauteilkatalogQuelle q = BauteilkatalogQuelle.Lesen(o);
            Assert.Null(q.Abgelehnt);
            Assert.Contains(q.Hinweise, h => h.Contains("Fenster.csv"));
        }

        // ---- Regeln ohne Datenbank ---------------------------------------------------------

        [Fact]
        public void Die_Regeln_zaehlen_je_Satzart_kennzeichnen_und_nummerieren_neu()
        {
            var stand = new BauteilkatalogStand();
            stand.Baustoffe.Add(new BauteilkatalogBestand
            { Id = 9001, Bezeichner = "Probestoff E", Gruppe = "SONSTIGE", Lambda = 0.2, Rho = 500, Herkunft = "VORGABE" });
            BauteilkatalogImport.Plan p = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(Hauptprobe())), stand);
            BauteilkatalogVorschau v = p.Vorschau;
            Assert.Null(v.Abgelehnt);
            Assert.Equal("Hottgenroth-Katalog, Datenstand 2025-06", v.Quelle);

            Assert.Equal(3, v.Baustoffe.Neu);            // B1, B2(+B3), B4
            Assert.Equal(1, v.Baustoffe.InQuelle);       // B3
            Assert.Equal(1, v.Baustoffe.MitKatalog);     // B8 trifft den Bestand
            Assert.Equal(2, v.Baustoffe.Abgelehnt);
            Assert.Equal(1, v.Baustoffe.Gruende["ohne Kennung"]);
            Assert.Equal(1, v.Baustoffe.Gruende[BauteilkatalogImport.GRUND_LAMBDA]);

            BauteilkatalogImport.Satz b1 = p.Baustoffe.Single(s => s.Kennung == G(1));
            Assert.Equal(1000.0, b1.Cp);
            Assert.Equal("Langtext \"mit\" Zitat;\nzweite Zeile", b1.Werte["Langtext"]);
            Assert.Equal("MAUERWERK", b1.Gruppe);
            BauteilkatalogImport.Satz b2 = p.Baustoffe.Single(s => s.Kennung == G(2));
            Assert.Equal(0.035, b2.Lambda);
            Assert.Equal("Info nur hier", b2.Werte["Infotext"]);   // leeres Feld aus der Dublette gefüllt
            BauteilkatalogImport.Satz b4 = p.Baustoffe.Single(s => s.Kennung == G(4));
            Assert.Null(b4.Lambda);
            Assert.Null(b4.Rho);
            Assert.Equal("PLATTEN", b4.Gruppe);
            Assert.Contains(v.Protokoll, z => z.StartsWith("Baustoffe mit widersprüchlicher Stoffgruppe", StringComparison.Ordinal));
            Assert.Equal(1, v.CodesOhneDeutung["Baustoffe.csv MaterialGroupType 12"]);
            Assert.False(v.CodesOhneDeutung.ContainsKey("Basis_Auszug.csv GroupType|SubType 14|0"));   // E134: Haustechnik
            Assert.Equal(1, v.HaustechnikAufbauten);

            Assert.Equal(5, v.Aufbauten.Neu);
            Assert.Equal(2, v.Abschnitte.Neu);
            Assert.Equal(5, v.Schichten.Neu);
            Assert.Equal(1, v.Schichten.Gruende["Aufbau fehlt oder abgelehnt"]);
            Assert.Equal(2, v.Gekennzeichnet);           // Dach ohne Schicht, Bauteil mit Schicht ohne λ

            BauteilkatalogImport.Satz a1 = p.Aufbauten.Single(s => s.Kennung == G(101));
            Assert.Equal(new[] { 1, 2 }, a1.Schichten.Select(s => (int)s.Werte["Reihenfolge"]).ToArray());
            Assert.Equal(0.035, a1.Schichten[1].Lambda);     // über den Namensabgleich gefüllt
            Assert.Contains(v.Protokoll, z => z.Contains(G(101)) && z.Contains("neu nummeriert"));
            Assert.Equal(0, a1.Werte["Simulation_Ungeeignet"]);
            Assert.Equal("AUSSENWAND", a1.Werte["Bauteilart"]);

            BauteilkatalogImport.Satz a2 = p.Aufbauten.Single(s => s.Kennung == G(102));
            Assert.Equal(0.3, (double)a2.Werte["Rahmenanteil"], 12);
            Assert.Equal(0.7, a2.Werte["Ug"]);
            Assert.Equal(0.5, a2.Werte["g_Wert"]);
            Assert.Equal(0, a2.Werte["Simulation_Ungeeignet"]);
            Assert.Equal("OEFFNUNGEN", a2.Werte["Gruppe_Code"]);
            Assert.Equal(1, p.Aufbauten.Single(s => s.Kennung == G(103)).Werte["Simulation_Ungeeignet"]);
            BauteilkatalogImport.Satz a4 = p.Aufbauten.Single(s => s.Kennung == G(104));
            Assert.Equal("SONSTIGES", a4.Werte["Bauteilart"]);
            Assert.Equal("SONSTIGE", a4.Werte["Gruppe_Code"]);
            Assert.Equal(BauteilkatalogImport.KlasseHaustechnik(false), a4.Werte["Klasse_Code"]);
            BauteilkatalogImport.Satz x1 = p.Abschnitte.Single(s => s.Kennung == G(401));
            Assert.Equal(0.8, (double)x1.Werte["Flaechenanteil"], 12);
            Assert.Equal(1000.0 * 1.6, p.Schichten.Single(s => s.Kennung == G(206)).Cp.Value, 9);

            Assert.Equal(1, v.Richtung.Paare);
            Assert.Equal(1, v.Richtung.DaemmungHinten);
            Assert.Equal(1, v.Richtung.UGeprueft);
            Assert.InRange(v.Richtung.MedianVerhaeltnis.Value, 0.9, 1.0);
            Assert.Equal(0.0, v.Richtung.AnteilAusserhalb);
            Assert.Equal(3, v.SchichtenMitBaustoff);
        }

        [Fact]
        public void Die_Dublettenregel_haelt_die_Toleranzen()
        {
            Assert.True(BauteilkatalogImport.WerteGleich(0.035, 30, 1450, 0.0357, 31.5, 1590));
            Assert.False(BauteilkatalogImport.WerteGleich(0.035, 30, 1450, 0.0358, 30, 1450));
            Assert.False(BauteilkatalogImport.WerteGleich(0.035, 30, 1450, 0.035, 31.6, 1450));
            Assert.False(BauteilkatalogImport.WerteGleich(0.035, 30, 1450, 0.035, 30, 1300));
            Assert.True(BauteilkatalogImport.WerteGleich(null, null, null, null, null, null));
            Assert.False(BauteilkatalogImport.WerteGleich(0.035, null, null, null, null, null));
            Assert.Equal("a; b", BauteilkatalogImport.QuelleAnhaengen("a", "b"));
            Assert.Equal("a; b", BauteilkatalogImport.QuelleAnhaengen("a; b", "b"));
        }

        // ---- Mit Datenbank -----------------------------------------------------------------

        private static long Zahl(string sql, params DbParam[] p) =>
            Convert.ToInt64(DataRepository.ExecuteScalar(sql, p) ?? 0L, CultureInfo.InvariantCulture);

        private static (long Id, string Name, int Code, double L, double R, double C, string Quelle) Katalogsatz()
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"ID\", \"Bezeichner\", \"Gruppe_Code\", \"Lambda\", \"Rho\", \"cp\", \"Quelle\" FROM \"Tab_Baustoff_STAMM\" " +
                "WHERE \"Herkunft\" = 'VORGABE' AND \"Gruppe_Code\" IN ('MAUERWERK','BETON','DAEMMSTOFFE') AND \"Lambda\" > 0 " +
                "AND \"Rho\" > 0 AND \"cp\" > 0 ORDER BY \"ID\" LIMIT 1");
            DataRow r = t.Rows[0];
            string g = (string)r["Gruppe_Code"];
            int code = g == "MAUERWERK" ? 4 : g == "BETON" ? 2 : 5;
            return (Convert.ToInt64(r["ID"]), (string)r["Bezeichner"], code, Convert.ToDouble(r["Lambda"]), Convert.ToDouble(r["Rho"]),
                    Convert.ToDouble(r["cp"]), r["Quelle"] as string);
        }

        [Fact]
        public void Uebernehmen_schreibt_fuehrt_mit_dem_Katalog_zusammen_und_der_Wiederimport_verdoppelt_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var k = Katalogsatz();
            string o = Schreiben(Hauptprobe((k.Name, k.Code, k.L, k.R, k.C)));
            long baustoffeVor = Zahl("SELECT COUNT(*) FROM \"Tab_Baustoff_STAMM\"");
            string pruefVor = DataRepository.ExecuteScalar("SELECT \"Lambda\" || '|' || \"Rho\" || '|' || \"cp\" FROM \"Tab_Baustoff_STAMM\" WHERE \"ID\" = ?",
                                                           new DbParam("@id", k.Id)) as string;

            BauteilkatalogVorschau vor = BauteilkatalogImport.Vorschau(Path.Combine(o, "Baustoffe.csv"));
            Assert.Equal(1, vor.Baustoffe.MitKatalog);
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoff_STAMM\" WHERE \"Herkunft\" = 'IMPORT'"));

            BauteilkatalogVorschau e = BauteilkatalogImport.Uebernehmen(o);
            Assert.Null(e.Fehler);
            Assert.True(e.Geschrieben);
            Assert.Equal(baustoffeVor + 4, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoff_STAMM\""));   // B1, B2, B4, B8
            Assert.Equal(4, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoff_STAMM\" WHERE \"Herkunft\" = 'IMPORT' AND \"ReadOnly\" = 0 " +
                                 "AND \"Quelle\" = 'Hottgenroth-Katalog, Datenstand 2025-06' AND length(\"Quellkennung\") = 36"));
            Assert.Equal(5, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau_STAMM\" WHERE \"Herkunft\" = 'IMPORT'"));
            Assert.Equal(2, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau_STAMM\" WHERE \"Herkunft\" = 'IMPORT' AND \"Simulation_Ungeeignet\" = 1"));
            Assert.Equal(2, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilabschnitt_STAMM\""));
            Assert.Equal(2, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilschicht_STAMM\" WHERE \"ID_Abschnitt\" IS NOT NULL"));
            Assert.Equal(3, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilschicht_STAMM\" s JOIN \"Tab_Baustoff_STAMM\" b ON b.\"ID\" = s.\"ID_Baustoff\" " +
                                 "WHERE b.\"Herkunft\" = 'IMPORT'"));

            // Der Auslieferungssatz behält seine Werte und nennt die Quelle zusätzlich.
            Assert.Equal(pruefVor, DataRepository.ExecuteScalar("SELECT \"Lambda\" || '|' || \"Rho\" || '|' || \"cp\" FROM \"Tab_Baustoff_STAMM\" WHERE \"ID\" = ?",
                                                                 new DbParam("@id", k.Id)) as string);
            string quelle = DataRepository.ExecuteScalar("SELECT \"Quelle\" FROM \"Tab_Baustoff_STAMM\" WHERE \"ID\" = ?", new DbParam("@id", k.Id)) as string;
            Assert.EndsWith("Hottgenroth-Katalog, Datenstand 2025-06", quelle);
            Assert.Equal("VORGABE", DataRepository.ExecuteScalar("SELECT \"Herkunft\" FROM \"Tab_Baustoff_STAMM\" WHERE \"ID\" = ?", new DbParam("@id", k.Id)));
            Assert.Equal(1L, Zahl("SELECT \"Kapillaraktiv\" FROM \"Tab_Baustoff_STAMM\" WHERE \"Quellkennung\" = ?", new DbParam("@k", G(1))));

            // Wiederimport: erkennt per Quellkennung, verdoppelt nicht.
            long aufbauten = Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau_STAMM\"");
            long schichten = Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilschicht_STAMM\"");
            BauteilkatalogVorschau zwei = BauteilkatalogImport.Uebernehmen(o);
            Assert.True(zwei.Geschrieben);
            Assert.Equal(0, zwei.Baustoffe.Neu);
            Assert.Equal(5, zwei.Baustoffe.Wiedererkannt + zwei.Baustoffe.MitKatalog);
            Assert.Equal(5, zwei.Aufbauten.Wiedererkannt);
            Assert.Equal(baustoffeVor + 4, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoff_STAMM\""));
            Assert.Equal(aufbauten, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau_STAMM\""));
            Assert.Equal(schichten, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilschicht_STAMM\""));
            Assert.Equal(quelle, DataRepository.ExecuteScalar("SELECT \"Quelle\" FROM \"Tab_Baustoff_STAMM\" WHERE \"ID\" = ?", new DbParam("@id", k.Id)));
        }

        // ---- BK-7: Teilkataloge und Unterordner Export ---------------------------------------

        /// <summary>
        /// Die Hauptprobe mit zwei erfundenen Teilkatalogen: B2 direkt in A, seine Dublette B3 in B, B4 über seine
        /// Basiszeile in B, die Aufbauten mit Basis 901 (A1, A5) über einen Elternknoten in A.
        /// </summary>
        private static Probe ProbeTeilkataloge((string Name, int Code, double L, double R, double C)? katalog = null)
        {
            Probe p = Hauptprobe(katalog);
            p.K = new List<string[]>
            {
                new[] { G(801), "Teilkatalog A", "" }, new[] { G(802), "Teilkatalog B", "" },
                new[] { G(803), "", "ohne Namen" }
            };
            p.I = new List<string[]>
            {
                new[] { G(851), G(801), G(2), "1" },
                new[] { G(852), G(802), G(3), "2" },
                new[] { G(853), G(801), "", "3" },                 // Knoten ohne Satz
                new[] { G(854), G(853), G(901), "4" },             // Kind des Knotens, Basiszeile der Aufbauten A1 und A5
                new[] { G(855), G(802), G(904), "5" },             // Basiszeile des Baustoffs B4
                new[] { G(856), G(803), G(102), "6" },             // Teilkatalog ohne Namen
                new[] { G(857), G(801), G(7), "7" }                // Satz, der den Katalog trifft (nur mit katalog)
            };
            return p;
        }

        [Fact]
        public void Die_Teilkataloge_gehen_in_die_Quelle_auch_ueber_Basiszeile_Elternknoten_und_Dublette()
        {
            BauteilkatalogImport.Plan p = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(ProbeTeilkataloge())), new BauteilkatalogStand());
            BauteilkatalogVorschau v = p.Vorschau;
            Assert.Null(v.Abgelehnt);
            string q = "Hottgenroth-Katalog, Datenstand 2025-06";
            Assert.Equal(q, v.Quelle);
            string Quelle(List<BauteilkatalogImport.Satz> l, int n) => (string)l.Single(s => s.Kennung == G(n)).Werte["Quelle"];
            Assert.Equal(q + ", Teilkataloge Teilkatalog A / Teilkatalog B", Quelle(p.Baustoffe, 2));   // B3 zusammengeführt
            Assert.Equal(q + ", Teilkatalog Teilkatalog B", Quelle(p.Baustoffe, 4));
            Assert.Equal(q, Quelle(p.Baustoffe, 1));
            Assert.Equal(q + ", Teilkatalog Teilkatalog A", Quelle(p.Aufbauten, 101));
            Assert.Equal(q + ", Teilkatalog Teilkatalog A", Quelle(p.Aufbauten, 105));
            Assert.Equal(q, Quelle(p.Aufbauten, 102));                                                 // Teilkatalog ohne Namen
            Assert.Equal(2, v.Teilkataloge);
            Assert.Equal(2, v.BaustoffeMitTeilkatalog);
            Assert.Equal(2, v.AufbautenMitTeilkatalog);
            Assert.Contains(v.Protokoll, z => z.StartsWith("Teilkataloge: 2", StringComparison.Ordinal));
            Assert.Contains(v.Protokoll, z => z.StartsWith("Katalog_Inhalt.csv: Zeilen mit Satz, aber ohne Teilkatalog: 1", StringComparison.Ordinal));
        }

        [Fact]
        public void Fehlen_Datei_oder_Spalte_der_Teilkataloge_laeuft_der_Import_benannt_weiter()
        {
            string q = "Hottgenroth-Katalog, Datenstand 2025-06";
            BauteilkatalogVorschau ohne = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(Hauptprobe())), null).Vorschau;
            Assert.Null(ohne.Abgelehnt);
            Assert.Contains(ohne.Protokoll, z => z == "Datei fehlt, ohne Teilkatalognamen weiter: Katalog.csv");
            Assert.Contains(ohne.Protokoll, z => z == "Datei fehlt, ohne Teilkatalognamen weiter: Katalog_Inhalt.csv");
            Assert.Equal(0, ohne.Teilkataloge);

            Probe p = ProbeTeilkataloge();
            p.KopfK = "UId;Kennzeichen;Bemerkung";                                   // keine Namensspalte
            BauteilkatalogImport.Plan plan = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(p)), null);
            Assert.Null(plan.Vorschau.Abgelehnt);
            Assert.Contains(plan.Vorschau.Protokoll, z => z.StartsWith("Katalog.csv: Spalte fehlt (Name:", StringComparison.Ordinal));
            Assert.All(plan.Baustoffe, s => Assert.Equal(q, s.Werte["Quelle"]));

            p = ProbeTeilkataloge();
            p.I.Add(new[] { "nur", "zwei" });                                         // kaputte Zeile
            plan = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(p)), null);
            Assert.Null(plan.Vorschau.Abgelehnt);
            Assert.Contains(plan.Vorschau.Protokoll, z => z.StartsWith("Katalog_Inhalt.csv: Datensatz", StringComparison.Ordinal) &&
                                                          z.EndsWith("ohne Teilkatalognamen weiter", StringComparison.Ordinal));
            Assert.Equal(0, plan.Vorschau.Teilkataloge);

            p = ProbeTeilkataloge();
            p.I = p.I.Select(z => new[] { z[0], z[1], G(999), z[3] }).ToList();      // trifft keinen Satz
            plan = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(p)), null);
            Assert.Contains(plan.Vorschau.Protokoll, z => z.StartsWith("Katalog_Inhalt.csv: keine Zeile trifft", StringComparison.Ordinal));
        }

        [Fact]
        public void Die_Quelle_mit_Teilkatalogen_bleibt_in_der_Hoechstlaenge()
        {
            string q = "Hottgenroth-Katalog, Datenstand 2025-06";
            Assert.Equal(q, BauteilkatalogTeilkataloge.QuelleMit(q, null));
            Assert.Equal(q + ", Teilkatalog A", BauteilkatalogTeilkataloge.QuelleMit(q, new[] { "A", "A", " " }));
            var viele = Enumerable.Range(1, 40).Select(i => "Teilkatalog " + i.ToString("D2", CultureInfo.InvariantCulture) + new string('x', 20)).ToList();
            string lang = BauteilkatalogTeilkataloge.QuelleMit(q, viele);
            Assert.True(lang.Length <= 400, lang.Length.ToString(CultureInfo.InvariantCulture));
            Assert.StartsWith(q + ", Teilkataloge Teilkatalog 01", lang);
            Assert.EndsWith(" u. a.", lang);
        }

        [Fact]
        public void Liegt_Baustoffe_csv_im_Unterordner_Export_wird_dieser_gelesen()
        {
            Probe p = ProbeTeilkataloge();
            p.Unterordner = "Export";
            string export = Schreiben(p);
            string oben = Path.GetDirectoryName(export);
            BauteilkatalogQuelle q = BauteilkatalogQuelle.Lesen(oben);
            Assert.Null(q.Abgelehnt);
            Assert.True(q.AusUnterordnerExport);
            Assert.Equal(export, q.Ordner);
            BauteilkatalogVorschau v = BauteilkatalogImport.Planen(q, null).Vorschau;
            Assert.True(v.UnterordnerExport);
            Assert.Equal(export, v.Ordner);
            Assert.Equal(2, v.Teilkataloge);
            Assert.Contains(v.Protokoll, z => z.StartsWith("Gelesen wird der Unterordner Export", StringComparison.Ordinal));

            // Liegt Baustoffe.csv im gewählten Ordner selbst, bleibt es bei ihm.
            BauteilkatalogQuelle direkt = BauteilkatalogQuelle.Lesen(export);
            Assert.False(direkt.AusUnterordnerExport);
            Assert.Equal(export, direkt.Ordner);
            // Weder hier noch im Unterordner: benannt abgelehnt wie bisher.
            Assert.Equal("Datei fehlt: Baustoffe.csv", BauteilkatalogQuelle.Lesen(LeererOrdnerMitExport()).Abgelehnt);
        }

        private string LeererOrdnerMitExport()
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-bk7-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(o, "Export"));
            _ordner.Add(o);
            return o;
        }

        [Fact]
        public void Uebernehmen_schreibt_die_Teilkataloge_in_die_Quelle_und_haengt_sie_am_Katalogsatz_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var k = Katalogsatz();
            BauteilkatalogVorschau e = BauteilkatalogImport.Uebernehmen(Schreiben(ProbeTeilkataloge((k.Name, k.Code, k.L, k.R, k.C))));
            Assert.Null(e.Fehler);
            Assert.True(e.Geschrieben);
            string q = "Hottgenroth-Katalog, Datenstand 2025-06";
            string Quelle(string tabelle, int n) => DataRepository.ExecuteScalar(
                "SELECT \"Quelle\" FROM \"" + tabelle + "\" WHERE \"Quellkennung\" = ?", new DbParam("@k", G(n))) as string;
            Assert.Equal(q + ", Teilkataloge Teilkatalog A / Teilkatalog B", Quelle("Tab_Baustoff_STAMM", 2));
            Assert.Equal(q + ", Teilkatalog Teilkatalog A", Quelle("Tab_Bauteilaufbau_STAMM", 101));
            Assert.Equal(q, Quelle("Tab_Bauteilaufbau_STAMM", 102));
            string katalog = DataRepository.ExecuteScalar("SELECT \"Quelle\" FROM \"Tab_Baustoff_STAMM\" WHERE \"ID\" = ?", new DbParam("@id", k.Id)) as string;
            Assert.EndsWith(q + ", Teilkatalog Teilkatalog A", katalog);
        }

        // ---- BK-7b: Katalog_Zuordnung.csv ---------------------------------------------------

        /// <summary>
        /// Die Probe der Teilkataloge mit <c>Katalog_Zuordnung.csv</c>: B4 über seine Basiszeile in A (sein
        /// Inhaltsknoten hängt in B — CatalogUId gilt), A1 und A5 über ihre BasisUId in B, A2 über DimensionUId in A,
        /// B8 über eine eigene Basiszeile in A und B, eine Zeile ohne Treffer, zwei Zeilen mit Satz ohne benannten
        /// Teilkatalog (A3, A4). B2 steht nur in Katalog_Inhalt.csv und bleibt deshalb ohne Teilkatalog.
        /// </summary>
        private static Probe ProbeZuordnung()
        {
            Probe p = ProbeTeilkataloge();
            p.Z.Add(new[] { G(908), "", "", G(8) });                          // Basiszeile des Baustoffs B8
            p.KZ = new List<string[]>
            {
                new[] { G(861), G(855), G(801), G(904), "", "0", "1" },       // B4 in A, Knoten 855 in B
                new[] { G(862), "", G(802), G(901), "", "0", "2" },           // A1, A5 über BasisUId in B
                new[] { G(863), "", G(801), "", G(102), "0", "3" },           // A2 über DimensionUId in A
                new[] { G(864), "", G(801), G(908), "", "0", "4" },           // B8 in A ...
                new[] { G(865), "", G(802), G(908), "", "0", "5" },           // ... und in B
                new[] { G(866), "", G(801), G(999), "", "0", "6" },           // kein Treffer
                new[] { G(867), "", G(803), G(903), "", "0", "7" },           // A3, Teilkatalog ohne Namen
                new[] { G(868), "", G(809), "", G(104), "0", "8" }            // A4, Teilkatalog unbekannt
            };
            return p;
        }

        private static string Quelle(List<BauteilkatalogImport.Satz> l, int n) => (string)l.Single(s => s.Kennung == G(n)).Werte["Quelle"];

        [Fact]
        public void Katalog_Zuordnung_gilt_vorrangig_ueber_Basiszeile_BasisUId_und_DimensionUId()
        {
            BauteilkatalogImport.Plan p = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(ProbeZuordnung())), new BauteilkatalogStand());
            BauteilkatalogVorschau v = p.Vorschau;
            Assert.Null(v.Abgelehnt);
            string q = v.Quelle;
            Assert.True(v.TeilkatalogeAusZuordnung);
            Assert.Equal(q + ", Teilkatalog Teilkatalog A", Quelle(p.Baustoffe, 4));                  // Basiszeile, CatalogUId gilt
            Assert.Equal(q + ", Teilkataloge Teilkatalog A / Teilkatalog B", Quelle(p.Baustoffe, 8));  // zwei Teilkataloge
            Assert.Equal(q, Quelle(p.Baustoffe, 2));                                                   // nur in Katalog_Inhalt.csv
            Assert.Equal(q, Quelle(p.Baustoffe, 1));
            Assert.Equal(q + ", Teilkatalog Teilkatalog B", Quelle(p.Aufbauten, 101));                 // BasisUId
            Assert.Equal(q + ", Teilkatalog Teilkatalog B", Quelle(p.Aufbauten, 105));
            Assert.Equal(q + ", Teilkatalog Teilkatalog A", Quelle(p.Aufbauten, 102));                 // DimensionUId
            Assert.Equal(q, Quelle(p.Aufbauten, 103));
            Assert.Equal(q, Quelle(p.Aufbauten, 104));
            Assert.Equal(2, v.Teilkataloge);
            Assert.Equal(2, v.TeilkatalogeInQuelle);
            Assert.Equal(2, v.BaustoffeMitTeilkatalog);
            Assert.Equal(1, v.BaustoffeMitMehrerenTeilkatalogen);
            Assert.Equal(2, v.BaustoffeOhneTeilkatalog);
            Assert.Equal(3, v.AufbautenMitTeilkatalog);
            Assert.Equal(0, v.AufbautenMitMehrerenTeilkatalogen);
            Assert.Equal(2, v.AufbautenOhneTeilkatalog);
            Assert.Contains(v.Protokoll, z => z == "Katalog_Zuordnung.csv: Teilkataloge aus dieser Datei; 7 von 8 Zeilen treffen einen Satz der Quelle");
            Assert.Contains(v.Protokoll, z => z == "Katalog_Zuordnung.csv: Zeilen mit Satz, aber ohne benannten Teilkatalog: 2");
            Assert.Contains(v.Protokoll, z => z == "Katalog_Zuordnung.csv: CatalogUId weicht vom Teilkatalog des Inhaltsknotens ab, CatalogUId gilt: 1");
            Assert.Contains(v.Protokoll, z => z.StartsWith("Teilkataloge in Katalog.csv: 2; aus Katalog_Zuordnung.csv; mit zwei oder mehr Teilkatalogen 1 Baustoffe",
                                                           StringComparison.Ordinal));
            Assert.DoesNotContain(v.Protokoll, z => z.StartsWith("Katalog_Inhalt.csv:", StringComparison.Ordinal));
        }

        [Fact]
        public void Fehlt_Katalog_Zuordnung_oder_taugt_sie_nicht_gilt_der_Weg_ueber_Katalog_Inhalt()
        {
            BauteilkatalogVorschau ohne = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(ProbeTeilkataloge())), null).Vorschau;
            Assert.False(ohne.TeilkatalogeAusZuordnung);
            Assert.Contains(ohne.Protokoll, z => z == "Datei fehlt, Teilkataloge aus Katalog_Inhalt.csv: Katalog_Zuordnung.csv");
            Assert.Equal(2, ohne.Teilkataloge);
            Assert.Equal(1, ohne.BaustoffeMitMehrerenTeilkatalogen);                                   // B2 mit Dublette B3

            Probe p = ProbeZuordnung();
            p.KopfKZ = "UId;CatalogContentUId;Kennzeichen;BasisUId;DimensionUId;StateType;SortNum";   // ohne CatalogUId
            BauteilkatalogImport.Plan plan = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(p)), null);
            Assert.Null(plan.Vorschau.Abgelehnt);
            Assert.False(plan.Vorschau.TeilkatalogeAusZuordnung);
            Assert.Contains(plan.Vorschau.Protokoll, z => z == "Katalog_Zuordnung.csv: Spalte fehlt (CatalogUId), Teilkataloge aus Katalog_Inhalt.csv");
            string q = plan.Vorschau.Quelle;
            Assert.Equal(q + ", Teilkataloge Teilkatalog A / Teilkatalog B", Quelle(plan.Baustoffe, 2));

            p = ProbeZuordnung();
            p.KZ.Add(new[] { "nur", "zwei" });                                                         // kaputte Zeile
            plan = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(p)), null);
            Assert.Null(plan.Vorschau.Abgelehnt);
            Assert.False(plan.Vorschau.TeilkatalogeAusZuordnung);
            Assert.Contains(plan.Vorschau.Protokoll, z => z.StartsWith("Katalog_Zuordnung.csv: Datensatz", StringComparison.Ordinal) &&
                                                          z.EndsWith("Teilkataloge aus Katalog_Inhalt.csv", StringComparison.Ordinal));

            p = ProbeZuordnung();
            p.KZ = p.KZ.Select(z => new[] { z[0], z[1], z[2], G(999), "", z[5], z[6] }).ToList();    // trifft keinen Satz
            plan = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(p)), null);
            Assert.True(plan.Vorschau.TeilkatalogeAusZuordnung);
            Assert.Contains(plan.Vorschau.Protokoll, z => z == "Katalog_Zuordnung.csv: keine Zeile trifft einen Satz der Quelle, ohne Teilkatalognamen weiter");
            Assert.Equal(0, plan.Vorschau.Teilkataloge);
            Assert.Equal(4, plan.Vorschau.BaustoffeOhneTeilkatalog);
        }

        [Fact]
        public void Katalog_Zuordnung_wird_auch_im_Unterordner_Export_gelesen()
        {
            Probe p = ProbeZuordnung();
            p.Unterordner = "Export";                                                                  // ganzer Export im Unterordner
            BauteilkatalogVorschau v = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Path.GetDirectoryName(Schreiben(p))), null).Vorschau;
            Assert.True(v.UnterordnerExport);
            Assert.True(v.TeilkatalogeAusZuordnung);
            Assert.Equal(3, v.AufbautenMitTeilkatalog);

            p = ProbeZuordnung();
            p.ZuordnungUnterordner = "Export";                                                         // nur die Zuordnung im Unterordner
            v = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(p)), null).Vorschau;
            Assert.False(v.UnterordnerExport);
            Assert.True(v.TeilkatalogeAusZuordnung);
            Assert.Contains(v.Protokoll, z => z == "Katalog_Zuordnung.csv gelesen aus dem Unterordner Export");
            Assert.Equal(1, v.BaustoffeMitMehrerenTeilkatalogen);
        }

        [Fact]
        public void Der_Wiederimport_mit_Katalog_Zuordnung_bleibt_konvergent()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string o = Schreiben(ProbeZuordnung());
            BauteilkatalogVorschau eins = BauteilkatalogImport.Uebernehmen(o);
            Assert.Null(eins.Fehler);
            Assert.True(eins.Geschrieben);
            string q = eins.Quelle;
            string Gelesen(string tabelle, int n) => DataRepository.ExecuteScalar(
                "SELECT \"Quelle\" FROM \"" + tabelle + "\" WHERE \"Quellkennung\" = ?", new DbParam("@k", G(n))) as string;
            Assert.Equal(q + ", Teilkataloge Teilkatalog A / Teilkatalog B", Gelesen("Tab_Baustoff_STAMM", 8));
            Assert.Equal(q + ", Teilkatalog Teilkatalog A", Gelesen("Tab_Baustoff_STAMM", 4));
            Assert.Equal(q + ", Teilkatalog Teilkatalog A", Gelesen("Tab_Bauteilaufbau_STAMM", 102));
            Assert.Equal(q + ", Teilkatalog Teilkatalog B", Gelesen("Tab_Bauteilaufbau_STAMM", 101));
            long baustoffe = Zahl("SELECT COUNT(*) FROM \"Tab_Baustoff_STAMM\"");
            long aufbauten = Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau_STAMM\"");
            string stand = string.Join("|", new[] { 1, 2, 4, 8 }.Select(n => Gelesen("Tab_Baustoff_STAMM", n)).Concat(
                                            new[] { 101, 102, 103, 104, 105 }.Select(n => Gelesen("Tab_Bauteilaufbau_STAMM", n))));

            BauteilkatalogVorschau zwei = BauteilkatalogImport.Uebernehmen(o);
            Assert.True(zwei.Geschrieben);
            Assert.Equal(0, zwei.Baustoffe.Neu);
            Assert.Equal(0, zwei.Aufbauten.Neu);
            Assert.Equal(baustoffe, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoff_STAMM\""));
            Assert.Equal(aufbauten, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau_STAMM\""));
            Assert.Equal(stand, string.Join("|", new[] { 1, 2, 4, 8 }.Select(n => Gelesen("Tab_Baustoff_STAMM", n)).Concat(
                                                 new[] { 101, 102, 103, 104, 105 }.Select(n => Gelesen("Tab_Bauteilaufbau_STAMM", n)))));
        }

        // ---- Nachzug E134 (BK-3b) -----------------------------------------------------------

        [Fact]
        public void Die_Code_Tafel_E134_ordnet_Stoffgruppen_Nachbargruppen_und_Haustechnik()
        {
            string[] erwartet = { null, "PUTZE", "BETON", "PLATTEN", "MAUERWERK", "DAEMMSTOFFE", "HOLZ", "ABDICHTUNGEN", "SONSTIGE" };
            for (int c = 1; c <= 8; c++)
            {
                (string g, _, bool h) = BauteilkatalogImport.GliederungAusCode(c, "probe", out bool ok);
                Assert.Equal(erwartet[c], g);
                Assert.True(ok);
                Assert.False(h);
            }
            foreach (int? leer in new int?[] { null, 0 })
            {
                (string g, string k, bool h) = BauteilkatalogImport.GliederungAusCode(leer, "probe", out bool ok);
                Assert.Equal("SONSTIGE", g);
                Assert.Null(k);
                Assert.True(ok);
                Assert.False(h);
            }
            foreach (int c in new[] { 14, 19, 29, 30 })
            {
                (string g, string k, bool h) = BauteilkatalogImport.GliederungAusCode(c, "probe", out bool ok);
                Assert.Equal("SONSTIGE", g);
                Assert.Equal(BauteilkatalogImport.KlasseHaustechnik(true), k);
                Assert.True(h);
                Assert.True(ok);
            }
            BauteilkatalogImport.GliederungAusCode(12, "probe", out bool gedeutet);
            Assert.False(gedeutet);

            // Nachbargruppen: die Klasse aus dem Namen zieht in die Gruppe des Katalogs.
            Assert.Equal(("ESTRICHE", "ZEMENTESTRICH", false), BauteilkatalogImport.GliederungAusCode(1, "probezementestrich", out _));
            Assert.Equal(("METALLE", "STAHL", false), BauteilkatalogImport.GliederungAusCode(8, "probestahl", out _));
            Assert.Equal(("BODENBELAEGE", "PARKETT", false), BauteilkatalogImport.GliederungAusCode(7, "probeparkett", out _));
            Assert.Equal(("PUTZE", "KALKPUTZ", false), BauteilkatalogImport.GliederungAusCode(1, "probekalkputz", out _));

            // Klasse Haustechnik: solange das Schema sie nicht führt, die sonstige Klasse der Gruppe (Prüfklausel).
            string kb = BauteilkatalogImport.KlasseHaustechnik(true), ka = BauteilkatalogImport.KlasseHaustechnik(false);
            Assert.Null(Bauteilkatalog.GliederungPruefen(true, "SONSTIGE", kb));
            Assert.Null(Bauteilkatalog.GliederungPruefen(false, "SONSTIGE", ka));

            // Bauteilart: 15|0 und 16|0 sind Wände, die Haustechnik-Codes gedeutet als Sonstiges.
            Assert.Equal(("AUSSENWAND", true), BauteilkatalogImport.BauteilartAusBasis(15, 0));
            Assert.Equal(("AUSSENWAND", true), BauteilkatalogImport.BauteilartAusBasis(16, 0));
            Assert.Equal(("AUSSENWAND", true), BauteilkatalogImport.BauteilartAusBasis(16, null));
            Assert.False(BauteilkatalogImport.BauteilartAusBasis(15, 3).Gedeutet);
            foreach (int c in new[] { 14, 19, 29, 30 })
                Assert.Equal(("SONSTIGES", true), BauteilkatalogImport.BauteilartAusBasis(c, 0));
        }

        [Theory]
        [InlineData(0.5, 1400.0, 0.5, BauteilkatalogImport.LambdaBefund.Gueltig)]
        [InlineData(10.0, null, 10.0, BauteilkatalogImport.LambdaBefund.Gueltig)]
        [InlineData(50.0, 7800.0, 50.0, BauteilkatalogImport.LambdaBefund.Metall)]
        [InlineData(20.0, 2500.0, 20.0, BauteilkatalogImport.LambdaBefund.Metall)]
        [InlineData(130.0, 600.0, 0.13, BauteilkatalogImport.LambdaBefund.Skaliert)]
        [InlineData(500.0, 2499.0, 0.5, BauteilkatalogImport.LambdaBefund.Skaliert)]
        [InlineData(61.0, null, 0.061, BauteilkatalogImport.LambdaBefund.Skaliert)]
        [InlineData(60.0, 600.0, null, BauteilkatalogImport.LambdaBefund.Abgelehnt)]      // Band 10–60: kein Skalenfehler
        [InlineData(45.0, 2000.0, null, BauteilkatalogImport.LambdaBefund.Abgelehnt)]
        [InlineData(900.0, 500.0, null, BauteilkatalogImport.LambdaBefund.Abgelehnt)]     // λ/1 000 über dem Band
        [InlineData(15.0, null, null, BauteilkatalogImport.LambdaBefund.Abgelehnt)]
        public void Die_Lambda_Plausibilitaet_haelt_Metall_Skalenfehler_und_Ablehnung(double lambda, double? rho, double? erwartet,
                                                                                   BauteilkatalogImport.LambdaBefund befund)
        {
            (double? l, BauteilkatalogImport.LambdaBefund b) = BauteilkatalogImport.LambdaPruefen(lambda, rho);
            Assert.Equal(befund, b);
            if (erwartet.HasValue) Assert.Equal(erwartet.Value, l.Value, 12);
            else Assert.Null(l);
        }

        /// <summary>Die Probe des Nachzugs E134 — synthetisch, Kennungen ab 500.</summary>
        private static Probe ProbeE134()
        {
            var p = new Probe();
            // N1: Code 0, Basis 4 → Mauerwerk.
            p.B.Add(new[] { G(501), "Probestein N", "", "", "0.6", "1500", "1", "0", "", "", "", ST });
            p.Z.Add(new[] { G(951), "4", "0", G(501) });
            // N2: Code leer, Basis 14 → Sonstige, Haustechnik.
            p.B.Add(new[] { G(502), "Probearmatur N", "", "", "0.3", "900", "1", "", "", "", "", ST });
            p.Z.Add(new[] { G(952), "14", "0", G(502) });
            // N3: Nichtmetall mit Skalenfehler → λ ÷ 1 000.
            p.B.Add(new[] { G(503), "Probeholzplatte N", "", "", "130", "600", "1.6", "6", "", "", "", ST });
            // N4: Metall (ρ ≥ 2 500) unter Code 8 → Metalle/Stahl, λ bleibt.
            p.B.Add(new[] { G(504), "Probestahl N", "", "", "50", "7800", "0.45", "8", "", "", "", ST });
            // N5: λ/1 000 außerhalb des Bandes → abgelehnt.
            p.B.Add(new[] { G(505), "Probeplatte N", "", "", "900", "500", "1", "3", "", "", "", ST });
            // N6: Code 0 ohne Basis → Sonstige, kein Code ohne Deutung.
            p.B.Add(new[] { G(506), "Probestoff N", "", "", "0.2", "800", "1", "0", "", "", "", ST });

            // Luftdurchlass (19) mit einer bauphysikalisch vollständigen Schicht → trotzdem gekennzeichnet.
            p.Z.Add(new[] { G(961), "19", "0", "" });
            p.A.Add(new[] { G(601), G(961), "Probedurchlass N", "", "", "", "", "", "", "", "", "", ST });
            p.S.Add(new[] { G(701), G(601), "", "1", "Probestahl N", "", "0.002", "50", "7800", "0.45", "8", "" });
            // 15|0 und 16|0 → Wand; Schicht mit Skalenfehler (λ 200, ρ 800 → 0,2).
            p.Z.Add(new[] { G(962), "15", "0", "" });
            p.A.Add(new[] { G(602), G(962), "Probewand N15", "", "", "", "0.5", "", "", "", "", "", ST });
            p.Z.Add(new[] { G(963), "16", "0", "" });
            p.A.Add(new[] { G(603), G(963), "Probewand N16", "", "", "0.3", "", "", "", "", "", "", ST });
            p.S.Add(new[] { G(702), G(603), "", "1", "Probeschicht N", "", "0.3", "200", "800", "1", "4", "" });
            return p;
        }

        [Fact]
        public void Der_Nachzug_E134_gliedert_skaliert_und_kennzeichnet_in_der_Vorschau()
        {
            BauteilkatalogImport.Plan p = BauteilkatalogImport.Planen(BauteilkatalogQuelle.Lesen(Schreiben(ProbeE134())), new BauteilkatalogStand());
            BauteilkatalogVorschau v = p.Vorschau;
            Assert.Null(v.Abgelehnt);
            Assert.Equal(5, v.Baustoffe.Neu);
            Assert.Equal(1, v.Baustoffe.Abgelehnt);
            Assert.Equal(1, v.Baustoffe.Gruende[BauteilkatalogImport.GRUND_LAMBDA]);
            Assert.Empty(v.CodesOhneDeutung);

            Assert.Equal("MAUERWERK", p.Baustoffe.Single(s => s.Kennung == G(501)).Gruppe);
            BauteilkatalogImport.Satz n2 = p.Baustoffe.Single(s => s.Kennung == G(502));
            Assert.Equal("SONSTIGE", n2.Gruppe);
            Assert.Equal(BauteilkatalogImport.KlasseHaustechnik(true), n2.Werte["Klasse_Code"]);
            Assert.Equal(0.13, p.Baustoffe.Single(s => s.Kennung == G(503)).Lambda.Value, 12);
            BauteilkatalogImport.Satz n4 = p.Baustoffe.Single(s => s.Kennung == G(504));
            Assert.Equal(50.0, n4.Lambda);
            Assert.Equal("METALLE", n4.Werte["Gruppe_Code"]);
            Assert.Equal("STAHL", n4.Werte["Klasse_Code"]);
            Assert.Equal("SONSTIGE", p.Baustoffe.Single(s => s.Kennung == G(506)).Gruppe);

            Assert.Equal(1, v.LambdaSkaliert);
            Assert.Contains(v.Protokoll, z => z.Contains(G(503)) && z.Contains("÷ 1 000") && z.Contains("130 → 0.13"));
            Assert.Contains(v.Protokoll, z => z.Contains(G(702)) && z.Contains("÷ 1 000"));
            Assert.Equal(0.2, p.Schichten.Single(s => s.Kennung == G(702)).Lambda.Value, 12);
            Assert.Equal(50.0, p.Schichten.Single(s => s.Kennung == G(701)).Lambda);
            Assert.Equal(1, v.HaustechnikBaustoffe);
            Assert.Equal(1, v.HaustechnikAufbauten);
            Assert.Equal(1, v.Luftdurchlaesse);

            BauteilkatalogImport.Satz durchlass = p.Aufbauten.Single(s => s.Kennung == G(601));
            Assert.Equal("SONSTIGES", durchlass.Werte["Bauteilart"]);
            Assert.Equal(1, durchlass.Werte["Simulation_Ungeeignet"]);
            foreach (string k in new[] { G(602), G(603) })
            {
                BauteilkatalogImport.Satz w = p.Aufbauten.Single(s => s.Kennung == k);
                Assert.Equal("AUSSENWAND", w.Werte["Bauteilart"]);
                Assert.Equal("WAENDE", w.Werte["Gruppe_Code"]);
            }
            Assert.Equal(0, p.Aufbauten.Single(s => s.Kennung == G(603)).Werte["Simulation_Ungeeignet"]);

            Assert.Equal(1, v.BaustoffGliederung["METALLE/STAHL"]);
            Assert.Equal(1, v.BaustoffGliederung["MAUERWERK/-"]);
            Assert.Equal(2, v.AufbauGliederung["WAENDE/AUSSENWAND"]);
            Assert.Equal(5, v.BaustoffGliederung.Values.Sum());
            Assert.Equal(3, v.AufbauGliederung.Values.Sum());

            // Die Hülle trägt die neuen Zähler in die Vorschau der Oberfläche.
            EPOS.UI.Dialoge.Bedarf.BauteilkatalogVorschauDaten d = BauteilkatalogImportHuelle.AlsDaten(v);
            Assert.Equal(1, d.LambdaSkaliert);
            Assert.False(string.IsNullOrEmpty(d.Haustechnik));
            Assert.Equal(v.BaustoffGliederung.Count + v.AufbauGliederung.Count, d.Gliederung.Count);
        }

        [Fact]
        public void Der_Wiederimport_mit_den_Regeln_E134_zieht_Gruppe_Klasse_Kennzeichen_und_Lambda_nach_ohne_Dubletten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string o = Schreiben(ProbeE134());
            BauteilkatalogVorschau eins = BauteilkatalogImport.Uebernehmen(o);
            Assert.True(eins.Geschrieben, eins.Fehler);
            const string IMP = " WHERE \"Herkunft\" = 'IMPORT'";
            long baustoffe = Zahl("SELECT COUNT(*) FROM \"Tab_Baustoff_STAMM\"" + IMP);
            long aufbauten = Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau_STAMM\"" + IMP);
            long schichten = Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilschicht_STAMM\"");
            Assert.Equal(5, baustoffe);
            Assert.Equal(3, aufbauten);

            // Stand der Regeln vor E134 nachstellen: Sonstige ohne Klasse, kein Kennzeichen am Durchlass, λ ungeteilt.
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_Baustoff_STAMM\" SET \"Gruppe_Code\" = 'SONSTIGE', \"Klasse_Code\" = NULL" + IMP);
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_Baustoff_STAMM\" SET \"Lambda\" = 130 WHERE \"Quellkennung\" = ?", new DbParam("@k", G(503)));
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_Bauteilaufbau_STAMM\" SET \"Bauteilart\" = 'SONSTIGES', \"Gruppe_Code\" = 'SONSTIGE', " +
                                           "\"Klasse_Code\" = 'SONSTIGES', \"Simulation_Ungeeignet\" = 0" + IMP);

            BauteilkatalogVorschau zwei = BauteilkatalogImport.Uebernehmen(o);
            Assert.True(zwei.Geschrieben, zwei.Fehler);
            Assert.Equal(0, zwei.Baustoffe.Neu);
            Assert.Equal(5, zwei.Baustoffe.Wiedererkannt);
            Assert.Equal(0, zwei.Aufbauten.Neu);
            Assert.Equal(3, zwei.Aufbauten.Wiedererkannt);
            Assert.Equal(baustoffe, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoff_STAMM\"" + IMP));
            Assert.Equal(aufbauten, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau_STAMM\"" + IMP));
            Assert.Equal(schichten, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilschicht_STAMM\""));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM (SELECT \"Quellkennung\" FROM \"Tab_Baustoff_STAMM\"" + IMP +
                                 " GROUP BY \"Quellkennung\" HAVING COUNT(*) > 1)"));

            string Feld(string tab, string spalte, string k) =>
                DataRepository.ExecuteScalar("SELECT \"" + spalte + "\" FROM \"" + tab + "\" WHERE \"Quellkennung\" = ?", new DbParam("@k", k)) as string;
            Assert.Equal("MAUERWERK", Feld("Tab_Baustoff_STAMM", "Gruppe_Code", G(501)));
            Assert.Equal("METALLE", Feld("Tab_Baustoff_STAMM", "Gruppe_Code", G(504)));
            Assert.Equal("STAHL", Feld("Tab_Baustoff_STAMM", "Klasse_Code", G(504)));
            Assert.Equal(BauteilkatalogImport.KlasseHaustechnik(true), Feld("Tab_Baustoff_STAMM", "Klasse_Code", G(502)));
            Assert.Equal(0.13, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT \"Lambda\" FROM \"Tab_Baustoff_STAMM\" WHERE \"Quellkennung\" = ?", new DbParam("@k", G(503))), CultureInfo.InvariantCulture), 12);
            Assert.Equal("AUSSENWAND", Feld("Tab_Bauteilaufbau_STAMM", "Bauteilart", G(602)));
            Assert.Equal("WAENDE", Feld("Tab_Bauteilaufbau_STAMM", "Gruppe_Code", G(603)));
            Assert.Equal(1L, Zahl("SELECT \"Simulation_Ungeeignet\" FROM \"Tab_Bauteilaufbau_STAMM\" WHERE \"Quellkennung\" = ?", new DbParam("@k", G(601))));
            Assert.Equal(BauteilkatalogImport.KlasseHaustechnik(false), Feld("Tab_Bauteilaufbau_STAMM", "Klasse_Code", G(601)));
        }

        [Fact]
        public void Ein_Fehler_in_einer_Schreibstufe_rollt_den_ganzen_Vorgang_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var k = Katalogsatz();
            string o = Schreiben(Hauptprobe((k.Name, k.Code, k.L, k.R, k.C)));
            BauteilkatalogImport.Schreibhaken = stufe => { if (stufe == "Abschnitte") throw new InvalidOperationException("Probefehler"); };
            BauteilkatalogVorschau e = BauteilkatalogImport.Uebernehmen(o);
            Assert.False(e.Geschrieben);
            Assert.Equal("Probefehler", e.Fehler);
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Baustoff_STAMM\" WHERE \"Herkunft\" = 'IMPORT'"));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau_STAMM\" WHERE \"Herkunft\" = 'IMPORT'"));
            Assert.Equal(k.Quelle, DataRepository.ExecuteScalar("SELECT \"Quelle\" FROM \"Tab_Baustoff_STAMM\" WHERE \"ID\" = ?", new DbParam("@id", k.Id)) as string);
        }

        [Fact]
        public void Die_Mengen_des_Feldinventars_laufen_in_wenigen_Sekunden()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var p = new Probe();
            string[] namen = { "Synthetischer Mauerstein", "Synthetische Dämmung", "Synthetischer Beton", "Synthetisches Holz" };
            int[] codes = { 4, 5, 2, 6 };
            for (int i = 1; i <= 10719; i++)
                p.B.Add(new[] { G(i), namen[i % 4] + " " + i.ToString(CultureInfo.InvariantCulture), "Lang " + i, i % 7 == 0 ? "Info\nmehrzeilig" : "",
                                (0.03 + (i % 50) * 0.01).ToString("R", CultureInfo.InvariantCulture), (100 + i % 2000).ToString(CultureInfo.InvariantCulture),
                                i % 2 == 0 ? "1" : "1000", codes[i % 4].ToString(CultureInfo.InvariantCulture), "1", "0", "", ST });
            for (int a = 1; a <= 8427; a++)
            {
                p.Z.Add(new[] { G(100000 + a), (1 + a % 5).ToString(CultureInfo.InvariantCulture), "0", "" });
                p.A.Add(new[] { G(200000 + a), G(100000 + a), "Synthetischer Aufbau " + a, "", "", "0.3", "0.25", "", "0.13", "0.04", "", "", ST });
            }
            int n = 0;
            for (int a = 1; n < 9296; a++)
                for (int s = 1; s <= 6 && n < 9296; s++, n++)
                    p.S.Add(new[] { G(300000 + n), G(200000 + a), "", s.ToString(CultureInfo.InvariantCulture), namen[s % 4] + " " + (s + a).ToString(CultureInfo.InvariantCulture),
                                    "", "0.05", "0.5", "1000", "1", codes[s % 4].ToString(CultureInfo.InvariantCulture), "" });
            string o = Schreiben(p);

            var uhr = Stopwatch.StartNew();
            BauteilkatalogVorschau v = BauteilkatalogImport.Vorschau(o);
            long vorschauMs = uhr.ElapsedMilliseconds;
            uhr.Restart();
            BauteilkatalogVorschau e = BauteilkatalogImport.Uebernehmen(o);
            long schreibenMs = uhr.ElapsedMilliseconds;
            _aus.WriteLine("Vorschau {0} ms, Übernehmen {1} ms", vorschauMs, schreibenMs);
            Assert.Null(e.Fehler);
            Assert.Equal(10719, v.Baustoffe.Neu + v.Baustoffe.MitKatalog + v.Baustoffe.InQuelle);
            Assert.Equal(8427, e.Aufbauten.Neu);
            Assert.Equal(9296, e.Schichten.Neu);
            Assert.Equal(9296L, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilschicht_STAMM\" s JOIN \"Tab_Bauteilaufbau_STAMM\" a ON a.\"ID\" = s.\"ID_Aufbau\" " +
                                     "WHERE a.\"Herkunft\" = 'IMPORT'"));
            Assert.InRange(schreibenMs, 0, 60000);
        }
    }
}
