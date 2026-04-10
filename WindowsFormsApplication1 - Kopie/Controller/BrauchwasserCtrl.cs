using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class BrauchwasserCtrl : BrauchwasserModel
    {
        public int rows;
        OdbcCommand DBCommand;
        public BrauchwasserModel model;

        public BrauchwasserCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new BrauchwasserModel();
        }
        ~BrauchwasserCtrl ()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll()
        {
            DBCommand.CommandText = "select * from Tab_Brauchwasser order by Bezeichner";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new BrauchwasserModel[1000];

            rows = 0;

            while (DBReader.Read())
            {
                BrauchwasserModel item = new BrauchwasserModel();

                if (!DBReader.IsDBNull(0)) item.m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_szBezeichner = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) item.m_szTyp = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.m_szBeschreibung = (string)DBReader.GetString(3);
                for (int i = 0; i < 12; i++)
                {
                    if (!DBReader.IsDBNull(i + 4)) item.m_Monat[i] = (double)DBReader.GetValue(i + 4);
                }

                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadSingle(int ID_Brauchwasser)
        {
            DBCommand.CommandText = "select * from Tab_Brauchwasser where ID=" + ID_Brauchwasser;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;

            DBReader.Read();
            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szBezeichner = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) m_szTyp = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) m_szBeschreibung = (string)DBReader.GetString(3);
                
                for (int i = 0; i < 12; i++)
                {
                    if (!DBReader.IsDBNull(i+4)) m_Monat[i] = (double)DBReader.GetValue(i+4);
                }

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadSingle(string szBezeichner)
        {
            DBCommand.CommandText = "select * from Tab_Brauchwasser where Bezeichner='" + szBezeichner + "'";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;

            DBReader.Read();
            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szBezeichner = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) m_szTyp = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) m_szBeschreibung = (string)DBReader.GetString(3);

                for (int i = 0; i < 12; i++)
                {
                    if (!DBReader.IsDBNull(i + 4)) m_Monat[i] = (double)DBReader.GetValue(i + 4);
                }

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

    }
}
