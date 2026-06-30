using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class GebaeudeCtrl : GebaeudeModel
    {
        public GebaeudeModel model;

        // --- Kompatibilitäts-Layer ---
        private List<GebaeudeModel> _internalList = new List<GebaeudeModel>();

        // Verhindert die Warnung durch 'new' und hält die UI-Logik am Laufen
        public int rows => _internalList.Count;
        public List<GebaeudeModel> items => _internalList;
   
        public GebaeudeCtrl()
        {
            model = new GebaeudeModel();
        }

        #region --- DATABASE OPERATIONS ---

        public void ReadAll(string szFilter = "Wohngebaeude_Nicht_Wohngebaeude='Wohngebaeude'")
        {
            string sql = "SELECT * FROM [Tab_Gebaeude]";
            if (!string.IsNullOrEmpty(szFilter))
            {
                sql += " WHERE " + szFilter;
            }
            sql += " ORDER BY Gebaeudename";

            ExecuteRead(sql);
        }

        public void Read(string sql)
        {
            ExecuteRead(sql);
        }

        private void ExecuteRead(string sql)
        {
            // Nutzt das neue Repository
            DataTable dt = DataRepository.GetDataTable(sql);

            _internalList.Clear();

            foreach (DataRow row in dt.Rows)
            {
                _internalList.Add(MapRowToModel(row));
            }
        }

        #endregion

        #region --- MAPPING HELPER ---

        private GebaeudeModel MapRowToModel(DataRow row)
        {
            GebaeudeModel item = new GebaeudeModel();

            // Numerische Werte und Strings sicher konvertieren
            item.ID = row[0] != DBNull.Value ? Convert.ToInt32(row[0]) : 0;
            item.Gebaeudename = row[1]?.ToString() ?? "";
            item.Typ = row[2]?.ToString() ?? "";
            item.Beschreibung = row[3]?.ToString() ?? "";

            // Double-Werte (Spalte 4 bis 49)
            item.Wohnflaeche_gesamt = row[4] != DBNull.Value ? Convert.ToDouble(row[4]) : 0;
            item.Bewohner = row[5] != DBNull.Value ? Convert.ToDouble(row[5]) : 0;
            item.Flaeche_Nutzer = row[6] != DBNull.Value ? Convert.ToDouble(row[6]) : 0;
            item.Interne_Waermegewinne = row[7] != DBNull.Value ? Convert.ToDouble(row[7]) : 0;
            item.Bauweise = row[8] != DBNull.Value ? Convert.ToDouble(row[8]) : 0;
            item.Fensterflaeche_Sued = row[9] != DBNull.Value ? Convert.ToDouble(row[9]) : 0;
            item.Fensterflaeche_Ost = row[10] != DBNull.Value ? Convert.ToDouble(row[10]) : 0;
            item.Fensterflaeche_Nord = row[11] != DBNull.Value ? Convert.ToDouble(row[11]) : 0;
            item.Fensterdurchlassgrad = row[12] != DBNull.Value ? Convert.ToDouble(row[12]) : 0;
            item.Raumsolltemperatur_Nachtabsenkung = row[13] != DBNull.Value ? Convert.ToDouble(row[13]) : 0;
            item.Raumsolltemperatur_Tag = row[14] != DBNull.Value ? Convert.ToDouble(row[14]) : 0;
            item.Raumsolltemperatur_Wochenende = row[15] != DBNull.Value ? Convert.ToDouble(row[15]) : 0;
            item.Raumsolltemperatur_Ferien = row[16] != DBNull.Value ? Convert.ToDouble(row[16]) : 0;
            item.Maximaleraumtemperatur = row[17] != DBNull.Value ? Convert.ToDouble(row[17]) : 0;
            item.k_Wert_Außenwand = row[18] != DBNull.Value ? Convert.ToDouble(row[18]) : 0;
            item.k_Wert_Fenster = row[19] != DBNull.Value ? Convert.ToDouble(row[19]) : 0;
            item.k_Wert_Dachflaeche = row[20] != DBNull.Value ? Convert.ToDouble(row[20]) : 0;
            item.k_Wert_Grundflaeche = row[21] != DBNull.Value ? Convert.ToDouble(row[21]) : 0;
            item.k_Wert_Sonstiges = row[22] != DBNull.Value ? Convert.ToDouble(row[22]) : 0;
            item.Flaeche_Außenwand = row[23] != DBNull.Value ? Convert.ToDouble(row[23]) : 0;
            item.gesamte_Fensterflaeche = row[24] != DBNull.Value ? Convert.ToDouble(row[24]) : 0;
            item.Dachflaeche = row[25] != DBNull.Value ? Convert.ToDouble(row[25]) : 0;
            item.Grundflaeche = row[26] != DBNull.Value ? Convert.ToDouble(row[26]) : 0;
            item.Sonstige_Flaechen = row[27] != DBNull.Value ? Convert.ToDouble(row[27]) : 0;
            item.Wohnflaeche = row[28] != DBNull.Value ? Convert.ToDouble(row[28]) : 0;
            item.Raumhoehe = row[29] != DBNull.Value ? Convert.ToDouble(row[29]) : 0;
            item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand = row[30] != DBNull.Value ? Convert.ToDouble(row[30]) : 0;
            item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach = row[31] != DBNull.Value ? Convert.ToDouble(row[31]) : 0;
            item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke = row[32] != DBNull.Value ? Convert.ToDouble(row[32]) : 0;
            item.Abmessung_Anschluß_Fenster_Wand = row[33] != DBNull.Value ? Convert.ToDouble(row[33]) : 0;
            item.Abmessung_Anschluß_Wand_Dach = row[34] != DBNull.Value ? Convert.ToDouble(row[34]) : 0;
            item.Abmessung_Anschluß_Außenwand_Kellerdecke = row[35] != DBNull.Value ? Convert.ToDouble(row[35]) : 0;
            item.Luftwechselrate = row[36] != DBNull.Value ? Convert.ToDouble(row[36]) : 0;
            item.Wochenende = row[37] != DBNull.Value ? Convert.ToDouble(row[37]) : 0;
            item.Ferien = row[38] != DBNull.Value ? Convert.ToDouble(row[38]) : 0;
            item.Ferienbeginn_1 = row[39] != DBNull.Value ? Convert.ToDouble(row[39]) : 0;
            item.Ferienende_1 = row[40] != DBNull.Value ? Convert.ToDouble(row[40]) : 0;
            item.Ferienbeginn_2 = row[41] != DBNull.Value ? Convert.ToDouble(row[41]) : 0;
            item.Ferienende_2 = row[42] != DBNull.Value ? Convert.ToDouble(row[42]) : 0;
            item.Ferienbeginn_3 = row[43] != DBNull.Value ? Convert.ToDouble(row[43]) : 0;
            item.Ferienende_3 = row[44] != DBNull.Value ? Convert.ToDouble(row[44]) : 0;
            item.Ferienbeginn_4 = row[45] != DBNull.Value ? Convert.ToDouble(row[45]) : 0;
            item.Ferienende_4 = row[46] != DBNull.Value ? Convert.ToDouble(row[46]) : 0;
            item.WW_Bedarf = row[47] != DBNull.Value ? Convert.ToDouble(row[47]) : 0;
            item.spez_Waermeverbrauch = row[48] != DBNull.Value ? Convert.ToDouble(row[48]) : 0;
            item.Waermebedarf = row[49] != DBNull.Value ? Convert.ToDouble(row[49]) : 0;

            // Restliche Strings
            item.Baualtersklasse = row[50]?.ToString() ?? "";
            item.Gebaeudeart = row[51]?.ToString() ?? "";
            item.Wohngebaeude_Nicht_Wohngebaeude = row[52]?.ToString() ?? "";

            return item;
        }

        #endregion
    }
}
