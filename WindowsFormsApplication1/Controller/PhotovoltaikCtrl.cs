using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class PhotovoltaikCtrl : PhotovoltaikModel
    {
        // --- Kompatibilitäts-Layer nach deinem Vorbild ---
        private List<PhotovoltaikModel> _internalList = new List<PhotovoltaikModel>();

        public int rows => _internalList.Count;
        public new List<PhotovoltaikModel> items => _internalList;

        [Obsolete("Verwendung von ODBC entfernt. DB-Operationen laufen jetzt über das DataRepository.")]
        public OleDbCommand DBCommand;

        public PhotovoltaikModel model = new PhotovoltaikModel();

        public PhotovoltaikCtrl()
        {
#pragma warning disable CS0618
            DBCommand = new OleDbCommand();
#pragma warning restore CS0618
        }

        ~PhotovoltaikCtrl()
        {
#pragma warning disable CS0618
            DBCommand?.Dispose();
#pragma warning restore CS0618
        }

        #region --- DATABASE READ OPERATIONS ---

        public void ReadAll(string szFilter = "")
        {
            string sql;

            if (string.IsNullOrEmpty(szFilter))
                sql = "SELECT * FROM Tab_PV ORDER BY Bezeichner";
            else
                sql = "SELECT * FROM Tab_PV WHERE " + szFilter + " ORDER BY Bezeichner";

            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();

            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    PhotovoltaikModel item = new PhotovoltaikModel();

                    if (row.Table.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.m_ID = Convert.ToInt32(row["ID"]);
                    if (row.Table.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) item.m_szName = row["Bezeichner"].ToString();
                    if (row.Table.Columns.Contains("Firma") && row["Firma"] != DBNull.Value) item.m_szFirma = row["Firma"].ToString();
                    if (row.Table.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) item.m_szBeschreibung = row["Beschreibung"].ToString();
                    if (row.Table.Columns.Contains("Leistung") && row["Leistung"] != DBNull.Value) item.m_Leistung = Convert.ToDouble(row["Leistung"]);
                    if (row.Table.Columns.Contains("Wirkungsgrad") && row["Wirkungsgrad"] != DBNull.Value) item.m_Wirkungsgrad = Convert.ToDouble(row["Wirkungsgrad"]);
                    if (row.Table.Columns.Contains("U_Mpp") && row["U_Mpp"] != DBNull.Value) item.m_U_Mpp = Convert.ToDouble(row["U_Mpp"]);
                    if (row.Table.Columns.Contains("U_Leerlauf") && row["U_Leerlauf"] != DBNull.Value) item.m_U_Leerlauf = Convert.ToDouble(row["U_Leerlauf"]);
                    if (row.Table.Columns.Contains("I_Mpp") && row["I_Mpp"] != DBNull.Value) item.m_I_Mpp = Convert.ToDouble(row["I_Mpp"]);
                    if (row.Table.Columns.Contains("I_Kurzschluss") && row["I_Kurzschluss"] != DBNull.Value) item.m_I_Kurzschluss = Convert.ToDouble(row["I_Kurzschluss"]);
                    if (row.Table.Columns.Contains("alpha_SC") && row["alpha_SC"] != DBNull.Value) item.m_alpha_SC = Convert.ToDouble(row["alpha_SC"]);
                    if (row.Table.Columns.Contains("beta_OC") && row["beta_OC"] != DBNull.Value) item.m_beta_OC = Convert.ToDouble(row["beta_OC"]);
                    if (row.Table.Columns.Contains("gamma_PMP") && row["gamma_PMP"] != DBNull.Value) item.m_Temp_Coeff_Pmax = Convert.ToDouble(row["gamma_PMP"]);
                    if (row.Table.Columns.Contains("T_NOCT") && row["T_NOCT"] != DBNull.Value) item.m_T_NOCT = Convert.ToDouble(row["T_NOCT"]);
                    if (row.Table.Columns.Contains("Laenge") && row["Laenge"] != DBNull.Value) item.m_Laenge = Convert.ToDouble(row["Laenge"]);
                    if (row.Table.Columns.Contains("Breite") && row["Breite"] != DBNull.Value) item.m_Breite = Convert.ToDouble(row["Breite"]);
                    if (row.Table.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value) item.m_Modulkosten = Convert.ToDouble(row["Modulkosten"]);

                    _internalList.Add(item);
                }
            }
        }

        public void ReadSingle(int ID)
        {
            // Bei ReadSingle befüllst du ja die Felder der eigenen Instanz (m_ID, m_szName etc.),
            // aber wir können zur Sicherheit die Liste leeren oder das gefundene Element hineinlegen,
            // falls das UI nach einem ReadSingle auch auf items[0] zugreift.
            _internalList.Clear();

            string sql = "SELECT * FROM Tab_PV WHERE ID = ?";
            OleDbParameter parameter = new OleDbParameter("?", ID);

            DataTable dt = DataRepository.GetDataTable(sql, parameter);

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (row.Table.Columns.Contains("ID") && row["ID"] != DBNull.Value) m_ID = Convert.ToInt32(row["ID"]);
                if (row.Table.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) m_szName = row["Bezeichner"].ToString();
                if (row.Table.Columns.Contains("Firma") && row["Firma"] != DBNull.Value) m_szFirma = row["Firma"].ToString();
                if (row.Table.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) m_szBeschreibung = row["Beschreibung"].ToString();
                if (row.Table.Columns.Contains("Leistung") && row["Leistung"] != DBNull.Value) m_Leistung = Convert.ToDouble(row["Leistung"]);
                if (row.Table.Columns.Contains("Wirkungsgrad") && row["Wirkungsgrad"] != DBNull.Value) m_Wirkungsgrad = Convert.ToDouble(row["Wirkungsgrad"]);
                if (row.Table.Columns.Contains("U_Mpp") && row["U_Mpp"] != DBNull.Value) m_U_Mpp = Convert.ToDouble(row["U_Mpp"]);
                if (row.Table.Columns.Contains("U_Leerlauf") && row["U_Leerlauf"] != DBNull.Value) m_U_Leerlauf = Convert.ToDouble(row["U_Leerlauf"]);
                if (row.Table.Columns.Contains("I_Mpp") && row["I_Mpp"] != DBNull.Value) m_I_Mpp = Convert.ToDouble(row["I_Mpp"]);
                if (row.Table.Columns.Contains("I_Kurzschluss") && row["I_Kurzschluss"] != DBNull.Value) m_I_Kurzschluss = Convert.ToDouble(row["I_Kurzschluss"]);
                if (row.Table.Columns.Contains("alpha_SC") && row["alpha_SC"] != DBNull.Value) m_alpha_SC = Convert.ToDouble(row["alpha_SC"]);
                if (row.Table.Columns.Contains("beta_OC") && row["beta_OC"] != DBNull.Value) m_beta_OC = Convert.ToDouble(row["beta_OC"]);
                if (row.Table.Columns.Contains("gamma_PMP") && row["gamma_PMP"] != DBNull.Value) m_Temp_Coeff_Pmax = Convert.ToDouble(row["gamma_PMP"]);
                if (row.Table.Columns.Contains("T_NOCT") && row["T_NOCT"] != DBNull.Value) m_T_NOCT = Convert.ToDouble(row["T_NOCT"]);
                if (row.Table.Columns.Contains("Laenge") && row["Laenge"] != DBNull.Value) m_Laenge = Convert.ToDouble(row["Laenge"]);
                if (row.Table.Columns.Contains("Breite") && row["Breite"] != DBNull.Value) m_Breite = Convert.ToDouble(row["Breite"]);
                if (row.Table.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value) m_Modulkosten = Convert.ToDouble(row["Modulkosten"]);

                // Kopie in die interne Liste legen, damit rows auf 1 springt
                _internalList.Add(this);
            }
        }

        #endregion

        #region --- DATABASE WRITE OPERATIONS ---

        public bool Update()
        {
            try
            {
                string sql = @"
                    UPDATE Tab_PV 
                    SET 
                        Firma = ?, 
                        Beschreibung = ?, 
                        Leistung = ?, 
                        Wirkungsgrad = ?, 
                        U_Mpp = ?, 
                        U_Leerlauf = ?, 
                        I_Mpp = ?, 
                        I_Kurzschluss = ?, 
                        alpha_SC = ?, 
                        beta_OC = ?, 
                        gamma_PMP = ?, 
                        T_NOCT = ?, 
                        Laenge = ?, 
                        Breite = ?, 
                        Modulkosten = ? 
                    WHERE 
                        Bezeichner = ?";

                OleDbParameter[] parameters = new OleDbParameter[]
                {
                    new OleDbParameter("?", model.m_szFirma ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_szBeschreibung ?? (object)DBNull.Value),
                    new OleDbParameter("?", model.m_Leistung),
                    new OleDbParameter("?", model.m_Wirkungsgrad),
                    new OleDbParameter("?", model.m_U_Mpp),
                    new OleDbParameter("?", model.m_U_Leerlauf),
                    new OleDbParameter("?", model.m_I_Mpp),
                    new OleDbParameter("?", model.m_I_Kurzschluss),

                    new OleDbParameter("?", model.m_alpha_SC == 0 ? DBNull.Value : (object)model.m_alpha_SC),
                    new OleDbParameter("?", model.m_beta_OC == 0 ? DBNull.Value : (object)model.m_beta_OC),
                    new OleDbParameter("?", model.m_Temp_Coeff_Pmax == 0 ? DBNull.Value : (object)model.m_Temp_Coeff_Pmax),
                    new OleDbParameter("?", model.m_T_NOCT == 0 ? DBNull.Value : (object)model.m_T_NOCT),

                    new OleDbParameter("?", model.m_Laenge),
                    new OleDbParameter("?", model.m_Breite),
                    new OleDbParameter("?", model.m_Modulkosten),
                    new OleDbParameter("?", model.m_szName ?? (object)DBNull.Value)
                };

                DataRepository.ExecuteNonQuery(sql, parameters);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler beim Update: " + ex.Message);
                return false;
            }
        }

        #endregion

        #region --- STAMM -> PROJEKT KOPIE (analog HeizkesselCtrl/BHKWCtrl) ---

        /// <summary>
        /// V3 (PV-Konzept § 2.3, Etappe P1): installierte PV-Leistung eines Projekts
        /// [kWp] — die GETEILTE Hilfsfunktion für Simulation, Vergütungsdialog und
        /// EEG-Größenklassen. <c>Tab_Energieanlagen.PV_Leistung</c> ist die
        /// MODULANZAHL (kein kW!); kWp gibt es nur rechnerisch:
        /// Σ (<c>Tab_PV.Leistung</c> [W je Modul] × Modulanzahl) / 1000.
        /// 0 = keine PV-Anlagen bzw. keine gepflegte Modulleistung.
        /// </summary>
        public static double KwpDesProjekts(int idProjekt)
        {
            return KwpSumme(idProjekt, 0);
        }

        /// <summary>
        /// DER KERN der kWp-Rechnung — dieselbe Formel wie
        /// <see cref="KwpDesProjekts"/>, wahlweise auf EINE Anlagenzeile eingegrenzt
        /// (<paramref name="idAnlage"/> &gt; 0).
        ///
        /// <para><b>Warum es die anlagenscharfe Fassung gibt</b> (Anwenderentscheid
        /// 30.08.2026, Befund I-1): Die Kostenseite bemisst „€ je kWp" je
        /// Kostenposition, und eine Position darf an EINER Anlage hängen
        /// (<c>Tab_ProjektWerte.ID_Anlage</c>). Sie brauchte deshalb dieselbe
        /// Bezugsgröße wie Simulation und Vergütungsdialog, nur enger geschnitten —
        /// eine zweite Formel daneben wäre genau der Bruch, der zu Befund I-1
        /// geführt hat (dort summierte die Kostenseite die MODULANZAHL).
        /// <c>internal</c> statt <c>private</c>, damit
        /// <see cref="TechnikPlanwertCtrl.BaugroesseSumme"/> denselben Kern ruft und
        /// es bei EINER kWp-Wahrheit bleibt.</para>
        ///
        /// <para>Der Filter <c>ID_Type = PV_TYP</c> bleibt unverändert Teil des Kerns:
        /// Referenz-/Bestandsanlagen (<c>REF_PV_TYP</c>) tragen keine geplante
        /// Leistung und dürfen weder die Simulationsgröße noch eine Kostenbemessung
        /// aufblähen.</para>
        /// </summary>
        internal static double KwpSumme(int idProjekt, int idAnlage)
        {
            try
            {
                string sql = "SELECT SUM(p.Leistung * a.PV_Leistung) " +
                             "FROM Tab_Energieanlagen AS a INNER JOIN Tab_PV AS p ON a.ID_PV = p.ID " +
                             "WHERE a.ID_Projekt = ? AND a.ID_Type = ?";
                var ps = new List<OleDbParameter>
                {
                    new OleDbParameter("@p", idProjekt),
                    new OleDbParameter("@t", WizardItemClass.PV_TYP)
                };
                if (idAnlage > 0)
                {
                    sql += " AND a.ID = ?";
                    ps.Add(new OleDbParameter("@a", idAnlage));
                }
                object o = DataRepository.ExecuteScalar(sql, ps.ToArray());
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToDouble(o) / 1000.0;
            }
            catch { return 0; }
        }

        // Liefert die Projekt-ID (Tab_PV.ID) eines Bezeichners im Projekt, oder 0.
        public int GetProjektId(string szBezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_PV WHERE Bezeichner = ? AND ID_Projekt = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""),
                new OleDbParameter("@idProj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        public bool ExistsInProjekt(string szBezeichner, int idProjekt)
        {
            return GetProjektId(szBezeichner, idProjekt) > 0;
        }

        // Kopiert einen Stammdatensatz (Tab_PV_STAMM) in die Projekt-Tabelle (Tab_PV), sofern er
        // fuer das Projekt noch nicht existiert. Setzt ID_Projekt und vergibt eine neue Projekt-ID.
        // Rueckgabe: Projekt-ID (Tab_PV.ID) des kopierten ODER vorhandenen Datensatzes, -1 bei Fehler.
        // Dies ist der Wert, den WErzeugerModel.ID_PV tragen muss (Beziehung -> Projekt-Tabelle).
        public int CopyFromStamm(int stammId, int idProjekt)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + PhotovoltaikStammCtrl.TABLE + "] WHERE ID = ?",
                    new OleDbParameter("@id", stammId));

                if (dt == null || dt.Rows.Count == 0)
                {
                    // NACHARBEIT PAKET 8, BEFUND N10: gemeinsame Entscheidungsstelle
                    // (Dialog in der Bedienung, Protokolleintrag im Rechenlauf) - wie in
                    // den vier baugleichen Geschwistern.
                    DataRepository.FehlerMelden("Der PV-Stammdatensatz wurde nicht gefunden (ID " + stammId + ").");
                    return -1;
                }

                DataRow s = dt.Rows[0];
                string szBezeichner = s["Bezeichner"].ToString();

                int vorhandeneId = GetProjektId(szBezeichner, idProjekt);
                if (vorhandeneId > 0) return vorhandeneId;

                int neueId = DataRepository.GetMaxID("Tab_PV") + 1;

                // ReadOnly wird NICHT uebernommen (existiert in der Projekt-Tabelle nicht).
                string sql = @"INSERT INTO Tab_PV
                    (ID, ID_Projekt, Bezeichner, Firma, Beschreibung, Leistung, Wirkungsgrad, U_Mpp, U_Leerlauf,
                     I_Mpp, I_Kurzschluss, alpha_SC, beta_OC, gamma_PMP, T_NOCT, Laenge, Breite, Modulkosten)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", neueId),
                    new OleDbParameter("@idProj", idProjekt),
                    P("@bez", s["Bezeichner"]),
                    P("@fir", ColOrNull(s, "Firma")),
                    P("@bes", ColOrNull(s, "Beschreibung")),
                    P("@lei", ColOrNull(s, "Leistung")),
                    P("@wir", ColOrNull(s, "Wirkungsgrad")),
                    P("@ump", ColOrNull(s, "U_Mpp")),
                    P("@ule", ColOrNull(s, "U_Leerlauf")),
                    P("@imp", ColOrNull(s, "I_Mpp")),
                    P("@iks", ColOrNull(s, "I_Kurzschluss")),
                    P("@asc", ColOrNull(s, "alpha_SC")),
                    P("@boc", ColOrNull(s, "beta_OC")),
                    P("@gam", ColOrNull(s, "gamma_PMP")),
                    P("@noc", ColOrNull(s, "T_NOCT")),
                    P("@lae", ColOrNull(s, "Laenge")),
                    P("@bre", ColOrNull(s, "Breite")),
                    P("@mod", ColOrNull(s, "Modulkosten"))
                };

                bool ok = DataRepository.ExecuteSQL(sql, ps);
                return ok ? neueId : -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Kopieren des PV-Moduls aus den Stammdaten: " + ex.Message);
                return -1;
            }
        }

        public int CopyFromStamm(string szBezeichner, int idProjekt)
        {
            int stammId = DataRepository.GetIdByName(PhotovoltaikStammCtrl.TABLE, "Bezeichner", szBezeichner);
            if (stammId <= 0) return -1;
            return CopyFromStamm(stammId, idProjekt);
        }

        public bool DeleteFromProjekt(string szBezeichner, int idProjekt)
        {
            string sql = "DELETE FROM Tab_PV WHERE Bezeichner = ? AND ID_Projekt = ?";
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

        #endregion
    }
}