using System;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der ZAHLENABZUG zum Anwenderentscheid vom 04.09.2026 (W11a-O-1): Er zeigt für
    /// die drei Referenzprojekte, was sich am Ergebnisblock der Übersicht ändert, wenn
    /// die sechs Summen die DECKUNG je Erzeuger führen statt der Produktion.
    ///
    /// <para><b>Warum als Testfall und nicht als Wegwerf-Harnisch.</b> Der Abzug ist die
    /// Begründung einer Zahlenänderung; er muss nachvollziehbar bleiben. Er prüft
    /// deshalb genau das, was der Entscheid verlangt — nicht die Zahlen selbst (die
    /// hängen am Projektstand der Testdatenbank), sondern die beiden Zusagen:</para>
    /// <list type="number">
    ///   <item>Der Restwärmebedarf ist in BEIDEN Feldern derselbe Wert.</item>
    ///   <item>Er ist NIE negativ — eine negative Restwärme zeigt eine falsche
    ///   Zuordnung zu den Erzeugern.</item>
    /// </list>
    ///
    /// <para>Die gemessenen Zahlen schreibt er ins Testprotokoll; ohne Testdatenbank
    /// schweigt er.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class W11bZahlenabzug : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _ausgabe;

        public W11bZahlenabzug(TestDatenbank db, ITestOutputHelper ausgabe)
        {
            _db = db;
            _ausgabe = ausgabe;
        }

        [Theory]
        [InlineData(1030)]
        [InlineData(1007)]
        [InlineData(1017)]
        public void Restwaerme_ist_eine_Zahl_und_nie_negativ(int idProjekt)
        {
            if (!_db.Vorhanden) return;

            SimulationRunner laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(idProjekt, out fehler), "Lauf gescheitert: " + fehler);

            var u = SimulationErgebnisCtrl.Uebersicht(
                laeufer.sim, laeufer.simulation_Waermebedarf, laeufer.simulation_Strombedarf);

            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Projekt {0}: Bedarf {1:F2} | Deckung gesamt {2:F2} | Restwaerme {3:F2} " +
                "(WP {4:F2}, Heizstab {5:F2}, Solar {6:F2}, Kessel {7:F2}, BHKW {8:F2})",
                idProjekt, u.WaermebedarfGesamtMwh, u.WaermeGesamtMwh, u.RestwaermebedarfMwh,
                u.WaermeWpMwh, u.WaermeHeizstabMwh, u.WaermeSolarMwh,
                u.WaermeKesselMwh, u.WaermeBhkwMwh));

            // (1) Eine Zahl, nicht zwei.
            Assert.Equal(u.RestwaermeMwh, u.RestwaermebedarfMwh);

            // (2) Nie negativ.
            Assert.True(u.RestwaermebedarfMwh >= 0.0,
                        "Projekt " + idProjekt + ": Restwaerme " + u.RestwaermebedarfMwh);

            // Und die Summe ist wirklich die Summe der fünf Deckungsanteile.
            Assert.Equal(u.WaermeWpMwh + u.WaermeHeizstabMwh + u.WaermeSolarMwh +
                         u.WaermeKesselMwh + u.WaermeBhkwMwh, u.WaermeGesamtMwh, 9);
        }
    }
}
