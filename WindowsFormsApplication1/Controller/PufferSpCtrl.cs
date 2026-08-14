using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class PufferSpCtrl : PufferSpModel
    {
        public const string TABLE = "Tab_Pufferspeicher";

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

                    DataRow r = dt.Rows[i];
                    if (r.Table.Columns.Contains("ID") && r["ID"] != DBNull.Value) item.ID = Convert.ToInt32(r["ID"]);
                    if (r.Table.Columns.Contains("Bezeichner") && r["Bezeichner"] != DBNull.Value) item.Name = r["Bezeichner"].ToString();
                    if (r.Table.Columns.Contains("Hersteller") && r["Hersteller"] != DBNull.Value) item.Firma = r["Hersteller"].ToString();
                    if (r.Table.Columns.Contains("Speichertyp") && r["Speichertyp"] != DBNull.Value) item.Speichertyp = r["Speichertyp"].ToString();
                    if (r.Table.Columns.Contains("Bereitschaftsverluste") && r["Bereitschaftsverluste"] != DBNull.Value) item.Betriebsbereitschaftverlust = Convert.ToDouble(r["Bereitschaftsverluste"]);
                    if (r.Table.Columns.Contains("Gesamtvolumen") && r["Gesamtvolumen"] != DBNull.Value) item.Gesamtvolumen = Convert.ToInt32(r["Gesamtvolumen"]);
                    if (r.Table.Columns.Contains("Investitionskosten") && r["Investitionskosten"] != DBNull.Value) item.Investitionskosten = Convert.ToDouble(r["Investitionskosten"]);

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

        // --- STAMM -> PROJEKT KOPIE (analog HeizkesselCtrl/BHKWCtrl) ---

        // Liefert die Projekt-ID (Tab_Pufferspeicher.ID) eines Bezeichners im Projekt, oder 0.
        public int GetProjektId(string szBezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Pufferspeicher WHERE Bezeichner = ? AND ID_Projekt = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""),
                new OleDbParameter("@idProj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        public bool ExistsInProjekt(string szBezeichner, int idProjekt)
        {
            return GetProjektId(szBezeichner, idProjekt) > 0;
        }

        // Kopiert einen Stammdatensatz (Tab_Pufferspeicher_STAMM) in die Projekt-Tabelle
        // (Tab_Pufferspeicher), sofern er fuer das Projekt noch nicht existiert. Setzt ID_Projekt
        // und vergibt eine neue Projekt-ID. Rueckgabe: Projekt-ID (Tab_Pufferspeicher.ID) des
        // kopierten ODER bereits vorhandenen Datensatzes, -1 bei Fehler. Dies ist der Wert, den
        // WErzeugerModel.ID_PUFFER tragen muss (Beziehung verweist auf die Projekt-Tabelle).
        public int CopyFromStamm(int stammId, int idProjekt)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + PufferSpStammCtrl.TABLE + "] WHERE ID = ?",
                    new OleDbParameter("@id", stammId));

                if (dt == null || dt.Rows.Count == 0)
                {
                    System.Windows.Forms.MessageBox.Show("Der Pufferspeicher-Stammdatensatz wurde nicht gefunden (ID " + stammId + ").");
                    return -1;
                }

                DataRow s = dt.Rows[0];
                string szBezeichner = s["Bezeichner"].ToString();

                int vorhandeneId = GetProjektId(szBezeichner, idProjekt);
                if (vorhandeneId > 0) return vorhandeneId;

                int neueId = DataRepository.GetMaxID("Tab_Pufferspeicher") + 1;

                // ReadOnly wird NICHT uebernommen (existiert in der Projekt-Tabelle nicht).
                string sql = @"INSERT INTO Tab_Pufferspeicher
                    (ID, ID_Projekt, Bezeichner, Hersteller, Speichertyp, Bereitschaftsverluste, Gesamtvolumen, Investitionskosten)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", neueId),
                    new OleDbParameter("@idProj", idProjekt),
                    P("@bez", s["Bezeichner"]),
                    P("@her", ColOrNull(s, "Hersteller")),
                    P("@typ", ColOrNull(s, "Speichertyp")),
                    P("@ver", ColOrNull(s, "Bereitschaftsverluste")),
                    P("@vol", ColOrNull(s, "Gesamtvolumen")),
                    P("@inv", ColOrNull(s, "Investitionskosten"))
                };

                bool ok = DataRepository.ExecuteSQL(sql, ps);
                return ok ? neueId : -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Kopieren des Pufferspeichers aus den Stammdaten: " + ex.Message);
                return -1;
            }
        }

        public int CopyFromStamm(string szBezeichner, int idProjekt)
        {
            int stammId = DataRepository.GetIdByName(PufferSpStammCtrl.TABLE, "Bezeichner", szBezeichner);
            if (stammId <= 0) return -1;
            return CopyFromStamm(stammId, idProjekt);
        }

        public bool DeleteFromProjekt(string szBezeichner, int idProjekt)
        {
            string sql = "DELETE FROM Tab_Pufferspeicher WHERE Bezeichner = ? AND ID_Projekt = ?";
            return DataRepository.ExecuteSQL(sql,
                new OleDbParameter("@bez", szBezeichner ?? ""),
                new OleDbParameter("@idProj", idProjekt));
        }

        /// <summary>
        /// B0-6a: Entfernt Projektkopien in Tab_Pufferspeicher, zu denen keine
        /// Puffer-Anlage (ID_Type = 12) mehr im Projekt existiert. Nach jedem
        /// Löschpfad der Puffer-Anlagen aufzurufen (Kontextmenü-Löschen, Dialog
        /// Hinzufügen/Bearbeiten, Startseite). Die Zuordnungen in Z_ProjektPufferSp
        /// räumt die Löschweitergabe (FK auf Tab_Pufferspeicher.ID) mit ab.
        /// Die fehlende Projekt-Kaskade der Tabelle selbst (B0-6b) ist eine
        /// Schemaänderung und zurückgestellt.
        /// </summary>
        public bool ProjektWaisenEntfernen(int idProjekt)
        {
            string sql = @"DELETE FROM Tab_Pufferspeicher
                           WHERE ID_Projekt = ?
                             AND Bezeichner NOT IN (SELECT Bezeichner FROM Tab_Energieanlagen
                                                    WHERE ID_Projekt = ? AND ID_Type = " +
                                                    WizardItemClass.PUFFER_TYP + ")";
            return DataRepository.ExecuteSQL(sql,
                new OleDbParameter("@idProj", idProjekt),
                new OleDbParameter("@idProj2", idProjekt));
        }

        private static OleDbParameter P(string name, object value)
        {
            return new OleDbParameter(name, value ?? DBNull.Value);
        }

        private static object ColOrNull(DataRow row, string col)
        {
            return row.Table.Columns.Contains(col) ? row[col] : DBNull.Value;
        }
    }
}
