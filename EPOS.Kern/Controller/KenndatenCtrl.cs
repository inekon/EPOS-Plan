using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class KenndatenCtrl : KenndatenModel
    {
        // --- Kompatibilitäts-Layer ---
        private List<KenndatenModel> _internalList = new List<KenndatenModel>();

        public int rows => _internalList.Count;
        public new List<KenndatenModel> items => _internalList;

        public KenndatenModel model;

        public KenndatenCtrl()
        {
            model = new KenndatenModel();
        }

        #region --- DATABASE READ OPERATIONS ---

        public void ReadAll()
        {
            string sql = "SELECT * FROM Tab_Kenndaten ORDER BY ID_WP";
            ExecuteRead(sql);
        }

        public void ReadVorlauf(string sql)
        {
            // Spezielle Read-Logik für Vorlauf-Abfragen
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();

            foreach (DataRow row in dt.Rows)
            {
                KenndatenModel item = new KenndatenModel();
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
                KenndatenModel item = new KenndatenModel();
                item.m_ID = row[0] != DBNull.Value ? Convert.ToInt32(row[0]) : 0;
                item.m_ID_WP = row[1] != DBNull.Value ? Convert.ToInt32(row[1]) : 0;
                item.m_nVorlauf = row[2] != DBNull.Value ? Convert.ToInt32(row[2]) : 0;
                item.m_nTemperatur = row[3] != DBNull.Value ? Convert.ToInt32(row[3]) : 0;
                item.m_nCOP = row[4] != DBNull.Value ? Convert.ToDouble(row[4]) : 0;
                item.m_nPTherm = row[5] != DBNull.Value ? Convert.ToDouble(row[5]) : 0;
                _internalList.Add(item);
            }
        }

        /// <summary>
        /// Die WÄRME-Kennlinien eines Stammgeräts für den Renderer (iU9-W7.0c): je
        /// Vorlauftemperatur eine COP- und eine Ptherm-Reihe über der Außentemperatur.
        ///
        /// <para><b>Zwei Abfragen, woertlich aus <c>Form_WP.InitChart</c> (Z. 243-331).</b>
        /// Erst die Vorlaufstufen (<c>GROUP BY Vorlauf, ID_WP HAVING ID_WP = …</c>),
        /// dann EINMAL alle Stuetzstellen des Geraets, nach Temperatur aufsteigend. Der
        /// Vorlaeufer teilte die Tabelle danach mit <c>DataTable.Select("Vorlauf=…")</c>
        /// auf; hier tut das eine Schleife ueber dieselben Zeilen — dieselbe Reihenfolge,
        /// eine Abfrage weniger je Reihe.</para>
        ///
        /// <para>Die Reihenfolge der REIHEN ist die der Vorlaufabfrage, die Reihenfolge
        /// der PUNKTE die der Datenabfrage. Beides bleibt so, weil daran die
        /// Farbzuordnung der Legende haengt.</para>
        /// </summary>
        public static KennlinienSatz Reihen(int idWp)
        {
            var vorlaeufe = new List<int>();
            DataTable dtv = DataRepository.GetDataTable(
                "SELECT Vorlauf, ID_WP FROM " + WPStammCtrl.CURVE + " GROUP BY Vorlauf, ID_WP HAVING ID_WP = ?",
                new DbParam("@id", idWp));
            if (dtv != null)
                foreach (DataRow r in dtv.Rows)
                    vorlaeufe.Add(r["Vorlauf"] != DBNull.Value ? Convert.ToInt32(r["Vorlauf"]) : 0);

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Vorlauf, Temperatur, COP, Ptherm FROM " + WPStammCtrl.CURVE +
                " WHERE ID_WP = ? ORDER BY Temperatur ASC",
                new DbParam("@id", idWp));

            return KennlinienSatz.Bauen(vorlaeufe, dt, "Ptherm");
        }

        #endregion

        #region --- DATABASE WRITE OPERATIONS ---

        public bool Delete()
        {
            // Korrektur: Das ursprüngliche SQL "DELETE WPName FROM..." war syntaktisch oft problematisch in Access
            string sql = $"DELETE FROM Tab_Kenndaten WHERE ID_WP = {m_ID_WP}";
            return DataRepository.ExecuteSQL(sql);
        }

        public bool Insert()
        {
            try
            {
                // ID-Ermittlung
                object result = DataRepository.ExecuteScalar("SELECT Max(ID) FROM Tab_Kenndaten");
                m_ID = (result == DBNull.Value) ? 1 : Convert.ToInt32(result) + 1;

                // Insert mit InvariantCulture für korrekte Dezimalpunkte (COP/Ptherm)
                string sql = FormattableString.Invariant($@"
                    INSERT INTO Tab_Kenndaten (ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm) 
                    VALUES ({m_ID}, {m_ID_WP}, {m_nVorlauf}, {m_nTemperatur}, {m_nCOP}, {m_nPTherm})");

                return DataRepository.ExecuteSQL(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei Insert: " + ex.Message);
                return false;
            }
        }

        public bool Update()
        {
            // Korrektur der Anführungszeichen und Logik aus dem Original
            string sql = FormattableString.Invariant($@"
                UPDATE Tab_Kenndaten 
                SET ID_WP={m_ID_WP}, Vorlauf={m_nVorlauf}, Temperatur={m_nTemperatur}, 
                    COP={m_nCOP}, Ptherm={m_nPTherm} 
                WHERE ID={m_ID}");

            return DataRepository.ExecuteSQL(sql);
        }

        #endregion
    }
}