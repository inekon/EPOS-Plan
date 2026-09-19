using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Rasterprojektion der DWD-Testreferenzjahre</b> — Lambert konform konisch
    /// und ihre Umkehrung nach WGS 84 (<see cref="LambertDwd"/>).
    ///
    /// <para><b>Wogegen gemessen wird.</b> Die Datei nennt ihr Koordinatensystem, nicht
    /// dessen Parameter; eine amtliche Punktliste liegt nicht bei. Geprüft wird deshalb,
    /// was ohne fremde Zahlentafel prüfbar ist: die <b>Rundprobe</b> (hin und zurück
    /// unter einem Meter — sonst wäre die Umkehrung keine), der <b>Projektionsursprung</b>
    /// (er muss auf 51° N / 10,5° O fallen, sonst stimmen die Zuschläge nicht) und die
    /// <b>Lage eines Rasterpunkts</b> der Probe.</para>
    ///
    /// <para>Ohne Netz, ohne Datenbank, ohne Datei — reine Rechnung. Die Kultur spielt
    /// keine Rolle: Hier wird nichts formatiert und nichts gelesen.</para>
    /// </summary>
    public class LambertDwdTests
    {
        /// <summary>Der Rasterpunkt der Probe <c>dwd_try_kopf_lambert.dat</c>.</summary>
        private const double PROBE_RW = 3936500.0;
        private const double PROBE_HW = 2695500.0;

        // =====================================================================
        //  1 — Die Rundprobe
        // =====================================================================

        /// <summary>
        /// <b>Hin und zurück unter einem Meter.</b> Der Hinweg
        /// <see cref="LambertDwd.AusWgs84"/> steht allein für diese Probe da; der Import
        /// braucht ihn nicht.
        /// </summary>
        [Theory]
        [InlineData(PROBE_RW, PROBE_HW)]
        [InlineData(4000000.0, 2800000.0)]     // der Projektionsursprung
        [InlineData(3450000.0, 2500000.0)]     // Suedwestecke des Rasters
        [InlineData(4300000.0, 3200000.0)]     // Nordostecke des Rasters
        [InlineData(3600000.0, 3100000.0)]
        [InlineData(4200000.0, 2550000.0)]
        public void Die_Umrechnung_ist_hin_und_zurueck_dieselbe(double rw, double hw)
        {
            (double lon, double lat) = LambertDwd.NachWgs84(rw, hw);
            (double rwZurueck, double hwZurueck) = LambertDwd.AusWgs84(lon, lat);

            Assert.True(Math.Abs(rw - rwZurueck) < 1.0,
                        "Rechtswert weicht um " + (rw - rwZurueck) + " m ab");
            Assert.True(Math.Abs(hw - hwZurueck) < 1.0,
                        "Hochwert weicht um " + (hw - hwZurueck) + " m ab");
        }

        // =====================================================================
        //  2 — Die zwei festen Punkte
        // =====================================================================

        /// <summary>
        /// <b>Der Zuschlagspunkt IST der Projektionsursprung</b>: Rechtswert
        /// 4 000 000 m und Hochwert 2 800 000 m fallen auf 10,5° O / 51° N. Fiele er
        /// woanders hin, stimmte einer der beiden Zuschläge nicht.
        /// </summary>
        [Fact]
        public void Der_Zuschlagspunkt_liegt_auf_10_5_Grad_Ost_und_51_Grad_Nord()
        {
            (double lon, double lat) = LambertDwd.NachWgs84(LambertDwd.RECHTSWERT_ZUSCHLAG,
                                                            LambertDwd.HOCHWERT_ZUSCHLAG);

            Assert.Equal(LambertDwd.URSPRUNG_LAENGE, lon, 9);
            Assert.Equal(LambertDwd.URSPRUNG_BREITE, lat, 9);
        }

        /// <summary>
        /// <b>Der Probe-Punkt</b> 3 936 500 / 2 695 500 fällt auf 9,6124° O / 50,0563° N —
        /// die Toleranz ist 1e-4 Grad, rund elf Meter.
        /// </summary>
        [Fact]
        public void Der_Punkt_der_Probe_faellt_auf_9_6124_Ost_und_50_0563_Nord()
        {
            (double lon, double lat) = LambertDwd.NachWgs84(PROBE_RW, PROBE_HW);

            Assert.Equal(9.6124, lon, 4);
            Assert.Equal(50.0563, lat, 4);
        }

        // =====================================================================
        //  3 — Die Plausibilitaetsschranke
        // =====================================================================

        /// <summary>
        /// <b>Die Schranke fängt fremde Projektionsparameter ab.</b> Der Probe-Punkt
        /// liegt innen; ein Punkt weit südwestlich des Rasters fällt durch — für ihn
        /// wird kein Standort vorgeschlagen.
        /// </summary>
        [Fact]
        public void InDeutschland_trennt_das_Raster_von_allem_anderen()
        {
            (double lonInnen, double latInnen) = LambertDwd.NachWgs84(PROBE_RW, PROBE_HW);
            Assert.True(LambertDwd.InDeutschland(lonInnen, latInnen));

            // Mittelmeer statt Mittelgebirge.
            (double lonAussen, double latAussen) = LambertDwd.NachWgs84(3000000.0, 1800000.0);
            Assert.False(LambertDwd.InDeutschland(lonAussen, latAussen));
        }

        /// <summary>Die vier Kanten des Rahmens 47…55° N und 5,5…15,5° O, einzeln.</summary>
        [Theory]
        [InlineData(10.0, 51.0, true)]       // mitten drin
        [InlineData(5.5, 47.0, true)]        // Suedwestecke, einschliesslich
        [InlineData(15.5, 55.0, true)]       // Nordostecke, einschliesslich
        [InlineData(10.0, 46.9, false)]      // zu weit sued
        [InlineData(10.0, 55.1, false)]      // zu weit nord
        [InlineData(5.4, 51.0, false)]       // zu weit west
        [InlineData(15.6, 51.0, false)]      // zu weit ost
        public void Der_Rahmen_des_TRY_Rasters_steht_fest(double lon, double lat, bool drin)
        {
            Assert.Equal(drin, LambertDwd.InDeutschland(lon, lat));
        }
    }
}
