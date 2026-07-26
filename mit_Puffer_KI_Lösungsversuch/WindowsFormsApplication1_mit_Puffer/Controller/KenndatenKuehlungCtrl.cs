using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class KenndatenKuehlungCtrl : KenndatenKuehlungModel
    {
        // --- Kompatibilitäts-Layer ---
        private List<KenndatenKuehlungModel> _internalList = new List<KenndatenKuehlungModel>();

        public int rows => _internalList.Count;
        public new List<KenndatenKuehlungModel> items => _internalList;

        public KenndatenKuehlungModel model;

        public KenndatenKuehlungCtrl()
        {
            model = new KenndatenKuehlungModel();
        }

        #region --- DATABASE READ OPERATIONS ---

        public void ReadAll(int ID_WP = 0)
        {
            string sql = "SELECT * FROM Tab_Kenndaten_Kuehlung";
            if (ID_WP > 0)
                sql += $" WHERE ID_WP = {ID_WP}";

            sql += " ORDER BY ID_WP";

            ExecuteRead(sql);
        }

        public void ReadSingle(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                // Eigenschaften des Controllers selbst setzen (für Kompatibilität)
                this.m_ID = row[0] != DBNull.Value ? Convert.ToInt32(row[0]) : 0;
                this.m_ID_WP = row[1] != DBNull.Value ? Convert.ToInt32(row[1]) : 0;
                this.m_nVorlauf = row[2] != DBNull.Value ? Convert.ToInt32(row[2]) : 0;
                this.m_nTemperatur = row[3] != DBNull.Value ? Convert.ToInt32(row[3]) : 0;
                this.m_nCOP = row[4] != DBNull.Value ? Convert.ToDouble(row[4]) : 0;
                this.m_nPkuehl = row[5] != DBNull.Value ? Convert.ToDouble(row[5]) : 0;

                // Auch in die Liste für 'rows = 1'
                _internalList.Add(this);
            }
        }

        public void ReadVorlauf(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();

            foreach (DataRow row in dt.Rows)
            {
                KenndatenKuehlungModel item = new KenndatenKuehlungModel();
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
                KenndatenKuehlungModel item = new KenndatenKuehlungModel();
                item.m_ID = row[0] != DBNull.Value ? Convert.ToInt32(row[0]) : 0;
                item.m_ID_WP = row[1] != DBNull.Value ? Convert.ToInt32(row[1]) : 0;
                item.m_nVorlauf = row[2] != DBNull.Value ? Convert.ToInt32(row[2]) : 0;
                item.m_nTemperatur = row[3] != DBNull.Value ? Convert.ToInt32(row[3]) : 0;
                item.m_nCOP = row[4] != DBNull.Value ? Convert.ToDouble(row[4]) : 0;
                item.m_nPkuehl = row[5] != DBNull.Value ? Convert.ToDouble(row[5]) : 0;
                _internalList.Add(item);
            }
        }

        #endregion

        #region --- DATABASE WRITE OPERATIONS ---

        public bool Delete()
        {
            // Korrektur: Standard DELETE Syntax
            string sql = $"DELETE FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = {m_ID_WP}";
            return DataRepository.ExecuteSQL(sql);
        }

        public bool Insert()
        {
            try
            {
                // ID-Ermittlung
                object result = DataRepository.ExecuteScalar("SELECT Max(ID) FROM Tab_Kenndaten_Kuehlung");
                m_ID = (result == DBNull.Value) ? 1 : Convert.ToInt32(result) + 1;

                // Insert mit InvariantCulture
                string sql = FormattableString.Invariant($@"
                    INSERT INTO Tab_Kenndaten_Kuehlung (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, Last) 
                    VALUES ({m_ID}, {m_ID_WP}, {m_nVorlauf}, {m_nTemperatur}, {m_nCOP}, {m_nPkuehl}, {m_nLast})");

                return DataRepository.ExecuteSQL(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei Insert (Kühlung): " + ex.Message);
                return false;
            }
        }

        public bool Update()
        {
            // Korrektur: UPDATE benötigt eine WHERE Klausel (normalerweise auf die ID)
            string sql = FormattableString.Invariant($@"
                UPDATE Tab_Kenndaten_Kuehlung 
                SET ID_WP = {m_ID_WP}, 
                    Vorlauf = {m_nVorlauf}, 
                    Temperatur = {m_nTemperatur}, 
                    COP = {m_nCOP}, 
                    Pkuehl = {m_nPkuehl}
                WHERE ID = {m_ID}");

            return DataRepository.ExecuteSQL(sql);
        }

        #endregion
    }
}
