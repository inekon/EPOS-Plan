using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    // Controller fuer die Stammdaten-Tabelle Tab_Heizkessel_STAMM.
    // Analog zu BHKWStammCtrl, aber fuer Heizkessel:
    //   - Tabelle = Tab_Heizkessel_STAMM (globaler Katalog)
    //   - DB-Spalte "Bezeichner" wird auf das Model-Feld Name abgebildet
    //   - liest/schreibt das Feld ReadOnly
    //   - Update() und Delete() verweigern die Aenderung schreibgeschuetzter Datensaetze
    //   - Insert() vergibt eine explizite ID (MAX+1) und setzt ReadOnly = false
    // Alle DB-Zugriffe laufen ueber DataRepository.
    public class HeizkesselStammCtrl : HeizkesselModel
    {
        public const string TABLE = "Tab_Heizkessel_STAMM";

        // --- Kompatibilitaets-Layer nach vereinbarter Schablone ---
        private List<HeizkesselModel> _internalList = new List<HeizkesselModel>();
        private bool _hasSingleData = false;

        public int rows => _internalList.Count > 0 ? _internalList.Count : (_hasSingleData ? 1 : 0);
        public List<HeizkesselModel> items => _internalList;

        // Zuletzt gelesener ReadOnly-Zustand (bei ReadSingle gesetzt)
        public bool m_bReadOnly = false;

        // Stammdaten-Listen (Dropdowns)
        public List<string> Brennstoffart = new List<string>();
        public List<string> Brennstoffart_Gruppe = new List<string>();

        public HeizkesselStammCtrl()
        {
            LoadMetaData();
        }

        private void LoadMetaData()
        {
            DataTable dtG = DataRepository.GetDataTable("SELECT Gruppe FROM Tab_BrennstoffKategorien ORDER BY ID");
            Brennstoffart_Gruppe.Clear();
            foreach (DataRow r in dtG.Rows) Brennstoffart_Gruppe.Add(r["Gruppe"].ToString());

            DataTable dtS = DataRepository.GetDataTable("SELECT Bezeichner FROM Tab_Brennstoff_Stamm ORDER BY ID");
            Brennstoffart.Clear();
            foreach (DataRow r in dtS.Rows) Brennstoffart.Add(r["Bezeichner"].ToString());
        }

        // --- READ ---

        public void ReadAll(string filter = "")
        {
            _internalList.Clear();
            _hasSingleData = false;

            string sql = "SELECT * FROM [" + TABLE + "]";
            if (!string.IsNullOrEmpty(filter)) sql += " WHERE " + filter;
            sql += " ORDER BY Bezeichner";

            DataTable dt = DataRepository.GetDataTable(sql);
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    _internalList.Add(MapRowToModel(row));
                }
            }
        }

        public void ReadSingle(string name)
        {
            _internalList.Clear();
            _hasSingleData = false;

            string sql = "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ?";
            DataTable dt = DataRepository.GetDataTable(sql, new OleDbParameter("@nam", name));

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                FillModelFromRow(this, row);
                _internalList.Add(MapRowToModel(row));
                _hasSingleData = true;
            }
        }

        public bool Exists(string name)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new OleDbParameter("@nam", name ?? ""));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        // ReadOnly-Pruefung (Instanz)
        public bool IsReadOnly(string name)
        {
            return IsReadOnlyStatic(name);
        }

        // ReadOnly-Pruefung (statisch, fuer die UI-Guards)
        public static bool IsReadOnlyStatic(string name)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new OleDbParameter("@nam", name ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        // --- SAVE ---

        public bool Save()
        {
            return Exists(this.Name) ? Update() : Insert();
        }

        public bool Insert()
        {
            int neueId = DataRepository.GetMaxID(TABLE) + 1;

            string sql = @"INSERT INTO [" + TABLE + @"]
                            (ID, Bezeichner, Beschreibung, Firma, Ptherm, Brennstoff,
                             Wirkungsgrad_Gas, Wirkungsgrad_Öl, Investitionskosten, Raumbedarf,
                             Wartungskosten, Nutzungsdauer, CO2, SO2, NOx, CO, Staub,
                             Betriebsbereitschaftverlust, Brennwert, Vorlauf, Ruecklauf, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            OleDbParameter[] ps = {
                new OleDbParameter("@id", neueId),
                new OleDbParameter("@bez", this.Name ?? ""),
                new OleDbParameter("@bes", this.Beschreibung ?? ""),
                new OleDbParameter("@fir", this.Firma ?? ""),
                new OleDbParameter("@pth", this.Ptherm),
                new OleDbParameter("@bre", this.Brennstoff),
                new OleDbParameter("@wgg", this.Wirkungsgrad_Gas),
                new OleDbParameter("@wgo", this.Wirkungsgrad_Oel),
                new OleDbParameter("@inv", this.Investitionskosten),
                new OleDbParameter("@rau", this.Raumbedarf),
                new OleDbParameter("@war", this.Wartungskosten),
                new OleDbParameter("@nut", this.Nutzungsdauer),
                new OleDbParameter("@co2", this.CO2),
                new OleDbParameter("@so2", this.SO2),
                new OleDbParameter("@nox", this.NOx),
                new OleDbParameter("@co", this.CO),
                new OleDbParameter("@sta", this.Staub),
                new OleDbParameter("@bbv", this.Betriebsbereitschaftverlust),
                new OleDbParameter("@brn", this.Brennwert),
                new OleDbParameter("@vl", this.Vorlauf),
                new OleDbParameter("@tl", this.Ruecklauf),
                new OleDbParameter("@ro", false)
            };

            bool ok = DataRepository.ExecuteSQL(sql, ps);
            if (ok) this.ID = neueId;
            return ok;
        }

        public bool Update()
        {
            // ReadOnly-Schutz: schreibgeschuetzte Stammdatensaetze duerfen nicht geaendert werden.
            if (IsReadOnly(this.Name))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Beschreibung = ?, Firma = ?, Ptherm = ?, Brennstoff = ?,
                            Wirkungsgrad_Gas = ?, Wirkungsgrad_Öl = ?, Investitionskosten = ?,
                            Raumbedarf = ?, Wartungskosten = ?, Nutzungsdauer = ?,
                            CO2 = ?, SO2 = ?, NOx = ?, CO = ?, Staub = ?,
                            Betriebsbereitschaftverlust = ?, Brennwert = ?, Vorlauf=?, Ruecklauf=?
                          WHERE Bezeichner = ?";

            OleDbParameter[] ps = {
                new OleDbParameter("@bes", this.Beschreibung ?? ""),
                new OleDbParameter("@fir", this.Firma ?? ""),
                new OleDbParameter("@pth", this.Ptherm),
                new OleDbParameter("@bre", this.Brennstoff),
                new OleDbParameter("@wgg", this.Wirkungsgrad_Gas),
                new OleDbParameter("@wgo", this.Wirkungsgrad_Oel),
                new OleDbParameter("@inv", this.Investitionskosten),
                new OleDbParameter("@rau", this.Raumbedarf),
                new OleDbParameter("@war", this.Wartungskosten),
                new OleDbParameter("@nut", this.Nutzungsdauer),
                new OleDbParameter("@co2", this.CO2),
                new OleDbParameter("@so2", this.SO2),
                new OleDbParameter("@nox", this.NOx),
                new OleDbParameter("@co", this.CO),
                new OleDbParameter("@sta", this.Staub),
                new OleDbParameter("@bbv", this.Betriebsbereitschaftverlust),
                new OleDbParameter("@brn", this.Brennwert),
                new OleDbParameter("@vl", this.Vorlauf),
                new OleDbParameter("@rl", this.Ruecklauf),
                new OleDbParameter("@nam", this.Name ?? "")
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Delete(string name)
        {
            if (IsReadOnly(name))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string sql = "DELETE FROM [" + TABLE + "] WHERE Bezeichner = ?";
            return DataRepository.ExecuteSQL(sql, new OleDbParameter("@nam", name ?? ""));
        }

        // --- MAPPING ---

        private void FillModelFromRow(HeizkesselModel target, DataRow row)
        {
            target.ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0;
            target.Name = row["Bezeichner"]?.ToString() ?? "";
            target.Firma = row["Firma"]?.ToString() ?? "";
            target.Beschreibung = row["Beschreibung"]?.ToString() ?? "";
            target.Ptherm = row["Ptherm"] != DBNull.Value ? Convert.ToDouble(row["Ptherm"]) : 0.0;
            target.Brennstoff = row["Brennstoff"] != DBNull.Value ? Convert.ToInt32(row["Brennstoff"]) : 0;
            target.Wirkungsgrad_Gas = row["Wirkungsgrad_Gas"] != DBNull.Value ? Convert.ToDouble(row["Wirkungsgrad_Gas"]) : 0.0;
            target.Wirkungsgrad_Oel = row["Wirkungsgrad_Öl"] != DBNull.Value ? Convert.ToDouble(row["Wirkungsgrad_Öl"]) : 0.0;
            target.Investitionskosten = row["Investitionskosten"] != DBNull.Value ? Convert.ToDouble(row["Investitionskosten"]) : 0.0;
            target.Raumbedarf = row["Raumbedarf"] != DBNull.Value ? Convert.ToDouble(row["Raumbedarf"]) : 0.0;
            target.Wartungskosten = row["Wartungskosten"] != DBNull.Value ? Convert.ToDouble(row["Wartungskosten"]) : 0.0;
            target.Nutzungsdauer = row["Nutzungsdauer"] != DBNull.Value ? Convert.ToDouble(row["Nutzungsdauer"]) : 0.0;
            target.CO2 = row["CO2"] != DBNull.Value ? Convert.ToDouble(row["CO2"]) : 0.0;
            target.SO2 = row["SO2"] != DBNull.Value ? Convert.ToDouble(row["SO2"]) : 0.0;
            target.NOx = row["NOx"] != DBNull.Value ? Convert.ToDouble(row["NOx"]) : 0.0;
            target.CO = row["CO"] != DBNull.Value ? Convert.ToDouble(row["CO"]) : 0.0;
            target.Staub = row["Staub"] != DBNull.Value ? Convert.ToDouble(row["Staub"]) : 0.0;
            target.Betriebsbereitschaftverlust = row["Betriebsbereitschaftverlust"] != DBNull.Value ? Convert.ToDouble(row["Betriebsbereitschaftverlust"]) : 0.0;
            target.Brennwert = row["Brennwert"] != DBNull.Value ? Convert.ToBoolean(row["Brennwert"]) : false;
            target.Vorlauf = row["Vorlauf"] != DBNull.Value ? Convert.ToInt32(row["Vorlauf"]) : 0;
            target.Ruecklauf = row["Ruecklauf"] != DBNull.Value ? Convert.ToInt32(row["Ruecklauf"]) : 0;

            if (ReferenceEquals(target, this))
            {
                this.m_bReadOnly = row.Table.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);
            }
        }

        private HeizkesselModel MapRowToModel(DataRow row)
        {
            HeizkesselModel m = new HeizkesselModel();
            FillModelFromRow(m, row);
            return m;
        }
    }
}
