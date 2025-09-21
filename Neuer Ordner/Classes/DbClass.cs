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
    }
}
