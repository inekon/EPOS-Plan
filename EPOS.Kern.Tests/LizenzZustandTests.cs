using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="LizenzManager.Bewerten"/> — <b>der Nachweis der Welle iU9-W15c</b>.
    ///
    /// <para><b>Warum es diesen Zeugen bis heute nicht gab.</b> Der Lizenzkern liegt
    /// seit iU5-U1 plattformfrei in <c>EPOS.Kern/Allgemein/Lizenz/</c> — 659 Zeilen mit
    /// sechs Zuständen, zwei Fristen, einem Kulanzfenster, einer Karenzzeit und einem
    /// Uhr-Manipulationsschutz. Geprüft hat davon nichts ein einziger Test: Ein
    /// <c>grep</c> über alle vier Testprojekte fand ausschließlich Treffer in
    /// <c>DiensteTests</c>, und die prüfen die SCHNITTSTELLE <c>ILizenzAblage</c>, nicht
    /// den Manager (Befund W15c-B1). Der Wellennachweis „Lizenzzustände
    /// NichtAktiviert…Lesemodus durchspielen" ist deshalb eine <b>Erstanlage</b>.</para>
    ///
    /// <para><b>Warum ohne Ablage.</b> <c>LizenzManager.Pruefe()</c> SCHREIBT bei jedem
    /// Aufruf den Zeitanker fort (Befund W15c-B23). Ein Test, der ihn ruft, würde die
    /// Ablage des Entwicklerrechners auf die Zukunft setzen und dort später
    /// <c>UhrManipuliert</c> auslösen (Risiko R-W15c-3). Mit W15c.1 ist die Rechnung
    /// deshalb als reine Funktion herausgezogen (Entscheid E-10, Weg W2): Sie nimmt
    /// Token, Geräte-Id, Tagesdatum und Anker entgegen und fasst nichts an. <b>Kein
    /// Fall dieser Klasse berührt <c>Dienste.Lizenzablage</c> oder
    /// <c>Dienste.Einstellungen</c>.</b></para>
    ///
    /// <para><b>Die Reihenfolge ist Bedeutung.</b> Uhrprüfung vor Token, Token vor
    /// Gerät, Lizenzlaufzeit vor Offline-Leine. Die beiden letzten Ränder sind die
    /// eigentliche Fachaussage: Kulanz endet EINSCHLIESSLICH des letzten Tages, Karenz
    /// ebenso, und wer abgelaufen ist, kommt nie in <c>NachpruefungFaellig</c>.</para>
    /// </summary>
    public class LizenzZustandTests
    {
        /// <summary>Die Geräte-Id dieses Arbeitsplatzes in allen Fällen.</summary>
        private const string GERAET = "SHA256:AAAA";

        /// <summary>Ein fester Tag — die Fälle rechnen relativ dazu, nie gegen die Uhr.</summary>
        private static readonly DateTime Heute = new DateTime(2026, 9, 4);

        /// <summary>Kein Anker: der allererste Start.</summary>
        private static readonly DateTime KeinAnker = DateTime.MinValue;

        // ==================================================================
        //  1-2  Kein Token, fremdes Gerät
        // ==================================================================

        /// <summary>Fall 1: Ohne Token ist nichts aktiviert.</summary>
        [Fact]
        public void Ohne_Token_ist_nichts_aktiviert()
        {
            Assert.Equal(LizenzStatus.NichtAktiviert,
                         LizenzManager.Bewerten(null, GERAET, Heute, KeinAnker));
        }

        /// <summary>
        /// Fall 2: Ein Token mit fremder Geräte-Id sieht aus wie „nicht aktiviert" —
        /// nicht wie „ungültig". Das ist Absicht: Ein kopiertes Token soll den Anwender
        /// zur Aktivierung führen und nicht in eine Sackgasse.
        /// </summary>
        [Fact]
        public void Ein_fremdes_Geraet_sieht_aus_wie_nicht_aktiviert()
        {
            var token = LizenzToken.FuerPruefstand("SHA256:FREMD", tokenBis: Heute.AddDays(30));

            Assert.Equal(LizenzStatus.NichtAktiviert,
                         LizenzManager.Bewerten(token, GERAET, Heute, KeinAnker));
        }

        // ==================================================================
        //  3  Der Normalfall
        // ==================================================================

        /// <summary>Fall 3: Beide Fristen in der Zukunft — gültig.</summary>
        [Fact]
        public void Beide_Fristen_in_der_Zukunft_ergeben_gueltig()
        {
            var token = LizenzToken.FuerPruefstand(GERAET,
                                                   gueltigBis: Heute.AddDays(300),
                                                   kulanzTage: 14,
                                                   tokenBis: Heute.AddDays(10));

            Assert.Equal(LizenzStatus.Gueltig,
                         LizenzManager.Bewerten(token, GERAET, Heute, KeinAnker));
        }

        // ==================================================================
        //  4-7  Die Lizenzlaufzeit und ihr Kulanzfenster
        // ==================================================================

        /// <summary>Fall 4: Gestern abgelaufen, Kulanzfenster 14 Tage — Kulanz.</summary>
        [Fact]
        public void Gestern_abgelaufen_mit_Kulanzfenster_ergibt_Kulanz()
        {
            var token = LizenzToken.FuerPruefstand(GERAET,
                                                   gueltigBis: Heute.AddDays(-1), kulanzTage: 14);

            Assert.Equal(LizenzStatus.Kulanz,
                         LizenzManager.Bewerten(token, GERAET, Heute, KeinAnker));
        }

        /// <summary>
        /// Fall 5 — <b>der Rand</b>: Genau vierzehn Tage nach Ablauf liegt noch IM
        /// Kulanzfenster (<c>heute &lt;= kulanzEnde</c>).
        /// </summary>
        [Fact]
        public void Der_letzte_Kulanztag_zaehlt_noch_dazu()
        {
            var token = LizenzToken.FuerPruefstand(GERAET,
                                                   gueltigBis: Heute.AddDays(-14), kulanzTage: 14);

            Assert.Equal(LizenzStatus.Kulanz,
                         LizenzManager.Bewerten(token, GERAET, Heute, KeinAnker));
        }

        /// <summary>Fall 6 — der Tag danach: Lesemodus.</summary>
        [Fact]
        public void Einen_Tag_nach_dem_Kulanzfenster_gilt_der_Lesemodus()
        {
            var token = LizenzToken.FuerPruefstand(GERAET,
                                                   gueltigBis: Heute.AddDays(-15), kulanzTage: 14);

            Assert.Equal(LizenzStatus.Lesemodus,
                         LizenzManager.Bewerten(token, GERAET, Heute, KeinAnker));
        }

        /// <summary>
        /// Fall 7: Ohne Kulanztage gibt es kein Fenster — der erste Tag nach Ablauf ist
        /// bereits Lesemodus.
        /// </summary>
        [Fact]
        public void Ohne_Kulanztage_beginnt_der_Lesemodus_sofort()
        {
            var token = LizenzToken.FuerPruefstand(GERAET,
                                                   gueltigBis: Heute.AddDays(-1), kulanzTage: 0);

            Assert.Equal(LizenzStatus.Lesemodus,
                         LizenzManager.Bewerten(token, GERAET, Heute, KeinAnker));
        }

        // ==================================================================
        //  8-10  Die Offline-Leine und ihre Karenzzeit
        // ==================================================================

        /// <summary>Fall 8: Die Offline-Leine ist gestern abgelaufen — Nachprüfung fällig.</summary>
        [Fact]
        public void Abgelaufene_Offline_Leine_verlangt_die_Nachpruefung()
        {
            var token = LizenzToken.FuerPruefstand(GERAET, tokenBis: Heute.AddDays(-1));

            Assert.Equal(LizenzStatus.NachpruefungFaellig,
                         LizenzManager.Bewerten(token, GERAET, Heute, KeinAnker));
        }

        /// <summary>
        /// Fall 9 — <b>der Rand</b>: Der vierzehnte Karenztag zählt noch dazu.
        /// <c>KARENZ_TAGE</c> steht dabei ausdrücklich im Zeugen: Eine Änderung der
        /// Konstante ist eine Fachentscheidung und soll hier auffallen.
        /// </summary>
        [Fact]
        public void Der_letzte_Karenztag_zaehlt_noch_dazu()
        {
            Assert.Equal(14, LizenzManager.KARENZ_TAGE);

            var token = LizenzToken.FuerPruefstand(GERAET,
                                                   tokenBis: Heute.AddDays(-LizenzManager.KARENZ_TAGE));

            Assert.Equal(LizenzStatus.NachpruefungFaellig,
                         LizenzManager.Bewerten(token, GERAET, Heute, KeinAnker));
        }

        /// <summary>Fall 10 — der Tag danach: Lesemodus.</summary>
        [Fact]
        public void Einen_Tag_nach_der_Karenzzeit_gilt_der_Lesemodus()
        {
            var token = LizenzToken.FuerPruefstand(GERAET,
                                                   tokenBis: Heute.AddDays(-LizenzManager.KARENZ_TAGE - 1));

            Assert.Equal(LizenzStatus.Lesemodus,
                         LizenzManager.Bewerten(token, GERAET, Heute, KeinAnker));
        }

        // ==================================================================
        //  11-12  Die Uhr
        // ==================================================================

        /// <summary>
        /// Fall 11: Steht der Anker zwei Tage vor uns, ist die Uhr zurückgestellt
        /// worden. Die Prüfung steht VOR allem anderen — auch vor der Frage, ob es
        /// überhaupt ein Token gibt.
        /// </summary>
        [Fact]
        public void Ein_Anker_zwei_Tage_voraus_meldet_eine_zurueckgestellte_Uhr()
        {
            Assert.Equal(LizenzStatus.UhrManipuliert,
                         LizenzManager.Bewerten(null, GERAET, Heute, Heute.AddDays(2)));
        }

        /// <summary>
        /// Fall 12 — <b>die Toleranz</b>: Genau ein Tag Vorsprung ist erlaubt
        /// („für Zeitzonen u. Ä.", <c>heute.AddDays(1) &lt; anker</c>). Ohne Token
        /// bleibt es deshalb bei <c>NichtAktiviert</c> statt <c>UhrManipuliert</c>.
        /// </summary>
        [Fact]
        public void Ein_Tag_Vorsprung_ist_noch_kein_Uhrverdacht()
        {
            Assert.Equal(LizenzStatus.NichtAktiviert,
                         LizenzManager.Bewerten(null, GERAET, Heute, Heute.AddDays(1)));
        }

        // ==================================================================
        //  13  Die Rangfolge der beiden Fristen
        // ==================================================================

        /// <summary>
        /// Fall 13: Sind BEIDE Fristen abgelaufen, sticht die Lizenzlaufzeit die
        /// Offline-Leine — wer abgelaufen ist, kommt nie in <c>NachpruefungFaellig</c>.
        /// </summary>
        [Fact]
        public void Die_Laufzeit_sticht_die_Leine()
        {
            var token = LizenzToken.FuerPruefstand(GERAET,
                                                   gueltigBis: Heute.AddDays(-30), kulanzTage: 14,
                                                   tokenBis: Heute.AddDays(-1));

            Assert.Equal(LizenzStatus.Lesemodus,
                         LizenzManager.Bewerten(token, GERAET, Heute, KeinAnker));
        }

        // ==================================================================
        //  14  Das Schreibrecht je Zustand
        // ==================================================================

        /// <summary>
        /// Fall 14: Genau drei der sechs Zustände dürfen schreiben — <c>Gueltig</c>,
        /// <c>Kulanz</c> und <c>NachpruefungFaellig</c>. Alles andere ist Lesemodus im
        /// weiteren Sinn.
        /// </summary>
        [Theory]
        [InlineData(LizenzStatus.Gueltig, true)]
        [InlineData(LizenzStatus.Kulanz, true)]
        [InlineData(LizenzStatus.NachpruefungFaellig, true)]
        [InlineData(LizenzStatus.NichtAktiviert, false)]
        [InlineData(LizenzStatus.Lesemodus, false)]
        [InlineData(LizenzStatus.UhrManipuliert, false)]
        public void Das_Schreibrecht_haengt_am_Zustand(LizenzStatus status, bool erwartet)
        {
            Assert.Equal(erwartet, LizenzManager.DarfSchreiben(status));
        }
    }
}
