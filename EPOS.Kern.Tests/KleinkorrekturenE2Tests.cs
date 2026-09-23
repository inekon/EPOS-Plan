using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ETAPPE E2 — die kleinen Kernkorrekturen</b> (Analysepapier 2026-09-19,
    /// Befunde B-7, V-3, I-5 und R11).
    ///
    /// <para>Jeder Fall prüft GENAU EINEN Befund und ist rein: keine Datenbank, keine
    /// Oberfläche. Die Kultur ist gepinnt, weil Einheiten- und Reihenamen aus den
    /// Ressourcen kommen.</para>
    /// </summary>
    public class KleinkorrekturenE2Tests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        private const int BHKW = 7;              // Tab_KostenKomponente.ID
        private const int PUFFERSPEICHER = 6;

        // =====================================================================
        //  B-7 — die Bezugsmenge trägt ihre eigene Einheit
        // =====================================================================

        /// <summary>
        /// <b>Befund B-7.</b> Bis E2 kannte <c>MengenEinheit</c> genau zwei Arten und
        /// beschriftete jede andere Bezugsmenge mit „€" — die Herleitungszeile las sich
        /// dann „500,00 € × 12,000 €/kW·a", wo eine Leistung in kW steht. Die Antwort
        /// kommt seither aus dem <c>BemessungKatalog</c>.
        ///
        /// <para>Die Regel: Trägt der Betriebssatz „·a", ist die Bezugsgröße ein BESTAND
        /// (Leistung, Fläche, Volumen) und bleibt ohne Jahr; sonst ist sie eine MENGE je
        /// Jahr. Eine prozentuale Art bemisst sich an einem Betrag.</para>
        /// </summary>
        [Theory]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, "kW")]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, "kW")]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, "kW")]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KWP, "kWp")]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH, "kWh/a")]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, "kWh/a")]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KWH, "kWh/a")]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_H, "h/a")]
        [InlineData(DbWerte.BEMESSUNG_PROZENT_INVESTITION, "€")]
        [InlineData(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF, "€")]
        [InlineData(DbWerte.BEMESSUNG_BETRAG, "€")]
        [InlineData(DbWerte.BEMESSUNG_JAHRESBETRAG, "€")]
        public void Jede_Bemessungsart_nennt_ihre_Bezugsmenge(string bemessung, string erwartet)
        {
            Assert.Equal(erwartet, BemessungKatalog.Mengeneinheit(bemessung, 0));
        }

        /// <summary>
        /// Die gewerkeigene Bezugsgröße zählt mit: Am Pufferspeicher bemisst sich die
        /// Leistungsart nach dem VOLUMEN (Anwenderentscheid 15.09.2026), und die
        /// Herleitungszeile muss „Ltr." schreiben, nicht „kW".
        /// </summary>
        [Fact]
        public void Am_Pufferspeicher_ist_die_Bezugsmenge_das_Volumen()
        {
            Assert.Equal("Ltr.", BemessungKatalog.Mengeneinheit(
                DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, PUFFERSPEICHER));
            Assert.Equal("kW", BemessungKatalog.Mengeneinheit(
                DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, BHKW));
        }

        /// <summary>
        /// Ein unbekannter Steuerwert fällt auf „€" zurück statt zu werfen — dieselbe
        /// Vorsicht wie überall im Kostenpfad („nie stillschweigend 0", nie ein Abbruch
        /// wegen einer Beschriftung).
        /// </summary>
        [Fact]
        public void Ein_unbekannter_Steuerwert_faellt_auf_Euro_zurueck()
        {
            Assert.Equal("€", BemessungKatalog.Mengeneinheit("GIBT_ES_NICHT", 0));
            Assert.Equal("€", BemessungKatalog.Mengeneinheit(null, 0));
        }

        /// <summary>
        /// Satz und Menge gehören zusammen: Ihre Einheiten müssen sich zu €/a
        /// multiplizieren. Das ist die eigentliche Aussage von B-7 — geprüft am
        /// fertigen Text der Herleitungszeile.
        /// </summary>
        [Fact]
        public void Die_Herleitungszeile_multipliziert_zu_Euro_je_Jahr()
        {
            var n = new KostenPositionNachweis
            {
                Bemessung = DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                Komponente = BHKW,
                Menge = 300.0,
                Einheitpreis = 12.0
            };

            Assert.Equal("300,00 kW × 12,000 €/kW·a", WirtschaftlichkeitZeilen.Herleitung(n, DE));
        }

        // =====================================================================
        //  V-3 — die PV-Reihe bekommt ihre Spalte
        // =====================================================================

        /// <summary>
        /// <b>Befund V-3.</b> <c>ErloesReihe.PV_VERGUETUNG</c> ist eine ZUSATZ-Reihe des
        /// Kapitalwertrechners: Sie steckt in „Netto nominal", aber NICHT in der Spalte
        /// „Einspeisung" (die trägt allein den konstanten Einspeiseerlös). Ohne eigene
        /// Spalte fehlte sie in der Summe der Positionsspalten, und die Selbstprüfung der
        /// Mehrjahrestabelle ging um genau diesen Betrag daneben.
        /// </summary>
        [Fact]
        public void Die_PV_Reihe_steht_als_eigene_Spalte_und_die_Selbstpruefung_geht_auf()
        {
            const int T = 3;
            var pv = new double[T + 1];
            for (int t = 1; t <= T; t++) pv[t] = 1000.0;

            KapitalwertRechner.Zahlungsbild b = KapitalwertRechner.Rechne(
                investitionen: null, betriebJahr: 500.0, energieJahr: 2000.0, erloesJahr: 300.0,
                zinsProzent: 5.0, jahre: T,
                preisstBetriebProzent: 0.0, preisstEnergieProzent: 0.0,
                behgJahr: 100.0,
                zusatzErloesReihen: new List<KapitalwertRechner.ErloesReihe>
                {
                    new KapitalwertRechner.ErloesReihe(
                        KapitalwertRechner.ErloesReihe.PV_VERGUETUNG, pv)
                });

            var serie = new VerlaufSerie
            {
                IdProjekt = 1,
                Bild = b,
                Kumuliert = b.BarwertReihe,
                RestwertBarwert = b.RestwertBarwert
            };
            Mehrjahresbild m = Mehrjahresbild.Baue(serie);

            MehrjahresSpalte spalte = m.Spalten.FirstOrDefault(s => s.Schluessel == "PV_VERGUETUNG");
            Assert.True(spalte != null,
                "Keine PV-Spalte. Gefunden: " + string.Join(", ", m.Spalten.Select(s => s.Schluessel)));
            Assert.Equal(1000.0, spalte.JeJahr[1], 6);

            // Die Selbstprüfung der Tabelle: Summe der Positionsspalten = Netto nominal.
            MehrjahresSpalte netto = m.Spalten.First(s => s.Schluessel == "NETTO");
            for (int t = 1; t <= T; t++)
            {
                double summe = m.Spalten
                    .Where(s => !s.IstSumme)
                    .Sum(s => s.JeJahr != null && t < s.JeJahr.Length ? s.JeJahr[t] : 0.0);
                Assert.Equal(netto.JeJahr[t], summe, 6);
            }
        }

        // =====================================================================
        //  I-5 — die Vergleichsstrenge
        // =====================================================================

        /// <summary>
        /// <b>Befund I-5.</b> Steuerwerte sind eingefrorene ASCII-Schlüssel und werden im
        /// ganzen Kern ZEICHENGENAU verglichen — 78 von 79 Stellen taten das bereits.
        /// Die eine tolerante Stelle stellte dieselbe Frage anders als der
        /// Betriebskostenpfad, der sie als SQL-Gleichheit stellt (SQLite vergleicht TEXT
        /// zeichengenau): Eine Zeile mit abweichender Schreibweise war hier ein Zuschuss
        /// und dort keiner.
        /// </summary>
        [Theory]
        [InlineData("ZUSCHUSS", true)]
        [InlineData(" ZUSCHUSS ", true)]      // führende Leerzeichen bleiben tolerant (Trim)
        [InlineData("Zuschuss", false)]
        [InlineData("zuschuss", false)]
        [InlineData("", false)]
        public void Die_Kostenart_Zuschuss_wird_zeichengenau_verglichen(string wert, bool erwartet)
        {
            var t = new System.Data.DataTable();
            t.Columns.Add(SchemaKatalog.SPALTE_PW_KOSTENART, typeof(string));
            System.Data.DataRow r = t.NewRow();
            r[SchemaKatalog.SPALTE_PW_KOSTENART] = wert;
            t.Rows.Add(r);

            Assert.Equal(erwartet, WirtschaftlichkeitCtrl.IstZuschuss(r));
        }

        // =====================================================================
        //  R11 — die Hi/Ho-Frage am CO₂-Grenzwert: es gilt immer der Brennwert
        //  (Konzept § 6.3 Nr. 29, Register R‑NR Nr. 29, Anwender 22.09.2026;
        //  umgesetzt mit E7 — Leser: SteuerGutschriftRechner.Co2JeEnergieertrag)
        // =====================================================================

        /// <summary>
        /// <b>Befund R11.</b> Der Katalog führt zum Erdgas zwei EBeV-Faktoren:
        /// heizwertbezogen (Hi) und brennwertbezogen (Ho). Der Unterschied ist rund 10 %
        /// und entscheidet am Grenzwert des § 2 StromStG über die Befreiung. Bis E7 las
        /// die Prüfung den Hi-Schlüssel der Anlage; seit E7 nimmt der Zähler den
        /// Ho-Faktor. Dieser Fall pinnt die drei Katalogwerte, auf denen die Regel steht.
        /// </summary>
        [Fact]
        public void Der_Hi_Ho_Unterschied_am_CO2_Grenzwert_ist_gepinnt()
        {
            IList<GesetzParameter> vor = GesetzKatalog.Vorbelegung();
            double Wert(string s) => vor.Where(p => p.Schluessel == s && p.JahrVon <= 2026)
                                        .OrderByDescending(p => p.JahrVon)
                                        .First().Wert.Value;

            double hi = Wert(DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HI);
            double ho = Wert(DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HO);
            double grenze = Wert(DbWerte.GESETZ_STROMST_CO2_GRENZWERT);

            Assert.Equal(200.9, hi, 3);
            Assert.Equal(181.4, ho, 3);
            Assert.Equal(270.0, grenze, 3);

            // Rund 10 % — genau die Größenordnung, um die ein heizwertbezogener Zähler
            // gegen eine brennwertbezogene Grenze zu hoch ausfällt.
            Assert.InRange((hi - ho) / hi, 0.09, 0.11);
        }

        /// <summary>
        /// <b>Der Vorher/Nachher-Fall der Etappe E7 (Nr. 29).</b> Bei einem Nutzungsgrad,
        /// der den Zähler genau zwischen beide Faktoren legt, entscheidet die
        /// BEZUGSGRÖSSE über die Befreiung:
        /// <code>
        /// Energieertrag = 72 % des Brennstoffs (1.000 MWh → 400 Strom + 320 Wärme)
        /// Hi: 200,9 / 0,72 = 279,0 g/kWh  (über 270 ⇒ keine Befreiung)
        /// Ho: 181,4 / 0,72 = 251,9 g/kWh  (unter 270 ⇒ Befreiung)
        /// </code>
        /// <para><b>ALT (E2, Hi-Faktor gepinnt):</b> Eine Erdgasanlage mit dem
        /// Hi-Schlüssel bekam <b>0,00 €/a</b> — Begründung „über dem CO₂-Grenzwert von
        /// 270". <b>NEU (E7):</b> Dieselbe Anlage bekommt <b>8.200,00 €/a</b> =
        /// 400 MWh × 20,50 €/MWh, weil der Zähler den Ho-Faktor nimmt; der Hi-Schlüssel
        /// der Anlage führt über <c>Co2SchluesselBrennwert</c> auf
        /// <c>EF_BILANZ_EBEV_ERDGAS_HO</c>. Beide Schlüssel rechnen damit gleich.</para>
        /// </summary>
        [Fact]
        public void Am_Grenzfall_entscheidet_die_Bezugsgroesse_ueber_die_Befreiung()
        {
            const double brennstoff = 1000.0, strom = 400.0, waerme = 320.0;

            SteuerErgebnis mitHi = Befreiungsfall(DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HI,
                                                  brennstoff, strom, waerme);
            SteuerErgebnis mitHo = Befreiungsfall(DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HO,
                                                  brennstoff, strom, waerme);

            // NEU: 8.200,00 €/a statt 0,00 €/a — der Grenzwert ist brennwertbezogen.
            Assert.Equal(8200.00, mitHi.StromsteuerBefreiungEur, 2);
            Assert.Equal(8200.00, mitHo.StromsteuerBefreiungEur, 2);
            Assert.DoesNotContain(mitHi.Begruendungen, g => g.Contains("270", StringComparison.Ordinal));

            // Die Herleitung nennt den brennwertbezogenen Wert und den Faktor.
            Assert.Contains(mitHi.Herkunft, h => h.Contains("251,9", StringComparison.Ordinal) &&
                                                 h.Contains("181,4", StringComparison.Ordinal));
        }

        private static SteuerErgebnis Befreiungsfall(string schluesselCo2, double brennstoff,
                                                     double strom, double waerme)
        {
            var a = new SteuerAnlage
            {
                Bezeichner = "Grenzfall",
                PelKW = 300.0,
                BrennstoffMWh = brennstoff,
                StromMWh = strom,
                WaermeMWh = waerme,
                SchluesselCo2 = schluesselCo2,
                Fossil = true,
                Stromerzeuger = true
            };
            var e = new SteuerEingabe
            {
                Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                HocheffizienzNachweis = true,
                RaeumlicherZusammenhang = true,
                KwkEigenMWh = strom
            };
            e.Anlagen.Add(a);

            IList<GesetzParameter> vor = GesetzKatalog.Vorbelegung();
            Func<string, GesetzParameter> saetze =
                s => vor.Where(p => p.Schluessel == s && p.JahrVon <= 2026)
                        .OrderByDescending(p => p.JahrVon)
                        .FirstOrDefault();

            return SteuerGutschriftRechner.Rechne(e, 2026, saetze, DE);
        }
    }
}
