using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace ZapfprofilValidierung.Tests
{
    /// <summary>
    /// <b>Die Spitzenstreuung im Werkzeug</b> (Folgen V9 und V10 des Validierungsberichts,
    /// Welle #561): Die Realisierungsspitzen liegen auf der Bilanzgrenze der verglichenen Reihe —
    /// bei Grenze 2 oder 3 samt Zirkulation, mit denselben Streckfaktoren wie die kalibrierte Reihe —,
    /// und die Streuung nimmt eigene Quantile (Vorgabe P85 / P95) statt der des Bands. Erfundene
    /// Zahlen; nur Verhältniszahlen gehen in einen Bericht.
    /// </summary>
    public sealed class SpitzenstreuungTests
    {
        /// <summary>
        /// <b>V9:</b> <c>Objektlauf.Spitzen</c> bildet je Realisierung
        /// <c>max_h (faktor · M_r[h] + Z[h])</c> — die Zapfung mit ihrem Streckfaktor plus den
        /// Zirkulationsteil der verglichenen Reihe. Ohne Zirkulation (Zapfstelle) bleibt es die
        /// gestreckte Spitze der Zapfung.
        /// </summary>
        [Fact]
        public void V9_Die_Realisierungsspitzen_tragen_den_Zirkulationsteil()
        {
            // Zwei Realisierungen: die eine hat ihre Spitze um 7 Uhr, die andere um 20 Uhr.
            double[] m0 = Enumerable.Repeat(1.0, 24).ToArray(); m0[7] = 10.0;
            double[] m1 = Enumerable.Repeat(1.0, 24).ToArray(); m1[20] = 9.0;
            var e = new ZapfprofilErgebnis
            {
                JeZone = new[]
                {
                    new ZonenErgebnis
                    {
                        Zone = "Probe",
                        StundenspitzenKw = new[] { 10.0, 9.0 },
                        TagesstundenspitzenKw = new IReadOnlyList<double>[] { m0, m1 }
                    }
                }
            };
            // Zirkulation nur von 18 bis 22 Uhr, 2 kW (erfunden), an jedem Tag gleich.
            var z = new double[Bilanzreihe.STUNDEN];
            for (int d = 0; d < 365; d++) for (int h = 18; h < 22; h++) z[d * 24 + h] = 2.0;
            var zirk = new Bilanzreihe(z);

            Assert.Equal(new[] { 10.0, 9.0 }, Objektlauf.Spitzen(e, 1.0, null));
            Assert.Equal(new[] { 20.0, 18.0 }, Objektlauf.Spitzen(e, 2.0, null));
            // Mit Zirkulation: r = 0 bleibt bei 7 Uhr (10 > 1 + 2), r = 1 steigt um die Zirkulation um 20 Uhr.
            Assert.Equal(new[] { 10.0, 11.0 }, Objektlauf.Spitzen(e, 1.0, zirk));
            Assert.Equal(new[] { 5.0, 6.5 }, Objektlauf.Spitzen(e, 0.5, zirk));
            // Ohne Ensemble: leer.
            Assert.Empty(Objektlauf.Spitzen(new ZapfprofilErgebnis(), 1.0, zirk));
        }

        /// <summary>
        /// <b>V9 und V10 im Beispiellauf:</b> Das Wohnobjekt misst mit Verteilung (Grenze 2) und
        /// rechnet eine Zirkulation. Mit zehn Realisierungen nimmt die Streuung die Quantile P85 und
        /// P95 (Ränge 9 und 10) — die Streubreite ist nicht mehr trivial 1 — und die Streuung liegt
        /// auf der Stufe der verglichenen Reihe: Die größte Realisierungsspitze reicht an die größte
        /// Stunde der verglichenen Reihe heran (mit Zirkulation), statt um deren Anteil darunter zu liegen.
        /// </summary>
        [Fact]
        public void V10_Mit_zehn_Realisierungen_ist_die_Streubreite_nicht_trivial()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            string quelle = v.ObjektKopie("BSP-WOHNEN-01", "streuung-v10");
            Vorrichtung.JsonErsetzen(Vorrichtung.Objektdatei(quelle, "BSP-WOHNEN-01"),
                                     "\"jahresreihe_stochastisch\": false", "\"jahresreihe_stochastisch\": true");
            Katalog katalog = Katalogquelle.Lesen(v.Katalog, out string fehler);
            Assert.Null(fehler);
            Objektbeschreibung o = Objektbeschreibung.Lesen(Vorrichtung.Objektdatei(quelle, "BSP-WOHNEN-01"),
                                                           out string lesefehler);
            Assert.Null(lesefehler);
            Objektbefund b = Objektlauf.Rechnen(Path.Combine(quelle, "BSP-WOHNEN-01"), o, katalog, 10, 4711);

            Assert.Null(b.Abbruch);
            Assert.Equal(ZapfBilanzgrenze.MitVerteilung, b.Grenze);
            Assert.Equal(10, b.StreuungRealisierungen);
            Assert.Equal(0.85, b.StreuungPerzentilUnten, 12);
            Assert.Equal(0.95, b.StreuungPerzentilOben, 12);
            Assert.True(b.Streubreite > 1.0, "Mit P85 / P95 bei zehn Realisierungen ist die Streubreite größer als 1.");
            // Die obere Grenze ist die größte Realisierungsspitze, bezogen auf die größte Stunde der
            // verglichenen Reihe (Zapfung samt Zirkulation). Die Realisierung zum Seed trägt dieselbe
            // Zirkulation; sie unterscheidet sich von der verglichenen Reihe nur um den Faktor der Energieprobe.
            Assert.True(b.StreuungOben > 0.9, "Die Realisierungsspitzen liegen nicht auf der Stufe der verglichenen Reihe.");
        }
    }
}
