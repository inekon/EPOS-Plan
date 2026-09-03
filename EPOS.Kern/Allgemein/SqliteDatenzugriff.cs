using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1.Allgemein;

namespace WindowsFormsApplication1
{
    // =====================================================================================
    // ARBEITSPAKET iU6-T4: DIE EINE UMSETZUNG VON IDatenzugriff - SQLite.
    //
    // Der gesamte Inhalt dieser Datei stammt WOERTLICH aus DataRepository.cs; verschoben,
    // nicht umgeschrieben. Geaendert sind ausschliesslich:
    //   - "public static" -> "public" bei den zwoelf Vertragsmethoden (sie sind jetzt
    //     Instanzmethoden dieser Klasse und erfuellen damit IDatenzugriff),
    //   - die Rueckgriffe auf die Fassade sind qualifiziert: DataRepository.GetDBPath(),
    //     DataRepository.FehlerMelden(), DataRepository.KurzSql().
    // Fehlerwortlaute, Rueckgabewerte im Fehlerfall, Reihenfolge und Kommentare sind
    // unveraendert - der Referenzlauf muss byte-gleich bleiben.
    //
    // WAS HIER LIEGT UND WARUM. Alles, was SQLite KENNT: Verbindungsaufbau samt PRAGMAs,
    // die Uebersetzung ? -> @pN, die Wertenormalisierung, der Typ-Rueckweg D9 aus dem
    // Reader, die sechs Ausfuehrungs- und die fuenf Schemamethoden. Ein zweiter Anbieter
    // wuerde genau diese Datei ersetzen - und sonst nichts.
    //
    // WAS BEWUSST AUF DER FASSADE BLEIBT (DataRepository): der Engine-Modus mit
    // FehlerMelden/StilleFehlerAbholen (eine Meldeentscheidung fuer das ganze Programm),
    // die Pfadaufloesung mit PfadUeberschreibung und GetDBPath (bekommt in iU5 ihr
    // IPfade) und die vier Bequemlichkeiten (GetMaxID, DeleteWithDependencies,
    // GetIdByName, GetValueById), die nichts als zusammengesetztes SQL ueber den Vertrag
    // schicken.
    //
    // internal sealed: Niemand ausserhalb des Kerns soll diese Klasse direkt anfassen -
    // der Weg fuehrt ueber DataRepository bzw. ab iU5 ueber Dienste.Daten.
    //
    // DREI STATISCHE HELFER BLEIBEN STATISCH, weil DbVorgang, RecordSet, StilleDb,
    // WaermequelleClass, PufferSpCtrl und GeraeteWaisen sie ueber die (weiterhin
    // vorhandenen) Weiterleitungen der Fassade nutzen: OeffneVerbindung,
    // ErzeugeKommando, LadeTabelle. Sie tragen SQLite-Typen in der Signatur und koennen
    // deshalb nicht Teil von IDatenzugriff sein; sie wandern mit einem Anbieterwechsel
    // genauso wie der Rest dieser Datei.
    // =====================================================================================

    internal sealed class SqliteDatenzugriff : IDatenzugriff
    {
        /// <summary>Der Pfad zur Datei, auf die dieser Zugriff arbeitet.</summary>
        public string DatenbankPfad
        {
            get { return DataRepository.GetDBPath(); }
        }


        // =================================================================================
        // Verbindung (Konzept 2.1)
        // =================================================================================

