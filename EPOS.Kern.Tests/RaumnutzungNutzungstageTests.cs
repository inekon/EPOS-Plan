using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die abgeleiteten Nutzungstage eines Profils</b> (E93): im Raster von 365 Tagen die Tage des Wochenmusters,
    /// abzüglich der Feiertage an Nutzungstagen (bei „Feiertage wie Sonntag", Lage wie <see cref="Kalenderregel.Feiertag"/>)
    /// und der Ferientage des Ziels. Bezugsjahr 2025: Der 1. Januar ist ein Mittwoch, alle neun Feiertage liegen auf
    /// einem Werktag. Ohne Datenbank.
    /// </summary>
    public sealed class RaumnutzungNutzungstageTests
    {
        /// <summary>Das Raster dieser Proben: 1. Januar = Mittwoch, ohne Jahr (ein Projektraster, E115).</summary>
        private static readonly Gemeinjahrkalender MITTWOCH = new Gemeinjahrkalender(2);

        private static Raumnutzungstage N(Raumnutzungsprofil p, Matrixeingang ferien = null)
            => Raumnutzungsgenerator.Nutzungstage(p, ferien, MITTWOCH);

        /// <summary>Ohne Raster (Katalog) gilt das Rückfallraster mit dem 1. Januar auf einem Sonntag (E115).</summary>
        [Fact]
        public void Ohne_Raster_gilt_das_Rueckfallraster()
        {
            Raumnutzungstage t = Raumnutzungsgenerator.Nutzungstage(Profil("1111100", true));
            Assert.Equal(260, t.Wochenmuster);   // 52 Wochen, der 365. Tag ist ein Sonntag
            Assert.Equal(252, t.Tage);
            Assert.Equal(Raumnutzungsgenerator.Nutzungstage(Profil("1111100", true), null, Konditionierungdatenweg.Rueckfallraster).Tage, t.Tage);
        }

        private static Raumnutzungsprofil Profil(string woche, bool? feiertage)
            => new Raumnutzungsprofil { Bezeichner = "Probe", Nutzungstage_Woche = woche, Feiertage_Wie_Sonntag = feiertage };

        private static Matrixeingang Ferien(params (int Von, int Bis)[] zeitraeume)
        {
            var b = new Matrixeingang();
            for (int k = 0; k < zeitraeume.Length; k++)
            {
                b.Ferienbeginn[k] = zeitraeume[k].Von;
                b.Ferienende[k] = zeitraeume[k].Bis;
            }
            return b;
        }

        [Fact]
        public void Werktage_mit_Feiertagen_wie_Sonntag_ergeben_252_Tage()
        {
            Raumnutzungstage t = N(Profil("1111100", true));
            Assert.Equal(261, t.Wochenmuster);
            Assert.Equal(9, t.Feiertage);
            Assert.Equal(0, t.Ferientage);
            Assert.Equal(252, t.OhneFerien);
            Assert.Equal(252, t.Tage);
        }

        [Fact]
        public void Ohne_Feiertagsvorgabe_zaehlt_das_Wochenmuster_allein()
        {
            Assert.Equal(261, N(Profil("1111100", false)).Tage);
            Assert.Equal(261, N(Profil("1111100", null)).Tage);
        }

        [Fact]
        public void Ohne_Wochenmuster_gilt_jeder_Tag_und_ein_genutzter_Sonntag_nutzt_auch_den_Feiertag()
        {
            Assert.Equal(365, N(Profil(null, false)).Tage);
            Raumnutzungstage t = N(Profil("1111111", true));
            Assert.Equal(365, t.Tage);
            Assert.Equal(0, t.Feiertage);
        }

        [Fact]
        public void Ferientage_des_Ziels_zaehlen_nur_an_Nutzungstagen()
        {
            // 1. bis 30. Juli (Tag 182 bis 211): 22 Werktage, kein Feiertag.
            Raumnutzungstage t = N(Profil("1111100", true), Ferien((182, 211)));
            Assert.Equal(252, t.OhneFerien);
            Assert.Equal(22, t.Ferientage);
            Assert.Equal(230, t.Tage);
        }

        [Fact]
        public void Ferien_ueber_den_Jahreswechsel_ohne_die_Feiertage_darin_und_aus_bleibt_aus()
        {
            // 24. Dezember bis 6. Januar: Werktage ohne Weihnachten und Neujahr — 24., 29., 30., 31.12. und 2., 3., 6.1.
            Raumnutzungstage t = N(Profil("1111100", true), Ferien((358, 6), (0, 40), (100, 366)));
            Assert.Equal(7, t.Ferientage);
            Assert.Equal(245, t.Tage);
        }

        [Fact]
        public void Das_Bezugsjahr_verschiebt_Wochentage_und_Feiertage()
        {
            // 2026: Der 1. Januar ist ein Donnerstag (261 Werktage); der 3. Oktober und der 26. Dezember sind Samstage.
            Raumnutzungstage t = Raumnutzungsgenerator.Nutzungstage(Profil("1111100", true), null, Gemeinjahrkalender.Kalenderjahr(2026));
            Assert.Equal(261, t.Wochenmuster);
            Assert.Equal(7, t.Feiertage);
            Assert.Equal(254, t.Tage);
        }
    }
}
