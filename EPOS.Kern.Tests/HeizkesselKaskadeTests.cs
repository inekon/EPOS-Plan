using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Seiten.Simulation;
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

        // =================================================================
        // DIE MERKSPALTE: die Kaskade, die der Anwender gepflegt hat
        // (Anwenderentscheid vom 16.09.2026, Schemaschritt 82)
        // =================================================================

        /// <summary>Setzt die Merkspalte und gibt den vorherigen Stand zurueck.</summary>
        private static bool GepflegtSetzen(int idProjekt, bool wert)
        {
            bool vorher = KonfigurationCtrl.KaskadeGepflegtLesen(idProjekt);
            Assert.True(KonfigurationCtrl.KaskadeGepflegtSchreiben(idProjekt, wert));
            return vorher;
        }

        /// <summary>Schreibt die vier Waermeplaetze eines Projekts unmittelbar.</summary>
        private static void PlaetzeSchreiben(int idProjekt, string t1, string t2, string t3, string t4)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Einstellungen SET Tool_1 = ?, Tool_2 = ?, Tool_3 = ?, Tool_4 = ? " +
                "WHERE ID_Projekt = ?",
                new DbParam("?", t1), new DbParam("?", t2), new DbParam("?", t3),
                new DbParam("?", t4), new DbParam("?", idProjekt));
        }

        /// <summary>
        /// <b>Der Fall, den es bisher nicht gab.</b> Der Anwender nimmt den Heizkessel
        /// mit „×" aus der Kaskade; damit ist sie GEPFLEGT. Das naechste Lesen der
        /// Konfiguration zieht ihn nicht mehr nach — er bleibt draussen, obwohl das
        /// Projekt die Kesselanlage weiterhin fuehrt.
        /// </summary>
        [Fact]
        public void Ein_entfernter_Kessel_bleibt_draussen()
        {
            if (!_db.Vorhanden) return;

            // Ausgangslage: der Kessel steht auf Tool_4 (die Automatik hat ihn geholt).
            KonfigurationModel konfig = KonfigurationCtrl.LiesProjekt(PROJEKT_LUECKE);
            Assert.Equal(DbWerte.ERZEUGER_HEIZKESSEL, Kaskade.Lesen(konfig)[3]);

            // Der Handgriff des Anwenders: entfernen. Die Huelle schreibt beides weg -
            // den leeren Platz und die Marke.
            Assert.True(Kaskade.Entfernen(konfig, DbWerte.ERZEUGER_HEIZKESSEL));
            List<string> ohneKessel = Kaskade.Lesen(konfig);
            PlaetzeSchreiben(PROJEKT_LUECKE, ohneKessel[0], ohneKessel[1], ohneKessel[2], ohneKessel[3]);
            bool vorher = GepflegtSetzen(PROJEKT_LUECKE, true);

            try
            {
                // Das naechste Lesen laesst ihn draussen - in Modell UND Datenbank.
                KonfigurationModel zweiteLesung = KonfigurationCtrl.LiesProjekt(PROJEKT_LUECKE);
                Assert.True(zweiteLesung.Kaskade_Gepflegt);
                Assert.DoesNotContain(DbWerte.ERZEUGER_HEIZKESSEL, Kaskade.Lesen(zweiteLesung));
                Assert.DoesNotContain(DbWerte.ERZEUGER_HEIZKESSEL, PlaetzeAusDerDatenbank(PROJEKT_LUECKE));
            }
            finally
            {
                // Aufraeumen: der Ausgangsstand dieser Arbeitskopie.
                GepflegtSetzen(PROJEKT_LUECKE, vorher);
                PlaetzeSchreiben(PROJEKT_LUECKE, "", DbWerte.ERZEUGER_SOLARTHERMIE,
                                 DbWerte.ERZEUGER_WAERMEPUMPE, "");
            }
        }

        /// <summary>
        /// <b>Auch eine NEUE Kesselanlage holt die Automatik nicht zurueck.</b> Das ist
        /// die bewusste Folge des Entscheids: Wer die Kaskade einmal von Hand angefasst
        /// hat, pflegt sie ab dann selbst. Gemessen an derselben Bedingung, die die
        /// Automatik sonst ausloest — das Projekt fuehrt eine Kesselanlage, die Kaskade
        /// fuehrt keinen Kessel.
        /// </summary>
        [Fact]
        public void Eine_gepflegte_Kaskade_bleibt_auch_ohne_jeden_Platz_unberuehrt()
        {
            if (!_db.Vorhanden) return;

            bool vorher = GepflegtSetzen(PROJEKT_LUECKE, true);
            PlaetzeSchreiben(PROJEKT_LUECKE, "", "", "", "");

            try
            {
                KonfigurationModel konfig = KonfigurationCtrl.LiesProjekt(PROJEKT_LUECKE);
                Assert.Equal(new List<string> { "", "", "", "" }, Kaskade.Lesen(konfig));
                Assert.Equal(new List<string> { "", "", "", "" }, PlaetzeAusDerDatenbank(PROJEKT_LUECKE));
            }
            finally
            {
                GepflegtSetzen(PROJEKT_LUECKE, vorher);
                PlaetzeSchreiben(PROJEKT_LUECKE, "", DbWerte.ERZEUGER_SOLARTHERMIE,
                                 DbWerte.ERZEUGER_WAERMEPUMPE, "");
            }
        }

        /// <summary>
        /// <b>Die Gegenprobe.</b> Bei <c>Kaskade_Gepflegt = 0</c> greift die Automatik
        /// unveraendert — genau das haelt die dreizehn Referenzprojekte byte-gleich, denn
        /// im ganzen Bestand steht dort 0.
        /// </summary>
        [Fact]
        public void Ohne_Marke_greift_die_Automatik_weiterhin()
        {
            if (!_db.Vorhanden) return;

            bool vorher = GepflegtSetzen(PROJEKT_LUECKE, false);
            PlaetzeSchreiben(PROJEKT_LUECKE, "", DbWerte.ERZEUGER_SOLARTHERMIE,
                             DbWerte.ERZEUGER_WAERMEPUMPE, "");

            try
            {
                KonfigurationModel konfig = KonfigurationCtrl.LiesProjekt(PROJEKT_LUECKE);
                Assert.False(konfig.Kaskade_Gepflegt);
                Assert.Equal(DbWerte.ERZEUGER_HEIZKESSEL, Kaskade.Lesen(konfig)[3]);
                Assert.Contains(DbWerte.ERZEUGER_HEIZKESSEL, PlaetzeAusDerDatenbank(PROJEKT_LUECKE));
            }
            finally
            {
                GepflegtSetzen(PROJEKT_LUECKE, vorher);
                PlaetzeSchreiben(PROJEKT_LUECKE, "", DbWerte.ERZEUGER_SOLARTHERMIE,
                                 DbWerte.ERZEUGER_WAERMEPUMPE, "");
            }
        }

        /// <summary>
        /// <b>Alle dreizehn Referenzprojekte stehen auf 0.</b> Dort ordnet niemand von
        /// Hand um — deshalb bleibt die Basis R8 gueltig, und deshalb darf dieser
        /// Schemaschritt kein Rechenergebnis verschieben.
        /// </summary>
        [Fact]
        public void Kein_Projekt_der_Testdatenbank_traegt_die_Marke()
        {
            if (!_db.Vorhanden) return;

            object zahl = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Einstellungen WHERE [" +
                SchemaKatalog.SPALTE_KASKADE_GEPFLEGT + "] <> 0");

            Assert.Equal(0, System.Convert.ToInt32(zahl));
        }

        /// <summary>
        /// <b>Ein Projekt OHNE Kesselanlage bleibt unberuehrt — mit und ohne Marke.</b>
        /// Die Marke ist eine Sperre, kein Ausloeser: Sie legt nie einen Platz an.
        /// </summary>
        [Fact]
        public void Ohne_Kesselanlage_aendert_die_Marke_nichts()
        {
            if (!_db.Vorhanden) return;

            List<string> ausgangslage = PlaetzeAusDerDatenbank(PROJEKT_OHNE_KESSEL);
            bool vorher = GepflegtSetzen(PROJEKT_OHNE_KESSEL, true);

            try
            {
                KonfigurationModel konfig = KonfigurationCtrl.LiesProjekt(PROJEKT_OHNE_KESSEL);
                Assert.DoesNotContain(DbWerte.ERZEUGER_HEIZKESSEL, Kaskade.Lesen(konfig));
                Assert.Equal(ausgangslage, PlaetzeAusDerDatenbank(PROJEKT_OHNE_KESSEL));
            }
            finally
            {
                GepflegtSetzen(PROJEKT_OHNE_KESSEL, vorher);
            }
        }

        /// <summary>
        /// <b>Auch das VERSCHIEBEN macht die Kaskade zu einer gepflegten.</b> Gemessen an
        /// dem, was die Simulationskonfiguration tut: erst der Handgriff
        /// (<see cref="Kaskade.Verschieben"/>), dann die Marke in die Datenbank. Danach
        /// haelt die Reihenfolge auch dann, wenn der Kessel spaeter einmal keinen Platz
        /// mehr haette.
        /// </summary>
        [Fact]
        public void Verschieben_macht_die_Kaskade_zu_einer_gepflegten()
        {
            if (!_db.Vorhanden) return;

            KonfigurationModel konfig = KonfigurationCtrl.LiesProjekt(PROJEKT_LUECKE);
            Assert.Equal(DbWerte.ERZEUGER_HEIZKESSEL, Kaskade.Lesen(konfig)[3]);

            bool vorher = KonfigurationCtrl.KaskadeGepflegtLesen(PROJEKT_LUECKE);

            try
            {
                // Der Handgriff: der Kessel rueckt einen Rang nach vorn.
                Assert.True(Kaskade.Verschieben(konfig, DbWerte.ERZEUGER_HEIZKESSEL, -1));
                List<string> gewaehlt = Kaskade.Lesen(konfig);
                PlaetzeSchreiben(PROJEKT_LUECKE, gewaehlt[0], gewaehlt[1], gewaehlt[2], gewaehlt[3]);
                Assert.True(KonfigurationCtrl.KaskadeGepflegtSchreiben(PROJEKT_LUECKE, true));

                // Die Marke steht in der Datenbank und kommt beim Lesen zurueck.
                Assert.True(KonfigurationCtrl.KaskadeGepflegtLesen(PROJEKT_LUECKE));
                KonfigurationModel zweiteLesung = KonfigurationCtrl.LiesProjekt(PROJEKT_LUECKE);
                Assert.True(zweiteLesung.Kaskade_Gepflegt);
                Assert.Equal(gewaehlt, Kaskade.Lesen(zweiteLesung));
            }
            finally
            {
                GepflegtSetzen(PROJEKT_LUECKE, vorher);
                PlaetzeSchreiben(PROJEKT_LUECKE, "", DbWerte.ERZEUGER_SOLARTHERMIE,
                                 DbWerte.ERZEUGER_WAERMEPUMPE, "");
            }
        }

        /// <summary>
        /// <b>#307 — die gepflegte Kaskade ist umkehrbar.</b> Der Handgriff
        /// „Automatik wieder uebernehmen" der Simulationskonfiguration setzt die
        /// Merkspalte auf 0 — in Modell UND Datenbank —, laesst die Kaskade selbst
        /// aber unberuehrt. Beim naechsten Lesen der Konfiguration greifen
        /// Nachziehen (Heizkessel) und Vorwahl Ae15 (alle uebrigen Erzeugerarten)
        /// wieder, und was dann hineinkommt, sieht der Anwender an Ort und Stelle.
        /// </summary>
        [Fact]
        public void Der_Rueckweg_gibt_Nachziehen_und_Vorwahl_wieder_frei()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            bool vorher = GepflegtSetzen(PROJEKT_LUECKE, true);
            PlaetzeSchreiben(PROJEKT_LUECKE, "", "", "", "");

            try
            {
                // 1) Gepflegt: weder Nachziehen noch Vorwahl fassen etwas an.
                SimulationKonfigDienste dienste = DiensteDerHuelle(PROJEKT_LUECKE);
                SimulationKonfigDaten daten = dienste.Laden(PROJEKT_LUECKE);
                Assert.True(daten.KaskadeGepflegt);
                Assert.Empty(Aufgenommen(daten));

                // 2) Der Handgriff: die Marke faellt, die Kaskade bleibt leer.
                Assert.NotNull(dienste.AutomatikUebernehmen);
                dienste.AutomatikUebernehmen();

                Assert.False(KonfigurationCtrl.KaskadeGepflegtLesen(PROJEKT_LUECKE));
                Assert.False(dienste.Laden(PROJEKT_LUECKE).KaskadeGepflegt);
                Assert.Equal(new List<string> { "", "", "", "" },
                             PlaetzeAusDerDatenbank(PROJEKT_LUECKE));

                // 3) Das naechste Lesen: beide Automatiken greifen wieder.
                SimulationKonfigDaten danach = DiensteDerHuelle(PROJEKT_LUECKE)
                                                   .Laden(PROJEKT_LUECKE);
                Assert.False(danach.KaskadeGepflegt);

                List<string> drin = Aufgenommen(danach);
                Assert.Contains(DbWerte.ERZEUGER_HEIZKESSEL, drin);   // Nachziehen
                Assert.Contains(DbWerte.ERZEUGER_WAERMEPUMPE, drin);  // Vorwahl Ae15
            }
            finally
            {
                GepflegtSetzen(PROJEKT_LUECKE, vorher);
                PlaetzeSchreiben(PROJEKT_LUECKE, "", DbWerte.ERZEUGER_SOLARTHERMIE,
                                 DbWerte.ERZEUGER_WAERMEPUMPE, "");
            }
        }

        /// <summary>Die Datenseite der Simulationskonfiguration zu einem Projekt.</summary>
        private static SimulationKonfigDienste DiensteDerHuelle(int idProjekt)
            => (SimulationKonfigDienste)SimulationKonfigHuelle.Erzeugen(idProjekt)
                                                              .Gaben()["Dienste"];

        /// <summary>
        /// Die Waermeerzeuger, die die Seite als AUFGENOMMEN zeigt — also die Belegung
        /// der Kaskade, wie sie der Anwender sieht.
        /// </summary>
        private static List<string> Aufgenommen(SimulationKonfigDaten daten)
            => daten.Gruppen[0].Zeilen.Where(z => !z.Verfuegbar)
                                      .Select(z => z.DbWert).Distinct().ToList();

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
