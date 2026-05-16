using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    public class BrennstoffCtrl : BrennstoffModel
    {
        // --- Kompatibilitäts-Layer für bestehenden UI-Code ---
        private List<BrennstoffModel> _internalList = new List<BrennstoffModel>();
        private bool _hasSingleData = false;

        // Simuliert die alte 'rows' Variable und das 'items' Array
        public int rows => _internalList.Count > 0 ? _internalList.Count : (_hasSingleData ? 1 : 0);
        public List<BrennstoffModel> items => _internalList;

        // Stammdaten-Listen (Bleiben erhalten für Dropdowns)
        public List<string> Brennstoffart = new List<string>();
        public List<string> Brennstoffart_Gruppe = new List<string>();

        public BrennstoffCtrl()
        {
            LoadMetaData();
        }

        private void LoadMetaData()
        {
            DataTable dtG = DataRepository.GetDataTable("SELECT Gruppe FROM Tab_BrennstoffKategorien ORDER BY ID");
            Brennstoffart_Gruppe.Clear();
            foreach (DataRow r in dtG.Rows) Brennstoffart_Gruppe.Add(r["Gruppe"].ToString());

            DataTable dtS = DataRepository.GetDataTable("SELECT Name FROM Tab_Brennstoff_Stamm ORDER BY ID");
            Brennstoffart.Clear();
            foreach (DataRow r in dtS.Rows) Brennstoffart.Add(r["Name"].ToString());
        }

        // --- READ Methoden ---

        public void ReadAll(string filter = "")
        {
            _internalList.Clear();
            _hasSingleData = false;

            string sql = "SELECT * FROM [Tab_Heizkessel]";
            if (!string.IsNullOrEmpty(filter)) sql += " WHERE " + filter;

            DataTable dt = DataRepository.GetDataTable(sql);
            foreach (DataRow row in dt.Rows)
            {
                _internalList.Add(MapRowToModel(row));
            }
        }

        public void ReadSingle(string name)
        {
            _internalList.Clear();
            _hasSingleData = false;

            string sql = "SELECT * FROM [Tab_Heizkessel] WHERE Name = ?";
            DataTable dt = DataRepository.GetDataTable(sql, new OleDbParameter("@nam", name));

            ProcessSingleResult(dt);
        }

        private void ProcessSingleResult(DataTable dt)
        {
            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                FillModelFromRow(this, row); // Füllt Felder des Controllers
                _internalList.Add(MapRowToModel(row)); // Füllt items[0]
                _hasSingleData = true;
            }
        }

        // --- SAVE Methoden ---

        public bool Save()
        {
            // Da Tab_Heizkessel oft den 'Name' als Key nutzt, prüfen wir hier auf ID oder Name
            if (this.ID <= 0)
                return Insert();
            else
                return Update();
        }

        private bool Insert()
        {
            string sql = @"INSERT INTO [Tab_Heizkessel] (Name, Beschreibung, Firma, Ptherm, Brennstoff, 
                            Wirkungsgrad_Gas, Wirkungsgrad_Öl, Investitionskosten, Raumbedarf, 
                            Wartungskosten, Nutzungsdauer, CO2, SO2, NOx, CO, Staub, Betriebsbereitschaftverlust, Brennwert) 
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            bool success = DataRepository.ExecuteSQL(sql, CreateParameters(false));
            if (success)
            {
                DataTable dt = DataRepository.GetDataTable("SELECT @@IDENTITY");
                if (dt.Rows.Count > 0) this.ID = Convert.ToInt32(dt.Rows[0][0]);
            }
            return success;
        }

        public bool Update()
        {
            string sql = @"UPDATE [Tab_Heizkessel] SET 
                            Beschreibung = ?, Firma = ?, Ptherm = ?, Brennstoff = ?, 
                            Wirkungsgrad_Gas = ?, Wirkungsgrad_Öl = ?, Investitionskosten = ?, 
                            Raumbedarf = ?, Wartungskosten = ?, Nutzungsdauer = ?, 
                            CO2 = ?, SO2 = ?, NOx = ?, CO = ?, Staub = ?, 
                            Betriebsbereitschaftverlust = ?, Brennwert = ? 
                          WHERE Name = ?"; // Oder WHERE ID = ?, falls ID der Primärschlüssel ist

            return DataRepository.ExecuteSQL(sql, CreateParameters(true));
        }

        public bool Delete(string name)
        {
            string sql = "DELETE FROM [Tab_Heizkessel] WHERE Name = ?";
            return DataRepository.ExecuteSQL(sql, new OleDbParameter("@nam", name));
        }

        // --- MAPPING & PARAMETER ---

        private OleDbParameter[] CreateParameters(bool isUpdate)
        {
            List<OleDbParameter> p = new List<OleDbParameter>();

            // Bei Insert muss der Name am Anfang stehen (gemäß SQL String)
            if (!isUpdate) p.Add(new OleDbParameter("@nam", this.Name ?? ""));

            p.Add(new OleDbParameter("@bes", this.Beschreibung ?? ""));
            p.Add(new OleDbParameter("@fir", this.Firma ?? ""));
            p.Add(new OleDbParameter("@pth", this.Ptherm));
            p.Add(new OleDbParameter("@bre", this.Brennstoff));
            p.Add(new OleDbParameter("@wgg", this.Wirkungsgrad_Gas));
            p.Add(new OleDbParameter("@wgo", this.Wirkungsgrad_Oel));
            p.Add(new OleDbParameter("@inv", this.Investitionskosten));
            p.Add(new OleDbParameter("@rau", this.Raumbedarf));
            p.Add(new OleDbParameter("@war", this.Wartungskosten));
            p.Add(new OleDbParameter("@nut", this.Nutzungsdauer));
            p.Add(new OleDbParameter("@co2", this.CO2));
            p.Add(new OleDbParameter("@so2", this.SO2));
            p.Add(new OleDbParameter("@nox", this.NOx));
            p.Add(new OleDbParameter("@co", this.CO));
            p.Add(new OleDbParameter("@sta", this.Staub));
            p.Add(new OleDbParameter("@bbv", this.Betriebsbereitschaftverlust));
            p.Add(new OleDbParameter("@brn", this.Brennwert));

            // Bei Update steht der Name im WHERE-Teil (am Ende)
            if (isUpdate) p.Add(new OleDbParameter("@nam", this.Name ?? ""));

            return p.ToArray();
        }

        private void FillModelFromRow(BrennstoffModel target, DataRow row)
        {
            target.ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0;
            target.Name = row["Name"]?.ToString() ?? "";
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
        }

        private BrennstoffModel MapRowToModel(DataRow row)
        {
            BrennstoffModel m = new BrennstoffModel();
            FillModelFromRow(m, row);
            return m;
        }
    }
}