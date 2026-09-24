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

                    // Die uebrigen vierzehn neuen Spalten (Stufe G1, Anbindung des VDI-Wegs):
                    // NULL-ERHALTEND gelesen - null heisst "Vorgabe des Eingangsbauers",
                    // nicht 0. Auf einer Sicht ohne die Spalten bleibt alles null.
                    if (mitRechenweg)
                    {
                        item.Fensterflaeche_Ost = ZahlOderNull(row, GebaeudeSchema.SPALTE_FENSTERFLAECHE_OST);
                        item.Fensterflaeche_West = ZahlOderNull(row, GebaeudeSchema.SPALTE_FENSTERFLAECHE_WEST);
                        item.Rahmenanteil = ZahlOderNull(row, GebaeudeSchema.SPALTE_RAHMENANTEIL);
                        item.Verschattungsfaktor = ZahlOderNull(row, GebaeudeSchema.SPALTE_VERSCHATTUNGSFAKTOR);
                        item.Grundflaeche_Randbedingung = TextOderNull(row, GebaeudeSchema.SPALTE_GRUNDFLAECHE_RANDBEDINGUNG);
                        item.Kellertemperatur = ZahlOderNull(row, GebaeudeSchema.SPALTE_KELLERTEMPERATUR);
                        item.Masseanteil_Aussen = ZahlOderNull(row, GebaeudeSchema.SPALTE_MASSEANTEIL_AUSSEN);
                        item.Innenflaechenfaktor = ZahlOderNull(row, GebaeudeSchema.SPALTE_INNENFLAECHENFAKTOR);
                        item.Heizung_Strahlungsanteil = ZahlOderNull(row, GebaeudeSchema.SPALTE_HEIZUNG_STRAHLUNGSANTEIL);
                        item.Heizleistung_Max = ZahlOderNull(row, GebaeudeSchema.SPALTE_HEIZLEISTUNG_MAX);
                        item.Aussenbauteile_Strahlung = Schalter(row, GebaeudeSchema.SPALTE_AUSSENBAUTEILE_STRAHLUNG);
                        item.Luftwechsel_Infiltration = ZahlOderNull(row, GebaeudeSchema.SPALTE_LUFTWECHSEL_INFILTRATION);
                        item.Luftwechsel_Nutzer = ZahlOderNull(row, GebaeudeSchema.SPALTE_LUFTWECHSEL_NUTZER);
                        item.Sommerlueftung = Schalter(row, GebaeudeSchema.SPALTE_SOMMERLUEFTUNG);
                    }

                    // Die vier Kuehleingaben aus KU-S1 (Schemaschritt 108, zweiter
                    // Sichtneubau): NULL-ERHALTEND beim Namen gelesen - auf einer Sicht ohne
                    // die Spalten bleibt alles null bzw. aus. Der Loeser nimmt Sollwert und
                    // Grenze nur mit dem Projektschalter (GebaeudeModellEingang.KuehlungWirksam).
                    item.Kuehl_Sollwert = ZahlOderNull(row, GebaeudeSchema.SPALTE_KUEHL_SOLLWERT);
                    item.Kuehlleistung_Max = ZahlOderNull(row, GebaeudeSchema.SPALTE_KUEHLLEISTUNG_MAX);
                    item.Kuehlung_Aktiv = Schalter(row, GebaeudeSchema.SPALTE_KUEHLUNG_AKTIV);
                    item.Kuehl_Sollwert_Nacht = ZahlOderNull(row, GebaeudeSchema.SPALTE_KUEHL_SOLLWERT_NACHT);

                    // Die dreizehn Spalten der Waermeuebergabe aus AK-S1 (Schemaschritt 122,
                    // dritter Sichtneubau): NULL-ERHALTEND beim Namen gelesen - auf einer Sicht
                    // ohne die Spalten bleibt alles null bzw. aus. Kein Rechenweg liest sie in
                    // der ersten Welle von AK1.
                    item.Heizkreis_Aktiv = Schalter(row, GebaeudeSchema.SPALTE_HEIZKREIS_AKTIV);
                    item.Uebergabe_Art = TextOderNull(row, GebaeudeSchema.SPALTE_UEBERGABE_ART);
                    item.Uebergabe_Exponent = ZahlOderNull(row, GebaeudeSchema.SPALTE_UEBERGABE_EXPONENT);
                    item.Uebergabe_Leistung_Nenn = ZahlOderNull(row, GebaeudeSchema.SPALTE_UEBERGABE_LEISTUNG_NENN);
                    item.Auslegung_Vorlauf = ZahlOderNull(row, GebaeudeSchema.SPALTE_AUSLEGUNG_VORLAUF);
                    item.Auslegung_Ruecklauf = ZahlOderNull(row, GebaeudeSchema.SPALTE_AUSLEGUNG_RUECKLAUF);
                    item.Auslegung_Raumtemperatur = ZahlOderNull(row, GebaeudeSchema.SPALTE_AUSLEGUNG_RAUMTEMPERATUR);
                    item.Auslegung_Aussentemperatur = ZahlOderNull(row, GebaeudeSchema.SPALTE_AUSLEGUNG_AUSSENTEMPERATUR);
                    item.Heizkurve_Aktiv = Schalter(row, GebaeudeSchema.SPALTE_HEIZKURVE_AKTIV);
                    item.Heizkurve_Niveau = ZahlOderNull(row, GebaeudeSchema.SPALTE_HEIZKURVE_NIVEAU);
                    item.Heizkurve_Steilheit = ZahlOderNull(row, GebaeudeSchema.SPALTE_HEIZKURVE_STEILHEIT);
                    item.Regler_Proportionalband = ZahlOderNull(row, GebaeudeSchema.SPALTE_REGLER_PROPORTIONALBAND);
                    item.Sollwertprofil = TextOderNull(row, GebaeudeSchema.SPALTE_SOLLWERTPROFIL);

                    // Die acht Spalten der Kuehluebergabe aus KAK-S1 (E37, vierter Sichtneubau):
                    // NULL-ERHALTEND beim Namen gelesen - auf einer Sicht ohne die Spalten bleibt
                    // alles null bzw. aus.
                    item.Kuehluebergabe_Aktiv = Schalter(row, GebaeudeSchema.SPALTE_KUEHLUEBERGABE_AKTIV);
                    item.Kuehl_Uebergabe_Art = TextOderNull(row, GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_ART);
                    item.Kuehl_Uebergabe_Exponent = ZahlOderNull(row, GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_EXPONENT);
                    item.Kuehl_Uebergabe_Leistung_Nenn = ZahlOderNull(row, GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_LEISTUNG_NENN);
                    item.Kuehl_Auslegung_Vorlauf = ZahlOderNull(row, GebaeudeSchema.SPALTE_KUEHL_AUSLEGUNG_VORLAUF);
                    item.Kuehl_Auslegung_Ruecklauf = ZahlOderNull(row, GebaeudeSchema.SPALTE_KUEHL_AUSLEGUNG_RUECKLAUF);
                    item.Kuehl_Auslegung_Raumtemperatur = ZahlOderNull(row, GebaeudeSchema.SPALTE_KUEHL_AUSLEGUNG_RAUMTEMPERATUR);
                    item.Kuehl_Vorlaufgrenze = ZahlOderNull(row, GebaeudeSchema.SPALTE_KUEHL_VORLAUFGRENZE);

                    _internalList.Add(item);
                }
            }
        }

        private static double? ZahlOderNull(DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte) || row[spalte] == DBNull.Value) return null;
            return Convert.ToDouble(row[spalte]);
        }

        private static string TextOderNull(DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte) || row[spalte] == DBNull.Value) return null;
            string text = row[spalte].ToString();
            return text.Length == 0 ? null : text;
        }

        private static bool Schalter(DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte) || row[spalte] == DBNull.Value) return false;
            return Convert.ToInt64(row[spalte]) != 0;
        }

        #endregion
    }
}