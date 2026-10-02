using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Lesbarmachung der Hilfeziele (<see cref="Hilfeziel"/>; Konzept
    /// Technikdokumentation, Abschnitt 7): Ein Seitenpfad der Rubrik Grundlagen wird zu
    /// „Grundlagen: &lt;Titel&gt;", ein Kurzname der Rubrik „Programm Dokumentation" zu
    /// seinem Kapitelnamen. Windows (Hilfekatalog) und iOS (Hilfedienst) zeigen
    /// denselben Text; beide rufen diese eine Funktion.
    ///
    /// <para>Pinnt <c>de-DE</c> (<see cref="Kulturvorrichtung"/>): Das Muster „Grundlagen:
    /// {0}" kommt aus der Ressource und folgt <c>CurrentUICulture</c>.</para>
    /// </summary>
    public sealed class HilfezielTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =================================================================
        //  Seitentitel
        // =================================================================

        [Theory]
        [InlineData("/wiki/Grundlagen/Kessel_und_Spitzenlast#kennzahlen", "Grundlagen/Kessel und Spitzenlast")]
        [InlineData("/wiki/Grundlagen/Wärmepumpe", "Grundlagen/Wärmepumpe")]
        [InlineData("/wiki/Grundlagen/Wärmequelle_Erdreich", "Grundlagen/Wärmequelle Erdreich")]
        [InlineData("  /WIKI/Grundlagen/BHKW  ", "Grundlagen/BHKW")]
        [InlineData("https://wiki.epos-plan.de/wiki/Grundlagen/W%C3%A4rmepumpe", "Grundlagen/Wärmepumpe")]
        [InlineData("https://wiki.epos-plan.de/wiki/Grundlagen/K%C3%BChlung#kaelte", "Grundlagen/Kühlung")]
        [InlineData("/wiki/Programm_Dokumentation/Berechnung/Photovoltaik", "Programm Dokumentation/Berechnung/Photovoltaik")]
        public void Der_Seitentitel_steht_hinter_wiki_dekodiert_und_ohne_Anker(string ziel, string erwartet)
        {
            Assert.Equal(erwartet, Hilfeziel.Seitentitel(ziel));
        }

        /// <summary>Ein Kurzname der Rubrik ist kein Seitenpfad — er hat keinen Seitentitel.</summary>
        [Theory]
        [InlineData("Pufferspeicher#schwellen")]
        [InlineData("Berechnung/Wärmepumpe#rechenweg")]
        [InlineData("Grundlagen/Wärmepumpe")]
        [InlineData("https://wiki.epos-plan.de/index.php?title=Grundlagen/BHKW")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Ein_Kurzname_hat_keinen_Seitentitel(string ziel)
        {
            Assert.Equal("", Hilfeziel.Seitentitel(ziel));
            Assert.False(Hilfeziel.IstGrundlagenseite(ziel));
        }

        [Theory]
        [InlineData("/wiki/Grundlagen/Kühlung", true)]
        [InlineData(" /wiki/Grundlagen/Wechselrichter#mpp", true)]
        [InlineData("Kühlung#eingaben", false)]
        [InlineData("Berechnung/Photovoltaik#wechselrichter", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Ein_Pfadziel_beginnt_mit_wiki(string ziel, bool erwartet)
        {
            Assert.Equal(erwartet, Hilfeziel.IstPfadziel(ziel));
        }

        [Theory]
        [InlineData("/wiki/Grundlagen/Kühlung", true)]
        [InlineData("https://wiki.epos-plan.de/wiki/Grundlagen/Stromspeicher", true)]
        [InlineData("/wiki/Grundlagen", false)]
        [InlineData("/wiki/Grundlagen/", false)]
        [InlineData("/wiki/Programm_Dokumentation/Kühlung", false)]
        [InlineData("/wiki/Grundlagenseite/Kühlung", false)]
        public void Nur_eine_Unterseite_der_Rubrik_ist_eine_Grundlagenseite(string ziel, bool erwartet)
        {
            Assert.Equal(erwartet, Hilfeziel.IstGrundlagenseite(ziel));
        }

        // =================================================================
        //  Kurztext
        // =================================================================

        /// <summary>
        /// Der Kurztext eines Grundlagen-Ziels: das Muster der Ressource mit dem
        /// Seitentitel — lesbar statt des rohen Pfads, den iOS bis dahin zeigte.
        /// </summary>
        [Theory]
        [InlineData("/wiki/Grundlagen/Wärmepumpe", "Grundlagen: Wärmepumpe")]
        [InlineData("/wiki/Grundlagen/Kessel_und_Spitzenlast#kennzahlen", "Grundlagen: Kessel und Spitzenlast")]
        [InlineData("/wiki/Grundlagen/Wärmequelle_Erdreich", "Grundlagen: Wärmequelle Erdreich")]
        [InlineData("/wiki/Grundlagen/Kühlung", "Grundlagen: Kühlung")]
        [InlineData("https://wiki.epos-plan.de/wiki/Grundlagen/Solarkollektoren", "Grundlagen: Solarkollektoren")]
        [InlineData("https://wiki.epos-plan.de/wiki/Grundlagen/W%C3%A4rmequelle_Erdreich", "Grundlagen: Wärmequelle Erdreich")]
        public void Ein_Grundlagen_Ziel_liest_sich_als_Grundlagen_Titel(string ziel, string erwartet)
        {
            Assert.Equal(erwartet, Hilfeziel.Kurztext(ziel));
        }

        /// <summary>
        /// Ein Kurzname der Rubrik „Programm Dokumentation" zeigt seinen Kapitelnamen — wie
        /// der Windows-Hilfekatalog (<c>WikiHelpCatalog.Kapitelname</c>) und ohne Anker.
        /// Ein Seitenpfad in die Rubrik liest sich genauso.
        /// </summary>
        [Theory]
        [InlineData("Pufferspeicher", "Pufferspeicher")]
        [InlineData("Pufferspeicher#schwellen", "Pufferspeicher")]
        [InlineData("Berechnung/Wärmepumpe#rechenweg", "Berechnung: Wärmepumpe")]
        [InlineData("Berechnung/Wärmequelle Erdreich#rechenweg", "Berechnung: Wärmequelle Erdreich")]
        [InlineData("Kühlung#kuehlbetrieb", "Kühlung")]
        [InlineData("/wiki/Programm_Dokumentation/Pufferspeicher", "Pufferspeicher")]
        [InlineData("/wiki/Programm_Dokumentation/Berechnung/Photovoltaik#wechselrichter", "Berechnung: Photovoltaik")]
        [InlineData("/wiki/Hauptseite", "Hauptseite")]
        [InlineData("/wiki/Grundlagen", "Grundlagen")]
        public void Ein_Rubrikziel_liest_sich_als_Kapitelname(string ziel, string erwartet)
        {
            Assert.Equal(erwartet, Hilfeziel.Kurztext(ziel));
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(null)]
        public void Ohne_Ziel_bleibt_der_Kurztext_leer(string ziel)
        {
            Assert.Equal("", Hilfeziel.Kurztext(ziel));
        }

        /// <summary>
        /// Das Muster folgt der Oberflächensprache; der Seitentitel bleibt deutsch — das
        /// Wiki führt nur deutsche Seiten und übersetzt beim Öffnen (<c>DokuUebersetzung</c>).
        /// </summary>
        [Fact]
        public void Unter_Englisch_folgt_das_Muster_der_Oberflaechensprache()
        {
            using var englisch = new Kulturvorrichtung("en-US");

            Assert.Equal("Fundamentals: Wärmepumpe", Hilfeziel.Kurztext("/wiki/Grundlagen/Wärmepumpe"));
            Assert.Equal("Fundamentals: Kessel und Spitzenlast",
                         Hilfeziel.Kurztext("/wiki/Grundlagen/Kessel_und_Spitzenlast#kennzahlen"));
            Assert.Equal("Berechnung: Wärmepumpe", Hilfeziel.Kurztext("Berechnung/Wärmepumpe#rechenweg"));
        }

        /// <summary>
        /// Gegenprobe zur Ressource: Beide Sprachen tragen das Muster mit genau einer
        /// Stelle <c>{0}</c> — sonst fiele der Kurztext still auf den deutschen Rückfall.
        /// </summary>
        [Fact]
        public void Beide_Sprachen_tragen_das_Muster_mit_Platzhalter()
        {
            Assert.Equal("Grundlagen: {0}", WindowsFormsApplication1.MyResource.Resource.HILFE_GRUNDLAGEN_KURZTEXT);

            using var englisch = new Kulturvorrichtung("en-US");
            Assert.Equal("Fundamentals: {0}", WindowsFormsApplication1.MyResource.Resource.HILFE_GRUNDLAGEN_KURZTEXT);
        }

        /// <summary>
        /// <b>Die Adresse eines Ziels</b> (Verbesserungen 29.09.2026, B9): Ein Seitenpfad
        /// geht wie ein Kurzname je Segment durch <c>Uri.EscapeDataString</c> — ein Umlaut
        /// steht kodiert in der Adresse, der Schrägstrich der Unterseite bleibt, der Anker
        /// hängt hinten. Ein schon kodierter Pfad wird nicht doppelt kodiert.
        /// </summary>
        [Fact]
        public void Die_Seitenadresse_kodiert_auch_einen_Seitenpfad_je_Segment()
        {
            const string B = "https://wiki.example";

            Assert.Equal(B + "/wiki/Grundlagen/W%C3%A4rmepumpe#kennzahlen",
                         Hilfeziel.Seitenadresse(B, "/wiki/Grundlagen/Wärmepumpe#kennzahlen"));
            Assert.Equal(B + "/wiki/Grundlagen/W%C3%A4rmepumpe",
                         Hilfeziel.Seitenadresse(B, "/wiki/Grundlagen/W%C3%A4rmepumpe"));
            Assert.Equal(B + "/wiki/Grundlagen/Kessel_und_Spitzenlast",
                         Hilfeziel.Seitenadresse(B, "/wiki/Grundlagen/Kessel_und_Spitzenlast"));

            // Der Kurzname bekommt die Rubrik davor - wie bisher.
            Assert.Equal(B + "/wiki/Programm_Dokumentation/Berechnung/W%C3%A4rmepumpe#rechenweg",
                         Hilfeziel.Seitenadresse(B, "Berechnung/Wärmepumpe#rechenweg"));

            Assert.Equal("", Hilfeziel.Seitenadresse(B, ""));
            Assert.Equal("", Hilfeziel.Seitenadresse(B, "#nur-anker"));
        }
    }
}
