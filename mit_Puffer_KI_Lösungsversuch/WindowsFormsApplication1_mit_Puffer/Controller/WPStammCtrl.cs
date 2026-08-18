using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    // Controller fuer die Waermepumpen-STAMMDATEN (Tab_WP_STAMM) samt Kennlinien-Stammtabellen
    // (Tab_Kenndaten_STAMM / Tab_Kenndaten_Kuehlung_STAMM).
    // Schluessel = ID, Namensfeld = Bezeichner (im WPModel weiterhin als WPName gefuehrt).
    // Neues Feld ReadOnly: schreibgeschuetzte Stammdatensaetze koennen nicht ueberschrieben/geloescht werden.
    // Wird von den Admin-/Katalog-Dialogen verwendet (Form_WP, Form_WP_einlesen, Wizard_WPItem,
    // Form_WPAuswahl, WPDataCtrl). Alle DB-Zugriffe laufen ueber DataRepository.
    class WPStammCtrl : WPModel
    {
        public const string TABLE     = "Tab_WP_STAMM";
        public const string CURVE     = "Tab_Kenndaten_STAMM";
        public const string CURVE_K   = "Tab_Kenndaten_Kuehlung_STAMM";

        private List<WPModel> _internalList = new List<WPModel>();
        public int rows => _internalList.Count;
        public new List<WPModel> items => _internalList;

        public WPStammCtrl()
        {
        }

        #region --- READ ---

        // filter z.B. "ID=5" oder "Bezeichner='...'"; leer = alle (nach Bezeichner sortiert).
        public void ReadAll(string filter = "")
        {
            string sql = string.IsNullOrEmpty(filter)
                ? "SELECT * FROM " + TABLE + " ORDER BY Bezeichner"
                : "SELECT * FROM " + TABLE + " WHERE " + filter;

            DataTable dt = DataRepository.GetDataTable(sql, null);
            MapDataTableToItems(dt);
        }

        public void ReadAll_MitMinMaxVorlauf(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql, null);
            MapDataTableToItems(dt);
        }

        // ReadSingle mit komplettem SQL (Aufrufer uebergeben "select * from Tab_WP_STAMM where Bezeichner='...'").
        public void ReadSingle(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql, null);
            _internalList.Clear();

            if (dt != null && dt.Rows.Count > 0)
            {
                MapRowToThis(dt.Rows[0]);
                _internalList.Add(this);
            }
        }

        public bool IsReadOnly(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM " + TABLE + " WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            foreach (var item in _internalList)
                if (item != null) ctrl.Items.Add(item.WPName);
        }

        #endregion

        #region --- ADMIN WRITE (Tab_WP_STAMM) ---

        // Aktualisiert einen Stammdatensatz (per Bezeichner). Schreibgeschuetzte Saetze werden abgelehnt.
        public bool Update()
        {
            if (IsReadOnly(WPName))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            try
            {
                string sql = @"UPDATE " + TABLE + @"
                               SET Firma = ?, Beschreibung = ?, Typ = ?, Baujahr = ?, Aufstellung = ?,
                                   Nennleistung = ?, maxPtherm = ?, Heizung = ?, Regelung = ?, Modulkosten = ?
                               WHERE Bezeichner = ?";
                OleDbParameter[] ps = {
                    new OleDbParameter("@fir", Firma ?? (object)DBNull.Value),
                    new OleDbParameter("@bes", Beschreibung ?? (object)DBNull.Value),
                    new OleDbParameter("@typ", Typ ?? (object)DBNull.Value),
                    new OleDbParameter("@bau", Baujahr),
                    new OleDbParameter("@auf", Aufstellung ?? (object)DBNull.Value),
                    new OleDbParameter("@nen", Nennleistung),
                    new OleDbParameter("@max", maxPTherm),
                    new OleDbParameter("@hei", Heizung),
                    new OleDbParameter("@reg", Regelung ?? (object)DBNull.Value),
                    new OleDbParameter("@mod", Modulkosten),
                    new OleDbParameter("@nam", WPName ?? (object)DBNull.Value)
                };
                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex) { Console.WriteLine("Fehler bei Update (STAMM): " + ex.Message); return false; }
        }

        // Loescht einen Stammdatensatz (per Bezeichner) samt Kennlinien, sofern nicht schreibgeschuetzt.
        public bool Delete()
        {
            if (IsReadOnly(WPName))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            try
            {
                int id = DataRepository.GetIdByName(TABLE, "Bezeichner", WPName);
                if (id > 0)
                {
                    DataRepository.ExecuteSQL("DELETE FROM " + CURVE   + " WHERE ID_WP = ?", new OleDbParameter("@id", id));
                    DataRepository.ExecuteSQL("DELETE FROM " + CURVE_K + " WHERE ID_WP = ?", new OleDbParameter("@id", id));
                }
                return DataRepository.ExecuteSQL("DELETE FROM " + TABLE + " WHERE Bezeichner = ?",
                    new OleDbParameter("@nam", WPName ?? (object)DBNull.Value));
            }
            catch (Exception ex) { Console.WriteLine("Fehler bei Delete (STAMM): " + ex.Message); return false; }
        }

        // Legt einen neuen Stammdatensatz an (Import). ReadOnly = false. Setzt ID (AutoWert) via @@IDENTITY.
        public bool Insert()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbTransaction trans = conn.BeginTransaction())
                    {
                        string sql = @"INSERT INTO " + TABLE + @"
                            (Bezeichner, Firma, Beschreibung, Typ, Baujahr, Aufstellung, Nennleistung,
                             maxPtherm, Heizung, Regelung, Modulkosten, Bauart, Kuehlleistung, ReadOnly)
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                        using (OleDbCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = trans;
                            cmd.CommandText = sql;
                            cmd.Parameters.Add(new OleDbParameter("@nam", WPName ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new OleDbParameter("@fir", Firma ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new OleDbParameter("@bes", Beschreibung ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new OleDbParameter("@typ", Typ ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new OleDbParameter("@bau", Baujahr));
                            cmd.Parameters.Add(new OleDbParameter("@auf", Aufstellung ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new OleDbParameter("@nen", Nennleistung));
                            cmd.Parameters.Add(new OleDbParameter("@max", maxPTherm));
                            cmd.Parameters.Add(new OleDbParameter("@hei", Heizung));
                            cmd.Parameters.Add(new OleDbParameter("@reg", Regelung ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new OleDbParameter("@mod", Modulkosten));
                            cmd.Parameters.Add(new OleDbParameter("@bart", Bauart ?? (object)DBNull.Value));
                            cmd.Parameters.Add(new OleDbParameter("@kuehl", Kuehlleistung));
                            cmd.Parameters.Add(new OleDbParameter("@ro", false));
                            cmd.ExecuteNonQuery();
                        }
                        trans.Commit();
                        using (var cmdId = new OleDbCommand("SELECT @@IDENTITY", conn))
                        {
                            object r = cmdId.ExecuteScalar();
                            if (r != null && r != DBNull.Value) ID = Convert.ToInt32(r);
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex) { Console.WriteLine("Fehler bei Insert (STAMM): " + ex.Message); return false; }
        }

        // Kennlinien-Import (Waerme) in die STAMM-Tabelle. ID explizit (MAX+1), ReadOnly = false.
        public bool InsertKenndatenStamm(int idWp, int vorlauf, int temperatur, double cop, double ptherm)
        {
            object m = DataRepository.ExecuteScalar("SELECT Max(ID) FROM " + CURVE);
            int id = (m == null || m == DBNull.Value) ? 1 : Convert.ToInt32(m) + 1;
            string sql = System.FormattableString.Invariant(
                $@"INSERT INTO {CURVE} (ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm, ReadOnly)
                   VALUES ({id}, {idWp}, {vorlauf}, {temperatur}, {cop}, {ptherm}, FALSE)");
            return DataRepository.ExecuteSQL(sql);
        }

        // Kennlinien-Import (Kuehlung) in die STAMM-Tabelle. ID explizit (MAX+1).
        public bool InsertKenndatenKuehlungStamm(int idWp, int vorlauf, int temperatur, double cop, double pkuehl, int last)
        {
            object m = DataRepository.ExecuteScalar("SELECT Max(ID) FROM " + CURVE_K);
            int id = (m == null || m == DBNull.Value) ? 1 : Convert.ToInt32(m) + 1;
            string sql = System.FormattableString.Invariant(
                $@"INSERT INTO {CURVE_K} (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last])
                   VALUES ({id}, {idWp}, {vorlauf}, {temperatur}, {cop}, {pkuehl}, {last})");
            return DataRepository.ExecuteSQL(sql);
        }

        #endregion

        #region --- MAPPING ---

        private void MapDataTableToItems(DataTable dt)
        {
            _internalList.Clear();
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
            {
                WPModel item = new WPModel();
                FillModel(item, dt, row);
                _internalList.Add(item);
            }
        }

        private void MapRowToThis(DataRow row)
        {
            FillModel(this, row.Table, row);
        }

        private void FillModel(WPModel item, DataTable dt, DataRow row)
        {
            if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.ID = Convert.ToInt32(row["ID"]);
            if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) item.WPName = row["Bezeichner"].ToString();
            if (dt.Columns.Contains("Firma") && row["Firma"] != DBNull.Value) item.Firma = row["Firma"].ToString();
            if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) item.Beschreibung = row["Beschreibung"].ToString();
            if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value) item.Typ = row["Typ"].ToString();
            if (dt.Columns.Contains("Baujahr") && row["Baujahr"] != DBNull.Value) item.Baujahr = Convert.ToInt32(row["Baujahr"]);
            if (dt.Columns.Contains("Aufstellung") && row["Aufstellung"] != DBNull.Value) item.Aufstellung = row["Aufstellung"].ToString();
            if (dt.Columns.Contains("Nennleistung") && row["Nennleistung"] != DBNull.Value) item.Nennleistung = Convert.ToInt32(row["Nennleistung"]);
            if (dt.Columns.Contains("maxPtherm") && row["maxPtherm"] != DBNull.Value) item.maxPTherm = Convert.ToInt32(row["maxPtherm"]);
            if (dt.Columns.Contains("Heizung") && row["Heizung"] != DBNull.Value) item.Heizung = Convert.ToInt32(row["Heizung"]);
            if (dt.Columns.Contains("Regelung") && row["Regelung"] != DBNull.Value) item.Regelung = row["Regelung"].ToString();
            if (dt.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value) item.Modulkosten = Convert.ToInt32(row["Modulkosten"]);
            if (dt.Columns.Contains("Kuehlleistung") && row["Kuehlleistung"] != DBNull.Value) item.Kuehlleistung = Convert.ToDouble(row["Kuehlleistung"]);
            if (dt.Columns.Contains("Bauart") && row["Bauart"] != DBNull.Value) item.Bauart = row["Bauart"].ToString();
            if (dt.Columns.Contains("Max") && row["Max"] != DBNull.Value) item.MaxVorlauf = Convert.ToInt32(row["Max"]);
            if (dt.Columns.Contains("Min") && row["Min"] != DBNull.Value) item.MinVorlauf = Convert.ToInt32(row["Min"]);
            item.m_bReadOnly = dt.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);
        }

        #endregion
    }
}
