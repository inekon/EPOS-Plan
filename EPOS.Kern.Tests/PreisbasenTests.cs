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
    /// (<see cref="EnergietraegerPreisCtrl.Preisbasen"/>) — Befund W4-B-1 aus der
    /// Windows-Abnahme vom 04.09.2026: „Die Preisbasis wird teilweise nicht
    /// angezeigt oder doppelt."
    ///
    /// <para><b>Woher die Dublette kam.</b> Der Vorläufer <c>ucFuelSettings</c>
    /// füllte die Liste mit der <c>to_unit</c> JEDER Regel des Brennstoffs —
    /// ohne Filter, ohne <c>ORDER BY</c>, ohne Dublettenprüfung
    /// (<c>Konzept_Einheitenbruch_Energietraeger_EPOS-Plan.md</c> § 2.2), und die
    /// Blazor-Hülle übernahm das wortgleich. Die Gas-Brennstoffe führen neben der
    /// Identitätsregel <c>Nm³ → Nm³</c> auch den z-Faktor <c>m³ → Nm³</c>: beide
    /// zeigen auf dieselbe Zieleinheit, also stand „Nm³" zweimal in der Liste.
    /// Bei Erdgas E kam über Regel 67 (<c>Nm³ → kWh</c>) genau das gemeldete
    /// „Nm³, kWh, Nm³" zustande.</para>
    ///
    /// <para><b>Woher das leere Feld kam.</b> Fünf Träger der Testdatenbank
    /// (73–77) hängen an <c>ID_Brennstoff = 0</c> und haben deshalb GAR KEINE
    /// Regel. Die Liste blieb leer, die Vorwahl damit <c>null</c> — und ein
    /// <c>&lt;select&gt;</c> ohne passende Option zeigt nichts an.</para>
    ///
    /// <para><b>Warum m³ keine Preisbasis ist.</b> Eine Preisbasis ist eine
    /// ZIELeinheit: Es muss einen Faktor geben, der den Wert aus der
    /// Abrechnungseinheit dorthin trägt. „m³" steht bei den Gasen nur als
    /// <c>from_unit</c> des z-Faktors; eine Regel <c>Nm³ → m³</c> gibt es nicht.
    /// Seit Migrationsschritt 26a ist m³ zudem „keine Abrechnungseinheit eines
    /// Gasträgers mehr" (<c>DbWerte.cs</c>, Leitentscheidung L4).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PreisbasenTests
    {
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
                R("Nm³", "kWh", 0.5),   // Regel 67
                R("m³",  "Nm³", 1.0)    // Regel 70 - der z-Faktor
            };
        }

        private static string[] Texte(IEnumerable<EnergietraegerPreisCtrl.Preisbasis> basen)
        {
            return basen.Select(b => b.Einheit).ToArray();
        }

        // =================================================================================
        // Der gemeldete Fall
        // =================================================================================

        [Fact]
        public void Erdgas_E_zeigt_jede_Einheit_genau_einmal()
        {
            using var _ = new DeutscheOberflaeche();

            var basen = EnergietraegerPreisCtrl.Preisbasen("Nm³", ErdgasERegeln());

            // Gemeldet war "Nm³, kWh, Nm³" - Nm³ doppelt.
            Assert.Equal(new[] { "Nm³", "kWh" }, Texte(basen));
        }

        [Fact]
        public void Erdgas_E_haengt_die_Abrechnungseinheit_an_die_Identitaetsregel()
        {
            using var _ = new DeutscheOberflaeche();

            var basen = EnergietraegerPreisCtrl.Preisbasen("Nm³", ErdgasERegeln());

            // Nicht an Regel 70 (m³ -> Nm³), sondern an Regel 40 (Nm³ -> Nm³):
            // dieselbe Zeile, die auch der Vorlaeufer ueber seinen Textvergleich fand.
            Assert.Equal("Nm³", basen[0].Umrechnung.FromUnit);
            Assert.Equal(1.0, basen[0].Faktor);
            Assert.Equal(0.5, basen[1].Faktor);
        }

        [Fact]
        public void Die_Abrechnungseinheit_ist_vorgewaehlt()
        {
            using var _ = new DeutscheOberflaeche();

            var basen = EnergietraegerPreisCtrl.Preisbasen("Nm³", ErdgasERegeln());

            Assert.Equal(0, EnergietraegerPreisCtrl.PreisbasisIndex(basen, "Nm³"));
        }

        // =================================================================================
        // Das leere Feld
        // =================================================================================

        [Fact]
        public void Ohne_jede_Regel_steht_die_Abrechnungseinheit_trotzdem_in_der_Liste()
        {
            using var _ = new DeutscheOberflaeche();

            // Traeger 73-77 der Testdatenbank: ID_Brennstoff = 0, keine Regel.
            var basen = EnergietraegerPreisCtrl.Preisbasen("kg", new List<EnergyConversion>());

            Assert.Equal(new[] { "kg" }, Texte(basen));
            Assert.Null(basen[0].Umrechnung);
            Assert.Equal(1.0, basen[0].Faktor);
            Assert.Equal(0, EnergietraegerPreisCtrl.PreisbasisIndex(basen, "kg"));
        }

        [Fact]
        public void Eine_Abrechnungseinheit_ohne_eigene_Regel_steht_trotzdem_in_der_Liste()
        {
            using var _ = new DeutscheOberflaeche();

            // Keine Regel traegt nach "t" - die Abrechnungseinheit gehoert
            // trotzdem an die erste Stelle, sonst zeigt der Dialog eine Einheit,
            // in der die Werte gar nicht liegen.
            var regeln = new List<EnergyConversion> { R("kg", "kg", 1.0), R("kg", "L", 2.0) };
            var basen = EnergietraegerPreisCtrl.Preisbasen("t", regeln);

            Assert.Equal(new[] { "t", "kg", "L" }, Texte(basen));
            Assert.Null(basen[0].Umrechnung);
            Assert.Equal(0, EnergietraegerPreisCtrl.PreisbasisIndex(basen, "t"));
        }

        [Fact]
        public void Ohne_Abrechnungseinheit_und_ohne_Regel_gibt_es_keine_Vorwahl()
        {
            using var _ = new DeutscheOberflaeche();

            var basen = EnergietraegerPreisCtrl.Preisbasen("", new List<EnergyConversion>());

            Assert.Empty(basen);
            Assert.Null(EnergietraegerPreisCtrl.PreisbasisIndex(basen, "kg"));
        }

        [Fact]
        public void Null_statt_einer_Regelliste_ist_erlaubt()
        {
            using var _ = new DeutscheOberflaeche();

            var basen = EnergietraegerPreisCtrl.Preisbasen("kWh", null);

            Assert.Equal(new[] { "kWh" }, Texte(basen));
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
            var vorher = EnergietraegerPreisCtrl.Preisbasen("Nm³", ErdgasERegeln());
            int kwh = EnergietraegerPreisCtrl.PreisbasisIndex(vorher, "kWh").Value;
            Assert.Equal(1, kwh);

            var nachher = EnergietraegerPreisCtrl.Preisbasen("Nm³", ErdgasERegeln());

            Assert.Equal(Texte(vorher), Texte(nachher));
            Assert.Equal(kwh, EnergietraegerPreisCtrl.PreisbasisIndex(nachher, "kWh"));
            Assert.Equal("kWh", nachher[kwh].Einheit);
            Assert.Equal(0.5, nachher[kwh].Faktor);
        }

        // =================================================================================
        // Schreibweisen
        // =================================================================================

        [Fact]
        public void Nm3_und_Nm_hoch_3_sind_dieselbe_Einheit()
        {
            using var _ = new DeutscheOberflaeche();

            var regeln = new List<EnergyConversion> { R("Nm3", "Nm3", 1.0), R("Nm³", "kWh", 0.5) };
            var basen = EnergietraegerPreisCtrl.Preisbasen("Nm³", regeln);

            // Angezeigt wird die Schreibweise der Abrechnungseinheit; "Nm3" der
            // Regel ist dieselbe Einheit und kommt kein zweites Mal.
            Assert.Equal(new[] { "Nm³", "kWh" }, Texte(basen));
        }

        [Fact]
        public void Rand_Leerzeichen_und_Grossschreibung_erzeugen_keine_Dublette()
        {
            using var _ = new DeutscheOberflaeche();

            var regeln = new List<EnergyConversion> { R("kg", " KG ", 1.0), R("kg", "t", 0.001) };
            var basen = EnergietraegerPreisCtrl.Preisbasen("kg", regeln);

            Assert.Equal(new[] { "kg", "t" }, Texte(basen));
        }

        [Fact]
        public void Eine_Regel_ohne_Zieleinheit_wird_uebergangen()
        {
            using var _ = new DeutscheOberflaeche();

            var regeln = new List<EnergyConversion> { R("L", "", 1.0), null, R("L", "kg", 0.84) };
            var basen = EnergietraegerPreisCtrl.Preisbasen("L", regeln);

            Assert.Equal(new[] { "L", "kg" }, Texte(basen));
        }

        [Fact]
        public void Schluessel_ist_kulturunabhaengig()
        {
            // Die tuerkische Kultur bildet "i" nicht auf "I" ab - der Schluessel
            // muss trotzdem derselbe sein, sonst haengt die Dublettenpruefung an
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
        // Gegen die Testdatenbank - kein Traeger des Katalogs zeigt noch eine Dublette
        // =================================================================================

        [Fact]
        public void Kein_Traeger_der_Testdatenbank_fuehrt_eine_Einheit_zweimal()
        {
            using var _ = new DeutscheOberflaeche();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Derselbe Leseweg, den die Hülle nimmt (EnergietraegerHuelle.cs:248).
            List<EnergyCarrier> traeger = KostenSummenCtrl.GetAllCarriers(0);
            Assert.NotEmpty(traeger);

            foreach (EnergyCarrier t in traeger)
            {
                var basen = EnergietraegerPreisCtrl.Preisbasen(
                    t.BillingUnit, EnergietraegerPreisCtrl.Umrechnungen(t.ID_Brennstoff));

                string[] schluessel = basen
                    .Select(b => EnergietraegerPreisCtrl.EinheitSchluessel(b.Einheit)).ToArray();

                Assert.Equal(schluessel.Length, schluessel.Distinct().Count());

                // Die Abrechnungseinheit ist immer da und immer vorgewaehlt -
                // damit kann das Feld nicht mehr leer bleiben.
                if (string.IsNullOrWhiteSpace(t.BillingUnit)) continue;
                Assert.Equal(0, EnergietraegerPreisCtrl.PreisbasisIndex(basen, t.BillingUnit));
            }
        }

        [Fact]
        public void Erdgas_E_der_Testdatenbank_liefert_Nm3_und_kWh()
        {
            using var _ = new DeutscheOberflaeche();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Brennstoff 3 = Erdgas E, Traeger 63 - der Fall des Bildschirmfotos.
            var basen = EnergietraegerPreisCtrl.Preisbasen(
                "Nm³", EnergietraegerPreisCtrl.Umrechnungen(3));

            Assert.Equal(new[] { "Nm³", "kWh" }, Texte(basen));
            Assert.Equal(0, EnergietraegerPreisCtrl.PreisbasisIndex(basen, "Nm³"));
        }
    }
}
