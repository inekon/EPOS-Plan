using System;
using System.Globalization;
using System.Linq;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Kühlung je Zone im Datenbankfall (Welle KU3-3)</b> — an einer Arbeitskopie der Testdatenbank
    /// (<see cref="TestDatenbank"/>, nichts bleibt): das Zonenprojekt 1052 mit eingeschaltetem Kühlbetrieb,
    /// gekühltem Gebäude (26 °C), eigenem Kühlsollwert der Gästezimmer (24 °C) und <c>Kuehlung_Aktiv = 0</c> an
    /// Gastronomie und Verwaltung. Der Lauf liest die Zonenspalten und schreibt die Kühlenergie je Zone nach
    /// <c>Tab_ErgebnisZone</c> — nur für die gekühlte Zone; die Gebäudesumme ist die Summe der Zonen.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KuehlungJeZoneDatenbankTests
    {
        private readonly ITestOutputHelper _aus;

        public KuehlungJeZoneDatenbankTests(ITestOutputHelper aus) { _aus = aus; }

        private const int PROJEKT = Zonenprojekt1052.NEU;

        private static void Setzen(string sql, params object[] werte)
            => DataRepository.ExecuteNonQuery(sql, werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray());

        [Fact]
        public void Lauf_1052_mit_Kuehlung_je_Zone_liest_die_Zonenspalten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int geb = Zonenprojekt1052.Gebaeude(PROJEKT);
            Setzen("UPDATE Tab_Einstellungen SET Kuehlbetrieb = 1 WHERE ID_Projekt = ?", PROJEKT);
            Setzen("UPDATE Tab_Gebaeude SET Kuehlung_Aktiv = 1, Kuehl_Sollwert = ?, Kuehlleistung_Max = ? WHERE ID = ?", 26.0, 40.0, geb);
            Setzen("UPDATE Tab_Zone SET Kuehl_Sollwert = ? WHERE ID_Gebaeude = ? AND Bezeichner = ?", 24.0, geb, Zonenprojekt1052.ZONE_GAESTE);
            Setzen("UPDATE Tab_Zone SET Kuehlung_Aktiv = 0 WHERE ID_Gebaeude = ? AND Bezeichner = ?", geb, Zonenprojekt1052.ZONE_GASTRO);

            int kopf = new SimulationRunner().SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);

            ErgebnisGebaeudeModel g = Assert.Single(new ErgebnisCtrl().Load(PROJEKT).Gebaeude);
            ErgebnisZoneModel gaeste = g.Zonen.Single(z => z.Bezeichner == Zonenprojekt1052.ZONE_GAESTE);
            ErgebnisZoneModel gastro = g.Zonen.Single(z => z.Bezeichner == Zonenprojekt1052.ZONE_GASTRO);
            ErgebnisZoneModel keller = g.Zonen.Single(z => !z.IstBeheizt);
            Assert.True(gaeste.KuehlenergieMwh > 0.0, "die Gästezimmer kühlen nicht");
            Assert.Null(gastro.KuehlenergieMwh);   // Kuehlung_Aktiv = 0 an der Zone
            Assert.Null(keller.KuehlenergieMwh);   // unbeheizt schwingt frei
            Assert.Equal(gaeste.KuehlenergieMwh.Value, g.KuehlenergieMwh.Value, 6);
            // Schritt 185: Kältespitze und Kühlstunden der Zone stehen im gespeicherten Ergebnis, nur bei wirksamer Kühlung.
            Assert.True(gaeste.KaeltespitzeKw > 0.0, "die Kältespitze der Gästezimmer fehlt");
            Assert.True(gaeste.KuehlstundenH > 0 && gaeste.KuehlstundenH <= 8760);
            Assert.True(gaeste.KaeltespitzeKw.Value * gaeste.KuehlstundenH.Value >= gaeste.KuehlenergieMwh.Value * 1000.0 - 1e-6);
            Assert.Null(gastro.KaeltespitzeKw);
            Assert.Null(gastro.KuehlstundenH);
            Assert.Null(keller.KaeltespitzeKw);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "1052 gekühlt: Gästezimmer {0:F3} MWh/a, Gebäude {1:F3} MWh/a",
                                         gaeste.KuehlenergieMwh, g.KuehlenergieMwh));
        }
    }
}
