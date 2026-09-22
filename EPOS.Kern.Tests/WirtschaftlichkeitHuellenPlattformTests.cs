using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die ZEUGEN der Etappe E3, Schritt 1: Die vier nahtlosen Hüllen der Kosten-
    /// und Wirtschaftlichkeitsseite sind plattformfrei erreichbar.
    ///
    /// <para><b>Was hier bewiesen wird.</b> Jede der vier Hüllen liegt seit E3/1 in
    /// <c>EPOS.UI.Daten</c> und baut ihren Parametersatz allein aus Kern-Controllern.
    /// Dieser Prüfstand läuft ohne Windows-Schale — kein <c>Program.Main</c>, kein
    /// belegtes <c>Dienste.*</c>, kein Fenster —, und genau das ist der Nachweis:
    /// Was hier durchläuft, läuft auch auf iOS. Die Fälle arbeiten auf der
    /// ARBEITSKOPIE der Testdatenbank (<see cref="TestDatenbank"/>).</para>
    ///
    /// <para><b>Die Naht gehört dazu.</b> Zwei Wege der vier Hüllen zeigen bis
    /// E3 Schritt 6 noch ein eigenes Fenster und kommen deshalb über
    /// <c>Wirtschaftlichkeitswege</c> herein. Der Prüfstand hält beide Seiten der
    /// Hausregel „kein Delegat, kein Knopf" fest: OHNE eingehängte Naht fehlt der
    /// Schlüssel im Parametersatz, MIT Naht steht er darin und trifft den
    /// eingehängten Weg.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WirtschaftlichkeitHuellenPlattformTests
    {
        /// <summary>Ein Referenzprojekt mit BHKW — Stammprojekt der Parameterhülle.</summary>
        private const int PROJEKT_BHKW = 1030;

        // =================================================================
        //  (1) WirtschaftlichkeitParameterHuelle
        // =================================================================

        [Fact]
        public void Parameterhuelle_baut_ihren_Satz_ohne_Windows_Dienst()
        {
            IReadOnlyDictionary<string, object> gaben =
                WirtschaftlichkeitParameterHuelle.Gaben(PROJEKT_BHKW);

            Assert.NotNull(gaben);
            Assert.True(gaben.ContainsKey("Parameter"), "Der Parametersatz führt die Parameter.");
            Assert.NotNull(gaben["Parameter"]);
            Assert.True(gaben.ContainsKey("Speichern"), "Der Schreibweg gehört zum Satz.");
        }

        [Fact]
        public void Parameterhuelle_ohne_Naht_bietet_den_Gesetzeskatalog_nicht_an()
        {
            Func<string, IReadOnlyDictionary<string, object>> vorher =
                Wirtschaftlichkeitswege.GesetzeskatalogGaben;
            try
            {
                Wirtschaftlichkeitswege.GesetzeskatalogGaben = null;

                IReadOnlyDictionary<string, object> gaben =
                    WirtschaftlichkeitParameterHuelle.Gaben(PROJEKT_BHKW);

                Assert.False(gaben.ContainsKey("GesetzeGaben"),
                             "Kein Delegat, kein Knopf: ohne Naht kein Schlüssel.");
            }
            finally { Wirtschaftlichkeitswege.GesetzeskatalogGaben = vorher; }
        }

        [Fact]
        public void Parameterhuelle_mit_Naht_reicht_die_Klasse_des_Co2_Preises_durch()
        {
            Func<string, IReadOnlyDictionary<string, object>> vorher =
                Wirtschaftlichkeitswege.GesetzeskatalogGaben;
            try
            {
                string gerufen = null;
                Wirtschaftlichkeitswege.GesetzeskatalogGaben = klasse =>
                {
                    gerufen = klasse;
                    return new Dictionary<string, object>();
                };

                IReadOnlyDictionary<string, object> gaben =
                    WirtschaftlichkeitParameterHuelle.Gaben(PROJEKT_BHKW);

                Assert.True(gaben.ContainsKey("GesetzeGaben"));
                var weg = (Func<IReadOnlyDictionary<string, object>>)gaben["GesetzeGaben"];
                Assert.NotNull(weg());
                Assert.Equal(DbWerte.GESETZ_KLASSE_CO2_PREIS, gerufen);
            }
            finally { Wirtschaftlichkeitswege.GesetzeskatalogGaben = vorher; }
        }

        // =================================================================
        //  (2) KostenfaktorKatalogHuelle
        // =================================================================

        [Fact]
        public void Kostenfaktorhuelle_baut_ihren_Satz_ohne_Windows_Dienst()
        {
            IReadOnlyDictionary<string, object> gaben = KostenfaktorKatalogHuelle.Gaben();

            Assert.NotNull(gaben);
            Assert.NotEmpty(gaben);
        }

        // =================================================================
        //  (3) VorlagenUebernahmeHuelle
        // =================================================================

        [Fact]
        public void Vorlagenuebernahme_baut_ihren_Satz_ohne_Windows_Dienst()
        {
            IList<KeyValuePair<int, string>> komponenten = KostenVorlagenCtrl.Komponenten();
            Assert.NotEmpty(komponenten);
            KeyValuePair<int, string> erste = komponenten[0];

            IReadOnlyDictionary<string, object> gaben = VorlagenUebernahmeHuelle.Gaben(
                erste.Key, erste.Value, KostenSummenCtrl.KATEGORIE_INVESTITION, null);

            Assert.NotNull(gaben);
            Assert.NotEmpty(gaben);
        }

        // =================================================================
        //  (4) ErtragBonusGaben
        // =================================================================

        [Fact]
        public void Ertragbonus_baut_seinen_Satz_ohne_Windows_Dienst()
        {
            Assert.True(ErtragBonusGaben.HatInhalt(DbWerte.KOSTEN_KOMPONENTE_BHKW));
            Assert.True(ErtragBonusGaben.HatInhalt(DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK));
            Assert.False(ErtragBonusGaben.HatInhalt("Heizkessel"));

            IReadOnlyDictionary<string, object> gaben =
                ErtragBonusGaben.Bauen(DbWerte.KOSTEN_KOMPONENTE_BHKW);

            Assert.NotNull(gaben);
            Assert.NotEmpty(gaben);
        }

        [Fact]
        public void Ertragbonus_ohne_Naht_bietet_keinen_Weg_in_die_Pv_Verguetung()
        {
            Action<int> vorher = Wirtschaftlichkeitswege.PvVerguetungOeffnen;
            try
            {
                Wirtschaftlichkeitswege.PvVerguetungOeffnen = null;

                IReadOnlyDictionary<string, object> gaben =
                    ErtragBonusGaben.Bauen(DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK);

                Assert.False(gaben.ContainsKey("PvOeffnen"),
                             "Kein Delegat, kein Knopf: ohne Naht kein Schlüssel.");
            }
            finally { Wirtschaftlichkeitswege.PvVerguetungOeffnen = vorher; }
        }

        [Fact]
        public void Ertragbonus_mit_Naht_traegt_den_Weg_in_die_Pv_Verguetung()
        {
            Action<int> vorher = Wirtschaftlichkeitswege.PvVerguetungOeffnen;
            try
            {
                Wirtschaftlichkeitswege.PvVerguetungOeffnen = _ => { };

                IReadOnlyDictionary<string, object> gaben =
                    ErtragBonusGaben.Bauen(DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK);

                Assert.True(gaben.ContainsKey("PvOeffnen"));
            }
            finally { Wirtschaftlichkeitswege.PvVerguetungOeffnen = vorher; }
        }
    }
}
