using System;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Umrechnung der BHKW-Investition (<see cref="BHKWKosten"/>) — Anwenderentscheid
    /// <b>W14a-E-8-B3</b> vom 07.09.2026: „Entweder Investitionskosten als Summe/Gesamt
    /// oder Investitionskosten auf kWh elektrisch x Kosten pro kWh elektrisch (sollte
    /// umgerechnet werden, je nach Eingabe)."
    ///
    /// <para><b>Was hier bewiesen wird.</b> Die drei Eingabewege — Gesamtsumme, Wert je
    /// kWel, fuenf Einzelposten — treffen dieselbe Groesse, und der Weg hin und zurueck
    /// bleibt stehen. Das ist die Bedingung dafuer, dass die Kostenplanung unveraendert
    /// mit den Posten rechnen kann: Jede Eingabe landet in ihnen, keine erzeugt einen
    /// sechsten Betrag.</para>
    ///
    /// <para>Die Kultur ist auf de-DE gepinnt. Gerechnet wird zwar kulturfrei, aber ein
    /// Testlauf unter einer anderen Kultur soll dieselben Zahlen sehen wie der Anwender.</para>
    ///
    /// <para><b>Rückstellung (iU9‑#167):</b> <c>CultureInfo.CurrentCulture</c> ist
    /// threadgebunden — xunit kann denselben Pool-Thread später für eine andere Klasse
    /// wiederverwenden, die selbst keine Kultur setzt. Ohne <see cref="Dispose"/> bliebe
    /// de-DE auf diesem Thread stehen.</para>
    /// </summary>
    public class BhkwKostenTests : IDisposable
    {
        private readonly CultureInfo _kulturVorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiKulturVorher = CultureInfo.CurrentUICulture;

        public BhkwKostenTests()
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _kulturVorher;
            CultureInfo.CurrentUICulture = _uiKulturVorher;
        }

        // =============================================================================
        // Die Summe der Posten
        // =============================================================================

        [Fact]
        public void Summe_addiert_die_fuenf_Posten()
        {
            Assert.Equal(50000.0, BHKWKosten.Summe(40000, 5000, 1000, 3000, 1000), 10);
        }

        [Fact]
        public void Gesamt_ist_die_Summe_auf_den_Cent_gerundet()
        {
            // Fuenf Drittel-Cent-Betraege: die rohe Summe traegt eine dritte
            // Nachkommastelle, die Gesamtsumme nicht mehr.
            Assert.Equal(0.555, BHKWKosten.Summe(0.111, 0.111, 0.111, 0.111, 0.111), 10);
            Assert.Equal(0.56, BHKWKosten.Gesamt(0.111, 0.111, 0.111, 0.111, 0.111), 10);
        }

        [Fact]
        public void Gesamt_rundet_den_halben_Cent_kaufmaennisch_auf()
        {
            // 0,005 EUR ist ein halber Cent - kaufmaennisch aufgerundet, nicht auf die
            // gerade Stelle (MidpointRounding.ToEven ergaebe 0,00).
            Assert.Equal(0.01, BHKWKosten.Gesamt(0.005, 0, 0, 0, 0), 10);
        }

        [Fact]
        public void Nebenposten_zaehlt_alles_ausser_dem_Modulpreis()
        {
            Assert.Equal(10000.0, BHKWKosten.Nebenposten(5000, 1000, 3000, 1000), 10);
        }

        // =============================================================================
        // Der Wert je kWel — die eine Richtung
        // =============================================================================

        [Fact]
        public void JeKWel_teilt_die_Summe_durch_die_elektrische_Leistung()
        {
            Assert.Equal(1250.0, BHKWKosten.JeKWel(50000, 40), 10);
        }

        [Fact]
        public void JeKWel_bleibt_ungerundet_damit_der_Rueckweg_die_Summe_trifft()
        {
            // 50 000 / 37 geht nicht auf. Der gespeicherte Wert bleibt exakt - erst so
            // ergibt Investition_kwel * Pel wieder die erfasste Summe.
            double jeKw = BHKWKosten.JeKWel(50000, 37);

            Assert.Equal(50000.0 / 37.0, jeKw, 10);
            Assert.Equal(50000.0, jeKw * 37.0, 6);
        }

        [Fact]
        public void JeKWelEingabe_liegt_auf_dem_Raster_von_einem_Euro_je_kW()
        {
            // 50 000 / 37 = 1351,351... -> das Eingabefeld zeigt 1351.
            Assert.Equal(1351.0, BHKWKosten.JeKWelEingabe(50000, 37), 10);

            // Und der halbe Euro geht nach oben, nicht auf die gerade Stelle.
            Assert.Equal(3.0, BHKWKosten.JeKWelEingabe(5, 2), 10);
        }

        // =============================================================================
        // Die Gegenrichtung
        // =============================================================================

        [Fact]
        public void GesamtAusJeKWel_multipliziert_mit_der_elektrischen_Leistung()
        {
            Assert.Equal(50000.0, BHKWKosten.GesamtAusJeKWel(1250, 40), 10);
        }

        [Fact]
        public void GesamtAusJeKWel_rundet_das_Ergebnis_auf_den_Cent()
        {
            // 1234,567 EUR/kW * 3 kW = 3703,701 EUR -> 3703,70 EUR.
            Assert.Equal(3703.70, BHKWKosten.GesamtAusJeKWel(1234.567, 3), 10);
        }

        [Fact]
        public void ModulAusGesamt_laesst_den_Nebenposten_ihren_Anteil()
        {
            // 50 000 gesamt, 10 000 Nebenposten -> 40 000 fuer das Modul.
            Assert.Equal(40000.0, BHKWKosten.ModulAusGesamt(50000, 5000, 1000, 3000, 1000), 10);
        }

        [Fact]
        public void ModulAusGesamt_wird_nicht_negativ()
        {
            // Eine Gesamtsumme unter den Nebenposten ergaebe rechnerisch -7 000 EUR.
            // Ein negativer Geraetepreis waere keine Angabe, sondern ein Fehler.
            Assert.Equal(0.0, BHKWKosten.ModulAusGesamt(3000, 5000, 1000, 3000, 1000), 10);
            Assert.True(BHKWKosten.NebenpostenUeberschreiten(3000, 5000, 1000, 3000, 1000));
        }

        [Fact]
        public void Genau_aufgehende_Nebenposten_uebersteigen_die_Gesamtsumme_nicht()
        {
            Assert.Equal(0.0, BHKWKosten.ModulAusGesamt(10000, 5000, 1000, 3000, 1000), 10);
            Assert.False(BHKWKosten.NebenpostenUeberschreiten(10000, 5000, 1000, 3000, 1000));
        }

        // =============================================================================
        // Hin und zurueck
        // =============================================================================

        [Fact]
        public void Gesamt_ueber_den_Modulpreis_zurueck_ergibt_wieder_die_Gesamtsumme()
        {
            double modul = BHKWKosten.ModulAusGesamt(63500.55, 5000, 1000, 3000, 1000);

            Assert.Equal(63500.55, BHKWKosten.Gesamt(modul, 5000, 1000, 3000, 1000), 10);
        }

        [Fact]
        public void Je_kW_ueber_die_Gesamtsumme_zurueck_ergibt_wieder_denselben_Wert()
        {
            double gesamt = BHKWKosten.GesamtAusJeKWel(1351, 37);
            double modul = BHKWKosten.ModulAusGesamt(gesamt, 5000, 1000, 3000, 1000);

            Assert.Equal(1351.0,
                BHKWKosten.JeKWelEingabe(BHKWKosten.Gesamt(modul, 5000, 1000, 3000, 1000), 37), 10);
        }

        // =============================================================================
        // Ohne elektrische Leistung
        // =============================================================================

        [Fact]
        public void Ohne_elektrische_Leistung_gibt_es_keinen_Wert_je_kW()
        {
            Assert.False(BHKWKosten.JeKWelBestimmbar(0));
            Assert.Equal(0.0, BHKWKosten.JeKWel(50000, 0), 10);
            Assert.Equal(0.0, BHKWKosten.JeKWelEingabe(50000, 0), 10);

            // Auch die Gegenrichtung gibt es nicht: jede Zahl mal 0 waere wieder 0 und
            // wuerde die erfasste Summe verschweigen.
            Assert.Equal(0.0, BHKWKosten.GesamtAusJeKWel(1250, 0), 10);
        }

        [Fact]
        public void Ohne_elektrische_Leistung_bleibt_die_Gesamtsumme_erfasst()
        {
            // Die Posten sind von Pel unabhaengig - nur die Kennzahl je kW faellt weg.
            Assert.Equal(50000.0, BHKWKosten.Gesamt(40000, 5000, 1000, 3000, 1000), 10);
            Assert.Equal(40000.0, BHKWKosten.ModulAusGesamt(50000, 5000, 1000, 3000, 1000), 10);
        }
    }
}
