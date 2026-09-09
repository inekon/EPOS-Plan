using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERENTSCHEID W5‑B‑12 (09.09.2026) — die ABLAGE der VALERI-Ergänzung:
    /// Migrationsschritt <b>72</b>.
    ///
    /// <para>Vier nullbare Spalten an <c>Tab_ProjektWirtschaftlichkeit</c> — der
    /// Preisänderungssatz der kapitalgebundenen Kosten p_I als Projektwert und je einer
    /// für Best und Worst (VALERI-Lücke <b>G4</b>) sowie ein Freitextfeld für die nicht
    /// monetären Wirkungen (Lücke <b>G6</b>). Kein DML: NULL heißt bei p_I „wie p_B",
    /// bei den Szenariospalten „wie das wirksame p_B des Szenarios" und beim Freitext
    /// „nichts erfasst".</para>
    ///
    /// <para>Den Rechenweg, der den Satz verbraucht, hält
    /// <see cref="KapitalwertRechnerPreisindexTests"/> fest.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class Migration72Tests
    {
        /// <summary>Der Zielstand ist 72, und die vier Spalten stehen im Katalog — alle
        /// an derselben Tabelle, alle mit verschiedenen Namen.</summary>
        [Fact]
        public void Der_Migrationsschritt_72_fuehrt_vier_Spalten()
        {
            Assert.True(SchemaStand.Zielversion >= 72,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 72.");

            var namen = new List<string>();
            foreach (SchemaSpalte s in SchemaKatalog.Schritt72_ValeriErgaenzung)
            {
                Assert.Equal(SchemaKatalog.TAB_PROJEKTWIRTSCHAFT, s.Tabelle);
                namen.Add(s.Name);
            }
            Assert.Equal(4, namen.Count);
            Assert.Equal(4, new HashSet<string>(namen).Count);
            Assert.Contains(SchemaKatalog.SPALTE_PW_PREIS_I, namen);
            Assert.Contains(SchemaKatalog.SPALTE_PW_SZEN_BEST_PREIS_I, namen);
            Assert.Contains(SchemaKatalog.SPALTE_PW_SZEN_WORST_PREIS_I, namen);
            Assert.Contains(SchemaKatalog.SPALTE_PW_NICHT_MONETAER, namen);
        }

        /// <summary>
        /// Die drei Preisspalten landen als <c>REAL</c> in der STRICT-Tabelle, der
        /// Freitext als <c>TEXT</c> OHNE Längenprüfung — genau der Typ, den
        /// <c>ALTER TABLE … ADD COLUMN</c> dort zulässt, und genau der, den ein
        /// Fließtext unbekannter Länge braucht.
        /// </summary>
        [Fact]
        public void Die_Spaltentypen_sind_REAL_und_TEXT()
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt72_PreisInvestition)
            {
                Assert.Equal("DOUBLE", s.TypDefinition);
                Assert.Equal("REAL", StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
            }

            foreach (SchemaSpalte s in SchemaKatalog.Schritt72_NichtMonetaer)
            {
                Assert.Equal("MEMO", s.TypDefinition);
                // TEXT ohne CHECK: eine Laengenpruefung schnitte den Freitext ab.
                Assert.Equal("TEXT", StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
            }
        }

        /// <summary>
        /// Der Sammel-Enumerator gibt beide Blöcke in Anlegereihenfolge heraus — erst die
        /// drei Preisspalten, dann den Freitext. Er ist die EINE Quelle, aus der sich
        /// Migration, Testdatenbankschema, Testdatenbank und dieser Nachweis bedienen.
        /// </summary>
        [Fact]
        public void Der_Sammel_Enumerator_fuehrt_beide_Bloecke_in_Anlegereihenfolge()
        {
            var namen = new List<string>();
            foreach (SchemaSpalte s in SchemaKatalog.Schritt72_ValeriErgaenzung) namen.Add(s.Name);

            Assert.Equal(SchemaKatalog.Schritt72_PreisInvestition.Length +
                         SchemaKatalog.Schritt72_NichtMonetaer.Length, namen.Count);
            Assert.Equal(SchemaKatalog.Schritt72_PreisInvestition[0].Name, namen[0]);
            Assert.Equal(SchemaKatalog.SPALTE_PW_NICHT_MONETAER, namen[namen.Count - 1]);
        }

        /// <summary>
        /// Die vier Spalten sind in der Testdatenbank angelegt (Schritt 72) — dieselbe
        /// Probe, die Schritt 71 für seine zwölf führt.
        /// </summary>
        [Fact]
        public void Die_vier_Spalten_stehen_in_der_Testdatenbank()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt72_ValeriErgaenzung)
                Assert.True(DataRepository.SpalteVorhanden(s.Tabelle, s.Name),
                            "Spalte fehlt: " + s.Tabelle + "." + s.Name);
        }

        /// <summary>
        /// Und sie nehmen an, was sie sollen: eine Zahl und einen Fließtext — und beide
        /// dürfen NULL bleiben. Das ist die Nullsemantik dieses Schritts, an der Teil 12b
        /// hängt: „nicht gepflegt" heißt bei p_I nicht 0 %, sondern „wie p_B".
        /// <para>Der Fall braucht eine Zeile in der Tabelle; gibt es keine, ist nichts zu
        /// zeigen und er endet still — dieselbe Zurückhaltung wie bei jeder anderen
        /// Datenbankprobe ohne Testträger.</para>
        /// </summary>
        [Fact]
        public void Die_Spalten_nehmen_Zahl_Text_und_NULL()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string tab = SchemaKatalog.TAB_PROJEKTWIRTSCHAFT;
            object anzahl = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM " + tab);
            if (anzahl == null || System.Convert.ToInt32(anzahl) == 0) return;

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE " + tab + " SET " + SchemaKatalog.SPALTE_PW_PREIS_I + " = 2.5, " +
                SchemaKatalog.SPALTE_PW_NICHT_MONETAER + " = 'Versorgungssicherheit, Laermschutz'"));
            Assert.Equal(2.5, System.Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT MAX(" + SchemaKatalog.SPALTE_PW_PREIS_I + ") FROM " + tab)), 9);
            Assert.Equal("Versorgungssicherheit, Laermschutz",
                         System.Convert.ToString(DataRepository.ExecuteScalar(
                             "SELECT MAX(" + SchemaKatalog.SPALTE_PW_NICHT_MONETAER + ") FROM " + tab)));

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE " + tab + " SET " + SchemaKatalog.SPALTE_PW_PREIS_I + " = NULL, " +
                SchemaKatalog.SPALTE_PW_NICHT_MONETAER + " = NULL"));
            Assert.Equal(0, System.Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(" + SchemaKatalog.SPALTE_PW_PREIS_I + ") FROM " + tab)));
        }
    }
}
