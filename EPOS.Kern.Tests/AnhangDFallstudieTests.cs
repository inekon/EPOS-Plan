using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E8b (V‑D, Punkt 6) — die <b>Anhang-D-Gegenprobe</b>: Die Fallstudie der
    /// DIN EN 17463 (Anhang D, Einbau eines BHKW mit 90 kW thermisch, Planungshorizont
    /// 18 Jahre) wird mit ihren eigenen Eingaben gegen <see cref="KapitalwertRechner.Rechne"/>
    /// gerechnet — gegen den Rechenkern, NICHT gegen die volle Kette (Analysepapier § 3.3,
    /// A7): Die Norm liefert Jahresmengen, keinen Stundenlauf; so ordnet jede Abweichung
    /// sich eindeutig dem Rechenkern zu.
    ///
    /// <para><b>Die Sollwerte</b> (Konzept § 2.11.2): Kapitalwert im wahrscheinlichsten
    /// Fall 64.480 €, im ungünstigsten −202.802 €, im günstigsten +598.320 € (Tabellen D.5
    /// und D.7). Toleranz ±1 € — das Konzept nennt keine eigene; die Norm rundet auf ganze
    /// Euro.</para>
    ///
    /// <para><b>Die Eingaben</b> (Tabellen D.2 bis D.7): 6.697 Volllaststunden, 90 kW
    /// thermisch, 52,2 kW elektrisch, Nutzungsgrad BHKW 82 %, Heizkessel 85 % — daraus
    /// Gasverbrauch des BHKW 1.161.358 kWh/a, eingesparter Kesselbrennstoff 709.094 kWh/a,
    /// eigengenutzter Strom 349.583 kWh/a (ungerundet gerechnet, wie die Norm); Gas
    /// 0,06 €/kWh, Strom 0,12 €/kWh; Investition 90.000 € im Jahr 0; Wartung 3.000 €/a;
    /// Kalkulationszins 6,96 %; Preisschwankung Energie 3 %/a, nicht energetisch 2 %/a;
    /// kein Restwert (Demontage und Schrottwert gleichen sich aus, D.5), keine Risikoanpassung
    /// (D.7), Degradation 0.</para>
    ///
    /// <para><b>Die eine Umrechnung:</b> Die Norm schreibt einen Basiswert zu Preisen des
    /// Jahres 0 fort, im Jahr t mit (1+p)^t (Tabelle D.5: Wartung im Jahr 1 = 3.060 €); der
    /// Rechenkern nimmt den Betrag des Jahres 1 und schreibt ihn mit (1+p)^(t−1) fort. Beides
    /// ist dieselbe Reihe, wenn der Jahr-1-Betrag Basis × (1+p) ist — genau so werden die
    /// Eingaben übergeben. Gerechnet wird wie in EPOS als Differenz zweier Zahlungsbilder:
    /// die Variante mit BHKW (Investition, Wartung, Gas des BHKW) gegen die Referenz ohne
    /// (Kesselbrennstoff und Strombezug, die das BHKW einspart). Die Nutzungsdauer der
    /// Investition ist der Planungshorizont — kein Ersatz, kein Restwert.</para>
    ///
    /// <para><b>Was NICHT in die Vorrichtung gehört</b> (Konzept § 2.11.2, dokumentiert):
    /// Zwei Zeilen der Sensitivitätstafel D.6 — „Reduzierter Energieverbrauch des
    /// Heizkessels" und „Stromerzeugung zur Eigennutzung" — tragen im Normtext Werte des
    /// Pumpenbeispiels (Grundeinstellung 239.603 € statt 64.480 €). Sie sind ausgenommen.
    /// Beim Nachrechnen fiel eine dritte auf: Die Zeile „Gasverbrauch BHKW" (−30.484 € bei
    /// −50 %, +159.443 € bei +50 %, Steigung 1.899 €/Δ%) trifft nicht die Änderung des
    /// Gasverbrauchs (dann +511.160 € bzw. −382.201 €), sondern die des GANZEN
    /// Energie-Nettostroms; auch sie ist ausgenommen. Und Tabelle D.7 nennt im
    /// wahrscheinlichsten Fall 348.583 kWh/a Strom statt 349.583 kWh/a — ein Tippfehler,
    /// denn nur die zweite Menge ergibt die 64.480 € derselben Spalte.</para>
    /// </summary>
    public class AnhangDFallstudieTests
    {
        // ---- Modelldaten, Tabelle D.2 ----
        private const double VOLLLASTSTUNDEN = 6697.0;
        private const double P_THERMISCH_KW = 90.0;
        private const double P_ELEKTRISCH_KW = 52.2;
        private const double NUTZUNGSGRAD_BHKW = 0.82;
        private const double NUTZUNGSGRAD_KESSEL = 0.85;

        /// <summary>Gasverbrauch des BHKW [kWh/a] — 1.161.358 kWh/a.</summary>
        private static readonly double Q_BHKW =
            VOLLLASTSTUNDEN * (P_THERMISCH_KW + P_ELEKTRISCH_KW) / NUTZUNGSGRAD_BHKW;

        /// <summary>Eingesparter Kesselbrennstoff [kWh/a] — 709.094 kWh/a.</summary>
        private static readonly double Q_KESSEL = VOLLLASTSTUNDEN * P_THERMISCH_KW / NUTZUNGSGRAD_KESSEL;

        /// <summary>Eigengenutzter Strom [kWh/a] — 349.583 kWh/a.</summary>
        private static readonly double Q_STROM = VOLLLASTSTUNDEN * P_ELEKTRISCH_KW;

        // ---- spezifische Preise, Tabelle D.4 ----
        private const double PREIS_GAS = 0.06;     // €/kWh
        private const double PREIS_STROM = 0.12;   // €/kWh

        /// <summary>Toleranz der Sollwerte [€]: die Norm rundet auf ganze Euro.</summary>
        private const double TOLERANZ_EUR = 1.0;

        /// <summary>Ein Fall der Fallstudie — Sätze in Prozent, wie der Rechenkern sie nimmt.</summary>
        private sealed class Fall
        {
            public double Investition = 90000.0;
            public double Wartung = 3000.0;          // Basiswert, Preise des Jahres 0
            public double PreisNichtEnergie = 2.0;   // %/a
            public double PreisEnergie = 3.0;        // %/a
            public double QBhkw = Q_BHKW;
            public double QKessel = Q_KESSEL;
            public double QStrom = Q_STROM;
            public int Jahre = 18;
            public double Zins = 6.96;               // %
        }

        /// <summary>Das Zahlungsbild der Variante mit BHKW.</summary>
        private static KapitalwertRechner.Zahlungsbild Variante(Fall f)
        {
            var investition = new List<KapitalwertRechner.InvestPosition>
            {
                // Nutzungsdauer = Planungshorizont: kein Ersatz, kein Restwert (D.5).
                new KapitalwertRechner.InvestPosition { Betrag = f.Investition, Nutzungsdauer = f.Jahre }
            };
            return KapitalwertRechner.Rechne(investition,
                Jahr1(f.Wartung, f.PreisNichtEnergie),
                Jahr1(f.QBhkw * PREIS_GAS, f.PreisEnergie),
                0.0, f.Zins, f.Jahre, f.PreisNichtEnergie, f.PreisEnergie);
        }

        /// <summary>Das Zahlungsbild der Referenz ohne BHKW: Kesselbrennstoff und Strombezug.</summary>
        private static KapitalwertRechner.Zahlungsbild Referenz(Fall f)
        {
            return KapitalwertRechner.Rechne(new List<KapitalwertRechner.InvestPosition>(), 0.0,
                Jahr1(f.QKessel * PREIS_GAS + f.QStrom * PREIS_STROM, f.PreisEnergie),
                0.0, f.Zins, f.Jahre, f.PreisNichtEnergie, f.PreisEnergie);
        }

        /// <summary>Norm (Basis zu Preisen des Jahres 0) → Rechenkern (Betrag des Jahres 1).</summary>
        private static double Jahr1(double basis, double satzProzent)
        {
            return basis * (1.0 + satzProzent / 100.0);
        }

        /// <summary>Der Kapitalwert der Maßnahme: Variante − Referenz.</summary>
        private static double Kapitalwert(Fall f)
        {
            return Variante(f).Kapitalwert - Referenz(f).Kapitalwert;
        }

        private static void Soll(double soll, double ist, string fall)
        {
            Assert.True(Math.Abs(ist - soll) <= TOLERANZ_EUR,
                fall + ": Rechenkern " + ist.ToString("N2") + " €, Norm " + soll.ToString("N0") + " €.");
        }

        // =====================================================================
        //  Die drei Sollwerte (D.5, D.7)
        // =====================================================================

        [Fact]
        public void Wahrscheinlichster_Fall_trifft_64480_Euro()
        {
            Soll(64480.0, Kapitalwert(new Fall()), "wahrscheinlichster Fall");
        }

        [Fact]
        public void Worst_Case_trifft_minus_202802_Euro()
        {
            var worst = new Fall
            {
                Investition = 120000.0, QBhkw = 1300000.0, QKessel = 600000.0, QStrom = 300000.0,
                PreisEnergie = 1.5, PreisNichtEnergie = 3.0, Jahre = 15, Zins = 9.0
            };
            Soll(-202802.0, Kapitalwert(worst), "Worst Case");
        }

        [Fact]
        public void Best_Case_trifft_598320_Euro()
        {
            var best = new Fall
            {
                Investition = 75000.0, QBhkw = 1000000.0, QKessel = 800000.0, QStrom = 400000.0,
                PreisEnergie = 4.5, PreisNichtEnergie = 1.5, Jahre = 21, Zins = 5.0
            };
            Soll(598320.0, Kapitalwert(best), "Best Case");
        }

        // =====================================================================
        //  Die Jahreswerte der Tabelle D.5
        // =====================================================================

        /// <summary>
        /// Die Jahresspalten der Tabelle D.5 (Jahre 1 und 18): Wartung, Gasverbrauch des
        /// BHKW, Netto und Barwert — dieselbe Reihe wie der Rechenkern, auf ganze Euro.
        /// </summary>
        [Fact]
        public void Jahreswerte_der_Tabelle_D5_stimmen()
        {
            var f = new Fall();
            KapitalwertRechner.Zahlungsbild v = Variante(f);
            KapitalwertRechner.Zahlungsbild r = Referenz(f);

            Assert.Equal(-90000.0, v.NominalReihe[0], 6);
            Assert.Equal(3060.0, Math.Round(v.BetriebJeJahr[1]));
            Assert.Equal(4285.0, Math.Round(v.BetriebJeJahr[18]));
            Assert.Equal(71772.0, Math.Round(v.EnergieJeJahr[1]));
            Assert.Equal(118628.0, Math.Round(v.EnergieJeJahr[18]));
            // Die Einsparungen der Referenz: Kessel 43.822 € + Strom 43.209 € im Jahr 1.
            Assert.Equal(43822.0 + 43209.0, Math.Round(r.EnergieJeJahr[1]), 0);

            double[] netto = KapitalwertRechner.Differenzreihe(v, r);
            Assert.Equal(12199.0, Math.Round(netto[1]));
            Assert.Equal(20935.0, Math.Round(netto[18]));
            Assert.Equal(11405.0, Math.Round(v.BarwertReihe[1] - r.BarwertReihe[1]));
            Assert.Equal(6236.0, Math.Round(v.BarwertReihe[18] - r.BarwertReihe[18]));
            Assert.Equal(0.0, v.RestwertNominal, 6);
        }

        // =====================================================================
        //  Die Sensitivitätstafel D.6 — ohne die drei ausgenommenen Zeilen
        // =====================================================================

        /// <summary>
        /// Die verwertbaren Zeilen der Tafel D.6: je Einstellparameter −50 % und +50 % um
        /// die Grundeinstellung, jede Zeile zwei vollständig neu gerechnete Zahlungsbilder;
        /// dazu die Steigung (NPV(+50 %) − NPV(−50 %)) / 100 in € je Prozent der Änderung.
        /// Die Zeilen „Reduzierter Energieverbrauch des Heizkessels", „Stromerzeugung zur
        /// Eigennutzung" (Werte des Pumpenbeispiels) und „Gasverbrauch BHKW" (Werte der
        /// Änderung des ganzen Energie-Nettostroms) sind ausgenommen — siehe Klassenkopf.
        /// </summary>
        [Theory]
        [InlineData("Energiepreisschwankung", 42705.0, 89886.0, 472.0)]
        [InlineData("Preisschwankung nicht energetisch", 67202.0, 61465.0, -57.0)]
        [InlineData("Laufzeit T", -522.0, 111605.0, 1121.0)]
        [InlineData("Kalkulationszins", 117983.0, 29057.0, -889.0)]
        [InlineData("CAPEX", 109480.0, 19480.0, -900.0)]
        [InlineData("OPEX", 82203.0, 46756.0, -354.0)]
        public void Sensitivitaet_der_Tafel_D6(string parameter, double minus50, double plus50, double steigung)
        {
            Fall unten = new Fall(), oben = new Fall();
            switch (parameter)
            {
                case "Energiepreisschwankung": unten.PreisEnergie = 1.5; oben.PreisEnergie = 4.5; break;
                case "Preisschwankung nicht energetisch": unten.PreisNichtEnergie = 1.0; oben.PreisNichtEnergie = 3.0; break;
                case "Laufzeit T": unten.Jahre = 9; oben.Jahre = 27; break;
                case "Kalkulationszins": unten.Zins = 3.48; oben.Zins = 10.44; break;
                case "CAPEX": unten.Investition = 45000.0; oben.Investition = 135000.0; break;
                case "OPEX": unten.Wartung = 1500.0; oben.Wartung = 4500.0; break;
                default: throw new ArgumentException(parameter);
            }
            double kwUnten = Kapitalwert(unten), kwOben = Kapitalwert(oben);
            Soll(minus50, kwUnten, parameter + " −50 %");
            Soll(plus50, kwOben, parameter + " +50 %");
            Assert.True(Math.Abs((kwOben - kwUnten) / 100.0 - steigung) <= TOLERANZ_EUR,
                        parameter + ": Steigung " + ((kwOben - kwUnten) / 100.0).ToString("N1") + " €/Δ%.");
        }
    }
}
