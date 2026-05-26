using Microsoft.Win32;
using System;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public static class DataRepository
    {
   
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
                    MessageBox.Show("Fehler beim Laden der Daten: " + ex.Message);
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
                    MessageBox.Show("Datenbankfehler: " + ex.Message);
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
                    System.Windows.Forms.MessageBox.Show("Datenbankfehler (NonQuery): " + ex.Message);
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
                    System.Windows.Forms.MessageBox.Show("Datenbankfehler (NonQuery): " + ex.Message);
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
                    System.Windows.Forms.MessageBox.Show("Datenbankfehler (Scalar): " + ex.Message);
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

        public static string GetDBPath()
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
                MessageBox.Show($"Fehler beim Löschen in {masterTable}: " + ex.Message);
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
