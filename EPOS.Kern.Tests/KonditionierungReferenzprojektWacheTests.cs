using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Referenzlauf;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ein Fall, der erst mit der Basis scharf wird, die 1051 führt</b>: Liegt unter <c>Referenzlaeufe/</c>
    /// genau eine Basis und führt sie den Ordner <c>Projekt_1051</c>, läuft der Fall; sonst ist er
    /// übersprungen mit dem Grund „Basis R34 noch nicht eingefroren". Ohne Eingriff am Test: Das Einfrieren
    /// der Basis R34 legt den Ordner an, und der Fall prüft seither.
    /// </summary>
    public sealed class BasisMit1051FactAttribute : FactAttribute
    {
        internal const string GRUND = "Basis R34 noch nicht eingefroren";

        public BasisMit1051FactAttribute()
        {
            if (KonditionierungReferenzprojektWacheTests.BasisOrdner1051() == null) Skip = GRUND;
        }
    }

    /// <summary>
    /// <b>Wache des Referenzprojekts 1051 „Referenzprojekt Konditionierung"</b> (Entwurf KP3, Festlegung 33;
    /// Teilkonzept Konditionierungsprofile 10.2) — das einzige Projekt der Testdatenbank mit angelegten
    /// Kalendern aller fünf Größen, Ferien, Feiertagen, Heizperiode, Nachtauskühlung, Sommerlüftung und der
    /// Aufheizoptimierung mit der Bemessung (b). Bau, gesäte Zellen und Programmwege stehen in EINER Quelle,
    /// dem Bauplan <c>Referenzlaeufe/Skripte/referenzprojekt_1051_bauplan.cs</c> (verlinkt); das Saatskript
    /// <c>referenzprojekt_1051_konditionierung.cs</c> zieht ihn. Muster: <see cref="ZonenReferenzprojektWacheTests"/>.
    /// <list type="bullet">
    /// <item><b>(a) Genau 1051</b> trägt Kalender und Schalter des Referenzbaus — kein anderes Projekt und kein
    /// anderer Katalogbau als der Referenzkatalogbau führt die fünf „Büro"-Kalender, die Nachtauskühlung,
    /// die Sommerlüftung oder die Ferien.</item>
    /// <item><b>(b) Jede gesäte Zelle</b> steht wie im Bauplan (<see cref="Konditionierungsprojekt1051.Pruefen"/>),
    /// dazu Vorgaben, Kalender, Perioden und Gebäudezellen ausdrücklich.</item>
    /// <item><b>(c) Dieselben Programmwege auf einer Arbeitskopie</b> ergeben einen bitgleichen Abdruck von
    /// Projekt und Referenzkatalogbau (ohne Schlüssel und Namen).</item>
    /// <item><b>(d) Der Lauf ist deterministisch</b>: Zwei Läufe des Referenzlaufs („lauf",
    /// <c>Ergebnisexport.ProjektAusfuehren</c>) schreiben bytegleiche Ordner (<c>protokoll.txt</c>
    /// schreibt der Gesamtlauf, nicht der Projektlauf).</item>
    /// <item><b>(e) Die ausgelieferte Vorlage „Büro"</b> ist unverändert — die Nachtzeile der Lüftung
    /// bleibt 0,1 1/h, die Nachtauskühlung lebt allein am Referenzbau.</item>
    /// <item><b>(f) Gegen die Basis</b>: <c>heizsollwert_&lt;n&gt;.csv</c> und <c>raumtemperatur_&lt;n&gt;.csv</c>
    /// von <c>Projekt_1051</c> — übersprungen, solange die Basis den Ordner nicht führt (<see cref="BasisMit1051FactAttribute"/>).</item>
    /// </list>
    /// 1051 steht in der Basis R34 (Einfrierregel „gesäte
    /// Konditionierungsdaten", Teilkonzept 10.3).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KonditionierungReferenzprojektWacheTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public KonditionierungReferenzprojektWacheTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        private const int PROJEKT = Konditionierungsprojekt1051.NEU;

        /// <summary>Das Gebäude der Projektkopie und der Referenzkatalogbau der gesäten Testdatenbank.</summary>
        internal const int GEBAEUDE = 10657, KATALOGBAU = 289;

        private static long Zahl(string sql, params object[] w) => Zonenprojekt1052.Zahl(sql, w);

        private static List<long> Liste(string sql, params object[] w)
            => Zonenprojekt1052.Tabelle(sql, w).Rows.Cast<DataRow>().Select(r => Convert.ToInt64(r[0], CultureInfo.InvariantCulture)).ToList();

        private static string Text(object o)
            => o == null || o == DBNull.Value ? "∅" : o is double d ? d.ToString("R", CultureInfo.InvariantCulture)
               : Convert.ToString(o, CultureInfo.InvariantCulture);

        /// <summary>Die Zeilen einer Abfrage als „a|b|c", eine je Zeile.</summary>
        private static List<string> Zeilen(string sql, params object[] w)
            => Zonenprojekt1052.Tabelle(sql, w).Rows.Cast<DataRow>().Select(r => string.Join("|", r.ItemArray.Select(Text))).ToList();

        // =====================================================================
        //  (a) Genau 1051
        // =====================================================================

        [Fact]
        public void Genau_1051_traegt_Kalender_und_Schalter_des_Referenzbaus()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(KATALOGBAU, Konditionierungsprojekt1051.Referenzbau());
            Assert.Equal(GEBAEUDE, Zonenprojekt1052.Gebaeude(PROJEKT));
            Assert.Equal(Konditionierungsprojekt1051.NAME, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture));

            // Die fünf Kalender am Gebäude (ohne Zone) und am Katalogbau: nur 1051 und der Referenzbau.
            Assert.Equal(new long[] { PROJEKT }, Liste(
                "SELECT DISTINCT g.ID_Projekt FROM Tab_Konditionierungskalender k JOIN Tab_Gebaeude g ON g.ID = k.ID_Gebaeude " +
                "WHERE k.ID_Zone IS NULL ORDER BY 1"));
            Assert.Equal(new long[] { GEBAEUDE }, Liste(
                "SELECT ID_Gebaeude FROM Tab_Konditionierungskalender WHERE ID_Gebaeude IS NOT NULL AND ID_Zone IS NULL " +
                "GROUP BY ID_Gebaeude HAVING COUNT(DISTINCT Groesse) = 5 ORDER BY 1"));
            Assert.Equal(new long[] { KATALOGBAU }, Liste(
                "SELECT DISTINCT ID_Gebaeude_Stamm FROM Tab_Konditionierungskalender WHERE ID_Gebaeude_Stamm IS NOT NULL ORDER BY 1"));
            Assert.Equal(new long[] { KATALOGBAU }, Liste(
                "SELECT DISTINCT ID_Gebaeude_Stamm FROM Tab_Konditionierungsvorgabe WHERE ID_Gebaeude_Stamm IS NOT NULL ORDER BY 1"));

            // Die Nachtauskühlung (bedingte Nachtzeile der Lüftung): nur Gebäude 10657 und Referenzbau 289.
            Assert.Equal(new[] { "∅|" + KATALOGBAU, GEBAEUDE + "|∅" }, Zeilen(
                "SELECT ID_Gebaeude, ID_Gebaeude_Stamm FROM Tab_Konditionierungsvorgabe WHERE Bedingt_K IS NOT NULL " +
                "ORDER BY ID_Gebaeude IS NOT NULL, ID"));

            // Die Schalter und Ferien des Referenzbaus: kein anderes Gebäude, kein anderer Katalogbau.
            Assert.Equal(new long[] { PROJEKT }, Liste("SELECT DISTINCT ID_Projekt FROM Tab_Gebaeude WHERE Sommerlueftung = 1 ORDER BY 1"));
            Assert.Equal(new long[] { KATALOGBAU }, Liste("SELECT ID FROM Tab_Gebaeude_STAMM WHERE Sommerlueftung = 1 ORDER BY 1"));
            Assert.Equal(new long[] { PROJEKT }, Liste(
                "SELECT DISTINCT ID_Projekt FROM Tab_Gebaeude WHERE Ferienbeginn_1 IS NOT NULL AND Ferienbeginn_1 NOT IN (0, 366) ORDER BY 1"));
            Assert.Equal(new long[] { KATALOGBAU }, Liste(
                "SELECT ID FROM Tab_Gebaeude_STAMM WHERE Ferienbeginn_1 = ? AND Ferienende_1 = ? ORDER BY 1",
                Konditionierungsprojekt1051.FERIEN[0].Beginn, Konditionierungsprojekt1051.FERIEN[0].Ende));
            Assert.Equal(new long[] { PROJEKT }, Liste(
                "SELECT ID_Projekt FROM Tab_Gebaeude WHERE ID_Gebaeude_Stamm = ? ORDER BY 1", KATALOGBAU));
            Assert.Equal(new long[] { 1051, 1052 }, Liste(
                "SELECT ID_Projekt FROM Tab_Einstellungen WHERE Aufheizoptimierung = 1 ORDER BY 1"));
        }

        // =====================================================================
        //  (b) Jede gesäte Zelle
        // =====================================================================

        /// <summary>Die sechzehn Vorgabezeilen von Gebäude und Referenzbau: Größe|Zeile|Wert|Aus|Von|Bis|Bedingt_K.</summary>
        internal static string[] Vorgaben()
        {
            string nacht = Konditionierungsprojekt1051.NACHTLUEFTUNG.ToString("R", CultureInfo.InvariantCulture) + "|0|" +
                           Konditionierungsprojekt1051.NACHTLUEFTUNG_VON + "|" + Konditionierungsprojekt1051.NACHTLUEFTUNG_BIS + "|" +
                           Konditionierungsprojekt1051.NACHTLUEFTUNG_ABSTAND_K.ToString("R", CultureInfo.InvariantCulture);
            return new[]
            {
                "HEIZSOLL|SAISON|∅|0|" + Konditionierungsprojekt1051.HEIZPERIODE_BEGINN + "|" + Konditionierungsprojekt1051.HEIZPERIODE_ENDE + "|∅",
                "KUEHLSOLL|NACHT|∅|1|18|7|∅",
                "KUEHLSOLL|WOCHENENDE|∅|1|∅|∅|∅",
                "KUEHLSOLL|FERIEN|∅|1|∅|∅|∅",
                "LUEFTUNG|NACHT|" + nacht,
                "LUEFTUNG|WOCHENENDE|0.1|0|∅|∅|∅",
                "LUEFTUNG|FERIEN|0.1|0|∅|∅|∅",
                "GERAETE|TAG|1|0|∅|∅|∅",
                "GERAETE|NACHT|0.1|0|18|7|∅",
                "GERAETE|WOCHENENDE|0.1|0|∅|∅|∅",
                "GERAETE|FERIEN|0.1|0|∅|∅|∅",
                "PERSONEN|NENNWERT|1232|0|∅|∅|∅",
                "PERSONEN|TAG|1|0|∅|∅|∅",
                "PERSONEN|NACHT|0|0|17|8|∅",
                "PERSONEN|WOCHENENDE|0|0|∅|∅|∅",
                "PERSONEN|FERIEN|0|0|∅|∅|∅",
            };
        }

        /// <summary>Die Perioden ohne Feiertage: Größe|Rang|Art|Bezeichner|Beginn|Ende|Wert|Aus.</summary>
        internal static string[] Perioden()
        {
            var f = Konditionierungsprojekt1051.FERIEN;
            var l = new List<string>
            {
                "HEIZSOLL|900|BETRIEBSPAUSE|Saison|" + (Konditionierungsprojekt1051.HEIZPERIODE_ENDE + 1) + "|" +
                (Konditionierungsprojekt1051.HEIZPERIODE_BEGINN - 1) + "|∅|1",
            };
            foreach ((string g, string w) in new[] { ("KUEHLSOLL", "∅|1"), ("LUEFTUNG", "0.1|0"), ("GERAETE", "0.1|0"), ("PERSONEN", "0|0") })
                for (int i = 0; i < f.Length; i++)
                    l.Add(g + "|" + (200 + i) + "|FERIEN|Ferien " + (i + 1) + "|" + f[i].Beginn + "|" + f[i].Ende + "|" + w);
            return l.ToArray();
        }

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_im_Bauplan()
        {
            if (!_db.Vorhanden) return;
            List<string> abw = Konditionierungsprojekt1051.Pruefen(PROJEKT);
            Assert.True(abw.Count == 0, string.Join("\n", abw));

            const string VORGABE = "SELECT Groesse, Zeile, Wert, Aus, Von, Bis, Bedingt_K FROM Tab_Konditionierungsvorgabe WHERE {0} = ? ORDER BY ID";
            const string KALENDER = "SELECT Groesse, Wert, Aus, Woche, Nennwert, Bemerkung FROM Tab_Konditionierungskalender WHERE {0} = ? ORDER BY ID";
            const string PERIODE = "SELECT k.Groesse, p.Rang, p.Art, p.Bezeichner, p.Beginn, p.Ende, p.Wert, p.Aus FROM Tab_Konditionierungsperiode p " +
                                   "JOIN Tab_Konditionierungskalender k ON k.ID = p.ID_Kalender WHERE k.{0} = ? AND p.Art <> 'FEIERTAG' ORDER BY k.ID, p.Rang";
            const string FEIERTAG = "SELECT k.Groesse, p.Rang, p.Feiertagsregel, p.Beginn, p.Ende, p.Wert, p.Aus, p.WieWochentag FROM Tab_Konditionierungsperiode p " +
                                    "JOIN Tab_Konditionierungskalender k ON k.ID = p.ID_Kalender WHERE k.{0} = ? AND p.Art = 'FEIERTAG' ORDER BY k.ID, p.Rang";
            string[] regeln = { "NEUJAHR", "KARFREITAG", "OSTERMONTAG", "ERSTER_MAI", "HIMMELFAHRT", "PFINGSTMONTAG", "EINHEIT", "WEIHNACHTEN_1", "WEIHNACHTEN_2" };
            string[] groessen = { "HEIZSOLL", "KUEHLSOLL", "LUEFTUNG", "GERAETE", "PERSONEN" };

            foreach ((string spalte, long id) in new[] { ("ID_Gebaeude_Stamm", (long)KATALOGBAU), ("ID_Gebaeude", (long)GEBAEUDE) })
            {
                Assert.Equal(Vorgaben(), Zeilen(string.Format(CultureInfo.InvariantCulture, VORGABE, spalte), id).ToArray());
                List<string> kal = Zeilen(string.Format(CultureInfo.InvariantCulture, KALENDER, spalte), id);
                Assert.Equal(groessen, kal.Select(k => k.Split('|')[0]).ToArray());
                Assert.All(kal, k => Assert.EndsWith("|aus Vorlage Büro", k, StringComparison.Ordinal));
                Assert.All(kal, k => Assert.StartsWith(k.Split('|')[0] + "|∅|0|", k, StringComparison.Ordinal));  // Wochenform
                Assert.Equal("3232", kal[3].Split('|')[4]);                                                      // Geräte: Nennwert = Interne_Waermegewinne
                Assert.Equal(Perioden(), Zeilen(string.Format(CultureInfo.InvariantCulture, PERIODE, spalte), id).ToArray());
                Assert.Equal(groessen.SelectMany(g => regeln.Select((r, i) => g + "|" + (100 + i) + "|" + r + "|∅|∅|∅|0|7")).ToArray(),
                             Zeilen(string.Format(CultureInfo.InvariantCulture, FEIERTAG, spalte), id).ToArray());
            }
            // Gebäude und Referenzbau tragen dieselben Kalender (der Kopierweg Katalog → Projekt).
            Assert.Equal(Zeilen(string.Format(CultureInfo.InvariantCulture, KALENDER, "ID_Gebaeude_Stamm"), KATALOGBAU),
                         Zeilen(string.Format(CultureInfo.InvariantCulture, KALENDER, "ID_Gebaeude"), GEBAEUDE));

            // Die Zellen des Gebäudes (und des Baus): Schalter, Ferien, Nachtzeit, Sollwerte, Lüftung, Gewinne.
            const string KOPF = "SELECT Kuehlung_Aktiv, Sommerlueftung, Ferienbeginn_1, Ferienende_1, Ferienbeginn_2, Ferienende_2, " +
                                "Ferienbeginn_3, Ferienende_3, Ferienbeginn_4, Ferienende_4, Nachtabsenkung_Beginn, Nachtabsenkung_Ende, " +
                                "Raumsolltemperatur_Tag, Raumsolltemperatur_Nachtabsenkung, Raumsolltemperatur_Wochenende, Raumsolltemperatur_Ferien, " +
                                "Kuehl_Sollwert, Kuehl_Sollwert_Nacht, Luftwechsel_Infiltration, Luftwechsel_Nutzer, Luftwechselrate, " +
                                "Interne_Waermegewinne, Ferien, Wochenende, Heizleistung_Max, Wohnflaeche_gesamt FROM \"{0}\" WHERE ID = ?";
            const string SOLL = "1|1|357|6|213|226|0|0|0|0|18|7|20|16|16|16|26|∅|0.3|0.3|0.6|3232|1|1|∅|572";
            Assert.Equal(SOLL, Assert.Single(Zeilen(string.Format(CultureInfo.InvariantCulture, KOPF, "Tab_Gebaeude"), GEBAEUDE)));
            Assert.Equal(SOLL, Assert.Single(Zeilen(string.Format(CultureInfo.InvariantCulture, KOPF, "Tab_Gebaeude_STAMM"), KATALOGBAU)));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID = ? AND Aufheizzeit_Manuell_H IS NULL", GEBAEUDE));
            Assert.Equal(572.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Wohnflaeche_Waermebedarf FROM Z_ProjektGebaeude WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture));

            // Die Projekteinstellung: Kühlbetrieb aus (der Schalter Kuehlung_Aktiv = 1 bleibt ohne Rechenwirkung),
            // Aufheizvorgabe (b) 2 K, täglich, Reserve leer, kein Aufschlag.
            Assert.False(KonfigurationCtrl.KuehlbetriebLesen(PROJEKT));
            Aufheizvorgabe v = KonfigurationCtrl.AufheizvorgabeLesen(PROJEKT);
            Assert.Equal(Konditionierungsprojekt1051.AUFHEIZ, v);
            Assert.True(v.An);
            Assert.Equal(DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, v.Bemessung);
            Assert.Equal(2.0, v.AbzugK);
            Assert.Null(v.Reserve);
            Assert.Equal(0.2, v.ReserveWirksam);
            Assert.Equal(DbWerte.AUFHEIZ_ART_TAEGLICH, v.ArtWirksam);
            Assert.False(v.HatAufschlag);
            Assert.Equal("1|STUNDE_ABZUG|2|∅|∅|0|0", Assert.Single(Zeilen(
                "SELECT Aufheizoptimierung, Aufheiz_Bemessung, Aufheiz_Abzug_K, Aufheiz_Reserve, Aufheiz_Art, " +
                "COALESCE(Aufheiz_Aufschlag_H, 0), COALESCE(Aufheiz_Aufschlag_Prozent, 0) FROM Tab_Einstellungen WHERE ID_Projekt = ?", PROJEKT)));
        }

        // =====================================================================
        //  (c) Dieselben Programmwege — bitgleicher Abdruck
        // =====================================================================

        /// <summary>Der Abdruck eines Katalogbaus samt Konditionierung, ohne Schlüssel.</summary>
        internal static string KatalogbauAbdruck(int id)
        {
            var sb = new StringBuilder();
            foreach ((string kopf, string sql) in new[]
            {
                ("Tab_Gebaeude_STAMM", "SELECT * FROM Tab_Gebaeude_STAMM WHERE ID = ?"),
                ("Tab_Konditionierungsvorgabe", "SELECT * FROM Tab_Konditionierungsvorgabe WHERE ID_Gebaeude_Stamm = ? ORDER BY ID"),
                ("Tab_Konditionierungskalender", "SELECT * FROM Tab_Konditionierungskalender WHERE ID_Gebaeude_Stamm = ? ORDER BY ID"),
                ("Tab_Konditionierungsperiode", "SELECT p.* FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k " +
                                                "ON k.ID = p.ID_Kalender WHERE k.ID_Gebaeude_Stamm = ? ORDER BY p.ID"),
            })
            {
                DataTable dt = Zonenprojekt1052.Tabelle(sql, id);
                sb.Append('#').Append(kopf).Append('\n');
                foreach (DataRow r in dt.Rows)
                {
                    foreach (DataColumn c in dt.Columns)
                        if (!c.ColumnName.Split('_').Any(t => t.Equals("ID", StringComparison.OrdinalIgnoreCase)))
                            sb.Append(Text(r[c])).Append('|');
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }

        private static void Gleich(string soll, string ist, string was)
        {
            if (ist == soll) return;
            string[] a = soll.Split('\n'), b = ist.Split('\n');
            int i = 0;
            while (i < Math.Min(a.Length, b.Length) && a[i] == b[i]) i++;
            Assert.Fail(was + ": Abdruck weicht ab ab Zeile " + i + ":\n  1051:    " + (i < a.Length ? a[i] : "∅") +
                        "\n  Nachbau: " + (i < b.Length ? b[i] : "∅"));
        }

        [Fact]
        public void Dieselben_Programmwege_auf_einer_Arbeitskopie_ergeben_einen_bitgleichen_Abdruck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            // Unter der Kultur des Saatskripts: Die Herkunft eines übernommenen Kalenders steht in der
            // Anzeigesprache in Bemerkung („aus Vorlage Büro").
            using var kultur = new Kulturvorrichtung("de-DE");
            const string NACHBAU = "RP1b Nachbau 1051";
            const string NACHBAU_BAU = "RP1b Nachbau Referenzbau";
            string sollProjekt = Zonenprojekt1052.Abdruck(PROJEKT, true);
            string sollBau = KatalogbauAbdruck(KATALOGBAU);

            int id = new ProjektDuplizierenCtrl().Duplizieren(Konditionierungsprojekt1051.VORLAGE_NAME, NACHBAU);
            Assert.True(id > Zonenprojekt1052.NEU, "Die Kopie fiel auf " + id);
            string fehler = Konditionierungsprojekt1051.KatalogbauAnlegen(Konditionierungsprojekt1051.KATALOGBAU, NACHBAU_BAU, out int kb);
            Assert.True(fehler == null, fehler);
            Assert.True(kb > KATALOGBAU, "Der Nachbau des Katalogbaus fiel auf " + kb);
            fehler = Konditionierungsprojekt1051.Bauen(id, kb);
            Assert.True(fehler == null, fehler);

            string NamenZurueck(string s) => s.Replace(NACHBAU_BAU, Konditionierungsprojekt1051.REFERENZBAU)
                                              .Replace(NACHBAU, Konditionierungsprojekt1051.NAME);
            string istProjekt = NamenZurueck(Zonenprojekt1052.Abdruck(id, true));
            string istBau = NamenZurueck(KatalogbauAbdruck(kb));
            Gleich(sollBau, istBau, "Referenzkatalogbau");
            Gleich(sollProjekt, istProjekt, "Projekt");
            _aus.WriteLine("Abdruck 1051 = Nachbau {0}: {1} Zeilen, {2} Zeichen; Referenzbau {3} = Nachbau {4}: {5} Zeilen, {6} Zeichen",
                           id, sollProjekt.Count(c => c == '\n'), sollProjekt.Length, KATALOGBAU, kb, sollBau.Count(c => c == '\n'), sollBau.Length);
        }

        // =====================================================================
        //  (d) Der Lauf ist deterministisch
        // =====================================================================

        /// <summary>Ein Lauf des Referenzlaufs („lauf") für 1051 in <paramref name="ziel"/>; die Zahl der Dateien.</summary>
        internal static int Lauf(string ziel)
        {
            var log = new Protokoll();
            int n = Ergebnisexport.ProjektAusfuehren(PROJEKT, ziel, log);
            Assert.True(n > 0, "Der Lauf von " + PROJEKT + " scheiterte (" + log.Fehler + " Fehler).");
            return n;
        }

        private static string Ordner(string name)
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-rp1b-" + name + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        [Fact]
        public void Zwei_Laeufe_von_1051_schreiben_bytegleiche_Ordner()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string a = Ordner("a"), b = Ordner("b");
            try
            {
                int na = Lauf(a), nb = Lauf(b);
                Assert.Equal(na, nb);
                string[] da = Directory.GetFiles(a).Select(Path.GetFileName).Where(f => f != "protokoll.txt").OrderBy(f => f, StringComparer.Ordinal).ToArray();
                string[] dbb = Directory.GetFiles(b).Select(Path.GetFileName).Where(f => f != "protokoll.txt").OrderBy(f => f, StringComparer.Ordinal).ToArray();
                Assert.Equal(da, dbb);
                Assert.Contains("heizsollwert_0.csv", da);
                Assert.Contains("raumtemperatur_0.csv", da);
                Assert.Contains("aggregate.csv", da);
                long bytes = 0;
                foreach (string f in da)
                {
                    byte[] x = File.ReadAllBytes(Path.Combine(a, f)), y = File.ReadAllBytes(Path.Combine(b, f));
                    Assert.True(x.AsSpan().SequenceEqual(y), f + " weicht zwischen zwei Läufen ab.");
                    bytes += x.Length;
                }
                _aus.WriteLine("1051: zwei Läufe, {0} Dateien, {1} Bytes bytegleich", da.Length, bytes);
            }
            finally
            {
                Directory.Delete(a, true);
                Directory.Delete(b, true);
            }
        }

        // =====================================================================
        //  (e) Die ausgelieferte Vorlage „Büro"
        // =====================================================================

        [Fact]
        public void Die_ausgelieferte_Vorlage_Buero_ist_unveraendert()
        {
            if (!_db.Vorhanden) return;
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                Assert.NotNull(Konditionierungsprojekt1051.Buero(g));
            KonditionierungsvorlageCtrl.Vorlage l = Konditionierungsprojekt1051.Buero(Konditionierungsgroesse.Lueftung);
            Assert.Equal(1L, Zahl("SELECT ReadOnly FROM Tab_Konditionierungsvorlage_STAMM WHERE ID = ?", l.Id));
            Assert.Equal(new[] { "NACHT|0.1|0|18|7|∅", "WOCHENENDE|0.1|0|∅|∅|∅", "FERIEN|0.1|0|∅|∅|∅" }, Zeilen(
                "SELECT Zeile, Wert, Aus, Von, Bis, Bedingt_K FROM Tab_Konditionierungsvorgabe WHERE ID_Vorlage = ? ORDER BY ID", l.Id).ToArray());
            Assert.Equal("LUEFTUNG|0.1|0|∅", Assert.Single(Zeilen(
                "SELECT Groesse, Wert, Aus, Woche FROM Tab_Konditionierungskalender WHERE ID_Vorlage = ?", l.Id)));
            // Keine Vorlage trägt eine bedingte Nachtzeile; die Nachtauskühlung lebt allein am Referenzbau.
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsvorgabe WHERE ID_Vorlage IS NOT NULL AND Bedingt_K IS NOT NULL"));
        }

        // =====================================================================
        //  (f) Gegen die Basis (scharf ab R34)
        // =====================================================================

        /// <summary>Der Ordner <c>Projekt_1051</c> der aktuellen Basis; <c>null</c> = keine eindeutige Basis oder ohne 1051.</summary>
        internal static string BasisOrdner1051()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !Directory.Exists(Path.Combine(d.FullName, "Referenzlaeufe"))) d = d.Parent;
            if (d == null) return null;
            string[] basen = Directory.GetDirectories(Path.Combine(d.FullName, "Referenzlaeufe"))
                .Where(o => Regex.IsMatch(Path.GetFileName(o), @"^\d{4}-\d{2}-\d{2}_R\d+_")).ToArray();
            if (basen.Length != 1) return null;
            string p = Path.Combine(basen[0], "Projekt_" + PROJEKT);
            return Directory.Exists(p) ? p : null;
        }

        /// <summary>Die Toleranz des Referenzvergleichs: Betrag ≥ 1 relativ 1e-4, sonst absolut 0,01; NaN = NaN.</summary>
        private static bool Nahe(string a, string b)
        {
            if (a == b) return true;
            if (!double.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out double x) ||
                !double.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out double y)) return false;
            double m = Math.Max(Math.Abs(x), Math.Abs(y));
            return m >= 1 ? Math.Abs(x - y) <= 1e-4 * m : Math.Abs(x - y) <= 0.01;
        }

        [BasisMit1051Fact]
        public void Heizsollwert_und_Raumtemperatur_von_1051_stehen_wie_die_Basis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string basis = BasisOrdner1051();
            Assert.NotNull(basis);
            string ziel = Ordner("basis");
            try
            {
                Lauf(ziel);
                string[] dateien = Directory.GetFiles(basis)
                    .Select(Path.GetFileName)
                    .Where(f => Regex.IsMatch(f, @"^(heizsollwert|raumtemperatur)_\d+\.csv$"))
                    .OrderBy(f => f, StringComparer.Ordinal).ToArray();
                Assert.Contains("heizsollwert_0.csv", dateien);
                Assert.Contains("raumtemperatur_0.csv", dateien);
                foreach (string f in dateien)
                {
                    string neu = Path.Combine(ziel, f);
                    Assert.True(File.Exists(neu), "Der Lauf schreibt " + f + " nicht.");
                    string[] s = File.ReadAllLines(Path.Combine(basis, f)), i = File.ReadAllLines(neu);
                    Assert.Equal(s.Length, i.Length);
                    for (int z = 0; z < s.Length; z++)
                    {
                        string[] ps = s[z].Split(';'), pi = i[z].Split(';');
                        Assert.True(ps.Length == pi.Length && ps.Zip(pi).All(p => Nahe(p.First, p.Second)),
                                    f + ", Zeile " + z + ": Basis '" + s[z] + "', Lauf '" + i[z] + "'");
                    }
                }
                _aus.WriteLine("1051 gegen die Basis {0}: {1}", basis, string.Join(", ", dateien));
            }
            finally
            {
                Directory.Delete(ziel, true);
            }
        }
    }
}
