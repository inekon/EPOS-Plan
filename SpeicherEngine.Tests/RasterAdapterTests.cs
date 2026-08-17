using System;
using System.Globalization;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Adaptertest des Arbeitspakets AP2 (Umsetzungskonzept 4, Fachkonzept 3.3):
    /// <see cref="RasterAdapter.ZuViertelstundenDouble"/> muss wertgleich zur
    /// Wertwiederholung des Bestands sein, der Rueckweg
    /// <see cref="RasterAdapter.ZuFloat"/> chartverlustfrei.
    /// </summary>
    public sealed class RasterAdapterTests
    {
        /// <summary>Fester Seed - der Test muss bei jedem Lauf dieselben Reihen pruefen.</summary>
        private const int Seed = 20260816;

        /// <summary>
        /// <b>Referenzimplementierung des Bestands</b>, zeichengetreu uebernommen aus
        /// <c>SimulationControl.Stundenwerte_zu_viertelstunden</c> (identisch in
        /// <c>SimulationPV</c> und <c>SimulationStrombedarf</c>). Das Testprojekt
        /// referenziert das Hauptprojekt bewusst nicht (COM-Referenzen, MSB4803),
        /// deshalb steht die Vergleichsfassung hier.
        /// </summary>
        private static float[] BestandsExpansion(float[] stundenwerte)
        {
            float[] viertelstundenwerte = new float[stundenwerte.Length * 4];
            for (int i = 0; i < stundenwerte.Length; i++)
            {
                viertelstundenwerte[i * 4] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 1] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 2] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 3] = stundenwerte[i];
            }
            return viertelstundenwerte;
        }

        /// <summary>
        /// Synthetische Stundenreihe: gemischte Groessenordnungen und Vorzeichen,
        /// dazu die haeufigen Sonderfaelle 0 und exakte Zweierpotenzen.
        /// </summary>
        private static float[] SyntheticheStundenreihe(int n = RasterAdapter.StundenJahr)
        {
            var zufall = new Random(Seed);
            float[] reihe = new float[n];
            for (int i = 0; i < n; i++)
            {
                switch (i % 7)
                {
                    case 0: reihe[i] = 0f; break;
                    case 1: reihe[i] = (float)(zufall.NextDouble() * 1000.0); break;
                    case 2: reihe[i] = (float)(zufall.NextDouble() * 1e-6); break;
                    case 3: reihe[i] = (float)(-zufall.NextDouble() * 500.0); break;
                    case 4: reihe[i] = 0.25f; break;
                    case 5: reihe[i] = (float)(zufall.NextDouble() * 1e6); break;
                    default: reihe[i] = (float)zufall.NextDouble(); break;
                }
            }
            return reihe;
        }

        // ------------------------------------------------------------ Expansion

        /// <summary>
        /// 8.760 -&gt; 35.040 ist exakt die Wertwiederholung des Bestands - Wert fuer
        /// Wert bitgleich, nicht nur naeherungsweise.
        /// </summary>
        [Fact]
        public void ZuViertelstundenDouble_Ist_Wertwiederholung_Wie_Im_Bestand()
        {
            float[] stunden = SyntheticheStundenreihe();
            float[] soll = BestandsExpansion(stunden);
            double[] ist = RasterAdapter.ZuViertelstundenDouble(stunden);

            Assert.Equal(RasterAdapter.ViertelstundenJahr, ist.Length);
            Assert.Equal(soll.Length, ist.Length);

            for (int i = 0; i < ist.Length; i++)
            {
                Assert.True(V7ReferenzTests.Bitgleich(ist[i], soll[i]),
                    "Intervall " + i + ": Adapter = " + V7ReferenzTests.Bits(ist[i]) +
                    ", Bestand = " + V7ReferenzTests.Bits(soll[i]));
            }
        }

        /// <summary>Jede Stunde liegt als vier gleiche Viertelstundenwerte vor.</summary>
        [Fact]
        public void ZuViertelstundenDouble_Legt_Jeden_Stundenwert_Auf_Vier_Intervalle()
        {
            float[] stunden = SyntheticheStundenreihe();
            double[] ist = RasterAdapter.ZuViertelstundenDouble(stunden);

            for (int i = 0; i < stunden.Length; i++)
            {
                double w = stunden[i];
                for (int j = 0; j < 4; j++)
                    Assert.True(V7ReferenzTests.Bitgleich(w, ist[i * 4 + j]),
                        "Stunde " + i + ", Viertel " + j);
            }
        }

        /// <summary>Eine bereits viertelstuendliche Reihe wird 1:1 uebernommen.</summary>
        [Fact]
        public void ZuViertelstundenDouble_Uebernimmt_Viertelstundenreihe_Eins_Zu_Eins()
        {
            float[] viertel = SyntheticheStundenreihe(RasterAdapter.ViertelstundenJahr);
            double[] ist = RasterAdapter.ZuViertelstundenDouble(viertel);

            Assert.Equal(RasterAdapter.ViertelstundenJahr, ist.Length);
            for (int i = 0; i < ist.Length; i++)
                Assert.True(V7ReferenzTests.Bitgleich(ist[i], viertel[i]), "Intervall " + i);
        }

        /// <summary>
        /// Jede andere Laenge wird abgewiesen - auch das Schaltjahr, das erst mit der
        /// Importerweiterung (AP5) dazukommt.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(24)]
        [InlineData(8759)]
        [InlineData(8761)]
        [InlineData(8784)]     // Schaltjahr, stuendlich
        [InlineData(35039)]
        [InlineData(35041)]
        [InlineData(35136)]    // Schaltjahr, viertelstuendlich
        public void ZuViertelstundenDouble_Lehnt_Andere_Laengen_Ab(int laenge)
        {
            Assert.Throws<ArgumentException>(() => RasterAdapter.ZuViertelstundenDouble(new float[laenge]));
        }

        /// <summary><c>null</c> ist kein gueltiger Eingang.</summary>
        [Fact]
        public void Adapter_Weist_Null_Ab()
        {
            Assert.Throws<ArgumentNullException>(() => RasterAdapter.ZuViertelstundenDouble(null!));
            Assert.Throws<ArgumentNullException>(() => RasterAdapter.ZuDouble(null!));
            Assert.Throws<ArgumentNullException>(() => RasterAdapter.ZuFloat(null!));
            Assert.Throws<ArgumentNullException>(() => RasterAdapter.Addiere(null!, new double[1]));
            Assert.Throws<ArgumentNullException>(() => RasterAdapter.Addiere(new double[1], null!));
        }

        // ------------------------------------------------------------- Roundtrip

        /// <summary>
        /// Der Rueckweg ist fuer Werte, die aus <c>float</c> stammen, verlustfrei:
        /// <c>ZuFloat(ZuDouble(x)) == x</c> bitgleich.
        /// </summary>
        [Fact]
        public void Roundtrip_Float_Double_Float_Erhaelt_Jeden_Wert()
        {
            float[] original = SyntheticheStundenreihe();
            float[] zurueck = RasterAdapter.ZuFloat(RasterAdapter.ZuDouble(original));

            Assert.Equal(original.Length, zurueck.Length);
            for (int i = 0; i < original.Length; i++)
                Assert.True(BitConverter.SingleToInt32Bits(original[i]) ==
                            BitConverter.SingleToInt32Bits(zurueck[i]),
                    "Intervall " + i + ": " + zurueck[i].ToString("R", CultureInfo.InvariantCulture) +
                    " statt " + original[i].ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Auch ueber die Expansion hinweg bleibt der Chartweg verlustfrei:
        /// <c>ZuFloat(ZuViertelstundenDouble(x))</c> ist bitgleich zur
        /// Bestandsexpansion.
        /// </summary>
        [Fact]
        public void Roundtrip_Ueber_Expansion_Trifft_Bestandsreihe()
        {
            float[] stunden = SyntheticheStundenreihe();
            float[] soll = BestandsExpansion(stunden);
            float[] ist = RasterAdapter.ZuFloat(RasterAdapter.ZuViertelstundenDouble(stunden));

            Assert.Equal(soll.Length, ist.Length);
            for (int i = 0; i < soll.Length; i++)
                Assert.True(BitConverter.SingleToInt32Bits(soll[i]) ==
                            BitConverter.SingleToInt32Bits(ist[i]), "Intervall " + i);
        }

        /// <summary>Der Adapter kopiert - er reicht keine Referenz auf den Eingang durch.</summary>
        [Fact]
        public void Adapter_Kopiert_Statt_Zu_Verweisen()
        {
            float[] stunden = new float[RasterAdapter.StundenJahr];
            stunden[0] = 5f;

            double[] viertel = RasterAdapter.ZuViertelstundenDouble(stunden);
            stunden[0] = 99f;

            Assert.Equal(5.0, viertel[0]);
            Assert.Equal(5.0, viertel[3]);
        }

        // --------------------------------------------------------------- Addiere

        /// <summary>Der Lastpfad summiert mehrere Reihen - elementweise, in-place.</summary>
        [Fact]
        public void Addiere_Summiert_Elementweise()
        {
            double[] ziel = { 1.0, 2.0, 3.0 };
            RasterAdapter.Addiere(ziel, new double[] { 0.5, -2.0, 0.0 });

            Assert.Equal(new double[] { 1.5, 0.0, 3.0 }, ziel);
            Assert.Throws<ArgumentException>(() => RasterAdapter.Addiere(new double[3], new double[2]));
        }
    }
}
