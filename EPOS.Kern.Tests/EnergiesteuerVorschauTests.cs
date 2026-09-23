using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c3 (E7c2‑Q8 b) — <b>die Energiesteuer-Vorschau je Wahl</b>
    /// (<see cref="SteuerGutschriftRechner.Vorschau"/>): für jede Anlage mit Brennstoff
    /// Satz, Menge und Wirkung im ersten Jahr für „keine", § 53 (voll und energetisch),
    /// § 53a Abs. 5 und § 54 — dieselbe Energiesteuerrechnung wie im Lauf, auf einer Kopie
    /// der Eingabe mit geänderter Wahl dieser einen Anlage.
    ///
    /// <para><b>Die Sollwerte</b> sind die Handproben des Rechenwegs 05, dieselben wie in
    /// <c>SteuerGutschriftRechnerTests</c>: § 53 26.383,46 €, energetisch 12.079,17 €,
    /// § 53a 21.202,71 €, § 54 6.369,85 € (nach 250 € Sockel). Der Katalog kommt aus der
    /// Vorbelegung; ohne Datenbank bis auf den letzten Fall, der den Lauf an 1030 hält.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergiesteuerVorschauTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        private const int JAHR = 2026;
        private const double PEL_KW = 300.0;
        private const double STROM_MWH = 1650.0;
        private const double ETA_EL = 0.38, ETA_TH = 0.45;
        private const double HI = 10.5, HS = 11.6;

        private static double BrennstoffHi => STROM_MWH / ETA_EL;

        private static Func<string, GesetzParameter> Saetze(int jahr)
        {
            IList<GesetzParameter> vor = GesetzKatalog.Vorbelegung();
            return s => vor.Where(p => p.Schluessel == s && p.JahrVon <= jahr)
                           .OrderByDescending(p => p.JahrVon)
                           .FirstOrDefault();
        }

        private static SteuerAnlage Bhkw(string name, string wahl = null)
        {
            return new SteuerAnlage
            {
                Bezeichner = name,
                PelKW = PEL_KW,
                BrennstoffMWh = BrennstoffHi,
                StromMWh = STROM_MWH,
                WaermeMWh = BrennstoffHi * ETA_TH,
                SchluesselSatzVoll = DbWerte.GESETZ_ENERGIEST_ERDGAS,
                SchluesselSatz53a = DbWerte.GESETZ_ENERGIEST_53A5_ERDGAS,
                SchluesselSatz54 = DbWerte.GESETZ_ENERGIEST_54_ERDGAS,
                SchluesselCo2 = DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HI,
                EffHi = HI,
                EffHs = HS,
                Fossil = true,
                Stromerzeuger = true,
                EnergiesteuerWahl = wahl,
            };
        }

        private static SteuerEingabe Projekt(params SteuerAnlage[] anlagen)
        {
            var e = new SteuerEingabe
            {
                Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                JahresnutzungsgradProzent = 83.0
            };
            foreach (SteuerAnlage a in anlagen) e.Anlagen.Add(a);
            return e;
        }

        private static List<EnergiesteuerVorschauZeile> Vorschau(SteuerEingabe e)
            => SteuerGutschriftRechner.Vorschau(e, JAHR, Saetze(JAHR), DE);

        private static EnergiesteuerVorschauZeile Zeile(List<EnergiesteuerVorschauZeile> liste, string anlage,
                                                        string wahl, string aufteilung = "")
            => Assert.Single(liste, z => z.Anlage == anlage && z.Wahl == wahl && z.Aufteilung == aufteilung);

        // =====================================================================

        /// <summary>Eine Anlage, fünf Zeilen: jede Wahl mit Satz, Menge und Betrag der
        /// Handprobe; § 54 nennt den Sockel, den diese Wahl auslöst.</summary>
        [Fact]
        public void Jede_Wahl_steht_mit_Satz_und_Betrag()
        {
            List<EnergiesteuerVorschauZeile> v = Vorschau(Projekt(Bhkw("BHKW")));
            Assert.Equal(5, v.Count);

            EnergiesteuerVorschauZeile keine = Zeile(v, "BHKW", DbWerte.ENERGIESTEUER_WAHL_KEINE);
            Assert.Equal(0.0, keine.BetragEur);
            Assert.Null(keine.SatzEur);
            Assert.Null(keine.Grund);

            EnergiesteuerVorschauZeile voll = Zeile(v, "BHKW", DbWerte.ENERGIESTEUER_WAHL_53,
                                                    DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF);
            Assert.Equal(26383.46, voll.BetragEur, 2);
            Assert.Equal(5.50, voll.SatzEur.Value, 6);
            Assert.Equal("€/MWh", voll.SatzEinheit());
            Assert.Equal(4796.99, voll.Menge.Value, 2);
            Assert.Null(voll.Grund);

            EnergiesteuerVorschauZeile energetisch = Zeile(v, "BHKW", DbWerte.ENERGIESTEUER_WAHL_53,
                                                           DbWerte.AUFTEILUNG_ENERGETISCH);
            Assert.Equal(12079.17, energetisch.BetragEur, 2);
            Assert.Equal(0.457831, energetisch.Menge.Value / voll.Menge.Value, 6);

            EnergiesteuerVorschauZeile a53 = Zeile(v, "BHKW", DbWerte.ENERGIESTEUER_WAHL_53A);
            Assert.Equal(21202.71, a53.BetragEur, 2);
            Assert.Equal(4.42, a53.SatzEur.Value, 6);

            EnergiesteuerVorschauZeile p54 = Zeile(v, "BHKW", DbWerte.ENERGIESTEUER_WAHL_54);
            Assert.Equal(6369.85, p54.BetragEur, 2);
            Assert.Equal(1.38, p54.SatzEur.Value, 6);
            Assert.Equal(250.0, p54.SockelEur, 6);
        }

        /// <summary>Die Vorschau rechnet auf Kopien: Die Eingabe des Laufs und ihr
        /// Ergebnis bleiben, wie sie waren — und die Zeile der gebuchten Wahl ist der
        /// Betrag des Laufs (Ausweis unverändert).</summary>
        [Fact]
        public void Die_Vorschau_laesst_den_Lauf_unberuehrt()
        {
            SteuerEingabe e = Projekt(Bhkw("BHKW", DbWerte.ENERGIESTEUER_WAHL_53));
            e.AufteilungMethode = DbWerte.AUFTEILUNG_ENERGETISCH;
            double vorher = SteuerGutschriftRechner.Rechne(e, JAHR, Saetze(JAHR), DE).EnergiesteuerEur;

            List<EnergiesteuerVorschauZeile> v = Vorschau(e);

            Assert.Equal(DbWerte.ENERGIESTEUER_WAHL_53, e.Anlagen[0].EnergiesteuerWahl);
            Assert.Null(e.Anlagen[0].AufteilungMethode);
            Assert.Equal(vorher, SteuerGutschriftRechner.Rechne(e, JAHR, Saetze(JAHR), DE).EnergiesteuerEur);
            Assert.Equal(vorher, Zeile(v, "BHKW", DbWerte.ENERGIESTEUER_WAHL_53,
                                       DbWerte.AUFTEILUNG_ENERGETISCH).BetragEur, 6);
        }

        /// <summary>Eine Bedingung, die die Wahl nicht erfüllt, steht als Grund an ihrer
        /// Zeile — § 54 ohne produzierendes Gewerbe, § 53a unter 70 % Nutzungsgrad.</summary>
        [Fact]
        public void Eine_nicht_erfuellte_Bedingung_steht_als_Grund()
        {
            SteuerEingabe e = Projekt(Bhkw("BHKW"));
            e.Unternehmensart = DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE;
            e.JahresnutzungsgradProzent = 60.0;

            List<EnergiesteuerVorschauZeile> v = Vorschau(e);

            EnergiesteuerVorschauZeile p54 = Zeile(v, "BHKW", DbWerte.ENERGIESTEUER_WAHL_54);
            Assert.Null(p54.SatzEur);
            Assert.Equal(0.0, p54.BetragEur);
            Assert.Equal(Resource.STEUER_ENERGIEST_54_UNTERNEHMENSART, p54.Grund);

            EnergiesteuerVorschauZeile a53 = Zeile(v, "BHKW", DbWerte.ENERGIESTEUER_WAHL_53A);
            Assert.Null(a53.SatzEur);
            Assert.Equal(0.0, a53.BetragEur);
            Assert.Contains("53a", a53.Grund);
        }

        /// <summary>Ein Heizkessel entlastet nie nach § 53 oder § 53a — die Zeilen sagen
        /// es; § 54 rechnet für ihn wie für ein BHKW.</summary>
        [Fact]
        public void Ein_Kessel_bekommt_fuer_Paragraf_53_den_Grund()
        {
            SteuerAnlage kessel = Bhkw("Kessel");
            kessel.Stromerzeuger = false;
            kessel.PelKW = 0;
            kessel.StromMWh = 0;

            List<EnergiesteuerVorschauZeile> v = Vorschau(Projekt(kessel));

            EnergiesteuerVorschauZeile voll = Zeile(v, "Kessel", DbWerte.ENERGIESTEUER_WAHL_53,
                                                    DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF);
            Assert.Null(voll.SatzEur);
            Assert.Equal(0.0, voll.BetragEur);
            Assert.Contains("§ 54", voll.Grund);
            Assert.Equal(6369.85, Zeile(v, "Kessel", DbWerte.ENERGIESTEUER_WAHL_54).BetragEur, 2);
        }

        /// <summary>Die Wirkung ist ein Unterschied: Wählt die zweite Anlage § 54, während
        /// die erste § 53 führt, sperrt die Mischlage den § 54-Betrag — die Zeile sagt 0 €
        /// und warum; § 53 für die zweite Anlage rechnet ihren vollen Betrag.</summary>
        [Fact]
        public void Die_Mischlage_steht_an_der_Wahl_die_sie_ausloest()
        {
            List<EnergiesteuerVorschauZeile> v = Vorschau(
                Projekt(Bhkw("A", DbWerte.ENERGIESTEUER_WAHL_53), Bhkw("B")));

            EnergiesteuerVorschauZeile b54 = Zeile(v, "B", DbWerte.ENERGIESTEUER_WAHL_54);
            Assert.Equal(0.0, b54.BetragEur, 6);
            Assert.Contains("§ 54 EnergieStG gesperrt", b54.Grund);

            EnergiesteuerVorschauZeile b53 = Zeile(v, "B", DbWerte.ENERGIESTEUER_WAHL_53,
                                                   DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF);
            Assert.Equal(26383.46, b53.BetragEur, 2);
            Assert.Null(b53.Grund);
        }

        /// <summary>Der Sockel des § 54 fällt einmal je Lauf: Führt schon die erste Anlage
        /// § 54, bringt die Wahl der zweiten ihren Bruttobetrag ohne zweiten Sockel.</summary>
        [Fact]
        public void Der_Sockel_zaehlt_nur_bei_der_ersten_Paragraf_54_Anlage()
        {
            List<EnergiesteuerVorschauZeile> v = Vorschau(
                Projekt(Bhkw("A", DbWerte.ENERGIESTEUER_WAHL_54), Bhkw("B")));

            EnergiesteuerVorschauZeile b54 = Zeile(v, "B", DbWerte.ENERGIESTEUER_WAHL_54);
            Assert.Equal(6619.85, b54.BetragEur, 2);
            Assert.Equal(0.0, b54.SockelEur, 6);

            EnergiesteuerVorschauZeile a54 = Zeile(v, "A", DbWerte.ENERGIESTEUER_WAHL_54);
            Assert.Equal(6369.85, a54.BetragEur, 2);
            Assert.Equal(250.0, a54.SockelEur, 6);
        }

        /// <summary>Der Umschlag der Fassung 8 trägt die Vorschau hin und zurück; eine
        /// ältere Fassung liest sich mit leerer Vorschau.</summary>
        [Fact]
        public void Der_Umschlag_traegt_die_Vorschau()
        {
            var e = new WirtschaftlichkeitErgebnis();
            e.EnergiesteuerVorschau.Add(new EnergiesteuerVorschauZeile
            {
                Anlage = "BHKW", Wahl = DbWerte.ENERGIESTEUER_WAHL_54, SatzEur = 1.38,
                Einheit = DbWerte.GESETZ_EINHEIT_EUR_MWH, Menge = 4796.99, BetragEur = 6369.85,
                SockelEur = 250, Grund = "Probe"
            });

            string grund;
            string text = ErgebnisNachweisUmschlag.Schreiben(e, out grund);
            Assert.Null(grund);

            var zurueck = new WirtschaftlichkeitErgebnis();
            ErgebnisNachweisUmschlag.Lesen(text).Uebernimm(zurueck);
            EnergiesteuerVorschauZeile z = Assert.Single(zurueck.EnergiesteuerVorschau);
            Assert.Equal("BHKW", z.Anlage);
            Assert.Equal(DbWerte.ENERGIESTEUER_WAHL_54, z.Wahl);
            Assert.Equal(1.38, z.SatzEur);
            Assert.Equal(6369.85, z.BetragEur);
            Assert.Equal(250.0, z.SockelEur);
            Assert.Equal("Probe", z.Grund);
            Assert.Equal("€/MWh", z.SatzEinheit());

            var alt = new WirtschaftlichkeitErgebnis();
            alt.EnergiesteuerVorschau.Add(new EnergiesteuerVorschauZeile());
            ErgebnisNachweisUmschlag.Lesen("nw1:{\"Version\":7}").Uebernimm(alt);
            Assert.Empty(alt.EnergiesteuerVorschau);
        }

        /// <summary>
        /// Am echten Lauf (1030, produzierendes Gewerbe, § 53 für das Projekt): Jede
        /// BHKW-Anlage trägt ihre fünf Zeilen, die Zeile der gebuchten Wahl ist ihr
        /// gebuchter Betrag, und der gespeicherte Stand trägt dieselbe Vorschau.
        /// </summary>
        [Fact]
        public void Der_Lauf_fuehrt_die_Vorschau_und_speichert_sie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWirtschaftlichkeit SET Unternehmensart = 'PROD_GEWERBE', " +
                "Energiesteuer_Wahl = 'PARAGRAF_53' WHERE ID_Projekt = 1030");
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(1030);
            var v = new VariantenDaten
            {
                IdProjekt = 1030, IstStamm = true, Projektname = "Prüffall Q8",
                Ergebnis = new ErgebnisCtrl().Load(1030)
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = 1030, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            WirtschaftlichkeitErgebnis e = ctrl.Berechne(daten, p).Single(
                x => x.IdProjekt == 1030 && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);

            Assert.NotEmpty(e.EnergiesteuerNachweise);
            foreach (EnergiesteuerNachweis n in e.EnergiesteuerNachweise)
            {
                Assert.Equal(5, e.EnergiesteuerVorschau.Count(z => z.Anlage == n.Anlage));
                EnergiesteuerVorschauZeile gebucht = Zeile(e.EnergiesteuerVorschau, n.Anlage,
                    DbWerte.ENERGIESTEUER_WAHL_53, DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF);
                Assert.Equal(n.BetragEur, gebucht.BetragEur, 6);
                Assert.Equal(n.SatzEur, gebucht.SatzEur.Value, 6);
            }

            WirtschaftlichkeitErgebnis geladen = ctrl.LadeErgebnisse(new List<int> { 1030 }).Single(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(e.EnergiesteuerVorschau.Count, geladen.EnergiesteuerVorschau.Count);
            for (int i = 0; i < e.EnergiesteuerVorschau.Count; i++)
            {
                Assert.Equal(e.EnergiesteuerVorschau[i].Anlage, geladen.EnergiesteuerVorschau[i].Anlage);
                Assert.Equal(e.EnergiesteuerVorschau[i].Wahl, geladen.EnergiesteuerVorschau[i].Wahl);
                Assert.Equal(e.EnergiesteuerVorschau[i].BetragEur, geladen.EnergiesteuerVorschau[i].BetragEur);
            }
        }
    }
}
