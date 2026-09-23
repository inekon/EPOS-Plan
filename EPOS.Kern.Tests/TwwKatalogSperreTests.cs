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
