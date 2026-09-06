using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis zum Anwenderentscheid W6‑E‑4</b> vom 06.09.2026: „Die Vor- und
    /// Rücklauftemperatur sollen beim Anlegen der Komponenten und Energieerzeuger die
    /// Vor- und Rücklauftemperatur aus dem Katalog übernommen werden. Diese können dann
    /// vom Benutzer für das Projekt geändert werden."
    ///
    /// <para><b>Was hier geprüft wird.</b> Die drei Wege von
    /// <see cref="AnlagenTemperaturen"/> (Stammsatz, Gerätekopie, Kennlinienstufe) und
    /// die Regel, an der beide Hälften des Entscheids hängen: Ein fehlendes Paar wird
    /// ergänzt, ein VORHANDENES vollständiges Paar wird nie überschrieben. Die Fälle 4
    /// und 5 fahren dafür den EINEN Schreibweg aller Anlagen
    /// (<c>WizardCtrl.Add_WP_Waermeerzeuger</c>) bis in die Datenbank.</para>
    ///
    /// <para><b>Was hier NICHT geprüft wird: ein Rechenergebnis.</b> Der Leseweg der
    /// Engine ist unberührt — nur der Schreibweg beim Anlegen ändert sich, und der
    /// Referenzlauf schreibt nicht. Er bleibt byte-gleich.</para>
    ///
    /// <para><b>Eine Arbeitskopie je Klasse</b> (Regel seit iU9‑W11a); fehlt die Datei,
    /// schweigen die Fälle. Die Klasse trägt <c>[Collection("Testdatenbank")]</c>, weil
    /// <c>DataRepository.PfadUeberschreibung</c> statisch ist. Jeder Fall arbeitet mit
    /// EIGENEN Wegwerfsätzen und räumt sie hinterher weg — zwei Fälle mit demselben
    /// Katalognamen liefen sonst in die Geräte-Dublettenfrage von
    /// <c>AnlagenEindeutigkeit</c>.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AnlagenTemperaturenTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public AnlagenTemperaturenTests(TestDatenbank db) { _db = db; }

        /// <summary>Das Projekt der Testdatenbank, in dem die Wegwerf-Anlagen entstehen.</summary>
        private const int TESTPROJEKT = 1030;

        // =================================================================================
        // 1-3 — Die Vorbelegung aus dem Stammsatz (der Weg der drei Hüllen)
        // =================================================================================

        /// <summary>
        /// <b>Fall 1</b>: Ein Feldsatz ohne Paar bekommt das Paar des Katalogsatzes —
        /// genau das, was <c>BhkwHuelle.Aufnehmen</c> bisher als eigene Zeile
        /// <c>Vorlauf = stamm.m_Vorlauf</c> tat.
        /// </summary>
        [Fact]
        public void Ein_Feldsatz_ohne_Paar_bekommt_das_Paar_des_Stammsatzes()
        {
            if (!_db.Vorhanden) return;

            int stamm = BhkwStammAnlegen("W6E4 Katalogpaar", 85, 65);
            try
            {
                var item = new WErzeugerModel { ID_Type = WizardItemClass.BHKW_TYP };

                Assert.True(AnlagenTemperaturen.AusStammsatz(item, stamm));
                Assert.Equal(85, item.Vorlauf);
                Assert.Equal(65, item.Ruecklauf);
            }
            finally { BhkwStammLoeschen(stamm); }
        }

        /// <summary>
        /// <b>Fall 2</b>: Die zweite Hälfte des Entscheids — was der Anwender für das
        /// Projekt eingestellt hat, bleibt stehen. Geprüft wird mit einem Paar, das
        /// <c>ProjektPuffer.IstTemperaturpaar</c> gelten lässt (80/60).
        /// </summary>
        [Fact]
        public void Ein_gepflegtes_Paar_wird_nicht_ueberschrieben()
        {
            if (!_db.Vorhanden) return;

            int stamm = BhkwStammAnlegen("W6E4 Anwenderpaar", 85, 65);
            try
            {
                var item = new WErzeugerModel
                {
                    ID_Type = WizardItemClass.BHKW_TYP,
                    Vorlauf = 80,
                    Ruecklauf = 60
                };

                Assert.False(AnlagenTemperaturen.AusStammsatz(item, stamm));
                Assert.Equal(80, item.Vorlauf);
                Assert.Equal(60, item.Ruecklauf);
            }
            finally { BhkwStammLoeschen(stamm); }
        }

        /// <summary>
        /// <b>Fall 3</b>: Ein Katalogsatz ohne brauchbares Paar lässt den Feldsatz bei
        /// 0/0. „Ohne Paar" heisst dabei zweierlei — gar keine Angabe UND die halbe
        /// Angabe „90/0", die es im Bestand mehrfach gibt: Als Betriebsvorgabe ist sie
        /// wertlos und sähe an der Anlagenzeile doch gepflegt aus.
        /// </summary>
        [Fact]
        public void Ein_Stammsatz_ohne_brauchbares_Paar_laesst_den_Feldsatz_bei_null()
        {
            if (!_db.Vorhanden) return;

            int leer = BhkwStammAnlegen("W6E4 ohne Angabe", 0, 0);
            int halb = BhkwStammAnlegen("W6E4 halbes Paar", 90, 0);
            try
            {
                var ohne = new WErzeugerModel { ID_Type = WizardItemClass.BHKW_TYP };
                Assert.False(AnlagenTemperaturen.AusStammsatz(ohne, leer));
                Assert.Equal(0, ohne.Vorlauf);
                Assert.Equal(0, ohne.Ruecklauf);

                var halbes = new WErzeugerModel { ID_Type = WizardItemClass.BHKW_TYP };
                Assert.False(AnlagenTemperaturen.AusStammsatz(halbes, halb));
                Assert.Equal(0, halbes.Vorlauf);
                Assert.Equal(0, halbes.Ruecklauf);
            }
            finally
            {
                BhkwStammLoeschen(leer);
                BhkwStammLoeschen(halb);
            }
        }

        // =================================================================================
        // 4-5 — Der EINE Schreibweg (WizardCtrl.Add_WP_Waermeerzeuger)
        // =================================================================================

        /// <summary>
        /// <b>Fall 4</b>: Eine Anlage, die OHNE Paar in den Schreibweg kommt — der
        /// Assistent ohne Hülle, der Import, die künftige iOS-Oberfläche —, trägt das
        /// Paar des Katalogs, sobald sie in <c>Tab_Energieanlagen</c> steht. Der Weg
        /// läuft über die Gerätekopie, die <c>CopyFromStamm</c> im selben Durchlauf
        /// anlegt.
        /// </summary>
        [Fact]
        public void Der_Schreibweg_traegt_das_Katalogpaar_in_die_Anlagenzeile()
        {
            if (!_db.Vorhanden) return;

            // Der Name steht als LOKALE Variable da, nicht als "const string name":
            // Werkzeuge/SqlDialektPruefer loest dynamische Tabellennamen ueber EINDEUTIGE
            // Kurznamen von Konstanten auf, und eine Konstante namens "name" setzte er
            // fuer k.name in ProjektExportImportCtrl ein (dieselbe Falle wie in
            // AnlageStrangTests).
            string bezeichner = "W6E4 Schreibweg ohne Paar";
            int stamm = BhkwStammAnlegen(bezeichner, 85, 65);
            var item = new WErzeugerModel
            {
                ID_Projekt = TESTPROJEKT,
                Bezeichner = bezeichner,
                ID_Type = WizardItemClass.BHKW_TYP,
                ID_BHKW = stamm
            };

            try
            {
                Assert.True(new WizardCtrl().Add_WP_Waermeerzeuger(
                    TESTPROJEKT, new System.Collections.Generic.List<WErzeugerModel> { item }));

                Assert.Equal(85, VorlaufLesen(item.ID));
                Assert.Equal(65, RuecklaufLesen(item.ID));
            }
            finally
            {
                AnlageLoeschen(item.ID);
                BhkwKopieLoeschen(bezeichner);
                BhkwStammLoeschen(stamm);
            }
        }

        /// <summary>
        /// <b>Fall 5</b>: Dieselbe Anlage mit einem Anwenderpaar 80/60 — der Schreibweg
        /// lässt es stehen, obwohl der Katalog 85/65 anbietet. Ohne diese Zusicherung
        /// wäre die zweite Hälfte des Entscheids („können dann vom Benutzer für das
        /// Projekt geändert werden") beim nächsten Speichern wieder fort.
        /// </summary>
        [Fact]
        public void Der_Schreibweg_laesst_das_Anwenderpaar_stehen()
        {
            if (!_db.Vorhanden) return;

            // Lokale Variable statt Konstante - Begruendung im Fall darueber.
            string bezeichner = "W6E4 Schreibweg mit Paar";
            int stamm = BhkwStammAnlegen(bezeichner, 85, 65);
            var item = new WErzeugerModel
            {
                ID_Projekt = TESTPROJEKT,
                Bezeichner = bezeichner,
                ID_Type = WizardItemClass.BHKW_TYP,
                ID_BHKW = stamm,
                Vorlauf = 80,
                Ruecklauf = 60
            };

            try
            {
                Assert.True(new WizardCtrl().Add_WP_Waermeerzeuger(
                    TESTPROJEKT, new System.Collections.Generic.List<WErzeugerModel> { item }));

                Assert.Equal(80, VorlaufLesen(item.ID));
                Assert.Equal(60, RuecklaufLesen(item.ID));
            }
            finally
            {
                AnlageLoeschen(item.ID);
                BhkwKopieLoeschen(bezeichner);
                BhkwStammLoeschen(stamm);
            }
        }

        // =================================================================================
        // 6 — Die Wärmepumpe: die kleinste Vorlaufstufe der Kennlinien
        // =================================================================================

        /// <summary>
        /// <b>Fall 6</b>: Die Wärmepumpe hat keine Katalogtemperaturen; ihr „Katalog"
        /// sind die Vorlaufstufen der Kennlinien. Die KLEINSTE wird vorgeschlagen, ein
        /// bereits gesetzter Vorlauf bleibt — und der RÜCKLAUF bleibt leer, weil es für
        /// ihn im Bestand keine Regel gibt (die Vorschlagsliste des Anlagendialogs ist
        /// eine feste Liste ohne Bezug zur Vorlaufstufe).
        /// </summary>
        [Fact]
        public void Die_Waermepumpe_bekommt_die_kleinste_Vorlaufstufe()
        {
            if (!_db.Vorhanden) return;

            int geraet = WpStammAnlegen("W6E4 Kennlinienprobe");
            try
            {
                KennlinienstufeAnlegen(geraet, 55);
                KennlinienstufeAnlegen(geraet, 45);
                KennlinienstufeAnlegen(geraet, 35);

                var ohne = new WErzeugerModel { ID_Type = WizardItemClass.WP_TYP, ID_WP = geraet };
                Assert.True(AnlagenTemperaturen.VorlaufAusKennlinien(ohne));
                Assert.Equal(35, ohne.Vorlauf);
                Assert.Equal(0, ohne.Ruecklauf);

                var gesetzt = new WErzeugerModel
                {
                    ID_Type = WizardItemClass.WP_TYP,
                    ID_WP = geraet,
                    Vorlauf = 50
                };
                Assert.False(AnlagenTemperaturen.VorlaufAusKennlinien(gesetzt));
                Assert.Equal(50, gesetzt.Vorlauf);
            }
            finally { WpStammLoeschen(geraet); }
        }

        // =================================================================================
        // Hilfsmittel
        // =================================================================================

        /// <summary>Ein Wegwerf-Katalogsatz im BHKW-Katalog; 0/0 heisst „keine Angabe".</summary>
        private static int BhkwStammAnlegen(string bezeichner, int vorlauf, int ruecklauf)
        {
            int id = DataRepository.GetMaxID(BHKWStammCtrl.TABLE) + 1;
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO [" + BHKWStammCtrl.TABLE + "] " +
                "(ID, Bezeichner, Ptherm, Pel, Vorlauf, Ruecklauf, ReadOnly) VALUES (?,?,?,?,?,?,?)",
                new DbParam("@id", id), new DbParam("@b", bezeichner),
                new DbParam("@pt", 20.0), new DbParam("@pe", 10.0),
                new DbParam("@vl", vorlauf), new DbParam("@rl", ruecklauf),
                new DbParam("@ro", false)));
            return id;
        }

        private static void BhkwStammLoeschen(int id)
        {
            if (id <= 0) return;
            DataRepository.ExecuteSQL("DELETE FROM [" + BHKWStammCtrl.TABLE + "] WHERE ID = ?",
                                      new DbParam("@id", id));
        }

        /// <summary>Die Projektkopie, die <c>CopyFromStamm</c> im Schreibweg angelegt hat.</summary>
        private static void BhkwKopieLoeschen(string bezeichner)
        {
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_BHKW WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@p", TESTPROJEKT), new DbParam("@b", bezeichner));
        }

        /// <summary>Ein Wegwerf-Gerät im Wärmepumpenkatalog (Anker der Kennlinienzeilen).</summary>
        private static int WpStammAnlegen(string bezeichner)
        {
            int id = DataRepository.GetMaxID(WPStammCtrl.TABLE) + 1;
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO [" + WPStammCtrl.TABLE + "] (ID, Bezeichner, Nennleistung, ReadOnly) " +
                "VALUES (?,?,?,?)",
                new DbParam("@id", id), new DbParam("@b", bezeichner),
                new DbParam("@n", 12.0), new DbParam("@ro", false)));
            return id;
        }

        /// <summary>
        /// Die Kennlinienzeilen gehen über die Löschweitergabe des Fremdschlüssels mit
        /// (<c>ON DELETE CASCADE</c> auf <c>Tab_WP_STAMM</c>).
        /// </summary>
        private static void WpStammLoeschen(int id)
        {
            if (id <= 0) return;
            DataRepository.ExecuteSQL("DELETE FROM [" + WPStammCtrl.TABLE + "] WHERE ID = ?",
                                      new DbParam("@id", id));
        }

        /// <summary>Eine Stützstelle einer Vorlaufstufe; mehr braucht die kleinste Stufe nicht.</summary>
        private static void KennlinienstufeAnlegen(int idWp, int vorlauf)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO [" + WPStammCtrl.CURVE + "] (ID_WP, Vorlauf, Temperatur, COP, Ptherm, ReadOnly) " +
                "VALUES (?,?,?,?,?,?)",
                new DbParam("@id", idWp), new DbParam("@vl", vorlauf),
                new DbParam("@t", 2), new DbParam("@cop", 3.5),
                new DbParam("@pth", 10.0), new DbParam("@ro", false)));
        }

        /// <summary>Der Vorlauf der Anlagenzeile.</summary>
        private static int VorlaufLesen(int idAnlage)
        {
            return Zahl(DataRepository.ExecuteScalar(
                "SELECT Vorlauf FROM Tab_Energieanlagen WHERE ID = ?",
                new DbParam("@id", idAnlage)));
        }

        /// <summary>
        /// Der Rücklauf der Anlagenzeile. Die Spalte heisst <c>[Rücklauf]</c> MIT Umlaut
        /// — anders als in den Gerätetabellen (Befund B0‑4); deshalb steht sie hier
        /// ausgeschrieben und nicht über einen Spaltennamen zusammengesetzt.
        /// </summary>
        private static int RuecklaufLesen(int idAnlage)
        {
            return Zahl(DataRepository.ExecuteScalar(
                "SELECT [Rücklauf] FROM Tab_Energieanlagen WHERE ID = ?",
                new DbParam("@id", idAnlage)));
        }

        private static int Zahl(object v)
        {
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        private static void AnlageLoeschen(int id)
        {
            if (id <= 0) return;
            DataRepository.ExecuteSQL(
                "DELETE FROM " + SchemaKatalog.TAB_ENERGIEANLAGEN + " WHERE ID = ?",
                new DbParam("@id", id));
        }
    }
}
