using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="LizenzTextCtrl"/> und <see cref="ZustimmungCtrl"/> — die zwei
    /// Kern-Vorarbeiten des Lizenzdialogs (iU9-W15c.8 und W15c.9).
    ///
    /// <para><b>Kein Netz.</b> Der Abruf selbst (<c>OnlineFassungHolen</c>) ist von der
    /// Auswertung getrennt: <c>AntwortLesen</c> nimmt die JSON-Antwort entgegen und ist
    /// damit ohne Internet prüfbar — dasselbe Vorgehen wie beim TMY-Abruf in
    /// <c>KlimaImportAblauf</c> (Risiko R-W14c-5).</para>
    ///
    /// <para><b>Der Fehlerpfad der Zustimmung ist der wichtigste Fall dieser Klasse.</b>
    /// Eine nicht lesbare Ablage blockiert den Start NICHT (Entscheid E-15, Befund
    /// W15c-B18) — eine Zusage, die man nur beim Lesen des Kommentars bemerkt und beim
    /// nächsten Umbau still verlieren würde.</para>
    ///
    /// <para><b>Warum die Sammlung (Befund iU5‑O‑1, 06.09.2026).</b> Die Fälle der
    /// Zustimmung tauschen <see cref="Dienste.Einstellungen"/> — prozessweiter Zustand.
    /// Ohne Sammlungsangabe gibt xunit jeder Testklasse ihre eigene Sammlung und fährt
    /// sie damit NEBEN allen anderen; diese Klasse tauschte den Dienst also gegen die
    /// übrigen Tauscher an. Alle Tauscher stehen deshalb in der einen seriellen
    /// Sammlung „Testdatenbank"; der Wächter <see cref="DiensteSammlungTests"/> prüft
    /// es.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class LizenzTextCtrlTests
    {
        // ==================================================================
        //  HtmlZuText
        // ==================================================================

        /// <summary>
        /// Die sieben Regeln des Wandlers an einem Schnipsel: Skript und Stil fliegen
        /// ganz raus, <c>&lt;br&gt;</c> wird eine Zeile, <c>&lt;li&gt;</c> ein
        /// Aufzählungsstrich, Überschriften und Absätze werden Zeilenwechsel, der Rest
        /// der Auszeichnung fällt weg, und Entitäten werden aufgelöst.
        /// </summary>
        [Fact]
        public void Der_Wandler_macht_aus_HTML_lesbaren_Flie_text()
        {
            string html =
                "<style>p{color:red}</style>" +
                "<h2>Vertrag</h2>" +
                "<p>Erster Absatz mit <strong>Auszeichnung</strong>.<br>Zweite Zeile.</p>" +
                "<ul><li>Erster Punkt</li><li>Zweiter Punkt</li></ul>" +
                "<script>alert('x')</script>" +
                "<p>&sect;&nbsp;3 Haftung &amp; Gew&auml;hrleistung</p>";

            string text = LizenzTextCtrl.HtmlZuText(html);

            Assert.DoesNotContain("<", text, StringComparison.Ordinal);
            Assert.DoesNotContain("alert", text, StringComparison.Ordinal);
            Assert.DoesNotContain("color:red", text, StringComparison.Ordinal);
            Assert.Contains("Vertrag", text, StringComparison.Ordinal);
            Assert.Contains("Erster Absatz mit Auszeichnung.", text, StringComparison.Ordinal);
            Assert.Contains("  - Erster Punkt", text, StringComparison.Ordinal);
            Assert.Contains("§", text, StringComparison.Ordinal);
            Assert.Contains("Haftung & Gewährleistung", text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Mehr als zwei Leerzeilen werden auf zwei eingedampft, und der Text ist am
        /// Rand getrimmt — sonst stünde der Vertrag mit einer Handbreit Luft davor.
        /// </summary>
        [Fact]
        public void Der_Wandler_ebnet_Leerraum_ein()
        {
            string text = LizenzTextCtrl.HtmlZuText("<p>A</p><p></p><p></p><p></p><p>B</p>");

            Assert.StartsWith("A", text, StringComparison.Ordinal);
            Assert.EndsWith("B", text, StringComparison.Ordinal);
            Assert.DoesNotContain(
                Environment.NewLine + Environment.NewLine + Environment.NewLine,
                text, StringComparison.Ordinal);
        }

        /// <summary>Leeres HTML bleibt leer und wirft nicht.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Leeres_HTML_bleibt_leer(string html)
        {
            Assert.Equal("", LizenzTextCtrl.HtmlZuText(html));
        }

        // ==================================================================
        //  StandFormatieren
        // ==================================================================

        /// <summary>Der ISO-Zeitpunkt der Schnittstelle wird zum deutschen Datum.</summary>
        [Fact]
        public void Der_Stand_wird_zum_deutschen_Datum()
        {
            Assert.Equal("13.08.2026", LizenzTextCtrl.StandFormatieren("2026-08-13T22:08:02"));
        }

        /// <summary>
        /// Was sich nicht lesen lässt, wird UNVERÄNDERT durchgereicht statt geraten —
        /// lieber ein roher Stand als ein falscher.
        /// </summary>
        [Fact]
        public void Ein_unlesbarer_Stand_wird_durchgereicht()
        {
            Assert.Equal("demnaechst", LizenzTextCtrl.StandFormatieren("demnaechst"));
            Assert.Null(LizenzTextCtrl.StandFormatieren(""));
            Assert.Null(LizenzTextCtrl.StandFormatieren(null));
        }

        // ==================================================================
        //  Die Antwort der Schnittstelle
        // ==================================================================

        /// <summary>Aus der Antwort kommen Text und Stand heraus.</summary>
        [Fact]
        public void Aus_der_Antwort_kommen_Text_und_Stand()
        {
            string json = JsonSerializer.Serialize(new[]
            {
                new
                {
                    modified = "2026-08-13T22:08:02",
                    content = new { rendered = "<p>Der Vertragstext.</p>" }
                }
            });

            var (text, stand) = LizenzTextCtrl.AntwortLesen(json);

            Assert.Equal("Der Vertragstext.", text);
            Assert.Equal("13.08.2026", stand);
        }

        /// <summary>
        /// Eine leere Liste, ein anderes Format oder Unsinn liefern nichts — und
        /// werfen nicht. Der Dialog behält dann seinen bisherigen Stand.
        /// </summary>
        [Theory]
        [InlineData("[]")]
        [InlineData("{}")]
        [InlineData("kein json")]
        public void Eine_unbrauchbare_Antwort_liefert_nichts(string json)
        {
            var (text, stand) = LizenzTextCtrl.AntwortLesen(json);

            Assert.Equal("", text);
            Assert.Null(stand);
        }

        /// <summary>
        /// Die Mindestlänge ist eine Fachaussage, keine Zahl im Nebensatz: Kürzeres ist
        /// kein Vertragstext, und der vorhandene Stand bleibt stehen.
        /// </summary>
        [Fact]
        public void Die_Mindestlaenge_steht_bei_zweitausend_Zeichen()
        {
            Assert.Equal(2000, LizenzTextCtrl.MINDESTLAENGE);
        }

        /// <summary>
        /// <b>Die Quelle ist EINE Zeile</b> (Auflage E-17) — und heute bitgleich die
        /// AGB-Seite über die WordPress-Schnittstelle, nicht der Vertragsendpunkt des
        /// Lizenzservers (Befund W15c-B27).
        /// </summary>
        [Fact]
        public void Die_Onlinequelle_ist_die_AGB_Seite()
        {
            Assert.Equal("https://epos-plan.de/wp-json/wp/v2/pages?slug=agb&_fields=modified,content",
                         LizenzTextCtrl.ONLINE_QUELLE);
            Assert.Equal("https://epos-plan.de/agb/", LizenzTextCtrl.ONLINE_FASSUNG);
        }

        // ==================================================================
        //  Der gemerkte Pfad und die Dateisuche
        // ==================================================================

        /// <summary>
        /// Der gewählte Pfad wird über <c>Dienste.Einstellungen</c> gemerkt — unter
        /// Windows derselbe Registry-Zweig wie im Bestand.
        /// </summary>
        [Fact]
        public void Der_gewaehlte_Pfad_wird_gemerkt()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            try
            {
                Dienste.Einstellungen = new FluechtigeEinstellungen();

                Assert.Equal("", LizenzTextCtrl.GewaehltenPfadLesen());

                LizenzTextCtrl.GewaehltenPfadSpeichern(@"C:\Vertrag\LIZENZ-INEKON.rtf");
                Assert.Equal(@"C:\Vertrag\LIZENZ-INEKON.rtf", LizenzTextCtrl.GewaehltenPfadLesen());
            }
            finally { Dienste.Einstellungen = vorher; }
        }

        /// <summary>
        /// <b>Der gewählte Pfad hat Vorrang</b> — er kann irgendwo liegen und wird von
        /// keiner der Suchebenen gefunden. Zeigt er ins Leere, geht die Suche
        /// weiter, statt aufzugeben.
        /// </summary>
        [Fact]
        public void Der_gewaehlte_Pfad_hat_Vorrang_und_ein_toter_Pfad_bricht_nichts()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            string datei = Path.Combine(Path.GetTempPath(),
                                        "epos-vertrag-" + Guid.NewGuid().ToString("N") + ".rtf");
            try
            {
                Dienste.Einstellungen = new FluechtigeEinstellungen();
                File.WriteAllText(datei, "{\\rtf1 Vertrag}");

                LizenzTextCtrl.GewaehltenPfadSpeichern(datei);
                Assert.Equal(datei, LizenzTextCtrl.DateiSuchen());

                // Ein toter Pfad darf die Suche nicht abbrechen - sie faellt auf die
                // uebrigen Ebenen zurueck (die im Pruefstand nichts finden).
                LizenzTextCtrl.GewaehltenPfadSpeichern(datei + ".gibtsnicht");
                Assert.Null(LizenzTextCtrl.DateiSuchen());
            }
            finally
            {
                Dienste.Einstellungen = vorher;
                if (File.Exists(datei)) File.Delete(datei);
            }
        }

        // ==================================================================
        //  Die Zustimmung (W15c.9)
        // ==================================================================

        /// <summary>Ohne Eintrag ist nicht zugestimmt; nach dem Merken schon.</summary>
        [Fact]
        public void Die_Zustimmung_wird_gemerkt()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            try
            {
                Dienste.Einstellungen = new FluechtigeEinstellungen();

                Assert.False(ZustimmungCtrl.IstZugestimmt());

                ZustimmungCtrl.Merken("1.1.0.0", new DateTime(2026, 9, 4, 10, 15, 0));

                Assert.True(ZustimmungCtrl.IstZugestimmt());
                Assert.Equal("1.1.0.0 | 2026-09-04 10:15",
                             Dienste.Einstellungen.Lies(ZustimmungCtrl.EINSTELLUNG));
            }
            finally { Dienste.Einstellungen = vorher; }
        }

        /// <summary>
        /// Das Format des Vermerks ist EINGEFROREN — es steht in der Registry von
        /// Bestandsrechnern — und kulturunabhängig: Auch unter englischer Kultur
        /// bleibt es <c>yyyy-MM-dd HH:mm</c>.
        /// </summary>
        [Fact]
        public void Das_Format_des_Vermerks_ist_kulturunabhaengig()
        {
            CultureInfo vorher = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                Assert.Equal("1.1.0.0 | 2026-09-04 10:15",
                             ZustimmungCtrl.Vermerk("1.1.0.0", new DateTime(2026, 9, 4, 10, 15, 0)));
            }
            finally { CultureInfo.CurrentCulture = vorher; }
        }

        /// <summary>
        /// <b>Der Fehlerpfad blockiert den Start NICHT</b> (Entscheid E-15, Befund
        /// W15c-B18). Eine Ablage, die beim Lesen wirft, beantwortet die Frage mit
        /// „ja, zugestimmt" — der Kommentar des Bestands lautet wörtlich „im Zweifel
        /// den Start nicht blockieren".
        /// </summary>
        [Fact]
        public void Eine_werfende_Ablage_blockiert_den_Start_nicht()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            try
            {
                Dienste.Einstellungen = new WerfendeEinstellungen();

                Assert.True(ZustimmungCtrl.IstZugestimmt());

                // Und das Merken bleibt folgenlos statt zu werfen.
                ZustimmungCtrl.Merken("1.1.0.0", DateTime.Now);
            }
            finally { Dienste.Einstellungen = vorher; }
        }

        /// <summary>Eine Ablage, die bei jedem Zugriff wirft — der Prüfstand zu E-15.</summary>
        private sealed class WerfendeEinstellungen : IEinstellungen
        {
            public string Lies(string schluessel, string vorgabe = null)
                => throw new InvalidOperationException("Ablage nicht lesbar.");
            public int LiesZahl(string schluessel, int vorgabe = 0)
                => throw new InvalidOperationException("Ablage nicht lesbar.");
            public void Schreib(string schluessel, string wert)
                => throw new InvalidOperationException("Ablage nicht schreibbar.");
            public void SchreibZahl(string schluessel, int wert)
                => throw new InvalidOperationException("Ablage nicht schreibbar.");
            public void Loesche(string schluessel)
                => throw new InvalidOperationException("Ablage nicht schreibbar.");
            public string LiesMaschine(string schluessel, string vorgabe = null)
                => throw new InvalidOperationException("Ablage nicht lesbar.");
        }
    }
}
