using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Temperaturpaar je Prozess auf seinen Wegen</b> (PW1 Stufe 1): Katalog → Projektkopie,
    /// Stammkopf schreiben, Projektkopie schreiben, Speichern der Zuordnungen, Vergleichszeilen und
    /// Bericht. Geschrieben wird nur ein zulässiges Paar; ohne Änderung bleibt die Kopie.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProzesswaermeTemperaturWegeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        private const int PROJEKT = 1050;

        /// <summary>Ein eigener Katalogsatz mit Paar (Typ „CONT"); Rückgabe sein Name.</summary>
        private static string Katalogsatz(string name, double? v, double? r)
        {
            Assert.True(BedarfStammCtrl.SaveHead(BedarfsArt.Prozesswaerme, name, "CONT", "",
                                                 Enumerable.Repeat(1.0, 12).ToArray(), true, v, r));
            return name;
        }

        [Fact]
        public void Die_Kopie_traegt_das_Paar_des_Katalogs()
        {
            if (!_db.Vorhanden) return;
            string name = Katalogsatz("PW-Kopie", 75, 40);
            Assert.Equal((75.0, 40.0), ProzesswaermeStammCtrl.Temperaturpaar(name));

            int kopie = ProzesswaermeStammCtrl.CopyFromStamm(name, PROJEKT);
            Assert.True(kopie > 0);
            Assert.Equal((75.0, 40.0), ProzesswaermeStammCtrl.ProjektTemperaturpaar(kopie));

            var ctrl = new ProzesswaermeStammCtrl();
            ctrl.ReadSingle(name);
            Assert.Equal(75, ctrl.m_Vorlauf);
            Assert.Equal(40, ctrl.m_Ruecklauf);
        }

        [Fact]
        public void Projektkopie_nur_mit_zulaessigem_Paar()
        {
            if (!_db.Vorhanden) return;
            int kopie = ProzesswaermeStammCtrl.CopyFromStamm(Katalogsatz("PW-Ohne", null, null), PROJEKT);
            Assert.True(kopie > 0);
            Assert.Equal((null, null), ProzesswaermeStammCtrl.ProjektTemperaturpaar(kopie));

            Assert.True(ProzesswaermeStammCtrl.ProjektTemperaturSetzen(kopie, 90, 60));
            Assert.Equal((90.0, 60.0), ProzesswaermeStammCtrl.ProjektTemperaturpaar(kopie));
            Assert.False(ProzesswaermeStammCtrl.ProjektTemperaturSetzen(kopie, 50, 60));
            Assert.False(ProzesswaermeStammCtrl.ProjektTemperaturSetzen(kopie, 90, null));
            Assert.Equal((90.0, 60.0), ProzesswaermeStammCtrl.ProjektTemperaturpaar(kopie));
            Assert.True(ProzesswaermeStammCtrl.ProjektTemperaturSetzen(kopie, null, null));
            Assert.Equal((null, null), ProzesswaermeStammCtrl.ProjektTemperaturpaar(kopie));
        }

        [Fact]
        public void Der_Stammkopf_schreibt_das_Paar()
        {
            if (!_db.Vorhanden) return;
            var monat = Enumerable.Repeat(1.0, 12).ToArray();
            Assert.True(BedarfStammCtrl.SaveHead(BedarfsArt.Prozesswaerme, "PW-Probe", "CONT", "", monat, true, 120, 90));
            Assert.Equal((120.0, 90.0), BedarfStammCtrl.Temperaturpaar(BedarfsArt.Prozesswaerme, "PW-Probe"));

            Assert.False(BedarfStammCtrl.SaveHead(BedarfsArt.Prozesswaerme, "PW-Probe", "CONT", "", monat, false, 60, 90));
            Assert.Equal((120.0, 90.0), BedarfStammCtrl.Temperaturpaar(BedarfsArt.Prozesswaerme, "PW-Probe"));

            // Der Weg ohne Paar (Verwaltung) lässt es stehen.
            Assert.True(BedarfStammCtrl.SaveHead(BedarfsArt.Prozesswaerme, "PW-Probe", "CONT", "x", monat, false));
            Assert.Equal((120.0, 90.0), BedarfStammCtrl.Temperaturpaar(BedarfsArt.Prozesswaerme, "PW-Probe"));

            // Die Vergleichszeilen führen Vorlauf und Rücklauf mit Stufe Simulation.
            IReadOnlyList<Parameterwert> z = BedarfStammCtrl.Vergleichszeilen(BedarfsArt.Prozesswaerme, "PW-Probe");
            Assert.Contains(z, p => p.Eintrag.Spalte == "Vorlauf" && p.Wert == "120" && p.Eintrag.Hat(Verwendung.Simulation));
            Assert.Contains(z, p => p.Eintrag.Spalte == "Ruecklauf" && p.Wert == "90");
            Assert.DoesNotContain(BedarfStammCtrl.Vergleichszeilen(BedarfsArt.Brauchwasser, "x"),
                                  p => p.Eintrag.Spalte == "Vorlauf");
            Assert.Equal((null, null), BedarfStammCtrl.Temperaturpaar(BedarfsArt.Brauchwasser, "PW-Probe"));
        }

        [Fact]
        public void Speichern_der_Zuordnung_schreibt_nur_ein_geaendertes_Paar_und_der_Bericht_zeigt_es()
        {
            if (!_db.Vorhanden) return;
            string name = Katalogsatz("PW-Zuordnung", 75, 40);
            var ctrl = new WizardCtrl();

            var ohne = new List<Z_ProjektProzesswaermeModel>
            {
                new Z_ProjektProzesswaermeModel { szProzessname = name, Summe = 20, Vorlauf = 99, Ruecklauf = 10 }
            };
            Assert.True(ctrl.Add_Projekt_Prozess(PROJEKT, ohne));
            int kopie = ohne[0].ID_Prozesswaerme;
            Assert.Equal((75.0, 40.0), ProzesswaermeStammCtrl.ProjektTemperaturpaar(kopie));   // Vorbelegung bleibt

            List<Z_ProjektProzesswaermeModel> gelesen = Z_ProjektProzesswaermeCtrl.LiesProjekt(PROJEKT);
            Assert.Equal(75, gelesen.Single(g => g.ID_Prozesswaerme == kopie).Vorlauf);

            DataRepository.ExecuteSQL("DELETE FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
            var mit = new List<Z_ProjektProzesswaermeModel>
            {
                new Z_ProjektProzesswaermeModel
                {
                    ID_Prozesswaerme = kopie, szProzessname = name, Summe = 20,
                    Vorlauf = 95, Ruecklauf = 65, TemperaturGeaendert = true
                }
            };
            Assert.True(ctrl.Add_Projekt_Prozess(PROJEKT, mit));
            Assert.Equal((95.0, 65.0), ProzesswaermeStammCtrl.ProjektTemperaturpaar(kopie));

            ProjektDetails d = ProjektDetails.Lade(PROJEKT);
            Assert.Equal(1, d.ProzessMitTemperatur);
            Assert.Equal(95, d.ProzessVorlaufMax);
            Assert.Equal(65, d.ProzessRuecklaufMin);

            // Ein Projekt ohne Prozess mit Paar führt keins.
            Assert.Null(ProjektDetails.Lade(1007).ProzessVorlaufMax);
        }
    }
}
