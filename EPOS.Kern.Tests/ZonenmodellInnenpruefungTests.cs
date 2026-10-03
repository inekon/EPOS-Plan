using System;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die innere Lastumkehr eines Abschnitts</b> (Rechenweg RP2a): Messung und allgemeine Innenprüfung.
    /// Grundlage ist die leichte Zone des Rechenbefunds RB-Z4 (<see cref="ZonenkopplungAbschnittsregelTests"/>):
    /// leichte Außenbauteile (Zeitkonstante rund 70 s), schwere Innenbauteile. Die schnelle Mode kehrt die
    /// Heizlast binnen Minuten um, die langsame holt sie bis zum Stundenende zurück.
    /// </summary>
    public class ZonenmodellInnenpruefungTests
    {
        private readonly ITestOutputHelper _aus;

        public ZonenmodellInnenpruefungTests(ITestOutputHelper aus) => _aus = aus;

        private static ErsatzparameterRC LeichteZone()
            => new ErsatzparameterRC(1.8016e6, 7.1519e7, 4.2769e-5, 5.44496e-4, 8.66509e-5, 1.93304e-4, 2.92046e-4,
                                     1.57705e-4, 3.02137e-3, 1762.195, 1268.193, 1466.932, 8.00903e-4, 4.0279e-3,
                                     153.804, 169.184, 2.08768e-5);

        /// <summary>Die Stunde des Befunds RB-Z4 mit dem Faktor <paramref name="gewinne"/> auf die Lasten.</summary>
        private static Stundenrand Stunde(double gewinne = 1.0)
            => new Stundenrand(6.15, 16.25, 20.0, double.PositiveInfinity,
                               4055.87 * gewinne, 2684.57 * gewinne, 1945.32 * gewinne);

        private static Zonenmodell2K Modell(bool messen, bool innenpruefung = true)
            => new Zonenmodell2K(LeichteZone())
            {
                Innenumkehrmessung = messen,
                InnenpruefungObergrenzeFuerProbe = innenpruefung ? Zonenmodell2K.INNENPRUEFUNG_ABSCHNITTE : 0,
            };

        private const double AW_UMKEHR = 19.4985, IW_UMKEHR = 21.0;
        private const double AW_BAND = 21.0, IW_BAND = 20.2, GEWINNE_BAND = 1.5;

        /// <summary>Die Bilanz einer Stunde: Speicheränderung = Zuflüsse (Muster Zonenmodell2KTests).</summary>
        private static void Bilanz(ErsatzparameterRC p, double aw0, double iw0, in Stundenrand r, in Stundenergebnis e)
        {
            double u0 = p.C_AW_Jk * aw0 + p.C_IW_Jk * iw0;
            double u1 = p.C_AW_Jk * e.ThetaMAwEnde + p.C_IW_Jk * e.ThetaMIwEnde;
            double zufluss = (r.ThetaEq - e.ThetaMAwMittel) / p.R_Rest_AWGruppe_KW + (r.ThetaOut - e.ThetaAirMittel) / p.R_ext_KW
                           + r.PhiRadAW + r.PhiRadIW + r.PhiConv + e.HeizleistungW - e.KuehlleistungW;
            double speicher = (u1 - u0) / Zonenmodell2K.STUNDE_S;
            Assert.True(Math.Abs(speicher - zufluss) < 1e-6 * (1.0 + Math.Abs(zufluss)), "Bilanz: Speicher " + speicher + " W, Zufluss " + zufluss + " W");
        }

        /// <summary>
        /// Die Messung zählt einen geregelten Abschnitt, dessen Heizlast im Innern unter null fällt, obwohl Mittel
        /// und Endpunkt positiv sind (Massen 19,4985 / 21,0 °C, die Stunde des Befunds), und eine Bandverletzung im
        /// Totband (Massen 21,0 / 20,2 °C, Lasten × 1,5: die Raumluft fällt im Innern unter den Sollwert und steigt
        /// wieder darüber). Die Messung ändert keine Zahl.
        /// </summary>
        [Fact]
        public void Die_Messung_zaehlt_und_aendert_nichts()
        {
            var r = Stunde();
            var mit = Modell(true, innenpruefung: false);
            var ohne = Modell(false, innenpruefung: false);
            var muster = new Stundenmuster(new[] { Betriebsfall.HeizenGeregelt }, new[] { Zonenmodell2K.STUNDE_S });
            mit.Zuruecksetzen(AW_UMKEHR, IW_UMKEHR);
            ohne.Zuruecksetzen(AW_UMKEHR, IW_UMKEHR);
            Stundenergebnis a = mit.SchrittMitMuster(in r, muster);
            Stundenergebnis b = ohne.SchrittMitMuster(in r, muster);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "Umkehr: {0} Abschnitt(e), {1:F1} J; Heizen {2:F3} W",
                                         a.MessungUmkehrAbschnitte, a.MessungUmkehrJ, a.HeizleistungW));
            Assert.Equal(1, a.MessungUmkehrAbschnitte);
            Assert.InRange(a.MessungUmkehrJ, 1.0e3, 1.0e6);
            Assert.Equal(0, b.MessungUmkehrAbschnitte);
            Assert.Equal(0.0, b.MessungUmkehrJ);
            Assert.Equal(b.HeizleistungW, a.HeizleistungW);
            Assert.Equal(b.ThetaMAwEnde, a.ThetaMAwEnde);
            Assert.Equal(b.ThetaMIwEnde, a.ThetaMIwEnde);

            var r2 = Stunde(GEWINNE_BAND);
            var band = new Stundenmuster(new[] { Betriebsfall.Totband }, new[] { Zonenmodell2K.STUNDE_S });
            mit.Zuruecksetzen(AW_BAND, IW_BAND);
            Stundenergebnis c = mit.SchrittMitMuster(in r2, band);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "Band: {0} Abschnitt(e), {1:F3} K·s; Raumluft {2:F4} °C",
                                         c.MessungBandAbschnitte, c.MessungBandKs, c.ThetaAirMittel));
            Assert.Equal(1, c.MessungBandAbschnitte);
            Assert.True(c.MessungBandKs > 0.0);
            Assert.Equal(0, c.MessungUmkehrAbschnitte);
        }

        /// <summary>
        /// <b>Die allgemeine Innenprüfung</b> (E62) schneidet den geregelten Abschnitt am ersten Nulldurchgang der Heizlast,
        /// auch bei positivem Mittel: danach läuft die Zone frei, keine Kälte, die Messung bleibt leer, die Bilanz
        /// geschlossen. Ohne Innenprüfung bliebe die Stunde ein Abschnitt mit der verrechneten negativen Last.
        /// </summary>
        [Fact]
        public void Die_Innenpruefung_schneidet_eine_Umkehr_bei_zulaessigem_Mittel()
        {
            ErsatzparameterRC p = LeichteZone();
            Stundenrand r = Stunde();
            var alt = Modell(true, innenpruefung: false);
            alt.Zuruecksetzen(AW_UMKEHR, IW_UMKEHR);
            Stundenergebnis a = alt.Schritt(in r);
            Assert.Equal(1, a.Abschnitte);
            Assert.Equal(1, a.MessungUmkehrAbschnitte);

            var neu = Modell(true);
            neu.Zuruecksetzen(AW_UMKEHR, IW_UMKEHR);
            Stundenergebnis e = neu.Schritt(in r);
            Stundenmuster muster = neu.LetztesMuster;
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "ohne Innenprüfung: Heizen {0:F3} W; mit: {1} Abschnitte, erster {2} {3:F1} s, Heizen {4:F3} W, Raumluft {5:F4} °C",
                a.HeizleistungW, e.Abschnitte, muster.Folge[0], muster.Dauer[0], e.HeizleistungW, e.ThetaAirMittel));
            Assert.True(e.Abschnitte >= 2);
            Assert.Equal(Betriebsfall.HeizenGeregelt, muster.Folge[0]);
            Assert.True(muster.Dauer[0] < Zonenmodell2K.STUNDE_S);
            Assert.Equal(0, e.MessungUmkehrAbschnitte);
            Assert.Equal(0, e.MessungBandAbschnitte);
            Assert.False(e.InnenpruefungGedeckelt);
            Assert.True(e.HeizleistungW > 0.0);
            Assert.Equal(0.0, e.KuehlleistungW);
            Assert.True(e.ThetaAirMittel >= 20.0 - 1e-9);
            Bilanz(p, AW_UMKEHR, IW_UMKEHR, in r, in e);

            // Das gefundene Muster rechnet nachgerechnet bitgleich; das alte (ein Abschnitt) ist nicht haltbar.
            neu.Zuruecksetzen(AW_UMKEHR, IW_UMKEHR);
            Stundenergebnis nach = neu.SchrittMitMuster(in r, muster);
            Assert.Equal(e.HeizleistungW, nach.HeizleistungW);
            Assert.Equal(e.ThetaAirMittel, nach.ThetaAirMittel);
            neu.Zuruecksetzen(AW_UMKEHR, IW_UMKEHR);
            var ex = Assert.Throws<GebaeudeModellException>(() =>
                neu.SchrittMitMuster(in r, new Stundenmuster(new[] { Betriebsfall.HeizenGeregelt }, new[] { Zonenmodell2K.STUNDE_S })));
            Assert.Equal(GebaeudeModellFehler.AbschnittsregelVerletzt, ex.Grund);
        }

        /// <summary>
        /// Das Totband: Verlässt die Raumluft das Band im Innern, endet der Abschnitt am Austritt; ein festes Muster mit
        /// diesem Totband ist nicht haltbar. Gerechnet wird mit dem Zustand und den Lasten des Messfalls.
        /// </summary>
        [Fact]
        public void Die_Innenpruefung_haelt_das_Band_im_Totband()
        {
            ErsatzparameterRC p = LeichteZone();
            Stundenrand r = Stunde(GEWINNE_BAND);
            var m = Modell(true);
            m.Zuruecksetzen(AW_BAND, IW_BAND);
            var ex = Assert.Throws<GebaeudeModellException>(() =>
                m.SchrittMitMuster(in r, new Stundenmuster(new[] { Betriebsfall.Totband }, new[] { Zonenmodell2K.STUNDE_S })));
            Assert.Equal(GebaeudeModellFehler.AbschnittsregelVerletzt, ex.Grund);

            m.Zuruecksetzen(AW_BAND, IW_BAND);
            Stundenergebnis e = m.Schritt(in r);
            Stundenmuster muster = m.LetztesMuster;
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} Abschnitte ({1}), Heizen {2:F3} W, Raumluft {3:F4} °C",
                                         e.Abschnitte, string.Join(", ", m.LetzteFallfolge), e.HeizleistungW, e.ThetaAirMittel));
            Assert.Equal(0, e.MessungBandAbschnitte);
            Assert.Equal(0, e.MessungUmkehrAbschnitte);
            Assert.Equal(0.0, e.KuehlleistungW);
            Bilanz(p, AW_BAND, IW_BAND, in r, in e);
        }

        /// <summary>
        /// Die Obergrenze: Mit Obergrenze 1 sucht schon der erste Abschnitt nicht mehr; die Stunde bleibt, wie die
        /// Endpunktprüfung sie lässt, und zählt als gedeckelt (Laufhinweis SIMENG_ZONE_ABSCHNITTE).
        /// </summary>
        [Fact]
        public void Ueber_der_Obergrenze_zaehlt_die_Stunde_als_gedeckelt()
        {
            Stundenrand r = Stunde();
            var m = Modell(true);
            m.InnenpruefungObergrenzeFuerProbe = 1;
            m.Zuruecksetzen(AW_UMKEHR, IW_UMKEHR);
            Stundenergebnis e = m.Schritt(in r);
            Assert.Equal(1, e.Abschnitte);
            Assert.True(e.InnenpruefungGedeckelt);
            Assert.Equal(1, e.MessungUmkehrAbschnitte);
            Assert.Equal(8, Zonenmodell2K.INNENPRUEFUNG_ABSCHNITTE);
        }

        /// <summary>
        /// <b>Stunden ohne innere Umkehr bleiben Zeichen für Zeichen</b>, und der Lauf ist deterministisch: zwei Tage der
        /// leichten Zone mit wechselnder Außentemperatur, mit und ohne Innenprüfung — jede Stunde ohne gemessene Umkehr
        /// bitgleich; zwei Läufe mit Innenprüfung bitgleich; jede Stunde bilanziert, keine Kälte, keine negative Last.
        /// </summary>
        [Fact]
        public void Ohne_Umkehr_bitgleich_und_deterministisch()
        {
            ErsatzparameterRC p = LeichteZone();
            var ohne = Modell(true, innenpruefung: false);
            var mit1 = Modell(true);
            var mit2 = Modell(false);
            foreach (Zonenmodell2K m in new[] { ohne, mit1, mit2 }) m.Zuruecksetzen(19.4985, 21.0807);
            int gleich = 0, umkehr = 0;
            for (int h = 0; h < 48; h++)
            {
                double aussen = 2.0 + 6.0 * Math.Sin(2.0 * Math.PI * h / 24.0);
                double gewinne = h % 24 >= 8 && h % 24 < 18 ? 1.0 : 0.2;
                var r = new Stundenrand(aussen, aussen + 10.0, 20.0, double.PositiveInfinity,
                                        4000.0 * gewinne, 2700.0 * gewinne, 1950.0 * gewinne);
                // Gleicher Startzustand je Stunde: der Zustand des Laufs mit Innenprüfung.
                double aw = mit1.ThetaMAw, iw = mit1.ThetaMIw;
                ohne.Zuruecksetzen(aw, iw);
                Stundenergebnis a = ohne.Schritt(in r);
                Stundenergebnis b = mit1.Schritt(in r);
                Stundenergebnis c = mit2.Schritt(in r);
                Assert.Equal(b.HeizleistungW, c.HeizleistungW);
                Assert.Equal(b.ThetaMAwEnde, c.ThetaMAwEnde);
                Assert.Equal(b.ThetaMIwEnde, c.ThetaMIwEnde);
                if (a.MessungUmkehrAbschnitte == 0 && a.MessungBandAbschnitte == 0)
                {
                    Assert.Equal(a.HeizleistungW, b.HeizleistungW);
                    Assert.Equal(a.ThetaAirMittel, b.ThetaAirMittel);
                    Assert.Equal(a.ThetaMAwEnde, b.ThetaMAwEnde);
                    Assert.Equal(a.ThetaMIwEnde, b.ThetaMIwEnde);
                    gleich++;
                }
                else umkehr++;
                Assert.Equal(0, b.MessungUmkehrAbschnitte);
                Assert.Equal(0, b.MessungBandAbschnitte);
                Assert.Equal(0.0, b.KuehlleistungW);
                Assert.True(b.HeizleistungW >= 0.0);
                Bilanz(p, aw, iw, in r, in b);
            }
            _aus.WriteLine("Stunden bitgleich " + gleich + ", mit innerer Umkehr " + umkehr);
            Assert.True(gleich > 0);
        }

        /// <summary>Die Messung des Laufs: Zähler und Einheiten (J → kWh, K·s → K·h), Summe und Skalierung.</summary>
        [Fact]
        public void Der_Zaehler_fasst_Stunden_und_Einheiten()
        {
            var z = new Innenumkehrzaehler();
            z.Aufnehmen(new Stundenergebnis(0, 0, 20, 20, 20, 20, 20, 20, 20, 20, 2) { MessungUmkehrJ = 3.6e6, MessungUmkehrAbschnitte = 2 });
            z.Aufnehmen(new Stundenergebnis(0, 0, 20, 20, 20, 20, 20, 20, 20, 20, 1) { MessungBandKs = 7200.0, MessungBandAbschnitte = 1 });
            z.Aufnehmen(new Stundenergebnis(0, 0, 20, 20, 20, 20, 20, 20, 20, 20, 1));
            Innenumkehrmessung m = z.Ergebnis();
            Assert.Equal(new Innenumkehrmessung(1, 2, 1.0, 1, 1, 2.0), m);
            Assert.Equal(new Innenumkehrmessung(2, 4, 2.0, 2, 2, 4.0), Innenumkehrmessung.Summe(new[] { m, null, m }));
            Assert.Null(Innenumkehrmessung.Summe(new Innenumkehrmessung[] { null }));
            Assert.Equal(new Innenumkehrmessung(1, 2, 3.0, 1, 1, 2.0), m.Skaliert(3.0));
        }
    }
}
