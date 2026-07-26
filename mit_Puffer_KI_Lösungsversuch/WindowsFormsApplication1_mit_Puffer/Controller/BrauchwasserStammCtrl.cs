using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    // Controller fuer die Brauchwasser-STAMMDATEN (Tab_Brauchwasser_STAMM) samt Typ-Stammtabelle
    // (Tab_Brauchwassertyp_STAMM). Katalog: Schluessel = ID, Namensfeld = Bezeichner, neues Feld ReadOnly.
    // Analog zu ProzesswaermeStammCtrl. Enthaelt Katalog-Lesen, Admin-Loeschen (mit Schutz) und die
    // zentrale Kopierlogik STAMM -> Projekt (Kopf + Typ-Profil, Master-Detail).
    // Hinweis: die Projekt-Tabellen Tab_Brauchwasser / Tab_Brauchwassertyp besitzen KEIN ReadOnly-Feld.
    // Parameternamen sind bewusst eindeutig und praefixfrei (ACE-OLEDB verwechselt sonst @m1/@m10 etc.).
    class BrauchwasserStammCtrl : BrauchwasserModel
    {
        public const string TABLE       = "Tab_Brauchwasser_STAMM";
        public const string TYP_STAMM   = "Tab_Brauchwassertyp_STAMM";
        public const string TABLE_PROJ  = "Tab_Brauchwasser";
        public const string TYP_PROJ    = "Tab_Brauchwassertyp";

        private List<BrauchwasserModel> _internalList = new List<BrauchwasserModel>();
        public int rows => _internalList.Count;
        public List<BrauchwasserModel> items => _internalList;

        public BrauchwasserStammCtrl() { }

        #region --- READ (Katalog) ---

        private void MapRow(DataRow row, DataTable dt, BrauchwasserModel item)
        {
            if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.m_ID = Convert.ToInt32(row["ID"]);
            if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) item.m_szBezeichner = row["Bezeichner"].ToString();
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
                BrauchwasserModel item = new BrauchwasserModel();
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
            m_ID = 0; m_szBezeichner = ""; m_szTyp = ""; m_szBeschreibung = ""; m_bReadOnly = false;
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

        // Loescht einen Brauchwasser-Stammdatensatz (per Bezeichner), sofern nicht schreibgeschuetzt.
        public bool Delete(string szBezeichner)
        {
            if (IsReadOnly(szBezeichner))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschuetzt (ReadOnly) und kann nicht geloescht werden.",
                    "Schreibgeschuetzt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return DataRepository.ExecuteSQL("DELETE FROM " + TABLE + " WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""));
        }

        #endregion

        #region --- STAMM -> PROJEKT KOPIE (Master-Detail) ---

        // Projekt-Brauchwasser-ID (Tab_Brauchwasser.ID) zu einem Bezeichner im Projekt, oder 0.
        public static int GetProjektId(string szBezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + TABLE_PROJ + " WHERE Bezeichner = ? AND ID_Projekt = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""),
                new OleDbParameter("@proj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Kopiert einen Stamm-Brauchwassersatz (+ zugehoeriges Typ-Profil) ins Projekt, falls noch nicht vorhanden.
        // Rueckgabe: Projekt-Brauchwasser-ID (Tab_Brauchwasser.ID), -1 bei Fehler.
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
                int neuBwId;
                using (OleDbCommand c = new OleDbCommand("SELECT Max(ID) FROM " + TABLE_PROJ, conn, trans))
                {
                    object m = c.ExecuteScalar();
                    neuBwId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                }

                // Kopf ins Projekt (Monatswerte per Name). KEIN ReadOnly (Projekttabelle hat es nicht).
                StringBuilder cols = new StringBuilder("ID, ID_Projekt, Bezeichner, Typ, Beschreibung");
                StringBuilder vals = new StringBuilder("?, ?, ?, ?, ?");
                for (int i = 1; i <= 12; i++) { cols.Append(", Monat_" + i); vals.Append(", ?"); }
                using (OleDbCommand c = new OleDbCommand(
                    "INSERT INTO " + TABLE_PROJ + " (" + cols + ") VALUES (" + vals + ")", conn, trans))
                {
                    c.Parameters.Add(new OleDbParameter("@hid", neuBwId));
                    c.Parameters.Add(new OleDbParameter("@hproj", idProjekt));
                    c.Parameters.Add(new OleDbParameter("@hbez", szBezeichner));
                    c.Parameters.Add(new OleDbParameter("@htyp", (object)typName ?? DBNull.Value));
                    c.Parameters.Add(new OleDbParameter("@hbesch", ColOrNull(h, "Beschreibung")));
                    for (int i = 1; i <= 12; i++)
                        c.Parameters.Add(new OleDbParameter("@mon" + i.ToString("D2"), ColOrNull(h, "Monat_" + i)));
                    c.ExecuteNonQuery();
                }

                // Typ-Profil ins Projekt (dynamische Spaltenliste inkl. der Zahlen-Spalten [1]..[N]). KEIN ReadOnly.
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
                        StringBuilder tc = new StringBuilder("ID, ID_Brauchwasser, ID_Projekt, Typname, Beschreibung");
                        StringBuilder tv = new StringBuilder("?, ?, ?, ?, ?");
                        foreach (string col in profil) { tc.Append(", [" + col + "]"); tv.Append(", ?"); }

                        using (OleDbCommand c = new OleDbCommand(
                            "INSERT INTO " + TYP_PROJ + " (" + tc + ") VALUES (" + tv + ")", conn, trans))
                        {
                            c.Parameters.Add(new OleDbParameter("@tid", neuTypId++));
                            c.Parameters.Add(new OleDbParameter("@tbw", neuBwId));
                            c.Parameters.Add(new OleDbParameter("@tproj", idProjekt));
                            c.Parameters.Add(new OleDbParameter("@ttypn", (object)typName ?? DBNull.Value));
                            c.Parameters.Add(new OleDbParameter("@tbesch", ColOrNull(tr, "Beschreibung")));
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
                return neuBwId;
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                Console.WriteLine("Fehler beim Kopieren des Brauchwassers aus den Stammdaten: " + ex.Message);
                return -1;
            }
            finally { try { conn.Close(); } catch { } }
        }

        private static object ColOrNull(DataRow row, string col)
        {
            return (row.Table.Columns.Contains(col) && row[col] != DBNull.Value) ? row[col] : (object)DBNull.Value;
        }

        #endregion

        #region --- KATALOG-SCHREIBEN (Kopf + Typ) ---

        public bool Exists(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar("SELECT ID FROM " + TABLE + " WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value;
        }

        // Schreibt einen Katalog-Kopf (Tab_Brauchwasser_STAMM). isNew: INSERT (neue ID, ReadOnly=false),
        // sonst UPDATE per Bezeichner (mit ReadOnly-Schutz). Praefixfreie Parameternamen.
        public bool SaveHead(string bez, string typ, string beschr, double[] monat, bool isNew)
        {
            if (isNew)
            {
                int newId = DataRepository.GetMaxID(TABLE) + 1;
                var cols = new StringBuilder("ID, Bezeichner, Typ, Beschreibung");
                var vals = new StringBuilder("?, ?, ?, ?");
                var ps = new List<OleDbParameter>
                {
                    new OleDbParameter("@hid", OleDbType.Integer) { Value = newId },
                    new OleDbParameter("@hbez", OleDbType.VarWChar) { Value = (object)(bez ?? "") },
                    new OleDbParameter("@htyp", OleDbType.VarWChar) { Value = (object)(typ ?? "") },
                    new OleDbParameter("@hbeschr", OleDbType.VarWChar) { Value = (object)(beschr ?? "") }
                };
                for (int i = 0; i < 12; i++)
                {
                    cols.Append(", Monat_" + (i + 1)); vals.Append(", ?");
                    ps.Add(new OleDbParameter("@mon" + (i + 1).ToString("D2"), OleDbType.Double) { Value = monat[i] });
                }
                cols.Append(", ReadOnly"); vals.Append(", ?");
                ps.Add(new OleDbParameter("@hro", OleDbType.Boolean) { Value = false });
                return DataRepository.ExecuteSQL("INSERT INTO " + TABLE + " (" + cols + ") VALUES (" + vals + ")", ps.ToArray());
            }
            else
            {
                if (IsReadOnly(bez))
                {
                    MessageBox.Show("Dieser Stammdatensatz ist schreibgeschuetzt (ReadOnly) und kann nicht ueberschrieben werden.",
                        "Schreibgeschuetzt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
                var set = new StringBuilder("Typ = ?, Beschreibung = ?");
                var ps = new List<OleDbParameter>
                {
                    new OleDbParameter("@utyp", OleDbType.VarWChar) { Value = (object)(typ ?? "") },
                    new OleDbParameter("@ubeschr", OleDbType.VarWChar) { Value = (object)(beschr ?? "") }
                };
                for (int i = 0; i < 12; i++)
                {
                    set.Append(", Monat_" + (i + 1) + " = ?");
                    ps.Add(new OleDbParameter("@umon" + (i + 1).ToString("D2"), OleDbType.Double) { Value = monat[i] });
                }
                ps.Add(new OleDbParameter("@ukey", OleDbType.VarWChar) { Value = (object)(bez ?? "") });
                return DataRepository.ExecuteSQL("UPDATE " + TABLE + " SET " + set + " WHERE Bezeichner = ?", ps.ToArray());
            }
        }

        // --- Typ-Katalog (Tab_Brauchwassertyp_STAMM) ---
        public static bool TypIsReadOnly(string bez)
        {
            object v = DataRepository.ExecuteScalar("SELECT ReadOnly FROM " + TYP_STAMM + " WHERE Bezeichner = ?",
                new OleDbParameter("@bez", bez ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        // Legt einen neuen Typ (nur Kopf, Profil = 0) im Katalog an. Rueckgabe: neue ID oder 0.
        public static int TypNew(string bez)
        {
            int newId = DataRepository.GetMaxID(TYP_STAMM) + 1;
            bool ok = DataRepository.ExecuteSQL(
                "INSERT INTO " + TYP_STAMM + " (ID, Bezeichner, ReadOnly) VALUES (?, ?, ?)",
                new OleDbParameter("@tid", OleDbType.Integer) { Value = newId },
                new OleDbParameter("@tbez", OleDbType.VarWChar) { Value = (object)(bez ?? "") },
                new OleDbParameter("@tro", OleDbType.Boolean) { Value = false });
            return ok ? newId : 0;
        }

        public static bool TypDelete(string bez)
        {
            if (TypIsReadOnly(bez))
            {
                MessageBox.Show("Dieser Typ ist schreibgeschuetzt (ReadOnly) und kann nicht geloescht werden.",
                    "Schreibgeschuetzt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return DataRepository.ExecuteSQL("DELETE FROM " + TYP_STAMM + " WHERE Bezeichner = ?",
                new OleDbParameter("@bez", bez ?? ""));
        }

        #endregion

    }
}
