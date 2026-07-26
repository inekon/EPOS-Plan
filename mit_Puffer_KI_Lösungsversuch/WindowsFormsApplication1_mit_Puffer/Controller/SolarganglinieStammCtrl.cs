using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    // Controller fuer die Solarganglinie-STAMMDATEN
    // (Tab_Solarganglinie_STAMM + Tab_SolarganglinieDaten_STAMM).
    // Kopf-Schluessel = ID, Name = Bezeichner, zusaetzlich Beschreibung; Feld ReadOnly.
    // Enthaelt die Admin-Operationen (Import/Loeschen) sowie die zentrale Kopierlogik
    // STAMM -> Projekt (Ganglinie-Kopf + Daten). Analog zu StromganglinieStammCtrl.
    class SolarganglinieStammCtrl
    {
        public const string HEAD_STAMM = "Tab_Solarganglinie_STAMM";
        public const string DATA_STAMM = "Tab_SolarganglinieDaten_STAMM";
        public const string HEAD_PROJ  = "Tab_Solarganglinie";
        public const string DATA_PROJ  = "Tab_SolarganglinieDaten";

        private List<SolarganglinieModel> _internalList = new List<SolarganglinieModel>();
        public int rows => _internalList.Count;
        public List<SolarganglinieModel> items => _internalList;

        // Liest alle Stamm-Ganglinien (Kopfdaten) in die Liste.
        public void ReadAll()
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + HEAD_STAMM + " ORDER BY Bezeichner", null);
            _internalList.Clear();
            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                SolarganglinieModel item = new SolarganglinieModel();
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                {
                    item.ID = Convert.ToInt32(row["ID"]);
                    item.m_ID_Ganglinie = item.ID;
                }
                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["Bezeichner"].ToString();
                if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                    item.m_szBeschreibung = row["Beschreibung"].ToString();
                _internalList.Add(item);
            }
        }

        public bool IsReadOnly(string szName)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM " + HEAD_STAMM + " WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szName ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        public int GetStammId(string szName)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + HEAD_STAMM + " WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szName ?? ""));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Loescht eine Stamm-Ganglinie samt Daten, sofern nicht schreibgeschuetzt.
        public bool Delete(string szName)
        {
            if (IsReadOnly(szName))
            {
                MessageBox.Show("Diese Solarganglinie ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.", "Hinweis");
                return false;
            }
            int id = GetStammId(szName);
            if (id <= 0) return false;

            DataRepository.ExecuteSQL("DELETE FROM " + DATA_STAMM + " WHERE ID_Ganglinie = ?",
                new OleDbParameter("@id", id));
            return DataRepository.ExecuteSQL("DELETE FROM " + HEAD_STAMM + " WHERE ID = ?",
                new OleDbParameter("@id", id));
        }

        // Import einer neuen Ganglinie in die STAMM-Tabellen (Admin-Dialog "Einlesen").
        // Kopf-ID und Daten-IDs explizit (MAX+1), ReadOnly = false. Alles in einer Transaktion.
        public bool ImportGanglinie(string szBezeichner, string szBeschreibung, List<string> roheWerte)
        {
            if (roheWerte == null || roheWerte.Count == 0) return false;

            var (conn, trans) = DataRepository.BeginTransaction();
            try
            {
                int neueId = 1;
                using (OleDbCommand c = new OleDbCommand("SELECT MAX(ID) FROM " + HEAD_STAMM, conn, trans))
                {
                    object m = c.ExecuteScalar();
                    neueId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                }

                using (OleDbCommand c = new OleDbCommand(
                    "INSERT INTO " + HEAD_STAMM + " (ID, Bezeichner, Beschreibung, ReadOnly) VALUES (?, ?, ?, ?)", conn, trans))
                {
                    c.Parameters.Add("@id", OleDbType.Integer).Value = neueId;
                    c.Parameters.Add("@bez", OleDbType.VarWChar).Value = szBezeichner ?? (object)DBNull.Value;
                    c.Parameters.Add("@beschr", OleDbType.VarWChar).Value = szBeschreibung ?? (object)DBNull.Value;
                    c.Parameters.Add("@ro", OleDbType.Boolean).Value = false;
                    c.ExecuteNonQuery();
                }

                int datenId = 1;
                using (OleDbCommand c = new OleDbCommand("SELECT MAX(ID) FROM " + DATA_STAMM, conn, trans))
                {
                    object m = c.ExecuteScalar();
                    datenId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                }

                using (OleDbCommand c = new OleDbCommand(
                    "INSERT INTO " + DATA_STAMM + " (ID, ID_Ganglinie, Wert, ReadOnly) VALUES (?, ?, ?, ?)", conn, trans))
                {
                    var pId = c.Parameters.Add("@id", OleDbType.Integer);
                    var pG = c.Parameters.Add("@g", OleDbType.Integer);
                    var pW = c.Parameters.Add("@w", OleDbType.Double);
                    var pR = c.Parameters.Add("@r", OleDbType.Boolean);
                    foreach (string s in roheWerte)
                    {
                        pId.Value = datenId++;
                        pG.Value = neueId;
                        pW.Value = double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
                        pR.Value = false;
                        c.ExecuteNonQuery();
                    }
                }

                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                MessageBox.Show("Fehler beim Speichern der Ganglinie (Stammdaten): " + ex.Message);
                return false;
            }
            finally { try { conn.Close(); } catch { } }
        }

        // Projekt-Ganglinie-ID (Tab_Solarganglinie.ID) zu einem Bezeichner im Projekt, oder 0.
        public static int GetProjektGanglinieId(string szName, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + HEAD_PROJ + " WHERE Bezeichner = ? AND ID_Projekt = ?",
                new OleDbParameter("@bez", szName ?? ""),
                new OleDbParameter("@proj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Zentrale Anwendung (per Bezeichner): liefert die Projekt-Ganglinie-ID; kopiert bei Bedarf die
        // Stamm-Ganglinie (+ Daten) ins Projekt. Rueckgabe: Projekt-Ganglinie-ID, 0 bei Fehler.
        public static int ApplyGanglinieToProjekt(string szBezeichner, int idProjekt)
        {
            if (string.IsNullOrEmpty(szBezeichner) || idProjekt <= 0) return 0;

            int existing = GetProjektGanglinieId(szBezeichner, idProjekt);
            if (existing > 0) return existing;

            var (conn, trans) = DataRepository.BeginTransaction();
            try
            {
                int neu = CopyGanglinieToProjekt(szBezeichner, idProjekt, conn, trans);
                if (neu > 0) trans.Commit(); else trans.Rollback();
                return neu;
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                MessageBox.Show("Fehler beim Kopieren der Solarganglinie ins Projekt: " + ex.Message);
                return 0;
            }
            finally { try { conn.Close(); } catch { } }
        }

        // Kopiert eine Stamm-Ganglinie (per Bezeichner) samt Daten in die Projekt-Tabellen.
        // Kopf-ID und Daten-IDs im Projekt explizit (MAX+1); ID_Ganglinie = neue Kopf-ID.
        // Die Daten werden in Stamm-Reihenfolge (nach ID) kopiert, damit die Zeitreihe erhalten bleibt.
        private static int CopyGanglinieToProjekt(string szBezeichner, int idProjekt, OleDbConnection conn, OleDbTransaction trans)
        {
            int stammId;
            string beschreibung;
            using (OleDbCommand c = new OleDbCommand(
                "SELECT ID, Beschreibung FROM " + HEAD_STAMM + " WHERE Bezeichner = ?", conn, trans))
            {
                c.Parameters.Add("@bez", OleDbType.VarWChar).Value = szBezeichner ?? (object)DBNull.Value;
                using (OleDbDataReader r = c.ExecuteReader())
                {
                    if (!r.Read()) return 0;
                    stammId = Convert.ToInt32(r["ID"]);
                    beschreibung = r["Beschreibung"] != DBNull.Value ? r["Beschreibung"].ToString() : "";
                }
            }

            // Neue Projekt-Kopf-ID
            int neueId;
            using (OleDbCommand c = new OleDbCommand("SELECT MAX(ID) FROM " + HEAD_PROJ, conn, trans))
            {
                object m = c.ExecuteScalar();
                neueId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
            }

            using (OleDbCommand c = new OleDbCommand(
                "INSERT INTO " + HEAD_PROJ + " (ID, ID_Projekt, Bezeichner, Beschreibung) VALUES (?, ?, ?, ?)", conn, trans))
            {
                c.Parameters.Add("@id", OleDbType.Integer).Value = neueId;
                c.Parameters.Add("@proj", OleDbType.Integer).Value = idProjekt;
                c.Parameters.Add("@bez", OleDbType.VarWChar).Value = szBezeichner ?? (object)DBNull.Value;
                c.Parameters.Add("@beschr", OleDbType.VarWChar).Value = (object)(beschreibung ?? "");
                c.ExecuteNonQuery();
            }

            // Daten der Stamm-Ganglinie einlesen (in Reihenfolge) ...
            List<double> werte = new List<double>();
            using (OleDbCommand c = new OleDbCommand(
                "SELECT Wert FROM " + DATA_STAMM + " WHERE ID_Ganglinie = ? ORDER BY ID", conn, trans))
            {
                c.Parameters.Add("@g", OleDbType.Integer).Value = stammId;
                using (OleDbDataReader r = c.ExecuteReader())
                    while (r.Read())
                        werte.Add(r["Wert"] != DBNull.Value ? Convert.ToDouble(r["Wert"]) : 0);
            }

            // Naechste freie Daten-ID im Projekt
            int datenId;
            using (OleDbCommand c = new OleDbCommand("SELECT MAX(ID) FROM " + DATA_PROJ, conn, trans))
            {
                object m = c.ExecuteScalar();
                datenId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
            }

            // ... und in die Projekt-Datentabelle schreiben (explizite IDs, Reihenfolge = Einfuegereihenfolge).
            using (OleDbCommand c = new OleDbCommand(
                "INSERT INTO " + DATA_PROJ + " (ID, ID_Ganglinie, Wert) VALUES (?, ?, ?)", conn, trans))
            {
                var pId = c.Parameters.Add("@id", OleDbType.Integer);
                var pG = c.Parameters.Add("@g", OleDbType.Integer);
                var pW = c.Parameters.Add("@w", OleDbType.Double);
                foreach (double w in werte)
                {
                    pId.Value = datenId++;
                    pG.Value = neueId;
                    pW.Value = w;
                    c.ExecuteNonQuery();
                }
            }

            return neueId;
        }
    }
}
