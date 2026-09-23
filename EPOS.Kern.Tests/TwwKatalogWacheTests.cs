using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Wächter über den Tww-Katalog der Repo-Testdatenbank</b> (Umsetzungskonzept
    /// Zapfprofilgenerator, Kapitel 6 (b), (c); Stufe Z0, Posten P11).
    ///
    /// <para><b>Warum es ihn braucht.</b> Die Testdatenbank liegt im Repository und ist
    /// Messlatte für Tests, Referenzlauf und CI. Normzahlen, Kennwerte und Referenzprofile
    /// dürfen dort nie stehen; die Auslieferungswerte kommen aus einem Katalogpaket außerhalb
    /// des Repositoriums (<c>Werkzeuge/Auslieferungsvorlage --katalogpaket</c>). Eine Zeile
    /// mit <c>Status = 'AUSLIEFERUNG'</c> in der Testdatenbank wäre genau so ein Wert — er
    /// fiele sonst erst in einer Vorlage oder einem Wiki-Beispiel auf.</para>
    ///
    /// <para><b>Vier Fälle:</b> keine Zeile mit <c>Status = 'AUSLIEFERUNG'</c>; jede
    /// Katalogzeile ist <c>EIGEN</c> UND trägt in jeder Herkunftsspalte <c>FIKTIV</c> und in
    /// jeder Quellenspalte „Testkatalog (fiktiv)“ (Kapitel 6 (b) verlangt alles zugleich;
    /// eine Tabelle ohne Status — die Tagesgänge — prüft nur Herkunft und Quelle, eine ohne
    /// Herkunftsspalte — der Tagesgangsatz — nur den Status); die Katalogversion ist nie
    /// leer; das Einspielskript <c>Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py</c> ist
    /// wiederholbar — ein weiterer Lauf auf einer Arbeitskopie ändert keine Tww-Zeile.</para>
    ///
    /// <para><b>Nur LESEND</b>, über <c>mode=ro&amp;immutable=1</c> wie
    /// <see cref="TestdatenbankSchemastandWacheTests"/> — ohne Beidateien. Fehlt die Datei,
    /// wird nicht geprüft; fehlt Python (<c>py</c> bzw. <c>python3</c>), schweigt der
    /// Skriptfall.</para>
    /// </summary>
    public class TwwKatalogWacheTests
    {
        /// <summary>Die sieben Katalogtabellen; die ersten fünf tragen Status und Katalogversion.</summary>
        private static readonly string[] KOEPFE =
        {
            TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, TwwSchema.TAB_TWW_PARAMETER_STAMM,
            TwwSchema.TAB_TWW_DIN4708_WERT_STAMM
        };

        private static readonly string[] KINDER =
        {
            TwwSchema.TAB_TWW_TAGESGANG_STAMM, TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM
        };

        private const string SKRIPT = "Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py";

        /// <summary>Die Quelle jeder Zeile des fiktiven Testkatalogs (Kapitel 6 (b)).</summary>
        private const string QUELLE_FIKTIV = "Testkatalog (fiktiv)";

        /// <summary>Frist eines Skriptlaufs; danach wird abgebrochen statt die CI zu blockieren.</summary>
        private const int SKRIPT_FRIST_MS = 120000;

        /// <summary>
        /// <b>Vermerk Übergang Z2 (23.09.2026).</b> Die Stufe Z2 erweitert den fiktiven
        /// Testkatalog (Parameter der Auslegung, zwei Bedarfstage, DIN-4708-Werte); die
        /// Repo-Testdatenbank bekommt ihn erst beim Nachzug im Merge der Stufe Z2 (Skript auf die
        /// Repo-Datei, LFS). Bis dahin darf Lauf 0 auf der Arbeitskopie Zeilen anlegen und
        /// nachführen; ab dem zweiten Lauf gilt 0/0. Trägt die Repo-Datei jeden Parameterschlüssel
        /// von Bilanz und Auslegung (Nachzug erfolgt), gilt 0/0 schon ab Lauf 0 — der Übergang
        /// endet von selbst.
        /// </summary>
        internal const string VERMERK_UEBERGANG_Z2 =
            "Übergang Z2 (Vermerk 23.09.2026): Lauf 0 darf bis zum Nachzug der Testdatenbank beim Merge der Stufe Z2 anlegen.";

        [Fact]
        public void Keine_Zeile_mit_Status_AUSLIEFERUNG_in_der_Testdatenbank()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            using SqliteConnection c = Oeffnen(pfad);
            var funde = new List<string>();
            foreach (string t in KOEPFE)
            {
                Assert.True(TabelleDa(c, t), t + " fehlt in der Testdatenbank (Schemaschritt 103).");
                long n = Zahl(c, "SELECT COUNT(*) FROM \"" + t + "\" WHERE \"Status\" = $w", TwwSchema.STATUS_AUSLIEFERUNG);
                if (n > 0) funde.Add(t + ": " + n);
            }
            Assert.True(funde.Count == 0,
                "Die Testdatenbank fuehrt Tww-Zeilen mit Status AUSLIEFERUNG (Kapitel 6 (b), (c)) — " +
                "Auslieferungswerte kommen nur aus dem Katalogpaket ausserhalb des Repositoriums:\n" +
                string.Join("\n", funde));
        }

        /// <summary>
        /// Kapitel 6 (b) verlangt alles ZUGLEICH: <c>Status = 'EIGEN'</c>, Herkunftsart
        /// <c>FIKTIV</c> in jeder Herkunftsspalte und die Quelle „Testkatalog (fiktiv)“ in jeder
        /// Quellenspalte. Eine Zeile <c>EIGEN</c> mit Herkunftsart <c>VERFAHREN</c> wäre genau
        /// der Weg, auf dem eine echte Normzahl als Anwenderkopie in die Testdatenbank käme;
        /// eine Zeile <c>IMPORT</c> mit <c>FIKTIV</c> ein mitgenommener Fremdkatalog.
        /// </summary>
        [Fact]
        public void Jede_Tww_Katalogzeile_ist_EIGEN_und_FIKTIV()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            using SqliteConnection c = Oeffnen(pfad);
            List<string> funde = Verstoesse(c, out long geprueft);
            Assert.True(funde.Count == 0,
                "Tww-Katalogzeilen der Testdatenbank, die nicht zugleich EIGEN, FIKTIV und mit der Quelle \"" +
                QUELLE_FIKTIV + "\" gefuehrt sind (Kapitel 6 (b)):\n" + string.Join("\n", funde));
            Assert.True(geprueft > 0, "Der fiktive Testkatalog fehlt — die Probe waere leer.");
        }

        /// <summary>
        /// Gegenprobe auf einer Arbeitskopie: Genau die zwei Faelle, die eine Regel „EIGEN ODER
        /// FIKTIV“ durchliesse, schlagen an — eine Nutzungsart EIGEN mit Herkunftsart
        /// VERFAHREN, ein Parameter IMPORT mit Herkunftsart FIKTIV — und dazu ein Tagesgang
        /// mit fremder Quelle.
        /// </summary>
        [Fact]
        public void Gegenprobe_EIGEN_mit_VERFAHREN_und_IMPORT_mit_FIKTIV_schlagen_an()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string ordner = Path.Combine(Path.GetTempPath(), "epos-twwwache-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string kopie = Path.Combine(ordner, "Kenndaten_Test.sqlite");
                File.Copy(pfad, kopie);
                using (var s = new SqliteConnection(new SqliteConnectionStringBuilder
                       { DataSource = kopie, Pooling = false }.ToString()))
                {
                    s.Open();
                    using SqliteCommand b = s.CreateCommand();
                    b.CommandText =
                        "UPDATE \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\" SET \"Bedarf_Herkunftsart\" = $v " +
                        "WHERE \"ID\" = (SELECT MIN(\"ID\") FROM \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\");" +
                        "UPDATE \"" + TwwSchema.TAB_TWW_PARAMETER_STAMM + "\" SET \"Status\" = $i " +
                        "WHERE \"ID\" = (SELECT MIN(\"ID\") FROM \"" + TwwSchema.TAB_TWW_PARAMETER_STAMM + "\");" +
                        "UPDATE \"" + TwwSchema.TAB_TWW_TAGESGANG_STAMM + "\" SET \"Quelle\" = 'Probe' " +
                        "WHERE \"ID\" = (SELECT MIN(\"ID\") FROM \"" + TwwSchema.TAB_TWW_TAGESGANG_STAMM + "\");";
                    b.Parameters.AddWithValue("$v", TwwSchema.HERKUNFT_VERFAHREN);
                    b.Parameters.AddWithValue("$i", TwwSchema.STATUS_IMPORT);
                    Assert.Equal(3, b.ExecuteNonQuery());
                }

                using SqliteConnection c = Oeffnen(kopie);
                List<string> funde = Verstoesse(c, out _);
                Assert.Equal(new[]
                {
                    TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ": 1", TwwSchema.TAB_TWW_PARAMETER_STAMM + ": 1",
                    TwwSchema.TAB_TWW_TAGESGANG_STAMM + ": 1"
                }, funde.ToArray());
            }
            finally
            {
                try { SqliteConnection.ClearAllPools(); } catch { }
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        /// <summary>
        /// Die Tabellen mit Zeilen, die nicht zugleich <c>EIGEN</c> (wo es Status gibt),
        /// <c>FIKTIV</c> in jeder Herkunftsspalte und die fiktive Quelle in jeder
        /// Quellenspalte tragen; <paramref name="geprueft"/> zaehlt die gepruefte Zeilen.
        /// </summary>
        private static List<string> Verstoesse(SqliteConnection c, out long geprueft)
        {
            var funde = new List<string>();
            geprueft = 0;
            foreach (string t in KOEPFE.Concat(KINDER))
            {
                Assert.True(TabelleDa(c, t), t + " fehlt in der Testdatenbank (Schemaschritt 103).");
                List<string> spalten = Spalten(c, t);
                List<string> herkunft = spalten.Where(s => s == "Herkunftsart" ||
                                                           s.EndsWith("_Herkunftsart", StringComparison.Ordinal)).ToList();
                List<string> quelle = spalten.Where(s => s == "Quelle" ||
                                                         s.EndsWith("_Quelle", StringComparison.Ordinal)).ToList();
                bool mitStatus = spalten.Contains("Status");
                if (!mitStatus && herkunft.Count == 0 && quelle.Count == 0) continue;   // Ereignisse: am Kopf geprueft

                // Verlangt: EIGEN (wo es Status gibt) UND in JEDER Herkunftsspalte FIKTIV UND in
                // JEDER Quellenspalte die fiktive Quelle. Verletzt ist jede Zeile, der eines fehlt
                // (IS statt =, damit NULL ebenfalls verletzt).
                var bedingung = new List<string>();
                if (mitStatus) bedingung.Add("\"Status\" IS $e");
                bedingung.AddRange(herkunft.Select(h => "\"" + h + "\" IS $f"));
                bedingung.AddRange(quelle.Select(q => "\"" + q + "\" IS $q"));
                long n = Zahl(c, "SELECT COUNT(*) FROM \"" + t + "\" WHERE NOT (" + string.Join(" AND ", bedingung) + ")",
                              null, ("$f", TwwSchema.HERKUNFT_FIKTIV), ("$e", TwwSchema.STATUS_EIGEN),
                              ("$q", QUELLE_FIKTIV));
                if (n > 0) funde.Add(t + ": " + n);
                geprueft += Zahl(c, "SELECT COUNT(*) FROM \"" + t + "\"", null);
            }
            return funde;
        }

        [Fact]
        public void Die_Katalogversion_ist_nie_leer()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            using SqliteConnection c = Oeffnen(pfad);
            var funde = new List<string>();
            foreach (string t in KOEPFE)
            {
                Assert.True(TabelleDa(c, t), t + " fehlt in der Testdatenbank (Schemaschritt 103).");
                long n = Zahl(c, "SELECT COUNT(*) FROM \"" + t + "\" WHERE \"Katalogversion\" IS NULL OR " +
                                 "trim(\"Katalogversion\") = ''", null);
                if (n > 0) funde.Add(t + ": " + n);
            }
            Assert.True(funde.Count == 0,
                "Tww-Katalogzeilen ohne Katalogversion (natuerlicher Schluessel, Konzept 3.2):\n" + string.Join("\n", funde));
        }

        /// <summary>
        /// Das Einspielskript ist wiederholbar: Ein erster Lauf zieht die Arbeitskopie auf den
        /// Stand des Skripts nach (<see cref="VERMERK_UEBERGANG_Z2"/>: nur bis zum Nachzug der
        /// Repo-Datei; danach legt schon er nichts an); der zweite und dritte Lauf legen nichts an,
        /// führen nichts nach und lassen jede Tww-Zeile gleich. Ohne Python schweigt der Fall —
        /// der Handlauf steht im Kopf des Skripts.
        /// </summary>
        [Fact]
        public void Das_Einspielskript_ist_wiederholbar()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);
            string skript = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(pfad)), SKRIPT);
            Assert.True(File.Exists(skript), "Das Einspielskript fehlt: " + skript);

            string ordner = Path.Combine(Path.GetTempPath(), "epos-twwwache-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string kopie = Path.Combine(ordner, "Kenndaten_Test.sqlite");
                File.Copy(pfad, kopie);

                // Ist die Repo-Datei schon nachgezogen (jeder Parameterschlüssel da)? Dann gilt 0/0
                // ab Lauf 0; sonst der Übergang des Vermerks.
                List<string> fehlendVorher = FehlendeParameter(kopie);
                SqliteConnection.ClearAllPools();
                bool nachgezogen = fehlendVorher.Count == 0;

                (int code, string ausgabe)? erster = PythonStarten(skript, kopie);
                if (erster == null) return;                                 // kein Python - schweigen
                Assert.True(erster.Value.code == 0, "Lauf 0 endete mit " + erster.Value.code + ":\n" + erster.Value.ausgabe);
                if (nachgezogen)
                    Assert.True(erster.Value.ausgabe.Contains("0 Zeile(n) angelegt, 0 nachgefuehrt"),
                        "Die Repo-Datei trägt jeden Parameterschlüssel (Nachzug erfolgt) — Lauf 0 muss 0/0 melden:\n"
                        + erster.Value.ausgabe);
                string vorher = Abbild(kopie);

                // Der Testkatalog trägt jeden Schlüssel, den Bilanz und Auslegung lesen — so rechnet
                // der Generator auf einer Projektkopie ohne fehlenden Parameter.
                List<string> fehlend = FehlendeParameter(kopie);
                Assert.True(fehlend.Count == 0, "Dem Testkatalog fehlen Parameter: " + string.Join(", ", fehlend));
                SqliteConnection.ClearAllPools();

                // Der zweite und der dritte Lauf: streng 0/0, keine Tww-Zeile verändert.
                for (int lauf = 1; lauf <= 2; lauf++)
                {
                    (int code, string ausgabe)? r = PythonStarten(skript, kopie);
                    if (r == null) return;                                  // kein Python - schweigen
                    Assert.True(r.Value.code == 0, "Lauf " + lauf + " endete mit " + r.Value.code + ":\n" + r.Value.ausgabe);
                    Assert.True(r.Value.ausgabe.Contains("0 Zeile(n) angelegt, 0 nachgefuehrt"),
                        "Lauf " + lauf + " (der " + (lauf + 1) + ". Lauf) legt an oder führt nach — nicht wiederholbar ("
                        + VERMERK_UEBERGANG_Z2 + "):\n" + r.Value.ausgabe);
                    Assert.True(vorher == Abbild(kopie), "Lauf " + lauf + " hat Tww-Zeilen veraendert.");
                }
            }
            finally
            {
                try { SqliteConnection.ClearAllPools(); } catch { }
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        /// <summary>Alle Tww-Katalogzeilen als Text, nach Tabelle und Id geordnet.</summary>
        private static string Abbild(string datei)
        {
            var sb = new StringBuilder();
            using var c = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = datei, Mode = SqliteOpenMode.ReadOnly, Pooling = false
            }.ToString());
            c.Open();
            foreach (string t in KOEPFE.Concat(KINDER))
            {
                using SqliteCommand b = c.CreateCommand();
                b.CommandText = "SELECT * FROM \"" + t + "\" ORDER BY \"ID\"";
                using SqliteDataReader r = b.ExecuteReader();
                while (r.Read())
                {
                    sb.Append(t);
                    for (int i = 0; i < r.FieldCount; i++)
                        sb.Append('|').Append(r.IsDBNull(i) ? "NULL"
                            : Convert.ToString(r.GetValue(i), System.Globalization.CultureInfo.InvariantCulture));
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }

        /// <summary>Die Parameterschlüssel von Bilanz und Auslegung, die der Datei fehlen (Präfixe ausgenommen).</summary>
        private static List<string> FehlendeParameter(string datei)
        {
            var fehlend = new List<string>();
            using SqliteConnection c = Oeffnen(datei);
            foreach (Type t in new[] { typeof(ZapfParameter), typeof(ZapfAuslegungParameter) })
                foreach (System.Reflection.FieldInfo f in t.GetFields(System.Reflection.BindingFlags.Static
                             | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
                {
                    if (!f.IsLiteral || f.FieldType != typeof(string)) continue;
                    string schluessel = (string)f.GetRawConstantValue();
                    if (schluessel.EndsWith(".", StringComparison.Ordinal)) continue;   // Präfix
                    if (Zahl(c, "SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_PARAMETER_STAMM + "\" WHERE \"Schluessel\" = $w",
                             schluessel) == 0)
                        fehlend.Add(schluessel);
                }
            return fehlend;
        }

        /// <summary>Das Einspielskript, vom Ausgabeordner aufwärts gesucht; <c>null</c>, wenn es fehlt.</summary>
        internal static string Einspielskript()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, SKRIPT.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(kandidat)) return kandidat;
            }
            return null;
        }

        /// <summary>
        /// Startet das Skript über <c>py</c> (Windows-Starter) oder <c>python3</c>;
        /// <c>null</c>, wenn keines von beiden startet.
        /// </summary>
        internal static (int, string)? PythonStarten(string skript, string datenbank)
        {
            foreach (string programm in new[] { "py", "python3" })
            {
                var start = new ProcessStartInfo
                {
                    FileName = programm,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                start.ArgumentList.Add(skript);
                start.ArgumentList.Add(datenbank);
                start.Environment["PYTHONIOENCODING"] = "utf-8";
                try
                {
                    using Process p = Process.Start(start);
                    if (p == null) continue;
                    // Beide Kanaele NEBENEINANDER lesen: Nacheinander blockierten Prozess und
                    // Test sich gegenseitig, sobald stderr den Pipe-Puffer fuellt.
                    var aus = p.StandardOutput.ReadToEndAsync();
                    var fehler = p.StandardError.ReadToEndAsync();
                    if (!p.WaitForExit(SKRIPT_FRIST_MS))
                    {
                        try { p.Kill(entireProcessTree: true); } catch { /* schon beendet */ }
                        Assert.Fail("Das Einspielskript lief laenger als " + (SKRIPT_FRIST_MS / 1000) +
                                    " s und wurde abgebrochen: " + skript);
                    }
                    p.WaitForExit();   // leert die asynchronen Leser
                    return (p.ExitCode, aus.GetAwaiter().GetResult() + fehler.GetAwaiter().GetResult());
                }
                catch (Win32Exception) { /* Programm nicht vorhanden - naechstes */ }
            }
            return null;
        }

        private static SqliteConnection Oeffnen(string pfad)
        {
            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            c.Open();
            return c;
        }

        private static bool TabelleDa(SqliteConnection c, string tabelle) =>
            Zahl(c, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $w", tabelle) > 0;

        private static List<string> Spalten(SqliteConnection c, string tabelle)
        {
            var l = new List<string>();
            using SqliteCommand b = c.CreateCommand();
            b.CommandText = "SELECT name FROM pragma_table_info($w)";
            b.Parameters.AddWithValue("$w", tabelle);
            using SqliteDataReader r = b.ExecuteReader();
            while (r.Read()) l.Add(r.GetString(0));
            return l;
        }

        private static long Zahl(SqliteConnection c, string sql, string wert, params (string Name, string Wert)[] weitere)
        {
            using SqliteCommand b = c.CreateCommand();
            b.CommandText = sql;
            if (wert != null) b.Parameters.AddWithValue("$w", wert);
            foreach (var (name, w) in weitere)
                if (sql.Contains(name)) b.Parameters.AddWithValue(name, w);
            object o = b.ExecuteScalar();
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o);
        }

        /// <summary>Die Repo-Testdatenbank wie in <see cref="TestdatenbankSchemastandWacheTests"/>; sonst <c>null</c>.</summary>
        private static string Testdatenbank([System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            string wurzel = null;
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string kandidat = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (kandidat != null && File.Exists(Path.Combine(kandidat, "WP-Plan.sln"))) wurzel = kandidat;
            }
            if (wurzel == null)
            {
                var d = new DirectoryInfo(AppContext.BaseDirectory);
                while (d != null && wurzel == null)
                {
                    if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) wurzel = d.FullName;
                    d = d.Parent;
                }
            }
            if (wurzel == null) return null;
            string datei = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            return File.Exists(datei) ? datei : null;
        }
    }
}
