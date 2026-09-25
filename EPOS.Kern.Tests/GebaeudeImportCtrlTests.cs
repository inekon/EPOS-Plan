using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Stufe G4c, Welle 3 — die REGELN des Schritts S-F ohne Datenbank: Nummer, DDL-Texte,
    /// Wertlisten, die Prüfung vor dem Schreiben und die Paarungen des Einzonenwegs
    /// (Datenaustauschkonzept 7.1 bis 7.4, Softwarearchitektur 2.2 und 2.4).
    /// </summary>
    public class ImportzuordnungSchemaRegelTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Der_Schritt_folgt_den_Mehrzonenschritten_und_der_Zielstand_traegt_ihn()
        {
            Assert.True(ImportzuordnungSchema.SCHRITT > ZonenSchema.SCHRITT,
                        "S-F muss hinter S-C liegen - die Paarung zeigt auf Zone, Bauteil, Aufbau und Baustoff.");
            Assert.True(SchemaStand.Zielversion >= ImportzuordnungSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + ImportzuordnungSchema.SCHRITT + ".");
        }

        /// <summary>Die Hausregeln: STRICT, IF NOT EXISTS, AUTOINCREMENT; DEFAULT allein am Verlustzähler.</summary>
        [Fact]
        public void Beide_Tabellen_sind_STRICT_wiederholbar_und_mit_AUTOINCREMENT()
        {
            List<KeyValuePair<string, string>> alle = ImportzuordnungSchema.Tabellenanweisungen.ToList();
            Assert.Equal(new[] { "Tab_Importquelle", "Tab_Importzuordnung" }, alle.Select(a => a.Key));
            foreach (KeyValuePair<string, string> a in alle)
            {
                Assert.StartsWith("CREATE TABLE IF NOT EXISTS \"" + a.Key + "\" (", a.Value, StringComparison.Ordinal);
                Assert.EndsWith(") STRICT", a.Value, StringComparison.Ordinal);
                Assert.Contains("\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT", a.Value);
                Assert.DoesNotContain("ID_Projekt", a.Value);
                foreach (string zeile in a.Value.Split('\n').Where(z => z.Contains("DEFAULT")))
                    Assert.Contains("\"FehlendeEntitaeten\" INTEGER NOT NULL DEFAULT 0", zeile);
            }
            foreach (KeyValuePair<string, string> i in ImportzuordnungSchema.Indexanweisungen)
                Assert.StartsWith("CREATE INDEX IF NOT EXISTS \"" + i.Key + "\"", i.Value, StringComparison.Ordinal);
            Assert.Equal(4, ImportzuordnungSchema.Anweisungen.Count());
        }

        /// <summary>Die CHECKs stehen mit den Längen der Konstanten und der Wertliste aus <see cref="DbWerte"/>.</summary>
        [Fact]
        public void Die_CHECKs_tragen_Laengen_Formate_und_genau_ein_Ziel()
        {
            Assert.Equal("'IFC','GBXML'", ImportzuordnungSchema.WERTE_FORMAT);
            Assert.Equal(new[] { DbWerte.IMPORT_FORMAT_IFC, DbWerte.IMPORT_FORMAT_GBXML }, DbWerte.IMPORT_FORMATE);
            Assert.Equal(GebaeudeQuelle.FORMAT_IFC, DbWerte.IMPORT_FORMAT_IFC);
            Assert.Equal(GebaeudeQuelle.FORMAT_GBXML, DbWerte.IMPORT_FORMAT_GBXML);

            string q = ImportzuordnungSchema.SQL_CREATE_QUELLE;
            Assert.Contains("\"Format\" TEXT NOT NULL CHECK (\"Format\" IN ('IFC','GBXML'))", q);
            Assert.Contains("CHECK (length(\"Dateiname\") <= " + ImportzuordnungSchema.DATEINAME_MAX + ")", q);
            Assert.Contains("CHECK (length(\"Hash\") = " + ImportzuordnungSchema.HASH_LAENGE + ")", q);
            Assert.Contains("REFERENCES \"Tab_Gebaeude\" (\"ID\") ON DELETE CASCADE", q);

            string z = ImportzuordnungSchema.SQL_CREATE_ZUORDNUNG;
            Assert.Contains("CHECK (length(\"Quellkennung\") <= " + ImportzuordnungSchema.QUELLKENNUNG_MAX + ")", z);
            Assert.Contains("CHECK (length(\"Quelltyp\") <= " + ImportzuordnungSchema.QUELLTYP_MAX + ")", z);
            Assert.Equal(Quellkennung.MAX_LAENGE, ImportzuordnungSchema.QUELLKENNUNG_MAX);
            Assert.Contains("(\"ID_Aufbau\" IS NOT NULL) + (\"ID_Baustoff\" IS NOT NULL) = 1)", z);
            Assert.Contains("REFERENCES \"Tab_Importquelle\" (\"ID\") ON DELETE CASCADE", z);
            // Der Arbeitsentscheid: alle fünf Zielverweise mit Kaskade (Kopf von ImportzuordnungSchema).
            foreach (KeyValuePair<string, string> v in ImportzuordnungSchema.Zielverweise)
                Assert.Contains("FOREIGN KEY (\"" + v.Key + "\") REFERENCES \"" + v.Value + "\" (\"ID\") ON DELETE CASCADE", z);
            Assert.Equal(5, ImportzuordnungSchema.Zielverweise.Count);
            Assert.Equal(new[] { "ID_Gebaeude", "ID_Zone", "ID_Bauteil", "ID_Aufbau", "ID_Baustoff" },
                         Enum.GetValues(typeof(ImportZiel)).Cast<ImportZiel>().Select(GebaeudeImportCtrl.Zielspalte));
            Assert.Equal(10, ImportzuordnungSchema.Quellspalten.Count);
            Assert.Equal(8, ImportzuordnungSchema.Zuordnungsspalten.Count);
        }

        [Fact]
        public void Die_Pruefung_nennt_Quelle_und_Paarung_vor_dem_Schreiben()
        {
            GebaeudeQuelle gut = Quelle();
            var gebaeude = new[] { new GebaeudeQuellzuordnung("Building", "bldg-1", ImportZiel.Gebaeude) };
            Assert.Null(GebaeudeImportCtrl.Pruefen(gut, gebaeude));
            Assert.Null(GebaeudeImportCtrl.Pruefen(gut, null));

            Assert.Equal(R.HERKUNFT_MSG_QUELLE_FEHLT, GebaeudeImportCtrl.Pruefen(null, gebaeude));
            Assert.Equal(string.Format(R.HERKUNFT_MSG_FORMAT, "XML"),
                         GebaeudeImportCtrl.Pruefen(Quelle(format: "XML"), gebaeude));
            Assert.Equal(string.Format(R.HERKUNFT_MSG_DATEINAME, 260),
                         GebaeudeImportCtrl.Pruefen(Quelle(dateiname: ""), gebaeude));
            Assert.Equal(string.Format(R.HERKUNFT_MSG_DATEINAME, 260),
                         GebaeudeImportCtrl.Pruefen(Quelle(dateiname: new string('d', 261)), gebaeude));
            Assert.Equal(R.HERKUNFT_MSG_HASH, GebaeudeImportCtrl.Pruefen(Quelle(hash: new string('a', 63)), gebaeude));
            Assert.Equal(R.HERKUNFT_MSG_HASH, GebaeudeImportCtrl.Pruefen(Quelle(hash: new string('A', 64)), gebaeude));
            Assert.Equal(R.HERKUNFT_MSG_ZEITPUNKT, GebaeudeImportCtrl.Pruefen(Quelle(zeitpunkt: " "), gebaeude));

            Assert.Equal(string.Format(R.HERKUNFT_MSG_PAARUNG, 2, 40), GebaeudeImportCtrl.Pruefen(gut, new[]
            {
                gebaeude[0], new GebaeudeQuellzuordnung(new string('T', 41), "x", ImportZiel.Gebaeude)
            }));
            Assert.Equal(string.Format(R.HERKUNFT_MSG_PAARUNG, 1, 40),
                         GebaeudeImportCtrl.Pruefen(gut, new[] { new GebaeudeQuellzuordnung("Space", "", ImportZiel.Gebaeude) }));
            Assert.Equal(string.Format(R.HERKUNFT_MSG_ZIEL_FEHLT, "sp-1", "ID_Zone"),
                         GebaeudeImportCtrl.Pruefen(gut, new[] { new GebaeudeQuellzuordnung("Space", "sp-1", ImportZiel.Zone) }));
            Assert.Null(GebaeudeImportCtrl.Pruefen(gut, new[] { new GebaeudeQuellzuordnung("Space", "sp-1", ImportZiel.Zone, 7) }));
        }

        /// <summary>
        /// Der Einzonenweg (G4c, Zonenregel X4) schreibt aus dem Satz des Probenhauses allein die
        /// Paarung des Gebäudes mit seiner <c>Building/@id</c> — Räume, Flächen und Öffnungen nicht.
        /// </summary>
        [Fact]
        public void Der_Einzonenweg_paart_nur_das_Gebaeude_mit_seiner_Kennung()
        {
            GebaeudeImportSatz satz = GbxmlImportTests.Satz("gbxml_haus_si.xml");
            Assert.True(satz.Quellzuordnungen.Count > 1, "Der Satz des Probenhauses traegt nur eine Paarung.");

            GebaeudeQuellzuordnung p = Assert.Single(GebaeudeImportCtrl.Einzonenpaarungen(satz));
            Assert.Equal("Building", p.Quelltyp);
            Assert.Equal(ImportZiel.Gebaeude, p.Ziel);
            Assert.Equal(satz.Gebaeudekennung, p.Quellkennung);
            Assert.Null(p.ZielId);
            Assert.Empty(GebaeudeImportCtrl.Einzonenpaarungen(null));
        }

        /// <summary>Dasselbe für IFC (G4a): die Paarung des <c>IfcBuilding</c> mit seiner GlobalId, Format IFC.</summary>
        [Fact]
        public void Der_Einzonenweg_paart_auch_das_IfcBuilding()
        {
            GebaeudeImportSatz satz = IfcSatz();
            GebaeudeQuellzuordnung p = Assert.Single(GebaeudeImportCtrl.Einzonenpaarungen(satz));
            Assert.Equal("IfcBuilding", p.Quelltyp);
            Assert.Equal(22, p.Quellkennung.Length);
            Assert.Equal(satz.Gebaeudekennung, p.Quellkennung);
            Assert.Equal(DbWerte.IMPORT_FORMAT_IFC, satz.Quelle.Format);
        }

        /// <summary>Liest die IFC-Probe <c>ifc4_haus.ifc</c> und ordnet ihr erstes Gebäude zu.</summary>
        internal static GebaeudeImportSatz IfcSatz()
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), "ifc4_haus.ifc");
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            Assert.True(a.Gebaeude.Count > 0, "Nichts gelesen: " + string.Join(" | ", a.Meldungen));
            return a.Zuordnen(0, 'E');
        }

        internal static GebaeudeQuelle Quelle(string format = "GBXML", string dateiname = @"C:\Plaene\haus.xml",
                                              string hash = null, string zeitpunkt = "2026-09-25T10:00:00+02:00")
            => new GebaeudeQuelle(format, dateiname, hash ?? new string('a', 64), 4711, "0.37", zeitpunkt,
                                  "1.2.0.1", "X4", 0);
    }

    /// <summary>
    /// Stufe G4c, Welle 3 — der Schritt S-F und der <see cref="GebaeudeImportCtrl"/> gegen die
    /// Arbeitskopie der Testdatenbank: Schema, CHECKs, Probe 14 (Persistenz), Probe 24 (überlange
    /// Kennung), der Arbeitsentscheid „Kaskade auf die fünf Zielverweise", Duplizieren und
    /// Projekttransfer.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeImportCtrlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int GEBAEUDE = 10614;
        private const int MEHRGEBAEUDE = 1039;
        private const int GEBAEUDE_1039 = 10642;

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        private static long Zeilen(string tabelle) => Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\"");

        // =============================================================================
        //  Schema
        // =============================================================================

        [Fact]
        public void Beide_Tabellen_stehen_STRICT_samt_Indizes_und_Fremdschluesseln()
        {
            if (!_db.Vorhanden) return;

            foreach (string t in new[] { SchemaKatalog.TAB_IMPORTQUELLE, SchemaKatalog.TAB_IMPORTZUORDNUNG })
                Assert.True(Zahl("SELECT COUNT(*) FROM pragma_table_list WHERE name = ? AND strict = 1",
                                 new DbParam("@t", t)) == 1, t + " fehlt oder ist nicht STRICT.");
            Assert.Equal(new[] { "ID_Importquelle" }, IndexSpalten(ImportzuordnungSchema.INDEX_ZUORDNUNG_QUELLE));
            Assert.Equal(new[] { "Quellkennung" }, IndexSpalten(ImportzuordnungSchema.INDEX_ZUORDNUNG_KENNUNG));
            Assert.Equal(new[] { "ID" }.Concat(ImportzuordnungSchema.Quellspalten), DataRepository.SpaltenVonTabelle("Tab_Importquelle"));
            Assert.Equal(new[] { "ID" }.Concat(ImportzuordnungSchema.Zuordnungsspalten),
                         DataRepository.SpaltenVonTabelle("Tab_Importzuordnung"));

            Assert.Equal(new[] { ("ID_Gebaeude", "Tab_Gebaeude", "CASCADE") }, Fks(SchemaKatalog.TAB_IMPORTQUELLE));
            Assert.Equal(new[]
            {
                ("ID_Aufbau", "Tab_Bauteilaufbau", "CASCADE"), ("ID_Baustoff", "Tab_Baustoff", "CASCADE"),
                ("ID_Bauteil", "Tab_Bauteil", "CASCADE"), ("ID_Gebaeude", "Tab_Gebaeude", "CASCADE"),
                ("ID_Importquelle", "Tab_Importquelle", "CASCADE"), ("ID_Zone", "Tab_Zone", "CASCADE")
            }, Fks(SchemaKatalog.TAB_IMPORTZUORDNUNG));

            Assert.True(ImportzuordnungSchema.Vollstaendig());
            Assert.Equal(0L, Zeilen("Tab_Importquelle"));
            Assert.Equal(0L, Zeilen("Tab_Importzuordnung"));
        }

        /// <summary><b>Idempotenz:</b> Ein zweiter Lauf legt nichts an; auf einer Datei ohne die Tabellen legt er beide an.</summary>
        [Fact]
        public void Der_Schritt_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(0, ImportzuordnungSchema.Ausfuehren());
            DataRepository.ExecuteNonQuery("DROP TABLE \"Tab_Importzuordnung\"");
            DataRepository.ExecuteNonQuery("DROP TABLE \"Tab_Importquelle\"");
            Assert.False(ImportzuordnungSchema.Vollstaendig());
            Assert.Equal(2, ImportzuordnungSchema.Ausfuehren());
            Assert.True(ImportzuordnungSchema.Vollstaendig());
            Assert.Equal(0, ImportzuordnungSchema.Ausfuehren());
        }

        /// <summary>Die Tabellen halten ihre Regeln selbst — auch an jedem Controller vorbei.</summary>
        [Theory]
        [InlineData("INSERT INTO Tab_Importquelle (ID_Gebaeude, Format, Dateiname, Hash, Groesse, Zeitpunkt) VALUES (10614, 'XML', 'a', '" + H64 + "', 1, 'z')")]
        [InlineData("INSERT INTO Tab_Importquelle (ID_Gebaeude, Format, Dateiname, Hash, Groesse, Zeitpunkt) VALUES (10614, 'gbxml', 'a', '" + H64 + "', 1, 'z')")]
        [InlineData("INSERT INTO Tab_Importquelle (ID_Gebaeude, Format, Dateiname, Hash, Groesse, Zeitpunkt) VALUES (10614, 'IFC', 'a', 'abc', 1, 'z')")]
        [InlineData("INSERT INTO Tab_Importquelle (ID_Gebaeude, Format, Dateiname, Hash, Groesse, Zeitpunkt) VALUES (10614, 'IFC', replace(hex(zeroblob(261)), '00', 'd'), '" + H64 + "', 1, 'z')")]
        [InlineData("INSERT INTO Tab_Importquelle (ID_Gebaeude, Format, Dateiname, Hash, Groesse) VALUES (10614, 'IFC', 'a', '" + H64 + "', 1)")]
        [InlineData("INSERT INTO Tab_Importquelle (ID_Gebaeude, Format, Dateiname, Hash, Groesse, Zeitpunkt) VALUES (999999, 'IFC', 'a', '" + H64 + "', 1, 'z')")]
        [InlineData("INSERT INTO Tab_Importzuordnung (ID_Importquelle, ID_Gebaeude, Quellkennung, Quelltyp) VALUES (999999, 10614, 'k', 'Building')")]
        public void Der_CHECK_und_der_Fremdschluessel_weisen_ab(string sql)
        {
            if (!_db.Vorhanden) return;
            using (DbVorgang v = DataRepository.Vorgang())
                Assert.ThrowsAny<Exception>(() => v.Ausfuehren(sql));
        }

        private const string H64 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        /// <summary>Die Paarung: genau ein Ziel, Kennung bis 64, Quelltyp bis 40 Zeichen.</summary>
        [Fact]
        public void Die_Paarung_verlangt_genau_ein_Ziel()
        {
            if (!_db.Vorhanden) return;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                int quelle = v.EinfuegenUndId(
                    "INSERT INTO Tab_Importquelle (ID_Gebaeude, Format, Dateiname, Hash, Groesse, Zeitpunkt) VALUES (?, 'IFC', 'a.ifc', ?, 1, 'z')",
                    new[] { new DbParam("@g", GEBAEUDE), new DbParam("@h", H64) });
                Assert.Equal(0L, Convert.ToInt64(v.Skalar("SELECT FehlendeEntitaeten FROM Tab_Importquelle WHERE ID = ?",
                                                          new DbParam("@q", quelle))));
                foreach (string sql in new[]
                {
                    "INSERT INTO Tab_Importzuordnung (ID_Importquelle, Quellkennung, Quelltyp) VALUES (?, 'k', 'IfcSpace')",
                    "INSERT INTO Tab_Importzuordnung (ID_Importquelle, ID_Gebaeude, ID_Baustoff, Quellkennung, Quelltyp) VALUES (?, 10614, 1, 'k', 'IfcSpace')",
                    "INSERT INTO Tab_Importzuordnung (ID_Importquelle, ID_Gebaeude, Quellkennung, Quelltyp) VALUES (?, 10614, replace(hex(zeroblob(65)), '00', 'k'), 'IfcBuilding')",
                    "INSERT INTO Tab_Importzuordnung (ID_Importquelle, ID_Gebaeude, Quellkennung, Quelltyp) VALUES (?, 10614, 'k', replace(hex(zeroblob(41)), '00', 'T'))",
                    "INSERT INTO Tab_Importzuordnung (ID_Importquelle, ID_Zone, Quellkennung, Quelltyp) VALUES (?, 999999, 'k', 'IfcSpace')",
                })
                    Assert.ThrowsAny<Exception>(() => v.Ausfuehren(sql, new DbParam("@q", quelle)));
                v.Ausfuehren("INSERT INTO Tab_Importzuordnung (ID_Importquelle, ID_Gebaeude, Quellkennung, Quelltyp) " +
                             "VALUES (?, 10614, replace(hex(zeroblob(64)), '00', 'k'), replace(hex(zeroblob(40)), '00', 'T'))", new DbParam("@q", quelle));
                v.Rollback();
            }
            Assert.Equal(0L, Zeilen("Tab_Importquelle"));
        }

        // =============================================================================
        //  Probe 14 — Persistenz
        // =============================================================================

        /// <summary>
        /// <b>Probe 14</b> (Datenaustauschkonzept 9): Nach dem Import des Probenhauses findet ein Test
        /// Gebäude und Zone über <c>Tab_Importzuordnung.Quellkennung</c> wieder; nach einem
        /// gewöhnlichen Speichern der Gebäudeliste (Startseite und Assistent) und der Zonen stehen
        /// Quelle und Zuordnung unverändert; erst nach dem LÖSCHEN des Gebäudes sind beide Tabellen
        /// leer.
        /// </summary>
        [Fact]
        public void Probe14_Wiederfinden_gewoehnliches_Speichern_und_Loeschen()
        {
            if (!_db.Vorhanden) return;
            var zonenCtrl = new GebaeudeZonenCtrl();
            Assert.True(zonenCtrl.SpeichernJeGebaeude(GEBAEUDE_1039, new List<ZoneModel> { GebaeudeG3PruefregelTests.GueltigeZone() }).Ok);
            ZoneModel zone = Assert.Single(zonenCtrl.LesenJeGebaeude(GEBAEUDE_1039));

            GebaeudeImportSatz satz = GbxmlImportTests.Satz("gbxml_haus_si.xml");
            GebaeudeQuellzuordnung raum = satz.Quellzuordnungen.First(z => z.Quelltyp == "Space");
            var paarungen = GebaeudeImportCtrl.Einzonenpaarungen(satz)
                .Append(new GebaeudeQuellzuordnung(raum.Quelltyp, raum.Quellkennung, ImportZiel.Zone, zone.ID)).ToList();

            var ctrl = new GebaeudeImportCtrl();
            Assert.False(ctrl.SchonImportiert(GEBAEUDE_1039, satz.Quelle.Hash));
            GebaeudeImportCtrl.Ergebnis e = ctrl.SchreibeHerkunft(GEBAEUDE_1039, satz.Quelle, paarungen);
            Assert.True(e.Ok, e.Meldung);
            Assert.True(e.IdImportquelle > 0);

            // Wiederfinden: Gebäude und Zone über die Kennung der Datei.
            ImportzuordnungModel g = Assert.Single(ctrl.FindeZuordnung(satz.Gebaeudekennung));
            Assert.Equal(GEBAEUDE_1039, g.ID_Gebaeude);
            Assert.Null(g.ID_Zone);
            Assert.Equal("Building", g.Quelltyp);
            ImportzuordnungModel z = Assert.Single(ctrl.FindeZuordnung(raum.Quellkennung, GEBAEUDE_1039));
            Assert.Equal(zone.ID, z.ID_Zone);
            Assert.Null(z.ID_Gebaeude);
            Assert.Empty(ctrl.FindeZuordnung(raum.Quellkennung, GEBAEUDE));

            ImportquelleModel q = Assert.Single(ctrl.LesenQuellen(GEBAEUDE_1039));
            Assert.Equal(e.IdImportquelle, q.ID);
            Assert.Equal(DbWerte.IMPORT_FORMAT_GBXML, q.Format);
            Assert.Equal("gbxml_haus_si.xml", q.Dateiname);                 // nur der Name, nie der Pfad
            Assert.Equal(satz.Quelle.Hash, q.Hash);
            Assert.Equal(64, q.Hash.Length);
            Assert.Equal(satz.Quelle.Groesse, q.Groesse);
            Assert.Equal(satz.Quelle.Zeitpunkt, q.Zeitpunkt);
            Assert.Equal("X4", q.Zonenregel);
            Assert.Equal(0, q.FehlendeEntitaeten);
            Assert.True(ctrl.SchonImportiert(GEBAEUDE_1039, satz.Quelle.Hash));
            Assert.True(ctrl.SchonImportiert(GEBAEUDE_1039, satz.Quelle.Hash.ToUpperInvariant()));
            Assert.False(ctrl.SchonImportiert(GEBAEUDE, satz.Quelle.Hash));
            string vorher = Fingerabdruck();

            // Gewöhnliches Speichern der Gebäudeliste - unverändert, dann mit geänderter Fläche.
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(MEHRGEBAEUDE);
            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(MEHRGEBAEUDE, liste);
            Assert.True(ok, meldung);
            Assert.Equal(vorher, Fingerabdruck());
            liste = Z_ProjGebCtrl.LiesProjekt(MEHRGEBAEUDE);
            liste[0].Wohnflaeche += 10;
            Assert.True(new WizardCtrl().Speichere_Projekt_Gebaeudeliste(MEHRGEBAEUDE, liste).Gelungen);
            Assert.Equal(vorher, Fingerabdruck());

            // Derselbe Abgleich im Vorgang des Assistenten.
            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.True(new WizardCtrl().Schreibe_Projekt_ZuordungGebäude(MEHRGEBAEUDE, Z_ProjGebCtrl.LiesProjekt(MEHRGEBAEUDE), v));
                v.Commit();
            }
            Assert.Equal(vorher, Fingerabdruck());

            // Gewöhnliches Speichern der Zonen - der Abgleich über die Ids lässt die Paarung stehen.
            Assert.True(zonenCtrl.SpeichernJeGebaeude(GEBAEUDE_1039, zonenCtrl.LesenJeGebaeude(GEBAEUDE_1039)).Ok);
            Assert.Equal(vorher, Fingerabdruck());

            // Löschen: das Gebäude fällt aus dem Projekt - Quelle und Paarungen mit.
            Assert.True(new WizardCtrl().Del_Projekt_ZuordungGebäude(MEHRGEBAEUDE, ProjektGebaeude(GEBAEUDE_1039)));
            Assert.Equal(0L, Zeilen("Tab_Importquelle"));
            Assert.Equal(0L, Zeilen("Tab_Importzuordnung"));
            Assert.Empty(ctrl.FindeZuordnung(satz.Gebaeudekennung));
        }

        /// <summary>Ein IFC-Import (G4a) schreibt seine Quelle mit Format und Schemastand der Datei und die Gebäudepaarung.</summary>
        [Fact]
        public void Ein_IFC_Import_schreibt_Quelle_und_Gebaeudepaarung()
        {
            if (!_db.Vorhanden) return;
            GebaeudeImportSatz satz = ImportzuordnungSchemaRegelTests.IfcSatz();
            var ctrl = new GebaeudeImportCtrl();
            GebaeudeImportCtrl.Ergebnis e = ctrl.SchreibeHerkunft(GEBAEUDE, satz.Quelle, GebaeudeImportCtrl.Einzonenpaarungen(satz));
            Assert.True(e.Ok, e.Meldung);

            ImportquelleModel q = Assert.Single(ctrl.LesenQuellen(GEBAEUDE));
            Assert.Equal(DbWerte.IMPORT_FORMAT_IFC, q.Format);
            Assert.Equal("ifc4_haus.ifc", q.Dateiname);
            Assert.Equal(satz.Quelle.Schemastand, q.Schemastand);
            ImportzuordnungModel z = Assert.Single(ctrl.LesenZuordnungen(q.ID));
            Assert.Equal("IfcBuilding", z.Quelltyp);
            Assert.Equal(GEBAEUDE, z.ID_Gebaeude);
            Assert.Equal(GEBAEUDE, Assert.Single(ctrl.FindeZuordnung(satz.Gebaeudekennung, GEBAEUDE)).ID_Gebaeude);
        }

        // =============================================================================
        //  Probe 24 — überlange Kennung
        // =============================================================================

        /// <summary>
        /// <b>Probe 24</b>: Die gbXML-Datei mit einer Flächen-<c>id</c> von 80 Zeichen wird gelesen;
        /// die Paarung steht gekürzt (56 Zeichen und acht Hexadezimalzeichen des SHA-256) in
        /// <c>Tab_Importzuordnung</c> und ist über die VOLLE Kennung wiederzufinden.
        /// </summary>
        [Fact]
        public void Probe24_ueberlange_Kennung_steht_gekuerzt_in_der_Tabelle()
        {
            if (!_db.Vorhanden) return;
            var zonenCtrl = new GebaeudeZonenCtrl();
            Assert.True(zonenCtrl.SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel> { GebaeudeG3PruefregelTests.GueltigeZone() }).Ok);
            int bauteil = Assert.Single(zonenCtrl.LesenJeGebaeude(GEBAEUDE)).Bauteile[0].ID;

            string lang = "aussenwand-nord-" + new string('x', 64);
            string kurz = lang.Substring(0, 56) + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(lang))).Substring(0, 8);
            GebaeudeImportSatz satz = GbxmlImportTests.Satz("gbxml_kennung_lang.xml");
            GebaeudeQuellzuordnung flaeche = satz.Quellzuordnungen.Single(z => z.Quelltyp == "Surface" && z.Quellkennung == kurz);

            var ctrl = new GebaeudeImportCtrl();
            GebaeudeImportCtrl.Ergebnis e = ctrl.SchreibeHerkunft(GEBAEUDE, satz.Quelle, GebaeudeImportCtrl.Einzonenpaarungen(satz)
                .Append(new GebaeudeQuellzuordnung(flaeche.Quelltyp, lang, ImportZiel.Bauteil, bauteil)).ToList());
            Assert.True(e.Ok, e.Meldung);

            Assert.Equal(kurz, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Quellkennung FROM Tab_Importzuordnung WHERE ID_Bauteil = ?", new DbParam("@b", bauteil))));
            Assert.Equal(64L, Zahl("SELECT MAX(length(Quellkennung)) FROM Tab_Importzuordnung"));
            ImportzuordnungModel z = Assert.Single(ctrl.FindeZuordnung(lang));
            Assert.Equal(bauteil, z.ID_Bauteil);
            Assert.Equal(kurz, z.Quellkennung);
            Assert.Single(ctrl.FindeZuordnung(kurz));
        }

        // =============================================================================
        //  Der Arbeitsentscheid: Kaskade auf die fünf Zielverweise
        // =============================================================================

        /// <summary>
        /// <b>Die fünf Zielverweise kaskadieren.</b> Entfernt der Anwender eine importierte Zone oder
        /// ein Bauteil über den Zonendialog (<see cref="GebaeudeZonenCtrl.SpeichernJeGebaeude"/>), einen
        /// Aufbau oder einen Baustoff über ihren Controller, gelingt das Speichern, und genau die
        /// Paarung auf das entfernte Ziel ist weg; Quelle und übrige Paarungen stehen. Ohne Löschregel
        /// scheiterte jeder dieser Wege am scharfen Fremdschlüssel.
        /// </summary>
        [Fact]
        public void Ein_entferntes_Ziel_nimmt_nur_seine_Paarung_mit()
        {
            if (!_db.Vorhanden) return;
            Ziele ziele = ZieleAnlegen();
            var ctrl = new GebaeudeImportCtrl();
            GebaeudeImportCtrl.Ergebnis e = ctrl.SchreibeHerkunft(GEBAEUDE, ImportzuordnungSchemaRegelTests.Quelle(), ziele.Paarungen());
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(7, ctrl.LesenZuordnungen(e.IdImportquelle).Count);

            var zonenCtrl = new GebaeudeZonenCtrl();
            List<ZoneModel> zonen = zonenCtrl.LesenJeGebaeude(GEBAEUDE);

            // Ein Bauteil fällt (das Dach der Zone "Wohnen").
            zonen[0].Bauteile.RemoveAll(b => b.ID == ziele.Dach);
            GebaeudeZonenCtrl.Ergebnis z1 = zonenCtrl.SpeichernJeGebaeude(GEBAEUDE, zonen);
            Assert.True(z1.Ok, z1.Meldung);
            Assert.Empty(ctrl.FindeZuordnung("surf-dach"));
            Assert.Equal(6, ctrl.LesenZuordnungen(e.IdImportquelle).Count);

            // Eine Zone fällt (der Keller) - samt ihrer Paarung; die Wand der anderen Zone bleibt gepaart.
            zonen = zonenCtrl.LesenJeGebaeude(GEBAEUDE).Where(z => z.ID != ziele.Keller).ToList();
            GebaeudeZonenCtrl.Ergebnis z2 = zonenCtrl.SpeichernJeGebaeude(GEBAEUDE, zonen);
            Assert.True(z2.Ok, z2.Meldung);
            Assert.Empty(ctrl.FindeZuordnung("space-keller"));
            Assert.Equal(ziele.Wand, Assert.Single(ctrl.FindeZuordnung("surf-wand")).ID_Bauteil);
            Assert.Equal(ziele.Wohnen, Assert.Single(ctrl.FindeZuordnung("space-wohnen")).ID_Zone);

            // Aufbau und Baustoff des Projekts fallen über ihren Controller.
            BauteilaufbauCtrl.Ergebnis a = new BauteilaufbauCtrl().ProjektLoeschen(ziele.Aufbau);
            Assert.True(a.Ok, a.Meldung);
            Assert.Empty(ctrl.FindeZuordnung("cons-1"));
            BaustoffCtrl.Ergebnis s = new BaustoffCtrl().ProjektLoeschen(ziele.Baustoff);
            Assert.True(s.Ok, s.Meldung);
            Assert.Empty(ctrl.FindeZuordnung("mat-1"));

            // Es bleiben die Quelle und die Paarungen von Gebäude, Zone "Wohnen" und Wand.
            Assert.Single(ctrl.LesenQuellen(GEBAEUDE));
            Assert.Equal(new[] { "bldg-1", "space-wohnen", "surf-wand" },
                         ctrl.LesenZuordnungen(e.IdImportquelle).Select(p => p.Quellkennung).OrderBy(k => k, StringComparer.Ordinal));

            // Das Gebäude fällt aus dem Projekt: beide Tabellen sind leer.
            Assert.True(new WizardCtrl().Del_Projekt_ZuordungGebäude(PROJEKT, ProjektGebaeude(GEBAEUDE)));
            Assert.Equal(0L, Zeilen("Tab_Importquelle"));
            Assert.Equal(0L, Zeilen("Tab_Importzuordnung"));
        }

        /// <summary>Ein gelöschtes Projekt nimmt Gebäude, Quellen und Paarungen mit.</summary>
        [Fact]
        public void Ein_geloeschtes_Projekt_nimmt_seine_Importherkunft_mit()
        {
            if (!_db.Vorhanden) return;
            Ziele ziele = ZieleAnlegen();
            Assert.True(new GebaeudeImportCtrl().SchreibeHerkunft(GEBAEUDE, ImportzuordnungSchemaRegelTests.Quelle(), ziele.Paarungen()).Ok);
            Assert.True(new GebaeudeImportCtrl().SchreibeHerkunft(GEBAEUDE_1039, ImportzuordnungSchemaRegelTests.Quelle(),
                new[] { new GebaeudeQuellzuordnung("Building", "bldg-1039", ImportZiel.Gebaeude) }).Ok);

            ProjektCtrl.LoeschenMitVorarbeiten(PROJEKT, PROJEKTNAME);
            Assert.Equal(1L, Zeilen("Tab_Importquelle"));
            Assert.Equal("bldg-1039", Convert.ToString(DataRepository.ExecuteScalar("SELECT Quellkennung FROM Tab_Importzuordnung")));
        }

        // =============================================================================
        //  Ablehnen und Transaktion
        // =============================================================================

        [Fact]
        public void Fremde_Ziele_und_ein_fehlendes_Gebaeude_werden_abgelehnt_ohne_zu_schreiben()
        {
            if (!_db.Vorhanden) return;
            var zonenCtrl = new GebaeudeZonenCtrl();
            Assert.True(zonenCtrl.SpeichernJeGebaeude(GEBAEUDE_1039, new List<ZoneModel> { GebaeudeG3PruefregelTests.GueltigeZone() }).Ok);
            ZoneModel fremd = Assert.Single(zonenCtrl.LesenJeGebaeude(GEBAEUDE_1039));
            int fremderStoff = new BaustoffCtrl().ProjektAnlegen(MEHRGEBAEUDE, new BaustoffModel { Bezeichner = "Fremdstoff", Lambda = 0.5 }).Id;
            var ctrl = new GebaeudeImportCtrl();
            GebaeudeQuelle quelle = ImportzuordnungSchemaRegelTests.Quelle();
            var gebaeude = new GebaeudeQuellzuordnung("Building", "bldg-1", ImportZiel.Gebaeude);

            Assert.Equal(string.Format(R.HERKUNFT_MSG_ZIEL_FREMD, "sp-1", "ID_Zone", fremd.ID),
                         ctrl.SchreibeHerkunft(GEBAEUDE, quelle, new[] { gebaeude,
                             new GebaeudeQuellzuordnung("Space", "sp-1", ImportZiel.Zone, fremd.ID) }).Meldung);
            Assert.Equal(string.Format(R.HERKUNFT_MSG_ZIEL_FREMD, "wand", "ID_Bauteil", fremd.Bauteile[0].ID),
                         ctrl.SchreibeHerkunft(GEBAEUDE, quelle, new[] {
                             new GebaeudeQuellzuordnung("Surface", "wand", ImportZiel.Bauteil, fremd.Bauteile[0].ID) }).Meldung);
            Assert.Equal(string.Format(R.HERKUNFT_MSG_ZIEL_FREMD, "mat", "ID_Baustoff", fremderStoff),
                         ctrl.SchreibeHerkunft(GEBAEUDE, quelle, new[] {
                             new GebaeudeQuellzuordnung("Material", "mat", ImportZiel.Baustoff, fremderStoff) }).Meldung);
            Assert.Equal(string.Format(R.HERKUNFT_MSG_ZIEL_FREMD, "bldg-x", "ID_Gebaeude", GEBAEUDE_1039),
                         ctrl.SchreibeHerkunft(GEBAEUDE, quelle, new[] {
                             new GebaeudeQuellzuordnung("Building", "bldg-x", ImportZiel.Gebaeude, GEBAEUDE_1039) }).Meldung);
            Assert.Equal(string.Format(R.ZONE_MSG_GEBAEUDE_FEHLT, 999999),
                         ctrl.SchreibeHerkunft(999999, quelle, new[] { gebaeude }).Meldung);
            Assert.False(ctrl.SchreibeHerkunft(GEBAEUDE, ImportzuordnungSchemaRegelTests.Quelle(hash: "x"), new[] { gebaeude }).Ok);

            Assert.Equal(0L, Zeilen("Tab_Importquelle"));
            Assert.Equal(0L, Zeilen("Tab_Importzuordnung"));

            // Eine Quelle ohne Paarung ist zulässig - sie hält den Lauf fest.
            Assert.True(ctrl.SchreibeHerkunft(GEBAEUDE, quelle, Array.Empty<GebaeudeQuellzuordnung>()).Ok);
            Assert.Equal(1L, Zeilen("Tab_Importquelle"));
        }

        /// <summary>
        /// <b>Eine Transaktion:</b> Im Vorgang des Aufrufers wird die Herkunft zum Sicherungspunkt —
        /// rollt der Aufrufer zurück, steht keine Zeile; schreibt er fest, stehen Quelle und Paarung.
        /// </summary>
        [Fact]
        public void Im_Vorgang_des_Aufrufers_steht_die_Herkunft_mit_ihm_oder_gar_nicht()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeImportCtrl();
            var paarung = new[] { new GebaeudeQuellzuordnung("IfcBuilding", "2O2Fr$t4X7Zf8NOew3FLOH", ImportZiel.Gebaeude) };
            GebaeudeQuelle quelle = ImportzuordnungSchemaRegelTests.Quelle(format: DbWerte.IMPORT_FORMAT_IFC, dateiname: "/var/mobile/haus.ifc");

            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.True(ctrl.SchreibeHerkunft(GEBAEUDE, quelle, paarung, v).Ok);
                v.Rollback();
            }
            Assert.Equal(0L, Zeilen("Tab_Importquelle"));
            Assert.Equal(0L, Zeilen("Tab_Importzuordnung"));

            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.True(ctrl.SchreibeHerkunft(GEBAEUDE, quelle, paarung, v).Ok);
                v.Commit();
            }
            ImportquelleModel q = Assert.Single(ctrl.LesenQuellen(GEBAEUDE));
            Assert.Equal("haus.ifc", q.Dateiname);
            Assert.Equal(DbWerte.IMPORT_FORMAT_IFC, q.Format);
            Assert.Equal(GEBAEUDE, Assert.Single(ctrl.FindeZuordnung("2O2Fr$t4X7Zf8NOew3FLOH")).ID_Gebaeude);
        }

        // =============================================================================
        //  Duplizieren und Projekttransfer (Datenaustauschkonzept 7.4, Softwarearchitektur 2.6)
        // =============================================================================

        /// <summary>
        /// „Projekt mit Importherkunft duplizieren": Quelle und Paarungen reisen mit, mit neuen Ids,
        /// und jede Paarung zeigt auf die KOPIE ihres Ziels — Gebäude, Zone, Bauteil, Aufbau und
        /// Baustoff des neuen Projekts. Die Quelle bleibt unverändert.
        /// </summary>
        [Fact]
        public void Projekt_mit_Importherkunft_duplizieren()
        {
            if (!_db.Vorhanden) return;
            Ziele ziele = ZieleAnlegen();
            GebaeudeImportCtrl.Ergebnis e = new GebaeudeImportCtrl().SchreibeHerkunft(GEBAEUDE, ImportzuordnungSchemaRegelTests.Quelle(),
                                                                                      ziele.Paarungen());
            Assert.True(e.Ok, e.Meldung);
            string quelle = Fingerabdruck(GEBAEUDE);

            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " G4c");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");

            PruefeKopie(neu, e.IdImportquelle);
            Assert.Equal(14L, Zeilen("Tab_Importzuordnung"));
            Assert.Equal(quelle, Fingerabdruck(GEBAEUDE));
        }

        /// <summary>
        /// Der Projekttransfer erbt die Tabellenmenge aus <c>ErmittlePlan</c>: Quelle und Paarungen
        /// reisen über ihre <c>KINDER</c>-Einträge mit und zeigen am Ziel auf die Kopien.
        /// </summary>
        [Fact]
        public void Projekttransfer_traegt_die_Importherkunft()
        {
            if (!_db.Vorhanden) return;
            Ziele ziele = ZieleAnlegen();
            GebaeudeImportCtrl.Ergebnis e = new GebaeudeImportCtrl().SchreibeHerkunft(GEBAEUDE, ImportzuordnungSchemaRegelTests.Quelle(),
                                                                                      ziele.Paarungen());
            Assert.True(e.Ok, e.Meldung);

            var plan = new ProjektDuplizierenCtrl().ErmittlePlan().ToDictionary(s => s.Tabelle, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("ID_Gebaeude IN (SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {0})", plan[SchemaKatalog.TAB_IMPORTQUELLE].Filter);
            Assert.StartsWith("ID_Importquelle IN (SELECT ID FROM Tab_Importquelle", plan[SchemaKatalog.TAB_IMPORTZUORDNUNG].Filter);

            string ordner = Path.Combine(Path.GetTempPath(), "epos-g4c-transfer-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "g4c.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Laurentiuskirche Transfer", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeKopie(neu, e.IdImportquelle);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { }
            }
        }

        // =============================================================================
        //  Die Nachzieh-Stellen des Schritts
        // =============================================================================

        /// <summary>
        /// Migration der Schale, Werkzeug <c>Testdatenbankschema</c>, Testvorrichtung und Zielstand
        /// führen den Schritt aus derselben Quelle, und die REPO-Datei trägt ihn (nur lesend geöffnet):
        /// beide Tabellen samt Indizes, leer.
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("ImportzuordnungSchema.Ausfuehren()", werkzeug);
            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_IMPORTZUORDNUNG = ImportzuordnungSchema.SCHRITT;", migration);
            Assert.Contains("private static bool Schritt_Importzuordnung(Lauf l)", migration);
            int ortZonen = migration.IndexOf("new Schritt(SCHRITT_ZONEN", StringComparison.Ordinal);
            int ortSchritt = migration.IndexOf("new Schritt(SCHRITT_IMPORTZUORDNUNG", StringComparison.Ordinal);
            Assert.True(ortZonen > 0 && ortSchritt > ortZonen, "S-F steht nicht nach S-C in der Schrittliste.");
            Assert.Contains("ImportzuordnungSchema.Tabellenanweisungen", migration);
            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("ImportzuordnungSchema.Ausfuehren()", vorrichtung);
            string stand = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern", "Allgemein", "Update", "SchemaStand.cs"));
            Assert.Contains("<see cref=\"ImportzuordnungSchema.SCHRITT\"/>", stand, StringComparison.Ordinal);

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= ImportzuordnungSchema.SCHRITT);
            foreach (KeyValuePair<string, string> a in ImportzuordnungSchema.Anweisungen)
                Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM sqlite_master WHERE name = '" + a.Key + "'"));
            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_list WHERE name = 'Tab_Importzuordnung' AND strict = 1"));
            Assert.Equal(0L, Repo(verbindung, "SELECT (SELECT COUNT(*) FROM Tab_Importquelle) + (SELECT COUNT(*) FROM Tab_Importzuordnung)"));
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        /// <summary>Die Ziele eines Imports in Projekt 1007: zwei Zonen, drei Bauteile, ein Aufbau, ein Baustoff.</summary>
        private sealed class Ziele
        {
            public int Wohnen, Keller, Wand, Dach, Aufbau, Baustoff;

            public List<GebaeudeQuellzuordnung> Paarungen() => new List<GebaeudeQuellzuordnung>
            {
                new GebaeudeQuellzuordnung("Building", "bldg-1", ImportZiel.Gebaeude),
                new GebaeudeQuellzuordnung("Space", "space-wohnen", ImportZiel.Zone, Wohnen),
                new GebaeudeQuellzuordnung("Space", "space-keller", ImportZiel.Zone, Keller),
                new GebaeudeQuellzuordnung("Surface", "surf-wand", ImportZiel.Bauteil, Wand),
                new GebaeudeQuellzuordnung("Surface", "surf-dach", ImportZiel.Bauteil, Dach),
                new GebaeudeQuellzuordnung("Construction", "cons-1", ImportZiel.Aufbau, Aufbau),
                new GebaeudeQuellzuordnung("Material", "mat-1", ImportZiel.Baustoff, Baustoff),
            };
        }

        private static Ziele ZieleAnlegen()
        {
            var zonenCtrl = new GebaeudeZonenCtrl();
            ZoneModel wohnen = GebaeudeG3PruefregelTests.GueltigeZone();
            wohnen.Nutzflaeche = 120;            // ab zwei Zonen Pflicht (G6a)
            var keller = new ZoneModel
            {
                ID = -2, Bezeichner = "Keller", IstBeheizt = false, Nutzflaeche = 60,
                Bauteile = { new BauteilModel { ID = -3, Bezeichner = "Bodenplatte", Bauteilart = DbWerte.BAUTEILART_BODENPLATTE,
                                                Flaeche = 60, Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH } }
            };
            GebaeudeZonenCtrl.Ergebnis z = zonenCtrl.SpeichernJeGebaeude(GEBAEUDE, new List<ZoneModel> { wohnen, keller });
            Assert.True(z.Ok, z.Meldung);

            var aufbau = new BauteilaufbauModel
            {
                Bezeichner = "Importwand", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                Schichten = { new BauteilschichtModel { Dicke = 0.2, Lambda = 0.7 } }
            };
            BauteilaufbauCtrl.Ergebnis a = new BauteilaufbauCtrl().ProjektSpeichern(PROJEKT, aufbau);
            Assert.True(a.Ok, a.Meldung);
            BaustoffCtrl.Ergebnis s = new BaustoffCtrl().ProjektAnlegen(PROJEKT, new BaustoffModel { Bezeichner = "Importstoff", Lambda = 0.04 });
            Assert.True(s.Ok, s.Meldung);

            return new Ziele
            {
                Wohnen = wohnen.ID, Keller = keller.ID, Wand = wohnen.Bauteile[0].ID, Dach = wohnen.Bauteile[1].ID,
                Aufbau = a.Id, Baustoff = s.Id
            };
        }

        /// <summary>Prüft die Kopie eines Projekts mit <see cref="Ziele"/>: neue Quelle, jede Paarung auf der Kopie ihres Ziels.</summary>
        private static void PruefeKopie(int neu, int quellId)
        {
            int gebaeude = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("@p", neu)), CultureInfo.InvariantCulture);
            Assert.NotEqual(GEBAEUDE, gebaeude);
            var ctrl = new GebaeudeImportCtrl();
            ImportquelleModel q = Assert.Single(ctrl.LesenQuellen(gebaeude));
            Assert.NotEqual(quellId, q.ID);
            Assert.Equal("haus.xml", q.Dateiname);

            List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(gebaeude);
            var zonenIds = zonen.Select(z => z.ID).ToHashSet();
            var bauteilIds = zonen.SelectMany(z => z.Bauteile).Select(b => b.ID).ToHashSet();
            int aufbau = Assert.Single(new BauteilaufbauCtrl().LesenJeProjekt(neu)).ID;
            int stoff = Assert.Single(new BaustoffCtrl().LesenProjekt(neu)).ID;

            List<ImportzuordnungModel> p = ctrl.LesenZuordnungen(q.ID);
            Assert.Equal(7, p.Count);
            Assert.Equal(gebaeude, p.Single(x => x.Quellkennung == "bldg-1").ID_Gebaeude);
            Assert.Contains(p.Single(x => x.Quellkennung == "space-wohnen").ID_Zone.Value, zonenIds);
            Assert.Contains(p.Single(x => x.Quellkennung == "space-keller").ID_Zone.Value, zonenIds);
            Assert.Equal(zonen.Single(z => z.Bezeichner == "Keller").ID, p.Single(x => x.Quellkennung == "space-keller").ID_Zone);
            Assert.Contains(p.Single(x => x.Quellkennung == "surf-wand").ID_Bauteil.Value, bauteilIds);
            Assert.Contains(p.Single(x => x.Quellkennung == "surf-dach").ID_Bauteil.Value, bauteilIds);
            Assert.Equal(aufbau, p.Single(x => x.Quellkennung == "cons-1").ID_Aufbau);
            Assert.Equal(stoff, p.Single(x => x.Quellkennung == "mat-1").ID_Baustoff);
            Assert.All(p, x => Assert.Equal(1, new int?[] { x.ID_Gebaeude, x.ID_Zone, x.ID_Bauteil, x.ID_Aufbau, x.ID_Baustoff }
                                                  .Count(i => i.HasValue)));
        }

        /// <summary>Ein Fingerabdruck beider Tabellen — Ids, Ziele und Werte; mit <paramref name="gebaeude"/> nur dessen Quellen.</summary>
        private static string Fingerabdruck(int? gebaeude = null)
        {
            var teile = new List<string>();
            DataTable q = DataRepository.GetDataTable("SELECT * FROM Tab_Importquelle ORDER BY ID");
            var eigene = new HashSet<long>();
            foreach (DataRow r in q.Rows)
            {
                if (gebaeude.HasValue && Convert.ToInt32(r["ID_Gebaeude"]) != gebaeude.Value) continue;
                eigene.Add(Convert.ToInt64(r["ID"]));
                teile.Add(string.Join("|", r.ItemArray.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture))));
            }
            foreach (DataRow r in DataRepository.GetDataTable("SELECT * FROM Tab_Importzuordnung ORDER BY ID").Rows)
                if (eigene.Contains(Convert.ToInt64(r["ID_Importquelle"])))
                    teile.Add(string.Join("|", r.ItemArray.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture))));
            return string.Join("\n", teile);
        }

        /// <summary>Die Zuordnung (<c>Z_ProjektGebaeude.ID</c>) der Projektkopie eines Gebäudes.</summary>
        private static int ProjektGebaeude(int gebaeude)
            => Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID_ProjektGebaeude FROM Tab_Gebaeude WHERE ID = ?",
                                                             new DbParam("@g", gebaeude)), CultureInfo.InvariantCulture);

        private static List<string> IndexSpalten(string index)
        {
            DataTable t = DataRepository.GetDataTable("SELECT name FROM pragma_index_info(?) ORDER BY seqno", new DbParam("@i", index));
            return t.Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0])).ToList();
        }

        private static List<(string, string, string)> Fks(string tabelle)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"from\", \"table\", on_delete FROM pragma_foreign_key_list(?) ORDER BY \"from\"", new DbParam("@t", tabelle));
            return t.Rows.Cast<DataRow>().Select(r => (Convert.ToString(r[0]), Convert.ToString(r[1]), Convert.ToString(r[2]))).ToList();
        }

        private static long Repo(SqliteConnection verbindung, string sql)
        {
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static string Repowurzel([System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
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
