using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Rechnungen der Gruppe „Teillast und Takten" im Katalogdialog der Kältemaschine (Fachkonzept Teillast und Takten
    /// 7.1, 3.2, 3.5, 3.7; KM3-E3-a): Lesezeile, Kurve aus Typkennfeld, Skalierung auf das Datenblatt und die Auskunft der
    /// Teillastpunkte — datenbankfrei, mit nachgerechneten Zahlenbeispielen.
    /// </summary>
    public sealed class KaeltemaschineTeillastDialogrechnungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const double GENAU = 1e-9;

        /// <summary>Die Beispielkurve des Fachkonzepts 3.2: a 0,10, b 0,60, c 0,30, x_u 0,2.</summary>
        private static KaeltemaschineModel Beispiel(string randweg = null) => new KaeltemaschineModel
        {
            Bezeichner = "Probe",
            Rueckkuehlart = KaeltemaschineSchema.RUECKKUEHLART_LUFT,
            Nennkaelteleistung_kW = 100,
            Nenn_EER = 4,
            Mindestteillast_Prozent = 20,
            Teillast_Weg = KaeltemaschineTeillastSchema.WEG_KURVE,
            Teillastkurve_a = 0.1,
            Teillastkurve_b = 0.6,
            Teillastkurve_c = 0.3,
            Teillastkurve_Lastgrad_Min = 0.2,
            Kennfeld_Randweg = randweg,
            Kennlinie = new List<KaeltemaschineKenndatenModel>
            {
                Punkt(30, 7, 5.0, 120), Punkt(40, 7, 4.0, 100),
                Punkt(30, 10, 5.5, 130), Punkt(40, 10, 4.4, 110)
            }
        };

        private static KaeltemaschineKenndatenModel Punkt(double rk, double kw, double eer, double q) =>
            new KaeltemaschineKenndatenModel { Rueckkuehltemperatur = rk, Kaltwassertemperatur = kw, EER = eer, Kaelteleistung_kW = q };

        private static double E(double x) => 0.1 + 0.6 * x + 0.3 * x * x;

        // ===================================================== Lesezeile

        [Fact]
        public void Lesezeile_rechnet_g_der_Beispielkurve()
        {
            KaeltemaschineTeillastDialogrechnung.Lesestand l = KaeltemaschineTeillastDialogrechnung.Lesezeile(Beispiel());

            Assert.Equal(KaeltemaschinenKurvenherkunft.Kurve, l.Herkunft);
            Assert.Equal(0.25 / E(0.25), l.G25, 9);
            Assert.Equal(0.5 / 0.475, l.G50, 9);
            Assert.Equal(0.75 / E(0.75), l.G75, 9);
            Assert.Equal("", l.Hinweis);
            Assert.Equal(10, l.Kurve.Count);
            Assert.Equal(1.0, l.Kurve.Last().G, 9);
            // Unter x_u bleibt g am Wert bei x_u stehen.
            Assert.Equal(0.2 / E(0.2), l.Kurve[0].G, 9);
        }

        [Fact]
        public void Lesezeile_nennt_Bestandsweg_und_verworfene_Kurve()
        {
            KaeltemaschineModel m = Beispiel();
            m.Teillast_Weg = null;
            KaeltemaschineTeillastDialogrechnung.Lesestand bestand = KaeltemaschineTeillastDialogrechnung.Lesezeile(m);
            Assert.Equal(KaeltemaschinenKurvenherkunft.Bestand, bestand.Herkunft);
            Assert.Equal(1.0, bestand.G50, 12);
            Assert.Equal("Ohne Teillastrechnung rechnet die Maschine wie bisher: linear und ohne Taktverlust.", bestand.Hinweis);

            m.Teillast_Weg = KaeltemaschineTeillastSchema.WEG_KURVE;
            m.Teillastkurve_c = null;
            KaeltemaschineTeillastDialogrechnung.Lesestand verworfen = KaeltemaschineTeillastDialogrechnung.Lesezeile(m);
            Assert.Equal(KaeltemaschinenKurvenherkunft.Verworfen, verworfen.Herkunft);
            Assert.Contains("nicht plausibel", verworfen.Hinweis);
            Assert.Equal(1.0, verworfen.G25, 12);
        }

        // ===================================================== Kurve aus Typkennfeld

        [Fact]
        public void Kurve_aus_Typkennfeld_traegt_die_Felder_des_Imports()
        {
            IReadOnlyList<string> namen = KaeltemaschineTeillastDialogrechnung.Typkennfeldnamen();
            Assert.NotEmpty(namen);
            int mitKurve = 0;
            foreach (string name in namen)
            {
                KaeltemaschineTeillastDialogrechnung.Typkurve k = KaeltemaschineTeillastDialogrechnung.KurveAusTypkennfeld(name);
                Assert.NotNull(k);
                KaeltemaschineModel m = KaeltemaschinenTypkennfelder.Lesen().First(t => t.Bezeichner == name).Modell();
                Assert.Equal(m.Teillast_Weg, k.TeillastWeg);
                Assert.Equal(m.Teillastkurve_a, k.A);
                Assert.Equal(m.Teillastkurve_b, k.B);
                Assert.Equal(m.Teillastkurve_c, k.C);
                Assert.Equal(m.Teillastkurve_Lastgrad_Min, k.LastgradMin);
                Assert.Equal(m.Verdichterregelung, k.Verdichterregelung);
                if (k.TeillastWeg == KaeltemaschineTeillastSchema.WEG_KURVE) mitKurve++;
            }
            Assert.True(mitKurve > 0);
            Assert.Null(KaeltemaschineTeillastDialogrechnung.KurveAusTypkennfeld("gibt es nicht"));
        }

        // ===================================================== Skalierung

        [Fact]
        public void Skalierung_trifft_den_Nennpunkt_des_Datenblatts()
        {
            KaeltemaschineModel q = Beispiel();
            Assert.Equal((100.0, 4.0), KaeltemaschineTeillastDialogrechnung.Nennpunkt(q));

            KaeltemaschineTeillastDialogrechnung.Skalierergebnis e =
                KaeltemaschineTeillastDialogrechnung.AufDatenblattSkalieren(q, 150, 4.8, "Typ X");
            Assert.True(e.Ok, e.Meldung);
            KaeltemaschineModel s = e.Satz;
            Assert.Equal(0, s.Id);
            Assert.False(s.ReadOnly);
            Assert.Equal("Typ X (skaliert)", s.Bezeichner);
            Assert.Contains("Typ X", s.Beschreibung);
            Assert.Equal(150, s.Nennkaelteleistung_kW);
            Assert.Equal(4.8, s.Nenn_EER);
            // Faktor 1,5 auf Q, 1,2 auf den EER an jeder Stützstelle.
            Assert.Equal(180, s.Kennlinie[0].Kaelteleistung_kW.Value, 6);
            Assert.Equal(6.0, s.Kennlinie[0].EER.Value, 6);
            Assert.Equal(5.28, s.Kennlinie[3].EER.Value, 6);
            (double qn, double en) = KaeltemaschineTeillastDialogrechnung.Nennpunkt(s);
            Assert.Equal(150, qn, 6);
            Assert.Equal(4.8, en, 6);
            // Teillast, Mindestteillast und Rückkühlart bleiben.
            Assert.Equal(q.Teillast_Weg, s.Teillast_Weg);
            Assert.Equal(q.Teillastkurve_b, s.Teillastkurve_b);
            Assert.Equal(q.Mindestteillast_Prozent, s.Mindestteillast_Prozent);
            Assert.Equal(q.Rueckkuehlart, s.Rueckkuehlart);
            // Die Quelle bleibt unberührt.
            Assert.Equal(4.0, q.Kennlinie[1].EER);
        }

        [Fact]
        public void Skalierung_eines_Typkennfelds_und_benannte_Ablehnungen()
        {
            string name = KaeltemaschineTeillastDialogrechnung.Typkennfeldnamen().First();
            KaeltemaschineTeillastDialogrechnung.Skalierergebnis e =
                KaeltemaschineTeillastDialogrechnung.AufDatenblattSkalieren(name, 222, 3.3);
            Assert.True(e.Ok, e.Meldung);
            (double qn, double en) = KaeltemaschineTeillastDialogrechnung.Nennpunkt(e.Satz);
            Assert.Equal(222, qn, 1);
            Assert.Equal(3.3, en, 2);
            Assert.Null(KaeltemaschineStammCtrl.NennEerHinweis(e.Satz));

            Assert.False(KaeltemaschineTeillastDialogrechnung.AufDatenblattSkalieren(name, 0, 3.3).Ok);
            Assert.Contains("Nenn-EER", KaeltemaschineTeillastDialogrechnung.AufDatenblattSkalieren(name, 10, -1).Meldung);
            Assert.Contains("nicht bekannt", KaeltemaschineTeillastDialogrechnung.AufDatenblattSkalieren("gibt es nicht", 10, 3).Meldung);
            Assert.False(KaeltemaschineTeillastDialogrechnung.AufDatenblattSkalieren(new KaeltemaschineModel(), 10, 3).Ok);
        }

        // ===================================================== Auskunft A bis D

        [Fact]
        public void Auskunft_rechnet_vier_Punkte_aus_dem_eigenen_Kennfeld()
        {
            var punkte = new[]
            {
                new KaeltemaschineTeillastDialogrechnung.Auskunftspunkt("A", 35, 1.0),
                new KaeltemaschineTeillastDialogrechnung.Auskunftspunkt("B", 25, 0.5),
                new KaeltemaschineTeillastDialogrechnung.Auskunftspunkt("C", 25, 0.1),
                new KaeltemaschineTeillastDialogrechnung.Auskunftspunkt("D", 45, 0.5)
            };
            KaeltemaschineTeillastDialogrechnung.Auskunftsergebnis e =
                KaeltemaschineTeillastDialogrechnung.TeillastpunkteAuskunft(Beispiel(), punkte);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(4, e.Zeilen.Count);

            // A: Luft 35 °C + 5 K = 40 °C, Volllast: Q 100 kW, P = 100 / 4 = 25 kW, EER 4.
            var a = e.Zeilen[0];
            Assert.Equal(40, a.RueckkuehlC, 9);
            Assert.Equal(100, a.KaelteKw, 9);
            Assert.Equal(1.0, a.LastgradMaschine, 9);
            Assert.Equal(25, a.LeistungsaufnahmeKw, 9);
            Assert.Equal(4, a.Eer, 9);
            Assert.False(a.Takt);

            // B: 30 °C Rückkühlung, Q_av 120 kW, EER 5; Last 50 kW, Lastgrad 50/120: P = 50 · E(x) / (5 · x).
            var b = e.Zeilen[1];
            double x = 50.0 / 120.0;
            Assert.Equal(x, b.LastgradMaschine, 9);
            Assert.Equal(50 * E(x) / (5 * x), b.LeistungsaufnahmeKw, 9);
            Assert.Equal(9.65, b.LeistungsaufnahmeKw, 3);

            // C: Last 10 kW unter P_min 20 kW — die Maschine taktet: g(0,2) = 0,862; P0 = 2,32; f = 0,5/0,55.
            var c = e.Zeilen[2];
            Assert.True(c.Takt);
            Assert.Equal(10 / (5 * (0.2 / 0.232)) / (0.5 / 0.55), c.LeistungsaufnahmeKw, 9);

            // D: 50 °C Rückkühlung liegt über dem Kennfeld: Randwert.
            Assert.True(e.Zeilen[3].Randwert);
            Assert.Equal(100, e.Zeilen[3].VerfuegbarKw, 9);
        }

        [Fact]
        public void Auskunft_folgt_dem_Randweg_Guetegrad()
        {
            var p = new[] { new KaeltemaschineTeillastDialogrechnung.Auskunftspunkt("D", 45, 1.0) };
            double randwert = KaeltemaschineTeillastDialogrechnung.TeillastpunkteAuskunft(Beispiel(), p).Zeilen[0].Eer;
            double guete = KaeltemaschineTeillastDialogrechnung
                .TeillastpunkteAuskunft(Beispiel(KaeltemaschineTeillastSchema.RANDWEG_GUETEGRAD), p).Zeilen[0].Eer;
            Assert.Equal(4.0, randwert, 9);
            Assert.Equal(4.0 / KaeltemaschinenRand.CarnotEer(7, 40) * KaeltemaschinenRand.CarnotEer(7, 50), guete, 9);
            Assert.True(guete < randwert);
        }

        [Fact]
        public void Auskunft_lehnt_benannt_ab()
        {
            var gut = new[] { new KaeltemaschineTeillastDialogrechnung.Auskunftspunkt("A", 35, 1.0) };
            Assert.Contains("Kennfeld", KaeltemaschineTeillastDialogrechnung.TeillastpunkteAuskunft(new KaeltemaschineModel(), gut).Meldung);
            Assert.False(KaeltemaschineTeillastDialogrechnung.TeillastpunkteAuskunft(Beispiel(),
                new[] { new KaeltemaschineTeillastDialogrechnung.Auskunftspunkt("A", 35, 0) }).Ok);
            Assert.False(KaeltemaschineTeillastDialogrechnung.TeillastpunkteAuskunft(Beispiel(),
                new[] { new KaeltemaschineTeillastDialogrechnung.Auskunftspunkt("A", double.NaN, 0.5) }).Ok);
            Assert.Contains("höchstens vier", KaeltemaschineTeillastDialogrechnung.TeillastpunkteAuskunft(Beispiel(),
                Enumerable.Repeat(gut[0], 5).ToList()).Meldung);
            Assert.False(KaeltemaschineTeillastDialogrechnung.TeillastpunkteAuskunft(Beispiel(),
                Array.Empty<KaeltemaschineTeillastDialogrechnung.Auskunftspunkt>()).Ok);
        }
    }
}
