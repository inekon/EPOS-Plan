using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Herkunftsprotokoll des Kerns als Zeilen der Karte „Herkunft"</b>
    /// (<c>ZapfprofilHuelle.Herkunftszeilen</c>; Umsetzungskonzept Zapfprofilgenerator N19):
    /// Der Kern hält das Protokoll als Kennung und Werte (<see cref="ZapfSatz"/>), die Hülle baut
    /// daraus die Sätze der Oberflächensprache — wie bei jedem Hinweis.
    ///
    /// <para><b>Was übersetzt wird:</b> der Vermerk (Satz des Kerns), der Stand
    /// (<see cref="Wertstatus"/>) und die Herkunftsart. <b>Was Daten bleibt:</b> der Feldname der
    /// Größe, der Zonenname, Regelwerk, Ausgabe und Katalogfassung.</para>
    ///
    /// <para>Ohne Datenbank: geprüft wird allein die Übersetzung. Werte erfunden.</para>
    /// </summary>
    public sealed class ZapfprofilHuelleHerkunftTests
    {
        private static readonly Provenienz QUELLE =
            new Provenienz("Testquelle", "2020", "TEST-1", Herkunftsart.Fiktiv);

        /// <summary>Ein erfundenes Protokoll mit allen vier Ständen und beiden Zonenlagen.</summary>
        private static Herkunftsprotokoll Protokoll()
        {
            var p = new Herkunftsprotokoll();
            p.Vermerken("Wohnen", ZapfFeld.BEZUGSMENGE, 20, "P", Wertstatus.Ueberschrieben, null);
            p.Vermerken("Wohnen", ZapfFeld.TAGESBEDARF, 100.5, "kWh/d", Wertstatus.Umgerechnet, QUELLE,
                        ZapfSatz.Neu("HERKUNFT_TEMPERATURFAKTOR", 50.0, 10.0, 60.0, 10.0));
            p.Vermerken("Wohnen", ZapfFeld.KALIBRIERFAKTOR, 1.25, "-", Wertstatus.Kalibriert, null);
            p.Vermerken("", ZapfFeld.ZIRKULATION_JAHRESVERLUST, 1200, "kWh", Wertstatus.Vorgabe, QUELLE);
            p.Vermerken("", ZapfFeld.MONATSFAKTOREN, null, "", Wertstatus.Vorgabe, QUELLE);
            return p;
        }

        /// <summary>
        /// Je Eintrag eine Zeile, in der Reihenfolge des Protokolls; Wert und Einheit zusammen, die
        /// Einheit „-" nicht angehängt, ein Eintrag ohne Wert mit leerer Wertspalte. Der Stand steht
        /// als Wort, die Zone des Projekts als „Projekt", die Quelle als Regelwerk, Katalogfassung
        /// und Herkunftsart; ohne Provenienz sagt sie, dass der Anwender den Wert gesetzt hat.
        /// </summary>
        [Fact]
        public void Jeder_Eintrag_wird_eine_Zeile_in_der_Reihenfolge_des_Protokolls()
        {
            using var _ = new Kulturvorrichtung("de-DE");
            List<ZapfprofilHerkunftZeile> zeilen = ZapfprofilHuelle.Herkunftszeilen(Protokoll().Abschrift());

            Assert.Equal(5, zeilen.Count);
            Assert.Equal(new[] { ZapfFeld.BEZUGSMENGE, ZapfFeld.TAGESBEDARF, ZapfFeld.KALIBRIERFAKTOR,
                                 ZapfFeld.ZIRKULATION_JAHRESVERLUST, ZapfFeld.MONATSFAKTOREN },
                         zeilen.Select(z => z.Groesse).ToArray());

            Assert.Equal("20 P", zeilen[0].Wert);
            Assert.Equal("Wohnen", zeilen[0].Zone);
            Assert.Equal("überschrieben", zeilen[0].Stand);
            Assert.Equal("Eingabe des Anwenders", zeilen[0].Quelle);
            Assert.Equal("", zeilen[0].Vermerk);

            Assert.Equal("100,5 kWh/d", zeilen[1].Wert);
            Assert.Equal("umgerechnet", zeilen[1].Stand);
            Assert.Equal("Testquelle 2020 · Katalogfassung TEST-1 · erfundener Wert", zeilen[1].Quelle);
            Assert.NotEqual("", zeilen[1].Vermerk);
            Assert.DoesNotContain("HERKUNFT_TEMPERATURFAKTOR", zeilen[1].Vermerk);

            // Die Einheit „-" ist dimensionslos und steht nicht hinter der Zahl.
            Assert.Equal("1,25", zeilen[2].Wert);
            Assert.Equal("kalibriert", zeilen[2].Stand);

            Assert.Equal("Projekt", zeilen[3].Zone);
            Assert.Equal("Vorgabe", zeilen[3].Stand);

            // Ein Eintrag ohne Wert: leere Wertspalte, der Rest steht.
            Assert.Equal("", zeilen[4].Wert);
            Assert.Equal("Projekt", zeilen[4].Zone);
        }

        /// <summary>
        /// In englischer Kultur stehen Stand, Quelle und Vermerk englisch, und die Zahl trägt den
        /// Punkt als Dezimalzeichen; der Feldname bleibt, wie der Kern ihn führt.
        /// </summary>
        [Fact]
        public void In_englischer_Kultur_stehen_Stand_Quelle_und_Vermerk_englisch()
        {
            List<ZapfprofilHerkunftZeile> deutsch;
            using (var _ = new Kulturvorrichtung("de-DE"))
                deutsch = ZapfprofilHuelle.Herkunftszeilen(Protokoll().Abschrift());

            using var __ = new Kulturvorrichtung("en-US");
            List<ZapfprofilHerkunftZeile> englisch = ZapfprofilHuelle.Herkunftszeilen(Protokoll().Abschrift());

            Assert.Equal("overridden", englisch[0].Stand);
            Assert.Equal("entered by the user", englisch[0].Quelle);
            Assert.Equal("converted", englisch[1].Stand);
            Assert.Equal("100.5 kWh/d", englisch[1].Wert);
            Assert.Equal("Testquelle 2020 · catalogue version TEST-1 · fictitious value", englisch[1].Quelle);
            Assert.Equal("Project", englisch[3].Zone);
            Assert.NotEqual(deutsch[1].Vermerk, englisch[1].Vermerk);

            // Der Feldname ist DATEN - in beiden Sprachen derselbe.
            Assert.Equal(deutsch.Select(z => z.Groesse), englisch.Select(z => z.Groesse));
        }

        /// <summary>Kein Protokoll und ein leeres Protokoll geben eine leere Liste — keine Ausnahme.</summary>
        [Fact]
        public void Ohne_Eintrag_kommt_eine_leere_Liste()
        {
            Assert.Empty(ZapfprofilHuelle.Herkunftszeilen(null));
            Assert.Empty(ZapfprofilHuelle.Herkunftszeilen(new Herkunftsprotokoll().Abschrift()));
        }
    }
}
