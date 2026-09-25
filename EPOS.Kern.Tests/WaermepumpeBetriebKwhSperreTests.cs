using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>E23 — die Betriebskosten der Wärmepumpe werden nicht je kWh bemessen</b>
    /// (Anwenderentscheide E20‑Q6 b und 25.09.2026: „Strom-kWh sind Energiekosten";
    /// „Betriebskosten bei Wärmepumpe: fixer Jahresbetrag oder % von
    /// Investitionskosten, nicht nach kWh/a — weder Strom noch Wärme").
    ///
    /// <para><b>Was hier bewiesen wird.</b> Die Landkarte
    /// (<see cref="WirtschaftlichkeitCtrl.BasisGrund(string, int, bool, bool)"/>)
    /// antwortet für „je kWh elektrisch" und „je kWh thermisch" an der Wärmepumpe
    /// GEWERK, und die Auswahl folgt ihr. Die übrigen Gewerke antworten wie zuvor. Eine
    /// Bestandszeile bleibt über „benutzt" wählbar, rechnet weiter und trägt in der
    /// Herleitungszeile den Vermerk „Altbestand" — im Projektmodus, auch ohne
    /// Bezugsgröße. „% der Endenergiekosten" und „% des Endenergiebedarfs" bleiben
    /// wählbar (E23‑Q6/Q7).</para>
    ///
    /// <para>Der Bestand ist ausgezählt (Testdatenbank, Auslieferungsvorlagen, Live-DB
    /// lesend, 25.09.2026): keine Projekt- und keine Vorlagenzeile trägt eine kWh-Art an
    /// der Wärmepumpe.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermepumpeBetriebKwhSperreTests : IDisposable
    {
        private const int K_WAERMEPUMPE = 1;
        private const int K_HEIZKESSEL = 2;
        private const int K_PHOTOVOLTAIK = 3;
        private const int K_SOLARTHERMIE = 4;
        private const int K_STROMSPEICHER = 5;
        private const int K_BHKW = 7;

        /// <summary>Projekt 1026, Betriebszeile der Wärmepumpe (Anlage 14917) — dieselbe
        /// Zeile wie in <see cref="BetriebskostenBemessungsmatrixTests"/>.</summary>
        private const int Z_1026_WP = 101600568;

        private static readonly string[] KWH_ARTEN =
        {
            DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,
            DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH,
        };

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =====================================================================
        //  Landkarte und Auswahl
        // =====================================================================

        [Fact]
        public void Die_Landkarte_sperrt_beide_kWh_Arten_an_der_Waermepumpe_in_beiden_Rastern()
        {
            foreach (string bem in KWH_ARTEN)
                foreach (bool invest in new[] { false, true })
                {
                    Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK,
                                 WirtschaftlichkeitCtrl.BasisGrund(bem, K_WAERMEPUMPE, false, invest));
                    Assert.False(BemessungKatalog.PasstZuGewerk(bem, K_WAERMEPUMPE, invest));
                }

            // Auch mit der Anlage in der Hand (Grund der Zeile ohne Lauf).
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK,
                         WirtschaftlichkeitCtrl.BasisGrundFuerZeile(
                             DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, K_WAERMEPUMPE, 0));
        }

        /// <summary>Die übrigen Gewerke behalten ihre kWh-Arten aus dem Lauf.</summary>
        [Fact]
        public void Die_uebrigen_Gewerke_behalten_ihre_kWh_Arten()
        {
            foreach (int k in new[] { K_PHOTOVOLTAIK, K_STROMSPEICHER, K_BHKW })
                Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_LAUF,
                             WirtschaftlichkeitCtrl.BasisGrund(
                                 DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, k));
            foreach (int k in new[] { K_HEIZKESSEL, K_BHKW, K_SOLARTHERMIE })
                Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_LAUF,
                             WirtschaftlichkeitCtrl.BasisGrund(
                                 DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH, k));
        }

        [Fact]
        public void Das_Betriebsraster_der_Waermepumpe_bietet_keine_kWh_Art_mehr()
        {
            List<string> betrieb = Persistenzwerte(K_WAERMEPUMPE, false, null);
            foreach (string bem in KWH_ARTEN) Assert.DoesNotContain(bem, betrieb);

            // Erlaubt bleiben die Kostenwelt und die beiden Endenergie-Anteile (E23‑Q6/Q7).
            Assert.Contains(DbWerte.BEMESSUNG_JAHRESBETRAG, betrieb);
            Assert.Contains(DbWerte.BEMESSUNG_PROZENT_INVESTITION, betrieb);
            Assert.Contains(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN, betrieb);
            Assert.Contains(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF, betrieb);

            // Das Investitionsraster bleibt, wie E20 es gebaut hat.
            Assert.Contains(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,
                            Persistenzwerte(K_WAERMEPUMPE, true, null));
        }

        /// <summary>DER SCHUTZ DES BESTANDS: Trägt eine vorhandene Zeile die Art, bleibt
        /// sie in der Liste — sonst verlöre die Zeile beim Anzeigen ihren Wert.</summary>
        [Fact]
        public void Eine_Bestandszeile_behaelt_ihre_kWh_Art_in_der_Liste()
        {
            foreach (string bem in KWH_ARTEN)
            {
                var benutzt = new HashSet<string>(StringComparer.Ordinal) { bem };
                Assert.Contains(bem, Persistenzwerte(K_WAERMEPUMPE, false, benutzt));
            }
        }

        // =====================================================================
        //  Bestand rechnet weiter
        // =====================================================================

        /// <summary>Die Menge einer Bestandszeile kommt aus dem Lauf, nicht aus der
        /// Landkarte: 1026 trägt an der Wärmepumpe Strom- und Wärmemenge.</summary>
        [Fact]
        public void Eine_Bestandszeile_rechnet_weiter()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (string bem in KWH_ARTEN)
            {
                string grund;
                double? basis = WirtschaftlichkeitCtrl.FrischeBasis(Z_1026_WP, bem, out grund);
                Assert.True(basis.HasValue && basis.Value > 0, bem + " ohne Menge (" + grund + ")");
                Assert.Equal("", grund);
            }
        }

        // =====================================================================
        //  Der Vermerk an der Herleitung
        // =====================================================================

        [Fact]
        public void Die_Bestandszeile_traegt_den_Vermerk_an_der_Herleitung()
        {
            string vermerk = R.KDLG_HERL_ALTBESTAND_WP_KWH;
            Assert.Equal("Altbestand — Betriebskosten der Wärmepumpe werden nicht je kWh bemessen",
                         vermerk);

            foreach (string bem in KWH_ARTEN)
            {
                // Mit Bezugsgröße: hinter der Herleitung.
                KostenHerleitung.Angabe mit = KostenHerleitung.Bilde(
                    Position(bem, 0.01), K_WAERMEPUMPE,
                    Betriebszeile(40000.0, KostenHerleitung.HERKUNFT_LAUF), true, true);
                Assert.EndsWith(" · " + vermerk, mit.Zeile);
                Assert.StartsWith("×", mit.Zeile);

                // Ohne Bezugsgröße: der Vermerk allein.
                KostenHerleitung.Angabe ohne = KostenHerleitung.Bilde(
                    Position(bem, 0.01), K_WAERMEPUMPE,
                    Betriebszeile(null, "", WirtschaftlichkeitCtrl.BASISGRUND_GEWERK), true, true);
                Assert.Equal(vermerk, ohne.Zeile);
                Assert.True(ohne.OhneBasis);
            }

            // Der Altwert „je kWh" zählt mit.
            Assert.True(KostenHerleitung.IstAltbestandWpKwh(DbWerte.BEMESSUNG_EUR_PRO_KWH, K_WAERMEPUMPE));
        }

        [Fact]
        public void Ohne_Waermepumpe_ohne_Projekt_und_im_Investitionsraster_kein_Vermerk()
        {
            string vermerk = R.KDLG_HERL_ALTBESTAND_WP_KWH;

            // Andere Gewerke: kein Vermerk.
            KostenHerleitung.Angabe bhkw = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, 0.02), K_BHKW,
                Betriebszeile(33000.0, KostenHerleitung.HERKUNFT_LAUF), true, true);
            Assert.DoesNotContain(vermerk, bhkw.Zeile);

            // Stammkontext (Vorlagenverwaltung, E23‑Q4): keine Zeile.
            KostenHerleitung.Angabe stamm = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, 0.01), K_WAERMEPUMPE,
                null, false, true);
            Assert.Equal("", stamm.Zeile);

            // Erlaubte Arten an der Wärmepumpe: kein Vermerk.
            foreach (string bem in new[] { DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                                           DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,
                                           DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG })
                Assert.False(KostenHerleitung.IstAltbestandWpKwh(bem, K_WAERMEPUMPE));

            // Eine Investitionszeile trägt keinen Betriebsvermerk.
            var invest = Betriebszeile(10.0, KostenHerleitung.HERKUNFT_ANLAGE);
            invest.KategorieId = DbWerte.KOSTEN_KATEGORIE_INVESTITION;
            KostenHerleitung.Angabe inv = KostenHerleitung.Bilde(
                Position(DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, 0.01), K_WAERMEPUMPE,
                invest, true, false);
            Assert.DoesNotContain(vermerk, inv.Zeile);
        }

        [Fact]
        public void Der_Vermerk_steht_auch_englisch()
        {
            using var _ = new Kulturvorrichtung("en-US");
            Assert.Equal("legacy entry — heat pump operating costs are not measured per kWh",
                         R.KDLG_HERL_ALTBESTAND_WP_KWH);
        }

        // =====================================================================
        //  Hilfsmittel
        // =====================================================================

        private static List<string> Persistenzwerte(int komponentenId, bool invest,
                                                    ICollection<string> benutzt)
        {
            var werte = new List<string>();
            foreach (BemessungKatalog.Info i in BemessungKatalog.Auswahl(komponentenId, invest, benutzt))
                werte.Add(i.Persistenz);
            return werte;
        }

        private static KostenVorlagenPosition Position(string bemessung, double? satz)
        {
            return new KostenVorlagenPosition
            {
                Bezeichnung = "Probe",
                Bemessung = bemessung,
                Satz = satz,
            };
        }

        private static KostenProjektPositionenCtrl.Zeile Betriebszeile(
            double? basis, string herkunft, string grund = "")
        {
            return new KostenProjektPositionenCtrl.Zeile
            {
                Basis = basis,
                Runde = 0,
                BasisHerkunft = herkunft,
                BasisGrund = grund,
                KategorieId = DbWerte.KOSTEN_KATEGORIE_BETRIEB,
            };
        }
    }
}
