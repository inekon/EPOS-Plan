using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class BHKWCtrl : BHKWModel
    {
        public BHKWModel model;

        // --- Statische Texte (beibehalten) ---
        public static string[] BrennstoffartText = { "Öl", "Gas", "Biogas", "Rapsöl", "Holz/Pellet", "Sonstiges", "", "", "Flüssiggas", "", "", "Bioerdgas", "", "", "", "Strom" };
        public static string[] LeistungText = { "kleiner 20 kW", "20 bis 40 kW", "40 bis 80 kW", "80 bis 200 kW", "200 bis 500 kW", "500 bis 800 kW", "800 bis 1200 kW", "größer 1200 kW" };
        public static string[] LeistungFilterText = { "Ptherm LIKE '%'", "Ptherm<20", "Ptherm>=20 and Ptherm<40", "Ptherm>=40 and Ptherm<80", "Ptherm>=80 and Ptherm<200",
                                                      "Ptherm>=200 and Ptherm<500", "Ptherm>=500 and Ptherm<800", "Ptherm>=800 and Ptherm<1200", "Ptherm>=1200" };

        // --- Kompatibilitäts-Layer für bestehenden UI-Code ---
        private List<BHKWModel> _internalList = new List<BHKWModel>();
        private bool _hasSingleData = false;

        // Simuliert die alte 'rows' Variable
        public new int rows => _internalList.Count > 0 ? _internalList.Count : (_hasSingleData ? 1 : 0);

        // Simuliert das alte 'items' Array (für Index-Zugriffe im UI)
        public List<BHKWModel> items => _internalList;

        public BHKWCtrl()
        {
            _hasSingleData = false;
        }

        #region --- DATABASE OPERATIONS ---

        public void ReadAll(string szFilter = "")
        {
            string sql = "SELECT * FROM Tab_BHKW";
            if (!string.IsNullOrEmpty(szFilter))
            {
                sql += " WHERE " + szFilter;
            }
            sql += " ORDER BY Bezeichner";

            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();
            _hasSingleData = false;

            foreach (DataRow row in dt.Rows)
            {
                _internalList.Add(MapRowToModel(row));
            }
        }

        public void ReadSingle(int ID)
        {
            string sql = "SELECT * FROM Tab_BHKW WHERE ID = ?";
            OleDbParameter[] ps = { new OleDbParameter("@id", ID) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            if (dt.Rows.Count > 0)
            {
                MapThisToRow(dt.Rows[0]);
                _hasSingleData = true;
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            string sql = "SELECT * FROM Tab_BHKW WHERE Bezeichner = ?";
            OleDbParameter[] ps = { new OleDbParameter("@name", szBezeichner) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            if (dt.Rows.Count > 0)
            {
                MapThisToRow(dt.Rows[0]);
                _hasSingleData = true;
            }
        }

        public bool Update()
        {
            // Hinweis: Bezeichner wird hier als Key verwendet
            string sql = @"UPDATE Tab_BHKW SET 
                            Beschreibung=?, Firma=?, Motortyp=?, Ptherm=?, Pel=?, 
                            Brennstoff=?, Wirkungsgrad=?, Investition_kwel=?, Raumbedarf=?, 
                            Wartungskosten_kwhel=?, Nutzungsdauer=?, NOx=?, SO2=?, CO=?, 
                            CO2=?, Staub=?, Grenzleistung=?, Kosten_Modul=?, Kosten_Montage=?, 
                            Kosten_Lieferung=?, Kosten_Schallschutzhaube=?, Kosten_Abgasreinigung=?
                           WHERE Bezeichner=?";

            OleDbParameter[] ps = {
                new OleDbParameter("@besch", m_szBeschreibung ?? ""),
                new OleDbParameter("@firma", m_szFirma ?? ""),
                new OleDbParameter("@motor", m_szMotortyp ?? ""),
                new OleDbParameter("@ptherm", m_Ptherm),
                new OleDbParameter("@pel", m_Pel),
                new OleDbParameter("@brenn", m_Brennstoff),
                new OleDbParameter("@wirk", m_Wirkungsgrad),
                new OleDbParameter("@inv", m_Investition_KWel),
                new OleDbParameter("@raum", m_Raumbedarf),
                new OleDbParameter("@wart", m_Wartungskosten_kWhel),
                new OleDbParameter("@nutz", m_Nutzungsdauer),
                new OleDbParameter("@nox", m_NOx),
                new OleDbParameter("@so2", m_SO2),
                new OleDbParameter("@co", m_CO),
                new OleDbParameter("@co2", m_CO2),
                new OleDbParameter("@staub", m_Staub),
                new OleDbParameter("@grenz", m_Grenzleistung),
                new OleDbParameter("@modul", m_Kosten_Modul),
                new OleDbParameter("@mont", m_Kosten_Montage),
                new OleDbParameter("@lief", m_Kosten_Lieferung),
                new OleDbParameter("@schall", m_Kosten_Schallschutzhaube),
                new OleDbParameter("@abgas", m_Kosten_Abgasreinigung),
                new OleDbParameter("@key", m_szBezeichner)
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        #endregion

        #region --- UI FILL METHODS ---

        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            foreach (var item in _internalList)
            {
                ctrl.Items.Add(item.m_szBezeichner);
            }
        }

        #endregion

        #region --- MAPPING HELPERS ---

        private BHKWModel MapRowToModel(DataRow row)
        {
            BHKWModel m = new BHKWModel();
            m.m_ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0;
            m.m_szBezeichner = row["Bezeichner"].ToString();
            m.m_szFirma = row["Firma"].ToString();
            m.m_szBeschreibung = row["Beschreibung"].ToString();
            m.m_Ptherm = row["Ptherm"] != DBNull.Value ? Convert.ToDouble(row["Ptherm"]) : 0;
            m.m_Pel = row["Pel"] != DBNull.Value ? Convert.ToDouble(row["Pel"]) : 0;
            m.m_Brennstoff = row["Brennstoff"] != DBNull.Value ? Convert.ToInt32(row["Brennstoff"]) : 0;
            m.m_Wirkungsgrad = row["Wirkungsgrad"] != DBNull.Value ? Convert.ToDouble(row["Wirkungsgrad"]) : 0;
            m.m_Investition_KWel = row["Investition_kwel"] != DBNull.Value ? Convert.ToDouble(row["Investition_kwel"]) : 0;
            m.m_Raumbedarf = row["Raumbedarf"] != DBNull.Value ? Convert.ToDouble(row["Raumbedarf"]) : 0;
            m.m_Wartungskosten_kWhel = row["Wartungskosten_kwhel"] != DBNull.Value ? Convert.ToDouble(row["Wartungskosten_kwhel"]) : 0;
            m.m_Nutzungsdauer = row["Nutzungsdauer"] != DBNull.Value ? Convert.ToInt32(row["Nutzungsdauer"]) : 0;
            m.m_NOx = row["NOx"] != DBNull.Value ? Convert.ToInt32(row["NOx"]) : 0;
            m.m_SO2 = row["SO2"] != DBNull.Value ? Convert.ToInt32(row["SO2"]) : 0;
            m.m_CO = row["CO"] != DBNull.Value ? Convert.ToInt32(row["CO"]) : 0;
            m.m_CO2 = row["CO2"] != DBNull.Value ? Convert.ToInt32(row["CO2"]) : 0;
            m.m_Staub = row["Staub"] != DBNull.Value ? Convert.ToInt32(row["Staub"]) : 0;
            m.m_szMotortyp = row["Motortyp"].ToString();
            m.m_Grenzleistung = row["Grenzleistung"] != DBNull.Value ? Convert.ToDouble(row["Grenzleistung"]) : 0;
            m.m_Kosten_Modul = row["Kosten_Modul"] != DBNull.Value ? Convert.ToDouble(row["Kosten_Modul"]) : 0;
            m.m_Kosten_Montage = row["Kosten_Montage"] != DBNull.Value ? Convert.ToDouble(row["Kosten_Montage"]) : 0;
            m.m_Kosten_Lieferung = row["Kosten_Lieferung"] != DBNull.Value ? Convert.ToDouble(row["Kosten_Lieferung"]) : 0;
            m.m_Kosten_Schallschutzhaube = row["Kosten_Schallschutzhaube"] != DBNull.Value ? Convert.ToDouble(row["Kosten_Schallschutzhaube"]) : 0;
            m.m_Kosten_Abgasreinigung = row["Kosten_Abgasreinigung"] != DBNull.Value ? Convert.ToDouble(row["Kosten_Abgasreinigung"]) : 0;
            return m;
        }

        private void MapThisToRow(DataRow row)
        {
            BHKWModel m = MapRowToModel(row);
            this.m_ID = m.m_ID;
            this.m_szBezeichner = m.m_szBezeichner;
            this.m_szFirma = m.m_szFirma;
            this.m_szBeschreibung = m.m_szBeschreibung;
            this.m_Ptherm = m.m_Ptherm;
            this.m_Pel = m.m_Pel;
            this.m_Brennstoff = m.m_Brennstoff;
            this.m_Wirkungsgrad = m.m_Wirkungsgrad;
            this.m_Investition_KWel = m.m_Investition_KWel;
            this.m_Raumbedarf = m.m_Raumbedarf;
            this.m_Wartungskosten_kWhel = m.m_Wartungskosten_kWhel;
            this.m_Nutzungsdauer = m.m_Nutzungsdauer;
            this.m_NOx = m.m_NOx;
            this.m_SO2 = m.m_SO2;
            this.m_CO = m.m_CO;
            this.m_CO2 = m.m_CO2;
            this.m_Staub = m.m_Staub;
            this.m_szMotortyp = m.m_szMotortyp;
            this.m_Grenzleistung = m.m_Grenzleistung;
            this.m_Kosten_Modul = m.m_Kosten_Modul;
            this.m_Kosten_Montage = m.m_Kosten_Montage;
            this.m_Kosten_Lieferung = m.m_Kosten_Lieferung;
            this.m_Kosten_Schallschutzhaube = m.m_Kosten_Schallschutzhaube;
            this.m_Kosten_Abgasreinigung = m.m_Kosten_Abgasreinigung;
        }

        #endregion
    }
}