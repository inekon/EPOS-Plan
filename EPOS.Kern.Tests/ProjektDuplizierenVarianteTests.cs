using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Duplizieren einer Variante: Die Kopie bleibt Variante DESSELBEN Stamms. Der Stammverweis
    /// <c>Tab_Variante.ID_ProjektRef</c> bekommt keinen Projektversatz; <c>ID_Projekt</c> der
    /// Variantenzeile ist die neue Projekt-ID. Projekt 1044 der Testdatenbank ist Variante von 1042.
    /// Die Gegenprobe kopiert den Stamm 1042: Er fuehrt keine eigene Variantenzeile, und seine
    /// Varianten kommen nicht mit.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektDuplizierenVarianteTests
    {
        private const int STAMM = 1042;
        private const int VARIANTE = 1044;

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        private static string Name(int id)
            => Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = " + id));

        [Fact]
        public void Die_Kopie_einer_Variante_zeigt_auf_denselben_Stamm()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(STAMM, Zahl("SELECT ID_ProjektRef FROM Tab_Variante WHERE ID_Projekt = " + VARIANTE));

            int kopie = new ProjektDuplizierenCtrl().Duplizieren(Name(VARIANTE), "Variantenprobe Kopie");
            Assert.True(kopie > 0);
            Assert.NotEqual(VARIANTE, kopie);

            // Genau eine Variantenzeile der Kopie, mit der neuen Projekt-ID und dem alten Stamm.
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Tab_Variante WHERE ID_Projekt = " + kopie));
            Assert.Equal(STAMM, Zahl("SELECT ID_ProjektRef FROM Tab_Variante WHERE ID_Projekt = " + kopie));

            // Die Quelle ist unberuehrt geblieben.
            Assert.Equal(STAMM, Zahl("SELECT ID_ProjektRef FROM Tab_Variante WHERE ID_Projekt = " + VARIANTE));
        }

        [Fact]
        public void Die_Kopie_eines_Stamms_legt_keine_Variantenzeile_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Variante WHERE ID_Projekt = " + STAMM));
            int vorher = Zahl("SELECT COUNT(*) FROM Tab_Variante");

            int kopie = new ProjektDuplizierenCtrl().Duplizieren(Name(STAMM), "Stammprobe Kopie");
            Assert.True(kopie > 0);

            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Variante WHERE ID_Projekt = " + kopie));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Variante WHERE ID_ProjektRef = " + kopie));
            Assert.Equal(vorher, Zahl("SELECT COUNT(*) FROM Tab_Variante"));
        }
    }
}
