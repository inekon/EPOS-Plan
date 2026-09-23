using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Einheiten- und Umrechnungsrechnung der Trägerkarte
    /// (<see cref="EnergietraegerPreiskarte"/>) — Anwenderbefund vom 14.09.2026:
    /// „Heizwert und Brennwert zeigen die Einheit kWh/kWh", während der Träger
    /// nach Nm³ abrechnet.
    ///
    /// <para><b>Woher das kam.</b> Die Hülle schrieb die Einheit von Heiz- und
    /// Brennwert aus der PREISBASIS (<c>"kWh/" + Preisbasis</c>) und rechnete
    /// beide Stoffwerte mit deren Faktor um. Stand die Preisbasis auf kWh — das
    /// tut sie, sobald ein Projekt die Regel Nm³ → kWh gespeichert hat —, las
    /// sich der Heizwert als „kWh/kWh", und jedes Speichern multiplizierte Hi,
    /// Hs und Arbeitspreis erneut mit dem Faktor.</para>
    ///
    /// <para><b>Die Regel dagegen:</b> Heizwert und Brennwert sind Stoffwerte je
    /// ABRECHNUNGSEINHEIT und tragen immer <c>kWh/&lt;Abrechnungseinheit&gt;</c>;
    /// nur der Arbeitspreis folgt der Preisbasis.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergietraegerPreiskarteTests
    {
        // =================================================================
        // Einheitentexte je Abrechnungseinheit
        // =================================================================

        [Theory]
        [InlineData("Nm³", "kWh/Nm³")]
        [InlineData("L", "kWh/L")]
        [InlineData("kg", "kWh/kg")]
        [InlineData("kWh", "kWh/kWh")]
        public void Heizwert_und_Brennwert_tragen_kWh_je_Abrechnungseinheit(
            string abrechnungseinheit, string erwartet)
        {
            using var _ = new Kulturvorrichtung();

            Assert.Equal(erwartet, EnergietraegerPreiskarte.HeizwertEinheit(abrechnungseinheit));
        }

        [Fact]
        public void Ohne_Abrechnungseinheit_bleibt_es_bei_kWh()
        {
            using var _ = new Kulturvorrichtung();

            Assert.Equal("kWh", EnergietraegerPreiskarte.HeizwertEinheit(""));
            Assert.Equal("kWh", EnergietraegerPreiskarte.HeizwertEinheit(null));
        }

        [Fact]
        public void Die_Preisbasis_steht_allein_am_Arbeitspreis()
        {
            using var _ = new Kulturvorrichtung();

            // Der Anwender gibt in Nm³ ein - oder in kWh, wenn er mag.
            Assert.Equal("€/Nm³", EnergietraegerPreiskarte.ArbeitspreisEinheit("Nm³", true));
            Assert.Equal("€/kWh", EnergietraegerPreiskarte.ArbeitspreisEinheit("kWh", true));

            // Ein Träger ohne Heizwert (Strom, Fernwärme) rechnet unmittelbar in kWh.
            Assert.Equal("€ / kWh", EnergietraegerPreiskarte.ArbeitspreisEinheit("kWh", false));
        }

        [Fact]
        public void Der_Leistungspreis_folgt_dem_Modus_nicht_der_Preisbasis()
        {
            using var _ = new Kulturvorrichtung();

            Assert.Equal("€/(kW·a)", EnergietraegerPreiskarte.LeistungspreisEinheit(false));
            Assert.Equal("€/(kW·Monat)", EnergietraegerPreiskarte.LeistungspreisEinheit(true));
        }

        // =================================================================
        // Anzeige und Basis - nur der Arbeitspreis
        // =================================================================

        [Fact]
        public void Der_Arbeitspreis_geht_durch_den_Faktor_und_wieder_zurueck()
        {
            using var _ = new Kulturvorrichtung();

            // Regel Nm³ -> kWh, Faktor 10,5: 0,50 €/Nm³ sind 0,0476 €/kWh.
            double anzeige = EnergietraegerPreiskarte.AnzeigeArbeitspreis(0.50, 10.5);
            Assert.Equal(0.50 / 10.5, anzeige, 12);

            Assert.Equal(0.50, EnergietraegerPreiskarte.BasisArbeitspreis(anzeige, 10.5), 12);
        }

        [Fact]
        public void Ein_Faktor_von_null_laesst_den_Wert_stehen()
        {
            using var _ = new Kulturvorrichtung();

            Assert.Equal(0.50, EnergietraegerPreiskarte.AnzeigeArbeitspreis(0.50, 0.0));
            Assert.Equal(0.50, EnergietraegerPreiskarte.BasisArbeitspreis(0.50, 0.0));
        }

        // =================================================================
        // Die Formelzeile nennt ihre Einheiten
        // =================================================================

        [Fact]
        public void Die_Formelzeile_nennt_Einheiten_statt_nackter_Zahlen()
        {
            using var _ = new Kulturvorrichtung();

            EnergietraegerPreiskarte.Formelzeile z =
                EnergietraegerPreiskarte.Formel(true, "Nm³", "Nm³", 0.50, 10.50);

            Assert.NotNull(z);
            Assert.Equal("0,0476 €", z.PreisJeKwh);
            // ANWENDERENTSCHEID Q7 (Etappe E2): Der ZÄHLER steht mit vier
            // Nachkommastellen. Mit zwei rundete er weg, was das Ergebnis darunter
            // mit vier Stellen ausweist — die Division ging nicht auf.
            Assert.Equal("0,5000 €/Nm³ ÷ 10,50 kWh/Nm³ = 0,0476 €/kWh", z.Text);
        }

        /// <summary>
        /// <b>ET-D-4.</b> Steht die Preisbasis auf kWh, gibt der Anwender €/kWh ein —
        /// die Zeile läuft dann in DIESER Richtung und sagt, in welcher Einheit
        /// gespeichert wird. Das frühere „Direktabrechnung: … €/kWh" las sich wie ein
        /// Abrechnungsweg und zeigte nur die eigene Eingabe.
        /// </summary>
        [Fact]
        public void Bei_Preisbasis_kWh_rechnet_die_Formel_von_der_Eingabe_zum_gespeicherten_Preis()
        {
            using var _ = new Kulturvorrichtung();

            // Stadtgas: 0,07 €/kWh bei Hi 4,8 kWh/Nm³ = 0,336 €/Nm³ je Mengeneinheit.
            EnergietraegerPreiskarte.Formelzeile z =
                EnergietraegerPreiskarte.Formel(true, "Nm³", "kWh", 0.336, 4.80);

            Assert.NotNull(z);
            Assert.Equal("0,0700 €/kWh × 4,80 kWh/Nm³ = 0,3360 €/Nm³ (gespeichert je Nm³)", z.Text);
            Assert.Equal("0,0700 €", z.PreisJeKwh);
            Assert.DoesNotContain("Direktabrechnung", z.Text);
        }

        [Fact]
        public void Die_Formel_je_kWh_steht_auch_auf_Englisch()
        {
            using var _ = new Kulturvorrichtung("en-US");

            EnergietraegerPreiskarte.Formelzeile z =
                EnergietraegerPreiskarte.Formel(true, "Nm³", "kWh", 0.336, 4.80);

            Assert.Equal("0.0700 €/kWh × 4.80 kWh/Nm³ = 0.3360 €/Nm³ (stored per Nm³)", z.Text);
        }

        [Fact]
        public void Eine_Abrechnung_nach_kWh_nennt_sich_Direktabrechnung()
        {
            using var _ = new Kulturvorrichtung();

            // Strom, Fernwaerme: kein Heizwert.
            EnergietraegerPreiskarte.Formelzeile ohne =
                EnergietraegerPreiskarte.Formel(false, "kWh", "kWh", 0.2500, 0.0);
            Assert.Equal("0,2500 €", ohne.PreisJeKwh);
            Assert.Equal("Direktabrechnung nach kWh", ohne.Text);

            // Auch ein Traeger MIT Heizwert, der nach kWh abrechnet.
            EnergietraegerPreiskarte.Formelzeile mit =
                EnergietraegerPreiskarte.Formel(true, "kWh", "kWh", 0.2500, 1.0);
            Assert.Equal("Direktabrechnung nach kWh", mit.Text);
        }

        [Fact]
        public void Ohne_Heizwert_bleiben_die_Texte_stehen()
        {
            using var _ = new Kulturvorrichtung();

            // Der Vorlaeufer kehrte hier zurueck, ohne etwas zu setzen - null
            // heisst genau das: nichts zu rechnen.
            Assert.Null(EnergietraegerPreiskarte.Formel(true, "Nm³", "Nm³", 0.50, 0.0));
        }

        // =================================================================
        // Effektivzeile - ueber der Abrechnungseinheit
        // =================================================================

        [Fact]
        public void Die_Effektivzeile_steht_ueber_der_Abrechnungseinheit()
        {
            using var _ = new Kulturvorrichtung();

            Assert.Equal("effektiv: 1 Nm³ = 10,50 kWh (Hi) / 11,60 kWh (Hs)",
                         EnergietraegerPreiskarte.Effektivzeile("Nm³", 10.5, 11.6));

            Assert.Equal("effektiv: Abrechnung unmittelbar in kWh",
                         EnergietraegerPreiskarte.Effektivzeile("kWh", 1.0, 1.0));
        }

        [Fact]
        public void Die_Schreibweise_der_Einheit_ist_egal()
        {
            using var _ = new Kulturvorrichtung();

            Assert.True(EnergietraegerPreiskarte.IstKwh("kWh"));
            Assert.True(EnergietraegerPreiskarte.IstKwh(" KWH "));
            Assert.False(EnergietraegerPreiskarte.IstKwh("Nm³"));
            Assert.False(EnergietraegerPreiskarte.IstKwh(""));
        }

        // =================================================================
        // ET-D-1: Die Anzeigekante der Preisbestandteile
        // =================================================================

        /// <summary>
        /// Der gerechnete Anteil steht in ct/kWh, der angezeigte in
        /// €/Abrechnungseinheit. 0,6076 ct/kWh sind bei Hi 10,5 kWh/m³ genau
        /// 0,063798 €/m³ — die Zahl des Mockups (0,0638 auf vier Stellen).
        /// </summary>
        [Fact]
        public void Ein_Anteil_geht_ueber_den_Heizwert_in_die_Abrechnungseinheit()
        {
            Assert.Equal(0.063798, EnergietraegerPreiskarte.AnteilJeEinheit(0.6076, 10.5), 9);
            Assert.Equal(0.6076, EnergietraegerPreiskarte.AnteilCtKwh(0.063798, 10.5), 9);
        }

        /// <summary>
        /// Hin und zurück verliert keine Stelle, und die zweite Runde ändert
        /// nichts mehr: Eine Anzeigekante, an der ein Wert bei jedem Hinsehen
        /// wandert, wäre keine. Bei Hi = 1 (ein Träger, der in kWh abrechnet)
        /// bleibt allein die Cent-Wandlung übrig; sie ist auf 12 Stellen genau
        /// und danach ein Festpunkt.
        /// </summary>
        [Theory]
        [InlineData(0.55, 1.0)]
        [InlineData(0.6076, 1.0)]
        [InlineData(2.05, 1.0)]
        [InlineData(6.44, 1.0)]
        [InlineData(0.1371, 1.0)]
        [InlineData(0.0, 1.0)]
        [InlineData(0.6076, 10.5)]
        [InlineData(1.3711, 10.5)]
        public void Hin_und_Rueckweg_verlieren_keine_Stelle(double ctKwh, double hi)
        {
            double hin = EnergietraegerPreiskarte.AnteilJeEinheit(ctKwh, hi);
            double zurueck = EnergietraegerPreiskarte.AnteilCtKwh(hin, hi);
            Assert.Equal(ctKwh, zurueck, 12);

            // Festpunkt: Eine zweite Runde bewegt den Wert nicht mehr.
            double zweite = EnergietraegerPreiskarte.AnteilCtKwh(
                EnergietraegerPreiskarte.AnteilJeEinheit(zurueck, hi), hi);
            Assert.True(zurueck.Equals(zweite));
        }

        /// <summary>
        /// Ohne Heizwert gibt es keinen Weg in die Abrechnungseinheit — der Wert
        /// bleibt stehen, und die Einheit bleibt ct/kWh.
        /// </summary>
        [Fact]
        public void Ohne_Heizwert_bleibt_der_Anteil_in_ct_je_kWh()
        {
            Assert.Equal(0.6076, EnergietraegerPreiskarte.AnteilJeEinheit(0.6076, 0.0), 9);
            Assert.Equal(0.6076, EnergietraegerPreiskarte.AnteilCtKwh(0.6076, 0.0), 9);
            Assert.Equal("ct/kWh", EnergietraegerPreiskarte.AnteilEinheit("m³", 0.0));
        }

        [Theory]
        [InlineData("m³", 10.5, "€/m³")]
        [InlineData("L", 10.0, "€/L")]
        [InlineData("kWh", 1.0, "ct/kWh")]
        [InlineData("", 10.5, "ct/kWh")]
        public void Die_Anzeigeeinheit_folgt_der_Abrechnungseinheit(
            string abrechnungseinheit, double hi, string erwartet)
        {
            Assert.Equal(erwartet, EnergietraegerPreiskarte.AnteilEinheit(abrechnungseinheit, hi));
        }

        // =================================================================
        // Hs/Hi und die CO2-Masse je Abrechnungseinheit
        // =================================================================

        [Fact]
        public void Der_Faktor_Hs_durch_Hi_steht_nur_mit_beiden_Werten()
        {
            Assert.Equal(1.104762, EnergietraegerPreiskarte.FaktorHsHi(10.5, 11.6).Value, 6);
            Assert.Null(EnergietraegerPreiskarte.FaktorHsHi(10.5, 0.0));
            Assert.Null(EnergietraegerPreiskarte.FaktorHsHi(0.0, 11.6));
        }

        /// <summary>
        /// Die Herleitung des BEHG-Anteils: 200,9 g/kWh × 10,5 kWh/m³ ÷ 1000 =
        /// 2,109 kg CO₂ je m³; mit 65 €/t sind das 0,1371 €/m³.
        /// </summary>
        [Fact]
        public void Die_CO2_Masse_je_Abrechnungseinheit_faellt_aus_Faktor_und_Heizwert()
        {
            double kg = EnergietraegerPreiskarte.Co2MasseJeEinheit(200.9, 10.5).Value;
            Assert.Equal(2.10945, kg, 9);
            Assert.Equal(0.13711425, kg * 65.0 / 1000.0, 9);

            Assert.Null(EnergietraegerPreiskarte.Co2MasseJeEinheit(0.0, 10.5));
            Assert.Null(EnergietraegerPreiskarte.Co2MasseJeEinheit(200.9, 0.0));
        }

        /// <summary>
        /// Die Energiesteuer-Herleitung: 5,50 €/MWh sind brennwertbezogen —
        /// 0,55 ct/kWh × Hs/Hi = 0,6076 ct/kWh, und das sind 0,0638 €/m³.
        /// </summary>
        [Fact]
        public void Der_brennwertbezogene_Satz_kommt_ueber_Hs_durch_Hi_an()
        {
            double ctKwh = 5.50 / 10.0 * EnergietraegerPreiskarte.FaktorHsHi(10.5, 11.6).Value;
            Assert.Equal(0.607619, ctKwh, 6);
            Assert.Equal(0.0638, EnergietraegerPreiskarte.AnteilJeEinheit(ctKwh, 10.5), 4);
        }

        // =================================================================
        // UR-1: Die Preisbasis rechnet mit dem Heizwert
        // =================================================================

        /// <summary>
        /// Der Anwenderfall: 0,07 €/kWh eingegeben, 0,735 €/Nm³ gespeichert —
        /// nicht 0,035 €/Nm³ (Regelfaktor 0,5). Der Weg zurück ist bitgleich für
        /// den Faktor 1 (Preisbasis = Abrechnungseinheit).
        /// </summary>
        [Fact]
        public void Die_Preisbasis_kWh_rechnet_mit_dem_Heizwert()
        {
            Assert.Equal(0.735, EnergietraegerPreiskarte.BasisArbeitspreis(0.07, 10.5), 9);
            Assert.Equal(0.07, EnergietraegerPreiskarte.AnzeigeArbeitspreis(0.735, 10.5), 9);

            // Faktor 1: unveraendert, Bit fuer Bit.
            Assert.True((0.0638).Equals(
                EnergietraegerPreiskarte.AnzeigeArbeitspreis(
                    EnergietraegerPreiskarte.BasisArbeitspreis(0.0638, 1.0), 1.0)));
        }
    }
}
