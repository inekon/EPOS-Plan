using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Netzverluste je Kanal</b> (Entscheidungsvorlage
    /// Modellgrenzen BW4; Konzept Simulationsablauf 17; Vorgabe bei <see cref="Netzverlustvorgabe"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Regeln der Vorgabe ohne Datenbank; im Lauf auf einer Kopie des
    /// Referenzprojekts 1041 (Heizung, Brauchwasser und Prozesswärme): leer = Projektwert mit
    /// anteiliger Verteilung bitgleich; gesetzt = je Kanal sein Wert, der Projektwert gilt nicht. Die
    /// Zirkulation prüft <see cref="ZirkulationBestandswegTests"/>.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class NetzverlusteJeKanalTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1041;

        // =============================================================================
        //  Regeln ohne Datenbank
        // =============================================================================

        [Fact]
        public void Die_Vorgabe_rechnet_je_Kanal_und_normalisiert()
        {
            Assert.False(Netzverlustvorgabe.Leer.JeKanal);
            Assert.False(Netzverlustvorgabe.Leer.MitZirkulation);
            Assert.Equal(0.0, Netzverlustvorgabe.Leer.ZirkulationJahresKwh);

            var v = new Netzverlustvorgabe
            {
                HeizungWert = 10, HeizungEinheit = "%",
                BrauchwasserWert = 8760, BrauchwasserEinheit = "kWh/a",
                ZirkulationLeistungKw = 2, ZirkulationLaufzeitHd = 10
            };
            Assert.True(v.JeKanal);
            Assert.Equal(87600 * 0.1 / 8760, v.BetragJeStunde(Kanal.HEIZUNG, 87600), 12);
            Assert.Equal(1.0, v.BetragJeStunde(Kanal.BRAUCHWASSER, 123456), 12);
            Assert.Equal(0.0, v.BetragJeStunde(Kanal.PROZESS, 5000));
            Assert.True(v.MitZirkulation);
            Assert.Equal(2 * 10 * 365, v.ZirkulationJahresKwh, 9);

            // Normalisiert: Wert ohne Einheit -> %, Einheit ohne Wert -> leer, NaN -> leer.
            var n = new Netzverlustvorgabe { ProzessWert = 3, BrauchwasserEinheit = "kWh/a", HeizungWert = double.NaN }.Normalisiert();
            Assert.Equal("%", n.ProzessEinheit);
            Assert.Null(n.BrauchwasserEinheit);
            Assert.Null(n.HeizungWert);
            Assert.Equal(Netzverlustvorgabe.Leer, new Netzverlustvorgabe().Normalisiert());

            // Prüfung mit denselben Grenzen wie die Spalten.
            Assert.Null(v.Pruefen());
            Assert.NotNull(new Netzverlustvorgabe { ProzessWert = 101, ProzessEinheit = "%" }.Pruefen());
            Assert.Null(new Netzverlustvorgabe { ProzessWert = 101, ProzessEinheit = "kWh/a" }.Pruefen());
            Assert.NotNull(new Netzverlustvorgabe { HeizungWert = -1, HeizungEinheit = "kWh/a" }.Pruefen());
            Assert.NotNull(new Netzverlustvorgabe { ZirkulationLeistungKw = 101 }.Pruefen());
            Assert.NotNull(new Netzverlustvorgabe { ZirkulationLaufzeitHd = 25 }.Pruefen());

            // Ohne Laufzeit oder ohne Leistung keine Zirkulation.
            Assert.False(new Netzverlustvorgabe { ZirkulationLeistungKw = 2 }.MitZirkulation);
            Assert.False(new Netzverlustvorgabe { ZirkulationLeistungKw = 0, ZirkulationLaufzeitHd = 5 }.MitZirkulation);
        }

        // =============================================================================
        //  Im Lauf
        // =============================================================================

        /// <summary>Leer geschrieben rechnet der Lauf bitgleich zum Projektwert ohne Spalten.</summary>
        [Fact]
        public void Leer_ist_der_Projektwert_bitgleich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationWaermebedarf a = Lauf(PROJEKT, 10, "%");
            Assert.True(KonfigurationCtrl.NetzverlustvorgabeSetzen(PROJEKT, Netzverlustvorgabe.Leer));
            Assert.Equal(Netzverlustvorgabe.Leer, KonfigurationCtrl.NetzverlustvorgabeLesen(PROJEKT));
            SimulationWaermebedarf b = Lauf(PROJEKT, 10, "%");

            Assert.True(a.Waermebedarf_Netzverluste > 0);
            Assert.Equal(a.Waermebedarf_Netzverluste, b.Waermebedarf_Netzverluste);
            ByteGleich(a.Waermebedarf, b.Waermebedarf, "Summenvektor");
            ByteGleich(a.brauchwasserwerte, b.brauchwasserwerte, "Brauchwasserkanal");
            ByteGleich(a.prozesswerte, b.prozesswerte, "Prozesskanal");
            Assert.Equal(0.0, b.Brauchwasser_Zirkulation_Mwh);
        }

        /// <summary>
        /// Gesetzt gilt je Kanal sein Wert: Heizung 10 % seines Jahresbedarfs, Brauchwasser 500 kWh/a,
        /// Prozess leer = 0. Der Projektwert (hier 20 %) gilt nicht, und nichts wird verteilt.
        /// </summary>
        [Fact]
        public void Gesetzt_gilt_je_Kanal_sein_Wert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationWaermebedarf ohne = Lauf(PROJEKT, 0, "%");
            double heiz = ohne.Waermebedarf_Heizkanal_Stunde.Sum();
            double bw = ohne.brauchwasserwerte.Sum();
            double pw = ohne.prozesswerte.Sum();
            Assert.True(heiz > 0 && bw > 0 && pw > 0, "Projekt 1041 trägt alle drei Kanäle.");

            Assert.True(KonfigurationCtrl.NetzverlustvorgabeSetzen(PROJEKT, new Netzverlustvorgabe
            {
                HeizungWert = 10, HeizungEinheit = "%",
                BrauchwasserWert = 500, BrauchwasserEinheit = "kWh/a"
            }));
            SimulationWaermebedarf mit = Lauf(PROJEKT, 20, "%");

            double erwartet = heiz * 0.10 + 500;
            Assert.True(Math.Abs(mit.Waermebedarf_Netzverluste - erwartet / 1000) < 1e-9,
                        mit.Waermebedarf_Netzverluste + " gegen " + erwartet / 1000);
            Assert.True(Math.Abs(mit.brauchwasserwerte.Sum() - (bw + 500)) < 1e-6);
            ByteGleich(ohne.prozesswerte, mit.prozesswerte, "Prozesskanal ohne Aufschlag");
            Assert.True(Math.Abs(mit.Waermebedarf.Sum() - (ohne.Waermebedarf.Sum() + erwartet)) < 1e-6);
            // Je Stunde fester Betrag auf dem Brauchwasserkanal.
            for (int h = 0; h < 8760; h += 997)
                Assert.True(Math.Abs(mit.brauchwasserwerte[h] - ohne.brauchwasserwerte[h] - 500.0 / 8760) < 1e-9);
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
