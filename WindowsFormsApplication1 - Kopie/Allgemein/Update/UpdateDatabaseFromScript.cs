using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    internal class UpdateDatabaseFromScript
    {
        // Pfad zur Logdatei (im Ordner der .exe)
        private string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_update_log.txt");

        private List<RelationInfo> gesicherteBeziehungen = new List<RelationInfo>();

        private string GetDBPath()
        {
            string db = "";
            string userPath = $@"SOFTWARE\ODBC\ODBC.INI\TEST";

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(userPath))
            {
                if (key != null)
                {
                    db = key.GetValue("DBQ")?.ToString() ?? key.GetValue("Database")?.ToString();
                }
            }
            return db;
        }

        public void UpdateDatabase(string scriptPath)
        {
            string dbPath = GetDBPath();
            string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};";

            AnalysiereAbhaengigkeiten();

            // --- START-LOG ---
            LogResult("========================================", "");
            LogResult("START", $"Datenbank-Update Prozess gestartet.");
            LogResult("DATEI", scriptPath);
            LogResult("DB-PFAD", dbPath);
            LogResult("----------------------------------------", "");

            if (!File.Exists(scriptPath))
            {
                LogResult("FEHLER", $"Skriptdatei nicht gefunden: {scriptPath}");
                return;
            }

   

            try
            {
                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    conn.Open();
                    string[] lines = File.ReadAllLines(scriptPath);

                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//")) continue;

                        // --- A: SPEZIALBEFEHLE (OHNE 'SQL=') ---
                        if (trimmedLine.StartsWith("BACKUP_REL:", StringComparison.OrdinalIgnoreCase))
                        {
                            string col = trimmedLine.Substring("BACKUP_REL:".Length).Trim();
                            SichertBeziehungen(conn, "Tab_ProjektWerte", col);
                        }
                        else if (trimmedLine.StartsWith("CLEAN_COL:", StringComparison.OrdinalIgnoreCase))
                        {
                            string col = trimmedLine.Substring("CLEAN_COL:".Length).Trim();
                            LogResult("SYSTEM", $"Starte Bereinigung für Spalte: {col}");
                            DropAllRelationsOnColumn(conn, "Tab_ProjektWerte", col);
                            DropAllIndexesOnColumn(conn, "Tab_ProjektWerte", col);
                        }
                        else if (trimmedLine.StartsWith("RESTORE_REL:", StringComparison.OrdinalIgnoreCase))
                        {
                            string col = trimmedLine.Substring("RESTORE_REL:".Length).Trim();
                            WiederherstellenBeziehungen(conn, col);
                        }

                        // --- B: ECHTE SQL-BEFEHLE (MIT 'SQL=') ---
                        else if (trimmedLine.StartsWith("SQL=", StringComparison.OrdinalIgnoreCase))
                        {
                            string sqlCommand = trimmedLine.Substring(4).Trim(); // Schneidet 'SQL=' ab
                            try
                            {
                                using (OleDbCommand cmd = new OleDbCommand(sqlCommand, conn))
                                {
                                    cmd.ExecuteNonQuery();
                                    LogResult("ERFOLG", sqlCommand);
                                }
                            }
                            catch (OleDbException ex)
                            {
                                LogResult("ÜBERSPRUNGEN", $"{sqlCommand} -> {ex.Message}");
                            }
                        }
                    }
                }
                // --- ENDE-LOG (ERFOLG) ---
                LogResult("ENDE", "Update-Prozess erfolgreich abgeschlossen.");
                LogResult("========================================", "");
                MessageBox.Show("Datenbank-Update erfolgreich.");
            }
            catch (Exception ex)
            {
                // --- ENDE-LOG (ABBRUCH) ---
                LogResult("ABBRUCH", $"Kritischer Fehler: {ex.Message}");
                LogResult("========================================", "");
                MessageBox.Show("Kritischer Fehler beim Datenbank-Update: " + ex.Message);
            }
        }

        private void DropAllRelationsOnColumn(OleDbConnection conn, string tableName, string columnName)
        {
            var schemaTable = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, null);
            if (schemaTable == null) return;

            foreach (System.Data.DataRow row in schemaTable.Rows)
            {
                string fkTable = row["FK_TABLE_NAME"].ToString();
                string fkColumn = row["FK_COLUMN_NAME"].ToString();
                string constraintName = row["FK_NAME"].ToString();

                if (fkTable.Equals(tableName, StringComparison.OrdinalIgnoreCase) &&
                    fkColumn.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (OleDbCommand cmd = new OleDbCommand($"ALTER TABLE [{fkTable}] DROP CONSTRAINT [{constraintName}]", conn))
                        {
                            cmd.ExecuteNonQuery();
                            LogResult("CLEANUP", $"Beziehung '{constraintName}' entfernt.");
                        }
                    }
                    catch (Exception ex) { LogResult("CLEANUP-FEHLER", ex.Message); }
                }
            }
        }

        private void DropAllIndexesOnColumn(OleDbConnection conn, string tableName, string columnName)
        {
            var schemaTable = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Indexes, new object[] { null, null, null, null, tableName });
            if (schemaTable == null) return;

            foreach (System.Data.DataRow row in schemaTable.Rows)
            {
                if (row["COLUMN_NAME"].ToString().Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    string indexName = row["INDEX_NAME"].ToString();
                    if (!(bool)row["PRIMARY_KEY"]) // Primärschlüssel niemals löschen!
                    {
                        try
                        {
                            using (OleDbCommand cmd = new OleDbCommand($"DROP INDEX [{indexName}] ON [{tableName}]", conn))
                            {
                                cmd.ExecuteNonQuery();
                                LogResult("CLEANUP", $"Index '{indexName}' entfernt.");
                            }
                        }
                        catch (Exception ex) { LogResult("CLEANUP-FEHLER", ex.Message); }
                    }
                }
            }
        }

        public class RelationInfo
        {
            public string Name { get; set; }
            public string MasterTable { get; set; }
            public string MasterColumn { get; set; }
            public string ForeignTable { get; set; }
            public string ForeignColumn { get; set; }
        }

        private void SichertBeziehungen(OleDbConnection conn, string tableName, string columnName)
        {
            var schemaTable = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, null);
            if (schemaTable == null) return;

            bool gefunden = false;
            foreach (System.Data.DataRow row in schemaTable.Rows)
            {
                // Trimme die Werte aus der Datenbank für den Vergleich
                string dbTable = row["FK_TABLE_NAME"].ToString().Trim();
                string dbColumn = row["FK_COLUMN_NAME"].ToString().Trim();

                if (dbTable.Equals(tableName, StringComparison.OrdinalIgnoreCase) &&
                    dbColumn.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    gesicherteBeziehungen.Add(new RelationInfo
                    {
                        Name = row["FK_NAME"].ToString(),
                        MasterTable = row["PK_TABLE_NAME"].ToString(),
                        MasterColumn = row["PK_COLUMN_NAME"].ToString(),
                        ForeignTable = dbTable,
                        ForeignColumn = dbColumn
                    });
                    LogResult("SICHERUNG", $"Beziehung '{row["FK_NAME"]}' für {tableName}.{columnName} gesichert.");
                    gefunden = true;
                }
            }

            if (!gefunden)
            {
                LogResult("SICHERUNG-INFO", $"Keine Beziehungen für {tableName}.{columnName} gefunden.");
            }
        }

        private void WiederherstellenBeziehungen(OleDbConnection conn, string zielSpalte)
        {
            if (gesicherteBeziehungen.Count == 0)
            {
                LogResult("RESTORE", "Keine Beziehungen im Speicher gefunden.");
                return;
            }

            foreach (var rel in gesicherteBeziehungen)
            {
                try
                {
                    // Wir ignorieren rel.ForeignColumn und nehmen stattdessen zielSpalte
                    string sql = $@"ALTER TABLE [{rel.ForeignTable}] 
                            ADD CONSTRAINT [{rel.Name}] 
                            FOREIGN KEY ([{zielSpalte}]) 
                            REFERENCES [{rel.MasterTable}] ([{rel.MasterColumn}])";

                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                        LogResult("RESTORE-ERFOLG", $"Beziehung {rel.Name} auf {zielSpalte} wiederhergestellt.");
                    }
                }
                catch (Exception ex)
                {
                    LogResult("RESTORE-FEHLER", $"Fehler bei {rel.Name}: {ex.Message}");
                }
            }
            // Nach dem Restore Liste leeren, damit sie beim nächsten Mal frisch ist
            gesicherteBeziehungen.Clear();
        }

        // Hilfsmethode zum Schreiben in die Textdatei
        private void LogResult(string status, string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logLine = $"[{timestamp}] {status}: {message}";

                // Schreibt die Zeile in die Datei (erstellt die Datei, falls nicht vorhanden)
                File.AppendAllLines(logFilePath, new[] { logLine });
            }
            catch
            {
                // Falls das Logging selbst fehlschlägt (z.B. keine Schreibrechte), 
                // unterdrücken wir den Fehler, um das Update nicht zu blockieren.
            }
        }

         public List<RelationInfo> GetBeziehungsListe()
        {
            List<RelationInfo> liste = new List<RelationInfo>();
            string dbPath = GetDBPath();
            string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};";

            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                conn.Open();
                var schemaTable = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, null);

                if (schemaTable != null)
                {
                    foreach (System.Data.DataRow row in schemaTable.Rows)
                    {
                        liste.Add(new RelationInfo
                        {
                            MasterTable = row["PK_TABLE_NAME"].ToString(),
                            ForeignTable = row["FK_TABLE_NAME"].ToString(),
                            MasterColumn = row["PK_COLUMN_NAME"].ToString(),
                            ForeignColumn = row["FK_COLUMN_NAME"].ToString(),
                            Name = row["FK_NAME"].ToString()
                        });
                    }
                }
            }
            return liste;
        }
        public void AnalysiereAbhaengigkeiten()
        {
            var relations = GetBeziehungsListe(); // Liste aller Master-Detail Paare
            var tabellenLevel = new Dictionary<string, int>();

            // Alle Tabellen initial auf Level 0 setzen
            foreach (var rel in relations)
            {
                if (!tabellenLevel.ContainsKey(rel.MasterTable)) tabellenLevel[rel.MasterTable] = 0;
                if (!tabellenLevel.ContainsKey(rel.ForeignTable)) tabellenLevel[rel.ForeignTable] = 0;
            }

            // Level berechnen (Einfacher Algorithmus: Level = Max(Level des Masters + 1))
            // Diesen Loop ein paar Mal durchlaufen, um Verschachtelungen zu finden
            for (int i = 0; i < 5; i++)
            {
                foreach (var rel in relations)
                {
                    if (tabellenLevel[rel.ForeignTable] <= tabellenLevel[rel.MasterTable])
                    {
                        tabellenLevel[rel.ForeignTable] = tabellenLevel[rel.MasterTable] + 1;
                    }
                }
            }

            // Ausgabe nach Level sortiert
            foreach (var entry in tabellenLevel.OrderBy(x => x.Value))
            {
                Console.WriteLine($"Level {entry.Value}: {entry.Key}");
            }

            ZeigeHierarchie(relations, tabellenLevel);
        }

        public void ZeigeHierarchie(List<RelationInfo> relations, Dictionary<string, int> tabellenLevel)
        {
            LogResult("ANALYSE", "--- Detaillierte Baumstruktur ---");

            foreach (var entry in tabellenLevel.OrderBy(x => x.Value))
            {
                string tabName = entry.Key;
                int level = entry.Value;

                // Einrücken für die Optik
                string indent = new string(' ', level * 4);

                // Finde heraus, wer der "Vater" dieser Tabelle ist (falls vorhanden)
                var eltern = relations.Where(r => r.ForeignTable.Equals(tabName, StringComparison.OrdinalIgnoreCase))
                                      .Select(r => r.MasterTable)
                                      .Distinct();

                string elternInfo = eltern.Any() ? " [Hängt ab von: " + string.Join(", ", eltern) + "]" : " [Stammdaten / Wurzel]";

                LogResult("STRUKTUR", $"{indent}Level {level}: {tabName}{elternInfo}");
            }
        }
    }
}