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

        /// <summary>
        /// Die WÄRME-Kennlinien eines STAMMGERÄTS als Zeilenliste (iU9-W7.3) — die
        /// Abfrage, mit der <c>Form_WP.btn_Kenndaten_Click</c> (Z. 479) das
        /// <c>DataSet</c> des Editors füllte, in derselben Spaltenfolge.
        ///
        /// <para>Gegenstück zu <see cref="Abgleichen"/>: Diese Methode liest den Stand
        /// IN den Editor, jene schreibt ihn zurück.</para>
        /// </summary>
        public static IReadOnlyList<KenndatenModel> LiesStamm(int idWp)
        {
            var liste = new List<KenndatenModel>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm FROM " + WPStammCtrl.CURVE +
                " WHERE ID_WP = ?",
                new DbParam("@id", idWp));
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
                liste.Add(new KenndatenModel
                {
                    m_ID = r["ID"] != DBNull.Value ? Convert.ToInt32(r["ID"]) : 0,
                    m_ID_WP = r["ID_WP"] != DBNull.Value ? Convert.ToInt32(r["ID_WP"]) : 0,
                    m_nVorlauf = r["Vorlauf"] != DBNull.Value ? Convert.ToInt32(r["Vorlauf"]) : 0,
                    m_nTemperatur = r["Temperatur"] != DBNull.Value ? Convert.ToInt32(r["Temperatur"]) : 0,
                    m_nCOP = r["COP"] != DBNull.Value ? Convert.ToDouble(r["COP"]) : 0,
                    m_nPTherm = r["Ptherm"] != DBNull.Value ? Convert.ToDouble(r["Ptherm"]) : 0
                });
            return liste;
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

        /// <summary>
        /// Gleicht die WÄRME-Kennlinien eines STAMMGERÄTS an einen Soll-Stand an
        /// (iU9-W7.0d) — der Rückschreibweg aus <c>Form_WP.btn_Kenndaten_Click</c>
        /// (Z. 475-553), jetzt in EINER Transaktion.
        ///
        /// <para><b>Was gleich bleibt.</b> Dieselben drei Anweisungen mit demselben
        /// Wortlaut: <c>DELETE … WHERE ID = ?</c> für weggefallene Zeilen,
        /// <c>INSERT … (ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm)</c> mit
        /// fortlaufender <c>ID = Max(ID)+1</c> für neue, <c>UPDATE … SET ID_WP, Vorlauf,
        /// Temperatur, COP, Ptherm WHERE ID = ?</c> für geänderte. Auch die Id-Vergabe
        /// bleibt: EINMAL <c>Max(ID)</c> über die GANZE Tabelle, danach hochzählen.</para>
        ///
        /// <para><b>Was sich ändert, und warum (Abweichung A-5).</b> Der Dialog
        /// <c>Kenndaten</c> bearbeitete ein <c>DataSet</c>; der Aufrufer las die drei
        /// Fälle aus <c>DataRow.RowState</c> ab und schrieb sie in einer Schleife OHNE
        /// Transaktion. Ein Fehler in der Mitte hinterliess damit einen HALBEN Stand:
        /// ein paar Zeilen gelöscht, der Rest noch alt. Eine Razor-Komponente hat kein
        /// <c>DataSet</c> — sie bearbeitet eine Liste, und die Differenz rechnet jetzt
        /// der Kern. Ergebnisgleich, aber alles oder nichts.</para>
        ///
        /// <para><b>Die Zuordnung läuft über <c>m_ID</c>.</b> Eine Soll-Zeile mit
        /// <c>m_ID == 0</c> ist neu (der Vorläufer: <c>RowState.Added</c>); eine
        /// Ist-Zeile, deren Id in keiner Soll-Zeile mehr vorkommt, ist gelöscht. Zeilen
        /// mit unveränderten Werten werden übersprungen — der Vorläufer erkannte das
        /// über <c>RowState.Unchanged</c>.</para>
        /// </summary>
        /// <param name="idWp">Das Stammgerät (<c>Tab_WP_STAMM.ID</c>).</param>
        /// <param name="sollZeilen">Der Stand, den der Dialog zurückgibt.</param>
        /// <returns><c>false</c>, wenn nichts geschrieben wurde (die Transaktion ist dann zurückgerollt).</returns>
        public static bool Abgleichen(int idWp, IReadOnlyList<KenndatenModel> sollZeilen)
        {
            if (idWp <= 0) return false;
            IReadOnlyList<KenndatenModel> soll = sollZeilen ?? (IReadOnlyList<KenndatenModel>)new List<KenndatenModel>();

            // Ist-Stand. Er wird VOR der Transaktion gelesen - genau wie der Vorlaeufer
            // das DataSet vor dem Oeffnen des Dialogs las.
            var ist = new Dictionary<int, KenndatenModel>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm FROM " + WPStammCtrl.CURVE + " WHERE ID_WP = ?",
                new DbParam("@id", idWp));
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                {
                    if (r["ID"] == DBNull.Value) continue;
                    var m = new KenndatenModel
                    {
                        m_ID = Convert.ToInt32(r["ID"]),
                        m_ID_WP = r["ID_WP"] != DBNull.Value ? Convert.ToInt32(r["ID_WP"]) : 0,
                        m_nVorlauf = r["Vorlauf"] != DBNull.Value ? Convert.ToInt32(r["Vorlauf"]) : 0,
                        m_nTemperatur = r["Temperatur"] != DBNull.Value ? Convert.ToInt32(r["Temperatur"]) : 0,
                        m_nCOP = r["COP"] != DBNull.Value ? Convert.ToDouble(r["COP"]) : 0,
                        m_nPTherm = r["Ptherm"] != DBNull.Value ? Convert.ToDouble(r["Ptherm"]) : 0
                    };
                    ist[m.m_ID] = m;
                }

            var behalten = new HashSet<int>();
            foreach (KenndatenModel s in soll) if (s != null && s.m_ID > 0) behalten.Add(s.m_ID);

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    // (1) Weggefallene Zeilen.
                    foreach (KenndatenModel a in ist.Values)
                        if (!behalten.Contains(a.m_ID))
                            v.Ausfuehren("DELETE FROM " + WPStammCtrl.CURVE + " WHERE ID = ?",
                                new DbParam("@id", DbParamTyp.Integer) { Wert = a.m_ID });

                    // (2) Id-Vergabe wie im Vorlaeufer: EINMAL Max(ID) ueber die ganze
                    //     Tabelle, danach hochzaehlen. Innerhalb des Vorgangs gelesen,
                    //     damit die soeben geloeschten Zeilen mitzaehlen.
                    int naechsteId;
                    {
                        object m = v.Skalar("SELECT Max(ID) FROM " + WPStammCtrl.CURVE);
                        naechsteId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                    }

                    foreach (KenndatenModel s in soll)
                    {
                        if (s == null) continue;

                        if (s.m_ID <= 0)
                        {
                            // (3) Neue Zeile.
                            v.Ausfuehren(
                                "INSERT INTO " + WPStammCtrl.CURVE +
                                " (ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm) VALUES (?, ?, ?, ?, ?, ?)",
                                new DbParam("@id", DbParamTyp.Integer) { Wert = naechsteId++ },
                                new DbParam("@wp", DbParamTyp.Integer) { Wert = idWp },
                                new DbParam("@vl", DbParamTyp.Integer) { Wert = s.m_nVorlauf },
                                new DbParam("@t", DbParamTyp.Integer) { Wert = s.m_nTemperatur },
                                new DbParam("@cop", DbParamTyp.Double) { Wert = s.m_nCOP },
                                new DbParam("@pt", DbParamTyp.Double) { Wert = s.m_nPTherm });
                            continue;
                        }

                        // (4) Geaenderte Zeile. Unveraenderte bleiben unberuehrt - das
                        //     entspricht RowState.Unchanged des Vorlaeufers.
                        KenndatenModel alt;
                        if (ist.TryGetValue(s.m_ID, out alt) &&
                            alt.m_ID_WP == idWp &&
                            alt.m_nVorlauf == s.m_nVorlauf &&
                            alt.m_nTemperatur == s.m_nTemperatur &&
                            Math.Abs(alt.m_nCOP - s.m_nCOP) < 1e-12 &&
                            Math.Abs(alt.m_nPTherm - s.m_nPTherm) < 1e-12) continue;

                        v.Ausfuehren(
                            "UPDATE " + WPStammCtrl.CURVE +
                            " SET ID_WP = ?, Vorlauf = ?, Temperatur = ?, COP = ?, Ptherm = ? WHERE ID = ?",
                            new DbParam("@wp", DbParamTyp.Integer) { Wert = idWp },
                            new DbParam("@vl", DbParamTyp.Integer) { Wert = s.m_nVorlauf },
                            new DbParam("@t", DbParamTyp.Integer) { Wert = s.m_nTemperatur },
                            new DbParam("@cop", DbParamTyp.Double) { Wert = s.m_nCOP },
                            new DbParam("@pt", DbParamTyp.Double) { Wert = s.m_nPTherm },
                            new DbParam("@id", DbParamTyp.Integer) { Wert = s.m_ID });
                    }

                    v.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    DataRepository.FehlerMelden("Fehler beim Speichern der Kennliniendaten: " + ex.Message);
                    return false;
                }
            }
        }

        #endregion
    }
}