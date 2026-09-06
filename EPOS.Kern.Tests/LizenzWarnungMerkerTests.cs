using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Tagesmerker der Lizenz-Warnstufen</b> (Anwenderentscheid <b>iF30‑O‑2</b>
    /// vom 06.09.2026: „einmal täglich reicht").
    ///
    /// <para>Der Zwilling zu <see cref="LizenzWarnstufenTests"/>: Dort steht, WELCHE Stufe
    /// aus einem Token folgt, hier, WIE OFT der Anwender sie zu sehen bekommt — einmal je
    /// Kalendertag statt bei jedem Programmstart.</para>
    ///
    /// <para><b>Ohne Ablage und ohne Uhr.</b> Jeder Fall rechnet gegen einen festen Tag
    /// und legt eine eigene <see cref="FluechtigeEinstellungen"/> ein; die Registry des
    /// Entwicklerrechners und der Zeitanker bleiben unberührt (Risiko R‑W15c‑3).</para>
    ///
    /// <para><b>Warum die Sammlung „Testdatenbank".</b> <see cref="Dienste.Einstellungen"/>
    /// ist prozessweiter Zustand, und xunit fährt Testklassen sonst nebeneinander: Zwei
    /// Klassen, die beide eine Attrappe einlegen, überschreiben sich gegenseitig. Hausregel
    /// seit dem 06.09.2026 — <b>jede Testklasse, die ein <c>Dienste.*</c> tauscht, trägt
    /// <c>[Collection("Testdatenbank")]</c></b>.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class LizenzWarnungMerkerTests
    {
        private static readonly DateTime Heute = new DateTime(2026, 9, 6);
        private const string GERAET = "SHA256:AAAA";

        // ==================================================================
        //  1-3  Die vier Regeln
        // ==================================================================

        /// <summary>
        /// Fall 1: <b>Warnstufe 0 zeigt nie und merkt nichts.</b> Das ist der Regelfall
        /// einer gültigen Lizenz fern vom Ablauf — er darf in der Ablage keine Spur
        /// hinterlassen, sonst stünde bei jedem Anwender ein Vermerk, der nie gebraucht
        /// wird.
        /// </summary>
        [Fact]
        public void Ohne_Warnstufe_wird_nichts_gezeigt_und_nichts_gemerkt()
        {
            MitAblage(ablage =>
            {
                Assert.False(LizenzWarnungMerker.SollZeigen(0, Heute));
                Assert.False(LizenzWarnungMerker.SollZeigen(-1, Heute));

                Assert.Null(ablage.Lies(LizenzWarnungMerker.SCHLUESSEL, null));
            });
        }

        /// <summary>
        /// Fall 2: <b>Im Zweifel zeigen.</b> Ein fehlender, leerer oder unlesbarer Vermerk
        /// führt zum Hinweis — dieselbe Linie wie <c>Schreibnaht.Lizenzantwort</c>: Der
        /// Fehlerfall darf dem Anwender nichts wegnehmen. Danach steht ein sauberer
        /// Vermerk da.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Unsinn")]
        [InlineData("2026-09-06")]
        [InlineData("2026-09-06|")]
        [InlineData("|14")]
        [InlineData("06.09.2026|14")]
        [InlineData("2026-09-06|vierzehn")]
        [InlineData("2026-09-06|0")]
        public void Ein_unlesbarer_Vermerk_zeigt(string hinterlegt)
        {
            MitAblage(ablage =>
            {
                if (hinterlegt != null) ablage.Schreib(LizenzWarnungMerker.SCHLUESSEL, hinterlegt);

                Assert.True(LizenzWarnungMerker.SollZeigen(14, Heute));
                Assert.Equal("2026-09-06|14", ablage.Lies(LizenzWarnungMerker.SCHLUESSEL, null));
            });
        }

        /// <summary>
        /// Fall 3: <b>Der zweite Start am selben Tag zeigt NICHT</b> — das ist der ganze
        /// Zweck des Entscheids. Der dritte und der vierte ebenso wenig.
        /// </summary>
        [Fact]
        public void Der_zweite_Start_am_selben_Tag_zeigt_nicht()
        {
            MitAblage(_ =>
            {
                Assert.True(LizenzWarnungMerker.SollZeigen(30, Heute));
                Assert.False(LizenzWarnungMerker.SollZeigen(30, Heute));
                Assert.False(LizenzWarnungMerker.SollZeigen(30, Heute));
            });
        }

        /// <summary>
        /// Fall 4: <b>Der nächste Tag zeigt wieder</b> — „einmal täglich" heißt: jeden Tag
        /// einmal, nicht einmal überhaupt.
        /// </summary>
        [Fact]
        public void Der_naechste_Tag_zeigt_wieder()
        {
            MitAblage(ablage =>
            {
                Assert.True(LizenzWarnungMerker.SollZeigen(30, Heute));
                Assert.False(LizenzWarnungMerker.SollZeigen(30, Heute));

                Assert.True(LizenzWarnungMerker.SollZeigen(30, Heute.AddDays(1)));
                Assert.Equal("2026-09-07|30", ablage.Lies(LizenzWarnungMerker.SCHLUESSEL, null));

                Assert.False(LizenzWarnungMerker.SollZeigen(30, Heute.AddDays(1)));
                Assert.True(LizenzWarnungMerker.SollZeigen(30, Heute.AddDays(2)));
            });
        }

        /// <summary>
        /// Fall 5: <b>Eine dringendere Stufe zeigt am selben Tag erneut.</b> 30 → 14 → 7
        /// ist jedes Mal eine NEUE Nachricht; wer heute schon „in 30 Tagen" gelesen hat und
        /// mittags auf „in 7 Tagen" springt (verstellte Uhr, gewechseltes Token), soll das
        /// sehen. <b>Die kleinere Zahl ist die dringendere Stufe.</b>
        /// </summary>
        [Fact]
        public void Eine_dringendere_Stufe_zeigt_am_selben_Tag_erneut()
        {
            MitAblage(ablage =>
            {
                Assert.True(LizenzWarnungMerker.SollZeigen(30, Heute));
                Assert.True(LizenzWarnungMerker.SollZeigen(14, Heute));
                Assert.True(LizenzWarnungMerker.SollZeigen(7, Heute));
                Assert.Equal("2026-09-06|7", ablage.Lies(LizenzWarnungMerker.SCHLUESSEL, null));

                // Und dieselbe Stufe danach nicht mehr.
                Assert.False(LizenzWarnungMerker.SollZeigen(7, Heute));
            });
        }

        /// <summary>
        /// Fall 6: <b>7 nach 30 am selben Tag zeigt</b> — der Sprung über eine Stufe
        /// hinweg zählt genauso.
        /// </summary>
        [Fact]
        public void Stufe_7_nach_Stufe_30_zeigt_am_selben_Tag()
        {
            MitAblage(_ =>
            {
                Assert.True(LizenzWarnungMerker.SollZeigen(30, Heute));
                Assert.True(LizenzWarnungMerker.SollZeigen(7, Heute));
            });
        }

        /// <summary>
        /// Fall 7: <b>Rückwärts nicht.</b> Ist heute schon die dringendere Stufe gezeigt
        /// worden, schweigt die leisere — sonst warnte ein zurückgestellter Zeitanker den
        /// Anwender zweimal mit derselben Sache, nur milder.
        /// </summary>
        [Fact]
        public void Eine_leisere_Stufe_zeigt_am_selben_Tag_nicht_mehr()
        {
            MitAblage(ablage =>
            {
                Assert.True(LizenzWarnungMerker.SollZeigen(7, Heute));
                Assert.False(LizenzWarnungMerker.SollZeigen(14, Heute));
                Assert.False(LizenzWarnungMerker.SollZeigen(30, Heute));

                // Der Vermerk bleibt bei der DRINGENDSTEN Stufe des Tages stehen.
                Assert.Equal("2026-09-06|7", ablage.Lies(LizenzWarnungMerker.SCHLUESSEL, null));
            });
        }

        /// <summary>
        /// Fall 8: Die Uhrzeit spielt keine Rolle — gerechnet wird auf den KALENDERTAG.
        /// Ein Start um 23:59 und einer um 00:01 desselben Tages sind derselbe Tag.
        /// </summary>
        [Fact]
        public void Die_Uhrzeit_spielt_keine_Rolle()
        {
            MitAblage(ablage =>
            {
                Assert.True(LizenzWarnungMerker.SollZeigen(14, Heute.AddHours(8).AddMinutes(30)));
                Assert.False(LizenzWarnungMerker.SollZeigen(14, Heute.AddHours(23).AddMinutes(59)));
                Assert.Equal("2026-09-06|14", ablage.Lies(LizenzWarnungMerker.SCHLUESSEL, null));
            });
        }

        /// <summary>
        /// Fall 9: <b>Eine werfende Ablage zeigt.</b> Wenn die Registry nicht lesbar ist,
        /// soll der Anwender seinen Hinweis bekommen und nicht schweigend um ihn gebracht
        /// werden — und das Schreiben darf den Start nicht mit einer Ausnahme abbrechen.
        /// </summary>
        [Fact]
        public void Eine_werfende_Ablage_zeigt_und_wirft_nicht_weiter()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            try
            {
                Dienste.Einstellungen = new WerfendeEinstellungen();

                Assert.True(LizenzWarnungMerker.SollZeigen(7, Heute));
                Assert.True(LizenzWarnungMerker.SollZeigen(7, Heute));
            }
            finally
            {
                Dienste.Einstellungen = vorher;
            }
        }

        // ==================================================================
        //  10-12  Die Naht zum Lagebild
        // ==================================================================

        /// <summary>
        /// Fall 10: <b>Die Entscheidung liegt im Kern.</b> Eine Warnstufen-Lage ist beim
        /// ersten Aufruf sichtbar und beim zweiten am selben Tag stumm — die Oberfläche
        /// fragt nur <c>LizenzLage.WarnungZeigen</c> und kennt weder Tag noch Stufe.
        /// Alles Übrige an der Lage bleibt unverändert.
        /// </summary>
        [Fact]
        public void Eine_Warnstufe_wird_beim_zweiten_Start_stumm()
        {
            MitAblage(_ =>
            {
                LizenzLage roh = LizenzLage.Bilden(LizenzStatus.Gueltig, Token(10), Heute);
                Assert.Equal(14, roh.Warnstufe);

                LizenzLage erster = roh.MitTagesmerker(Heute);
                Assert.True(erster.WarnungZeigen);
                Assert.Same(roh, erster);

                LizenzLage zweiter = roh.MitTagesmerker(Heute);
                Assert.False(zweiter.WarnungZeigen);

                // Der Rest der Lage ist derselbe - stumm heisst „nicht zeigen",
                // nicht „nicht vorhanden".
                Assert.Equal(roh.Text, zweiter.Text);
                Assert.Equal(roh.Warnstufe, zweiter.Warnstufe);
                Assert.Equal(roh.RestTage, zweiter.RestTage);
                Assert.Equal(roh.Dringlichkeit, zweiter.Dringlichkeit);
                Assert.Equal(roh.Detail, zweiter.Detail);

                // Und am naechsten Tag wieder sichtbar.
                Assert.True(roh.MitTagesmerker(Heute.AddDays(1)).WarnungZeigen);
            });
        }

        /// <summary>
        /// Fall 11: <b>Der LESEMODUS bleibt bei JEDEM Start sichtbar.</b> Er ist keine
        /// Warnstufe, sondern der Zustand, den der Anwender beheben MUSS und sonst nicht
        /// sieht (Hausregel W16b‑E‑6). Nachgewiesen an zehn Aufrufen hintereinander — und
        /// daran, dass er den Merker gar nicht erst anfasst.
        /// </summary>
        [Theory]
        [InlineData(LizenzStatus.Lesemodus)]
        [InlineData(LizenzStatus.NichtAktiviert)]
        [InlineData(LizenzStatus.UhrManipuliert)]
        public void Der_Lesemodus_bleibt_bei_jedem_Start_sichtbar(LizenzStatus status)
        {
            MitAblage(ablage =>
            {
                LizenzLage lage = LizenzLage.Bilden(status, Token(-40), Heute);
                Assert.True(lage.Lesemodus);

                for (int i = 0; i < 10; i++)
                    Assert.True(lage.MitTagesmerker(Heute).WarnungZeigen);

                Assert.Null(ablage.Lies(LizenzWarnungMerker.SCHLUESSEL, null));
            });
        }

        /// <summary>
        /// Fall 12: Kulanzfenster und fällige Nachprüfung tragen Warnstufe <c>0</c> und
        /// laufen deshalb gar nicht durch den Merker — ihr Hinweis steht bei jedem Start.
        /// Sie sind der „deutliche Hinweis" der Konzeptzeile „Ablauf bis +14 Tage", nicht
        /// eine der drei Stufen davor.
        /// </summary>
        [Fact]
        public void Kulanz_und_Nachpruefung_stehen_bei_jedem_Start()
        {
            MitAblage(ablage =>
            {
                LizenzLage kulanz = LizenzLage.Bilden(LizenzStatus.Kulanz, Token(-3), Heute);
                Assert.Equal(0, kulanz.Warnstufe);
                Assert.True(kulanz.MitTagesmerker(Heute).WarnungZeigen);
                Assert.True(kulanz.MitTagesmerker(Heute).WarnungZeigen);

                LizenzLage nach = LizenzLage.Bilden(LizenzStatus.NachpruefungFaellig, Token(60), Heute);
                Assert.True(nach.MitTagesmerker(Heute).WarnungZeigen);
                Assert.True(nach.MitTagesmerker(Heute).WarnungZeigen);

                Assert.Null(ablage.Lies(LizenzWarnungMerker.SCHLUESSEL, null));
            });
        }

        /// <summary>
        /// Fall 13: Eine ruhige Lage bleibt ruhig — <c>Ruhig</c> trägt Warnstufe 0, und
        /// der Merker macht daraus keinen Vermerk.
        /// </summary>
        [Fact]
        public void Eine_ruhige_Lage_bleibt_unberuehrt()
        {
            MitAblage(ablage =>
            {
                Assert.Same(LizenzLage.Ruhig, LizenzLage.Ruhig.MitTagesmerker(Heute));
                Assert.True(LizenzLage.Ruhig.WarnungZeigen);
                Assert.Null(ablage.Lies(LizenzWarnungMerker.SCHLUESSEL, null));
            });
        }

        // ==================================================================

        /// <summary>
        /// Legt eine frische Ablage ein, führt den Fall aus und stellt die vorherige
        /// zurück. <see cref="Dienste.Einstellungen"/> ist prozessweiter Zustand.
        /// </summary>
        private static void MitAblage(Action<IEinstellungen> fall)
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            try
            {
                var ablage = new FluechtigeEinstellungen();
                Dienste.Einstellungen = ablage;
                fall(ablage);
            }
            finally
            {
                Dienste.Einstellungen = vorher;
            }
        }

        /// <summary>Ein Token, das in <paramref name="gueltigInTagen"/> Tagen abläuft.</summary>
        private static LizenzToken Token(int gueltigInTagen)
        {
            return LizenzToken.FuerPruefstand(GERAET,
                                              gueltigBis: Heute.AddDays(gueltigInTagen),
                                              kulanzTage: 14,
                                              tokenBis: Heute.AddDays(gueltigInTagen));
        }

        /// <summary>
        /// Eine Ablage, die bei jedem Zugriff wirft — die Registry eines Rechners, auf dem
        /// der Zweig gesperrt ist.
        /// </summary>
        private sealed class WerfendeEinstellungen : IEinstellungen
        {
            public string Lies(string schluessel, string vorgabe = null)
                => throw new InvalidOperationException("Ablage gesperrt");

            public int LiesZahl(string schluessel, int vorgabe = 0)
                => throw new InvalidOperationException("Ablage gesperrt");

            public void Schreib(string schluessel, string wert)
                => throw new InvalidOperationException("Ablage gesperrt");

            public void SchreibZahl(string schluessel, int wert)
                => throw new InvalidOperationException("Ablage gesperrt");

            public void Loesche(string schluessel)
                => throw new InvalidOperationException("Ablage gesperrt");

            public string LiesMaschine(string schluessel, string vorgabe = null)
                => throw new InvalidOperationException("Ablage gesperrt");
        }
    }
}
