using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Profilquelle der Kälte</b> (<see cref="ProfilQuelle.Kaelte"/>, Welle K1): Tabellen und Spalten je Modus, und
    /// <see cref="ProfilBedarf.Rechnen"/> mit einem Kältetyp ergibt 8760 Werte mit der Jahressumme der Zuordnung.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProfilQuelleKaelteTests : IDisposable
    {
        private const int PROJEKT = 1017;
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Fabrik_liefert_Tabellen_und_Spalten_je_Modus()
        {
            ProfilQuelle lauf = ProfilQuelle.Kaelte(ProfilQuellmodus.Projektrechnung);
            Assert.Equal("Tab_Kaeltebedarf", lauf.KopfTabelle);
            Assert.Equal("Tab_Kaeltetyp", lauf.TypTabelle);
            Assert.Equal("Typname", lauf.TypSchluesselSpalte);
            Assert.Equal("Z_Projekt_Kaeltebedarf", lauf.ZuordnungTabelle);
            Assert.Equal("ID_Kaeltebedarf", lauf.ZuordnungIdSpalte);
            Assert.Equal("ID_Kaeltebedarf", lauf.TypKopfIdSpalte);
            Assert.True(lauf.ProjektfilterAktiv);
            Assert.Null(lauf.Rueckfall);

            ProfilQuelle katalog = ProfilQuelle.Kaelte(ProfilQuellmodus.Katalogvorschau);
            Assert.Equal("Tab_Kaeltebedarf_STAMM", katalog.KopfTabelle);
            Assert.Equal("Tab_Kaeltetyp_STAMM", katalog.TypTabelle);
            Assert.Equal("Bezeichner", katalog.TypSchluesselSpalte);
            Assert.False(katalog.ProjektfilterAktiv);
            Assert.Null(katalog.TypKopfIdSpalte);

            ProfilQuelle vorschau = ProfilQuelle.Kaelte(ProfilQuellmodus.Projektvorschau);
            Assert.NotNull(vorschau.Rueckfall);
            Assert.Equal("Tab_Kaeltebedarf_STAMM", vorschau.Rueckfall.KopfTabelle);
        }

        [Fact]
        public void Ein_Kaeltetyp_ergibt_8760_Werte_mit_der_Jahressumme()
        {
            if (!_db.Vorhanden) return;
            long stamm = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = 'Kühlraum'"), CultureInfo.InvariantCulture);
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = (int)stamm, Bezeichner = "Kühlraum", Summe = 50 }
            }));

            (int[] a, int[] e) = Monate();
            var ziel = new double[8760];
            var monate = new double[12];
            Assert.True(ProfilBedarf.Rechnen(ProfilQuelle.Kaelte(ProfilQuellmodus.Projektrechnung), PROJEKT, null, 3, a, e, ziel, monate));
            Assert.Equal(8760, ziel.Length);
            Assert.All(ziel, x => Assert.True(x > 0));                   // Kühlraum: rund um die Uhr
            double jahr = ziel.Sum();
            Assert.True(Math.Abs(jahr - 50 * 1000) < 1e-6 * 50 * 1000, "Jahressumme " + jahr.ToString(CultureInfo.InvariantCulture));
            Assert.True(monate[6] > monate[0]);                           // leichter Sommeranstieg
        }

        private static (int[] anfang, int[] ende) Monate()
        {
            var a = new int[12];
            var e = new int[12];
            int h = 0;
            for (int m = 0; m < 12; m++)
            {
                a[m] = h;
                h += Feiertage.TageJeMonat[m] * 24;
                e[m] = h - 1;
            }
            return (a, e);
        }
    }
}
