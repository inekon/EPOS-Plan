using System;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    public class RecordSet
    {
        public OdbcCommand DBCommand;
        OdbcDataReader DBReader;

        public RecordSet()
        {
            DBCommand = Program.DBConnection.CreateCommand();
        }

        public bool Open(string sql)
        {
            DBCommand.CommandText = sql;
            DBReader = DBCommand.ExecuteReader();
            return true;
        }

        public bool Insert(string sql)
        {
            try
            {
                DBCommand.CommandText = sql;
                DBCommand.ExecuteNonQuery();
                return true;
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                return false;
            }
        }

        public bool EOF()
        {
            if (DBReader.Read()) return false;
            return true;
        }

        public bool Next()
        {
            return DBReader.Read(); 
        }

        public Object Read(string name)
        {
           return DBReader[name];
        }
        
        public Object Read(int index)
        {
            return DBReader.GetValue(index);
        }

        public String GetString(string name)
        {
            if (DBReader[name] == DBNull.Value) return "";
            return (string)DBReader[name];
        }   

        public void Close()
        {
            if (DBReader != null)
            {
                DBReader.Close();
                DBReader.Dispose();
            }
        }

     }
}
