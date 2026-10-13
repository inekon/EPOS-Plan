using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Zonenferien als Liste</b> (Konzept Zapfprofil-Benutzerführung ZB5, Entscheidung ZB‑Q3 B; Schemaschritt
    /// <see cref="TwwZonenferienSchema"/>): die Tabelle <c>Tab_TwwZonenferien</c>, die Übertragung der vier Spaltenpaare
    /// von <c>Tab_TwwZone</c> samt Abbau, der Schreib- und Leseweg über die Liste, die Hülle 1:1, der Zapfkalender über die
    /// Liste — gleich dem alten Weg für höchstens vier Zeiträume — und die Paketanhebung.
    ///
    /// <para>Datenbankfälle arbeiten auf einer Arbeitskopie der Testdatenbank (Projekt 1006, kein Referenzprojekt);
    /// alle Werte sind erfunden und rund.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwZonenferienSchemaTests
    {
        private const int PROJEKT = 1006;

        // =================================================================================
        // Kette und Register
        // =================================================================================

        [Fact]
        public void Kette_Zielstand_und_Paketanhebung_kennen_den_Schritt()
        {
            // VORLÄUFIG hängt der Schritt an 221; nach dem Umhängen an 223 (KaeltebedarfSplitAbrechnungSchema) lautet
            // die Kette dort + 1 = 224.
            Assert.Equal(BauteilkatalogSchema.SCHRITT + 1, TwwZonenferienSchema.SCHRITT);
            Assert.Equal(TwwZonenferienSchema.SCHRITT, SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == TwwZonenferienSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Umformung, s.Wirkung);
        }

        [Fact]
        public void Werkzeug_Migration_und_Testkopie_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int w = werkzeug.IndexOf("TwwZonenferienSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(w > 0 && w > werkzeug.IndexOf("BauteilkatalogSchema.Ausfuehren(", StringComparison.Ordinal));

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_TWW_ZONENFERIEN = TwwZonenferienSchema.SCHRITT", migration, StringComparison.Ordinal);
            int vorher = migration.IndexOf("new Schritt(SCHRITT_BAUTEILKATALOG", StringComparison.Ordinal);
            int ort = migration.IndexOf("new Schritt(SCHRITT_TWW_ZONENFERIEN", StringComparison.Ordinal);
            Assert.True(vorher > 0 && ort > vorher);
            Assert.Contains("TwwZonenferienSchema.Ausfuehren(bericht)", migration, StringComparison.Ordinal);

            string testkopie = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("TwwZonenferienSchema.Ausfuehren(null)", testkopie, StringComparison.Ordinal);
        }

        // =================================================================================
        // Regeln ohne Datenbank
        // =================================================================================

        [Fact]
        public void Zeitraum_nimmt_nur_ein_gueltiges_Ende_und_leert_einen_ungueltigen_Beginn()
        {
            Assert.Equal(((int?)null, 6), TwwZonenferienSchema.Zeitraum(null, 6).Value);
            Assert.Equal(((int?)null, 6), TwwZonenferienSchema.Zeitraum(0, 6).Value);
            Assert.Equal(((int?)null, 6), TwwZonenferienSchema.Zeitraum(366, 6).Value);
            Assert.Equal(((int?)355, 6), TwwZonenferienSchema.Zeitraum(355, 6).Value);   // über den Jahreswechsel
            Assert.Equal(((int?)100, 110), TwwZonenferienSchema.Zeitraum(100, 110).Value);
            Assert.Null(TwwZonenferienSchema.Zeitraum(200, null));
            Assert.Null(TwwZonenferienSchema.Zeitraum(200, 0));
            Assert.Null(TwwZonenferienSchema.Zeitraum(200, 366));
            Assert.Null(TwwZonenferienSchema.Zeitraum(null, null));
        }

        [Fact]
        public void Zieltext_nimmt_genau_die_acht_Spalten_aus_dem_Grundschema()
        {
            string alt = TwwSchema.SQL_CREATE_ZONE.Replace("CREATE TABLE IF NOT EXISTS", "CREATE TABLE");
            string neu = TwwZonenferienSchema.Zieltext(alt, TwwZonenferienSchema.Altspalten());

            string erwartet = string.Join("\n", alt.Split('\n').Where(z => !z.TrimStart().StartsWith("\"Ferien", StringComparison.Ordinal)
                                                                        || z.TrimStart().StartsWith("\"Ferienfaktor", StringComparison.Ordinal)));
            Assert.Equal(erwartet, neu);
            Assert.Equal(8, TwwZonenferienSchema.Altspalten().Count);
            foreach (string s in TwwZonenferienSchema.Altspalten()) Assert.DoesNotContain("\"" + s + "\"", neu);
            Assert.EndsWith(") STRICT", neu);
            Assert.Null(TwwZonenferienSchema.Zieltext(neu, TwwZonenferienSchema.Altspalten()));
        }

        // =================================================================================
        // Der Schritt an der Testdatenbank
        // =================================================================================

        /// <summary>
        /// Die Übertragung samt Abbau: Beginn leer, 0 und 366 = keine Angabe, ein Zeitraum über den Jahreswechsel, ein Paar
        /// ohne gültiges Ende fällt weg; IDs, AUTOINCREMENT-Stand und Wohnungstypen bleiben, die acht Spalten sind weg, ein
        /// zweiter Lauf ändert nichts.
        /// </summary>
        [Fact]
        public void Schritt_uebertraegt_die_Paare_baut_die_Spalten_ab_und_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AltenStandHerstellen();

            long nutzung = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_TwwNutzungsart_STAMM"));
            long a = ZoneAlt(nutzung, "Zone A", 1, new int?[] { null, 0, 355, 200 }, new int?[] { 6, 50, 6, 366 });
            long b = ZoneAlt(nutzung, "Zone B", 2, new int?[] { null, 366, null, null }, new int?[] { null, 100, null, null });
            long c = ZoneAlt(nutzung, "Zone C", 3, new int?[4], new int?[4]);
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_TwwWohnungstyp (ID_Zone, Anzahl, Raumzahl, Reihenfolge) VALUES (?, 4, 3.0, 1), (?, 2, 2.0, 2)",
                new DbParam("@a", a), new DbParam("@b", a));

            string zonenVorher = Abzug("SELECT ID, ID_Projekt, Name, Reihenfolge, Bezugsmenge FROM Tab_TwwZone ORDER BY ID");
            string wohnungenVorher = Abzug("SELECT ID, ID_Zone, Anzahl, Reihenfolge FROM Tab_TwwWohnungstyp ORDER BY ID");
            long seqVorher = Zahl("SELECT seq FROM sqlite_sequence WHERE name = 'Tab_TwwZone'");
            Assert.False(TwwZonenferienSchema.Vollstaendig());

            // Der alte Weg: die Fenster der vier Paare, wie Zapfkalender sie bis zum Schritt bildete.
            List<Ferienfenster> altA = AlteFenster(new int?[] { null, 0, 355, 200 }, new int?[] { 6, 50, 6, 366 });

            var bericht = new List<string>();
            Assert.Equal(2, TwwZonenferienSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.StartsWith("4 Ferienzeitraum", StringComparison.Ordinal));
            Assert.True(TwwZonenferienSchema.Vollstaendig());

            // --- Die Zeilen -----------------------------------------------------------------
            Assert.Equal(new (long, long?, long)[] { (1, null, 6), (2, null, 50), (3, 355, 6) }, Ferienzeilen(a));
            Assert.Equal(new (long, long?, long)[] { (1, null, 100) }, Ferienzeilen(b));
            Assert.Empty(Ferienzeilen(c));

            // --- Die Spalten sind weg, alles andere bleibt -------------------------------------
            foreach (string s in TwwZonenferienSchema.Altspalten())
                Assert.False(DataRepository.SpalteVorhanden(TwwSchema.TAB_TWW_ZONE, s), s);
            Assert.Equal(zonenVorher, Abzug("SELECT ID, ID_Projekt, Name, Reihenfolge, Bezugsmenge FROM Tab_TwwZone ORDER BY ID"));
            Assert.Equal(wohnungenVorher, Abzug("SELECT ID, ID_Zone, Anzahl, Reihenfolge FROM Tab_TwwWohnungstyp ORDER BY ID"));
            Assert.Equal(seqVorher, Zahl("SELECT seq FROM sqlite_sequence WHERE name = 'Tab_TwwZone'"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check('Tab_TwwZone')"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check('Tab_TwwWohnungstyp')"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check('Tab_TwwZonenferien')"));
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND tbl_name = 'Tab_TwwZone' AND sql IS NOT NULL"));
            string grund = TwwZonenferienSchema.Zieltext(TwwSchema.SQL_CREATE_ZONE.Replace("CREATE TABLE IF NOT EXISTS", "CREATE TABLE"),
                                                         TwwZonenferienSchema.Altspalten());
            Assert.Equal(grund, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Tab_TwwZone'")));

            // --- Der Zapfkalender über die Liste = der alte Weg über die vier Paare -----------
            ZonenStand gelesen = ZapfprofilCtrl.Lies(PROJEKT).Zonen.Single(z => z.Id == a);
            Assert.Equal(altA, Zapfkalender.FensterDerZone(gelesen));

            // --- Wiederholbar ------------------------------------------------------------------
            Assert.Equal(0, TwwZonenferienSchema.Ausfuehren(null));
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM Tab_TwwZonenferien"));

            // --- Die Kaskade -------------------------------------------------------------------
            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_TwwZone WHERE ID = ?", new DbParam("@id", a)));
            Assert.Empty(Ferienzeilen(a));
        }

        // =================================================================================
        // Schreib- und Leseweg
        // =================================================================================

        [Fact]
        public void Speichern_und_Lies_tragen_sechs_Zeitraeume_samt_Namen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZonenferienStand[] sechs = Sechs();
            var zone = new ZonenStand
            {
                IdNutzungsart = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_TwwNutzungsart_STAMM")),
                Name = "Ferienzone",
                Bezugsmenge = 10.0,
                // dazu zwei Zeiträume, die keine Zeile werden (ohne gültiges Ende), und ein Beginn 0 = keine Angabe
                Ferien = sechs.Take(3)
                              .Concat(new[] { new ZonenferienStand { Beginn = 200, Ende = 366 }, new ZonenferienStand { Beginn = 210 } })
                              .Concat(sechs.Skip(3))
                              .Concat(new[] { new ZonenferienStand { Name = "  ", Beginn = 0, Ende = 40 } })
                              .ToArray()
            };
            ZapfprofilStand geschrieben = ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone }, null));

            var erwartet = sechs.Concat(new[] { new ZonenferienStand { Beginn = null, Ende = 40 } }).ToArray();
            Assert.Equal(erwartet, geschrieben.Zonen[0].Ferien);
            ZonenStand gelesen = ZapfprofilCtrl.Lies(PROJEKT).Zonen.Single(z => z.Id == geschrieben.Zonen[0].Id);
            Assert.Equal(erwartet, gelesen.Ferien);
            Assert.Equal(Enumerable.Range(1, 7).Select(i => (long)i),
                         Ferienzeilen(gelesen.Id).Select(f => f.Reihenfolge));

            // Ein zweites Speichern desselben Stands schreibt dieselben sieben Zeilen.
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { gelesen }, null));
            Assert.Equal(erwartet, ZapfprofilCtrl.Lies(PROJEKT).Zonen.Single(z => z.Id == gelesen.Id).Ferien);
            Assert.Equal(7L, Zahl("SELECT COUNT(*) FROM Tab_TwwZonenferien WHERE ID_Zone = " +
                                  gelesen.Id.ToString(CultureInfo.InvariantCulture)));

            // Ohne Ferien: die Zeilen fallen; die Zone zu löschen nimmt die Zeilen mit.
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator,
                new[] { gelesen with { Ferien = new ZonenferienStand[0] } }, null));
            Assert.Empty(ZapfprofilCtrl.Lies(PROJEKT).Zonen.Single(z => z.Id == gelesen.Id).Ferien);
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { gelesen }, null));
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], null));
            Assert.Empty(Ferienzeilen(gelesen.Id));
        }

        [Fact]
        public void Ein_Jahrestag_ausserhalb_0_bis_366_wird_benannt_abgelehnt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var zone = new ZonenStand
            {
                IdNutzungsart = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_TwwNutzungsart_STAMM")),
                Name = "Zone X",
                Bezugsmenge = 10.0,
                Ferien = new[] { new ZonenferienStand { Beginn = 10, Ende = 400 } }
            };
            var ex = Assert.Throws<ZapfprofilSpeicherException>(() =>
                ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone }, null)));
            Assert.Equal(ZapfSpeicherfehler.ZoneUngueltig, ex.Fehler);
        }

        // =================================================================================
        // Hülle: jede Zeile 1:1
        // =================================================================================

        [Fact]
        public void Die_Huelle_bildet_jede_Zeile_ab_auch_die_fuenfte_und_sechste()
        {
            var zone = new ZonenStand { Id = 4, IdNutzungsart = 11, Name = "Zone Nord", Reihenfolge = 1, Bezugsmenge = 20.0, Ferien = Sechs() };
            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone }, null);

            ZapfprofilEingabeDaten e = ZapfprofilHuelle.AlsEingabe(stand);
            List<ZapfprofilFerienDaten> zeilen = e.Zonen[0].Angaben.Ferien;
            Assert.Equal(6, zeilen.Count);
            Assert.Equal(new[] { "Winter", null, "Ostern", "Pfingsten", "Sommer", "Herbst" }, zeilen.Select(f => f.Name));
            Assert.Equal((355, 6), (zeilen[0].Von, zeilen[0].Bis));     // 21.12. bis 06.01.
            Assert.Null(zeilen[1].Von);
            Assert.Equal(6, e.Zonen[0].Angaben.Ueberschrieben());

            ZapfprofilStand zurueck = ZapfprofilHuelle.AlsStand(e, stand);
            Assert.Equal(Sechs(), zurueck.Zonen[0].Ferien);

            // Eine Zone mit zwei Zeiträumen zeigt genau zwei Zeilen — ohne Auffüllen — und gibt zwei zurück.
            var kurz = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone with { Ferien = Sechs().Take(2).ToArray() } }, null);
            ZapfprofilEingabeDaten k = ZapfprofilHuelle.AlsEingabe(kurz);
            Assert.Equal(2, k.Zonen[0].Angaben.Ferien.Count);
            Assert.Equal(Sechs().Take(2), ZapfprofilHuelle.AlsStand(k, kurz).Zonen[0].Ferien);
        }

        // =================================================================================
        // Zapfkalender
        // =================================================================================

        [Fact]
        public void Sechs_Zeitraeume_ergeben_die_erwarteten_Ferientage()
        {
            var z = new ZonenStand { Ferien = Sechs() };
            ZapfTagtyp[] k = Zapfkalender.Bilden(0, new bool[Zapfkalender.TAGE], Zapfkalender.FensterDerZone(z));
            var erwartet = new HashSet<int>();
            for (int d = 355; d <= 365; d++) erwartet.Add(d);                    // Winter über den Jahreswechsel …
            for (int d = 1; d <= 6; d++) erwartet.Add(d);                        // … bis zum 6. Januar
            for (int d = 1; d <= 20; d++) erwartet.Add(d);                       // ohne Beginn: vom 1. Januar an
            for (int d = 90; d <= 100; d++) erwartet.Add(d);
            for (int d = 140; d <= 145; d++) erwartet.Add(d);
            for (int d = 200; d <= 230; d++) erwartet.Add(d);
            for (int d = 300; d <= 305; d++) erwartet.Add(d);
            for (int d = 1; d <= Zapfkalender.TAGE; d++)
                Assert.Equal(erwartet.Contains(d), k[d - 1] == ZapfTagtyp.Ruhetag);
        }

        /// <summary>Für höchstens vier Zeiträume bildet die Liste dieselben Fenster wie die vier Paare des alten Wegs.</summary>
        [Theory]
        [InlineData(new[] { 366, 100, -1, -1 }, new[] { 3, 102, -1, -1 })]            // der Fall aus ZapfkalenderTests
        [InlineData(new[] { 360, -1, 0, 50 }, new[] { 5, 20, 30, 366 })]
        [InlineData(new[] { 200, 210, 220, 230 }, new[] { 205, 215, 225, 235 })]
        [InlineData(new[] { -1, -1, -1, -1 }, new[] { -1, -1, -1, -1 })]
        public void Bis_vier_Zeitraeume_gleich_dem_alten_Weg(int[] beginn, int[] ende)
        {
            int?[] b = beginn.Select(x => x < 0 ? (int?)null : x).ToArray();
            int?[] e = ende.Select(x => x < 0 ? (int?)null : x).ToArray();
            var z = new ZonenStand { Ferien = Zonenferienprobe.Liste(b, e) };
            Assert.Equal(AlteFenster(b, e), Zapfkalender.FensterDerZone(z));

            // Auch nach dem Speicherweg (nur Zeiträume mit gültigem Ende) dieselben Ruhetage.
            var gespeichert = new ZonenStand { Ferien = ZapfprofilCtrl.FerienZumSchreiben(z) };
            Assert.Equal(Zapfkalender.Bilden(0, new bool[Zapfkalender.TAGE], AlteFenster(b, e)),
                         Zapfkalender.Bilden(0, new bool[Zapfkalender.TAGE], Zapfkalender.FensterDerZone(gespeichert)));
        }

        // =================================================================================
        // Paketanhebung
        // =================================================================================

        [Fact]
        public void Paketanhebung_bildet_die_Zeilen_aus_den_Paaren_eines_aelteren_Pakets()
        {
            using var frei = new TwwTestdatenbank(mitTwwSchema: false);   // Schreibfreigabe der Arbeitsdatenbank
            var zonen = new List<Dictionary<string, JsonElement>>
            {
                Paketzeile(("ID", 7), ("ID_Projekt", 1), ("Name", "Alt"), ("Ferienbeginn_1", 355), ("Ferienende_1", 6),
                           ("Ferienbeginn_2", 0), ("Ferienende_2", 40), ("Ferienbeginn_3", 200), ("Ferienende_3", 366),
                           ("Ferienbeginn_4", null), ("Ferienende_4", null)),
                Paketzeile(("ID", 8), ("ID_Projekt", 1), ("Name", "Ohne"), ("Ferienbeginn_1", null), ("Ferienende_1", null))
            };
            var baum = new Dictionary<string, List<Dictionary<string, JsonElement>>>(StringComparer.OrdinalIgnoreCase)
            {
                [TwwSchema.TAB_TWW_ZONE] = zonen
            };
            List<string> bericht = Paketanhebung.Anheben(BauteilkatalogSchema.SCHRITT, new[] { baum }, null,
                                                         out List<List<string>> neu);

            Assert.Contains(TwwZonenferienSchema.TAB_ZONENFERIEN, neu[0]);
            Assert.Contains(bericht, z => z.Contains("2 ", StringComparison.Ordinal));
            List<Dictionary<string, JsonElement>> zeilen = baum[TwwZonenferienSchema.TAB_ZONENFERIEN];
            Assert.Equal(2, zeilen.Count);
            Assert.Equal(new[] { (7L, 1L, (long?)355, 6L), (7L, 2L, (long?)null, 40L) },
                         zeilen.Select(r => (r["ID_Zone"].GetInt64(), r["Reihenfolge"].GetInt64(),
                                             r.TryGetValue("Beginn", out JsonElement bj) && bj.ValueKind != JsonValueKind.Null ? bj.GetInt64() : (long?)null,
                                             r["Ende"].GetInt64())));
            Assert.Equal(2, zeilen.Select(r => r["ID"].GetInt64()).Distinct().Count());
        }

        // =================================================================================
        // Handwerkszeug
        // =================================================================================

        /// <summary>Sechs Zeiträume: Winter über den Jahreswechsel, einer ohne Beginn und ohne Namen, vier gewöhnliche.</summary>
        private static ZonenferienStand[] Sechs() => new[]
        {
            new ZonenferienStand { Name = "Winter", Beginn = 355, Ende = 6 },
            new ZonenferienStand { Name = null, Beginn = null, Ende = 20 },
            new ZonenferienStand { Name = "Ostern", Beginn = 90, Ende = 100 },
            new ZonenferienStand { Name = "Pfingsten", Beginn = 140, Ende = 145 },
            new ZonenferienStand { Name = "Sommer", Beginn = 200, Ende = 230 },
            new ZonenferienStand { Name = "Herbst", Beginn = 300, Ende = 305 }
        };

        /// <summary>Die Fenster des alten Wegs: je Paar <see cref="Zapfkalender.AusJahrestagen"/>, in der Reihenfolge der Paare.</summary>
        private static List<Ferienfenster> AlteFenster(int?[] beginn, int?[] ende)
        {
            var f = new List<Ferienfenster>();
            for (int i = 0; i < beginn.Length; i++) f.AddRange(Zapfkalender.AusJahrestagen(beginn[i], ende[i]));
            return f;
        }

        /// <summary>
        /// Der Stand VOR dem Schritt an der Arbeitskopie: die Ferientabelle fällt, <c>Tab_TwwZone</c> wird mit dem
        /// Grundschema (samt den acht Spalten) neu gebaut — über dasselbe Rezept des Hauses.
        /// </summary>
        private static void AltenStandHerstellen()
        {
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    v.Ausfuehren("DROP TABLE IF EXISTS \"" + TwwZonenferienSchema.TAB_ZONENFERIEN + "\"");
                    TwwBezugsartSchema.Neubau(v, TwwSchema.TAB_TWW_ZONE,
                                              TwwSchema.SQL_CREATE_ZONE.Replace("CREATE TABLE IF NOT EXISTS", "CREATE TABLE"),
                                              TwwSchema.TAB_TWW_ZONE + "_probe");
                    v.Commit();
                }
                finally
                {
                    if (v.Offen) v.Ausfuehren("PRAGMA legacy_alter_table = OFF");
                }
            }
            Assert.True(DataRepository.SpalteVorhanden(TwwSchema.TAB_TWW_ZONE, "Ferienbeginn_1"));
            Assert.False(DataRepository.TabelleVorhanden(TwwZonenferienSchema.TAB_ZONENFERIEN));
        }

        /// <summary>Eine Zone im alten Stand mit vier Paaren (Spaltennamen aus dem Schritt, nie aus einer Eingabe).</summary>
        private static long ZoneAlt(long nutzung, string name, int reihenfolge, int?[] beginn, int?[] ende)
        {
            var spalten = new List<string> { "ID_Projekt", "ID_Nutzungsart", "Reihenfolge", "Name", "Bezugsmenge" };
            var werte = new List<DbParam>
            {
                new DbParam("@p", PROJEKT), new DbParam("@n", nutzung), new DbParam("@r", reihenfolge),
                new DbParam("@name", name), new DbParam("@m", 10.0)
            };
            for (int n = 1; n <= TwwZonenferienSchema.PAARE; n++)
            {
                spalten.Add(TwwZonenferienSchema.Altspalte(true, n));
                werte.Add(new DbParam("@b" + n, (object)beginn[n - 1] ?? DBNull.Value));
                spalten.Add(TwwZonenferienSchema.Altspalte(false, n));
                werte.Add(new DbParam("@e" + n, (object)ende[n - 1] ?? DBNull.Value));
            }
            return DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO Tab_TwwZone (" + string.Join(", ", spalten.Select(s => "\"" + s + "\"")) + ") VALUES (" +
                string.Join(", ", spalten.Select(_ => "?")) + ")", werte.ToArray());
        }

        private static (long Reihenfolge, long? Beginn, long Ende)[] Ferienzeilen(long zone) =>
            DataRepository.GetDataTable(
                "SELECT Reihenfolge, Beginn, Ende FROM Tab_TwwZonenferien WHERE ID_Zone = ? ORDER BY Reihenfolge",
                new DbParam("@z", zone)).Rows.Cast<DataRow>()
                .Select(r => (Convert.ToInt64(r["Reihenfolge"]), r["Beginn"] == DBNull.Value ? (long?)null : Convert.ToInt64(r["Beginn"]),
                              Convert.ToInt64(r["Ende"])))
                .ToArray();

        private static string Abzug(string sql)
            => string.Join("|", DataRepository.GetDataTable(sql).Rows.Cast<DataRow>()
                .Select(r => string.Join(";", r.ItemArray.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture)))));

        private static long Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, JsonElement> Paketzeile(params (string Spalte, object Wert)[] werte)
        {
            var z = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach ((string s, object w) in werte) z[s] = JsonSerializer.SerializeToElement(w);
            return z;
        }

        private static string Repowurzel()
        {
            string d = AppContext.BaseDirectory;
            while (d != null && !File.Exists(Path.Combine(d, "WP-Plan.sln"))) d = Path.GetDirectoryName(d);
            return d;
        }
    }
}