        /// <summary>
        /// Oeffnet eine Verbindung und setzt die verbindungsgebundenen PRAGMAs.
        ///
        /// foreign_keys und busy_timeout gelten JE VERBINDUNG. Microsoft.Data.Sqlite
        /// schaltet Fremdschluessel beim Oeffnen inzwischen von sich aus ein (Befund aus
        /// S3, 02.09.) - anders als SQLite nativ. Die explizite PRAGMA-Zeile bleibt
        /// trotzdem stehen: Sie dokumentiert die Absicht und schuetzt gegen eine
        /// Verhaltensaenderung der Bibliothek. Ohne sie waeren die 90 Fremdschluessel
        /// wirkungslose Dekoration (Rev. 2, 5.3).
        ///
        /// journal_mode=WAL ist dateipersistent und wird einmalig vom Migrator gesetzt.
        /// Das Verbindungspooling bleibt an; die PRAGMAs je Open() sind billig und
        /// idempotent.
        ///
        /// ARBEITSPAKET S4c/S4d - KONSOLIDIERUNG: <c>internal</c> statt privat. S4b musste
        /// den Verbindungsaufbau in <c>StilleDb</c> ein zweites Mal hinschreiben, weil
        /// diese Methode nicht erreichbar war (Vormerkung ebendort). Jetzt gibt es wieder
        /// GENAU EINEN Verbindungsaufbau: <c>StilleDb.OeffneVerbindung</c> ruft nur noch
        /// hierher. Wer eine Verbindung braucht, kommt an dieser Stelle vorbei - PRAGMAs
        /// und Verbindungsstring lassen sich damit an einer einzigen Stelle aendern.
        /// </summary>
        internal static SqliteConnection OeffneVerbindung()
        {
            SqliteConnection verbindung = new SqliteConnection($"Data Source={DataRepository.GetDBPath()};Foreign Keys=True");
            verbindung.Open();
            using (SqliteCommand cmd = verbindung.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
                cmd.ExecuteNonQuery();
            }
            return verbindung;
        }

