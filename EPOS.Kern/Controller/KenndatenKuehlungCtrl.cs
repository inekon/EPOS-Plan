using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class KenndatenKuehlungCtrl : KenndatenKuehlungModel
    {
        // --- Kompatibilitäts-Layer ---
        private List<KenndatenKuehlungModel> _internalList = new List<KenndatenKuehlungModel>();

        public int rows => _internalList.Count;
        public new List<KenndatenKuehlungModel> items => _internalList;

        public KenndatenKuehlungModel model;

        public KenndatenKuehlungCtrl()
        {
            model = new KenndatenKuehlungModel();
        }

        #region --- DATABASE READ OPERATIONS ---

        public void ReadAll(int ID_WP = 0)
        {
            string sql = "SELECT * FROM Tab_Kenndaten_Kuehlung";
            if (ID_WP > 0)
                sql += $" WHERE ID_WP = {ID_WP}";

            sql += " ORDER BY ID_WP";

            ExecuteRead(sql);
        }

        public void ReadSingle(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                // Eigenschaften des Controllers selbst setzen (für Kompatibilität)
                this.m_ID = row[0] != DBNull.Value ? Convert.ToInt32(row[0]) : 0;
                this.m_ID_WP = row[1] != DBNull.Value ? Convert.ToInt32(row[1]) : 0;
                this.m_nVorlauf = row[2] != DBNull.Value ? Convert.ToInt32(row[2]) : 0;
                this.m_nTemperatur = row[3] != DBNull.Value ? Convert.ToInt32(row[3]) : 0;
                this.m_nCOP = row[4] != DBNull.Value ? Convert.ToDouble(row[4]) : 0;
                this.m_nPkuehl = row[5] != DBNull.Value ? Convert.ToDouble(row[5]) : 0;

                // Auch in die Liste für 'rows = 1'
                _internalList.Add(this);
            }
        }

        public void ReadVorlauf(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();

            foreach (DataRow row in dt.Rows)
            {
                KenndatenKuehlungModel item = new KenndatenKuehlungModel();
                item.m_nVorlauf = row[0] != DBNull.Value ? Convert.ToInt32(row[0]) : 0;
                item.m_ID_WP = row[1] != DBNull.Value ? Convert.ToInt32(row[1]) : 0;
                _internalList.Add(item);
            }
        }

        private void ExecuteRead(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();

            foreach (DataRow row in dt.Rows)
            {
                KenndatenKuehlungModel item = new KenndatenKuehlungModel();
                item.m_ID = row[0] != DBNull.Value ? Convert.ToInt32(row[0]) : 0;
                item.m_ID_WP = row[1] != DBNull.Value ? Convert.ToInt32(row[1]) : 0;
                item.m_nVorlauf = row[2] != DBNull.Value ? Convert.ToInt32(row[2]) : 0;
                item.m_nTemperatur = row[3] != DBNull.Value ? Convert.ToInt32(row[3]) : 0;
                item.m_nCOP = row[4] != DBNull.Value ? Convert.ToDouble(row[4]) : 0;
                item.m_nPkuehl = row[5] != DBNull.Value ? Convert.ToDouble(row[5]) : 0;
                _internalList.Add(item);
            }
        }

        /// <summary>
        /// Die KÜHL-Kennlinien eines Stammgeräts für den Renderer (iU9-W7.0c): je
        /// Vorlauftemperatur eine COP- und eine Pkuehl-Reihe über der Außentemperatur.
        ///
        /// <para><b>Nur die höchste Laststufe.</b> <c>Tab_Kenndaten_Kuehlung_STAMM</c>
        /// führt die Kennlinien je Teillast; <c>Form_WP.InitChart</c> holt sich mit
        /// <c>SELECT MAX(Last)</c> die größte und zeigt allein deren Zeilen (Z. 256-271).
        /// Gibt es keine Laststufe — <c>MAX</c> liefert dann <c>NULL</c> oder gar keine
        /// Zeile —, werden ALLE Zeilen genommen. Beides ist woertlich uebernommen.</para>
        /// </summary>
        public static KennlinienSatz Reihen(int idWp)
        {
            object maxLast = DataRepository.ExecuteScalar(
                "SELECT MAX([Last]) FROM " + WPStammCtrl.CURVE_K + " WHERE ID_WP = ?",
                new DbParam("@id", idWp));

            var vorlaeufe = new List<int>();
            DataTable dtv = DataRepository.GetDataTable(
                "SELECT Vorlauf, ID_WP FROM " + WPStammCtrl.CURVE_K + " GROUP BY Vorlauf, ID_WP HAVING ID_WP = ?",
                new DbParam("@id", idWp));
            if (dtv != null)
                foreach (DataRow r in dtv.Rows)
                    vorlaeufe.Add(r["Vorlauf"] != DBNull.Value ? Convert.ToInt32(r["Vorlauf"]) : 0);

            DataTable dt;
            if (maxLast != null && maxLast != DBNull.Value)
                dt = DataRepository.GetDataTable(
                    "SELECT Vorlauf, Temperatur, COP, Pkuehl FROM " + WPStammCtrl.CURVE_K +
                    " WHERE ID_WP = ? AND [Last] = ? ORDER BY Temperatur ASC",
                    new DbParam("@id", idWp), new DbParam("@last", Convert.ToInt32(maxLast)));
            else
                dt = DataRepository.GetDataTable(
                    "SELECT Vorlauf, Temperatur, COP, Pkuehl FROM " + WPStammCtrl.CURVE_K +
                    " WHERE ID_WP = ? ORDER BY Temperatur ASC",
                    new DbParam("@id", idWp));

            return KennlinienSatz.Bauen(vorlaeufe, dt, "Pkuehl");
        }

        /// <summary>
        /// Gibt es zu diesem Stammgerät überhaupt Kühl-Kenndaten? Das entscheidet, ob
        /// <c>Form_WP</c> den Umschalter „Wärme / Kühlung" zeigt
        /// (<c>HatKuehlKenndaten</c>, Z. 177).
        /// </summary>
        public static bool HatKenndaten(int idWp)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + WPStammCtrl.CURVE_K + " WHERE ID_WP = ?",
                new DbParam("@id", idWp));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        #endregion

        #region --- DATABASE WRITE OPERATIONS ---

        public bool Delete()
        {
            // Korrektur: Standard DELETE Syntax
            string sql = $"DELETE FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = {m_ID_WP}";
            return DataRepository.ExecuteSQL(sql);
        }

        public bool Insert()
        {
            try
            {
                // ID-Ermittlung
                object result = DataRepository.ExecuteScalar("SELECT Max(ID) FROM Tab_Kenndaten_Kuehlung");
                m_ID = (result == DBNull.Value) ? 1 : Convert.ToInt32(result) + 1;

                // Insert mit InvariantCulture
                string sql = FormattableString.Invariant($@"
                    INSERT INTO Tab_Kenndaten_Kuehlung (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) 
                    VALUES ({m_ID}, {m_ID_WP}, {m_nVorlauf}, {m_nTemperatur}, {m_nCOP}, {m_nPkuehl}, {m_nLast})");

                return DataRepository.ExecuteSQL(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei Insert (Kühlung): " + ex.Message);
                return false;
            }
        }

        public bool Update()
        {
            // Korrektur: UPDATE benötigt eine WHERE Klausel (normalerweise auf die ID)
            string sql = FormattableString.Invariant($@"
                UPDATE Tab_Kenndaten_Kuehlung 
                SET ID_WP = {m_ID_WP}, 
                    Vorlauf = {m_nVorlauf}, 
                    Temperatur = {m_nTemperatur}, 
                    COP = {m_nCOP}, 
                    Pkuehl = {m_nPkuehl}
                WHERE ID = {m_ID}");

            return DataRepository.ExecuteSQL(sql);
        }

        #endregion
    }
}
