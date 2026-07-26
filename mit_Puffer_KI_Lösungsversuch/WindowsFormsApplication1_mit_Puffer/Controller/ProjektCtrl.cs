using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class ProjektCtrl : ProjektModel
    {
        // --- Kompatibilitäts-Layer für bestehenden UI-Code ---
        private List<ProjektModel> _internalList = new List<ProjektModel>();
        private bool _hasSingleData = false;

        // Simuliert die alte 'rows' Variable
        public new int rows => _internalList.Count > 0 ? _internalList.Count : (_hasSingleData ? 1 : 0);

        // Simuliert das alte 'items' Array (als Liste, die sich wie ein Array verhält)
        public List<ProjektModel> items => _internalList;

        public ProjektCtrl()
        {
            _hasSingleData = false;
        }

        #region --- DATABASE OPERATIONS ---

        public int GetMaxID() => DataRepository.GetMaxID("Tab_Projekt", "ID");

        public bool Insert()
        {
            m_ID = GetMaxID() + 1;

            string sql = @"INSERT INTO Tab_Projekt 
                           (Projektname, Bearbeiter, Beschreibung, Kunde, Aenderungsdatum, ID_Klimaregion, Erstelldatum) 
                           VALUES (?, ?, ?, ?, ?, ?, ?)";

            OleDbParameter[] ps = {
                new OleDbParameter("@name", m_szProjektname ?? ""),
                new OleDbParameter("@bearb", m_szBearbeiter ?? ""),
                new OleDbParameter("@besch", m_szBeschreibung ?? ""),
                new OleDbParameter("@kunde", m_szKunde ?? ""),
                new OleDbParameter("@date", OleDbType.Date) { Value = ValidateDate(m_Aenderungsdatum) },
                new OleDbParameter("@klima", m_ID_Klimaregion),
                new OleDbParameter("@edate", OleDbType.Date) { Value = ValidateDate(m_Erstelldatum) }
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Update()
        {
            string sql = @"UPDATE Tab_Projekt SET 
                            Bearbeiter=?, Beschreibung=?, Kunde=?, 
                            Aenderungsdatum=?, ID_Klimaregion=?, Erstelldatum=? 
                           WHERE Projektname=?";

            OleDbParameter[] ps = {
                new OleDbParameter("@bearb", (object)m_szBearbeiter ?? ""),
                new OleDbParameter("@besch", (object)m_szBeschreibung ?? ""),
                new OleDbParameter("@kunde", (object)m_szKunde ?? ""),
                new OleDbParameter("@date", OleDbType.Date) { Value = ValidateDate(m_Aenderungsdatum) },
                new OleDbParameter("@klima", m_ID_Klimaregion),
                new OleDbParameter("@edate", OleDbType.Date) { Value = ValidateDate(m_Erstelldatum) },
                new OleDbParameter("@pname", m_szProjektname)
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Delete(string szProjekt)
        {
            string sql = "DELETE FROM Tab_Projekt WHERE Projektname=?";
            OleDbParameter[] ps = { new OleDbParameter("@pname", szProjekt) };
            return DataRepository.ExecuteSQL(sql, ps);
        }

        public void ReadAll()
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM Tab_Projekt ORDER BY Projektname");
            _internalList.Clear();
            _hasSingleData = false;

            foreach (DataRow row in dt.Rows)
            {
                _internalList.Add(MapRowToModel(row));
            }
        }

        public void ReadSingle(string projektName)
        {
            string sql = "SELECT * FROM Tab_Projekt WHERE Projektname=?";
            OleDbParameter[] ps = { new OleDbParameter("@pname", projektName) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            _internalList.Clear(); // Liste leeren, da wir nur einen Datensatz laden

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                ProjektModel model = MapRowToModel(row);

                // Daten in die aktuelle Instanz mappen
                this.m_ID = model.m_ID;
                this.m_szProjektname = model.m_szProjektname;
                this.m_szBearbeiter = model.m_szBearbeiter;
                this.m_szBeschreibung = model.m_szBeschreibung;
                this.m_szKunde = model.m_szKunde;
                this.m_Aenderungsdatum = model.m_Aenderungsdatum;
                this.m_ID_Klimaregion = model.m_ID_Klimaregion;
                this.m_Erstelldatum = model.m_Erstelldatum;

                _hasSingleData = true;
            }
            else
            {
                _hasSingleData = false;
            }
        }

        public void ReadSingle(int IDProjekt)
        {
            string sql = "SELECT * FROM Tab_Projekt WHERE ID=?";
            OleDbParameter[] ps = { new OleDbParameter("@id", IDProjekt) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            _internalList.Clear(); // Liste leeren, da wir nur einen Datensatz laden

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                ProjektModel model = MapRowToModel(row);

                // Daten in die aktuelle Instanz mappen
                this.m_ID = model.m_ID;
                this.m_szProjektname = model.m_szProjektname;
                this.m_szBearbeiter = model.m_szBearbeiter;
                this.m_szBeschreibung = model.m_szBeschreibung;
                this.m_szKunde = model.m_szKunde;
                this.m_Aenderungsdatum = model.m_Aenderungsdatum;
                this.m_ID_Klimaregion = model.m_ID_Klimaregion;
                this.m_Erstelldatum = model.m_Erstelldatum;

                _hasSingleData = true;
            }
            else
            {
                _hasSingleData = false;
            }
        }
        #endregion

        #region --- UI FILL METHODS ---

        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            foreach (var p in _internalList) ctrl.Items.Add(p.m_szProjektname);
        }

        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            foreach (var p in _internalList) ctrl.Items.Add(p.m_szProjektname);
        }

        #endregion

        #region --- HELPER METHODS ---

        private DateTime ValidateDate(DateTime date)
        {
            if (date < new DateTime(1900, 1, 1)) return DateTime.Now;
            return date;
        }

        private ProjektModel MapRowToModel(DataRow row)
        {
            return new ProjektModel
            {
                m_ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0,
                m_szProjektname = row["Projektname"].ToString(),
                m_szBearbeiter = row["Bearbeiter"]?.ToString() ?? "",
                m_szBeschreibung = row["Beschreibung"]?.ToString() ?? "",
                m_szKunde = row["Kunde"]?.ToString() ?? "",
                m_Aenderungsdatum = row["Aenderungsdatum"] != DBNull.Value ? Convert.ToDateTime(row["Aenderungsdatum"]) : DateTime.Now,
                m_ID_Klimaregion = row["ID_Klimaregion"] != DBNull.Value ? Convert.ToInt32(row["ID_Klimaregion"]) : 0,
                m_Erstelldatum = row["Erstelldatum"] != DBNull.Value ? Convert.ToDateTime(row["Erstelldatum"]) : DateTime.Now
            };
        }

        #endregion
    }
}