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
    class PhotovoltaikCtrl : PhotovoltaikModel
    {
        public OdbcCommand DBCommand;
        public PhotovoltaikModel model = new PhotovoltaikModel();
        public int rows;
    
        public PhotovoltaikCtrl ()
        {
            DBCommand = Program.DBConnection.CreateCommand();
            rows = 0;
        }

        ~PhotovoltaikCtrl ()
        {
            DBCommand.Dispose();
        }

        public void ReadAll(string szFilter="")
        {
            string sql;

            if (szFilter == "")
                sql = "select * from Tab_PV order by Modulname";
            else
                sql = "select * from Tab_PV where " + szFilter + " order by Modulname";   

            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new PhotovoltaikModel[1000];
            rows = 0;
            while (DBReader.Read())
            {
                PhotovoltaikModel item = new PhotovoltaikModel();

                if (!DBReader.IsDBNull(0)) item.m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_szName = DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) item.m_szFirma = DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.m_szBeschreibung = DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) item.m_Leistung = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.m_Wirkungsgrad = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) item.m_U_Mpp = (double)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) item.m_U_Leerlauf = (double)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) item.m_I_Mpp = (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) item.m_I_Kurzschluss = (double)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) item.m_alpha_SC = (double)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) item.m_beta_OC = (double)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(12)) item.m_Temp_Coeff_Pmax = (double)DBReader.GetValue(12);
                if (!DBReader.IsDBNull(13)) item.m_T_NOCT = (double)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) item.m_Laenge = (double)DBReader.GetValue(14);
                if (!DBReader.IsDBNull(15)) item.m_Breite = (double)DBReader.GetValue(15);
                if (!DBReader.IsDBNull(16)) item.m_Modulkosten = (double)DBReader.GetValue(16);

                items[rows] = item;
                item = null;
                rows += 1;
            }
            DBReader.Close();
            DBReader.Dispose();
        }

        public void ReadSingle(int ID)
        {
            DBCommand.CommandText = "select * from Tab_PV where ID=" + ID;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;

            DBReader.Read();

            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szName = DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) m_szFirma = DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) m_szBeschreibung = DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) m_Leistung = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) m_Wirkungsgrad = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) m_U_Mpp = (double)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) m_U_Leerlauf = (double)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) m_I_Mpp = (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) m_I_Kurzschluss = (double)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) m_alpha_SC = (double)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) m_beta_OC = (double)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(12)) m_Temp_Coeff_Pmax = (double)DBReader.GetValue(12);
                if (!DBReader.IsDBNull(13)) m_T_NOCT= (double)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) m_Laenge = (double)DBReader.GetValue(14);
                if (!DBReader.IsDBNull(15)) m_Breite = (double)DBReader.GetValue(15);
                if (!DBReader.IsDBNull(16)) m_Modulkosten = (double)DBReader.GetValue(16);

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public bool Update()
        {
            try
            {
                string sql = FormattableString.Invariant($@"
                    UPDATE Tab_PV 
                    SET 
                        Firma = '{model.m_szFirma}', 
                        Beschreibung = '{model.m_szBeschreibung}', 
                        Leistung = {model.m_Leistung}, 
                        Wirkungsgrad = {model.m_Wirkungsgrad}, 
                        U_Mpp = {model.m_U_Mpp}, 
                        U_Leerlauf = {model.m_U_Leerlauf}, 
                        I_Mpp = {model.m_I_Mpp}, 
                        I_Kurzschluss = {model.m_I_Kurzschluss}, 
                        alpha_SC= {SqlVal(model.m_alpha_SC)}, 
                        beta_OC= {SqlVal(model.m_beta_OC)}, 
                        gamma_PMP = {SqlVal(model.m_Temp_Coeff_Pmax)}, 
                        T_NOCT = {SqlVal(model.m_T_NOCT)}, 
                        Laenge = {model.m_Laenge}, 
                        Breite = {model.m_Breite}, 
                        Modulkosten = {model.m_Modulkosten} 
                    WHERE 
                        Modulname = '{model.m_szName}'");

                DBCommand.CommandText = sql;
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

        private string SqlVal(double value)
        {
            // Wenn 0, dann SQL NULL, sonst den Invariant-String des Wertes
            return value == 0 ? "NULL" : value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
