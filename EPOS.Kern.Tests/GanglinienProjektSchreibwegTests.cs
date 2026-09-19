using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>GL-1 — die vier Ganglinien-Schreibwege geben das Projekt mit</b>
    /// (Anwenderauftrag vom 19.09.2026, „Altfehler beheben").
    ///
    /// <para><b>Worum es geht.</b> <c>Tab_Solarganglinie</c>, <c>Tab_Stromganglinie</c>
    /// und <c>Tab_Waermebedarf</c> sind PROJEKTtabellen; ihre Spalte <c>ID_Projekt</c>
    /// traegt seit Schemaschritt 96 (FK-2) einen Fremdschluessel auf <c>Tab_Projekt</c>
    /// mit <c>ON DELETE CASCADE</c>. Vier Schreibwege —
    /// <c>SolarganglinieCtrl.Insert</c>, <c>StromganglinieCtrl.Insert</c>,
    /// <c>WaermebedarfCtrl.Insert</c> und
    /// <c>StromganglinieDatenCtrl.InsertKompletteGanglinie</c> — liessen die Spalte
    /// leer: erst still als Spaltenvorgabe 0, seit FK-2 ausdruecklich als NULL. Ein
    /// Satz ohne Projekt ist ueber <c>ID_Projekt</c> nicht auffindbar und wird vom
    /// Loeschweg des Projekts nicht mitgenommen.</para>
    ///
    /// <para><b>Was hier festgehalten wird.</b> Jeder der vier Wege verlangt jetzt die
    /// Projektnummer und schreibt sie; ein Aufruf ohne Projekt wird BENANNT abgewiesen
    /// (<see cref="ArgumentOutOfRangeException"/>), ein Aufruf mit unbekanntem Projekt
    /// vom Fremdschluessel. Dazu der Weg, den die Bedienung heute wirklich nimmt:
    /// Katalogware liegt in den <c>_STAMM</c>-Tabellen und kommt ueber
    /// <c>…StammCtrl.ApplyGanglinieToProjekt</c> als Kopie MIT Projektnummer ins
    /// Projekt.</para>
    ///
    /// <para><b>Jeder Fall bekommt seine eigene Arbeitskopie</b> — die Faelle
    /// schreiben, und einer loescht ein Projekt. Ohne Datenbank schweigen sie
    /// (<see cref="TestDatenbank.Vorhanden"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GanglinienProjektSchreibwegTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =====================================================================
        //  Teil 1 — der Satz traegt das Projekt und ist danach filterbar
        // =====================================================================

        /// <summary>
        /// <c>StromganglinieCtrl.Insert</c> schreibt das Projekt, und ein Filter nach
        /// <c>ID_Projekt</c> findet den Satz.
        /// </summary>
        [Fact]
        public void Stromganglinie_wird_mit_Projekt_geschrieben_und_ist_filterbar()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            int projekt = ProjektAnlegen("GL-1 Strom");

            StromganglinieCtrl ctrl = new StromganglinieCtrl();
            ctrl.m_szBezeichner = "GL1_Strom";
            ctrl.m_Zeitinterval = 1;

            Assert.True(ctrl.Insert(projekt));
            Assert.True(ctrl.m_ID_Ganglinie > 0);

            Assert.Equal((long)projekt, Zahl(
                "SELECT ID_Projekt FROM Tab_Stromganglinie WHERE ID = " + ctrl.m_ID_Ganglinie));
            Assert.Equal(1L, Zahl(
                "SELECT COUNT(*) FROM Tab_Stromganglinie WHERE ID_Projekt = " + projekt));
        }

        /// <summary>
        /// <c>SolarganglinieCtrl.Insert</c> schreibt das Projekt, und ein Filter nach
        /// <c>ID_Projekt</c> findet den Satz.
        /// </summary>
        [Fact]
        public void Solarganglinie_wird_mit_Projekt_geschrieben_und_ist_filterbar()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            int projekt = ProjektAnlegen("GL-1 Solar");

            SolarganglinieCtrl ctrl = new SolarganglinieCtrl();
            ctrl.m_szBezeichner = "GL1_Solar";
            ctrl.m_szBeschreibung = "Probe GL-1";

            Assert.True(ctrl.Insert(projekt));
            Assert.True(ctrl.m_ID_Ganglinie > 0);

            Assert.Equal((long)projekt, Zahl(
                "SELECT ID_Projekt FROM Tab_Solarganglinie WHERE ID = " + ctrl.m_ID_Ganglinie));
            Assert.Equal(1L, Zahl(
                "SELECT COUNT(*) FROM Tab_Solarganglinie WHERE ID_Projekt = " + projekt));
        }

        /// <summary>
        /// <c>WaermebedarfCtrl.Insert</c> schreibt das Projekt, und ein Filter nach
        /// <c>ID_Projekt</c> findet den Satz.
        /// </summary>
        [Fact]
        public void Waermebedarf_wird_mit_Projekt_geschrieben_und_ist_filterbar()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            int projekt = ProjektAnlegen("GL-1 Waerme");

            WaermebedarfCtrl ctrl = new WaermebedarfCtrl();
            ctrl.m_szBezeichner = "GL1_Waerme";

            Assert.True(ctrl.Insert(projekt));
            Assert.True(ctrl.m_ID_Ganglinie > 0);

            Assert.Equal((long)projekt, Zahl(
                "SELECT ID_Projekt FROM Tab_Waermebedarf WHERE ID = " + ctrl.m_ID_Ganglinie));
            Assert.Equal(1L, Zahl(
                "SELECT COUNT(*) FROM Tab_Waermebedarf WHERE ID_Projekt = " + projekt));
        }

        /// <summary>
        /// <c>InsertKompletteGanglinie</c> schreibt Kopf UND Werte in einer Transaktion,
        /// der Kopf traegt das Projekt — und das Loeschen des Projekts nimmt beide mit
        /// (Nachweis der Kaskade aus Schemaschritt 96, Windows-Abnahme A-GL1-2).
        /// </summary>
        [Fact]
        public void Kopf_und_Werte_tragen_das_Projekt_und_folgen_dem_Loeschweg()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            int projekt = ProjektAnlegen("GL-1 Kopf und Werte");

            StromganglinieCtrl kopf = new StromganglinieCtrl();
            kopf.m_szBezeichner = "GL1_KopfUndWerte";
            kopf.m_Zeitinterval = 1;

            List<string> werte = new List<string> { "1.5", "2.5", "3.5" };

            Assert.True(new StromganglinieDatenCtrl().InsertKompletteGanglinie(kopf, werte, projekt));

            int id = kopf.m_ID_Ganglinie;
            Assert.True(id > 0);
            Assert.Equal((long)projekt, Zahl("SELECT ID_Projekt FROM Tab_Stromganglinie WHERE ID = " + id));
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_StromganglinieDaten WHERE ID_Ganglinie = " + id));

            // Der Loeschweg des Projekts: Kopf und Werte gehen mit.
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Projekt WHERE ID = ?",
                                           new DbParam("@p", DbParamTyp.Integer) { Wert = projekt });

            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Stromganglinie WHERE ID = " + id));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_StromganglinieDaten WHERE ID_Ganglinie = " + id));
        }

        // =====================================================================
        //  Teil 2 — ohne gueltiges Projekt wird benannt abgewiesen
        // =====================================================================

        /// <summary>
        /// Keiner der vier Wege schreibt ohne Projektnummer. Die Ablehnung ist BENANNT
        /// und nennt die <c>_STAMM</c>-Tabelle als richtiges Ziel fuer Katalogware.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Ohne_Projektnummer_weist_jeder_der_vier_Wege_benannt_ab(int idProjekt)
        {
            using var _ = new Kulturvorrichtung();

            var strom = Assert.Throws<ArgumentOutOfRangeException>(
                () => new StromganglinieCtrl().Insert(idProjekt));
            Assert.Contains("Tab_Stromganglinie_STAMM", strom.Message, StringComparison.Ordinal);

            var solar = Assert.Throws<ArgumentOutOfRangeException>(
                () => new SolarganglinieCtrl().Insert(idProjekt));
            Assert.Contains("Tab_Solarganglinie_STAMM", solar.Message, StringComparison.Ordinal);

            var waerme = Assert.Throws<ArgumentOutOfRangeException>(
                () => new WaermebedarfCtrl().Insert(idProjekt));
            Assert.Contains("Tab_Waermebedarf_STAMM", waerme.Message, StringComparison.Ordinal);

            // Auch dann, wenn gar keine Werte mitkommen - die Pruefung steht VOR dem
            // stillen "nichts zu tun, also true".
            var komplett = Assert.Throws<ArgumentOutOfRangeException>(
                () => new StromganglinieDatenCtrl()
                          .InsertKompletteGanglinie(new StromganglinieCtrl(), null, idProjekt));
            Assert.Contains("Tab_Stromganglinie_STAMM", komplett.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ein Projekt, das es nicht gibt, weist der Fremdschluessel aus Schemaschritt 96
        /// ab: Der Schreibweg meldet den Datenbankfehler und gibt <c>false</c> zurueck —
        /// die Tabelle bleibt unveraendert.
        /// </summary>
        [Fact]
        public void Ein_unbekanntes_Projekt_weist_der_Fremdschluessel_ab()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            int unbekannt = (int)Zahl("SELECT MAX(ID) FROM Tab_Projekt") + 1000;
            long vorherStrom = Zahl("SELECT COUNT(*) FROM Tab_Stromganglinie");
            long vorherSolar = Zahl("SELECT COUNT(*) FROM Tab_Solarganglinie");
            long vorherWaerme = Zahl("SELECT COUNT(*) FROM Tab_Waermebedarf");

            string[] meldungen;
            using (DataRepository.EngineModus())
            {
                StromganglinieCtrl strom = new StromganglinieCtrl();
                strom.m_szBezeichner = "GL1_Waise";
                Assert.False(strom.Insert(unbekannt));

                SolarganglinieCtrl solar = new SolarganglinieCtrl();
                solar.m_szBezeichner = "GL1_Waise";
                Assert.False(solar.Insert(unbekannt));

                WaermebedarfCtrl waerme = new WaermebedarfCtrl();
                waerme.m_szBezeichner = "GL1_Waise";
                Assert.False(waerme.Insert(unbekannt));

                meldungen = DataRepository.StilleFehlerAbholen();
            }

            Assert.Equal(3, meldungen.Length);
            Assert.All(meldungen, m => Assert.Contains("FOREIGN KEY", m, StringComparison.OrdinalIgnoreCase));

            Assert.Equal(vorherStrom, Zahl("SELECT COUNT(*) FROM Tab_Stromganglinie"));
            Assert.Equal(vorherSolar, Zahl("SELECT COUNT(*) FROM Tab_Solarganglinie"));
            Assert.Equal(vorherWaerme, Zahl("SELECT COUNT(*) FROM Tab_Waermebedarf"));
        }

        // =====================================================================
        //  Teil 3 — der Weg, den die Bedienung heute nimmt
        // =====================================================================

        /// <summary>
        /// <b>Ende zu Ende:</b> Die Importkette legt Katalogware in der
        /// <c>_STAMM</c>-Tabelle ab (<c>GanglinienZiel</c>), und erst die Anwendung auf
        /// ein Projekt kopiert sie MIT Projektnummer in die Projekttabelle. Danach ist
        /// der Satz ueber <c>ID_Projekt</c> filterbar — Windows-Abnahme A-GL1-1.
        /// </summary>
        [Fact]
        public void Die_Katalogkopie_ins_Projekt_setzt_die_Projektnummer()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            int projekt = ProjektAnlegen("GL-1 Katalogkopie");

            // Katalogware — in die _STAMM-Tabelle, ohne Projektspalte.
            var werte = new List<double> { 1.0, 2.0, 3.0, 4.0 };
            Assert.True(new StromganglinieStammCtrl().ImportGanglinie("GL1_Katalog_Strom", 1, werte));
            Assert.True(new WaermebedarfStammCtrl().ImportGanglinie("GL1_Katalog_Waerme", werte));

            // Die Kopie ins Projekt traegt die Projektnummer.
            int idStrom = StromganglinieStammCtrl.ApplyGanglinieToProjekt("GL1_Katalog_Strom", projekt);
            int idWaerme = WaermebedarfStammCtrl.ApplyGanglinieToProjekt("GL1_Katalog_Waerme", projekt);

            Assert.True(idStrom > 0);
            Assert.True(idWaerme > 0);
            Assert.Equal((long)projekt, Zahl("SELECT ID_Projekt FROM Tab_Stromganglinie WHERE ID = " + idStrom));
            Assert.Equal((long)projekt, Zahl("SELECT ID_Projekt FROM Tab_Waermebedarf WHERE ID = " + idWaerme));
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM Tab_StromganglinieDaten WHERE ID_Ganglinie = " + idStrom));
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM Tab_WaermebedarfDaten WHERE ID_Ganglinie = " + idWaerme));

            // Der Katalogsatz selbst bleibt, wo er hingehoert.
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Stromganglinie_STAMM " +
                                 "WHERE Bezeichner = 'GL1_Katalog_Strom'"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Waermebedarf_STAMM " +
                                 "WHERE Bezeichner = 'GL1_Katalog_Waerme'"));
        }

        /// <summary>
        /// DIE WACHE: In den drei Projekttabellen steht kein Satz ohne Projekt. Die
        /// Bereinigung des Schemaschritts 96 hat die letzten entfernt (vier
        /// Stromganglinien, drei Waermebedarfe); ein neuer Satz ohne Projekt waere ein
        /// Rueckfall hinter GL-1.
        /// </summary>
        [Fact]
        public void Keine_Ganglinie_der_Projekttabellen_steht_ohne_Projekt()
        {
            using var _ = new Kulturvorrichtung();
            if (!_db.Vorhanden) return;

            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Stromganglinie " +
                                 "WHERE ID_Projekt IS NULL OR ID_Projekt = 0"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Solarganglinie " +
                                 "WHERE ID_Projekt IS NULL OR ID_Projekt = 0"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Waermebedarf " +
                                 "WHERE ID_Projekt IS NULL OR ID_Projekt = 0"));
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Legt ein leeres Projekt an und liefert seine Id.</summary>
        private static int ProjektAnlegen(string name)
        {
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Projekt (Projektname) VALUES (?)",
                new DbParam("@name", DbParamTyp.VarWChar) { Wert = name });
            return (int)Zahl("SELECT MAX(ID) FROM Tab_Projekt");
        }

        private static long Zahl(string sql)
        {
            object wert = DataRepository.ExecuteScalar(sql);
            return wert == null || wert == DBNull.Value
                ? -1
                : Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }
    }
}
