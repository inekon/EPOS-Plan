using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>E26 — PV-Ausweis und Strommatrix-Bedarf</b> (Befunde N1 und N3 aus E25), auf einer
    /// Arbeitskopie der Testdatenbank an drei Wärmepumpenprojekten mit Photovoltaik: 1040
    /// (ohne Speicher), 1026 (mit Stromspeicher) und 1042 (Anlage ohne Ertrag).
    ///
    /// <para><b>N1:</b> <c>Ergebnis.Photovoltaik.Stromproduktion</c> ist die Erzeugung der
    /// Module (<c>Stromproduktion_Theoretisch</c>, gleich der Summe der Modulzeilen), nicht
    /// der Direktverbrauch. Der Ausweis „PV: vermiedener Bezug" rechnet Erzeugung −
    /// Einspeisung und wird damit nie negativ.</para>
    ///
    /// <para><b>N3:</b> Die <see cref="StromMatrix"/> misst am Bedarf ALLER Verbraucher des
    /// Anschlusses (<see cref="ZeitreihenSatz.STROMBEDARF_GESAMT"/>): Projektbedarf plus
    /// Wärmepumpe, Heizstab, Elektrokessel und Kältestrom der Stufenrechnung. Der Netzbezug
    /// ist davon der Rest; die vermiedene Menge ist PV-Eigennutzung plus Speicherentladung
    /// (plus KWK-Eigenstrom) und nie negativ.</para>
    ///
    /// <para><b>Kapitalwert unverändert:</b> Anker aus dem Stand vor E26 (868afc57), gerechnet
    /// mit Sammler und <see cref="WirtschaftlichkeitCtrl"/> wie im Bericht.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class PvAusweisStromMatrixTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const double EPS = 1e-9;

        private static double Summe(double[] reihe) => reihe == null ? 0.0 : reihe.Sum();

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

        // =====================================================================
        //  N1 — die Stromproduktion ist die Erzeugung
        // =====================================================================

        [Theory]
        [InlineData(1040)]
        [InlineData(1026)]
        [InlineData(1042)]
        public void Die_Stromproduktion_ist_die_Erzeugung_der_Module(int idProjekt)
        {
            if (!_db.Vorhanden) return;
            Lauf l = Rechne(idProjekt);
            Assert.True(l.Sim.bSimulationPV);
            ErgebnisPhotovoltaikModel pv = l.Ergebnis.Photovoltaik;
            Assert.NotNull(pv);

            double erzeugung = Summe(l.Sim.simulation_pv.Stromproduktion_Theoretisch) / 1000.0;
            double genutzt = Summe(l.Reihen.Hole(ZeitreihenSatz.PV_GENUTZT)) / 1000.0;
            double direktUeberschuss = Summe(l.Sim.simulation_pv.Ueberschuss) / 1000.0;

            Assert.Equal(erzeugung, pv.Stromproduktion, 9);
            Assert.Equal(pv.Module.Sum(m => m.Stromproduktion), pv.Stromproduktion, 9);
            // Erzeugung = Direktverbrauch + Überschuss vor Speicherladung, Stunde für Stunde.
            Assert.Equal(genutzt + direktUeberschuss, pv.Stromproduktion, 9);

            // Der Eigenverbrauch des Ausweises (Erzeugung − Einspeisung) ist nie negativ und
            // nie kleiner als der Direktverbrauch; die Differenz ist die PV-Ladung des Speichers.
            double eigen = pv.Stromproduktion - pv.Ueberschuss;
            Assert.True(eigen >= -EPS, "Eigenverbrauch negativ: " + eigen);
            Assert.True(eigen >= genutzt - EPS, "Eigenverbrauch unter dem Direktverbrauch.");
            if (!l.Sim.bSimulationSSP) Assert.Equal(genutzt, eigen, 9);

            // Die Kennzahlen lesen dieselbe Größe.
            var v = new VariantenDaten { IdProjekt = idProjekt, Ergebnis = l.Ergebnis };
            KennzahlenKatalog.Berechne(v);
            Assert.Equal(erzeugung, v.Kennzahlen["energie.pv_strom"].Value, 9);
            if (erzeugung > 0)
            {
                double quote = v.Kennzahlen["eff.pv_eigen"].Value;
                Assert.InRange(quote, 0.0, 100.0);
            }
        }

        /// <summary>Anker an 1040 (VDI-Weg bis Stufe GA, 20 Module ohne Speicher) und
        /// 1026 (Stromspeicher): die Mengen des Messprotokolls E26.</summary>
        [Fact]
        public void Anker_der_PV_Mengen()
        {
            if (!_db.Vorhanden) return;
            ErgebnisPhotovoltaikModel a = Rechne(1040).Ergebnis.Photovoltaik;
            Assert.Equal(6.713, a.Stromproduktion, 3);     // vor E26 4,441 (der Direktverbrauch)
            Assert.Equal(2.273, a.Ueberschuss, 3);
            Assert.Equal(4.441, a.Stromproduktion - a.Ueberschuss, 3);   // vor E26 2,168

            ErgebnisPhotovoltaikModel b = Rechne(1026).Ergebnis.Photovoltaik;
            Assert.Equal(6.713, b.Stromproduktion, 3);     // vor E26 4,197
            Assert.Equal(1.246, b.Ueberschuss, 3);
            Assert.Equal(5.467, b.Stromproduktion - b.Ueberschuss, 3);   // vor E26 2,950
        }

        // =====================================================================
        //  N3 — die Strommatrix misst am Bedarf aller Verbraucher
        // =====================================================================

        [Theory]
        [InlineData(1040)]
        [InlineData(1026)]
        [InlineData(1042)]
        public void Die_Strommatrix_misst_am_Bedarf_aller_Verbraucher(int idProjekt)
        {
            if (!_db.Vorhanden) return;
            Lauf l = Rechne(idProjekt);
            SimulationControl sim = l.Sim;
            ZeitreihenSatz z = l.Reihen;

            double[] gesamt = z.Hole(ZeitreihenSatz.STROMBEDARF_GESAMT);
            Assert.NotNull(gesamt);
            Assert.Equal(ZeitreihenSatz.Stunden, gesamt.Length);

            // Die Summe der Verbraucherreihen …
            double verbraucher = Summe(z.Hole(ZeitreihenSatz.STROMBEDARF))
                               + Summe(z.Hole(ZeitreihenSatz.WP_STROM))
                               + Summe(z.Hole(ZeitreihenSatz.HEIZSTAB))
                               + (sim.bSimulationKessel ? Summe(sim.simulation_spk.Stromverbrauch_stuendlich) : 0.0)
                               + Summe(sim.Kaeltestrom_Stufenrechnung_stuendlich);
            Assert.Equal(verbraucher / 1000.0, Summe(gesamt) / 1000.0, 6);

            // … und Stunde für Stunde der Bedarf, den die Photovoltaik sieht (ohne BHKW gleich).
            double[] bhkw = z.Hole(ZeitreihenSatz.BHKW_STROM);
            double[] pvEingang = sim.simulation_pv.Strombedarf_stuendlich;
            double abw = 0;
            for (int h = 0; h < ZeitreihenSatz.Stunden; h++)
                abw = Math.Max(abw, Math.Abs(gesamt[h] - (bhkw != null ? bhkw[h] : 0.0) - pvEingang[h]));
            Assert.True(abw < 1e-6, "Bedarf aller Verbraucher weicht vom PV-Eingangsbedarf ab: " + abw);

            StromMatrix m = StromMatrix.Baue(z, new TarifParameter());
            Assert.NotNull(m);
            Assert.False(m.StrombedarfFehlt);
            Assert.Equal(Summe(gesamt) / 1000.0, m.BedarfGesamtMWh, 9);
            Assert.True(m.BedarfGesamtMWh >= m.BezugGesamtMWh - EPS, "Bedarf unter dem Netzbezug.");
            Assert.Equal(Summe(z.Hole(ZeitreihenSatz.PV_GENUTZT)) / 1000.0, m.PvEigenGesamtMWh, 9);

            // Vermiedene Menge = PV-Eigennutzung + Speicherentladung (kein BHKW in diesen Projekten).
            double entladung = sim.Speicherergebnis != null ? sim.Speicherergebnis.EntladeenergieKwh / 1000.0 : 0.0;
            Assert.Equal(m.PvEigenGesamtMWh + entladung, m.BedarfGesamtMWh - m.BezugGesamtMWh, 6);
        }

        /// <summary>Anker der vermiedenen Menge im Rollentarif (Differenzmethode, 0,30 €/kWh
        /// für Bezug und Reststrom): vor E26 negativ in jedem Wärmepumpenprojekt.</summary>
        [Fact]
        public void Anker_der_vermiedenen_Menge()
        {
            if (!_db.Vorhanden) return;
            var erwartet = new Dictionary<int, (double bedarf, double menge)>
            {
                [1040] = (27.427, 4.441),     // vor E26 8,000 / −14,986
                [1026] = (31.351, 5.348),     // vor E26 8,000 / −18,004
                [1042] = (41.345, 0.000)      // vor E26 8,000 / −33,345
            };
            var rolle = new TarifRolle
            {
                ArbeitspreisEurKWh = 0.30,
                Leistungsmodell = DbWerte.LEISTUNGSMODELL_MONATLICH
            };
            foreach (KeyValuePair<int, (double bedarf, double menge)> p in erwartet)
            {
                StromMatrix m = StromMatrix.Baue(Rechne(p.Key).Reihen, new TarifParameter());
                Assert.Equal(p.Value.bedarf, m.BedarfGesamtMWh, 3);
                StromErloesErgebnis r = StromTarifRechner.Rechne(new StromErloesEingabe
                {
                    BedarfMWh = m.BedarfGesamtMWh,
                    RestbezugMWh = m.BezugGesamtMWh,
                    EinspeisungMWh = m.EinspeisungPvGesamtMWh + m.KwkEinspeisungGesamtMWh,
                    LastBedarf = m.LastBedarf,
                    LastRestbezug = m.LastBezug
                }, rolle, rolle, null, BerichtTexte.Kultur);
                Assert.Equal(p.Value.menge, r.VermiedenMengeMWh, 3);
                Assert.True(r.VermiedenGesamtEur >= -EPS, "Vermiedene Kosten negativ in " + p.Key);
            }
        }

        // =====================================================================
        //  Kapitalwert unverändert
        // =====================================================================

        /// <summary>
        /// Die Kapitalwerte je Szenario sind die des Stands vor E26 (868afc57, gleiche
        /// Testdatenbank, bitgleich gemessen): 1024 (BHKW, Wärmepumpe, Heizstab und
        /// Elektrokessel — der KWK-Split misst jetzt am Bedarf aller Verbraucher) und 1030
        /// (BHKW mit KWKG-Zuschlag). Die PV-Projekte 1040/1026/1042 haben in der
        /// Testdatenbank keinen Strompreis und damit keinen Kapitalwert.
        /// </summary>
        [Theory]
        [MemberData(nameof(Kapitalwertanker))]
        public void Der_Kapitalwert_bleibt_unveraendert(int idProjekt, string szenario, double erwartet)
        {
            if (!_db.Vorhanden) return;
            BerichtsDatenSammler.VariantenStatus stamm =
                BerichtsDatenSammler.ErmittleStatus(idProjekt, "").First(s => s.IstStamm);
            BerichtsDaten daten = new BerichtsDatenSammler().Sammle(idProjekt, stamm.Projektname,
                new List<int>(), true, true, null, CancellationToken.None);
            var ctrl = new WirtschaftlichkeitCtrl();
            List<WirtschaftlichkeitErgebnis> e = ctrl.Berechne(daten, ctrl.LadeParameter(idProjekt));
            WirtschaftlichkeitErgebnis r = e.Single(x => x.IdProjekt == idProjekt && x.Szenario == szenario);
            Assert.True(r.Kapitalwert.HasValue);
            Assert.Equal(erwartet, r.Kapitalwert.Value, 6);
            if (idProjekt == 1030) Assert.Equal(KWKG_1030, r.KwkgErloesJahr1, 6);
        }

        public static IEnumerable<object[]> Kapitalwertanker()
        {
            yield return new object[] { 1024, WirtschaftlichkeitSzenario.ERWARTET, -2772642.2674731365 };
            yield return new object[] { 1024, WirtschaftlichkeitSzenario.BEST, -2801567.756181355 };
            yield return new object[] { 1030, WirtschaftlichkeitSzenario.ERWARTET, -31141242.708693754 };
        }

        /// <summary>KWKG-Zuschlag Jahr 1 von 1030 vor E26 [€] — der KWK-Split trägt ihn.</summary>
        private const double KWKG_1030 = 7322.633879278648;
    }
}
