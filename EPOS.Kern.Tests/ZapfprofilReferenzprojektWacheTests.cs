using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache der Einfrierregel „gesäte Zapfprofil-Eingaben"</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 3.4, Anwenderentscheid ZU7; <c>Referenzlaeufe/LIESMICH.md</c>).
    ///
    /// <para><b>Gegenstand ist genau ein Referenzprojekt:</b> 1045 „Prüfprojekt Ost/West Stränge"
    /// rechnet sein Brauchwasser über den Zapfprofilgenerator — eine Projektzeile in
    /// <c>Tab_TwwProjekt</c> mit <c>Weg = 'GENERATOR'</c> und eine Zone der Nutzungsart „Wohnen groß
    /// (abgeleitet)" am Gebäude 10651, gesät von
    /// <c>Referenzlaeufe/Skripte/referenzprojekt_zapfprofil.py</c>. Alle übrigen Referenzprojekte
    /// stehen ohne Projektzeile und ohne Zone auf dem Bestandsweg.</para>
    ///
    /// <para><b>Was sie hält:</b> jede gesäte Zelle der Projektzeile und der Zone, die
    /// Kennzeichen der benutzten Katalogzeile samt Tagesgangsatz und die Parameter des
    /// Kaltwassers; dazu die Jahresenergie der Generator-Bilanz von 1045 und — Stunde für Stunde
    /// zeichengleich — die Brauchwasserreihe <c>Projekt_1045/waermebedarf_brauchwasser.csv</c> der
    /// aktuellen Basis. Wer eine dieser Zellen ändert, ohne die Basis neu einzufrieren, sieht sie
    /// rot. Alles kulturunabhängig (InvariantCulture, Muster <see cref="GebaeudeRueckwegTests"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZapfprofilReferenzprojektWacheTests : IClassFixture<TestDatenbank>
    {
        /// <summary>Das Referenzprojekt auf dem Generator.</summary>
        public const int PROJEKT = 1045;

        /// <summary>Sein einziges Gebäude; die Zone bindet sich daran.</summary>
        public const int GEBAEUDE = 10651;

        /// <summary>Die Nutzungsart der Zone (Katalogversion <c>TEST-1</c>).</summary>
        public const string NUTZUNGSART = "Wohnen groß (abgeleitet)";

        /// <summary>
        /// Die Jahresenergie der Generator-Bilanz von 1045 [kWh/a], wie sie die Basis R20 einfror
        /// (<c>aggregate.csv</c>, <c>Vektor.waermebedarf_brauchwasser.Summe</c>).
        /// </summary>
        public const double JAHRESENERGIE_KWH = 5006.62138;

        /// <summary>Die vierzehn Referenzprojekte der Basis.</summary>
        private static readonly int[] Referenzprojekte =
            { 1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047 };

        private readonly TestDatenbank _db;

        public ZapfprofilReferenzprojektWacheTests(TestDatenbank db) { _db = db; }

        /// <summary>
        /// Genau ein Referenzprojekt steht auf dem Generator — 1045; die übrigen dreizehn tragen
        /// weder Projektzeile noch Zone und rechnen auf dem Bestandsweg.
        /// </summary>
        [Fact]
        public void Genau_ein_Referenzprojekt_steht_auf_dem_Generator()
        {
            if (!_db.Vorhanden) return;

            var generator = new List<int>();
            foreach (int p in Referenzprojekte)
            {
                if (ZapfprofilCtrl.Weg(p) == BrauchwasserWeg.Generator) { generator.Add(p); continue; }
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_TwwProjekt WHERE ID_Projekt = ?", p));
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_TwwZone WHERE ID_Projekt = ?", p));
            }
            Assert.Equal(new[] { PROJEKT }, generator);
        }

        /// <summary>Die gesäten Zellen der Projektzeile von 1045 — jede Spalte, auch die leeren.</summary>
        [Fact]
        public void Die_Projektzeile_von_1045_steht_wie_gesaet()
        {
            if (!_db.Vorhanden) return;

            DataRow r = EineZeile("SELECT * FROM Tab_TwwProjekt WHERE ID_Projekt = ?", PROJEKT);
            var gesetzt = new Dictionary<string, object>
            {
                ["Weg"] = TwwSchema.WEG_GENERATOR,
                ["Jahresreihe_Stochastisch"] = 0L,
                ["Seed"] = 1045L,
                ["Realisierungen"] = 10L,
                ["Perzentil"] = 99L,
                ["Zirk_Auto"] = 1L,
                ["Zirk_Methode"] = 3L,
                ["Lade_Auto"] = 1L,
                ["Speicherart"] = 1L,
                ["Personen_Auto"] = 1L,
                ["Typtage_Aktiv"] = 0L,
                ["Aenderungsdatum"] = new DateTime(2026, 9, 26),
            };
            ZeileGleich(r, gesetzt, "ID", "ID_Projekt");
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_TwwKonstruktorzeile WHERE ID_TwwProjekt = ?",
                                  Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture)));
        }

        /// <summary>Die gesäten Zellen der einen Zone von 1045 — ohne Wohnungstypen, ohne Messwert.</summary>
        [Fact]
        public void Die_Zone_von_1045_steht_wie_gesaet()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_TwwZone WHERE ID_Projekt = ?", PROJEKT));
            DataRow r = EineZeile("SELECT * FROM Tab_TwwZone WHERE ID_Projekt = ?", PROJEKT);
            long nutzungsart = Zahl("SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = 'TEST-1'",
                                    NUTZUNGSART);
            var gesetzt = new Dictionary<string, object>
            {
                ["ID_Nutzungsart"] = nutzungsart,
                ["ID_Gebaeude"] = (long)GEBAEUDE,
                ["Reihenfolge"] = 1L,
                ["Name"] = "Wohnen",
                ["Bezugsmenge"] = 8.3,
                ["Niveau"] = 2L,
                ["Topologie"] = 1L,
                ["Zirkulation"] = 0L,
                ["Tagesbedarf_Auto"] = 1L,
            };
            ZeileGleich(r, gesetzt, "ID", "ID_Projekt");
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_TwwWohnungstyp WHERE ID_Zone = ?",
                                  Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Die Katalogzeilen, die die Zone benutzt: die Nutzungsart mit ihren Kennzeichen und
        /// Bedarfswerten, ihr Tagesgangsatz mit vier vollständigen Tagesgängen und die drei
        /// Kaltwasser-Parameter der Bilanz.
        /// </summary>
        [Fact]
        public void Die_benutzten_Katalogzeilen_stehen_wie_gesaet()
        {
            if (!_db.Vorhanden) return;

            DataRow n = EineZeile("SELECT * FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = 'TEST-1'",
                                  NUTZUNGSART);
            Assert.Equal(1L, Ganz(n, "Bezugsart"));
            Assert.Equal(1L, Ganz(n, "Kalenderart"));
            Assert.Equal(1L, Ganz(n, "Bilanzgrenze"));
            Assert.Equal(60.0, Kommazahl(n, "Bezug_Zapftemperatur"));
            Assert.Equal(12.0, Kommazahl(n, "Bezug_Kaltwasser"));
            Assert.Equal(1.618896, Kommazahl(n, "Bedarf_Mittel"), 9);
            Assert.Equal("VERFAHREN", Convert.ToString(n["Bedarf_Herkunftsart"], CultureInfo.InvariantCulture));

            long satz = Ganz(n, "ID_Tagesgangsatz");
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM Tab_TwwTagesgang_STAMM WHERE ID_Tagesgangsatz = ?", satz));

            var kaltwasser = new Dictionary<string, double>
            {
                [ZapfParameter.KALTWASSER_MITTEL] = 11.0,
                [ZapfParameter.KALTWASSER_AMPLITUDE] = 3.0,
                [ZapfParameter.KALTWASSER_MONAT_MAXIMUM] = 9.0,
            };
            Parametersatz ps = ZapfprofilCtrl.Parameter();
            Assert.Equal("TEST-1", ps.Katalogversion);
            foreach (KeyValuePair<string, double> k in kaltwasser)
                Assert.Equal(k.Value, ps.Lies(k.Key).Wert);
        }

        /// <summary>
        /// <b>Die Generator-Bilanz von 1045</b> rechnet wie die Basis: Jahresenergie auf 1e‑6
        /// genau und die Stundenreihe zeichengleich mit <c>waermebedarf_brauchwasser.csv</c>.
        /// </summary>
        [Fact]
        public void Die_Generator_Bilanz_von_1045_rechnet_wie_die_Basis()
        {
            if (!_db.Vorhanden) return;

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            var sim = new SimulationWaermebedarf();
            sim.Waermebedarf_berechnen(PROJEKT, projekt.m_ID_Klimaregion);
            Assert.True(string.IsNullOrEmpty(sim.Fehlertext), "Der Generatorweg brach ab: " + sim.Fehlertext);

            double summe = sim.brauchwasserwerte.Sum();
            Assert.True(Math.Abs(summe - JAHRESENERGIE_KWH) <= 1e-6 * JAHRESENERGIE_KWH,
                string.Format(CultureInfo.InvariantCulture, "Jahresenergie {0:R} kWh statt {1:R} kWh", summe, JAHRESENERGIE_KWH));

            string[] basis = BasisReihe("waermebedarf_brauchwasser.csv");
            Assert.Equal("Index;Wert", basis[0]);
            Assert.Equal(8760, basis.Length - 1);
            for (int h = 0; h < 8760; h++)
            {
                string ist = h.ToString(CultureInfo.InvariantCulture) + ";" + ZahlText(sim.brauchwasserwerte[h]);
                Assert.True(basis[h + 1] == ist, $"Stunde {h}: Basis '{basis[h + 1]}', Lauf '{ist}'");
            }
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static long Zahl(string sql, object wert)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, new DbParam("@p", wert)), CultureInfo.InvariantCulture);

        private static DataRow EineZeile(string sql, object wert)
        {
            DataTable t = DataRepository.GetDataTable(sql, new DbParam("@p", wert));
            Assert.True(t != null && t.Rows.Count == 1, "Erwartet genau eine Zeile: " + sql);
            return t.Rows[0];
        }

        private static long Ganz(DataRow r, string spalte) => Convert.ToInt64(r[spalte], CultureInfo.InvariantCulture);

        private static double Kommazahl(DataRow r, string spalte) => Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture);

        /// <summary>Jede gesetzte Spalte trägt ihren Wert, jede übrige (außer <paramref name="ohne"/>) NULL.</summary>
        private static void ZeileGleich(DataRow r, IReadOnlyDictionary<string, object> gesetzt, params string[] ohne)
        {
            foreach (DataColumn c in r.Table.Columns)
            {
                if (ohne.Contains(c.ColumnName)) continue;
                object ist = r[c];
                if (!gesetzt.TryGetValue(c.ColumnName, out object soll))
                {
                    Assert.True(ist == null || ist == DBNull.Value, $"{c.ColumnName}: erwartet NULL, steht '{ist}'.");
                    continue;
                }
                Assert.False(ist == null || ist == DBNull.Value, $"{c.ColumnName}: erwartet '{soll}', steht NULL.");
                bool gleich = soll switch
                {
                    string s => s == Convert.ToString(ist, CultureInfo.InvariantCulture),
                    double d => d == Convert.ToDouble(ist, CultureInfo.InvariantCulture),
                    long l => l == Convert.ToInt64(ist, CultureInfo.InvariantCulture),
                    DateTime dt => dt == Convert.ToDateTime(ist, CultureInfo.InvariantCulture),
                    _ => Equals(soll, ist)
                };
                Assert.True(gleich, $"{c.ColumnName}: erwartet '{soll}', steht '{ist}'.");
            }
        }

        /// <summary>Die Schreibweise des Referenzlaufs (<c>Ergebnisexport.Zahl</c>).</summary>
        private static string ZahlText(double d)
        {
            if (double.IsNaN(d)) return "NaN";
            if (double.IsPositiveInfinity(d)) return "Inf";
            if (double.IsNegativeInfinity(d)) return "-Inf";
            return d.ToString("G9", CultureInfo.InvariantCulture);
        }

        /// <summary>Die Zeilen einer Datei von 1045 in der <b>aktuellen</b> Basis (Muster <see cref="GebaeudeRueckwegTests"/>).</summary>
        private static string[] BasisReihe(string datei)
        {
            string[] basen = Directory.GetDirectories(Path.Combine(Wurzel(), "Referenzlaeufe"))
                .Where(o => Regex.IsMatch(Path.GetFileName(o), @"^\d{4}-\d{2}-\d{2}_R\d+_"))
                .ToArray();
            Assert.True(basen.Length == 1,
                "Unter Referenzlaeufe/ liegt nicht genau eine Basis: " + string.Join(", ", basen));

            string pfad = Path.Combine(basen[0], "Projekt_" + PROJEKT, datei);
            Assert.True(File.Exists(pfad), "Die Basis führt " + pfad + " nicht.");
            return File.ReadAllLines(pfad);
        }

        private static string Wurzel()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj")))
                d = d.Parent;
            Assert.True(d != null, "Die Repowurzel ist vom Ausgabeordner aus nicht zu finden.");
            return d.FullName;
        }
    }
}
