using System.IO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1.Allgemein;

namespace WindowsFormsApplication1
{
    // =====================================================================================
    // ARBEITSPAKET S4a (Implementierungskonzept DB-Migration SQLite, Abschnitt 2):
    // Die Zugriffsschicht spricht ab hier Microsoft.Data.Sqlite. Uebersetzt wird INNEN -
    // die oeffentlichen Signaturen bleiben, und der reine DATENTRAEGER an den ~2.300
    // Aufrufstellen im Bestand bleibt es auch. OleDb band rein nach POSITION; die
    // Parameternamen im Bestand sind beliebig ("?", "@p", "@id"). Der Uebersetzer wertet
    // die Namen deshalb NICHT aus, sondern nummeriert strikt nach Reihenfolge
    // (? -> @p0 ... @pN, Parameterarray -> @p0 ... @pN).
    //
    // NICHT geaendert wurden: Engine-Modus, FehlerMelden, StilleFehlerAbholen, KurzSql,
    // saemtliche Fehlermeldungs-Wortlaute und die Rueckgabewerte im Fehlerfall.
    //
    // ARBEITSPAKET iU6 (Umsetzungskonzept iOS 1.4, iF10): Der Datentraeger ist nicht mehr
    // OleDbParameter, sondern DbParam - "new OleDbParameter(...)" wirft auf Linux/macOS
    // schon im Konstruktor (Entscheidungsregister 2.2, Messung B). Der Uebersetzer
    // arbeitet unveraendert: dieselbe Positionsnummerierung, dieselbe Normalisierung.
    // Altaufrufe mit OleDbParameter kompilieren ueber die Bruecke in DbParam weiter.
    // =====================================================================================

    public static class DataRepository
    {
        // Dateiname der SQLite-Datenbank (liegt im konfigurierten Datenbank-Ordner).
        // Bis zur Umstellung war das "Kenndaten.accdb"; siehe N7-Vorgriff in GetDBPath().
        private const string DB_DATEINAME = "Kenndaten.sqlite";

        // =================================================================================
        // Engine-Modus: Datenbankfehler ohne MessageBox (Paket 8, Konzept 13.4)
        // =================================================================================
        //
        // AUSGANGSLAGE. Jede Zugriffsmethode dieser Klasse meldete einen Datenbankfehler
        // selbst per MessageBox. Weil die Simulationsklassen durchgängig hierüber
        // zugreifen, blockierte ein einziger DB-Fehler jeden unbeaufsichtigten Lauf bis
        // zum Timeout - Konzept 13.4 nennt das ausdrücklich als Teil des Pakets
        // ("DataRepository-Fehlerpfad ohne MessageBox im Engine-Kontext"). Die
        // Umgehung war bisher StilleDb, die aber nur für NEUEN Code galt; der Altbestand
        // im Rechenpfad blieb bei DataRepository.
        //
        // LÖSUNG. Ein zählender Schalter, den ausschließlich die Einstiegspunkte eines
        // Simulationslaufs setzen. Das sind (berichtigt in der Nacharbeit, Befund N14c):
        //   - SimulationRunner.Simuliere              - der ganze headless-Lauf
        //   - SimulationRunner.SimuliereUndSpeichere  - zusätzlich Ergebnisaufbau + Save
        //   - SimulationControl.Do_Simulation         - innere Absicherung, falls die
        //                                               Engine ohne Runner gerufen wird
        // Form_Simulation_Detail setzt den Modus NICHT: Der Lauf aus der Detailansicht
        // läuft auf dem UI-Thread, dort ist ein Dialog die richtige Meldung, und die
        // innere Absicherung in Do_Simulation deckt den Rechenteil trotzdem ab.
        // Solange der Schalter steht, wandert der Fehlertext in eine Sammelliste und auf
        // die Konsole statt in einen Dialog; nach dem Lauf holt der Aufrufer ihn ab und
        // legt ihn in den Protokollkanal.
        //
        // FÜR ALLE ÜBRIGEN AUFRUFER ÄNDERT SICH NICHTS: Ohne gesetzten Schalter - also
        // in jeder Bedienung der Oberfläche - kommt die MessageBox wie bisher, mit
        // demselben Wortlaut.
        //
        // Der Schalter ZÄHLT (statt bool), weil sich die Bereiche schachteln:
        // SimulationRunner.Simuliere setzt ihn um den ganzen Lauf, SimulationControl noch
        // einmal um Do_Simulation.
        //
        // PROZESSWEIT, NICHT THREADGEBUNDEN (Nacharbeit, Befund N7). _stillTiefe und
        // _stilleFehler gelten für den ganzen Prozess. Das trägt nur, solange HÖCHSTENS
        // EIN Simulationslauf gleichzeitig läuft - und die Anwendung ist dafür NICHT
        // einläufig: Der Berichtspfad rechnet in Task.Run auf einem ThreadPool-Thread
        // (BerichtsDatenSammler.Sammle, gerufen aus Form_Bericht,
        // Form_Wirtschaftlichkeit, Form_WirtschaftlichkeitVerlauf). Getragen wird die
        // Annahme von der MODALITÄT dieser drei Formulare: Alle drei werden ausschließlich
        // über ShowDialog() geöffnet, der MDI-Thread kann währenddessen keinen zweiten
        // Lauf starten. Wer eines davon je nicht-modal öffnet, bricht diese Invariante -
        // dann gehören Schalter und Sammelliste threadgebunden (dieselbe Vormerkung wie
        // in SimulationProtokoll).

