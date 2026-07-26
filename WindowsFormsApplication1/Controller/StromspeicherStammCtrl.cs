using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    // Controller fuer die Stammdaten-Tabelle Tab_Stromspeicher_STAMM (globaler Katalog).
    // Analog zu HeizkesselStammCtrl / BHKWStammCtrl:
    //   - Tabelle = Tab_Stromspeicher_STAMM
    //   - liest/schreibt das Feld ReadOnly
    //   - Insert() vergibt eine explizite ID (MAX+1) und setzt ReadOnly = false
    //   - Update()/Delete() verweigern schreibgeschuetzte Datensaetze
    // Alle DB-Zugriffe laufen ueber DataRepository.
    public class StromspeicherStammCtrl : StromspeicherModel
    {
        public const string TABLE = "Tab_Stromspeicher_STAMM";

        private List<StromspeicherModel> _internalList = new List<StromspeicherModel>();
        public int rows => _internalList.Count;
        public List<StromspeicherModel> items => _internalList;

        // Zuletzt gelesener ReadOnly-Zustand
        public bool m_bReadOnly = false;

        public void ReadAll()
        {
            string sql = "SELECT * FROM [" + TABLE + "] ORDER BY Bezeichner";
            DataTable dt = DataRepository.GetDataTable(sql, null);

            _internalList.Clear();
            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                _internalList.Add(MapRowToModel(row));
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            string sql = "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ?";
            DataTable dt = DataRepository.GetDataTable(sql, new OleDbParameter("@bez", szBezeichner ?? (object)DBNull.Value));

            Reset();
            _internalList.Clear();

            if (dt != null && dt.Rows.Count > 0)
            {
                FillFromRow(this, dt.Rows[0]);
                this.m_bReadOnly = ReadOnlyOf(dt.Rows[0]);
                _internalList.Add(this);
            }
        }

        public bool Exists(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        public static bool IsReadOnlyStatic(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        // Legt einen neuen Stammdatensatz an (explizite ID, ReadOnly = false).
        public bool Insert()
        {
            int neueId = DataRepository.GetMaxID(TABLE) + 1;

            string sql = @"INSERT INTO [" + TABLE + @"]
                            (ID, Bezeichner, Typ, Leistung, Energie, Degradation, Ladezustand, Modulkosten, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

            OleDbParameter[] ps = {
                new OleDbParameter("@id", neueId),
                new OleDbParameter("@bez", this.m_szBezeichner ?? ""),
                new OleDbParameter("@typ", (object)(this.m_szTyp ?? "") ),
                new OleDbParameter("@lei", this.m_Leistung),
                new OleDbParameter("@ene", this.m_Energie),
                new OleDbParameter("@deg", this.m_Degradation),
                new OleDbParameter("@lad", this.m_Ladezustand),
                new OleDbParameter("@mod", this.m_Modulkosten),
                new OleDbParameter("@ro", false)
            };

            bool ok = DataRepository.ExecuteSQL(sql, ps);
            if (ok) this.m_ID = neueId;
            return ok;
        }

        // Aktualisiert den Datensatz. szKey ist der urspruengliche Bezeichner (WHERE-Schluessel),
        // this.m_szBezeichner der (evtl. geaenderte) neue Bezeichner.
        public bool Update(string szKey)
        {
            if (IsReadOnlyStatic(szKey))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Bezeichner = ?, Typ = ?, Leistung = ?, Energie = ?,
                            Degradation = ?, Ladezustand = ?, Modulkosten = ?
                          WHERE Bezeichner = ?";

            OleDbParameter[] ps = {
                new OleDbParameter("@bez", this.m_szBezeichner ?? ""),
                new OleDbParameter("@typ", (object)(this.m_szTyp ?? "") ),
                new OleDbParameter("@lei", this.m_Leistung),
                new OleDbParameter("@ene", this.m_Energie),
                new OleDbParameter("@deg", this.m_Degradation),
                new OleDbParameter("@lad", this.m_Ladezustand),
                new OleDbParameter("@mod", this.m_Modulkosten),
                new OleDbParameter("@key", szKey ?? "")
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Delete(string szBezeichner)
        {
            if (IsReadOnlyStatic(szBezeichner))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string sql = "DELETE FROM [" + TABLE + "] WHERE Bezeichner = ?";
            return DataRepository.ExecuteSQL(sql, new OleDbParameter("@bez", szBezeichner ?? ""));
        }

        // --- MAPPING ---

        private void Reset()
        {
            m_ID = 0; m_szBezeichner = string.Empty; m_szTyp = string.Empty;
            m_Leistung = 0; m_Energie = 0; m_Degradation = 0; m_Ladezustand = 0; m_Modulkosten = 0;
            m_bReadOnly = false;
        }

        private static bool ReadOnlyOf(DataRow row)
        {
            return row.Table.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);
        }

        private static void FillFromRow(StromspeicherModel t, DataRow row)
        {
            if (row["ID"] != DBNull.Value) t.m_ID = Convert.ToInt32(row["ID"]);
            if (row["Bezeichner"] != DBNull.Value) t.m_szBezeichner = row["Bezeichner"].ToString();
            if (row.Table.Columns.Contains("Typ") && row["Typ"] != DBNull.Value) t.m_szTyp = row["Typ"].ToString();
            if (row.Table.Columns.Contains("Leistung") && row["Leistung"] != DBNull.Value) t.m_Leistung = Convert.ToDouble(row["Leistung"]);
            if (row.Table.Columns.Contains("Energie") && row["Energie"] != DBNull.Value) t.m_Energie = Convert.ToDouble(row["Energie"]);
            if (row.Table.Columns.Contains("Degradation") && row["Degradation"] != DBNull.Value) t.m_Degradation = Convert.ToDouble(row["Degradation"]);
            if (row.Table.Columns.Contains("Ladezustand") && row["Ladezustand"] != DBNull.Value) t.m_Ladezustand = Convert.ToDouble(row["Ladezustand"]);
            if (row.Table.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value) t.m_Modulkosten = Convert.ToDouble(row["Modulkosten"]);
        }

        private StromspeicherModel MapRowToModel(DataRow row)
        {
            StromspeicherModel m = new StromspeicherModel();
            FillFromRow(m, row);
            return m;
        }
    }
}
