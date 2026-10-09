using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kalenderbedienung über dem gelesenen Referenzprojekt 1051</b> (Welle K1a, Einfrierregel 10.3): Die Schicht
    /// liest den Arbeitsstand des Referenzgebäudes (Kalender mit Ferien, Heizperiode und Nachtzeile der Lüftung), zeigt
    /// Zuordnungen und Jahresraster und ändert ihn im Arbeitsstand — die Datenbank bleibt dabei unberührt, die gesäten
    /// Konditionierungszeilen zählen danach wie davor.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KalenderbedienungDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        [Fact]
        public void Die_Schicht_liest_1051_und_aendert_nur_den_Arbeitsstand()
        {
            if (!_db.Vorhanden || !KonditionierungSchema.Lesbar()) return;
            object id = DataRepository.ExecuteScalar(
                "SELECT k.ID_Gebaeude FROM Tab_Konditionierungskalender k JOIN Tab_Gebaeude g ON g.ID = k.ID_Gebaeude " +
                "WHERE g.ID_Projekt = ? ORDER BY k.ID_Gebaeude LIMIT 1",
                new DbParam("@p", Konditionierungsprojekt1051.NEU));
            Assert.NotNull(id);
            long gebaeude = Convert.ToInt64(id, CultureInfo.InvariantCulture);
            long perioden = Zaehlen("Tab_Konditionierungsperiode");
            long kalender = Zaehlen("Tab_Konditionierungskalender");

            Konditionierungsarbeitsstand a = new KonditionierungCtrl().ArbeitsstandLesen(gebaeude, null, out string meldung);
            Assert.True(a != null, meldung);
            Konditionierungsgroesse g = Konditionierungsgroessen.Alle.First(x => a.Gebaeude.Kalender(x) != null);
            var ort = new Konditionierungsort(g);

            IReadOnlyList<Rastertag> raster = Kalenderbedienung.Jahresraster(a, ort);
            Assert.Equal(365, raster.Count);
            Assert.Equal(365, Kalenderbedienung.Jahresband(raster).Sum(x => x.Ende - x.Beginn + 1));
            Assert.NotEmpty(Kalenderbedienung.Wochenprofile(a, ort));
            int vorher = Kalenderbedienung.Zuordnungen(a, null).Count;

            // Ein Tag der Standardwoche (weder Saison noch Ferien noch Feiertag) wird Einzeltag.
            int tag = raster.First(t => t.Art == Rastertagart.Grundwoche).Tag;
            Konditionierungsschritt s = Kalenderbedienung.EinzeltagSetzen(a, null, null, tag, "Brückentag", aus: true);
            Assert.True(s.Ok, s.Meldung);
            Assert.Equal(vorher + 1, Kalenderbedienung.Zuordnungen(s.Stand, null).Count);
            Assert.Equal(Rastertagart.Einzeltag, Kalenderbedienung.Jahresraster(s.Stand, ort)[tag - 1].Art);

            Assert.Equal(perioden, Zaehlen("Tab_Konditionierungsperiode"));
            Assert.Equal(kalender, Zaehlen("Tab_Konditionierungskalender"));
        }

        private static long Zaehlen(string tabelle)
            => Convert.ToInt64(DataRepository.ExecuteScalar(
                   tabelle == "Tab_Konditionierungsperiode"
                       ? "SELECT COUNT(*) FROM Tab_Konditionierungsperiode"
                       : "SELECT COUNT(*) FROM Tab_Konditionierungskalender"), CultureInfo.InvariantCulture);
    }
}
