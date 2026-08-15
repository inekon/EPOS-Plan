using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class StromverbraucherCtrl : StromverbraucherModel
    {
        private List<StromverbraucherModel> _internalList = new List<StromverbraucherModel>();
        public int rows => _internalList.Count;
        public new List<StromverbraucherModel> items => _internalList;

        public StromverbraucherCtrl()
        {
        }

        public void ReadAll()
        {
            string sql = "SELECT * FROM Tab_Stromverbraucher ORDER BY Bezeichner";
            DataTable dt = DataRepository.GetDataTable(sql, null);

            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                StromverbraucherModel item = new StromverbraucherModel();

                // Basisdaten sicher auslesen
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    item.m_ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["Bezeichner"].ToString();

                if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value)
                    item.m_szTyp = row["Typ"].ToString();

                if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                    item.m_szBeschreibung = row["Beschreibung"].ToString();

                // Die 12 Monate dynamisch und namensbasiert auslesen
                for (int i = 0; i < 12; i++)
                {
                    // Erzeugt Spaltennamen wie "Monat1", "Monat2" ... passend zur Access-Tabelle
                    string colName = "Monat" + (i + 1);

                    if (dt.Columns.Contains(colName) && row[colName] != DBNull.Value)
                    {
                        item.m_Monat[i] = Convert.ToDouble(row[colName]);
                    }
                    else if (dt.Columns.Count > (i + 4) && row[i + 4] != DBNull.Value)
                    {
                        // Fallback auf den alten Index-basierten Zugriff, falls die Spalten nicht "MonatX" heißen
                        item.m_Monat[i] = Convert.ToDouble(row[i + 4]);
                    }
                }

                _internalList.Add(item);
            }
        }

        public void ReadSingle(int ID_Stromverbraucher)
        {
            string sql = "SELECT * FROM Tab_Stromverbraucher WHERE ID = ?";

            OleDbParameter paramId = new OleDbParameter("@id", OleDbType.Integer);
            paramId.Value = ID_Stromverbraucher;
            OleDbParameter[] ps = { paramId };

            DataTable dt = DataRepository.GetDataTable(sql, ps);

            // Instanzdaten zurücksetzen für den Fall, dass nichts gefunden wird
            m_ID = 0;
            m_szBezeichner = string.Empty;
            m_szTyp = string.Empty;
            m_szBeschreibung = string.Empty;

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    m_ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    m_szBezeichner = row["Bezeichner"].ToString();

                if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value)
                    m_szTyp = row["Typ"].ToString();

                if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                    m_szBeschreibung = row["Beschreibung"].ToString();

                // Die 12 Monate namensbasiert befüllen
                for (int i = 0; i < 12; i++)
                {
                    string colName = "Monat" + (i + 1);
                    if (dt.Columns.Contains(colName) && row[colName] != DBNull.Value)
                    {
                        m_Monat[i] = Convert.ToDouble(row[colName]);
                    }
                    else if (dt.Columns.Count > (i + 5) && row[i + 5] != DBNull.Value)
                    {
                        m_Monat[i] = Convert.ToDouble(row[i + 5]);
                    }
                }

                // Um die alte Logik (rows = 1) für die UI kompatibel zu halten, 
                // wird hier temporär die interne Liste manipuliert:
                _internalList.Clear();
                _internalList.Add(this);
            }
            else
            {
                _internalList.Clear();
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            string sql = "SELECT * FROM Tab_Stromverbraucher WHERE Bezeichner = ?";

            OleDbParameter paramBez = new OleDbParameter("@bez", OleDbType.VarWChar);
            paramBez.Value = szBezeichner ?? (object)DBNull.Value;
            OleDbParameter[] ps = { paramBez };

            DataTable dt = DataRepository.GetDataTable(sql, ps);

            // Instanzdaten zurücksetzen
            m_ID = 0;
            m_szBezeichner = string.Empty;
            m_szTyp = string.Empty;
            m_szBeschreibung = string.Empty;

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    m_ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    m_szBezeichner = row["Bezeichner"].ToString();

                if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value)
                    m_szTyp = row["Typ"].ToString();

                if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                    m_szBeschreibung = row["Beschreibung"].ToString();

                // Die 12 Monate namensbasiert befüllen
                for (int i = 0; i < 12; i++)
                {
                    string colName = "Monat" + (i + 1);
                    if (dt.Columns.Contains(colName) && row[colName] != DBNull.Value)
                    {
                        m_Monat[i] = Convert.ToDouble(row[colName]);
                    }
                    else if (dt.Columns.Count > (i + 4) && row[i + 4] != DBNull.Value)
                    {
                        m_Monat[i] = Convert.ToDouble(row[i + 4]);
                    }
                }

                _internalList.Clear();
                _internalList.Add(this);
            }
            else
            {
                _internalList.Clear();
            }
        }
    }
}