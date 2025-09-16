using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class StromspeicherCtrl : StromspeicherModel
    {
        OdbcCommand DBCommand;
        public StromspeicherModel stromspeichermodel;

        public StromspeicherCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            stromspeichermodel = new StromspeicherModel();
        }

        ~StromspeicherCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll()
        {
            DBCommand.CommandText = "select * from Tab_Stromspeicher order by Bezeichner";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();
            items = new StromspeicherModel[1000];
            rows = 0;

            while (DBReader.Read())
            {
                StromspeicherModel item = new StromspeicherModel();

                if (!DBReader.IsDBNull(0)) item.m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_szBezeichner = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) item.m_szTyp = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.m_Leistung = (int)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) item.m_Energie = (int)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.m_Degradation = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) item.m_Ladezustand = (int)DBReader.GetValue(6); ;

                items[rows] = item;
                item = null;
                rows += 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadSingle(int ID)
        {
            DBCommand.CommandText = "select * from Tab_Stromspeicher where ID=" + ID;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;

            DBReader.Read();

            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szBezeichner = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) m_szTyp = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) m_Leistung = (int)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) m_Energie = (int)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) m_Degradation = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) m_Ladezustand = (int)DBReader.GetValue(6); ;
                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadSingle(string szBezeichner)
        {
            DBCommand.CommandText = "select * from Tab_Stromspeicher where Bezeichner='" + szBezeichner + "'";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;

            DBReader.Read();

            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szBezeichner = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) m_szTyp = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) m_Leistung = (int)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) m_Energie = (int)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) m_Degradation = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) m_Ladezustand = (int)DBReader.GetValue(6); ;
                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }
    }
}
