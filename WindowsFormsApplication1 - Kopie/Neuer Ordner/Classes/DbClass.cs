using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    
    class DbClass
    {
        public OdbcConnection DBConnection;
        public string[] array;

        public OdbcConnection openDB()
        {
            DBConnection = new OdbcConnection("DSN=TEST");
            DBConnection.Open();
            return DBConnection;
        }

        public void closeDB()
        {
            DBConnection.Close();
        }

        public void Read(string sql)
        {
            OdbcCommand DBCommand = DBConnection.CreateCommand();
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();
            int fCount = DBReader.FieldCount;
            string fName = DBReader.GetName(fCount - 1);

            List<string> list = new List<string>();
            while (DBReader.Read())
            {
                for (int i = 0; i < DBReader.FieldCount; i++)
                {
                    string temp="";
                    if(DBReader.IsDBNull(i) == false)
                    {
                        temp = DBReader.GetString(i);
                    }
                    list.Add(temp);
                }
            }

  
            array = list.ToArray();
            list = null;
            DBReader.Close();
            DBCommand.Dispose();
        }
    }
}
