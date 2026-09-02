using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class SolarganglinieCtrl : SolarganglinieModel
    {
        // Das besprochene dynamische Listen-Schema
        private List<SolarganglinieModel> _internalList = new List<SolarganglinieModel>();

        public int rows => _internalList.Count;
        public new List<SolarganglinieModel> items => _internalList;

        public int max_id = 0;

        public SolarganglinieCtrl()
        {
            // Konstruktor bereinigt - Ressourcen werden vom DataRepository verwaltet
        }

        public bool Delete(string szName)
        {
            try
            {
                // Parametrisierte Abfrage ohne fehleranfällige Stringverkettungen
                string sql = "DELETE FROM Tab_Solarganglinie WHERE Bezeichner = ?";

                DbParam paramBez = new DbParam("@bez", DbParamTyp.VarWChar);
                paramBez.Wert = szName ?? (object)DBNull.Value;

                DbParam[] ps = { paramBez };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Delete: " + ex.Message);
                return false;
            }
        }

        public bool Insert()
        {
            try
            {
                // Ermittlung der nächsten ID über das Repository (Ersatz für sequenzielle Reader)
                string sqlCount = "SELECT COUNT(*) FROM Tab_Solarganglinie";
                object countResult = DataRepository.ExecuteScalar(sqlCount, null);
                int count = countResult != null ? Convert.ToInt32(countResult) : 0;

                if (count == 0)
                {
                    m_ID_Ganglinie = 1;
                }
                else
                {
                    string sqlMax = "SELECT MAX(ID) FROM Tab_Solarganglinie";
                    object maxResult = DataRepository.ExecuteScalar(sqlMax, null);
                    m_ID_Ganglinie = (maxResult != null ? Convert.ToInt32(maxResult) : 0) + 1;
                }

                // Umstellung auf das standardkonforme und sichere VALUES-Statement
                string sql = "INSERT INTO Tab_Solarganglinie (ID, Bezeichner, Beschreibung) VALUES (?, ?, ?)";

                DbParam paramId = new DbParam("@id", DbParamTyp.Integer);
                paramId.Wert = m_ID_Ganglinie;

                DbParam paramBez = new DbParam("@bez", DbParamTyp.VarWChar);
                paramBez.Wert = m_szBezeichner ?? (object)DBNull.Value;

                DbParam paramBeschr = new DbParam("@beschr", DbParamTyp.VarWChar);
                paramBeschr.Wert = m_szBeschreibung ?? (object)DBNull.Value;

                DbParam[] ps = { paramId, paramBez, paramBeschr };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Insert: " + ex.Message);
                return false;
            }
        }

        public void ReadAll()
        {
            string sql = "SELECT * FROM Tab_Solarganglinie ORDER BY Bezeichner";
            DataTable dt = DataRepository.GetDataTable(sql, null);

            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                SolarganglinieModel item = new SolarganglinieModel();

                // Spaltenbasiertes, sicheres Auslesen über Spaltennamen
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    item.ID = Convert.ToInt32(row["ID"]);

                // Die Ganglinien-ID ist im aktuellen Schema der Primärschlüssel der Kopftabelle
                item.m_ID_Ganglinie = item.ID;

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["Bezeichner"].ToString();

                if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                    item.m_szBeschreibung = row["Beschreibung"].ToString();

                _internalList.Add(item);
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            string sql = "SELECT * FROM Tab_Solarganglinie WHERE Bezeichner = ?";

            DbParam paramBez = new DbParam("@bez", DbParamTyp.VarWChar);
            paramBez.Wert = szBezeichner ?? (object)DBNull.Value;

            DbParam[] ps = { paramBez };

            DataTable dt = DataRepository.GetDataTable(sql, ps);

            // Instanzdaten vorsorglich bereinigen, falls kein Treffer erzielt wird
            ID = 0;
            m_ID_Ganglinie = 0;
            m_szBezeichner = string.Empty;
            m_szBeschreibung = string.Empty;

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    ID = Convert.ToInt32(row["ID"]);

                // Die Ganglinien-ID ist im aktuellen Schema der Primärschlüssel der Kopftabelle
                m_ID_Ganglinie = ID;

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    m_szBezeichner = row["Bezeichner"].ToString();

                if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                    m_szBeschreibung = row["Beschreibung"].ToString();

                // Listensynchronisation zur Erhaltung der UI-Kompatibilität (rows = 1)
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