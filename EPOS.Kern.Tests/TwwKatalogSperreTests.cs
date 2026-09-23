using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Sperre benutzter Tww-Katalogzeilen in der allgemeinen Katalogpflege</b>
    /// (Umsetzungskonzept Zapfprofilgenerator 3.2): <see cref="KatalogBereinigung"/> löscht,
    /// benennt und bereinigt eine benutzte oder ausgelieferte Zeile eines Katalogs mit
    /// <see cref="KatalogDefinition.VerwendungSperrt"/> nicht, und das Löschen eines Kopfs
    /// samt Datenblock läuft in EINEM Vorgang. Die Namensgruppen bilden sich über den
    /// natürlichen Schlüssel (Bezeichner, Katalogversion).
    /// Alle Werte erfunden, jede Probe in einer eigenen leeren Datei (<see cref="TwwTestdatenbank"/>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwKatalogSperreTests
    {
        private static KatalogDefinition Katalog(string schluessel) => KatalogRegistry.Finde(schluessel);

        private static long Zahl(string sql, params DbParam[] p)
            => System.Convert.ToInt64(DataRepository.ExecuteScalar(sql, p));

        private static int BedarfstagAnlegen(string bezeichner, string version)
        {
            int id = DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO \"Tab_TwwBedarfstag_STAMM\" (\"Bezeichner\", \"Katalogversion\", \"Quelle_Art\", \"Bezugsmenge\", " +
                "\"Quelle\", \"Ausgabe\", \"Version\", \"Herkunftsart\", \"Status\", \"Beleg\", \"ReadOnly\") " +
                "VALUES (?, ?, 4, 10.0, ?, NULL, ?, 'FIKTIV', 'EIGEN', NULL, 0)",
                new[] { new DbParam("@b", bezeichner), new DbParam("@k", version),
                        new DbParam("@q", TwwTestdatenbank.QUELLE), new DbParam("@v", version) });
            for (int i = 1; i <= 3; i++)
                DataRepository.ExecuteNonQuery(
                    "INSERT INTO \"Tab_TwwBedarfstagEreignis_STAMM\" (\"ID_Bedarfstag\", \"Minute_Beginn\", \"Dauer_min\", " +
                    "\"Energie_Kwh\", \"Reihenfolge\") VALUES (?, ?, 10, 1.0, ?)",
                    new DbParam("@id", id), new DbParam("@m", i * 100), new DbParam("@r", i));
            return id;
        }

        // =================================================================================
        // Löschen
        // =================================================================================

        [Fact]
        public void Ein_benutzter_Tagesgangsatz_bleibt_samt_seinen_vier_Tagesgaengen_stehen()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            KatalogDefinition k = Katalog("TWW_TAGESGANGSATZ");

            Assert.Equal("verwendet: Tab_TwwNutzungsart_STAMM (1)", KatalogBereinigung.Sperrgrund(k, satz));
            Assert.False(KatalogBereinigung.SatzLoeschen(k, satz));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"Tab_TwwTagesgangsatz_STAMM\""));
            Assert.Equal(4, Zahl("SELECT COUNT(*) FROM \"Tab_TwwTagesgang_STAMM\" WHERE \"ID_Tagesgangsatz\" = ?", new DbParam("@id", satz)));
        }

        [Fact]
        public void Scheitert_der_Kopf_rollt_das_Loeschen_auch_die_Bloecke_zurueck()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);

            // Dieselbe Beschreibung OHNE Sperre: Erst der Fremdschlüssel der Nutzungsart hält den
            // Kopf - und der Vorgang gibt die schon gelöschten Tagesgänge zurück.
            KatalogDefinition registry = Katalog("TWW_TAGESGANGSATZ");
            var ohneSperre = new KatalogDefinition
            {
                Schluessel = registry.Schluessel,
                Tabelle = registry.Tabelle,
                Datenbloecke = registry.Datenbloecke
            };

            Assert.Null(KatalogBereinigung.Sperrgrund(ohneSperre, satz));
            Assert.False(KatalogBereinigung.SatzLoeschen(ohneSperre, satz));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"Tab_TwwTagesgangsatz_STAMM\""));
            Assert.Equal(4, Zahl("SELECT COUNT(*) FROM \"Tab_TwwTagesgang_STAMM\""));

            // Eine fehlende Zeile ist kein Erfolg.
            Assert.False(KatalogBereinigung.SatzLoeschen(ohneSperre, 9999));
        }

        [Fact]
        public void Ein_benutzter_Bedarfstag_laesst_sich_nicht_loeschen_und_die_Projektwahl_bleibt()
        {
            using var db = new TwwTestdatenbank();
            int benutzt = BedarfstagAnlegen("Tag A", "T1");
            int frei = BedarfstagAnlegen("Tag B", "T1");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO \"Tab_TwwProjekt\" (\"ID_Projekt\", \"ID_Bedarfstag\") VALUES (1, ?)", new DbParam("@t", benutzt));
            KatalogDefinition k = Katalog("TWW_BEDARFSTAG");

            Assert.Equal("verwendet: Tab_TwwProjekt (1)", KatalogBereinigung.Sperrgrund(k, benutzt));
            Assert.False(KatalogBereinigung.SatzLoeschen(k, benutzt));
            Assert.Equal(benutzt, Zahl("SELECT \"ID_Bedarfstag\" FROM \"Tab_TwwProjekt\" WHERE \"ID_Projekt\" = 1"));
            Assert.Equal(3, Zahl("SELECT COUNT(*) FROM \"Tab_TwwBedarfstagEreignis_STAMM\" WHERE \"ID_Bedarfstag\" = ?", new DbParam("@id", benutzt)));

            // Ein freier Bedarfstag geht samt seinen Ereignissen.
            Assert.Null(KatalogBereinigung.Sperrgrund(k, frei));
            Assert.True(KatalogBereinigung.SatzLoeschen(k, frei));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_TwwBedarfstag_STAMM\" WHERE \"ID\" = ?", new DbParam("@id", frei)));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_TwwBedarfstagEreignis_STAMM\" WHERE \"ID_Bedarfstag\" = ?", new DbParam("@id", frei)));
        }

        [Fact]
        public void Eine_ausgelieferte_oder_benutzte_Nutzungsart_bleibt_eine_freie_geht()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int aus = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz, TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);
            int benutzt = TwwTestdatenbank.NutzungsartAnlegen("Nutzung B", "T1", satz);
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung C", "T1", satz);
            TwwTestdatenbank.ZoneAnlegen(1, benutzt, "Zone", 10.0);
            TwwTestdatenbank.ZoneAnlegen(2, benutzt, "Zone", 10.0);
            KatalogDefinition k = Katalog("TWW_NUTZUNGSART");

            Assert.Equal("schreibgeschuetzt (ReadOnly)", KatalogBereinigung.Sperrgrund(k, aus));
            Assert.Equal("verwendet: Tab_TwwZone (2)", KatalogBereinigung.Sperrgrund(k, benutzt));
            Assert.False(KatalogBereinigung.SatzLoeschen(k, aus));
            Assert.False(KatalogBereinigung.SatzLoeschen(k, benutzt));
            Assert.True(KatalogBereinigung.SatzLoeschen(k, frei));
            Assert.Equal(new[] { aus, benutzt }, TwwNutzungsartCtrl.Liste().Select(z => z.Id).OrderBy(i => i).ToArray());
        }

        // =================================================================================
        // Zapfkategorien (Schemaschritt T2): Datenblock der Nutzungsart
        // =================================================================================

        [Fact]
        public void Eine_freie_Nutzungsart_geht_samt_ihren_Zapfkategorien()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            int andere = TwwTestdatenbank.NutzungsartAnlegen("Nutzung B", "T1", satz);
            TwwTestdatenbank.KategorieAnlegen(frei, "Kurz", 1, 4.0, 1, 0.5, 1.0);
            TwwTestdatenbank.KategorieAnlegen(frei, "Lang", 2, 8.0, 5, 0.5, 2.0, kappung: 12.0);
            TwwTestdatenbank.KategorieAnlegen(andere, "Kurz", 1, 4.0, 1, 1.0, 1.0);
            KatalogDefinition k = Katalog("TWW_NUTZUNGSART");

            Assert.Null(KatalogBereinigung.Sperrgrund(k, frei));
            Assert.True(KatalogBereinigung.SatzLoeschen(k, frei));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM \"Tab_TwwZapfkategorie_STAMM\" WHERE \"ID_Nutzungsart\" = ?", new DbParam("@id", frei)));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"Tab_TwwZapfkategorie_STAMM\""));
        }

        /// <summary>
        /// Eine ausgelieferte Kategorie (ReadOnly) ist unveränderlich wie ein ausgelieferter
        /// Kopf: Sie sperrt ihre Nutzungsart, auch wenn deren eigene Zeile beschreibbar ist.
        /// </summary>
        [Fact]
        public void Eine_ausgelieferte_Zapfkategorie_sperrt_ihre_Nutzungsart()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int nutzung = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            TwwTestdatenbank.KategorieAnlegen(nutzung, "Kurz", 1, 4.0, 1, 1.0, 1.0,
                                              status: TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);
            KatalogDefinition k = Katalog("TWW_NUTZUNGSART");

            Assert.Equal("schreibgeschuetzt (ReadOnly in Tab_TwwZapfkategorie_STAMM)", KatalogBereinigung.Sperrgrund(k, nutzung));
            Assert.False(KatalogBereinigung.SatzLoeschen(k, nutzung));
            Assert.False(KatalogBereinigung.SatzUmbenennen(k, nutzung, "Neu A"));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"Tab_TwwZapfkategorie_STAMM\""));
            Assert.NotNull(TwwNutzungsartCtrl.Lies(nutzung));
        }

        /// <summary>
        /// Eine Datenbank vor Schritt 115 führt die Kategorien nicht: Löschen und Dublettenscan
        /// laufen ohne den Block, statt an der fehlenden Tabelle zu scheitern.
        /// </summary>
        [Fact]
        public void Ohne_Kategorientabelle_laufen_Loeschen_und_Scan_wie_zuvor()
        {
            using var db = new TwwTestdatenbank(mitTwwSchema: false);
            TwwTestdatenbank.SchemaAnlegen(mitT2: false);
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            TwwTestdatenbank.NutzungsartAnlegen("Nutzung B", "T1", satz);
            KatalogDefinition k = Katalog("TWW_NUTZUNGSART");

            ScanErgebnis scan = DublettenPruefung.ScanKatalog(k);
            Assert.Null(scan.Fehler);
            Assert.Equal(2, scan.Saetze.Count);
            Assert.Equal(new[] { "" }, DublettenPruefung.BlockHashes(k, frei));
            Assert.True(KatalogBereinigung.SatzLoeschen(k, frei));
            Assert.Null(TwwNutzungsartCtrl.Lies(frei));
        }

        /// <summary>Die Kategorien zählen zum Inhalt: Zwei sonst gleiche Nutzungsarten mit anderen Kategorien sind keine Inhaltsdublette.</summary>
        [Fact]
        public void Die_Zapfkategorien_zaehlen_zum_Inhalt_der_Nutzungsart()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int a = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            int b = TwwTestdatenbank.NutzungsartAnlegen("Nutzung B", "T1", satz);
            int c = TwwTestdatenbank.NutzungsartAnlegen("Nutzung C", "T1", satz);
            TwwTestdatenbank.KategorieAnlegen(a, "Kurz", 1, 4.0, 1, 1.0, 1.0);
            TwwTestdatenbank.KategorieAnlegen(b, "Kurz", 1, 4.0, 1, 1.0, 1.0);
            TwwTestdatenbank.KategorieAnlegen(c, "Kurz", 1, 4.0, 1, 1.0, 2.0);

            ScanErgebnis scan = DublettenPruefung.ScanKatalog(Katalog("TWW_NUTZUNGSART"));
            string Hash(int id) => scan.Saetze.Single(s => s.Id == id).InhaltsHash;
            Assert.Equal(Hash(a), Hash(b));
            Assert.NotEqual(Hash(a), Hash(c));
        }

        // =================================================================================
        // Zapfkategorien in der Pflege der Nutzungsart (TwwNutzungsartCtrl)
        // =================================================================================

        /// <summary>Die Kategorien einer Nutzungsart als Zeilentext, nach Reihenfolge — Werte, Provenienz, Status, ReadOnly.</summary>
        private static string Kategorien(int idNutzungsart)
            => string.Join("\n", DataRepository.GetDataTable(
                   "SELECT \"Kategorie\", \"Reihenfolge\", \"Volumenstrom_l_min\", \"Dauer_min\", \"Anteil\", \"Sigma\", " +
                   "\"Kappung_l_min\", \"Quelle\", \"Version\", \"Herkunftsart\", \"Status\", \"ReadOnly\" " +
                   "FROM \"Tab_TwwZapfkategorie_STAMM\" WHERE \"ID_Nutzungsart\" = ? ORDER BY \"Reihenfolge\", \"ID\"",
                   new DbParam("@id", idNutzungsart))
               .Rows.Cast<System.Data.DataRow>()
               .Select(r => string.Join("|", r.ItemArray.Select(x => System.Convert.ToString(x, System.Globalization.CultureInfo.InvariantCulture)))));

        /// <summary>
        /// Eine ausgelieferte Kategorie (<c>ReadOnly</c>) sperrt ihre Nutzungsart auch in der Pflege:
        /// Ändern und Löschen lehnen benannt ab, und „Tagesgang…" schreibt nicht an Ort und Stelle,
        /// sondern legt eine neue Version an — samt den Kategorien der alten (Status <c>EIGEN</c>,
        /// <c>ReadOnly = 0</c>). Die alte bleibt unberührt.
        /// </summary>
        [Fact]
        public void Eine_ausgelieferte_Zapfkategorie_sperrt_auch_die_Pflege_und_Tagesgang_kopiert_sie()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int id = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            TwwTestdatenbank.KategorieAnlegen(id, "Kurz", 1, 4.0, 1, 0.5, 1.0,
                                              status: TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);
            TwwTestdatenbank.KategorieAnlegen(id, "Lang", 2, 8.0, 5, 0.5, 2.0, kappung: 12.0);
            string vorher = Kategorien(id);

            Assert.True(TwwNutzungsartCtrl.IstReadOnly(id));
            TwwNutzungsartEntwurf e = TwwNutzungsartEntwurf.Aus(TwwNutzungsartCtrl.Lies(id));
            Assert.Equal(TwwKatalogAusgang.ReadOnlyGesperrt, TwwNutzungsartCtrl.Aendern(id, e with { Bezeichner = "Nutzung A2" }).Ausgang);
            Assert.Equal(TwwKatalogAusgang.ReadOnlyGesperrt, TwwNutzungsartCtrl.Loeschen(id).Ausgang);
            Assert.Equal("Nutzung A", TwwNutzungsartCtrl.Lies(id).Name);

            var gaenge = Enumerable.Range(0, 4)
                .Select(_ => Enumerable.Range(1, 24).Select(h => h == 7 || h == 19 ? 0.5 : 0.0).ToArray()).ToList();
            double[] woche = { 0.1, 0.1, 0.2, 0.2, 0.2, 0.1, 0.1 };
            Assert.Equal(TwwKatalogAusgang.EntwurfUnvollstaendig, TwwNutzungsartCtrl.TagesgangSpeichern(id, gaenge, woche).Ausgang);

            TwwTagesgangErgebnis erg = TwwNutzungsartCtrl.TagesgangSpeichern(id, gaenge, woche, "T2");
            Assert.True(erg.Ok);
            Assert.NotEqual(id, erg.IdNutzungsart);
            Assert.Equal(vorher, Kategorien(id));                                 // die alte bleibt
            string kopie = Kategorien(erg.IdNutzungsart);
            Assert.Equal("Kurz|1|4|1|0.5|1||" + TwwTestdatenbank.QUELLE + "|T1|FIKTIV|EIGEN|False\n" +
                         "Lang|2|8|5|0.5|2|12|" + TwwTestdatenbank.QUELLE + "|T1|FIKTIV|EIGEN|False", kopie);
            Assert.False(TwwNutzungsartCtrl.IstReadOnly(erg.IdNutzungsart));
        }

        /// <summary>
        /// „Speichern unter" nimmt die Kategorien der Vorlage mit — Werte, Reihenfolge und Provenienz
        /// unverändert, Status wie die Kopie, <c>ReadOnly = 0</c> —, sodass die neue Version
        /// stochastisch rechnet wie die alte; die Vorlage behält ihre. Ohne Kategorientabelle (Stand
        /// vor Schritt 115) läuft „Speichern unter" wie zuvor.
        /// </summary>
        [Fact]
        public void Speichern_unter_nimmt_die_Zapfkategorien_der_Vorlage_mit()
        {
            using (var db = new TwwTestdatenbank())
            {
                int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
                int alt = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz,
                                                                status: TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);
                TwwTestdatenbank.KategorieAnlegen(alt, "Lang", 2, 8.0, 5, 0.25, 2.0, kappung: 12.0,
                                                  status: TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);
                TwwTestdatenbank.KategorieAnlegen(alt, "Kurz", 1, 4.0, 1, 0.75, 1.0,
                                                  status: TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);
                string vorher = Kategorien(alt);

                TwwKatalogErgebnis erg = TwwNutzungsartCtrl.SpeichernUnter(alt,
                    TwwNutzungsartEntwurf.Aus(TwwNutzungsartCtrl.Lies(alt)) with { Katalogversion = "T2" });
                Assert.True(erg.Ok);
                Assert.Equal(vorher, Kategorien(alt));
                Assert.Equal("Kurz|1|4|1|0.75|1||" + TwwTestdatenbank.QUELLE + "|T1|FIKTIV|EIGEN|False\n" +
                             "Lang|2|8|5|0.25|2|12|" + TwwTestdatenbank.QUELLE + "|T1|FIKTIV|EIGEN|False", Kategorien(erg.Id));
                Assert.False(TwwNutzungsartCtrl.IstReadOnly(erg.Id));

                // Die Kopie rechnet mit denselben Kategorien wie die Vorlage.
                Zapfkategoriensatz a = Zapfkategoriensatz.Aus(ZapfprofilCtrl.Zapfkategorien(new[] { alt }), alt, "Zone");
                Zapfkategoriensatz b = Zapfkategoriensatz.Aus(ZapfprofilCtrl.Zapfkategorien(new[] { erg.Id }), erg.Id, "Zone");
                Assert.Equal(a.Werte.Select(w => w.EnergieJeKelvinKwh), b.Werte.Select(w => w.EnergieJeKelvinKwh));
            }
            using (var db = new TwwTestdatenbank(mitTwwSchema: false))
            {
                TwwTestdatenbank.SchemaAnlegen(mitT2: false);
                int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
                int alt = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
                Assert.True(TwwNutzungsartCtrl.SpeichernUnter(alt,
                    TwwNutzungsartEntwurf.Aus(TwwNutzungsartCtrl.Lies(alt)) with { Katalogversion = "T2" }).Ok);
                Assert.False(TwwNutzungsartCtrl.IstReadOnly(alt));
            }
        }

        // =================================================================================
        // Umbenennen
        // =================================================================================

        [Fact]
        public void Eine_benutzte_oder_ausgelieferte_Zeile_behaelt_ihren_Namen()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int aus = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz, TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);
            int benutzt = TwwTestdatenbank.NutzungsartAnlegen("Nutzung B", "T1", satz);
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung C", "T1", satz);
            TwwTestdatenbank.ZoneAnlegen(1, benutzt, "Zone", 10.0);
            KatalogDefinition k = Katalog("TWW_NUTZUNGSART");

            Assert.False(KatalogBereinigung.SatzUmbenennen(k, aus, "Neu A"));
            Assert.False(KatalogBereinigung.SatzUmbenennen(k, benutzt, "Neu B"));
            Assert.False(KatalogBereinigung.SatzUmbenennen(Katalog("TWW_TAGESGANGSATZ"), satz, "Neu Satz"));
            Assert.True(KatalogBereinigung.SatzUmbenennen(k, frei, "Neu C"));
            Assert.False(KatalogBereinigung.SatzUmbenennen(k, 9999, "Neu D"));

            Assert.Equal("Nutzung A", TwwNutzungsartCtrl.Lies(aus).Name);
            Assert.Equal("Nutzung B", TwwNutzungsartCtrl.Lies(benutzt).Name);
            Assert.Equal("Neu C", TwwNutzungsartCtrl.Lies(frei).Name);
            Assert.Equal("Satz A", ZapfprofilCtrl.Tagesgangsaetze().Single().Bezeichner);
        }

        // =================================================================================
        // Bereinigen (Leerkopien-Regel)
        // =================================================================================

        [Fact]
        public void Eine_benutzte_Dublette_mit_leerer_Katalogversion_wird_nicht_bereinigt()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int behalten = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            // Eine leere Katalogversion erfüllt NOT NULL und gilt der Regel als Leerwert
            // (Handänderung, Import) - sie trägt sonst dieselben Werte.
            int leer = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "", satz);
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_TwwNutzungsart_STAMM\" SET \"Bedarf_Version\" = 'T1', " +
                                           "\"Jahresgang_Version\" = 'T1', \"Wochengang_Version\" = 'T1' WHERE \"ID\" = ?",
                                           new DbParam("@id", leer));
            TwwTestdatenbank.ZoneAnlegen(1, leer, "Zone", 10.0);
            KatalogDefinition k = Katalog("TWW_NUTZUNGSART");

            // Der natürliche Schlüssel trennt die beiden - keine Namensgruppe, nichts zu bereinigen.
            ScanErgebnis scan = DublettenPruefung.ScanKatalog(k);
            Assert.Empty(scan.Namensgruppen);
            Assert.Equal(0, KatalogBereinigung.LeereKopienBereinigen(k).Geloescht);

            // Auch als Gruppe von Hand gereicht (etwa aus dem Scan eines älteren Stands): Die
            // benutzte Zeile bleibt, die Regel meldet sie als offen.
            var gruppe = new DublettenGruppe();
            gruppe.Saetze.Add(scan.Saetze.Single(s => s.Id == behalten));
            gruppe.Saetze.Add(scan.Saetze.Single(s => s.Id == leer));
            var erg = new BereinigungsErgebnis();
            KatalogBereinigung.GruppeBereinigen(k, gruppe, erg);
            Assert.Equal(0, erg.Geloescht);
            Assert.Equal(1, erg.Offen);
            Assert.Contains(erg.Protokoll, z => z.Contains("gesperrt (verwendet: Tab_TwwZone (1))"));
            Assert.NotNull(TwwNutzungsartCtrl.Lies(leer));

            // Ohne Zone greift die Regel wie bei jedem Katalog.
            DataRepository.ExecuteNonQuery("DELETE FROM \"Tab_TwwZone\"");
            var erg2 = new BereinigungsErgebnis();
            KatalogBereinigung.GruppeBereinigen(k, gruppe, erg2);
            Assert.Equal(1, erg2.Geloescht);
            Assert.Null(TwwNutzungsartCtrl.Lies(leer));
        }

        [Fact]
        public void Gleicher_Bezeichner_und_gleiche_Version_in_anderer_Schreibweise_bleibt_eine_Namensgruppe()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            TwwTestdatenbank.NutzungsartAnlegen("nutzung  a", "t1", satz);
            TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T2", satz);

            ScanErgebnis scan = DublettenPruefung.ScanKatalog(Katalog("TWW_NUTZUNGSART"));
            DublettenGruppe g = Assert.Single(scan.Namensgruppen);
            Assert.Equal(2, g.Saetze.Count);
        }
    }
}
