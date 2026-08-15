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
                sql = "SELECT * FROM Tab_Solarkollektoren ORDER BY Bezeichner";
                dt = DataRepository.GetDataTable(sql);
            }
            else
            {
                // Hinweis: Falls szFilter dynamische Werte enthält, sollte idealerweise auch dieser 
                // parametrisiert werden. Für den 1:1 Umbau belassen wir es bei der bestehenden Logik.
                sql = "SELECT * FROM Tab_Solarkollektoren WHERE " + szFilter + " ORDER BY Bezeichner";
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

                    if (row.Table.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.m_ID = Convert.ToInt32(row["ID"]);
                    if (row.Table.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) item.m_szKollektorname = row["Bezeichner"].ToString();
                    if (row.Table.Columns.Contains("Firma") && row["Firma"] != DBNull.Value) item.m_szFirma = row["Firma"].ToString();
                    if (row.Table.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) item.m_szBeschreibung = row["Beschreibung"].ToString();
                    if (row.Table.Columns.Contains("Kollektortyp") && row["Kollektortyp"] != DBNull.Value) item.m_szKollektortyp = row["Kollektortyp"].ToString();
                    if (row.Table.Columns.Contains("Modulflaeche") && row["Modulflaeche"] != DBNull.Value) item.m_Modulfläche = Convert.ToDouble(row["Modulflaeche"]);
                    if (row.Table.Columns.Contains("Aperturflaeche") && row["Aperturflaeche"] != DBNull.Value) item.m_Aperturfläche = Convert.ToDouble(row["Aperturflaeche"]);
                    if (row.Table.Columns.Contains("h0") && row["h0"] != DBNull.Value) item.m_h0 = Convert.ToDouble(row["h0"]);
                    if (row.Table.Columns.Contains("k1") && row["k1"] != DBNull.Value) item.m_k1 = Convert.ToDouble(row["k1"]);
                    if (row.Table.Columns.Contains("k2") && row["k2"] != DBNull.Value) item.m_k2 = Convert.ToDouble(row["k2"]);
                    if (row.Table.Columns.Contains("Kdir") && row["Kdir"] != DBNull.Value) item.m_Kdir = Convert.ToDouble(row["Kdir"]);
                    if (row.Table.Columns.Contains("Kdfu") && row["Kdfu"] != DBNull.Value) item.m_Kdfu = Convert.ToDouble(row["Kdfu"]);
                    if (row.Table.Columns.Contains("Investitionskosten") && row["Investitionskosten"] != DBNull.Value) item.m_Kosten = Convert.ToDouble(row["Investitionskosten"]);
                    if (row.Table.Columns.Contains("Vorlauf") && row["Vorlauf"] != DBNull.Value) item.m_Vorlauf = Convert.ToInt32(row["Vorlauf"]);
                    if (row.Table.Columns.Contains("Ruecklauf") && row["Ruecklauf"] != DBNull.Value) item.m_Ruecklauf = Convert.ToInt32(row["Ruecklauf"]);

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

                if (row.Table.Columns.Contains("ID") && row["ID"] != DBNull.Value) m_ID = Convert.ToInt32(row["ID"]);
                if (row.Table.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) m_szKollektorname = row["Bezeichner"].ToString();
                if (row.Table.Columns.Contains("Firma") && row["Firma"] != DBNull.Value) m_szFirma = row["Firma"].ToString();
                if (row.Table.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) m_szBeschreibung = row["Beschreibung"].ToString();
                if (row.Table.Columns.Contains("Kollektortyp") && row["Kollektortyp"] != DBNull.Value) m_szKollektortyp = row["Kollektortyp"].ToString();
                if (row.Table.Columns.Contains("Modulflaeche") && row["Modulflaeche"] != DBNull.Value) m_Modulfläche = Convert.ToDouble(row["Modulflaeche"]);
                if (row.Table.Columns.Contains("Aperturflaeche") && row["Aperturflaeche"] != DBNull.Value) m_Aperturfläche = Convert.ToDouble(row["Aperturflaeche"]);
                if (row.Table.Columns.Contains("h0") && row["h0"] != DBNull.Value) m_h0 = Convert.ToDouble(row["h0"]);
                if (row.Table.Columns.Contains("k1") && row["k1"] != DBNull.Value) m_k1 = Convert.ToDouble(row["k1"]);
                if (row.Table.Columns.Contains("k2") && row["k2"] != DBNull.Value) m_k2 = Convert.ToDouble(row["k2"]);
                if (row.Table.Columns.Contains("Kdir") && row["Kdir"] != DBNull.Value) m_Kdir = Convert.ToDouble(row["Kdir"]);
                if (row.Table.Columns.Contains("Kdfu") && row["Kdfu"] != DBNull.Value) m_Kdfu = Convert.ToDouble(row["Kdfu"]);
                if (row.Table.Columns.Contains("Investitionskosten") && row["Investitionskosten"] != DBNull.Value) m_Kosten = Convert.ToDouble(row["Investitionskosten"]);
                if (row.Table.Columns.Contains("Vorlauf") && row["Vorlauf"] != DBNull.Value) m_Vorlauf = Convert.ToInt32(row["Vorlauf"]);
                if (row.Table.Columns.Contains("Ruecklauf") && row["Ruecklauf"] != DBNull.Value) m_Ruecklauf = Convert.ToInt32(row["Ruecklauf"]);

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
                                WHERE Bezeichner = ?";

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

        // --- STAMM -> PROJEKT KOPIE (analog HeizkesselCtrl/PhotovoltaikCtrl) ---

        // Liefert die Projekt-ID (Tab_Solarkollektoren.ID) eines Bezeichners im Projekt, oder 0.
        public int GetProjektId(string szBezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Solarkollektoren WHERE Bezeichner = ? AND ID_Projekt = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""),
                new OleDbParameter("@idProj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        public bool ExistsInProjekt(string szBezeichner, int idProjekt)
        {
            return GetProjektId(szBezeichner, idProjekt) > 0;
        }

        // Kopiert einen Stammdatensatz (Tab_Solarkollektoren_STAMM) in die Projekt-Tabelle
        // (Tab_Solarkollektoren), sofern er fuer das Projekt noch nicht existiert. Setzt ID_Projekt
        // und vergibt eine neue Projekt-ID. Rueckgabe: Projekt-ID (Tab_Solarkollektoren.ID) des
        // kopierten ODER vorhandenen Datensatzes, -1 bei Fehler. Dies ist der Wert, den
        // WErzeugerModel.ID_Solar tragen muss (Beziehung -> Projekt-Tabelle).
        public int CopyFromStamm(int stammId, int idProjekt)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + SolarkollektorenStammCtrl.TABLE + "] WHERE ID = ?",
                    new OleDbParameter("@id", stammId));

                if (dt == null || dt.Rows.Count == 0)
                {
                    // NACHARBEIT PAKET 8, BEFUND N10: gemeinsame Entscheidungsstelle
                    // (Dialog in der Bedienung, Protokolleintrag im Rechenlauf) - wie in
                    // den vier baugleichen Geschwistern.
                    DataRepository.FehlerMelden("Der Solarkollektor-Stammdatensatz wurde nicht gefunden (ID " + stammId + ").");
                    return -1;
                }

                DataRow s = dt.Rows[0];
                string szBezeichner = s["Bezeichner"].ToString();

                int vorhandeneId = GetProjektId(szBezeichner, idProjekt);
                if (vorhandeneId > 0) return vorhandeneId;

                int neueId = DataRepository.GetMaxID("Tab_Solarkollektoren") + 1;

                string rueckColSrc = s.Table.Columns.Contains("Ruecklauf") ? "Ruecklauf" : "Rücklauf";

                // ReadOnly wird NICHT uebernommen (existiert in der Projekt-Tabelle nicht).
                string sql = @"INSERT INTO Tab_Solarkollektoren
                    (ID, ID_Projekt, Bezeichner, Firma, Beschreibung, Kollektortyp, Modulflaeche, Aperturflaeche,
                     h0, k1, k2, Kdir, Kdfu, Investitionskosten, Vorlauf, Ruecklauf)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", neueId),
                    new OleDbParameter("@idProj", idProjekt),
                    P("@bez", s["Bezeichner"]),
                    P("@fir", ColOrNull(s, "Firma")),
                    P("@bes", ColOrNull(s, "Beschreibung")),
                    P("@typ", ColOrNull(s, "Kollektortyp")),
                    P("@mfl", ColOrNull(s, "Modulflaeche")),
                    P("@afl", ColOrNull(s, "Aperturflaeche")),
                    P("@h0", ColOrNull(s, "h0")),
                    P("@k1", ColOrNull(s, "k1")),
                    P("@k2", ColOrNull(s, "k2")),
                    P("@kdir", ColOrNull(s, "Kdir")),
                    P("@kdfu", ColOrNull(s, "Kdfu")),
                    P("@inv", ColOrNull(s, "Investitionskosten")),
                    P("@vor", ColOrNull(s, "Vorlauf")),
                    P("@rue", ColOrNull(s, rueckColSrc))
                };

                bool ok = DataRepository.ExecuteSQL(sql, ps);
                return ok ? neueId : -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Kopieren des Solarkollektors aus den Stammdaten: " + ex.Message);
                return -1;
            }
        }

        public int CopyFromStamm(string szBezeichner, int idProjekt)
        {
            int stammId = DataRepository.GetIdByName(SolarkollektorenStammCtrl.TABLE, "Bezeichner", szBezeichner);
            if (stammId <= 0) return -1;
            return CopyFromStamm(stammId, idProjekt);
        }

        public bool DeleteFromProjekt(string szBezeichner, int idProjekt)
        {
            string sql = "DELETE FROM Tab_Solarkollektoren WHERE Bezeichner = ? AND ID_Projekt = ?";
            return DataRepository.ExecuteSQL(sql,
                new OleDbParameter("@bez", szBezeichner ?? ""),
                new OleDbParameter("@idProj", idProjekt));
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
