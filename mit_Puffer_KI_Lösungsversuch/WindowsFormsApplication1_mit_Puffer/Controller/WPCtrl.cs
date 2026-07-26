using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
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
                OleDbParameter[] ps = { new OleDbParameter("@nam", WPName ?? (object)DBNull.Value) };

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
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbTransaction trans = conn.BeginTransaction())
                    {
                        // Parametrisierter INSERT-Befehl
                        string insertSql = @"INSERT INTO Tab_WP 
                                            (
                                                Bezeichner, ID_Projekt, Firma, Beschreibung, Typ, 
                                                Baujahr, Aufstellung, Nennleistung, maxPTherm, 
                                                Heizung, Regelung, Modulkosten, Bauart, Kuehlleistung
                                            ) 
                                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                        using (OleDbCommand cmdInsert = conn.CreateCommand())
                        {
                            cmdInsert.Transaction = trans;
                            cmdInsert.CommandText = insertSql;

                            cmdInsert.Parameters.Add(new OleDbParameter("@nam", WPName ?? (object)DBNull.Value));
                            cmdInsert.Parameters.Add(new OleDbParameter("@proj", ID_Projekt));
                            cmdInsert.Parameters.Add(new OleDbParameter("@fir", Firma ?? (object)DBNull.Value));
                            cmdInsert.Parameters.Add(new OleDbParameter("@bes", Beschreibung ?? (object)DBNull.Value));
                            cmdInsert.Parameters.Add(new OleDbParameter("@typ", Typ ?? (object)DBNull.Value));
                            cmdInsert.Parameters.Add(new OleDbParameter("@bau", Baujahr));
                            cmdInsert.Parameters.Add(new OleDbParameter("@auf", Aufstellung ?? (object)DBNull.Value));
                            cmdInsert.Parameters.Add(new OleDbParameter("@nen", Nennleistung));
                            cmdInsert.Parameters.Add(new OleDbParameter("@max", maxPTherm));
                            cmdInsert.Parameters.Add(new OleDbParameter("@hei", Heizung));
                            cmdInsert.Parameters.Add(new OleDbParameter("@reg", Regelung ?? (object)DBNull.Value));
                            cmdInsert.Parameters.Add(new OleDbParameter("@mod", Modulkosten));
                            cmdInsert.Parameters.Add(new OleDbParameter("@bart", Bauart ?? (object)DBNull.Value));
                            cmdInsert.Parameters.Add(new OleDbParameter("@kuehl", Kuehlleistung));

                            cmdInsert.ExecuteNonQuery();
                        }

                        trans.Commit(); // Schreibt die Daten jetzt unwiderruflich in die Datenbank

                        // 3. JETZT die ID abfragen (Die Verbindung 'conn' ist ja noch offen!)
                        using (var cmdIdentity = new OleDbCommand("SELECT @@IDENTITY", conn))
                        {
                            // Hier KEINE Transaktion mehr zuweisen, da trans bereits geschlossen ist!
                            object result = cmdIdentity.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                ID = Convert.ToInt32(result);
                            }
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
                new OleDbParameter("@bez", szBezeichner ?? ""),
                new OleDbParameter("@proj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Loescht einen Projekt-WP (per Bezeichner + Projekt) samt Kennlinien.
        public bool DeleteFromProjekt(string szBezeichner, int idProjekt)
        {
            int id = GetProjektId(szBezeichner, idProjekt);
            if (id > 0)
            {
                DataRepository.ExecuteSQL("DELETE FROM Tab_Kenndaten WHERE ID_WP = ?", new OleDbParameter("@id", id));
                DataRepository.ExecuteSQL("DELETE FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = ?", new OleDbParameter("@id", id));
            }
            return DataRepository.ExecuteSQL("DELETE FROM Tab_WP WHERE Bezeichner = ? AND ID_Projekt = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""), new OleDbParameter("@proj", idProjekt));
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
                    "SELECT * FROM " + WPStammCtrl.TABLE + " WHERE ID = ?", new OleDbParameter("@id", stammId));
                if (head == null || head.Rows.Count == 0) return -1;
                DataRow sHead = head.Rows[0];
                string bez = sHead["Bezeichner"].ToString();

                int vorhanden = GetProjektId(bez, idProjekt);
                if (vorhanden > 0) return vorhanden;

                DataTable cw = DataRepository.GetDataTable(
                    "SELECT * FROM " + WPStammCtrl.CURVE   + " WHERE ID_WP = ? ORDER BY ID", new OleDbParameter("@id", stammId));
                DataTable ck = DataRepository.GetDataTable(
                    "SELECT * FROM " + WPStammCtrl.CURVE_K + " WHERE ID_WP = ? ORDER BY ID", new OleDbParameter("@id", stammId));

                var (conn, trans) = DataRepository.BeginTransaction();
                try
                {
                    int neueId;
                    using (OleDbCommand c = new OleDbCommand("SELECT Max(ID) FROM Tab_WP", conn, trans))
                    {
                        object m = c.ExecuteScalar();
                        neueId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                    }

                    string hsql = @"INSERT INTO Tab_WP
                        (ID, ID_Projekt, Bezeichner, Firma, Beschreibung, Typ, Baujahr, Aufstellung,
                         Nennleistung, maxPtherm, Heizung, Regelung, Modulkosten, Laenge, Breite, Hoehe,
                         Gewicht, Raum, Kuehlleistung, Bauart)
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    using (OleDbCommand c = new OleDbCommand(hsql, conn, trans))
                    {
                        c.Parameters.Add(new OleDbParameter("@id", neueId));
                        c.Parameters.Add(new OleDbParameter("@proj", idProjekt));
                        c.Parameters.Add(P(sHead, "Bezeichner"));
                        c.Parameters.Add(P(sHead, "Firma"));
                        c.Parameters.Add(P(sHead, "Beschreibung"));
                        c.Parameters.Add(P(sHead, "Typ"));
                        c.Parameters.Add(P(sHead, "Baujahr"));
                        c.Parameters.Add(P(sHead, "Aufstellung"));
                        c.Parameters.Add(P(sHead, "Nennleistung"));
                        c.Parameters.Add(P(sHead, "maxPtherm"));
                        c.Parameters.Add(P(sHead, "Heizung"));
                        c.Parameters.Add(P(sHead, "Regelung"));
                        c.Parameters.Add(P(sHead, "Modulkosten"));
                        c.Parameters.Add(P(sHead, "Laenge"));
                        c.Parameters.Add(P(sHead, "Breite"));
                        c.Parameters.Add(P(sHead, "Hoehe"));
                        c.Parameters.Add(P(sHead, "Gewicht"));
                        c.Parameters.Add(P(sHead, "Raum"));
                        c.Parameters.Add(P(sHead, "Kuehlleistung"));
                        c.Parameters.Add(P(sHead, "Bauart"));
                        c.ExecuteNonQuery();
                    }

                    // Kennlinien Waerme (ID explizit MAX+1, ID_WP = neue Projekt-WP-ID)
                    if (cw != null && cw.Rows.Count > 0)
                    {
                        int cid;
                        using (OleDbCommand c = new OleDbCommand("SELECT Max(ID) FROM Tab_Kenndaten", conn, trans))
                        { object m = c.ExecuteScalar(); cid = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1; }
                        foreach (DataRow r in cw.Rows)
                        {
                            using (OleDbCommand c = new OleDbCommand(
                                "INSERT INTO Tab_Kenndaten (ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm) VALUES (?, ?, ?, ?, ?, ?)", conn, trans))
                            {
                                c.Parameters.Add(new OleDbParameter("@id", cid++));
                                c.Parameters.Add(new OleDbParameter("@wp", neueId));
                                c.Parameters.Add(P(r, "Vorlauf"));
                                c.Parameters.Add(P(r, "Temperatur"));
                                c.Parameters.Add(P(r, "COP"));
                                c.Parameters.Add(P(r, "Ptherm"));
                                c.ExecuteNonQuery();
                            }
                        }
                    }

                    // Kennlinien Kuehlung
                    if (ck != null && ck.Rows.Count > 0)
                    {
                        int ckid;
                        using (OleDbCommand c = new OleDbCommand("SELECT Max(ID) FROM Tab_Kenndaten_Kuehlung", conn, trans))
                        { object m = c.ExecuteScalar(); ckid = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1; }
                        foreach (DataRow r in ck.Rows)
                        {
                            using (OleDbCommand c = new OleDbCommand(
                                "INSERT INTO Tab_Kenndaten_Kuehlung (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, Last) VALUES (?, ?, ?, ?, ?, ?, ?)", conn, trans))
                            {
                                c.Parameters.Add(new OleDbParameter("@id", ckid++));
                                c.Parameters.Add(new OleDbParameter("@wp", neueId));
                                c.Parameters.Add(P(r, "Vorlauf"));
                                c.Parameters.Add(P(r, "Temperatur"));
                                c.Parameters.Add(P(r, "COP"));
                                c.Parameters.Add(P(r, "Pkuehl"));
                                c.Parameters.Add(P(r, "Last"));
                                c.ExecuteNonQuery();
                            }
                        }
                    }

                    trans.Commit();
                    return neueId;
                }
                catch (Exception ex)
                {
                    try { trans.Rollback(); } catch { }
                    Console.WriteLine("Fehler beim Kopieren des WP aus den Stammdaten: " + ex.Message);
                    return -1;
                }
                finally { try { conn.Close(); } catch { } }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Kopieren des WP aus den Stammdaten: " + ex.Message);
                return -1;
            }
        }

        // Parameter aus Spaltenwert (DBNull, falls Spalte fehlt).
        private static OleDbParameter P(DataRow row, string col)
        {
            object v = row.Table.Columns.Contains(col) ? row[col] : DBNull.Value;
            return new OleDbParameter("@" + col, v ?? DBNull.Value);
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
