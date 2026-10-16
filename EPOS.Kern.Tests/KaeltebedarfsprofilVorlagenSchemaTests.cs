using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Saatwache von Schemaschritt A der Kältebedarfsprofile</b> (<see cref="KaeltebedarfsprofilVorlagenSchema"/>,
    /// <see cref="KaeltevorlagenSaat"/>, Welle KBP-2, Konzept Kältebedarfsprofile 6.2 und 8.1): Nummer und Register, die
    /// Vorlagen 7–16 (Monatsanteile zusammen 100, Tagesgänge vollständig, Quelle je Vorlage), die Zeilen 1–6 bytegleich
    /// gegen den eingefrorenen Abdruck, die Prüfklausel der Eingabeart, Art-Vorschlag und Wiederholbarkeit.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaeltebedarfsprofilVorlagenSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die Tafel der zehn Vorlagen — ID, Name, Art-Vorschlag, Grundlast (Konzept 6.2).</summary>
        private static readonly (int Id, string Name, string Art, double Grundlast)[] TAFEL =
        {
            (7,  "Krankenhaus",                                "RAUM",    0.40),
            (8,  "Prozesskälte einschichtig",                  "PROZESS", 0.10),
            (9,  "Prozesskälte zweischichtig",                 "PROZESS", 0.15),
            (10, "Prozesskälte dreischichtig",                 "PROZESS", 0.20),
            (11, "Rechenzentrum",                              "PROZESS", 0.85),
            (12, "Büro/Verwaltung",                            "RAUM",    0.10),
            (13, "Hotel",                                      "RAUM",    0.25),
            (14, "Lebensmitteleinzelhandel mit Gewerbekälte",  "PROZESS", 0.60),
            (15, "Schule/Hochschule",                          "RAUM",    0.05),
            (16, "Labor",                                      "RAUM",    0.40),
        };

        /// <summary>
        /// <b>Der eingefrorene Abdruck der Bestandszeilen 1–6</b> (R2): Schlüssel und Prüfsumme je Satz, abgelesen an der
        /// Testdatenbank VOR dem Schritt. Die Prüfsumme deckt jede Fachspalte (Name, Typ, Beschreibung, zwölf Monate,
        /// Temperaturpaar bzw. 168 Wochenstunden).
        /// </summary>
        private static readonly (long Id, string Schluessel, string Pruefsumme)[] ABDRUCK_KOPF =
        {
            (1, "KB:RAUMKUEHLUNG_BUERO",      "a64f424ada0aab876615e7f3d92519d18185d1d593a8128ed5477f271f0cc218"),
            (2, "KB:RAUMKUEHLUNG_HANDEL",     "295c04a8ca1650d6ab87c935e8d2bdfd0969e4e489384fa56e0831c5bb96af09"),
            (3, "KB:PROZESSKAELTE_DAUERLAST", "9ffe94c20b37989e181feea0f35ce55cd58c8e66cc9d5bc1dfbc10e8924e2887"),
            (4, "KB:KUEHLRAUM",               "ecd764c5ae2b35170681d521ff516f74450c80db435288aeacdbd1aa8beb4c0a"),
            (5, "KB:TIEFKUEHLRAUM",           "b494a5e15b9ef9e728ef9b2c3a66174be9e7c76ee081c616679a5646c336c79b"),
            (6, "KB:SERVERRAUM",              "e98c1f14381f198ca1cf648d44dbc4b1ab7babe2faba70617a17294a928b466b"),
        };

        private static readonly (long Id, string Schluessel, string Pruefsumme)[] ABDRUCK_TYP =
        {
            (1, "KBT:RAUMKUEHLUNG_BUERO",      "974599671653707d48586a1f6261495b012901b21a98b29b9d0100d3cb1d239a"),
            (2, "KBT:RAUMKUEHLUNG_HANDEL",     "48e4f2f276aa868ff11434909358705440fd28067ee837587a6dc6e35da445b7"),
            (3, "KBT:PROZESSKAELTE_DAUERLAST", "cb6d53b0927807964dd1489cb208acad30d6a038a0d46c8a72148e07e002dfea"),
            (4, "KBT:KUEHLRAUM",               "15c1c9cc0ed409ae92d0bcc96e41dcd23602f9899ab892f5d9850addfc39b6a9"),
            (5, "KBT:TIEFKUEHLRAUM",           "41094ef1bb16205e13e688790903923319cf10311abef1b7ea4b4a38932c23c4"),
            (6, "KBT:SERVERRAUM",              "c3ebe120ebca172c541fd9d9a622e187ca3ea24a9002a0f36b3d1796b588b267"),
        };

        /// <summary>
        /// Der eingefrorene Zeilenabdruck der Zeilen 1–6 beider Kataloge: SHA-256 über jede Spalte (außer dem neuen
        /// Art-Vorschlag) mit Typ und Wert, Zahlen rundreisefest — fängt auch Metaspalten wie ReadOnly und Ausgelaufen.
        /// Eingefroren an der Testdatenbank VOR dem Schritt (Schemastand 227, unabhängig gegengerechnet).
        /// </summary>
        private const string ZEILENABDRUCK = "3d18d230b0c28e973e4134bc21959bcf847ad4f8949dfe1f843b0b3bb0098318";

        [Fact]
        public void Nummer_Kette_und_Register()
        {
            // KETTE: 230 haengt ueber + 1 an 229 (StromspeicherKostenSchema, KB-5).
            Assert.Equal(230, KaeltebedarfsprofilVorlagenSchema.SCHRITT);
            Assert.Equal(StromspeicherKostenSchema.SCHRITT + 1, KaeltebedarfsprofilVorlagenSchema.SCHRITT);
            Assert.Equal(KaeltebedarfsprofilVorlagenSchema.SCHRITT, SchemaStand.Zielversion);
            Assert.Contains(Paketanhebung.Stufen, s => s.Nr == KaeltebedarfsprofilVorlagenSchema.SCHRITT && s.Wirkung == Paketanhebung.Art.Ddl);
            Assert.Equal(3, KaeltebedarfsprofilVorlagenSchema.SPALTEN.Count);
            Assert.Equal(new[] { "jahr", "monat", "woche", "typtag" }, KaeltebedarfsprofilVorlagenSchema.EINGABEARTEN);
            Assert.Contains(KaeltebedarfsprofilVorlagenSchema.SPALTE_ART, Katalogfassung.Tabelle(KaeltebedarfSchema.TAB_KOPF_STAMM).Fachspalten);
        }

        [Fact]
        public void Die_Saat_haelt_die_Tafel_Monate_zusammen_100_Tagesgaenge_vollstaendig()
        {
            Assert.Equal(TAFEL.Select(t => t.Id), KaeltevorlagenSaat.Alle.Select(s => s.Id));
            Assert.Equal(KaeltevorlagenSaat.ERSTE_ID, KaeltevorlagenSaat.Alle.First().Id);
            Assert.Equal(KaeltevorlagenSaat.LETZTE_ID, KaeltevorlagenSaat.Alle.Last().Id);
            foreach (var (id, name, art, grundlast) in TAFEL)
            {
                Kaeltevorlage s = KaeltevorlagenSaat.Alle.Single(x => x.Id == id);
                Assert.Equal(name, s.Name);
                Assert.Equal(art, s.Art);
                Assert.Equal(grundlast, s.Grundlast);
                // Tagesgang: 168 Werte, jeder in [0, 1], Spitze 1, kein Tag leer außer gewollt (keiner der zehn).
                Assert.Equal(168, s.Woche.Length);
                Assert.All(s.Woche, w => Assert.InRange(w, 0.0, 1.0));
                Assert.Equal(1.0, s.Woche.Max());
                for (int tag = 0; tag < 7; tag++)
                    Assert.True(s.Woche.Skip(tag * 24).Take(24).Sum() > 0, name + ": Tag " + tag + " leer");
                // Grundlast: der kleinste Wochenwert oder der kleinste Monatsfaktor ist der Grundlast-Anteil.
                Assert.True(Math.Abs(s.Woche.Min() - grundlast) < 1e-12 || Math.Abs(s.Monatsfaktoren.Min() - grundlast) < 1e-12,
                            name + ": Grundlast");
                // Monatsanteile: zwölf, zusammen 100.
                Assert.Equal(12, s.Monatsfaktoren.Length);
                Assert.All(s.Monatswerte(), m => Assert.True(m > 0));
                Assert.Equal(100.0, s.Monatswerte().Sum(), 9);
                Assert.Null(KaeltebedarfSchema.Paarpruefung(s.Vorlauf, s.Ruecklauf));
                Assert.EndsWith(KaeltevorlagenSaat.VERMERK, s.Beschreibung, StringComparison.Ordinal);
                // Jede Vorlage nennt ihre Quelle: Norm samt Profil oder die Kennung des Richtwerts.
                Assert.False(string.IsNullOrWhiteSpace(s.Quelle));
                Assert.True(s.Quelle.Contains(KaeltevorlagenSaat.DIN) || s.Quelle.Contains(KaeltevorlagenSaat.RICHTWERT), name);
                Assert.DoesNotContain("2018", s.Quelle);
                Assert.DoesNotContain("Ausgabe prüfen", s.Quelle);
                if (s.Id == 16) Assert.Contains("Profil 38", s.Quelle);
            }
            // Schule nennt die Ferien über den Betriebskalender (Doppelzählung vermeiden).
            Assert.Contains(KaeltevorlagenSaat.VERMERK_FERIEN, KaeltevorlagenSaat.Alle.Single(s => s.Id == 15).Beschreibung);
            // Kein Name doppelt mit dem Bestand.
            Assert.Empty(KaeltevorlagenSaat.Alle.Select(s => s.Name).Intersect(KaeltetypSaat.Alle.Select(s => s.Name)));
        }

        [Fact]
        public void Die_Testkopie_traegt_die_Zeilen_7_bis_16_mit_Art_Schluessel_und_Summe_100()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KaeltebedarfsprofilVorlagenSchema.Vollstaendig());
            foreach (Kaeltevorlage s in KaeltevorlagenSaat.Alle)
            {
                DataRow r = Assert.Single(DataRepository.GetDataTable(
                    "SELECT * FROM Tab_Kaeltebedarf_STAMM WHERE ID = ?", new DbParam("@id", s.Id)).Rows.Cast<DataRow>());
                Assert.Equal(s.Name, r["Bezeichner"]);
                Assert.Equal(s.Name, r["Typ"]);
                Assert.Equal(s.Art, r[KaeltebedarfsprofilVorlagenSchema.SPALTE_ART]);
                Assert.Equal(1L, Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture));
                Assert.StartsWith("KB:", Convert.ToString(r["Katalog_Schluessel"], CultureInfo.InvariantCulture), StringComparison.Ordinal);
                Assert.Equal(64, Convert.ToString(r["Katalog_Pruefsumme"], CultureInfo.InvariantCulture).Length);
                double summe = Enumerable.Range(1, 12).Sum(m => Convert.ToDouble(r["Monat_" + m], CultureInfo.InvariantCulture));
                Assert.Equal(100.0, summe, 9);

                DataRow tr = Assert.Single(DataRepository.GetDataTable(
                    "SELECT * FROM Tab_Kaeltetyp_STAMM WHERE ID = ?", new DbParam("@id", s.Id)).Rows.Cast<DataRow>());
                Assert.Equal(s.Name, tr["Bezeichner"]);
                Assert.StartsWith("KBT:", Convert.ToString(tr["Katalog_Schluessel"], CultureInfo.InvariantCulture), StringComparison.Ordinal);
                for (int h = 1; h <= 168; h++)
                {
                    object w = tr[h.ToString(CultureInfo.InvariantCulture)];
                    Assert.NotEqual(DBNull.Value, w);
                    Assert.Equal(s.Woche[h - 1], Convert.ToDouble(w, CultureInfo.InvariantCulture));
                }
                Assert.Equal(s.Art, KaeltebedarfStammCtrl.ArtVorschlag(s.Name));
            }
            Assert.Equal(16L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltebedarf_STAMM WHERE ReadOnly = 1"));
            Assert.Equal(16L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyp_STAMM WHERE ReadOnly = 1"));
        }

        [Fact]
        public void Die_Zeilen_1_bis_6_sind_bytegleich_gegen_den_Abdruck()
        {
            if (!_db.Vorhanden) return;
            Pruefe(KaeltebedarfSchema.TAB_KOPF_STAMM, ABDRUCK_KOPF);
            Pruefe(KaeltebedarfSchema.TAB_TYP_STAMM, ABDRUCK_TYP);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltebedarf_STAMM WHERE ID <= 6 AND Kaelte_Art IS NOT NULL"));
            foreach (KaeltetypSaatsatz s in KaeltetypSaat.Alle) Assert.Null(KaeltebedarfStammCtrl.ArtVorschlag(s.Name));
            Assert.Equal(ZEILENABDRUCK, Zeilenabdruck());
        }

        private static void Pruefe(string tabelle, (long Id, string Schluessel, string Pruefsumme)[] abdruck)
        {
            Katalogtabelle t = Katalogfassung.Tabelle(tabelle);
            List<string> spalten = t.Fachspalten.ToList();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Katalog_Schluessel, Katalog_Pruefsumme, " + Katalogfassung.Spaltentext(spalten) +
                " FROM \"" + tabelle + "\" WHERE ID <= 6 ORDER BY ID", null);
            Assert.Equal(abdruck.Length, dt.Rows.Count);
            for (int i = 0; i < abdruck.Length; i++)
            {
                DataRow r = dt.Rows[i];
                Assert.Equal(abdruck[i].Id, Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture));
                Assert.Equal(abdruck[i].Schluessel, r["Katalog_Schluessel"]);
                Assert.Equal(abdruck[i].Pruefsumme, r["Katalog_Pruefsumme"]);
                // Die Fachwerte bilden heute noch dieselbe Summe - auch mit dem Art-Vorschlag in der Spaltenliste.
                Assert.Equal(abdruck[i].Pruefsumme, Katalogfassung.PruefsummeDerZeile(t, spalten, r, Katalogfassung.LeseOhneVorgang));
            }
        }

        /// <summary>SHA-256 über jede Spalte der Zeilen 1–6 beider Kataloge außer dem Art-Vorschlag, in Spaltenfolge des Namens.</summary>
        private static string Zeilenabdruck()
        {
            var sb = new StringBuilder();
            foreach (string tabelle in new[] { KaeltebedarfSchema.TAB_KOPF_STAMM, KaeltebedarfSchema.TAB_TYP_STAMM })
            {
                DataTable dt = DataRepository.GetDataTable("SELECT * FROM \"" + tabelle + "\" WHERE ID <= 6 ORDER BY ID", null);
                List<string> spalten = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName)
                                         .Where(c => c != KaeltebedarfsprofilVorlagenSchema.SPALTE_ART)
                                         // KBP-5a: die Nutzungstage nach Norm (Schritt 234) sind eine spaetere neue Spalte.
                                         .Where(c => c != BedarfsprofilEingabeartSchema.SPALTE_NUTZUNGSTAGE)
                                         .OrderBy(c => c, StringComparer.Ordinal).ToList();
                foreach (DataRow r in dt.Rows)
                {
                    sb.Append('#').Append(tabelle).Append('\n');
                    foreach (string c in spalten)
                    {
                        object w = r[c];
                        string text = w == DBNull.Value ? "NULL"
                            : w is double d ? "R:" + d.ToString("R", CultureInfo.InvariantCulture)
                            : w is bool b ? (b ? "I:1" : "I:0")
                            : w is long || w is int || w is short || w is byte
                                ? "I:" + Convert.ToInt64(w, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
                            : "T:" + Convert.ToString(w, CultureInfo.InvariantCulture);
                        sb.Append(c).Append('=').Append(text).Append('\n');
                    }
                }
            }
            using (SHA256 sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(sb.ToString()))
                                        .Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }

        [Fact]
        public void Die_Eingabeart_steht_auf_jahr_und_die_Pruefklausel_greift()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KaeltebedarfsprofilVorlagenSchema.EingabeartLesbar());
            DataTable info = DataRepository.GetDataTable("PRAGMA table_info(\"Z_Projekt_Kaeltebedarf\")", null);
            DataRow spalte = info.Rows.Cast<DataRow>().Single(r => (string)r["name"] == "Eingabeart");
            Assert.Equal(1L, Convert.ToInt64(spalte["notnull"], CultureInfo.InvariantCulture));
            Assert.Equal("'jahr'", Convert.ToString(spalte["dflt_value"], CultureInfo.InvariantCulture));

            // Eine Zuordnung anlegen: Projektkopie aus dem Katalog, Zuordnung ohne Eingabeart -> Vorgabe 'jahr'.
            long idProjekt = Zahl("SELECT MIN(ID) FROM Tab_Projekt");
            int kopie = KaeltebedarfStammCtrl.CopyFromStamm("Rechenzentrum", (int)idProjekt);
            Assert.True(kopie > 0);
            Assert.Equal("PROZESS", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Kaelte_Art FROM Tab_Kaeltebedarf WHERE ID = ?", new DbParam("@id", kopie)), CultureInfo.InvariantCulture));
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Z_Projekt_Kaeltebedarf (ID_Projekt, ID_Kaeltebedarf, Bezeichner, Summe) VALUES (?, ?, 'Probe', 10)",
                new DbParam("@p", idProjekt), new DbParam("@k", kopie));
            long idZ = Zahl("SELECT MAX(ID) FROM Z_Projekt_Kaeltebedarf");
            Assert.Equal("jahr", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Eingabeart FROM Z_Projekt_Kaeltebedarf WHERE ID = ?", new DbParam("@id", idZ)), CultureInfo.InvariantCulture));
            Z_ProjektKaeltebedarfModel z = Z_ProjektKaeltebedarfCtrl.LiesProjekt((int)idProjekt).Single(m => m.ID_Z == idZ);
            Assert.Equal("jahr", z.Eingabeart);

            foreach (string gut in KaeltebedarfsprofilVorlagenSchema.EINGABEARTEN)
            {
                DataRepository.ExecuteNonQuery("UPDATE Z_Projekt_Kaeltebedarf SET Eingabeart = ? WHERE ID = ?",
                                               new DbParam("@e", gut), new DbParam("@id", idZ));
                Assert.Equal(gut, Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT Eingabeart FROM Z_Projekt_Kaeltebedarf WHERE ID = ?", new DbParam("@id", idZ)), CultureInfo.InvariantCulture));
            }
            // Ein fremder Wert und NULL scheitern an der Prüfklausel bzw. NOT NULL - der Wert bleibt.
            foreach (object schlecht in new object[] { "tag", "JAHR", DBNull.Value })
            {
                Assert.ThrowsAny<Exception>(() =>
                {
                    using (DbVorgang v = DataRepository.Vorgang())
                    {
                        v.Ausfuehren("UPDATE Z_Projekt_Kaeltebedarf SET Eingabeart = ? WHERE ID = ?",
                                     new DbParam("@e", schlecht), new DbParam("@id", idZ));
                        v.Commit();
                    }
                });
            }
            Assert.Equal("typtag", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Eingabeart FROM Z_Projekt_Kaeltebedarf WHERE ID = ?", new DbParam("@id", idZ)), CultureInfo.InvariantCulture));
            // Der Art-Vorschlag kennt nur die Werte der Zuordnung.
            Assert.ThrowsAny<Exception>(() =>
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    v.Ausfuehren("UPDATE Tab_Kaeltebedarf_STAMM SET Kaelte_Art = 'KAELTE' WHERE ID = 7");
                    v.Commit();
                }
            });
        }

        [Fact]
        public void Der_Schritt_ist_wiederholbar_und_ueberschreibt_keinen_eigenen_Satz()
        {
            if (!_db.Vorhanden) return;
            string abdruckVorher = Zeilenabdruck();
            long seqVorher = Zahl("SELECT seq FROM sqlite_sequence WHERE name = 'Tab_Kaeltebedarf_STAMM'");
            var bericht = new List<string>();
            Assert.Equal(0, KaeltebedarfsprofilVorlagenSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("stehen bereits"));
            Assert.Equal(seqVorher, Zahl("SELECT seq FROM sqlite_sequence WHERE name = 'Tab_Kaeltebedarf_STAMM'"));
            Assert.Equal(abdruckVorher, Zeilenabdruck());

            // Eine eigene Zeile belegt den Namen "Labor" und die ID 16 ist frei: nicht gesät, eigener Satz bleibt.
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Kaeltebedarf_STAMM WHERE ID = 16");
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Kaeltetyp_STAMM WHERE ID = 16");
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Kaeltetyp_STAMM (Bezeichner, ReadOnly) VALUES ('Labor', 0)");
            KaeltevorlagenSaat.Bericht b = KaeltevorlagenSaat.Ausfuehren(null);
            Assert.Equal(0, b.Koepfe);
            Assert.Equal(new[] { "Labor" }, b.Eigene);

            // Eine eigene Zeile belegt die ID 15 unter anderem Namen: die Vorlage kommt mit vergebener ID.
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Kaeltetyp_STAMM WHERE Bezeichner = 'Labor'");
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Kaeltebedarf_STAMM WHERE ID = 15");
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Kaeltebedarf_STAMM (ID, Bezeichner, ReadOnly) VALUES (15, 'Eigen', 0)");
            b = KaeltevorlagenSaat.Ausfuehren(null);
            Assert.Equal(2, b.Koepfe);      // Labor (ID 16 frei) und Schule (ID 15 belegt)
            Assert.Equal(1, b.Profile);     // nur Labor; das Wochenprofil der Schule stand noch
            Assert.Equal(1, b.IdBelegt);
            Assert.Equal(16L, Zahl("SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = 'Labor'"));
            Assert.True(Zahl("SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = 'Schule/Hochschule'") > 16);
            Assert.Equal("Eigen", DataRepository.ExecuteScalar("SELECT Bezeichner FROM Tab_Kaeltebedarf_STAMM WHERE ID = 15"));
            Assert.Equal(0, KaeltevorlagenSaat.Ausfuehren(null).Koepfe);
            Assert.Equal(abdruckVorher, Zeilenabdruck());
        }

        private static long Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
