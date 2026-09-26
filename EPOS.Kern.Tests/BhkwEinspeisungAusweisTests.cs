using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>E29 (#536): Anzeige-Welle Stromausweis.</b>
    /// <list type="bullet">
    /// <item>E27‑Q3 b: die BHKW-Einspeisung als Diagnosereihe (<c>BHKW_UEBERSCHUSS</c>, auch ohne
    /// PV) und als Zeile im BHKW-Reiter — die Stundenformel des KWK-Splits
    /// (<see cref="SimulationControl.BhkwEinspeisungStuendlich"/>, Entscheide E29‑Q1…Q4 a).</item>
    /// <item>E26‑Q6: die Linie der Strombilanz und die Excel-Spalte „Strombedarf" messen am
    /// Strombedarf aller Verbraucher (<c>STROMBEDARF_GESAMT</c>), mit Rückfall (E29‑Q7/Q8 a);
    /// die BHKW-Einspeisung als Excel-Spalte auch ohne Flotte (E29‑Q6 a).</item>
    /// <item>N6: der Kältestrom der Stufenrechnung im Nenner der Übersicht (E29‑Q9 a).</item>
    /// <item>Restpunkt E28 (a): der PV-Deckungsgrad teilt durch den je Stunde geklemmten Bedarf
    /// (E29‑Q10 a).</item>
    /// </list>
    /// Kein Kernwert der Wirtschaftlichkeit ändert sich; die Strommatrix bleibt unberührt.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BhkwEinspeisungAusweisTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        // =====================================================================
        //  Die Stundenformel (ohne Datenbank)
        // =====================================================================

        /// <summary>
        /// Eine Stunde: BHKW-Strom, Bedarf aller Verbraucher und PV-Eigenverbrauch [kWh] →
        /// Einspeisung. Der PV-Eigenverbrauch geht vor (10, 4, 3 → 9); nimmt die PV den ganzen
        /// Bedarf, speist das BHKW alles ein (10, 4, 6 → 10).
        /// </summary>
        [Theory]
        [InlineData(10.0, 4.0, 0.0, 6.0)]
        [InlineData(10.0, 4.0, 3.0, 9.0)]
        [InlineData(10.0, 15.0, 0.0, 0.0)]
        [InlineData(0.0, 5.0, 0.0, 0.0)]
        [InlineData(10.0, 4.0, 6.0, 10.0)]
        public void Die_Einspeisung_ist_der_Strom_den_die_Verbraucher_nach_der_PV_nicht_abnehmen(
            double bhkw, double bedarf, double pv, double erwartet)
        {
            double[] b = new double[8760], d = new double[8760], p = new double[8760];
            b[17] = bhkw; d[17] = bedarf; p[17] = pv;

            double[] e = SimulationControl.BhkwEinspeisungStuendlich(b, d, p);

            Assert.Equal(8760, e.Length);
            Assert.Equal(erwartet, e[17]);
            Assert.Equal(erwartet, e.Sum());
        }

        /// <summary>Ohne Bedarfswert ist die Stunde Eigenstrom — wie im KWK-Split.</summary>
        [Fact]
        public void Stunden_ohne_Bedarfswert_zaehlen_als_Eigenstrom()
        {
            double[] b = Enumerable.Repeat(3.0, 8760).ToArray();
            double[] e = SimulationControl.BhkwEinspeisungStuendlich(b, new[] { 1.0 }, null);
            Assert.Equal(2.0, e[0]);
            Assert.Equal(0.0, e[1]);
            Assert.Equal(0.0, e[8759]);
            Assert.Null(SimulationControl.BhkwEinspeisungStuendlich(null, new double[8760], null));
        }

        /// <summary>Über ein ganzes Zufallsjahr dieselbe Menge wie der KWK-Split der Strommatrix.</summary>
        [Fact]
        public void Die_Jahressumme_ist_die_KWK_Einspeisung_der_Strommatrix()
        {
            var zufall = new Random(536);
            double[] bhkw = new double[8760], bedarf = new double[8760], pv = new double[8760], netz = new double[8760];
            for (int h = 0; h < 8760; h++)
            {
                bhkw[h] = zufall.NextDouble() * 20.0;
                bedarf[h] = zufall.NextDouble() * 25.0;
                pv[h] = zufall.NextDouble() < 0.5 ? 0.0 : zufall.NextDouble() * 10.0;
                netz[h] = Math.Max(0, bedarf[h] - pv[h] - bhkw[h]);
            }
            var z = new ZeitreihenSatz();
            z.Reihen[ZeitreihenSatz.BHKW_STROM] = bhkw;
            z.Reihen[ZeitreihenSatz.STROMBEDARF_GESAMT] = bedarf;
            z.Reihen[ZeitreihenSatz.PV_GENUTZT] = pv;
            z.Reihen[ZeitreihenSatz.NETZBEZUG] = netz;

            StromMatrix m = StromMatrix.Baue(z, new TarifParameter());
            double[] e = SimulationControl.BhkwEinspeisungStuendlich(bhkw, bedarf, pv);

            Assert.True(m.KwkEinspeisungGesamtMWh > 1.0);
            Assert.Equal(m.KwkEinspeisungGesamtMWh, e.Sum() / 1000.0, 9);
        }

        // =====================================================================
        //  Die BHKW-Projekte der Testdatenbank
        // =====================================================================

        /// <summary>
        /// Reiterzeile = Diagnosereihe = KWK-Split des Laufs: 1018 speist die ganze Erzeugung
        /// ein (kein Strombedarf), 1030 zwölf Stunden, 1017/1024/1047 nichts — dort fehlt die
        /// Reihe (Schwelle 0,5 kWh).
        /// </summary>
        [Theory]
        [InlineData(1018, 27.4575)]
        [InlineData(1030, 0.392)]
        [InlineData(1017, 0.0)]
        [InlineData(1024, 0.0)]
        [InlineData(1047, 0.0)]
        public void Reiter_Reihe_und_KWK_Split_fuehren_dieselbe_Einspeisung(int idProjekt, double erwartetMwh)
        {
            if (!_db.Vorhanden) return;

            var r = new SimulationRunner();
            Assert.True(r.Simuliere(idProjekt, out string fehler), "Lauf gescheitert: " + fehler);
            ZeitreihenSatz z = ZeitreihenExtraktor.AusLauf(r);
            StromMatrix m = StromMatrix.Baue(z, new TarifParameter());
            var bh = SimulationErgebnisCtrl.Bhkw(r.sim, r.simulation_Waermebedarf, r.simulation_Strombedarf);

            Assert.Equal(erwartetMwh, Math.Round(bh.EinspeisungMwh, 4), 3);
            Assert.Equal(m.KwkEinspeisungGesamtMWh, bh.EinspeisungMwh, 9);

            double[] reihe = z.Hole(ZeitreihenSatz.BHKW_UEBERSCHUSS);
            if (erwartetMwh > 0)
            {
                Assert.NotNull(reihe);
                Assert.Equal(bh.EinspeisungMwh, reihe.Sum() / 1000.0, 9);
            }
            else
            {
                Assert.Null(reihe);
            }
        }

        /// <summary>
        /// 1018 mit der PV-Anlage von 1040 (Muster E28): Die Reihe kommt dann aus
        /// <c>SimulationPV.BhkwUeberschuss</c> (Zweig unverändert) — dieselbe Größe wie die neue
        /// Stundenformel, Stunde für Stunde; der Reiter zeigt 27,46 MWh.
        /// </summary>
        [Fact]
        public void Mit_PV_ist_die_Stundenformel_der_BHKW_Ueberschuss_der_PV()
        {
            if (!_db.Vorhanden) return;
            Kopiere("Tab_Energieanlagen", "ID = 14742");
            Kopiere("Tab_PV", "ID_Projekt = 1040");
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Einstellungen SET Tool_5 = 'Photovoltaik' WHERE ID_Projekt = 1018");

            var r = new SimulationRunner();
            Assert.True(r.Simuliere(1018, out string fehler), "Lauf gescheitert: " + fehler);
            Assert.True(r.sim.bSimulationPV);

            double[] neu = r.sim.BhkwEinspeisungDesLaufs();
            double[] pv = r.sim.simulation_pv.BhkwUeberschuss;
            double abw = 0;
            for (int h = 0; h < 8760; h++) abw = Math.Max(abw, Math.Abs(neu[h] - pv[h]));
            Assert.True(abw < 1e-9, "Stundenformel weicht vom BHKW-Überschuss der PV ab: " + abw);

            var bh = SimulationErgebnisCtrl.Bhkw(r.sim, r.simulation_Waermebedarf, r.simulation_Strombedarf);
            Assert.Equal(27.4575, bh.EinspeisungMwh, 4);

            // E29‑Q10 a: der PV-Deckungsgrad bleibt ≤ 100 % und ist in Ergebnis und Ansicht gleich.
            ErgebnisModel e = SimulationRunner.BaueErgebnis(1018, r.simulation_Waermebedarf,
                                                            r.simulation_Strombedarf, r.sim);
            Assert.InRange(e.Photovoltaik.Strombedarfsdeckung, 0.0, 100.0);
            Assert.Equal(e.Photovoltaik.Strombedarfsdeckung,
                         SimulationErgebnisCtrl.Photovoltaik(r.sim).DeckungProzent, 9);
        }

        /// <summary>E29‑Q10 a an 1040 (keine negative Stunde): der Deckungsgrad bitgleich zur alten Formel.</summary>
        [Fact]
        public void Der_PV_Deckungsgrad_ohne_BHKW_Ueberschuss_bleibt_bitgleich()
        {
            if (!_db.Vorhanden) return;

            var r = new SimulationRunner();
            Assert.True(r.Simuliere(1040, out string fehler), "Lauf gescheitert: " + fehler);
            SimulationPV pvs = r.sim.simulation_pv;
            double alt = pvs.Stromproduktion.Sum() * 100.0 / pvs.Strombedarf_stuendlich.Sum();

            ErgebnisModel e = SimulationRunner.BaueErgebnis(1040, r.simulation_Waermebedarf,
                                                            r.simulation_Strombedarf, r.sim);
            Assert.Equal(BitConverter.DoubleToInt64Bits(alt),
                         BitConverter.DoubleToInt64Bits(e.Photovoltaik.Strombedarfsdeckung));
        }

        /// <summary>Die Klemme des Nenners: negative Stunden zählen 0, ohne sie dasselbe Array.</summary>
        [Fact]
        public void Der_Nenner_des_PV_Deckungsgrads_klemmt_je_Stunde()
        {
            double[] bedarf = { 4.0, -3.0, 2.0 };
            Assert.Equal(6.0, SimulationControl.NetzbezugGeklemmt(bedarf).Sum());   // vorher 3 → Deckung verdoppelt
            double[] ohne = { 4.0, 0.0, 2.0 };
            Assert.Same(ohne, SimulationControl.NetzbezugGeklemmt(ohne));
        }

        // =====================================================================
        //  Strombilanz und Excel-Monatsblock
        // =====================================================================

        private static double[] Konstant(double wert) => Enumerable.Repeat(wert, 8760).ToArray();

        private static ZeitreihenSatz Strombilanzsatz(bool mitGesamt)
        {
            var z = new ZeitreihenSatz();
            z.Reihen[ZeitreihenSatz.STROMBEDARF] = Konstant(1.0);
            if (mitGesamt) z.Reihen[ZeitreihenSatz.STROMBEDARF_GESAMT] = Konstant(3.0);
            z.Reihen[ZeitreihenSatz.BHKW_STROM] = Konstant(1.5);
            z.Reihen[ZeitreihenSatz.NETZBEZUG] = Konstant(1.5);
            return z;
        }

        /// <summary>
        /// Die Linie der Strombilanz ist der Gesamtbedarf: dasselbe Bild wie ein Satz, dessen
        /// Projektbedarf die Gesamtwerte trägt — und ein anderes als der Projektbedarf allein.
        /// Ohne Gesamtreihe bleibt das Bild (Rückfall; ChartProben unverändert).
        /// </summary>
        [Fact]
        public void Die_Strombilanz_zeichnet_den_Gesamtbedarf_als_Linie()
        {
            byte[] mitGesamt = SkiaMaler.Png(ChartRenderer.StrombilanzMonateModell(Strombilanzsatz(true)));
            ZeitreihenSatz ersatz = Strombilanzsatz(false);
            ersatz.Reihen[ZeitreihenSatz.STROMBEDARF] = Konstant(3.0);
            byte[] erwartet = SkiaMaler.Png(ChartRenderer.StrombilanzMonateModell(ersatz));
            byte[] projekt = SkiaMaler.Png(ChartRenderer.StrombilanzMonateModell(Strombilanzsatz(false)));

            Assert.True(mitGesamt.SequenceEqual(erwartet));
            Assert.False(mitGesamt.SequenceEqual(projekt));
        }

        /// <summary>
        /// Excel: „Strombedarf" trägt den Gesamtbedarf (Januar 744 h × 3 kWh = 2,232 MWh), ohne
        /// Gesamtreihe den Projektbedarf; die Spalte „BHKW-Einspeisung" steht ohne Flotte, sobald
        /// die Reihe da ist.
        /// </summary>
        [Fact]
        public void Der_Excel_Monatsblock_fuehrt_Gesamtbedarf_und_BHKW_Einspeisung()
        {
            ZeitreihenSatz z = Strombilanzsatz(true);
            z.Reihen[ZeitreihenSatz.BHKW_UEBERSCHUSS] = Konstant(0.5);
            (List<string> kopf, Dictionary<string, double> januar) = Monatsblock(z);
            Assert.Equal(new[] { "Monat", "Strombedarf", "BHKW-Strom", "BHKW-Einspeisung", "Netzbezug" }, kopf);
            Assert.Equal(2.232, januar["Strombedarf"], 9);
            Assert.Equal(0.372, januar["BHKW-Einspeisung"], 9);

            (List<string> kopfAlt, Dictionary<string, double> januarAlt) = Monatsblock(Strombilanzsatz(false));
            Assert.Equal(new[] { "Monat", "Strombedarf", "BHKW-Strom", "Netzbezug" }, kopfAlt);
            Assert.Equal(0.744, januarAlt["Strombedarf"], 9);
        }

        private static (List<string>, Dictionary<string, double>) Monatsblock(ZeitreihenSatz z)
        {
            using var mappe = new XLWorkbook();
            IXLWorksheet ws = mappe.AddWorksheet("Probe");
            ExcelBerichtGenerator.MonatsBlock(ws, 1, z);
            var kopf = new List<string>();
            var januar = new Dictionary<string, double>();
            for (int s = 1; !ws.Cell(2, s).IsEmpty(); s++)
            {
                string name = ws.Cell(2, s).GetString();
                kopf.Add(name);
                if (s > 1) januar[name] = ws.Cell(3, s).GetDouble();
            }
            return (kopf, januar);
        }

        // =====================================================================
        //  N9: der Kessel im Stromgang der Ergebnisansicht
        // =====================================================================

        /// <summary>
        /// Der Stromgang (Bild, Summenlinie, CSV) stapelt als „Heizkessel" den Stromverbrauch
        /// des Kessels — 1017: 20,12 MWh, nicht den Strom-Stufeneingang 635,2 MWh; 1030: 0 statt
        /// 4.790,09 MWh.
        /// </summary>
        [Theory]
        [InlineData(1017, 20.12, 635.2)]
        [InlineData(1030, 0.0, 4790.09)]
        public void Der_Stromgang_fuehrt_den_Stromverbrauch_des_Kessels(int idProjekt, double verbrauchMwh,
                                                                       double stufeneingangMwh)
        {
            if (!_db.Vorhanden) return;
            var r = new SimulationRunner();
            Assert.True(r.Simuliere(idProjekt, out string fehler), "Lauf gescheitert: " + fehler);

            double[] reihe = SimulationErgebnisHuelle.StromverbrauchKessel(r.sim);
            Assert.Same(r.sim.simulation_spk.Stromverbrauch_stuendlich, reihe);
            Assert.Equal(verbrauchMwh, Math.Round(reihe.Sum() / 1000.0, 2));
            Assert.Equal(stufeneingangMwh, Math.Round(r.sim.simulation_spk.Strombedarf_stuendlich.Sum() / 1000.0, 2));
        }

        // =====================================================================
        //  N6: der Kältestrom im Nenner der Übersicht (1045 mit Kühlung, Muster E34)
        // =====================================================================

        /// <summary>1045: Luft-Wasser-Wärmepumpe (Anlage 14924, Gerät 1672044), VDI-Gebäude 10651.</summary>
        private const int KUEHLPROJEKT = 1045, KUEHL_WP = 1672044, KUEHL_ANLAGE = 14924,
                          KUEHL_GEBAEUDE = 10651, KUEHLTRAEGER = 58;

        /// <summary>
        /// Mit Kühlung ist der Nenner des Strom-Rings der Bedarf aller Verbraucher: Projekt,
        /// Wärmepumpe, Heizstab, Kessel UND der Kältestrom der Stufenrechnung — dieselbe Menge
        /// wie <c>Strombedarf_Verbraucher</c>. Führt die Anlage einen eigenen Zähler (E34), läuft
        /// ihr Kältestrom neben der Stufenrechnung und bleibt draußen.
        /// </summary>
        [Fact]
        public void Der_Nenner_der_Uebersicht_fuehrt_den_Kaeltestrom_der_Stufenrechnung()
        {
            if (!_db.Vorhanden) return;
            KuehlungEinrichten();

            var r = new SimulationRunner();
            Assert.True(r.Simuliere(KUEHLPROJEKT, out string fehler), "Lauf gescheitert: " + fehler);
            var u = SimulationErgebnisCtrl.Uebersicht(r.sim, r.simulation_Waermebedarf, r.simulation_Strombedarf);

            double kaelte = r.sim.Kaeltestrom_Stufenrechnung_stuendlich.Sum() / 1000.0;
            Assert.True(kaelte > 0, "Vorbedingung: der Lauf rechnet Kältestrom in der Stufenrechnung");
            Assert.Equal(kaelte, u.KaeltestromStufeMwh, 9);
            Assert.Equal(u.StrombedarfGesamtMwh + u.WpStromverbrauchMwh + u.HeizstabStromverbrauchMwh
                         + u.KesselStromverbrauchMwh + u.KaeltestromStufeMwh,
                         u.StrombedarfMitEigenverbrauchMwh);
            Assert.Equal(r.sim.Strombedarf_Verbraucher_viertelstuendlich.Sum() / 4000.0,
                         u.StrombedarfMitEigenverbrauchMwh, 3);

            // Eigener Zähler: der Kältestrom verlässt die Stufenrechnung und den Nenner.
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(KUEHL_ANLAGE, KUEHLPROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlIdCarrier: KUEHLTRAEGER, KuehlEigenerZaehler: true)).Ok);
            var r2 = new SimulationRunner();
            Assert.True(r2.Simuliere(KUEHLPROJEKT, out fehler), "Lauf gescheitert: " + fehler);
            var u2 = SimulationErgebnisCtrl.Uebersicht(r2.sim, r2.simulation_Waermebedarf, r2.simulation_Strombedarf);
            Assert.Equal(0.0, u2.KaeltestromStufeMwh);
            Assert.True(r2.simulation_Kaeltebedarf.Kaskade.StromGesamtKwh > 0);
        }

        /// <summary>Ohne Kälte (1040) ist der Nenner bitgleich die Summe der vier bisherigen Glieder.</summary>
        [Fact]
        public void Ohne_Kaelte_bleibt_der_Nenner_der_Uebersicht_bitgleich()
        {
            if (!_db.Vorhanden) return;
            var r = new SimulationRunner();
            Assert.True(r.Simuliere(1040, out string fehler), "Lauf gescheitert: " + fehler);
            var u = SimulationErgebnisCtrl.Uebersicht(r.sim, r.simulation_Waermebedarf, r.simulation_Strombedarf);
            Assert.Equal(0.0, u.KaeltestromStufeMwh);
            double alt = u.StrombedarfGesamtMwh + u.WpStromverbrauchMwh + u.HeizstabStromverbrauchMwh
                       + u.KesselStromverbrauchMwh;
            Assert.Equal(BitConverter.DoubleToInt64Bits(alt),
                         BitConverter.DoubleToInt64Bits(u.StrombedarfMitEigenverbrauchMwh));
        }

        /// <summary>1045 mit Kühlung und reversibler Wärmepumpe — der Ausschnitt aus
        /// <c>KaeltestromAbrechnungTests.Einrichten</c> ohne Preise und Träger.</summary>
        private static void KuehlungEinrichten()
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Kuehlung_Aktiv = 1, Kuehl_Sollwert = 24 WHERE ID = ?",
                new DbParam("@id", KUEHL_GEBAEUDE)));
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(KUEHLPROJEKT, true));
            foreach (var z in new[] { (18, 20, 5.5, 15.0), (18, 30, 4.5, 14.0), (18, 40, 3.5, 13.0) })
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_Kenndaten_Kuehlung (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) " +
                    "VALUES ((SELECT COALESCE(MAX(ID), 0) + 1 FROM Tab_Kenndaten_Kuehlung), ?, ?, ?, ?, ?, 100)",
                    new DbParam("@wp", KUEHL_WP), new DbParam("@v", z.Item1), new DbParam("@t", z.Item2),
                    new DbParam("@e", z.Item3), new DbParam("@p", z.Item4)));
            WPCtrl.SpeicherErgebnis e = WPCtrl.KuehlkonfigurationSchreiben(KUEHL_WP, KUEHLPROJEKT, true, 18, 0.05);
            Assert.True(e.Ok, e.Meldung);
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

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
    }
}
