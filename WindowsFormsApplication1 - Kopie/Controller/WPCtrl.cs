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
                               WHERE WPName = ?";

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
                string sql = "DELETE FROM Tab_WP WHERE WPName = ?";
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
                                                WPName, Firma, Beschreibung, Typ, 
                                                Baujahr, Aufstellung, Nennleistung, maxPTherm, 
                                                Heizung, Regelung, Modulkosten, Bauart, Kuehlleistung
                                            ) 
                                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                        using (OleDbCommand cmdInsert = conn.CreateCommand())
                        {
                            cmdInsert.Transaction = trans;
                            cmdInsert.CommandText = insertSql;

                            cmdInsert.Parameters.Add(new OleDbParameter("@nam", WPName ?? (object)DBNull.Value));
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
                ? "SELECT * FROM Tab_WP ORDER BY WPName"
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

                if (row["ID_WP"] != DBNull.Value) ID = Convert.ToInt32(row["ID_WP"]);
                if (row["WPName"] != DBNull.Value) WPName = row["WPName"].ToString();
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

        // Mappt die DataTable direkt in die dynamische Liste
        private void MapDataTableToItems(DataTable dt)
        {
            _internalList.Clear(); // Alte Einträge aus der Liste löschen

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                WPModel item = new WPModel();

                if (dt.Columns.Contains("ID_WP") && row["ID_WP"] != DBNull.Value) item.ID = Convert.ToInt32(row["ID_WP"]);
                if (dt.Columns.Contains("WPName") && row["WPName"] != DBNull.Value) item.WPName = row["WPName"].ToString();
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