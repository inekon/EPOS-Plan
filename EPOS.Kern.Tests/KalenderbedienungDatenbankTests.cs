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

        [Fact]
        public void Stufe_2_schreibt_eine_Gemeinschaftsperiode_Wochen_Ferienliste_Wochenende_und_Land_und_liest_sie_zurueck()
        {
            if (!_db.Vorhanden || !KonditionierungSchema.Lesbar() || !Kalendergemeinschaft.SchrittSteht()) return;
            long gebaeude = Gebaeude1051();
            var ctrl = new KonditionierungCtrl();
            Konditionierungsarbeitsstand a = ctrl.ArbeitsstandLesen(gebaeude, null, out string meldung);
            Assert.True(a != null, meldung);
            Konditionierungsgroesse g = Konditionierungsgroessen.Alle.First(x => a.Gebaeude.Kalender(x) != null && x == Konditionierungsgroesse.Heizsoll);
            var ort = new Konditionierungsort(g);

            a = Gut(Kalenderbedienung.ZuordnungSetzen(a, null, null, Zuordnungsschluessel.Zeitraum("Betriebsruhe", 120, 125),
                                                      Zuordnungsangabe.Abgeschaltet));
            a = Gut(Kalenderbedienung.WocheAnlegen(a, ort, "Schichtwoche"));
            a = Gut(Kalenderbedienung.ZuordnungSetzen(a, null, null, Zuordnungsschluessel.Zeitraum("Schicht", 130, 140),
                                                      Zuordnungsangabe.Profilwoche("Schichtwoche"), new[] { g }));
            a = Gut(Kalenderbedienung.FerienlisteSetzen(a, new[]
            {
                new Ferienzeile("A", 10, 12), new Ferienzeile("B", 60, 62), new Ferienzeile("C", 150, 152),
                new Ferienzeile("D", 200, 210), new Ferienzeile("Herbst", 290, 295),
            }));
            a = Gut(Kalenderbedienung.WochenendeSetzen(a, new[] { 4, 5 }));
            a = Gut(Kalenderbedienung.FeiertagslandSetzen(a, "BY"));
            Schreiben(ctrl, gebaeude, a.Gebaeude, out _);

            Konditionierungsarbeitsstand b = ctrl.ArbeitsstandLesen(gebaeude, null, out meldung);
            Assert.True(b != null, meldung);
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k ON k.ID = p.ID_Kalender " +
                                 "WHERE k.ID_Gebaeude = ? AND p.Bezeichner = 'Betriebsruhe'", gebaeude));            // EINE Zeile, keine Kopien
            Assert.Equal(KalenderbedienungSchema.MASKE_ALLE, b.Gebaeude.Gemeinsam.Single(p => p.Regel.Bezeichner == "Betriebsruhe").Maske);
            BenannteWoche w = Assert.Single(b.Gebaeude.Wochen);
            Assert.True(w.Id > 0);
            Assert.Equal(w.Id, b.Gebaeude.Gemeinsam.Single(p => p.Regel.Bezeichner == "Schicht").Regel.Angabe.IdWoche);
            Assert.Equal(3, b.Gebaeude.Gemeinsam.Count(Kalenderbedienung.IstLandesregel));
            Assert.Equal("Herbst", Assert.Single(b.Gebaeude.Ferienliste).Name);
            Assert.Equal(48, b.Gebaeude.Bestand.Wochenendtage);
            Assert.Equal("BY", b.Gebaeude.Bestand.Feiertagsland);
            Assert.Equal(Kalenderbedienung.Zuordnungen(a, null).Count, Kalenderbedienung.Zuordnungen(b, null).Count);
            Assert.True(Kalendervergleich.KalenderGleich(a.Gebaeude.Kalender(g), b.Gebaeude.Kalender(g)) ||
                        a.Gebaeude.Kalender(g).Perioden.Count == b.Gebaeude.Kalender(g).Perioden.Count);

            // Ein zweites OK schreibt nichts; eine verwiesene Woche fällt erst mit ihrer Zeile.
            Schreiben(ctrl, gebaeude, b.Gebaeude, out bool zweimal);
            Assert.False(zweimal);
            b = Gut(Kalenderbedienung.ZuordnungLoeschen(b, null, Zuordnungsschluessel.Zeitraum("Schicht", 130, 140)));
            b = Gut(Kalenderbedienung.WocheLoeschen(b, ort, w.Id));
            Schreiben(ctrl, gebaeude, b.Gebaeude, out _);
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungswoche WHERE ID_Gebaeude = ?", gebaeude));
        }

        private static long Gebaeude1051()
            => Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT k.ID_Gebaeude FROM Tab_Konditionierungskalender k JOIN Tab_Gebaeude g ON g.ID = k.ID_Gebaeude " +
                "WHERE g.ID_Projekt = ? ORDER BY k.ID_Gebaeude LIMIT 1", new DbParam("@p", Konditionierungsprojekt1051.NEU)),
                CultureInfo.InvariantCulture);

        private static void Schreiben(KonditionierungCtrl ctrl, long gebaeude, Konditionierungsstand stand, out bool geschrieben)
        {
            using (DbVorgang v = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
            {
                KonditionierungCtrl.Ergebnis e = ctrl.StandSchreiben(v, KonditionierungCtrl.Eigner.Gebaeude(gebaeude), stand, true, out geschrieben);
                Assert.True(e.Ok, e.Meldung);
                v.Commit();
            }
        }

        private static long Zahl(string sql, long id)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, new DbParam("@id", id)), CultureInfo.InvariantCulture);

        private static Konditionierungsarbeitsstand Gut(Konditionierungsschritt s)
        {
            Assert.True(s.Ok, s.Meldung);
            return s.Stand;
        }

        private static long Zaehlen(string tabelle)
            => Convert.ToInt64(DataRepository.ExecuteScalar(
                   tabelle == "Tab_Konditionierungsperiode"
                       ? "SELECT COUNT(*) FROM Tab_Konditionierungsperiode"
                       : "SELECT COUNT(*) FROM Tab_Konditionierungskalender"), CultureInfo.InvariantCulture);
    }
}
