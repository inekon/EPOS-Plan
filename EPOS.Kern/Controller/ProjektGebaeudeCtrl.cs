using System;
using System.Collections.Generic;
using System.Data;

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
            DbParam parameter = new DbParam("?", ID_Projekt);

            // Daten über das Repository laden
            DataTable dt = DataRepository.GetDataTable(sql, parameter);
            _internalList.Clear();

            if (dt != null)
            {
                // NAMENSLESER (Gebaeudespalten-Schritt M3, Schemaschritt 101): Die Spalten
                // werden bei ihrem Namen gelesen, nicht an ihrer Stelle 0..57 - die Namen
                // stehen in GebaeudeSchema.SICHT_BESTAND und sind dieselben, aus denen
                // GebaeudeSchema.SQL_VIEW_NEU die Sicht baut. Die acht Bezeichner mit
                // Umlaut oder Eszett stehen buchstabengetreu (BETRIEB_SQLITE.md 6.1).
                // Von den fuenfzehn neuen Spalten liest dieser Leser allein den Rechenweg
                // (Gebaeude_Modell) - die Weiche der Gebaeudebedarfsrechnung (Stufe G1.0).
                bool mitRechenweg = dt.Columns.Contains(GebaeudeSchema.SPALTE_GEBAEUDE_MODELL);
                foreach (DataRow row in dt.Rows)
                {
                    ProjektGebaeudeModel item = new ProjektGebaeudeModel();

                    if (row["ID_Projekt"] != DBNull.Value) item.ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);
                    if (row["Wohnflaeche_Waermebedarf"] != DBNull.Value) item.Z_AuswahlWohnflaeche = Convert.ToDouble(row["Wohnflaeche_Waermebedarf"]);
                    if (row["Einheit_Waermebedarf_Wohnflaeche"] != DBNull.Value) item.Einheit = row["Einheit_Waermebedarf_Wohnflaeche"].ToString();
                    if (row["Jahresnutzungsgrad"] != DBNull.Value) item.Jahresnutzungsgrad = Convert.ToDouble(row["Jahresnutzungsgrad"]);
                    if (row["dezWarmwasserbereitung"] != DBNull.Value) item.DezentralWarmwasser = Convert.ToBoolean(row["dezWarmwasserbereitung"]);
                    if (row["Gebaeudename"] != DBNull.Value) item.Gebaeudename = row["Gebaeudename"].ToString();
                    if (row["Typ"] != DBNull.Value) item.Typ = row["Typ"].ToString();
                    if (row["Beschreibung"] != DBNull.Value) item.Beschreibung = row["Beschreibung"].ToString();
                    if (row["Wohnflaeche_gesamt"] != DBNull.Value) item.Wohnflaeche_gesamt = Convert.ToDouble(row["Wohnflaeche_gesamt"]);
                    if (row["Bewohner"] != DBNull.Value) item.Bewohner = Convert.ToDouble(row["Bewohner"]);
                    if (row["Flaeche_Nutzer"] != DBNull.Value) item.Flaeche_Nutzer = Convert.ToDouble(row["Flaeche_Nutzer"]);
                    if (row["Interne_Waermegewinne"] != DBNull.Value) item.Interne_Waermegewinne = Convert.ToDouble(row["Interne_Waermegewinne"]);
                    if (row["Bauweise"] != DBNull.Value) item.Bauweise = Convert.ToDouble(row["Bauweise"]);
                    if (row["Fensterflaeche_Sued"] != DBNull.Value) item.Fensterflaeche_Sued = Convert.ToDouble(row["Fensterflaeche_Sued"]);
                    if (row["Fensterflaeche_Ost_West"] != DBNull.Value) item.Fensterflaeche_OstWest = Convert.ToDouble(row["Fensterflaeche_Ost_West"]);
                    if (row["Fensterflaeche_Nord"] != DBNull.Value) item.Fensterflaeche_Nord = Convert.ToDouble(row["Fensterflaeche_Nord"]);
                    if (row["Fensterdurchlassgrad"] != DBNull.Value) item.Fensterdurchlassgrad = Convert.ToDouble(row["Fensterdurchlassgrad"]);
                    if (row["Raumsolltemperatur_Nachtabsenkung"] != DBNull.Value) item.Raumsolltemperatur_Nachtabsenkung = Convert.ToDouble(row["Raumsolltemperatur_Nachtabsenkung"]);
                    if (row["Raumsolltemperatur_Tag"] != DBNull.Value) item.Raumsolltemperatur_Tag = Convert.ToDouble(row["Raumsolltemperatur_Tag"]);
                    if (row["Raumsolltemperatur_Wochenende"] != DBNull.Value) item.Raumsolltemperatur_Wochenende = Convert.ToDouble(row["Raumsolltemperatur_Wochenende"]);
                    if (row["Raumsolltemperatur_Ferien"] != DBNull.Value) item.Raumsolltemperatur_Ferien = Convert.ToDouble(row["Raumsolltemperatur_Ferien"]);
                    if (row["Maximaleraumtemperatur"] != DBNull.Value) item.Maximaleraumtemperatur = Convert.ToDouble(row["Maximaleraumtemperatur"]);
                    if (row["k_Wert_Außenwand"] != DBNull.Value) item.k_Wert_Außenwand = Convert.ToDouble(row["k_Wert_Außenwand"]);
                    if (row["k_Wert_Fenster"] != DBNull.Value) item.k_Wert_Fenster = Convert.ToDouble(row["k_Wert_Fenster"]);
                    if (row["k_Wert_Dachflaeche"] != DBNull.Value) item.k_Wert_Dachflaeche = Convert.ToDouble(row["k_Wert_Dachflaeche"]);
                    if (row["k_Wert_Grundflaeche"] != DBNull.Value) item.k_Wert_Grundflaeche = Convert.ToDouble(row["k_Wert_Grundflaeche"]);
                    if (row["k_Wert_Sonstiges"] != DBNull.Value) item.k_Wert_Sonstiges = Convert.ToDouble(row["k_Wert_Sonstiges"]);
                    if (row["Flaeche_Außenwand"] != DBNull.Value) item.Flaeche_Außenwand = Convert.ToDouble(row["Flaeche_Außenwand"]);
                    if (row["gesamte_Fensterflaeche"] != DBNull.Value) item.gesamte_Fensterflaeche = Convert.ToDouble(row["gesamte_Fensterflaeche"]);
                    if (row["Dachflaeche"] != DBNull.Value) item.Dachflaeche = Convert.ToDouble(row["Dachflaeche"]);
                    if (row["Grundflaeche"] != DBNull.Value) item.Grundflaeche = Convert.ToDouble(row["Grundflaeche"]);
                    if (row["Sonstige_Flaechen"] != DBNull.Value) item.Sonstige_Flaechen = Convert.ToDouble(row["Sonstige_Flaechen"]);
                    if (row["Nutzflaeche"] != DBNull.Value) item.Nutzflaeche = Convert.ToDouble(row["Nutzflaeche"]);
                    if (row["Raumhoehe"] != DBNull.Value) item.Raumhoehe = Convert.ToDouble(row["Raumhoehe"]);
                    if (row["WBVK_Anschluß_Fenster_Wand"] != DBNull.Value) item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand = Convert.ToDouble(row["WBVK_Anschluß_Fenster_Wand"]);
                    if (row["WBVK_Anschluß_Wand_Dach"] != DBNull.Value) item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach = Convert.ToDouble(row["WBVK_Anschluß_Wand_Dach"]);
                    if (row["WBVK_Anschluß_Außenwand_Kellerdecke"] != DBNull.Value) item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke = Convert.ToDouble(row["WBVK_Anschluß_Außenwand_Kellerdecke"]);
                    if (row["Abmessung_Anschluß_Fenster_Wand"] != DBNull.Value) item.Abmessung_Anschluß_Fenster_Wand = Convert.ToDouble(row["Abmessung_Anschluß_Fenster_Wand"]);
                    if (row["Abmessung_Anschluß_Wand_Dach"] != DBNull.Value) item.Abmessung_Anschluß_Wand_Dach = Convert.ToDouble(row["Abmessung_Anschluß_Wand_Dach"]);
                    if (row["Abmessung_Anschluß_Außenwand_Kellerdecke"] != DBNull.Value) item.Abmessung_Anschluß_Außenwand_Kellerdecke = Convert.ToDouble(row["Abmessung_Anschluß_Außenwand_Kellerdecke"]);
                    if (row["Luftwechselrate"] != DBNull.Value) item.Luftwechselrate = Convert.ToDouble(row["Luftwechselrate"]);
                    if (row["Wochenende"] != DBNull.Value) item.Wochenende = Convert.ToDouble(row["Wochenende"]);
                    if (row["Ferien"] != DBNull.Value) item.Ferien = Convert.ToDouble(row["Ferien"]);
                    if (row["Ferienbeginn_1"] != DBNull.Value) item.Ferienbeginn_1 = Convert.ToDouble(row["Ferienbeginn_1"]);
                    if (row["Ferienende_1"] != DBNull.Value) item.Ferienende_1 = Convert.ToDouble(row["Ferienende_1"]);
                    if (row["Ferienbeginn_2"] != DBNull.Value) item.Ferienbeginn_2 = Convert.ToDouble(row["Ferienbeginn_2"]);
                    if (row["Ferienende_2"] != DBNull.Value) item.Ferienende_2 = Convert.ToDouble(row["Ferienende_2"]);
                    if (row["Ferienbeginn_3"] != DBNull.Value) item.Ferienbeginn_3 = Convert.ToDouble(row["Ferienbeginn_3"]);
                    if (row["Ferienende_3"] != DBNull.Value) item.Ferienende_3 = Convert.ToDouble(row["Ferienende_3"]);
                    if (row["Ferienbeginn_4"] != DBNull.Value) item.Ferienbeginn_4 = Convert.ToDouble(row["Ferienbeginn_4"]);
                    if (row["Ferienende_4"] != DBNull.Value) item.Ferienende_4 = Convert.ToDouble(row["Ferienende_4"]);
                    if (row["WW_Bedarf"] != DBNull.Value) item.WW_Bedarf = Convert.ToDouble(row["WW_Bedarf"]);
                    if (row["spez_Waermeverbrauch"] != DBNull.Value) item.spez_Waermeverbrauch = Convert.ToDouble(row["spez_Waermeverbrauch"]);
                    if (row["Waermebedarf"] != DBNull.Value) item.Waermebedarf = Convert.ToDouble(row["Waermebedarf"]);
                    if (row["Baualtersklasse"] != DBNull.Value) item.Baualtersklasse = row["Baualtersklasse"].ToString();
                    if (row["Gebaeudeart"] != DBNull.Value) item.Gebaeudeart = row["Gebaeudeart"].ToString();
                    if (row["Wohngebaeude_Nicht_Wohngebaeude"] != DBNull.Value) item.Wohngebaeude_Nicht_Wohngebaeude = row["Wohngebaeude_Nicht_Wohngebaeude"].ToString();
                    if (row["ID"] != DBNull.Value) item.ID_Gebaeude = Convert.ToInt32(row["ID"]);
                    if (mitRechenweg && row[GebaeudeSchema.SPALTE_GEBAEUDE_MODELL] != DBNull.Value) item.Gebaeude_Modell = row[GebaeudeSchema.SPALTE_GEBAEUDE_MODELL].ToString();

                    _internalList.Add(item);
                }
            }
        }

        #endregion
    }
}