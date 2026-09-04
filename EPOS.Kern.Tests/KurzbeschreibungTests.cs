using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="Kurzbeschreibung"/> nach iU9-W15b.0e — der Zeuge T-7 zur Auflage H-1.
    ///
    /// <para><b>Warum das geprueft wird.</b> Die Rechnung lag seit H11 (7.6) als
    /// <c>internal static BeschreibungUmbrechen</c> in <c>Form_HelpPopup</c>, und ihr
    /// Kommentarkopf versprach ausdruecklich: „internal statt private, damit der
    /// Pruefstand die Kappung ohne Bildschirm nachrechnen kann". Einen Pruefstand gab es
    /// nie (Befund W15b-B18). Weil <c>Form_HelpPopup</c> als einzige Maske des Pakets
    /// weder umgestellt noch geloescht wird (Entscheid E-2), waere die Zusage sonst bis
    /// iU11 uneingeloest geblieben.</para>
    ///
    /// <para>Fuenf Faelle: leer, kurz genug, Umbruch an der Wortgrenze, Kappung mit
    /// Auslassungszeichen, ueberlanges Einzelwort. Dazu die Zusage, dass mehrfacher
    /// Leerraum vorher eingeebnet wird.</para>
    /// </summary>
    public class KurzbeschreibungTests
    {
        /// <summary>Die beiden Masse sind Teil der Zusage und stehen deshalb im Zeugen.</summary>
        [Fact]
        public void Siebzig_Zeichen_und_zwei_Zeilen()
        {
            Assert.Equal(70, Kurzbeschreibung.ZEICHEN);
            Assert.Equal(2, Kurzbeschreibung.ZEILEN);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\r\n\t ")]
        public void Ohne_Text_bleibt_es_leer(string eingabe)
        {
            Assert.Equal("", Kurzbeschreibung.Umbrechen(eingabe));
        }

        /// <summary>
        /// Was in eine Zeile passt, bleibt eine Zeile — ohne Auslassungszeichen und ohne
        /// Umbruch. Der Katalog liefert den Normalfall genau so.
        /// </summary>
        [Fact]
        public void Kurzer_Text_bleibt_unveraendert()
        {
            const string text = "Die Waermepumpe der Anlage.";

            Assert.Equal(text, Kurzbeschreibung.Umbrechen(text));
        }

        /// <summary>
        /// Vorhandene Zeilenumbrueche und Mehrfachleerzeichen werden VOR der Rechnung
        /// eingeebnet — sonst braechte ein Umbruch aus einer alten Sicherung die
        /// Zeilenzaehlung durcheinander.
        /// </summary>
        [Fact]
        public void Leerraum_wird_eingeebnet()
        {
            Assert.Equal("Ein Satz mit Luft.",
                         Kurzbeschreibung.Umbrechen("  Ein\r\nSatz   mit\tLuft.  "));
        }

        /// <summary>
        /// Ueber 70 Zeichen wird an der WORTGRENZE auf zwei Zeilen gebrochen, verbunden
        /// mit CRLF. Beide Zeilen bleiben unter der Ziellaenge, und es wird nichts
        /// gekappt — es passt ja.
        /// </summary>
        [Fact]
        public void Ueber_siebzig_Zeichen_bricht_an_der_Wortgrenze_auf_zwei_Zeilen()
        {
            // 12 x "Wortfolge" (9 Zeichen) + Leerzeichen = 119 Zeichen: mehr als eine,
            // weniger als zwei volle Zeilen.
            string text = string.Join(" ", new string[12].Select(_ => "Wortfolge"));

            string umbrochen = Kurzbeschreibung.Umbrechen(text);
            string[] zeilen = umbrochen.Split(new[] { "\r\n" }, StringSplitOptions.None);

            Assert.Equal(2, zeilen.Length);
            Assert.All(zeilen, z => Assert.True(z.Length <= Kurzbeschreibung.ZEICHEN,
                                                "Zeile zu lang: " + z));
            Assert.DoesNotContain("…", umbrochen);
            // Kein Wort geht verloren und keins wird getrennt.
            Assert.Equal(12, umbrochen.Replace("\r\n", " ").Split(' ').Length);
        }

        /// <summary>
        /// Was nach zwei Zeilen uebrig bleibt, faellt weg — und das muss man sehen:
        /// die letzte Zeile endet mit dem Auslassungszeichen.
        /// </summary>
        [Fact]
        public void Zu_langer_Text_wird_nach_zwei_Zeilen_gekappt()
        {
            string text = string.Join(" ", new string[40].Select(_ => "Wortfolge"));

            string umbrochen = Kurzbeschreibung.Umbrechen(text);
            string[] zeilen = umbrochen.Split(new[] { "\r\n" }, StringSplitOptions.None);

            Assert.Equal(Kurzbeschreibung.ZEILEN, zeilen.Length);
            Assert.EndsWith("…", umbrochen);
            Assert.DoesNotContain("…", zeilen[0]);
        }

        /// <summary>
        /// Ein einzelnes ueberlanges Wort wird NICHT getrennt. Getrennte Fachwoerter
        /// waeren schlimmer als eine zu lange Zeile — die Randklemmung des Popups holt
        /// die Breite ohnehin wieder auf den Bildschirm.
        /// </summary>
        [Fact]
        public void Ueberlanges_Einzelwort_bleibt_ganz()
        {
            string wort = new string('W', 120);

            Assert.Equal(wort, Kurzbeschreibung.Umbrechen(wort));
        }
    }
}
