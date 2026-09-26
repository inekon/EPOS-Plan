using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Projektgebäude kennt seinen Katalogsatz</b> — Schemaschritt 121 (Welle #468,
    /// Konzept Administrationsdialoge 7.1 (a)): Spalte <c>Tab_Gebaeude.ID_Gebaeude_Stamm</c>
    /// mit Fremdschlüssel <c>ON DELETE SET NULL</c>, Index, Nachtrag über den eindeutigen
    /// Namen; die Übernahme setzt den Verweis, Duplizieren und Transfer führen ihn richtig
    /// weiter, die Löschsperre der Gebäudeverwaltung fragt zuerst ihn und erst ohne ihn den
    /// Namen. Dazu die Reparatur der „Sonstigen Fläche" ohne U-Wert im Katalog.
    ///
    /// <para><b>Jeder Fall bekommt seine eigene Arbeitskopie</b> — die Fälle benennen um,
    /// löschen und duplizieren; geteilt sähe einer den anderen.</para>
    ///
    /// <para><b>Die Träger.</b> Katalogsatz 142 „EFH-A-TS-212" führen die Projekte 1006,
    /// 1007, 1008, 1032 und 1046; Katalogsatz 125 „GMH-D-S-118" allein Projekt 1017.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeKatalogverweisTests
    {
        private const int STAMM_EFH = 142;
        private const string NAME_EFH = "EFH-A-TS-212";
        private const int STAMM_GMH = 125;
        private const string NAME_GMH = "GMH-D-S-118";

        /// <summary>Projekt 1007 „Laurentiuskirche" mit genau einem Gebäude (10614, EFH-A-TS-212).</summary>
        private const int PROJEKT = 1007;
        private const int GEBAEUDE_1007 = 10614;
        private const string PROJEKTNAME = "Laurentiuskirche";

        // =====================================================================
        //  1 - Der Schritt: Spalte, Beziehung, Index, Nachtrag
        // =====================================================================

        /// <summary>
        /// Nach dem Schritt steht die Spalte mit ihrer Beziehung (<c>SET NULL</c>) und dem
        /// Index, und jede der 26 Projektkopien der Testdatenbank zeigt auf den Katalogsatz
        /// ihres Namens — keine bleibt ohne Verweis.
        /// </summary>
        [Fact]
        public void Schritt_121_steht_mit_Beziehung_Index_und_vollstaendigem_Nachtrag()
        {
            Assert.True(SchemaStand.Zielversion >= 121, "Zielstand " + SchemaStand.Zielversion + " liegt unter 121.");
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(GebaeudeKatalogverweis.SpalteVorhanden());
            Assert.False(GebaeudeKatalogverweis.NachtragNoetig());

            DataTable fk = DataRepository.GetDataTable("SELECT \"table\", \"from\", \"to\", on_delete FROM pragma_foreign_key_list('Tab_Gebaeude')");
            DataRow verweis = fk.Rows.Cast<DataRow>().Single(r => Convert.ToString(r["from"]) == GebaeudeKatalogverweis.SPALTE);
            Assert.Equal(GebaeudeKatalogverweis.TABELLE_STAMM, Convert.ToString(verweis["table"]));
            Assert.Equal("ID", Convert.ToString(verweis["to"]));
            Assert.Equal("SET NULL", Convert.ToString(verweis["on_delete"]));

            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?",
                                  GebaeudeKatalogverweis.INDEX));

            // 29 Projektgebäude: die 26 des Schritts 121, 10653 (die Kopie von 10599 im
            // Referenzprojekt der Anlagenkopplung 1047), die Kopie von 10645 im Prüfprojekt
            // PV mit Preisen 1048 und die Kopie des Gebäudes von 1018 im Referenzprojekt
            // Solarthermie 1049 - jede mit dem Verweis ihrer Vorlage.
            Assert.Equal(29L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude"));
            Assert.Equal(29L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude g INNER JOIN Tab_Gebaeude_STAMM s " +
                                   "ON s.ID = g.ID_Gebaeude_Stamm WHERE s.Bezeichner = g.Gebaeudename"));
            Assert.Equal(0L, Zahl(GebaeudeKatalogverweis.ZaehlungOhneVerweis()));
            Assert.Equal((long)STAMM_EFH, Zahl("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1007));

            // Wiederholbar: Ein zweiter Lauf tut nichts.
            GebaeudeKatalogverweis.Bericht zweiter = GebaeudeKatalogverweis.Ausfuehren();
            Assert.False(zweiter.SpalteAngelegt);
            Assert.Equal(0L, zweiter.Nachgetragen);
            Assert.Equal(0L, zweiter.OhneVerweis);
            Assert.Equal(0L, zweiter.Repariert);
        }

        /// <summary>
        /// <b>Der Nachtrag rät nicht:</b> Er trägt nur ein, wo der Name GENAU EINEN
        /// Katalogsatz trifft. Ein Name ohne Katalogsatz und ein mehrdeutiger Name (hier
        /// künstlich: der eindeutige Index ist abgeräumt und ein zweiter Satz desselben
        /// Namens angelegt) bleiben NULL; ein gesetzter Verweis bleibt stehen.
        /// </summary>
        [Fact]
        public void Der_Nachtrag_traegt_nur_eindeutige_Namen_ein()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Sql("UPDATE Tab_Gebaeude SET ID_Gebaeude_Stamm = NULL");
            Sql("UPDATE Tab_Gebaeude SET Gebaeudename = ? WHERE ID = ?", "Kein Katalogsatz #468", GEBAEUDE_1007);
            Sql("DROP INDEX Tab_Gebaeude_STAMM_Gebaeudename");
            Sql("INSERT INTO Tab_Gebaeude_STAMM (Bezeichner) VALUES (?)", "MFH-H-U-112");  // Kopien in 1008 und 1009

            // Ein gesetzter Verweis bleibt stehen, auch wenn der Name anderes sagt.
            Sql("UPDATE Tab_Gebaeude SET ID_Gebaeude_Stamm = ? WHERE ID = 10599", STAMM_EFH);

            // 25 = 29 Projektgebäude - 10599 (gesetzt) - 3 ohne eindeutigen Namen; darin 10653, die
            // Kopie von 10599 im Referenzprojekt der Anlagenkopplung 1047, die Kopie von 10645
            // im Prüfprojekt PV mit Preisen 1048 und die Kopie des Gebäudes von 1018 im
            // Referenzprojekt Solarthermie 1049.
            Assert.Equal(25L, Zahl(GebaeudeKatalogverweis.Zaehlung()));
            GebaeudeKatalogverweis.Bericht b = GebaeudeKatalogverweis.Ausfuehren();

            Assert.Equal(25L, b.Nachgetragen);
            Assert.Equal(3L, b.OhneVerweis);                 // 1 ohne Katalogsatz + 2 mehrdeutig
            Assert.Null(Wert("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1007));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE Gebaeudename = 'MFH-H-U-112' " +
                                  "AND ID_Gebaeude_Stamm IS NOT NULL"));
            Assert.Equal((long)STAMM_EFH, Zahl("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = 10599"));
            Assert.Equal(0L, Zahl(GebaeudeKatalogverweis.Zaehlung()));
        }

        // =====================================================================
        //  2 - Die Übernahme setzt den Verweis
        // =====================================================================

        /// <summary>
        /// Der Weg des Assistenten und des Projekt-Gebäudedialogs
        /// (<c>WizardCtrl.Add_Projekt_ZuordungGebäude</c> → <c>CopyFromStamm</c>) setzt den
        /// Verweis — beim ersten Übernehmen wie beim Neuschreiben der Projektliste.
        /// </summary>
        [Fact]
        public void Die_Uebernahme_ins_Projekt_setzt_den_Verweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var wizard = new WizardCtrl();
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            liste.Add(new Z_ProjGebModel { Gebaeudename = NAME_GMH, Wohnflaeche = 500.0, Einheit = "kWh/m²a", Jahresnutzungsgrad = 0.9 });

            Assert.True(wizard.Del_Projekt_ZuordungGebäude(PROJEKT));
            Assert.True(wizard.Add_Projekt_ZuordungGebäude(PROJEKT, liste));

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Gebaeudename, ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID_Projekt = ? ORDER BY Gebaeudename",
                new DbParam("@p", PROJEKT));
            Assert.Equal(2, dt.Rows.Count);
            Assert.Equal(NAME_EFH, Convert.ToString(dt.Rows[0]["Gebaeudename"]));
            Assert.Equal((long)STAMM_EFH, Convert.ToInt64(dt.Rows[0]["ID_Gebaeude_Stamm"]));
            Assert.Equal(NAME_GMH, Convert.ToString(dt.Rows[1]["Gebaeudename"]));
            Assert.Equal((long)STAMM_GMH, Convert.ToInt64(dt.Rows[1]["ID_Gebaeude_Stamm"]));
        }

        // =====================================================================
        //  3 - Die Kopierwege: Duplizieren (auch Varianten) und Projekttransfer
        // =====================================================================

        /// <summary>
        /// <b>Duplizieren</b> (derselbe Weg wie „Variante anlegen") kopiert den Verweis
        /// UNVERSETZT: Kopie und Quelle zeigen auf denselben Katalogsatz.
        /// </summary>
        [Fact]
        public void Duplizieren_fuehrt_den_Verweis_auf_denselben_Katalogsatz()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var dup = new ProjektDuplizierenCtrl();
            Assert.Null(dup.ErmittleZieltabelle(GebaeudeKatalogverweis.TABELLE, GebaeudeKatalogverweis.SPALTE, "ID"));

            int neu = dup.Duplizieren(PROJEKTNAME, PROJEKTNAME + " #468");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ?", neu));
            Assert.Equal((long)STAMM_EFH, Zahl("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID_Projekt = ?", neu));
            Assert.Contains(PROJEKTNAME + " #468", GebaeudeStammCtrl.Loeschsperre(NAME_EFH));
        }

        /// <summary>
        /// <b>Der Projekttransfer</b> nimmt den Katalogverweis NICHT über die Paketgrenze mit
        /// (die Id eines fremden Katalogs sagt am Ziel nichts) und füllt keinen Katalogsatz
        /// unter seiner Original-Id auf. Am Ziel findet das Gebäude seinen Satz über den
        /// Namen — hier ein Satz desselben Namens unter einer ANDEREN Id; ohne Satz dieses
        /// Namens bleibt der Verweis leer, und der Katalog wächst nicht.
        /// </summary>
        [Fact]
        public void Der_Transfer_findet_den_Katalogsatz_am_Ziel_ueber_den_Namen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = Path.Combine(Path.GetTempPath(), "epos-gebverweis-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                using (ZipArchive zip = ZipFile.OpenRead(paket))
                {
                    Assert.DoesNotContain(zip.Entries, e => e.FullName == "fill/Tab_Gebaeude_STAMM.json");
                    Assert.Contains(zip.Entries, e => e.FullName == "data/Tab_Gebaeude.json");
                }

                // (a) Das Ziel fuehrt das Gebaeude unter einer ANDEREN Id: der alte Satz
                //     heisst anders, ein neuer traegt den Namen.
                Sql("UPDATE Tab_Gebaeude_STAMM SET Bezeichner = ? WHERE ID = ?", NAME_EFH + " (alt)", STAMM_EFH);
                Sql("INSERT INTO Tab_Gebaeude_STAMM (Bezeichner) VALUES (?)", NAME_EFH);
                long neueStammId = Zahl("SELECT ID FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", NAME_EFH);
                Assert.NotEqual((long)STAMM_EFH, neueStammId);
                long katalog = Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM");

                int a = io.Importieren(paket, "Transfer #468 a", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fa);
                Assert.True(a > 0, "Import fehlgeschlagen: " + fa);
                Assert.Equal(neueStammId, Zahl("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID_Projekt = ?", a));
                Assert.Equal(katalog, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"));

                // (b) Das Ziel kennt den Namen nicht: kein Verweis, kein neuer Katalogsatz.
                Sql("UPDATE Tab_Gebaeude_STAMM SET Bezeichner = ? WHERE ID = ?", NAME_EFH + " (weg)", neueStammId);
                int b = io.Importieren(paket, "Transfer #468 b", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fb);
                Assert.True(b > 0, "Import fehlgeschlagen: " + fb);
                Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ?", b));
                Assert.Null(Wert("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID_Projekt = ?", b));
                Assert.Equal(katalog, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        // =====================================================================
        //  4 - Die Löschsperre: zuerst die ID, dann der Name
        // =====================================================================

        /// <summary>
        /// <b>Umbenennen reißt die Sperre nicht mehr:</b> Nach der Umbenennung des
        /// Katalogsatzes stehen seine Projekte unter dem NEUEN Namen, und Löschen scheitert.
        /// Unter dem alten Namen steht nichts mehr.
        /// </summary>
        [Fact]
        public void Die_Loeschsperre_haelt_ueber_die_ID_auch_nach_der_Umbenennung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string neuerName = NAME_EFH + " umbenannt";
            Sql("UPDATE Tab_Gebaeude_STAMM SET Bezeichner = ? WHERE ID = ?", neuerName, STAMM_EFH);

            IReadOnlyDictionary<string, IReadOnlyList<string>> verwendung = GebaeudeStammCtrl.Projektverwendung();
            Assert.True(verwendung.ContainsKey(neuerName));
            Assert.False(verwendung.ContainsKey(NAME_EFH));
            IReadOnlyList<string> projekte = GebaeudeStammCtrl.Loeschsperre(neuerName);
            Assert.Equal(5, projekte.Count);
            Assert.Contains(PROJEKTNAME, projekte);

            Assert.False(GebaeudeStammCtrl.Loeschen(neuerName));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE ID = ?", STAMM_EFH));
        }

        /// <summary>
        /// <b>Der Verweis schlägt den Namen:</b> Eine Kopie, die auf Katalogsatz 125 zeigt,
        /// aber „EFH-A-TS-212" heißt, sperrt 125 — nicht den Satz ihres Namens.
        /// </summary>
        [Fact]
        public void Der_Verweis_schlaegt_den_Namen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Sql("UPDATE Tab_Gebaeude SET ID_Gebaeude_Stamm = ? WHERE ID = ?", STAMM_GMH, GEBAEUDE_1007);

            Assert.Contains(PROJEKTNAME, GebaeudeStammCtrl.Loeschsperre(NAME_GMH));
            Assert.DoesNotContain(PROJEKTNAME, GebaeudeStammCtrl.Loeschsperre(NAME_EFH));
            Assert.Equal(4, GebaeudeStammCtrl.Loeschsperre(NAME_EFH).Count);
        }

        /// <summary>
        /// <b>Rückfall Name:</b> Eine Kopie OHNE Verweis (Altbestand) sperrt weiter über ihren
        /// Namen — groß/klein egal; ein freier Satz lässt sich löschen.
        /// </summary>
        [Fact]
        public void Ohne_Verweis_sperrt_weiter_der_Name()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Sql("UPDATE Tab_Gebaeude SET ID_Gebaeude_Stamm = NULL");
            IReadOnlyList<string> projekte = GebaeudeStammCtrl.Loeschsperre(NAME_EFH.ToLowerInvariant());
            Assert.Equal(5, projekte.Count);
            Assert.False(GebaeudeStammCtrl.Loeschen(NAME_EFH));

            // Ein Satz, den kein Projekt fuehrt (und der kein Auslieferungssatz ist), geht.
            Assert.Empty(GebaeudeStammCtrl.Loeschsperre("AltenH-95-EnEV2016"));
            Assert.True(GebaeudeStammCtrl.Loeschen("AltenH-95-EnEV2016"));
        }

        /// <summary>
        /// <b>„Gebäude in DB löschen" des Projekt-Gebäudedialogs trägt dieselbe Sperre</b>
        /// wie die Gebäudeverwaltung (#487): Der Sperrgrund nennt die Projekte, die den Satz
        /// führen, bzw. den Auslieferungssatz; ein freier Satz hat keinen. Die Hülle reicht
        /// genau diesen Grund und den Kernweg <c>Loeschen</c> herein — ein benutzter Satz
        /// bleibt auch dann stehen, wenn die Oberfläche ihn doch hereinreicht.
        /// </summary>
        [Fact]
        public void Der_Projektdialog_loescht_mit_der_Sperre_der_Verwaltung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const string FREI = "AltenH-95-EnEV2016";
            Sql("UPDATE Tab_Gebaeude_STAMM SET ReadOnly = 0 WHERE Bezeichner IN (?, ?)", NAME_GMH, FREI);

            // Von einem Projekt gefuehrt: der Grund der Verwaltung, mit dem Projektnamen.
            IReadOnlyList<string> projekte = GebaeudeStammCtrl.Loeschsperre(NAME_GMH);
            Assert.NotEmpty(projekte);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture,
                             WindowsFormsApplication1.MyResource.Resource.ADM_AW_LOESCHEN_VERWENDET,
                             string.Join(", ", projekte)),
                         GebaeudeStammCtrl.Loeschsperrgrund(NAME_GMH));

            // Frei: kein Grund. Auslieferungssatz: der benannte Schreibschutz.
            Assert.Equal("", GebaeudeStammCtrl.Loeschsperrgrund(FREI));
            Assert.Equal("", GebaeudeStammCtrl.Loeschsperrgrund(""));
            Sql("UPDATE Tab_Gebaeude_STAMM SET ReadOnly = 1 WHERE Bezeichner = ?", FREI);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.BADM_MSG_SCHREIBGESCHUETZT,
                         GebaeudeStammCtrl.Loeschsperrgrund(FREI));

            // Die Huelle des Projektdialogs: derselbe Grund, derselbe Kernweg.
            IReadOnlyDictionary<string, object> gaben =
                GebaeudeHuelle.Gaben(PROJEKT, PROJEKTNAME, Z_ProjGebCtrl.LiesProjekt(PROJEKT), false);
            var sperre = (Func<string, string>)gaben["KatalogLoeschsperre"];
            var loeschen = (Func<string, bool>)gaben["KatalogLoeschen"];
            Assert.Equal(GebaeudeStammCtrl.Loeschsperrgrund(NAME_GMH), sperre(NAME_GMH));
            Assert.False(loeschen(NAME_GMH));
            Assert.False(loeschen(FREI));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE ID = ?", STAMM_GMH));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", FREI));

            Sql("UPDATE Tab_Gebaeude_STAMM SET ReadOnly = 0 WHERE Bezeichner = ?", FREI);
            Assert.True(loeschen(FREI));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", FREI));
        }

        /// <summary>
        /// <b><c>ON DELETE SET NULL</c>:</b> Verschwindet ein Katalogsatz auf einem anderen
        /// Weg (Dublettenbereinigung, „Gebäude in DB löschen" des Projektdialogs), bleibt die
        /// Projektkopie mit allen Werten stehen; nur ihr Verweis wird leer.
        /// </summary>
        [Fact]
        public void Ein_geloeschter_Katalogsatz_leert_nur_den_Verweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(new GebaeudeStammCtrl().Delete(NAME_GMH));

            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE ID = ?", STAMM_GMH));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID = 10599"));
            Assert.Null(Wert("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = 10599"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check('Tab_Gebaeude')"));
        }

        // =====================================================================
        //  5 - Die Sonstige Fläche ohne U-Wert
        // =====================================================================

        /// <summary>
        /// Die vier Katalogsätze mit dem Schadensbild tragen nach dem Schritt die Fläche 0
        /// (U · A war 0 und bleibt 0); ein Satz MIT U-Wert bleibt, der U-Wert Fenster des
        /// Krankenhaussatzes gehört nicht zu diesem Bild (ihn berichtigt
        /// <see cref="GebaeudeKatalogReparatur"/>, Stand der Kopie: 1,3). Ein neu entstandenes Bild wird auch an
        /// einem Auslieferungssatz repariert; eine PROJEKTKOPIE mit demselben Bild bleibt.
        /// </summary>
        [Fact]
        public void Die_Sonstige_Flaeche_ohne_U_Wert_wird_im_Katalog_null()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (int id in new[] { 1, 6, 100, 103 })
            {
                Assert.Equal(0.0, Kommazahl("SELECT Sonstige_Flaechen FROM Tab_Gebaeude_STAMM WHERE ID = ?", id));
                Assert.Equal(0.0, Kommazahl("SELECT k_Wert_Sonstiges FROM Tab_Gebaeude_STAMM WHERE ID = ?", id));
            }
            Assert.Equal(4.0, Kommazahl("SELECT Sonstige_Flaechen FROM Tab_Gebaeude_STAMM WHERE ID = 7"));
            Assert.Equal(1.3, Kommazahl("SELECT k_Wert_Fenster FROM Tab_Gebaeude_STAMM WHERE ID = 79"), 6);
            Assert.Equal(0L, Zahl(GebaeudeSonstigeFlaeche.SQL_ZAEHLUNG));

            // Das Bild entsteht neu - an einem Auslieferungssatz - und an einer Projektkopie.
            Sql("UPDATE Tab_Gebaeude_STAMM SET k_Wert_Sonstiges = 0, ReadOnly = 1 WHERE ID = 7");
            Sql("UPDATE Tab_Gebaeude SET k_Wert_Sonstiges = 0, Sonstige_Flaechen = 3 WHERE ID = ?", GEBAEUDE_1007);

            GebaeudeKatalogverweis.Bericht b = GebaeudeKatalogverweis.Ausfuehren();
            Assert.Equal(1L, b.Repariert);
            Assert.Equal(new[] { "Pflegeheim-C-S-139" }, b.RepariertNamen);
            Assert.Equal(0.0, Kommazahl("SELECT Sonstige_Flaechen FROM Tab_Gebaeude_STAMM WHERE ID = 7"));
            Assert.Equal(3.0, Kommazahl("SELECT Sonstige_Flaechen FROM Tab_Gebaeude WHERE ID = ?", GEBAEUDE_1007));
        }

        /// <summary>
        /// <b>Die Prüfung des Katalogeditors</b> nach den Schritten 121 und
        /// <see cref="GebaeudeKatalogReparatur.SCHRITT"/> (Welle #485): JEDER der 275
        /// Katalogsätze besteht sie ganz — die vier Sätze der Sonstigen Fläche, der
        /// Krankenhaussatz (U-Wert Fenster 1,3) und die vier Sätze, die ihre „Fläche je Nutzer"
        /// bekamen, eingeschlossen. Der Wächter hält den Katalog: Ein neuer Satz, den der Editor
        /// nicht speichern ließe, fällt hier auf.
        /// </summary>
        [Fact]
        public void Nach_der_Reparatur_besteht_jeder_Katalogsatz_die_Editorpruefung()
        {
            using var db = new TestDatenbank();
            using var kultur = new Kulturvorrichtung();
            if (!db.Vorhanden) return;

            GebaeudePrueftexte prueftexte = GebaeudeKatalogHuelle.Prueftexte();
            GebaeudeHuelleTexte huelltexte = GebaeudeKatalogHuelle.Texte();
            string uBereich = huelltexte.MeldungUBereich.Split("{0}")[0];
            Assert.False(string.IsNullOrWhiteSpace(uBereich));

            var verstoesse = new List<string>();
            var frei = new List<string>();
            foreach (string name in GebaeudeStammCtrl.Katalognamen())
            {
                var arbeit = new GebaeudeArbeitsstand();
                arbeit.Laden(GebaeudeAdminHuelle.Satz(name).Feldsatz, neu: false);
                GebaeudePruefbefund befund = arbeit.Pruefen(false, prueftexte, huelltexte);
                if (befund is null) frei.Add(name);
                else verstoesse.Add(name + ": " + befund.Meldung);
            }
            Assert.Equal(275, GebaeudeStammCtrl.Katalognamen().Count);   // 269 + sechs Katalogsätze M/A (E51)
            Assert.Empty(verstoesse);

            // Die reparierten Saetze bestehen die Pruefung ganz.
            foreach (string name in new[] { "AltenH-95-EnEV2016", "Pflegeheim-122-EnEV2016",
                                            "SpH-Umkl-287-EnEV2016", "SpH-Umkl-NE",
                                            "Krankenhaus_92-EnEV2016", "EFH-BZ2", "KrankenH-F-U-400",
                                            "KMEH-M-U-54", "Z-EFH-A-S-126",
                                            // Welle #493: die berichtigten Anschlusslaengen.
                                            "KrankenH-F-S-136", "KrankenH-F-TS-236", "KrankenH-F-U-400-Pinneberg",
                                            "gr_Hotel-G-134", "Kaufhaus" })
                Assert.Contains(name, frei);
            // Welle #496: die Folgeberichtigung (Tausch von Laibung und Dachkante, Hotel-F-228,
            // Aussenwand des Kaufhauses) - zwanzig Saetze.
            foreach (string name in GebaeudeAnschlusslaengenFolgereparatur.Berichtigungen.Select(b => b.Bezeichner).Distinct())
                Assert.Contains(name, frei);
            // Welle #505: die dritte Berichtigung (Laibungen 0 m oder leer, gerundete
            // EnEV-Laibungen, Kellerkanten 14,6 m, Kanten von Industrie_ne_81) - achtzehn Saetze.
            foreach (string name in GebaeudeAnschlusslaengenDritteReparatur.Berichtigungen.Select(b => b.Bezeichner).Distinct())
                Assert.Contains(name, frei);
        }

        // =====================================================================
        //  6 - Repo-Datei, Werkzeug und Migration
        // =====================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache des Schritts 121.</b> Alle drei Wege führen ihn (Migration
        /// der Schale, Werkzeug <c>Testdatenbankschema</c>, Nachzieh-Liste der Tests), und die
        /// REPO-Datei trägt ihn: Spalte mit Beziehung, jeder Verweis gesetzt, kein Katalogsatz
        /// mit dem Schadensbild (gelesen nur lesend und ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt_121()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("GebaeudeKatalogverweis.Ausfuehren()", werkzeug);
            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_121_GEBAEUDE_KATALOGVERWEIS = 121", migration);
            Assert.Contains("Schritt_121_GebaeudeKatalogverweis", migration);
            Assert.Contains("GebaeudeKatalogverweis.SqlNachtrag()", migration);
            Assert.Contains("GebaeudeSonstigeFlaeche.SQL_REPARATUR", migration);
            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("GebaeudeKatalogverweis.Ausfuehren()", vorrichtung);

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_foreign_key_list('Tab_Gebaeude') " +
                                              "WHERE \"from\" = 'ID_Gebaeude_Stamm' AND \"table\" = 'Tab_Gebaeude_STAMM' " +
                                              "AND on_delete = 'SET NULL'"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Gebaeude_Stamm IS NULL"));
            Assert.Equal(0L, Repo(verbindung, GebaeudeSonstigeFlaeche.SQL_ZAEHLUNG));
        }

        // =====================================================================
        //  7 - Neuschreiben der Gebäudeliste: zuerst die Id, dann der Name
        // =====================================================================

        /// <summary>
        /// <b>Der Neuaufbau</b> (<c>Z_ProjGebCtrl.LiesProjekt</c> →
        /// <c>Del_Projekt_ZuordungGebäude</c> → <c>Add_Projekt_ZuordungGebäude</c>; Startseite
        /// und Assistent gleichen die Liste ab und bauen nur geänderte Zeilen so neu auf,
        /// <c>GebaeudelisteAbgleichTests</c>): Die Liste trägt den Katalogverweis, und nach
        /// einer Umbenennung des Katalogsatzes entsteht die neue Kopie aus DEMSELBEN Satz — mit
        /// dessen neuem Namen und den Zuordnungswerten der Zeile.
        /// </summary>
        [Fact]
        public void Neuschreiben_nach_der_Umbenennung_findet_den_Satz_ueber_die_Id()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            Z_ProjGebModel zeile = Assert.Single(liste);
            Assert.Equal(STAMM_EFH, zeile.ID_Gebaeude_Stamm);
            double flaeche = zeile.Wohnflaeche;

            const string NEU = NAME_EFH + " (umbenannt)";
            Sql("UPDATE Tab_Gebaeude_STAMM SET Bezeichner = ? WHERE ID = ?", NEU, STAMM_EFH);

            var wizard = new WizardCtrl();
            Assert.True(wizard.Del_Projekt_ZuordungGebäude(PROJEKT));
            Assert.True(wizard.Add_Projekt_ZuordungGebäude(PROJEKT, liste));

            Z_ProjGebModel neu = Assert.Single(Z_ProjGebCtrl.LiesProjekt(PROJEKT));
            Assert.Equal(NEU, neu.Gebaeudename);
            Assert.Equal(STAMM_EFH, neu.ID_Gebaeude_Stamm);
            Assert.Equal(flaeche, neu.Wohnflaeche);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT));
        }

        /// <summary>
        /// <b>Altbestand ohne Verweis geht über den Namen</b> — und bekommt dabei den Verweis
        /// (die neue Kopie entsteht über <c>CopyFromStamm</c>). Ohne Verweis und nach einer
        /// Umbenennung findet der Rückfall nichts: Das Neuschreiben meldet <c>false</c>, wie
        /// vor dem Verweis.
        /// </summary>
        [Fact]
        public void Neuschreiben_ohne_Verweis_faellt_auf_den_Namen_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Sql("UPDATE Tab_Gebaeude SET ID_Gebaeude_Stamm = NULL WHERE ID_Projekt = ?", PROJEKT);
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            Assert.Null(Assert.Single(liste).ID_Gebaeude_Stamm);

            var wizard = new WizardCtrl();
            Assert.True(wizard.Del_Projekt_ZuordungGebäude(PROJEKT));
            Assert.True(wizard.Add_Projekt_ZuordungGebäude(PROJEKT, liste));
            Z_ProjGebModel neu = Assert.Single(Z_ProjGebCtrl.LiesProjekt(PROJEKT));
            Assert.Equal(NAME_EFH, neu.Gebaeudename);
            Assert.Equal(STAMM_EFH, neu.ID_Gebaeude_Stamm);

            // Ohne Verweis und umbenannt: kein Treffer, keine Kopie.
            liste[0].ID_Gebaeude_Stamm = null;
            Sql("UPDATE Tab_Gebaeude_STAMM SET Bezeichner = ? WHERE ID = ?", NAME_EFH + " (weg)", STAMM_EFH);
            Assert.True(wizard.Del_Projekt_ZuordungGebäude(PROJEKT));
            Assert.False(wizard.Add_Projekt_ZuordungGebäude(PROJEKT, liste));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT));
        }

        /// <summary>
        /// <b>Ein Verweis auf einen Satz, den es nicht mehr gibt</b>, fällt ebenfalls auf den
        /// Namen zurück — die Id allein entscheidet nicht über einen Fehlschlag.
        /// </summary>
        [Fact]
        public void Ein_Verweis_ins_Leere_faellt_auf_den_Namen_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int idZ = Assert.Single(Z_ProjGebCtrl.LiesProjekt(PROJEKT)).ID_Z;
            int neu = new GebaeudeStammCtrl().CopyFromStamm(987654, NAME_GMH, PROJEKT, idZ);
            Assert.True(neu > 0, "Kopie ueber den Namen fehlgeschlagen.");
            Assert.Equal((long)STAMM_GMH, Zahl("SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = ?", neu));
            Assert.Equal(NAME_GMH, Convert.ToString(Wert("SELECT Gebaeudename FROM Tab_Gebaeude WHERE ID = ?", neu)));
        }

        /// <summary>
        /// <b>Die Hülle führt den Verweis durch</b> (<c>GebaeudeHuelle</c>, Weg von Assistent
        /// und Startseite): Die Projektzeile trägt ihn aus der Kopie, eine übernommene
        /// Katalogzeile aus dem Satz, und die Rückabbildung ins Modell gibt ihn weiter.
        /// </summary>
        [Fact]
        public void Die_Huelle_fuehrt_den_Verweis_von_der_Zeile_ins_Modell()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            IReadOnlyDictionary<string, object> gaben = GebaeudeHuelle.Gaben(PROJEKT, PROJEKTNAME, modelle, false);

            var zeilen = (List<GebaeudeProjektZeile>)gaben["Zeilen"];
            Assert.Equal(STAMM_EFH, Assert.Single(zeilen).IdKatalog);

            var stammSatz = (Func<string, GebaeudeProjektZeile>)gaben["StammSatz"];
            GebaeudeProjektZeile aufgenommen = stammSatz(NAME_GMH);
            Assert.Equal(STAMM_GMH, aufgenommen.IdKatalog);

            Assert.Equal(STAMM_GMH, GebaeudeHuelle.NachModell(aufgenommen, PROJEKT).ID_Gebaeude_Stamm);
            Assert.Equal(STAMM_EFH, GebaeudeHuelle.NachModell(zeilen[0], PROJEKT).ID_Gebaeude_Stamm);
            Assert.Null(GebaeudeHuelle.AusModell(new Z_ProjGebModel()).IdKatalog);
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static DbParam[] Parameter(object[] werte)
            => werte.Select((w, i) => new DbParam("@p" + i.ToString(CultureInfo.InvariantCulture), w)).ToArray();

        private static void Sql(string sql, params object[] werte)
            => Assert.True(DataRepository.ExecuteSQL(sql, Parameter(werte)), "Fehlgeschlagen: " + sql);

        private static object Wert(string sql, params object[] werte)
        {
            object o = DataRepository.ExecuteScalar(sql, Parameter(werte));
            return o == DBNull.Value ? null : o;
        }

        private static long Zahl(string sql, params object[] werte)
        {
            object o = Wert(sql, werte);
            return o == null ? -1 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static double Kommazahl(string sql, params object[] werte)
        {
            object o = Wert(sql, werte);
            return o == null ? double.NaN : Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        private static long Repo(SqliteConnection verbindung, string sql)
        {
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static string Repowurzel(
            [System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string kandidat = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (kandidat != null && File.Exists(Path.Combine(kandidat, "WP-Plan.sln"))) return kandidat;
            }
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