        private static readonly object _stillSperre = new object();
        private static int _stillTiefe;
        private static readonly List<string> _stilleFehler = new List<string>();

        /// <summary>Höchstzahl gesammelter Meldungen je Lauf - gegen Meldungsfluten in Schleifen.</summary>
        private const int MAX_STILLE_FEHLER = 50;

        /// <summary>true, solange ein Simulationslauf den dialogfreien Modus hält.</summary>
        public static bool EngineModusAktiv
        {
            get { lock (_stillSperre) { return _stillTiefe > 0; } }
        }

        /// <summary>
        /// Öffnet den dialogfreien Modus für die Dauer eines <c>using</c>-Blocks.
        /// Verschachtelung ist zulässig; erst der äußerste Block gibt ihn wieder frei.
        /// </summary>
        public static IDisposable EngineModus()
        {
            return new EngineModusBereich();
        }

        /// <summary>
        /// Liefert die im dialogfreien Modus aufgelaufenen Meldungen und leert die
        /// Sammlung. Aufzurufen unmittelbar nach dem Lauf.
        /// </summary>
        public static string[] StilleFehlerAbholen()
        {
            lock (_stillSperre)
            {
                string[] kopie = _stilleFehler.ToArray();
                _stilleFehler.Clear();
                return kopie;
            }
        }

        private sealed class EngineModusBereich : IDisposable
        {
            private bool _offen = true;

            public EngineModusBereich()
            {
                lock (_stillSperre)
                {
                    // Der ÄUSSERSTE Bereich beginnt mit leerer Sammlung; ein
                    // verschachtelter erbt die des äußeren.
                    if (_stillTiefe == 0) _stilleFehler.Clear();
                    _stillTiefe++;
                }
            }

            public void Dispose()
            {
                if (!_offen) return;
                _offen = false;
                lock (_stillSperre) { if (_stillTiefe > 0) _stillTiefe--; }
            }
        }

        /// <summary>
        /// Meldet einen Datenbankfehler - als Dialog (Bedienung) oder als Protokolleintrag
        /// (Engine-Modus). EINE Entscheidungsstelle für alle sechs Fehlerpfade dieser
        /// Klasse UND für die datenbanknahen Meldungen der Controller, die aus dem
        /// Rechenpfad heraus erreichbar sind (<c>ErgebnisCtrl.Save</c> beim Speichern des
        /// Laufergebnisses, <c>PufferSpCtrl.CopyFromStamm</c>).
        ///
        /// Öffentlich, damit es bei EINER Entscheidung bleibt: Jede zweite Fassung des
        /// „Dialog oder Protokoll"-Musters wäre die Stelle, an der der nächste
        /// headless-Lauf wieder hängen bleibt.
        /// </summary>
        public static void FehlerMelden(string meldung)
        {
            bool still;
            lock (_stillSperre)
            {
                still = _stillTiefe > 0;
                if (still)
                {
                    // NACHARBEIT PAKET 8, BEFUND N12a: Beim Überlauf EINMAL sagen, dass
                    // gekappt wurde. Sonst liest sich eine abgeschnittene Liste wie eine
                    // vollständige - und gerade bei einer Meldungsflut ist die Zahl der
                    // Fehler die eigentliche Information.
                    if (_stilleFehler.Count < MAX_STILLE_FEHLER) _stilleFehler.Add(meldung);
                    else if (_stilleFehler.Count == MAX_STILLE_FEHLER)
                        _stilleFehler.Add("… weitere Meldungen unterdrückt (Grenze " +
                                          MAX_STILLE_FEHLER + " je Abholung).");
                }
            }

            if (!still)
            {
                MessageBox.Show(meldung);
                return;
            }

            try { Console.WriteLine("Datenbankfehler im Simulationslauf (ohne Dialog): " + meldung); }
            catch { }
        }


        // =================================================================================
        // Verbindung (Konzept 2.1)
        // =================================================================================

