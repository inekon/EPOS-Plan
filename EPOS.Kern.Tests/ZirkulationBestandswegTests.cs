using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Zirkulation im Bestandsweg</b> (Entscheidungsvorlage Modellgrenzen BW4; Konzept
    /// Simulationsablauf 17; <see cref="SimulationWaermebedarf.BestandswegZirkulation"/>).
    ///
    /// <para><b>Geprüft wird</b> auf Kopien der Testdatenbank: der Jahresposten Leistung × Laufzeit ×
    /// 365 als eigene Teilreihe des Brauchwasserkanals (Referenzprojekt 1041), getrennt ausgewiesen,
    /// nicht in der Vorschau mit Namensliste, und auf dem Generatorweg (Referenzprojekt 1045) ohne
    /// Wirkung — dort bleibt dessen Zirkulation.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZirkulationBestandswegTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1041;
        private const int GENERATORPROJEKT = 1045;

        /// <summary>
        /// Die Zirkulation im Bestandsweg: Jahresposten Leistung × Laufzeit × 365, als eigene Teilreihe
        /// im Brauchwasserkanal, je Tag genau t_Lauf Stunden mit der Leistung, Monatsschichten
        /// Zapfung + Zirkulation = Kanal. Die Vorschau mit Namensliste rechnet sie nicht.
        /// </summary>
        [Fact]
        public void Die_Zirkulation_im_Bestandsweg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationWaermebedarf ohne = Lauf(PROJEKT, 0, "%");
            Assert.True(KonfigurationCtrl.NetzverlustvorgabeSetzen(PROJEKT, new Netzverlustvorgabe
            {
                ZirkulationLeistungKw = 2, ZirkulationLaufzeitHd = 10
            }));
            SimulationWaermebedarf mit = Lauf(PROJEKT, 0, "%");

            double jahr = 2.0 * 10 * 365;
            Assert.True(Math.Abs(mit.Brauchwasser_Zirkulation_Mwh - jahr / 1000) < 1e-9);
            Assert.True(Math.Abs(mit.brauchwasserwerte.Sum() - (ohne.brauchwasserwerte.Sum() + jahr)) < 1e-6);
            Assert.True(Math.Abs(mit.Waermebedarf_Brauchwasser - (ohne.Waermebedarf_Brauchwasser + jahr / 1000)) < 1e-9);

            // Je Tag zehn Stunden mit 2 kW, zusammenhängend.
            for (int d = 0; d < 365; d += 50)
            {
                int stunden = 0;
                for (int s = 0; s < 24; s++)
                {
                    double zirk = mit.brauchwasserwerte[d * 24 + s] - ohne.brauchwasserwerte[d * 24 + s];
                    if (zirk > 1e-9) { stunden++; Assert.True(Math.Abs(zirk - 2.0) < 1e-9); }
                }
                Assert.Equal(10, stunden);
            }

            for (int m = 0; m < 12; m++)
            {
                Assert.True(mit.Waermebedarf_Brauchwasser_Zirkulation_Monat[m] > 0);
                Assert.True(Math.Abs(mit.Waermebedarf_Brauchwasser_Zapfung_Monat[m] - ohne.Waermebedarf_Brauchwasser_Monat[m]) < 1e-9);
                Assert.True(Math.Abs(mit.Waermebedarf_Brauchwasser_Zapfung_Monat[m] + mit.Waermebedarf_Brauchwasser_Zirkulation_Monat[m]
                                     - mit.Waermebedarf_Brauchwasser_Monat[m]) < 1e-9);
            }

            // Die Vorschau mit Namensliste rechnet keine Zirkulation.
            var namen = Z_ProjektBrauchwasserCtrl.LiesProjekt(PROJEKT).Select(z => z.szBezeichner).ToList();
            var vorschau = new SimulationWaermebedarf { m_ID_Projekt = PROJEKT };
            vorschau.ZapfprofilKalenderLesen(Klimaregion(PROJEKT));
            vorschau.Brauchwasserwaerme_berechnen(namen);
            Assert.Equal(0.0, vorschau.Brauchwasser_Zirkulation_Mwh);
        }

        /// <summary>Auf dem Generatorweg bleibt dessen Zirkulation; die Projekteinstellung wirkt dort nicht.</summary>
        [Fact]
        public void Auf_dem_Generatorweg_bleibt_dessen_Zirkulation()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.Equal(BrauchwasserWeg.Generator, ZapfprofilCtrl.Weg(GENERATORPROJEKT));

            SimulationWaermebedarf ohne = Lauf(GENERATORPROJEKT, 0, "%");
            Assert.True(KonfigurationCtrl.NetzverlustvorgabeSetzen(GENERATORPROJEKT, new Netzverlustvorgabe
            {
                ZirkulationLeistungKw = 50, ZirkulationLaufzeitHd = 24
            }));
            SimulationWaermebedarf mit = Lauf(GENERATORPROJEKT, 0, "%");

            Assert.NotNull(mit.Zapfprofil);
            Assert.Equal(ohne.Brauchwasser_Zirkulation_Mwh, mit.Brauchwasser_Zirkulation_Mwh);
            ByteGleich(ohne.brauchwasserwerte, mit.brauchwasserwerte, "Brauchwasserkanal");
        }

        // -----------------------------------------------------------------------------

        private static SimulationWaermebedarf Lauf(int idProjekt, int netzverluste, string einheit)
        {
            var sim = new SimulationWaermebedarf { Netzverluste = netzverluste, Netzverluste_Einheit = einheit };
            sim.Waermebedarf_berechnen(idProjekt, Klimaregion(idProjekt));
            Assert.Equal("", sim.Fehlertext);
            return sim;
        }

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }

        private static void ByteGleich(double[] a, double[] b, string was)
        {
            Assert.Equal(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
                Assert.True(BitConverter.DoubleToInt64Bits(a[i]) == BitConverter.DoubleToInt64Bits(b[i]), was + ", Index " + i);
        }
    }
}
