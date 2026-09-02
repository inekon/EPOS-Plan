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

        public bool Exists(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + TABLE + " WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
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

        // Legt einen neuen Stammdatensatz an (Import). ReadOnly = false. Die ID ist ein
        // AutoWert und wird vom Einfuegeaufruf des Vorgangs zurueckgeliefert (S4e).
        public bool Insert()
        {
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    // Der innere Block haelt nur den Einzug: das INSERT-SQL steht als
                    // @"…"-Literal, dessen Zeilenumbrueche und Einrueckungen INHALT der
                    // Zeichenkette sind. Sie bleiben mit S4e Zeichen fuer Zeichen stehen.
                    {
                        string sql = @"INSERT INTO " + TABLE + @"
                            (Bezeichner, Firma, Beschreibung, Typ, Baujahr, Aufstellung, Nennleistung,
                             maxPtherm, Heizung, Regelung, Modulkosten, Bauart, Kuehlleistung, ReadOnly)
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                        OleDbParameter[] ps = {
                            new OleDbParameter("@nam", WPName ?? (object)DBNull.Value),
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
                            new OleDbParameter("@bart", Bauart ?? (object)DBNull.Value),
                            new OleDbParameter("@kuehl", Kuehlleistung),
                            new OleDbParameter("@ro", false)
                        };

                        // ARBEITSPAKET S4e: Einfuegen und ID-Rueckgabe in EINEM Aufruf auf der
                        // Verbindung des Vorgangs (frueher SELECT @@IDENTITY nach dem Commit
                        // auf derselben Verbindung - gleicher Wert, nur eine Anweisung frueher).
                        int neueId = v.EinfuegenUndId(sql, ps);

                        v.Commit();
                        if (neueId > 0) ID = neueId;
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

        // SQL und Parameter des Import-Updates - EINE Stelle fuer UpdateImport und
        // UeberschreibeMitKennlinien, damit die Feldliste nicht auseinanderlaufen kann.
        private string ImportUpdateSql()
        {
            return @"UPDATE [" + TABLE + @"] SET
                        Firma = ?, Typ = ?, Baujahr = ?, Aufstellung = ?,
                        Nennleistung = ?, maxPtherm = ?, Heizung = ?, Regelung = ?,
                        Bauart = ?, Kuehlleistung = ?
                      WHERE ID = ?";
        }

        private OleDbParameter[] ImportUpdateParameter(int id)
        {
            return new[] {
                new OleDbParameter("@fir", Firma ?? (object)DBNull.Value),
                new OleDbParameter("@typ", Typ ?? (object)DBNull.Value),
                new OleDbParameter("@bau", Baujahr),
                new OleDbParameter("@auf", Aufstellung ?? (object)DBNull.Value),
                new OleDbParameter("@nen", Nennleistung),
                new OleDbParameter("@max", maxPTherm),
                new OleDbParameter("@hei", Heizung),
                new OleDbParameter("@reg", Regelung ?? (object)DBNull.Value),
                new OleDbParameter("@bart", Bauart ?? (object)DBNull.Value),
                new OleDbParameter("@kuehl", Kuehlleistung),
                new OleDbParameter("@id", id)
            };
        }

        /// <summary>
        /// Import-Ueberschreiben (Dublettenkonzept 4.2): aktualisiert GENAU die Felder,
        /// die der VDI-Import liefert, adressiert per ID. Vom Anwender gepflegte Felder
        /// (Bezeichner, Beschreibung, Modulkosten, ReadOnly) bleiben unangetastet.
        /// </summary>
        /// <remarks>
        /// Bewusst OHNE ReadOnly-Sperre: Das Ueberschreiben eines ReadOnly-Satzes ist
        /// erlaubt und wird vorher im Konfliktdialog bestaetigt (Entscheidung 9.2 -
        /// erlauben mit Hinweis).
        /// </remarks>
        public bool UpdateImport(int id)
        {
            if (id <= 0) return false;
            return DataRepository.ExecuteSQL(ImportUpdateSql(), ImportUpdateParameter(id));
        }

        /// <summary>
        /// Import-Ueberschreiben samt Kennlinien in EINER Transaktion (Dublettenkonzept 4.2):
        /// dasselbe Stammsatz-Update wie <see cref="UpdateImport"/>, danach werden die
        /// Kennlinien (Waerme und Kuehlung) geloescht und durch die neuen Importzeilen
        /// ersetzt. <paramref name="kuehlung"/> darf leer sein.
        /// </summary>
        /// <remarks>
        /// Bewusst OHNE ReadOnly-Sperre: Das Ueberschreiben eines ReadOnly-Satzes ist
        /// erlaubt und wird vorher im Konfliktdialog bestaetigt (Entscheidung 9.2 -
        /// erlauben mit Hinweis). Transaktionsmuster wie
        /// <c>StromganglinieStammCtrl.ImportGanglinie</c>.
        /// </remarks>
        public bool UeberschreibeMitKennlinien(int id,
            IList<(int Vorlauf, int Temperatur, double COP, double Ptherm)> kenndaten,
            IList<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)> kuehlung)
        {
            if (id <= 0) return false;

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    // (1) Stammsatz aktualisieren - identisches UPDATE wie UpdateImport
                    v.Ausfuehren(ImportUpdateSql(), ImportUpdateParameter(id));

                    // (2) Alte Kennlinien beider Tabellen entfernen
                    {
                        List<OleDbParameter> p = new List<OleDbParameter>();
                        p.Add(new OleDbParameter("@id", OleDbType.Integer) { Value = id });
                        v.Ausfuehren("DELETE FROM " + CURVE + " WHERE ID_WP = ?", p.ToArray());
                    }
                    {
                        List<OleDbParameter> p = new List<OleDbParameter>();
                        p.Add(new OleDbParameter("@id", OleDbType.Integer) { Value = id });
                        v.Ausfuehren("DELETE FROM " + CURVE_K + " WHERE ID_WP = ?", p.ToArray());
                    }

                    // (3) Neue Kennlinien einfuegen. Die ID wird je Tabelle EINMAL als MAX+1
                    //     innerhalb der Transaktion ermittelt und fortlaufend hochgezaehlt.
                    if (kenndaten != null && kenndaten.Count > 0)
                    {
                        int naechsteId;
                        {
                            object m = v.Skalar("SELECT MAX(ID) FROM " + CURVE);
                            naechsteId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                        }

                        foreach (var k in kenndaten)
                        {
                            v.Ausfuehren(
                                "INSERT INTO " + CURVE + " (ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm, ReadOnly) VALUES (?, ?, ?, ?, ?, ?, ?)",
                                new OleDbParameter("@id", OleDbType.Integer) { Value = naechsteId++ },
                                new OleDbParameter("@wp", OleDbType.Integer) { Value = id },
                                new OleDbParameter("@vor", OleDbType.Integer) { Value = k.Vorlauf },
                                new OleDbParameter("@tem", OleDbType.Integer) { Value = k.Temperatur },
                                new OleDbParameter("@cop", OleDbType.Double) { Value = k.COP },
                                new OleDbParameter("@pth", OleDbType.Double) { Value = k.Ptherm },
                                new OleDbParameter("@ro", OleDbType.Boolean) { Value = false });
                        }
                    }

                    // Kuehlung: Tabelle hat KEIN ReadOnly, dafuer ID_Projekt - das bleibt
                    // beim Stamm-Import bewusst leer.
                    if (kuehlung != null && kuehlung.Count > 0)
                    {
                        int naechsteId;
                        {
                            object m = v.Skalar("SELECT MAX(ID) FROM " + CURVE_K);
                            naechsteId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                        }

                        foreach (var k in kuehlung)
                        {
                            v.Ausfuehren(
                                "INSERT INTO " + CURVE_K + " (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) VALUES (?, ?, ?, ?, ?, ?, ?)",
                                new OleDbParameter("@id", OleDbType.Integer) { Value = naechsteId++ },
                                new OleDbParameter("@wp", OleDbType.Integer) { Value = id },
                                new OleDbParameter("@vor", OleDbType.Integer) { Value = k.Vorlauf },
                                new OleDbParameter("@tem", OleDbType.Integer) { Value = k.Temperatur },
                                new OleDbParameter("@cop", OleDbType.Double) { Value = k.COP },
                                new OleDbParameter("@pk", OleDbType.Double) { Value = k.Pkuehl },
                                new OleDbParameter("@last", OleDbType.Integer) { Value = k.Last });
                        }
                    }

                    v.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    MessageBox.Show("Fehler beim Überschreiben der Wärmepumpe (Stammdaten): " + ex.Message);
                    return false;
                }
            }
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
