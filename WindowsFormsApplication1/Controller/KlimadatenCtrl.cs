using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class KlimadatenCtrl : KlimadatenModel
    {
        // --- Kompatibilitäts-Layer ---
        private List<KlimadatenModel> _internalList = new List<KlimadatenModel>();
        public new int rows => _internalList.Count;
        public new List<KlimadatenModel> items => _internalList;

        public KlimadatenModel klimamodel = new KlimadatenModel();
        public List<double> list_Temperatur = new List<double>();
        public List<int> list_Tag = new List<int>();
        public string Klimazone;

        public KlimadatenCtrl()
        {
            Klimazone = "";
            m_ID_Klimaregion = 0;
        }

        #region --- READ OPERATIONS ---

        public void ReadAll()
        {
            string sql = "SELECT * FROM Tab_Klimadaten ORDER BY ID_Klimadaten";
            ExecuteRead(sql, false);
        }

        public void ReadAll(int ID_Klimaregion)
        {
            string sql = $"SELECT * FROM Tab_Klimadaten WHERE ID_Klimaregion={ID_Klimaregion} ORDER BY ID_Klimadaten";
            ExecuteRead(sql, true); // True füllt auch die Hilfslisten für Charts/Berechnungen
        }

        public void ReadSingle(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                MapRowToThis(row);
                _internalList.Add(this);
            }
        }

        private void ExecuteRead(string sql, bool fillHelperLists)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();
            if (fillHelperLists)
            {
                list_Temperatur.Clear();
                list_Tag.Clear();
            }

            int count = 0;
            foreach (DataRow row in dt.Rows)
            {
                KlimadatenModel item = new KlimadatenModel();
                item.m_ID_Klimadaten = row[0] != DBNull.Value ? Convert.ToInt32(row[0]) : 0;
                item.m_ID_Klimaregion = row[1] != DBNull.Value ? Convert.ToInt32(row[1]) : 0;
                item.m_Sol_Nord = row[2] != DBNull.Value ? Convert.ToDouble(row[2]) : 0;
                item.m_Sol_Ost = row[3] != DBNull.Value ? Convert.ToDouble(row[3]) : 0;
                item.m_Sol_Sued = row[4] != DBNull.Value ? Convert.ToDouble(row[4]) : 0;
                item.m_Sol_West = row[5] != DBNull.Value ? Convert.ToDouble(row[5]) : 0;
                item.m_nTemperatur = row[6] != DBNull.Value ? Convert.ToDouble(row[6]) : 0;
                item.m_WE = row[7] != DBNull.Value ? Convert.ToBoolean(row[7]) : false;
                item.m_TagTyp_W = row[8] != DBNull.Value ? Convert.ToDouble(row[8]) : 0;
                item.m_TagTyp_NW = row[9] != DBNull.Value ? Convert.ToDouble(row[9]) : 0;

                // Index 10 sicherheitshalber prüfen (Globalstrahlung)
                if (dt.Columns.Count > 10)
                    item.m_Globalstrahlung = row[10] != DBNull.Value ? Convert.ToDouble(row[10]) : 0;

                _internalList.Add(item);

                if (fillHelperLists)
                {
                    list_Temperatur.Add(item.m_nTemperatur);
                    list_Tag.Add(++count);
                }
            }
        }

        private void MapRowToThis(DataRow row)
        {
            this.m_ID_Klimadaten = row[0] != DBNull.Value ? Convert.ToInt32(row[0]) : 0;
            this.m_ID_Klimaregion = row[1] != DBNull.Value ? Convert.ToInt32(row[1]) : 0;
            this.m_Sol_Nord = row[2] != DBNull.Value ? Convert.ToDouble(row[2]) : 0;
            this.m_Sol_Ost = row[3] != DBNull.Value ? Convert.ToDouble(row[3]) : 0;
            this.m_Sol_Sued = row[4] != DBNull.Value ? Convert.ToDouble(row[4]) : 0;
            this.m_Sol_West = row[5] != DBNull.Value ? Convert.ToDouble(row[5]) : 0;
            this.m_nTemperatur = row[6] != DBNull.Value ? Convert.ToDouble(row[6]) : 0;
            this.m_WE = row[7] != DBNull.Value ? Convert.ToBoolean(row[7]) : false;
            this.m_TagTyp_W = row[8] != DBNull.Value ? Convert.ToDouble(row[8]) : 0;
            this.m_TagTyp_NW = row[9] != DBNull.Value ? Convert.ToDouble(row[9]) : 0;
        }

        #endregion

        #region --- WRITE OPERATIONS ---

        public bool Delete(string szName)
        {
            string sql = $"DELETE FROM Tab_Klimaregion WHERE Name = '{szName}'";
            return DataRepository.ExecuteSQL(sql);
        }

        // Die DataTable-Variante für hohe Performance bei Massendaten
        public bool WritetDataTable(DataTable dt, string szName, OleDbTransaction transaction)
        {
            try
            {
                // IDs ermitteln
                object maxId = DataRepository.ExecuteScalar("SELECT Max(ID_Klimadaten) FROM Tab_Klimadaten");
                int nextId = (maxId == DBNull.Value) ? 1 : Convert.ToInt32(maxId) + 1;

                object regId = DataRepository.ExecuteScalar($"SELECT ID_Klimaregion FROM Tab_Klimaregion WHERE Name='{szName}'");
                if (regId == DBNull.Value || regId == null) return false;
                int id_ref = Convert.ToInt32(regId);

                // Adapter Setup
                OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT * FROM Tab_Klimadaten", DataRepository.GetConnectionString());
                OleDbCommandBuilder cb = new OleDbCommandBuilder(adapter);

                // Spalten für die interne Verarbeitung im Gedächtnis vorbereiten
                if (!dt.Columns.Contains("ID_Klimadaten"))
                {
                    dt.Columns.Add("ID_Klimaregion", typeof(int)).SetOrdinal(0);
                    dt.Columns.Add("ID_Klimadaten", typeof(int)).SetOrdinal(0);
                }

                foreach (DataRow row in dt.Rows)
                {
                    row["ID_Klimadaten"] = nextId++;
                    row["ID_Klimaregion"] = id_ref;
                }

                // Spaltenmapping (Falls die DataTable aus einem CSV/Excel kommt und die Header nicht passen)
                dt.Columns[2].ColumnName = "Sol_Nord"; dt.Columns[3].ColumnName = "Sol_Ost";
                dt.Columns[4].ColumnName = "Sol_Sued"; dt.Columns[5].ColumnName = "Sol_West";
                dt.Columns[6].ColumnName = "Temperatur"; dt.Columns[7].ColumnName = "WE";
                dt.Columns[8].ColumnName = "Tagtyp_W"; dt.Columns[9].ColumnName = "Tagtyp_NW";

                adapter.Update(dt);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Massen-Schreiben: " + ex.Message);
                return false;
            }
        }

        #endregion
    }
}