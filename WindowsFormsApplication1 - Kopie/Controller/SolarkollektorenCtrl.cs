using System;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class SolarkollektorenCtrl : SolarkollektorenModel
    {
        // Auf OleDbCommand umgestellt, damit es mit der übergeordneten Transaktion kompatibel ist
        public OleDbCommand DBCommand;
        public SolarkollektorenModel model = new SolarkollektorenModel();

        public SolarkollektorenCtrl()
        {
            // Initialisierung eines Standard-Commands. 
            // Wichtig: Wird dieses Control in einer Transaktion genutzt, überschreibt die Form 
            // die Connection und die Transaction dieses Objekts von außen.
            DBCommand = new OleDbCommand();
        }

        ~SolarkollektorenCtrl()
        {
            if (DBCommand != null)
            {
                DBCommand.Dispose();
            }
        }

        public void ReadAll(string szFilter = "")
        {
            string sql;
            DataTable dt;

            if (szFilter == "")
            {
                sql = "SELECT * FROM Tab_Solarkollektoren ORDER BY Kollektorname";
                dt = DataRepository.GetDataTable(sql);
            }
            else
            {
                // Hinweis: Falls szFilter dynamische Werte enthält, sollte idealerweise auch dieser 
                // parametrisiert werden. Für den 1:1 Umbau belassen wir es bei der bestehenden Logik.
                sql = "SELECT * FROM Tab_Solarkollektoren WHERE " + szFilter + " ORDER BY Kollektorname";
                dt = DataRepository.GetDataTable(sql);
            }

            items = new SolarkollektorenModel[1000];
            rows = 0;

            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (rows >= items.Length) break;

                    SolarkollektorenModel item = new SolarkollektorenModel();

                    if (row[0] != DBNull.Value) item.m_ID = Convert.ToInt32(row[0]);
                    if (row[1] != DBNull.Value) item.m_szKollektorname = row[1].ToString();
                    if (row[2] != DBNull.Value) item.m_szFirma = row[2].ToString();
                    if (row[3] != DBNull.Value) item.m_szBeschreibung = row[3].ToString();
                    if (row[4] != DBNull.Value) item.m_szKollektortyp = row[4].ToString();
                    if (row[5] != DBNull.Value) item.m_Modulfläche = Convert.ToDouble(row[5]);
                    if (row[6] != DBNull.Value) item.m_Aperturfläche = Convert.ToDouble(row[6]);
                    if (row[7] != DBNull.Value) item.m_h0 = Convert.ToDouble(row[7]);
                    if (row[8] != DBNull.Value) item.m_k1 = Convert.ToDouble(row[8]);
                    if (row[9] != DBNull.Value) item.m_k2 = Convert.ToDouble(row[9]);
                    if (row[10] != DBNull.Value) item.m_Kdir = Convert.ToDouble(row[10]);
                    if (row[11] != DBNull.Value) item.m_Kdfu = Convert.ToDouble(row[11]);
                    if (row[12] != DBNull.Value) item.m_Kosten = Convert.ToDouble(row[12]);
                    if (row[13] != DBNull.Value) item.m_Vorlauf = Convert.ToInt32(row[13]);
                    if (row[14] != DBNull.Value) item.m_Ruecklauf = Convert.ToInt32(row[14]);

                    items[rows] = item;
                    rows += 1;
                }
            }
        }

        public void ReadSingle(int ID)
        {
            string sql = "SELECT * FROM Tab_Solarkollektoren WHERE ID = ?";
            OleDbParameter parameter = new OleDbParameter("?", ID);
            DataTable dt = DataRepository.GetDataTable(sql, parameter);

            rows = 0;

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (row[0] != DBNull.Value) m_ID = Convert.ToInt32(row[0]);
                if (row[1] != DBNull.Value) m_szKollektorname = row[1].ToString();
                if (row[2] != DBNull.Value) m_szFirma = row[2].ToString();
                if (row[3] != DBNull.Value) m_szBeschreibung = row[3].ToString();
                if (row[4] != DBNull.Value) m_szKollektortyp = row[4].ToString();
                if (row[5] != DBNull.Value) m_Modulfläche = Convert.ToDouble(row[5]);
                if (row[6] != DBNull.Value) m_Aperturfläche = Convert.ToDouble(row[6]);
                if (row[7] != DBNull.Value) m_h0 = Convert.ToDouble(row[7]);
                if (row[8] != DBNull.Value) m_k1 = Convert.ToDouble(row[8]);
                if (row[9] != DBNull.Value) m_k2 = Convert.ToDouble(row[9]);
                if (row[10] != DBNull.Value) m_Kdir = Convert.ToDouble(row[10]);
                if (row[11] != DBNull.Value) m_Kdfu = Convert.ToDouble(row[11]);
                if (row[12] != DBNull.Value) m_Kosten = Convert.ToDouble(row[12]);
                if (row[13] != DBNull.Value) m_Vorlauf = Convert.ToInt32(row[13]);
                if (row[14] != DBNull.Value) m_Ruecklauf = Convert.ToInt32(row[14]);

                rows = 1;
            }
        }

        public bool Update()
        {
            try
            {
                // Vollständig parametrisiertes SQL-Statement (schützt vor SQL-Injections)
                string sql = @"UPDATE Tab_Solarkollektoren SET 
                                Firma = ?, 
                                Beschreibung = ?, 
                                Kollektortyp = ?, 
                                Modulflaeche = ?, 
                                Aperturflaeche = ?, 
                                h0 = ?, 
                                k1 = ?, 
                                k2 = ?, 
                                Kdir = ?, 
                                Kdfu = ?, 
                                Investitionskosten = ? 
                                WHERE Kollektorname = ?";

                DBCommand.CommandText = sql;
                DBCommand.Parameters.Clear(); // Wichtig: Alte Parameter bei Wiederverwendung leeren

                // Die Reihenfolge der Parameter MUSS exakt der Reihenfolge der '?' im SQL entsprechen!
                DBCommand.Parameters.Add(new OleDbParameter("?", model.m_szFirma ?? (object)DBNull.Value));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.m_szBeschreibung ?? (object)DBNull.Value));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.m_szKollektortyp ?? (object)DBNull.Value));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.m_Modulfläche));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.m_Aperturfläche));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.m_h0));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.m_k1));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.m_k2));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.m_Kdir));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.m_Kdfu));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.m_Kosten));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.m_szKollektorname));

                // Führt den Befehl auf der von außen gesetzten Verbindung & Transaktion aus
                DBCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren des Solarkollektors: " + ex.Message);
                return false;
            }
            return true;
        }
    }
}
