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

        // =================================================================
        // ST6 - Bezugsfläche der Kennwerte
        // =================================================================

        /// <summary>Apertur ist die Vorgabe; Brutto nimmt die Modulfläche, ohne sie die Apertur mit Rückfall.</summary>
        [Fact]
        public void Bezugsflaeche_waehlt_die_rechnende_Flaeche()
        {
            Assert.Equal("apertur", Solarkreis.Bezugsflaeche(null));
            Assert.Equal("apertur", Solarkreis.Bezugsflaeche(""));
            Assert.Equal("apertur", Solarkreis.Bezugsflaeche("absorber"));
            Assert.Equal("brutto", Solarkreis.Bezugsflaeche(" Brutto "));

            bool rueckfall;
            Assert.Equal(2.35, Solarkreis.Modulbezugsflaeche("apertur", 2.35, 2.51, out rueckfall));
            Assert.False(rueckfall);
            Assert.Equal(2.35, Solarkreis.Modulbezugsflaeche(null, 2.35, 2.51, out rueckfall));
            Assert.False(rueckfall);
            Assert.Equal(2.51, Solarkreis.Modulbezugsflaeche("brutto", 2.35, 2.51, out rueckfall));
            Assert.False(rueckfall);
            Assert.Equal(2.35, Solarkreis.Modulbezugsflaeche("brutto", 2.35, 0, out rueckfall));
            Assert.True(rueckfall);
        }

        /// <summary>
        /// Der Import nach VDI 3805 Blatt 19: Ist die Bezugsfläche (Feld 11) die Bruttofläche (Feld 25),
        /// ist der Bezug brutto; die Aperturfläche, eine Absorberfläche und fehlende Angaben bleiben apertur.
        /// </summary>
        [Fact]
        public void Der_Import_erkennt_die_Bezugsflaeche()
        {
            Assert.Equal("apertur", Solarkollektorenlmport.BezugBestimmen(2.35, 2.51, 2.35));
            Assert.Equal("brutto", Solarkollektorenlmport.BezugBestimmen(2.51, 2.51, 2.35));
            Assert.Equal("apertur", Solarkollektorenlmport.BezugBestimmen(4.6, 5.16, 4.64));   // Absorberfläche
            Assert.Equal("apertur", Solarkollektorenlmport.BezugBestimmen(2.0, 2.0, 2.0));     // nicht zu unterscheiden
            Assert.Equal("apertur", Solarkollektorenlmport.BezugBestimmen(0, 2.51, 2.35));
            Assert.Equal("apertur", Solarkollektorenlmport.BezugBestimmen(2.35, 0, 0));
        }

        // =================================================================
        // ST5 - Diffus-IAM
        // =================================================================

        /// <summary>K_b(θ) ist die Korrektur des Bestands: CalculateThermalPower ohne Verluste ist η₀·K_b·G.</summary>
        [Fact]
        public void IamDirekt_ist_die_Korrektur_des_Bestands()
        {
            var st = new SimulationSolarthermie();
            foreach (double cos in new[] { 1.0, 0.9, 0.64278760968653936, 0.5, 0.2, 0.0005 })
                foreach (double kdir in new[] { 0.91, 0.98, 1.27 })
                {
                    double q = st.CalculateThermalPower(800, 20, 20, cos, 0.8, 0, 0, kdir);
                    Assert.Equal(q, 800 * (0.8 * Solarkreis.IamDirekt(cos, kdir)), 10);
                }
            Assert.Equal(1.0, Solarkreis.IamDirekt(1.0, 0.91));
            Assert.Equal(0.91, Solarkreis.IamDirekt(Math.Cos(50.0 * Math.PI / 180.0), 0.91), 12);
        }

        /// <summary>Gegen die Handrechnung: getrennte Korrektur mit K_dfu = 0,9 bei θ = 60°.</summary>
        [Fact]
        public void Leistung_mit_Kdfu_gegen_Handrechnung()
        {
            double b0 = (1 - 0.91) / (1 / Math.Cos(50 * Math.PI / 180) - 1);
            double kb = 1 - b0 * (1 / 0.5 - 1);
            double erwartet = 0.737 * (kb * 600 + 0.9 * 200) - 3.69 * 40 - 0.012 * 40 * 40;

            double q = Solarkreis.LeistungJeQm(600, 200, 20, 60, 0.5, 0.737, 3.69, 0.012, 0.91, 0.9);
            Assert.Equal(erwartet, q, 10);
            Assert.InRange(q, 336.44, 336.45);

            // Ohne K_dfu bekommt die Diffusstrahlung K_b(θ) - die Rechnung des Bestands.
            var st = new SimulationSolarthermie();
            double ohne = Solarkreis.LeistungJeQm(600, 200, 20, 60, 0.5, 0.737, 3.69, 0.012, 0.91, 0);
            Assert.Equal(st.CalculateThermalPower(800, 20, 60, 0.5, 0.737, 3.69, 0.012, 0.91), ohne, 10);

            // Bei tiefer Sonne (K_b klein) bekommt die Diffusstrahlung mit K_dfu MEHR, bei hoher Sonne
            // (K_b = 1) weniger als zuvor.
            Assert.True(q > ohne);
            Assert.True(Solarkreis.LeistungJeQm(600, 200, 20, 60, 1.0, 0.737, 3.69, 0.012, 0.91, 0.9) <
                        Solarkreis.LeistungJeQm(600, 200, 20, 60, 1.0, 0.737, 3.69, 0.012, 0.91, 0));

            Assert.Equal(0.0, Solarkreis.LeistungJeQm(0, 0, 20, 60, 0.5, 0.737, 3.69, 0.012, 0.91, 0.9));
            Assert.Equal(0.0, Solarkreis.LeistungJeQm(10, 5, -10, 80, 0.5, 0.737, 3.69, 0.012, 0.91, 0.9));
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
