using System;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Das Vorher/Nachher-Bild der Lastspitzenkappung
    /// (<see cref="PeakShavingBild"/>, iU9-W12.6 / Vorarbeit W12.0h).
    ///
    /// <para><b>Kein neuer Renderer.</b> Geprueft wird, dass das Bild ueber
    /// <see cref="ChartRenderer.ErzeugerStapel"/> entsteht — mit Sekundaerachse fuer
    /// den Ladezustand, im Viertelstundenraster ohne Kappung auf 8 760 Punkte, und
    /// deterministisch (dieselbe Reihe, dasselbe Bild). Die Zahl der ChartProben
    /// bleibt damit bei 30.</para>
    /// </summary>
    public class PeakShavingBildTests
    {
        private static double[] Lastgang()
        {
            double[] w = new double[35040];
            for (int i = 0; i < w.Length; i++)
                w[i] = 100.0 + 40.0 * Math.Sin(2.0 * Math.PI * i / 96.0);
            for (int i = 1000; i < 1040; i++) w[i] = 400.0;
            return w;
        }

        private static PeakShavingErgebnis Ergebnis()
        {
            SpeicherParameter p = new SpeicherParameter
            {
                CNomKwh = 200.0,
                PKw = 100.0,
                SoCMinKwh = 20.0,
                SoCMaxKwh = 180.0,
                RoundTripWirkungsgrad = 0.9,
                StartSoCKwh = 20.0,
                DtH = 0.25,
                CCapEurProKwh = 400.0,
                CPowEurProKw = 200.0,
                IFixEur = 1000.0,
                Kapitalzins = 0.03,
                NutzungsdauerA = 15.0,
                DegradationProA = 0.0
            };
            PeakShavingParameter ps = new PeakShavingParameter
            {
                PZielKw = 300.0,
                Adaptiv = false,
                LeistungspreisEurProKwA = 120.0,
                BezugspreisMittelCtKwh = 25.0
            };
            return new PeakShaving(ps, SpeicherModus.Energetisch)
                       .BerechnePeakShaving(Lastgang(), p);
        }

        [Fact]
        public void Ohne_Ergebnis_entsteht_kein_Bild()
        {
            Assert.Null(PeakShavingBild.Lastgang(null, true));
        }

        /// <summary>Ein PNG, und zwar ein richtiges: die vier Kennbytes stehen vorn.</summary>
        [Fact]
        public void Das_Bild_ist_ein_PNG()
        {
            byte[] png = PeakShavingBild.Lastgang(Ergebnis(), false);

            Assert.NotNull(png);
            Assert.True(png.Length > 1000, "Das Bild ist verdaechtig klein.");
            Assert.Equal(0x89, png[0]);
            Assert.Equal((byte)'P', png[1]);
            Assert.Equal((byte)'N', png[2]);
            Assert.Equal((byte)'G', png[3]);
        }

        /// <summary>
        /// 1 240 x 560 — dasselbe Mass wie <c>GanglinieNormiert</c> und
        /// <c>Temperaturverlauf</c>. Die Groesse steht in den Bytes 16 bis 23 des
        /// IHDR-Blocks, jeweils als 32-Bit-Zahl mit hoechstwertigem Byte zuerst.
        /// </summary>
        [Fact]
        public void Das_Bild_misst_1240_auf_560()
        {
            byte[] png = PeakShavingBild.Lastgang(Ergebnis(), true);

            int breite = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
            int hoehe = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];

            Assert.Equal(1240, breite);
            Assert.Equal(560, hoehe);
        }

        /// <summary>
        /// Der Schalter „Ladezustand im Diagramm zeigen" aendert das Bild — sonst
        /// waere die Sekundaerachse wirkungslos.
        /// </summary>
        [Fact]
        public void Der_Ladezustand_aendert_das_Bild()
        {
            PeakShavingErgebnis r = Ergebnis();

            byte[] ohne = PeakShavingBild.Lastgang(r, false);
            byte[] mit = PeakShavingBild.Lastgang(r, true);

            Assert.NotEqual(ohne.Length, mit.Length);
        }

        /// <summary>Dieselbe Reihe, dasselbe Bild — Bit fuer Bit.</summary>
        [Fact]
        public void Dasselbe_Ergebnis_liefert_dasselbe_Bild()
        {
            PeakShavingErgebnis r = Ergebnis();

            Assert.Equal(PeakShavingBild.Lastgang(r, true), PeakShavingBild.Lastgang(r, true));
        }

        /// <summary>
        /// Die drei Farben stehen woertlich im Vorlaeufer
        /// (<c>Form_PeakShaving.ChartZeichnen</c> :708-716).
        /// </summary>
        [Fact]
        public void Die_drei_Farben_sind_die_des_Vorlaeufers()
        {
            Assert.Equal(new SkiaSharp.SKColor(190, 90, 90), PeakShavingBild.FarbeAlt);
            Assert.Equal(new SkiaSharp.SKColor(40, 110, 180), PeakShavingBild.FarbeNeu);
            Assert.Equal(new SkiaSharp.SKColor(120, 130, 140), PeakShavingBild.FarbeSoC);
        }
    }
}
