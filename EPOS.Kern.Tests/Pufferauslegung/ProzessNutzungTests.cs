using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// <b>V29, V31, V32 der Pufferauslegung</b> auf einer Arbeitskopie der Testdatenbank: Prozesstemperatur in
    /// der Prozesszone samt Warncode <see cref="PufferWarncode.PROZESS_TEMPERATUR"/>, Schemaschritt
    /// <see cref="ProzessNutzungSchema"/> (Zuordnung der Nutzungsprofile, Zapf-Nutzungsarten Büro, Schule,
    /// Gewerbe) aus dem Stand davor und wiederholbar, die Einfrierregel für 1045 (benutzte Katalogzeilen
    /// unverändert gegen die eingefrorene Testdatenbank) und das Nutzungsprofil über die Zuordnung.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ProzessNutzungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const int P_ZAPF = 1045;
        private const int P_PROZESS = 1041;
        private const int P_VORGABE = 1030;
        private const int PUFFER_1030 = 1054170;

        private static long Zahl(string sql, params object[] p) =>
            Convert.ToInt64(DataRepository.ExecuteScalar(sql, p.Select(w => new DbParam("?", w)).ToArray()), CultureInfo.InvariantCulture);

        private static string Bild(DataTable t)
        {
            var sb = new StringBuilder();
            foreach (DataRow r in t.Rows)
                sb.Append(string.Join("|", r.ItemArray.Select(w => w is bool b ? (b ? "1" : "0")
                                                                 : w is DateTime d ? d.ToString(d.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                                                                 : Convert.ToString(w, CultureInfo.InvariantCulture)))).Append('\n');
            return sb.ToString();
        }

        /// <summary>Die eingefrorene Testdatenbank im Repositorium (nur lesend).</summary>
        private static string Original()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string k = Path.Combine(d.FullName, "Referenzlaeufe", "Kenndaten_Test.sqlite");
                if (File.Exists(k)) return k;
            }
            return null;
        }

        private static string BildOriginal(string pfad, string sql)
        {
            using var c = new SqliteConnection("Data Source=" + pfad + ";Mode=ReadOnly");
            c.Open();
            using SqliteCommand k = c.CreateCommand();
            k.CommandText = sql;
            var t = new DataTable();
            using (SqliteDataReader r = k.ExecuteReader()) t.Load(r);
            return Bild(t);
        }

        // =============================================================================
        //  Schemaschritt
        // =============================================================================

        [Fact]
        public void Nummer_Ziel_und_Paketstufe()
        {
            Assert.Equal(WaermepumpeSperrprofilSchema.SCHRITT + 1, ProzessNutzungSchema.SCHRITT);
            Assert.Equal(178, ProzessNutzungSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= ProzessNutzungSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, Paketanhebung.Stufen.Single(x => x.Nr == ProzessNutzungSchema.SCHRITT).Wirkung);
        }

        [Fact]
        public void Tabellen_sind_STRICT_und_gesaet()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ProzessNutzungSchema.Vollstaendig());
            foreach (string t in new[] { ProzessNutzungSchema.TAB_PROFIL, ProzessNutzungSchema.TAB_ZUORDNUNG })
                Assert.EndsWith("STRICT", Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE name = ?", new DbParam("?", t))).TrimEnd());
            Assert.Equal(5L, Zahl("SELECT COUNT(*) FROM Tab_Nutzungsprofil_STAMM WHERE ReadOnly = 1"));
            // Dazu die Musternamen Büro und Schule unter KONDITIONIERUNG (Schritt 189, RaumnutzungSaat, NP-F15).
            Assert.Equal((long)(NutzungsprofilZuordnung.VORGABE.Count + RaumnutzungSaat.Pufferzuordnungen.Count),
                         Zahl("SELECT COUNT(*) FROM Z_Nutzungsprofil"));
            Assert.Equal(26, NutzungsprofilZuordnung.VORGABE.Count);
            // Die Prüfklauseln greifen.
            try { DataRepository.ExecuteNonQuery("INSERT INTO Z_Nutzungsprofil (ID_Nutzungsprofil, Quelle, Schluessel) VALUES (1, 'FREITEXT', 'x')"); }
            catch (Exception) { }
            try { DataRepository.ExecuteNonQuery("INSERT INTO Tab_Nutzungsprofil_STAMM (Kennung) VALUES ('UNBEKANNT')"); }
            catch (Exception) { }
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Z_Nutzungsprofil WHERE Quelle = 'FREITEXT'"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Nutzungsprofil_STAMM WHERE Kennung = 'UNBEKANNT'"));
        }

        [Fact]
        public void Schritt_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            string neue = "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner IN ('Büro (Setzung)', 'Schule (Setzung)', " +
                          "'Gewerbe Schichtbetrieb (Setzung)')";
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM (" + neue + ")"));
            // Stand davor: beide Tabellen und die nachgetragenen Nutzungsarten samt Sätzen fehlen.
            DataRepository.ExecuteNonQuery("DROP TABLE Z_Nutzungsprofil");
            DataRepository.ExecuteNonQuery("DROP TABLE Tab_Nutzungsprofil_STAMM");
            DataRepository.ExecuteNonQuery("CREATE TEMP TABLE alt_saetze AS SELECT ID_Tagesgangsatz AS ID FROM Tab_TwwNutzungsart_STAMM WHERE ID IN (" + neue + ")");
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_TwwZapfkategorie_STAMM WHERE ID_Nutzungsart IN (" + neue + ")");
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_TwwNutzungsart_STAMM WHERE ID IN (" + neue + ")");
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_TwwTagesgang_STAMM WHERE ID_Tagesgangsatz IN (SELECT ID FROM alt_saetze)");
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_TwwTagesgangsatz_STAMM WHERE ID IN (SELECT ID FROM alt_saetze)");
            Assert.False(ProzessNutzungSchema.SchemaVollstaendig());

            var bericht = new List<string>();
            Assert.Equal(2, ProzessNutzungSchema.Ausfuehren(bericht));
            Assert.True(ProzessNutzungSchema.Vollstaendig());
            Assert.Contains(bericht, z => z.Contains(": 3 Nutzungsart(en) nachgetragen", StringComparison.Ordinal));
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM (" + neue + ")"));
            Assert.Equal(12L, Zahl("SELECT COUNT(*) FROM Tab_TwwTagesgang_STAMM WHERE ID_Tagesgangsatz IN " +
                                   "(SELECT ID_Tagesgangsatz FROM Tab_TwwNutzungsart_STAMM WHERE ID IN (" + neue + "))"));
            Assert.Equal(6L, Zahl("SELECT COUNT(*) FROM Tab_TwwZapfkategorie_STAMM WHERE ID_Nutzungsart IN (" + neue + ")"));

            // Ein zweiter Lauf legt nichts an und ändert nichts.
            string vorher = Bild(DataRepository.GetDataTable("SELECT * FROM Z_Nutzungsprofil ORDER BY ID")) +
                            Bild(DataRepository.GetDataTable("SELECT * FROM Tab_TwwNutzungsart_STAMM ORDER BY ID")) +
                            Bild(DataRepository.GetDataTable("SELECT * FROM Tab_TwwZapfkategorie_STAMM ORDER BY ID"));
            Assert.Equal(0, ProzessNutzungSchema.Ausfuehren(null));
            Assert.Equal(0, ProzessNutzungSchema.Saat(null));
            Assert.Equal(vorher, Bild(DataRepository.GetDataTable("SELECT * FROM Z_Nutzungsprofil ORDER BY ID")) +
                                 Bild(DataRepository.GetDataTable("SELECT * FROM Tab_TwwNutzungsart_STAMM ORDER BY ID")) +
                                 Bild(DataRepository.GetDataTable("SELECT * FROM Tab_TwwZapfkategorie_STAMM ORDER BY ID")));
        }

        // =============================================================================
        //  V31: Nutzungsarten und Einfrierregel 1045
        // =============================================================================

        [Fact]
        public void Neue_Nutzungsarten_stehen_im_Katalog_und_sind_waehlbar()
        {
            if (!_db.Vorhanden) return;
            string version = ZapfprofilCtrl.AktuelleKatalogversion();
            IReadOnlyList<Nutzungsart> katalog = ZapfprofilCtrl.Katalog();
            foreach (string n in ProzessNutzungSchema.NUTZUNGSARTEN)
            {
                Nutzungsart a = katalog.Single(x => x.Name == n);
                Assert.Equal(version, a.Katalogversion);
                Assert.True(a.Tagesgaenge.Vollstaendig, n + ": Tagesgangsatz unvollständig");
                Assert.Contains(TwwNutzungsartCtrl.Liste(), z => z.Bezeichner == n);
            }
            Assert.Equal(ZapfKalenderart.Arbeitstage, katalog.Single(x => x.Name == "Büro (Setzung)").Kalender);
            Assert.Equal(ZapfKalenderart.Schulferien, katalog.Single(x => x.Name == "Schule (Setzung)").Kalender);
            Assert.Equal(ZapfKalenderart.Betrieb, katalog.Single(x => x.Name == "Gewerbe Schichtbetrieb (Setzung)").Kalender);
            Assert.Equal(1L, Zahl("SELECT COUNT(DISTINCT Bedarf_Quelle) FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner IN " +
                                  "('Büro (Setzung)', 'Schule (Setzung)', 'Gewerbe Schichtbetrieb (Setzung)') AND " +
                                  "Bedarf_Quelle = 'Setzung nach DIN EN 12831-3 Profilfamilie' AND Bedarf_Herkunftsart = 'EIGENKONSTRUKTION'"));
        }

        [Fact]
        public void Katalogzeilen_von_1045_sind_byte_gleich_zur_eingefrorenen_Testdatenbank()
        {
            if (!_db.Vorhanden) return;
            string original = Original();
            Assert.NotNull(original);
            string arten = "SELECT ID_Nutzungsart FROM Tab_TwwZone WHERE ID_Projekt = " + P_ZAPF;
            string saetze = "SELECT ID_Tagesgangsatz FROM Tab_TwwNutzungsart_STAMM WHERE ID IN (" + arten + ")";
            long maxArt = Convert.ToInt64(BildOriginal(original, "SELECT MAX(ID) FROM Tab_TwwNutzungsart_STAMM").Trim(), CultureInfo.InvariantCulture);
            var abfragen = new[]
            {
                "SELECT * FROM Tab_TwwNutzungsart_STAMM WHERE ID IN (" + arten + ") ORDER BY ID",
                "SELECT * FROM Tab_TwwNutzungsart_STAMM WHERE ID <= " + maxArt + " ORDER BY ID",
                "SELECT * FROM Tab_TwwTagesgangsatz_STAMM WHERE ID IN (" + saetze + ") ORDER BY ID",
                "SELECT * FROM Tab_TwwTagesgang_STAMM WHERE ID_Tagesgangsatz IN (" + saetze + ") ORDER BY ID",
                "SELECT * FROM Tab_TwwZapfkategorie_STAMM WHERE ID_Nutzungsart IN (" + arten + ") ORDER BY ID",
                "SELECT * FROM Tab_TwwParameter_STAMM ORDER BY ID",
                "SELECT * FROM Tab_TwwZone WHERE ID_Projekt = " + P_ZAPF + " ORDER BY ID",
                "SELECT * FROM Tab_TwwProjekt WHERE ID_Projekt = " + P_ZAPF + " ORDER BY ID",
            };
            foreach (string sql in abfragen)
            {
                string soll = BildOriginal(original, sql);
                Assert.False(string.IsNullOrEmpty(soll), sql);
                Assert.Equal(soll, Bild(DataRepository.GetDataTable(sql)));
            }
        }

        // =============================================================================
        //  V32: Nutzungsprofil über die Zuordnung
        // =============================================================================

        [Fact]
        public void Nutzungsprofil_ueber_die_Zuordnung_ohne_Teiltext()
        {
            Assert.Equal(PufferNutzungsprofil.BUERO_SCHULE, Nutzungsprofil.AusZapfnutzung("Büro (Setzung)"));
            Assert.Equal(PufferNutzungsprofil.BUERO_SCHULE, Nutzungsprofil.AusZapfnutzung("Schule (Setzung)"));
            Assert.Equal(PufferNutzungsprofil.GEWERBE, Nutzungsprofil.AusZapfnutzung("Gewerbe Schichtbetrieb (Setzung)"));
            Assert.Null(Nutzungsprofil.AusZapfnutzung("Mehrfamilienhaus"));            // kein Teiltext mehr
            Assert.Null(Nutzungsprofil.AusZapfnutzung("Hotelzimmer"));
            Assert.Equal(PufferNutzungsprofil.PFLEGE, Nutzungsprofil.AusGebaeudeart("Krankenhaus "));
            Assert.Equal(PufferNutzungsprofil.BUERO_SCHULE, Nutzungsprofil.AusGebaeudeart("Verwaltungsgebäude"));
            Assert.Null(Nutzungsprofil.AusGebaeudeart("Hallenbad"));
            Assert.True(Nutzungsprofil.Ableiten(null, false, new[] { "WOHNEN" }).Vorgabe);
        }

        [Fact]
        public void Nutzungsprofil_je_Testprojekt_liest_die_Datenbank()
        {
            if (!_db.Vorhanden) return;
            PufferNutzungsprofilAbleitung zapf = PufferAuslegungCtrl.Vorbelegen(P_ZAPF, null).Nutzungsprofil;
            Assert.Equal(PufferNutzungsprofil.WOHNEN, zapf.Profil);
            Assert.Equal("Zapf-Nutzungsart „Wohnen groß (abgeleitet)“", zapf.Herkunft);
            Assert.Equal(PufferNutzungsprofil.GEWERBE, PufferAuslegungCtrl.Vorbelegen(P_PROZESS, null).Nutzungsprofil.Profil);
            PufferNutzungsprofilAbleitung vorgabe = PufferAuslegungCtrl.Vorbelegen(P_VORGABE, PUFFER_1030).Nutzungsprofil;
            Assert.Equal(PufferNutzungsprofil.WOHNEN, vorgabe.Profil);
            Assert.True(vorgabe.Vorgabe);

            // Die Zuordnung kommt aus der Tabelle: umgehängt auf PFLEGE, folgt die Ableitung.
            DataRepository.ExecuteNonQuery(
                "UPDATE Z_Nutzungsprofil SET ID_Nutzungsprofil = (SELECT ID FROM Tab_Nutzungsprofil_STAMM WHERE Kennung = 'PFLEGE') " +
                "WHERE Quelle = 'ZAPF' AND Schluessel = 'Wohnen groß (abgeleitet)'");
            Assert.Equal(PufferNutzungsprofil.PFLEGE, PufferAuslegungCtrl.Vorbelegen(P_ZAPF, null).Nutzungsprofil.Profil);

            // Ohne Tabelle: Rückfall auf die Vorgabe im Code.
            DataRepository.ExecuteNonQuery("DROP TABLE Z_Nutzungsprofil");
            Assert.Same(NutzungsprofilZuordnung.VORGABE_TABELLE, NutzungsprofilZuordnung.Lesen());
            Assert.Equal(PufferNutzungsprofil.WOHNEN, PufferAuslegungCtrl.Vorbelegen(P_ZAPF, null).Nutzungsprofil.Profil);
        }

        // =============================================================================
        //  V29: Prozesstemperatur und Warncode
        // =============================================================================

        private static int Prozesskopie1041() => (int)Zahl(
            "SELECT p.ID FROM Tab_Prozesswaerme p JOIN Z_Projekt_Prozesswaerme z ON z.ID_Prozesswaerme = p.ID " +
            "WHERE z.ID_Projekt = ? AND p.ID_Projekt = ? ORDER BY p.ID LIMIT 1", P_PROZESS, P_PROZESS);

        [Fact]
        public void Vorbelegen_liest_das_Temperaturpaar_der_Prozesswaerme_und_den_Erzeugervorlauf()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungEingang ohne = PufferAuslegungCtrl.Vorbelegen(P_PROZESS, null).Eingang;
            Assert.True(ohne.KlasseProzess);
            Assert.Null(ohne.ProzessVorlaufC);                    // Referenzprojekt ohne Paar (Einfrierregel)
            Assert.Equal(45.0, ohne.ErzeugerVorlaufMaxC);

            Assert.True(ProzesswaermeStammCtrl.ProjektTemperaturSetzen(Prozesskopie1041(), 80.0, 60.0));
            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(P_PROZESS, null);
            Assert.Equal(80.0, v.Eingang.ProzessVorlaufC);
            Assert.Equal(60.0, v.Eingang.ProzessRuecklaufC);
            Assert.Contains(v.Herkunft, h => h.Feld == nameof(PufferAuslegungEingang.ProzessVorlaufC) &&
                                             h.Baustein.Klartext == "Temperaturpaar der Prozesswärme");

            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(v.Eingang);
            Assert.True(r.HatWarnung(PufferWarncode.PROZESS_TEMPERATUR));          // 80 °C über 45 °C der Wärmepumpe
            PufferWarnung w = r.Warnungen.First(x => x.Code == PufferWarncode.PROZESS_TEMPERATUR);
            Assert.Equal(PufferZone.Prozess, w.Zone);
            Assert.Equal("PA_PROZESS_TEMPERATUR", w.Ressourcenschluessel);
            Assert.False(string.IsNullOrEmpty(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(w.Ressourcenschluessel, CultureInfo.GetCultureInfo("de-DE"))));
            Assert.False(string.IsNullOrEmpty(WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(w.Ressourcenschluessel, CultureInfo.GetCultureInfo("en-US"))));
        }

        [Fact]
        public void Warncode_gegen_95_Grad_und_gegen_den_Erzeugervorlauf()
        {
            Assert.False(ProzesszoneRechner.ProzessvorlaufKritisch(null, 45));
            Assert.True(ProzesszoneRechner.ProzessvorlaufKritisch(96, null));         // ohne Erzeugervorlauf nur gegen 95 °C
            Assert.False(ProzesszoneRechner.ProzessvorlaufKritisch(90, null));
            Assert.True(ProzesszoneRechner.ProzessvorlaufKritisch(60, 55));
            Assert.False(ProzesszoneRechner.ProzessvorlaufKritisch(55, 60));
            Assert.True(ProzesszoneRechner.ProzessvorlaufKritisch(100, 120));         // über 95 °C immer
        }

        [Fact]
        public void Prozesszone_rechnet_mit_der_Spreizung_der_Prozesswaerme()
        {
            if (!_db.Vorhanden) return;
            // Teillast unter der Mindestleistung: die Mindestlaufzeit verlangt Speicher (D1/D2 der Prozesszone).
            double[] reihe = Enumerable.Repeat(5.0, 8760).ToArray();
            PufferAuslegungEingang v = PufferAuslegungCtrl.Vorbelegen(P_PROZESS, null).Eingang;
            PufferAuslegungEingang e = v with
            {
                ReiheProzess = reihe,
                ErzeugerVorlaufMaxC = null,
                MindestlaufzeitMin = 60,
                Erzeuger = v.Erzeuger with { NennleistungKw = 50, MindestleistungKw = 25 }
            };
            double ohne = PufferAuslegung.Rechnen(e).Zone(PufferZone.Prozess).VolumenL;
            double schmal = PufferAuslegung.Rechnen(e with { ProzessVorlaufC = 60, ProzessRuecklaufC = 50 }).Zone(PufferZone.Prozess).VolumenL;
            double breit = PufferAuslegung.Rechnen(e with { ProzessVorlaufC = 70, ProzessRuecklaufC = 40 }).Zone(PufferZone.Prozess).VolumenL;
            Assert.True(schmal > breit, "schmal " + schmal + " / breit " + breit);
            Assert.False(PufferAuslegung.Rechnen(e with { ProzessVorlaufC = 70, ProzessRuecklaufC = 40 }).HatWarnung(PufferWarncode.PROZESS_TEMPERATUR));
            Assert.True(ohne > 0);
        }
    }
}
