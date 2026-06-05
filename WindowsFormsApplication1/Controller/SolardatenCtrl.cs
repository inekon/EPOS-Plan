using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class SolardatenCtrl : SolardatenModel
    {
        // Dynamisches Listen-Schema zur Aufhebung des 1.000.000er Limits
        private List<SolardatenModel> _internalList = new List<SolardatenModel>();

        public int rows => _internalList.Count;
        public new List<SolardatenModel> items => _internalList;

        // Zusätzliche Analyse-Listen aus dem Altcode beibehalten
        public List<double> list_Temperatur = new List<double>();
        public List<double> list_Sonnenwinkel = new List<double>();
        public List<int> list_Tag = new List<int>();

        public string Klimazone { get; set; }

        public SolardatenCtrl()
        {
            Klimazone = "";
            m_ID_Klimaregion = 0;
        }

        private void MapDataRowToModel(DataRow row, SolardatenModel item, DataTable dt)
        {
            if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.m_ID = Convert.ToInt32(row["ID"]);
            if (dt.Columns.Contains("ID_Klimaregion") && row["ID_Klimaregion"] != DBNull.Value) item.m_ID_Klimaregion = Convert.ToInt32(row["ID_Klimaregion"]);
            if (dt.Columns.Contains("Außen_Temp") && row["Außen_Temp"] != DBNull.Value) item.Außen_Temp = Convert.ToDouble(row["Außen_Temp"]);
            if (dt.Columns.Contains("Sol_Nord") && row["Sol_Nord"] != DBNull.Value) item.Sol_Nord = Convert.ToDouble(row["Sol_Nord"]);
            if (dt.Columns.Contains("Sol_Ost") && row["Sol_Ost"] != DBNull.Value) item.Sol_Ost = Convert.ToDouble(row["Sol_Ost"]);
            if (dt.Columns.Contains("Sol_Sued") && row["Sol_Sued"] != DBNull.Value) item.Sol_Sued = Convert.ToDouble(row["Sol_Sued"]);
            if (dt.Columns.Contains("Sol_West") && row["Sol_West"] != DBNull.Value) item.Sol_West = Convert.ToDouble(row["Sol_West"]);
            if (dt.Columns.Contains("Globalstrahlung") && row["Globalstrahlung"] != DBNull.Value) item.Globalstrahlung = Convert.ToDouble(row["Globalstrahlung"]);
            if (dt.Columns.Contains("Direktstrahlung") && row["Direktstrahlung"] != DBNull.Value) item.Direktstrahlung = Convert.ToDouble(row["Direktstrahlung"]);
            if (dt.Columns.Contains("Diffusstrahlung") && row["Diffusstrahlung"] != DBNull.Value) item.Diffusstrahlung = Convert.ToDouble(row["Diffusstrahlung"]);
            if (dt.Columns.Contains("Sonnenwinkel") && row["Sonnenwinkel"] != DBNull.Value) item.Sonnenwinkel = Convert.ToDouble(row["Sonnenwinkel"]);
        }

        public void ReadAll(string sql = "")
        {
            if (string.IsNullOrEmpty(sql))
            {
                sql = "SELECT * FROM Tab_Solar ORDER BY ID";
            }

            DataTable dt = DataRepository.GetDataTable(sql, null);
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                SolardatenModel item = new SolardatenModel();
                MapDataRowToModel(row, item, dt);
                _internalList.Add(item);
            }
        }

        public void ReadAll(int ID_Klimaregion)
        {
            string sql = "SELECT * FROM Tab_Solar WHERE ID_Klimaregion = ? ORDER BY ID";

            OleDbParameter paramReg = new OleDbParameter("@regId", OleDbType.Integer);
            paramReg.Value = ID_Klimaregion;

            DataTable dt = DataRepository.GetDataTable(sql, new[] { paramReg });

            _internalList.Clear();
            list_Temperatur.Clear();
            list_Sonnenwinkel.Clear();
            list_Tag.Clear();

            if (dt == null) return;

            int currentIndex = 0;
            foreach (DataRow row in dt.Rows)
            {
                SolardatenModel item = new SolardatenModel();
                MapDataRowToModel(row, item, dt);

                list_Temperatur.Add(item.Außen_Temp);
                list_Sonnenwinkel.Add(item.Sonnenwinkel);
                list_Tag.Add(currentIndex + 1);

                _internalList.Add(item);
                currentIndex++;
            }
        }

        public bool Insert(int ID_Klimaregion, List<SolardatenModel> list)
        {
            if (list == null || list.Count == 0) return true;

            try
            {
                string sqlCount = "SELECT COUNT(*) FROM Tab_Solar";
                object countResult = DataRepository.ExecuteScalar(sqlCount, null);
                int count = countResult != null ? Convert.ToInt32(countResult) : 0;

                int currentID = 1;
                if (count > 0)
                {
                    string sqlMax = "SELECT MAX(ID) FROM Tab_Solar";
                    object maxResult = DataRepository.ExecuteScalar(sqlMax, null);
                    currentID = (maxResult != null ? Convert.ToInt32(maxResult) : 0) + 1;
                }

                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbTransaction trans = conn.BeginTransaction())
                    {
                        using (OleDbCommand cmd = new OleDbCommand())
                        {
                            cmd.Connection = conn;
                            cmd.Transaction = trans;
                            cmd.CommandText = "INSERT INTO Tab_Solar (ID, ID_Klimaregion, Außen_Temp) VALUES (?, ?, ?)";

                            cmd.Parameters.Add("@id", OleDbType.Integer);
                            cmd.Parameters.Add("@regId", OleDbType.Integer);
                            cmd.Parameters.Add("@temp", OleDbType.Double);

                            try
                            {
                                foreach (var item in list)
                                {
                                    cmd.Parameters[0].Value = currentID;
                                    cmd.Parameters[1].Value = ID_Klimaregion;
                                    cmd.Parameters[2].Value = item.Außen_Temp;

                                    cmd.ExecuteNonQuery();
                                    currentID++;
                                }

                                trans.Commit();
                                return true;
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();
                                Console.WriteLine("Fehler beim Massen-Insert in der Schleife: " + ex.Message);
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Insert: " + ex.Message);
                return false;
            }
        }

        public bool WriteDataTable(DataTable dt, string szName, OleDbTransaction transaction)
        {
            if (dt == null) return false;

            try
            {
                OleDbConnection conn = transaction.Connection;

                int nextID = 1;
                using (OleDbCommand cmdMax = new OleDbCommand("SELECT MAX(ID) FROM Tab_Solar", conn, transaction))
                {
                    object maxRes = cmdMax.ExecuteScalar();
                    nextID = (maxRes != DBNull.Value && maxRes != null ? Convert.ToInt32(maxRes) : 0) + 1;
                }

                int refID = 0;
                string sqlRef = "SELECT ID_Klimaregion FROM Tab_Klimaregion WHERE Name = ?";
                using (OleDbCommand cmdRef = new OleDbCommand(sqlRef, conn, transaction))
                {
                    cmdRef.Parameters.Add("@name", OleDbType.VarWChar).Value = szName ?? (object)DBNull.Value;
                    object refRes = cmdRef.ExecuteScalar();
                    if (refRes != null && refRes != DBNull.Value)
                    {
                        refID = Convert.ToInt32(refRes);
                    }
                }

                // Typsicheres, hocheffizientes zeilenweises Schreiben über ein Command-Objekt
                string sqlInsert = "INSERT INTO Tab_Solar (ID, ID_Klimaregion, Außen_Temp) VALUES (?, ?, ?)";
                using (OleDbCommand cmdInsert = new OleDbCommand(sqlInsert, conn, transaction))
                {
                    cmdInsert.Parameters.Add("@id", OleDbType.Integer);
                    cmdInsert.Parameters.Add("@regId", OleDbType.Integer);
                    cmdInsert.Parameters.Add("@temp", OleDbType.Double);

                    foreach (DataRow row in dt.Rows)
                    {
                        cmdInsert.Parameters[0].Value = nextID++;
                        cmdInsert.Parameters[1].Value = refID;

                        // Dynamische Typprüfung für die übergebene DataTable
                        cmdInsert.Parameters[2].Value = row[0] != DBNull.Value ? Convert.ToDouble(row[0]) : 0.0;

                        cmdInsert.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei WriteDataTable: " + ex.Message);
                MessageBox.Show("Fehler beim Schreiben der Tabellendaten: " + ex.Message);
                return false;
            }
        }
    }
}