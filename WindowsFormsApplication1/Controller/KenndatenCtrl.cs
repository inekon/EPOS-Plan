using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class KenndatenCtrl : KenndatenModel
    {
        // --- Kompatibilitäts-Layer ---
        private List<KenndatenModel> _internalList = new List<KenndatenModel>();

        public int rows => _internalList.Count;
        public new List<KenndatenModel> items => _internalList;

        public KenndatenModel model;

        public KenndatenCtrl()
        {
            model = new KenndatenModel();
        }

        #region --- DATABASE READ OPERATIONS ---

        public void ReadAll()
        {
            string sql = "SELECT * FROM Tab_Kenndaten ORDER BY ID_WP";
            ExecuteRead(sql);
        }

        public void ReadVorlauf(string sql)
        {
            // Spezielle Read-Logik für Vorlauf-Abfragen
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();

            foreach (DataRow row in dt.Rows)
            {
                KenndatenModel item = new KenndatenModel();
                item.m_nVorlauf = row[0] != DBNull.Value ? Convert.ToInt32(row[0]) : 0;
                item.m_ID_WP = row[1] != DBNull.Value ? Convert.ToInt32(row[1]) : 0;
                _internalList.Add(item);
            }
        }

        private void ExecuteRead(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();

            foreach (DataRow row in dt.Rows)
            {
                KenndatenModel item = new KenndatenModel();
                item.m_ID = row[0] != DBNull.Value ? Convert.ToInt32(row[0]) : 0;
                item.m_ID_WP = row[1] != DBNull.Value ? Convert.ToInt32(row[1]) : 0;
                item.m_nVorlauf = row[2] != DBNull.Value ? Convert.ToInt32(row[2]) : 0;
                item.m_nTemperatur = row[3] != DBNull.Value ? Convert.ToInt32(row[3]) : 0;
                item.m_nCOP = row[4] != DBNull.Value ? Convert.ToDouble(row[4]) : 0;
                item.m_nPTherm = row[5] != DBNull.Value ? Convert.ToDouble(row[5]) : 0;
                _internalList.Add(item);
            }
        }

        #endregion

        #region --- DATABASE WRITE OPERATIONS ---

        public bool Delete()
        {
            // Korrektur: Das ursprüngliche SQL "DELETE WPName FROM..." war syntaktisch oft problematisch in Access
            string sql = $"DELETE FROM Tab_Kenndaten WHERE ID_WP = {m_ID_WP}";
            return DataRepository.ExecuteSQL(sql);
        }

        public bool Insert()
        {
            try
            {
                // ID-Ermittlung
                object result = DataRepository.ExecuteScalar("SELECT Max(ID) FROM Tab_Kenndaten");
                m_ID = (result == DBNull.Value) ? 1 : Convert.ToInt32(result) + 1;

                // Insert mit InvariantCulture für korrekte Dezimalpunkte (COP/Ptherm)
                string sql = FormattableString.Invariant($@"
                    INSERT INTO Tab_Kenndaten (ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm) 
                    VALUES ({m_ID}, {m_ID_WP}, {m_nVorlauf}, {m_nTemperatur}, {m_nCOP}, {m_nPTherm})");

                return DataRepository.ExecuteSQL(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei Insert: " + ex.Message);
                return false;
            }
        }

        public bool Update()
        {
            // Korrektur der Anführungszeichen und Logik aus dem Original
            string sql = FormattableString.Invariant($@"
                UPDATE Tab_Kenndaten 
                SET ID_WP={m_ID_WP}, Vorlauf={m_nVorlauf}, Temperatur={m_nTemperatur}, 
                    COP={m_nCOP}, Ptherm={m_nPTherm} 
                WHERE ID={m_ID}");

            return DataRepository.ExecuteSQL(sql);
        }

        #endregion
    }
}