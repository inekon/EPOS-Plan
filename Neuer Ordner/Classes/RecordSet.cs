using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Data;

namespace WindowsFormsApplication1
{
    public class RecordSet
    {
        OdbcCommand DBCommand;
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
        
        public void Close()
        {
            DBReader.Dispose();
            DBReader.Close();
        }

        public void CreateDataAdapter()
        {
  
            OdbcCommand cmd = Program.DBConnection.CreateCommand();
            cmd.CommandText = "INSERT INTO Tab_Typ_Energieanlagen (ID,Bezeichner) VALUES (?,'test')";
            //OdbcParameter par =  cmd.CreateParameter();
            //par.ParameterName = "ID";
            cmd.Parameters.Add("@id", OdbcType.Int).Value = 10;
            
            cmd.Connection = Program.DBConnection;
            cmd.ExecuteNonQuery();

            return;
        }

     }
}
