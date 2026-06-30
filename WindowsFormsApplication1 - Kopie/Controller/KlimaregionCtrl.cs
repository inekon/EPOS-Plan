using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class KlimaregionCtrl : KlimaregionModel
    {
        // --- Kompatibilitäts-Layer ---
        private List<KlimaregionModel> _internalList = new List<KlimaregionModel>();
        public new int rows => _internalList.Count;
        public new List<KlimaregionModel> items => _internalList;

        public KlimaregionModel klimaregionmodel = new KlimaregionModel();

        public KlimaregionCtrl()
        {
            m_ID_Klimaregion = 0;
            m_szName = "";
            Longitude = 0;
            Latitude = 0;
            Details = "";
        }

        #region --- READ OPERATIONS ---

        public void ReadAll()
        {
            string sql = "SELECT * FROM Tab_Klimaregion ORDER BY ID_Klimaregion";
            ExecuteRead(sql);
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

        private void ExecuteRead(string sql, params OleDbParameter[] parameters)
        {
            DataTable dt = DataRepository.GetDataTable(sql, parameters);
            _internalList.Clear();

            foreach (DataRow row in dt.Rows)
            {
                KlimaregionModel item = new KlimaregionModel();

                // Zuweisung über Spaltennamen – passend zu deinem KlimaregionModel aufgebaut:
                item.m_ID_Klimaregion = row["ID_Klimaregion"] != DBNull.Value ? Convert.ToInt32(row["ID_Klimaregion"]) : 0;
                item.m_szName = row["Name"] != DBNull.Value ? row["Name"].ToString() : "";
                item.Longitude = row["Longitude"] != DBNull.Value ? Convert.ToDouble(row["Longitude"]) : 0;
                item.Latitude = row["Latitude"] != DBNull.Value ? Convert.ToDouble(row["Latitude"]) : 0;
                item.Details = row["Details"] != DBNull.Value ? row["Details"].ToString() : "";

                _internalList.Add(item);
            }
        }

        private void MapRowToThis(DataRow row)
        {
            // Zuweisung an die "this"-Instanz über Spaltennamen:
            this.m_ID_Klimaregion = row["ID_Klimaregion"] != DBNull.Value ? Convert.ToInt32(row["ID_Klimaregion"]) : 0;
            this.m_szName = row["Name"] != DBNull.Value ? row["Name"].ToString() : "";
            this.Longitude = row["Longitude"] != DBNull.Value ? Convert.ToDouble(row["Longitude"]) : 0;
            this.Latitude = row["Latitude"] != DBNull.Value ? Convert.ToDouble(row["Latitude"]) : 0;
            this.Details = row["Details"] != DBNull.Value ? row["Details"].ToString() : "";
        }

        #endregion

        #region --- WRITE OPERATIONS ---

        public bool Add(string szName, double Longitude, double Latitude, string Details, OleDbConnection conn, OleDbTransaction trans)
        {
            // 1. Das SQL ohne das ID-Feld, da Access dieses nun als Autowert selbst befüllt!
            string sql = "INSERT INTO Tab_Klimaregion (Name, Longitude, Latitude, Details) VALUES (?, ?, ?, ?)";

            using (OleDbCommand cmd = new OleDbCommand(sql, conn, trans))
            {
                // WICHTIG: Die Reihenfolge der Parameter MUSS exakt mit dem SQL übereinstimmen!
                cmd.Parameters.Add(new OleDbParameter("?", string.IsNullOrEmpty(szName) ? (object)DBNull.Value : szName));
                cmd.Parameters.Add(new OleDbParameter("?", Longitude));
                cmd.Parameters.Add(new OleDbParameter("?", Latitude));
                cmd.Parameters.Add(new OleDbParameter("?", string.IsNullOrEmpty(Details) ? (object)DBNull.Value : Details));

                cmd.ExecuteNonQuery();
            }

            // 2. Da es ein Autowert ist, fragen wir Access nach der ID, 
            // die gerade eben automatisch für diesen Datensatz generiert wurde:
            using (OleDbCommand cmdIdentity = new OleDbCommand("SELECT @@IDENTITY", conn, trans))
            {
                object result = cmdIdentity.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    m_ID_Klimaregion = Convert.ToInt32(result);
                }
            }

            return true;
        }

        public bool Update()
        {
            string sql = "UPDATE Tab_Klimaregion SET Name = ?, Längengrad = ?, Breitengrad = ?, Beschreibung = ? " +
                         "WHERE ID_Klimaregion = ?";

            OleDbParameter[] parameters = {
                new OleDbParameter("?", string.IsNullOrEmpty(m_szName) ? (object)DBNull.Value : m_szName),
                new OleDbParameter("?", Longitude),
                new OleDbParameter("?", Latitude),
                new OleDbParameter("?", string.IsNullOrEmpty(Details) ? (object)DBNull.Value : Details),
                new OleDbParameter("?", m_ID_Klimaregion) // WHERE-Bedingung
            };

            return DataRepository.ExecuteSQL(sql, parameters);
        }

        public bool Delete(string szName)
        {
            string sql = "DELETE FROM Tab_Klimaregion WHERE Name = ?";
            return DataRepository.ExecuteSQL(sql, new OleDbParameter("?", szName));
        }

        #endregion

        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(items[i].m_szName);
            }
        }

        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(items[i].m_szName);
            }
        }
    }
}