using System;
using System.Globalization;
using System.Threading;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die drei Warnstufen und das Lagebild des Banners</b> (Welle iF30, Konzept
    /// „Zeitlich beschränkte Lizenzierung" § 6).
    ///
    /// <para>Der Zwilling zu <see cref="LizenzZustandTests"/>: Dort geht es um die SECHS
    /// Zustände, hier um das, was der Anwender davon SIEHT — 30, 14 und 7 Tage vor dem
    /// Ablauf ein Hinweis, danach das dauerhafte Lesemodus-Banner.</para>
    ///
    /// <para><b>Ohne Ablage und ohne Uhr.</b> Wie dort rechnet jeder Fall gegen einen
    /// festen Tag; <c>LizenzManager.Pruefe()</c> kommt nicht vor, der Zeitanker des
    /// Entwicklerrechners bleibt unberührt (Risiko R-W15c-3).</para>
    /// </summary>
    public class LizenzWarnstufenTests
    {
        private const string GERAET = "SHA256:AAAA";
        private static readonly DateTime Heute = new DateTime(2026, 9, 6);

        // ==================================================================
        //  1-4  Restlaufzeit und Stufe
        // ==================================================================

        /// <summary>Fall 1: Ohne Token und ohne Frist gibt es keine Restlaufzeit.</summary>
        [Fact]
        public void Ohne_Frist_gibt_es_keine_Restlaufzeit()
        {
            Assert.Null(LizenzManager.RestTage(null, Heute));
            Assert.Equal(0, LizenzManager.Warnstufe(null, Heute));

            LizenzToken unbefristet = LizenzToken.FuerPruefstand(GERAET, gueltigBis: null,
                                                                 kulanzTage: 14, tokenBis: Heute.AddDays(30));
            Assert.Null(LizenzManager.RestTage(unbefristet, Heute));
            Assert.Equal(0, LizenzManager.Warnstufe(unbefristet, Heute));
        }

        /// <summary>
        /// Fall 2: Die Restlaufzeit zählt TAGE bis <c>gueltig_bis</c> — gemessen an der
        /// LIZENZLAUFZEIT und nicht an der Offline-Leine, die sich bei jeder stillen
        /// Nachprüfung von selbst erneuert.
        /// </summary>
        [Theory]
        [InlineData(45)]
        [InlineData(30)]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-3)]
        public void Die_Restlaufzeit_zaehlt_Tage_bis_zum_Ablauf(int tage)
        {
            LizenzToken t = Token(gueltigInTagen: tage);
            Assert.Equal(tage, LizenzManager.RestTage(t, Heute));
        }

        /// <summary>
        /// Fall 3: <b>Die drei Ränder sind die Fachaussage.</b> 31 Tage sind noch keine
        /// Stufe, 30 sind die erste; 15/14 und 8/7 ebenso. Der Tag des Ablaufs selbst
        /// (0 Tage) gehört noch zur dritten Stufe — an ihm ist die Lizenz gültig.
        /// </summary>
        [Theory]
        [InlineData(60, 0)]
        [InlineData(31, 0)]
        [InlineData(30, 30)]
        [InlineData(15, 30)]
        [InlineData(14, 14)]
        [InlineData(8, 14)]
        [InlineData(7, 7)]
        [InlineData(1, 7)]
        [InlineData(0, 7)]
        public void Die_drei_Warnstufen_haben_scharfe_Raender(int tage, int erwartet)
        {
            Assert.Equal(erwartet, LizenzManager.Warnstufe(Token(tage), Heute));
        }

        /// <summary>
        /// Fall 4: <b>Nach dem Ablauf gibt es keine Warnstufe mehr</b>, sondern einen
        /// ZUSTAND — Kulanz oder Lesemodus. Die Stufen warnen VOR dem Ablauf.
        /// </summary>
        [Fact]
        public void Nach_dem_Ablauf_gibt_es_keine_Warnstufe()
        {
            Assert.Equal(0, LizenzManager.Warnstufe(Token(-1), Heute));
            Assert.Equal(0, LizenzManager.Warnstufe(Token(-30), Heute));
        }

        // ==================================================================
        //  5-9  Das Lagebild
        // ==================================================================

        /// <summary>
        /// Fall 5: Eine gültige Lizenz fern vom Ablauf meldet NICHTS — kein Banner, keine
        /// Dringlichkeit. Ein Programm, das den ganzen Tag warnt, wird nicht gelesen.
        /// </summary>
        [Fact]
        public void Eine_gueltige_Lizenz_meldet_nichts()
        {
            LizenzLage lage = LizenzLage.Bilden(LizenzStatus.Gueltig, Token(90), Heute);

            Assert.Equal(LizenzDringlichkeit.Keine, lage.Dringlichkeit);
            Assert.Equal("", lage.Text);
            Assert.False(lage.Lesemodus);
            Assert.Equal(0, lage.Warnstufe);
        }

        /// <summary>
        /// Fall 6: Der LESEMODUS schlägt alles — dauerhaftes Banner, Warnstufe egal. Er
        /// ist der Zustand, den der Anwender beheben MUSS und sonst nicht sieht.
        /// </summary>
        [Theory]
        [InlineData(LizenzStatus.Lesemodus)]
        [InlineData(LizenzStatus.NichtAktiviert)]
        [InlineData(LizenzStatus.UhrManipuliert)]
        public void Der_Lesemodus_traegt_ein_dauerhaftes_Banner(LizenzStatus status)
        {
            LizenzLage lage = LizenzLage.Bilden(status, Token(-40), Heute);

            Assert.True(lage.Lesemodus);
            Assert.Equal(LizenzDringlichkeit.Warnung, lage.Dringlichkeit);
            Assert.Equal(Resource.LIZ_BANNER_LESEMODUS, lage.Text);
        }

        /// <summary>
        /// Fall 7: Die zwei ersten Stufen sind ein HINWEIS, die dritte eine WARNUNG — und
        /// beide nennen die Zahl der Tage samt Datum.
        /// </summary>
        [Theory]
        [InlineData(30, LizenzDringlichkeit.Hinweis)]
        [InlineData(14, LizenzDringlichkeit.Hinweis)]
        [InlineData(7, LizenzDringlichkeit.Warnung)]
        [InlineData(3, LizenzDringlichkeit.Warnung)]
        public void Die_Warnstufen_nennen_Tage_und_Datum(int tage, LizenzDringlichkeit erwartet)
        {
            LizenzToken t = Token(tage);
            LizenzLage lage = LizenzLage.Bilden(LizenzStatus.Gueltig, t, Heute);

            Assert.False(lage.Lesemodus);
            Assert.Equal(erwartet, lage.Dringlichkeit);
            Assert.Equal(tage, lage.RestTage);
            Assert.Contains(tage.ToString(CultureInfo.CurrentCulture), lage.Text);
            Assert.Contains(Heute.AddDays(tage).ToString("dd.MM.yyyy", CultureInfo.CurrentCulture), lage.Text);
        }

        /// <summary>
        /// Fall 8: Ein Tag und der Ablauftag selbst bekommen EIGENE Sätze — „in 1 Tagen"
        /// und „in 0 Tagen" wären falsches Deutsch, und die Zeile steht dem Anwender
        /// prominent vor Augen.
        /// </summary>
        [Fact]
        public void Der_letzte_Tag_bekommt_einen_eigenen_Satz()
        {
            Assert.Equal(
                string.Format(CultureInfo.CurrentCulture, Resource.LIZ_BANNER_ABLAUF_EIN,
                              Heute.AddDays(1).ToString("dd.MM.yyyy", CultureInfo.CurrentCulture)),
                LizenzLage.Bilden(LizenzStatus.Gueltig, Token(1), Heute).Text);

            Assert.Equal(
                string.Format(CultureInfo.CurrentCulture, Resource.LIZ_BANNER_ABLAUF_HEUTE,
                              Heute.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture)),
                LizenzLage.Bilden(LizenzStatus.Gueltig, Token(0), Heute).Text);
        }

        /// <summary>
        /// Fall 9: Kulanzfenster und fällige Nachprüfung sind ein HINWEIS mit voller
        /// Funktion — der Text ist die Statuszeile, die die Lizenzverwaltung ohnehin führt
        /// (Konzept § 6, Zeile „Ablauf bis +14 Tage").
        /// </summary>
        [Fact]
        public void Kulanz_und_Nachpruefung_sind_ein_Hinweis_mit_voller_Funktion()
        {
            LizenzToken t = Token(-3);

            LizenzLage kulanz = LizenzLage.Bilden(LizenzStatus.Kulanz, t, Heute);
            Assert.False(kulanz.Lesemodus);
            Assert.Equal(LizenzDringlichkeit.Hinweis, kulanz.Dringlichkeit);
            Assert.NotEqual("", kulanz.Text);

            LizenzLage nach = LizenzLage.Bilden(LizenzStatus.NachpruefungFaellig, Token(60), Heute);
            Assert.False(nach.Lesemodus);
            Assert.Equal(LizenzDringlichkeit.Hinweis, nach.Dringlichkeit);
            Assert.Equal(Resource.LIZ_ST_NACHPRUEFUNG, nach.Text);
        }

        // ==================================================================
        //  10  Beide Sprachen
        // ==================================================================

        /// <summary>
        /// Fall 10: Die vier Bannertexte stehen in BEIDEN Sprachen im Katalog und sind
        /// verschieden — ein fehlender englischer Schlüssel fiele sonst erst beim Anwender
        /// auf, weil <c>ResourceManager</c> still auf die neutrale Fassung zurückfällt.
        /// </summary>
        [Fact]
        public void Die_Bannertexte_stehen_in_beiden_Sprachen()
        {
            CultureInfo vorher = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
                string deLesemodus = Resource.LIZ_BANNER_LESEMODUS;
                string deSperre = Resource.LIZ_LESEMODUS_SPERRE;
                string deSim = Resource.SIM_MSG_LESEMODUS;
                string deAblauf = Resource.LIZ_BANNER_ABLAUF;

                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                Assert.NotEqual(deLesemodus, Resource.LIZ_BANNER_LESEMODUS);
                Assert.NotEqual(deSperre, Resource.LIZ_LESEMODUS_SPERRE);
                Assert.NotEqual(deSim, Resource.SIM_MSG_LESEMODUS);
                Assert.NotEqual(deAblauf, Resource.LIZ_BANNER_ABLAUF);

                // Der Platzhaltersatz muss in beiden Sprachen dieselben Stellen fuehren -
                // sonst wirft string.Format beim Anwender.
                Assert.Contains("{0}", Resource.LIZ_BANNER_ABLAUF);
                Assert.Contains("{1}", Resource.LIZ_BANNER_ABLAUF);
                Assert.Contains("{0}", Resource.LIZ_BANNER_ABLAUF_EIN);
                Assert.Contains("{0}", Resource.LIZ_BANNER_ABLAUF_HEUTE);
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = vorher;
            }
        }

        // ==================================================================

        /// <summary>Ein Token, das in <paramref name="gueltigInTagen"/> Tagen abläuft.</summary>
        private static LizenzToken Token(int gueltigInTagen)
        {
            return LizenzToken.FuerPruefstand(GERAET,
                                              gueltigBis: Heute.AddDays(gueltigInTagen),
                                              kulanzTage: 14,
                                              tokenBis: Heute.AddDays(gueltigInTagen));
        }
    }
}
