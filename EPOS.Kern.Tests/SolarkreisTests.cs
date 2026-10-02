using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Regeln des Solarkreises</b> (Welle M2 der Entscheidungsvorlage Modellgrenzen,
    /// <see cref="Solarkreis"/>) gegen die Handrechnung, ohne Datenbank: Verluste des Solarkreises
    /// (ST3 Stufe 1) und Pumpenstrom (ST1).
    /// </summary>
    public sealed class SolarkreisTests
    {
        // =================================================================
        // ST3 Stufe 1 - Verluste des Solarkreises
        // =================================================================

        /// <summary>Leer, nicht endlich und außerhalb 0 … 50 % gilt die Vorgabe 8 % — bitgleich das Literal 0,92.</summary>
        [Fact]
        public void Verlustfaktor_Vorgabe_ist_bitgleich_das_Literal()
        {
            Assert.Equal(BitConverter.DoubleToInt64Bits(0.92), BitConverter.DoubleToInt64Bits(Solarkreis.Verlustfaktor(null)));
            Assert.Equal(BitConverter.DoubleToInt64Bits(0.92), BitConverter.DoubleToInt64Bits(Solarkreis.Verlustfaktor(8)));
            Assert.Equal(0.92, Solarkreis.Verlustfaktor(double.NaN));
            Assert.Equal(0.92, Solarkreis.Verlustfaktor(-1));
            Assert.Equal(0.92, Solarkreis.Verlustfaktor(51));

            Assert.Equal(1.0, Solarkreis.Verlustfaktor(0));
            Assert.Equal(0.85, Solarkreis.Verlustfaktor(15), 15);
            Assert.Equal(0.5, Solarkreis.Verlustfaktor(50));
        }

        // =================================================================
        // ST1 - Pumpenstrom
        // =================================================================

        /// <summary>Pumpenleistung · 1 h in Betriebsstunden; hilfsweise der Anteil auf die genutzte Wärme.</summary>
        [Fact]
        public void Pumpenstrom_nach_Leistung_oder_Anteil()
        {
            Assert.Equal(0.06, Solarkreis.PumpenstromKwh(60, null, 3.5), 15);
            Assert.Equal(0.06, Solarkreis.PumpenstromKwh(60, 2, 3.5), 15);      // die Leistung hat Vorrang
            Assert.Equal(0.07, Solarkreis.PumpenstromKwh(null, 2, 3.5), 15);    // 2 % von 3,5 kWh
            Assert.Equal(0.07, Solarkreis.PumpenstromKwh(0, 2, 3.5), 15);       // 0 W = nicht gepflegt

            Assert.Equal(0.0, Solarkreis.PumpenstromKwh(60, null, 0));          // keine Abgabe, kein Strom
            Assert.Equal(0.0, Solarkreis.PumpenstromKwh(null, 2, 0));
            Assert.Equal(0.0, Solarkreis.PumpenstromKwh(null, null, 3.5));
            Assert.Equal(0.0, Solarkreis.PumpenstromKwh(null, 0, 3.5));

            Assert.True(Solarkreis.RechnetPumpenstrom(60, null));
            Assert.True(Solarkreis.RechnetPumpenstrom(null, 1));
            Assert.False(Solarkreis.RechnetPumpenstrom(null, null));
            Assert.False(Solarkreis.RechnetPumpenstrom(0, 0));
        }

        /// <summary>
        /// Im Modul: Strom nur in den Stunden, in denen das Feld Wärme abgibt — nicht in Stunden ohne
        /// Potenzial und nicht in Stunden, in denen alles Überschuss ist.
        /// </summary>
        [Fact]
        public void Pumpenstrom_nur_in_Betriebsstunden_des_Felds()
        {
            double[] potenzial = new double[8760];
            potenzial[10] = 5.0;   // deckt
            potenzial[12] = 3.0;   // ohne Bedarf: Überschuss, keine Abgabe
            potenzial[14] = 2.0;   // deckt

            var st = new SimulationSolarthermie();
            st.Vorbereiten_Testfelder(new List<SimulationSolarthermie.Testfeld>
            {
                new SimulationSolarthermie.Testfeld { ID_Anlage = 7, Potenzial = potenzial, PumpenleistungW = 80 }
            }, null);

            double[] rest = new double[Kanal.ANZAHL];
            for (int h = 0; h < 8760; h++)
            {
                Array.Clear(rest, 0, rest.Length);
                rest[Kanal.HEIZUNG] = h == 12 ? 0 : 10;
                st.Stunde_Start(h, rest);
                st.Stunde_Bedarf(h, rest);
                st.Stunde_Ende(h);
            }
            st.Abschluss_Zweikanalig();

            Assert.Equal(0.08, st.Pumpenstrom_stuendlich[10], 15);
            Assert.Equal(0.08, st.Pumpenstrom_stuendlich[14], 15);
            Assert.Equal(0.0, st.Pumpenstrom_stuendlich[12]);
            Assert.Equal(2, st.Pumpenstrom_stuendlich.Count(v => v > 0));
            Assert.Equal(0.16, st.PumpenstromGesamtKwh, 12);
            Assert.Equal(0.16, st.Kollektor_Ergebnisse[0].PumpenstromKwh, 12);
            Assert.Equal(3.0, st.UeberschussSummeKwh, 12);
        }

        /// <summary>Ohne Pumpenleistung und ohne Anteil bleibt die Reihe 0 — die Vorgabe rechnet wie vor der Welle.</summary>
        [Fact]
        public void Ohne_Angabe_kein_Pumpenstrom()
        {
            double[] potenzial = Enumerable.Repeat(1.0, 8760).ToArray();
            var st = new SimulationSolarthermie();
            st.Vorbereiten_Testfelder(new List<SimulationSolarthermie.Testfeld>
            {
                new SimulationSolarthermie.Testfeld { ID_Anlage = 3, Potenzial = potenzial }
            }, null);

            double[] rest = new double[Kanal.ANZAHL];
            for (int h = 0; h < 8760; h++)
            {
                rest[Kanal.HEIZUNG] = 2;
                st.Stunde_Start(h, rest);
                st.Stunde_Bedarf(h, rest);
                st.Stunde_Ende(h);
            }
            st.Abschluss_Zweikanalig();

            Assert.Equal(8760.0, st.WaermeproduktionGesamtKwh, 9);
            Assert.Equal(0.0, st.PumpenstromGesamtKwh);
            Assert.All(st.Pumpenstrom_stuendlich, v => Assert.Equal(0.0, v));
        }

        /// <summary>Der Hilfsenergieanteil als Ersatzweg: Anteil der in der Stunde genutzten Wärme.</summary>
        [Fact]
        public void Pumpenstrom_aus_Hilfsenergieanteil()
        {
            double[] potenzial = new double[8760];
            potenzial[100] = 4.0;
            var st = new SimulationSolarthermie();
            st.Vorbereiten_Testfelder(new List<SimulationSolarthermie.Testfeld>
            {
                new SimulationSolarthermie.Testfeld { ID_Anlage = 2, Potenzial = potenzial, HilfsenergieAnteil = 1.5 }
            }, null);

            double[] rest = new double[Kanal.ANZAHL];
            for (int h = 0; h < 8760; h++)
            {
                Array.Clear(rest, 0, rest.Length);
                rest[Kanal.HEIZUNG] = 3.0;   // genutzt 3 von 4 kWh
                st.Stunde_Start(h, rest);
                st.Stunde_Bedarf(h, rest);
                st.Stunde_Ende(h);
            }
            st.Abschluss_Zweikanalig();

            Assert.Equal(0.045, st.PumpenstromGesamtKwh, 12);   // 1,5 % von 3 kWh
        }
    }
}
