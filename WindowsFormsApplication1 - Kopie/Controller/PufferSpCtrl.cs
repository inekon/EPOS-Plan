using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class PufferSpCtrl : PufferSpModel
    {
        // --- Kompatibilitäts-Layer für bestehenden UI-Code ---
        private List<PufferSpModel> _internalList = new List<PufferSpModel>();
        private bool _hasSingleData = false;

        // Simuliert die alte 'rows' Variable dynamisch
        public int rows => _internalList.Count > 0 ? _internalList.Count : (_hasSingleData ? 1 : 0);

        // Simuliert das alte 'items' Array als Liste
        public List<PufferSpModel> items => _internalList;

        public OleDbCommand DBCommand;
        public PufferSpModel model;

        public PufferSpCtrl()
        {
            DBCommand = new OleDbCommand();
            model = new PufferSpModel();
        }

        ~PufferSpCtrl()
        {
            if (DBCommand != null)
            {
                DBCommand.Dispose();
            }
        }

        public void ReadAll(string filter = "")
        {
            string sql;
            if (filter == "")
            {
                sql = "SELECT * FROM Tab_Pufferspeicher";
            }
            else
            {
                sql = "SELECT * FROM Tab_Pufferspeicher WHERE " + filter;
            }

            DataTable dt = DataRepository.GetDataTable(sql);

            // Liste und Zustand zurücksetzen
            _internalList.Clear();
            _hasSingleData = false;

            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    PufferSpModel item = new PufferSpModel();

                    if (dt.Rows[i][0] != DBNull.Value) item.ID = Convert.ToInt32(dt.Rows[i][0]);
                    if (dt.Rows[i][1] != DBNull.Value) item.Name = dt.Rows[i][1].ToString();
                    if (dt.Rows[i][2] != DBNull.Value) item.Firma = dt.Rows[i][2].ToString();
                    if (dt.Rows[i][3] != DBNull.Value) item.Speichertyp = dt.Rows[i][3].ToString();
                    if (dt.Rows[i][4] != DBNull.Value) item.Betriebsbereitschaftverlust = Convert.ToDouble(dt.Rows[i][4]);
                    if (dt.Rows[i][5] != DBNull.Value) item.Gesamtvolumen = Convert.ToInt32(dt.Rows[i][5]);
                    if (dt.Rows[i][6] != DBNull.Value) item.Investitionskosten = Convert.ToDouble(dt.Rows[i][6]);

                    _internalList.Add(item);
                }
            }
        }

        public bool Delete(string szName)
        {
            try
            {
                string sql = "DELETE FROM Tab_Pufferspeicher WHERE Bezeichner = ?";

                DBCommand.CommandText = sql;
                DBCommand.Parameters.Clear();
                DBCommand.Parameters.Add(new OleDbParameter("?", szName ?? (object)DBNull.Value));

                DBCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Löschen des Pufferspeichers: " + ex.Message);
                return false;
            }
            return true;
        }

        public bool Update()
        {
            try
            {
                string sql = "UPDATE Tab_Pufferspeicher SET " +
                             "Hersteller = ?, " +
                             "Speichertyp = ?, " +
                             "Bereitschaftsverluste = ?, " +
                             "Investitionskosten = ?, " +
                             "Gesamtvolumen = ? " +
                             "WHERE Bezeichner = ?";

                DBCommand.CommandText = sql;
                DBCommand.Parameters.Clear();

                DBCommand.Parameters.Add(new OleDbParameter("?", model.Firma ?? (object)DBNull.Value));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.Speichertyp ?? (object)DBNull.Value));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.Betriebsbereitschaftverlust));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.Investitionskosten));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.Gesamtvolumen));
                DBCommand.Parameters.Add(new OleDbParameter("?", model.Name ?? (object)DBNull.Value));

                DBCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren des Pufferspeichers: " + ex.Message);
                return false;
            }
            return true;
        }
    }
}
