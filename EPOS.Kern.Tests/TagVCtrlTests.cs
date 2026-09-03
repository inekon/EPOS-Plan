using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="TagVCtrl"/> nach iU9-W8.0d — die Gebaeudetyp-Verwaltung, die bis dahin
    /// vollstaendig in <c>Form_EingGebTyp</c> stand.
    ///
    /// <para>Der Gebaeudetyp ist das einzige Kopf-Detail-Modell dieser Welle: Ein Kopf in
    /// <c>Tab_DBTagV_STAMM</c>, 24 Datenzeilen je Tageskurve in
    /// <c>Tab_DBTagVDaten_STAMM</c>, und die REIHENFOLGE der Zeilen ist die Zuordnung zur
    /// Kurve — eine Kurvennummer fuehrt die Tabelle nicht. Genau das prueft
    /// <see cref="Anlegen_legt_Kopf_und_192_Datenzeilen_an"/>.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class TagVCtrlTests
    {
        // ================================================================= KurvenNamen

        /// <summary>
        /// Entschieden wird ueber die KURVENZAHL, nicht ueber die Listenposition — die
        /// Typliste ist alphabetisch sortiert, und der 5-Kurven-Typ steht nicht immer vorn
        /// (Kommentar <c>GetTagVName</c>:108).
        /// </summary>
        [Fact]
        public void KurvenNamen_liefert_bei_fuenf_Kurven_die_kurze_Liste()
        {
            List<string> namen = TagVCtrl.KurvenNamen(5);

            Assert.Equal(5, namen.Count);
            Assert.Equal("Winter-heiter", namen[0]);
            Assert.Equal("Sommertag", namen[4]);
        }

        [Fact]
        public void KurvenNamen_liefert_bei_acht_Kurven_die_lange_Liste()
        {
            List<string> namen = TagVCtrl.KurvenNamen(8);

            Assert.Equal(8, namen.Count);
            Assert.Equal("Winter-Wochentag", namen[0]);
            Assert.Equal("Übergang2-Wochenende", namen[7]);
        }

        /// <summary>
        /// Vier Kurven sind weniger als fuenf und nehmen deshalb ebenfalls die kurze Liste
        /// — der Vorlaeufer verglich <c>&lt;=</c>, nicht <c>==</c>.
        /// </summary>
        [Fact]
        public void KurvenNamen_nimmt_unterhalb_von_fuenf_ebenfalls_die_kurze_Liste()
        {
            Assert.Equal(new[] { "Winter-heiter", "Winter-trübe", "Übergang-heiter", "Übergang-trübe" },
                         TagVCtrl.KurvenNamen(4));
        }

        // ================================================================= Datenbank

        [Fact]
        public void Typen_liefert_die_Gebaeudetypen_alphabetisch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<string> typen = TagVCtrl.Typen();
            Assert.NotEmpty(typen);
            Assert.Equal(typen.OrderBy(t => t, StringComparer.Ordinal).ToList(), typen);
        }

        [Fact]
        public void Lies_liefert_Kopf_Kurvenzahl_und_Verteilung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (string name in TagVCtrl.Typen())
            {
                var gelesen = TagVCtrl.Lies(name);
                Assert.NotNull(gelesen);
                Assert.Equal(name, gelesen.Value.Kopf.Name);
                Assert.Equal(gelesen.Value.Kurven, gelesen.Value.Verteilung.GetLength(0));
                Assert.Equal(24, gelesen.Value.Verteilung.GetLength(1));

                // Fuenf oder acht Kurven - eine dritte Groesse kennt die Maske nicht.
                Assert.True(gelesen.Value.Kurven == 5 || gelesen.Value.Kurven == 8,
                            name + " hat " + gelesen.Value.Kurven + " Kurven");
            }

            Assert.Null(TagVCtrl.Lies("gibt-es-nicht-" + Guid.NewGuid()));
        }

        [Fact]
        public void Anlegen_legt_Kopf_und_192_Datenzeilen_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string name = "W8-Gebtyp-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            int id = TagVCtrl.Anlegen(name, "Probe");
            Assert.True(id > 0);

            object anzahl = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_DBTagVDaten_STAMM WHERE ID_TagV = ?", new DbParam("@id", id));
            Assert.Equal(192, Convert.ToInt32(anzahl));

            var gelesen = TagVCtrl.Lies(name);
            Assert.NotNull(gelesen);
            Assert.Equal(8, gelesen.Value.Kurven);            // 192 / 24
            Assert.Equal("Probe", gelesen.Value.Kopf.Beschreibung);
            Assert.True(gelesen.Value.Kopf.Veraenderbar);
            Assert.False(gelesen.Value.Kopf.ReadOnly);
            Assert.Equal(0.0, gelesen.Value.Verteilung[3, 11], 6);
        }

        [Fact]
        public void Speichern_schreibt_die_Verteilung_zeilenweise_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string name = "W8-Gebtyp-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            int id = TagVCtrl.Anlegen(name, "");
            Assert.True(id > 0);

            var verteilung = new double[8, 24];
            for (int n = 0; n < 8; n++)
                for (int i = 0; i < 24; i++) verteilung[n, i] = n * 24 + i + 1;

            Assert.True(TagVCtrl.Speichern(id, verteilung));

            var zurueck = TagVCtrl.Lies(name);
            Assert.NotNull(zurueck);
            Assert.Equal(1.0, zurueck.Value.Verteilung[0, 0], 6);
            Assert.Equal(192.0, zurueck.Value.Verteilung[7, 23], 6);
            Assert.Equal(90.0, zurueck.Value.Verteilung[3, 17], 6);
        }

        /// <summary>Detail vor Kopf — sonst bleiben Waisen in der Detailtabelle stehen.</summary>
        [Fact]
        public void Loeschen_entfernt_Kopf_und_Datenzeilen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string name = "W8-Gebtyp-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            int id = TagVCtrl.Anlegen(name, "");
            Assert.True(id > 0);

            Assert.True(TagVCtrl.Loeschen(id));

            Assert.Null(TagVCtrl.Lies(name));
            object rest = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_DBTagVDaten_STAMM WHERE ID_TagV = ?", new DbParam("@id", id));
            Assert.Equal(0, Convert.ToInt32(rest));
        }
    }
}
