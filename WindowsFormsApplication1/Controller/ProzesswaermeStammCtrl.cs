using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    // Controller fuer die Prozesswaerme-STAMMDATEN (Tab_Prozesswaerme_STAMM) samt Typ-Stammtabelle
    // (Tab_Prozesstyp_STAMM). Katalog/Stammdaten: Schluessel = ID, Namensfeld = Bezeichner
    // (im Model weiterhin als m_szProzessname gefuehrt). Neues Feld ReadOnly.
    // Enthaelt die Admin-Leseoperationen sowie die zentrale Kopierlogik STAMM -> Projekt
    // (Prozess + zugehoeriges Typ-Profil, Master-Detail).
    class ProzesswaermeStammCtrl : ProzesswaermeModel
    {
        public const string TABLE       = "Tab_Prozesswaerme_STAMM";
        public const string TYP_STAMM   = "Tab_Prozesstyp_STAMM";
        public const string TABLE_PROJ  = "Tab_Prozesswaerme";
        public const string TYP_PROJ    = "Tab_Prozesstyp";

        private List<ProzesswaermeModel> _internalList = new List<ProzesswaermeModel>();
        public int rows => _internalList.Count;
        public new List<ProzesswaermeModel> items => _internalList;

        public ProzesswaermeStammCtrl() { }

        #region --- READ (Katalog) ---

        private void MapRow(DataRow row, DataTable dt, ProzesswaermeModel item)
        {
            if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.m_ID = Convert.ToInt32(row["ID"]);
            if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) item.m_szProzessname = row["Bezeichner"].ToString();
            if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value) item.m_szTyp = row["Typ"].ToString();
            if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) item.m_szBeschreibung = row["Beschreibung"].ToString();
            if (dt.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value) item.m_bReadOnly = Convert.ToBoolean(row["ReadOnly"]);
            for (int i = 0; i < 12; i++)
            {
                string col = "Monat_" + (i + 1);
                if (dt.Columns.Contains(col) && row[col] != DBNull.Value) item.m_Monat[i] = Convert.ToDouble(row[col]);
            }
        }

        public void ReadAll()
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + TABLE + " ORDER BY Bezeichner", null);
            _internalList.Clear();
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
            {
                ProzesswaermeModel item = new ProzesswaermeModel();
                MapRow(row, dt, item);
                _internalList.Add(item);
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + TABLE + " WHERE Bezeichner = ?",
                new[] { new OleDbParameter("@bez", szBezeichner ?? (object)DBNull.Value) });
            _internalList.Clear();
            m_ID = 0; m_szProzessname = ""; m_szTyp = ""; m_szBeschreibung = ""; m_bReadOnly = false;
            for (int i = 0; i < 12; i++) m_Monat[i] = 0.0;
            if (dt != null && dt.Rows.Count > 0)
            {
                MapRow(dt.Rows[0], dt, this);
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

        // Loescht einen Prozess-Stammdatensatz (per Bezeichner), sofern nicht schreibgeschuetzt.
        public bool Delete(string szBezeichner)
        {
            if (IsReadOnly(szBezeichner))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return DataRepository.ExecuteSQL("DELETE FROM " + TABLE + " WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""));
        }

        #endregion

        #region --- STAMM -> PROJEKT KOPIE (Master-Detail) ---

        // Projekt-Prozess-ID (Tab_Prozesswaerme.ID) zu einem Bezeichner im Projekt, oder 0.
        public static int GetProjektId(string szBezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + TABLE_PROJ + " WHERE Bezeichner = ? AND ID_Projekt = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""),
                new OleDbParameter("@proj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Kopiert einen Stamm-Prozess (+ zugehoeriges Typ-Profil) ins Projekt, falls noch nicht vorhanden.
        // Rueckgabe: Projekt-Prozess-ID (Tab_Prozesswaerme.ID), -1 bei Fehler.
        public static int CopyFromStamm(string szBezeichner, int idProjekt)
        {
            if (string.IsNullOrEmpty(szBezeichner) || idProjekt <= 0) return -1;

            int vorhanden = GetProjektId(szBezeichner, idProjekt);
            if (vorhanden > 0) return vorhanden;

            // Kopf-Stammsatz lesen
            DataTable head = DataRepository.GetDataTable(
                "SELECT * FROM " + TABLE + " WHERE Bezeichner = ?",
                new[] { new OleDbParameter("@bez", szBezeichner) });
            if (head == null || head.Rows.Count == 0) return -1;
            DataRow h = head.Rows[0];
            string typName = h.Table.Columns.Contains("Typ") && h["Typ"] != DBNull.Value ? h["Typ"].ToString() : "";

            // Typ-Profil(e) aus Stamm lesen (per Bezeichner = Typname)
            DataTable dtTyp = null;
            if (!string.IsNullOrEmpty(typName))
            {
                dtTyp = DataRepository.GetDataTable(
                    "SELECT * FROM " + TYP_STAMM + " WHERE Bezeichner = ?",
                    new[] { new OleDbParameter("@bez", typName) });
            }

            var (conn, trans) = DataRepository.BeginTransaction();
            try
            {
                int neuProzId;
                using (OleDbCommand c = new OleDbCommand("SELECT Max(ID) FROM " + TABLE_PROJ, conn, trans))
                {
                    object m = c.ExecuteScalar();
                    neuProzId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                }

                // Kopf ins Projekt (Monatswerte per Name)
                StringBuilder cols = new StringBuilder("ID, ID_Projekt, Bezeichner, Typ, Beschreibung");
                StringBuilder vals = new StringBuilder("?, ?, ?, ?, ?");
                for (int i = 1; i <= 12; i++) { cols.Append(", Monat_" + i); vals.Append(", ?"); }
                cols.Append(", ReadOnly"); vals.Append(", ?");
                using (OleDbCommand c = new OleDbCommand(
                    "INSERT INTO " + TABLE_PROJ + " (" + cols + ") VALUES (" + vals + ")", conn, trans))
                {
                    c.Parameters.Add(new OleDbParameter("@hid", neuProzId));
                    c.Parameters.Add(new OleDbParameter("@hproj", idProjekt));
                    c.Parameters.Add(new OleDbParameter("@hbez", szBezeichner));
                    c.Parameters.Add(new OleDbParameter("@htyp", (object)typName ?? DBNull.Value));
                    c.Parameters.Add(new OleDbParameter("@hbeschr", ColOrNull(h, "Beschreibung")));
                    for (int i = 1; i <= 12; i++)
                        c.Parameters.Add(new OleDbParameter("@hmon" + i.ToString("D2"), ColOrNull(h, "Monat_" + i)));
                    c.Parameters.Add(new OleDbParameter("@hro", false));
                    c.ExecuteNonQuery();
                }

                // Typ-Profil ins Projekt (dynamische Spaltenliste inkl. der Zahlen-Spalten [1]..[N])
                if (dtTyp != null && dtTyp.Rows.Count > 0)
                {
                    int neuTypId;
                    using (OleDbCommand c = new OleDbCommand("SELECT Max(ID) FROM " + TYP_PROJ, conn, trans))
                    {
                        object m = c.ExecuteScalar();
                        neuTypId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                    }

                    // Zahlen-Spalten des Stamm-Typs ermitteln (z.B. [1]..[168])
                    List<string> profil = new List<string>();
                    foreach (DataColumn dc in dtTyp.Columns)
                        if (int.TryParse(dc.ColumnName, out _)) profil.Add(dc.ColumnName);

                    foreach (DataRow tr in dtTyp.Rows)
                    {
                        StringBuilder tc = new StringBuilder("ID, ID_Prozesswaerme, ID_Projekt, Typname, Beschreibung, ReadOnly");
                        StringBuilder tv = new StringBuilder("?, ?, ?, ?, ?, ?");
                        foreach (string col in profil) { tc.Append(", [" + col + "]"); tv.Append(", ?"); }

                        using (OleDbCommand c = new OleDbCommand(
                            "INSERT INTO " + TYP_PROJ + " (" + tc + ") VALUES (" + tv + ")", conn, trans))
                        {
                            c.Parameters.Add(new OleDbParameter("@tid", neuTypId++));
                            c.Parameters.Add(new OleDbParameter("@tpw", neuProzId));
                            c.Parameters.Add(new OleDbParameter("@tproj", idProjekt));
                            c.Parameters.Add(new OleDbParameter("@ttypn", (object)typName ?? DBNull.Value));
                            c.Parameters.Add(new OleDbParameter("@tbeschr", ColOrNull(tr, "Beschreibung")));
                            c.Parameters.Add(new OleDbParameter("@tro", false));
                            int k = 0;
                            foreach (string col in profil)
                            {
                                object v = tr[col] != DBNull.Value ? tr[col] : (object)DBNull.Value;
                                c.Parameters.Add(new OleDbParameter("@cp" + (k++).ToString("D3"), v));
                            }
                            c.ExecuteNonQuery();
                        }
                    }
                }

                trans.Commit();
                return neuProzId;
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                Console.WriteLine("Fehler beim Kopieren der Prozesswaerme aus den Stammdaten: " + ex.Message);
                return -1;
            }
            finally { try { conn.Close(); } catch { } }
        }

        private static object ColOrNull(DataRow row, string col)
        {
            return (row.Table.Columns.Contains(col) && row[col] != DBNull.Value) ? row[col] : (object)DBNull.Value;
        }

        #endregion
    }
}
