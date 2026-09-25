using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Stufe G3, Welle C — die Kernseite der zwei Verwaltungen ohne Datenbank: die Kennwerte eines
    /// Aufbaus (Summenfuß), die Eingabeprüfung nach Mehrzonenkonzept 5.3, die Umrechnung der
    /// Schichtdicke, die Anzeigetexte und der Filterausdruck „ohne Wert".
    /// </summary>
    public class BauteilaufbauKennwerteTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static BauteilschichtModel Stoff(double dickeM, double lambda, double rho, double cp)
            => new BauteilschichtModel { Dicke = dickeM, Lambda = lambda, Rho = rho, Cp = cp };

        private static BauteilaufbauModel Wand(params BauteilschichtModel[] schichten)
            => new BauteilaufbauModel { Bezeichner = "Wand", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Schichten = schichten.ToList() };

        [Fact]
        public void Die_Kennwerte_einer_Wand_rechnen_ueber_den_Bauteilweg()
        {
            BauteilaufbauModel wand = Wand(Stoff(0.175, 0.99, 1800, 1000), Stoff(0.14, 0.035, 30, 1030));

            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(wand);

            double r = 0.175 / 0.99 + 0.14 / 0.035;
            Assert.True(k.Gerechnet, k.Grund);
            Assert.Equal("", k.Grund);
            Assert.Equal(90.0, k.NeigungGrad);
            Assert.Equal(0.13, k.RSi_M2KW, 12);
            Assert.Equal(0.04, k.RSe_M2KW, 12);
            Assert.Equal(r, k.R_M2KW!.Value, 12);
            Assert.Equal(1.0 / (0.13 + r + 0.04), k.U_WM2K!.Value, 12);
            Assert.Equal((1800 * 1000 * 0.175 + 30 * 1030 * 0.14) / 1000.0, k.Kapazitaet_KJM2K!.Value, 9);
            Assert.Equal(2, k.RJeSchicht_M2KW.Count);
            Assert.Equal(0.175 / 0.99, k.RJeSchicht_M2KW[0]!.Value, 12);

            // Die Bezugsperiode nach Gl. (10a)-(10d) steht mit ihrem Beleg.
            Assert.Contains(k.Bezugsperiode_D, new double?[] { 2.0, 7.0 });
            Assert.True(k.R1Rel.HasValue && k.C1Rel.HasValue);
            Assert.True(k.KapazitaetWirksam_KJM2K > 0);
            Assert.Equal("", k.PeriodeGrund);
        }

        [Fact]
        public void Die_Kennwerte_nehmen_dieselbe_Rechnung_wie_der_Lauf()
        {
            BauteilaufbauModel wand = Wand(Stoff(0.015, 0.7, 1400, 1000), Stoff(0.24, 0.45, 1000, 1000));
            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(wand);

            var schichten = new[]
            {
                new Schicht(0.015, 0.7, 1400, 1000),
                new Schicht(0.24, 0.45, 1000, 1000)
            };
            Schichtkennwerte lauf = Bauteilreduktion.UWertAusSchichten(schichten, 90.0, Bauteilrand.Aussenluft);
            Bezugsperiodenwahl wahl = Bauteilreduktion.BezugsperiodeWaehlen(schichten, 1.0, Waermestromrichtung.Horizontal);

            Assert.Equal(lauf.U_WM2K, k.U_WM2K!.Value, 15);
            Assert.Equal(wahl.Periode_d, k.Bezugsperiode_D!.Value);
            Assert.Equal(wahl.Kennwerte.C1_Jk / 1000.0, k.KapazitaetWirksam_KJM2K!.Value, 12);
        }

        [Fact]
        public void Die_Bauteilart_waehlt_die_Uebergaenge()
        {
            BauteilaufbauModel dach = Wand(Stoff(0.2, 0.04, 30, 1000));
            dach.Bauteilart = DbWerte.BAUTEILART_DACH;
            Assert.Equal(0.10, BauteilaufbauCtrl.Kennwerte(dach).RSi_M2KW, 12);

            dach.Bauteilart = DbWerte.BAUTEILART_BODENPLATTE;
            Assert.Equal(0.17, BauteilaufbauCtrl.Kennwerte(dach).RSi_M2KW, 12);

            dach.Bauteilart = null;
            BauteilaufbauKennwerte jede = BauteilaufbauCtrl.Kennwerte(dach);
            Assert.Equal(90.0, jede.NeigungGrad);
            Assert.Equal(0.13, jede.RSi_M2KW, 12);
        }

        [Fact]
        public void Eine_ruhende_Luftschicht_rechnet_nach_Tabelle_8_ohne_Masse()
        {
            var luft = new BauteilschichtModel { Dicke = 0.05, IstLuftschicht = true };
            BauteilaufbauModel wand = Wand(Stoff(0.1, 0.5, 1000, 1000), luft, Stoff(0.1, 0.5, 1000, 1000));

            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(wand);

            Assert.True(k.Gerechnet, k.Grund);
            Assert.Equal(Bauteilreduktion.Luftschichtwiderstand(0.05, Waermestromrichtung.Horizontal),
                         k.RJeSchicht_M2KW[1]!.Value, 12);
            Assert.Equal(2 * 1000 * 1000 * 0.1 / 1000.0, k.Kapazitaet_KJM2K!.Value, 9);
        }

        [Fact]
        public void Eine_Luecke_laesst_die_Summen_leer_und_nennt_sie()
        {
            BauteilaufbauModel wand = Wand(Stoff(0.1, 0.5, 1000, 1000), new BauteilschichtModel { Dicke = 0.1, Rho = 1000, Cp = 1000 });

            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(wand);

            Assert.False(k.Gerechnet);
            Assert.Null(k.R_M2KW);
            Assert.Equal(string.Format(R.BAUTEIL_MSG_SCHICHT_LAMBDA, 2), k.Grund);
            Assert.NotNull(k.RJeSchicht_M2KW[0]);
            Assert.Null(k.RJeSchicht_M2KW[1]);

            BauteilaufbauKennwerte leer = BauteilaufbauCtrl.Kennwerte(Wand());
            Assert.False(leer.Gerechnet);
            Assert.Equal(R.BAUTEIL_MSG_KEINE_SCHICHT, leer.Grund);

            // Ein Stoffwert ausserhalb des Bandes nennt der Bauteilweg mit Schichtnummer.
            BauteilaufbauKennwerte band = BauteilaufbauCtrl.Kennwerte(Wand(Stoff(0.1, 0.5, 1000, 1000), Stoff(0.1, 900, 1000, 1000)));
            Assert.False(band.Gerechnet);
            Assert.Contains("2", band.Grund);
        }

        [Fact]
        public void Ein_Aufbau_ohne_Speichermasse_hat_U_aber_keine_Bezugsperiode()
        {
            BauteilaufbauModel luft = Wand(new BauteilschichtModel { Dicke = 0.05, IstLuftschicht = true });

            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(luft);

            Assert.True(k.Gerechnet, k.Grund);
            Assert.Null(k.Bezugsperiode_D);
            Assert.NotEqual("", k.PeriodeGrund);
            Assert.Null(BauteilaufbauCtrl.Kennwerte(luft, mitBezugsperiode: false).Bezugsperiode_D);
        }

        [Fact]
        public void Die_Eingabepruefung_haelt_die_Regeln_aus_Mehrzonenkonzept_5_3()
        {
            Assert.Null(BauteilaufbauCtrl.EingabePruefen(Wand(Stoff(0.1, 0.5, 1000, 1000))));

            Assert.Equal(R.BAUTEIL_MSG_AUFBAU_NAME_LEER, BauteilaufbauCtrl.EingabePruefen(new BauteilaufbauModel()));
            Assert.Equal(R.BAUTEIL_MSG_KEINE_SCHICHT, BauteilaufbauCtrl.EingabePruefen(Wand()));

            string duenn = BauteilaufbauCtrl.EingabePruefen(Wand(Stoff(0.0005, 0.5, 1000, 1000)));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.BAUTEIL_MSG_SCHICHT_DICKE_BAND, 1, 0.5, 1.0, 1000.0), duenn);
            Assert.NotNull(BauteilaufbauCtrl.EingabePruefen(Wand(Stoff(1.2, 0.5, 1000, 1000))));

            var dickeLuft = new BauteilschichtModel { Dicke = 0.4, IstLuftschicht = true };
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.BAUTEIL_MSG_LUFTSCHICHT_DICKE, 1, 400.0, 300.0),
                         BauteilaufbauCtrl.EingabePruefen(Wand(dickeLuft)));

            var ohneLambda = new BauteilschichtModel { Dicke = 0.1, Rho = 1000, Cp = 1000 };
            Assert.Equal(string.Format(R.BAUTEIL_MSG_SCHICHT_LAMBDA, 1), BauteilaufbauCtrl.EingabePruefen(Wand(ohneLambda)));

            var ohneRho = new BauteilschichtModel { Dicke = 0.1, Lambda = 0.5, Cp = 1000 };
            Assert.Equal(string.Format(R.BAUTEIL_MSG_SCHICHT_FEHLT, 1, R.KFLT_SP_RHO), BauteilaufbauCtrl.EingabePruefen(Wand(ohneRho)));

            // Eine Luftschicht mit λ darf ohne ρ und c_p stehen.
            var luftMitLambda = new BauteilschichtModel { Dicke = 0.02, IstLuftschicht = true, Lambda = 0.1 };
            Assert.Null(BauteilaufbauCtrl.EingabePruefen(Wand(Stoff(0.1, 0.5, 1000, 1000), luftMitLambda)));

            // Stoffwerte ausserhalb des Bandes (Kernregel Pruefen).
            Assert.NotNull(BauteilaufbauCtrl.EingabePruefen(Wand(Stoff(0.1, 900, 1000, 1000))));

            // Die Reihenfolge, wo sie gesetzt ist, laeuft lueckenlos ab 1.
            BauteilschichtModel a = Stoff(0.1, 0.5, 1000, 1000), b = Stoff(0.1, 0.5, 1000, 1000);
            a.Reihenfolge = 1;
            b.Reihenfolge = 3;
            Assert.Equal(string.Format(R.BAUTEIL_MSG_REIHENFOLGE, 2, 3), BauteilaufbauCtrl.EingabePruefen(Wand(a, b)));

            // Fenster und Vorhangfassade rechnen aus dem U-Wert.
            BauteilaufbauModel fenster = Wand(Stoff(0.1, 0.5, 1000, 1000));
            fenster.Bauteilart = DbWerte.BAUTEILART_FENSTER;
            Assert.Equal(string.Format(R.SIMENG_G3_TRANSPARENT_SCHICHTEN, "Wand"), BauteilaufbauCtrl.EingabePruefen(fenster));
        }

        [Theory]
        [InlineData(0.175)]
        [InlineData(0.0125)]
        [InlineData(0.001)]
        [InlineData(1.0)]
        [InlineData(0.36499999)]
        public void Die_Schichtdicke_reist_stabil_zwischen_m_und_mm(double meter)
        {
            double mm = BauteilaufbauCtrl.DickeMm(meter);
            Assert.Equal(meter * 1000.0, mm, 6);
            Assert.Equal(meter, BauteilaufbauCtrl.DickeM(mm), 12);
            Assert.Equal(mm, BauteilaufbauCtrl.DickeMm(BauteilaufbauCtrl.DickeM(mm)));
        }

        [Fact]
        public void Bauteilart_und_Herkunft_erscheinen_als_Text()
        {
            Assert.Equal(R.BTA_ART_AUSSENWAND, BauteilaufbauCtrl.BauteilartText(DbWerte.BAUTEILART_AUSSENWAND));
            Assert.Equal("Außenwand", BauteilaufbauCtrl.BauteilartText(DbWerte.BAUTEILART_AUSSENWAND));
            Assert.Equal(R.BTA_ART_JEDE, BauteilaufbauCtrl.BauteilartText(null));
            Assert.Equal("UNBEKANNT", BauteilaufbauCtrl.BauteilartText("UNBEKANNT"));
            foreach (string art in DbWerte.BAUTEILARTEN)
                Assert.NotEqual(art, BauteilaufbauCtrl.BauteilartText(art));

            Assert.Equal(R.GIMP_HERKUNFT_VORGABE, BaustoffCtrl.HerkunftText(DbWerte.HERKUNFT_VORGABE));
            Assert.Equal(R.GIMP_HERKUNFT_MANUELL, BaustoffCtrl.HerkunftText(DbWerte.HERKUNFT_MANUELL));
            Assert.Equal("", BaustoffCtrl.HerkunftText(null));
        }

        /// <summary>
        /// Der Filterausdruck „=" (ohne Wert) — der Werkzeugschalter „nur herstellerneutral" der
        /// Baustoffverwaltung. Er trifft den Leerwert einer Text- und einer Zahlenspalte, sonst nichts.
        /// </summary>
        [Fact]
        public void Der_Ausdruck_ohne_Wert_trifft_nur_leere_Zellen()
        {
            Katalogfilterprofil profil = Katalogfilterprofil.FuerBaustoff();
            var zeilen = new List<Katalogfilterzeile>
            {
                new Katalogfilterzeile(1, "Neutral").MitText(Katalogfilterprofil.SpHersteller, "")
                                                    .MitZahl(Katalogfilterprofil.SpLambda, 0.5, 3),
                new Katalogfilterzeile(2, "Marke").MitText(Katalogfilterprofil.SpHersteller, "Hersteller A")
                                                  .MitZahl(Katalogfilterprofil.SpLambda, null, 3)
            };

            var stand = new Katalogfilterstand();
            stand.Setzen(Katalogfilterprofil.SpHersteller, Katalogfilterprofil.AUSDRUCK_LEER);
            Assert.Equal(new[] { "Neutral" }, Katalogfilter.Anwenden(profil, zeilen, stand).Select(z => z.Bezeichner));

            stand.Zuruecksetzen();
            stand.Setzen(Katalogfilterprofil.SpLambda, Katalogfilterprofil.AUSDRUCK_LEER);
            Assert.Equal(new[] { "Marke" }, Katalogfilter.Anwenden(profil, zeilen, stand).Select(z => z.Bezeichner));

            // Ein Gleichheitszeichen MIT Zahl bleibt der Vergleich.
            stand.Zuruecksetzen();
            stand.Setzen(Katalogfilterprofil.SpLambda, "=0,5");
            Assert.Equal(new[] { "Neutral" }, Katalogfilter.Anwenden(profil, zeilen, stand).Select(z => z.Bezeichner));
        }

        [Fact]
        public void Das_Profil_der_Aufbauliste_fuehrt_U_R_und_C()
        {
            Katalogfilterprofil p = Katalogfilterprofil.FuerBauteilaufbau();
            Assert.Equal(new[]
            {
                Katalogfilterprofil.SpBezeichner, Katalogfilterprofil.SpBauteilart, Katalogfilterprofil.SpUWert,
                Katalogfilterprofil.SpRWert, Katalogfilterprofil.SpKapazitaet, Katalogfilterprofil.SpSchichten,
                Katalogfilterprofil.SpDicke, Katalogfilterprofil.SpHerkunft
            }, p.Spalten.Select(s => s.Schluessel));
            Assert.Equal(Katalogspaltenrang.Immer, p.Spalte(Katalogfilterprofil.SpUWert).Rang);
        }
    }

    /// <summary>
    /// Stufe G3, Welle C — die Kernseite der zwei Verwaltungen an der Testdatenbank: die Zeilen der
    /// Listen (Schlüssel = Id, U/R/C aus dem Bauteilweg), das Duplizieren eines Aufbaus samt
    /// Schichten und die Verwendung der Stoffe im Aufbaukatalog.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BauteilaufbauVerwaltungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static BauteilaufbauModel Wand(string name)
            => new BauteilaufbauModel
            {
                Bezeichner = name,
                Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                Schichten = new List<BauteilschichtModel>
                {
                    new BauteilschichtModel { ID_Baustoff = 1, Dicke = 0.015 },
                    new BauteilschichtModel { Dicke = 0.2, Lambda = 0.04, Rho = 30, Cp = 1000 }
                }
            };

        [Fact]
        public void Die_Aufbauliste_fuehrt_Id_als_Schluessel_und_U_aus_dem_Bauteilweg()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new BauteilaufbauCtrl();
            BauteilaufbauCtrl.Ergebnis e = ctrl.KatalogSpeichern(Wand("Probewand G3C"));
            Assert.True(e.Ok, e.Meldung);

            Katalogfilterzeile zeile = BauteilaufbauCtrl.Katalogfilterzeilen().Single(z => z.Id == e.Id);
            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(ctrl.LesenKatalogsatz(e.Id));

            Assert.Equal(e.Id.ToString(CultureInfo.InvariantCulture), zeile.Schluessel);
            Assert.Equal(R.BTA_ART_AUSSENWAND, zeile.Text(Katalogfilterprofil.SpBauteilart));
            Assert.Equal(k.U_WM2K!.Value, zeile.Zahl(Katalogfilterprofil.SpUWert)!.Value, 12);
            Assert.Equal(k.R_M2KW!.Value, zeile.Zahl(Katalogfilterprofil.SpRWert)!.Value, 12);
            Assert.Equal(2, zeile.Zahl(Katalogfilterprofil.SpSchichten));
            Assert.Equal(R.GIMP_HERKUNFT_MANUELL, zeile.Text(Katalogfilterprofil.SpHerkunft));
            Assert.False(zeile.Geschuetzt);
        }

        [Fact]
        public void Duplizieren_kopiert_den_Aufbau_samt_Schichten_als_eigenen_Satz()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new BauteilaufbauCtrl();
            BauteilaufbauCtrl.Ergebnis e = ctrl.KatalogSpeichern(Wand("Probewand G3C Original"));
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(1, BauteilaufbauCtrl.SchlossSetzen(new[] { e.Id }, true).Geaendert.Count);

            BauteilaufbauCtrl.Ergebnis kopie = ctrl.KatalogDuplizieren(e.Id, "Probewand G3C Kopie");
            Assert.True(kopie.Ok, kopie.Meldung);
            Assert.NotEqual(e.Id, kopie.Id);

            BauteilaufbauModel alt = ctrl.LesenKatalogsatz(e.Id);
            BauteilaufbauModel neu = ctrl.LesenKatalogsatz(kopie.Id);
            Assert.True(alt.ReadOnly);
            Assert.False(neu.ReadOnly);
            Assert.Equal(DbWerte.HERKUNFT_MANUELL, neu.Herkunft);
            Assert.Equal(alt.Bauteilart, neu.Bauteilart);
            Assert.Equal(alt.Schichten.Count, neu.Schichten.Count);
            for (int i = 0; i < alt.Schichten.Count; i++)
            {
                Assert.Equal(alt.Schichten[i].ID_Baustoff, neu.Schichten[i].ID_Baustoff);
                Assert.Equal(alt.Schichten[i].Dicke, neu.Schichten[i].Dicke);
                Assert.Equal(alt.Schichten[i].Lambda, neu.Schichten[i].Lambda);
                Assert.Equal(i + 1, neu.Schichten[i].Reihenfolge);
            }

            // Der Name ist frei - ein zweites Duplizieren unter demselben Namen scheitert benannt.
            BauteilaufbauCtrl.Ergebnis doppelt = ctrl.KatalogDuplizieren(e.Id, "Probewand G3C Kopie");
            Assert.False(doppelt.Ok);
            Assert.Equal(string.Format(R.BAUTEIL_MSG_AUFBAU_NAME_VERGEBEN, "Probewand G3C Kopie"), doppelt.Meldung);
        }

        [Fact]
        public void Die_Verwendung_zaehlt_die_Katalogschichten_je_Stoff()
        {
            if (!_db.Vorhanden) return;
            int vorher = BaustoffCtrl.KatalogVerwendung().TryGetValue(1, out int n) ? n : 0;
            var ctrl = new BauteilaufbauCtrl();
            Assert.True(ctrl.KatalogSpeichern(Wand("Probewand G3C A")).Ok);
            Assert.True(ctrl.KatalogSpeichern(Wand("Probewand G3C B")).Ok);

            IReadOnlyDictionary<int, int> verwendung = BaustoffCtrl.KatalogVerwendung();
            Assert.Equal(vorher + 2, verwendung[1]);
            Assert.All(verwendung.Values, v => Assert.True(v > 0));
        }

        [Fact]
        public void Die_Baustoffliste_fuehrt_die_Id_als_Schluessel()
        {
            if (!_db.Vorhanden) return;
            IReadOnlyList<Katalogfilterzeile> zeilen = BaustoffCtrl.Katalogfilterzeilen();
            Assert.All(zeilen, z => Assert.Equal(z.Id.ToString(CultureInfo.InvariantCulture), z.Schluessel));
            Assert.Equal(zeilen.Count, zeilen.Select(z => z.Schluessel).Distinct().Count());

            // Die herstellerneutralen Normzeilen tragen den Leerwert - der Trichter "=" findet genau sie.
            var stand = new Katalogfilterstand();
            stand.Setzen(Katalogfilterprofil.SpHersteller, Katalogfilterprofil.AUSDRUCK_LEER);
            Assert.Equal(65, Katalogfilter.Anwenden(Katalogfilterprofil.FuerBaustoff(), zeilen, stand).Count);
        }
    }
}
