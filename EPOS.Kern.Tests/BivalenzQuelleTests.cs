using System;
using EPOS.UI.Dialoge.Waermepumpe;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Datenquelle der Bivalenzherleitung gegen die Testdatenbank</b> (Übergabegrenze UB‑E1-b): Ein gekoppeltes
    /// Referenzprojekt (1047, AK1) liefert Übergabe und Heizlast oder benennt, warum es ohne Lauf nicht geht; ein
    /// Projekt ohne Kopplung (1017) liefert keine Gebäudeseite; das Kennfeld der Projektwärmepumpe steht bei 55 °C;
    /// die Abbildung rechnet daraus eine Zeile, die ohne Einbindung ruht.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BivalenzQuelleTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public BivalenzQuelleTests(ITestOutputHelper ausgabe) => _ausgabe = ausgabe;

        [Fact]
        public void Gekoppeltes_Referenzprojekt_liefert_eine_ruhende_Herleitung()
        {
            using var kultur = new Kulturvorrichtung("de-DE");
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BivalenzProjektdaten p = BivalenzQuelle.Projekt(1047);
            Assert.Equal("", p.Befund);
            Assert.True(p.Gebaeude != null || p.Unvollstaendig);
            Assert.True(BivalenzQuelle.Kennfeld(1672046, 55.0).Count > 0);

            var m = new WErzeugerModel { ID_Projekt = 1047, ID_WP = 1672046, Vorlauf = 55 };
            var d = new WaermepumpeAnlageDaten { IdWp = 1672046, Vorlauf = 55, Betriebsart = DbWerte.WP_BETRIEBSART_TEILPARALLEL };
            BivalenzAbbildung.Lesen(m, d);
            WaermepumpeBivalenzWerte? w = d.BivalenzRechnen!(d);
            Assert.NotNull(w);
            Assert.Contains(w!.Kennzeichen, new[] { BivalenzKennzeichen.NichtWirksam, BivalenzKennzeichen.Unvollstaendig });
            string zeile = WaermepumpeBivalenzText.Zeile(w, new WaermepumpeKonfigurationTexte());
            _ausgabe.WriteLine(zeile);
            Assert.False(string.IsNullOrWhiteSpace(zeile));
            if (w.Kennzeichen == BivalenzKennzeichen.NichtWirksam)
            {
                Assert.StartsWith("Einbindung nicht gesetzt — Übergabegrenze ruht.", zeile);
                Assert.True(w.UebergabeKw > 0.0 && w.HeizlastKw > 0.0);
            }
        }

        [Fact]
        public void Projekt_ohne_Kopplung_hat_keine_Gebaeudeseite()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BivalenzProjektdaten p = BivalenzQuelle.Projekt(1017);
            Assert.Null(p.Gebaeude);
            Assert.False(p.Unvollstaendig);
            Assert.Equal(28.0, p.KesselleistungKw, 6);
        }
    }
}
