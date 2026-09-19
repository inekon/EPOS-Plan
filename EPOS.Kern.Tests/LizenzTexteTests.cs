using System;
using System.Globalization;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die sechs Zustands- und drei Typtexte des Lizenzkerns (iU9-W15c.3).
    ///
    /// <para><b>Warum sie umgezogen sind.</b> <c>LizenzManager.StatusText()</c> lieferte
    /// bis W15c sechs deutsche Sätze AUS DEM QUELLTEXT — der letzte unlokalisierte
    /// Anwendertext des Lizenzwegs, gelesen an drei Stellen (Lizenzverwaltung, Fußzeile
    /// des Lizenzdialogs, Ablehnungsmeldung des KI-Assistenten). Dasselbe galt für die
    /// drei Lizenztypen. Seit W15c.3 kommen sie aus
    /// <c>MyResource.Resource.LIZ_ST_*</c> bzw. <c>LIZ_TYP_*</c>.</para>
    ///
    /// <para><b>Die Sprache wird im Rumpf gepinnt, mit <c>finally</c></b> (Regel seit
    /// W8): Der Ressourcenmanager liest <c>CurrentUICulture</c>, und ein Gesamtlauf
    /// unter <c>LANG=en_US</c> fände sonst die englischen Werte.</para>
    /// </summary>
    public class LizenzTexteTests
    {
        private static void MitSprache(string kuerzel, Action fall)
        {
            CultureInfo vorher = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(kuerzel);
                fall();
            }
            finally
            {
                CultureInfo.CurrentUICulture = vorher;
            }
        }

        private const string GERAET = "SHA256:AAAA";
        private static readonly DateTime Heute = new DateTime(2026, 9, 4);

        /// <summary>
        /// Zu jedem der sechs Zustände gehört genau ein Schlüssel — und der Text ist
        /// nicht leer. Das ist die zweite Hälfte des Wellennachweises: Die Zustände sind
        /// nicht nur gerechnet, sie sind auch benannt.
        /// </summary>
        [Fact]
        public void Zu_jedem_Zustand_gehoert_genau_ein_Text()
        {
            MitSprache("de-DE", () =>
            {
                var gueltig = LizenzToken.FuerPruefstand(GERAET, gueltigBis: Heute.AddDays(90),
                                                         kulanzTage: 14, typ: "firma");
                var abgelaufen = LizenzToken.FuerPruefstand(GERAET, gueltigBis: Heute.AddDays(-1),
                                                            kulanzTage: 14, typ: "firma");

                Assert.Equal("Firmenlizenz · gültig bis 03.12.2026",
                             LizenzManager.StatusText(LizenzStatus.Gueltig, gueltig));
                Assert.Equal("Lizenz am 03.09.2026 abgelaufen — Kulanzfenster läuft, bitte verlängern.",
                             LizenzManager.StatusText(LizenzStatus.Kulanz, abgelaufen));
                Assert.Equal("Online-Nachprüfung fällig — bitte einmal mit Internetverbindung starten.",
                             LizenzManager.StatusText(LizenzStatus.NachpruefungFaellig, gueltig));
                Assert.Equal("Lizenz abgelaufen — Lesemodus (Projekte ansehen und exportieren).",
                             LizenzManager.StatusText(LizenzStatus.Lesemodus, gueltig));
                Assert.Equal("Die Systemuhr wurde zurückgestellt — bitte Uhrzeit korrigieren oder online nachprüfen.",
                             LizenzManager.StatusText(LizenzStatus.UhrManipuliert, null));
                // ANWENDERENTSCHEID MN-1 (19.09.2026): Der Weg heisst „Hilfe →
                // Lizenz". Der Menuepunkt „Administration → Lizenz…" ist
                // entfallen; der Lizenzdialog unter Hilfe fuehrt die Verwaltung
                // als Reiter „Status & Aktivierung".
                Assert.Equal("Nicht aktiviert — Testversion oder Lizenzschlüssel unter Hilfe → Lizenz.",
                             LizenzManager.StatusText(LizenzStatus.NichtAktiviert, null));
            });
        }

        /// <summary>
        /// Dieselben neun Schlüssel gibt es in BEIDEN Sprachen, und keiner fällt auf
        /// den deutschen Wert zurück. Der englische Zweig war bis W15c gar nicht
        /// vorhanden — es gab keine Schlüssel.
        /// </summary>
        [Theory]
        [InlineData("LIZ_ST_GUELTIG")]
        [InlineData("LIZ_ST_KULANZ")]
        [InlineData("LIZ_ST_NACHPRUEFUNG")]
        [InlineData("LIZ_ST_LESEMODUS")]
        [InlineData("LIZ_ST_UHR")]
        [InlineData("LIZ_ST_NICHTAKTIVIERT")]
        [InlineData("LIZ_TYP_DEMO")]
        [InlineData("LIZ_TYP_PERSON")]
        [InlineData("LIZ_TYP_FIRMA")]
        public void Jeder_Schluessel_steht_in_beiden_Sprachen(string schluessel)
        {
            string deutsch = null, englisch = null;
            MitSprache("de-DE", () => deutsch = Resource.ResourceManager.GetString(schluessel));
            MitSprache("en-US", () => englisch = Resource.ResourceManager.GetString(schluessel));

            Assert.False(string.IsNullOrWhiteSpace(deutsch), schluessel + " fehlt auf Deutsch.");
            Assert.False(string.IsNullOrWhiteSpace(englisch), schluessel + " fehlt auf Englisch.");
            Assert.NotEqual(deutsch, englisch);
        }

        /// <summary>
        /// Die zwei Formatschlüssel tragen ihre Platzhalter in beiden Sprachen —
        /// <c>LIZ_ST_GUELTIG</c> zwei, <c>LIZ_ST_KULANZ</c> einen. Ohne sie stünde der
        /// Text ohne Datum da.
        /// </summary>
        [Fact]
        public void Die_Formatschluessel_tragen_ihre_Platzhalter()
        {
            foreach (string sprache in new[] { "de-DE", "en-US" })
            {
                MitSprache(sprache, () =>
                {
                    string gueltig = Resource.ResourceManager.GetString("LIZ_ST_GUELTIG");
                    string kulanz = Resource.ResourceManager.GetString("LIZ_ST_KULANZ");

                    Assert.Contains("{0}", gueltig, StringComparison.Ordinal);
                    Assert.Contains("{1}", gueltig, StringComparison.Ordinal);
                    Assert.Contains("{0}", kulanz, StringComparison.Ordinal);
                });
            }
        }

        /// <summary>
        /// Auf Englisch heißen die drei Lizenztypen anders — geprüft an der Stelle, an
        /// der sie tatsächlich gelesen werden.
        /// </summary>
        [Fact]
        public void Der_Lizenztyp_wird_uebersetzt()
        {
            var token = LizenzToken.FuerPruefstand(GERAET, typ: "demo");

            MitSprache("de-DE", () => Assert.Equal("Demoversion", token.TypText()));
            MitSprache("en-US", () => Assert.Equal("Trial version", token.TypText()));
        }

        // ==============================================================
        //  ANWENDERENTSCHEID MN-1 (19.09.2026) — die Rechtstexte
        // ==============================================================

        /// <summary>
        /// <b>Der Abschnitt „Datenverarbeitung" nennt die Übertragung an den
        /// LIZENZSERVER</b> — Zweck, Daten, Empfänger und Rechtsgrundlage. Bis MN-1
        /// beschrieb er nur Klimadaten, Ortssuche und den Hilfe-Assistenten; die
        /// Aktivierung überträgt aber Schlüssel, Adresse und Gerätekennung, und das
        /// Konzept Lizenzierung § 7 verlangt, dass genau das dort steht.
        ///
        /// <para>Der Rechtstext ist in BEIDEN Katalogen deutsch — verbindlich ist
        /// die deutsche Fassung (Entscheid W15c-E-7).</para>
        /// </summary>
        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Die_Datenverarbeitung_nennt_den_Lizenzserver(string sprache)
        {
            MitSprache(sprache, () =>
            {
                string text = Resource.ResourceManager.GetString("LIZR_RH_A6");

                Assert.False(string.IsNullOrWhiteSpace(text));
                foreach (string stueck in new[]
                         {
                             "Lizenzserver", "epos-plan.de", "Lizenzschlüssel",
                             "E-Mail-Adresse", "Geräte-Hash", "Programmversion",
                             "Art. 6 Abs. 1 lit. b DSGVO",
                         })
                    Assert.Contains(stueck, text, StringComparison.Ordinal);

                // Und die Zusage, die daneben stehen bleibt.
                Assert.Contains("werden dabei nicht übertragen", text, StringComparison.Ordinal);
            });
        }

        /// <summary>
        /// <b>Die Komponentenliste ist vollständig.</b> Sie nannte bis MN-1 vier
        /// Microsoft-Bestandteile; ausgeliefert werden auch der Diagramm-Renderer,
        /// der Fahrplaner, die Berichtserzeugung und die Signaturprüfung. Jede
        /// Lizenzart stammt aus der <c>.nuspec</c> des Pakets, nicht aus dem
        /// Gedächtnis.
        /// </summary>
        [Fact]
        public void Die_Komponentenliste_nennt_jedes_ausgelieferte_Paket()
        {
            MitSprache("de-DE", () =>
            {
                string text = Resource.ResourceManager.GetString("LIZR_KO_A2");

                foreach (string paket in new[]
                         {
                             "SkiaSharp", "HarfBuzzSharp", "SixLabors.Fonts",
                             "Google OR-Tools", "QuickGrid", "BouncyCastle",
                             "ClosedXML", "DocumentFormat.OpenXml", "MathNet.Numerics",
                             "Humanizer", "WinForms.DataVisualization", "SQLitePCLRaw",
                             "JsonSchema.Net",
                         })
                    Assert.Contains(paket, text, StringComparison.Ordinal);

                // Die Lizenzarten stehen dabei - und zwar die, die in den .nuspec
                // steht: Apache-2.0 fuer OR-Tools, SixLabors.Fonts 1.0 und
                // SQLitePCLRaw, MIT fuer den Rest.
                Assert.Contains("Google OR-Tools (Apache-2.0-Lizenz", text, StringComparison.Ordinal);
                Assert.Contains("SixLabors.Fonts 1.0 (Apache-2.0-Lizenz", text, StringComparison.Ordinal);
                Assert.Contains("SkiaSharp samt SkiaSharp.HarfBuzz und HarfBuzzSharp (MIT-Lizenz",
                                text, StringComparison.Ordinal);

                // Der Assistentenabschnitt nennt seine Anbindung.
                Assert.Contains("Mscc.GenerativeAI (Apache-2.0-Lizenz",
                                Resource.ResourceManager.GetString("LIZR_KO_A5"),
                                StringComparison.Ordinal);
            });
        }

        /// <summary>
        /// Die vier NEUEN Schlüssel von MN-1 stehen in beiden Sprachen: der Reiter
        /// „Status &amp; Aktivierung", der feste Übertragungshinweis über dem
        /// Aktivieren-Knopf und die zwei Sprünge darunter.
        /// </summary>
        [Theory]
        [InlineData("LIZR_REITER_STATUS")]
        [InlineData("LIZ_HINWEIS_UEBERTRAGUNG")]
        [InlineData("LIZ_LINK_VEREINBARUNG")]
        [InlineData("LIZ_LINK_DATENVERARBEITUNG")]
        public void Die_neuen_Schluessel_stehen_in_beiden_Sprachen(string schluessel)
        {
            string deutsch = null, englisch = null;
            MitSprache("de-DE", () => deutsch = Resource.ResourceManager.GetString(schluessel));
            MitSprache("en-US", () => englisch = Resource.ResourceManager.GetString(schluessel));

            Assert.False(string.IsNullOrWhiteSpace(deutsch), schluessel + " fehlt auf Deutsch.");
            Assert.False(string.IsNullOrWhiteSpace(englisch), schluessel + " fehlt auf Englisch.");
            Assert.NotEqual(deutsch, englisch);
        }

        /// <summary>
        /// Der feste Hinweis sagt AUSDRÜCKLICH, was übertragen wird und wohin — und
        /// verweist auf die zwei Papiere, die dafür gelten. Er ist der Kern der
        /// Rechtskonformität, die der Anwender angemahnt hat.
        /// </summary>
        [Fact]
        public void Der_Uebertragungshinweis_nennt_Daten_Ziel_und_Papiere()
        {
            MitSprache("de-DE", () =>
            {
                string text = Resource.ResourceManager.GetString("LIZ_HINWEIS_UEBERTRAGUNG");

                foreach (string stueck in new[]
                         {
                             "Lizenzschlüssel", "E-Mail-Adresse", "Gerätekennung",
                             "Lizenzserver", "Lizenzvereinbarung", "Datenverarbeitung",
                         })
                    Assert.Contains(stueck, text, StringComparison.Ordinal);
            });
        }
    }
}
