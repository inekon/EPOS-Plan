using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// DER KOPIERWEG DER WIRTSCHAFTLICHKEITSZEILEN (Anwenderentscheid 23.09.2026, Punkt 3)
    /// und Schemaschritt <b>106</b>.
    ///
    /// <para><b>Der Befund.</b> <c>Tab_ErgebnisWirtschaftlichkeit.ID_Ergebnis</c> nennt den
    /// Simulationslauf, auf dem eine gespeicherte Wirtschaftlichkeit beruht. Der generische
    /// Kopierlauf (<see cref="ProjektDuplizierenCtrl"/>, auch der Weg jeder Variante) übernahm
    /// die Spalte UNVERSETZT — ohne deklarierte Beziehung und ohne Eintrag in seinen
    /// Zuordnungen —, und die Kopie zeigte auf den Lauf des Quellprojekts. In der
    /// Testdatenbank 21 Zeilen: 1028, 1029, 1040, 1041 auf Lauf 167 (Projekt 1026), 1043 und
    /// 1044 auf Lauf 206 (Projekt 1042).</para>
    ///
    /// <para><b>Die Regel.</b> Ergebnisverweise werden nicht mitkopiert: Die Zeilen der
    /// Kopie stehen mit leerem Verweis da und gelten als „passt nicht zum Simulationsstand";
    /// die Wirtschaftlichkeit rechnet nach dem ersten Lauf neu. Schritt 106 bereinigt den
    /// Bestand mit derselben Regel.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — die Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErgebnisverweisKopieTests
    {
        /// <summary>Das Quellprojekt „Beispiel WP WG 1" mit eigenem Lauf 167 und drei Wirtschaftlichkeitszeilen.</summary>
        private const int QUELLE = 1026;
        private const string QUELLNAME = "Beispiel WP WG 1";
        private const int LAUF_QUELLE = 167;

        /// <summary>Die 21 Zeilen der Testdatenbank mit fremdem Verweis (vor Schritt 106).</summary>
        private static readonly int[] FREMD = { 16, 18, 20, 21, 23, 25, 189, 191, 193, 194, 196, 198,
                                                213, 214, 215, 216, 217, 218, 219, 220, 221 };

        // =================================================================================
        // 1 — Duplizieren und Variante
        // =================================================================================

        /// <summary>
        /// DUPLIZIEREN: Die Kopie trägt dieselben Wirtschaftlichkeitszeilen wie die Quelle,
        /// aber ohne Ergebnisverweis — und keine Zeile der Datenbank zeigt danach auf den
        /// Lauf eines fremden Projekts. Die Quelle behält ihren Verweis.
        /// </summary>
        [Fact]
        public void Die_Kopie_traegt_keinen_Ergebnisverweis_der_Quelle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<int?> quelleVorher = Verweise(QUELLE);
            Assert.Equal(3, quelleVorher.Count);
            Assert.All(quelleVorher, v => Assert.Equal(LAUF_QUELLE, v));

            int kopie = new ProjektDuplizierenCtrl().Duplizieren(QUELLNAME, "Verweisprobe Kopie");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");

            List<int?> kopieVerweise = Verweise(kopie);
            Assert.Equal(3, kopieVerweise.Count);                  // die Zeilen kommen mit
            Assert.All(kopieVerweise, v => Assert.Null(v));        // ihr Verweis nicht

            Assert.Equal(quelleVorher, Verweise(QUELLE));
            Assert.Equal(0, WirtschaftlichkeitFremdverweis.Offen());

            // Die Kopie gilt als "passt nicht zum Simulationsstand" - sie rechnet nach dem
            // ersten Lauf neu.
            var ctrl = new WirtschaftlichkeitCtrl();
            Assert.All(ctrl.LadeErgebnisse(new List<int> { kopie }),
                       e => Assert.False(ctrl.ErgebnisAktuell(e)));
        }

        /// <summary>VARIANTE: derselbe Kopierlauf, dieselbe Regel.</summary>
        [Fact]
        public void Die_Variante_traegt_keinen_Ergebnisverweis_des_Stamms()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int variante = new VariantenCtrl().AnlegenAusStamm(QUELLE, QUELLNAME, "Verweisprobe",
                                                               out string fehler);
            Assert.True(variante > 0, fehler);

            List<int?> verweise = Verweise(variante);
            Assert.Equal(3, verweise.Count);
            Assert.All(verweise, v => Assert.Null(v));
            Assert.Equal(0, WirtschaftlichkeitFremdverweis.Offen());
        }

        /// <summary>
        /// Nur der Wirtschaftlichkeitsverweis wird geleert — die Verweise der
        /// Ergebnis-Detailtabellen sind deklarierte Beziehungen und ziehen auf die Kopie mit.
        /// </summary>
        [Fact]
        public void Geleert_wird_nur_der_Verweis_der_Wirtschaftlichkeit()
        {
            Assert.True(ProjektDuplizierenCtrl.ErgebnisverweisLeeren("Tab_ErgebnisWirtschaftlichkeit", "ID_Ergebnis"));
            Assert.True(ProjektDuplizierenCtrl.ErgebnisverweisLeeren("tab_ergebniswirtschaftlichkeit", "id_ergebnis"));
            Assert.False(ProjektDuplizierenCtrl.ErgebnisverweisLeeren("Tab_ErgebnisBHKW", "ID_Ergebnis"));
            Assert.False(ProjektDuplizierenCtrl.ErgebnisverweisLeeren("Tab_ErgebnisWirtschaftlichkeit", "ID_Projekt"));
            Assert.False(ProjektDuplizierenCtrl.ErgebnisverweisLeeren(null, "ID_Ergebnis"));
        }

        // =================================================================================
        // 2 — Schemaschritt 106
        // =================================================================================

        /// <summary>
        /// Der Zielstand ist 106, und die Anweisung trifft genau einen gesetzten Verweis ohne
        /// Lauf desselben Projekts.
        /// </summary>
        [Fact]
        public void Der_Zielstand_ist_106_und_der_Schritt_trifft_nur_fremde_Verweise()
        {
            Assert.True(SchemaStand.Zielversion >= 106,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 106.");
            Assert.Equal("Tab_ErgebnisWirtschaftlichkeit", WirtschaftlichkeitFremdverweis.TABELLE);
            Assert.Equal("ID_Ergebnis", WirtschaftlichkeitFremdverweis.SPALTE);
            Assert.StartsWith("UPDATE [Tab_ErgebnisWirtschaftlichkeit] SET [ID_Ergebnis] = NULL WHERE [ID_Ergebnis] > 0 AND NOT EXISTS",
                              WirtschaftlichkeitFremdverweis.SQL_SETZEN, StringComparison.Ordinal);
            Assert.Contains("e.[ID_Projekt] = [Tab_ErgebnisWirtschaftlichkeit].[ID_Projekt]",
                            WirtschaftlichkeitFremdverweis.SQL_SETZEN, StringComparison.Ordinal);
        }

        /// <summary>
        /// Die Arbeitskopie ist nachgezogen (<see cref="TestDatenbank"/>): Die 21 Zeilen
        /// stehen ohne Verweis da, die Zeilen mit eigenem Lauf behalten ihn — 1026 auf 167,
        /// 1042 auf 206, 1030 auf 212 hat keine, 1027 auf 168.
        /// </summary>
        [Fact]
        public void Die_21_Zeilen_der_Testdatenbank_stehen_ohne_fremden_Verweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, WirtschaftlichkeitFremdverweis.Offen());
            Assert.Empty(WirtschaftlichkeitFremdverweis.Betroffene());

            foreach (int id in FREMD)
                Assert.True(Verweis(id) == null, "Zeile " + id + ": Verweis ist nicht NULL.");

            Assert.All(Verweise(QUELLE), v => Assert.Equal(LAUF_QUELLE, v));
            Assert.All(Verweise(1042), v => Assert.Equal(206, v));
            Assert.All(Verweise(1027), v => Assert.Equal(168, v));
        }

        /// <summary>
        /// Die Anweisung an drei Zeilen: ein Verweis auf den Lauf eines fremden Projekts und
        /// einer auf einen Lauf, den es nicht gibt, werden NULL; der eigene bleibt. Der zweite
        /// Lauf findet nichts mehr.
        /// </summary>
        [Fact]
        public void Der_Schritt_leert_fremde_Verweise_und_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int FREMD_ZEILE = 16, VERWAIST_ZEILE = 18, EIGEN_ZEILE = 213;   // 1028, 1028, 1043
            Setze(FREMD_ZEILE, LAUF_QUELLE);   // Projekt 1028 -> Lauf von 1026
            Setze(VERWAIST_ZEILE, 999999);     // Projekt 1028 -> kein Lauf
            Setze(EIGEN_ZEILE, 209);           // Projekt 1043 -> eigener Lauf 209

            Assert.Equal(2, WirtschaftlichkeitFremdverweis.Offen());
            List<string> betroffene = WirtschaftlichkeitFremdverweis.Betroffene();
            Assert.Equal(new[] { "Id 16, Projekt 1028: Lauf 167 (Erwartet)",
                                 "Id 18, Projekt 1028: Lauf 999999 (Best)" }, betroffene);
            Assert.Single(WirtschaftlichkeitFremdverweis.Anweisungen);

            Assert.Equal(2, WirtschaftlichkeitFremdverweis.Ausfuehren());

            Assert.Null(Verweis(FREMD_ZEILE));
            Assert.Null(Verweis(VERWAIST_ZEILE));
            Assert.Equal(209, Verweis(EIGEN_ZEILE));

            // WIEDERHOLBAR: nichts mehr offen, keine Anweisung, nichts gesetzt.
            Assert.Equal(0, WirtschaftlichkeitFremdverweis.Offen());
            Assert.Empty(WirtschaftlichkeitFremdverweis.Anweisungen);
            Assert.Equal(0, WirtschaftlichkeitFremdverweis.Ausfuehren());
            Assert.Equal(209, Verweis(EIGEN_ZEILE));
        }

        /// <summary>Ohne die (lazy angelegte) Tabelle tut der Schritt nichts.</summary>
        [Fact]
        public void Ohne_Tabelle_tut_der_Schritt_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(DataRepository.ExecuteSQL("DROP TABLE [Tab_ErgebnisWirtschaftlichkeit]"));

            Assert.False(WirtschaftlichkeitFremdverweis.Vorhanden());
            Assert.Equal(0, WirtschaftlichkeitFremdverweis.Offen());
            Assert.Empty(WirtschaftlichkeitFremdverweis.Betroffene());
            Assert.Empty(WirtschaftlichkeitFremdverweis.Anweisungen);
            Assert.Equal(0, WirtschaftlichkeitFremdverweis.Ausfuehren());
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static List<int?> Verweise(int projekt)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT [ID_Ergebnis] FROM [Tab_ErgebnisWirtschaftlichkeit] WHERE [ID_Projekt] = ? ORDER BY [ID]",
                new DbParam("@p", projekt));
            var liste = new List<int?>();
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
                liste.Add(r[0] == DBNull.Value ? (int?)null : Convert.ToInt32(r[0], CultureInfo.InvariantCulture));
            return liste;
        }

        private static int? Verweis(int zeile)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT [ID_Ergebnis] FROM [Tab_ErgebnisWirtschaftlichkeit] WHERE [ID] = ?",
                new DbParam("@id", zeile));
            return o == null || o == DBNull.Value ? (int?)null : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static void Setze(int zeile, int lauf)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE [Tab_ErgebnisWirtschaftlichkeit] SET [ID_Ergebnis] = ? WHERE [ID] = ?",
                new DbParam("@lauf", lauf), new DbParam("@id", zeile)));
        }
    }
}
