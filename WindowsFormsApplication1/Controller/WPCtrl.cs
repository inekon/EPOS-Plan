using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class WPCtrl : WPModel
    {
        private List<WPModel> _internalList = new List<WPModel>();
        public int rows => _internalList.Count;
        public new List<WPModel> items => _internalList;

        public WPCtrl()
        {
        }

        public bool Update()
        {
            try
            {
                string sql = @"UPDATE Tab_WP 
                               SET Firma = ?, 
                                   Beschreibung = ?, 
                                   Typ = ?, 
                                   Baujahr = ?, 
                                   Aufstellung = ?, 
                                   Nennleistung = ?, 
                                   maxPTherm = ?, 
                                   Heizung = ?, 
                                   Regelung = ?, 
                                   Modulkosten = ? 
                               WHERE Bezeichner = ?";

                DbParam[] ps = {
                    new DbParam("@fir", Firma ?? (object)DBNull.Value),
                    new DbParam("@bes", Beschreibung ?? (object)DBNull.Value),
                    new DbParam("@typ", Typ ?? (object)DBNull.Value),
                    new DbParam("@bau", Baujahr),
                    new DbParam("@auf", Aufstellung ?? (object)DBNull.Value),
                    new DbParam("@nen", Nennleistung),
                    new DbParam("@max", maxPTherm),
                    new DbParam("@hei", Heizung),
                    new DbParam("@reg", Regelung ?? (object)DBNull.Value),
                    new DbParam("@mod", Modulkosten),
                    new DbParam("@nam", WPName ?? (object)DBNull.Value)
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
                string sql = "DELETE FROM Tab_WP WHERE Bezeichner = ?";
                DbParam[] ps = { new DbParam("@nam", WPName ?? (object)DBNull.Value) };

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
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    // Der innere Block haelt nur den Einzug: das INSERT-SQL steht als
                    // @"…"-Literal, dessen Zeilenumbrueche und Einrueckungen INHALT der
                    // Zeichenkette sind. Sie bleiben mit S4e Zeichen fuer Zeichen stehen.
                    {
                        // Parametrisierter INSERT-Befehl
                        string insertSql = @"INSERT INTO Tab_WP 
                                            (
                                                Bezeichner, ID_Projekt, Firma, Beschreibung, Typ, 
                                                Baujahr, Aufstellung, Nennleistung, maxPTherm, 
                                                Heizung, Regelung, Modulkosten, Bauart, Kuehlleistung
                                            ) 
                                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                        DbParam[] ps = {
                            new DbParam("@nam", WPName ?? (object)DBNull.Value),
                            new DbParam("@proj", ID_Projekt),
                            new DbParam("@fir", Firma ?? (object)DBNull.Value),
                            new DbParam("@bes", Beschreibung ?? (object)DBNull.Value),
                            new DbParam("@typ", Typ ?? (object)DBNull.Value),
                            new DbParam("@bau", Baujahr),
                            new DbParam("@auf", Aufstellung ?? (object)DBNull.Value),
                            new DbParam("@nen", Nennleistung),
                            new DbParam("@max", maxPTherm),
                            new DbParam("@hei", Heizung),
                            new DbParam("@reg", Regelung ?? (object)DBNull.Value),
                            new DbParam("@mod", Modulkosten),
                            new DbParam("@bart", Bauart ?? (object)DBNull.Value),
                            new DbParam("@kuehl", Kuehlleistung)
                        };

                        // ARBEITSPAKET S4e: Einfuegen und ID-Rueckgabe in EINEM Aufruf auf der
                        // Verbindung des Vorgangs. Frueher stand SELECT @@IDENTITY NACH dem
                        // Commit auf derselben, noch offenen Verbindung; jetzt liefert der
                        // Einfuegeaufruf die ID unmittelbar VOR dem Commit. Verbindung und
                        // Wert sind dieselben, nur der Lesezeitpunkt liegt eine Anweisung
                        // frueher - anders ist die ID nach dem Commit nicht mehr zu haben.
                        int neueId = v.EinfuegenUndId(insertSql, ps);

                        v.Commit(); // Schreibt die Daten jetzt unwiderruflich in die Datenbank

                        if (neueId > 0)
                        {
                            ID = neueId;
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Insert: " + ex.Message);
                return false;
            }
        }

        public void ReadAll(string filter = "")
        {
            string sql = string.IsNullOrEmpty(filter)
                ? "SELECT * FROM Tab_WP ORDER BY Bezeichner"
                : "SELECT * FROM Tab_WP WHERE " + filter;

            DataTable dt = DataRepository.GetDataTable(sql, null);
            MapDataTableToItems(dt);
        }

        public void ReadAll_MitMinMaxVorlauf(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql, null);
            MapDataTableToItems(dt);
        }

        public void ReadSingle(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql, null);
            _internalList.Clear(); // Liste leeren bei ReadSingle

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (row["ID"] != DBNull.Value) ID = Convert.ToInt32(row["ID"]);
                if (row["Bezeichner"] != DBNull.Value) WPName = row["Bezeichner"].ToString();
                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value) ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);
                if (row["Firma"] != DBNull.Value) Firma = row["Firma"].ToString();
                if (row["Beschreibung"] != DBNull.Value) Beschreibung = row["Beschreibung"].ToString();
                if (row["Typ"] != DBNull.Value) Typ = row["Typ"].ToString();
                if (row["Baujahr"] != DBNull.Value) Baujahr = Convert.ToInt32(row["Baujahr"]);
                if (row["Aufstellung"] != DBNull.Value) Aufstellung = row["Aufstellung"].ToString();
                if (row["Nennleistung"] != DBNull.Value) Nennleistung = Convert.ToInt32(row["Nennleistung"]);
                if (row["Heizung"] != DBNull.Value) Heizung = Convert.ToInt32(row["Heizung"]);
                if (row["Regelung"] != DBNull.Value) Regelung = row["Regelung"].ToString();
                if (row["Modulkosten"] != DBNull.Value) Modulkosten = Convert.ToInt32(row["Modulkosten"]);
                if (dt.Columns.Contains("Kuehlleistung") && row["Kuehlleistung"] != DBNull.Value) Kuehlleistung = Convert.ToDouble(row["Kuehlleistung"]);
                if (dt.Columns.Contains("Bauart") && row["Bauart"] != DBNull.Value) Bauart = row["Bauart"].ToString();

                // Bei ReadSingle fügen wir diese Instanz (this) als Kopie hinzu, damit rows auf 1 springt
                _internalList.Add(this);
            }
        }

        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            foreach (var item in _internalList)
            {
                if (item != null)
                {
                    ctrl.Items.Add(item.WPName);
                }
            }
        }

        #region --- STAMM -> PROJEKT KOPIE ---

        // Projekt-WP-ID (Tab_WP.ID) zu einem Bezeichner im Projekt, oder 0.
        public int GetProjektId(string szBezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_WP WHERE Bezeichner = ? AND ID_Projekt = ?",
                new DbParam("@bez", szBezeichner ?? ""),
                new DbParam("@proj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Loescht einen Projekt-WP (per Bezeichner + Projekt) samt Kennlinien.
        public bool DeleteFromProjekt(string szBezeichner, int idProjekt)
        {
            int id = GetProjektId(szBezeichner, idProjekt);
            if (id > 0)
            {
                DataRepository.ExecuteSQL("DELETE FROM Tab_Kenndaten WHERE ID_WP = ?", new DbParam("@id", id));
                DataRepository.ExecuteSQL("DELETE FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = ?", new DbParam("@id", id));
            }
            return DataRepository.ExecuteSQL("DELETE FROM Tab_WP WHERE Bezeichner = ? AND ID_Projekt = ?",
                new DbParam("@bez", szBezeichner ?? ""), new DbParam("@proj", idProjekt));
        }

        // Komfort-Ueberladung: kopiert per Bezeichner aus den Stammdaten ins Projekt.
        public int CopyFromStamm(string szBezeichner, int idProjekt)
        {
            int stammId = DataRepository.GetIdByName(WPStammCtrl.TABLE, "Bezeichner", szBezeichner);
            if (stammId <= 0) return -1;
            return CopyFromStamm(stammId, idProjekt);
        }

        // Kopiert einen Stamm-WP (Tab_WP_STAMM) samt Kennlinien (Tab_Kenndaten_STAMM /
        // Tab_Kenndaten_Kuehlung_STAMM) in die Projekt-Tabellen, sofern fuer das Projekt noch nicht
        // vorhanden. Setzt ID_Projekt und remappt die Kennlinien auf die neue Projekt-WP-ID.
        // Rueckgabe: Projekt-WP-ID (Tab_WP.ID) des kopierten ODER bereits vorhandenen Satzes, -1 bei Fehler.
        public int CopyFromStamm(int stammId, int idProjekt)
        {
            try
            {
                DataTable head = DataRepository.GetDataTable(
                    "SELECT * FROM " + WPStammCtrl.TABLE + " WHERE ID = ?", new DbParam("@id", stammId));
                if (head == null || head.Rows.Count == 0) return -1;
                DataRow sHead = head.Rows[0];
                string bez = sHead["Bezeichner"].ToString();

                int vorhanden = GetProjektId(bez, idProjekt);
                if (vorhanden > 0) return vorhanden;

                DataTable cw = DataRepository.GetDataTable(
                    "SELECT * FROM " + WPStammCtrl.CURVE   + " WHERE ID_WP = ? ORDER BY ID", new DbParam("@id", stammId));
                DataTable ck = DataRepository.GetDataTable(
                    "SELECT * FROM " + WPStammCtrl.CURVE_K + " WHERE ID_WP = ? ORDER BY ID", new DbParam("@id", stammId));

                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        int neueId;
                        {
                            object m = v.Skalar("SELECT Max(ID) FROM Tab_WP");
                            neueId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                        }

                        string hsql = @"INSERT INTO Tab_WP
                        (ID, ID_Projekt, Bezeichner, Firma, Beschreibung, Typ, Baujahr, Aufstellung,
                         Nennleistung, maxPtherm, Heizung, Regelung, Modulkosten, Laenge, Breite, Hoehe,
                         Gewicht, Raum, Kuehlleistung, Bauart)
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                        {
                            List<DbParam> p = new List<DbParam>();
                            p.Add(new DbParam("@id", neueId));
                            p.Add(new DbParam("@proj", idProjekt));
                            p.Add(P(sHead, "Bezeichner"));
                            p.Add(P(sHead, "Firma"));
                            p.Add(P(sHead, "Beschreibung"));
                            p.Add(P(sHead, "Typ"));
                            p.Add(P(sHead, "Baujahr"));
                            p.Add(P(sHead, "Aufstellung"));
                            p.Add(P(sHead, "Nennleistung"));
                            p.Add(P(sHead, "maxPtherm"));
                            p.Add(P(sHead, "Heizung"));
                            p.Add(P(sHead, "Regelung"));
                            p.Add(P(sHead, "Modulkosten"));
                            p.Add(P(sHead, "Laenge"));
                            p.Add(P(sHead, "Breite"));
                            p.Add(P(sHead, "Hoehe"));
                            p.Add(P(sHead, "Gewicht"));
                            p.Add(P(sHead, "Raum"));
                            p.Add(P(sHead, "Kuehlleistung"));
                            p.Add(P(sHead, "Bauart"));
                            v.Ausfuehren(hsql, p.ToArray());
                        }

                        // Kennlinien Waerme (ID explizit MAX+1, ID_WP = neue Projekt-WP-ID)
                        if (cw != null && cw.Rows.Count > 0)
                        {
                            int cid;
                            { object m = v.Skalar("SELECT Max(ID) FROM Tab_Kenndaten"); cid = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1; }
                            foreach (DataRow r in cw.Rows)
                            {
                                {
                                    List<DbParam> p = new List<DbParam>();
                                    p.Add(new DbParam("@id", cid++));
                                    p.Add(new DbParam("@wp", neueId));
                                    p.Add(P(r, "Vorlauf"));
                                    p.Add(P(r, "Temperatur"));
                                    p.Add(P(r, "COP"));
                                    p.Add(P(r, "Ptherm"));
                                    v.Ausfuehren("INSERT INTO Tab_Kenndaten (ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm) VALUES (?, ?, ?, ?, ?, ?)", p.ToArray());
                                }
                            }
                        }

                        // Kennlinien Kuehlung
                        if (ck != null && ck.Rows.Count > 0)
                        {
                            int ckid;
                            { object m = v.Skalar("SELECT Max(ID) FROM Tab_Kenndaten_Kuehlung"); ckid = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1; }
                            foreach (DataRow r in ck.Rows)
                            {
                                {
                                    List<DbParam> p = new List<DbParam>();
                                    p.Add(new DbParam("@id", ckid++));
                                    p.Add(new DbParam("@wp", neueId));
                                    p.Add(P(r, "Vorlauf"));
                                    p.Add(P(r, "Temperatur"));
                                    p.Add(P(r, "COP"));
                                    p.Add(P(r, "Pkuehl"));
                                    p.Add(P(r, "Last"));
                                    v.Ausfuehren("INSERT INTO Tab_Kenndaten_Kuehlung (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) VALUES (?, ?, ?, ?, ?, ?, ?)", p.ToArray());
                                }
                            }
                        }

                        v.Commit();
                        return neueId;
                    }
                    catch (Exception ex)
                    {
                        try { v.Rollback(); } catch { }
                        Console.WriteLine("Fehler beim Kopieren des WP aus den Stammdaten: " + ex.Message);
                        return -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Kopieren des WP aus den Stammdaten: " + ex.Message);
                return -1;
            }
        }

        // Parameter aus Spaltenwert (DBNull, falls Spalte fehlt).
        private static DbParam P(DataRow row, string col)
        {
            object v = row.Table.Columns.Contains(col) ? row[col] : DBNull.Value;
            return new DbParam("@" + col, v ?? DBNull.Value);
        }

        #endregion

        // Mappt die DataTable direkt in die dynamische Liste
        private void MapDataTableToItems(DataTable dt)
        {
            _internalList.Clear(); // Alte Einträge aus der Liste löschen

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                WPModel item = new WPModel();

                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.ID = Convert.ToInt32(row["ID"]);
                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value) item.ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);
                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) item.WPName = row["Bezeichner"].ToString();
                if (dt.Columns.Contains("Firma") && row["Firma"] != DBNull.Value) item.Firma = row["Firma"].ToString();
                if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) item.Beschreibung = row["Beschreibung"].ToString();
                if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value) item.Typ = row["Typ"].ToString();
                if (dt.Columns.Contains("Baujahr") && row["Baujahr"] != DBNull.Value) item.Baujahr = Convert.ToInt32(row["Baujahr"]);
                if (dt.Columns.Contains("Aufstellung") && row["Aufstellung"] != DBNull.Value) item.Aufstellung = row["Aufstellung"].ToString();
                if (dt.Columns.Contains("Nennleistung") && row["Nennleistung"] != DBNull.Value) item.Nennleistung = Convert.ToInt32(row["Nennleistung"]);
                if (dt.Columns.Contains("maxPTherm") && row["maxPTherm"] != DBNull.Value) item.maxPTherm = Convert.ToInt32(row["maxPTherm"]);
                if (dt.Columns.Contains("Heizung") && row["Heizung"] != DBNull.Value) item.Heizung = Convert.ToInt32(row["Heizung"]);
                if (dt.Columns.Contains("Regelung") && row["Regelung"] != DBNull.Value) item.Regelung = row["Regelung"].ToString();
                if (dt.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value) item.Modulkosten = Convert.ToInt32(row["Modulkosten"]);
                if (dt.Columns.Contains("Kuehlleistung") && row["Kuehlleistung"] != DBNull.Value) item.Kuehlleistung = Convert.ToDouble(row["Kuehlleistung"]);
                if (dt.Columns.Contains("Bauart") && row["Bauart"] != DBNull.Value) item.Bauart = row["Bauart"].ToString();

                // Für erweiterte Abfragen (ReadAll_MitMinMaxVorlauf)
                if (dt.Columns.Contains("Max") && row["Max"] != DBNull.Value) item.MaxVorlauf = Convert.ToInt32(row["Max"]);
                if (dt.Columns.Contains("Min") && row["Min"] != DBNull.Value) item.MinVorlauf = Convert.ToInt32(row["Min"]);

                _internalList.Add(item); // Dynamisch zur Liste hinzufügen
            }
        }
    }
}
