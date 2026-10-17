using System;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Prüfprojekt 1053 „Test BHKW mit PV ohne Kaskade"</b> — ein BHKW mit Photovoltaik ohne
    /// Speicherflotte, ohne Stromspeicher und ohne Einspeisegrenze, mit schwankendem Viertelstundenbedarf.
    ///
    /// <para>Ohne Flotte ist die BHKW-Einspeisung die Viertelstundenbilanz des Laufs
    /// (<see cref="SimulationControl.BhkwEinspeisung_viertelstuendlich"/>, Stundenmittel
    /// <see cref="SimulationControl.BhkwEinspeisungDesLaufs"/>): Zeitreihensatz (<c>BHKW_UEBERSCHUSS</c>),
    /// BHKW-Reiter, Kennzahl <c>EinspeisungMwh</c>, der KWK-Split der Strommatrix und der
    /// Excel-Monatsblock lesen dieselbe Reihe. 1053 erzeugt den Fall, in dem sich das von einer
    /// Klemmung des Stundenmittels unterscheidet: der Katalog-Lastgang „test" (Viertelstundenwerte,
    /// × 0,005) schwankt innerhalb der Stunde um die BHKW-Leistung. Angelegt von
    /// <c>Referenzlaeufe/Skripte/pruefprojekt_1053_bhkw_pv.cs</c> (Kopie von 1018, nur das BHKW in der
    /// Kaskade, 60 PV-Module, Stromverbraucher Hotel 50 MWh/a, Lastgang „test").</para>
    ///
    /// <para>Kein Referenzprojekt: 1053 steht in keiner Basis und in keiner Projektliste der CI.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BhkwPvPruefprojektTests : IDisposable
    {
        internal const int PROJEKT = 1053;
        internal const string NAME = "Test BHKW mit PV ohne Kaskade";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static object Wert(string sql, params object[] w)
            => DataRepository.ExecuteScalar(sql, w.Select(x => new DbParam("?", x ?? DBNull.Value)).ToArray());

        private static long Zahl(string sql, params object[] w)
        {
            object o = Wert(sql, w);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        [Fact]
        public void Projekt_1053_hat_das_BHKW_allein_in_der_Kaskade_PV_und_keinen_Speicher()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(NAME, Convert.ToString(Wert("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", PROJEKT)));

            DataTable e = DataRepository.GetDataTable(
                "SELECT Tool_1, Tool_2, Tool_3, Tool_4, Tool_5, Tool_6, Einspeisegrenze_Wert FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                new DbParam("?", PROJEKT));
            DataRow r = Assert.Single(e.Rows.Cast<DataRow>());
            string T(string s) => r[s] == DBNull.Value ? "" : Convert.ToString(r[s]);
            Assert.Equal(DbWerte.ERZEUGER_BHKW, T("Tool_1"));
            Assert.Equal("", T("Tool_2"));
            Assert.Equal("", T("Tool_3"));
            Assert.Equal("", T("Tool_4"));
            Assert.Equal(DbWerte.ERZEUGER_PHOTOVOLTAIK, T("Tool_5"));
            Assert.Equal("", T("Tool_6"));
            Assert.Equal(DBNull.Value, r["Einspeisegrenze_Wert"]);

            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_BHKW > 0", PROJEKT));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen a JOIN Tab_PV p ON p.ID = a.ID_PV " +
                                 "WHERE a.ID_Projekt = ? AND p.ID_Projekt = ? AND a.PV_Leistung > 0", PROJEKT, PROJEKT));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ?", PROJEKT));
            // Der Lastgang „test": eine Projektkopie mit 35 040 Viertelstundenwerten (Zeitinterval 4).
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Z_ProjektStromganglinie z JOIN Tab_Stromganglinie g " +
                                 "ON g.ID = z.ID_Ganglinie WHERE z.ID_Projekt = ? AND g.ID_Projekt = ? AND " +
                                 "g.Bezeichner = 'test' AND g.Zeitinterval = 4", PROJEKT, PROJEKT));
            Assert.Equal(35040, Zahl("SELECT COUNT(*) FROM Tab_StromganglinieDaten d JOIN Tab_Stromganglinie g " +
                                     "ON g.ID = d.ID_Ganglinie WHERE g.ID_Projekt = ?", PROJEKT));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Stromspeicher WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ?", PROJEKT));
        }

        [Fact]
        public void Der_Lauf_von_1053_erzeugt_BHKW_Ueberschuss_mit_PV_ohne_Flotte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            SimulationControl sim = lauf.sim;

            Assert.True(sim.bSimulationBHKW);
            Assert.True(sim.bSimulationPV);
            Assert.Null(sim.Speicherflottennetzbilanz);
            Assert.True(sim.simulation_pv.BhkwUeberschussGesamtKwh > 0);
            Assert.True(sim.simulation_pv.Ueberschuss.Sum() > 0);

            double[] einspeisung = sim.BhkwEinspeisungDesLaufs();
            Assert.NotNull(einspeisung);
            Assert.True(einspeisung.Sum() > 0);
        }

        /// <summary>
        /// Eine Einspeisung für alle Leser: Reiterlinie = <c>BHKW_UEBERSCHUSS</c> des Zeitreihensatzes
        /// = <see cref="SimulationPV.BhkwUeberschuss"/> je Stunde; Kennzahl = KWK-Einspeisung der
        /// Strommatrix = Summe des Excel-Monatsblocks; Eigenstrom der Matrix = Erzeugung − Einspeisung.
        /// </summary>
        [Fact]
        public void Reiter_Zeitreihensatz_Matrix_und_Kennzahl_fuehren_dieselbe_Einspeisung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var r = new SimulationRunner();
            Assert.True(r.Simuliere(PROJEKT, out string fehler), "Lauf gescheitert: " + fehler);
            SimulationControl sim = r.sim;
            ZeitreihenSatz z = ZeitreihenExtraktor.AusLauf(r);
            StromMatrix m = StromMatrix.Baue(z, new TarifParameter());
            var bh = SimulationErgebnisCtrl.Bhkw(sim, r.simulation_Waermebedarf, r.simulation_Strombedarf);
            SimulationErgebnisCtrl.BhkwStromreihen reiter = SimulationErgebnisCtrl.BhkwStromStunden(sim);

            double[] satz = z.Hole(ZeitreihenSatz.BHKW_UEBERSCHUSS);
            double[] lauf = sim.BhkwEinspeisungDesLaufs();
            Assert.NotNull(satz);
            Assert.Equal(8760, reiter.Einspeisung.Length);
            for (int h = 0; h < 8760; h++)
            {
                Assert.Equal(lauf[h], satz[h]);
                Assert.Equal(lauf[h], reiter.Einspeisung[h]);
                Assert.Equal(lauf[h], sim.simulation_pv.BhkwUeberschuss[h]);
            }

            double summeMwh = satz.Sum() / 1000.0;
            Assert.True(summeMwh > 1.0, "Einspeisung " + summeMwh + " MWh");
            Assert.Equal(summeMwh, bh.EinspeisungMwh, 9);
            Assert.Equal(summeMwh, m.KwkEinspeisungGesamtMWh, 9);
            Assert.Equal(z.Hole(ZeitreihenSatz.BHKW_STROM).Sum() / 1000.0 - summeMwh, m.KwkEigenGesamtMWh, 9);
            Assert.Equal(m.KwkEigenGesamtMWh, SimulationErgebnisCtrl.BhkwEigenverbrauchMwh(sim), 9);

            using var mappe = new ClosedXML.Excel.XLWorkbook();
            ClosedXML.Excel.IXLWorksheet ws = mappe.AddWorksheet("Probe");
            ExcelBerichtGenerator.MonatsBlock(ws, 1, z);
            int spalte = 0;
            for (int s = 1; !ws.Cell(2, s).IsEmpty(); s++)
                if (ws.Cell(2, s).GetString() == "BHKW-Einspeisung") spalte = s;
            Assert.True(spalte > 0, "Spalte BHKW-Einspeisung fehlt");
            double monate = 0;
            for (int monat = 0; monat < 12; monat++) monate += ws.Cell(3 + monat, spalte).GetDouble();
            Assert.Equal(m.KwkEinspeisungGesamtMWh, monate, 6);
        }

        /// <summary>
        /// Die Energiebilanz des Laufs schließt mit der Einspeisung: je Viertelstunde Netzbezug +
        /// PV-Eigenverbrauch + BHKW-Strom − BHKW-Einspeisung = Bedarf aller Verbraucher, über das
        /// Jahr unter 1e‑6 kWh; ebenso im Stundenraster des Zeitreihensatzes. Und der Fall ist scharf:
        /// Es gibt Stunden mit schwankendem Bedarf und Einspeisung, in denen eine Klemmung des
        /// Stundenmittels <c>max(0, B − max(0, D̄ − PV̄))</c> weniger einspeisen ließe.
        /// </summary>
        [Fact]
        public void Die_Energiebilanz_schliesst_und_der_Viertelstundenfall_ist_scharf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var r = new SimulationRunner();
            Assert.True(r.Simuliere(PROJEKT, out string fehler), "Lauf gescheitert: " + fehler);
            SimulationControl sim = r.sim;
            double[] bhkwQ = sim.Stundenwerte_zu_viertelstunden(sim.simulation_bhkw.stromproduktion);
            double[] einspQ = sim.BhkwEinspeisung_viertelstuendlich;
            double[] netzQ = sim.Rest_Strombedarf_viertelstuendlich;
            double[] pvQ = sim.simulation_pv.Stromproduktion_viertelstunde;
            double[] bedarfQ = sim.Strombedarf_Verbraucher_viertelstuendlich;

            double rest = 0, restBetrag = 0;
            for (int q = 0; q < 35040; q++)
            {
                double d = (netzQ[q] + pvQ[q] + bhkwQ[q] - einspQ[q] - bedarfQ[q]) / 4.0;   // kWh
                rest += d;
                restBetrag += Math.Abs(d);
            }
            Assert.True(Math.Abs(rest) < 1e-6, "Bilanzrest " + rest + " kWh");
            Assert.True(restBetrag < 1e-6, "Bilanzrest (Betrag) " + restBetrag + " kWh");

            ZeitreihenSatz z = ZeitreihenExtraktor.AusLauf(r);
            double stunden = 0;
            double[] netz = z.Hole(ZeitreihenSatz.NETZBEZUG), pv = z.Hole(ZeitreihenSatz.PV_GENUTZT),
                     bhkw = z.Hole(ZeitreihenSatz.BHKW_STROM), einsp = z.Hole(ZeitreihenSatz.BHKW_UEBERSCHUSS),
                     bedarf = z.Hole(ZeitreihenSatz.STROMBEDARF_GESAMT);
            for (int h = 0; h < 8760; h++) stunden += netz[h] + pv[h] + bhkw[h] - einsp[h] - bedarf[h];
            Assert.True(Math.Abs(stunden) < 1e-6, "Bilanzrest Stundenraster " + stunden + " kWh");

            // Scharf: schwankender Bedarf in der Stunde, Einspeisung > 0 und über der Klemmung des Mittels.
            int scharf = 0;
            double mehr = 0;
            for (int h = 0; h < 8760; h++)
            {
                double min = double.MaxValue, max = double.MinValue;
                for (int k = 0; k < 4; k++)
                {
                    min = Math.Min(min, bedarfQ[h * 4 + k]);
                    max = Math.Max(max, bedarfQ[h * 4 + k]);
                }
                double klemme = Math.Max(0, bhkw[h] - Math.Max(0, bedarf[h] - pv[h]));
                if (max - min > 1e-9 && einsp[h] > 0 && einsp[h] > klemme + 1e-9)
                {
                    scharf++;
                    mehr += einsp[h] - klemme;
                }
            }
            Assert.True(scharf > 0, "keine Stunde mit schwankendem Bedarf und Einspeisung über der Stundenklemmung");
            Assert.True(mehr > 1.0, "Unterschied zur Stundenklemmung nur " + mehr + " kWh");
        }
    }
}