        // Zentraler Ort für den Pfad - einfach anzupassen
        public static string GetConnectionString()
        {
            // UEBERGANGSZUSTAND BIS S4b: 36 Dateien im Bestand bauen aus diesem String noch
            // eine EIGENE OleDbConnection. Das KOMPILIERT weiterhin (der Konstruktor nimmt
            // einen beliebigen string), traegt zur Laufzeit aber nicht mehr - genau diese
            // Eigenverbindungen fuehrt S4b auf die Zugriffsschicht zurueck. Bis dahin
            // liefert die Methode bewusst schon den SQLite-Verbindungsstring, damit es
            // ab S4b nur noch EINE Wahrheit gibt.
            return $"Data Source={GetDBPath()};Foreign Keys=True";
        }

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
            SqliteConnection verbindung = new SqliteConnection($"Data Source={GetDBPath()};Foreign Keys=True");
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
        public static bool DatenbankVorhanden()
        {
            try
            {
                string pfad = GetDBPath();
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
        public static DataTable GetDataTable(string sql, params DbParam[] parameters)
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
                FehlerMelden("Fehler beim Laden der Daten: " + ex.Message + KurzSql(sql));
                return new DataTable();
            }
        }

        /// <summary>
        /// Diagnosezusatz der Fehlermeldungen (26.08.2026): Die Box „Für mindestens
        /// einen erforderlichen Parameter …“ nennt ohne die Abfrage weder Ort noch
        /// Ursache — der Anfang des SQL macht jede Meldung selbstverortend.
        /// </summary>
        private static string KurzSql(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return "";
            string s = sql.Replace("\r", " ").Replace("\n", " ").Trim();
            while (s.IndexOf("  ", StringComparison.Ordinal) >= 0) s = s.Replace("  ", " ");
            if (s.Length > 160) s = s.Substring(0, 160) + "…";
            return Environment.NewLine + Environment.NewLine + "Abfrage: " + s;
        }

