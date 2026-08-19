using System;
using System.Globalization;
using System.Threading;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Protokollformat (Fachkonzept 3.6): genau EINE Zeile je Versuch, acht Felder,
    /// maschinenlesbar und lesbar zugleich.
    /// </summary>
    public class KiProtokollTests
    {
        private static readonly DateTime Zeitpunkt = new DateTime(2026, 8, 19, 14, 22, 31);

        private static KiAufruf Aufruf(params object?[] paare)
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(Beispielregister.MitId(), Beispielregister.Werte(paare));
            Assert.True(p.Gueltig, p.FehlerText());
            return p.Aufruf!;
        }

        [Fact]
        public void Zeile_HatAchtFelderUndKeinenUmbruch()
        {
            KiErgebnis e = KiErgebnis.Ok("4 Varianten", null, 4).MitDauer(TimeSpan.FromMilliseconds(128));
            string zeile = KiProtokoll.Zeile(Zeitpunkt, Aufruf("projekt_id", 1007), e, 1007);

            Assert.Equal(KiProtokoll.Feldzahl,
                         zeile.Split(new[] { KiProtokoll.Trenner }, StringSplitOptions.None).Length);
            Assert.DoesNotContain("\n", zeile);
            Assert.DoesNotContain("\r", zeile);
        }

        [Fact]
        public void Zeile_SiehtAusWieErwartet()
        {
            KiErgebnis e = KiErgebnis.Ok("4 Varianten", null, 4).MitDauer(TimeSpan.FromMilliseconds(128));

            Assert.Equal(
                "2026-08-19 14:22:31 | projekt_lesen | lesen | {\"projekt_id\":1007} | 1007 | " +
                "ausgefuehrt | 4x; 4 Varianten | 128 ms",
                KiProtokoll.Zeile(Zeitpunkt, Aufruf("projekt_id", 1007), e, 1007));
        }

        [Fact]
        public void OhneProjektbezug_StehtEinStrich()
        {
            KiErgebnis e = KiErgebnis.Ok("2 Projekte", null, 2);
            string zeile = KiProtokoll.Zeile(Zeitpunkt, Aufruf("projekt_id", 1007), e, 0);

            Assert.Equal(KiProtokoll.KeinProjekt,
                         zeile.Split(new[] { KiProtokoll.Trenner }, StringSplitOptions.None)[4]);
        }

        [Fact]
        public void Zeitstempel_IstInvariant_AuchUnterAndererKultur()
        {
            CultureInfo vorher = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                string zeile = KiProtokoll.Zeile(Zeitpunkt, Aufruf("projekt_id", 1007), KiErgebnis.Ok("ok"));
                Assert.StartsWith("2026-08-19 14:22:31", zeile);
            }
            finally { Thread.CurrentThread.CurrentCulture = vorher; }
        }

        [Theory]
        [InlineData(KiStatus.Ausgefuehrt, "ausgefuehrt")]
        [InlineData(KiStatus.Abgelehnt, "abgelehnt")]
        [InlineData(KiStatus.Abgebrochen, "abgebrochen")]
        [InlineData(KiStatus.Fehlgeschlagen, "fehlgeschlagen")]
        public void JederAusgangHatSeinenSchluessel(KiStatus status, string schluessel)
        {
            string zeile = KiProtokoll.Zeile(Zeitpunkt, "irgendwas", Schutzstufe.Lesen, "{}", 0,
                                             status, "", TimeSpan.Zero);

            Assert.Equal(schluessel, zeile.Split(new[] { KiProtokoll.Trenner }, StringSplitOptions.None)[5]);
        }

        [Fact]
        public void AbgelehnterVersuchOhneAufruf_WirdEbenfallsProtokolliert()
        {
            // Fachkonzept 3.6: protokolliert wird JEDER Versuch, auch der abgewiesene -
            // fuer den es noch gar keinen gepruefen KiAufruf gibt.
            string zeile = KiProtokoll.Zeile(Zeitpunkt, "datenbank_leeren", Schutzstufe.Lesen, "{}", 0,
                                             KiStatus.Abgelehnt, "Aktion unbekannt", TimeSpan.Zero);

            KiProtokollEintrag? e = KiProtokoll.Lies(zeile);
            Assert.NotNull(e);
            Assert.Equal(KiStatus.Abgelehnt, e!.Status);
            Assert.Equal("datenbank_leeren", e.Aktion);
        }

        // -------------------------------------------------------------------- Maskierung

        [Fact]
        public void MehrzeiligeMeldung_BleibtEineZeile()
        {
            KiErgebnis e = KiErgebnis.Fehlgeschlagen("Fehler in Zeile 1\r\nund Zeile 2 | mit Trenner");
            string zeile = KiProtokoll.Zeile(Zeitpunkt, Aufruf("projekt_id", 1007), e, 1007);

            Assert.DoesNotContain("\n", zeile);
            Assert.Equal(KiProtokoll.Feldzahl,
                         zeile.Split(new[] { KiProtokoll.Trenner }, StringSplitOptions.None).Length);
        }

        [Theory]
        [InlineData("ganz gewöhnlich")]
        [InlineData("mit | Trenner")]
        [InlineData("mit \\ Rückstrich")]
        [InlineData("mit\nUmbruch\r\nund CRLF")]
        [InlineData("")]
        public void MaskierenUndZurueck_LiefertDenAusgangstext(string text)
        {
            Assert.Equal(text, KiProtokoll.Demaskiere(KiProtokoll.Maskiere(text)));
        }

        // ------------------------------------------------------------------------- Lesen

        [Fact]
        public void ZeileLaesstSichVollstaendigZurueckLesen()
        {
            KiErgebnis e = KiErgebnis.Fehlgeschlagen("Text | mit Trenner\nund Umbruch")
                                     .MitDauer(TimeSpan.FromMilliseconds(1234));
            string zeile = KiProtokoll.Zeile(Zeitpunkt, Aufruf("projekt_id", 1007), e, 1007);

            KiProtokollEintrag? gelesen = KiProtokoll.Lies(zeile);

            Assert.NotNull(gelesen);
            Assert.Equal(Zeitpunkt, gelesen!.Zeitpunkt);
            Assert.Equal("projekt_lesen", gelesen.Aktion);
            Assert.Equal(Schutzstufe.Lesen, gelesen.Stufe);
            Assert.Equal("{\"projekt_id\":1007}", gelesen.Parameter);
            Assert.Equal(1007, gelesen.ProjektId);
            Assert.Equal(KiStatus.Fehlgeschlagen, gelesen.Status);
            Assert.Contains("Text | mit Trenner", gelesen.Ergebnis);
            Assert.Contains("\n", gelesen.Ergebnis);
            Assert.Equal(1234, gelesen.DauerMs);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("# Protokoll der KI-Aktionen (EPOS-Plan)")]
        [InlineData("nur ein Satz ohne Trenner")]
        [InlineData("a | b | c")]
        public void UnbrauchbareZeile_LiefertNull_StattZuWerfen(string zeile)
        {
            Assert.Null(KiProtokoll.Lies(zeile));
        }

        [Fact]
        public void KaputterZeitstempel_LiefertNull()
        {
            Assert.Null(KiProtokoll.Lies(
                "kein Datum | a | lesen | {} | - | ausgefuehrt | ok | 0 ms"));
        }

        [Fact]
        public void UnbekannteStufe_LiefertNull()
        {
            Assert.Null(KiProtokoll.Lies(
                "2026-08-19 14:22:31 | a | zaubern | {} | - | ausgefuehrt | ok | 0 ms"));
        }

        [Fact]
        public void LiesAlle_UeberliestVorspannUndKaputteZeilen()
        {
            string[] datei =
            {
                "# Protokoll der KI-Aktionen (EPOS-Plan)",
                "# Zeitstempel | Aktion | …",
                "",
                KiProtokoll.Zeile(Zeitpunkt, "projekte_auflisten", Schutzstufe.Lesen, "{}", 0,
                                  KiStatus.Ausgefuehrt, "2x", TimeSpan.FromMilliseconds(12)),
                "Müll",
                KiProtokoll.Zeile(Zeitpunkt, "projekt_lesen", Schutzstufe.Lesen, "{\"projekt_id\":1}", 1,
                                  KiStatus.Ausgefuehrt, "1x", TimeSpan.FromMilliseconds(7))
            };

            var eintraege = KiProtokoll.LiesAlle(datei);

            Assert.Equal(2, eintraege.Count);
            Assert.Equal("projekte_auflisten", eintraege[0].Aktion);
            Assert.Equal("projekt_lesen", eintraege[1].Aktion);
        }

        [Fact]
        public void Vorspann_BeginntJedeZeileMitEinemDoppelkreuz()
        {
            foreach (string z in KiProtokoll.Vorspann().Split('\n'))
                if (z.Length > 0) Assert.StartsWith("#", z);
        }

        [Fact]
        public void Vorspann_NenntAlleAchtFelder()
        {
            Assert.Equal(KiProtokoll.Feldzahl,
                         KiProtokoll.Kopfzeile()
                                    .Split(new[] { KiProtokoll.Trenner }, StringSplitOptions.None).Length);
            Assert.Contains(KiProtokoll.Kopfzeile(), KiProtokoll.Vorspann());
        }
    }
}
