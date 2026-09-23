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
                this.m_nLast = Last(dt, row);

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
                item.m_nLast = Last(dt, row);
                _internalList.Add(item);
            }
        }

        /// <summary>
        /// <c>Last</c> einer Zeile NULL-treu (Kuehlkonzept 5.1, Festlegung 1; 7.3) - ueber den
        /// NAMEN, nicht ueber die Position: Die Stammtabelle fuehrt vor <c>ID_WP</c> noch
        /// <c>ID_Projekt</c>, die Positionskette <c>row[0..5]</c> gilt allein fuer die
        /// Projekttabelle. Eine fehlende Spalte gilt wie NULL.
        /// </summary>
        private static int? Last(DataTable dt, DataRow row)
        {
            if (dt == null || !dt.Columns.Contains("Last")) return null;
            object v = row["Last"];
            return v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v);
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
        /// Gibt es zu diesem STAMMgerät überhaupt Kühl-Kenndaten
        /// (<c>Tab_Kenndaten_Kuehlung_STAMM</c>)? Das entscheidet, ob der Stammdialog den
        /// Umschalter „Wärme / Kühlung" zeigt und ob die Katalogliste ein Gerät „rechenbar
        /// kühlfähig" nennt.
        ///
        /// <para><b>Zwei benannte Prüfungen nebeneinander</b> (Kühlkonzept 5.0.1, KU2): Diese hier
        /// fragt den KATALOG — bis KU2 hieß sie <c>HatKenndaten</c>, der Name ist nur eindeutig
        /// geworden. Ob ein PROJEKTgerät kühlen kann, sagt allein
        /// <see cref="HatKenndatenProjekt"/>: Ein Gerät kann im Katalog eine Kennlinie haben und im
        /// Projekt keine.</para>
        /// </summary>
        public static bool HatKenndatenStamm(int idStammWp)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + WPStammCtrl.CURVE_K + " WHERE ID_WP = ?",
                new DbParam("@id", idStammWp));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        /// <summary>
        /// Gibt es zu diesem PROJEKTgerät Kühl-Kenndaten (<c>Tab_Kenndaten_Kuehlung</c>, deren
        /// <c>ID_WP</c> auf die Projektkopie in <c>Tab_WP</c> zeigt)? Daran hängt der Sperrgrund des
        /// Kühlbetriebs — im Schreibweg (<see cref="WPCtrl.KuehlkonfigurationSchreiben"/>), im
        /// Erzeugerdialog und die Ablehnung im Lauf (Kühlkonzept 5.0.1, 8.2, 10.3).
        /// </summary>
        public static bool HatKenndatenProjekt(int idProjektWp)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = ?",
                new DbParam("@id", idProjektWp));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        /// <summary>
        /// <b>Der projektseitige Kennlinienleser</b> (Kühlkonzept 5.1, Festlegung 1 (b)): alle
        /// Zeilen der Kühlkennlinie eines PROJEKTgeräts aus <c>Tab_Kenndaten_Kuehlung</c>, nach
        /// <c>ID</c> geordnet — die Grundlage von <see cref="Kuehlkennlinie.Bilden"/>, die Vorlauf,
        /// Laststufe, Dubletten und Achsenlage entscheidet. Nicht die Stammtabelle: Gerechnet wird
        /// mit der Kopie im Projekt, wie auf der Heizseite (<c>SimulationWaermepumpe.ModuleAufbauen</c>).
        /// </summary>
        public static List<KuehlkennlinienZeile> ZeilenProjekt(int idProjektWp)
        {
            return Zeilen("Tab_Kenndaten_Kuehlung", idProjektWp);
        }

        /// <summary>Dieselben Zeilen aus dem Katalog (<c>Tab_Kenndaten_Kuehlung_STAMM</c>) — für die Kennzeichnung eines Katalogsatzes.</summary>
        public static List<KuehlkennlinienZeile> ZeilenStamm(int idStammWp)
        {
            return Zeilen(WPStammCtrl.CURVE_K, idStammWp);
        }

        /// <summary>Die Kühlkennlinie eines Projektgeräts für seinen Kühl-Vorlauf (NULL = kleinster Stützwert).</summary>
        public static Kuehlkennlinie KennlinieProjekt(int idProjektWp, int? kuehlVorlauf)
        {
            return Kuehlkennlinie.Bilden(ZeilenProjekt(idProjektWp), kuehlVorlauf);
        }

        private static List<KuehlkennlinienZeile> Zeilen(string tabelle, int idWp)
        {
            var liste = new List<KuehlkennlinienZeile>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Vorlauf, Temperatur, COP, Pkuehl, [Last] FROM " + tabelle +
                " WHERE ID_WP = ? ORDER BY ID",
                new DbParam("@id", idWp));
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
                liste.Add(new KuehlkennlinienZeile(
                    r["ID"] != DBNull.Value ? Convert.ToInt32(r["ID"]) : 0,
                    r["Vorlauf"] != DBNull.Value ? Convert.ToInt32(r["Vorlauf"]) : 0,
                    r["Temperatur"] != DBNull.Value ? Convert.ToInt32(r["Temperatur"]) : 0,
                    r["COP"] != DBNull.Value ? Convert.ToDouble(r["COP"]) : 0.0,
                    r["Pkuehl"] != DBNull.Value ? Convert.ToDouble(r["Pkuehl"]) : 0.0,
                    r["Last"] != DBNull.Value ? (int?)Convert.ToInt32(r["Last"]) : null));
            return liste;
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

                // Parametrisiert statt eingesetzt (Kuehlkonzept 5.1, Festlegung 1): Last
                // bleibt NULL-treu - aus einer Zeile ohne Laststufe wird keine 0.
                return DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_Kenndaten_Kuehlung (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) " +
                    "VALUES (?, ?, ?, ?, ?, ?, ?)",
                    Parameter(true));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei Insert (Kühlung): " + ex.Message);
                return false;
            }
        }

        public bool Update()
        {
            // Mit Last (Kuehlkonzept 5.1, Festlegung 1; 7.3): Bis KU2 schrieb der Weg die
            // Laststufe nicht mit. Parametrisiert, NULL-treu, adressiert ueber die ID.
            return DataRepository.ExecuteSQL(
                "UPDATE Tab_Kenndaten_Kuehlung SET ID_WP = ?, Vorlauf = ?, Temperatur = ?, " +
                "COP = ?, Pkuehl = ?, [Last] = ? WHERE ID = ?",
                Parameter(false));
        }

        /// <summary>
        /// Die Parameter des Schreibwegs: mit <paramref name="idVorn"/> in der Reihenfolge des
        /// INSERT (ID zuerst), sonst in der des UPDATE (ID zuletzt). <c>Last</c> geht mit
        /// ausdruecklichem Typ, weil NULL dort ein Regelwert ist.
        /// </summary>
        private DbParam[] Parameter(bool idVorn)
        {
            var p = new List<DbParam>();
            if (idVorn) p.Add(new DbParam("@id", m_ID));
            p.Add(new DbParam("@wp", m_ID_WP));
            p.Add(new DbParam("@vor", m_nVorlauf));
            p.Add(new DbParam("@tem", m_nTemperatur));
            p.Add(new DbParam("@cop", m_nCOP));
            p.Add(new DbParam("@pk", m_nPkuehl));
            p.Add(ProjektPuffer.Par("@last", DbParamTyp.Integer,
                                    m_nLast.HasValue ? (object)m_nLast.Value : null));
            if (!idVorn) p.Add(new DbParam("@id", m_ID));
            return p.ToArray();
        }

        #endregion
    }
}
