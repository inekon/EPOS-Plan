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

        /// <summary>
        /// Löscht ALLE Zeilen dieses Bezeichners - projektübergreifend.
        ///
        /// Seit Schritt 4 der SchemaMigration sind die vier Anlagen-Referenzen auf
        /// Tab_Pufferspeicher.ID erzwungen und RESTRIKTIV; ohne vorheriges Lösen lehnt
        /// Access das DELETE ab ("includes related records"). Deshalb hier derselbe
        /// Vorlauf wie in <see cref="DeleteFromProjekt"/>.
        ///
        /// Achtung: der Aufrufkreis dieser Methode ist heute leer (Form_PufferSp_Admin
        /// und Form_PufferSp arbeiten auf den STAMM-Tabellen). Sie bleibt trotzdem
        /// stehen und wird mitgepflegt, damit ein späterer Aufruf nicht in die
        /// Beziehung läuft.
        /// </summary>
        public bool Delete(string szName)
        {
            ReferenzenLoesen(BetroffeneIds(
                "SELECT ID FROM Tab_Pufferspeicher WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szName ?? (object)DBNull.Value)));

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

        /// <summary>
        /// Liefert die Projekt-ID (Tab_Pufferspeicher.ID) eines Bezeichners im Projekt, oder 0.
        ///
        /// Paket 2 / Konzept 5.2: Seit der Dedup-Aufhebung darf es MEHRERE Projektzeilen
        /// gleichen Bezeichners geben (Mehrfachanlage desselben Katalogtyps, E7). Die
        /// bezeichnerbasierten Altpfade (<c>Z_ProjektPufferSpCtrl.Insert</c>,
        /// <c>PufferSpCtrl.CopyFromStamm</c>, <c>WaermequelleClass.Quellspeicher</c>)
        /// brauchen trotzdem ein eindeutiges Ergebnis. <c>MIN(ID)</c> macht die Auswahl
        /// deterministisch und trifft dieselbe Zeile wie die übrigen Altpfade
        /// (<c>PufferSpCtrl.PendelspeicherId</c>: <c>TOP 1 … ORDER BY ID</c>, Migration R6).
        ///
        /// Ohne diese Festlegung entschiede die Datenbankreihenfolge — genau der stille
        /// Datenfehler, den Konzept 3.4 an anderer Stelle ausdrücklich ausschließt.
        /// Die Verwaltung (4.3) hängt bei Namensgleichheit deshalb zusätzlich ein Suffix
        /// an, siehe <see cref="EindeutigerBezeichner"/>.
        /// </summary>
        public int GetProjektId(string szBezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM Tab_Pufferspeicher WHERE Bezeichner = ? AND ID_Projekt = ?",
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
            // Die Beziehungen aus Schritt 4 der SchemaMigration sind RESTRIKTIV (kein
            // DEL-CASCADE, siehe SchemaMigration.Schritt_4_Beziehungen). Ohne das
            // vorherige Loesen der Referenzen wuerde Access das DELETE ablehnen.
            ReferenzenLoesen(BetroffeneIds(
                "SELECT ID FROM Tab_Pufferspeicher WHERE Bezeichner = ? AND ID_Projekt = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""),
                new OleDbParameter("@idProj", idProjekt)));

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
            string filter = @"ID_Projekt = ?
                              AND Bezeichner NOT IN (SELECT Bezeichner FROM Tab_Energieanlagen
                                                     WHERE ID_Projekt = ? AND ID_Type = " +
                                                     WizardItemClass.PUFFER_TYP + ")";

            // Erst die Referenzen loesen (restriktive Beziehungen aus Schritt 4 der
            // SchemaMigration), dann loeschen. Heute liest kein Engine-Code die
            // WS_*/WQ_*-Spalten; das Nullen ist deshalb verhaltensneutral.
            ReferenzenLoesen(BetroffeneIds(
                "SELECT ID FROM Tab_Pufferspeicher WHERE " + filter,
                new OleDbParameter("@idProj", idProjekt),
                new OleDbParameter("@idProj2", idProjekt)));

            return DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Pufferspeicher WHERE " + filter,
                new OleDbParameter("@idProj", idProjekt),
                new OleDbParameter("@idProj2", idProjekt));
        }

        // --- Projekt-Puffer-Verwaltung (Paket 2, Konzept 4.3 / 5.2) ------------------

        /// <summary>
        /// EXPLIZITE Übernahme aus dem Katalog: legt IMMER eine neue Projektzeile an
        /// (Konzept 4.3, Punkt 4 und 5.2 „Mehrfachanlage desselben Katalogtyps ist
        /// zulässig", E7).
        ///
        /// Bewusst getrennt von <see cref="CopyFromStamm(int,int)"/>: der Altpfad wird
        /// implizit aus <c>Z_ProjektPufferSpCtrl.Insert</c> heraus bei JEDEM Speichern der
        /// Konfiguration gerufen. Würde dort die Dedup-Prüfung entfallen, entstünde bei
        /// jedem Speichern ein weiterer Duplikat-Puffer (Befund aus Paket 1). Die
        /// Aufhebung gilt deshalb nur hier — im Pfad, den der Anwender ausdrücklich
        /// auslöst.
        ///
        /// Bei Namensgleichheit hängt <see cref="EindeutigerBezeichner"/> ein Suffix an,
        /// damit die verbleibenden bezeichnerbasierten Altpfade eindeutig bleiben.
        /// </summary>
        /// <returns>ID der neuen Projektzeile, -1 bei Fehler.</returns>
        public int CopyFromStammNeu(int stammId, int idProjekt, string verwendung,
                                    int? vorlauf = null, int? ruecklauf = null)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + PufferSpStammCtrl.TABLE + "] WHERE ID = ?",
                new OleDbParameter("@id", stammId));

            if (dt == null || dt.Rows.Count == 0)
            {
                Console.WriteLine("Pufferspeicher-Stammdatensatz nicht gefunden (ID " + stammId + ").");
                return -1;
            }

            DataRow s = dt.Rows[0];

            return ProjektPufferAnlegen(
                idProjekt,
                s["Bezeichner"].ToString(),
                Text(ColOrNull(s, "Hersteller")),
                Text(ColOrNull(s, "Speichertyp")),
                StilleDb.Zahl(ColOrNull(s, "Gesamtvolumen")),
                StilleDb.Kommazahl(ColOrNull(s, "Bereitschaftsverluste")),
                StilleDb.Kommazahl(ColOrNull(s, "Investitionskosten")),
                verwendung,
                vorlauf ?? SystemVorlauf(idProjekt),
                ruecklauf ?? SystemRuecklauf(idProjekt),
                ProjektPuffer.SCHWELLE_EIN_DEFAULT,
                ProjektPuffer.SCHWELLE_AUS_DEFAULT,
                ProjektPuffer.SCHWELLE_AUS_DEFAULT,
                0);
        }

        /// <summary>
        /// Legt einen Projekt-Pufferspeicher an — Puffer-Zeile UND Anlagenzeile
        /// (<c>ID_Type = 12</c>), wie es die Konsistenzregel aus Konzept 5.2 verlangt:
        /// „Beim Anlegen eines Puffers über die Verwaltung wird zusätzlich die
        /// Anlagenzeile geschrieben, damit der Projektbaum den Puffer weiterhin zeigt."
        ///
        /// Der Bezeichner wird über <see cref="EindeutigerBezeichner"/> geführt.
        /// </summary>
        /// <returns>ID des neuen Puffers, -1 bei Fehler.</returns>
        public static int ProjektPufferAnlegen(int idProjekt, string bezeichner, string hersteller,
                                               string speichertyp, int volumenLiter, double verluste,
                                               double investitionskosten, string verwendung,
                                               int? vorlauf, int? ruecklauf,
                                               double schwelleEin, double schwelleAus,
                                               double schwelleAusNachrang, int entladeprio)
        {
            if (idProjekt <= 0 || string.IsNullOrEmpty(bezeichner)) return -1;

            string name = EindeutigerBezeichner(idProjekt, bezeichner, 0);

            // Tab_Pufferspeicher.ID ist kein AutoWert (Muster CopyFromStamm).
            int neueId = StilleDb.Zahl(StilleDb.Scalar("SELECT MAX(ID) FROM Tab_Pufferspeicher")) + 1;
            if (neueId <= 0) return -1;

            if (StilleDb.NonQuery(ProjektPuffer.SQL_PUFFER_INSERT_VOLL,
                                  ProjektPuffer.PufferParameterVoll(
                                      neueId, idProjekt, name, hersteller, speichertyp,
                                      volumenLiter, verluste, investitionskosten, verwendung,
                                      vorlauf, ruecklauf,
                                      schwelleEin, schwelleAus, schwelleAusNachrang, entladeprio)) < 0)
                return -1;

            // Anlagenzeile nachtragen - eine je Projekt + Bezeichner (Regel R4 der Migration)
            if (!AnlagenzeileVorhanden(idProjekt, name))
                StilleDb.NonQuery(ProjektPuffer.SQL_ANLAGENZEILE_INSERT,
                                  ProjektPuffer.AnlagenzeileParameter(idProjekt, name, neueId));

            return neueId;
        }

        /// <summary>Ändert einen vorhandenen Projekt-Puffer (Konzept 4.3).</summary>
        public static bool ProjektPufferAendern(int idPuffer, int idProjekt, string bezeichner,
                                                string hersteller, string speichertyp, int volumenLiter,
                                                double verluste, double investitionskosten,
                                                string verwendung, int? vorlauf, int? ruecklauf,
                                                double schwelleEin, double schwelleAus,
                                                double schwelleAusNachrang, int entladeprio)
        {
            if (idPuffer <= 0 || string.IsNullOrEmpty(bezeichner)) return false;

            string alterName = StilleDb.Text(StilleDb.Scalar(
                "SELECT Bezeichner FROM Tab_Pufferspeicher WHERE ID = ?",
                StilleDb.Par("@id", OleDbType.Integer, idPuffer)));

            string name = string.Equals(alterName, bezeichner, StringComparison.Ordinal)
                ? bezeichner
                : EindeutigerBezeichner(idProjekt, bezeichner, idPuffer);

            if (StilleDb.NonQuery(ProjektPuffer.SQL_PUFFER_UPDATE_VOLL,
                                  ProjektPuffer.PufferParameterVollUpdate(
                                      idPuffer, name, hersteller, speichertyp, volumenLiter,
                                      verluste, investitionskosten, verwendung,
                                      vorlauf, ruecklauf,
                                      schwelleEin, schwelleAus, schwelleAusNachrang, entladeprio)) < 0)
                return false;

            // Die Anlagenzeile führt denselben Bezeichner - sonst greift
            // ProjektWaisenEntfernen zu (Abgleich läuft über den Namen).
            if (!string.Equals(alterName, name, StringComparison.Ordinal) && alterName.Length > 0)
            {
                StilleDb.NonQuery(
                    "UPDATE Tab_Energieanlagen SET Bezeichner = ? " +
                    "WHERE ID_Projekt = ? AND ID_Type = ? AND Bezeichner = ?",
                    StilleDb.Par("@neu", OleDbType.VarWChar, name),
                    StilleDb.Par("@proj", OleDbType.Integer, idProjekt),
                    StilleDb.Par("@typ", OleDbType.Integer, ProjektPuffer.TYP_PUFFER),
                    StilleDb.Par("@alt", OleDbType.VarWChar, alterName));

                // Und die Alt-Zuordnung: Z_ProjektPufferSp.Pufferspeicher ist eine
                // TEXTreferenz. Bleibt dort der alte Name stehen, legt das nächste
                // "Speichern" einen DUPLIKAT-PUFFER an - Z_ProjektPufferSpCtrl.Insert
                // löst den Namen über GetProjektId auf, findet ihn nach dem Umbenennen
                // nicht mehr und ruft CopyFromStamm, das eine zweite Projektkopie unter
                // dem ALTEN Namen erzeugt. Reproduziert im Review zu Paket 2.
                //
                // Schlüssel ist die ID_Pufferspeicher, nicht der Name: sie ist seit der
                // Migration Pflichtspalte mit erzwungener Beziehung und trifft genau die
                // Zeilen dieses Speichers - gleichnamige Zeilen anderer Speicher bleiben
                // unangetastet.
                StilleDb.NonQuery(
                    "UPDATE Z_ProjektPufferSp SET Pufferspeicher = ? WHERE ID_Pufferspeicher = ?",
                    StilleDb.Par("@neu", OleDbType.VarWChar, name),
                    StilleDb.Par("@id", OleDbType.Integer, idPuffer));
            }

            return true;
        }

        /// <summary>
        /// Ein im Projekt noch nicht vergebener Bezeichner: „PS 800", „PS 800 (2)", …
        ///
        /// Die Dedup-Aufhebung aus Konzept 5.2 erlaubt mehrere baugleiche Puffer je
        /// Projekt. Gleiche NAMEN darf es trotzdem nicht geben: <c>GetProjektId</c>,
        /// <c>Z_ProjektPufferSp.Pufferspeicher</c>, <c>WaermequelleClass.Quellspeicher</c>
        /// und <c>ProjektWaisenEntfernen</c> lösen weiterhin über den Bezeichner auf. Das
        /// Suffix hält diese Altpfade eindeutig, ohne die Mehrfachanlage zu verhindern.
        /// </summary>
        /// <param name="idAusnahme">Puffer-ID, die beim Namensvergleich übergangen wird (Ändern).</param>
        public static string EindeutigerBezeichner(int idProjekt, string wunsch, int idAusnahme)
        {
            string basis = (wunsch ?? "").Trim();
            if (basis.Length == 0) basis = "Pufferspeicher";

            for (int n = 1; n < 1000; n++)
            {
                string kandidat = n == 1 ? basis : basis + " (" + n + ")";

                int treffer = StilleDb.Zahl(StilleDb.Scalar(
                    "SELECT COUNT(*) FROM Tab_Pufferspeicher " +
                    "WHERE ID_Projekt = ? AND Bezeichner = ? AND ID <> ?",
                    StilleDb.Par("@proj", OleDbType.Integer, idProjekt),
                    StilleDb.Par("@bez", OleDbType.VarWChar, kandidat),
                    StilleDb.Par("@aus", OleDbType.Integer, idAusnahme)));

                if (treffer == 0) return kandidat;
            }

            return basis + " (" + DateTime.Now.ToString("HHmmss") + ")";
        }

        /// <summary>
        /// Anlagen, die den Puffer als Quelle oder Senke referenzieren — Grundlage für
        /// die Blockade beim Entfernen (Konzept 5.2, Konsistenzregel). Je Treffer ein
        /// fertiger Anzeigetext.
        /// </summary>
        public static List<string> ReferenzenAufPuffer(int idPuffer)
        {
            List<string> treffer = new List<string>();
            if (idPuffer <= 0) return treffer;

            DataTable dt = StilleDb.Tabelle(
                "SELECT Bezeichner, ID_Type, WS_ID_Puffer, WS_ID_Puffer2, WQ_ID_Puffer " +
                "FROM Tab_Energieanlagen " +
                "WHERE WS_ID_Puffer = ? OR WS_ID_Puffer2 = ? OR WQ_ID_Puffer = ? " +
                "ORDER BY Bezeichner",
                StilleDb.Par("@a", OleDbType.Integer, idPuffer),
                StilleDb.Par("@b", OleDbType.Integer, idPuffer),
                StilleDb.Par("@c", OleDbType.Integer, idPuffer));
            if (dt == null) return treffer;

            foreach (DataRow r in dt.Rows)
            {
                string bezeichner = StilleDb.Text(StilleDb.Feld(r, "Bezeichner"));
                string erzeuger = Ladeordnung.ErzeugerName(StilleDb.Zahl(StilleDb.Feld(r, "ID_Type")));

                List<string> rollen = new List<string>();
                if (StilleDb.Zahl(StilleDb.Feld(r, "WS_ID_Puffer")) == idPuffer) rollen.Add("Hauptsenke");
                if (StilleDb.Zahl(StilleDb.Feld(r, "WS_ID_Puffer2")) == idPuffer) rollen.Add("Zweitsenke");
                if (StilleDb.Zahl(StilleDb.Feld(r, "WQ_ID_Puffer")) == idPuffer) rollen.Add("Wärmequelle");

                treffer.Add(bezeichner + " (" + erzeuger + ") - " + string.Join(", ", rollen));
            }

            return treffer;
        }

        /// <summary>
        /// Entfernt einen Projekt-Pufferspeicher samt Anlagenzeile (Konzept 5.2).
        ///
        /// Der Aufrufer muss vorher über <see cref="ReferenzenAufPuffer"/> prüfen und mit
        /// Hinweis blockieren; hier wird nur noch geräumt. <c>ReferenzenLoesen</c> läuft
        /// trotzdem mit — die Beziehungen aus Schritt 4 der SchemaMigration sind
        /// RESTRIKTIV, und ein Rest-Verweis (etwa das alte <c>ID_PUFFER</c> der eigenen
        /// Anlagenzeile) ließe das DELETE sonst scheitern.
        /// </summary>
        public static bool ProjektPufferEntfernen(int idPuffer, int idProjekt)
        {
            if (idPuffer <= 0 || idProjekt <= 0) return false;

            string bezeichner = StilleDb.Text(StilleDb.Scalar(
                "SELECT Bezeichner FROM Tab_Pufferspeicher WHERE ID = ?",
                StilleDb.Par("@id", OleDbType.Integer, idPuffer)));

            // Alt-Zuordnungen dieses Speichers zuerst - sie hängen über
            // Z_ProjektPufferSp.ID_Pufferspeicher am Puffer.
            StilleDb.NonQuery("DELETE FROM Z_ProjektPufferSp WHERE ID_Pufferspeicher = ?",
                              StilleDb.Par("@id", OleDbType.Integer, idPuffer));

            // Anlagenzeile (ID_Type = 12) des Speichers
            if (bezeichner.Length > 0)
                StilleDb.NonQuery(
                    "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? AND Bezeichner = ?",
                    StilleDb.Par("@proj", OleDbType.Integer, idProjekt),
                    StilleDb.Par("@typ", OleDbType.Integer, ProjektPuffer.TYP_PUFFER),
                    StilleDb.Par("@bez", OleDbType.VarWChar, bezeichner));

            ReferenzenLoesen(new List<int> { idPuffer });

            bool ok = StilleDb.NonQuery("DELETE FROM Tab_Pufferspeicher WHERE ID = ?",
                                        StilleDb.Par("@id", OleDbType.Integer, idPuffer)) >= 0;

            // Waisen aufräumen (B0-6a) - Projektkopien ohne Anlagenzeile
            new PufferSpCtrl().ProjektWaisenEntfernen(idProjekt);
            return ok;
        }

        private static string Text(object o)
        {
            return (o == null || o == DBNull.Value) ? "" : o.ToString();
        }

        // --- Systemvorgaben und Betriebstemperaturen (Etappe 4, 14.08.2026) ----------

        /// <summary>
        /// Vorlauftemperatur-Vorgabe des Projekts [°C]: der KLEINSTE Vorlauf über alle
        /// Wärmeerzeuger-Anlagen (Wärmepumpe, Solarthermie, Heizkessel, BHKW) - die
        /// konservative Auslegung für einen gemeinsamen Speicher (Konzept 13.7).
        ///
        /// <c>null</c>, wenn im Projekt keine Anlage einen gepflegten Vorlauf trägt.
        /// Dann bleibt die Vorbelegung leer, statt eine Zahl zu erfinden.
        ///
        /// Still wie die übrigen Engine-nahen Methoden (nur Console.WriteLine): die
        /// Vorbelegung läuft auch aus der Migration heraus, und die zeigt keine Dialoge.
        /// </summary>
        public static int? SystemVorlauf(int idProjekt)
        {
            return SystemTemperatur(idProjekt, ProjektPuffer.SQL_SYSTEM_VORLAUF);
        }

        /// <summary>
        /// Rücklauftemperatur-Vorgabe des Projekts [°C]: der GRÖSSTE Rücklauf über
        /// dieselben Anlagen. Gegenstück zu <see cref="SystemVorlauf"/>.
        /// </summary>
        public static int? SystemRuecklauf(int idProjekt)
        {
            return SystemTemperatur(idProjekt, ProjektPuffer.SQL_SYSTEM_RUECKLAUF);
        }

        private static int? SystemTemperatur(int idProjekt, string sql)
        {
            if (idProjekt <= 0) return null;

            object v = StillScalar(sql, ProjektPuffer.SystemTemperaturParameter(idProjekt));
            if (v == null || v == DBNull.Value) return null;

            try { return Convert.ToInt32(v); }
            catch { return null; }
        }

        /// <summary>
        /// Betriebstemperaturen einer Puffer-Zeile. Seit Etappe 4 ist der Puffer die
        /// FÜHRENDE Ablage (Konzept 5.1) - diese Methode ist der erste Griff aller
        /// Leser, die Zuordnung nur noch der Rückfallweg.
        ///
        /// Liefert nur dann <c>true</c>, wenn BEIDE Werte gesetzt und &gt; 0 sind: eine
        /// halbe Angabe ergibt keine auswertbare Spreizung. Ein Rückgabewert
        /// <c>false</c> bedeutet also "am Puffer steht nichts Brauchbares - nimm den
        /// Rückfallweg", nicht "Fehler".
        ///
        /// Ausdrücklich OHNE Untergrenze: 35/28 (Niedertemperatur) ist ein gültiges
        /// Paar und wird unverändert durchgereicht.
        /// </summary>
        public static bool TemperaturenLesen(int idPuffer, out int vorlauf, out int ruecklauf)
        {
            vorlauf = 0;
            ruecklauf = 0;
            if (idPuffer <= 0) return false;

            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(ProjektPuffer.SQL_PUFFER_TEMPERATUREN, conn))
                    {
                        cmd.Parameters.Add(new OleDbParameter("@id", idPuffer));
                        using (OleDbDataReader r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) return false;
                            if (r.IsDBNull(0) || r.IsDBNull(1)) return false;

                            vorlauf = Convert.ToInt32(r.GetValue(0));
                            ruecklauf = Convert.ToInt32(r.GetValue(1));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fehlende Spalten (Datenbank noch nicht migriert) landen hier - das ist
                // kein Fehlerfall, sondern genau der Grund fuer den Rueckfallweg.
                Console.WriteLine("Puffertemperaturen nicht lesbar: " + ex.Message);
                vorlauf = 0;
                ruecklauf = 0;
                return false;
            }

            return vorlauf > 0 && ruecklauf > 0;
        }

        /// <summary>
        /// Schreibt die Betriebstemperaturen an die Puffer-Zeile. Geschrieben wird nur
        /// ein Paar, das als Betriebsvorgabe taugt - geprueft ueber
        /// <see cref="ProjektPuffer.IstTemperaturpaar"/> (beide gesetzt, Ruecklauf &gt; 0,
        /// Vorlauf &gt; Ruecklauf). Sonst bleibt der Bestand stehen und der Rueckfallweg
        /// greift weiter.
        ///
        /// Das blosse "&gt; 0" reichte nicht: ein vertauschtes Paar wie 35/45 (Bestand,
        /// Projekt 1008) haette den Test bestanden, am Speicher aber eine Spreizung
        /// &lt;= 0 hinterlassen - der Speicher saehe gepflegt aus und faende doch nur den
        /// stillen Rueckfall auf die Engine-Vorgabe.
        ///
        /// Bewusst OHNE fachliche UNTERgrenze: die Plausibilitaet der Eingabe gehoert an
        /// die Oberflaeche (ProjektPuffer.TemperaturenPruefen). Hier darf deshalb auch
        /// 35/28 landen.
        /// </summary>
        public static bool SetTemperaturen(int idPuffer, int vorlauf, int ruecklauf)
        {
            if (idPuffer <= 0) return false;
            if (!ProjektPuffer.IstTemperaturpaar(vorlauf, ruecklauf)) return false;

            return StillNonQuery(ProjektPuffer.SQL_PUFFER_TEMPERATUREN_UPDATE,
                                 new OleDbParameter("@vor", vorlauf),
                                 new OleDbParameter("@rueck", ruecklauf),
                                 new OleDbParameter("@id", idPuffer)) >= 0;
        }

        /// <summary>
        /// Setzt Vorlauf und Ruecklauf der Puffer-Zeile auf NULL - die RUECKNAHME einer
        /// Vorgabe (Etappe 4 / Review-Nacharbeit).
        ///
        /// Leert der Anwender die Temperaturzellen, darf am Speicher kein alter Wert
        /// stehen bleiben: die Puffer-Zeile ist seit Etappe 4 die FUEHRENDE Ablage, ein
        /// zurueckgebliebenes Paar wuerde die Zuordnung dauerhaft verdecken. Mit NULL
        /// faellt die Engine geordnet auf Stufe 2 (Zuordnung) und Stufe 3
        /// (Engine-Vorgabe 10 K) zurueck.
        ///
        /// Bewusst getrennt von <see cref="SetTemperaturen"/>: dort ist "unbrauchbares
        /// Paar" ein Grund, NICHTS zu tun; hier ist das Leeren die Absicht.
        /// </summary>
        public static bool TemperaturenLoeschen(int idPuffer)
        {
            if (idPuffer <= 0) return false;

            return StillNonQuery(ProjektPuffer.SQL_PUFFER_TEMPERATUREN_UPDATE,
                                 ProjektPuffer.Par("@vor", OleDbType.Integer, DBNull.Value),
                                 ProjektPuffer.Par("@rueck", OleDbType.Integer, DBNull.Value),
                                 new OleDbParameter("@id", idPuffer)) >= 0;
        }

        // --- BHKW-Pendelspeicher (Etappe 3, 14.08.2026) ------------------------------

        /// <summary>
        /// Volumen des BHKW-Pendelspeichers eines Projekts in LITERN; 0, wenn es im
        /// Projekt keinen Puffer mit diesem Bezeichner gibt.
        ///
        /// Das ist seit Etappe 3 die EINZIGE Quelle - fuer die Engine
        /// (SimulationRunner, Form_Simulation_Detail) wie fuer die Eingabe. Der
        /// Alt-Parameter Tab_Einstellungen.Pendelspeicher (m3) wird nirgends mehr
        /// gelesen; die Spalte bleibt physisch bestehen, ihr Wert ist bedeutungslos.
        ///
        /// Gelesen wird die Zeile mit der kleinsten ID - dieselbe Auswahl, mit der
        /// SchemaMigration R6 einen vorhandenen Puffer wiederverwendet.
        ///
        /// Bewusst still (nur Console.WriteLine, kein Dialog): die Methode haengt im
        /// Engine-Pfad, und der bleibt dialogfrei (Konzept 13.4).
        /// </summary>
        public static int PendelspeicherVolumenLiter(int idProjekt)
        {
            if (idProjekt <= 0) return 0;

            object v = StillScalar(
                "SELECT TOP 1 Gesamtvolumen FROM Tab_Pufferspeicher " +
                "WHERE ID_Projekt = ? AND Bezeichner = ? ORDER BY ID",
                new OleDbParameter("@idProj", idProjekt),
                new OleDbParameter("@bez", ProjektPuffer.BEZ_PENDELSPEICHER));

            if (v == null || v == DBNull.Value) return 0;
            try { return Convert.ToInt32(v); }
            catch { return 0; }
        }

        /// <summary>
        /// Setzt das Volumen des BHKW-Pendelspeichers in LITERN:
        ///
        ///   - Zeile vorhanden          -> Gesamtvolumen aktualisieren. Das gilt auch
        ///     fuer 0: die Zeile bleibt stehen, damit ein bewusst geleerter Speicher
        ///     nicht heimlich verschwindet (und mit ihm seine Betriebsparameter).
        ///   - keine Zeile, liter &gt; 0 -> Puffer, Anlagenzeile (ID_Type = 12) und
        ///     BHKW-Senke exakt nach dem Muster von SchemaMigration R6 anlegen. Die
        ///     Bausteine dafuer stehen in ProjektPuffer, nicht hier nachgebaut.
        ///   - keine Zeile, liter = 0   -> nichts zu tun.
        ///
        /// Negative Werte werden abgelehnt (Rueckgabe false, nichts geschrieben).
        /// Still wie <see cref="PendelspeicherVolumenLiter"/>: der Aufrufer entscheidet,
        /// ob er den Fehlschlag anzeigt.
        /// </summary>
        public static bool SetPendelspeicherVolumenLiter(int idProjekt, int liter)
        {
            if (idProjekt <= 0 || liter < 0) return false;

            int idPuffer = PendelspeicherId(idProjekt);

            if (idPuffer > 0)
            {
                return StillNonQuery(
                    "UPDATE Tab_Pufferspeicher SET Gesamtvolumen = ? WHERE ID = ?",
                    new OleDbParameter("@vol", liter),
                    new OleDbParameter("@id", idPuffer)) >= 0;
            }

            if (liter == 0) return true;

            // Tab_Pufferspeicher.ID ist kein AutoWert (Muster CopyFromStamm).
            object max = StillScalar("SELECT MAX(ID) FROM Tab_Pufferspeicher");
            int neueId = 1;
            if (max != null && max != DBNull.Value)
            {
                try { neueId = Convert.ToInt32(max) + 1; }
                catch { return false; }
            }

            // Etappe 4: Der neue Puffer bekommt die SYSTEMVORGABEN des Projekts als
            // Vorbelegung mit. Fehlen sie, bleiben beide Spalten NULL - eine erfundene
            // Vorbelegung (etwa 70/50) waere bei einem Niedertemperatursystem falsch.
            //
            // Heute ergebnisneutral: die Kapazitaet des Pendelspeichers rechnet
            // SimulationControl weiterhin mit fest 20 K. Erst Paket 6 zieht sie aus
            // SimulationPufferspeicher - dann bestimmt die hier abgelegte Spreizung
            // die Kapazitaet.
            if (StillNonQuery(ProjektPuffer.SQL_PUFFER_INSERT,
                              ProjektPuffer.PufferParameter(neueId, idProjekt,
                                                            ProjektPuffer.BEZ_PENDELSPEICHER,
                                                            liter,
                                                            SystemVorlauf(idProjekt),
                                                            SystemRuecklauf(idProjekt))) < 0)
                return false;

            // Anlagenzeile nachtragen, damit der Speicher im Projektbaum erscheint -
            // dieselbe Regel wie R4 der Migration (eine Zeile je Projekt+Bezeichner).
            if (!AnlagenzeileVorhanden(idProjekt, ProjektPuffer.BEZ_PENDELSPEICHER))
                StillNonQuery(ProjektPuffer.SQL_ANLAGENZEILE_INSERT,
                              ProjektPuffer.AnlagenzeileParameter(idProjekt,
                                                                  ProjektPuffer.BEZ_PENDELSPEICHER,
                                                                  neueId));

            // BHKW-Anlagen des Projekts auf die neue Senke (R6). Heute noch ohne
            // Wirkung auf das Rechenergebnis - die Engine liest WS_Ziel erst in Paket 2.
            StillNonQuery(ProjektPuffer.SQL_BHKW_AUF_PUFFER,
                          ProjektPuffer.BhkwAufPufferParameter(idProjekt, neueId));

            return true;
        }

        /// <summary>
        /// ID des Pendelspeichers eines Projekts (kleinste), 0 wenn keiner.
        ///
        /// Seit Paket 6 auch von der Engine gelesen: Hat ein BHKW keine Puffer-Senke,
        /// aber ein Pendelspeichervolumen, baut <c>SimulationControl</c> daraus den
        /// Ersatzspeicher (Konzept 6.5, zweiter Punkt).
        /// </summary>
        public static int PendelspeicherId(int idProjekt)
        {
            object v = StillScalar(
                "SELECT TOP 1 ID FROM Tab_Pufferspeicher " +
                "WHERE ID_Projekt = ? AND Bezeichner = ? ORDER BY ID",
                new OleDbParameter("@idProj", idProjekt),
                new OleDbParameter("@bez", ProjektPuffer.BEZ_PENDELSPEICHER));

            if (v == null || v == DBNull.Value) return 0;
            try { return Convert.ToInt32(v); }
            catch { return 0; }
        }

        private static bool AnlagenzeileVorhanden(int idProjekt, string bezeichner)
        {
            object v = StillScalar(
                "SELECT COUNT(*) FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type = ? AND Bezeichner = ?",
                new OleDbParameter("@idProj", idProjekt),
                new OleDbParameter("@typ", ProjektPuffer.TYP_PUFFER),
                new OleDbParameter("@bez", bezeichner ?? ""));

            if (v == null || v == DBNull.Value) return false;
            try { return Convert.ToInt32(v) > 0; }
            catch { return false; }
        }

        /// <summary>
        /// Skalare Abfrage auf eigener Verbindung, OHNE Dialog. DataRepository zeigt im
        /// Fehlerfall eine MessageBox - im Engine-Pfad waere das ein haengender Lauf
        /// (der Referenzlauf braucht dafuer eigens einen Dialogwaechter).
        /// </summary>
        private static object StillScalar(string sql, params OleDbParameter[] parameter)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        if (parameter != null) cmd.Parameters.AddRange(parameter);
                        return cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Pufferspeicher-Abfrage fehlgeschlagen: " + ex.Message);
                return null;
            }
        }

        /// <summary>Schreibende Anweisung ohne Dialog; -1 bei Fehler.</summary>
        private static int StillNonQuery(string sql, params OleDbParameter[] parameter)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        if (parameter != null) cmd.Parameters.AddRange(parameter);
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Schreibzugriff auf Pufferspeicher fehlgeschlagen: " + ex.Message);
                return -1;
            }
        }

        // --- Referenzen auf Projekt-Pufferspeicher (restriktive Beziehungen) ---------

        /// <summary>
        /// Spalten in Tab_Energieanlagen, die auf Tab_Pufferspeicher.ID zeigen
        /// (Konzept 5.3). Alle vier haben seit Schritt 4 der SchemaMigration eine
        /// erzwungene Beziehung OHNE Loeschweitergabe.
        /// </summary>
        private static readonly string[] PUFFER_REFERENZEN =
            { "ID_PUFFER", "WS_ID_Puffer", "WS_ID_Puffer2", "WQ_ID_Puffer" };

        /// <summary>
        /// Löst alle Anlagen-Verweise auf die Projekt-Pufferspeicher EINES PROJEKTS.
        ///
        /// Vor dem Löschen eines Projekts aufzurufen (B0-6b): die Puffer-Projektkopien
        /// selbst fallen über die Löschweitergabe
        /// <c>Tab_Projekt.ID -&gt; Tab_Pufferspeicher.ID_Projekt</c> weg. Die vier
        /// Anlagen-Referenzen auf <c>Tab_Pufferspeicher.ID</c> sind dagegen restriktiv -
        /// zeigt beim Projekt-DELETE noch eine Anlage auf einen dieser Puffer, lehnt
        /// Access die ganze Kaskade ab.
        ///
        /// Die Anlagen des Projekts werden von den Löschpfaden zwar ohnehin vorher
        /// entfernt (<c>WErzeugerCtrl.Delete</c>) - aber genau diese Reihenfolge soll
        /// keine Voraussetzung sein. Deshalb steht der Aufruf zentral in
        /// <c>ProjektCtrl.Delete</c> und nicht in den Aufrufern.
        /// </summary>
        public static void ReferenzenLoesenFuerProjekt(int idProjekt)
        {
            if (idProjekt <= 0) return;

            ReferenzenLoesen(BetroffeneIds(
                "SELECT ID FROM Tab_Pufferspeicher WHERE ID_Projekt = ?",
                new OleDbParameter("@idProj", idProjekt)));
        }

        /// <summary>Liefert die IDs der Puffer-Zeilen, die ein Filter trifft.</summary>
        private static List<int> BetroffeneIds(string sql, params OleDbParameter[] parameter)
        {
            List<int> ids = new List<int>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        if (parameter != null) cmd.Parameters.AddRange(parameter);
                        using (OleDbDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                                if (!r.IsDBNull(0)) ids.Add(Convert.ToInt32(r.GetValue(0)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Betroffene Pufferspeicher konnten nicht ermittelt werden: " + ex.Message);
            }
            return ids;
        }

        /// <summary>
        /// Setzt alle Verweise auf die uebergebenen Puffer-IDs auf NULL, damit das
        /// anschliessende DELETE nicht an der restriktiven Beziehung scheitert.
        /// Still: fehlt eine der Spalten (Datenbank noch nicht migriert), wird der
        /// Fehler uebergangen.
        /// </summary>
        private static void ReferenzenLoesen(List<int> pufferIds)
        {
            if (pufferIds == null || pufferIds.Count == 0) return;

            string liste = string.Join(",", pufferIds);
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    foreach (string spalte in PUFFER_REFERENZEN)
                    {
                        try
                        {
                            using (OleDbCommand cmd = new OleDbCommand(
                                "UPDATE Tab_Energieanlagen SET [" + spalte + "] = NULL " +
                                "WHERE [" + spalte + "] IN (" + liste + ")", conn))
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }
                        catch (Exception ex)
                        {
                            // z. B. Spalte noch nicht angelegt - kein Grund, das Loeschen zu stoppen
                            Console.WriteLine("Referenz " + spalte + " nicht geloest: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Referenzen auf Pufferspeicher konnten nicht geloest werden: " + ex.Message);
            }
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
