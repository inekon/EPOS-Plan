using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class PhotovoltaikCtrl : PhotovoltaikModel
    {
        // --- Kompatibilitäts-Layer nach deinem Vorbild ---
        private List<PhotovoltaikModel> _internalList = new List<PhotovoltaikModel>();

        public int rows => _internalList.Count;
        public new List<PhotovoltaikModel> items => _internalList;

        [Obsolete("Verwendung von ODBC entfernt. DB-Operationen laufen jetzt über das DataRepository.")]
        public OleDbCommand DBCommand;

        public PhotovoltaikModel model = new PhotovoltaikModel();

        public PhotovoltaikCtrl()
        {
#pragma warning disable CS0618
            DBCommand = new OleDbCommand();
#pragma warning restore CS0618
        }

        ~PhotovoltaikCtrl()
        {
#pragma warning disable CS0618
            DBCommand?.Dispose();
#pragma warning restore CS0618
        }

        #region --- DATABASE READ OPERATIONS ---

        public void ReadAll(string szFilter = "")
        {
            string sql;

            if (string.IsNullOrEmpty(szFilter))
                sql = "SELECT * FROM Tab_PV ORDER BY Modulname";
            else
                sql = "SELECT * FROM Tab_PV WHERE " + szFilter + " ORDER BY Modulname";

            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();

            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    PhotovoltaikModel item = new PhotovoltaikModel();

                    if (row[0] != DBNull.Value) item.m_ID = Convert.ToInt32(row[0]);
                    if (row[1] != DBNull.Value) item.m_szName = row[1].ToString();
                    if (row[2] != DBNull.Value) item.m_szFirma = row[2].ToString();
                    if (row[3] != DBNull.Value) item.m_szBeschreibung = row[3].ToString();
                    if (row[4] != DBNull.Value) item.m_Leistung = Convert.ToDouble(row[4]);
                    if (row[5] != DBNull.Value) item.m_Wirkungsgrad = Convert.ToDouble(row[5]);
                    if (row[6] != DBNull.Value) item.m_U_Mpp = Convert.ToDouble(row[6]);
                    if (row[7] != DBNull.Value) item.m_U_Leerlauf = Convert.ToDouble(row[7]);
                    if (row[8] != DBNull.Value) item.m_I_Mpp = Convert.ToDouble(row[8]);
                    if (row[9] != DBNull.Value) item.m_I_Kurzschluss = Convert.ToDouble(row[9]);
                    if (row[10] != DBNull.Value) item.m_alpha_SC = Convert.ToDouble(row[10]);
                    if (row[11] != DBNull.Value) item.m_beta_OC = Convert.ToDouble(row[11]);
                    if (row[12] != DBNull.Value) item.m_Temp_Coeff_Pmax = Convert.ToDouble(row[12]);
                    if (row[13] != DBNull.Value) item.m_T_NOCT = Convert.ToDouble(row[13]);
                    if (row[14] != DBNull.Value) item.m_Laenge = Convert.ToDouble(row[14]);
                    if (row[15] != DBNull.Value) item.m_Breite = Convert.ToDouble(row[15]);
                    if (row[16] != DBNull.Value) item.m_Modulkosten = Convert.ToDouble(row[16]);

                    _internalList.Add(item);
                }
            }
        }

        public void ReadSingle(int ID)
        {
            // Bei ReadSingle befüllst du ja die Felder der eigenen Instanz (m_ID, m_szName etc.),
            // aber wir können zur Sicherheit die Liste leeren oder das gefundene Element hineinlegen,
            // falls das UI nach einem ReadSingle auch auf items[0] zugreift.
            _internalList.Clear();

            string sql = "SELECT * FROM Tab_PV WHERE ID = ?";
            OleDbParameter parameter = new OleDbParameter("?", ID);

            DataTable dt = DataRepository.GetDataTable(sql, parameter);

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (row[0] != DBNull.Value) m_ID = Convert.ToInt32(row[0]);
                if (row[1] != DBNull.Value) m_szName = row[1].ToString();
                if (row[2] != DBNull.Value) m_szFirma = row[2].ToString();
                if (row[3] != DBNull.Value) m_szBeschreibung = row[3].ToString();
                if (row[4] != DBNull.Value) m_Leistung = Convert.ToDouble(row[4]);
                if (row[5] != DBNull.Value) m_Wirkungsgrad = Convert.ToDouble(row[5]);
                if (row[6] != DBNull.Value) m_U_Mpp = Convert.ToDouble(row[6]);
                if (row[7] != DBNull.Value) m_U_Leerlauf = Convert.ToDouble(row[7]);
                if (row[8] != DBNull.Value) m_I_Mpp = Convert.ToDouble(row[8]);
                if (row[9] != DBNull.Value) m_I_Kurzschluss = Convert.ToDouble(row[9]);
                if (row[10] != DBNull.Value) m_alpha_SC = Convert.ToDouble(row[10]);
                if (row[11] != DBNull.Value) m_beta_OC = Convert.ToDouble(row[11]);
                if (row[12] != DBNull.Value) m_Temp_Coeff_Pmax = Convert.ToDouble(row[12]);
                if (row[13] != DBNull.Value) m_T_NOCT = Convert.ToDouble(row[13]);
                if (row[14] != DBNull.Value) m_Laenge = Convert.ToDouble(row[14]);
                if (row[15] != DBNull.Value) m_Breite = Convert.ToDouble(row[15]);
                if (row[16] != DBNull.Value) m_Modulkosten = Convert.ToDouble(row[16]);

                // Kopie in die interne Liste legen, damit rows auf 1 springt
                _internalList.Add(this);
            }
        }

        #endregion

        #region --- DATABASE WRITE OPERATIONS ---

        public bool Update()
        {
            try
            {
                string sql = @"
                    UPDATE Tab_PV 
                    SET 
                        Firma = ?, 
                        Beschreibung = ?, 
                        Leistung = ?, 
                        Wirkungsgrad = ?, 
                        U_Mpp = ?, 
                        U_Leerlauf = ?, 
                        I_Mpp = ?, 
                        I_Kurzschluss = ?, 
                        alpha_SC = ?, 
                        beta_OC = ?, 
                        gamma_PMP = ?, 
                        T_NOCT = ?, 
                        Laenge = ?, 
                        Breite = ?, 
                        Modulkosten = ? 
                    WHERE 
                        Modulname = ?";

                OleDbParameter[] parameters = new OleDbParameter[]
                {
                    new OleDbParameter("?", model.m_szFirma ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_szBeschreibung ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Leistung),
                    new OleDbParameter("?", model.m_Wirkungsgrad),
                    new OleDbParameter("?", model.m_U_Mpp),
                    new OleDbParameter("?", model.m_U_Leerlauf),
                    new OleDbParameter("?", model.m_I_Mpp),
                    new OleDbParameter("?", model.m_I_Kurzschluss),

                    new OleDbParameter("?", model.m_alpha_SC == 0 ? DBNull.Value : (object)model.m_alpha_SC),
                    new OleDbParameter("?", model.m_beta_OC == 0 ? DBNull.Value : (object)model.m_beta_OC),
                    new OleDbParameter("?", model.m_Temp_Coeff_Pmax == 0 ? DBNull.Value : (object)model.m_Temp_Coeff_Pmax),
                    new OleDbParameter("?", model.m_T_NOCT == 0 ? DBNull.Value : (object)model.m_T_NOCT),

                    new OleDbParameter("?", model.m_Laenge),
                    new OleDbParameter("?", model.m_Breite),
                    new OleDbParameter("?", model.m_Modulkosten),
                    new OleDbParameter("?", model.m_szName ?? (object)DBNull.Value)
                };

                DataRepository.ExecuteNonQuery(sql, parameters);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler beim Update: " + ex.Message);
                return false;
            }
        }

        #endregion
    }
}