using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ein Wochentagsraster für alle Leser</b> (Entscheid E115): Gebäudelauf (Wochenendmaske,
    /// Konditionierungskalender), Zapfkalender und Bedarfsprofile rechnen mit dem Raster der Klimaregion des
    /// Projekts (<see cref="Konditionierungdatenweg.Raster(int)"/>); trägt das Projekt eine Preisreihe mit Jahr,
    /// gilt für alle Leser der Kalender dieses Jahres — Raster des echten 1. Januar und echte Feiertagsdaten. An
    /// der Testdatenbank liegt der 1. Januar im Raster der Klimaregion auf einem Donnerstag; der Sonderfall wird
    /// mit 2027 geprobt (1. Januar = Freitag), weil 2026 wie das Raster der Klimaregion beginnt.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class EinRasterFuerAlleLeserTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public EinRasterFuerAlleLeserTests(TestDatenbank db) => _db = db;

        /// <summary>Donnerstag (0 = Montag) — der 1. Januar im Raster der Klimaregion der Testdatenbank.</summary>
        private const int DONNERSTAG = 3;

        /// <summary>Freitag (0 = Montag) — der 1. Januar 2027.</summary>
        private const int FREITAG = 4;

        /// <summary>Die aktive Stromspeicher-Variante von 1051 und eine Preisreihe der Testdatenbank (Jahr 2026).</summary>
        private const int VARIANTE_1051 = 19, PREISREIHE = 3;

        /// <summary>Das Raster, mit dem der Gebäudelauf eines Projekts rechnet (Klimakalender des Laufs).</summary>
        private static Gemeinjahrkalender LaufRaster(int idProjekt, out bool[] wochenende)
            => LaufRaster(idProjekt, out wochenende, out _);

        private static Gemeinjahrkalender LaufRaster(int idProjekt, out bool[] wochenende, out SimulationWaermebedarf sim)
        {
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
            wochenende = sim.Kalender.Gemeinsam.WochenendeOrtszeit;
            Assert.Equal(sim.WochentagJan1, sim.Kalender.Gemeinsam.Raster.W0);
            return sim.Kalender.Gemeinsam.Raster;
        }

        /// <summary>Das Raster, mit dem der Zapfkalender eines Projekts seine Feiertage legt.</summary>
        private static Gemeinjahrkalender ZapfRaster(int idProjekt) => ZapfRaster(idProjekt, out _, out _);

        private static Gemeinjahrkalender ZapfRaster(int idProjekt, out int w0, out bool[] we)
        {
            Assert.True(ZapfprofilCtrl.KalenderLesen(idProjekt, out w0, out we));
            return Konditionierungdatenweg.Raster(idProjekt, w0);
        }

        /// <summary>Die Stromverbraucher eines Projekts über die Profilroutine, mit einem w₀ des Aufrufers.</summary>
        private static double[] Stromprofil(int idProjekt, int w0, SimulationWaermebedarf sim)
        {
            var reihe = new double[8760];
            ProfilBedarf.Rechnen(ProfilQuelle.Strom(ProfilQuellmodus.Projektrechnung), idProjekt, null, w0,
                                 sim.mo_anfang, sim.mo_ende, reihe);
            return reihe;
        }

        private static int Klimaregion(int idProjekt)
        {
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            return projekt.m_ID_Klimaregion;
        }

        [Theory]
        [InlineData(1045)]
        [InlineData(1051)]
        public void Gebaeudelauf_und_Zapfkalender_rechnen_im_Raster_der_Klimaregion(int idProjekt)
        {
            if (!_db.Vorhanden) return;
            int w0 = ProfilBedarf.WochentagJan1AusKlimaregion(Klimaregion(idProjekt));
            Assert.Equal(DONNERSTAG, w0);

            Gemeinjahrkalender lauf = LaufRaster(idProjekt, out bool[] wochenende);
            Assert.Equal(w0, lauf.W0);
            Assert.False(lauf.MitJahr);
            Assert.Equal(lauf, ZapfRaster(idProjekt));
            Assert.Equal(lauf, Konditionierungdatenweg.Raster(idProjekt));

            // Die Wochenendmaske des Gebäudelaufs liegt im selben Raster: 1. Januar Donnerstag, 3./4. Januar Wochenende.
            Assert.Equal(KlimakalenderGemeinsam.WochenendmaskeBilden(lauf), wochenende);
            Assert.False(wochenende[0]);
            Assert.True(wochenende[2] && wochenende[3]);
            Assert.Equal(DONNERSTAG, GebaeudeModellEingang.WochentagDesErstenTags(wochenende));
        }

        [Fact]
        public void Mit_Preisreihenjahr_gilt_fuer_alle_Leser_der_Kalender_des_Jahres()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1051;
            // Regelfall: Gebäudelauf und Profilroutine im Raster der Klimaregion — ein anderes w₀ verschiebt die Kachelung.
            LaufRaster(PROJEKT, out _, out SimulationWaermebedarf regel);
            Assert.NotEqual(Stromprofil(PROJEKT, DONNERSTAG, regel), Stromprofil(PROJEKT, FREITAG, regel));

            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Preisreihe SET Jahr = ? WHERE ID = ?",
                                                  new DbParam("@j", 2027), new DbParam("@p", PREISREIHE)));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_StromspeicherVariante SET ID_Preisreihe = ? WHERE ID = ?",
                                                  new DbParam("@p", PREISREIHE), new DbParam("@v", VARIANTE_1051)));
            try
            {
                Assert.Equal(2027, SolardatenCtrl.Preisreihenjahr(PROJEKT));

                // Gebäudelauf: der Kalender 2027 — w₀ Freitag, nicht das Donnerstag-Raster der Klimaregion.
                Gemeinjahrkalender lauf = LaufRaster(PROJEKT, out bool[] wochenende, out SimulationWaermebedarf sim);
                Assert.Equal(Gemeinjahrkalender.Kalenderjahr(2027), lauf);
                Assert.Equal(FREITAG, lauf.W0);
                Assert.Equal(FREITAG, sim.WochentagJan1);
                Assert.Equal(lauf, Konditionierungdatenweg.Raster(PROJEKT));

                // Zapfkalender: dasselbe w₀ und die Wochenenden des Jahres.
                Assert.Equal(lauf, ZapfRaster(PROJEKT, out int w0Zapf, out bool[] weZapf));
                Assert.Equal(FREITAG, w0Zapf);
                Assert.Equal(KlimakalenderGemeinsam.WochenendmaskeBilden(2027), weZapf);

                // Die Wochenendmaske des Laufs folgt den echten Wochenenden 2027: 2./3. Januar, nicht 3./4. Januar.
                Assert.Equal(KlimakalenderGemeinsam.WochenendmaskeBilden(2027), wochenende);
                Assert.True(wochenende[1] && wochenende[2]);
                Assert.False(wochenende[3]);
                Assert.Equal(FREITAG, GebaeudeModellEingang.WochentagDesErstenTags(wochenende));

                // Ostersonntag (28.03.2027 = Jahrestag 87) liegt auf einem Sonntag des Rasters, Buß- und Bettag
                // (17.11.2027) auf einem Mittwoch.
                Assert.Equal(87, lauf.Ostersonntag);
                Assert.Equal(6, lauf.Wochentag(lauf.Ostersonntag));
                Assert.Equal(Feiertage.Gemeinjahrestag(11, 17), lauf.BussUndBettag);
                Assert.Equal(2, lauf.Wochentag(lauf.BussUndBettag));

                // ProfilBedarf: Ein Aufrufer mit dem w₀ der Klimaregion rechnet dieselbe Reihe wie mit dem w₀ des Jahres.
                Assert.Equal(Stromprofil(PROJEKT, FREITAG, sim), Stromprofil(PROJEKT, DONNERSTAG, sim));
            }
            finally
            {
                Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_StromspeicherVariante SET ID_Preisreihe = NULL WHERE ID = ?",
                                                      new DbParam("@v", VARIANTE_1051)));
                Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Preisreihe SET Jahr = ? WHERE ID = ?",
                                                      new DbParam("@j", 2026), new DbParam("@p", PREISREIHE)));
            }
            Assert.Null(SolardatenCtrl.Preisreihenjahr(PROJEKT));
        }

        [Fact]
        public void Ein_Jahr_traegt_sein_eigenes_Raster()
        {
            // 2025 begann an einem Mittwoch: Der Kalender des Jahres trägt dieses Raster, ein fremdes ist nicht baubar.
            Gemeinjahrkalender r = Gemeinjahrkalender.Kalenderjahr(2025);
            Assert.Equal(2, r.W0);
            Assert.Equal(Feiertage.Gemeinjahrestag(4, 20), r.Ostersonntag);
            Assert.Equal(6, r.Wochentag(r.Ostersonntag));
            Assert.Throws<ArgumentException>(() => Gemeinjahrkalender.Aus(DONNERSTAG, 2025));
            Assert.Equal(r, Gemeinjahrkalender.Aus(2, 2025));
        }

        [Fact]
        public void Ohne_Projekt_gilt_das_Rueckfallraster_wie_im_Zapfkalender()
        {
            Gemeinjahrkalender r = Konditionierungdatenweg.Raster(0);
            Assert.Equal(ProfilBedarf.WOCHENTAG_ALTKONVENTION, r.W0);
            Assert.False(r.MitJahr);
            Assert.Equal(ProfilBedarf.WOCHENTAG_ALTKONVENTION, ProfilBedarf.WochentagJan1AusKlimaregion(0));
            Assert.Equal(r, new Konditionierungsarbeitsstand(null, null).Kalender);
        }

        /// <summary>Das Gebäude von 1051 (<c>Tab_Gebaeude.ID</c>).</summary>
        private const long GEBAEUDE_1051 = 10657;

        /// <summary>
        /// Der Arbeitsstand ohne Raster des Aufrufers (Raumnutzung, Zonenplan, Gebäudestamm): Das Projektgebäude rechnet im
        /// Raster seines Projekts (<c>Tab_Gebaeude.ID_Projekt</c>).
        /// </summary>
        [Fact]
        public void Der_Arbeitsstand_eines_Projektgebaeudes_rechnet_im_Raster_seines_Projekts()
        {
            if (!_db.Vorhanden) return;
            Konditionierungsarbeitsstand a = new KonditionierungCtrl().ArbeitsstandLesen(GEBAEUDE_1051, null, out string m);
            Assert.True(a != null, m);
            Assert.Equal(Konditionierungdatenweg.Raster(1051), a.Kalender);
            Assert.Equal(DONNERSTAG, a.Kalender.W0);
            Assert.NotEqual(Konditionierungdatenweg.Rueckfallraster, a.Kalender);
            // Ein Raster des Aufrufers geht vor.
            Assert.Equal(Gemeinjahrkalender.Kalenderjahr(2027),
                         new KonditionierungCtrl().ArbeitsstandLesen(GEBAEUDE_1051, Gemeinjahrkalender.Kalenderjahr(2027), out _).Kalender);
        }

        /// <summary>Ein Gebäude ohne Projekt (Katalogbau) rechnet im Rückfallraster.</summary>
        [Fact]
        public void Der_Arbeitsstand_eines_Gebaeudes_ohne_Projekt_rechnet_im_Rueckfallraster()
        {
            if (!_db.Vorhanden) return;
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET ID_Projekt = NULL WHERE ID = ?",
                                                  new DbParam("@g", GEBAEUDE_1051)));
            try
            {
                Konditionierungsarbeitsstand a = new KonditionierungCtrl().ArbeitsstandLesen(GEBAEUDE_1051, null, out string m);
                Assert.True(a != null, m);
                Assert.Equal(Konditionierungdatenweg.Rueckfallraster, a.Kalender);
            }
            finally
            {
                Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET ID_Projekt = ? WHERE ID = ?",
                                                      new DbParam("@p", 1051), new DbParam("@g", GEBAEUDE_1051)));
            }
        }

        [Fact]
        public void Der_Abdruck_aendert_sich_mit_dem_Raster()
        {
            Konditionierungsstand g = Konditionierungsstand.Leer(Kalendereigentuemer.Gebaeude, null);
            string donnerstag = Konditionierungsarbeit.Abdruck(new Konditionierungsarbeitsstand(g, null, 100.0, new Gemeinjahrkalender(DONNERSTAG)));
            string mittwoch = Konditionierungsarbeit.Abdruck(new Konditionierungsarbeitsstand(g, null, 100.0, new Gemeinjahrkalender(2)));
            string mitJahr = Konditionierungsarbeit.Abdruck(new Konditionierungsarbeitsstand(g, null, 100.0, Gemeinjahrkalender.Aus(DONNERSTAG, 2026)));
            Assert.NotEqual(donnerstag, mittwoch);
            Assert.NotEqual(donnerstag, mitJahr);
            Assert.Equal(donnerstag, Konditionierungsarbeit.Abdruck(new Konditionierungsarbeitsstand(g, null, 100.0, new Gemeinjahrkalender(DONNERSTAG))));
        }

        [Fact]
        public void Der_Teppich_nennt_das_Jahr_nur_mit_Preisreihe()
        {
            var k = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(20.0), null, Array.Empty<Kalenderregel>());
            Kalenderteppich ohne = Kalenderteppich.ImGemeinjahr(k, new Gemeinjahrkalender(DONNERSTAG));
            Kalenderteppich mit = Kalenderteppich.ImGemeinjahr(k, Gemeinjahrkalender.Kalenderjahr(2027));
            Assert.False(ohne.MitJahr);
            Assert.Equal(DONNERSTAG, ohne.WochentagDesErstenTags);
            Assert.True(mit.MitJahr);
            Assert.Equal(2027, mit.Bezugsjahr);
            // Der Wochentag der Anzeige folgt dem Jahr: 1. Januar 2027 = Freitag.
            Assert.Equal(FREITAG, mit.WochentagDesErstenTags);
            Assert.Equal(FREITAG, mit.Wochentag(0));
        }
    }
}
