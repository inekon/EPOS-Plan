using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Ergebnisspalten der Aufheizoptimierung</b> (Schemaschritt KP-S3, Entwurf KP3 Abschnitt 4,
    /// Grundsatz 4) — der Weg über <see cref="ErgebnisCtrl"/> nach <c>Tab_ErgebnisGebaeude</c> und
    /// <c>Tab_ErgebnisZone</c> und zurück.
    ///
    /// <para><b>Geprüft wird:</b> ein Lauf lässt alle Aufheizspalten leer, und NULL bleibt über Speichern
    /// und Lesen NULL („Schalter aus"); gesetzte Werte am Gebäude und an den Zonen kommen gleich zurück,
    /// auch ein gekoppeltes Gebäude nur mit den Kappungsstunden; die Zonenzeile entsteht aus der
    /// Spaltenliste nach Vorhandensein (B23) — eine Datenbank vor KP-S3 schreibt und liest wie vorher;
    /// Variante und Duplikat lassen das Ergebnis beim Quellprojekt (die Kopie nimmt keine Rechenergebnisse
    /// mit) und rechnen ihr eigenes; der Projekttransfer trägt die Spalten über die Paketgrenze, und ein
    /// Paket vom Stand davor wird ohne Umformung gehoben — Schalter 0, Spalten leer.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ErgebnisAufheizTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Projekt 1039: zwei Gebäude auf dem VDI-Weg (Muster <c>ErgebnisZoneTests</c>).</summary>
        private const int PROJEKT = 1039;

        // =============================================================================
        //  Teil 1 - NULL-Rundlauf und gesetzte Werte
        // =============================================================================

        /// <summary>
        /// Ein Lauf schreibt seine Gebäudezeilen und lässt die vierzehn Aufheizspalten leer; über Lesen und
        /// erneutes Speichern bleibt NULL NULL — „Schalter aus" ist überall dasselbe (Grundsatz 4).
        /// </summary>
        [Fact]
        public void Ein_Lauf_laesst_die_Aufheizspalten_leer_und_NULL_bleibt_NULL()
        {
            if (!_db.Vorhanden) return;
            int kopf = Lauf();

            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = " + kopf) >= 2);
            foreach (KeyValuePair<string, string> s in AufheizErgebnisSchema.SpaltenGebaeude)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = " + kopf +
                                      " AND \"" + s.Key + "\" IS NOT NULL"));

            ErgebnisModel m = new ErgebnisCtrl().Load(PROJEKT);
            Assert.All(m.Gebaeude, g => Assert.Equal(Leer(), Abdruck(g)));

            // Mit einer Zone ohne Aufheizwerte: auch die Zonenzeile bleibt leer.
            m.Gebaeude[0].Zonen.Add(new ErgebnisZoneModel { Rang = 1, Bezeichner = "Halle", IstBeheizt = true, HeizwaermeMwh = 1.5 });
            Assert.True(new ErgebnisCtrl().Save(m) > 0);
            foreach (KeyValuePair<string, string> s in AufheizErgebnisSchema.SpaltenZone)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisZone WHERE \"" + s.Key + "\" IS NOT NULL"));

            ErgebnisModel geladen = new ErgebnisCtrl().Load(PROJEKT);
            Assert.All(geladen.Gebaeude, g => Assert.Equal(Leer(), Abdruck(g)));
            Assert.Equal(LeerZone(), Abdruck(geladen.Gebaeude[0].Zonen.Single()));
            Assert.Equal(1.5, geladen.Gebaeude[0].Zonen[0].HeizwaermeMwh);
        }

        /// <summary>
        /// Gesetzte Werte gehen nach <c>Tab_ErgebnisGebaeude</c> und <c>Tab_ErgebnisZone</c> und kommen
        /// gleich zurück: ein bemessenes Gebäude, ein gekoppeltes nur mit den Kappungsstunden (Festlegung 25),
        /// eine bemessene und eine unbeheizte Zone samt Sommerlüftungsstunden; NULL bleibt null.
        /// </summary>
        [Fact]
        public void Gesetzte_Werte_gehen_nach_Gebaeude_und_Zone_und_kommen_gleich_zurueck()
        {
            if (!_db.Vorhanden) return;
            Lauf();

            ErgebnisModel m = new ErgebnisCtrl().Load(PROJEKT);
            Setzen(m);
            Assert.True(new ErgebnisCtrl().Save(m) > 0);

            ErgebnisModel geladen = new ErgebnisCtrl().Load(PROJEKT);
            Assert.Equal(Abdruck(m.Gebaeude[0]), Abdruck(geladen.Gebaeude[0]));
            Assert.Equal(Abdruck(m.Gebaeude[1]), Abdruck(geladen.Gebaeude[1]));
            Assert.Equal(2, geladen.Gebaeude[1].Zonen.Count);
            Assert.Equal(Abdruck(m.Gebaeude[1].Zonen[0]), Abdruck(geladen.Gebaeude[1].Zonen[0]));
            Assert.Equal(Abdruck(m.Gebaeude[1].Zonen[1]), Abdruck(geladen.Gebaeude[1].Zonen[1]));
            Assert.Equal("Wohnen", geladen.Gebaeude[1].Zonen[0].Bezeichner);
            Assert.Equal(4, geladen.Gebaeude[1].Zonen[0].MusterwechselH);
            Assert.Equal(17, geladen.Gebaeude[1].Zonen[0].NachtauskuehlstundenH);

            // Das gekoppelte Gebäude: nur die Kappungsstunden, alles übrige NULL.
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, geladen.Gebaeude[1].AufheizZustand);
            Assert.Equal(3.75, geladen.Gebaeude[1].HeizleistungMaxStundenH);
            Assert.Null(geladen.Gebaeude[1].AufheizzeitMaxH);
            Assert.Null(geladen.Gebaeude[1].AufheizLeistungKw);
        }

        /// <summary>
        /// <b>B23:</b> Die Zonenzeile entsteht aus der Spaltenliste nach Vorhandensein. Auf einer Datenbank
        /// vor KP-S3 (und hier zusätzlich ohne die Nachtauskühlstunden der Zone) schreibt das Speichern, was
        /// die Tabellen tragen, und das Lesen liefert für die fehlenden Spalten <c>null</c>.
        /// </summary>
        [Fact]
        public void Ohne_die_Spalten_schreibt_und_liest_das_Ergebnis_wie_vorher()
        {
            if (!_db.Vorhanden) return;
            Lauf();
            foreach (KeyValuePair<string, string> s in AufheizErgebnisSchema.SpaltenGebaeude)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_ErgebnisGebaeude\" DROP COLUMN \"" + s.Key + "\"");
            foreach (KeyValuePair<string, string> s in AufheizErgebnisSchema.SpaltenZone)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_ErgebnisZone\" DROP COLUMN \"" + s.Key + "\"");
            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_ErgebnisZone\" DROP COLUMN \"" +
                                           KonditionierungVorlagenSchema.SPALTE_NACHTAUSKUEHLSTUNDEN + "\"");

            ErgebnisModel m = new ErgebnisCtrl().Load(PROJEKT);
            Setzen(m);
            Assert.True(new ErgebnisCtrl().Save(m) > 0);

            ErgebnisModel geladen = new ErgebnisCtrl().Load(PROJEKT);
            Assert.All(geladen.Gebaeude, g => Assert.Equal(Leer(), Abdruck(g)));
            Assert.Equal(2, geladen.Gebaeude[1].Zonen.Count);
            ErgebnisZoneModel z = geladen.Gebaeude[1].Zonen[0];
            Assert.Equal(LeerZone(), Abdruck(z));
            Assert.Null(z.NachtauskuehlstundenH);
            Assert.Equal("Wohnen", z.Bezeichner);
            Assert.Equal(12.5, z.HeizwaermeMwh);
            Assert.Equal(4, z.MusterwechselH);
            Assert.False(geladen.Gebaeude[1].Zonen[1].IstBeheizt);
        }

        // =============================================================================
        //  Teil 2 - Variante, Duplikat, Projekttransfer, Paketanhebung
        // =============================================================================

        /// <summary>
        /// Variante und Duplikat gehen über den Kopierweg, der Rechenergebnisse nicht mitnimmt
        /// (<c>ProjektDuplizierenCtrl.IstErgebnisTabelle</c>): Das Ergebnis mit Aufheizwerten bleibt beim
        /// Quellprojekt unverändert, die Kopien starten ohne Ergebnis — und ihr eigenes Ergebnis trägt die
        /// Spalten unabhängig vom Quellprojekt.
        /// </summary>
        [Fact]
        public void Variante_und_Duplikat_lassen_das_Ergebnis_beim_Quellprojekt()
        {
            if (!_db.Vorhanden) return;
            Lauf();
            ErgebnisModel m = new ErgebnisCtrl().Load(PROJEKT);
            Setzen(m);
            Assert.True(new ErgebnisCtrl().Save(m) > 0);
            string vorher = Abdruck(new ErgebnisCtrl().Load(PROJEKT));

            string name = StartseiteCtrl.Projektname(PROJEKT);
            int variante = new VariantenCtrl().AnlegenAusStamm(PROJEKT, name, "Aufheizung", out string fehler);
            Assert.True(variante > 0, "Variante: " + fehler);
            int duplikat = new ProjektDuplizierenCtrl().Duplizieren(name, name + " Aufheizung");
            Assert.True(duplikat > 0, "Duplizieren fehlgeschlagen.");
            Assert.False(new ErgebnisCtrl().HasErgebnis(variante));
            Assert.False(new ErgebnisCtrl().HasErgebnis(duplikat));
            Assert.Equal(vorher, Abdruck(new ErgebnisCtrl().Load(PROJEKT)));

            // Die Variante rechnet ihr eigenes Ergebnis: andere Aufheizwerte, das Quellprojekt bleibt.
            ErgebnisModel v = new ErgebnisCtrl().Load(PROJEKT);
            v.ID_Projekt = variante;
            v.Gebaeude[0].AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR;
            v.Gebaeude[0].AufheizzeitMaxH = null;
            v.Gebaeude[0].AufheiztageUnerreichbar = 41;
            Assert.True(new ErgebnisCtrl().Save(v) > 0);
            ErgebnisModel geladen = new ErgebnisCtrl().Load(variante);
            Assert.Equal(Abdruck(v.Gebaeude[0]), Abdruck(geladen.Gebaeude[0]));
            Assert.Equal(vorher, Abdruck(new ErgebnisCtrl().Load(PROJEKT)));
        }

        /// <summary>
        /// Der Projekttransfer (Export und Import eines <c>.wpx</c>-Pakets) nimmt das Ergebnis mit — die
        /// vierzehn Spalten je Gebäude und Zone kommen bitgleich an, NULL bleibt NULL.
        /// </summary>
        [Fact]
        public void Der_Projekttransfer_traegt_die_Aufheizspalten()
        {
            if (!_db.Vorhanden) return;
            Lauf();
            ErgebnisModel m = new ErgebnisCtrl().Load(PROJEKT);
            Setzen(m);
            Assert.True(new ErgebnisCtrl().Save(m) > 0);
            string vorher = Abdruck(new ErgebnisCtrl().Load(PROJEKT));

            using var ordner = new Arbeitsordner();
            string paket = ordner.Datei("p.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(StartseiteCtrl.Projektname(PROJEKT), paket));
            int neu = io.Importieren(paket, "Transfer Aufheizergebnis", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            Assert.Equal(vorher, Abdruck(new ErgebnisCtrl().Load(neu)));
        }

        /// <summary>
        /// <b>Die Paketanhebung:</b> Ein Paket vom Stand der Änderungsstempel (159) kennt weder die
        /// Projektspalten noch die Ergebnisspalten. Es wird ohne Umformung gehoben (beide Stufen sind DDL,
        /// kein Berichtseintrag) — der Schalter steht auf 0, die Aufheizspalten bleiben leer, das übrige
        /// Ergebnis kommt an.
        /// </summary>
        [Fact]
        public void Ein_Paket_vom_Stand_davor_wird_ohne_Umformung_gehoben()
        {
            if (!_db.Vorhanden) return;
            Lauf();
            ErgebnisModel m = new ErgebnisCtrl().Load(PROJEKT);
            Setzen(m);
            Assert.True(new ErgebnisCtrl().Save(m) > 0);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(PROJEKT,
                new Aufheizvorgabe(true, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, 3.0, 0.3, null)));

            using var ordner = new Arbeitsordner();
            string paket = ordner.Datei("neu.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(StartseiteCtrl.Projektname(PROJEKT), paket));

            int stand = KostenStempelSchema.SCHRITT;
            var neueSpalten = new HashSet<string>(
                AufheizvorgabeSchema.SPALTEN.Select(s => s.Key)
                    .Concat(AufheizErgebnisSchema.SpaltenGebaeude.Select(s => s.Key))
                    .Concat(AufheizErgebnisSchema.SpaltenZone.Select(s => s.Key)), StringComparer.Ordinal);
            int entfernt = 0;
            string alt = ordner.Datei("alt.wpx");
            Umbauen(paket, alt, stand, (tabelle, zeile) =>
            {
                bool betroffen = tabelle == "Tab_Einstellungen" || tabelle == "Tab_ErgebnisGebaeude" || tabelle == "Tab_ErgebnisZone";
                if (!betroffen) return;
                foreach (string sp in neueSpalten)
                {
                    // Sommerlueftungsstunden_H stand am Gebäude schon vor KP-S3.
                    if (tabelle == "Tab_ErgebnisGebaeude" && sp == AufheizErgebnisSchema.SPALTE_SOMMERLUEFTUNG) continue;
                    if (zeile.Remove(sp)) entfernt++;
                }
            });
            Assert.True(entfernt > 0, "Das Paket trug keine der neuen Spalten.");

            Paketanhebung.Vorschau v = Paketanhebung.Vorschauen(stand);
            Assert.True(v.Noetig);
            int neu = io.Importieren(alt, "Paket vor KP3", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            Assert.DoesNotContain(io.LetzterBericht, z => z.StartsWith("Schritt " + AufheizvorgabeSchema.SCHRITT, StringComparison.Ordinal));
            Assert.DoesNotContain(io.LetzterBericht, z => z.StartsWith("Schritt " + AufheizErgebnisSchema.SCHRITT, StringComparison.Ordinal));

            Assert.Equal(Aufheizvorgabe.Aus, KonfigurationCtrl.AufheizvorgabeLesen(neu));
            ErgebnisModel geladen = new ErgebnisCtrl().Load(neu);
            Assert.Equal(m.Gebaeude.Count, geladen.Gebaeude.Count);
            Assert.All(geladen.Gebaeude, g => Assert.Equal(Leer(), Abdruck(g)));
            Assert.Equal(2, geladen.Gebaeude[1].Zonen.Count);
            Assert.All(geladen.Gebaeude[1].Zonen, z => Assert.Equal(LeerZone(), Abdruck(z)));
            Assert.Equal(m.Gebaeude[0].HeizwaermeMwh, geladen.Gebaeude[0].HeizwaermeMwh);
            Assert.Equal(12.5, geladen.Gebaeude[1].Zonen[0].HeizwaermeMwh);
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        /// <summary>Ein Lauf des Projekts; liefert den Kopf des Ergebnisses.</summary>
        private static int Lauf()
        {
            var laeufer = new SimulationRunner();
            int kopf = laeufer.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            return kopf;
        }

        /// <summary>
        /// Setzt die Aufheizwerte: Gebäude 1 bemessen mit allen vierzehn Werten, Gebäude 2 gekoppelt nur mit
        /// den Kappungsstunden und mit zwei Zonen — eine bemessen samt Sommerlüftung, eine unbeheizt.
        /// </summary>
        private static void Setzen(ErgebnisModel m)
        {
            Assert.True(m.Gebaeude.Count >= 2, "Projekt " + PROJEKT + " führt weniger als zwei Gebäude.");
            ErgebnisGebaeudeModel a = m.Gebaeude[0];
            a.AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN;
            a.AufheizBemessung = DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG;
            a.AufheizzeitMaxH = 5;
            a.AufheizAussenC = -14.2;
            a.AufheizLeistungKw = 37.0412345678901;
            a.AufheizLeistungsquelle = DbWerte.AUFHEIZ_QUELLE_GEMISCHT;
            a.Aufheiztage = 211;
            a.AufheiztageBegrenzt = 3;
            a.AufheiztageUnerreichbar = 0;
            a.AufheiztageNachweisband = 2;
            a.AufheizstundenH = 512;
            a.AufheizzeitLaengsteH = 6;
            a.AufheizspruengeAus = 1;
            a.HeizleistungMaxStundenH = 0.125;

            ErgebnisGebaeudeModel b = m.Gebaeude[1];
            b.AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT;
            b.HeizleistungMaxStundenH = 3.75;
            b.Zonen.Clear();
            b.Zonen.Add(new ErgebnisZoneModel
            {
                Rang = 1, Bezeichner = "Wohnen", IstBeheizt = true, HeizwaermeMwh = 12.5, SpitzeKw = 7.25,
                MittlereRaumtemperaturC = 20.4, UeberhitzungsstundenH = 12, DeltaThetaMaxK = 9.5, DurchlaeufeMax = 3,
                MusterwechselH = 4, NachtauskuehlstundenH = 17, SommerlueftungsstundenH = 640,
                AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, AufheizzeitMaxH = 47, AufheizAussenC = -16.0,
                AufheizLeistungKw = 9.5, AufheizLeistungsquelle = DbWerte.AUFHEIZ_QUELLE_GRENZE, Aufheiztage = 365,
                AufheiztageBegrenzt = 0, AufheiztageUnerreichbar = 1, AufheiztageNachweisband = 0,
                AufheizstundenH = 8760, AufheizzeitLaengsteH = 0, AufheizspruengeAus = 8760, HeizleistungMaxStundenH = 8760,
            });
            b.Zonen.Add(new ErgebnisZoneModel
            {
                Rang = 2, Bezeichner = "Keller", IstBeheizt = false, MittlereRaumtemperaturC = 11.0,
                UeberhitzungsstundenH = 0, SommerlueftungsstundenH = 0,
                AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT,
            });
        }

        /// <summary>Die vierzehn Aufheizwerte eines Gebäudes als Text — <c>~</c> für null, Zahlen rundlaufsicher.</summary>
        private static string Abdruck(ErgebnisGebaeudeModel g) => string.Join("|",
            T(g.AufheizZustand), T(g.AufheizBemessung), Z(g.AufheizzeitMaxH), Z(g.AufheizAussenC), Z(g.AufheizLeistungKw),
            T(g.AufheizLeistungsquelle), Z(g.Aufheiztage), Z(g.AufheiztageBegrenzt), Z(g.AufheiztageUnerreichbar),
            Z(g.AufheiztageNachweisband), Z(g.AufheizstundenH), Z(g.AufheizzeitLaengsteH), Z(g.AufheizspruengeAus),
            Z(g.HeizleistungMaxStundenH));

        /// <summary>Die vierzehn Aufheizwerte einer Zone (mit den Sommerlüftungsstunden) als Text.</summary>
        private static string Abdruck(ErgebnisZoneModel z) => string.Join("|",
            T(z.AufheizZustand), Z(z.AufheizzeitMaxH), Z(z.AufheizAussenC), Z(z.AufheizLeistungKw),
            T(z.AufheizLeistungsquelle), Z(z.Aufheiztage), Z(z.AufheiztageBegrenzt), Z(z.AufheiztageUnerreichbar),
            Z(z.AufheiztageNachweisband), Z(z.AufheizstundenH), Z(z.AufheizzeitLaengsteH), Z(z.AufheizspruengeAus),
            Z(z.HeizleistungMaxStundenH), Z(z.SommerlueftungsstundenH));

        /// <summary>Alle Gebäude und Zonen eines Ergebnisses samt ihrer Aufheizwerte.</summary>
        private static string Abdruck(ErgebnisModel m) => string.Join("\n",
            m.Gebaeude.Select(g => g.Merkplatz + ":" + Abdruck(g) + "[" +
                                   string.Join(";", g.Zonen.Select(z => z.Rang + "=" + Abdruck(z))) + "]"));

        private static string Leer() => string.Join("|", Enumerable.Repeat("~", 14));

        private static string LeerZone() => string.Join("|", Enumerable.Repeat("~", 14));

        private static string T(string s) => s ?? "~";

        private static string Z(int? w) => w.HasValue ? w.Value.ToString(CultureInfo.InvariantCulture) : "~";

        private static string Z(double? w) => w.HasValue ? w.Value.ToString("R", CultureInfo.InvariantCulture) : "~";

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>
        /// Schreibt das Paket mit anderem Schemastand neu und lässt jede Zeile jedes Projektbaums umbauen
        /// (Muster <c>ProjektpaketAnhebungTests.Umbauen</c>).
        /// </summary>
        private static void Umbauen(string quelle, string ziel, int stand, Action<string, JsonObject> zeile)
        {
            var eintraege = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            using (ZipArchive zip = ZipFile.OpenRead(quelle))
                foreach (ZipArchiveEntry e in zip.Entries)
                {
                    using Stream s = e.Open();
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    eintraege[e.FullName] = ms.ToArray();
                }

            var utf8 = new UTF8Encoding(false);
            using var stream = new FileStream(ziel, FileMode.Create);
            using var aus = new ZipArchive(stream, ZipArchiveMode.Create);
            foreach (KeyValuePair<string, byte[]> kvp in eintraege)
            {
                byte[] roh = kvp.Value;
                if (kvp.Key == "manifest.json")
                {
                    JsonObject mf = JsonNode.Parse(utf8.GetString(roh)).AsObject();
                    mf["schemaVersion"] = stand;
                    roh = utf8.GetBytes(mf.ToJsonString());
                }
                else if (kvp.Key.StartsWith("data/", StringComparison.Ordinal) ||
                         (kvp.Key.StartsWith("projects/", StringComparison.Ordinal) && kvp.Key.Contains("/data/")))
                {
                    string tabelle = Path.GetFileNameWithoutExtension(kvp.Key);
                    JsonArray zeilen = JsonNode.Parse(utf8.GetString(roh)).AsArray();
                    foreach (JsonNode z in zeilen) zeile(tabelle, z.AsObject());
                    roh = utf8.GetBytes(zeilen.ToJsonString());
                }
                using Stream s = aus.CreateEntry(kvp.Key, CompressionLevel.Optimal).Open();
                s.Write(roh, 0, roh.Length);
            }
        }

        /// <summary>Ein Ordner für die Paketdateien eines Falls; er räumt sich selbst auf.</summary>
        private sealed class Arbeitsordner : IDisposable
        {
            private readonly string _pfad = Path.Combine(Path.GetTempPath(),
                "epos-aufheizergebnis-" + Guid.NewGuid().ToString("N").Substring(0, 8));

            public Arbeitsordner() => Directory.CreateDirectory(_pfad);

            public string Datei(string name) => Path.Combine(_pfad, name);

            public void Dispose()
            {
                try { Directory.Delete(_pfad, true); } catch { /* Aufräumen darf nicht scheitern */ }
            }
        }
    }
}
