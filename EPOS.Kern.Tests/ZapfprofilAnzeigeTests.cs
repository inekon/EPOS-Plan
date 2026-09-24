using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.ZapfprofilTestbau;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>θ_Anzeige und Schwelle der Stundenzählung</b> (Umsetzungskonzept Zapfprofilgenerator 4.0,
    /// 4.6, N9 (h); Stufe Z4): Im Eingang gilt die Laufangabe des Dialogs, sonst die Einstellung
    /// (<c>Zapfprofil.Anzeigetemperatur</c>, <c>Zapfprofil.Stundenschwelle</c>), sonst die Vorgabe
    /// des Parametersatzes (gleiche Schlüssel, freier Paketteil); eine ungültige Einstellung ist
    /// benannt verworfen, dann gilt die Vorgabe. Der Rechenweg weist eine Anzeigetemperatur nicht über
    /// θ̄_KW und eine negative Schwelle benannt ab (Hinweis). Auf der Arbeitskopie der Testdatenbank
    /// (Projekt 1006; ohne sie schweigen diese Fälle) bzw. mit dem fiktiven Satz. Werte erfunden.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilAnzeigeTests
    {
        private const int PROJEKT = 1006;

        [Fact]
        public void Laufangabe_vor_Einstellung_vor_Parametersatz()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            IEinstellungen vorher = Dienste.Einstellungen;
            try
            {
                var einstellungen = new FluechtigeEinstellungen();
                Dienste.Einstellungen = einstellungen;
                ZapfprofilStand stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], null);
                bool[] we = new bool[365];

                // Ohne Laufangabe und Einstellung: die Vorgabe des Parametersatzes (Werte aus der Datenbank).
                Parametersatz ps = ZapfprofilCtrl.Parameter();
                double vorgabeC = ps.Wert(ZapfParameter.ANZEIGETEMPERATUR), vorgabeKw = ps.Wert(ZapfParameter.STUNDENSCHWELLE);
                Zapfprofileingang ohne = ZapfprofilCtrl.Eingang(PROJEKT, stand, 0, we);
                Assert.Equal(vorgabeC, ohne.AnzeigetemperaturC);
                Assert.Equal(vorgabeKw, ohne.SchwelleKw);
                Assert.Empty(ohne.Vorhinweise);

                einstellungen.Schreib(ZapfprofilCtrl.EINSTELLUNG_ANZEIGETEMPERATUR, "45.5");
                einstellungen.Schreib(ZapfprofilCtrl.EINSTELLUNG_STUNDENSCHWELLE, "7");
                Zapfprofileingang mitEinstellung = ZapfprofilCtrl.Eingang(PROJEKT, stand, 0, we);
                Assert.Equal(45.5, mitEinstellung.AnzeigetemperaturC);
                Assert.Equal(7.0, mitEinstellung.SchwelleKw);

                Zapfprofileingang mitLauf = ZapfprofilCtrl.Eingang(PROJEKT, stand with { Anzeige = new ZapfAnzeige(40.0, null) }, 0, we);
                Assert.Equal(40.0, mitLauf.AnzeigetemperaturC);
                Assert.Equal(7.0, mitLauf.SchwelleKw);

                // Eine ungültige Einstellung nennt ein Hinweis; dann gilt die Vorgabe.
                einstellungen.Schreib(ZapfprofilCtrl.EINSTELLUNG_STUNDENSCHWELLE, "viel");
                Zapfprofileingang ungueltig = ZapfprofilCtrl.Eingang(PROJEKT, stand, 0, we);
                Assert.Equal(vorgabeKw, ungueltig.SchwelleKw);
                ZapfHinweis h = Assert.Single(ungueltig.Vorhinweise);
                Assert.Equal(ZapfprofilCtrl.HINWEIS_EINSTELLUNG_UNGUELTIG, h.Code);
                Assert.Equal(new object[] { ZapfprofilCtrl.EINSTELLUNG_STUNDENSCHWELLE, "viel" }, h.Satz.Werte.ToArray());

                // Ohne Parameter im Katalog: keine Anzeige, kein Hinweis.
                Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_TwwParameter_STAMM WHERE Schluessel IN ('"
                                                      + ZapfParameter.ANZEIGETEMPERATUR + "', '" + ZapfParameter.STUNDENSCHWELLE + "')"));
                Dienste.Einstellungen = new FluechtigeEinstellungen();
                Zapfprofileingang leer = ZapfprofilCtrl.Eingang(PROJEKT, stand, 0, we);
                Assert.Null(leer.AnzeigetemperaturC);
                Assert.Null(leer.SchwelleKw);
                Assert.Empty(leer.Vorhinweise);
            }
            finally { Dienste.Einstellungen = vorher; }
        }

        /// <summary>
        /// Der Rechenweg prüft die Anzeige benannt: Eine Anzeigetemperatur nicht über θ̄_KW der Zone
        /// (hier 12 °C) gibt keine Literanzeige und einen Hinweis mit Zone und Temperaturen; eine
        /// negative oder nicht endliche Schwelle zählt nicht und nennt es. Gültige Werte rechnen.
        /// </summary>
        [Fact]
        public void Anzeigetemperatur_nicht_ueber_dem_Kaltwasser_und_negative_Schwelle_sind_benannt()
        {
            IReadOnlyList<Nutzungsart> katalog = new[] { Art(1) };
            ZonenStand zone = Zone("Zone A", 1, 10.0, 1) with { KaltwasserMittelC = 12.0 };
            ZapfprofilErgebnis Rechnen(double? anzeigeC, double? schwelleKw)
                => ZapfprofilRechner.Rechnen(Eingang(Projekt(), Parameter(), zone) with { AnzeigetemperaturC = anzeigeC, SchwelleKw = schwelleKw },
                                             katalog);

            ZapfprofilErgebnis gut = Rechnen(45.0, 0.1);
            Assert.NotNull(gut.Kennzahlen.ZapfungLiterJeTag);
            Assert.NotNull(gut.Kennzahlen.StundenUeberSchwelle);
            Assert.Equal(0.1, gut.Dauerlinie.SchwelleKw);
            Assert.DoesNotContain(gut.Hinweise, h => h.Code == ZapfprofilRechner.HINWEIS_ANZEIGETEMPERATUR
                                                  || h.Code == ZapfprofilRechner.HINWEIS_STUNDENSCHWELLE);

            ZapfprofilErgebnis kalt = Rechnen(12.0, null);
            Assert.Null(kalt.Kennzahlen.ZapfungLiterJeTag);
            Assert.Null(kalt.JeZone[0].ZapfungLiterJeTag);
            ZapfHinweis a = Assert.Single(kalt.Hinweise, h => h.Code == ZapfprofilRechner.HINWEIS_ANZEIGETEMPERATUR);
            Assert.Equal("Zone A", a.Zone);
            Assert.False(a.Warnung);
            Assert.Equal(new object[] { "Zone A", 12.0, 12.0 }, a.Satz.Werte.ToArray());
            Assert.Equal("Die Anzeigetemperatur (12 °C) liegt nicht über dem Kaltwassermittel der Zone „Zone A“ (12 °C) — die Zone zeigt keine Liter.",
                         a.Text);

            foreach (double falsch in new[] { -0.5, double.NaN })
            {
                ZapfprofilErgebnis negativ = Rechnen(null, falsch);
                Assert.Null(negativ.Kennzahlen.StundenUeberSchwelle);
                Assert.Null(negativ.Kennzahlen.SchwelleKw);
                Assert.Null(negativ.Dauerlinie.StundenUeberSchwelle);
                ZapfHinweis s = Assert.Single(negativ.Hinweise, h => h.Code == ZapfprofilRechner.HINWEIS_STUNDENSCHWELLE);
                Assert.Equal("HINWEIS_STUNDENSCHWELLE_UNGUELTIG", s.Satz.Kennung);
            }
        }
    }
}
