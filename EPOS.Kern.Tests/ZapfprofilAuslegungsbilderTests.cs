using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;
using static EPOS.Kern.Tests.AuslegungTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Bilder der Auslegung</b> (<c>ZapfprofilBilder</c>, <c>ChartRenderer.SummenlinieModell</c>;
    /// Umsetzungskonzept Zapfprofilgenerator 5.6; Stufe Z2, Gruppe 2): Die Reihen entstehen im
    /// Kern aus Bedarfstag, Nachweis und Speicherauslegung — die Hülle rechnet nichts. Geprüft
    /// wird, dass die Summenlinie den Speicherinhalt als Abstand trägt, die Woche das Defizit der
    /// Woche 2 und den Füllstand beim Bezugsvolumen, und dass die Modelle Zeichenfläche, Reihen,
    /// zweite Achse und Marken führen. Werte erfunden.
    /// </summary>
    public sealed class ZapfprofilAuslegungsbilderTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Die_Summenlinie_traegt_den_Speicherinhalt_als_Abstand_von_Versorgung_und_Bedarf()
        {
            Bedarfstag tag = Tag(new Zapfereignis(420, 30, 6.0), new Zapfereignis(1080, 60, 9.0));
            Summenlinienergebnis sl = Summenlinie.Rechnen(tag, Linie(), 0);
            Summenliniennachweis n = sl.Nachweis;

            Summenlinienkurven k = ZapfprofilBilder.SummenlinieKurven(tag, n);

            Assert.Equal(Bedarfstag.MINUTEN + 1, k.BedarfKwh.Length);
            Assert.Equal(0.0, k.BedarfKwh[0]);
            Assert.Equal(tag.TagessummeKwh, k.BedarfKwh[Bedarfstag.MINUTEN], 9);
            for (int i = 0; i <= Bedarfstag.MINUTEN; i++)
                Assert.Equal(n.InhaltKwh[i], k.VersorgungKwh[i] - k.BedarfKwh[i], 9);
            Assert.Equal(n.SpeicherMaxKwh, k.VersorgungKwh[0], 9);

            // Der kleinste Abstand Q(i) − q(i) liegt in einem Zapfblock; die Marke steht dahinter.
            int m = k.BeruehrungMinute;
            Assert.True((m > 420 && m <= 450) || (m > 1080 && m <= 1140), "Minute " + m);
            Assert.Equal(n.KleinsterAbstandKwh, n.InhaltKwh[m - 1] - tag.MinutenKwh[m - 1] - n.MindestinhaltKwh, 9);

            Assert.Null(ZapfprofilBilder.SummenlinieKurven(tag, n with { InhaltKwh = null }));
            Assert.Null(ZapfprofilBilder.SummenlinieKurven(null, n));
        }

        [Fact]
        public void Das_Summenlinienmodell_fuehrt_zwei_Reihen_und_beide_Marken()
        {
            var bedarf = new double[1441];
            var versorgung = new double[1441];
            for (int i = 0; i <= 1440; i++) { bedarf[i] = i * 0.01; versorgung[i] = 20.0 + i * 0.01; }

            Zeichenmodell z = ZapfprofilBilder.SummenlinieModell(bedarf, versorgung, 700, new ZapfprofilAuslegungBildtexte());

            Assert.NotNull(z.Flaeche);
            Assert.Equal(Achsenart.Wert, z.Flaeche.X);
            Assert.Equal(0.0, z.Flaeche.Daten.XVon);
            Assert.Equal(1440.0, z.Flaeche.Daten.XBis);
            Assert.Equal(new[] { "Bedarf kumuliert", "Versorgung kumuliert" }, z.Reihen.Select(r => r.Name).ToArray());
            Assert.Single(z.Befehle.OfType<Kreis>(), b => b.Marke == "marke");            // kleinster Abstand
            Assert.Single(z.Befehle.OfType<Linie>(), b => b.Marke == "marke");            // Speicherinhalt
            Assert.Equal(new[] { "Speicherinhalt", "kleinster Abstand" },
                         z.Befehle.OfType<Text>().Where(b => b.Marke == "marke").Select(b => b.Inhalt).ToArray());
            Assert.DoesNotContain(z.Befehle, b => b.Marke == "yachse2");
        }

        [Fact]
        public void Die_Wertepaarkurve_ordnet_nach_der_Leistung_und_traegt_ihre_x_Stellen()
        {
            Zeichenmodell z = ZapfprofilBilder.WertepaarkurveModell(new[] { 20.0, 5.0, 10.0 }, new[] { 280.0, 600.0, 420.0 },
                                                                     10.0, 420.0, new ZapfprofilAuslegungBildtexte());
            Datenreihe r = Assert.Single(z.Reihen);
            Assert.Equal(new[] { 5.0, 10.0, 20.0 }, r.XWerte);
            Assert.Equal(new[] { 600.0, 420.0, 280.0 }, r.Werte);
            Assert.Equal(5.0, z.Flaeche.Daten.XVon);
            Assert.Equal(20.0, z.Flaeche.Daten.XBis);

            // Weniger als zwei Paare: der Leerhinweis, keine Reihe.
            Zeichenmodell leer = ZapfprofilBilder.WertepaarkurveModell(new[] { 5.0 }, new[] { 600.0 }, null, null, null);
            Assert.Empty(leer.Reihen);
            Assert.Contains(leer.Befehle, b => b.Marke == "leerhinweis");
        }

        [Fact]
        public void Das_Wochenbild_zeigt_Defizit_und_Fuellstand_der_Woche_zwei_auf_der_zweiten_Achse()
        {
            Parametersatz ps = Auslegungssatz();
            var s = new double[168];
            for (int k = 0; k < 7; k++) { s[k * 24 + 7] = 10.0; s[k * 24 + 19] = 20.0; }
            Wochenreihe woche = Wochenreihe.Aus(s, 100, 0, Enumerable.Repeat(ZapfTagtyp.Werktag, 7).ToArray());
            Speicherauslegungsergebnis sa = TwwSpeicherauslegung.Rechnen(new Speicherauslegungseingang
            {
                Woche = woche, SpeicherC = 62.0, KaltwasserAuslegungC = 12.0, Nutzanteil = 0.75, Zuschlag = 0.1,
                Ladefenster = new Tagesfenster(22.0, 10.0), LadeAuto = false, LadeManuellKw = 4.0,
                Zirkulation = new Schaetzwert(false, 0.0, 0.5), ZirkulationLaufzeit = new Tagesfenster(5.0, 16.0),
                Din = new Din4708Ergebnis(), Wohnen = false,
                SummenlinienpunktL = 400.0
            }, ps);

            Wochenbildreihen w = ZapfprofilBilder.Wochenreihen(woche, sa);

            Assert.Equal(woche.StundenKwh.ToArray(), w.ZapfungKw);
            Assert.Equal(sa.DefizitKwh.Skip(168).ToArray(), w.DefizitKwh);
            Assert.Equal(0.5 * 16.0 * 7, w.ZirkulationKw.Sum(), 9);           // 0,5 kW in 16 Laufzeitstunden je Tag
            Assert.Equal(4.0 * 10.0 * 7, w.LadungKw.Sum(), 9);                // 4 kW im Fenster von 10 h je Tag
            Assert.Equal(0.0, w.LadungKw[12]);                                 // 12 Uhr liegt außerhalb 22–8 Uhr
            for (int t = 0; t < 168; t++)
                Assert.Equal(Math.Max(0.0, sa.KapazitaetKwh.Value - w.DefizitKwh[t]), w.FuellstandKwh[t], 9);
            Assert.Equal(sa.ZeitpunktStunde.Value - 168, w.MassgebendStunde);

            Zeichenmodell z = ZapfprofilBilder.AuslegungswocheModell(w.ZapfungKw, w.ZirkulationKw, w.LadungKw, w.DefizitKwh,
                w.FuellstandKwh, sa.FuellstandBezugL, w.MassgebendStunde, new ZapfprofilAuslegungBildtexte());
            Assert.Equal(new[] { "Zapfung", "Zirkulation", "Ladung", "Defizit kumuliert", "Füllstand bei 400 l" },
                         z.Reihen.Select(r => r.Name).ToArray());
            Assert.Equal(Reihenart.Flaeche, z.Reihen[0].Art);
            Assert.Equal(Achsenseite.Rechts, z.Reihen[3].Achsenseite);
            Assert.Equal(Achsenseite.Rechts, z.Reihen[4].Achsenseite);
            Assert.Contains(z.Befehle, b => b.Marke == "yachse2");
            Assert.Contains(z.Befehle, b => b.Marke == "marke");
            Assert.Equal(1.0, z.Flaeche.Daten.XVon);
            Assert.Equal(168.0, z.Flaeche.Daten.XBis);

            // Ohne Defizitfeld kein Wochenbild.
            Assert.Null(ZapfprofilBilder.Wochenreihen(woche, sa with { DefizitKwh = new double[3] }));
        }
    }
}
