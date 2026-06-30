using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class ProjektGebaeudeCtrl : ProjektGebaeudeModel
    {
        // --- Kompatibilitäts-Layer nach Vorbild (List gesteuert) ---
        private List<ProjektGebaeudeModel> _internalList = new List<ProjektGebaeudeModel>();

        public int rows => _internalList.Count;
        public new List<ProjektGebaeudeModel> items => _internalList;

        public ProjektGebaeudeModel model;

        public ProjektGebaeudeCtrl()
        {
            model = new ProjektGebaeudeModel();
        }

        // Destruktor wurde gelöscht, da kein DBCommand mehr bereinigt werden muss!

        #region --- DATABASE READ OPERATIONS ---

        public void ReadAll(int ID_Projekt)
        {
            // Sicherer parametrisierter SQL-String statt ungeschützter String-Verkettung
            string sql = "SELECT * FROM Abfrage_Projektgebaeude WHERE ID_Projekt = ?";
            OleDbParameter parameter = new OleDbParameter("?", ID_Projekt);

            // Daten über das Repository laden
            DataTable dt = DataRepository.GetDataTable(sql, parameter);
            _internalList.Clear();

            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    ProjektGebaeudeModel item = new ProjektGebaeudeModel();

                    if (row[0] != DBNull.Value) item.ID_Projekt = Convert.ToInt32(row[0]);
                    if (row[1] != DBNull.Value) item.Z_AuswahlWohnflaeche = Convert.ToDouble(row[1]);
                    if (row[2] != DBNull.Value) item.Einheit = row[2].ToString();
                    if (row[3] != DBNull.Value) item.Jahresnutzungsgrad = Convert.ToDouble(row[3]);
                    if (row[4] != DBNull.Value) item.DezentralWarmwasser = Convert.ToBoolean(row[4]);
                    if (row[5] != DBNull.Value) item.Gebaeudename = row[5].ToString();
                    if (row[6] != DBNull.Value) item.Typ = row[6].ToString();
                    if (row[7] != DBNull.Value) item.Beschreibung = row[7].ToString();
                    if (row[8] != DBNull.Value) item.Wohnflaeche_gesamt = Convert.ToDouble(row[8]);
                    if (row[9] != DBNull.Value) item.Bewohner = Convert.ToDouble(row[9]);
                    if (row[10] != DBNull.Value) item.Flaeche_Nutzer = Convert.ToDouble(row[10]);
                    if (row[11] != DBNull.Value) item.Interne_Waermegewinne = Convert.ToDouble(row[11]);
                    if (row[12] != DBNull.Value) item.Bauweise = Convert.ToDouble(row[12]);
                    if (row[13] != DBNull.Value) item.Fensterflaeche_Sued = Convert.ToDouble(row[13]);
                    if (row[14] != DBNull.Value) item.Fensterflaeche_Ost = Convert.ToDouble(row[14]);
                    if (row[15] != DBNull.Value) item.Fensterflaeche_Nord = Convert.ToDouble(row[15]);
                    if (row[16] != DBNull.Value) item.Fensterdurchlassgrad = Convert.ToDouble(row[16]);
                    if (row[17] != DBNull.Value) item.Raumsolltemperatur_Nachtabsenkung = Convert.ToDouble(row[17]);
                    if (row[18] != DBNull.Value) item.Raumsolltemperatur_Tag = Convert.ToDouble(row[18]);
                    if (row[19] != DBNull.Value) item.Raumsolltemperatur_Wochenende = Convert.ToDouble(row[19]);
                    if (row[20] != DBNull.Value) item.Raumsolltemperatur_Ferien = Convert.ToDouble(row[20]);
                    if (row[21] != DBNull.Value) item.Maximaleraumtemperatur = Convert.ToDouble(row[21]);
                    if (row[22] != DBNull.Value) item.k_Wert_Außenwand = Convert.ToDouble(row[22]);
                    if (row[23] != DBNull.Value) item.k_Wert_Fenster = Convert.ToDouble(row[23]);
                    if (row[24] != DBNull.Value) item.k_Wert_Dachflaeche = Convert.ToDouble(row[24]);
                    if (row[25] != DBNull.Value) item.k_Wert_Grundflaeche = Convert.ToDouble(row[25]);
                    if (row[26] != DBNull.Value) item.k_Wert_Sonstiges = Convert.ToDouble(row[26]);
                    if (row[27] != DBNull.Value) item.Flaeche_Außenwand = Convert.ToDouble(row[27]);
                    if (row[28] != DBNull.Value) item.gesamte_Fensterflaeche = Convert.ToDouble(row[28]);
                    if (row[29] != DBNull.Value) item.Dachflaeche = Convert.ToDouble(row[29]);
                    if (row[30] != DBNull.Value) item.Grundflaeche = Convert.ToDouble(row[30]);
                    if (row[31] != DBNull.Value) item.Sonstige_Flaechen = Convert.ToDouble(row[31]);
                    if (row[32] != DBNull.Value) item.Wohnflaeche = Convert.ToDouble(row[32]);
                    if (row[33] != DBNull.Value) item.Raumhoehe = Convert.ToDouble(row[33]);
                    if (row[34] != DBNull.Value) item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand = Convert.ToDouble(row[34]);
                    if (row[35] != DBNull.Value) item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach = Convert.ToDouble(row[35]);
                    if (row[36] != DBNull.Value) item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke = Convert.ToDouble(row[36]);
                    if (row[37] != DBNull.Value) item.Abmessung_Anschluß_Fenster_Wand = Convert.ToDouble(row[37]);
                    if (row[38] != DBNull.Value) item.Abmessung_Anschluß_Wand_Dach = Convert.ToDouble(row[38]);
                    if (row[39] != DBNull.Value) item.Abmessung_Anschluß_Außenwand_Kellerdecke = Convert.ToDouble(row[39]);
                    if (row[40] != DBNull.Value) item.Luftwechselrate = Convert.ToDouble(row[40]);
                    if (row[41] != DBNull.Value) item.Wochenende = Convert.ToDouble(row[41]);
                    if (row[42] != DBNull.Value) item.Ferien = Convert.ToDouble(row[42]);
                    if (row[43] != DBNull.Value) item.Ferienbeginn_1 = Convert.ToDouble(row[43]);
                    if (row[44] != DBNull.Value) item.Ferienende_1 = Convert.ToDouble(row[44]);
                    if (row[45] != DBNull.Value) item.Ferienbeginn_2 = Convert.ToDouble(row[45]);
                    if (row[46] != DBNull.Value) item.Ferienende_2 = Convert.ToDouble(row[46]);
                    if (row[47] != DBNull.Value) item.Ferienbeginn_3 = Convert.ToDouble(row[47]);
                    if (row[48] != DBNull.Value) item.Ferienende_3 = Convert.ToDouble(row[48]);
                    if (row[49] != DBNull.Value) item.Ferienbeginn_4 = Convert.ToDouble(row[49]);
                    if (row[50] != DBNull.Value) item.Ferienende_4 = Convert.ToDouble(row[50]);
                    if (row[51] != DBNull.Value) item.WW_Bedarf = Convert.ToDouble(row[51]);
                    if (row[52] != DBNull.Value) item.spez_Waermeverbrauch = Convert.ToDouble(row[52]);
                    if (row[53] != DBNull.Value) item.Waermebedarf = Convert.ToDouble(row[53]);
                    if (row[54] != DBNull.Value) item.Baualtersklasse = row[54].ToString();
                    if (row[55] != DBNull.Value) item.Gebaeudeart = row[55].ToString();
                    if (row[56] != DBNull.Value) item.Wohngebaeude_Nicht_Wohngebaeude = row[56].ToString();

                    _internalList.Add(item);
                }
            }
        }

        #endregion
    }
}