using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle des Blatts „Modell prüfen“</b> (MA-1b) gegen die Arbeitskopie der Testdatenbank: Stand mit fünf Gruppen und
    /// benanntem Ziel je Befund, „Alle Vorschläge übernehmen“ mit neuem Stand und Rücknahme je Gruppe, Quellhinweise.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ModellpruefungHuelleTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static int Gebaeude(int projekt)
            => Convert.ToInt32(DataRepository.GetDataTable(
                "SELECT g.\"ID\" FROM \"Tab_Gebaeude\" g INNER JOIN \"Z_ProjektGebaeude\" z ON z.\"ID\" = g.\"ID_ProjektGebaeude\" " +
                "WHERE z.\"ID_Projekt\" = ? ORDER BY g.\"ID\"", new DbParam("@p", projekt)).Rows[0][0], CultureInfo.InvariantCulture);

        [Fact]
        public void Stand_traegt_fuenf_Gruppen_Status_und_benannte_Ziele()
        {
            if (!_db.Vorhanden) return;
            ModellpruefungWeg weg = ModellpruefungHuelle.Weg(Gebaeude(1054), "Hotel");
            Assert.Equal("Hotel", weg.Gebaeudename);
            ModellpruefungStand stand = weg.Pruefen();
            Assert.NotNull(stand);
            Assert.Equal(5, stand.Gruppen.Count);
            Assert.Equal(Rechenstatus.RechnetMitVorgaben, stand.Status);
            Assert.Equal(ModellpruefungCtrl.Statusname(Rechenstatus.RechnetMitVorgaben), stand.Statustext);
            Assert.Equal(Enumerable.Range(1, 5), stand.Gruppen.Select(g => (int)g.Gruppe));
            ModellpruefungGruppenzeile aufbau = stand.Gruppen.Single(g => g.Gruppe == Pruefgruppe.AufbautenStoffwerte);
            Assert.Equal(ModellpruefungCtrl.Gruppenname(Pruefgruppe.AufbautenStoffwerte), aufbau.Name);
            Assert.Equal(Pruefsprungziel.Aufbau, aufbau.Sprungziel);
            Assert.True(aufbau.Vorschlaege > 0);
            Assert.Equal(aufbau.Anzahl, aufbau.Befunde.Count);
            Assert.All(aufbau.Befunde, b => Assert.Contains(" — ", b.Ziel));      // Zone — Bauteil benannt
            Assert.All(aufbau.Befunde.Where(b => b.Vorschlag.Length > 0), b => Assert.NotEmpty(b.Text));
            Assert.False(aufbau.KannZuruecknehmen);
        }

        [Fact]
        public void Alle_uebernehmen_zeigt_neuen_Stand_und_Gruppe_ist_zuruecknehmbar()
        {
            if (!_db.Vorhanden) return;
            ModellpruefungWeg weg = ModellpruefungHuelle.Weg(Gebaeude(1054), "Hotel");
            ModellpruefungAusgang a = weg.AlleUebernehmen();
            Assert.True(a.Ok, a.Meldung);
            Assert.NotEmpty(a.Meldung);
            Assert.NotNull(a.Stand);
            ModellpruefungGruppenzeile aufbau = a.Stand.Gruppen.Single(g => g.Gruppe == Pruefgruppe.AufbautenStoffwerte);
            Assert.Equal(0, aufbau.Anzahl);
            Assert.True(aufbau.KannZuruecknehmen);

            ModellpruefungAusgang r = weg.Zuruecknehmen(Pruefgruppe.AufbautenStoffwerte);
            Assert.True(r.Ok, r.Meldung);
            ModellpruefungGruppenzeile zurueck = r.Stand.Gruppen.Single(g => g.Gruppe == Pruefgruppe.AufbautenStoffwerte);
            Assert.True(zurueck.Vorschlaege > 0);
            Assert.False(zurueck.KannZuruecknehmen);
        }

        [Fact]
        public void Ohne_Projektgebaeude_kein_Weg()
        {
            Assert.Null(ModellpruefungHuelle.Weg(0, "x"));
            Assert.Null(ModellpruefungHuelle.Weg(new GebaeudeProjektZeile { Name = "neu" }, 0));
        }

        [Fact]
        public void Quellhinweise_aus_dem_Plan_und_einmal_verbraucht()
        {
            Assert.Empty(ModellpruefungHuelle.HinweiseAus(null));
            if (!_db.Vorhanden) return;
            int id = Gebaeude(1054);
            var zeile = new GebaeudeProjektZeile { Name = "Hotel" };
            ModellpruefungHuelle.HinweiseMerken(zeile, new[] { new Zonenquellhinweis("Unbekannte Zone", 1, "", "") });
            // Die Zeile gibt ihre Hinweise an das Gebäude weiter; das Öffnen verbraucht sie (die Zone ist unbekannt → still).
            ModellpruefungWeg weg = ModellpruefungHuelle.Weg(zeile, id);
            Assert.NotNull(weg);
            Assert.NotNull(weg.Pruefen());
        }

        [Fact]
        public void Nordrichtung_bestaetigen_nennt_den_Grund_ohne_Importquelle()
        {
            if (!_db.Vorhanden) return;
            ModellpruefungWeg weg = ModellpruefungHuelle.Weg(Gebaeude(1007), "Bürobau");
            Assert.NotNull(weg.NordrichtungBestaetigen);
            ModellpruefungAusgang a = weg.NordrichtungBestaetigen();
            Assert.False(a.Ok);
            Assert.NotEmpty(a.Meldung);
            Assert.NotNull(a.Stand);
        }
    }
}
