using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class GebaeudeCtrl : GebaeudeModel
    {
        public GebaeudeModel model;

        private List<GebaeudeModel> _internalList = new List<GebaeudeModel>();
        public int rows => _internalList.Count;
        public List<GebaeudeModel> items => _internalList;

        public GebaeudeCtrl()
        {
            model = new GebaeudeModel();
        }

        #region --- DATABASE OPERATIONS ---

        // Liest aus der PROJEKT-Tabelle Tab_Gebaeude (Namensfeld Gebaeudename).
        public void ReadAll(string szFilter = "Wohngebaeude_Nicht_Wohngebaeude='Wohngebaeude'")
        {
            string sql = "SELECT * FROM [Tab_Gebaeude]";
            if (!string.IsNullOrEmpty(szFilter)) sql += " WHERE " + szFilter;
            sql += " ORDER BY Gebaeudename";
            ExecuteRead(sql);
        }

        public void Read(string sql)
        {
            ExecuteRead(sql);
        }

        private void ExecuteRead(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
                _internalList.Add(MapRowToModel(row));
        }

        #endregion

        #region --- MAPPING HELPER (nach Spaltennamen, robust gegen Spaltenreihenfolge) ---

        // Mappt eine DataRow (Tab_Gebaeude ODER Tab_Gebaeude_STAMM) namensbasiert in ein GebaeudeModel.
        private GebaeudeModel MapRowToModel(DataRow row)
        {
            DataTable dt = row.Table;
            GebaeudeModel item = new GebaeudeModel();
            if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.ID = Convert.ToInt32(row["ID"]);
            // Namensfeld: Projekt = Gebaeudename, Stamm = Bezeichner
            if (dt.Columns.Contains("Gebaeudename") && row["Gebaeudename"] != DBNull.Value) item.Gebaeudename = row["Gebaeudename"].ToString();
            else if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) item.Gebaeudename = row["Bezeichner"].ToString();
            if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value) item.Typ = row["Typ"].ToString();
            if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) item.Beschreibung = row["Beschreibung"].ToString();
            if (dt.Columns.Contains("Wohnflaeche_gesamt") && row["Wohnflaeche_gesamt"] != DBNull.Value) item.Wohnflaeche_gesamt = Convert.ToDouble(row["Wohnflaeche_gesamt"]);
            if (dt.Columns.Contains("Bewohner") && row["Bewohner"] != DBNull.Value) item.Bewohner = Convert.ToDouble(row["Bewohner"]);
            if (dt.Columns.Contains("Flaeche_Nutzer") && row["Flaeche_Nutzer"] != DBNull.Value) item.Flaeche_Nutzer = Convert.ToDouble(row["Flaeche_Nutzer"]);
            if (dt.Columns.Contains("Interne_Waermegewinne") && row["Interne_Waermegewinne"] != DBNull.Value) item.Interne_Waermegewinne = Convert.ToDouble(row["Interne_Waermegewinne"]);
            if (dt.Columns.Contains("Bauweise") && row["Bauweise"] != DBNull.Value) item.Bauweise = Convert.ToDouble(row["Bauweise"]);
            if (dt.Columns.Contains("Fensterflaeche_Sued") && row["Fensterflaeche_Sued"] != DBNull.Value) item.Fensterflaeche_Sued = Convert.ToDouble(row["Fensterflaeche_Sued"]);
            if (dt.Columns.Contains("Fensterflaeche_Ost_West") && row["Fensterflaeche_Ost_West"] != DBNull.Value) item.Fensterflaeche_Ost = Convert.ToDouble(row["Fensterflaeche_Ost_West"]);
            if (dt.Columns.Contains("Fensterflaeche_Nord") && row["Fensterflaeche_Nord"] != DBNull.Value) item.Fensterflaeche_Nord = Convert.ToDouble(row["Fensterflaeche_Nord"]);
            if (dt.Columns.Contains("Fensterdurchlassgrad") && row["Fensterdurchlassgrad"] != DBNull.Value) item.Fensterdurchlassgrad = Convert.ToDouble(row["Fensterdurchlassgrad"]);
            if (dt.Columns.Contains("Raumsolltemperatur_Nachtabsenkung") && row["Raumsolltemperatur_Nachtabsenkung"] != DBNull.Value) item.Raumsolltemperatur_Nachtabsenkung = Convert.ToDouble(row["Raumsolltemperatur_Nachtabsenkung"]);
            if (dt.Columns.Contains("Raumsolltemperatur_Tag") && row["Raumsolltemperatur_Tag"] != DBNull.Value) item.Raumsolltemperatur_Tag = Convert.ToDouble(row["Raumsolltemperatur_Tag"]);
            if (dt.Columns.Contains("Raumsolltemperatur_Wochenende") && row["Raumsolltemperatur_Wochenende"] != DBNull.Value) item.Raumsolltemperatur_Wochenende = Convert.ToDouble(row["Raumsolltemperatur_Wochenende"]);
            if (dt.Columns.Contains("Raumsolltemperatur_Ferien") && row["Raumsolltemperatur_Ferien"] != DBNull.Value) item.Raumsolltemperatur_Ferien = Convert.ToDouble(row["Raumsolltemperatur_Ferien"]);
            if (dt.Columns.Contains("Maximaleraumtemperatur") && row["Maximaleraumtemperatur"] != DBNull.Value) item.Maximaleraumtemperatur = Convert.ToDouble(row["Maximaleraumtemperatur"]);
            if (dt.Columns.Contains("k_Wert_Außenwand") && row["k_Wert_Außenwand"] != DBNull.Value) item.k_Wert_Außenwand = Convert.ToDouble(row["k_Wert_Außenwand"]);
            if (dt.Columns.Contains("k_Wert_Fenster") && row["k_Wert_Fenster"] != DBNull.Value) item.k_Wert_Fenster = Convert.ToDouble(row["k_Wert_Fenster"]);
            if (dt.Columns.Contains("k_Wert_Dachflaeche") && row["k_Wert_Dachflaeche"] != DBNull.Value) item.k_Wert_Dachflaeche = Convert.ToDouble(row["k_Wert_Dachflaeche"]);
            if (dt.Columns.Contains("k_Wert_Grundflaeche") && row["k_Wert_Grundflaeche"] != DBNull.Value) item.k_Wert_Grundflaeche = Convert.ToDouble(row["k_Wert_Grundflaeche"]);
            if (dt.Columns.Contains("k_Wert_Sonstiges") && row["k_Wert_Sonstiges"] != DBNull.Value) item.k_Wert_Sonstiges = Convert.ToDouble(row["k_Wert_Sonstiges"]);
            if (dt.Columns.Contains("Flaeche_Außenwand") && row["Flaeche_Außenwand"] != DBNull.Value) item.Flaeche_Außenwand = Convert.ToDouble(row["Flaeche_Außenwand"]);
            if (dt.Columns.Contains("gesamte_Fensterflaeche") && row["gesamte_Fensterflaeche"] != DBNull.Value) item.gesamte_Fensterflaeche = Convert.ToDouble(row["gesamte_Fensterflaeche"]);
            if (dt.Columns.Contains("Dachflaeche") && row["Dachflaeche"] != DBNull.Value) item.Dachflaeche = Convert.ToDouble(row["Dachflaeche"]);
            if (dt.Columns.Contains("Grundflaeche") && row["Grundflaeche"] != DBNull.Value) item.Grundflaeche = Convert.ToDouble(row["Grundflaeche"]);
            if (dt.Columns.Contains("Sonstige_Flaechen") && row["Sonstige_Flaechen"] != DBNull.Value) item.Sonstige_Flaechen = Convert.ToDouble(row["Sonstige_Flaechen"]);
            if (dt.Columns.Contains("Wohnflaeche") && row["Wohnflaeche"] != DBNull.Value) item.Wohnflaeche = Convert.ToDouble(row["Wohnflaeche"]);
            if (dt.Columns.Contains("Raumhoehe") && row["Raumhoehe"] != DBNull.Value) item.Raumhoehe = Convert.ToDouble(row["Raumhoehe"]);
            if (dt.Columns.Contains("WBVK_Anschluß_Fenster_Wand") && row["WBVK_Anschluß_Fenster_Wand"] != DBNull.Value) item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand = Convert.ToDouble(row["WBVK_Anschluß_Fenster_Wand"]);
            if (dt.Columns.Contains("WBVK_Anschluß_Wand_Dach") && row["WBVK_Anschluß_Wand_Dach"] != DBNull.Value) item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach = Convert.ToDouble(row["WBVK_Anschluß_Wand_Dach"]);
            if (dt.Columns.Contains("WBVK_Anschluß_Außenwand_Kellerdecke") && row["WBVK_Anschluß_Außenwand_Kellerdecke"] != DBNull.Value) item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke = Convert.ToDouble(row["WBVK_Anschluß_Außenwand_Kellerdecke"]);
            if (dt.Columns.Contains("Abmessung_Anschluß_Fenster_Wand") && row["Abmessung_Anschluß_Fenster_Wand"] != DBNull.Value) item.Abmessung_Anschluß_Fenster_Wand = Convert.ToDouble(row["Abmessung_Anschluß_Fenster_Wand"]);
            if (dt.Columns.Contains("Abmessung_Anschluß_Wand_Dach") && row["Abmessung_Anschluß_Wand_Dach"] != DBNull.Value) item.Abmessung_Anschluß_Wand_Dach = Convert.ToDouble(row["Abmessung_Anschluß_Wand_Dach"]);
            if (dt.Columns.Contains("Abmessung_Anschluß_Außenwand_Kellerdecke") && row["Abmessung_Anschluß_Außenwand_Kellerdecke"] != DBNull.Value) item.Abmessung_Anschluß_Außenwand_Kellerdecke = Convert.ToDouble(row["Abmessung_Anschluß_Außenwand_Kellerdecke"]);
            if (dt.Columns.Contains("Luftwechselrate") && row["Luftwechselrate"] != DBNull.Value) item.Luftwechselrate = Convert.ToDouble(row["Luftwechselrate"]);
            if (dt.Columns.Contains("Wochenende") && row["Wochenende"] != DBNull.Value) item.Wochenende = Convert.ToDouble(row["Wochenende"]);
            if (dt.Columns.Contains("Ferien") && row["Ferien"] != DBNull.Value) item.Ferien = Convert.ToDouble(row["Ferien"]);
            if (dt.Columns.Contains("Ferienbeginn_1") && row["Ferienbeginn_1"] != DBNull.Value) item.Ferienbeginn_1 = Convert.ToDouble(row["Ferienbeginn_1"]);
            if (dt.Columns.Contains("Ferienende_1") && row["Ferienende_1"] != DBNull.Value) item.Ferienende_1 = Convert.ToDouble(row["Ferienende_1"]);
            if (dt.Columns.Contains("Ferienbeginn_2") && row["Ferienbeginn_2"] != DBNull.Value) item.Ferienbeginn_2 = Convert.ToDouble(row["Ferienbeginn_2"]);
            if (dt.Columns.Contains("Ferienende_2") && row["Ferienende_2"] != DBNull.Value) item.Ferienende_2 = Convert.ToDouble(row["Ferienende_2"]);
            if (dt.Columns.Contains("Ferienbeginn_3") && row["Ferienbeginn_3"] != DBNull.Value) item.Ferienbeginn_3 = Convert.ToDouble(row["Ferienbeginn_3"]);
            if (dt.Columns.Contains("Ferienende_3") && row["Ferienende_3"] != DBNull.Value) item.Ferienende_3 = Convert.ToDouble(row["Ferienende_3"]);
            if (dt.Columns.Contains("Ferienbeginn_4") && row["Ferienbeginn_4"] != DBNull.Value) item.Ferienbeginn_4 = Convert.ToDouble(row["Ferienbeginn_4"]);
            if (dt.Columns.Contains("Ferienende_4") && row["Ferienende_4"] != DBNull.Value) item.Ferienende_4 = Convert.ToDouble(row["Ferienende_4"]);
            if (dt.Columns.Contains("WW_Bedarf") && row["WW_Bedarf"] != DBNull.Value) item.WW_Bedarf = Convert.ToDouble(row["WW_Bedarf"]);
            if (dt.Columns.Contains("spez_Waermeverbrauch") && row["spez_Waermeverbrauch"] != DBNull.Value) item.spez_Waermeverbrauch = Convert.ToDouble(row["spez_Waermeverbrauch"]);
            if (dt.Columns.Contains("Waermebedarf") && row["Waermebedarf"] != DBNull.Value) item.Waermebedarf = Convert.ToDouble(row["Waermebedarf"]);
            if (dt.Columns.Contains("Baualtersklasse") && row["Baualtersklasse"] != DBNull.Value) item.Baualtersklasse = row["Baualtersklasse"].ToString();
            if (dt.Columns.Contains("Gebaeudeart") && row["Gebaeudeart"] != DBNull.Value) item.Gebaeudeart = row["Gebaeudeart"].ToString();
            if (dt.Columns.Contains("Wohngebaeude_Nicht_Wohngebaeude") && row["Wohngebaeude_Nicht_Wohngebaeude"] != DBNull.Value) item.Wohngebaeude_Nicht_Wohngebaeude = row["Wohngebaeude_Nicht_Wohngebaeude"].ToString();
            return item;
        }

        #endregion
    }
}
