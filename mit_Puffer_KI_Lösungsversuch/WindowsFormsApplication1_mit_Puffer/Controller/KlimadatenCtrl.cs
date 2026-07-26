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
        public int rows => _internalList.Count;
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
            string sql = "SELECT * FROM Tab_Klimadaten ORDER BY ID";
            ExecuteRead(sql, false);
        }

        public void ReadAll(int ID_Klimaregion)
        {
            string sql = $"SELECT * FROM Tab_Klimadaten WHERE ID_Klimaregion={ID_Klimaregion} ORDER BY ID";
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
                // Namensbasiertes Mapping (Projekt-Tabelle Tab_Klimadaten hat zusaetzlich ID_Projekt).
                item.m_ID_Klimadaten = dt.Columns.Contains("ID_Klimadaten") && row["ID_Klimadaten"] != DBNull.Value ? Convert.ToInt32(row["ID_Klimadaten"])
                                        : (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0);
                item.m_ID_Klimaregion = row["ID_Klimaregion"] != DBNull.Value ? Convert.ToInt32(row["ID_Klimaregion"]) : 0;
                item.m_Sol_Nord = row["Sol_Nord"] != DBNull.Value ? Convert.ToDouble(row["Sol_Nord"]) : 0;
                item.m_Sol_Ost = row["Sol_Ost"] != DBNull.Value ? Convert.ToDouble(row["Sol_Ost"]) : 0;
                item.m_Sol_Sued = row["Sol_Sued"] != DBNull.Value ? Convert.ToDouble(row["Sol_Sued"]) : 0;
                item.m_Sol_West = row["Sol_West"] != DBNull.Value ? Convert.ToDouble(row["Sol_West"]) : 0;
                item.m_nTemperatur = row["Temperatur"] != DBNull.Value ? Convert.ToDouble(row["Temperatur"]) : 0;
                item.m_WE = row["WE"] != DBNull.Value ? Convert.ToBoolean(row["WE"]) : false;
                item.m_TagTyp_W = row["TagTyp_W"] != DBNull.Value ? Convert.ToDouble(row["TagTyp_W"]) : 0;
                item.m_TagTyp_NW = row["TagTyp_NW"] != DBNull.Value ? Convert.ToDouble(row["TagTyp_NW"]) : 0;
                if (dt.Columns.Contains("Globalstrahlung"))
                    item.m_Globalstrahlung = row["Globalstrahlung"] != DBNull.Value ? Convert.ToDouble(row["Globalstrahlung"]) : 0;

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
            this.m_ID_Klimadaten = row.Table.Columns.Contains("ID_Klimadaten") && row["ID_Klimadaten"] != DBNull.Value ? Convert.ToInt32(row["ID_Klimadaten"])
                                    : (row.Table.Columns.Contains("ID") && row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0);
            this.m_ID_Klimaregion = row["ID_Klimaregion"] != DBNull.Value ? Convert.ToInt32(row["ID_Klimaregion"]) : 0;
            this.m_Sol_Nord = row["Sol_Nord"] != DBNull.Value ? Convert.ToDouble(row["Sol_Nord"]) : 0;
            this.m_Sol_Ost = row["Sol_Ost"] != DBNull.Value ? Convert.ToDouble(row["Sol_Ost"]) : 0;
            this.m_Sol_Sued = row["Sol_Sued"] != DBNull.Value ? Convert.ToDouble(row["Sol_Sued"]) : 0;
            this.m_Sol_West = row["Sol_West"] != DBNull.Value ? Convert.ToDouble(row["Sol_West"]) : 0;
            this.m_nTemperatur = row["Temperatur"] != DBNull.Value ? Convert.ToDouble(row["Temperatur"]) : 0;
            this.m_WE = row["WE"] != DBNull.Value ? Convert.ToBoolean(row["WE"]) : false;
            this.m_TagTyp_W = row["TagTyp_W"] != DBNull.Value ? Convert.ToDouble(row["TagTyp_W"]) : 0;
            this.m_TagTyp_NW = row["TagTyp_NW"] != DBNull.Value ? Convert.ToDouble(row["TagTyp_NW"]) : 0;
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
                // 1. ID_Klimaregion ermitteln (Nutzt jetzt die übergebene Verbindung/Transaktion!)
                string regSql = "SELECT ID_Klimaregion FROM Tab_Klimaregion WHERE Name = ?";
                int id_ref;

                using (OleDbCommand cmdReg = new OleDbCommand(regSql, transaction.Connection, transaction))
                {
                    cmdReg.Parameters.Add(new OleDbParameter("?", szName ?? (object)DBNull.Value));
                    object regId = cmdReg.ExecuteScalar();
                    if (regId == DBNull.Value || regId == null) return false;
                    id_ref = Convert.ToInt32(regId);
                }

                // 2. Adapter Setup & zwingend an die bestehende Transaktion koppeln!
                OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT * FROM Tab_Klimadaten WHERE 1=0", transaction.Connection);
                OleDbCommandBuilder cb = new OleDbCommandBuilder(adapter);

                // Das sorgt dafür, dass die generierten INSERT-Befehle die Transaktion nutzen:
                adapter.SelectCommand.Transaction = transaction;

                // 3. Spalten für die Verarbeitung vorbereiten
                // WICHTIG: 'ID_Klimadaten' fügen wir NICHT manuell hinzu, das macht Access per Autowert!
                if (!dt.Columns.Contains("ID_Klimaregion"))
                {
                    dt.Columns.Add("ID_Klimaregion", typeof(int)).SetOrdinal(0);
                }

                // 4. Nur noch die Fremdschlüssel-ID eintragen
                foreach (DataRow row in dt.Rows)
                {
                    row["ID_Klimaregion"] = id_ref;
                }

                // Spaltenmapping (Bleibt genau so, wie du es hattest)
                dt.Columns[1].ColumnName = "Sol_Nord"; dt.Columns[2].ColumnName = "Sol_Ost";
                dt.Columns[3].ColumnName = "Sol_Sued"; dt.Columns[4].ColumnName = "Sol_West";
                dt.Columns[5].ColumnName = "Temperatur"; dt.Columns[6].ColumnName = "WE";
                dt.Columns[7].ColumnName = "Tagtyp_W"; dt.Columns[8].ColumnName = "Tagtyp_NW";
                // Hinweis: Die Indizes [1] bis [8] verschieben sich um eins nach vorne, 
                // weil wir die 'ID_Klimadaten'-Spalte links nicht mehr künstlich einfügen!

                // 5. Daten über den Adapter in die DB jagen
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