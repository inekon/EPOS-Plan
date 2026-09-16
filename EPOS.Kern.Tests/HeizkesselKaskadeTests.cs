using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>HK-E-1 — der Heizkessel kommt in die Kaskade</b> (Anwenderentscheid vom
    /// 15.09.2026: „Umsetzen", Vorgabe NACHRANGIG, Position waehlbar).
    ///
    /// <para>Gemessen wird an <see cref="KonfigurationCtrl"/>: Ein Heizkessel, den das
    /// Projekt fuehrt, bekommt seinen Platz beim Lesen der Konfiguration — am hinteren
    /// Ende, geschrieben, und nur EINMAL. Danach entscheidet allein der Anwender ueber
    /// die Reihenfolge.</para>
    ///
    /// <para><b>Eigene Arbeitskopie:</b> Diese Faelle SCHREIBEN in
    /// <c>Tab_Einstellungen</c>. Sie teilen sich deshalb keine Kopie mit einer Klasse,
    /// die dieselben Projekte liest.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class HeizkesselKaskadeTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public HeizkesselKaskadeTests(TestDatenbank db) { _db = db; }

        /// <summary>Kessel angelegt, Kaskade <c>('', Solarthermie, Waermepumpe, '')</c>.</summary>
        private const int PROJEKT_LUECKE = 1007;

        /// <summary>Kessel angelegt UND auf <c>Tool_2</c> — hier ist nichts nachzuziehen.</summary>
        private const int PROJEKT_BELEGT = 1040;

        /// <summary>Kein Heizkessel im Projekt (nur Waermepumpen und Puffer).</summary>
        private const int PROJEKT_OHNE_KESSEL = 1019;

        /// <summary>
        /// Fuehrt eine Solarthermieanlage OHNE Platz, hat den Kessel aber auf
        /// <c>Tool_2</c> — die Gegenprobe „nur der Heizkessel".
        /// </summary>
        private const int PROJEKT_SOLAR_OHNE_PLATZ = 1028;

        /// <summary>Die vier Waermeplaetze, wie sie in der DATENBANK stehen.</summary>
        private static List<string> PlaetzeAusDerDatenbank(int idProjekt)
        {
            var liste = new List<string>();
            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT Tool_1, Tool_2, Tool_3, Tool_4 FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                new DbParam("?", idProjekt));
            if (dt == null || dt.Rows.Count == 0) return liste;

            for (var i = 0; i < 4; i++)
            {
                object o = dt.Rows[0][i];
                liste.Add(o == System.DBNull.Value ? "" : o.ToString());
            }
            return liste;
        }

        /// <summary>
        /// Der Fall des Entscheids: Projekt 1007 fuehrt einen Heizkessel, seine Kaskade
        /// kennt ihn nicht. Das Lesen der Konfiguration zieht ihn nach — NACHRANGIG, also
        /// auf den ersten freien Platz hinter dem letzten belegten (hier
        /// <c>Tool_4</c>), ohne die drei anderen Plaetze anzuruehren.
        /// </summary>
        [Fact]
        public void Ein_Kessel_ohne_Platz_kommt_nachrangig_in_die_Kaskade()
        {
            if (!_db.Vorhanden) return;

            KonfigurationModel konfig = KonfigurationCtrl.LiesProjekt(PROJEKT_LUECKE);
            Assert.NotNull(konfig);

            List<string> plaetze = Kaskade.Lesen(konfig);
            Assert.Equal("", plaetze[0]);
            Assert.Equal(DbWerte.ERZEUGER_SOLARTHERMIE, plaetze[1]);
            Assert.Equal(DbWerte.ERZEUGER_WAERMEPUMPE, plaetze[2]);
            Assert.Equal(DbWerte.ERZEUGER_HEIZKESSEL, plaetze[3]);
        }

        /// <summary>
        /// Der Platz steht in den PROJEKTDATEN, nicht nur im Arbeitsspeicher: Der Lauf
        /// liest <c>Tool_1..4</c> an einer zweiten Stelle unmittelbar aus der Datenbank
        /// (<c>Ladeordnung.Kaskadenpositionen</c>). Stuende er nur im Modell, rechnete
        /// derselbe Lauf mit zwei verschiedenen Kaskaden.
        /// </summary>
        [Fact]
        public void Der_nachgezogene_Platz_steht_in_der_Datenbank()
        {
            if (!_db.Vorhanden) return;

            KonfigurationCtrl.LiesProjekt(PROJEKT_LUECKE);

            Assert.Contains(DbWerte.ERZEUGER_HEIZKESSEL, PlaetzeAusDerDatenbank(PROJEKT_LUECKE));

            // Die zweite Lesestelle sieht denselben Platz: Spalte Tool_4.
            Assert.Equal(4, Ladeordnung.Kaskadenpositionen(PROJEKT_LUECKE)[ProjektPuffer.TYP_KESSEL]);
        }

        /// <summary>
        /// <b>Die Wahl haelt.</b> Nach dem Nachziehen ordnet der Anwender um (dieselben
        /// Pfeile, die die Erzeugerkachel schon immer hatte); ein zweites Lesen greift
        /// nicht mehr ein, weil ein Platz den Heizkessel traegt. Sonst waere die Vorgabe
        /// keine Vorgabe, sondern ein Zwang.
        /// </summary>
        [Fact]
        public void Eine_gewaehlte_Reihenfolge_ueberschreibt_die_Vorgabe_dauerhaft()
        {
            if (!_db.Vorhanden) return;

            // Erstes Lesen: der Kessel landet hinten.
            KonfigurationModel konfig = KonfigurationCtrl.LiesProjekt(PROJEKT_LUECKE);
            Assert.Equal(DbWerte.ERZEUGER_HEIZKESSEL, Kaskade.Lesen(konfig)[3]);

            // Der Anwender zieht ihn nach vorn und speichert.
            Assert.True(Kaskade.Verschieben(konfig, DbWerte.ERZEUGER_HEIZKESSEL, -1));
            List<string> gewaehlt = Kaskade.Lesen(konfig);
            Assert.Equal(DbWerte.ERZEUGER_HEIZKESSEL, gewaehlt[2]);
            Assert.Equal(DbWerte.ERZEUGER_WAERMEPUMPE, gewaehlt[3]);
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Einstellungen SET Tool_1 = ?, Tool_2 = ?, Tool_3 = ?, Tool_4 = ? " +
                "WHERE ID_Projekt = ?",
                new DbParam("?", gewaehlt[0]), new DbParam("?", gewaehlt[1]),
                new DbParam("?", gewaehlt[2]), new DbParam("?", gewaehlt[3]),
                new DbParam("?", PROJEKT_LUECKE));

            // Zweites Lesen: die Automatik ruehrt die Wahl nicht an.
            Assert.Equal(gewaehlt, Kaskade.Lesen(KonfigurationCtrl.LiesProjekt(PROJEKT_LUECKE)));

            // Aufraeumen: der Ausgangsstand dieser Arbeitskopie.
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Einstellungen SET Tool_1 = ?, Tool_2 = ?, Tool_3 = ?, Tool_4 = ? " +
                "WHERE ID_Projekt = ?",
                new DbParam("?", ""), new DbParam("?", DbWerte.ERZEUGER_SOLARTHERMIE),
                new DbParam("?", DbWerte.ERZEUGER_WAERMEPUMPE), new DbParam("?", ""),
                new DbParam("?", PROJEKT_LUECKE));
        }

        /// <summary>
        /// Ein Projekt OHNE Heizkesselanlage bekommt keinen Platz — sonst stuende in der
        /// Kaskade ein Erzeuger, den es nicht gibt.
        /// </summary>
        [Fact]
        public void Ohne_Kesselanlage_bleibt_die_Kaskade_unberuehrt()
        {
            if (!_db.Vorhanden) return;

            KonfigurationModel konfig = KonfigurationCtrl.LiesProjekt(PROJEKT_OHNE_KESSEL);
            Assert.NotNull(konfig);

            Assert.DoesNotContain(DbWerte.ERZEUGER_HEIZKESSEL, Kaskade.Lesen(konfig));
            Assert.DoesNotContain(DbWerte.ERZEUGER_HEIZKESSEL, PlaetzeAusDerDatenbank(PROJEKT_OHNE_KESSEL));
        }

        /// <summary>
        /// Ein Projekt, dessen Kessel schon einen Platz hat, wird nicht angefasst —
        /// weder der Platz noch die Reihenfolge.
        /// </summary>
        [Fact]
        public void Ein_belegter_Platz_bleibt_stehen()
        {
            if (!_db.Vorhanden) return;

            List<string> vorher = PlaetzeAusDerDatenbank(PROJEKT_BELEGT);
            KonfigurationModel konfig = KonfigurationCtrl.LiesProjekt(PROJEKT_BELEGT);

            Assert.Equal(vorher, Kaskade.Lesen(konfig));
            Assert.Equal(vorher, PlaetzeAusDerDatenbank(PROJEKT_BELEGT));
        }

        /// <summary>
        /// <b>NUR der Heizkessel.</b> Der Entscheid nennt ihn allein; eine
        /// Solarthermieanlage ohne Platz bleibt drausssen und wird weiterhin nur
        /// gemeldet (#190).
        /// </summary>
        [Fact]
        public void Andere_Erzeugerarten_ohne_Platz_bleiben_draussen()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            KonfigurationModel konfig = KonfigurationCtrl.LiesProjekt(PROJEKT_SOLAR_OHNE_PLATZ);
            Assert.NotNull(konfig);
            Assert.DoesNotContain(DbWerte.ERZEUGER_SOLARTHERMIE, Kaskade.Lesen(konfig));

            var gemeldet = new List<string>();
            foreach (Warnbefund b in SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(
                         PROJEKT_SOLAR_OHNE_PLATZ, konfig))
                gemeldet.Add(b.Steuerwert);

            Assert.Contains(DbWerte.ERZEUGER_SOLARTHERMIE, gemeldet);
            Assert.DoesNotContain(DbWerte.ERZEUGER_HEIZKESSEL, gemeldet);
        }

        /// <summary>
        /// Kulturpinnung fuer die Faelle mit Ressourcentext — Hausregel des Kerns; die
        /// Klasse steht je Testklasse fuer sich (dasselbe Muster wie nebenan).
        /// </summary>
        private sealed class DeutscheOberflaeche : System.IDisposable
        {
            private readonly System.Globalization.CultureInfo _vorher =
                System.Threading.Thread.CurrentThread.CurrentUICulture;

            public DeutscheOberflaeche()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture =
                    new System.Globalization.CultureInfo("de-DE");
            }

            public void Dispose()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = _vorher;
            }
        }

        /// <summary>
        /// <b>Die Wirkung.</b> Der Lauf des Projekts 1007 rechnet den Kessel jetzt: Er
        /// deckt die Restwaerme, die vorher ungedeckt blieb.
        /// </summary>
        [Fact]
        public void Der_Lauf_rechnet_den_nachgezogenen_Kessel_mit()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(PROJEKT_LUECKE, out fehler), fehler);

            Assert.True(laeufer.sim.bSimulationKessel);
            Assert.Equal(0.0, laeufer.sim.RestwaermeMwh, 6);

            // Und die Luecke ist zu: der Lauf meldet keinen Kessel ohne Platz mehr.
            foreach (Warnbefund b in SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(
                         PROJEKT_LUECKE, laeufer.sim.tool))
                Assert.NotEqual(DbWerte.ERZEUGER_HEIZKESSEL, b.Steuerwert);
        }
    }
}
