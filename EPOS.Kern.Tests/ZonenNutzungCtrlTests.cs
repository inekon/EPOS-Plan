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

        /// <summary>
        /// E139: Die Ferienwerte, die der Profilgenerator an der Zone schreibt, und eigene Ferienzeiträume der Zonenkalender
        /// gelten als Abweichung (Herkunft Profilgenerator); eine Zone ohne eigene Ferien steht auf „Projekt“ (Gegenprobe).
        /// </summary>
        [Fact]
        public void Generatorferien_gelten_als_Abweichung_mit_Herkunft()
        {
            if (!_db.Vorhanden) return;
            int g = Gebaeude(1054);
            var ctrl = new ZonenNutzungCtrl(g);
            ZonenNutzungsstand s0 = ctrl.Lesen();
            int gast = Zone(s0, "Gästezimmer").IdZone;
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_Konditionierungsvorgabe\" (\"ID_Gebaeude\", \"Groesse\", \"Zeile\", \"Wert\") VALUES (?, 'HEIZSOLL', 'FERIEN', 16)",
                                           new DbParam("@g", g));
            Assert.Equal(Ferienkennung.Projekt, Zone(ctrl.Lesen(), "Gästezimmer").Ferien);
            Assert.True(ctrl.ProfilZuweisen(new[] { gast }, ProfilId("Büro")).Ok);
            Assert.True(Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Zone\" = ? AND \"Zeile\" = 'FERIEN'", gast) > 0);

            ZonenNutzungsstand s = ctrl.Lesen();
            ZonenNutzungszeile z = Zone(s, "Gästezimmer");
            Assert.Equal(Ferienkennung.Abweichung, z.Ferien);
            Assert.True(z.FerienAngaben.Abweichend);
            Assert.Equal(Ferienherkunft.Profilgenerator, z.FerienAngaben.Herkunft);
            Assert.True(z.FerienAngaben.Werte > 0);
            ZonenNutzungszeile gastro = Zone(s, "Gastronomie und Verwaltung");
            Assert.Equal(Ferienkennung.Projekt, gastro.Ferien);
            Assert.False(gastro.FerienAngaben.Abweichend);
            Assert.Null(ctrl.FerienRueckfrage(new[] { gastro.IdZone }));
            string frage = ctrl.FerienRueckfrage(new[] { gast, gastro.IdZone });
            Assert.NotNull(frage);
            Assert.Contains("Gästezimmer", frage);
            Assert.DoesNotContain("Gastronomie", frage);
        }

        /// <summary>
        /// E139: „Projektvorgabe übernehmen“ ersetzt eigene Ferien nur mit Zustimmung — ohne sie bleibt die Zone unverändert und
        /// die übrigen übernehmen; mit ihr fallen Ferienwerte und abweichende Ferienzeiträume; die Rücknahme stellt sie wieder her.
        /// </summary>
        [Fact]
        public void Projektvorgabe_ersetzt_eigene_Ferien_nur_mit_Zustimmung()
        {
            if (!_db.Vorhanden) return;
            int g = Gebaeude(1054);
            var ctrl = new ZonenNutzungCtrl(g);
            ZonenNutzungsstand s0 = ctrl.Lesen();
            int gast = Zone(s0, "Gästezimmer").IdZone, gastro = Zone(s0, "Gastronomie und Verwaltung").IdZone;
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_Konditionierungsvorgabe\" (\"ID_Gebaeude\", \"Groesse\", \"Zeile\", \"Wert\") VALUES (?, 'HEIZSOLL', 'FERIEN', 16)",
                                           new DbParam("@g", g));
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_Konditionierungsvorgabe\" (\"ID_Gebaeude\", \"Groesse\", \"Zeile\", \"Von\", \"Bis\") VALUES (?, 'HEIZSOLL', 'SAISON', 274, 120)",
                                           new DbParam("@g", g));
            Assert.True(ctrl.ProfilZuweisen(new[] { gast }, ProfilId("Büro")).Ok);
            // Ein eigener Ferienzeitraum im Heizkalender der Zone, den das Projekt nicht kennt.
            int kalender = Zahl("SELECT \"ID\" FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Zone\" = ? AND \"Groesse\" = 'HEIZSOLL'", gast);
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_Konditionierungsperiode\" (\"ID_Kalender\", \"Rang\", \"Art\", \"Bezeichner\", \"Beginn\", \"Ende\", \"Aus\") " +
                                           "VALUES (?, 290, 'FERIEN', 'Betriebsferien', 100, 110, 1)", new DbParam("@k", kalender));
            DataRepository.ExecuteNonQuery("INSERT INTO \"Tab_Konditionierungsvorgabe\" (\"ID_Gebaeude\", \"ID_Zone\", \"Groesse\", \"Zeile\", \"Von\", \"Bis\") VALUES (?, ?, 'HEIZSOLL', 'SAISON', 260, 140)",
                                           new DbParam("@g", g), new DbParam("@z", gastro));
            const string WERTE = "SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Zone\" = ? AND \"Zeile\" = 'FERIEN'";
            const string EIGENER = "SELECT COUNT(*) FROM \"Tab_Konditionierungsperiode\" WHERE \"ID_Kalender\" = ? AND \"Rang\" = 290";
            int werte = Zahl(WERTE, gast);
            ZonenNutzungszeile vorher = Zone(ctrl.Lesen(), "Gästezimmer");
            Assert.True(vorher.FerienAngaben.ZeitraeumeAbweichend);
            Assert.True(vorher.FerienAngaben.Zeitraeume >= 1);

            // Ohne Zustimmung: Gästezimmer bleibt, Gastronomie übernimmt.
            ZonenNutzungsergebnis ohne = ctrl.ProjektvorgabeUebernehmen(new[] { gast, gastro }, false);
            Assert.True(ohne.Ok, ohne.Meldung);
            Assert.Equal(1, ohne.Zonen);
            Assert.Equal(Ferienkennung.Abweichung, Zone(ohne.Stand, "Gästezimmer").Ferien);
            Assert.Equal(werte, Zahl(WERTE, gast));
            Assert.Equal(1, Zahl(EIGENER, kalender));
            Assert.Equal(Ferienkennung.Projekt, Zone(ohne.Stand, "Gastronomie und Verwaltung").Heizperiode);
            Assert.True(ctrl.Zuruecknehmen().Ok);
            Assert.Equal(Ferienkennung.Abweichung, Zone(ctrl.Lesen(), "Gastronomie und Verwaltung").Heizperiode);

            // Mit Zustimmung: Ferienwerte und der eigene Zeitraum fallen, die Marke steht auf „Projekt“.
            ZonenNutzungsergebnis mit = ctrl.ProjektvorgabeUebernehmen(new[] { gast, gastro }, true);
            Assert.True(mit.Ok, mit.Meldung);
            Assert.Equal(2, mit.Zonen);
            Assert.Equal(Ferienkennung.Projekt, Zone(mit.Stand, "Gästezimmer").Ferien);
            Assert.Equal(0, Zahl(WERTE, gast));
            Assert.Equal(0, Zahl(EIGENER, kalender));
            Assert.Null(ctrl.FerienRueckfrage(new[] { gast }));

            // Rücknahme über das Abbild.
            ZonenNutzungsergebnis r = ctrl.Zuruecknehmen();
            Assert.True(r.Ok, r.Meldung);
            Assert.Equal(Ferienkennung.Abweichung, Zone(r.Stand, "Gästezimmer").Ferien);
            Assert.Equal(werte, Zahl(WERTE, gast));
            Assert.Equal(1, Zahl(EIGENER, kalender));
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
        public void Sollwerte_setzen_traegt_die_Werte_in_den_Heizkalender_der_Zone_mit_Ruecknahme()
        {
            // N1: Das Gästezimmer (1054) trägt einen Heizkalender des Profils „WOHNEN" (Woche 18/20 °C); er gilt vor den Spalten.
            if (!_db.Vorhanden) return;
            var ctrl = new ZonenNutzungCtrl(Gebaeude(1054));
            ZonenNutzungszeile gast = Zone(ctrl.Lesen(), "Gästezimmer");
            Assert.Equal(Kalenderkennung.Profil, gast.Kalender[DbWerte.KOND_GROESSE_HEIZSOLL]);
            double[] vorher = gast.Wochen[DbWerte.KOND_GROESSE_HEIZSOLL];
            Assert.Equal(168, vorher.Length);
            Assert.Contains(20.0, vorher);
            Assert.Contains(18.0, vorher);

            ZonenNutzungsergebnis e = ctrl.SollwerteSetzen(new[] { gast.IdZone }, 21.5, 16.5);
            Assert.True(e.Ok, e.Meldung);
            double[] nachher = Zone(e.Stand, "Gästezimmer").Wochen[DbWerte.KOND_GROESSE_HEIZSOLL];
            for (int i = 0; i < 168; i++)
                Assert.Equal(vorher[i] == 20.0 ? 21.5 : vorher[i] == 18.0 ? 16.5 : vorher[i], nachher[i]);
            // Die Nachtzeile der Zone trägt nur Zeiten — sie bleibt, wie sie ist.
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Zone\" = ? AND \"Zeile\" = 'NACHT' " +
                                 "AND \"Wert\" IS NULL AND \"Von\" = 22 AND \"Bis\" = 6", gast.IdZone));

            Assert.True(ctrl.Zuruecknehmen().Ok);
            Assert.Equal(vorher, Zone(ctrl.Lesen(), "Gästezimmer").Wochen[DbWerte.KOND_GROESSE_HEIZSOLL]);
        }

        [Fact]
        public void Kurzbild_der_Woche_je_Groesse_eigen_und_ohne_Kalender()
        {
            if (!_db.Vorhanden) return;
            ZonenNutzungsstand s = new ZonenNutzungCtrl(Gebaeude(1054)).Lesen();
            ZonenNutzungszeile gastro = Zone(s, "Gastronomie und Verwaltung");
            Assert.True(gastro.Wochen.ContainsKey(DbWerte.KOND_GROESSE_HEIZSOLL));
            Assert.False(gastro.Wochen.ContainsKey(DbWerte.KOND_GROESSE_LUEFTUNG));
            Assert.Empty(Zone(s, "Keller").Wochen.Where(w => w.Key == DbWerte.KOND_GROESSE_HEIZSOLL));
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
