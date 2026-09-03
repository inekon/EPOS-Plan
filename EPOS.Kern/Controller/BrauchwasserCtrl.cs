using System;
using System.Collections.Generic;
using System.Data;

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
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@id", id));

            ProcessSingleResult(dt);
        }

        public void ReadSingle(string bezeichner)
        {
            _internalList.Clear();
            _hasSingleData = false;

            string sql = "SELECT * FROM Tab_Brauchwasser WHERE Bezeichner = ?";
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@bez", bezeichner));

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
            // SQL-Dialekt-Audit 03.09.2026: Die zwoelf Monatsspalten hiessen hier M1..M12,
            // im Schema heissen sie Monat_1..Monat_12 - so, wie FillModelFromRow sie
            // weiter unten auch LIEST. Der Satz konnte damit nie geschrieben werden
            // ("table Tab_Brauchwasser has no column named M1"); unter Access war er
            // ebenso falsch, nur ruft niemand diese Klasse auf (die Masken arbeiten mit
            // Z_ProjektBrauchwasserCtrl), also fiel es nicht auf.
            string sql = @"INSERT INTO Tab_Brauchwasser (Bezeichner, Typ, Beschreibung, Monat_1, Monat_2, Monat_3, Monat_4, Monat_5, Monat_6, Monat_7, Monat_8, Monat_9, Monat_10, Monat_11, Monat_12)
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
            // Spaltennamen wie im Schema (siehe Insert): Monat_1..Monat_12 statt M1..M12.
            string sql = @"UPDATE Tab_Brauchwasser SET
                            Bezeichner = ?, Typ = ?, Beschreibung = ?,
                            Monat_1=?, Monat_2=?, Monat_3=?, Monat_4=?, Monat_5=?, Monat_6=?,
                            Monat_7=?, Monat_8=?, Monat_9=?, Monat_10=?, Monat_11=?, Monat_12=?
                           WHERE ID = ?";

            return DataRepository.ExecuteSQL(sql, CreateParameters(true));
        }

        // --- DELETE Methoden (Löschen) ---

        public bool Delete()
        {
            if (this.m_ID <= 0) return false;
            string sql = "DELETE FROM Tab_Brauchwasser WHERE ID = ?";
            return DataRepository.ExecuteSQL(sql, new DbParam("@id", this.m_ID));
        }

        public bool Delete(string bezeichner)
        {
            string sql = "DELETE FROM Tab_Brauchwasser WHERE Bezeichner = ?";
            return DataRepository.ExecuteSQL(sql, new DbParam("@bez", bezeichner));
        }

        // --- MAPPING & PARAMETER (Die "Maschinenräume") ---

        private DbParam[] CreateParameters(bool includeId)
        {
            List<DbParam> p = new List<DbParam>
            {
                new DbParam("@bez", this.m_szBezeichner ?? ""),
                new DbParam("@typ", this.m_szTyp ?? ""),
                new DbParam("@desc", this.m_szBeschreibung ?? "")
            };

            for (int i = 0; i < 12; i++)
            {
                p.Add(new DbParam("@m" + (i + 1), this.m_Monat[i]));
            }

            if (includeId)
                p.Add(new DbParam("@id", this.m_ID));

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