        /// <summary>
        /// Prüft, ob die Datenbankdatei vorhanden und lesbar ist (Startprüfung,
        /// Konzept 2.8). Tritt an die Stelle des frueheren <c>ProviderVorhanden()</c>:
        /// Einen registrierungspflichtigen Provider gibt es nicht mehr, die einzige
        /// fruehe Diagnose vor <c>SchemaMigration.Ausfuehren</c> soll aber bleiben.
        ///
        /// Die Probe oeffnet ausdruecklich NUR LESEND (<c>Mode=ReadOnly</c>) - sie soll
        /// eine fehlende Datei feststellen, nicht versehentlich eine leere anlegen
        /// (SQLite legt im Standardmodus stillschweigend eine neue Datei an).
        /// </summary>
        public bool DatenbankVorhanden()
        {
            try
            {
                string pfad = DataRepository.GetDBPath();
                if (string.IsNullOrWhiteSpace(pfad)) return false;
                if (!File.Exists(pfad)) return false;

                using (SqliteConnection probe = new SqliteConnection($"Data Source={pfad};Mode=ReadOnly"))
                {
                    probe.Open();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        // =================================================================================
        // Uebersetzung: ? -> @pN und Wertenormalisierung (Konzept 2.2 / 2.3)
        // =================================================================================

        /// <summary>
        /// Ersetzt die OleDb-Platzhalter <c>?</c> strikt in Reihenfolge durch
        /// <c>@p0 … @pN</c>. Ueberspringt '…'-Textliterale (inklusive ''-Escape) und
        /// [eckige Bezeichner].
        ///
        /// Die Messung fand kein ? in einem SQL-Literal (inkl. Kreuzfragment-Pruefung) -
        /// der Schutz kostet trotzdem nichts und sichert kuenftiges SQL ab.
        /// </summary>
        public static string UebersetzeParameterzeichen(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return sql;

            StringBuilder sb = new StringBuilder(sql.Length + 16);
            int n = 0;
            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];
                if (c == '\'')
                {
                    sb.Append(c);
                    for (i++; i < sql.Length; i++)
                    {
                        sb.Append(sql[i]);
                        if (sql[i] == '\'')
                        {
                            if (i + 1 < sql.Length && sql[i + 1] == '\'') { sb.Append(sql[++i]); continue; }
                            break;
                        }
                    }
                    continue;
                }
                if (c == '[')
                {
                    sb.Append(c);
                    for (i++; i < sql.Length && sql[i] != ']'; i++) sb.Append(sql[i]);
                    if (i < sql.Length) sb.Append(']');
                    continue;
                }
                if (c == '?') { sb.Append("@p").Append(n++); continue; }
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Hebt einen Parameterwert auf die Speicherform der SQLite-Datei.
        ///
        /// Das Datumsformat ist bewusst OHNE Sekundenbruchteile und identisch mit dem des
        /// Migrators - sonst stuenden im selben Feld zwei Schreibweisen.
        /// </summary>
        public static object NormalisiereWert(object w)
        {
            if (w == null || w == DBNull.Value) return DBNull.Value;
            if (w is bool b) return b ? 1 : 0;                       // Boolean -> INTEGER 0/1
            if (w is DateTime d)                                     // Datum -> ISO-8601-Text
                return d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            if (w is Guid g) return g.ToString();                    // Altstellen, TEXT
            if (w is decimal m) return (double)m;                    // Ziel ist immer REAL
            return w;
        }

        /// <summary>
        /// Uebersetzt die Datentraeger-Parameter des Bestands. Die NAMEN der
        /// Quellparameter werden bewusst IGNORIERT - OleDb band rein positionsweise,
        /// und der Bestand nutzt beliebige Namen fuer dieselbe Stelle. Unveraendert
        /// gueltig fuer <see cref="DbParam"/> (iU6).
        /// </summary>
        private static SqliteParameter[] UebersetzeParameter(DbParam[] quelle)
        {
            if (quelle == null || quelle.Length == 0) return Array.Empty<SqliteParameter>();
            SqliteParameter[] ziel = new SqliteParameter[quelle.Length];
            for (int i = 0; i < quelle.Length; i++)
                ziel[i] = new SqliteParameter("@p" + i, NormalisiereWert(quelle[i] == null ? null : quelle[i].Wert));
            return ziel;
        }

        /// <summary>
        /// Baut ein fertiges Kommando (SQL uebersetzt, Parameter uebersetzt und gebunden).
        /// <c>internal</c> statt privat, damit <see cref="DbVorgang"/> dieselbe eine
        /// Uebersetzung nutzt und nicht eine zweite Fassung davon entsteht.
        /// </summary>
        internal static SqliteCommand ErzeugeKommando(SqliteConnection verbindung, SqliteTransaction transaktion,
                                                      string sql, DbParam[] parameter)
        {
            SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = UebersetzeParameterzeichen(sql);
            if (transaktion != null) cmd.Transaction = transaktion;
            foreach (SqliteParameter p in UebersetzeParameter(parameter)) cmd.Parameters.Add(p);
            return cmd;
        }


        // =================================================================================
        // Typ-Rueckweg (Bauentscheidung D9, Konzept 2.4)
        // =================================================================================
        //
        // Microsoft.Data.Sqlite bringt keinen DataAdapter mit; geladen wird ueber den
        // Reader. Dabei wird der Typ-Rueckweg ZENTRAL begradigt, statt 675 Konsumstellen
        // anzufassen:
        //   - INTEGER  -> Int32 (alle Ganzzahlspalten stammen aus Access Long/Integer);
        //   - Boolean- und Datumsspalten ueber den generierten SchemaTypKatalog - der
        //     deklarierte SQLite-Typ (INTEGER/TEXT) verraet sie nicht;
        //   - Namensdubletten aus Joins wie der alte Adapter entdoppeln (ID, ID1, ...).
        // Spalten-Aliasse (AS x) entziehen sich dem Katalog; Restabdeckung liefern der
        // begrenzte Dialekt-Sweep (S5) und die Referenzlaeufe (S7).

        /// <summary>Zulaessige Textformen einer Datumsspalte beim Lesen.</summary>
        private static readonly string[] DATUM_FORMATE =
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF",
            "yyyy-MM-dd",
        };

        /// <summary>Behandlung einer Spalte beim Materialisieren.</summary>
        private enum Spaltenart { Unveraendert, GanzzahlInt32, Wahrheitswert, Zeitpunkt }

        /// <summary>
        /// Leitet den Grundtyp einer Spalte aus dem DEKLARIERTEN Typ ab (SQLite-Affinitaet).
        /// Liefert null, wenn die Deklaration nichts hergibt (Ausdrucksspalten, NUMERIC) -
        /// dann entscheidet der beobachtete Wert der ersten Zeile.
        /// </summary>
        private static Type TypAusDeklaration(string deklaration)
        {
            if (string.IsNullOrWhiteSpace(deklaration)) return null;
            string d = deklaration.ToUpperInvariant();
            if (d.Contains("INT")) return typeof(long);
            if (d.Contains("CHAR") || d.Contains("CLOB") || d.Contains("TEXT")) return typeof(string);
            if (d.Contains("BLOB")) return typeof(byte[]);
            if (d.Contains("REAL") || d.Contains("FLOA") || d.Contains("DOUB")) return typeof(double);
            return null;   // NUMERIC/DECIMAL und Unbekanntes: der Wert entscheidet
        }

        /// <summary>
        /// Laedt einen offenen Reader in eine DataTable und stellt dabei die heutigen
        /// CLR-Typen wieder her. Gemeinsamer Kern von <see cref="GetDataTable"/> und
        /// <see cref="DbVorgang.Lese"/>.
        /// </summary>
        internal static DataTable LadeTabelle(SqliteDataReader leser)
        {
            DataTable tabelle = new DataTable();
            int spaltenzahl = leser.FieldCount;
            if (spaltenzahl == 0) return tabelle;

            // Die erste Zeile wird VOR dem Spaltenbau geholt: Fuer Ausdrucksspalten
            // (COUNT(*), MAX(ID), Aliasse) gibt es keinen deklarierten Typ - dort
            // entscheidet der tatsaechliche Wert.
            bool hatZeile = leser.Read();

            string[] namen = new string[spaltenzahl];
            Spaltenart[] arten = new Spaltenart[spaltenzahl];
            HashSet<string> belegt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < spaltenzahl; i++)
            {
                // --- Name entdoppeln (ID, ID1, ID2 ... wie der alte OleDbDataAdapter) ---
                string roh = leser.GetName(i);
                if (string.IsNullOrEmpty(roh)) roh = "Column" + (i + 1);
                string name = roh;
                int lauf = 1;
                while (!belegt.Add(name)) { name = roh + lauf.ToString(CultureInfo.InvariantCulture); lauf++; }
                namen[i] = name;

                // --- Grundtyp: Deklaration zuerst, sonst der beobachtete Wert ---
                Type grundtyp = null;
                try { grundtyp = TypAusDeklaration(leser.GetDataTypeName(i)); }
                catch (Exception) { grundtyp = null; }

                if (grundtyp == null && hatZeile)
                {
                    try { if (!leser.IsDBNull(i)) grundtyp = leser.GetFieldType(i); }
                    catch (Exception) { grundtyp = null; }
                }
                if (grundtyp == null) grundtyp = typeof(string);

                // --- D9-Regeln ---
                Type zieltyp;
                if (grundtyp == typeof(long) && SchemaTypKatalog.BoolSpalten.Contains(roh))
                {
                    arten[i] = Spaltenart.Wahrheitswert;
                    zieltyp = typeof(bool);
                }
                else if (grundtyp == typeof(string) && SchemaTypKatalog.DatumSpalten.Contains(roh))
                {
                    arten[i] = Spaltenart.Zeitpunkt;
                    zieltyp = typeof(DateTime);
                }
                else if (grundtyp == typeof(long))
                {
                    // Ueberlauf ueber Int32 wirft und laeuft in den bestehenden Fehlerpfad.
                    arten[i] = Spaltenart.GanzzahlInt32;
                    zieltyp = typeof(int);
                }
                else
                {
                    arten[i] = Spaltenart.Unveraendert;
                    zieltyp = grundtyp;
                }

                tabelle.Columns.Add(new DataColumn(name, zieltyp));
            }

            // --- Werte ---
            tabelle.BeginLoadData();
            try
            {
                while (hatZeile)
                {
                    object[] werte = new object[spaltenzahl];
                    for (int i = 0; i < spaltenzahl; i++)
                    {
                        if (leser.IsDBNull(i)) { werte[i] = DBNull.Value; continue; }  // DBNull immer durchreichen

                        object roh = leser.GetValue(i);
                        switch (arten[i])
                        {
                            case Spaltenart.GanzzahlInt32:
                                werte[i] = Convert.ToInt32(roh, CultureInfo.InvariantCulture);
                                break;
                            case Spaltenart.Wahrheitswert:
                                werte[i] = Convert.ToInt64(roh, CultureInfo.InvariantCulture) != 0L;
                                break;
                            case Spaltenart.Zeitpunkt:
                                werte[i] = DateTime.ParseExact(Convert.ToString(roh, CultureInfo.InvariantCulture),
                                                               DATUM_FORMATE, CultureInfo.InvariantCulture,
                                                               DateTimeStyles.None);
                                break;
                            default:
                                werte[i] = roh;
                                break;
                        }
                    }
                    tabelle.Rows.Add(werte);
                    hatZeile = leser.Read();
                }
            }
            finally
            {
                tabelle.EndLoadData();
            }

            tabelle.AcceptChanges();
            return tabelle;
        }


        // =================================================================================
        // Die sechs Zugriffsmethoden (Konzept 2.4 / 2.5)
        // =================================================================================

        // Für SELECT-Abfragen: Liefert Daten in den Arbeitsspeicher
        public DataTable GetDataTable(string sql, params DbParam[] parameters)
        {
            try
            {
                using (SqliteConnection conn = OeffneVerbindung())
                using (SqliteCommand cmd = ErzeugeKommando(conn, null, sql, parameters))
                using (SqliteDataReader leser = cmd.ExecuteReader())
                {
                    return LadeTabelle(leser);
                }
            }
            catch (Exception ex)
            {
                DataRepository.FehlerMelden("Fehler beim Laden der Daten: " + ex.Message +
                                   DataRepository.KurzSql(sql));
                return new DataTable();
            }
        }

        // Für INSERT, UPDATE, DELETE
        public bool ExecuteSQL(string sql, params DbParam[] parameters)
        {
            try
            {
                using (SqliteConnection conn = OeffneVerbindung())
                using (SqliteCommand cmd = ErzeugeKommando(conn, null, sql, parameters))
                {
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                DataRepository.FehlerMelden("Datenbankfehler: " + ex.Message + "\n\nAnweisung: " + Kurz(sql));
                return false;
            }
        }

        // Für INSERT, UPDATE, DELETE – gibt die Anzahl der betroffenen Zeilen zurück
        public int ExecuteNonQuery(string sql, params DbParam[] parameters)
        {
            try
            {
                using (SqliteConnection conn = OeffneVerbindung())
                using (SqliteCommand cmd = ErzeugeKommando(conn, null, sql, parameters))
                {
                    // ExecuteNonQuery liefert die Anzahl der betroffenen Datensätze (int)
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                DataRepository.FehlerMelden("Datenbankfehler (NonQuery): " + ex.Message + "\n\nAnweisung: " + Kurz(sql));
                // Wir geben -1 zurück, um einen Fehler von "0 betroffenen Zeilen" zu unterscheiden
                return -1;
            }
        }

        // Signatur bewusst OHNE params - 7 Aufrufstellen verlassen sich darauf.
        public int ExecuteInsertAndGetId(string insertSql, DbParam[] parameters)
        {
            try
            {
                using (SqliteConnection conn = OeffneVerbindung())
                {
                    using (SqliteCommand cmd = ErzeugeKommando(conn, null, insertSql, parameters))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Holt die ID des gerade erzeugten Datensatzes auf DIESER Verbindung
                    // (frueher SELECT @@IDENTITY).
                    using (SqliteCommand cmdIdentity = conn.CreateCommand())
                    {
                        cmdIdentity.CommandText = "SELECT last_insert_rowid()";
                        return Convert.ToInt32(cmdIdentity.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                DataRepository.FehlerMelden("Datenbankfehler (NonQuery): " + ex.Message + "\n\nAnweisung: " + Kurz(insertSql));
                // Wir geben -1 zurück, um einen Fehler von "0 betroffenen Zeilen" zu unterscheiden
                return 0;
            }
        }

        public object ExecuteScalar(string sql, params DbParam[] parameters)
        {
            try
            {
                using (SqliteConnection conn = OeffneVerbindung())
                using (SqliteCommand cmd = ErzeugeKommando(conn, null, sql, parameters))
                {
                    object result = cmd.ExecuteScalar();

                    // Falls das Ergebnis DBNull ist, geben wir null zurück
                    if (result == DBNull.Value) return null;

                    return result;
                }
            }
            catch (Exception ex)
            {
                DataRepository.FehlerMelden("Datenbankfehler (Scalar): " + ex.Message +
                                   DataRepository.KurzSql(sql));
                return null;
            }
        }


        // =================================================================================
        // Transaktionen (Konzept 2.6)
        // =================================================================================

        /// <summary>
        /// Oeffnet einen Datenbankvorgang (Verbindung + Transaktion) fuer einen
        /// <c>using</c>-Block. Ohne <c>Commit()</c> wird beim Verlassen zurueckgerollt.
        ///
        /// EINZIGER WEG IN EINE TRANSAKTION. Bis Arbeitspaket S4e gab es daneben ein
        /// <c>BeginTransaction()</c>, das ein <c>(OleDbConnection, OleDbTransaction)</c>-Tupel
        /// an 18 Dateien herausgab; seit S4e ist es ersatzlos geloescht - Verbindung und
        /// Transaktion verlassen die Zugriffsschicht nicht mehr.
        /// </summary>
        public DbVorgang Vorgang()
        {
            return new DbVorgang(OeffneVerbindung());
        }


        // =================================================================================
        // Schema-Auskunft - Ersatz fuer GetOleDbSchemaTable (Konzept 2.7, Befund N2)
        // =================================================================================
        //
        // Der Tabellenname wird DURCHGAENGIG als Parameter an die table-valued Form der
        // PRAGMAs uebergeben (pragma_table_info(?) statt "PRAGMA table_info(name)") -
        // so entsteht keine Injektionsflaeche durch zusammengesetztes SQL.

        /// <summary>Gibt es eine Tabelle (oder Sicht) dieses Namens?</summary>
        public bool TabelleVorhanden(string name)
        {
            object treffer = ExecuteScalar(
                "SELECT COUNT(*) FROM sqlite_master WHERE type IN ('table','view') AND name = ?",
                new DbParam("?", name ?? string.Empty));
            return treffer != null && Convert.ToInt32(treffer) > 0;
        }

        /// <summary>Gibt es diese Spalte in dieser Tabelle? (Namensvergleich ohne Gross-/Kleinschreibung)</summary>
        public bool SpalteVorhanden(string tabelle, string spalte)
        {
            if (string.IsNullOrWhiteSpace(spalte)) return false;
            foreach (string vorhanden in SpaltenVonTabelle(tabelle))
                if (string.Equals(vorhanden, spalte, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Spaltennamen einer Tabelle in Schemareihenfolge (leer, wenn es sie nicht gibt).</summary>
        public List<string> SpaltenVonTabelle(string tabelle)
        {
            List<string> spalten = new List<string>();
            DataTable dt = GetDataTable(
                "SELECT name FROM pragma_table_info(?) ORDER BY cid",
                new DbParam("?", tabelle ?? string.Empty));
            foreach (DataRow zeile in dt.Rows)
            {
                if (zeile["name"] == DBNull.Value) continue;
                spalten.Add(Convert.ToString(zeile["name"]));
            }
            return spalten;
        }

        /// <summary>
        /// Indizes einer Tabelle, eine Zeile je Index-Spalte
        /// (Indexname, Eindeutig, Position, Spaltenname) - deckt das frühere
        /// <c>Indexes</c>-Rowset ab.
        /// </summary>
        public DataTable IndexListe(string tabelle)
        {
            return GetDataTable(
                "SELECT il.name AS Indexname, il.[unique] AS Eindeutig, " +
                "       ii.seqno AS Position, ii.name AS Spaltenname " +
                "FROM pragma_index_list(?) AS il " +
                "LEFT JOIN pragma_index_info(il.name) AS ii " +
                "ORDER BY il.seq, ii.seqno",
                new DbParam("?", tabelle ?? string.Empty));
        }

        /// <summary>
        /// Fremdschlüssel einer Tabelle - deckt das frühere <c>Foreign_Keys</c>-Rowset ab.
        /// </summary>
        public DataTable FremdschluesselListe(string tabelle)
        {
            return GetDataTable(
                "SELECT id AS Nummer, seq AS Position, [table] AS Zieltabelle, " +
                "       [from] AS Quellspalte, [to] AS Zielspalte, " +
                "       on_update AS BeiAenderung, on_delete AS BeiLoeschung " +
                "FROM pragma_foreign_key_list(?) " +
                "ORDER BY id, seq",
                new DbParam("?", tabelle ?? string.Empty));
        }
    
        /// <summary>Die Anweisung fuer die Fehlermeldung, auf 300 Zeichen gekuerzt - damit
        /// der Anwender melden kann, WELCHE Anweisung scheiterte (Befund 03.09.2026).</summary>
        private static string Kurz(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return "";
            string s = sql.Replace("\r", " ").Replace("\n", " ");
            return s.Length > 300 ? s.Substring(0, 300) + " …" : s;
        }
}
}
