using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class BrauchwasserCtrl : BrauchwasserModel
    {
        // --- Kompatibilitäts-Layer für bestehenden UI-Code ---
        private List<BrauchwasserModel> _internalList = new List<BrauchwasserModel>();
        private bool _hasSingleData = false;

        // Simuliert die alte 'rows' Variable und das 'items' Array
        public int rows => _internalList.Count > 0 ? _internalList.Count : (_hasSingleData ? 1 : 0);
        public List<BrauchwasserModel> items => _internalList;

        // --- READ Methoden (Lesen) ---
        public void ReadAll()
        {
            _internalList.Clear();
            _hasSingleData = false;

            string sql = "SELECT * FROM Tab_Brauchwasser ORDER BY Bezeichner";
            DataTable dt = DataRepository.GetDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                _internalList.Add(MapRowToModel(row));
            }
        }

        public void ReadSingle(int id)
        {
            _internalList.Clear();
            _hasSingleData = false;

            string sql = "SELECT * FROM Tab_Brauchwasser WHERE ID = ?";
            DataTable dt = DataRepository.GetDataTable(sql, new OleDbParameter("@id", id));

            ProcessSingleResult(dt);
        }

        public void ReadSingle(string bezeichner)
        {
            _internalList.Clear();
            _hasSingleData = false;

            string sql = "SELECT * FROM Tab_Brauchwasser WHERE Bezeichner = ?";
            DataTable dt = DataRepository.GetDataTable(sql, new OleDbParameter("@bez", bezeichner));

            ProcessSingleResult(dt);
        }

        private void ProcessSingleResult(DataTable dt)
        {
            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                FillModelFromRow(this, row); // Füllt die Felder des Controllers (this)
                _internalList.Add(MapRowToModel(row)); // Füllt items[0]
                _hasSingleData = true;
            }
        }

        // --- SAVE Methoden (Schreiben) ---

        public bool Save()
        {
            // Entscheidungslogik: Neu anlegen oder Vorhandenes ändern
            if (this.m_ID <= 0)
                return Insert();
            else
                return Update();
        }

        private bool Insert()
        {
            string sql = @"INSERT INTO Tab_Brauchwasser (Bezeichner, Typ, Beschreibung, M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12) 
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            // ARBEITSPAKET S4b, ALTFEHLER BEHOBEN: Bisher lief das INSERT über
            // ExecuteSQL und die ID-Rückgabe über ein zweites, EIGENES
            // GetDataTable("SELECT @@IDENTITY"). @@IDENTITY gilt aber je VERBINDUNG -
            // die zweite Abfrage lief auf einer FRISCHEN Verbindung und lieferte
            // deshalb den Wert eines fremden oder gar keines Vorgangs. ExecuteInsertAndGetId
            // macht beides auf DERSELBEN Verbindung (last_insert_rowid()).
            int neueId = DataRepository.ExecuteInsertAndGetId(sql, CreateParameters(false));
            bool success = neueId > 0;

            if (success) this.m_ID = neueId;
            return success;
        }

        private bool Update()
        {
            string sql = @"UPDATE Tab_Brauchwasser SET 
                            Bezeichner = ?, Typ = ?, Beschreibung = ?, 
                            M1=?, M2=?, M3=?, M4=?, M5=?, M6=?, M7=?, M8=?, M9=?, M10=?, M11=?, M12=? 
                           WHERE ID = ?";

            return DataRepository.ExecuteSQL(sql, CreateParameters(true));
        }

        // --- DELETE Methoden (Löschen) ---

        public bool Delete()
        {
            if (this.m_ID <= 0) return false;
            string sql = "DELETE FROM Tab_Brauchwasser WHERE ID = ?";
            return DataRepository.ExecuteSQL(sql, new OleDbParameter("@id", this.m_ID));
        }

        public bool Delete(string bezeichner)
        {
            string sql = "DELETE FROM Tab_Brauchwasser WHERE Bezeichner = ?";
            return DataRepository.ExecuteSQL(sql, new OleDbParameter("@bez", bezeichner));
        }

        // --- MAPPING & PARAMETER (Die "Maschinenräume") ---

        private OleDbParameter[] CreateParameters(bool includeId)
        {
            List<OleDbParameter> p = new List<OleDbParameter>
            {
                new OleDbParameter("@bez", this.m_szBezeichner ?? ""),
                new OleDbParameter("@typ", this.m_szTyp ?? ""),
                new OleDbParameter("@desc", this.m_szBeschreibung ?? "")
            };

            for (int i = 0; i < 12; i++)
            {
                p.Add(new OleDbParameter("@m" + (i + 1), this.m_Monat[i]));
            }

            if (includeId)
                p.Add(new OleDbParameter("@id", this.m_ID));

            return p.ToArray();
        }

        private void FillModelFromRow(BrauchwasserModel target, DataRow row)
        {
            target.m_ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0;
            target.m_szBezeichner = row["Bezeichner"]?.ToString() ?? "";
            target.m_szTyp = row["Typ"]?.ToString() ?? "";
            target.m_szBeschreibung = row["Beschreibung"]?.ToString() ?? "";

            for (int i = 0; i < 12; i++)
            {
                string colName = "Monat_" + (i + 1);
                target.m_Monat[i] = row[colName] != DBNull.Value ? Convert.ToDouble(row[colName]) : 0.0;
            }
        }

        private BrauchwasserModel MapRowToModel(DataRow row)
        {
            BrauchwasserModel m = new BrauchwasserModel();
            FillModelFromRow(m, row);
            return m;
        }
    }
}