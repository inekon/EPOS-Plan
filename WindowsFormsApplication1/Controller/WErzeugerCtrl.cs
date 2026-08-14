using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class WErzeugerCtrl : WErzeugerModel
    {
        private List<WErzeugerModel> _internalList = new List<WErzeugerModel>();
        public int rows => _internalList.Count;
        public new List<WErzeugerModel> items => _internalList;

        public WErzeugerCtrl()
        {
        }

        public bool Update()
        {
            try
            {
                string sql = @"UPDATE Tab_Energieanlagen 
                               SET ID_Projekt = ?, Bezeichner = ?, ID_Type = ?, ID_WP = ?, Betriebsart = ?, 
                                   Sperrung = ?, Sperrzeit_von = ?, Sperrzeit_bis = ?, Vorlauf = ?, Rücklauf = ?,
                                   Bivalenter_Betrieb = ?, Abschaltpunkt = ?, Nutzungszeit = ?, ID_SP = ?, ID_PV = ?, ID_Solar = ?
                               WHERE ID = ?";

                OleDbParameter[] ps = {
                    new OleDbParameter("@idProj", ID_Projekt),
                    new OleDbParameter("@bez", Bezeichner ?? (object)DBNull.Value),
                    new OleDbParameter("@idType", ID_Type),
                    new OleDbParameter("@idWp", ID_WP),
                    new OleDbParameter("@betr", Betriebsart ?? (object)DBNull.Value),
                    new OleDbParameter("@sperr", Sperrung),
                    new OleDbParameter("@von", Sperrzeit_von),
                    new OleDbParameter("@bis", Sperrzeit_bis),
                    new OleDbParameter("@vor", Vorlauf),
                    new OleDbParameter("@rue", Ruecklauf),
                    new OleDbParameter("@biv", Bivalenter_Betrieb),
                    new OleDbParameter("@absch", Abschaltpunkt),
                    new OleDbParameter("@nutz", Nutzungszeit),
                    new OleDbParameter("@idSp", ID_SP),
                    new OleDbParameter("@idPv", ID_PV),
                    new OleDbParameter("@idSol", ID_Solar),
                    new OleDbParameter("@id", ID) // Die ID am Ende bestimmt die WHERE-Klausel
                };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Update: " + ex.Message);
                return false;
            }
        }

        public bool Delete()
        {
            try
            {
                // Korrektur: DELETE * FROM bzw. DELETE FROM statt der alten fehlerhaften Syntax "DELETE ID_Projekt FROM..."
                string sql = "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ?";
                OleDbParameter[] ps = { new OleDbParameter("@idProj", ID_Projekt) };

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
               string sql = @"INSERT INTO Tab_Energieanlagen 
                               (
                                   ID_Projekt, Bezeichner, ID_Type, ID_WP, Betriebsart, Sperrung, 
                                   Sperrzeit_von, Sperrzeit_bis, Vorlauf, Rücklauf, Bivalenter_Betrieb,
                                   Abschaltpunkt, Nutzungszeit, ID_SP, ID_PV, ID_Solar
                               ) 
                               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@idProj", ID_Projekt),
                    new OleDbParameter("@bez", Bezeichner ?? (object)DBNull.Value),
                    new OleDbParameter("@idType", ID_Type),
                    new OleDbParameter("@idWp", ID_WP),
                    new OleDbParameter("@betr", Betriebsart ?? (object)DBNull.Value),
                    new OleDbParameter("@sperr", Sperrung),
                    new OleDbParameter("@von", Sperrzeit_von),
                    new OleDbParameter("@bis", Sperrzeit_bis),
                    new OleDbParameter("@vor", Vorlauf),
                    new OleDbParameter("@rue", Ruecklauf),
                    new OleDbParameter("@biv", Bivalenter_Betrieb),
                    new OleDbParameter("@absch", Abschaltpunkt),
                    new OleDbParameter("@nutz", Nutzungszeit),
                    new OleDbParameter("@idSp", ID_SP),
                    new OleDbParameter("@idPv", ID_PV),
                    new OleDbParameter("@idSol", ID_Solar)
                };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Insert: " + ex.Message);
                return false;
            }
        }

        public void ReadAllFilter(string filter = "")
        {
            string sql;
            if (string.IsNullOrEmpty(filter))
            {
                sql = "SELECT * FROM Tab_Energieanlagen ORDER BY Bezeichner";
            }
            else
            {
                sql = "SELECT * FROM Tab_Energieanlagen WHERE " + filter;
            }

            DataTable dt = DataRepository.GetDataTable(sql, null);
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                WErzeugerModel item = new WErzeugerModel();

                // Spaltenbasiertes, sicheres Auslesen aus der DataTable
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.ID = Convert.ToInt32(row["ID"]);
                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value) item.ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);
                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) item.Bezeichner = row["Bezeichner"].ToString();
                if (dt.Columns.Contains("ID_Type") && row["ID_Type"] != DBNull.Value) item.ID_Type = Convert.ToInt32(row["ID_Type"]);
                if (dt.Columns.Contains("ID_WP") && row["ID_WP"] != DBNull.Value) item.ID_WP = Convert.ToInt32(row["ID_WP"]);
                if (dt.Columns.Contains("Betriebsart") && row["Betriebsart"] != DBNull.Value) item.Betriebsart = row["Betriebsart"].ToString();
                if (dt.Columns.Contains("Sperrung") && row["Sperrung"] != DBNull.Value) item.Sperrung = Convert.ToBoolean(row["Sperrung"]);
                if (dt.Columns.Contains("Sperrzeit_von") && row["Sperrzeit_von"] != DBNull.Value) item.Sperrzeit_von = Convert.ToInt32(row["Sperrzeit_von"]);
                if (dt.Columns.Contains("Sperrzeit_bis") && row["Sperrzeit_bis"] != DBNull.Value) item.Sperrzeit_bis = Convert.ToInt32(row["Sperrzeit_bis"]);
                if (dt.Columns.Contains("Vorlauf") && row["Vorlauf"] != DBNull.Value) item.Vorlauf = Convert.ToInt32(row["Vorlauf"]);
                if (dt.Columns.Contains("Rücklauf") && row["Rücklauf"] != DBNull.Value) item.Ruecklauf = Convert.ToInt32(row["Rücklauf"]);
                if (dt.Columns.Contains("Bivalenter_Betrieb") && row["Bivalenter_Betrieb"] != DBNull.Value) item.Bivalenter_Betrieb = Convert.ToBoolean(row["Bivalenter_Betrieb"]);
                if (dt.Columns.Contains("Abschaltpunkt") && row["Abschaltpunkt"] != DBNull.Value) item.Abschaltpunkt = Convert.ToDouble(row["Abschaltpunkt"]);
                if (dt.Columns.Contains("Nutzungszeit") && row["Nutzungszeit"] != DBNull.Value) item.Nutzungszeit = Convert.ToInt32(row["Nutzungszeit"]);
                if (dt.Columns.Contains("ID_SP") && row["ID_SP"] != DBNull.Value) item.ID_SP = Convert.ToInt32(row["ID_SP"]);
                if (dt.Columns.Contains("ID_PV") && row["ID_PV"] != DBNull.Value) item.ID_PV = Convert.ToInt32(row["ID_PV"]);
                if (dt.Columns.Contains("ID_Solar") && row["ID_Solar"] != DBNull.Value) item.ID_Solar = Convert.ToInt32(row["ID_Solar"]);

                // Zusätzliche Felder aus dem alten ReadAll
                if (dt.Columns.Contains("Heizstab") && row["Heizstab"] != DBNull.Value) item.Heizstab = Convert.ToBoolean(row["Heizstab"]);
                if (dt.Columns.Contains("Volumen") && row["Volumen"] != DBNull.Value) item.Volumen = Convert.ToDouble(row["Volumen"]);
                if (dt.Columns.Contains("rendeMix") && row["rendeMix"] != DBNull.Value) item.rendeMix = Convert.ToBoolean(row["rendeMix"]);
                if (dt.Columns.Contains("Solaranteil") && row["Solaranteil"] != DBNull.Value) item.Solaranteil = Convert.ToInt32(row["Solaranteil"]);
                if (dt.Columns.Contains("ID_Kessel") && row["ID_Kessel"] != DBNull.Value) item.ID_Kessel = Convert.ToInt32(row["ID_Kessel"]);
                if (dt.Columns.Contains("ID_BHKW") && row["ID_BHKW"] != DBNull.Value) item.ID_BHKW = Convert.ToInt32(row["ID_BHKW"]);
                if (dt.Columns.Contains("Grenzleistung") && row["Grenzleistung"] != DBNull.Value) item.Grenzleistung = Convert.ToDouble(row["Grenzleistung"]);
                if (dt.Columns.Contains("Kollektormodulanzahl") && row["Kollektormodulanzahl"] != DBNull.Value) item.Kollektormodulanzahl = Convert.ToInt32(row["Kollektormodulanzahl"]);
                if (dt.Columns.Contains("PV_Leistung") && row["PV_Leistung"] != DBNull.Value) item.PV_Leistung = Convert.ToDouble(row["PV_Leistung"]);
                if (dt.Columns.Contains("Neigung") && row["Neigung"] != DBNull.Value) item.m_Neigung = Convert.ToInt32(row["Neigung"]);
                if (dt.Columns.Contains("Azimut") & row["Azimut"] != DBNull.Value) item.m_Azimut = Convert.ToInt32(row["Azimut"]);
                if (dt.Columns.Contains("ID_PUFFER") && row["ID_PUFFER"] != DBNull.Value) item.ID_PUFFER = Convert.ToInt32(row["ID_PUFFER"]);

                _internalList.Add(item);
            }
        }

        public void ReadSingle(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql, null);

            // "rows" Variable spiegelt im Single-Modus die Existenz wider (0 oder 1)
            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value) ID = Convert.ToInt32(row["ID"]);
                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value) ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);
                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) Bezeichner = row["Bezeichner"].ToString();
                if (dt.Columns.Contains("ID_Type") && row["ID_Type"] != DBNull.Value) ID_Type = Convert.ToInt32(row["ID_Type"]);
                if (dt.Columns.Contains("ID_WP") && row["ID_WP"] != DBNull.Value) ID_WP = Convert.ToInt32(row["ID_WP"]);
                if (dt.Columns.Contains("Betriebsart") && row["Betriebsart"] != DBNull.Value) Betriebsart = row["Betriebsart"].ToString();
                if (dt.Columns.Contains("Sperrung") && row["Sperrung"] != DBNull.Value) Sperrung = Convert.ToBoolean(row["Sperrung"]);
                if (dt.Columns.Contains("Sperrzeit_von") && row["Sperrzeit_von"] != DBNull.Value) Sperrzeit_von = Convert.ToInt32(row["Sperrzeit_von"]);
                if (dt.Columns.Contains("Sperrzeit_bis") && row["Sperrzeit_bis"] != DBNull.Value) Sperrzeit_bis = Convert.ToInt32(row["Sperrzeit_bis"]);
                if (dt.Columns.Contains("Vorlauf") && row["Vorlauf"] != DBNull.Value) Vorlauf = Convert.ToInt32(row["Vorlauf"]);
                if (dt.Columns.Contains("Rücklauf") && row["Rücklauf"] != DBNull.Value) Ruecklauf = Convert.ToInt32(row["Rücklauf"]);
                if (dt.Columns.Contains("Bivalenter_Betrieb") && row["Bivalenter_Betrieb"] != DBNull.Value) Bivalenter_Betrieb = Convert.ToBoolean(row["Bivalenter_Betrieb"]);
                if (dt.Columns.Contains("Abschaltpunkt") && row["Abschaltpunkt"] != DBNull.Value) Abschaltpunkt = Convert.ToDouble(row["Abschaltpunkt"]);
                if (dt.Columns.Contains("Nutzungszeit") && row["Nutzungszeit"] != DBNull.Value) Nutzungszeit = Convert.ToInt32(row["Nutzungszeit"]);
                if (dt.Columns.Contains("ID_SP") && row["ID_SP"] != DBNull.Value) ID_SP = Convert.ToInt32(row["ID_SP"]);
                if (dt.Columns.Contains("ID_PV") && row["ID_PV"] != DBNull.Value) ID_PV = Convert.ToInt32(row["ID_PV"]);
                if (dt.Columns.Contains("ID_Solar") && row["ID_Solar"] != DBNull.Value) ID_Solar = Convert.ToInt32(row["ID_Solar"]);
                if (dt.Columns.Contains("Heizstab") && row["Heizstab"] != DBNull.Value) Heizstab = Convert.ToBoolean(row["Heizstab"]);
                if (dt.Columns.Contains("Volumen") && row["Volumen"] != DBNull.Value) Volumen = Convert.ToDouble(row["Volumen"]);
                if (dt.Columns.Contains("rendeMix") && row["rendeMix"] != DBNull.Value) rendeMix = Convert.ToBoolean(row["rendeMix"]);
                if (dt.Columns.Contains("Solaranteil") && row["Solaranteil"] != DBNull.Value) Solaranteil = Convert.ToInt32(row["Solaranteil"]);
                if (dt.Columns.Contains("ID_Kessel") && row["ID_Kessel"] != DBNull.Value) ID_Kessel = Convert.ToInt32(row["ID_Kessel"]);
                if (dt.Columns.Contains("ID_BHKW") && row["ID_BHKW"] != DBNull.Value) ID_BHKW = Convert.ToInt32(row["ID_BHKW"]);
                if (dt.Columns.Contains("Grenzleistung") && row["Grenzleistung"] != DBNull.Value) Grenzleistung = Convert.ToDouble(row["Grenzleistung"]);
                if (dt.Columns.Contains("Kollektormodulanzahl") && row["Kollektormodulanzahl"] != DBNull.Value) Kollektormodulanzahl = Convert.ToInt32(row["Kollektormodulanzahl"]);
                if (dt.Columns.Contains("PV_Leistung") && row["PV_Leistung"] != DBNull.Value) PV_Leistung = Convert.ToDouble(row["PV_Leistung"]);
                if (dt.Columns.Contains("Neigung") && row["Neigung"] != DBNull.Value) m_Neigung = Convert.ToInt32(row["Neigung"]);
                if (dt.Columns.Contains("Azimut") && row["Azimut"] != DBNull.Value) m_Azimut = Convert.ToInt32(row["Azimut"]);
                if (dt.Columns.Contains("ID_PUFFER") && row["ID_PUFFER"] != DBNull.Value) ID_PUFFER = Convert.ToInt32(row["ID_PUFFER"]);

            }
        }
         
    }
}