using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    public class StromspeicherCtrl : StromspeicherModel
    {
        private List<StromspeicherModel> _internalList = new List<StromspeicherModel>();
        public int rows => _internalList.Count;
        public List<StromspeicherModel> items => _internalList;

        public void ReadAll()
        {
            string sql = "SELECT * FROM Tab_Stromspeicher ORDER BY Bezeichner";
            DataTable dt = DataRepository.GetDataTable(sql, null);

            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                StromspeicherModel item = new StromspeicherModel();

                // Namensbasiertes und typsicheres Auslesen der Spalten
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    item.m_ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["Bezeichner"].ToString();

                if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value)
                    item.m_szTyp = row["Typ"].ToString();

                if (dt.Columns.Contains("Leistung") && row["Leistung"] != DBNull.Value)
                    item.m_Leistung = Convert.ToDouble(row["Leistung"]);

                if (dt.Columns.Contains("Energie") && row["Energie"] != DBNull.Value)
                    item.m_Energie = Convert.ToDouble(row["Energie"]);

                if (dt.Columns.Contains("Degradation") && row["Degradation"] != DBNull.Value)
                    item.m_Degradation = Convert.ToDouble(row["Degradation"]);

                if (dt.Columns.Contains("Ladezustand") && row["Ladezustand"] != DBNull.Value)
                    item.m_Ladezustand = Convert.ToDouble(row["Ladezustand"]);

                if (dt.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value)
                    item.m_Modulkosten = Convert.ToDouble(row["Modulkosten"]);

                _internalList.Add(item);
            }
        }

        public void ReadSingle(int ID)
        {
            string sql = "SELECT * FROM Tab_Stromspeicher WHERE ID = ?";

            OleDbParameter paramId = new OleDbParameter("@id", OleDbType.Integer);
            paramId.Value = ID;
            OleDbParameter[] ps = { paramId };

            DataTable dt = DataRepository.GetDataTable(sql, ps);

            // Instanzdaten vorsorglich zurücksetzen
            m_ID = 0;
            m_szBezeichner = string.Empty;
            m_szTyp = string.Empty;
            m_Leistung = 0;
            m_Energie = 0;
            m_Degradation = 0;
            m_Ladezustand = 0;
            m_Modulkosten = 0;

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    m_ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    m_szBezeichner = row["Bezeichner"].ToString();

                if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value)
                    m_szTyp = row["Typ"].ToString();

                if (dt.Columns.Contains("Leistung") && row["Leistung"] != DBNull.Value)
                    m_Leistung = Convert.ToDouble(row["Leistung"]);

                if (dt.Columns.Contains("Energie") && row["Energie"] != DBNull.Value)
                    m_Energie = Convert.ToDouble(row["Energie"]);

                if (dt.Columns.Contains("Degradation") && row["Degradation"] != DBNull.Value)
                    m_Degradation = Convert.ToDouble(row["Degradation"]);

                if (dt.Columns.Contains("Ladezustand") && row["Ladezustand"] != DBNull.Value)
                    m_Ladezustand = Convert.ToDouble(row["Ladezustand"]);

                if (dt.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value)
                    m_Modulkosten = Convert.ToDouble(row["Modulkosten"]);

                // UI-Kompatibilität wahren
                _internalList.Clear();
                _internalList.Add(this);
            }
            else
            {
                _internalList.Clear();
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            string sql = "SELECT * FROM Tab_Stromspeicher WHERE Bezeichner = ?";

            OleDbParameter paramBez = new OleDbParameter("@bez", OleDbType.VarWChar);
            paramBez.Value = szBezeichner ?? (object)DBNull.Value;
            OleDbParameter[] ps = { paramBez };

            DataTable dt = DataRepository.GetDataTable(sql, ps);

            // Instanzdaten vorsorglich zurücksetzen
            m_ID = 0;
            m_szBezeichner = string.Empty;
            m_szTyp = string.Empty;
            m_Leistung = 0;
            m_Energie = 0;
            m_Degradation = 0;
            m_Ladezustand = 0;
            m_Modulkosten = 0;

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    m_ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    m_szBezeichner = row["Bezeichner"].ToString();

                if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value)
                    m_szTyp = row["Typ"].ToString();

                if (dt.Columns.Contains("Leistung") && row["Leistung"] != DBNull.Value)
                    m_Leistung = Convert.ToDouble(row["Leistung"]);

                if (dt.Columns.Contains("Energie") && row["Energie"] != DBNull.Value)
                    m_Energie = Convert.ToDouble(row["Energie"]);

                if (dt.Columns.Contains("Degradation") && row["Degradation"] != DBNull.Value)
                    m_Degradation = Convert.ToDouble(row["Degradation"]);

                if (dt.Columns.Contains("Ladezustand") && row["Ladezustand"] != DBNull.Value)
                    m_Ladezustand = Convert.ToDouble(row["Ladezustand"]);

                if (dt.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value)
                    m_Modulkosten = Convert.ToDouble(row["Modulkosten"]);

                // UI-Kompatibilität wahren
                _internalList.Clear();
                _internalList.Add(this);
            }
            else
            {
                _internalList.Clear();
            }
        }

        // --- STAMM -> PROJEKT KOPIE (analog HeizkesselCtrl/BHKWCtrl) ---

        // Liefert die Projekt-ID (Tab_Stromspeicher.ID) eines Bezeichners im Projekt, oder 0.
        public int GetProjektId(string szBezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Stromspeicher WHERE Bezeichner = ? AND ID_Projekt = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""),
                new OleDbParameter("@idProj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        public bool ExistsInProjekt(string szBezeichner, int idProjekt)
        {
            return GetProjektId(szBezeichner, idProjekt) > 0;
        }

        // Kopiert einen Stammdatensatz (Tab_Stromspeicher_STAMM) in die Projekt-Tabelle
        // (Tab_Stromspeicher), sofern er fuer das Projekt noch nicht existiert. Setzt ID_Projekt
        // und vergibt eine neue Projekt-ID. Rueckgabe: Projekt-ID (Tab_Stromspeicher.ID) des
        // kopierten ODER bereits vorhandenen Datensatzes, -1 bei Fehler. Dies ist der Wert, den
        // WErzeugerModel.ID_SP tragen muss (Beziehung verweist auf die Projekt-Tabelle).
        public int CopyFromStamm(int stammId, int idProjekt)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + StromspeicherStammCtrl.TABLE + "] WHERE ID = ?",
                    new OleDbParameter("@id", stammId));

                if (dt == null || dt.Rows.Count == 0)
                {
                    // NACHARBEIT PAKET 8, BEFUND N10: gemeinsame Entscheidungsstelle
                    // (Dialog in der Bedienung, Protokolleintrag im Rechenlauf) - wie in
                    // den vier baugleichen Geschwistern.
                    DataRepository.FehlerMelden("Der Stromspeicher-Stammdatensatz wurde nicht gefunden (ID " + stammId + ").");
                    return -1;
                }

                DataRow s = dt.Rows[0];
                string szBezeichner = s["Bezeichner"].ToString();

                int vorhandeneId = GetProjektId(szBezeichner, idProjekt);
                if (vorhandeneId > 0) return vorhandeneId;

                int neueId = DataRepository.GetMaxID("Tab_Stromspeicher") + 1;

                // ReadOnly wird NICHT uebernommen (existiert in der Projekt-Tabelle nicht).
                string sql = @"INSERT INTO Tab_Stromspeicher
                    (ID, ID_Projekt, Bezeichner, Typ, Leistung, Energie, Degradation, Ladezustand, Modulkosten)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", neueId),
                    new OleDbParameter("@idProj", idProjekt),
                    P("@bez", s["Bezeichner"]),
                    P("@typ", ColOrNull(s, "Typ")),
                    P("@lei", ColOrNull(s, "Leistung")),
                    P("@ene", ColOrNull(s, "Energie")),
                    P("@deg", ColOrNull(s, "Degradation")),
                    P("@lad", ColOrNull(s, "Ladezustand")),
                    P("@mod", ColOrNull(s, "Modulkosten"))
                };

                bool ok = DataRepository.ExecuteSQL(sql, ps);
                return ok ? neueId : -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Kopieren des Stromspeichers aus den Stammdaten: " + ex.Message);
                return -1;
            }
        }

        public int CopyFromStamm(string szBezeichner, int idProjekt)
        {
            int stammId = DataRepository.GetIdByName(StromspeicherStammCtrl.TABLE, "Bezeichner", szBezeichner);
            if (stammId <= 0) return -1;
            return CopyFromStamm(stammId, idProjekt);
        }

        public bool DeleteFromProjekt(string szBezeichner, int idProjekt)
        {
            string sql = "DELETE FROM Tab_Stromspeicher WHERE Bezeichner = ? AND ID_Projekt = ?";
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
