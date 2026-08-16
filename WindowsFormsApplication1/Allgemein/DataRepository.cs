using System.IO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public static class DataRepository
    {
        // Dateiname der Access-Datenbank (liegt im konfigurierten Datenbank-Ordner).
        private const string DB_DATEINAME = "Kenndaten.accdb";

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
        /// Laufergebnisses, <c>PufferSpCtrl.CopyFromStamm</c>,
        /// <c>Z_ProjektPufferSpCtrl.Insert</c>).
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


        // Zentraler Ort für den Pfad - einfach anzupassen
        public static string GetConnectionString()
        {
            // Beispiel: Datenbank liegt im Programmordner
            string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={GetDBPath()};";
            return connString;
        }

        // Für SELECT-Abfragen: Liefert Daten in den Arbeitsspeicher
        public static DataTable GetDataTable(string sql, params OleDbParameter[] parameters)
        {
            using (OleDbConnection conn = new OleDbConnection(GetConnectionString()))
            {
                try
                {
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        if (parameters != null) cmd.Parameters.AddRange(parameters);

                        DataTable dt = new DataTable();
                        using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                        {
                            adapter.Fill(dt); // Öffnet/schließt Verbindung automatisch
                        }
                        return dt;
                    }
                }
                catch (Exception ex)
                {
                    FehlerMelden("Fehler beim Laden der Daten: " + ex.Message);
                    return new DataTable();
                }
            }
        }

        // Für INSERT, UPDATE, DELETE
        public static bool ExecuteSQL(string sql, params OleDbParameter[] parameters)
        {
            using (OleDbConnection conn = new OleDbConnection(GetConnectionString()))
            {
                try
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        if (parameters != null) cmd.Parameters.AddRange(parameters);
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
        }

        // Für INSERT, UPDATE, DELETE – gibt die Anzahl der betroffenen Zeilen zurück
        public static int ExecuteNonQuery(string sql, params OleDbParameter[] parameters)
        {
            using (OleDbConnection conn = new OleDbConnection(GetConnectionString()))
            {
                try
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        if (parameters != null && parameters.Length > 0)
                        {
                            cmd.Parameters.AddRange(parameters);
                        }

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
        }

        public static int ExecuteInsertAndGetId(string insertSql, OleDbParameter[] parameters)
        {
            // Nutzen Sie hier Ihren bestehenden Verbindungsstring
            using (var conn = new OleDbConnection(GetConnectionString()))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand(insertSql, conn))
                    {
                        if (parameters != null) cmd.Parameters.AddRange(parameters);
                        cmd.ExecuteNonQuery();
                    }

                    // Holt die ID des gerade erzeugten Datensatzes auf dieser Verbindung
                    using (var cmdIdentity = new OleDbCommand("SELECT @@IDENTITY", conn))
                    {
                        return Convert.ToInt32(cmdIdentity.ExecuteScalar());
                    }
                }
                catch (Exception ex)
                {
                    FehlerMelden("Datenbankfehler (NonQuery): " + ex.Message);
                    // Wir geben -1 zurück, um einen Fehler von "0 betroffenen Zeilen" zu unterscheiden
                    return 0;
                }
            }
        }

        public static object ExecuteScalar(string sql, params OleDbParameter[] parameters)
        {
            using (OleDbConnection conn = new OleDbConnection(GetConnectionString()))
            {
                try
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        // Parameter hinzufügen, falls vorhanden
                        if (parameters != null && parameters.Length > 0)
                        {
                            cmd.Parameters.AddRange(parameters);
                        }

                        object result = cmd.ExecuteScalar();

                        // Falls das Ergebnis DBNull ist, geben wir null zurück
                        if (result == DBNull.Value) return null;

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    FehlerMelden("Datenbankfehler (Scalar): " + ex.Message);
                    return null;
                }
            }
        }

        // Hilfsmethode für Transaktionen (Master-Detail)
        public static (OleDbConnection, OleDbTransaction) BeginTransaction()
        {
            OleDbConnection conn = new OleDbConnection(GetConnectionString());
            conn.Open();
            OleDbTransaction trans = conn.BeginTransaction();
            return (conn, trans);
        }

        // Vollstaendiger Pfad zur Access-Datenbank.
        // Ordner: konfigurierter Standard-Datenbankpfad aus den Einstellungen (Form_AdminSettings),
        // sonst Fallback %ProgramData%\EPOS_PLAN. Darunter liegt die Datei DB_DATEINAME.
        public static string GetDBPath()
        {
            string ordner = Properties.Settings.Default.DBPath;
            if (string.IsNullOrWhiteSpace(ordner))
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                ordner = Path.Combine(programData, "EPOS_PLAN");
            }
            return Path.Combine(ordner, Properties.Settings.Default.DBName); // DB_DATEINAME);
        }

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
            var (conn, trans) = BeginTransaction();
            try
            {
                // 1. Details löschen (z.B. project_settings)
                string sqlDetail = $"DELETE FROM {detailTable} WHERE {detailForeignKey} = ?";
                using (OleDbCommand cmd = new OleDbCommand(sqlDetail, conn, trans))
                {
                    cmd.Parameters.AddWithValue("?", masterId);
                    cmd.ExecuteNonQuery();
                }

                // 2. Master löschen (z.B. energy_carrier)
                string sqlMaster = $"DELETE FROM {masterTable} WHERE ID = ?";
                using (OleDbCommand cmd = new OleDbCommand(sqlMaster, conn, trans))
                {
                    cmd.Parameters.AddWithValue("?", masterId);
                    cmd.ExecuteNonQuery();
                }

                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                FehlerMelden($"Fehler beim Löschen in {masterTable}: " + ex.Message);
                return false;
            }
            finally { conn.Close(); }
        }

        public static int GetIdByName(string tableName, string nameField, string nameValue)
        {
            string sql = $"SELECT ID FROM {tableName} WHERE {nameField} = ?";
            object result = ExecuteScalar(sql, new OleDbParameter("?", nameValue));
            return result != null ? Convert.ToInt32(result) : -1;
        }

        public static object GetValueById(string tableName, string nameField, int id)
        {
            string sql = $"SELECT {nameField} FROM {tableName} WHERE id = ?";
            object result = ExecuteScalar(sql, new OleDbParameter("?", id));
            return result;
        }
    }
}
