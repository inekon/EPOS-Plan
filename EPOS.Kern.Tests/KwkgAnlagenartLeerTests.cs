using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7 — Schemaschritt <b>101</b>: Die leere Zeichenkette in
    /// <c>Tab_Energieanlagen.KWKG_Anlagenart</c> wird NULL (Konzept Wirtschaftlichkeit
    /// § 6.3 Nr. 30, Register R‑NR Nr. 30, Anwenderentscheid 22.09.2026).
    ///
    /// <para>Geprüft wird dreierlei: der ZIELSTAND, die sieben Anlagen der
    /// Testdatenbank, die der Schritt trifft (nach dem Nachziehen der Arbeitskopie
    /// NULL, ihr Eigenstromfall bleibt), und die Anweisung selbst an zwei Zeilen — sie
    /// trifft genau die leere Zeichenkette und ist wiederholbar.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KwkgAnlagenartLeerTests
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        /// <summary>
        /// Die sieben Anlagen der Testdatenbank mit leerer Anlagenart (Stand
        /// Schemastand 100): eine Wärmepumpe in Projekt 1032, in Projekt 1043 ein Kessel,
        /// drei Pufferspeicher und zwei Wärmepumpen. Keine ist ein BHKW.
        /// </summary>
        private static readonly int[] SIEBEN = { 12310, 14819, 14842, 14843, 14844, 14851, 14852 };

        [Fact]
        public void Der_Zielstand_ist_101_und_der_Schritt_trifft_genau_die_leere_Zeichenkette()
        {
            Assert.True(SchemaStand.Zielversion >= 101,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 101.");

            Assert.Equal("Tab_Energieanlagen", KwkgAnlagenartLeer.TABELLE);
            Assert.Equal("KWKG_Anlagenart", KwkgAnlagenartLeer.SPALTE);
            Assert.Equal("", KwkgAnlagenartLeer.LEER);
            Assert.Contains("SET [KWKG_Anlagenart] = NULL WHERE [KWKG_Anlagenart] = ?",
                            KwkgAnlagenartLeer.SQL_SETZEN, StringComparison.Ordinal);
        }

        /// <summary>
        /// Die Arbeitskopie ist nachgezogen (<see cref="TestDatenbank"/>): Keine Anlage
        /// trägt mehr eine leere Anlagenart, die sieben stehen auf NULL — und ihr
        /// Eigenstromfall, der in denselben Zeilen leer ist, bleibt, wie er ist: Der
        /// Entscheid nennt allein die Anlagenart.
        /// </summary>
        [Fact]
        public void Die_sieben_Anlagen_der_Testdatenbank_stehen_auf_NULL()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(0, KwkgAnlagenartLeer.Offen());
            Assert.Empty(KwkgAnlagenartLeer.Betroffene());

            foreach (int id in SIEBEN)
            {
                DataRow r = Zeile(id);
                Assert.NotNull(r);
                Assert.True(r["KWKG_Anlagenart"] == DBNull.Value,
                            "Anlage " + id + ": Anlagenart ist nicht NULL.");
                Assert.Equal("", Convert.ToString(r["KWKG_Eigenstromfall"]));
            }
        }

        /// <summary>
        /// Die Anweisung an zwei Zeilen: Die leere Zeichenkette wird NULL, eine gepflegte
        /// Anlagenart bleibt. Der zweite Lauf findet nichts mehr.
        /// </summary>
        [Fact]
        public void Der_Schritt_setzt_nur_die_leere_Zeichenkette_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            const int LEER = 12310, GEPFLEGT = 14819;
            Setze(LEER, "");
            Setze(GEPFLEGT, DbWerte.KWKG_ANLAGENART_NEU);

            Assert.Equal(1, KwkgAnlagenartLeer.Offen());
            List<string> betroffene = KwkgAnlagenartLeer.Betroffene();
            Assert.Single(betroffene);
            Assert.Equal("Id 12310, Projekt 1032: T 800-2", betroffene[0]);
            Assert.Single(KwkgAnlagenartLeer.Anweisungen);

            Assert.Equal(1, KwkgAnlagenartLeer.Ausfuehren());

            Assert.True(Zeile(LEER)["KWKG_Anlagenart"] == DBNull.Value);
            Assert.Equal(DbWerte.KWKG_ANLAGENART_NEU, Convert.ToString(Zeile(GEPFLEGT)["KWKG_Anlagenart"]));

            // WIEDERHOLBAR: nichts mehr offen, keine Anweisung, nichts gesetzt.
            Assert.Equal(0, KwkgAnlagenartLeer.Offen());
            Assert.Empty(KwkgAnlagenartLeer.Anweisungen);
            Assert.Equal(0, KwkgAnlagenartLeer.Ausfuehren());
            Assert.Equal(DbWerte.KWKG_ANLAGENART_NEU, Convert.ToString(Zeile(GEPFLEGT)["KWKG_Anlagenart"]));
        }

        private static DataRow Zeile(int id)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT [KWKG_Anlagenart], [KWKG_Eigenstromfall] FROM [Tab_Energieanlagen] WHERE [ID] = ?",
                new DbParam("@id", id));
            return t != null && t.Rows.Count == 1 ? t.Rows[0] : null;
        }

        private static void Setze(int id, string wert)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE [Tab_Energieanlagen] SET [KWKG_Anlagenart] = ? WHERE [ID] = ?",
                new DbParam("@wert", wert), new DbParam("@id", id));
        }
    }
}
