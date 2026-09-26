using System;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace ZapfprofilValidierung.Tests
{
    /// <summary>
    /// <b>Die Ergänzungen des zweiten Laufs an offenen Messreihen</b> (Validierungsbericht, Folgen V1
    /// bis V3): die Herkunft der Bezugsmenge, die Auswahl der Objekte für die √N-Skalierung und die
    /// Analyse „Band je Größenklasse". Jede Probe mit Gegenprobe.
    /// </summary>
    public sealed class ZweiterLaufTests
    {
        [Fact]
        public void Die_Herkunft_der_Bezugsmenge_wird_gelesen_und_ein_fremdes_Wort_benannt_abgelehnt()
        {
            string ordner = Path.Combine(Path.GetTempPath(), "zval-herkunft-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);
            try
            {
                string pfad = Path.Combine(ordner, "objekt.json");
                File.WriteAllText(pfad, Json("\"Abgeleitet\""));
                Objektbeschreibung o = Objektbeschreibung.Lesen(pfad, out string fehler);
                Assert.Null(fehler);
                Assert.Equal(Bezugsmengenherkunft.Abgeleitet, o.HerkunftWert);

                File.WriteAllText(pfad, Json("null"));
                Assert.Null(Objektbeschreibung.Lesen(pfad, out _).HerkunftWert);

                File.WriteAllText(pfad, Json("\"Geschaetzt\""));
                Assert.Null(Objektbeschreibung.Lesen(pfad, out fehler));
                Assert.Contains("bezugsmenge_herkunft", fehler);
            }
            finally { Directory.Delete(ordner, true); }
        }

        [Fact]
        public void Die_Wurzel_N_Skalierung_laesst_Platzhalter_und_unbekannte_Mengen_weg()
        {
            // Drei belastbare Objekte liegen genau auf der Steigung -0,5; zwei Platzhalter weit daneben.
            var befunde = new[]
            {
                Befund("A", 4, 1.0 / 2.0, Bezugsmengenherkunft.Veroeffentlichung),
                Befund("B", 16, 1.0 / 4.0, Bezugsmengenherkunft.Abgeleitet),
                Befund("C", 64, 1.0 / 8.0, null),
                Befund("D", 3, 8.0, Bezugsmengenherkunft.Unbekannt),
                Befund("E", 100, 5.0, Bezugsmengenherkunft.Platzhalter),
            };

            Kriterium k = Sammelkriterium.Bilden(befunde);
            Assert.Equal(Ampel.Gruen, k.Ampel);
            Assert.Equal(-0.5, k.Mass.Value, 9);
            Assert.Contains("2 Objekte mit Platzhalter", k.Satz);

            // Gegenprobe: über alle Objekte liegt die Steigung weit weg.
            (double? alle, int n) = Sammelkriterium.SteigungAlle(befunde);
            Assert.Equal(5, n);
            Assert.True(Math.Abs(alle.Value + 0.5) > 0.25);
        }

        [Fact]
        public void Die_Bandanalyse_nennt_das_Perzentil_der_Messspitze_und_die_Lage_im_Ensemble()
        {
            // 8760 Stunden 1 bis 8760: Die Messspitze bei 0,9 der Rechenspitze trifft das Perzentil 0,9.
            var werte = Enumerable.Range(1, 8760).Select(i => (double)i).ToArray();
            var reihe = new Bilanzreihe(werte);
            Objektbefund b = Befund("X", 50, 0.9, null);
            Bandanalyse.Objekt(b, reihe, new[] { 10.0, 8.0, 12.0, 9.5 });

            Assert.Equal(0.9, b.MessspitzePerzentil.Value, 3);
            Assert.Equal(0.8, b.EnsembleUnten.Value, 9);
            Assert.Equal(1.2, b.EnsembleOben.Value, 9);
            Assert.Equal(0.25, b.EnsembleAnteilDarunter.Value, 9);   // nur 0,8 liegt darunter

            // Gegenprobe: über der Rechenspitze ist das Perzentil 1, ohne Ensemble bleibt der Teil leer.
            Objektbefund ueber = Befund("Y", 3, 2.0, null);
            Bandanalyse.Objekt(ueber, reihe, new double[0]);
            Assert.Equal(1.0, ueber.MessspitzePerzentil.Value, 9);
            Assert.Null(ueber.EnsembleUnten);

            string text = Bandanalyse.Markdown(new[] { b, ueber });
            Assert.Contains("N < 10", text);
            Assert.Contains("10 ≤ N < 100", text);
        }

        private static Objektbefund Befund(string kennung, int n, double spitze, Bezugsmengenherkunft? herkunft)
            => new Objektbefund { Kennung = kennung, Einheiten = n, Spitzenverhaeltnis = spitze, Herkunft = herkunft };

        private static string Json(string herkunft)
            => "{ \"kennung\": \"T-1\", \"nutzungsart\": \"Wohnen\", \"bezugsmenge\": 10, "
               + "\"bezugsmenge_herkunft\": " + herkunft + ", \"kalender\": { \"wochentag_jan1\": 0 } }";
    }
}
