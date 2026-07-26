using System;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    public class RecordSet
    {
        // Auf OleDbCommand umgestellt, damit Zuweisungen aus dem UI-Code (z.B. transaction) ohne Cast funktionieren
        public OleDbCommand DBCommand { get; set; }
        private OleDbDataReader DBReader;
        private OleDbConnection _internalConnection;

        public RecordSet()
        {
            DBCommand = new OleDbCommand();
        }

        public bool Open(string sql)
        {
            try
            {
                // Falls dem DBCommand von außen (über eine Transaktion) bereits eine Connection zugewiesen wurde, nutzen wir diese.
                // Andernfalls öffnen wir eine eigene, interne Verbindung für diese Abfrage.
                if (DBCommand.Connection == null)
                {
                    _internalConnection = new OleDbConnection(DataRepository.GetConnectionString());
                    _internalConnection.Open();
                    DBCommand.Connection = _internalConnection;
                }

                DBCommand.CommandText = sql;
                DBReader = DBCommand.ExecuteReader();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Öffnen des RecordSets: " + ex.Message);
                return false;
            }
        }

        public bool Insert(string sql)
        {
            try
            {
                // Gleiche Logik: Verbindung prüfen / dynamisch zuweisen
                if (DBCommand.Connection == null)
                {
                    _internalConnection = new OleDbConnection(DataRepository.GetConnectionString());
                    _internalConnection.Open();
                    DBCommand.Connection = _internalConnection;
                }

                DBCommand.CommandText = sql;
                DBCommand.ExecuteNonQuery();
                return true;
            }
            catch (Exception sqlEx)
            {
                Console.WriteLine("SQL Fehler beim Insert: " + sqlEx.Message);
                return false;
            }
            finally
            {
                // Wenn wir für den Nicht-Abfrage-Befehl (Insert) eine interne Verbindung geöffnet haben,
                // schließen wir sie direkt wieder, da kein Reader darauf wartet.
                if (_internalConnection != null && DBCommand.Transaction == null)
                {
                    _internalConnection.Close();
                    _internalConnection.Dispose();
                    _internalConnection = null;
                    DBCommand.Connection = null;
                }
            }
        }

        public bool EOF()
        {
            if (DBReader == null) return true;

            // Verhält sich wie der alte OdbcReader: Liest den nächsten Datensatz. 
            // Gibt es keinen, sind wir am Ende (EOF = true).
            if (DBReader.Read()) return false;
            return true;
        }

        public bool Next()
        {
            if (DBReader == null) return false;
            return DBReader.Read();
        }

        public Object Read(string name)
        {
            if (DBReader == null) return null;
            return DBReader[name];
        }

        public Object Read(int index)
        {
            if (DBReader == null) return null;
            return DBReader.GetValue(index);
        }

        public String GetString(string name)
        {
            if (DBReader == null || DBReader[name] == DBNull.Value) return "";
            return DBReader[name].ToString();
        }

        public void Close()
        {
            if (DBReader != null)
            {
                DBReader.Close();
                DBReader.Dispose();
                DBReader = null;
            }

            // WICHTIG: Wenn wir eine eigene interne Verbindung geöffnet hatten, 
            // sauber abbauen, sobald das RecordSet geschlossen wird.
            if (_internalConnection != null)
            {
                _internalConnection.Close();
                _internalConnection.Dispose();
                _internalConnection = null;
                DBCommand.Connection = null;
            }
        }
    }
}