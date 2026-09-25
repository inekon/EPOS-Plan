using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;

namespace Gebaeudevergleich
{
    /// <summary>
    /// <b>Die eigene SQL des Werkzeugs</b> — genau drei feste Texte, jeder über ein eigenes
    /// <see cref="SqliteCommand"/> auf einer eigenen, ungepoolten Verbindung, nie über
    /// <c>SqliteDatenzugriff</c>: Dort finge die Schreibnaht <c>VACUUM INTO</c> als schreibende
    /// Anweisung ab (<c>Datenbanksicherung</c> öffnet dafür eine Freigabe; dieses Werkzeug lässt
    /// die Sperre dagegen ausnahmslos stehen).
    ///
    /// <para>Der <c>SqlDialektPruefer</c> sieht <c>Werkzeuge/</c> nicht; die Texte führen deshalb
    /// die Proben T8 (Aufnahme) und T15 (Variante) aus. Die Parameterschreibweise <c>?</c> ist
    /// die des Kerns und wird mit derselben Übersetzung gebunden
    /// (<c>SqliteDatenzugriff.UebersetzeParameterzeichen</c>).</para>
    /// </summary>
    internal static class Sqlitehilfe
    {
        internal const string SQL_VACUUM_INTO = "VACUUM INTO ?";
        internal const string SQL_INTEGRITAET = "PRAGMA integrity_check";
        internal const string SQL_MODELL_SETZEN = "UPDATE Tab_Gebaeude SET Gebaeude_Modell = ?";

        /// <summary>
        /// Die URI einer Quelle mit <c>immutable=1</c>: SQLite liest die Datei, ohne je eine
        /// Sperre zu setzen oder eine Nebendatei anzulegen — auch im WAL-Modus. Maskiert werden
        /// <c>%</c>, Leerzeichen, <c>#</c> und <c>?</c>; Rückstriche werden Schrägstriche.
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

        /// <summary>Nur lesend und unveränderlich (<c>immutable=1</c>, <c>Mode=ReadOnly</c>, <c>Pooling=False</c>).</summary>
        internal static SqliteConnection Unveraenderlich(string pfad)
            => Oeffnen(new SqliteConnectionStringBuilder
            {
                DataSource = UnveraenderlichUri(pfad),
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            });

        /// <summary>Nur lesend (<c>Mode=ReadOnly</c>, <c>Pooling=False</c>).</summary>
        internal static SqliteConnection NurLesend(string pfad)
            => Oeffnen(new SqliteConnectionStringBuilder
            {
                DataSource = Path.GetFullPath(pfad),
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            });

        private static SqliteConnection Oeffnen(SqliteConnectionStringBuilder b)
        {
            var v = new SqliteConnection(b.ToString());
            v.Open();
            return v;
        }

        /// <summary><c>VACUUM INTO ?</c> — eine in sich geschlossene Kopie, die Quelle bleibt unberührt.</summary>
        internal static void VacuumInto(SqliteConnection quelle, string ziel)
        {
            using SqliteCommand k = Kommando(quelle, SQL_VACUUM_INTO, Path.GetFullPath(ziel));
            k.ExecuteNonQuery();
        }

        /// <summary><c>PRAGMA integrity_check</c> auf einer nur lesenden Verbindung; <c>"ok"</c> oder die Befunde.</summary>
        internal static string Integritaet(string pfad)
        {
            using SqliteConnection v = NurLesend(pfad);
            using SqliteCommand k = Kommando(v, SQL_INTEGRITAET);
            var zeilen = new List<string>();
            using (SqliteDataReader r = k.ExecuteReader())
                while (r.Read() && zeilen.Count < 20) zeilen.Add(Convert.ToString(r.GetValue(0)));
            return string.Join(" | ", zeilen);
        }

        /// <summary>
        /// <c>UPDATE Tab_Gebaeude SET Gebaeude_Modell = ?</c> auf der Variantenkopie; liefert die
        /// Zahl der geänderten Zeilen.
        /// </summary>
        internal static int ModellSetzen(string pfad, string modell)
        {
            var b = new SqliteConnectionStringBuilder
            {
                DataSource = Path.GetFullPath(pfad),
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false
            };
            using SqliteConnection v = Oeffnen(b);
            using SqliteCommand k = Kommando(v, SQL_MODELL_SETZEN, modell);
            return k.ExecuteNonQuery();
        }

        private static SqliteCommand Kommando(SqliteConnection v, string sql, params object[] werte)
        {
            SqliteCommand k = v.CreateCommand();
            k.CommandText = SqliteDatenzugriff.UebersetzeParameterzeichen(sql);
            for (int i = 0; i < werte.Length; i++)
                k.Parameters.AddWithValue("@p" + i, werte[i] ?? DBNull.Value);
            return k;
        }

        /// <summary>
        /// SHA-256 einer Datei, gelesen mit <c>FileShare.ReadWrite | FileShare.Delete</c> — das
        /// laufende Programm darf die Datei dabei offen halten. Kleinbuchstaben-Hex: Die Zeile
        /// geht ohne Namensbereinigung ins Protokoll (<see cref="Ausgabe.ProtokollPruefsumme"/>).
        /// </summary>
        internal static string Sha256(string pfad)
        {
            using var s = new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return Convert.ToHexStringLower(SHA256.HashData(s));
        }
    }
}
