using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Waechter ueber die DUBLETTEN in <c>Z_ProjektPufferSp</c> (Auftrag #307,
    /// Anwenderentscheid vom 16.09.2026).
    ///
    /// <para><b>Der Befund, der ihn ausgeloest hat.</b> Die Repo-Testdatenbank fuehrte
    /// fuenf wortgleiche Wiederholungen: dasselbe Projekt, derselbe Pufferspeicher,
    /// derselbe Erzeuger, dieselben Vor- und Ruecklauftemperaturen, dieselbe
    /// Prioritaet — nur die Id unterschied sich. Entfernt hat sie
    /// <c>sql/tools/Bereinige-Pufferdubletten</c>; der Referenzlauf blieb dabei
    /// 13/13 byte-gleich, die Wiederholungen waren also wirkungslos.</para>
    ///
    /// <para><b>Eine Wache statt eines Zwangs.</b> Bewusst KEIN eindeutiger Index und
    /// kein Schemaschritt: Ob zwei Zeilen mit demselben Erzeuger je fachlich richtig
    /// sein koennen, ist nicht belegt, und ein Index wuerde diese Frage stillschweigend
    /// entscheiden. Dieser Fall MELDET stattdessen — mit Projekt, Puffer, Erzeuger und
    /// den Ids, damit der Naechste entscheiden kann.</para>
    ///
    /// <para><b>Gemessen wird die REPO-Datei</b>, nicht eine Arbeitskopie: Sie ist die
    /// Messlatte fuer CI, Werkzeuge und jeden neuen Klon. Geoeffnet wird lesend ueber
    /// eine <c>file:</c>-URI mit <c>mode=ro</c> und <c>immutable=1</c> — ohne
    /// <c>immutable=1</c> legte SQLite <c>-wal</c>/<c>-shm</c> neben die Datenbank, und
    /// die meldete <see cref="RepositoryOrdnungWacheTests"/> als Rueckstand. Fehlt die
    /// Datei, wird nicht geprueft; ein LFS-Zeiger bekommt seine benannte Meldung.</para>
    /// </summary>
    public class PufferzuordnungWacheTests
    {
        /// <summary>
        /// Die Merkmale, die eine Zuordnung ausmachen. Die GLEICHHEIT dieser drei ist
        /// der Befund, den der Auftrag meldet („gleiche Projekt-, Puffer- und
        /// Erzeugerangabe mehr als einmal") — nicht erst die Gleichheit aller Spalten.
        /// Zwei Zeilen, die sich nur in Temperatur oder Prioritaet unterscheiden, sind
        /// genauso wenig erklaert wie zwei voellig gleiche.
        /// </summary>
        private const string Gruppe = "ID_Projekt, ID_Pufferspeicher, Erzeuger";

        [Fact]
        public void Keine_Pufferzuordnung_steht_zweimal_im_selben_Projekt()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;              // keine Datei - nichts zu pruefen

            LfsZeigerProbe.Sicherstellen(pfad);

            List<string> dubletten = Dubletten(pfad);

            Assert.True(dubletten.Count == 0,
                "Z_ProjektPufferSp fuehrt " + dubletten.Count +
                " mehrfach besetzte Zuordnung(en) in " + pfad + ":" +
                Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", dubletten) + Environment.NewLine +
                "Wortgleiche Wiederholungen entfernt " +
                "\"python3 sql/tools/Bereinige-Pufferdubletten.py --db " + pfad +
                " --anwenden\" (Probe auf einer Kopie, dann die echte Datei). " +
                "Unterscheiden sich die Zeilen in Temperatur, Prioritaet oder " +
                "Schwellen, ist es kein Fall fuer das Werkzeug, sondern ein " +
                "Fachentscheid: Welche Zeile gilt?");
        }

        /// <summary>Je mehrfach besetzter Gruppe eine Zeile Klartext.</summary>
        private static List<string> Dubletten(string pfad)
        {
            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") +
                         "?mode=ro&immutable=1";

            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = uri
            }.ToString());
            verbindung.Open();

            using SqliteCommand befehl = verbindung.CreateCommand();
            befehl.CommandText =
                "SELECT ID_Projekt, ID_Pufferspeicher, Erzeuger, COUNT(*), GROUP_CONCAT(ID) " +
                "FROM Z_ProjektPufferSp GROUP BY " + Gruppe + " HAVING COUNT(*) > 1 " +
                "ORDER BY ID_Projekt, ID_Pufferspeicher";

            var befunde = new List<string>();
            using SqliteDataReader leser = befehl.ExecuteReader();
            while (leser.Read())
                befunde.Add("Projekt " + leser.GetValue(0) +
                            ", Puffer " + leser.GetValue(1) +
                            ", Erzeuger \"" + leser.GetValue(2) + "\": " +
                            leser.GetValue(3) + " Zeilen (Ids " + leser.GetValue(4) + ")");
            return befunde;
        }

        /// <summary>
        /// Sucht <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> unter der Repowurzel;
        /// <c>null</c>, wenn es sie nicht gibt — woertlich wie in
        /// <see cref="TestdatenbankSchemastandWacheTests"/>.
        /// </summary>
        private static string Testdatenbank(
            [System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            string wurzel = null;

            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string kandidat = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (kandidat != null && File.Exists(Path.Combine(kandidat, "WP-Plan.sln")))
                    wurzel = kandidat;
            }

            if (wurzel == null)
            {
                DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
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
