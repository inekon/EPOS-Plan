using System;
using System.Collections.Generic;
using System.Data;

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
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@bez", szBezeichner ?? (object)DBNull.Value));

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
                new DbParam("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        public static bool IsReadOnlyStatic(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        // Legt einen neuen Stammdatensatz an (explizite ID, ReadOnly = false).
        public bool Insert()
        {
            StromspeicherCtrl.StelleGeraetespaltenSicher();   // AP3-Spalten, bevor sie im INSERT stehen

            int neueId = DataRepository.GetMaxID(TABLE) + 1;

            string sql = @"INSERT INTO [" + TABLE + @"]
                            (ID, Bezeichner, Typ, Leistung, Energie, Degradation, Ladezustand, Modulkosten, ReadOnly,
                             Wirkungsgrad_RT, Zyklen_Zugesichert, Verschleisskosten, Leistungskosten, Investition_Fix, Standby_Verbrauch)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            DbParam[] ps = {
                new DbParam("@id", neueId),
                new DbParam("@bez", this.m_szBezeichner ?? ""),
                new DbParam("@typ", (object)(this.m_szTyp ?? "") ),
                new DbParam("@lei", this.m_Leistung),
                new DbParam("@ene", this.m_Energie),
                new DbParam("@deg", this.m_Degradation),
                new DbParam("@lad", this.m_Ladezustand),
                new DbParam("@mod", this.m_Modulkosten),
                new DbParam("@ro", false),
                new DbParam("@eta", this.m_WirkungsgradRT),
                new DbParam("@nzyk", this.m_ZyklenZugesichert),
                new DbParam("@cver", this.m_Verschleisskosten),
                new DbParam("@cpow", this.m_Leistungskosten),
                new DbParam("@ifix", this.m_InvestitionFix),
                new DbParam("@stby", this.m_StandbyVerbrauch)
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
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt");
                return false;
            }

            StromspeicherCtrl.StelleGeraetespaltenSicher();   // AP3-Spalten, bevor sie im UPDATE stehen

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Bezeichner = ?, Typ = ?, Leistung = ?, Energie = ?,
                            Degradation = ?, Ladezustand = ?, Modulkosten = ?,
                            Wirkungsgrad_RT = ?, Zyklen_Zugesichert = ?, Verschleisskosten = ?,
                            Leistungskosten = ?, Investition_Fix = ?, Standby_Verbrauch = ?
                          WHERE Bezeichner = ?";

            DbParam[] ps = {
                new DbParam("@bez", this.m_szBezeichner ?? ""),
                new DbParam("@typ", (object)(this.m_szTyp ?? "") ),
                new DbParam("@lei", this.m_Leistung),
                new DbParam("@ene", this.m_Energie),
                new DbParam("@deg", this.m_Degradation),
                new DbParam("@lad", this.m_Ladezustand),
                new DbParam("@mod", this.m_Modulkosten),
                new DbParam("@eta", this.m_WirkungsgradRT),
                new DbParam("@nzyk", this.m_ZyklenZugesichert),
                new DbParam("@cver", this.m_Verschleisskosten),
                new DbParam("@cpow", this.m_Leistungskosten),
                new DbParam("@ifix", this.m_InvestitionFix),
                new DbParam("@stby", this.m_StandbyVerbrauch),
                new DbParam("@key", szKey ?? "")
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Delete(string szBezeichner)
        {
            if (IsReadOnlyStatic(szBezeichner))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt");
                return false;
            }

            string sql = "DELETE FROM [" + TABLE + "] WHERE Bezeichner = ?";
            return DataRepository.ExecuteSQL(sql, new DbParam("@bez", szBezeichner ?? ""));
        }

        // --- MAPPING ---

        private void Reset()
        {
            m_ID = 0; m_szBezeichner = string.Empty; m_szTyp = string.Empty;
            m_Leistung = 0; m_Energie = 0; m_Degradation = 0; m_Ladezustand = 0; m_Modulkosten = 0;
            m_WirkungsgradRT = 0; m_ZyklenZugesichert = 0; m_Verschleisskosten = 0;
            m_Leistungskosten = 0; m_InvestitionFix = 0; m_StandbyVerbrauch = 0;
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

            // AP3-Geraetetechnik (Fachkonzept 5.1) - dieselbe Columns.Contains-Wache wie
            // darueber: auf einer Datenbank vor Migrationsschritt 11 fehlen die Spalten,
            // die Felder behalten dann ihre 0.
            if (row.Table.Columns.Contains("Wirkungsgrad_RT") && row["Wirkungsgrad_RT"] != DBNull.Value) t.m_WirkungsgradRT = Convert.ToDouble(row["Wirkungsgrad_RT"]);
            if (row.Table.Columns.Contains("Zyklen_Zugesichert") && row["Zyklen_Zugesichert"] != DBNull.Value) t.m_ZyklenZugesichert = Convert.ToInt32(row["Zyklen_Zugesichert"]);
            if (row.Table.Columns.Contains("Verschleisskosten") && row["Verschleisskosten"] != DBNull.Value) t.m_Verschleisskosten = Convert.ToDouble(row["Verschleisskosten"]);
            if (row.Table.Columns.Contains("Leistungskosten") && row["Leistungskosten"] != DBNull.Value) t.m_Leistungskosten = Convert.ToDouble(row["Leistungskosten"]);
            if (row.Table.Columns.Contains("Investition_Fix") && row["Investition_Fix"] != DBNull.Value) t.m_InvestitionFix = Convert.ToDouble(row["Investition_Fix"]);
            if (row.Table.Columns.Contains("Standby_Verbrauch") && row["Standby_Verbrauch"] != DBNull.Value) t.m_StandbyVerbrauch = Convert.ToDouble(row["Standby_Verbrauch"]);
        }

        private StromspeicherModel MapRowToModel(DataRow row)
        {
            StromspeicherModel m = new StromspeicherModel();
            FillFromRow(m, row);
            return m;
        }
    }
}
