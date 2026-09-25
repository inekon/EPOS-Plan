using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;

namespace ZapfprofilValidierung
{
    /// <summary>
    /// <b>Die eigene SQL des Werkzeugs</b> — fünf lesende <c>SELECT</c>-Texte auf einer
    /// <b>unveränderlichen</b>, ungepoolten Verbindung (<c>immutable=1</c>, <c>Mode=ReadOnly</c>):
    /// SQLite legt damit weder <c>-wal</c> noch <c>-shm</c> neben die Quelle und setzt keine Sperre.
    /// Die Katalogdatei — in der Regel die Testdatenbank, die Messlatte jedes Referenzlaufs — bleibt
    /// byte-gleich.
    ///
    /// <para><b>Nicht über <c>SqliteDatenzugriff</c></b>: Dieses Werkzeug öffnet eine <i>fremde</i>
    /// Datei nach Pfad, nicht die Datenbank des Anwenders; die Zugriffsschicht des Kerns kennt nur
    /// letztere. Der <c>SqlDialektPruefer</c> sieht <c>Werkzeuge/</c> nicht — die fünf Texte stehen
    /// deshalb als Konstanten hier und laufen in den Proben des Werkzeugs gegen eine Kopie der
    /// Testdatenbank. Bauform übernommen von <c>Werkzeuge/Gebaeudevergleich/Sqlitehilfe.cs</c>.</para>
    /// </summary>
    internal static class Sqlitehilfe
    {
        /// <summary>
        /// Die URI einer Quelle mit <c>immutable=1</c>; maskiert werden <c>%</c>, Leerzeichen,
        /// <c>#</c> und <c>?</c>, Rückstriche werden Schrägstriche.
        /// </summary>
        internal static string UnveraenderlichUri(string pfad)
        {
            string p = Path.GetFullPath(pfad).Replace('\\', '/');
            if (!p.StartsWith("/", StringComparison.Ordinal)) p = "/" + p;   // file:///C:/…
            var sb = new StringBuilder(p.Length + 16);
            foreach (char c in p)
            {
                switch (c)
                {
                    case '%': sb.Append("%25"); break;
                    case ' ': sb.Append("%20"); break;
                    case '#': sb.Append("%23"); break;
                    case '?': sb.Append("%3F"); break;
                    default: sb.Append(c); break;
                }
            }
            return "file://" + sb + "?immutable=1";
        }

        /// <summary>Öffnet die Datei nur lesend und unveränderlich.</summary>
        internal static SqliteConnection Unveraenderlich(string pfad)
        {
            var b = new SqliteConnectionStringBuilder
            {
                DataSource = UnveraenderlichUri(pfad),
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            };
            var v = new SqliteConnection(b.ToString());
            v.Open();
            return v;
        }

        /// <summary>
        /// Liest eine Tabelle als Zeilen von Feldname → Text (leeres Feld = <c>null</c>) — dieselbe
        /// Gestalt, die der Paketleser aus einer CSV-Datei liefert. So baut
        /// <see cref="Katalogbau"/> aus beiden Quellen mit <b>einem</b> Weg.
        /// </summary>
        internal static List<Dictionary<string, string>> Tabelle(SqliteConnection v, string sql)
        {
            var zeilen = new List<Dictionary<string, string>>();
            using SqliteCommand k = v.CreateCommand();
            k.CommandText = sql;
            using SqliteDataReader r = k.ExecuteReader();
            while (r.Read())
            {
                var zeile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < r.FieldCount; i++)
                    zeile[r.GetName(i)] = r.IsDBNull(i) ? null : Convert.ToString(r.GetValue(i),
                                              System.Globalization.CultureInfo.InvariantCulture);
                zeilen.Add(zeile);
            }
            return zeilen;
        }

        /// <summary>Gibt es die Tabelle? Ein Katalog ohne Kategorien rechnet deterministisch weiter.</summary>
        internal static bool TabelleVorhanden(SqliteConnection v, string tabelle)
        {
            using SqliteCommand k = v.CreateCommand();
            k.CommandText = SQL_TABELLE_VORHANDEN;
            k.Parameters.AddWithValue("@name", tabelle);
            using SqliteDataReader r = k.ExecuteReader();
            return r.Read();
        }

        internal const string SQL_TABELLE_VORHANDEN =
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name = @name";

        internal const string SQL_NUTZUNGSART =
            "SELECT * FROM Tab_TwwNutzungsart_STAMM ORDER BY ID";

        internal const string SQL_TAGESGANGSATZ =
            "SELECT * FROM Tab_TwwTagesgangsatz_STAMM ORDER BY ID";

        internal const string SQL_TAGESGANG =
            "SELECT * FROM Tab_TwwTagesgang_STAMM ORDER BY ID_Tagesgangsatz, Tagtyp";

        internal const string SQL_PARAMETER =
            "SELECT * FROM Tab_TwwParameter_STAMM ORDER BY Schluessel";

        internal const string SQL_ZAPFKATEGORIE =
            "SELECT * FROM Tab_TwwZapfkategorie_STAMM ORDER BY ID_Nutzungsart, Reihenfolge";
    }
}
