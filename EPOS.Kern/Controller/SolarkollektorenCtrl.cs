using System;
using System.Data;

namespace WindowsFormsApplication1
{
    // =====================================================================================
    // ARBEITSPAKET iU6-T2: DER TOTE OleDb-ZWEIG IST GESTRICHEN.
    //
    // Bis hierher trug diese Klasse ein eigenes "public OleDbCommand DBCommand" samt
    // lazy angelegtem Feld, Finalizer und einer Update()-Methode, die dieses Kommando
    // fuellte und "DBCommand.ExecuteNonQuery()" aufrief. Das war seit der
    // SQLite-Umstellung (6486c36) TOTER CODE:
    //
    //   - Update() hatte 0 Aufrufer. Alle fuenf Instanzen der Klasse lesen nur oder
    //     kopieren: SimulationSolarthermie.cs:230 (ReadSingle), FormMain.cs:1239
    //     (ReadSingle), WizardCtrl.cs:1006 (CopyFromStamm), Form_SolarKollektoren.cs:233
    //     (CopyFromStamm) und :271 (DeleteFromProjekt).
    //   - Das DBCommand bekam nie eine Verbindung; niemand griff von aussen darauf zu.
    //     Auf Windows waere ExecuteNonQuery() also in die InvalidOperationException und
    //     damit in den catch-Zweig gelaufen - stilles "return false".
    //
    // Geschrieben wird der Solarkollektorkatalog ueber SolarkollektorenStammCtrl
    // (Form_SolarKollektoren, Form_SolarKollektorenAdmin); der ist OleDb-frei und
    // laeuft ueber DataRepository/DbParam. Ein Ersatz fuer Update() war deshalb nicht
    // noetig - er haette eine Schreibmoeglichkeit vorgetaeuscht, die es hier nie gab.
    // =====================================================================================

    class SolarkollektorenCtrl : SolarkollektorenModel
    {
        public SolarkollektorenModel model = new SolarkollektorenModel();

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
            DbParam parameter = new DbParam("?", ID);
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

        // --- STAMM -> PROJEKT KOPIE (analog HeizkesselCtrl/PhotovoltaikCtrl) ---

        // Liefert die Projekt-ID (Tab_Solarkollektoren.ID) eines Bezeichners im Projekt, oder 0.
        public int GetProjektId(string szBezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Solarkollektoren WHERE Bezeichner = ? AND ID_Projekt = ?",
                new DbParam("@bez", szBezeichner ?? ""),
                new DbParam("@idProj", idProjekt));
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
                    new DbParam("@id", stammId));

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

                DbParam[] ps = {
                    new DbParam("@id", neueId),
                    new DbParam("@idProj", idProjekt),
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
                new DbParam("@bez", szBezeichner ?? ""),
                new DbParam("@idProj", idProjekt));
        }

        private static DbParam P(string name, object value)
        {
            return new DbParam(name, value ?? DBNull.Value);
        }

        private static object ColOrNull(DataRow row, string col)
        {
            return row.Table.Columns.Contains(col) ? row[col] : DBNull.Value;
        }
    }
}
