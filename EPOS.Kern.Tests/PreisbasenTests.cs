using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Klappliste „Preisbasis" des Energieträgerdialogs
    /// (<see cref="EnergietraegerPreisCtrl.Preisbasen"/>).
    ///
    /// <para><b>Befund UR-1 (Anwenderfoto 18.09.2026).</b> Die Liste entstand aus
    /// den Zieleinheiten der Umrechnungsregeln, und <b>deren <c>factor</c> rechnete
    /// den Arbeitspreis um</b>. Der Faktor einer Regel ist aber kein Heizwert: In
    /// der Testdatenbank trägt die Regel <c>Nm³ → kWh</c> des Erdgases E den Faktor
    /// <c>0,5</c>, während Hi bei <c>10,5 kWh/Nm³</c> steht. Wer 0,07 €/kWh eingab,
    /// bekam 0,035 €/Nm³ gespeichert und las in der Formelzeile 0,0033 €/kWh.</para>
    ///
    /// <para><b>Was jetzt gilt.</b> Genau zwei Einträge: die Abrechnungseinheit
    /// (Faktor 1) und die Kilowattstunde (Faktor = HEIZWERT). Ohne Heizwert gibt es
    /// keine kWh-Basis; rechnet der Träger ohnehin in kWh ab, bleibt der eine
    /// Eintrag. Die Regeln stellen nur noch die <c>ID_Umrechnung</c>, die die
    /// Projektzeile seit jeher merkt.</para>
    ///
    /// <para><b>Woher das leere Feld kam</b> (Befund W4-B-1, weiterhin gültig):
    /// Fünf Träger der Testdatenbank (73–77) hängen an <c>ID_Brennstoff = 0</c> und
    /// haben deshalb GAR KEINE Regel. Die Abrechnungseinheit steht trotzdem an
    /// erster Stelle — sie ist die Einheit, in der die Werte in der Datenbank
    /// liegen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PreisbasenTests
    {
        /// <summary>Heizwert des Erdgases E je Nm³ [kWh] — der Faktor der kWh-Basis.</summary>
        private const double HI_ERDGAS_E = 10.5;

        /// <summary>
        /// Der Windows-Läufer steht auf en-US — die Fälle hier vergleichen
        /// Einheitentexte wörtlich und legen die Oberflächensprache deshalb für
        /// ihre Dauer fest. Der Normalisierer arbeitet zwar kulturunabhängig
        /// (<c>ToUpperInvariant</c>); der Fall <see cref="Schluessel_ist_kulturunabhaengig"/>
        /// weist das eigens nach.
        /// </summary>
        private sealed class DeutscheOberflaeche : IDisposable
        {
            private readonly CultureInfo _vorherUi = Thread.CurrentThread.CurrentUICulture;
            private readonly CultureInfo _vorher = Thread.CurrentThread.CurrentCulture;

            public DeutscheOberflaeche()
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            }

            public void Dispose()
            {
                Thread.CurrentThread.CurrentUICulture = _vorherUi;
                Thread.CurrentThread.CurrentCulture = _vorher;
            }
        }

        private static EnergyConversion R(string von, string nach, double faktor)
        {
            return new EnergyConversion
            {
                IDBrennstoff = 3,
                FromUnit = von,
                ToUnitCode = nach,
                Factor = faktor
            };
        }

        /// <summary>Die drei Regeln des Brennstoffs 3 (Erdgas E) in Regelreihenfolge.</summary>
        private static List<EnergyConversion> ErdgasERegeln()
        {
            return new List<EnergyConversion>
            {
                R("Nm³", "Nm³", 1.0),   // Regel 40 - die Identitaetsregel
                R("Nm³", "kWh", 0.5),   // Regel 67 - der FALSCHE Faktor des Befunds
                R("m³",  "Nm³", 1.0)    // Regel 70 - der z-Faktor
            };
        }

        private static string[] Texte(IEnumerable<EnergietraegerPreisCtrl.Preisbasis> basen)
        {
            return basen.Select(b => b.Einheit).ToArray();
        }

        // =================================================================================
        // UR-1: Die kWh-Basis rechnet mit dem HEIZWERT
        // =================================================================================

        [Fact]
        public void Die_kWh_Basis_traegt_den_Heizwert_als_Faktor()
        {
            using var _ = new DeutscheOberflaeche();

            var basen = EnergietraegerPreisCtrl.Preisbasen("Nm³", ErdgasERegeln(), HI_ERDGAS_E);

            Assert.Equal(new[] { "Nm³", "kWh" }, Texte(basen));
            Assert.Equal(1.0, basen[0].Faktor);

            // Der Befund: Hier stand 0,5 - der Faktor der Regel 67.
            Assert.Equal(HI_ERDGAS_E, basen[1].Faktor, 9);
            Assert.NotEqual(0.5, basen[1].Faktor);
        }

        [Fact]
        public void Der_Anwenderfall_0_07_Euro_je_kWh_sind_0_735_Euro_je_Nm3()
        {
            using var _ = new DeutscheOberflaeche();

            var basen = EnergietraegerPreisCtrl.Preisbasen("Nm³", ErdgasERegeln(), HI_ERDGAS_E);
            double faktor = basen[1].Faktor;

            // Eingegeben 0,07 €/kWh -> gespeichert wird der Basiswert je Nm³.
            double basis = EnergietraegerPreiskarte.BasisArbeitspreis(0.07, faktor);
            Assert.Equal(0.735, basis, 9);

            // Und der Rückweg zeigt wieder genau 0,07 €/kWh.
            Assert.Equal(0.07, EnergietraegerPreiskarte.AnzeigeArbeitspreis(basis, faktor), 9);

            // Mit dem Regelfaktor 0,5 waeren daraus 0,035 €/Nm³ geworden - die Zahl
            // des Anwenderfotos.
            Assert.Equal(0.035, EnergietraegerPreiskarte.BasisArbeitspreis(0.07, 0.5), 9);
        }

        [Fact]
        public void Die_Regel_nach_kWh_traegt_weiter_die_ID_Umrechnung()
        {
            using var _ = new DeutscheOberflaeche();

            var basen = EnergietraegerPreisCtrl.Preisbasen("Nm³", ErdgasERegeln(), HI_ERDGAS_E);

            // Der Faktor kommt aus dem Heizwert, die KENNUNG aber weiter aus der
            // Regel - sonst verloere die Projektzeile ihre gespeicherte Preisbasis.
            Assert.NotNull(basen[1].Umrechnung);
            Assert.Equal("Nm³", basen[1].Umrechnung.FromUnit);
            Assert.Equal("kWh", basen[1].Umrechnung.ToUnitCode);

            // Die Abrechnungseinheit haengt an der Identitaetsregel (Nm³ -> Nm³),
            // nicht am z-Faktor (m³ -> Nm³).
            Assert.Equal("Nm³", basen[0].Umrechnung.FromUnit);
        }

        [Fact]
        public void Ohne_kWh_Regel_bleibt_die_kWh_Basis_trotzdem_waehlbar()
        {
            using var _ = new DeutscheOberflaeche();

            // Die Regel Nm³ -> kWh fuehrt nur Brennstoff 3; alle anderen Traeger
            // konnten bis ET-D gar nicht in €/kWh eingeben.
            var regeln = new List<EnergyConversion> { R("kg", "kg", 1.0), R("kg", "t", 0.001) };
            var basen = EnergietraegerPreisCtrl.Preisbasen("kg", regeln, 8.0);

            Assert.Equal(new[] { "kg", "kWh" }, Texte(basen));
            Assert.Equal(8.0, basen[1].Faktor, 9);
            Assert.Null(basen[1].Umrechnung);
        }

        // =================================================================================
        // Die Regeln stellen keine Preisbasis mehr
        // =================================================================================

        [Fact]
        public void Eine_Zieleinheit_einer_Regel_ist_keine_Preisbasis_mehr()
        {
            using var _ = new DeutscheOberflaeche();

            // "t" und "L" sind Zieleinheiten von Regeln - aber kein Preis wird je
            // Tonne eingegeben, und ein Faktor dorthin ist kein Heizwert.
            var regeln = new List<EnergyConversion> { R("kg", "t", 0.001), R("kg", "L", 2.0) };
            var basen = EnergietraegerPreisCtrl.Preisbasen("kg", regeln, 8.0);

            Assert.Equal(new[] { "kg", "kWh" }, Texte(basen));
        }

        [Fact]
        public void Ohne_Heizwert_gibt_es_nur_die_Abrechnungseinheit()
        {
            using var _ = new DeutscheOberflaeche();

            var basen = EnergietraegerPreisCtrl.Preisbasen("Nm³", ErdgasERegeln(), 0.0);

            Assert.Equal(new[] { "Nm³" }, Texte(basen));
            Assert.Equal(1.0, basen[0].Faktor);
        }

        [Fact]
        public void Wer_in_kWh_abrechnet_bekommt_kWh_nur_einmal()
        {
            using var _ = new DeutscheOberflaeche();

            // Strom und Fernwaerme: billing_unit = kWh, Hi = 1. Zwei Eintraege
            // "kWh" waeren derselbe Eintrag zweimal.
            var basen = EnergietraegerPreisCtrl.Preisbasen("kWh", null, 1.0);

            Assert.Equal(new[] { "kWh" }, Texte(basen));
            Assert.Equal(1.0, basen[0].Faktor);
        }

        [Fact]
        public void Ohne_Abrechnungseinheit_und_ohne_Heizwert_gibt_es_keine_Vorwahl()
        {
            using var _ = new DeutscheOberflaeche();

            var basen = EnergietraegerPreisCtrl.Preisbasen("", new List<EnergyConversion>(), 0.0);

            Assert.Empty(basen);
            Assert.Null(EnergietraegerPreisCtrl.PreisbasisIndex(basen, "kg"));
        }

        [Fact]
        public void Ohne_jede_Regel_steht_die_Abrechnungseinheit_trotzdem_in_der_Liste()
        {
            using var _ = new DeutscheOberflaeche();

            // Traeger 73-77 der Testdatenbank: ID_Brennstoff = 0, keine Regel.
            var basen = EnergietraegerPreisCtrl.Preisbasen("kg", new List<EnergyConversion>(), 4.8);

            Assert.Equal(new[] { "kg", "kWh" }, Texte(basen));
            Assert.Null(basen[0].Umrechnung);
            Assert.Equal(1.0, basen[0].Faktor);
            Assert.Equal(0, EnergietraegerPreisCtrl.PreisbasisIndex(basen, "kg"));
        }

        [Fact]
        public void Die_Abrechnungseinheit_ist_vorgewaehlt()
        {
            using var _ = new DeutscheOberflaeche();

            var basen = EnergietraegerPreisCtrl.Preisbasen("Nm³", ErdgasERegeln(), HI_ERDGAS_E);

            Assert.Equal(0, EnergietraegerPreisCtrl.PreisbasisIndex(basen, "Nm³"));
            Assert.Equal(1, EnergietraegerPreisCtrl.PreisbasisIndex(basen, "kWh"));
        }

        // =================================================================================
        // Die Ids verschieben sich nicht
        // =================================================================================

        [Fact]
        public void Umrechnen_auf_kWh_verschiebt_die_Ids_nicht()
        {
            using var _ = new DeutscheOberflaeche();

            // Die Huelle baut den Stand nach jedem Wechsel neu auf (PreisbasisGewechselt).
            // Aus denselben Eingaben muss dieselbe Liste in derselben Reihenfolge
            // entstehen - sonst zeigte die gemerkte Id nach dem Wechsel auf eine
            // andere Zeile.
            var vorher = EnergietraegerPreisCtrl.Preisbasen("Nm³", ErdgasERegeln(), HI_ERDGAS_E);
            int kwh = EnergietraegerPreisCtrl.PreisbasisIndex(vorher, "kWh").Value;
            Assert.Equal(1, kwh);

            var nachher = EnergietraegerPreisCtrl.Preisbasen("Nm³", ErdgasERegeln(), HI_ERDGAS_E);

            Assert.Equal(Texte(vorher), Texte(nachher));
            Assert.Equal(kwh, EnergietraegerPreisCtrl.PreisbasisIndex(nachher, "kWh"));
            Assert.Equal("kWh", nachher[kwh].Einheit);
            Assert.Equal(HI_ERDGAS_E, nachher[kwh].Faktor, 9);
        }

        // =================================================================================
        // Schreibweisen
        // =================================================================================

        [Fact]
        public void Nm3_und_Nm_hoch_3_sind_dieselbe_Einheit()
        {
            using var _ = new DeutscheOberflaeche();

            var regeln = new List<EnergyConversion> { R("Nm3", "Nm3", 1.0), R("Nm³", "kWh", 0.5) };
            var basen = EnergietraegerPreisCtrl.Preisbasen("Nm³", regeln, HI_ERDGAS_E);

            // Angezeigt wird die Schreibweise der Abrechnungseinheit; "Nm3" der
            // Regel ist dieselbe Einheit und traegt trotzdem die Identitaetsregel.
            Assert.Equal(new[] { "Nm³", "kWh" }, Texte(basen));
            Assert.Equal("Nm3", basen[0].Umrechnung.FromUnit);
        }

        [Fact]
        public void Eine_Regel_ohne_Zieleinheit_wird_uebergangen()
        {
            using var _ = new DeutscheOberflaeche();

            var regeln = new List<EnergyConversion> { R("L", "", 1.0), null, R("L", "kWh", 0.1) };
            var basen = EnergietraegerPreisCtrl.Preisbasen("L", regeln, 10.0);

            Assert.Equal(new[] { "L", "kWh" }, Texte(basen));
            Assert.Equal(10.0, basen[1].Faktor, 9);
        }

        [Fact]
        public void Schluessel_ist_kulturunabhaengig()
        {
            // Die tuerkische Kultur bildet "i" nicht auf "I" ab - der Schluessel
            // muss trotzdem derselbe sein, sonst haengt die Einheitenpruefung an
            // der eingestellten Sprache.
            CultureInfo vorher = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                string tuerkisch = EnergietraegerPreisCtrl.EinheitSchluessel("Liter");

                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                Assert.Equal(EnergietraegerPreisCtrl.EinheitSchluessel("liter"), tuerkisch);
                Assert.Equal("", EnergietraegerPreisCtrl.EinheitSchluessel(null));
                Assert.Equal("NM3", EnergietraegerPreisCtrl.EinheitSchluessel(" nm³ "));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = vorher;
            }
        }

        // =================================================================================
        // Gegen die Testdatenbank
        // =================================================================================

        [Fact]
        public void Kein_Traeger_der_Testdatenbank_fuehrt_mehr_als_zwei_Preisbasen()
        {
            using var _ = new DeutscheOberflaeche();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Derselbe Leseweg, den die Hülle nimmt.
            List<EnergyCarrier> traeger = KostenSummenCtrl.GetAllCarriers(0);
            Assert.NotEmpty(traeger);

            foreach (EnergyCarrier t in traeger)
            {
                var basen = EnergietraegerPreisCtrl.Preisbasen(
                    t.BillingUnit, EnergietraegerPreisCtrl.Umrechnungen(t.ID_Brennstoff),
                    t.HiKwhPerUnit);

                Assert.True(basen.Count <= 2, t.Name + " führt " + basen.Count + " Preisbasen.");

                string[] schluessel = basen
                    .Select(b => EnergietraegerPreisCtrl.EinheitSchluessel(b.Einheit)).ToArray();
                Assert.Equal(schluessel.Length, schluessel.Distinct().Count());

                // Die Abrechnungseinheit ist immer da und immer vorgewaehlt -
                // damit kann das Feld nicht mehr leer bleiben.
                if (string.IsNullOrWhiteSpace(t.BillingUnit)) continue;
                Assert.Equal(0, EnergietraegerPreisCtrl.PreisbasisIndex(basen, t.BillingUnit));

                // Und wo es eine kWh-Basis gibt, ist ihr Faktor der Heizwert.
                if (basen.Count == 2) Assert.Equal(t.HiKwhPerUnit, basen[1].Faktor, 9);
            }
        }

        [Fact]
        public void Erdgas_E_der_Testdatenbank_rechnet_mit_10_5_statt_0_5()
        {
            using var _ = new DeutscheOberflaeche();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Brennstoff 3 = Erdgas E, Traeger 63 - der Fall des Bildschirmfotos.
            var basen = EnergietraegerPreisCtrl.Preisbasen(
                "Nm³", EnergietraegerPreisCtrl.Umrechnungen(3), HI_ERDGAS_E);

            Assert.Equal(new[] { "Nm³", "kWh" }, Texte(basen));
            Assert.Equal(0, EnergietraegerPreisCtrl.PreisbasisIndex(basen, "Nm³"));
            Assert.Equal(HI_ERDGAS_E, basen[1].Faktor, 9);
        }

        [Fact]
        public void Eine_abgeschaltete_Regel_wird_nicht_mehr_gelesen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Jede gelesene Regel ist aktiv - der Filter sitzt in der Abfrage.
            foreach (EnergyConversion c in EnergietraegerPreisCtrl.Umrechnungen(3))
                Assert.NotNull(c);

            int aktive = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_conversion WHERE id_brennstoff = 3 AND aktiv = 1"));
            Assert.Equal(aktive, EnergietraegerPreisCtrl.Umrechnungen(3).Count);
        }
    }
}
