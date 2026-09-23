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
    /// Katalogzeile ist <c>EIGEN</c> oder trägt in jeder Herkunftsspalte <c>FIKTIV</c> (die
    /// Tagesgänge, die keinen Status haben, tragen <c>FIKTIV</c>); die Katalogversion ist nie
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
                Assert.True(TabelleDa(c, t), t + " fehlt in der Testdatenbank (Schemaschritt 102).");
                long n = Zahl(c, "SELECT COUNT(*) FROM \"" + t + "\" WHERE \"Status\" = $w", TwwSchema.STATUS_AUSLIEFERUNG);
                if (n > 0) funde.Add(t + ": " + n);
            }
            Assert.True(funde.Count == 0,
                "Die Testdatenbank fuehrt Tww-Zeilen mit Status AUSLIEFERUNG (Kapitel 6 (b), (c)) — " +
                "Auslieferungswerte kommen nur aus dem Katalogpaket ausserhalb des Repositoriums:\n" +
                string.Join("\n", funde));
        }

        [Fact]
        public void Jede_Tww_Katalogzeile_ist_FIKTIV_oder_EIGEN()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            using SqliteConnection c = Oeffnen(pfad);
            var funde = new List<string>();
            long geprueft = 0;
            foreach (string t in KOEPFE.Concat(KINDER))
            {
                Assert.True(TabelleDa(c, t), t + " fehlt in der Testdatenbank (Schemaschritt 102).");
                List<string> spalten = Spalten(c, t);
                List<string> herkunft = spalten.Where(s => s == "Herkunftsart" ||
                                                           s.EndsWith("_Herkunftsart", StringComparison.Ordinal)).ToList();
                bool mitStatus = spalten.Contains("Status");
                if (!mitStatus && herkunft.Count == 0) continue;   // Ereignisse: am Kopf geprueft

                // Verletzt: weder EIGEN noch in JEDER Herkunftsspalte FIKTIV.
                string fiktiv = herkunft.Count == 0
                    ? "0"
                    : string.Join(" AND ", herkunft.Select(h => "\"" + h + "\" = $f"));
                string eigen = mitStatus ? "\"Status\" = $e" : "0";
                long n = Zahl(c, "SELECT COUNT(*) FROM \"" + t + "\" WHERE NOT ((" + eigen + ") OR (" + fiktiv + "))",
                              null, ("$f", TwwSchema.HERKUNFT_FIKTIV), ("$e", TwwSchema.STATUS_EIGEN));
                if (n > 0) funde.Add(t + ": " + n);
                geprueft += Zahl(c, "SELECT COUNT(*) FROM \"" + t + "\"", null);
            }
            Assert.True(funde.Count == 0,
                "Tww-Katalogzeilen der Testdatenbank, die weder EIGEN noch FIKTIV sind (Kapitel 6 (b)):\n" +
                string.Join("\n", funde));
            Assert.True(geprueft > 0, "Der fiktive Testkatalog fehlt — die Probe waere leer.");
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
                Assert.True(TabelleDa(c, t), t + " fehlt in der Testdatenbank (Schemaschritt 102).");
                long n = Zahl(c, "SELECT COUNT(*) FROM \"" + t + "\" WHERE \"Katalogversion\" IS NULL OR " +
                                 "trim(\"Katalogversion\") = ''", null);
                if (n > 0) funde.Add(t + ": " + n);
            }
            Assert.True(funde.Count == 0,
                "Tww-Katalogzeilen ohne Katalogversion (natuerlicher Schluessel, Konzept 3.2):\n" + string.Join("\n", funde));
        }

        /// <summary>
        /// Das Einspielskript ist wiederholbar: Die Repo-Datei ist das Ergebnis eines ersten
        /// Laufs; zwei weitere Läufe auf einer Arbeitskopie legen nichts an, führen nichts nach
        /// und lassen jede Tww-Zeile gleich. Ohne Python schweigt der Fall — der Handlauf
        /// steht im Kopf des Skripts.
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
                string vorher = Abbild(kopie);

                for (int lauf = 1; lauf <= 2; lauf++)
                {
                    (int code, string ausgabe)? r = PythonStarten(skript, kopie);
                    if (r == null) return;                                  // kein Python - schweigen
                    Assert.True(r.Value.code == 0, "Lauf " + lauf + " endete mit " + r.Value.code + ":\n" + r.Value.ausgabe);
                    Assert.Contains("0 Zeile(n) angelegt, 0 nachgefuehrt", r.Value.ausgabe);
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

        /// <summary>
        /// Startet das Skript über <c>py</c> (Windows-Starter) oder <c>python3</c>;
        /// <c>null</c>, wenn keines von beiden startet.
        /// </summary>
        private static (int, string)? PythonStarten(string skript, string datenbank)
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
                    string aus = p.StandardOutput.ReadToEnd();
                    string fehler = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    return (p.ExitCode, aus + fehler);
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
