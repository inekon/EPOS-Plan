using System;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Zeitprogramm-Leser des Erzeugers</b> (AK2-1, <see cref="Anlagenzeitprogramm"/>): gültig, leer, 167
    /// Werte, keine Zahl, Faktor außerhalb 0 … 1 und der Wochenbeginn nach der Regel des Sollwertprofils.
    /// </summary>
    public sealed class AnlagenzeitprogrammTests
    {
        /// <summary>168 unterscheidbare Faktoren i/200 (0 … 0,835).</summary>
        private static string Stufen(int anzahl = 168)
            => string.Join(";", Enumerable.Range(0, anzahl).Select(i => (i / 200.0).ToString("0.###", CultureInfo.InvariantCulture)));

        [Fact]
        public void Ein_gueltiges_Programm_liefert_168_Faktoren_ab_Montag()
        {
            Anlagenzeitprogramm p = Anlagenzeitprogramm.Lesen(AnlagenfahrplanSchemaTests.Programm(), "WP");
            Assert.True(p.Gepflegt);
            Assert.Equal(168, p.Faktoren.Count);
            Assert.Equal(0, p.WochentagDesErstenTags);
            Assert.Equal(0.5, p.Faktor(0));             // Montag 00:00
            Assert.Equal(1.0, p.Faktor(6));             // Montag 06:00
            Assert.Equal(0.0, p.Faktor(6 * 24 + 12));   // Sonntag 12:00
            Assert.Equal(p.Faktor(30), p.Faktor(30 + 168));
            Assert.Equal(p.Faktor(8759), p.Faktor(8759 % 168));
            Assert.Null(Anlagenzeitprogramm.Pruefen(AnlagenfahrplanSchemaTests.Programm()));
            // Leerraum um einen Wert ist erlaubt, wie beim Sollwertprofil.
            Assert.True(Anlagenzeitprogramm.Lesen(" " + Stufen().Replace(";", " ; ") + " ").Gepflegt);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Leer_ist_immer_verfuegbar(string text)
        {
            Anlagenzeitprogramm p = Anlagenzeitprogramm.Lesen(text);
            Assert.False(p.Gepflegt);
            Assert.Same(Anlagenzeitprogramm.ImmerVerfuegbar, p);
            Assert.All(Enumerable.Range(0, 8760), h => Assert.Equal(1.0, p.Faktor(h)));
            Assert.Null(Anlagenzeitprogramm.Pruefen(text));
        }

        [Fact]
        public void Hundertsiebenundsechzig_Werte_sind_ein_benannter_Fehler()
        {
            var f = Assert.Throws<AnlagenzeitprogrammFehler>(() => Anlagenzeitprogramm.Lesen(Stufen(167), "WP 1"));
            Assert.Equal(AnlagenkopplungSchema.WochenprofilBefund.FalscheWertzahl, f.Befund);
            Assert.Contains("167", f.Message, StringComparison.Ordinal);
            Assert.Contains("WP 1", f.Message, StringComparison.Ordinal);
            Assert.Equal(AnlagenkopplungSchema.WochenprofilBefund.FalscheWertzahl,
                         Assert.Throws<AnlagenzeitprogrammFehler>(() => Anlagenzeitprogramm.Lesen(Stufen(169))).Befund);
            Assert.NotNull(Anlagenzeitprogramm.Pruefen(Stufen(167)));
        }

        [Fact]
        public void Keine_Zahl_und_Komma_sind_benannte_Fehler()
        {
            string[] teile = Stufen().Split(';');
            teile[9] = "x";
            var f = Assert.Throws<AnlagenzeitprogrammFehler>(() => Anlagenzeitprogramm.Lesen(string.Join(";", teile)));
            Assert.Equal(AnlagenkopplungSchema.WochenprofilBefund.KeineZahl, f.Befund);
            Assert.Equal(10, f.Stelle);
            teile[9] = "0,5";
            Assert.Equal(10, Assert.Throws<AnlagenzeitprogrammFehler>(() => Anlagenzeitprogramm.Lesen(string.Join(";", teile))).Stelle);
        }

        [Theory]
        [InlineData("1.01")]
        [InlineData("-0.01")]
        [InlineData("2")]
        public void Ein_Faktor_ausserhalb_null_bis_eins_ist_ein_benannter_Fehler(string wert)
        {
            string[] teile = Stufen().Split(';');
            teile[100] = wert;
            var f = Assert.Throws<AnlagenzeitprogrammFehler>(() => Anlagenzeitprogramm.Lesen(string.Join(";", teile)));
            Assert.True(f.AusserhalbBereich);
            Assert.Equal(101, f.Stelle);
        }

        [Fact]
        public void Die_Grenzen_null_und_eins_sind_zulaessig()
        {
            string[] teile = Stufen().Split(';');
            teile[0] = "0";
            teile[1] = "1";
            Anlagenzeitprogramm p = Anlagenzeitprogramm.Lesen(string.Join(";", teile));
            Assert.Equal(0.0, p.Faktor(0));
            Assert.Equal(1.0, p.Faktor(1));
        }

        /// <summary>
        /// Der Wochenbeginn folgt der Wochenendmaske wie beim Sollwertprofil: Beginnt das Jahr an einem Sonntag
        /// (Maske: Tag 0 Wochenende, Tag 1 nicht), liest Stunde 0 den Sonntagswert (Wochenstunde 144).
        /// </summary>
        [Fact]
        public void Der_Wochenbeginn_folgt_der_Wochenendmaske_des_Sollwertprofils()
        {
            Assert.Equal(0, Anlagenzeitprogramm.WochentagAusWochenende(Maske(0)));
            Assert.Equal(6, Anlagenzeitprogramm.WochentagAusWochenende(Maske(6)));
            Assert.Equal(3, Anlagenzeitprogramm.WochentagAusWochenende(Maske(3)));
            Assert.Equal(-1, Anlagenzeitprogramm.WochentagAusWochenende(new bool[365]));

            Anlagenzeitprogramm montag = Anlagenzeitprogramm.Lesen(Stufen());
            Anlagenzeitprogramm sonntag = Anlagenzeitprogramm.Lesen(Stufen(), null, 6);
            Assert.Equal(6, sonntag.WochentagDesErstenTags);
            Assert.Equal(0.0, montag.Faktor(0));
            Assert.Equal(144 / 200.0, sonntag.Faktor(0), 9);
            Assert.Equal(167 / 200.0, sonntag.Faktor(23), 9);
            Assert.Equal(0.0, sonntag.Faktor(24));                 // Montag 00:00 am zweiten Tag
            Assert.Equal(sonntag.Faktor(5), montag.ImKalender(6).Faktor(5));
            Assert.Throws<ArgumentOutOfRangeException>(() => montag.ImKalender(7));
        }

        /// <summary>Die Wochenendmaske eines Jahres, dessen erster Tag der Wochentag <paramref name="w0"/> ist (0 = Montag).</summary>
        private static bool[] Maske(int w0) => Enumerable.Range(0, 365).Select(d => (w0 + d) % 7 >= 5).ToArray();
    }
}
