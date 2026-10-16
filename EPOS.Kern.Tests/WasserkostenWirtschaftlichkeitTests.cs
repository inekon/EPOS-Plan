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
    /// <b>Welle KB-7b — die Wasserkosten des Rückkühlwerks in der Wirtschaftlichkeit</b> (Entwurf
    /// <c>Dialoge/2026-10-16_Entwurf_KB7_Wasserkosten_Rueckkuehlwerk.md</c>, Abschnitte 4 und 7).
    ///
    /// <para>Menge × Preis im Betriebs-Topf samt Nachweiszeile, der Kapitalwert ändert sich genau um den
    /// Barwert mit p_B, ohne Preis 0 € und ein Hinweis mit der Menge, der Szenariopreis wirkt, Energiekosten,
    /// CO₂ und BEHG bleiben unverändert, der Wasserträger steht in keiner Erzeugerauswahl und in keiner
    /// Verwendung, und der Lauf-Hinweis nennt in beiden Sprachen Menge und Nassstunden ohne Betrag.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class WasserkostenWirtschaftlichkeitTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        private const int STAMM = 1040;
        private const int VARIANTE = 1041;          // ohne eigene Betriebszeilen in der Testdatenbank
        private const int KESSELANLAGE = 14759;     // Kesselzeile der Variante
        private const int PROJEKT_KOSTEN = 1030;    // gespeicherter Lauf mit bepreisten Trägern (BHKW)
        private const double MENGE = 1000.0;        // m³/a, zwei Maschinen 400 + 600
        private const double PREIS = 2.5;           // €/m³
        private const string ERWARTET = WirtschaftlichkeitSzenario.ERWARTET;
        private const string BEST = WirtschaftlichkeitSzenario.BEST;

        // =====================================================================
        //  Der Träger
        // =====================================================================

        /// <summary>Ohne Zuordnung gilt der Katalogträger WATER; ein dem Projekt zugeordneter zweiter Wasserträger
        /// schlägt ihn — erkannt am Preismodell, nicht am Namen.</summary>
        [Fact]
        public void Der_Wassertraeger_ist_der_zugeordnete_sonst_der_Katalogtraeger()
        {
            if (!_db.Vorhanden) return;
            int katalog = Katalogtraeger();
            Assert.True(katalog > 0, "Kein Wasserträger in der Testkopie.");
            Assert.Equal(katalog, Wasserkosten.WasserTraeger(VARIANTE));
            Assert.True(Wasserkosten.IstWasserTraeger(katalog));

            DataRepository.ExecuteNonQuery(
                "INSERT INTO energy_carrier (ID_Brennstoff, name, code, group_code, pricing_model, billing_unit) " +
                "VALUES (0, 'Brunnen', 'Brunnen', 'Wasser', 'WATER', 'm³')");
            int zweiter = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MAX(id) FROM energy_carrier WHERE pricing_model = 'WATER'"), CultureInfo.InvariantCulture);
            Assert.NotEqual(katalog, zweiter);
            Zuordnen(VARIANTE, zweiter, PREIS);
            Assert.Equal(zweiter, Wasserkosten.WasserTraeger(VARIANTE));
            Assert.Equal(katalog, Wasserkosten.WasserTraeger(STAMM));
        }

        // =====================================================================
        //  Die Buchung
        // =====================================================================

        /// <summary>Menge × Preis steht im Betriebs-Topf und als Nachweiszeile; die Gliederung geht auf, der
        /// Kapitalwert sinkt genau um den Barwert Σ K·(1+p_B)^(t−1)/(1+i)^t.</summary>
        [Fact]
        public void Menge_mal_Preis_im_Betriebstopf_und_Kapitalwert_um_den_Barwert_mit_pB()
        {
            if (!_db.Vorhanden) return;
            WirtschaftlichkeitErgebnis ohne = Ergebnis(Gruppe(0), VARIANTE, ERWARTET);
            Zuordnen(VARIANTE, Katalogtraeger(), PREIS);
            WirtschaftlichkeitErgebnis mit = Ergebnis(Gruppe(MENGE), VARIANTE, ERWARTET);

            Assert.Equal(ohne.BetriebskostenJahr.Value + MENGE * PREIS, mit.BetriebskostenJahr.Value, 9);
            KostenPositionNachweis n = Assert.Single(mit.Betriebskosten, x => x.Menge == MENGE && x.Einheitpreis == PREIS);
            Assert.Equal(MENGE * PREIS, n.BetragJahr, 9);
            Assert.Equal(DbWerte.KOSTENART_BEDARFSGEBUNDEN, n.Kostenart);
            Assert.Equal(string.Format(DE, R.WIRT_BK_WASSER_RKW, MENGE.ToString("N1", DE), PREIS.ToString("N2", DE)), n.Bezeichnung);
            Assert.StartsWith("Wasser Rückkühlwerk: 1.000,0 m³ × 2,50 €/m³", n.Bezeichnung, StringComparison.Ordinal);
            double erstesJahr = mit.Betriebskosten.Where(WirtschaftlichkeitZeilen.LaeuftImErstenJahr).Sum(x => x.BetragJahr);
            Assert.Equal("", WirtschaftlichkeitZeilen.GliederungAbweichung(erstesJahr, mit.BetriebskostenJahr, DE));
            Assert.DoesNotContain(Hinweistext(MENGE), mit.Hinweis ?? "", StringComparison.Ordinal);

            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(STAMM).FuerSzenario(ERWARTET);
            double i = p.Zinssatz / 100.0, pB = p.PreissteigerungBetrieb / 100.0;
            double hand = 0;
            for (int t = 1; t <= p.Betrachtungszeitraum; t++)
                hand += MENGE * PREIS * Math.Pow(1.0 + pB, t - 1) / Math.Pow(1.0 + i, t);
            Assert.True(hand > 0);
            double differenz = Kw(ohne) - Kw(mit);
            Assert.True(Math.Abs(hand - differenz) <= 0.011,
                        "Handrechnung " + hand.ToString("R", CultureInfo.InvariantCulture) + ", Lauf " +
                        differenz.ToString("R", CultureInfo.InvariantCulture));
            // Gegenprobe: mit p_E statt p_B gerechnet träfe die Differenz nicht (die Steigerungen unterscheiden sich).
            if (Math.Abs(p.PreissteigerungEnergie - p.PreissteigerungBetrieb) > 1e-9)
            {
                double mitPe = 0, pE = p.PreissteigerungEnergie / 100.0;
                for (int t = 1; t <= p.Betrachtungszeitraum; t++)
                    mitPe += MENGE * PREIS * Math.Pow(1.0 + pE, t - 1) / Math.Pow(1.0 + i, t);
                Assert.True(Math.Abs(mitPe - differenz) > 0.011);
            }
        }

        /// <summary>Ohne Preis (keine Zuordnung, Katalogpreis 0) keine Kosten, aber ein Hinweis mit der Menge.</summary>
        [Fact]
        public void Ohne_Preis_null_Euro_und_ein_Hinweis_mit_der_Menge()
        {
            if (!_db.Vorhanden) return;
            WirtschaftlichkeitErgebnis ohne = Ergebnis(Gruppe(0), VARIANTE, ERWARTET);
            WirtschaftlichkeitErgebnis mit = Ergebnis(Gruppe(MENGE), VARIANTE, ERWARTET);
            Assert.Equal(ohne.BetriebskostenJahr.Value, mit.BetriebskostenJahr.Value, 9);
            Assert.Equal(Kw(ohne), Kw(mit), 9);
            Assert.DoesNotContain(mit.Betriebskosten, x => x.Menge == MENGE);
            Assert.Contains(Hinweistext(MENGE), mit.Hinweis ?? "", StringComparison.Ordinal);
            Assert.Contains("1.000,0 m³/a", mit.Hinweis, StringComparison.Ordinal);
            Assert.DoesNotContain(Hinweistext(MENGE), ohne.Hinweis ?? "", StringComparison.Ordinal);

            // Ein Projektpreis 0 ist „nicht gepflegt“ — derselbe Hinweis.
            Zuordnen(VARIANTE, Katalogtraeger(), 0.0);
            WirtschaftlichkeitErgebnis null0 = Ergebnis(Gruppe(MENGE), VARIANTE, ERWARTET);
            Assert.Equal(ohne.BetriebskostenJahr.Value, null0.BetriebskostenJahr.Value, 9);
            Assert.Contains(Hinweistext(MENGE), null0.Hinweis ?? "", StringComparison.Ordinal);
        }

        /// <summary>Der Szenariopreis des Wasserträgers wirkt im Best-Fall; die Menge folgt dem Mengenfaktor des
        /// Szenarios, der Erwartungsfall bleibt beim Projektpreis.</summary>
        [Fact]
        public void Der_Szenariopreis_wirkt()
        {
            if (!_db.Vorhanden) return;
            int wasser = Katalogtraeger();
            Zuordnen(VARIANTE, wasser, PREIS);
            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(VARIANTE, wasser,
                new TraegerpreisSzenario { ArbeitspreisBest = 1.0 }));

            List<WirtschaftlichkeitErgebnis> alle = Gruppe(MENGE, out WirtschaftlichkeitParameter p);
            KostenPositionNachweis erwartet = Assert.Single(Ergebnis(alle, VARIANTE, ERWARTET).Betriebskosten,
                                                            x => x.Einheitpreis.HasValue && x.Menge.HasValue && x.Kostenart == DbWerte.KOSTENART_BEDARFSGEBUNDEN);
            Assert.Equal(PREIS, erwartet.Einheitpreis.Value, 12);
            Assert.Equal(MENGE * PREIS, erwartet.BetragJahr, 9);

            double faktor = p.SatzFuer(BEST)?.MengeFaktor ?? 1.0;
            KostenPositionNachweis best = Assert.Single(Ergebnis(alle, VARIANTE, BEST).Betriebskosten,
                                                        x => x.Einheitpreis.HasValue && x.Menge.HasValue && x.Kostenart == DbWerte.KOSTENART_BEDARFSGEBUNDEN);
            Assert.Equal(1.0, best.Einheitpreis.Value, 12);
            Assert.Equal(MENGE * faktor, best.Menge.Value, 9);
            Assert.Equal(MENGE * faktor * 1.0, best.BetragJahr, 9);
        }

        // =====================================================================
        //  Die Abgrenzung
        // =====================================================================

        /// <summary>Energiekosten, CO₂ und BEHG-Menge des Kostenrechners bleiben mit Wasser und Wasserpreis
        /// bitgleich; der Wasserträger steht in keiner Trägerliste der Energiekosten.</summary>
        [Fact]
        public void Energiekosten_CO2_und_BEHG_bleiben_unveraendert()
        {
            if (!_db.Vorhanden) return;
            VariantenDaten vorher = Gerechnet(PROJEKT_KOSTEN, 0);
            Assert.True(vorher.Energiekosten.HasValue, "Keine Energiekosten: " + vorher.Fehler);

            int wasser = Katalogtraeger();
            Zuordnen(PROJEKT_KOSTEN, wasser, PREIS);
            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT_KOSTEN, wasser,
                new TraegerpreisSzenario { ArbeitspreisBest = 1.0, GrundpreisBest = 100.0 }));
            VariantenDaten nachher = Gerechnet(PROJEKT_KOSTEN, MENGE);
            Assert.Equal(vorher.Energiekosten, nachher.Energiekosten);
            Assert.Equal(vorher.CO2Gesamt, nachher.CO2Gesamt);
            Assert.Equal(vorher.CO2Brennstoff, nachher.CO2Brennstoff);
            Assert.Equal(vorher.BiogenBehgMengeMWh, nachher.BiogenBehgMengeMWh);
            if (nachher.EnergiekostenJeTraeger != null)
                Assert.DoesNotContain(nachher.EnergiekostenJeTraeger, t => t.CarrierId == wasser);
        }

        /// <summary>Der Wasserträger ist nie Träger eines Erzeugers: weder in der Auswahl (mit und ohne
        /// Einengung) noch zulässig, und auch an einer Anlage eingetragen keine Verwendung des Projekts.</summary>
        [Fact]
        public void Der_Wassertraeger_steht_in_keiner_Zulaessigkeit_und_keiner_Verwendung()
        {
            if (!_db.Vorhanden) return;
            int wasser = Katalogtraeger();
            Assert.DoesNotContain(EnergietraegerZulaessigkeit.ZulaessigerKatalog(null), c => c.ID == wasser);
            Assert.DoesNotContain(EnergietraegerZulaessigkeit.ZulaessigerKatalog(DbWerte.ERZEUGER_HEIZKESSEL), c => c.ID == wasser);
            Assert.False(EnergietraegerZulaessigkeit.IstZulaessig(null, wasser));
            Assert.False(EnergietraegerZulaessigkeit.IstZulaessig(DbWerte.ERZEUGER_HEIZKESSEL, wasser));
            Assert.NotEmpty(EnergietraegerZulaessigkeit.ZulaessigerKatalog(null));

            Zuordnen(VARIANTE, wasser, PREIS);
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET [" + SchemaKatalog.SPALTE_ID_CARRIER + "] = ? WHERE ID = ?",
                new DbParam("@c", wasser), new DbParam("@a", KESSELANLAGE));
            Assert.DoesNotContain(ProjektEnergietraegerCtrl.Verwendete(VARIANTE), v => v.CarrierId == wasser);
        }

        // =====================================================================
        //  Der Lauf-Hinweis
        // =====================================================================

        /// <summary>Der Lauf-Hinweis nennt Menge und Nassstunden und verweist auf die Betriebskosten — in beiden
        /// Sprachen, ohne Betrag; die zwei alten Schlüssel sind fort.</summary>
        [Fact]
        public void Der_Lauf_Hinweis_nennt_Menge_und_Nassstunden_in_beiden_Sprachen()
        {
            string de = string.Format(DE, R.SIMENG_KAELTE_RKW_WASSER, "KM 1", "RK 1", MENGE.ToString("N1", DE), 1234);
            Assert.Equal("Kältemaschine „KM 1“: Das Rückkühlwerk „RK 1“ verbraucht 1.000,0 m³ Wasser in 1234 Nassstunden; " +
                         "die Wasserkosten (Menge × Preis des Wasserträgers) stehen in den Betriebskosten der Wirtschaftlichkeit.", de);
            using (new Kulturvorrichtung("en-US"))
            {
                CultureInfo en = CultureInfo.GetCultureInfo("en-US");
                string text = string.Format(en, R.SIMENG_KAELTE_RKW_WASSER, "KM 1", "RK 1", MENGE.ToString("N1", en), 1234);
                Assert.Equal("Chiller \"KM 1\": the heat rejection unit \"RK 1\" uses 1,000.0 m³ of water in 1234 wet hours; " +
                             "the water cost (quantity × price of the water carrier) is part of the operating costs of the economic analysis.", text);
                Assert.Equal("Heat rejection water: 1,000.0 m³ × 2.50 €/m³",
                             string.Format(en, R.WIRT_BK_WASSER_RKW, MENGE.ToString("N1", en), PREIS.ToString("N2", en)));
                Assert.StartsWith("Heat rejection water 1,000.0 m³/a without price",
                                  string.Format(en, R.WIRT_HINWEIS_WASSER_OHNE_PREIS, MENGE.ToString("N1", en)), StringComparison.Ordinal);
            }
            Assert.Null(R.ResourceManager.GetString("SIMENG_KAELTE_RKW_WASSER_KOSTEN"));
            Assert.Null(R.ResourceManager.GetString("SIMENG_KAELTE_RKW_WASSER_OHNE_PREIS"));
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static int Katalogtraeger() =>
            (int)(WasserpreisTraegerkarteSchema.TraegerId() ?? 0);

        /// <summary>Ordnet dem Projekt den Träger mit Arbeitspreis zu (Zeile anlegen oder Preis setzen).</summary>
        private static void Zuordnen(int projekt, int traeger, double preis)
        {
            object id = DataRepository.ExecuteScalar(
                "SELECT ID FROM energy_project_settings WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", projekt), new DbParam("@t", traeger));
            if (id == null || id == DBNull.Value)
                DataRepository.ExecuteNonQuery(
                    "INSERT INTO energy_project_settings (ID_Projekt, [ID_Energieträger], custom_price_work) VALUES (?, ?, ?)",
                    new DbParam("@p", projekt), new DbParam("@t", traeger), new DbParam("@w", preis));
            else
                DataRepository.ExecuteNonQuery(
                    "UPDATE energy_project_settings SET custom_price_work = ? WHERE ID = ?",
                    new DbParam("@w", preis), new DbParam("@id", id));
        }

        /// <summary>Zwei Kältemaschinen mit zusammen <paramref name="menge"/> m³ Wasser (40 % / 60 %).</summary>
        private static List<ErgebnisKaeltemaschineModel> Maschinen(double menge) =>
            menge <= 0
                ? new List<ErgebnisKaeltemaschineModel>()
                : new List<ErgebnisKaeltemaschineModel>
                  {
                      new ErgebnisKaeltemaschineModel { Bezeichner = "KM 1", Wasser_m3 = menge * 0.4 },
                      new ErgebnisKaeltemaschineModel { Bezeichner = "KM 2", Wasser_m3 = menge * 0.6 },
                      new ErgebnisKaeltemaschineModel { Bezeichner = "KM 3 trocken", Wasser_m3 = null }
                  };

        /// <summary>Stamm 1040 mit den Varianten 1041/1042 und synthetischen Energiekosten (Muster
        /// <c>WiederholperiodeTests</c>); die Variante 1041 trägt <paramref name="wasserM3"/> m³ Wasser.</summary>
        private static List<WirtschaftlichkeitErgebnis> Gruppe(double wasserM3) => Gruppe(wasserM3, out _);

        private static List<WirtschaftlichkeitErgebnis> Gruppe(double wasserM3, out WirtschaftlichkeitParameter p)
        {
            int[] ids = { STAMM, VARIANTE, 1042 };
            double[] energie = { 12000.0, 9000.0, 7000.0 };
            string[] namen = { "Stammprojekt", "Variante A", "Variante B" };
            var daten = new BerichtsDaten { IdStamm = ids[0], Stammprojektname = "Stammprojekt" };
            for (int i = 0; i < ids.Length; i++)
            {
                var erg = new ErgebnisModel();
                if (ids[i] == VARIANTE) erg.Kaeltemaschinen = Maschinen(wasserM3);
                daten.Varianten.Add(new VariantenDaten
                {
                    IdProjekt = ids[i], IstStamm = i == 0, Projektname = "Stammprojekt",
                    Variantenname = i == 0 ? "" : namen[i], Ergebnis = erg, Energiekosten = energie[i]
                });
            }
            p = new WirtschaftlichkeitCtrl().LadeParameter(ids[0]);
            p.IdStamm = ids[0];
            return new WirtschaftlichkeitCtrl().Berechne(daten, p, 0, false);
        }

        private static WirtschaftlichkeitErgebnis Ergebnis(List<WirtschaftlichkeitErgebnis> alle, int projekt, string szenario) =>
            alle.Single(x => x.IdProjekt == projekt && x.Szenario == szenario);

        private static double Kw(WirtschaftlichkeitErgebnis e)
        {
            Assert.True(e.Kapitalwert.HasValue, "Kein Kapitalwert: " + (e.Fehlgrund ?? e.Hinweis));
            return e.Kapitalwert.Value;
        }

        private static string Hinweistext(double menge) =>
            string.Format(DE, R.WIRT_HINWEIS_WASSER_OHNE_PREIS, menge.ToString("N1", DE));

        /// <summary>Der gespeicherte Lauf des Projekts mit <paramref name="wasserM3"/> m³ Wasser, durch den Kostenrechner.</summary>
        private static VariantenDaten Gerechnet(int projekt, double wasserM3)
        {
            ErgebnisModel erg = new ErgebnisCtrl().Load(projekt);
            Assert.NotNull(erg);
            erg.Kaeltemaschinen = Maschinen(wasserM3);
            var v = new VariantenDaten { IdProjekt = projekt, Ergebnis = erg };
            KostenEmissionRechner.Berechne(v);
            return v;
        }
    }
}
