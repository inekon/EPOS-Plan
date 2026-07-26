using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    // Controller fuer die Stammdaten-Tabelle Tab_PV_STAMM (globaler PV-Modul-Katalog).
    // Analog zu HeizkesselStammCtrl / PufferSpStammCtrl:
    //   - Tabelle = Tab_PV_STAMM
    //   - DB-Spalte Bezeichner wird auf m_szName abgebildet, gamma_PMP auf m_Temp_Coeff_Pmax
    //   - liest/schreibt das Feld ReadOnly
    //   - Insert() vergibt eine explizite ID (MAX+1) und setzt ReadOnly = false
    //   - Update()/Delete() verweigern schreibgeschuetzte Datensaetze
    // Alle DB-Zugriffe laufen ueber DataRepository.
    public class PhotovoltaikStammCtrl : PhotovoltaikModel
    {
        public const string TABLE = "Tab_PV_STAMM";

        private List<PhotovoltaikModel> _internalList = new List<PhotovoltaikModel>();
        public int rows => _internalList.Count;
        public new List<PhotovoltaikModel> items => _internalList;

        public bool m_bReadOnly = false;

        public void ReadAll(string filter = "")
        {
            string sql = "SELECT * FROM [" + TABLE + "]";
            if (!string.IsNullOrEmpty(filter)) sql += " WHERE " + filter;
            sql += " ORDER BY Bezeichner";

            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();
            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                _internalList.Add(MapRowToModel(row));
            }
        }

        public void ReadSingle(string szName)
        {
            _internalList.Clear();
            string sql = "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ?";
            DataTable dt = DataRepository.GetDataTable(sql, new OleDbParameter("@bez", szName ?? (object)DBNull.Value));

            if (dt != null && dt.Rows.Count > 0)
            {
                FillFromRow(this, dt.Rows[0]);
                this.m_bReadOnly = ReadOnlyOf(dt.Rows[0]);
                _internalList.Add(this);
            }
        }

        public bool Exists(string szName)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szName ?? ""));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        public static bool IsReadOnlyStatic(string szName)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szName ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        public bool InsertFrom(PhotovoltaikModel m)
        {
            if (m != null) CopyFrom(m);
            return Insert();
        }

        public bool UpdateFrom(PhotovoltaikModel m, string szKey)
        {
            if (m != null) CopyFrom(m);
            return Update(szKey);
        }

        public bool Insert()
        {
            int neueId = DataRepository.GetMaxID(TABLE) + 1;

            string sql = @"INSERT INTO [" + TABLE + @"]
                            (ID, Bezeichner, Firma, Beschreibung, Leistung, Wirkungsgrad, U_Mpp, U_Leerlauf,
                             I_Mpp, I_Kurzschluss, alpha_SC, beta_OC, gamma_PMP, T_NOCT, Laenge, Breite, Modulkosten, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            OleDbParameter[] ps = {
                new OleDbParameter("@id", neueId),
                new OleDbParameter("@bez", this.m_szName ?? ""),
                new OleDbParameter("@fir", (object)(this.m_szFirma ?? "")),
                new OleDbParameter("@bes", (object)(this.m_szBeschreibung ?? "")),
                new OleDbParameter("@lei", this.m_Leistung),
                new OleDbParameter("@wir", this.m_Wirkungsgrad),
                new OleDbParameter("@ump", this.m_U_Mpp),
                new OleDbParameter("@ule", this.m_U_Leerlauf),
                new OleDbParameter("@imp", this.m_I_Mpp),
                new OleDbParameter("@iks", this.m_I_Kurzschluss),
                new OleDbParameter("@asc", this.m_alpha_SC),
                new OleDbParameter("@boc", this.m_beta_OC),
                new OleDbParameter("@gam", this.m_Temp_Coeff_Pmax),
                new OleDbParameter("@noc", this.m_T_NOCT),
                new OleDbParameter("@lae", this.m_Laenge),
                new OleDbParameter("@bre", this.m_Breite),
                new OleDbParameter("@mod", this.m_Modulkosten),
                new OleDbParameter("@ro", false)
            };

            bool ok = DataRepository.ExecuteSQL(sql, ps);
            if (ok) this.m_ID = neueId;
            return ok;
        }

        // Aktualisiert den Datensatz. szKey ist der urspruengliche Bezeichner (WHERE-Schluessel);
        // this.m_szName darf einen neuen Bezeichner tragen (Umbenennung).
        public bool Update(string szKey)
        {
            if (IsReadOnlyStatic(szKey))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Bezeichner = ?, Firma = ?, Beschreibung = ?, Leistung = ?, Wirkungsgrad = ?,
                            U_Mpp = ?, U_Leerlauf = ?, I_Mpp = ?, I_Kurzschluss = ?,
                            alpha_SC = ?, beta_OC = ?, gamma_PMP = ?, T_NOCT = ?,
                            Laenge = ?, Breite = ?, Modulkosten = ?
                          WHERE Bezeichner = ?";

            OleDbParameter[] ps = {
                new OleDbParameter("@bez", this.m_szName ?? ""),
                new OleDbParameter("@fir", (object)(this.m_szFirma ?? "")),
                new OleDbParameter("@bes", (object)(this.m_szBeschreibung ?? "")),
                new OleDbParameter("@lei", this.m_Leistung),
                new OleDbParameter("@wir", this.m_Wirkungsgrad),
                new OleDbParameter("@ump", this.m_U_Mpp),
                new OleDbParameter("@ule", this.m_U_Leerlauf),
                new OleDbParameter("@imp", this.m_I_Mpp),
                new OleDbParameter("@iks", this.m_I_Kurzschluss),
                new OleDbParameter("@asc", this.m_alpha_SC),
                new OleDbParameter("@boc", this.m_beta_OC),
                new OleDbParameter("@gam", this.m_Temp_Coeff_Pmax),
                new OleDbParameter("@noc", this.m_T_NOCT),
                new OleDbParameter("@lae", this.m_Laenge),
                new OleDbParameter("@bre", this.m_Breite),
                new OleDbParameter("@mod", this.m_Modulkosten),
                new OleDbParameter("@key", szKey ?? "")
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Delete(string szName)
        {
            if (IsReadOnlyStatic(szName))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string sql = "DELETE FROM [" + TABLE + "] WHERE Bezeichner = ?";
            return DataRepository.ExecuteSQL(sql, new OleDbParameter("@bez", szName ?? ""));
        }

        // --- MAPPING ---

        private void CopyFrom(PhotovoltaikModel m)
        {
            this.m_szName = m.m_szName;
            this.m_szFirma = m.m_szFirma;
            this.m_szBeschreibung = m.m_szBeschreibung;
            this.m_Leistung = m.m_Leistung;
            this.m_Wirkungsgrad = m.m_Wirkungsgrad;
            this.m_U_Mpp = m.m_U_Mpp;
            this.m_U_Leerlauf = m.m_U_Leerlauf;
            this.m_I_Mpp = m.m_I_Mpp;
            this.m_I_Kurzschluss = m.m_I_Kurzschluss;
            this.m_alpha_SC = m.m_alpha_SC;
            this.m_beta_OC = m.m_beta_OC;
            this.m_Temp_Coeff_Pmax = m.m_Temp_Coeff_Pmax;
            this.m_T_NOCT = m.m_T_NOCT;
            this.m_Laenge = m.m_Laenge;
            this.m_Breite = m.m_Breite;
            this.m_Modulkosten = m.m_Modulkosten;
        }

        private static bool ReadOnlyOf(DataRow row)
        {
            return row.Table.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);
        }

        private static double D(DataRow row, string col)
        {
            return (row.Table.Columns.Contains(col) && row[col] != DBNull.Value) ? Convert.ToDouble(row[col]) : 0.0;
        }

        private static void FillFromRow(PhotovoltaikModel m, DataRow row)
        {
            if (row.Table.Columns.Contains("ID") && row["ID"] != DBNull.Value) m.m_ID = Convert.ToInt32(row["ID"]);
            if (row.Table.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) m.m_szName = row["Bezeichner"].ToString();
            if (row.Table.Columns.Contains("Firma") && row["Firma"] != DBNull.Value) m.m_szFirma = row["Firma"].ToString();
            if (row.Table.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) m.m_szBeschreibung = row["Beschreibung"].ToString();
            m.m_Leistung = D(row, "Leistung");
            m.m_Wirkungsgrad = D(row, "Wirkungsgrad");
            m.m_U_Mpp = D(row, "U_Mpp");
            m.m_U_Leerlauf = D(row, "U_Leerlauf");
            m.m_I_Mpp = D(row, "I_Mpp");
            m.m_I_Kurzschluss = D(row, "I_Kurzschluss");
            m.m_alpha_SC = D(row, "alpha_SC");
            m.m_beta_OC = D(row, "beta_OC");
            m.m_Temp_Coeff_Pmax = D(row, "gamma_PMP");
            m.m_T_NOCT = D(row, "T_NOCT");
            m.m_Laenge = D(row, "Laenge");
            m.m_Breite = D(row, "Breite");
            m.m_Modulkosten = D(row, "Modulkosten");
        }

        private PhotovoltaikModel MapRowToModel(DataRow row)
        {
            PhotovoltaikModel m = new PhotovoltaikModel();
            FillFromRow(m, row);
            return m;
        }
    }
}
