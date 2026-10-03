using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schemaschritt KP-S1v</b> (<see cref="KonditionierungVorlagenSchema"/>, Stufe KP1b Welle W1;
    /// Konzept Konditionierungsprofile 5.1, 5.6 und 5.7): Vorlagentabelle samt Namensregel, der
    /// Fremdschlüssel <c>ID_Vorlage</c> per Tabellenneubau, acht Teilindizes der Eindeutigkeit und
    /// <c>Nachtauskuehlstunden_H</c> an beiden Ergebnistabellen.
    ///
    /// <para>Die Fälle arbeiten auf einer Arbeitskopie der Testdatenbank (<see cref="TestDatenbank"/>);
    /// ohne sie schweigen sie. Wo ein Fall den Neubau selbst prüft, setzt er die Kopie zuerst auf den
    /// Stand VOR dem Schritt zurück (<see cref="AufStand151"/>): Vorlagentabelle und Ergebnisspalten
    /// fallen, Kalender- und Vorgabetabelle entstehen wie in Schritt 151 — ohne Fremdschlüssel und ohne
    /// Teilindizes. Keine Kulturpinnung: Geprüft werden Zahlen, Kennwörter und Schematexte.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KonditionierungVorlagenSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        /// <summary>Gibt die Arbeitskopie frei (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose() => _db.Dispose();

        private const string KAL = KonditionierungSchema.TAB_KALENDER;
        private const string PER = KonditionierungSchema.TAB_PERIODE;
        private const string VOR = KonditionierungSchema.TAB_VORGABE;
        private const string VLG = KonditionierungVorlagenSchema.TAB_VORLAGE;

        /// <summary>Gibt es die Arbeitskopie? Dann MUSS der Schritt auf ihr stehen — kein stilles Schweigen.</summary>
        private bool Bereit()
        {
            if (!_db.Vorhanden) return false;
            Assert.True(KonditionierungVorlagenSchema.Vollstaendig(), "Schritt KP-S1v steht nicht auf der Arbeitskopie.");
            return true;
        }

        // =============================================================================
        //  Teil 1 - Nummer und Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>Die Nummer folgt lückenlos auf Schritt 151; der Zielstand liegt nicht darunter (die Kette bis zum
        /// Ziel hält <see cref="TwwBezugsartSchemaTests"/>).</summary>
        [Fact]
        public void Die_Nummer_folgt_auf_die_Konditionierungsprofile_und_das_Ziel_liegt_nicht_darunter()
        {
            Assert.Equal(KonditionierungSchema.SCHRITT + 1, KonditionierungVorlagenSchema.SCHRITT);
            Assert.Equal(152, KonditionierungVorlagenSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KonditionierungVorlagenSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + KonditionierungVorlagenSchema.SCHRITT + ".");
            Assert.Contains(Paketanhebung.Stufen, s => s.Nr == KonditionierungVorlagenSchema.SCHRITT
                                                      && s.Wirkung == Paketanhebung.Art.Ddl);
        }

        /// <summary>Die vier Nutzungen sind ASCII-Persistenzwerte und stehen im CHECK der Vorlagentabelle.</summary>
        [Fact]
        public void Die_Nutzungen_sind_Persistenzwerte_und_stehen_im_CHECK()
        {
            Assert.Equal(new[] { "WOHNEN", "BUERO", "SCHULE", "SONSTIGE" }, DbWerte.KOND_NUTZUNGEN);
            foreach (string n in DbWerte.KOND_NUTZUNGEN)
            {
                Assert.True(n.All(ch => ch < 128 && (char.IsUpper(ch) || ch == '_')), n);
                Assert.Contains("'" + n + "'", KonditionierungVorlagenSchema.SQL_CREATE_VORLAGE, StringComparison.Ordinal);
            }
            Assert.EndsWith(") STRICT", KonditionierungVorlagenSchema.SQL_CREATE_VORLAGE, StringComparison.Ordinal);
        }

        /// <summary>
        /// Der Zieltext setzt die Klausel GENAU hinter die eine Stelle und lässt sonst jedes Zeichen;
        /// fehlt die Stelle, steht sie zweimal da oder trägt sie schon einen Verweis, bricht er benannt ab.
        /// </summary>
        [Fact]
        public void Der_Zieltext_setzt_die_Klausel_genau_einmal_ein_oder_bricht_benannt_ab()
        {
            string bestand = KonditionierungSchema.SQL_CREATE_KALENDER.Replace("CREATE TABLE IF NOT EXISTS", "CREATE TABLE");
            string ziel = KonditionierungVorlagenSchema.Zieltext(KAL, bestand);
            Assert.Equal(bestand.Replace(KonditionierungVorlagenSchema.STELLE,
                                         KonditionierungVorlagenSchema.STELLE + KonditionierungVorlagenSchema.KLAUSEL), ziel);
            Assert.Contains("\"ID_Vorlage\" INTEGER REFERENCES \"Tab_Konditionierungsvorlage_STAMM\" (\"ID\") ON DELETE CASCADE,",
                            ziel, StringComparison.Ordinal);

            Assert.Throws<InvalidOperationException>(() => KonditionierungVorlagenSchema.Zieltext(KAL, ziel));
            Assert.Throws<InvalidOperationException>(() => KonditionierungVorlagenSchema.Zieltext(KAL,
                bestand.Replace("\"ID_Vorlage\" INTEGER,", "\"ID_Vorlage\" INTEGER,\n    \"ID_Vorlage\" INTEGER,")));
            Assert.Throws<InvalidOperationException>(() => KonditionierungVorlagenSchema.Zieltext(KAL,
                bestand.Replace("\"ID_Vorlage\" INTEGER,", "\"ID_Vorlage2\" TEXT,")));
            Assert.Throws<InvalidOperationException>(() => KonditionierungVorlagenSchema.Zieltext(KAL,
                bestand.Replace(") STRICT", ")")));
            Assert.Throws<InvalidOperationException>(() => KonditionierungVorlagenSchema.Zieltext(VOR, bestand));
        }

        /// <summary>
        /// Die Ergebnisspalte ist nullbar und hält 0 … 8 760 — geprüft an einer Wegwerftabelle im
        /// Speicher mit genau der Anweisung des Schritts.
        /// </summary>
        [Fact]
        public void Die_Ergebnisspalte_ist_nullbar_und_haelt_das_Jahr()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE \"" + ErgebnisGebaeudeSchema.TAB + "\" (\"ID\" INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, KonditionierungVorlagenSchema.SpalteAnlegen(ErgebnisGebaeudeSchema.TAB));
            Ausfuehren(c, "INSERT INTO \"Tab_ErgebnisGebaeude\" (\"ID\") VALUES (1)");
            foreach (string gut in new[] { "NULL", "0", "8760" })
                Ausfuehren(c, "UPDATE \"Tab_ErgebnisGebaeude\" SET \"Nachtauskuehlstunden_H\" = " + gut);
            foreach (string fremd in new[] { "-1", "8761", "'viel'" })
                Assert.Throws<SqliteException>(() => Ausfuehren(c,
                    "UPDATE \"Tab_ErgebnisGebaeude\" SET \"Nachtauskuehlstunden_H\" = " + fremd));
        }

        // =============================================================================
        //  Teil 2 - der Schritt auf der Arbeitskopie
        // =============================================================================

        /// <summary>Der Schritt steht auf der Messlatte und läuft ein zweites Mal ohne Wirkung.</summary>
        [Fact]
        public void Der_Schritt_steht_und_laeuft_ein_zweites_Mal_ohne_Wirkung()
        {
            if (!Bereit()) return;
            Assert.Equal(SchemaStand.Zielversion, (int)Zahl("SELECT SchemaVersion FROM Tab_Applikation"));
            Dictionary<string, string> schemaVorher = Schema();
            var zahlen = new[] { KAL, PER, VOR, VLG }.Select(t => Zahl("SELECT COUNT(*) FROM \"" + t + "\"")).ToList();

            var bericht = new List<string>();
            Assert.Equal(0, KonditionierungVorlagenSchema.Ausfuehren(bericht));
            Assert.Equal(0, KonditionierungVorlagenSchema.Ausfuehren(null));
            Assert.Single(bericht);
            Assert.StartsWith("steht bereits", bericht[0], StringComparison.Ordinal);

            Assert.Equal(schemaVorher, Schema());
            Assert.Equal(zahlen, new[] { KAL, PER, VOR, VLG }.Select(t => Zahl("SELECT COUNT(*) FROM \"" + t + "\"")).ToList());
            Assert.True(KonditionierungVorlagenSchema.Vollstaendig());

            // Die Messlatte traegt in den Tabellen der Konditionierung nur die Saat der ausgelieferten
            // Vorlagen (Schritt 156, KonditionierungsvorlagenWacheTests) - keine Zeile eines Gebaeudes,
            // einer Zone oder eines Katalogbaus, ausser den Zonenkalendern des Zonenprojekts 1052 (G6d).
            Assert.Equal(KonditionierungsvorlagenSaattabelle.VORLAGEN, (int)Zahl("SELECT COUNT(*) FROM \"" + VLG + "\""));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + KAL + "\" WHERE \"ID_Vorlage\" IS NULL AND " + Zonenbestand.NICHT_1052));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + VOR + "\" WHERE \"ID_Vorlage\" IS NULL AND " + Zonenbestand.NICHT_1052));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + PER + "\" WHERE \"ID_Kalender\" NOT IN " +
                                 "(SELECT \"ID\" FROM \"" + KAL + "\" WHERE \"ID_Vorlage\" IS NOT NULL) " +
                                 "AND \"ID_Kalender\" NOT IN (" + Zonenbestand.KALENDER_1052 + ")"));
        }

        /// <summary>
        /// Leert die Vorlagentabelle der Arbeitskopie — Vorgaben, Kalender und Perioden der Vorlagen gehen
        /// über die Kaskade mit. Die Regeln dieses Schritts werden an LEEREN Tabellen gemessen; die
        /// ausgelieferten Vorlagen (Schritt <see cref="KonditionierungsvorlagenSaatSchema.SCHRITT"/>) hält
        /// <see cref="KonditionierungsvorlagenWacheTests"/>.
        /// </summary>
        private static void OhneSaat()
        {
            DataRepository.ExecuteNonQuery("DELETE FROM \"" + VLG + "\"");
            // Dazu die Zonenkalender des Zonenprojekts 1052 (G6d) samt Perioden und Vorgaben - die Regeln
            // werden an leeren Tabellen gemessen.
            DataRepository.ExecuteNonQuery("DELETE FROM \"" + VOR + "\"");
            DataRepository.ExecuteNonQuery("DELETE FROM \"" + KAL + "\"");
            foreach (string t in new[] { KAL, PER, VOR, VLG })
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + t + "\""));
        }

        /// <summary>
        /// Die Vorlagentabelle ist STRICT mit sechs Spalten, alle vier Tabellen haben einen leeren
        /// <c>foreign_key_check</c>, die acht Teilindizes sind eindeutig UND partiell, die Namensregel
        /// ist eindeutig und nicht partiell, und beide Ergebnistabellen tragen die neue Spalte.
        /// </summary>
        [Fact]
        public void Das_Schema_ist_STRICT_die_Teilindizes_partiell_und_foreign_key_check_leer()
        {
            if (!Bereit()) return;

            Assert.Equal(KonditionierungVorlagenSchema.SPALTENZAHL_VORLAGE,
                         (int)Zahl("SELECT COUNT(*) FROM pragma_table_info(?)", new DbParam("@t", VLG)));
            Assert.EndsWith(") STRICT", Sql("table", VLG), StringComparison.Ordinal);
            foreach (string t in new[] { KAL, PER, VOR, VLG })
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check(?)", new DbParam("@t", t)));
            Assert.Equal("ok", Convert.ToString(DataRepository.ExecuteScalar("PRAGMA integrity_check"), CultureInfo.InvariantCulture));

            foreach (KonditionierungVorlagenSchema.Teilindex i in KonditionierungVorlagenSchema.Teilindizes)
            {
                DataTable z = DataRepository.GetDataTable(
                    "SELECT \"unique\", partial FROM pragma_index_list(?) WHERE name = ?",
                    new DbParam("@t", i.Tabelle), new DbParam("@n", i.Name));
                Assert.Equal(1, z.Rows.Count);
                Assert.Equal(1L, Convert.ToInt64(z.Rows[0]["unique"], CultureInfo.InvariantCulture));
                Assert.Equal(1L, Convert.ToInt64(z.Rows[0]["partial"], CultureInfo.InvariantCulture));
            }
            Assert.Equal(8, KonditionierungVorlagenSchema.Teilindizes.Count);
            Assert.Equal(4, KonditionierungVorlagenSchema.Teilindizes.Count(i => i.Tabelle == KAL));

            DataTable name = DataRepository.GetDataTable(
                "SELECT \"unique\", partial FROM pragma_index_list(?) WHERE name = ?",
                new DbParam("@t", VLG), new DbParam("@n", KonditionierungVorlagenSchema.INDEX_VORLAGE_NAME));
            Assert.Equal(1L, Convert.ToInt64(name.Rows[0]["unique"], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Convert.ToInt64(name.Rows[0]["partial"], CultureInfo.InvariantCulture));

            // Beide Fremdschluessel mit Kaskade.
            foreach (string t in KonditionierungVorlagenSchema.TabellenMitVorlage)
            {
                DataTable fk = DataRepository.GetDataTable(
                    "SELECT on_delete FROM pragma_foreign_key_list(?) WHERE \"table\" = ? AND \"from\" = ?",
                    new DbParam("@t", t), new DbParam("@z", VLG), new DbParam("@s", "ID_Vorlage"));
                Assert.Equal(1, fk.Rows.Count);
                Assert.Equal("CASCADE", Convert.ToString(fk.Rows[0][0], CultureInfo.InvariantCulture));
            }

            // Die Ergebnisspalten - die Messlatte steht auf dem Zielstand: dazu je vierzehn Spalten der
            // Aufheizoptimierung (KP-S3) und die drei bzw. eine des Schritts KP-S4 (B24).
            Assert.Equal(AufheizManuellSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE,
                         DataRepository.SpaltenVonTabelle(ErgebnisGebaeudeSchema.TAB).Count);
            Assert.Equal(AufheizManuellSchema.SPALTENZAHL_ERGEBNIS_ZONE,
                         DataRepository.SpaltenVonTabelle(ZonenkopplungSchema.TAB_ERGEBNIS).Count);
            foreach (string t in KonditionierungVorlagenSchema.Ergebnistabellen)
            {
                DataTable s = DataRepository.GetDataTable(
                    "SELECT type, \"notnull\", dflt_value FROM pragma_table_info(?) WHERE name = ?",
                    new DbParam("@t", t), new DbParam("@s", KonditionierungVorlagenSchema.SPALTE_NACHTAUSKUEHLSTUNDEN));
                Assert.Equal(1, s.Rows.Count);
                Assert.Equal("INTEGER", Convert.ToString(s.Rows[0]["type"], CultureInfo.InvariantCulture));
                Assert.Equal(0L, Convert.ToInt64(s.Rows[0]["notnull"], CultureInfo.InvariantCulture));
                Assert.True(s.Rows[0]["dflt_value"] == DBNull.Value);
            }
        }

        /// <summary>
        /// <b>Der Neubau erhält jede Zeile samt ID.</b> Vorbefüllte Kalender, Perioden und Vorgaben
        /// aller vier Eigentümerarten überstehen ihn mit gleichen Werten; der AUTOINCREMENT-Stand reist
        /// mit; die Periodentabelle verweist danach NAMENTLICH auf die Kalendertabelle, und ihre Kaskade
        /// greift weiter.
        /// </summary>
        [Fact]
        public void Vorbefuellte_Zeilen_ueberstehen_den_Neubau_mit_gleichen_Ids()
        {
            if (!Bereit()) return;
            AufStand151();

            long g = EinGebaeude();
            long z = EineZone(g);
            long s = EinKatalogbau();
            // Eine Vorlage schon VOR dem Schritt - die Tabelle von Hand, wie sie der Schritt anlegt.
            DataRepository.ExecuteNonQuery(KonditionierungVorlagenSchema.SQL_CREATE_VORLAGE);
            long vl = Einfuegen("INSERT INTO \"" + VLG + "\" (\"Groesse\", \"Bezeichner\") VALUES ('LUEFTUNG', 'Buero')");

            long kg = Kalender("\"ID_Gebaeude\"", g, "HEIZSOLL");
            long kz = Kalender("\"ID_Gebaeude\", \"ID_Zone\"", new object[] { g, z }, "HEIZSOLL");
            long ks = Kalender("\"ID_Gebaeude_Stamm\"", s, "KUEHLSOLL");
            long kv = Kalender("\"ID_Vorlage\"", vl, "LUEFTUNG");
            // Eine Luecke im Zaehler: der fuenfte Kalender faellt wieder weg.
            long weg = Kalender("\"ID_Gebaeude\"", g, "PERSONEN");
            DataRepository.ExecuteNonQuery("DELETE FROM \"" + KAL + "\" WHERE \"ID\" = ?", new DbParam("@i", weg));
            foreach (long k in new[] { kg, kz, ks, kv }) Periode(k, 1);
            Periode(kg, 2);
            Vorgabe("\"ID_Gebaeude\"", g, "HEIZSOLL", "TAG");
            Vorgabe("\"ID_Gebaeude\", \"ID_Zone\"", new object[] { g, z }, "HEIZSOLL", "TAG");
            Vorgabe("\"ID_Gebaeude_Stamm\"", s, "KUEHLSOLL", "NACHT");
            Vorgabe("\"ID_Vorlage\"", vl, "LUEFTUNG", "TAG");

            List<object[]> kalVorher = Zeilen(KAL), perVorher = Zeilen(PER), vorVorher = Zeilen(VOR);
            long seqKal = Zahl("SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("@n", KAL));
            long seqVor = Zahl("SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("@n", VOR));
            Assert.Equal(weg, seqKal);

            var bericht = new List<string>();
            Assert.True(KonditionierungVorlagenSchema.Ausfuehren(bericht) > 0);
            Assert.True(KonditionierungVorlagenSchema.Vollstaendig());
            Assert.Contains(bericht, b => b.StartsWith(KAL + ": Zeilen mit ID_Vorlage ohne Vorlage 0 geloescht", StringComparison.Ordinal));
            Assert.Contains(bericht, b => b.StartsWith(KAL + ": Fremdschluessel", StringComparison.Ordinal) && b.Contains("Zeilen 4 -> 4"));
            Assert.Contains(bericht, b => b.StartsWith(VOR + ": Fremdschluessel", StringComparison.Ordinal) && b.Contains("Zeilen 4 -> 4"));

            GleicheZeilen(kalVorher, Zeilen(KAL));
            GleicheZeilen(perVorher, Zeilen(PER));
            GleicheZeilen(vorVorher, Zeilen(VOR));
            Assert.Equal(seqKal, Zahl("SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("@n", KAL)));
            Assert.Equal(seqVor, Zahl("SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("@n", VOR)));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM sqlite_master WHERE name LIKE '%\\_alt' ESCAPE '\\'"));

            // DIE FALLE: Die Periodentabelle verweist namentlich auf die Kalendertabelle.
            string periode = Sql("table", PER);
            Assert.Contains("REFERENCES \"" + KAL + "\" (\"ID\") ON DELETE CASCADE", periode, StringComparison.Ordinal);
            Assert.DoesNotContain(KonditionierungVorlagenSchema.HILFSZUSATZ, periode, StringComparison.Ordinal);
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_list(?) WHERE \"table\" = ?",
                                 new DbParam("@t", PER), new DbParam("@z", KAL)));
            foreach (string t in new[] { KAL, PER, VOR, VLG })
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check(?)", new DbParam("@t", t)));

            // Die neun Indizes von 151 stehen wieder.
            Assert.True(KonditionierungSchema.Vollstaendig());

            // Die Kaskade der Periodentabelle greift weiter, und der Zaehler vergibt keine Id zweimal.
            DataRepository.ExecuteNonQuery("DELETE FROM \"" + KAL + "\" WHERE \"ID\" = ?", new DbParam("@i", kg));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + PER + "\" WHERE \"ID_Kalender\" = ?", new DbParam("@k", kg)));
            Assert.Equal(3, Zahl("SELECT COUNT(*) FROM \"" + PER + "\""));
            long neu = Kalender("\"ID_Gebaeude\"", g, "GERAETE");
            Assert.Equal(seqKal + 1, neu);
        }

        /// <summary>
        /// <b>Die Gegenprobe der Falle:</b> Ohne <c>legacy_alter_table</c> schreibt diese SQLite-Fassung
        /// beim Umbenennen der Elterntabelle den Verweis der Kindtabelle auf den Hilfsnamen um — auch bei
        /// ausgeschalteten Fremdschlüsseln. Mit dem Legacy-Modus bleibt er stehen. Der Vorgang wird nie
        /// festgeschrieben.
        /// </summary>
        [Fact]
        public void Die_Falle_des_Umbenennens_besteht_ohne_Legacy_Modus()
        {
            if (!_db.Vorhanden) return;
            using DbVorgang v = DataRepository.VorgangOhneFremdschluessel();
            try
            {
                foreach (string paar in new[] { "Ohne", "Mit" })
                {
                    v.Ausfuehren("CREATE TABLE \"Tab_Probe" + paar + "Eltern\" (\"ID\" INTEGER PRIMARY KEY) STRICT");
                    v.Ausfuehren("CREATE TABLE \"Tab_Probe" + paar + "Kind\" (\"ID\" INTEGER PRIMARY KEY, \"ID_Eltern\" INTEGER " +
                                 "REFERENCES \"Tab_Probe" + paar + "Eltern\" (\"ID\") ON DELETE CASCADE) STRICT");
                }

                v.Ausfuehren("PRAGMA legacy_alter_table = OFF");
                v.Ausfuehren("ALTER TABLE \"Tab_ProbeOhneEltern\" RENAME TO \"Tab_ProbeOhneEltern_alt\"");
                v.Ausfuehren("PRAGMA legacy_alter_table = ON");
                v.Ausfuehren("ALTER TABLE \"Tab_ProbeMitEltern\" RENAME TO \"Tab_ProbeMitEltern_alt\"");
                v.Ausfuehren("PRAGMA legacy_alter_table = OFF");

                string ohne = Convert.ToString(v.Skalar("SELECT sql FROM sqlite_master WHERE name = 'Tab_ProbeOhneKind'"),
                                               CultureInfo.InvariantCulture);
                string mit = Convert.ToString(v.Skalar("SELECT sql FROM sqlite_master WHERE name = 'Tab_ProbeMitKind'"),
                                              CultureInfo.InvariantCulture);
                Assert.Contains("Tab_ProbeOhneEltern_alt", ohne, StringComparison.Ordinal);
                Assert.Contains("REFERENCES \"Tab_ProbeMitEltern\" (\"ID\")", mit, StringComparison.Ordinal);
                Assert.DoesNotContain("_alt", mit, StringComparison.Ordinal);
            }
            finally
            {
                v.Rollback();
            }
        }

        /// <summary>Das Löschen einer Vorlage nimmt ihren Kalender, dessen Perioden und ihre Vorgaben mit — und nur die.</summary>
        [Fact]
        public void Loeschen_einer_Vorlage_nimmt_Kalender_Perioden_und_Vorgaben_mit()
        {
            if (!Bereit()) return;
            OhneSaat();
            long g = EinGebaeude();
            long vl = Einfuegen("INSERT INTO \"" + VLG + "\" (\"Groesse\", \"Bezeichner\", \"Nutzung\", \"ReadOnly\") " +
                                "VALUES ('HEIZSOLL', 'Buero', 'BUERO', 0)");
            long kv = Kalender("\"ID_Vorlage\"", vl, "HEIZSOLL");
            Periode(kv, 1);
            Periode(kv, 2);
            Vorgabe("\"ID_Vorlage\"", vl, "HEIZSOLL", "TAG");
            Vorgabe("\"ID_Vorlage\"", vl, "HEIZSOLL", "NACHT");
            long kg = Kalender("\"ID_Gebaeude\"", g, "HEIZSOLL");
            Periode(kg, 1);
            Vorgabe("\"ID_Gebaeude\"", g, "HEIZSOLL", "TAG");

            Assert.Equal(1, DataRepository.ExecuteNonQuery("DELETE FROM \"" + VLG + "\" WHERE \"ID\" = ?", new DbParam("@v", vl)));

            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + KAL + "\" WHERE \"ID_Vorlage\" IS NOT NULL"));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + VOR + "\" WHERE \"ID_Vorlage\" IS NOT NULL"));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"" + PER + "\" WHERE \"ID_Kalender\" = ?", new DbParam("@k", kv)));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"" + KAL + "\""));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"" + PER + "\""));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"" + VOR + "\""));

            // Ein Verweis auf eine Vorlage, die es nicht gibt, scheitert jetzt am Fremdschluessel.
            Assert.True(Wirft("INSERT INTO \"" + KAL + "\" (\"ID_Vorlage\", \"Groesse\", \"Wert\") VALUES (?, 'HEIZSOLL', 20.0)",
                              new DbParam("@v", vl)));
        }

        /// <summary>
        /// <b>Ein Kalender je Eigentümer und Größe:</b> Der zweite derselben Größe scheitert je
        /// Eigentümerart am Teilindex; eine andere Größe geht, ein Zonenkalender neben dem
        /// Gebäudekalender derselben Größe geht, und eine Vorlage trägt höchstens EINEN Kalender.
        /// </summary>
        [Fact]
        public void Ein_zweiter_Kalender_derselben_Groesse_scheitert_je_Eigentuemerart()
        {
            if (!Bereit()) return;
            OhneSaat();
            long g = EinGebaeude();
            long z1 = EineZone(g);
            long z2 = EineZone(g);
            long s = EinKatalogbau();
            long vl = Einfuegen("INSERT INTO \"" + VLG + "\" (\"Groesse\", \"Bezeichner\") VALUES ('HEIZSOLL', 'Wohnen')");

            // Gebaeude ohne Zone
            Kalender("\"ID_Gebaeude\"", g, "HEIZSOLL");
            Assert.True(WirftKalender("\"ID_Gebaeude\"", new object[] { g }, "HEIZSOLL"));
            Kalender("\"ID_Gebaeude\"", g, "KUEHLSOLL");

            // Zone - neben dem Gebaeudekalender derselben Groesse erlaubt, je Zone einmal
            Kalender("\"ID_Gebaeude\", \"ID_Zone\"", new object[] { g, z1 }, "HEIZSOLL");
            Kalender("\"ID_Gebaeude\", \"ID_Zone\"", new object[] { g, z2 }, "HEIZSOLL");
            Assert.True(WirftKalender("\"ID_Gebaeude\", \"ID_Zone\"", new object[] { g, z1 }, "HEIZSOLL"));
            Kalender("\"ID_Gebaeude\", \"ID_Zone\"", new object[] { g, z1 }, "LUEFTUNG");

            // Katalogbau
            Kalender("\"ID_Gebaeude_Stamm\"", s, "HEIZSOLL");
            Assert.True(WirftKalender("\"ID_Gebaeude_Stamm\"", new object[] { s }, "HEIZSOLL"));
            Kalender("\"ID_Gebaeude_Stamm\"", s, "GERAETE");

            // Vorlage - hoechstens EIN Kalender, gleich welcher Groesse
            Kalender("\"ID_Vorlage\"", vl, "HEIZSOLL");
            Assert.True(WirftKalender("\"ID_Vorlage\"", new object[] { vl }, "HEIZSOLL"));
            Assert.True(WirftKalender("\"ID_Vorlage\"", new object[] { vl }, "KUEHLSOLL"));

            Assert.Equal(8, Zahl("SELECT COUNT(*) FROM \"" + KAL + "\""));
        }

        /// <summary>
        /// <b>Eine Vorgabezeile je Eigentümer, Größe und Zeile</b> (bei der Vorlage je Zeile): die
        /// zweite derselben Zeile scheitert je Eigentümerart, eine andere Zeile geht.
        /// </summary>
        [Fact]
        public void Eine_zweite_Vorgabezeile_derselben_Zeile_scheitert_je_Eigentuemerart()
        {
            if (!Bereit()) return;
            OhneSaat();
            long g = EinGebaeude();
            long z = EineZone(g);
            long s = EinKatalogbau();
            long vl = Einfuegen("INSERT INTO \"" + VLG + "\" (\"Groesse\", \"Bezeichner\") VALUES ('GERAETE', 'Schule')");

            foreach (var (spalten, werte, groesse) in new (string, object[], string)[]
                     {
                         ("\"ID_Gebaeude\"", new object[] { g }, "HEIZSOLL"),
                         ("\"ID_Gebaeude\", \"ID_Zone\"", new object[] { g, z }, "HEIZSOLL"),
                         ("\"ID_Gebaeude_Stamm\"", new object[] { s }, "KUEHLSOLL"),
                         ("\"ID_Vorlage\"", new object[] { vl }, "GERAETE"),
                     })
            {
                Vorgabe(spalten, werte, groesse, "TAG");
                Assert.True(WirftVorgabe(spalten, werte, groesse, "TAG"), spalten);
                Vorgabe(spalten, werte, groesse, "WOCHENENDE");
            }

            // Die Vorlage haelt die Zeile auch ueber die Groesse hinweg eindeutig (5.7: nur ihre Groesse).
            Assert.True(WirftVorgabe("\"ID_Vorlage\"", new object[] { vl }, "PERSONEN", "TAG"));
            // Gebaeude: dieselbe Zeile in einer anderen Groesse geht.
            Vorgabe("\"ID_Gebaeude\"", g, "KUEHLSOLL", "TAG");

            Assert.Equal(9, Zahl("SELECT COUNT(*) FROM \"" + VOR + "\""));
        }

        /// <summary>
        /// <b>Die Namensregel:</b> „Buero" und „BUERO" in derselben Größe scheitern, derselbe Name in einer
        /// anderen Größe geht; Rand-Leerzeichen, leere und zu lange Namen, zu lange Beschreibungen und
        /// fremde Kennwörter scheitern am CHECK.
        /// </summary>
        [Fact]
        public void Die_Namensregel_haelt_je_Groesse_ohne_Gross_und_Kleinschreibung()
        {
            if (!Bereit()) return;
            OhneSaat();
            const string ein = "INSERT INTO \"Tab_Konditionierungsvorlage_STAMM\" (\"Groesse\", \"Bezeichner\", " +
                               "\"Beschreibung\", \"Nutzung\", \"ReadOnly\") VALUES (?, ?, ?, ?, ?)";
            DbParam[] P(string gr, string bez, string besch = null, string nutz = null, long ro = 0)
                => new[]
                {
                    new DbParam("@g", gr), new DbParam("@b", bez), new DbParam("@e", (object)besch),
                    new DbParam("@n", (object)nutz), new DbParam("@r", ro),
                };

            Einfuegen(ein, P("HEIZSOLL", "Buero", "Tagbetrieb", "BUERO", 1));
            Assert.True(Wirft(ein, P("HEIZSOLL", "BUERO")));
            Assert.True(Wirft(ein, P("HEIZSOLL", "buero")));
            Einfuegen(ein, P("KUEHLSOLL", "Buero"));
            Einfuegen(ein, P("KUEHLSOLL", "Buero 2"));

            Assert.True(Wirft(ein, P("HEIZSOLL", " Rand")));
            Assert.True(Wirft(ein, P("HEIZSOLL", "Rand ")));
            Assert.True(Wirft(ein, P("HEIZSOLL", "")));
            Assert.True(Wirft(ein, P("HEIZSOLL", new string('x', 81))));
            Einfuegen(ein, P("HEIZSOLL", new string('x', 80)));
            Assert.True(Wirft(ein, P("HEIZSOLL", "Lang", new string('b', 401))));
            Einfuegen(ein, P("HEIZSOLL", "Lang", new string('b', 400)));
            Assert.True(Wirft(ein, P("HEIZSOLL", "Fremd", null, "KINO")));
            Assert.True(Wirft(ein, P("HEIZSOLL", "Schloss", null, null, 2)));
            Assert.True(Wirft(ein, P("TEMPERATUR", "Groesse")));
            foreach (string n in DbWerte.KOND_NUTZUNGEN)
                Einfuegen(ein, P("PERSONEN", "Nutzung " + n, null, n));

            Assert.Equal(9, Zahl("SELECT COUNT(*) FROM \"" + VLG + "\""));
        }

        /// <summary>
        /// <b>Dubletten brechen benannt ab:</b> Auf dem Stand vor dem Schritt scheitert er an zwei
        /// Gebäudekalendern derselben Größe und zwei gleichen Vorgabezeilen eines Katalogbaus, nennt
        /// beide Indizes und lässt die Datei, wie sie war. Sind die Dubletten geklärt, läuft er durch.
        /// </summary>
        [Fact]
        public void Dubletten_vor_dem_Schritt_brechen_benannt_ab_und_der_Schritt_bleibt_wiederholbar()
        {
            if (!Bereit()) return;
            AufStand151();
            long g = EinGebaeude();
            long s = EinKatalogbau();
            long k1 = Kalender("\"ID_Gebaeude\"", g, "HEIZSOLL");
            Kalender("\"ID_Gebaeude\"", g, "HEIZSOLL");
            long v1 = Vorgabe("\"ID_Gebaeude_Stamm\"", s, "KUEHLSOLL", "NACHT");
            Vorgabe("\"ID_Gebaeude_Stamm\"", s, "KUEHLSOLL", "NACHT");
            Dictionary<string, string> schemaVorher = Schema();

            var bericht = new List<string>();
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => KonditionierungVorlagenSchema.Ausfuehren(bericht));
            Assert.Contains("Dubletten", ex.Message, StringComparison.Ordinal);
            Assert.Contains("idx_KondKalender_EindeutigGebaeude", ex.Message, StringComparison.Ordinal);
            Assert.Contains("idx_KondVorgabe_EindeutigGebaeudeStamm", ex.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("idx_KondKalender_EindeutigZone", ex.Message, StringComparison.Ordinal);

            // Nichts geaendert: Schema, Zeilen, keine Vorlagentabelle, kein Fremdschluessel.
            Assert.Equal(schemaVorher, Schema());
            Assert.Equal(2, Zahl("SELECT COUNT(*) FROM \"" + KAL + "\""));
            Assert.Equal(2, Zahl("SELECT COUNT(*) FROM \"" + VOR + "\""));
            Assert.False(DataRepository.TabelleVorhanden(VLG));
            Assert.False(KonditionierungVorlagenSchema.FremdschluesselSteht(KAL));
            Assert.Equal(1L, Zahl("PRAGMA foreign_keys"));

            // Geklaert - der Schritt laeuft durch.
            DataRepository.ExecuteNonQuery("DELETE FROM \"" + KAL + "\" WHERE \"ID\" = ?", new DbParam("@i", k1));
            DataRepository.ExecuteNonQuery("DELETE FROM \"" + VOR + "\" WHERE \"ID\" = ?", new DbParam("@i", v1));
            Assert.True(KonditionierungVorlagenSchema.Ausfuehren(null) > 0);
            Assert.True(KonditionierungVorlagenSchema.Vollstaendig());
            Assert.Equal(0, KonditionierungVorlagenSchema.Ausfuehren(null));
        }

        /// <summary>
        /// <b>Die benannte Waisenlöschung:</b> Eine Kalender- oder Vorgabezeile, deren <c>ID_Vorlage</c>
        /// auf keine Vorlage zeigt, fällt samt Perioden und steht im Bericht; alle übrigen Zeilen bleiben.
        /// </summary>
        [Fact]
        public void Waisen_an_ID_Vorlage_fallen_benannt_samt_Perioden()
        {
            if (!Bereit()) return;
            AufStand151();
            long g = EinGebaeude();
            long kg = Kalender("\"ID_Gebaeude\"", g, "HEIZSOLL");
            Periode(kg, 1);
            long kw = Kalender("\"ID_Vorlage\"", 4711L, "HEIZSOLL");
            Periode(kw, 1);
            Periode(kw, 2);
            Vorgabe("\"ID_Vorlage\"", 4711L, "HEIZSOLL", "TAG");
            Vorgabe("\"ID_Gebaeude\"", g, "HEIZSOLL", "TAG");

            var bericht = new List<string>();
            Assert.True(KonditionierungVorlagenSchema.Ausfuehren(bericht) > 0);
            Assert.Contains(bericht, b => b == KAL + ": Zeilen mit ID_Vorlage ohne Vorlage 1 geloescht, ihre Perioden 2 (erwartet 0)");
            Assert.Contains(bericht, b => b == VOR + ": Zeilen mit ID_Vorlage ohne Vorlage 1 geloescht (erwartet 0)");

            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"" + KAL + "\""));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"" + PER + "\" WHERE \"ID_Kalender\" = ?", new DbParam("@k", kg)));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"" + PER + "\""));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"" + VOR + "\""));
            foreach (string t in new[] { KAL, PER, VOR, VLG })
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check(?)", new DbParam("@t", t)));
        }

        // =============================================================================
        //  Helfer
        // =============================================================================

        /// <summary>
        /// Setzt die Arbeitskopie auf den Stand VOR dem Schritt: Vorlagentabelle und beide
        /// Ergebnisspalten fallen, Kalender-, Perioden- und Vorgabetabelle entstehen neu aus Schritt 151
        /// (leer, ohne Fremdschlüssel auf die Vorlage, ohne Teilindizes).
        /// </summary>
        private static void AufStand151()
        {
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                v.Ausfuehren("DROP TABLE \"" + PER + "\"");
                v.Ausfuehren("DROP TABLE \"" + VOR + "\"");
                v.Ausfuehren("DROP TABLE \"" + KAL + "\"");
                v.Ausfuehren("DROP TABLE \"" + VLG + "\"");
                foreach (string t in KonditionierungVorlagenSchema.Ergebnistabellen)
                    v.Ausfuehren("ALTER TABLE \"" + t + "\" DROP COLUMN \"" +
                                 KonditionierungVorlagenSchema.SPALTE_NACHTAUSKUEHLSTUNDEN + "\"");
                v.Commit();
            }
            Assert.Equal(3, KonditionierungSchema.Ausfuehren(null));
            Assert.True(KonditionierungSchema.Vollstaendig());
            Assert.False(KonditionierungVorlagenSchema.Lesbar());
            Assert.False(KonditionierungVorlagenSchema.FremdschluesselSteht(KAL));
            Assert.False(KonditionierungVorlagenSchema.FremdschluesselSteht(VOR));
        }

        private static long EinGebaeude()
            => Zahl("SELECT MIN(\"ID\") FROM \"Tab_Gebaeude\"");

        private static long EinKatalogbau()
            => Zahl("SELECT MIN(\"ID\") FROM \"Tab_Gebaeude_STAMM\"");

        private static long EineZone(long gebaeude)
        {
            long rang = Zahl("SELECT COUNT(*) FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", gebaeude)) + 1;
            return Einfuegen("INSERT INTO \"Tab_Zone\" (\"ID_Gebaeude\", \"Rang\", \"Bezeichner\") VALUES (?, ?, ?)",
                             new DbParam("@g", gebaeude), new DbParam("@r", rang), new DbParam("@b", "Probezone " + rang));
        }

        private static long Kalender(string eigner, object wert, string groesse)
            => Kalender(eigner, wert as object[] ?? new[] { wert }, groesse);

        private static long Kalender(string eigner, object[] werte, string groesse)
            => Einfuegen(KalenderSql(eigner, werte.Length), Parameter(werte, groesse, null));

        private static bool WirftKalender(string eigner, object[] werte, string groesse)
            => Wirft(KalenderSql(eigner, werte.Length), Parameter(werte, groesse, null));

        private static string KalenderSql(string eigner, int n)
            => "INSERT INTO \"" + KAL + "\" (" + eigner + ", \"Groesse\", \"Wert\", \"Bemerkung\") VALUES (" +
               string.Join(", ", Enumerable.Repeat("?", n)) + ", ?, 20.5, 'Probe')";

        private static long Vorgabe(string eigner, object wert, string groesse, string zeile)
            => Vorgabe(eigner, wert as object[] ?? new[] { wert }, groesse, zeile);

        private static long Vorgabe(string eigner, object[] werte, string groesse, string zeile)
            => Einfuegen(VorgabeSql(eigner, werte.Length), Parameter(werte, groesse, zeile));

        private static bool WirftVorgabe(string eigner, object[] werte, string groesse, string zeile)
            => Wirft(VorgabeSql(eigner, werte.Length), Parameter(werte, groesse, zeile));

        private static string VorgabeSql(string eigner, int n)
            => "INSERT INTO \"" + VOR + "\" (" + eigner + ", \"Groesse\", \"Zeile\", \"Wert\") VALUES (" +
               string.Join(", ", Enumerable.Repeat("?", n)) + ", ?, ?, 21.25)";

        private static DbParam[] Parameter(object[] werte, string groesse, string zeile)
        {
            var p = werte.Select((w, i) => new DbParam("@e" + i.ToString(CultureInfo.InvariantCulture), w)).ToList();
            p.Add(new DbParam("@gr", groesse));
            if (zeile != null) p.Add(new DbParam("@ze", zeile));
            return p.ToArray();
        }

        private static void Periode(long kalender, int rang)
            => Einfuegen("INSERT INTO \"" + PER + "\" (\"ID_Kalender\", \"Rang\", \"Art\", \"Bezeichner\", \"Beginn\", " +
                         "\"Ende\", \"Wert\") VALUES (?, ?, 'ZEITRAUM', ?, ?, ?, 17.5)",
                         new DbParam("@k", kalender), new DbParam("@r", rang), new DbParam("@b", "Periode " + rang),
                         new DbParam("@a", 10 * rang), new DbParam("@e", 10 * rang + 5));

        /// <summary>Schreibt EINE Zeile in einem eigenen, festgeschriebenen Vorgang und liefert ihre Id.</summary>
        private static long Einfuegen(string sql, params DbParam[] parameter)
        {
            using DbVorgang v = DataRepository.Vorgang();
            v.Ausfuehren(sql, parameter);
            long id = Convert.ToInt64(v.Skalar("SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
            v.Commit();
            return id;
        }

        /// <summary>Scheitert die Anweisung an einer Prüfung? Der Vorgang wird nie festgeschrieben.</summary>
        private static bool Wirft(string sql, params DbParam[] parameter)
        {
            using DbVorgang v = DataRepository.Vorgang();
            try
            {
                v.Ausfuehren(sql, parameter);
                return false;
            }
            catch (SqliteException)
            {
                return true;
            }
        }

        private static long Zahl(string sql, params DbParam[] parameter)
        {
            object o = DataRepository.ExecuteScalar(sql, parameter);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static string Sql(string art, string name)
            => Convert.ToString(DataRepository.ExecuteScalar(
                   "SELECT sql FROM sqlite_master WHERE type = ? AND name = ?", new DbParam("@a", art), new DbParam("@n", name)),
                   CultureInfo.InvariantCulture);

        /// <summary>Das ganze Schema als Name → CREATE-Text (für „nichts geändert").</summary>
        private static Dictionary<string, string> Schema()
        {
            var s = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (DataRow r in DataRepository.GetDataTable("SELECT type, name, sql FROM sqlite_master").Rows)
                s[Convert.ToString(r["type"], CultureInfo.InvariantCulture) + ":" + Convert.ToString(r["name"], CultureInfo.InvariantCulture)] =
                    r["sql"] == DBNull.Value ? "" : Convert.ToString(r["sql"], CultureInfo.InvariantCulture);
            return s;
        }

        /// <summary>Alle Zeilen einer Tabelle in Id-Reihenfolge, Spalten in Schemareihenfolge.</summary>
        private static List<object[]> Zeilen(string tabelle)
            => DataRepository.GetDataTable("SELECT * FROM \"" + tabelle + "\" ORDER BY \"ID\"").Rows
                             .Cast<DataRow>().Select(r => r.ItemArray).ToList();

        /// <summary>Gleiche Zeilen — Kommazahlen mit Toleranz, alles andere exakt.</summary>
        private static void GleicheZeilen(List<object[]> soll, List<object[]> ist)
        {
            Assert.Equal(soll.Count, ist.Count);
            for (int i = 0; i < soll.Count; i++)
            {
                Assert.Equal(soll[i].Length, ist[i].Length);
                for (int j = 0; j < soll[i].Length; j++)
                {
                    if (soll[i][j] is double a && ist[i][j] is double b)
                        Assert.True(Math.Abs(a - b) <= 1e-12, "Zeile " + i + ", Spalte " + j + ": " + a + " != " + b);
                    else
                        Assert.Equal(soll[i][j], ist[i][j]);
                }
            }
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}
