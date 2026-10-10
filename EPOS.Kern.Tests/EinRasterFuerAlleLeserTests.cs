using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ein Wochentagsraster für alle Leser</b> (Entscheid E115): Gebäudelauf (Wochenendmaske,
    /// Konditionierungskalender), Zapfkalender und Bedarfsprofile rechnen mit dem Raster der Klimaregion des
    /// Projekts (<see cref="Konditionierungdatenweg.Raster(int)"/>); nur eine Preisreihe mit Jahr setzt die
    /// Feiertage auf echte Daten, das Raster bleibt das der Klimaregion. An der Testdatenbank liegt der
    /// 1. Januar im Raster der Klimaregion auf einem Donnerstag.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class EinRasterFuerAlleLeserTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public EinRasterFuerAlleLeserTests(TestDatenbank db) => _db = db;

        /// <summary>Donnerstag (0 = Montag) — der 1. Januar im Raster der Klimaregion der Testdatenbank.</summary>
        private const int DONNERSTAG = 3;

        /// <summary>Die aktive Stromspeicher-Variante von 1051 und die Preisreihe mit Jahr 2026 der Testdatenbank.</summary>
        private const int VARIANTE_1051 = 19, PREISREIHE_2026 = 3;

        /// <summary>Das Raster, mit dem der Gebäudelauf eines Projekts rechnet (Klimakalender des Laufs).</summary>
        private static Gemeinjahrkalender LaufRaster(int idProjekt, out bool[] wochenende)
        {
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
            wochenende = sim.Kalender.Gemeinsam.WochenendeOrtszeit;
            Assert.Equal(sim.WochentagJan1, sim.Kalender.Gemeinsam.Raster.W0);
            return sim.Kalender.Gemeinsam.Raster;
        }

        /// <summary>Das Raster, mit dem der Zapfkalender eines Projekts seine Feiertage legt.</summary>
        private static Gemeinjahrkalender ZapfRaster(int idProjekt)
        {
            Assert.True(ZapfprofilCtrl.KalenderLesen(idProjekt, out int w0, out _));
            return Konditionierungdatenweg.Raster(idProjekt, w0);
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
        public void Mit_Preisreihenjahr_loesen_beide_Leser_dasselbe_Raster_auf()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1051;
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_StromspeicherVariante SET ID_Preisreihe = ? WHERE ID = ?",
                                                  new DbParam("@p", PREISREIHE_2026), new DbParam("@v", VARIANTE_1051)));
            try
            {
                Assert.Equal(2026, SolardatenCtrl.Preisreihenjahr(PROJEKT));
                Gemeinjahrkalender lauf = LaufRaster(PROJEKT, out bool[] wochenende);
                // Das Jahr legt allein die Feiertage; das Raster bleibt das der Klimaregion (2026 begänne auch Donnerstag,
                // die Probe hält deshalb zusätzlich, dass das Raster nicht aus dem Datum stammt: siehe unten).
                Assert.Equal(Gemeinjahrkalender.Aus(DONNERSTAG, 2026), lauf);
                Assert.Equal(2026, lauf.Jahr);
                Assert.Equal(lauf, ZapfRaster(PROJEKT));
                Assert.Equal(lauf, Konditionierungdatenweg.Raster(PROJEKT));
                Assert.Equal(KlimakalenderGemeinsam.WochenendmaskeBilden(lauf), wochenende);
                // Mit 2026 liegt Ostern auf seinem echten Datum (5. April = Jahrestag 95), im Regelfall nach der Konvention.
                Assert.Equal(95, lauf.Ostersonntag);
            }
            finally
            {
                Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_StromspeicherVariante SET ID_Preisreihe = NULL WHERE ID = ?",
                                                      new DbParam("@v", VARIANTE_1051)));
            }
            Assert.Null(SolardatenCtrl.Preisreihenjahr(PROJEKT));
        }

        [Fact]
        public void Das_Raster_haengt_nicht_am_Datum_des_Preisreihenjahrs()
        {
            // 2025 begann an einem Mittwoch; im Raster Donnerstag bleibt der 1. Januar ein Donnerstag, nur die Feiertage
            // liegen auf den Daten von 2025.
            Gemeinjahrkalender r = Gemeinjahrkalender.Aus(DONNERSTAG, 2025);
            Assert.Equal(DONNERSTAG, r.W0);
            Assert.Equal(DONNERSTAG, r.Wochentag(1));
            Assert.NotEqual(Gemeinjahrkalender.Kalenderjahr(2025).W0, r.W0);
            Assert.Equal(Feiertage.Gemeinjahrestag(4, 20), r.Ostersonntag);
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
            Kalenderteppich mit = Kalenderteppich.ImGemeinjahr(k, Gemeinjahrkalender.Aus(DONNERSTAG, 2027));
            Assert.False(ohne.MitJahr);
            Assert.Equal(DONNERSTAG, ohne.WochentagDesErstenTags);
            Assert.True(mit.MitJahr);
            Assert.Equal(2027, mit.Bezugsjahr);
            Assert.Equal(DONNERSTAG, mit.WochentagDesErstenTags);
        }
    }
}
