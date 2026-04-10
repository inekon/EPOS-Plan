using System;
using System.Data.Odbc;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class KonfigurationCtrl : KonfigurationModel
    {
        public OdbcCommand DBCommand;
        public KonfigurationModel model = new KonfigurationModel();
        public int rows;

        public enum Energieerzeuger
        {
            BHKW = 0,
            HEIZKESSEL = 1,
            PHOTOVOLTAIK = 2,
            SOLARTHERMIE = 3,
            WAERMEPUMPE = 4
        }

        public KonfigurationCtrl()
        {
            DBCommand = Program.DBConnection.CreateCommand();
            rows = 0;
        }

        ~KonfigurationCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadSingle(string sql)
        {
            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;
            DBReader.Read();
            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) model.m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) model.m_ID_Projekt = (int)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) model.m_BHKW_Grenzleistung = (double)DBReader.GetValue(2);
                if (!DBReader.IsDBNull(3)) model.m_Netzverluste = (double)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) model.m_szNetzverlusteEinheit = DBReader.GetString(4);   
                if (!DBReader.IsDBNull(5)) model.m_WP_Heizstab = (bool)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) model.m_Kessel_Betriebsbereitschaft = (int)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) model.m_Tool_1 = DBReader.GetString(7);
                if (!DBReader.IsDBNull(8)) model.m_Tool_2 = DBReader.GetString(8);
                if (!DBReader.IsDBNull(9)) model.m_Tool_3 = DBReader.GetString(9);
                if (!DBReader.IsDBNull(10)) model.m_Tool_4 = DBReader.GetString(10);
                if (!DBReader.IsDBNull(11)) model.m_Tool_5 = DBReader.GetString(11);
                if (!DBReader.IsDBNull(12)) model.m_Tool_6 = DBReader.GetString(12);
                if (!DBReader.IsDBNull(13)) model.m_Ladefuellstand_Min = (int)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) model.m_Ladefuellstand_Max = (int)DBReader.GetValue(14);
                if (!DBReader.IsDBNull(15)) model.m_Ladeleistung_Max = (int)DBReader.GetValue(15);
                if (!DBReader.IsDBNull(16)) model.m_Ladefuellstand_Min_Auswahl = DBReader.GetString(16);
                if (!DBReader.IsDBNull(17)) model.m_Ladefuellstand_Max_Auswahl = DBReader.GetString(17);
                if (!DBReader.IsDBNull(18)) model.m_Ladeleistung_Max_Auswahl = DBReader.GetString(18);
                if (!DBReader.IsDBNull(19)) model.m_Ladeschwellwert = (double)DBReader.GetValue(19);
                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public bool Insert(int ID_Projekt)
        {
            try
            {
                string sql = FormattableString.Invariant($@"
                    INSERT INTO TAB_Einstellungen 
                    (
                        ID_Projekt, BHKW_Grenzleistung, Netzverluste, NetzverlusteEinheit, 
                        WP_Heizstab, Kessel_Betriebsbereitschaft, 
                        Tool_1, Tool_2, Tool_3, Tool_4, Tool_5, Tool_6,
                        Ladefuellstand_Min, Ladefuellstand_Max, Ladeleistung_Max,
                        Ladefuellstand_Min_Auswahl, Ladefuellstand_Max_Auswahl, 
                        Ladeleistung_Max_Auswahl, Ladeschwellwert
                    ) 
                    SELECT 
                        {ID_Projekt}, 
                        {model.m_BHKW_Grenzleistung}, 
                        {model.m_Netzverluste}, 
                        '{model.m_szNetzverlusteEinheit}', 
                        {model.m_WP_Heizstab}, 
                        {model.m_Kessel_Betriebsbereitschaft}, 
                        '{model.m_Tool_1}', '{model.m_Tool_2}', '{model.m_Tool_3}', 
                        '{model.m_Tool_4}', '{model.m_Tool_5}', '{model.m_Tool_6}',
                        {model.m_Ladefuellstand_Min}, 
                        {model.m_Ladefuellstand_Max}, 
                        {model.m_Ladeleistung_Max}, 
                        '{model.m_Ladefuellstand_Min_Auswahl}', 
                        '{model.m_Ladefuellstand_Max_Auswahl}', 
                        '{model.m_Ladeleistung_Max_Auswahl}', 
                        {model.m_Ladeschwellwert}");

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
                MessageBox.Show("Allgemeiner Fehler: " + ex.Message);
                return false;
            }
            return true;
        }

        public bool Delete(int ID_Projekt)
        {
            try
            {
                DBCommand.CommandText = "DELETE * FROM Tab_Einstellungen where ID_Projekt=" + ID_Projekt;
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
