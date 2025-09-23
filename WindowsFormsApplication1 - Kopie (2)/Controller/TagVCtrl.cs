using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class TagVCtrl : TagVModel
    {
        OdbcCommand DBCommand;
        TagVModel model;
        public int rows;

        public TagVCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new TagVModel();
        }

        ~TagVCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new TagVCtrl[1000];
            rows = 0;
            while (DBReader.Read())
            {
                TagVCtrl item = new TagVCtrl();

                if (!DBReader.IsDBNull(0)) item.ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.Name = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) item.Beschreibung = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.Veraenderbar = (bool)DBReader.GetValue(3);

                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

    }
}
