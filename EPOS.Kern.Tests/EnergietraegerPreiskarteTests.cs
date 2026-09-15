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
            Assert.Equal("0,50 €/Nm³ ÷ 10,50 kWh/Nm³ = 0,0476 €/kWh", z.Text);
        }

        [Fact]
        public void Bei_Preisbasis_kWh_kommt_die_Direktabrechnung_dazu()
        {
            using var _ = new Kulturvorrichtung();

            EnergietraegerPreiskarte.Formelzeile z =
                EnergietraegerPreiskarte.Formel(true, "Nm³", "kWh", 0.50, 10.50);

            Assert.NotNull(z);
            Assert.StartsWith("0,50 €/Nm³ ÷ 10,50 kWh/Nm³ = 0,0476 €/kWh", z.Text);
            Assert.Contains("Direktabrechnung: 0,0476 €/kWh", z.Text);
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
    }
}
