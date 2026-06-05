using System;
using System.Data;
using System.Data.Odbc;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{

    class DbClass
    {
        public OdbcConnection DBConnection;

        public string[] tabellenNamen_import;
        public string[] tabellenNamen_delete;
        public string szError;
        private readonly string[] scripte = { "TABELLEN", "SPALTEN", "DATENTYPEN", "IMPORT", "DELETE" };

        public OdbcConnection OpenDB(string szDSN)
        {
            DBConnection = new OdbcConnection(szDSN);
            DBConnection.Open();
            return DBConnection;
        }

        public void CloseDB()
        {
            DBConnection.Close();
        }

        public async Task<bool> UpdateDatabaseAsync(string sourceConnStr, string targetConnStr, ProgressBar progressBar1 = null)
        {
            int totalSteps = tabellenNamen_import.Length + tabellenNamen_delete.Length;
            
            if (progressBar1 != null)
            {
                progressBar1.Maximum = totalSteps;
                progressBar1.Value = 0;
            }
            int done = 0;

            var progress = new Progress<int>(val =>
            {
                // Fortschrittsanzeige - absolute abgeschlossene Schritte
                if (val >= 0 && val <= progressBar1.Maximum)
                    progressBar1.Value = val;

                Thread.Sleep(100);
            });

            bool result = true;

            try
            {
                // Schwere Arbeit außerhalb des UI-Threads ausführen
                await Task.Run(() =>
                {
                    // Löschen der Tabellen
                    foreach (string tableName in tabellenNamen_delete)
                    {
                        string sql = $"DELETE FROM [{tableName}]"; // SQL zum Leeren der Tabelle
                        using (OdbcConnection targetConn = new OdbcConnection(targetConnStr))
                        {
                            targetConn.Open();
                            OdbcCommand cmd = new OdbcCommand(sql, targetConn);

                            try
                            {
                                cmd.ExecuteNonQuery();
                                targetConn.Close();
                            }
                            catch
                            {
                                // Fehlerausgabe
                                targetConn.Close(); 
                            }
                        }
                        done++;
                        if (progressBar1 != null) (progress as IProgress<int>)?.Report(done);
                    }

                    if (!result)
                    {
                        // Abbruch bei Fehler
                        return;
                    }

                    // Import
                    for (int i = 0; i < tabellenNamen_import.Length; i++)
                    {
                        // Bestehende ImportData verwenden (führt DB-Operationen außerhalb des UI-Threads aus)
                        result = ImportTableData(sourceConnStr, targetConnStr, tabellenNamen_import[i]);
                        if (!result)
                        {
                            // Abbruch bei Fehler
                            break;
                        }
                        done++;
                        if (progressBar1 != null) (progress as IProgress<int>)?.Report(done);

                    }
                });
                return result;
            }
            catch
            {
                // Fehler im UI-Thread anzeigen
                return false;
            }
            finally
            {
                if (progressBar1 != null) progressBar1.Value = progressBar1.Maximum;
            }
        }

        public string GetIniFilePath()
        {
            if (File.Exists(Program.ApplicationPath_User + "\\UpdateDB.ini"))
            {
                return Program.ApplicationPath_User + "\\UpdateDB.ini";
            }
            else return "";
        }

        public string GetDBFilePath()
        {
            if (File.Exists(Program.ApplicationPath_User + "\\Kenndaten.accdb"))
            {
                return Program.ApplicationPath_User + "\\Kenndaten.accdb";
            }
            else return "";
        }

        public IniFileParser ParseIniFile(string szIniFile)
        {
            IniFileParser ini = new IniFileParser();
            ini.Parse(szIniFile);
            return ini;
        }

        public bool UpdateTablesStructure(IniFileParser ini, string sourceConnStr)
        {
            // in gesicherter DB ggf. die Tabellenstruktur aktualisieren
            OdbcConnection sourceConn = new OdbcConnection(sourceConnStr);
            sourceConn.Open();
            string szTemp;

            for (int n = 0; n < 3; n++)
            {
                szTemp = ini.GetValue(scripte[n], "ANZAHL");
                if (szTemp != null)
                {
                    int anzahl = Convert.ToInt32(szTemp);
                    for (int i = 1; i <= anzahl; i++)
                    {
                        string sql = ini.GetValue(scripte[n], "SQL" + i.ToString());

                        if (sql != null)
                        {
                            try
                            {
                                OdbcCommand cmd = new OdbcCommand(sql, sourceConn);
                                cmd.ExecuteNonQuery();
                            }
                            catch (OdbcException ex)
                            {
                                szError = ex.Message;
                                OdbcError sqlError = ex.Errors[0];
                                if (string.Equals(sqlError.SQLState, "42S01", StringComparison.OrdinalIgnoreCase) 
                                    || sqlError.NativeError == -1303) // Access: table existiert bereits, tritt bei CREATE TABLE auf
                                {
                                    continue; //  "table exists" ignorieren
                                }
                                else return false;
                            }
                            catch (Exception ex)
                            {
                                szError = ex.Message;
                                return false;
                            }
                        }
                    }
                }
            }
            sourceConn.Close(); 
            return true;
        }

        public void GetUpdateTables(IniFileParser ini)
        {
            string szTemp;
            szTemp = ini.GetValue(scripte[3], "ANZAHL");
            if (szTemp != null)
            {
                int anzahl = Convert.ToInt32(szTemp);
                string[] tabellenNamen_import = new string[anzahl];
                this.tabellenNamen_import = tabellenNamen_import;

                for (int i = 1; i <= anzahl; i++)
                {
                    string table = ini.GetValue(scripte[3], "TAB" + i.ToString());
                    if (table != null)
                    {
                        this.tabellenNamen_import[i - 1] = table;
                    }
                }
            }

            szTemp = ini.GetValue(scripte[4], "ANZAHL");
            if (szTemp != null)
            {
                int anzahl = Convert.ToInt32(szTemp);
                string[] tabellenNamen_delete = new string[anzahl];
                this.tabellenNamen_delete = tabellenNamen_delete;
                for (int i = 1; i <= anzahl; i++)
                {
                    string table = ini.GetValue(scripte[4], "TAB" + i.ToString());
                    if (table != null)
                    {
                        this.tabellenNamen_delete[i - 1] = table;
                    }
                }
            }
        }

        public bool ImportTableData(string sourceConnStr, string targetConnStr, string QuellTabelle)
        {
            DataTable dt = new DataTable(QuellTabelle);

            try
            {
                // 1. Daten aus der Quell-Datenbank lesen
                using (OdbcConnection sourceConn = new OdbcConnection(sourceConnStr))
                {
                    string selectSql = "SELECT * FROM [" + QuellTabelle + "]";
                    OdbcCommand cmd = new OdbcCommand(selectSql, sourceConn);
                    sourceConn.Open();
                    dt.Load(cmd.ExecuteReader()); // Lädt Daten in die DataTable
                }

                // 2. Daten in die Ziel-Datenbank schreiben
                using (OdbcConnection targetConn = new OdbcConnection(targetConnStr))
                {
                    string cols = "";
                    string parameters = "";

                    targetConn.Open();

                    foreach (DataColumn col in dt.Columns)
                    {
                        cols += "[" + col.ColumnName + "],";
                        parameters += "?,";
                    }

                    if (cols.Length == 0) return false;

                    cols = cols.Substring(0, cols.Length - 1);
                    parameters = parameters.Substring(0, parameters.Length - 1);
                    string insertSql = "INSERT INTO [" + QuellTabelle + "] (" + cols + ") VALUES (" + parameters + ")";

                    using (OdbcCommand insertCmd = new OdbcCommand(insertSql, targetConn))
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            // Parameter verwenden, um SQL-Injection zu vermeiden
                            insertCmd.Parameters.Clear();
                            int i = 0;
                            foreach (DataColumn col in dt.Columns)
                            {
                                insertCmd.Parameters.AddWithValue("?", row[i++]);
                            }

                            insertCmd.ExecuteNonQuery();

                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Fehlerausgabe
                szError = "Fehler beim Importieren in '" + QuellTabelle + "'\n\n" + ex.Message;
                return false;
            }
        }

    }
}
