using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class ProzesswaermeCtrl : ProzesswaermeModel
    {
        public int rows;
        OdbcCommand DBCommand;
        public ProzesswaermeModel model;

        public ProzesswaermeCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new ProzesswaermeModel();
        }
        ~ProzesswaermeCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll()
        {
            DBCommand.CommandText = "select * from Tab_Prozesswaerme order by Prozessname";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new ProzesswaermeModel[1000];

            rows = 0;

            while (DBReader.Read())
            {
                ProzesswaermeModel item = new ProzesswaermeModel();

                if (!DBReader.IsDBNull(0)) item.m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_szProzessname = (string)DBReader.GetString(1);
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

        public void ReadSingle(int ID_Prozesswaerme)
        {
            DBCommand.CommandText = "select * from Tab_Prozesswaerme where ID=" + ID_Prozesswaerme;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;

            DBReader.Read();
            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szProzessname = (string)DBReader.GetString(1);
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

        public void ReadSingle(string szProzessname)
        {
            DBCommand.CommandText = "select * from Tab_Prozesswaerme where Prozessname='" + szProzessname + "'";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;

            DBReader.Read();
            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szProzessname = (string)DBReader.GetString(1);
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
