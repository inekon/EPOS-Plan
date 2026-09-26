using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace ZapfprofilValidierung.Tests
{
    /// <summary>
    /// <b>Das Bandkriterium nach dem Anwenderentscheid ZU35</b> im Werkzeug: Ab der Mindestzahl der
    /// Einheiten entscheidet das Band P95–P99,9 wie bisher grün oder rot; darunter ist das Band
    /// „nicht bewertbar" — gelb mit Grund, nie rot —, und ein Objekt, dessen übrige Kriterien grün
    /// sind, ist gelb. Der Sammelbericht nennt die Regel und zählt grün, gelb und rot. Die Befunde
    /// sind erfunden; nur Verhältniszahlen.
    /// </summary>
    public sealed class BandkriteriumTests
    {
        [Fact]
        public void Ein_nicht_bewertbares_Band_ist_gelb_mit_Grund_und_das_Objekt_gelb()
        {
            Objektbefund klein = Befund("K-1", 3, Spitzenlage.NichtBewertbar);
            klein.KriterienBilden();

            Kriterium band = klein.Kriterien[0];
            Assert.Equal("Band der Dauerlinie", band.Name);
            Assert.Equal(Ampel.Gelb, band.Ampel);
            Assert.Contains("Nicht bewertbar: 3 Einheiten, bewertet wird ab 10", band.Satz);
            Assert.All(klein.Kriterien.Skip(1), k => Assert.Equal(Ampel.Gruen, k.Ampel));
            Assert.Equal(Ampel.Gelb, klein.Gesamt);

            // Gegenproben: im Band grün, unter dem Band rot - die Mindestzahl ändert daran nichts.
            Objektbefund gross = Befund("G-1", 12, Spitzenlage.ImBand);
            gross.KriterienBilden();
            Assert.Equal(Ampel.Gruen, gross.Gesamt);
            Objektbefund darunter = Befund("G-2", 12, Spitzenlage.Unterhalb);
            darunter.KriterienBilden();
            Assert.Equal(Ampel.Rot, darunter.Kriterien[0].Ampel);
            Assert.Equal(Ampel.Rot, darunter.Gesamt);

            // Ein nicht bewertbares Band hebt eine rote Form nicht auf: rot bleibt rot.
            Objektbefund kleinRot = Befund("K-2", 3, Spitzenlage.NichtBewertbar);
            kleinRot.Formmass = 0.05;
            kleinRot.KriterienBilden();
            Assert.Equal(Ampel.Rot, kleinRot.Gesamt);
        }

        [Fact]
        public void Der_Sammelbericht_nennt_die_Regel_und_zaehlt_gruen_gelb_und_rot()
        {
            var befunde = new[]
            {
                Befund("K-1", 3, Spitzenlage.NichtBewertbar),
                Befund("K-2", 5, Spitzenlage.NichtBewertbar),
                Befund("G-1", 12, Spitzenlage.ImBand),
                Befund("G-2", 40, Spitzenlage.Oberhalb),
            };
            foreach (Objektbefund b in befunde) b.KriterienBilden();

            string text = Bericht.SammelMarkdown(befunde, "Probe (erfunden)", "");
            Assert.Contains("ab 10 Einheiten liegt die Messspitze im Band P95 bis P99.9", text, StringComparison.Ordinal);
            Assert.Contains("| gruen | 1 |", text, StringComparison.Ordinal);
            Assert.Contains("| gelb | 2 |", text, StringComparison.Ordinal);
            Assert.Contains("| rot | 1 |", text, StringComparison.Ordinal);
            Assert.Contains("Davon gelb, weil das Band nicht bewertbar ist und die übrigen Kriterien grün sind: 2.",
                            text, StringComparison.Ordinal);
        }

        /// <summary>Ein erfundener Befund mit grüner Form und genauer Energie; das Band nach <paramref name="lage"/>.</summary>
        private static Objektbefund Befund(string kennung, int einheiten, Spitzenlage lage)
            => new Objektbefund
            {
                Kennung = kennung,
                Einheiten = einheiten,
                MindestEinheiten = 10,
                Lage = lage,
                Spitzenverhaeltnis = 0.9,
                BandUnten = 0.8,
                BandOben = 0.97,
                PerzentilUnten = 0.95,
                PerzentilOben = 0.999,
                Formmass = 0.005,
                Formschwelle = 0.01,
                EnergieResiduum = 0.0
            };
    }
}
