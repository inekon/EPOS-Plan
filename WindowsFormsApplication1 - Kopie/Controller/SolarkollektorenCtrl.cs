using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class SolarkollektorenCtrl : SolarkollektorenModel
    {
        public OdbcCommand DBCommand;
        public SolarkollektorenModel model = new SolarkollektorenModel();
    
        public SolarkollektorenCtrl()
        {
            DBCommand = Program.DBConnection.CreateCommand();
        }

        ~SolarkollektorenCtrl()
        {
            DBCommand.Dispose();
        }

        public void ReadAll(string szFilter="")
        {
            string sql;

            if (szFilter == "")
                sql = "select * from Tab_Solarkollektoren order by Kollektorname";
            else
                sql = "select * from Tab_Solarkollektoren where " + szFilter + " order by Kollektorname";   

            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new SolarkollektorenModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                SolarkollektorenModel item = new SolarkollektorenModel();

                if (!DBReader.IsDBNull(0)) item.m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_szKollektorname = DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) item.m_szFirma = DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.m_szBeschreibung = DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) item.m_szKollektortyp = DBReader.GetString(4);
                if (!DBReader.IsDBNull(5)) item.m_Modulfläche = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) item.m_Aperturfläche = (double)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) item.m_h0 = (double)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) item.m_k1= (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) item.m_k2 = (double)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) item.m_Kdir = (double)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) item.m_Kdfu  = (double)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(12)) item.m_Kosten = (double)DBReader.GetValue(12);
                if (!DBReader.IsDBNull(13)) item.m_Vorlauf = (int)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) item.m_Ruecklauf = (int)DBReader.GetValue(14);

                items[rows] = item;
                item = null;
                rows += 1;
            }
            DBReader.Close();
            DBReader.Dispose();
        }

        public void ReadSingle(int ID)
        {
            DBCommand.CommandText = "select * from Tab_Solarkollektoren where ID=" + ID;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;

            DBReader.Read();

            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szKollektorname = DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) m_szFirma = DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) m_szBeschreibung = DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) m_szKollektortyp = DBReader.GetString(4);
                if (!DBReader.IsDBNull(5)) m_Modulfläche = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) m_Aperturfläche = (double)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) m_h0 = (double)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) m_k1 = (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) m_k2 = (double)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) m_Kdir = (double)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) m_Kdfu = (double)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(12)) m_Kosten = (double)DBReader.GetValue(12);
                if (!DBReader.IsDBNull(13)) m_Vorlauf = (int)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) m_Ruecklauf = (int)DBReader.GetValue(14);

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public bool Update()
        {
            try
            {
               FormattableString sql = $@"UPDATE Tab_Solarkollektoren SET 
                    Firma = '{model.m_szFirma}', 
                    Beschreibung = '{model.m_szBeschreibung}', 
                    Kollektortyp = '{model.m_szKollektortyp}', 
                    Modulflaeche = {model.m_Modulfläche}, 
                    Aperturflaeche = {model.m_Aperturfläche}, 
                    h0 = {model.m_h0}, 
                    k1 = {model.m_k1}, 
                    k2 = {model.m_k2}, 
                    Kdir = {model.m_Kdir}, 
                    Kdfu = {model.m_Kdfu}, 
                    Investitionskosten = {model.m_Kosten} 
                    WHERE Kollektorname = '{model.m_szKollektorname}'";

                string commandText = sql.ToString(CultureInfo.InvariantCulture);

                DBCommand.CommandText = commandText;
                DBCommand.ExecuteNonQuery();
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                return false;
            }
            return true;

        }
    }
}
