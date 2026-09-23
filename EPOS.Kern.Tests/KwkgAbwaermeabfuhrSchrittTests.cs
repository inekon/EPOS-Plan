using System;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c — Schemaschritt <b>105</b>: der zweite Fall des § 2 Nr. 16 KWKG
    /// (Befund K‑1, Entscheide EZ‑5 und E7‑Q2 vom 23.09.2026). <c>Tab_Energieanlagen</c>
    /// bekommt das Kennzeichen „Vorrichtung zur Abwärmeabfuhr" (0/1 mit <c>CHECK</c>,
    /// Vorgabe 0) und die nullbare Stromkennzahl.
    ///
    /// <para>Geprüft wird: der Zielstand und die Liste des Schrittes samt ihrer
    /// SQLite-Typen; die nachgezogene Arbeitskopie (beide Spalten stehen je einmal, der
    /// Bestand steht auf 0 bzw. NULL, die Tabelle bleibt STRICT); und die
    /// <c>CHECK</c>-Bedingung, die nur 0 und 1 durchlässt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KwkgAbwaermeabfuhrSchrittTests
    {
        [Fact]
        public void Der_Zielstand_ist_105_und_der_Schritt_hat_zwei_Anlagenspalten()
        {
            Assert.True(SchemaStand.Zielversion >= 105,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 105.");

            SchemaSpalte[] spalten = SchemaKatalog.Schritt105_KwkgAbwaermeabfuhr;
            Assert.Equal(2, spalten.Length);
            Assert.All(spalten, s => Assert.Equal("Tab_Energieanlagen", s.Tabelle));
            Assert.Equal(new[] { "KWKG_Abwaermeabfuhr", "KWKG_Stromkennzahl" },
                         spalten.Select(s => s.Name).ToArray());
            Assert.Equal(new[] { "YESNO", "DOUBLE" }, spalten.Select(s => s.TypDefinition).ToArray());

            // Die SQLite-Typen: das Kennzeichen 0/1 mit CHECK und Vorgabe 0 (STRICT
            // verlangt die Vorgabe beim ADD COLUMN mit NOT NULL), die Kennzahl nullbar.
            Assert.Equal("INTEGER NOT NULL DEFAULT 0 CHECK (\"KWKG_Abwaermeabfuhr\" IN (0,1))",
                         StilleDb.SqliteSpaltenTyp(spalten[0].Name, spalten[0].TypDefinition));
            Assert.Equal("REAL", StilleDb.SqliteSpaltenTyp(spalten[1].Name, spalten[1].TypDefinition));
        }

        /// <summary>
        /// Die nachgezogene Arbeitskopie: beide Spalten stehen genau einmal, der ganze
        /// Bestand trägt 0 (Fall 1) bzw. NULL (nicht gepflegt), und die Tabelle ist
        /// weiter STRICT — der Schritt ist kein Tabellenneubau.
        /// </summary>
        [Fact]
        public void Die_Arbeitskopie_fuehrt_beide_Spalten_und_der_Bestand_ist_Fall_1()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt105_KwkgAbwaermeabfuhr)
            {
                Assert.True(DataRepository.SpalteVorhanden(s.Tabelle, s.Name), s.Name + " fehlt.");
                Assert.Equal(1, Zahl("SELECT COUNT(*) FROM pragma_table_info('Tab_Energieanlagen') " +
                                     "WHERE name = '" + s.Name + "'"));
            }

            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen") > 0);
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE KWKG_Abwaermeabfuhr <> 0"));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE KWKG_Stromkennzahl IS NOT NULL"));

            Assert.Equal(1, Zahl("SELECT \"notnull\" FROM pragma_table_info('Tab_Energieanlagen') " +
                                 "WHERE name = 'KWKG_Abwaermeabfuhr'"));
            Assert.Equal(0, Zahl("SELECT \"notnull\" FROM pragma_table_info('Tab_Energieanlagen') " +
                                 "WHERE name = 'KWKG_Stromkennzahl'"));

            string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Tab_Energieanlagen'"));
            Assert.EndsWith("STRICT", ddl.TrimEnd());
            Assert.Contains("CHECK (\"KWKG_Abwaermeabfuhr\" IN (0,1))", ddl, StringComparison.Ordinal);
        }

        /// <summary>
        /// Die <c>CHECK</c>-Bedingung: 1 geht durch, 2 nicht — der Wert bleibt dann, wie
        /// er war. Die Stromkennzahl nimmt eine Kommazahl.
        /// </summary>
        [Fact]
        public void Das_Kennzeichen_nimmt_nur_0_und_1()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int anlage = Zahl("SELECT MIN(ID) FROM Tab_Energieanlagen WHERE ID_Type = " + WizardItemClass.BHKW_TYP);
            Assert.True(anlage > 0, "Die Testdatenbank führt keine BHKW-Anlage.");

            Assert.True(DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET KWKG_Abwaermeabfuhr = 1, KWKG_Stromkennzahl = 0.617 WHERE ID = ?",
                new DbParam("@id", anlage)) == 1);
            Assert.Equal(1, Zahl("SELECT KWKG_Abwaermeabfuhr FROM Tab_Energieanlagen WHERE ID = " + anlage));

            using (DataRepository.EngineModus())
                DataRepository.ExecuteNonQuery(
                    "UPDATE Tab_Energieanlagen SET KWKG_Abwaermeabfuhr = 2 WHERE ID = ?",
                    new DbParam("@id", anlage));
            DataRepository.StilleFehlerAbholen();
            Assert.Equal(1, Zahl("SELECT KWKG_Abwaermeabfuhr FROM Tab_Energieanlagen WHERE ID = " + anlage));
            Assert.Equal(0.617, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT KWKG_Stromkennzahl FROM Tab_Energieanlagen WHERE ID = " + anlage)), 9);
        }

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }
    }
}
