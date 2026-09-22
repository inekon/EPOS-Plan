using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Seiten.Berichte;
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

        /// <summary>
        /// E3/6: Der Gesetzeskatalog liegt selbst in <c>EPOS.UI.Daten</c> — die
        /// Überlagerung des Parameterdialogs steht damit ohne Windows-Schale,
        /// vorgewählt auf die Klasse des CO₂-Preises.
        /// </summary>
        [Fact]
        public void Parameterhuelle_traegt_den_Gesetzeskatalog_ohne_Naht_der_Schale()
        {
            IReadOnlyDictionary<string, object> gaben =
                WirtschaftlichkeitParameterHuelle.Gaben(PROJEKT_BHKW);

            Assert.True(gaben.ContainsKey("GesetzeGaben"));
            var weg = (Func<IReadOnlyDictionary<string, object>>)gaben["GesetzeGaben"];

            IReadOnlyDictionary<string, object> katalog = weg();
            Assert.NotNull(katalog);
            Assert.Equal(DbWerte.GESETZ_KLASSE_CO2_PREIS, katalog["Vorwahl"]);
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

        /// <summary>
        /// E3/6: Der Weg in die PV-Vergütung gehört seither dem WIRT
        /// (<c>KostenKomponenteDialog</c> zeigt sie als Überlagerung und setzt
        /// <c>PvOeffnen</c> selbst) — dieser Satz führt ihn nicht mehr.
        /// </summary>
        [Fact]
        public void Ertragbonus_fuehrt_den_Weg_in_die_Pv_Verguetung_nicht_mehr()
        {
            IReadOnlyDictionary<string, object> gaben =
                ErtragBonusGaben.Bauen(DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK);

            Assert.False(gaben.ContainsKey("PvOeffnen"),
                         "Den Rückruf setzt der Wirt, nicht die Gaben des Blatts.");
        }

        // =================================================================
        //  (5) KostenSeiteGaben — E3 Schritt 3
        // =================================================================

        [Fact]
        public void Kostenseite_baut_ihren_Satz_ohne_Windows_Dienst()
        {
            var seite = new KostenSeiteGaben();
            seite.SetzeGruppe(PROJEKT_BHKW, "");
            seite.SetzeProjekt(PROJEKT_BHKW, "");

            IReadOnlyDictionary<string, object> gaben = seite.Gaben();

            Assert.NotNull(gaben);
            Assert.True(gaben.ContainsKey("Laden"), "Der Ladeweg gehört zum Satz.");
            Assert.True(gaben.ContainsKey("VerwaltungGaben"));
        }

        /// <summary>
        /// E3/5: Die Kostenverwaltung braucht keine Naht mehr — ihre Hülle liegt
        /// selbst in <c>EPOS.UI.Daten</c>. Die Überlagerung der Kostenseite steht
        /// damit OHNE Windows-Schale, also auch auf iOS.
        /// </summary>
        [Fact]
        public void Kostenseite_zeigt_die_Kostenverwaltung_ohne_Naht_der_Schale()
        {
            var seite = new KostenSeiteGaben();
            seite.SetzeGruppe(PROJEKT_BHKW, "");
            seite.SetzeProjekt(PROJEKT_BHKW, "");

            var weg = (Func<KostenZeile, IReadOnlyDictionary<string, object>>)
                      seite.Gaben()["VerwaltungGaben"];

            IReadOnlyDictionary<string, object> gaben = weg(null);

            Assert.NotNull(gaben);
            Assert.True(gaben.ContainsKey("Laden"), "Der Ladeweg gehört zum Satz.");
            Assert.True(gaben.ContainsKey("Speichern"), "Der Schreibweg gehört zum Satz.");
        }

        // =================================================================
        //  (7) KostenKomponenteHuelle — E3 Schritt 5
        // =================================================================

        [Fact]
        public void Kostenverwaltung_baut_ihren_Satz_ohne_Windows_Dienst()
        {
            string titel;
            IReadOnlyDictionary<string, object> gaben =
                KostenKomponenteHuelle.FuerStamm().Gaben(null, false, 0, out titel);

            Assert.NotNull(gaben);
            Assert.True(gaben.ContainsKey("Eintraege"), "Die Klappliste gehört zum Satz.");
            Assert.True(gaben.ContainsKey("NutzungsdauerVorbelegen"),
                        "Die Nutzungsdauern-Vorbelegung gehört zum Satz.");
            Assert.True(gaben.ContainsKey("UebernahmeGaben"), "Die Übernahme gehört zum Satz.");
            Assert.False(string.IsNullOrEmpty(titel), "Der Titel entsteht in der Hülle.");
        }

        /// <summary>
        /// E3/6: Die Kostenverwaltung trägt ZWEI Überlagerungen, die bis dahin an
        /// der Schale hingen — den Gesetzeskatalog (jeweils ohne eigenen Titel,
        /// #187) und die PV-Vergütung. Beide stehen ohne Windows.
        /// </summary>
        [Fact]
        public void Kostenverwaltung_traegt_Gesetzeskatalog_und_Pv_ohne_Naht_der_Schale()
        {
            IReadOnlyDictionary<string, object> gaben =
                KostenKomponenteHuelle.GabenProjekt(PROJEKT_BHKW, "");

            Assert.True(gaben.ContainsKey("GesetzeGaben"));
            var gesetze = (Func<IReadOnlyDictionary<string, object>>)gaben["GesetzeGaben"];
            Assert.Equal("", gesetze()["TitelText"]);

            Assert.True(gaben.ContainsKey("PvGaben"));
            Assert.True(gaben.ContainsKey("PvTitel"));
            var pv = (Func<int, IReadOnlyDictionary<string, object>>)gaben["PvGaben"];
            IReadOnlyDictionary<string, object> pvSatz = pv(PROJEKT_BHKW);
            Assert.NotNull(pvSatz);
            Assert.True(pvSatz.ContainsKey("Modell"), "Der PV-Satz führt sein Modell.");
        }

        // =================================================================
        //  (6) WirtschaftlichkeitSeiteGaben — E3 Schritt 3
        // =================================================================

        [Fact]
        public void Wirtschaftlichkeitsseite_baut_ihren_Satz_ohne_Windows_Dienst()
        {
            var seite = new WirtschaftlichkeitSeiteGaben(PROJEKT_BHKW, "");

            IReadOnlyDictionary<string, object> gaben = seite.Gaben();

            Assert.NotNull(gaben);
            Assert.True(gaben.ContainsKey("Laden"), "Der Ladeweg gehört zum Satz.");
            Assert.True(gaben.ContainsKey("Berechnen"), "Der Rechenweg gehört zum Satz.");

            // Der Kern-Weg der Seite läuft ohne Schale: Laden ruft
            // WirtschaftlichkeitCtrl und BerichtsDatenSammler.ErmittleStatus.
            var laden = (Func<WirtschaftlichkeitStand>)gaben["Laden"];
            WirtschaftlichkeitStand stand = laden();
            Assert.NotNull(stand);
        }

        /// <summary>
        /// E3/6: Alle FÜNF Unterdialoge der Seite stehen ohne Windows-Schale —
        /// die Übergangsnaht <c>Wirtschaftlichkeitswege</c> ist weg, und damit ist
        /// die Seite auf iOS vollständig, nicht nur zur Hälfte.
        /// </summary>
        [Fact]
        public void Wirtschaftlichkeitsseite_zeigt_alle_fuenf_Unterdialoge_ohne_Schale()
        {
            var seite = new WirtschaftlichkeitSeiteGaben(PROJEKT_BHKW, "");
            var weg = (Func<WirtschaftlichkeitSeite.Unterdialog,
                            IReadOnlyDictionary<string, object>>)seite.Gaben()["Gaben"];

            Assert.NotNull(weg(WirtschaftlichkeitSeite.Unterdialog.Photovoltaik));
            Assert.NotNull(weg(WirtschaftlichkeitSeite.Unterdialog.Bhkw));
            Assert.NotNull(weg(WirtschaftlichkeitSeite.Unterdialog.Strombezug));
            Assert.NotNull(weg(WirtschaftlichkeitSeite.Unterdialog.Verlauf));
            Assert.NotNull(weg(WirtschaftlichkeitSeite.Unterdialog.Parameter));
        }

        // =================================================================
        //  (8) Die fünf Hüllen aus E3 Schritt 6
        // =================================================================

        [Fact]
        public void Gesetzeskatalog_baut_seinen_Satz_ohne_Windows_Dienst()
        {
            IReadOnlyDictionary<string, object> gaben =
                GesetzeskatalogHuelle.Gaben(DbWerte.GESETZ_KLASSE_CO2_PREIS);

            Assert.True(gaben.ContainsKey("Klassen"), "Die Klassenliste gehört zum Satz.");
            Assert.True(gaben.ContainsKey("Zeilen"), "Der Zeilenweg gehört zum Satz.");
            Assert.True(gaben.ContainsKey("Anlegen"), "Der Schreibweg gehört zum Satz.");
            Assert.Equal(DbWerte.GESETZ_KLASSE_CO2_PREIS, gaben["Vorwahl"]);
            Assert.False(string.IsNullOrEmpty(GesetzeskatalogHuelle.Titel()));
        }

        [Fact]
        public void Tarifstruktur_baut_ihren_Satz_ohne_Windows_Dienst()
        {
            IReadOnlyDictionary<string, object> gaben =
                TarifstrukturHuelle.Gaben(PROJEKT_BHKW, TarifSicht.Strombezug);

            Assert.NotNull(gaben["Tarif"]);
            Assert.Equal(TarifSicht.Strombezug, gaben["Sicht"]);
            Assert.True(gaben.ContainsKey("Speichern"), "Der Schreibweg gehört zum Satz.");
            Assert.False(string.IsNullOrEmpty(TarifstrukturHuelle.Titel(TarifSicht.Bhkw)));
        }

        [Fact]
        public void Pv_Verguetung_baut_ihren_Satz_ohne_Windows_Dienst()
        {
            IReadOnlyDictionary<string, object> gaben =
                PhotovoltaikVerguetungHuelle.Gaben(PROJEKT_BHKW);

            Assert.NotNull(gaben["Modell"]);
            Assert.True(gaben.ContainsKey("Speichern"), "Der Schreibweg gehört zum Satz.");

            // Die Dateiwahl des Marktwert-Imports läuft seit E3/2 über
            // Dienste.Datei und braucht keinen Fensterbesitzer mehr.
            Assert.True(gaben.ContainsKey("MarktwerteImportieren"));
            Assert.False(string.IsNullOrEmpty(PhotovoltaikVerguetungHuelle.Titel()));
        }

        [Fact]
        public void Bhkw_Wirtschaftlichkeit_baut_ihren_Satz_ohne_Windows_Dienst()
        {
            string titel;
            IReadOnlyDictionary<string, object> gaben =
                BhkwWirtschaftlichkeitHuelle.Gaben(PROJEKT_BHKW, null, out titel);

            Assert.Equal(PROJEKT_BHKW, gaben["IdStamm"]);
            Assert.NotNull(gaben["Anlagen"]);
            Assert.True(gaben.ContainsKey("SpeichereAnlage"), "Der erste Schreibweg fehlt.");
            Assert.True(gaben.ContainsKey("SpeichereVorgaben"), "Der zweite Schreibweg fehlt.");
            Assert.False(string.IsNullOrEmpty(titel));
        }

        /// <summary>
        /// P7: Sammeln, Rechnen und Zeichnen des Verlaufs laufen plattformfrei —
        /// der Renderer des Kerns braucht kein Windows, und der Arbeitsfaden
        /// entsteht über <c>Kulturweitergabe</c>. Gerechnet wird hier nicht (das
        /// wäre ein Simulationslauf); geprüft ist, dass der Satz ohne Schale
        /// steht und seinen Rechenweg trägt.
        /// </summary>
        [Fact]
        public void Kapitalwertverlauf_baut_seinen_Satz_ohne_Windows_Dienst()
        {
            Func<bool> neuGesammelt;
            IReadOnlyDictionary<string, object> gaben = KapitalwertVerlaufHuelle.Gaben(
                PROJEKT_BHKW, "", new List<int>(), out neuGesammelt);

            Assert.True(gaben.ContainsKey("Szenarien"), "Die drei Szenarien gehören zum Satz.");
            Assert.True(gaben.ContainsKey("Berechnen"), "Der Rechenweg gehört zum Satz.");
            Assert.True(gaben.ContainsKey("FarbeSetzen"), "Die Farbwahl gehört zum Satz.");
            Assert.NotNull(neuGesammelt);
            Assert.False(neuGesammelt(), "Vor dem ersten Lauf ist nichts gesammelt.");
        }
    }
}
