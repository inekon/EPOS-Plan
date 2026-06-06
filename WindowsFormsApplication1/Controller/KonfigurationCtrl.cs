using System;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class KonfigurationCtrl : KonfigurationModel
    {
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
            rows = 0;
        }

        ~KonfigurationCtrl()
        {
            rows = 0;
        }

        public void ReadSingle(string sql)
        {
            rows = 0;

            // Nutzt dein DataRepository (intern OLEDB) statt ODBC
            DataTable dt = DataRepository.GetDataTable(sql);

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (row[0] != DBNull.Value) model.m_ID = Convert.ToInt32(row[0]);
                if (row[1] != DBNull.Value) model.m_ID_Projekt = Convert.ToInt32(row[1]);
                if (row[2] != DBNull.Value) model.m_BHKW_Grenzleistung = Convert.ToDouble(row[2]);
                if (row[3] != DBNull.Value) model.m_Netzverluste = Convert.ToDouble(row[3]);
                if (row[4] != DBNull.Value) model.m_szNetzverlusteEinheit = row[4].ToString();
                if (row[5] != DBNull.Value) model.m_WP_Heizstab = Convert.ToBoolean(row[5]);
                if (row[6] != DBNull.Value) model.m_Kessel_Betriebsbereitschaft = Convert.ToInt32(row[6]);
                if (row[7] != DBNull.Value) model.m_Tool_1 = row[7].ToString();
                if (row[8] != DBNull.Value) model.m_Tool_2 = row[8].ToString();
                if (row[9] != DBNull.Value) model.m_Tool_3 = row[9].ToString();
                if (row[10] != DBNull.Value) model.m_Tool_4 = row[10].ToString();
                if (row[11] != DBNull.Value) model.m_Tool_5 = row[11].ToString();
                if (row[12] != DBNull.Value) model.m_Tool_6 = row[12].ToString();
                if (row[13] != DBNull.Value) model.m_Ladefuellstand_Min = Convert.ToInt32(row[13]);
                if (row[14] != DBNull.Value) model.m_Ladefuellstand_Max = Convert.ToInt32(row[14]);
                if (row[15] != DBNull.Value) model.m_Ladeleistung_Max = Convert.ToInt32(row[15]);
                if (row[16] != DBNull.Value) model.m_Ladefuellstand_Min_Auswahl = row[16].ToString();
                if (row[17] != DBNull.Value) model.m_Ladefuellstand_Max_Auswahl = row[17].ToString();
                if (row[18] != DBNull.Value) model.m_Ladeleistung_Max_Auswahl = row[18].ToString();
                if (row[19] != DBNull.Value) model.m_Ladeschwellwert = Convert.ToDouble(row[19]);

                rows = 1;
            }
        }

        public bool Insert(int ID_Projekt)
        {
            try
            {
                // Umstellung auf sichere Parameter-Marker (?) statt ungesicherter String-Verkettung
                string sql = @"
                    INSERT INTO TAB_Einstellungen 
                    (
                        ID_Projekt, BHKW_Grenzleistung, Netzverluste, NetzverlusteEinheit, 
                        WP_Heizstab, Kessel_Betriebsbereitschaft, 
                        Tool_1, Tool_2, Tool_3, Tool_4, Tool_5, Tool_6,
                        Ladefuellstand_Min, Ladefuellstand_Max, Ladeleistung_Max,
                        Ladefuellstand_Min_Auswahl, Ladefuellstand_Max_Auswahl, 
                        Ladeleistung_Max_Auswahl, Ladeschwellwert
                    ) 
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                // Die Parameter werden als OLEDB-Objekte an dein DataRepository gereicht
                OleDbParameter[] parameters = new OleDbParameter[]
                {
                    new OleDbParameter("?", ID_Projekt),
                    new OleDbParameter("?", model.m_BHKW_Grenzleistung),
                    new OleDbParameter("?", model.m_Netzverluste),
                    new OleDbParameter("?", model.m_szNetzverlusteEinheit ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_WP_Heizstab),
                    new OleDbParameter("?", model.m_Kessel_Betriebsbereitschaft),
                    new OleDbParameter("?", model.m_Tool_1 ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Tool_2 ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Tool_3 ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Tool_4 ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Tool_5 ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Tool_6 ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Ladefuellstand_Min),
                    new OleDbParameter("?", model.m_Ladefuellstand_Max),
                    new OleDbParameter("?", model.m_Ladeleistung_Max),
                    new OleDbParameter("?", model.m_Ladefuellstand_Min_Auswahl ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Ladefuellstand_Max_Auswahl ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Ladeleistung_Max_Auswahl ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Ladeschwellwert)
                };

                // Übergabe an das DataRepository
                DataRepository.ExecuteNonQuery(sql, parameters);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Einfügen der Konfiguration: " + ex.Message);
                MessageBox.Show("Allgemeiner Fehler: " + ex.Message);
                return false;
            }
        }

        public bool Delete(int ID_Projekt)
        {
            try
            {
                // Sauberes ANSI-SQL für OLEDB ohne das ungültige "DELETE *"
                string sql = "DELETE FROM Tab_Einstellungen WHERE ID_Projekt = ?";
                OleDbParameter parameter = new OleDbParameter("?", ID_Projekt);

                DataRepository.ExecuteNonQuery(sql, parameter);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Löschen der Konfiguration: " + ex.Message);
                return false;
            }
        }
    }
}
