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

    }
}
