using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // Controller fuer die Gebaeude-STAMMDATEN (Tab_Gebaeude_STAMM).
    // Katalog: Schluessel = ID, Namensfeld = Bezeichner (im Model als Gebaeudename gefuehrt).
    // Neues Feld ReadOnly. Enthaelt Katalog-Lesen und die Admin-Operationen (Loeschen mit Schutz).
    // Die Kopierlogik STAMM -> Projekt folgt in Etappe 2.
    class GebaeudeStammCtrl : GebaeudeModel
    {
        public const string TABLE      = "Tab_Gebaeude_STAMM";
        public const string TABLE_PROJ = "Tab_Gebaeude";

        private List<GebaeudeModel> _internalList = new List<GebaeudeModel>();
        public int rows => _internalList.Count;
        public List<GebaeudeModel> items => _internalList;

        public bool m_bReadOnly = false;

        public GebaeudeStammCtrl() { }

        #region --- READ (Katalog) ---

        public void ReadAll(string szFilter = "")
        {
            string sql = "SELECT * FROM [" + TABLE + "]";
            if (!string.IsNullOrEmpty(szFilter)) sql += " WHERE " + szFilter;
            sql += " ORDER BY Bezeichner";
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
            {
                GebaeudeModel item = new GebaeudeModel();
                FillModel(item, row);
                _internalList.Add(item);
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@bez", szBezeichner ?? (object)DBNull.Value));
            _internalList.Clear();
            if (dt != null && dt.Rows.Count > 0)
            {
                FillModel(this, dt.Rows[0]);
                _internalList.Add(this);
            }
        }

        public bool IsReadOnly(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        // Loescht einen Gebaeude-Stammdatensatz (per Bezeichner), sofern nicht schreibgeschuetzt.
        public bool Delete(string szBezeichner)
        {
            if (IsReadOnly(szBezeichner))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt");
                return false;
            }
            return DataRepository.ExecuteSQL("DELETE FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@bez", szBezeichner ?? ""));
        }

        #endregion

        #region --- MAPPING (namensbasiert) ---

        private void FillModel(GebaeudeModel item, DataRow row)
        {
            DataTable dt = row.Table;
            if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.ID = Convert.ToInt32(row["ID"]);
            if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) item.Gebaeudename = row["Bezeichner"].ToString();
            else if (dt.Columns.Contains("Gebaeudename") && row["Gebaeudename"] != DBNull.Value) item.Gebaeudename = row["Gebaeudename"].ToString();
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
            if (item == this && dt.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value)
                this.m_bReadOnly = Convert.ToBoolean(row["ReadOnly"]);
        }

        #endregion

        #region --- WRITE (Katalog: Insert / Overwrite) ---

        // Wert-Parameter in fester Spaltenreihenfolge (positionsgebunden fuer OleDb "?").
        // WICHTIG: eindeutige, praefixfreie Parameternamen (@b00..), damit der ACE-OLEDB-Provider
        // keine namensaehnlichen Parameter verwechselt (z.B. Wohnflaeche vs. Wohnflaeche_gesamt).
        private DbParam[] BuildValueParams(GebaeudeModel m)
        {
            return new DbParam[]
            {
                new DbParam("@b00", DbParamTyp.VarWChar) { Wert = (object)(m.Gebaeudename ?? "") },
                new DbParam("@b01", DbParamTyp.VarWChar) { Wert = (object)(m.Typ ?? "") },
                new DbParam("@b02", DbParamTyp.VarWChar) { Wert = (object)(m.Beschreibung ?? "") },
                new DbParam("@b03", DbParamTyp.Double) { Wert = m.Wohnflaeche_gesamt },
                new DbParam("@b04", DbParamTyp.Double) { Wert = m.Bewohner },
                new DbParam("@b05", DbParamTyp.Double) { Wert = m.Flaeche_Nutzer },
                new DbParam("@b06", DbParamTyp.Double) { Wert = m.Interne_Waermegewinne },
                new DbParam("@b07", DbParamTyp.Double) { Wert = m.Bauweise },
                new DbParam("@b08", DbParamTyp.Double) { Wert = m.Fensterflaeche_Sued },
                new DbParam("@b09", DbParamTyp.Double) { Wert = m.Fensterflaeche_Ost },
                new DbParam("@b10", DbParamTyp.Double) { Wert = m.Fensterflaeche_Nord },
                new DbParam("@b11", DbParamTyp.Double) { Wert = m.Fensterdurchlassgrad },
                new DbParam("@b12", DbParamTyp.Double) { Wert = m.Raumsolltemperatur_Nachtabsenkung },
                new DbParam("@b13", DbParamTyp.Double) { Wert = m.Raumsolltemperatur_Tag },
                new DbParam("@b14", DbParamTyp.Double) { Wert = m.Raumsolltemperatur_Wochenende },
                new DbParam("@b15", DbParamTyp.Double) { Wert = m.Raumsolltemperatur_Ferien },
                new DbParam("@b16", DbParamTyp.Double) { Wert = m.Maximaleraumtemperatur },
                new DbParam("@b17", DbParamTyp.Double) { Wert = m.k_Wert_Außenwand },
                new DbParam("@b18", DbParamTyp.Double) { Wert = m.k_Wert_Fenster },
                new DbParam("@b19", DbParamTyp.Double) { Wert = m.k_Wert_Dachflaeche },
                new DbParam("@b20", DbParamTyp.Double) { Wert = m.k_Wert_Grundflaeche },
                new DbParam("@b21", DbParamTyp.Double) { Wert = m.k_Wert_Sonstiges },
                new DbParam("@b22", DbParamTyp.Double) { Wert = m.Flaeche_Außenwand },
                new DbParam("@b23", DbParamTyp.Double) { Wert = m.gesamte_Fensterflaeche },
                new DbParam("@b24", DbParamTyp.Double) { Wert = m.Dachflaeche },
                new DbParam("@b25", DbParamTyp.Double) { Wert = m.Grundflaeche },
                new DbParam("@b26", DbParamTyp.Double) { Wert = m.Sonstige_Flaechen },
                new DbParam("@b27", DbParamTyp.Double) { Wert = m.Wohnflaeche },
                new DbParam("@b28", DbParamTyp.Double) { Wert = m.Raumhoehe },
                new DbParam("@b29", DbParamTyp.Double) { Wert = m.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand },
                new DbParam("@b30", DbParamTyp.Double) { Wert = m.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach },
                new DbParam("@b31", DbParamTyp.Double) { Wert = m.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke },
                new DbParam("@b32", DbParamTyp.Double) { Wert = m.Abmessung_Anschluß_Fenster_Wand },
                new DbParam("@b33", DbParamTyp.Double) { Wert = m.Abmessung_Anschluß_Wand_Dach },
                new DbParam("@b34", DbParamTyp.Double) { Wert = m.Abmessung_Anschluß_Außenwand_Kellerdecke },
                new DbParam("@b35", DbParamTyp.Double) { Wert = m.Luftwechselrate },
                new DbParam("@b36", DbParamTyp.Double) { Wert = m.Wochenende },
                new DbParam("@b37", DbParamTyp.Double) { Wert = m.Ferien },
                new DbParam("@b38", DbParamTyp.Double) { Wert = m.Ferienbeginn_1 },
                new DbParam("@b39", DbParamTyp.Double) { Wert = m.Ferienende_1 },
                new DbParam("@b40", DbParamTyp.Double) { Wert = m.Ferienbeginn_2 },
                new DbParam("@b41", DbParamTyp.Double) { Wert = m.Ferienende_2 },
                new DbParam("@b42", DbParamTyp.Double) { Wert = m.Ferienbeginn_3 },
                new DbParam("@b43", DbParamTyp.Double) { Wert = m.Ferienende_3 },
                new DbParam("@b44", DbParamTyp.Double) { Wert = m.Ferienbeginn_4 },
                new DbParam("@b45", DbParamTyp.Double) { Wert = m.Ferienende_4 },
                new DbParam("@b46", DbParamTyp.Double) { Wert = m.WW_Bedarf },
                new DbParam("@b47", DbParamTyp.Double) { Wert = m.spez_Waermeverbrauch },
                new DbParam("@b48", DbParamTyp.Double) { Wert = m.Waermebedarf },
                new DbParam("@b49", DbParamTyp.VarWChar) { Wert = (object)(m.Baualtersklasse ?? "") },
                new DbParam("@b50", DbParamTyp.VarWChar) { Wert = (object)(m.Gebaeudeart ?? "") },
                new DbParam("@b51", DbParamTyp.VarWChar) { Wert = (object)(m.Wohngebaeude_Nicht_Wohngebaeude ?? "") },
            };
        }

        // Legt einen neuen Gebaeude-Stammdatensatz an. ID explizit als MAX(ID)+1
        // (beim Kopieren einer Access-Tabelle wird die Autonummerierung zu einer normalen Long-Zahl).
        public bool Insert(GebaeudeModel m)
        {
            int newId = DataRepository.GetMaxID(TABLE) + 1;
            string sql = "INSERT INTO [" + TABLE + "] ([ID], [Bezeichner], [Typ], [Beschreibung], [Wohnflaeche_gesamt], [Bewohner], [Flaeche_Nutzer], [Interne_Waermegewinne], [Bauweise], [Fensterflaeche_Sued], [Fensterflaeche_Ost_West], [Fensterflaeche_Nord], [Fensterdurchlassgrad], [Raumsolltemperatur_Nachtabsenkung], [Raumsolltemperatur_Tag], [Raumsolltemperatur_Wochenende], [Raumsolltemperatur_Ferien], [Maximaleraumtemperatur], [k_Wert_Außenwand], [k_Wert_Fenster], [k_Wert_Dachflaeche], [k_Wert_Grundflaeche], [k_Wert_Sonstiges], [Flaeche_Außenwand], [gesamte_Fensterflaeche], [Dachflaeche], [Grundflaeche], [Sonstige_Flaechen], [Wohnflaeche], [Raumhoehe], [WBVK_Anschluß_Fenster_Wand], [WBVK_Anschluß_Wand_Dach], [WBVK_Anschluß_Außenwand_Kellerdecke], [Abmessung_Anschluß_Fenster_Wand], [Abmessung_Anschluß_Wand_Dach], [Abmessung_Anschluß_Außenwand_Kellerdecke], [Luftwechselrate], [Wochenende], [Ferien], [Ferienbeginn_1], [Ferienende_1], [Ferienbeginn_2], [Ferienende_2], [Ferienbeginn_3], [Ferienende_3], [Ferienbeginn_4], [Ferienende_4], [WW_Bedarf], [spez_Waermeverbrauch], [Waermebedarf], [Baualtersklasse], [Gebaeudeart], [Wohngebaeude_Nicht_Wohngebaeude], [ReadOnly]) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
            var ps = new List<DbParam>();
            ps.Add(new DbParam("@bid", DbParamTyp.Integer) { Wert = newId });
            ps.AddRange(BuildValueParams(m));
            ps.Add(new DbParam("@bro", DbParamTyp.Boolean) { Wert = false });
            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        // Ueberschreibt einen vorhandenen Stammdatensatz (Schluessel = Bezeichner), sofern nicht schreibgeschuetzt.
        public bool Overwrite(GebaeudeModel m)
        {
            if (IsReadOnly(m.Gebaeudename))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht überschrieben werden.",
                    "Schreibgeschützt");
                return false;
            }
            string sql = "UPDATE [" + TABLE + "] SET [Bezeichner] = ?, [Typ] = ?, [Beschreibung] = ?, [Wohnflaeche_gesamt] = ?, [Bewohner] = ?, [Flaeche_Nutzer] = ?, [Interne_Waermegewinne] = ?, [Bauweise] = ?, [Fensterflaeche_Sued] = ?, [Fensterflaeche_Ost_West] = ?, [Fensterflaeche_Nord] = ?, [Fensterdurchlassgrad] = ?, [Raumsolltemperatur_Nachtabsenkung] = ?, [Raumsolltemperatur_Tag] = ?, [Raumsolltemperatur_Wochenende] = ?, [Raumsolltemperatur_Ferien] = ?, [Maximaleraumtemperatur] = ?, [k_Wert_Außenwand] = ?, [k_Wert_Fenster] = ?, [k_Wert_Dachflaeche] = ?, [k_Wert_Grundflaeche] = ?, [k_Wert_Sonstiges] = ?, [Flaeche_Außenwand] = ?, [gesamte_Fensterflaeche] = ?, [Dachflaeche] = ?, [Grundflaeche] = ?, [Sonstige_Flaechen] = ?, [Wohnflaeche] = ?, [Raumhoehe] = ?, [WBVK_Anschluß_Fenster_Wand] = ?, [WBVK_Anschluß_Wand_Dach] = ?, [WBVK_Anschluß_Außenwand_Kellerdecke] = ?, [Abmessung_Anschluß_Fenster_Wand] = ?, [Abmessung_Anschluß_Wand_Dach] = ?, [Abmessung_Anschluß_Außenwand_Kellerdecke] = ?, [Luftwechselrate] = ?, [Wochenende] = ?, [Ferien] = ?, [Ferienbeginn_1] = ?, [Ferienende_1] = ?, [Ferienbeginn_2] = ?, [Ferienende_2] = ?, [Ferienbeginn_3] = ?, [Ferienende_3] = ?, [Ferienbeginn_4] = ?, [Ferienende_4] = ?, [WW_Bedarf] = ?, [spez_Waermeverbrauch] = ?, [Waermebedarf] = ?, [Baualtersklasse] = ?, [Gebaeudeart] = ?, [Wohngebaeude_Nicht_Wohngebaeude] = ? WHERE Bezeichner = ?";
            var ps = new List<DbParam>(BuildValueParams(m));
            ps.Add(new DbParam("@bkey", DbParamTyp.VarWChar) { Wert = (object)(m.Gebaeudename ?? "") });
            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        #endregion

        #region --- COPY (STAMM -> Projekt) ---

        // Kopiert einen Gebaeude-Stammdatensatz (per Bezeichner) in die Projekt-Tabelle Tab_Gebaeude.
        // Setzt ID_Projekt und die Verknuepfung ID_ProjektGebaeude (-> Z_ProjektGebaeude.ID).
        // Rueckgabe: neue Tab_Gebaeude.ID (>0) oder 0 bei Fehler / nicht gefunden.
        public int CopyFromStamm(string szBezeichner, int idProjekt, int idProjektGebaeude)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@cbez", szBezeichner ?? (object)DBNull.Value));
            if (dt == null || dt.Rows.Count == 0) return 0;
            DataRow r = dt.Rows[0];

            int newId = DataRepository.GetMaxID(TABLE_PROJ) + 1;

            string sql = "INSERT INTO [" + TABLE_PROJ + "] ([ID], [ID_ProjektGebaeude], [ID_Projekt], [Gebaeudename], [Typ], [Beschreibung], [Wohnflaeche_gesamt], [Bewohner], [Flaeche_Nutzer], [Interne_Waermegewinne], [Bauweise], [Fensterflaeche_Sued], [Fensterflaeche_Ost_West], [Fensterflaeche_Nord], [Fensterdurchlassgrad], [Raumsolltemperatur_Nachtabsenkung], [Raumsolltemperatur_Tag], [Raumsolltemperatur_Wochenende], [Raumsolltemperatur_Ferien], [Maximaleraumtemperatur], [k_Wert_Außenwand], [k_Wert_Fenster], [k_Wert_Dachflaeche], [k_Wert_Grundflaeche], [k_Wert_Sonstiges], [Flaeche_Außenwand], [gesamte_Fensterflaeche], [Dachflaeche], [Grundflaeche], [Sonstige_Flaechen], [Wohnflaeche], [Raumhoehe], [WBVK_Anschluß_Fenster_Wand], [WBVK_Anschluß_Wand_Dach], [WBVK_Anschluß_Außenwand_Kellerdecke], [Abmessung_Anschluß_Fenster_Wand], [Abmessung_Anschluß_Wand_Dach], [Abmessung_Anschluß_Außenwand_Kellerdecke], [Luftwechselrate], [Wochenende], [Ferien], [Ferienbeginn_1], [Ferienende_1], [Ferienbeginn_2], [Ferienende_2], [Ferienbeginn_3], [Ferienende_3], [Ferienbeginn_4], [Ferienende_4], [WW_Bedarf], [spez_Waermeverbrauch], [Waermebedarf], [Baualtersklasse], [Gebaeudeart], [Wohngebaeude_Nicht_Wohngebaeude]) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
            DbParam[] ps = new DbParam[]
            {
                new DbParam("@c00", DbParamTyp.Integer) { Wert = newId },
                new DbParam("@c01", DbParamTyp.Integer) { Wert = idProjektGebaeude },
                new DbParam("@c02", DbParamTyp.Integer) { Wert = idProjekt },
                new DbParam("@c03", DbParamTyp.VarWChar) { Wert = (object)(r["Bezeichner"] == DBNull.Value ? "" : r["Bezeichner"].ToString()) },
                new DbParam("@c04", DbParamTyp.VarWChar) { Wert = (object)(r["Typ"] == DBNull.Value ? "" : r["Typ"].ToString()) },
                new DbParam("@c05", DbParamTyp.VarWChar) { Wert = (object)(r["Beschreibung"] == DBNull.Value ? "" : r["Beschreibung"].ToString()) },
                new DbParam("@c06", DbParamTyp.Double) { Wert = (object)(r["Wohnflaeche_gesamt"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Wohnflaeche_gesamt"])) },
                new DbParam("@c07", DbParamTyp.Double) { Wert = (object)(r["Bewohner"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Bewohner"])) },
                new DbParam("@c08", DbParamTyp.Double) { Wert = (object)(r["Flaeche_Nutzer"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Flaeche_Nutzer"])) },
                new DbParam("@c09", DbParamTyp.Double) { Wert = (object)(r["Interne_Waermegewinne"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Interne_Waermegewinne"])) },
                new DbParam("@c10", DbParamTyp.Double) { Wert = (object)(r["Bauweise"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Bauweise"])) },
                new DbParam("@c11", DbParamTyp.Double) { Wert = (object)(r["Fensterflaeche_Sued"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Fensterflaeche_Sued"])) },
                new DbParam("@c12", DbParamTyp.Double) { Wert = (object)(r["Fensterflaeche_Ost_West"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Fensterflaeche_Ost_West"])) },
                new DbParam("@c13", DbParamTyp.Double) { Wert = (object)(r["Fensterflaeche_Nord"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Fensterflaeche_Nord"])) },
                new DbParam("@c14", DbParamTyp.Double) { Wert = (object)(r["Fensterdurchlassgrad"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Fensterdurchlassgrad"])) },
                new DbParam("@c15", DbParamTyp.Double) { Wert = (object)(r["Raumsolltemperatur_Nachtabsenkung"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Raumsolltemperatur_Nachtabsenkung"])) },
                new DbParam("@c16", DbParamTyp.Double) { Wert = (object)(r["Raumsolltemperatur_Tag"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Raumsolltemperatur_Tag"])) },
                new DbParam("@c17", DbParamTyp.Double) { Wert = (object)(r["Raumsolltemperatur_Wochenende"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Raumsolltemperatur_Wochenende"])) },
                new DbParam("@c18", DbParamTyp.Double) { Wert = (object)(r["Raumsolltemperatur_Ferien"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Raumsolltemperatur_Ferien"])) },
                new DbParam("@c19", DbParamTyp.Double) { Wert = (object)(r["Maximaleraumtemperatur"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Maximaleraumtemperatur"])) },
                new DbParam("@c20", DbParamTyp.Double) { Wert = (object)(r["k_Wert_Außenwand"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["k_Wert_Außenwand"])) },
                new DbParam("@c21", DbParamTyp.Double) { Wert = (object)(r["k_Wert_Fenster"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["k_Wert_Fenster"])) },
                new DbParam("@c22", DbParamTyp.Double) { Wert = (object)(r["k_Wert_Dachflaeche"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["k_Wert_Dachflaeche"])) },
                new DbParam("@c23", DbParamTyp.Double) { Wert = (object)(r["k_Wert_Grundflaeche"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["k_Wert_Grundflaeche"])) },
                new DbParam("@c24", DbParamTyp.Double) { Wert = (object)(r["k_Wert_Sonstiges"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["k_Wert_Sonstiges"])) },
                new DbParam("@c25", DbParamTyp.Double) { Wert = (object)(r["Flaeche_Außenwand"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Flaeche_Außenwand"])) },
                new DbParam("@c26", DbParamTyp.Double) { Wert = (object)(r["gesamte_Fensterflaeche"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["gesamte_Fensterflaeche"])) },
                new DbParam("@c27", DbParamTyp.Double) { Wert = (object)(r["Dachflaeche"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Dachflaeche"])) },
                new DbParam("@c28", DbParamTyp.Double) { Wert = (object)(r["Grundflaeche"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Grundflaeche"])) },
                new DbParam("@c29", DbParamTyp.Double) { Wert = (object)(r["Sonstige_Flaechen"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Sonstige_Flaechen"])) },
                new DbParam("@c30", DbParamTyp.Double) { Wert = (object)(r["Wohnflaeche"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Wohnflaeche"])) },
                new DbParam("@c31", DbParamTyp.Double) { Wert = (object)(r["Raumhoehe"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Raumhoehe"])) },
                new DbParam("@c32", DbParamTyp.Double) { Wert = (object)(r["WBVK_Anschluß_Fenster_Wand"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["WBVK_Anschluß_Fenster_Wand"])) },
                new DbParam("@c33", DbParamTyp.Double) { Wert = (object)(r["WBVK_Anschluß_Wand_Dach"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["WBVK_Anschluß_Wand_Dach"])) },
                new DbParam("@c34", DbParamTyp.Double) { Wert = (object)(r["WBVK_Anschluß_Außenwand_Kellerdecke"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["WBVK_Anschluß_Außenwand_Kellerdecke"])) },
                new DbParam("@c35", DbParamTyp.Double) { Wert = (object)(r["Abmessung_Anschluß_Fenster_Wand"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Abmessung_Anschluß_Fenster_Wand"])) },
                new DbParam("@c36", DbParamTyp.Double) { Wert = (object)(r["Abmessung_Anschluß_Wand_Dach"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Abmessung_Anschluß_Wand_Dach"])) },
                new DbParam("@c37", DbParamTyp.Double) { Wert = (object)(r["Abmessung_Anschluß_Außenwand_Kellerdecke"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Abmessung_Anschluß_Außenwand_Kellerdecke"])) },
                new DbParam("@c38", DbParamTyp.Double) { Wert = (object)(r["Luftwechselrate"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Luftwechselrate"])) },
                new DbParam("@c39", DbParamTyp.Double) { Wert = (object)(r["Wochenende"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Wochenende"])) },
                new DbParam("@c40", DbParamTyp.Double) { Wert = (object)(r["Ferien"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferien"])) },
                new DbParam("@c41", DbParamTyp.Double) { Wert = (object)(r["Ferienbeginn_1"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienbeginn_1"])) },
                new DbParam("@c42", DbParamTyp.Double) { Wert = (object)(r["Ferienende_1"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienende_1"])) },
                new DbParam("@c43", DbParamTyp.Double) { Wert = (object)(r["Ferienbeginn_2"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienbeginn_2"])) },
                new DbParam("@c44", DbParamTyp.Double) { Wert = (object)(r["Ferienende_2"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienende_2"])) },
                new DbParam("@c45", DbParamTyp.Double) { Wert = (object)(r["Ferienbeginn_3"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienbeginn_3"])) },
                new DbParam("@c46", DbParamTyp.Double) { Wert = (object)(r["Ferienende_3"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienende_3"])) },
                new DbParam("@c47", DbParamTyp.Double) { Wert = (object)(r["Ferienbeginn_4"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienbeginn_4"])) },
                new DbParam("@c48", DbParamTyp.Double) { Wert = (object)(r["Ferienende_4"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienende_4"])) },
                new DbParam("@c49", DbParamTyp.Double) { Wert = (object)(r["WW_Bedarf"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["WW_Bedarf"])) },
                new DbParam("@c50", DbParamTyp.Double) { Wert = (object)(r["spez_Waermeverbrauch"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["spez_Waermeverbrauch"])) },
                new DbParam("@c51", DbParamTyp.Double) { Wert = (object)(r["Waermebedarf"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Waermebedarf"])) },
                new DbParam("@c52", DbParamTyp.VarWChar) { Wert = (object)(r["Baualtersklasse"] == DBNull.Value ? "" : r["Baualtersklasse"].ToString()) },
                new DbParam("@c53", DbParamTyp.VarWChar) { Wert = (object)(r["Gebaeudeart"] == DBNull.Value ? "" : r["Gebaeudeart"].ToString()) },
                new DbParam("@c54", DbParamTyp.VarWChar) { Wert = (object)(r["Wohngebaeude_Nicht_Wohngebaeude"] == DBNull.Value ? "" : r["Wohngebaeude_Nicht_Wohngebaeude"].ToString()) },
            };
            bool ok = DataRepository.ExecuteSQL(sql, ps);

            // Tagesverteilung des Gebaeudetyps (Tab_Gebaeude.Typ) mitkopieren.
            if (ok)
                CopyTagVForGebaeude(newId, r["Typ"] == DBNull.Value ? "" : r["Typ"].ToString());

            return ok ? newId : 0;
        }


        // Kopiert die Tagesverteilung (Tab_DBTagV_STAMM + Tab_DBTagVDaten_STAMM) des Gebaeudetyps
        // (Katalog-Bezeichner == Gebaeudetyp) in die Projekt-Tabellen und verknuepft ueber ID_Gebaeude.
        // Gibt es fuer den Typ keinen Katalogeintrag, wird sauber uebersprungen.
        private void CopyTagVForGebaeude(int idGebaeude, string szTyp)
        {
            if (string.IsNullOrEmpty(szTyp)) return;

            DataTable head = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, Beschreibung FROM [Tab_DBTagV_STAMM] WHERE Bezeichner = ?",
                new DbParam("@t01", szTyp));
            if (head == null || head.Rows.Count == 0) return; // kein Tagesgang fuer diesen Typ

            int stammTagvId = Convert.ToInt32(head.Rows[0]["ID"]);
            string bez = head.Rows[0]["Bezeichner"] == DBNull.Value ? szTyp : head.Rows[0]["Bezeichner"].ToString();
            string beschr = head.Rows[0]["Beschreibung"] == DBNull.Value ? "" : head.Rows[0]["Beschreibung"].ToString();

            int newTagvId = DataRepository.GetMaxID("Tab_DBTagV") + 1;
            bool ok = DataRepository.ExecuteSQL(
                "INSERT INTO [Tab_DBTagV] ([ID], [ID_Gebaeude], [Bezeichner], [Beschreibung]) VALUES (?, ?, ?, ?)",
                new DbParam("@t02", DbParamTyp.Integer) { Wert = newTagvId },
                new DbParam("@t03", DbParamTyp.Integer) { Wert = idGebaeude },
                new DbParam("@t04", DbParamTyp.VarWChar) { Wert = (object)bez },
                new DbParam("@t05", DbParamTyp.VarWChar) { Wert = (object)beschr });
            if (!ok) return;

            DataTable daten = DataRepository.GetDataTable(
                "SELECT Verteilung FROM [Tab_DBTagVDaten_STAMM] WHERE ID_TagV = ? ORDER BY ID",
                new DbParam("@t06", stammTagvId));
            if (daten == null || daten.Rows.Count == 0) return;

            int nextId = DataRepository.GetMaxID("Tab_DBTagVDaten") + 1;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (DataRow dr in daten.Rows)
                    {
                        v.Ausfuehren(
                            "INSERT INTO [Tab_DBTagVDaten] ([ID], [ID_TagV], [Verteilung]) VALUES (?, ?, ?)",
                            new DbParam("@d01", DbParamTyp.Integer) { Wert = nextId++ },
                            new DbParam("@d02", DbParamTyp.Integer) { Wert = newTagvId },
                            new DbParam("@d03", DbParamTyp.Double)
                            { Wert = dr["Verteilung"] == DBNull.Value ? 0.0 : Convert.ToDouble(dr["Verteilung"]) });
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                }
            }
        }

        #endregion
    }
}
