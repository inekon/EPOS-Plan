using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class ProjektCtrl : ProjektModel
    {
        // --- Kompatibilitäts-Layer für bestehenden UI-Code ---
        private List<ProjektModel> _internalList = new List<ProjektModel>();
        private bool _hasSingleData = false;

        // Simuliert die alte 'rows' Variable
        public new int rows => _internalList.Count > 0 ? _internalList.Count : (_hasSingleData ? 1 : 0);

        // Simuliert das alte 'items' Array (als Liste, die sich wie ein Array verhält)
        public List<ProjektModel> items => _internalList;

        public ProjektCtrl()
        {
            _hasSingleData = false;
        }

        #region --- DATABASE OPERATIONS ---

        public int GetMaxID() => DataRepository.GetMaxID("Tab_Projekt", "ID");

        public bool Insert()
        {
            m_ID = GetMaxID() + 1;

            string sql = @"INSERT INTO Tab_Projekt 
                           (Projektname, Bearbeiter, Beschreibung, Kunde, Aenderungsdatum, ID_Klimaregion, Erstelldatum) 
                           VALUES (?, ?, ?, ?, ?, ?, ?)";

            OleDbParameter[] ps = {
                new OleDbParameter("@name", m_szProjektname ?? ""),
                new OleDbParameter("@bearb", m_szBearbeiter ?? ""),
                new OleDbParameter("@besch", m_szBeschreibung ?? ""),
                new OleDbParameter("@kunde", m_szKunde ?? ""),
                new OleDbParameter("@date", OleDbType.Date) { Value = ValidateDate(m_Aenderungsdatum) },
                new OleDbParameter("@klima", m_ID_Klimaregion),
                new OleDbParameter("@edate", OleDbType.Date) { Value = ValidateDate(m_Erstelldatum) }
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Update()
        {
            string sql = @"UPDATE Tab_Projekt SET 
                            Bearbeiter=?, Beschreibung=?, Kunde=?, 
                            Aenderungsdatum=?, ID_Klimaregion=?, Erstelldatum=? 
                           WHERE Projektname=?";

            OleDbParameter[] ps = {
                new OleDbParameter("@bearb", (object)m_szBearbeiter ?? ""),
                new OleDbParameter("@besch", (object)m_szBeschreibung ?? ""),
                new OleDbParameter("@kunde", (object)m_szKunde ?? ""),
                new OleDbParameter("@date", OleDbType.Date) { Value = ValidateDate(m_Aenderungsdatum) },
                new OleDbParameter("@klima", m_ID_Klimaregion),
                new OleDbParameter("@edate", OleDbType.Date) { Value = ValidateDate(m_Erstelldatum) },
                new OleDbParameter("@pname", m_szProjektname)
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        /// <summary>
        /// Löscht das Projekt. Die Detailtabellen fallen über ihre Löschweitergaben mit
        /// weg - seit Schritt 4 der SchemaMigration auch die Puffer-Projektkopien
        /// (B0-6b: Tab_Projekt.ID -> Tab_Pufferspeicher.ID_Projekt, ON DELETE CASCADE).
        ///
        /// Vorher werden die Anlagen-Verweise auf diese Puffer gelöst. Grund: die vier
        /// Referenzen ID_PUFFER / WS_ID_Puffer / WS_ID_Puffer2 / WQ_ID_Puffer sind
        /// bewusst RESTRIKTIV angelegt (keine Löschweitergabe, sonst risse ein gelöschter
        /// Speicher die referenzierende Wärmepumpe mit). Zeigt beim Projekt-DELETE noch
        /// eine Anlage auf einen Projekt-Puffer, lehnt Access die gesamte Kaskade ab.
        ///
        /// Die Aufrufer (MenueCtrl.ProjektDelete, VariantenCtrl.LoescheVariante) löschen
        /// die Energieanlagen zwar vorher - aber die B0-6b-Kaskade soll nicht von der
        /// Aufrufreihenfolge abhängen. Deshalb steht das Lösen hier, an der einen
        /// zentralen Stelle, durch die beide Wege laufen.
        /// </summary>
        public bool Delete(string szProjekt)
        {
            PufferReferenzenLoesen(szProjekt);
            BerichtsKonfigurationEntfernen(szProjekt);

            string sql = "DELETE FROM Tab_Projekt WHERE Projektname=?";
            OleDbParameter[] ps = { new OleDbParameter("@pname", szProjekt) };
            return DataRepository.ExecuteSQL(sql, ps);
        }

        /// <summary>
        /// Entfernt die Berichtskonfigurationen aller Projekte dieses Namens VOR dem
        /// Projekt-DELETE. Die Tabelle Berichtskonfiguration hängt an keiner
        /// Löschweitergabe (Ad-hoc-DDL ohne Beziehung, BerichtCtrl) — verbliebe die
        /// Zeile, kollidierte eine spätere Projektkopie am eindeutigen Index
        /// UQ_BerichtKonfigProj, sobald die neue Projekt-ID (MAX+1) auf die verwaiste
        /// ProjektID fällt (Duplizier-Abbruch vom 21.08.2026). Still über StilleDb:
        /// Fehlt die Tabelle (Datenbank ohne Berichtsmodul), läuft das Löschen ohne
        /// Dialog weiter.
        /// </summary>
        private static void BerichtsKonfigurationEntfernen(string szProjekt)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID FROM Tab_Projekt WHERE Projektname=?",
                    new OleDbParameter("@pname", szProjekt ?? ""));

                if (dt == null) return;

                foreach (DataRow r in dt.Rows)
                    if (r[0] != DBNull.Value)
                        StilleDb.NonQuery(
                            "DELETE FROM " + BerichtCtrl.TAB_KONFIG + " WHERE ProjektID = ?",
                            StilleDb.Par("@proj", OleDbType.Integer, Convert.ToInt32(r[0])));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Berichtskonfiguration des Projekts konnte nicht entfernt werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Löst die Anlagen-Verweise auf die Pufferspeicher aller Projekte dieses Namens.
        /// Still: schlägt es fehl, soll das Löschen trotzdem versucht werden - die
        /// Beziehung meldet sich dann von selbst.
        /// </summary>
        private static void PufferReferenzenLoesen(string szProjekt)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID FROM Tab_Projekt WHERE Projektname=?",
                    new OleDbParameter("@pname", szProjekt ?? ""));

                if (dt == null) return;

                foreach (DataRow r in dt.Rows)
                    if (r[0] != DBNull.Value)
                        PufferSpCtrl.ReferenzenLoesenFuerProjekt(Convert.ToInt32(r[0]));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Puffer-Referenzen des Projekts konnten nicht gelöst werden: " + ex.Message);
            }
        }

        public void ReadAll()
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM Tab_Projekt ORDER BY Projektname");
            _internalList.Clear();
            _hasSingleData = false;

            foreach (DataRow row in dt.Rows)
            {
                _internalList.Add(MapRowToModel(row));
            }
        }

        public void ReadSingle(string projektName)
        {
            string sql = "SELECT * FROM Tab_Projekt WHERE Projektname=?";
            OleDbParameter[] ps = { new OleDbParameter("@pname", projektName) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            _internalList.Clear(); // Liste leeren, da wir nur einen Datensatz laden

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                ProjektModel model = MapRowToModel(row);

                // Daten in die aktuelle Instanz mappen
                this.m_ID = model.m_ID;
                this.m_szProjektname = model.m_szProjektname;
                this.m_szBearbeiter = model.m_szBearbeiter;
                this.m_szBeschreibung = model.m_szBeschreibung;
                this.m_szKunde = model.m_szKunde;
                this.m_Aenderungsdatum = model.m_Aenderungsdatum;
                this.m_ID_Klimaregion = model.m_ID_Klimaregion;
                this.m_Erstelldatum = model.m_Erstelldatum;

                _hasSingleData = true;
            }
            else
            {
                _hasSingleData = false;
            }
        }

        public void ReadSingle(int IDProjekt)
        {
            string sql = "SELECT * FROM Tab_Projekt WHERE ID=?";
            OleDbParameter[] ps = { new OleDbParameter("@id", IDProjekt) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            _internalList.Clear(); // Liste leeren, da wir nur einen Datensatz laden

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                ProjektModel model = MapRowToModel(row);

                // Daten in die aktuelle Instanz mappen
                this.m_ID = model.m_ID;
                this.m_szProjektname = model.m_szProjektname;
                this.m_szBearbeiter = model.m_szBearbeiter;
                this.m_szBeschreibung = model.m_szBeschreibung;
                this.m_szKunde = model.m_szKunde;
                this.m_Aenderungsdatum = model.m_Aenderungsdatum;
                this.m_ID_Klimaregion = model.m_ID_Klimaregion;
                this.m_Erstelldatum = model.m_Erstelldatum;

                _hasSingleData = true;
            }
            else
            {
                _hasSingleData = false;
            }
        }
        #endregion

        #region --- UI FILL METHODS ---

        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            foreach (var p in _internalList) ctrl.Items.Add(p.m_szProjektname);
        }

        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            foreach (var p in _internalList) ctrl.Items.Add(p.m_szProjektname);
        }

        #endregion

        #region --- HELPER METHODS ---

        private DateTime ValidateDate(DateTime date)
        {
            if (date < new DateTime(1900, 1, 1)) return DateTime.Now;
            return date;
        }

        private ProjektModel MapRowToModel(DataRow row)
        {
            return new ProjektModel
            {
                m_ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0,
                m_szProjektname = row["Projektname"].ToString(),
                m_szBearbeiter = row["Bearbeiter"]?.ToString() ?? "",
                m_szBeschreibung = row["Beschreibung"]?.ToString() ?? "",
                m_szKunde = row["Kunde"]?.ToString() ?? "",
                m_Aenderungsdatum = row["Aenderungsdatum"] != DBNull.Value ? Convert.ToDateTime(row["Aenderungsdatum"]) : DateTime.Now,
                m_ID_Klimaregion = row["ID_Klimaregion"] != DBNull.Value ? Convert.ToInt32(row["ID_Klimaregion"]) : 0,
                m_Erstelldatum = row["Erstelldatum"] != DBNull.Value ? Convert.ToDateTime(row["Erstelldatum"]) : DateTime.Now
            };
        }

        #endregion
    }
}