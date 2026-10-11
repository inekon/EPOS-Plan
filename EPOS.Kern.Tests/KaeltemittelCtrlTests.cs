using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der lesende Controller der Kältemitteltabelle</b> (<see cref="KaeltemittelCtrl"/>; KM4-E1-b, Fachkonzept
    /// Kältemittel als Stammdatum 4.1): Liste in Sortierung, Satz zur ID, Code → ID mit Trim und ohne Groß/Klein, und eine
    /// Datenbank vor Schritt 216 liefert die leere Liste bzw. <c>null</c>.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemittelCtrlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Liste_fuehrt_die_Saat_in_ihrer_Sortierung()
        {
            if (!_db.Vorhanden) return;
            var liste = KaeltemittelCtrl.Liste();
            Assert.Equal(KaeltemittelSchema.SAAT.Count, liste.Count);
            Assert.Equal(KaeltemittelSchema.SAAT.Select(k => k.Code), liste.Select(z => z.Code));
            Assert.True(liste.All(z => z.ReadOnly));
            Assert.True(liste.All(z => z.Status == DbWerte.GESETZ_STATUS_VORLAEUFIG));

            KaeltemittelCtrl.Kaeltemittelsatz co2 = liste.Single(z => z.Code == "R744");
            Assert.Equal(DbWerte.KAELTEMITTEL_GRUPPE_NATUERLICH, co2.Gruppe);
            Assert.True(co2.Natuerlich);
            Assert.False(co2.F_Gas);
            Assert.Equal(1.0, co2.GWP);
            Assert.False(string.IsNullOrEmpty(co2.Bezeichnung));
            Assert.False(string.IsNullOrEmpty(co2.GWP_Quelle));
            Assert.False(string.IsNullOrEmpty(co2.Quelle));
            Assert.NotNull(co2.PFAS_Status);
            Assert.NotNull(co2.Sortierung);
            Assert.Null(co2.Sicherheitsgruppe);
        }

        [Fact]
        public void Code_zur_ID_mit_Trim_und_ohne_Gross_Klein()
        {
            if (!_db.Vorhanden) return;
            int? r513a = KaeltemittelCtrl.IdZumCode("R513A");
            Assert.NotNull(r513a);
            Assert.Equal(r513a, KaeltemittelCtrl.IdZumCode("  r513a\t"));
            Assert.Equal("R1234ze(E)", KaeltemittelCtrl.Laden(KaeltemittelCtrl.IdZumCode("r1234ZE(e)").Value).Code);
            Assert.Null(KaeltemittelCtrl.IdZumCode("R1234ze"));   // ohne Isomer kein Treffer
            Assert.Null(KaeltemittelCtrl.IdZumCode("Frigen X"));
            Assert.Null(KaeltemittelCtrl.IdZumCode(""));
            Assert.Null(KaeltemittelCtrl.IdZumCode("   "));
            Assert.Null(KaeltemittelCtrl.IdZumCode(null));

            KaeltemittelCtrl.Kaeltemittelsatz s = KaeltemittelCtrl.Laden(r513a.Value);
            Assert.Equal("R513A", s.Code);
            Assert.Equal(r513a.Value, s.Id);
            Assert.Equal(s, KaeltemittelCtrl.LadenZumCode(" R513a "));
            Assert.Null(KaeltemittelCtrl.Laden(999999));
            Assert.Null(KaeltemittelCtrl.LadenZumCode("Frigen X"));
        }

        [Fact]
        public void Vor_dem_Schritt_leer_und_null()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("DROP TABLE " + KaeltemittelSchema.TAB_STAMM);
            Assert.False(KaeltemittelCtrl.Vorhanden());
            Assert.Empty(KaeltemittelCtrl.Liste());
            Assert.Null(KaeltemittelCtrl.IdZumCode("R32"));
            Assert.Null(KaeltemittelCtrl.Laden(1));
        }
    }
}
