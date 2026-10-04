using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wache des Referenzprojekts 1054 „Referenzprojekt Zonen mit Heizkreis"</b> (Welle AK1z, Wärmeübergabe je
    /// Zone, E63) — die Kopie des Zonenprojekts 1052 mit Anlagenkopplung AK1: das Gebäude mit Heizkreis und
    /// Radiatoren, die Zone „Gastronomie und Verwaltung" mit eigener Übergabe (Konvektor, 70/50 °C,
    /// Proportionalband 2 K), Gästezimmer und Keller mit den Werten des Gebäudes. Die gesäten Zellen stehen in
    /// EINER Quelle, dem Bauplan <c>Referenzlaeufe/Skripte/referenzprojekt_1054_bauplan.cs</c> (hier verlinkt);
    /// das Saatskript <c>referenzprojekt_1054_zonen_heizkreis.cs</c> zieht ihn.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Zelle</b> steht wie geplant: Kopplungsstufe, Gebäudespalten, die drei Zonen samt
    /// den sieben Übergabespalten.</item>
    /// <item><b>1054 ist eine Kopie von 1052</b>: dieselben Zonen, Bauteile, Trennflächen, Luftströme und
    /// Kalender.</item>
    /// <item><b>Ein Lauf aus der Datenbank</b> schreibt drei Zonenzeilen mit Vorlauf, Rücklauf und begrenzten
    /// Stunden je beheizter Zone (leer am Keller), das Gebäude trägt Vorlauf und Rücklauf, die Zonen stehen
    /// GEKOPPELT, und zwei Läufe sind bitgleich.</item>
    /// </list>
    /// 1054 gehört zu den Einfrierregeln „gesäte Auslegungsdaten der Übergabe" und „gesäte Zonendaten"
    /// (<c>Referenzlaeufe/LIESMICH.md</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenHeizkreisReferenzprojektWacheTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public ZonenHeizkreisReferenzprojektWacheTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        private const int PROJEKT = Zonenprojekt1054.NEU;

        private static string F(double? x, string format = "F2")
            => x.HasValue ? x.Value.ToString(format, CultureInfo.InvariantCulture) : "∅";

        private static long Zahl(string sql, params object[] w) => Zonenprojekt1052.Zahl(sql, w);

        // =====================================================================
        //  Jede gesäte Zelle
        // =====================================================================

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_geplant()
        {
            if (!_db.Vorhanden) return;
            List<string> abw = Zonenprojekt1054.Pruefen(PROJEKT);
            Assert.True(abw.Count == 0, string.Join("\n", abw));

            // Die gesäten Werte ausdrücklich (gegen einen Bauplan, der mitwandert).
            Assert.Equal(Zonenprojekt1054.NAME, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture));
            Assert.Equal("AK1", KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));
            Assert.Equal(0L, Zahl("SELECT Kuehlbetrieb FROM Tab_Einstellungen WHERE ID_Projekt = ?", PROJEKT));
            Assert.True(KonfigurationCtrl.AufheizvorgabeLesen(PROJEKT).An);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Z_ProjektGebaeude WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Kosten_Geaendert IS NULL", PROJEKT));

            int geb = Zonenprojekt1052.Gebaeude(PROJEKT);
            DataRow g = Zonenprojekt1052.Tabelle("SELECT * FROM Tab_Gebaeude WHERE ID = ?", geb).Rows[0];
            Assert.Equal(1L, Convert.ToInt64(g["Heizkreis_Aktiv"], CultureInfo.InvariantCulture));
            Assert.Equal("RADIATOR", g["Uebergabe_Art"]);
            Assert.Equal(0L, Convert.ToInt64(g["Heizkurve_Aktiv"], CultureInfo.InvariantCulture));
            foreach (string s in new[] { "Uebergabe_Exponent", "Uebergabe_Leistung_Nenn", "Auslegung_Vorlauf", "Auslegung_Ruecklauf",
                                         "Auslegung_Raumtemperatur", "Auslegung_Aussentemperatur", "Heizkurve_Niveau",
                                         "Heizkurve_Steilheit", "Regler_Proportionalband", "Sollwertprofil" })
                Assert.True(g[s] == DBNull.Value, "Gebäude." + s + " ist nicht leer");

            List<ZoneModel> z = new GebaeudeZonenCtrl().LesenJeGebaeude(geb);
            Assert.Equal(new[] { Zonenprojekt1052.ZONE_GAESTE, Zonenprojekt1052.ZONE_GASTRO, Zonenprojekt1052.ZONE_KELLER },
                         z.Select(x => x.Bezeichner).ToArray());
            Assert.Equal(new[] { true, true, false }, z.Select(x => x.IstBeheizt).ToArray());
            ZoneModel gastro = z[1];
            Assert.Equal("KONVEKTOR", gastro.Uebergabe_Art);
            Assert.Equal(70.0, gastro.Auslegung_Vorlauf);
            Assert.Equal(50.0, gastro.Auslegung_Ruecklauf);
            Assert.Equal(2.0, gastro.Regler_Proportionalband);
            Assert.Null(gastro.Uebergabe_Exponent);
            Assert.Null(gastro.Uebergabe_Leistung_Nenn);
            Assert.Null(gastro.Auslegung_Raumtemperatur);
            foreach (ZoneModel x in new[] { z[0], z[2] })
            {
                Assert.Null(x.Uebergabe_Art);
                Assert.Null(x.Uebergabe_Exponent);
                Assert.Null(x.Uebergabe_Leistung_Nenn);
                Assert.Null(x.Auslegung_Vorlauf);
                Assert.Null(x.Auslegung_Ruecklauf);
                Assert.Null(x.Auslegung_Raumtemperatur);
                Assert.Null(x.Regler_Proportionalband);
            }
        }

        // =====================================================================
        //  Kopie von 1052
        // =====================================================================

        [Fact]
        public void Projekt_1054_ist_eine_Kopie_von_1052()
        {
            if (!_db.Vorhanden) return;
            string soll = Zonenprojekt1054.Zaehlung(Zonenprojekt1052.NEU);
            Assert.Equal("Zonen 3, Bauteile 31, Trennflächen 3, Luftströme 1, Kalender 2, Perioden 9, Vorgaben 2", soll);
            Assert.Equal(soll, Zonenprojekt1054.Zaehlung(PROJEKT));

            // Die Zonen tragen dieselben Werte (ohne Schlüssel und Übergabespalten) und dieselben Bauteile.
            List<ZoneModel> a = new GebaeudeZonenCtrl().LesenJeGebaeude(Zonenprojekt1052.Gebaeude(Zonenprojekt1052.NEU));
            List<ZoneModel> b = new GebaeudeZonenCtrl().LesenJeGebaeude(Zonenprojekt1052.Gebaeude(PROJEKT));
            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.Equal((a[i].Bezeichner, a[i].Rang, a[i].Nutzflaeche, a[i].IstBeheizt, a[i].Raumsolltemperatur_Tag),
                             (b[i].Bezeichner, b[i].Rang, b[i].Nutzflaeche, b[i].IstBeheizt, b[i].Raumsolltemperatur_Tag));
                Assert.Equal(a[i].Bauteile.Select(x => (x.Bezeichner, x.Flaeche, x.U_Wert)).ToArray(),
                             b[i].Bauteile.Select(x => (x.Bezeichner, x.Flaeche, x.U_Wert)).ToArray());
            }
            // Die Vorlage bleibt die einzige Zonenquelle ohne Kopplung.
            Assert.Null(KonfigurationCtrl.AnlagenkopplungLesen(Zonenprojekt1052.NEU));
        }

        // =====================================================================
        //  Der Lauf
        // =====================================================================

        /// <summary>Die Ergebniszeilen eines Laufs ohne Schlüsselspalten - der Vergleichstext zweier Läufe.</summary>
        private static string Abdruck(int kopf)
        {
            var sb = new StringBuilder();
            void Teil(string sql)
            {
                DataTable t = Zonenprojekt1052.Tabelle(sql, kopf);
                foreach (DataRow r in t.Rows)
                {
                    foreach (DataColumn c in t.Columns)
                    {
                        if (c.ColumnName == "ID" || c.ColumnName.StartsWith("ID_", StringComparison.Ordinal)) continue;
                        object w = r[c];
                        sb.Append(w == DBNull.Value ? "∅" : w is double d ? d.ToString("R", CultureInfo.InvariantCulture)
                                  : Convert.ToString(w, CultureInfo.InvariantCulture)).Append('|');
                    }
                    sb.Append('\n');
                }
            }
            Teil("SELECT * FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = ? ORDER BY ID");
            Teil("SELECT * FROM Tab_ErgebnisZone WHERE ID_ErgebnisGebaeude IN (SELECT ID FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = ?) ORDER BY Rang");
            return sb.ToString();
        }

        [Fact]
        public void Ein_Lauf_schreibt_Vorlauf_Ruecklauf_und_begrenzte_Stunden_je_Zone_und_ist_bitgleich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int kopf1 = new SimulationRunner().SimuliereUndSpeichere(PROJEKT, out string fehler1);
            Assert.True(kopf1 > 0, "Lauf gescheitert: " + fehler1);
            string eins = Abdruck(kopf1);

            DataTable zz = Zonenprojekt1052.Tabelle(
                "SELECT Bezeichner, IstBeheizt, Vorlauf_Mittel_C, Ruecklauf_Mittel_C, Uebergabe_Begrenzt_H, Aufheiz_Zustand, Heizwaerme_Mwh " +
                "FROM Tab_ErgebnisZone WHERE ID_ErgebnisGebaeude IN (SELECT ID FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = ?) ORDER BY Rang", kopf1);
            Assert.Equal(3, zz.Rows.Count);
            Assert.Equal(new[] { Zonenprojekt1052.ZONE_GAESTE, Zonenprojekt1052.ZONE_GASTRO, Zonenprojekt1052.ZONE_KELLER },
                         zz.Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0], CultureInfo.InvariantCulture)).ToArray());
            for (int i = 0; i < 2; i++)
            {
                DataRow r = zz.Rows[i];
                foreach (int s in new[] { 2, 3, 4 })
                    Assert.True(r[s] != DBNull.Value, r[0] + "." + zz.Columns[s].ColumnName + " ist leer");
                Assert.True(Convert.ToDouble(r[2], CultureInfo.InvariantCulture) > Convert.ToDouble(r[3], CultureInfo.InvariantCulture),
                            r[0] + ": Vorlauf nicht über Rücklauf");
                Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, r[5]);
            }
            DataRow keller = zz.Rows[2];
            foreach (int s in new[] { 2, 3, 4 })
                Assert.True(keller[s] == DBNull.Value, "Keller." + zz.Columns[s].ColumnName + " ist nicht leer");

            ErgebnisGebaeudeModel g = Assert.Single(new ErgebnisCtrl().Load(PROJEKT).Gebaeude);
            Assert.Equal("RADIATOR", g.UebergabeArt);
            Assert.NotNull(g.VorlaufMittelC);
            Assert.NotNull(g.RuecklaufMittelC);
            Assert.True(g.VorlaufMittelC > g.RuecklaufMittelC);
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, g.AufheizZustand);

            int kopf2 = new SimulationRunner().SimuliereUndSpeichere(PROJEKT, out string fehler2);
            Assert.True(kopf2 > 0, "Zweiter Lauf gescheitert: " + fehler2);
            string zwei = Abdruck(kopf2);
            Assert.Equal(eins, zwei);

            _aus.WriteLine("1054 Gebäude: Heizwärme {0} MWh, Spitze {1} kW, Vorlauf {2} °C, Rücklauf {3} °C, begrenzt {4} h, Aufheizzustand {5}",
                           F(g.HeizwaermeMwh, "F3"), F(g.SpitzeKw, "F3"), F(g.VorlaufMittelC), F(g.RuecklaufMittelC),
                           F(g.UebergabeBegrenztStundenH, "F0"), g.AufheizZustand);
            foreach (DataRow r in zz.Rows)
                _aus.WriteLine("1054 Zone {0}: Heizwärme {1} MWh, Vorlauf {2} °C, Rücklauf {3} °C, begrenzt {4} h, Zustand {5}",
                               r[0], r[6] == DBNull.Value ? "∅" : F(Convert.ToDouble(r[6], CultureInfo.InvariantCulture), "F3"),
                               r[2] == DBNull.Value ? "∅" : F(Convert.ToDouble(r[2], CultureInfo.InvariantCulture)),
                               r[3] == DBNull.Value ? "∅" : F(Convert.ToDouble(r[3], CultureInfo.InvariantCulture)),
                               r[4] == DBNull.Value ? "∅" : F(Convert.ToDouble(r[4], CultureInfo.InvariantCulture), "F0"),
                               r[5] == DBNull.Value ? "∅" : r[5]);
        }
    }
}
