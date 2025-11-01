using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class BHKWCtrl : BHKWModel
    {
        OdbcCommand DBCommand;
  
        public BHKWCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
        }

        ~BHKWCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll()
        {
            DBCommand.CommandText = "select * from Tab_BHKW order by Modulname";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();
            items = new BHKWModel[1000];
            rows = 0;

            while (DBReader.Read())
            {
                BHKWModel item = new BHKWModel();

                if (!DBReader.IsDBNull(0)) item.m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_szBezeichner = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) item.m_szFirma = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.m_szBeschreibung = (string)DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) item.m_Ptherm = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.m_Pel = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) item.m_Brennstoff = (int)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) item.m_Wirkungsgrad = (double)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) item.m_Investition_KWel = (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) item.m_Raumbedarf = (double)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) item.m_Wartungskosten_kWhel = (double)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) item.m_Nutzungsdauer = (int)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(12)) item.m_NOx = (int)DBReader.GetValue(12);
                if (!DBReader.IsDBNull(13)) item.m_SO2 = (int)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) item.m_CO = (int)DBReader.GetValue(14);
                if (!DBReader.IsDBNull(15)) item.m_CO2 = (int)DBReader.GetValue(15);
                if (!DBReader.IsDBNull(16)) item.m_Staub = (int)DBReader.GetValue(16);

                items[rows] = item;
                item = null;
                rows += 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadSingle(int ID)
        {
            DBCommand.CommandText = "select * from Tab_BHKW where ID=" + ID;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();
  
            rows = 0;

            DBReader.Read();

            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szBezeichner = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) m_szFirma = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) m_szBeschreibung = (string)DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) m_Ptherm = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) m_Pel = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) m_Brennstoff = (int)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) m_Wirkungsgrad = (double)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) m_Investition_KWel = (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) m_Raumbedarf = (double)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) m_Wartungskosten_kWhel = (double)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) m_Nutzungsdauer = (int)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(12)) m_NOx = (int)DBReader.GetValue(12);
                if (!DBReader.IsDBNull(13)) m_SO2 = (int)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) m_CO = (int)DBReader.GetValue(14);
                if (!DBReader.IsDBNull(15)) m_CO2 = (int)DBReader.GetValue(15);
                if (!DBReader.IsDBNull(16)) m_Staub = (int)DBReader.GetValue(16);

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadSingle(string szBezeichner)
        {
            DBCommand.CommandText = "select * from Tab_BHKW where Modulname='" + szBezeichner + "'";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;

            DBReader.Read();

            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szBezeichner = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) m_szFirma = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) m_szBeschreibung = (string)DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) m_Ptherm = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) m_Pel = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) m_Brennstoff = (int)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) m_Wirkungsgrad = (double)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) m_Investition_KWel = (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) m_Raumbedarf = (double)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) m_Wartungskosten_kWhel = (double)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) m_Nutzungsdauer = (int)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(12)) m_NOx = (int)DBReader.GetValue(12);
                if (!DBReader.IsDBNull(13)) m_SO2 = (int)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) m_CO = (int)DBReader.GetValue(14);
                if (!DBReader.IsDBNull(15)) m_CO2 = (int)DBReader.GetValue(15);
                if (!DBReader.IsDBNull(16)) m_Staub = (int)DBReader.GetValue(16);

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

    }
}
