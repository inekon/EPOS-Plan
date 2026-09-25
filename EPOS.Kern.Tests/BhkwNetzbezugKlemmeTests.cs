using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>E27 — der Netzbezug ist nie negativ</b> (Befund N5 aus E26). Die Kaskade zieht die
    /// BHKW-Erzeugung ungeklemmt ab, damit spätere Verbraucher derselben Viertelstunde und die
    /// Photovoltaik den Überschuss sehen. Folgt keine klemmende Stufe, setzt der Lauf am Ende
    /// jeden Wert unter 0 auf 0 (<see cref="SimulationControl.NetzbezugGeklemmt"/>); der
    /// Überschuss steht allein im KWK-Split als Einspeisung. Der Reststrombedarf der BHKW-Zeile
    /// ist je Stunde geklemmt (<see cref="SimulationControl.BhkwReststrombedarfMwh"/>,
    /// Entscheid E27‑Q4).
    ///
    /// <para>Anker auf der Arbeitskopie der Testdatenbank: 1018 (BHKW ohne Strombedarf, ganzjährig
    /// Überschuss) und 1030 (zwölf Überschussstunden). Vor E27: 1018 Netzbezug −27,4575 MWh,
    /// Reststromkosten im Rollentarif 0,30 €/kWh −8.237,25 €/a; 1030 Netzbezug 4.357,7808 MWh.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BhkwNetzbezugKlemmeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private sealed class Lauf
        {
            public SimulationRunner Runner;
            public ErgebnisModel Ergebnis;
            public ZeitreihenSatz Reihen;
            public SimulationControl Sim => Runner.sim;
        }

        private static Lauf Rechne(int idProjekt)
        {
            var r = new SimulationRunner();
            Assert.True(r.Simuliere(idProjekt, out string fehler), "Lauf gescheitert: " + fehler);
            return new Lauf
            {
                Runner = r,
                Ergebnis = SimulationRunner.BaueErgebnis(idProjekt, r.simulation_Waermebedarf,
                                                          r.simulation_Strombedarf, r.sim),
                Reihen = ZeitreihenExtraktor.AusLauf(r)
            };
        }

        private static StromErloesErgebnis Rollentarif(StromMatrix m)
        {
            var rolle = new TarifRolle
            {
                ArbeitspreisEurKWh = 0.30,
                Leistungsmodell = DbWerte.LEISTUNGSMODELL_MONATLICH
            };
            return StromTarifRechner.Rechne(new StromErloesEingabe
            {
                BedarfMWh = m.BedarfGesamtMWh,
                RestbezugMWh = m.BezugGesamtMWh,
                EinspeisungMWh = m.EinspeisungPvGesamtMWh + m.KwkEinspeisungGesamtMWh,
                LastBedarf = m.LastBedarf,
                LastRestbezug = m.LastBezug
            }, rolle, rolle, null, BerichtTexte.Kultur);
        }

        // =====================================================================
        //  Die Klemme selbst
        // =====================================================================

        [Fact]
        public void Ohne_negativen_Wert_bleibt_der_Rest_dasselbe_Array()
        {
            double[] rest = { 0.0, -0.0, 1.5, 1e-300 };
            double[] ergebnis = SimulationControl.NetzbezugGeklemmt(rest);
            Assert.Same(rest, ergebnis);
            Assert.True(double.IsNegative(ergebnis[1]), "−0,0 bleibt bitgleich");
        }

        [Fact]
        public void Negative_Werte_werden_0_und_die_Eingabe_bleibt_unberuehrt()
        {
            double[] rest = { -2.0, 3.0, -1e-12, 0.25 };
            double[] ergebnis = SimulationControl.NetzbezugGeklemmt(rest);
            Assert.NotSame(rest, ergebnis);
            Assert.Equal(new[] { 0.0, 3.0, 0.0, 0.25 }, ergebnis);
            Assert.Equal(-2.0, rest[0]);
        }

        [Fact]
        public void Der_BHKW_Rest_klemmt_je_Stunde_und_bleibt_ohne_Ueberschuss_die_Jahresdifferenz()
        {
            double[] bedarf = { 10.0, 2.0, 5.0 };
            double[] strom = { 4.0, 6.0, 5.0 };
            Assert.Equal(0.006, SimulationControl.BhkwReststrombedarfMwh(bedarf, strom, 0.015), 12);

            double[] bedarf2 = { 10.0, 7.0 };
            double[] strom2 = { 4.0, 6.0 };
            Assert.Equal(bedarf2.Sum() / 1000.0 - 0.010,
                         SimulationControl.BhkwReststrombedarfMwh(bedarf2, strom2, 0.010));
        }

        // =====================================================================
        //  1018: BHKW ohne Strombedarf
        // =====================================================================

        [Fact]
        public void Projekt_1018_hat_Netzbezug_0_und_die_Einspeisung_im_KWK_Split()
        {
            if (!_db.Vorhanden) return;
            Lauf l = Rechne(1018);

            Assert.Equal(0.0, l.Sim.ReststromMwh);
            Assert.True(l.Sim.Rest_Strombedarf_viertelstuendlich.All(x => x >= 0), "negativer Netzbezug");
            Assert.Equal(0.0, l.Ergebnis.Energiebedarf.Stromrestbedarf);
            Assert.Equal(0.0, l.Ergebnis.BHKW.Reststrombedarf);
            Assert.Equal(27.4575, l.Ergebnis.BHKW.Stromproduktion, 4);

            StromMatrix m = StromMatrix.Baue(l.Reihen, new TarifParameter());
            Assert.Equal(0.0, m.BezugGesamtMWh);
            Assert.Equal(0.0, m.KwkEigenGesamtMWh);
            Assert.Equal(27.4575, m.KwkEinspeisungGesamtMWh, 4);

            // Rollentarif 0,30 €/kWh: vor E27 −8.237,25 €/a Reststromkosten (Gutschrift).
            StromErloesErgebnis r = Rollentarif(m);
            Assert.Equal(0.0, r.Reststrom.SummeEur);
            Assert.Equal(0.0, r.VermiedenMengeMWh);
        }

        [Fact]
        public void Projekt_1018_verliert_die_CO2_Gutschrift_des_negativen_Netzbezugs()
        {
            if (!_db.Vorhanden) return;
            BerichtsDaten daten = Sammle(1018);
            VariantenDaten v = daten.Varianten.Single(x => x.IdProjekt == 1018);
            // E27: vor E27 13,0557 t/a — der negative Netzbezug (−27,46 MWh × 435 g/kWh
            // = −11,9451 t/a) stand als Gutschrift darin.
            Assert.Equal(25.0008, v.CO2Gesamt.Value, 4);
        }

        // =====================================================================
        //  1030: zwölf Überschussstunden
        // =====================================================================

        [Fact]
        public void Projekt_1030_bezieht_die_Ueberschussstunden_nicht_mehr_als_Gutschrift()
        {
            if (!_db.Vorhanden) return;
            Lauf l = Rechne(1030);

            Assert.Equal(4358.1728, l.Sim.ReststromMwh, 4);          // vor E27 4.357,7808
            Assert.Equal(4358.1728, l.Ergebnis.BHKW.Reststrombedarf, 4);
            Assert.True(l.Sim.Rest_Strombedarf_viertelstuendlich.All(x => x >= 0), "negativer Netzbezug");

            StromMatrix m = StromMatrix.Baue(l.Reihen, new TarifParameter());
            Assert.Equal(4358.1728, m.BezugGesamtMWh, 4);
            Assert.Equal(0.392, m.KwkEinspeisungGesamtMWh, 6);       // unverändert
            Assert.Equal(431.9132, m.KwkEigenGesamtMWh, 4);          // unverändert
        }

        // =====================================================================
        //  Photovoltaik nach BHKW: die Klemme hat nichts zu tun
        // =====================================================================

        /// <summary>
        /// 1018 bekommt auf der Arbeitskopie die PV-Anlage von 1040 (Anlagenzeile, Modulzeile,
        /// <c>Tool_5</c>). Die PV-Stufe liest den negativen Rest als BHKW-Überschuss und klemmt
        /// selbst; am Laufende steht kein Wert unter 0 mehr — die Klemme gibt dasselbe Array
        /// zurück, der Pfad bleibt byte-gleich (Reihen-Hashes vor und nach E27 gemessen gleich).
        /// Die Modulzeile des BHKW war vor E27 auch hier −27,46.
        /// </summary>
        [Fact]
        public void Photovoltaik_nach_BHKW_bleibt_byte_gleich()
        {
            if (!_db.Vorhanden) return;
            Kopiere("Tab_Energieanlagen", "ID = 14742");
            Kopiere("Tab_PV", "ID_Projekt = 1040");
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Einstellungen SET Tool_5 = 'Photovoltaik' WHERE ID_Projekt = 1018");

            Lauf l = Rechne(1018);
            Assert.True(l.Sim.bSimulationPV);
            double[] rest = l.Sim.Rest_Strombedarf_viertelstuendlich;
            Assert.Same(rest, SimulationControl.NetzbezugGeklemmt(rest));
            Assert.True(rest.All(x => x == 0.0 && !double.IsNegative(x)));
            Assert.Equal(0.0, l.Sim.ReststromMwh);
            Assert.Equal(27457.510347756746, l.Sim.simulation_pv.BhkwUeberschussGesamtKwh, 6);
            Assert.Equal(0.0, l.Ergebnis.BHKW.Reststrombedarf);

            StromMatrix m = StromMatrix.Baue(l.Reihen, new TarifParameter());
            Assert.Equal(27.4575, m.KwkEinspeisungGesamtMWh, 4);
            Assert.Equal(6.5997, m.EinspeisungPvGesamtMWh, 4);
        }

        private static void Kopiere(string tabelle, string bedingung)
        {
            List<string> spalten = DataRepository.SpaltenVonTabelle(tabelle)
                .Where(s => !string.Equals(s, "ID", StringComparison.OrdinalIgnoreCase)).ToList();
            string ziel = string.Join(", ", spalten.Select(s => "[" + s + "]"));
            string quelle = string.Join(", ", spalten.Select(s =>
                string.Equals(s, "ID_Projekt", StringComparison.OrdinalIgnoreCase) ? "1018" : "[" + s + "]"));
            Assert.True(DataRepository.ExecuteSQL("INSERT INTO " + tabelle + " (" + ziel + ") SELECT " +
                                                  quelle + " FROM " + tabelle + " WHERE " + bedingung));
        }

        private static BerichtsDaten Sammle(int idProjekt)
        {
            BerichtsDatenSammler.VariantenStatus stamm =
                BerichtsDatenSammler.ErmittleStatus(idProjekt, "").First(s => s.IstStamm);
            return new BerichtsDatenSammler().Sammle(idProjekt, stamm.Projektname,
                new List<int>(), true, true, null, CancellationToken.None);
        }
    }
}
