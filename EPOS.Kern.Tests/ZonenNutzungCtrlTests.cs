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
    /// <b>Das Blatt „Nutzung und Kalender“</b> (MA-2) gegen die Arbeitskopie der Testdatenbank, Projekt 1054 (Hotel, drei
    /// Zonen, die Keller-Zone unbeheizt): Liste je Zone (Herkunft, Kennungen), Profil für mehrere Zonen in einem Vorgang,
    /// Ferien und Heizperiode je Projekt mit Abweichung je Zone, Kalender kopieren, Sollwerte und Rücknahme. Die Referenzbasis
    /// bleibt unberührt — geschrieben wird nur in die Kopie.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenNutzungCtrlTests : IDisposable
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

        private static ZonenNutzungszeile Zone(ZonenNutzungsstand s, string name) => s.Zonen.Single(z => z.Zone == name);

        private static int Zahl(string sql, params object[] werte)
            => Convert.ToInt32(DataRepository.GetDataTable(sql, werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray()).Rows[0][0],
                               CultureInfo.InvariantCulture);

        private static long ProfilId(string name) => ZonenNutzungCtrl.Profile().First(p => p.Name == name).Id;

        [Fact]
        public void Liste_traegt_je_Zone_Herkunft_Kennungen_und_Sollwerte()
        {
            if (!_db.Vorhanden) return;
            ZonenNutzungsstand s = new ZonenNutzungCtrl(Gebaeude(1054)).Lesen();
            Assert.NotNull(s);
            Assert.Equal(3, s.Zonen.Count);
            ZonenNutzungszeile gast = Zone(s, "Gästezimmer");
            Assert.Equal(Profilherkunft.Profil, gast.Herkunft);
            Assert.Equal("WOHNEN", gast.Profil);
            Assert.Equal(Kalenderkennung.Profil, gast.Kalender[DbWerte.KOND_GROESSE_HEIZSOLL]);
            Assert.Equal(Kalenderkennung.Fehlt, gast.Kalender[DbWerte.KOND_GROESSE_LUEFTUNG]);
            Assert.Equal(20.0, gast.SollTag);
            Assert.Equal(18.0, gast.SollNacht);
            Assert.Equal(Ferienkennung.Fehlt, gast.Ferien);
            ZonenNutzungszeile keller = Zone(s, "Keller");
            Assert.False(keller.Beheizt);
            Assert.Equal(Kalenderkennung.Entfaellt, keller.Kalender[DbWerte.KOND_GROESSE_HEIZSOLL]);
            Assert.Equal(Kalenderkennung.Entfaellt, keller.Kalender[DbWerte.KOND_GROESSE_KUEHLSOLL]);
            Assert.Equal(5, gast.Kalender.Count);
            Assert.False(s.KannZuruecknehmen);
            Assert.NotEmpty(ZonenNutzungCtrl.Profile());
        }

        [Fact]
        public void Profil_fuer_mehrere_Zonen_in_einem_Vorgang_und_Ruecknahme()
        {
            if (!_db.Vorhanden) return;
            int g = Gebaeude(1054);
            var ctrl = new ZonenNutzungCtrl(g);
            ZonenNutzungsstand vorher = ctrl.Lesen();
            int[] ids = vorher.Zonen.Where(z => z.Beheizt).Select(z => z.IdZone).ToArray();
            int kalenderVorher = Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Gebaeude\" = ?", g);
            long buero = ProfilId("Büro");

            ZonenNutzungsergebnis e = ctrl.ProfilZuweisen(ids, buero);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(2, e.Zonen);
            Assert.True(e.Stand.KannZuruecknehmen);
            foreach (int id in ids)
            {
                ZonenNutzungszeile z = e.Stand.Zonen.Single(x => x.IdZone == id);
                Assert.Equal(buero, z.IdProfil);
                Assert.Equal(Profilherkunft.Profil, z.Herkunft);
                Assert.NotEmpty(z.Kategorie);
                Assert.Equal(Kalenderkennung.Profil, z.Kalender[DbWerte.KOND_GROESSE_HEIZSOLL]);
                Assert.Equal(Kalenderkennung.Profil, z.Kalender[DbWerte.KOND_GROESSE_LUEFTUNG]);
            }
            Assert.True(Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Gebaeude\" = ?", g) > kalenderVorher);

            ZonenNutzungsergebnis r = ctrl.Zuruecknehmen();
            Assert.True(r.Ok, r.Meldung);
            Assert.False(r.Stand.KannZuruecknehmen);
            Assert.Equal(kalenderVorher, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Gebaeude\" = ?", g));
            Assert.Equal("WOHNEN", Zone(r.Stand, "Gästezimmer").Profil);
            Assert.Equal(Kalenderkennung.Fehlt, Zone(r.Stand, "Gästezimmer").Kalender[DbWerte.KOND_GROESSE_LUEFTUNG]);
        }

        [Fact]
        public void Ferien_und_Heizperiode_je_Projekt_mit_Abweichung_je_Zone()
        {
            if (!_db.Vorhanden) return;
            int g = Gebaeude(1054);
            var ctrl = new ZonenNutzungCtrl(g);
            int gast = Zone(ctrl.Lesen(), "Gästezimmer").IdZone;
            // Projektvorgabe am Gebäude, eine Abweichung an der Zone.
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_Konditionierungsvorgabe\" (\"ID_Gebaeude\", \"Groesse\", \"Zeile\", \"Von\", \"Bis\") VALUES (?, 'HEIZSOLL', 'SAISON', 274, 120)",
                                           new DbParam("@g", g));
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_Konditionierungsvorgabe\" (\"ID_Gebaeude\", \"Groesse\", \"Zeile\", \"Wert\") VALUES (?, 'HEIZSOLL', 'FERIEN', 16)",
                                           new DbParam("@g", g));
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_Konditionierungsvorgabe\" (\"ID_Gebaeude\", \"ID_Zone\", \"Groesse\", \"Zeile\", \"Von\", \"Bis\") VALUES (?, ?, 'HEIZSOLL', 'SAISON', 260, 140)",
                                           new DbParam("@g", g), new DbParam("@z", gast));
            ZonenNutzungsstand s = ctrl.Lesen();
            Assert.True(s.ProjektFerien && s.ProjektHeizperiode);
            Assert.Equal(Ferienkennung.Abweichung, Zone(s, "Gästezimmer").Heizperiode);
            Assert.Equal(Ferienkennung.Projekt, Zone(s, "Gästezimmer").Ferien);
            Assert.Equal(Ferienkennung.Projekt, Zone(s, "Gastronomie und Verwaltung").Heizperiode);

            ZonenNutzungsergebnis e = ctrl.ProjektvorgabeUebernehmen(s.Zonen.Select(z => z.IdZone).ToList());
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(1, e.Zonen);
            Assert.All(e.Stand.Zonen, z => Assert.Equal(Ferienkennung.Projekt, z.Heizperiode));
            Assert.Equal(2, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL AND \"Zeile\" IN ('FERIEN','SAISON')", g));

            ZonenNutzungsergebnis r = ctrl.Zuruecknehmen();
            Assert.True(r.Ok, r.Meldung);
            Assert.Equal(Ferienkennung.Abweichung, Zone(r.Stand, "Gästezimmer").Heizperiode);
        }

        [Fact]
        public void Kalender_kopieren_ersetzt_die_Kalender_der_Zielzonen()
        {
            if (!_db.Vorhanden) return;
            int g = Gebaeude(1054);
            var ctrl = new ZonenNutzungCtrl(g);
            ZonenNutzungsstand s = ctrl.Lesen();
            int gast = Zone(s, "Gästezimmer").IdZone, gastro = Zone(s, "Gastronomie und Verwaltung").IdZone;
            Assert.True(ctrl.ProfilZuweisen(new[] { gast }, ProfilId("Büro")).Ok);

            ZonenNutzungsergebnis e = ctrl.KalenderKopieren(gast, new[] { gastro, gast });
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(1, e.Zonen);
            ZonenNutzungszeile ziel = e.Stand.Zonen.Single(z => z.IdZone == gastro);
            ZonenNutzungszeile quelle = e.Stand.Zonen.Single(z => z.IdZone == gast);
            Assert.Equal(quelle.Profil, ziel.Profil);
            Assert.Equal(quelle.Kalender.OrderBy(k => k.Key), ziel.Kalender.OrderBy(k => k.Key));
            Assert.Equal(Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Zone\" = ?", gast),
                         Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Zone\" = ?", gastro));

            ZonenNutzungsergebnis r = ctrl.Zuruecknehmen();
            Assert.True(r.Ok, r.Meldung);
            Assert.Equal("BUERO", r.Stand.Zonen.Single(z => z.IdZone == gastro).Profil);
        }

        [Fact]
        public void Sollwerte_fuer_mehrere_Zonen_mit_Bereich()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ZonenNutzungCtrl(Gebaeude(1054));
            List<int> ids = ctrl.Lesen().Zonen.Where(z => z.Beheizt).Select(z => z.IdZone).ToList();
            ZonenNutzungsergebnis e = ctrl.SollwerteSetzen(ids, 21.0, 17.0);
            Assert.True(e.Ok, e.Meldung);
            Assert.All(e.Stand.Zonen.Where(z => z.Beheizt), z => { Assert.Equal(21.0, z.SollTag); Assert.Equal(17.0, z.SollNacht); });
            Assert.False(ctrl.SollwerteSetzen(ids, 18.0, 19.0).Ok);
            Assert.False(ctrl.SollwerteSetzen(ids, 40.0, null).Ok);
            Assert.False(ctrl.ProfilZuweisen(new int[0], 1).Ok);
            Assert.True(ctrl.Zuruecknehmen().Ok);
            Assert.Equal(16.0, ctrl.Lesen().Zonen.Single(z => z.Zone == "Gastronomie und Verwaltung").SollNacht);
        }

        [Fact]
        public void Huelle_baut_den_Weg_und_ohne_Gebaeude_keinen()
        {
            Assert.Null(ZonenNutzungHuelle.Weg(0, "x"));
            if (!_db.Vorhanden) return;
            ZonenNutzungWeg weg = ZonenNutzungHuelle.Weg(Gebaeude(1054), "Hotel");
            Assert.Equal("Hotel", weg.Gebaeudename);
            Assert.Equal(3, weg.Lesen().Zonen.Count);
            Assert.NotEmpty(weg.Profile());
            Assert.False(weg.Zuruecknehmen().Ok);
        }
    }
}
