using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Lambert-Koordinaten des DWD</b> (Auftrag KL-2) — Rechts-/Hochwert einer
    /// TRY-Datei nach Länge/Breite und zurück.
    ///
    /// <para><b>Die Messlatte sind die offenen TRY-Regionaldaten.</b> Ihre fünfzehn
    /// Regionsdateien tragen Breite und Länge des Mittelpunktes im DATEINAMEN
    /// (<c>TRY2015_&lt;Breite·1e4&gt;&lt;Länge·1e4&gt;_Jahr.dat</c>) und Rechts-/Hochwert
    /// im KOPF — zwei unabhängige Angaben desselben Punktes. Die fünfzehn Paare sind
    /// einmal aus <c>data.zip</c> v1.4.0 gelesen und stehen hier als FESTE Fälle; der
    /// Test braucht kein Netz.</para>
    ///
    /// <para>Sie belegen die Projektion: <b>ETRS89 / LCC Europa, EPSG:3034</b>. Läge eine
    /// andere Projektion zugrunde, wichen die Punkte um Kilometer ab, nicht um
    /// Zentimeter.</para>
    /// </summary>
    public class LambertKoordinatenTests
    {
        /// <summary>
        /// Die fünfzehn Regionsmittelpunkte: Rechtswert, Hochwert, Länge, Breite
        /// (Länge/Breite aus dem DATEINAMEN, Rechts-/Hochwert aus dem KOPF).
        /// </summary>
        public static TheoryData<int, double, double, double, double> Regionen => new()
        {
            {  1, 3909500.0, 2968500.0,  8.5872, 53.5591 },
            {  2, 4135500.0, 3026500.0, 12.1412, 54.0878 },
            {  3, 4000500.0, 2964500.0, 10.0078, 53.5299 },
            {  4, 4201500.0, 2846500.0, 13.0651, 52.3938 },
            {  5, 3802500.0, 2745500.0,  7.0568, 51.4562 },
            {  6, 3859500.0, 2656500.0,  7.9426, 50.6461 },
            {  7, 3964500.0, 2728500.0,  9.4725, 51.3334 },
            {  8, 4040500.0, 2770500.0, 10.6069, 51.7239 },
            {  9, 4198500.0, 2677500.0, 12.9181, 50.8233 },
            { 10, 4131500.0, 2621500.0, 11.9124, 50.3226 },
            { 11, 4202500.0, 2635500.0, 12.9522, 50.4312 },
            { 12, 3892500.0, 2531500.0,  8.4637, 49.4902 },
            { 13, 4181500.0, 2399500.0, 12.5286, 48.2432 },
            { 14, 3990500.0, 2440500.0,  9.8666, 48.6536 },
            { 15, 4080500.0, 2316500.0, 11.1046, 47.4945 }
        };

        /// <summary>
        /// <b>Der Nachweis der Projektion.</b> Jeder der fünfzehn Regionsmittelpunkte
        /// wird aus Rechts-/Hochwert gerechnet und gegen die Angabe des Dateinamens
        /// gehalten. Die Grenze 0,001° ist die Abnahme des Auftrags; gemessen wird
        /// höchstens 0,00005° — der Rest ist die Rundung der Kopfwerte auf das
        /// 500-m-Raster.
        /// </summary>
        [Theory]
        [MemberData(nameof(Regionen))]
        public void Jede_TRY_Region_trifft_ihren_Dateinamen(int nummer, double rechtswert,
                                                            double hochwert,
                                                            double laenge, double breite)
        {
            Assert.True(LambertKoordinaten.NachGeographisch(rechtswert, hochwert,
                                                            out double l, out double b),
                        "Region " + nummer.ToString(System.Globalization.CultureInfo.InvariantCulture));

            Assert.True(Math.Abs(l - laenge) < 0.001, "Länge Region " + nummer + ": " + l);
            Assert.True(Math.Abs(b - breite) < 0.001, "Breite Region " + nummer + ": " + b);
        }

        /// <summary>
        /// Der URSPRUNG der Projektion (52° N / 10° O) liegt genau auf falschem Ost- und
        /// Nordwert — die eine Stelle, an der die Formeln ohne Rechnung stimmen müssen.
        /// </summary>
        [Fact]
        public void Der_Ursprung_liegt_auf_dem_falschen_Ost_und_Nordwert()
        {
            Assert.True(LambertKoordinaten.NachLambert(LambertKoordinaten.URSPRUNG_LAENGE,
                                                       LambertKoordinaten.URSPRUNG_BREITE,
                                                       out double rw, out double hw));

            Assert.Equal(LambertKoordinaten.FALSCHER_OSTWERT, rw, 6);
            Assert.Equal(LambertKoordinaten.FALSCHER_NORDWERT, hw, 6);

            Assert.True(LambertKoordinaten.NachGeographisch(
                LambertKoordinaten.FALSCHER_OSTWERT, LambertKoordinaten.FALSCHER_NORDWERT,
                out double l, out double b));

            Assert.Equal(LambertKoordinaten.URSPRUNG_LAENGE, l, 9);
            Assert.Equal(LambertKoordinaten.URSPRUNG_BREITE, b, 9);
        }

        /// <summary>
        /// <b>Hin und zurück unter einem Millimeter.</b> Fünf Punkte über den ganzen
        /// zulässigen Bereich — die Ecken und die Mitte. Ein Millimeter sind rund
        /// 1e-8 Grad in der Breite; geprüft wird auf 1e-9 Grad, also schärfer.
        /// </summary>
        [Theory]
        [InlineData(10.0, 52.0)]
        [InlineData(5.0, 45.0)]
        [InlineData(16.0, 56.0)]
        [InlineData(5.0, 56.0)]
        [InlineData(9.0, 49.0)]
        public void Hin_und_Rueckrechnung_treffen_denselben_Punkt(double laenge, double breite)
        {
            Assert.True(LambertKoordinaten.NachLambert(laenge, breite,
                                                       out double rw, out double hw));
            Assert.True(LambertKoordinaten.NachGeographisch(rw, hw,
                                                            out double l, out double b));

            Assert.True(Math.Abs(l - laenge) < 1e-9, "Länge: " + l);
            Assert.True(Math.Abs(b - breite) < 1e-9, "Breite: " + b);
        }

        /// <summary>
        /// <b>Der Punkt der Importprobe.</b> Rechtswert 3 929 310 / Hochwert 2 478 193
        /// ergeben 9,0000° O / 49,0000° N — der runde Punkt, auf den die synthetische
        /// Probe gesetzt ist (<c>Referenzlaeufe/Importproben/dwd_try_synthetisch_72h.dat</c>,
        /// Kopfzeilen 6 und 7). Die Werte sind mit <c>NachLambert</c> gerechnet und auf
        /// volle Meter gerundet.
        /// </summary>
        [Fact]
        public void Der_Punkt_der_Importprobe_ist_neun_Ost_und_neunundvierzig_Nord()
        {
            Assert.True(LambertKoordinaten.NachLambert(9.0, 49.0,
                                                       out double rw, out double hw));
            Assert.Equal(3929310.0, Math.Round(rw));
            Assert.Equal(2478193.0, Math.Round(hw));

            Assert.True(LambertKoordinaten.NachGeographisch(3929310.0, 2478193.0,
                                                            out double l, out double b));
            Assert.Equal(9.0, l, 4);
            Assert.Equal(49.0, b, 4);
        }

        /// <summary>
        /// <b>Außerhalb der Plausibilitätsgrenzen gibt es keine stille Null.</b> Der
        /// Nullpunkt des Rasters, ein Punkt weit südlich und einer weit östlich werden
        /// abgelehnt — und die Rückgaben sind <c>NaN</c>, nicht 0.
        /// </summary>
        [Theory]
        [InlineData(0.0, 0.0)]                  // gar keine Koordinaten
        [InlineData(3929310.0, 1200000.0)]      // weit suedlich von 45° N
        [InlineData(5200000.0, 2478193.0)]      // weit oestlich von 16° O
        public void Ein_unplausibler_Punkt_wird_benannt_abgelehnt(double rechtswert,
                                                                  double hochwert)
        {
            Assert.False(LambertKoordinaten.NachGeographisch(rechtswert, hochwert,
                                                             out double l, out double b));
            Assert.True(double.IsNaN(l));
            Assert.True(double.IsNaN(b));
        }

        /// <summary>Dieselbe Regel in der Gegenrichtung.</summary>
        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(9.0, 44.999)]
        [InlineData(16.001, 50.0)]
        [InlineData(double.NaN, 49.0)]
        public void Eine_unplausible_Laenge_oder_Breite_ergibt_keine_Meter(double laenge,
                                                                          double breite)
        {
            Assert.False(LambertKoordinaten.NachLambert(laenge, breite,
                                                        out double rw, out double hw));
            Assert.True(double.IsNaN(rw));
            Assert.True(double.IsNaN(hw));
        }

        /// <summary>
        /// Die Grenzen selbst gehören noch dazu — <c>ImBereich</c> schließt sie ein,
        /// damit ein Punkt genau auf 45° N nicht durchfällt.
        /// </summary>
        [Fact]
        public void Die_Grenzen_gehoeren_noch_zum_Bereich()
        {
            Assert.True(LambertKoordinaten.ImBereich(LambertKoordinaten.LAENGE_MIN,
                                                     LambertKoordinaten.BREITE_MIN));
            Assert.True(LambertKoordinaten.ImBereich(LambertKoordinaten.LAENGE_MAX,
                                                     LambertKoordinaten.BREITE_MAX));
            Assert.False(LambertKoordinaten.ImBereich(LambertKoordinaten.LAENGE_MIN - 0.001,
                                                      LambertKoordinaten.BREITE_MIN));
            Assert.False(LambertKoordinaten.ImBereich(double.NaN, 50.0));
        }
    }
}
