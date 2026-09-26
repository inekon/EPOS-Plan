using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Arbeitsdatenbank eines Projektbaums beim Anheben eines Pakets</b>
    /// (Konzept Projektpaket-Migration, Weg B).
    ///
    /// <para>Ein Paket führt seine Zeilen als JSON im Spaltenbild der Quelle. Damit die
    /// Paketstufen DIESELBEN Anweisungen fahren können wie die Schemamigration (SQL-Texte
    /// der Bausteine unter <c>Allgemein/Update</c>), liegen die Zeilen eines Baums für die
    /// Dauer der Anhebung in einer SQLite-Datenbank im Speicher: je Pakettabelle eine
    /// Tabelle mit genau den Paketspalten (ohne Typ, die Werte bleiben, wie sie kamen), dazu
    /// die mitgereisten Kataloge als Nachschlagetabellen.</para>
    ///
    /// <para><b>Zurückgeschrieben</b> werden nur die Baumtabellen. Eine Zelle, deren Wert
    /// sich nicht geändert hat, behält ihr JSON-Element — die Anhebung fasst nichts an, was
    /// keine Stufe angefasst hat. Eine Tabelle, die eine Stufe neu angelegt hat, reist als
    /// neue Pakettabelle weiter (<see cref="NeueTabellen"/>).</para>
    ///
    /// <para>Die Anweisungen laufen über <see cref="SqliteDatenzugriff.ErzeugeKommando"/> —
    /// dieselbe Übersetzung der <c>?</c>-Platzhalter wie überall im Kern.</para>
    /// </summary>
    internal sealed class Paketarbeitsdatenbank : IDisposable
    {
        /// <summary>Die Hilfsspalte, über die jede Zeile ihr JSON-Original wiederfindet.</summary>
        internal const string ZEILE = "__zeile";

        private readonly SqliteConnection _verbindung;
        private readonly Dictionary<string, List<Dictionary<string, JsonElement>>> _baum;
        private readonly HashSet<string> _nachschlagen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _neu = new List<string>();

        /// <param name="baum">Die Zeilen eines Projektbaums (Tabelle → Zeilen); sie werden
        /// beim <see cref="Zurueckschreiben"/> ersetzt.</param>
        /// <param name="nachschlagen">Kataloge des Pakets, nur zum Lesen (darf <c>null</c> sein).</param>
        internal Paketarbeitsdatenbank(Dictionary<string, List<Dictionary<string, JsonElement>>> baum,
                                       IEnumerable<KeyValuePair<string, List<Dictionary<string, JsonElement>>>> nachschlagen)
        {
            _baum = baum ?? throw new ArgumentNullException(nameof(baum));
            // Ohne Fremdschlüssel: Die Arbeitsdatenbank kennt keine Primärschlüssel, und die
            // Beziehungen prüft am Ziel der Import (Umschlüsselung, Selbstheilung).
            _verbindung = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
            _verbindung.Open();
            Roh("PRAGMA foreign_keys = OFF");

            foreach (KeyValuePair<string, List<Dictionary<string, JsonElement>>> t in _baum)
                Laden(t.Key, t.Value, true);

            if (nachschlagen != null)
                foreach (KeyValuePair<string, List<Dictionary<string, JsonElement>>> t in nachschlagen)
                {
                    if (_baum.ContainsKey(t.Key) || _nachschlagen.Contains(t.Key)) continue;
                    Laden(t.Key, t.Value, false);
                    _nachschlagen.Add(t.Key);
                }
        }

        /// <summary>Tabellen, die eine Stufe angelegt hat und die als Pakettabelle weiterreisen.</summary>
        internal IReadOnlyList<string> NeueTabellen => _neu;

        public void Dispose() => _verbindung.Dispose();

        // =================================================================
        //  Laden
        // =================================================================

        private void Laden(string tabelle, List<Dictionary<string, JsonElement>> zeilen, bool mitZeile)
        {
            var spalten = new List<string>();
            var gesehen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, JsonElement> z in zeilen ?? new List<Dictionary<string, JsonElement>>())
                foreach (string s in z.Keys)
                    if (!string.Equals(s, ZEILE, StringComparison.OrdinalIgnoreCase) && gesehen.Add(s)) spalten.Add(s);

            var kopf = new List<string>();
            if (mitZeile) kopf.Add(Q(ZEILE) + " INTEGER");
            kopf.AddRange(spalten.Select(Q));
            if (kopf.Count == 0) kopf.Add(Q(ZEILE) + " INTEGER");
            Roh("CREATE TABLE " + Q(tabelle) + " (" + string.Join(", ", kopf) + ")");
            if (zeilen == null || zeilen.Count == 0 || spalten.Count == 0) return;

            var namen = new List<string>();
            if (mitZeile) namen.Add(ZEILE);
            namen.AddRange(spalten);
            string sql = "INSERT INTO " + Q(tabelle) + " (" + string.Join(", ", namen.Select(Q)) + ") VALUES (" +
                         string.Join(", ", namen.Select(_ => "?")) + ")";

            using (SqliteTransaction tx = _verbindung.BeginTransaction())
            {
                for (int i = 0; i < zeilen.Count; i++)
                {
                    var p = new List<DbParam>();
                    if (mitZeile) p.Add(new DbParam("@z", i));
                    foreach (string s in spalten)
                        p.Add(new DbParam("@" + p.Count.ToString(CultureInfo.InvariantCulture),
                                          zeilen[i].TryGetValue(s, out JsonElement je) ? AlsWert(je) : DBNull.Value));
                    using (SqliteCommand cmd = SqliteDatenzugriff.ErzeugeKommando(_verbindung, tx, sql, p.ToArray()))
                        cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }

        // =================================================================
        //  Auskunft und Anweisungen der Stufen
        // =================================================================

        internal bool TabelleVorhanden(string tabelle)
        {
            object o = Skalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ? COLLATE NOCASE",
                              new DbParam("@t", tabelle));
            return o != null && o != DBNull.Value && Convert.ToInt64(o, CultureInfo.InvariantCulture) > 0;
        }

        internal bool SpalteVorhanden(string tabelle, string spalte)
        {
            if (!TabelleVorhanden(tabelle)) return false;
            foreach (string s in Spalten(tabelle))
                if (string.Equals(s, spalte, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Legt eine fehlende Spalte ohne Typ an — der Schritt der Migration hätte sie
        /// mit Vorgabe angelegt, die Vorgabe setzt am Ziel der Import.</summary>
        internal void SpalteSicherstellen(string tabelle, string spalte)
        {
            if (!SpalteVorhanden(tabelle, spalte))
                Roh("ALTER TABLE " + Q(tabelle) + " ADD COLUMN " + Q(spalte));
        }

        /// <summary>Eine Nachschlagetabelle, die das Paket nicht führt, leer anlegen (und
        /// fehlende Spalten ergänzen) — damit eine Anweisung mit Unterabfrage läuft. Sie
        /// wird nicht zurückgeschrieben.</summary>
        internal void NachschlagenSicherstellen(string tabelle, params string[] spalten)
        {
            if (!TabelleVorhanden(tabelle))
            {
                Roh("CREATE TABLE " + Q(tabelle) + " (" + string.Join(", ", spalten.Select(Q)) + ")");
                _nachschlagen.Add(tabelle);
                return;
            }
            foreach (string s in spalten) SpalteSicherstellen(tabelle, s);
        }

        internal int Ausfuehren(string sql, params DbParam[] parameter)
        {
            using (SqliteCommand cmd = SqliteDatenzugriff.ErzeugeKommando(_verbindung, null, sql, parameter))
                return cmd.ExecuteNonQuery();
        }

        internal object Skalar(string sql, params DbParam[] parameter)
        {
            using (SqliteCommand cmd = SqliteDatenzugriff.ErzeugeKommando(_verbindung, null, sql, parameter))
                return cmd.ExecuteScalar();
        }

        /// <summary>Liest roh — die Werte, wie SQLite sie hält (long, double, string, DBNull).</summary>
        internal DataTable Lesen(string sql, params DbParam[] parameter)
        {
            var dt = new DataTable { Locale = CultureInfo.InvariantCulture };
            using (SqliteCommand cmd = SqliteDatenzugriff.ErzeugeKommando(_verbindung, null, sql, parameter))
            using (SqliteDataReader r = cmd.ExecuteReader())
            {
                for (int i = 0; i < r.FieldCount; i++)
                {
                    string name = r.GetName(i);
                    string eindeutig = name;
                    for (int n = 1; dt.Columns.Contains(eindeutig); n++) eindeutig = name + n.ToString(CultureInfo.InvariantCulture);
                    dt.Columns.Add(eindeutig, typeof(object));
                }
                while (r.Read())
                {
                    object[] werte = new object[r.FieldCount];
                    for (int i = 0; i < r.FieldCount; i++) werte[i] = r.IsDBNull(i) ? DBNull.Value : r.GetValue(i);
                    dt.Rows.Add(werte);
                }
            }
            return dt;
        }

        private List<string> Spalten(string tabelle)
        {
            var liste = new List<string>();
            DataTable dt = Lesen("SELECT name FROM pragma_table_info(?)", new DbParam("@t", tabelle));
            foreach (DataRow r in dt.Rows) liste.Add(Convert.ToString(r[0], CultureInfo.InvariantCulture));
            return liste;
        }

        private void Roh(string sql)
        {
            using (SqliteCommand cmd = SqliteDatenzugriff.ErzeugeKommando(_verbindung, null, sql, null))
                cmd.ExecuteNonQuery();
        }

        // =================================================================
        //  Zurückschreiben
        // =================================================================

        /// <summary>
        /// Ersetzt die Zeilen jeder Baumtabelle durch den Stand der Arbeitsdatenbank und
        /// nimmt neu angelegte Tabellen auf. Unveränderte Zellen behalten ihr JSON-Element.
        /// </summary>
        internal void Zurueckschreiben()
        {
            var alle = new List<string>();
            foreach (DataRow r in Lesen("SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY rowid").Rows)
                alle.Add(Convert.ToString(r[0], CultureInfo.InvariantCulture));

            foreach (string tabelle in alle)
            {
                if (tabelle.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase)) continue;
                if (_nachschlagen.Contains(tabelle)) continue;

                bool bekannt = _baum.TryGetValue(tabelle, out List<Dictionary<string, JsonElement>> original);
                bool mitZeile = SpalteVorhanden(tabelle, ZEILE);
                DataTable dt = Lesen("SELECT * FROM " + Q(tabelle) +
                                     (mitZeile ? " ORDER BY (" + Q(ZEILE) + " IS NULL), " + Q(ZEILE) + ", rowid" : " ORDER BY rowid"));

                var neu = new List<Dictionary<string, JsonElement>>(dt.Rows.Count);
                foreach (DataRow r in dt.Rows)
                {
                    Dictionary<string, JsonElement> alt = null;
                    if (mitZeile && bekannt && r[ZEILE] != DBNull.Value)
                    {
                        int i = Convert.ToInt32(r[ZEILE], CultureInfo.InvariantCulture);
                        if (i >= 0 && i < original.Count) alt = original[i];
                    }

                    var zeile = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                    foreach (DataColumn c in dt.Columns)
                    {
                        if (string.Equals(c.ColumnName, ZEILE, StringComparison.OrdinalIgnoreCase)) continue;
                        object wert = r[c];
                        if (alt != null && alt.TryGetValue(c.ColumnName, out JsonElement je) && Gleich(je, wert))
                            zeile[c.ColumnName] = je;
                        else
                            zeile[c.ColumnName] = JsonSerializer.SerializeToElement(wert == DBNull.Value ? null : wert);
                    }
                    neu.Add(zeile);
                }

                if (!bekannt)
                {
                    if (neu.Count == 0) continue;      // eine leere neue Tabelle reist nicht
                    _neu.Add(tabelle);
                }
                _baum[tabelle] = neu;
            }
        }

        // =================================================================
        //  Werte
        // =================================================================

        private static object AlsWert(JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined: return DBNull.Value;
                case JsonValueKind.True: return 1L;
                case JsonValueKind.False: return 0L;
                case JsonValueKind.Number: return je.TryGetInt64(out long l) ? (object)l : je.GetDouble();
                case JsonValueKind.String: return je.GetString();
                default: return je.GetRawText();
            }
        }

        private static bool Gleich(JsonElement je, object wert)
        {
            object alt = AlsWert(je);
            if (alt == DBNull.Value || wert == DBNull.Value || wert == null) return alt == DBNull.Value && (wert == DBNull.Value || wert == null);
            if (alt is string sa) return wert is string sb && string.Equals(sa, sb, StringComparison.Ordinal);
            if (wert is string) return false;
            if (alt is long la && wert is long lb) return la == lb;
            try
            {
                return Convert.ToDouble(alt, CultureInfo.InvariantCulture)
                       .Equals(Convert.ToDouble(wert, CultureInfo.InvariantCulture));
            }
            catch (Exception) { return false; }
        }

        private static string Q(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";
    }
}