        // Für INSERT, UPDATE, DELETE
        public static bool ExecuteSQL(string sql, params DbParam[] parameters)
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
                FehlerMelden("Datenbankfehler: " + ex.Message);
                return false;
            }
        }

        // Für INSERT, UPDATE, DELETE – gibt die Anzahl der betroffenen Zeilen zurück
        public static int ExecuteNonQuery(string sql, params DbParam[] parameters)
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
                FehlerMelden("Datenbankfehler (NonQuery): " + ex.Message);
                // Wir geben -1 zurück, um einen Fehler von "0 betroffenen Zeilen" zu unterscheiden
                return -1;
            }
        }

        // Signatur bewusst OHNE params - 7 Aufrufstellen verlassen sich darauf.
        public static int ExecuteInsertAndGetId(string insertSql, DbParam[] parameters)
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
                FehlerMelden("Datenbankfehler (NonQuery): " + ex.Message);
                // Wir geben -1 zurück, um einen Fehler von "0 betroffenen Zeilen" zu unterscheiden
                return 0;
            }
        }

        public static object ExecuteScalar(string sql, params DbParam[] parameters)
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
                FehlerMelden("Datenbankfehler (Scalar): " + ex.Message + KurzSql(sql));
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
        public static DbVorgang Vorgang()
        {
            return new DbVorgang(OeffneVerbindung());
        }


        // =================================================================================
        // Pfad und Startprüfung
        // =================================================================================

        /// <summary>
        /// Uebersteuert den Datenbankpfad vollstaendig, wenn gesetzt (null = normaler
        /// Weg ueber die Einstellungen).
        ///
        /// HAKEN FUER PROBEN UND REFERENZLAUF-SUITE: Damit laesst sich die Zugriffsschicht
        /// auf eine KOPIE der Datenbank richten, ohne die Einstellungen des Anwenders
        /// anzufassen. Im Programmbetrieb bleibt die Eigenschaft unbesetzt.
        /// </summary>
        public static string PfadUeberschreibung { get; set; }

        // Vollstaendiger Pfad zur SQLite-Datenbank.
        // Ordner: konfigurierter Standard-Datenbankpfad aus den Einstellungen (Form_AdminSettings),
        // sonst Fallback %ProgramData%\EPOS_PLAN. Darunter liegt die Datei DB_DATEINAME.
        public static string GetDBPath()
        {
            // Proben-/Referenzlauf-Haken: schlaegt alles andere.
            if (!string.IsNullOrWhiteSpace(PfadUeberschreibung)) return PfadUeberschreibung;

            string ordner = Properties.Settings.Default.DBPath;
            if (string.IsNullOrWhiteSpace(ordner))
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                ordner = Path.Combine(programData, "EPOS_PLAN");
            }

            string datei = Properties.Settings.Default.DBName;

            // VORGRIFF AUF N7 (endgueltiger Settings-Fixup ist Arbeitspaket S8):
            // In den gespeicherten Einstellungen steht auf jedem Bestandsrechner noch
            // "Kenndaten.accdb". Solange der Fixup nicht gelaufen ist, zeigt der
            // konfigurierte Name auf eine Access-Datei, die es nach der Migration nicht
            // mehr gibt - deshalb wird ein .accdb-Name hier auf die SQLite-Datei
            // umgebogen, statt den Start an einem Altwert scheitern zu lassen.
            if (string.IsNullOrWhiteSpace(datei) ||
                datei.EndsWith(".accdb", StringComparison.OrdinalIgnoreCase))
            {
                datei = DB_DATEINAME;
            }

            return Path.Combine(ordner, datei);
        }


        // =================================================================================
        // Schema-Auskunft - Ersatz fuer GetOleDbSchemaTable (Konzept 2.7, Befund N2)
        // =================================================================================
        //
        // Der Tabellenname wird DURCHGAENGIG als Parameter an die table-valued Form der
        // PRAGMAs uebergeben (pragma_table_info(?) statt "PRAGMA table_info(name)") -
        // so entsteht keine Injektionsflaeche durch zusammengesetztes SQL.

        /// <summary>Gibt es eine Tabelle (oder Sicht) dieses Namens?</summary>
        public static bool TabelleVorhanden(string name)
        {
            object treffer = ExecuteScalar(
                "SELECT COUNT(*) FROM sqlite_master WHERE type IN ('table','view') AND name = ?",
                new DbParam("?", name ?? string.Empty));
            return treffer != null && Convert.ToInt32(treffer) > 0;
        }

        /// <summary>Gibt es diese Spalte in dieser Tabelle? (Namensvergleich ohne Gross-/Kleinschreibung)</summary>
        public static bool SpalteVorhanden(string tabelle, string spalte)
        {
            if (string.IsNullOrWhiteSpace(spalte)) return false;
            foreach (string vorhanden in SpaltenVonTabelle(tabelle))
                if (string.Equals(vorhanden, spalte, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Spaltennamen einer Tabelle in Schemareihenfolge (leer, wenn es sie nicht gibt).</summary>
        public static List<string> SpaltenVonTabelle(string tabelle)
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
        public static DataTable IndexListe(string tabelle)
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
        public static DataTable FremdschluesselListe(string tabelle)
        {
            return GetDataTable(
                "SELECT id AS Nummer, seq AS Position, [table] AS Zieltabelle, " +
                "       [from] AS Quellspalte, [to] AS Zielspalte, " +
                "       on_update AS BeiAenderung, on_delete AS BeiLoeschung " +
                "FROM pragma_foreign_key_list(?) " +
                "ORDER BY id, seq",
                new DbParam("?", tabelle ?? string.Empty));
        }


        // =================================================================================
        // Bequemlichkeiten (unveraendert im Verhalten)
        // =================================================================================

        public static int GetMaxID(string tableName, string fieldName = "ID")
        {
            // Wir nutzen string.Format, da Tabellen- und Spaltennamen nicht als ? Parameter übergeben werden können
            string sql = string.Format("SELECT MAX({0}) FROM {1}", fieldName, tableName);

            DataTable dt = GetDataTable(sql);

            if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
            {
                return Convert.ToInt32(dt.Rows[0][0]);
            }

            return 0;
        }

        public static bool DeleteWithDependencies(string masterTable, string detailTable, string detailForeignKey, int masterId)
        {
            using (DbVorgang vorgang = Vorgang())
            {
                try
                {
                    // 1. Details löschen (z.B. project_settings)
                    vorgang.Ausfuehren($"DELETE FROM {detailTable} WHERE {detailForeignKey} = ?",
                                       new DbParam("?", masterId));

                    // 2. Master löschen (z.B. energy_carrier)
                    vorgang.Ausfuehren($"DELETE FROM {masterTable} WHERE ID = ?",
                                       new DbParam("?", masterId));

                    vorgang.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    // Ein bereits von SQLite selbst zurueckgerollter Vorgang darf die
                    // eigentliche Meldung nicht verdraengen.
                    try { vorgang.Rollback(); } catch (Exception) { }
                    FehlerMelden($"Fehler beim Löschen in {masterTable}: " + ex.Message);
                    return false;
                }
            }
        }

        public static int GetIdByName(string tableName, string nameField, string nameValue)
        {
            string sql = $"SELECT ID FROM {tableName} WHERE {nameField} = ?";
            object result = ExecuteScalar(sql, new DbParam("?", nameValue));
            return result != null ? Convert.ToInt32(result) : -1;
        }

        public static object GetValueById(string tableName, string nameField, int id)
        {
            string sql = $"SELECT {nameField} FROM {tableName} WHERE id = ?";
            object result = ExecuteScalar(sql, new DbParam("?", id));
            return result;
        }
    }
}
