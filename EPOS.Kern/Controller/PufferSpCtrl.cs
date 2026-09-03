using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;

namespace WindowsFormsApplication1
{
    // =====================================================================================
    // ARBEITSPAKET iU6-T2: DER TOTE OleDb-ZWEIG IST GESTRICHEN.
    //
    // Bis hierher trug diese Klasse ein eigenes "public OleDbCommand DBCommand" samt
    // lazy angelegtem Feld, Finalizer sowie Delete(string) und Update(), die dieses
    // Kommando fuellten und "DBCommand.ExecuteNonQuery()" aufriefen. Das war seit der
    // SQLite-Umstellung (6486c36) TOTER CODE:
    //
    //   - Delete(string) und Update() hatten 0 Aufrufer. Alle fuenf Instanzen der Klasse
    //     lesen, kopieren oder raeumen auf: PufferSpKontextMenuCtrl.cs:98 und :142
    //     (ProjektWaisenEntfernen), WizardCtrl.cs:968 (CopyFromStamm), FormMain.cs:1207
    //     (ReadAll) und Form_Start.cs:1807 (ProjektWaisenEntfernen).
    //   - Das DBCommand bekam nie eine Verbindung; niemand griff von aussen darauf zu.
    //     Auf Windows waere ExecuteNonQuery() also in die InvalidOperationException und
    //     damit in den catch-Zweig gelaufen - stilles "return false".
    //
    // Geschrieben wird der Pufferspeicherkatalog ueber PufferSpStammCtrl (Form_PufferSp,
    // Form_PufferSp_Admin); der ist OleDb-frei und laeuft ueber DataRepository/DbParam.
    // Die Projektseite schreibt ueber ProjektPufferAnlegen/-Aendern/-Entfernen. Ein
    // Ersatz fuer Delete(string)/Update() war deshalb nicht noetig.
    //
    // Der Referenzvorlauf, den Delete(string) vor dem DELETE fuhr (ReferenzenLoesen ueber
    // BetroffeneIds), bleibt unangetastet - DeleteFromProjekt und
    // ProjektWaisenEntfernen benutzen ihn weiterhin.
    // =====================================================================================

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

        public PufferSpModel model;

        public PufferSpCtrl()
        {
            model = new PufferSpModel();
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

                    // KLASSEN-SET (Migrationsschritt 49, Konzept 6.1). Spaltentolerant
                    // mit Rueckfall auf Verwendung - siehe KlassenSetAusZeile.
                    KlassenSet ks = KlassenSetAusZeile(r);
                    item.Nutzung_Heizung = ks.Heizung;
                    item.Nutzung_Brauchwasser = ks.Brauchwasser;
                    item.Nutzung_Prozess = ks.Prozess;

                    // SCHICHTUNG (Migrationsschritt 53, Konzept 7.2). Ebenso
                    // spaltentolerant - fehlen die Spalten, bleibt es beim
                    // verhaltensneutralen Ein-Zonen-Modell.
                    SchichtdatenAusZeile(r).NachModell(item);

                    _internalList.Add(item);
                }
            }
        }

        // --- STAMM -> PROJEKT KOPIE (analog HeizkesselCtrl/BHKWCtrl) ---

        /// <summary>
        /// Liefert die Projekt-ID (Tab_Pufferspeicher.ID) eines Bezeichners im Projekt, oder 0.
        ///
        /// Paket 2 / Konzept 5.2: Seit der Dedup-Aufhebung darf es MEHRERE Projektzeilen
        /// gleichen Bezeichners geben (Mehrfachanlage desselben Katalogtyps, E7). Die
        /// bezeichnerbasierten Altpfade (<c>PufferSpCtrl.CopyFromStamm</c>,
        /// <c>WaermequelleClass.Quellspeicher</c>; bis Paket L auch
        /// <c>Z_ProjektPufferSpCtrl.Insert</c>, mit dem Aufräumschnitt entfallen)
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
                new DbParam("@bez", szBezeichner ?? ""),
                new DbParam("@idProj", idProjekt));
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
                    new DbParam("@id", stammId));

                if (dt == null || dt.Rows.Count == 0)
                {
                    // PAKET 8 (Konzept 13.4): Sicherheitsnetz. Heute wird CopyFromStamm
                    // nur aus der Oberfläche gerufen (Projektbaum, Puffer-Verwaltung;
                    // bis Paket L auch aus Z_ProjektPufferSpCtrl.Insert, das mit dem
                    // Aufräumschnitt entfallen ist) - dort erscheint der Dialog
                    // unverändert. Sollte der Pfad je in den Rechenlauf geraten, meldet
                    // er still ins Protokoll statt den Lauf anzuhalten.
                    DataRepository.FehlerMelden(
                        "Der Pufferspeicher-Stammdatensatz wurde nicht gefunden (ID " + stammId + ").");
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

                DbParam[] ps = {
                    new DbParam("@id", neueId),
                    new DbParam("@idProj", idProjekt),
                    P("@bez", s["Bezeichner"]),
                    P("@her", ColOrNull(s, "Hersteller")),
                    P("@typ", ColOrNull(s, "Speichertyp")),
                    P("@ver", ColOrNull(s, "Bereitschaftsverluste")),
                    P("@vol", ColOrNull(s, "Gesamtvolumen")),
                    P("@inv", ColOrNull(s, "Investitionskosten"))
                };

                bool ok = DataRepository.ExecuteSQL(sql, ps);
                if (!ok) return -1;

                // KLASSEN-SET der neuen Projektzeile (Migrationsschritt 49, Konzept 6.1).
                //
                // Die STAMM-Tabelle bekommt KEINE Flags - ein Katalogeintrag beschreibt
                // eine BAUFORM (Speichertyp), keine hydraulische Verwendung. Sie fuehrt
                // heute nicht einmal eine Verwendungs-Spalte; ColOrNull liefert dann
                // DBNull, und die Rueckfallregel macht daraus {Heizung}. Genau das war
                // auch bisher das Verhalten dieser Zeilen: Die Spaltenliste des INSERT
                // oben traegt Verwendung nicht, der Wert blieb NULL, und NULL galt
                // ueberall als Heizung (WaermesenkeClass.WirksameVerwendung,
                // Ladeordnung.Entladereihenfolge).
                //
                // Fuehrt eine Datenbank die Spalte doch (Anwender-Erweiterung), wird sie
                // gelesen und uebertragen - dieselbe Ableitungsregel wie ueberall.
                KlassenSetSchreiben(neueId,
                                    KlassenSetAusVerwendung(Text(ColOrNull(s, "Verwendung"))));

                return neueId;
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
                new DbParam("@bez", szBezeichner ?? ""),
                new DbParam("@idProj", idProjekt)));

            string sql = "DELETE FROM Tab_Pufferspeicher WHERE Bezeichner = ? AND ID_Projekt = ?";
            return DataRepository.ExecuteSQL(sql,
                new DbParam("@bez", szBezeichner ?? ""),
                new DbParam("@idProj", idProjekt));
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
                new DbParam("@idProj", idProjekt),
                new DbParam("@idProj2", idProjekt)));

            return DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Pufferspeicher WHERE " + filter,
                new DbParam("@idProj", idProjekt),
                new DbParam("@idProj2", idProjekt));
        }

        // --- Projekt-Puffer-Verwaltung (Paket 2, Konzept 4.3 / 5.2) ------------------

        /// <summary>
        /// EXPLIZITE Übernahme aus dem Katalog: legt IMMER eine neue Projektzeile an
        /// (Konzept 4.3, Punkt 4 und 5.2 „Mehrfachanlage desselben Katalogtyps ist
        /// zulässig", E7).
        ///
        /// Bewusst getrennt von <see cref="CopyFromStamm(int,int)"/>: Der Altpfad wurde
        /// bis Paket L implizit aus <c>Z_ProjektPufferSpCtrl.Insert</c> heraus bei JEDEM
        /// Speichern der Konfiguration gerufen (die Klasse ist mit dem Aufräumschnitt
        /// entfallen). Wäre dort die Dedup-Prüfung entfallen, hätte jedes Speichern einen
        /// weiteren Duplikat-Puffer erzeugt (Befund aus Paket 1). Die Aufhebung gilt
        /// deshalb nur hier — im Pfad, den der Anwender ausdrücklich auslöst.
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
                new DbParam("@id", stammId));

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
        /// <param name="schwelleReserve">
        /// Mindestfüllstand/Notreserve [%] (Paket BHKW-Regulär). VORBELEGT, damit die
        /// Aufrufer aus dem Katalogweg (<see cref="CopyFromStammNeu"/>) und aus der
        /// Migration unverändert bleiben und ein neu angelegter Puffer dieselbe Reserve
        /// bekommt wie ein migrierter.
        /// </param>
        /// <param name="nutzungHeizung">
        /// KLASSEN-SET (Migrationsschritt 49, Konzept 6.1) — <c>null</c> heißt „aus
        /// <paramref name="verwendung"/> ableiten". Alle drei Flags sind VORBELEGT,
        /// damit die Aufrufer aus dem Katalogweg (<see cref="CopyFromStammNeu"/>) und
        /// aus der Migration unverändert bleiben; nur der Dialog gibt ein Set vor, das
        /// über den Altwert hinausgeht (etwa {Heizung, Prozess}).
        /// </param>
        /// <param name="nutzungBrauchwasser">siehe <paramref name="nutzungHeizung"/>.</param>
        /// <param name="nutzungProzess">siehe <paramref name="nutzungHeizung"/>.</param>
        /// <param name="schicht">
        /// SCHICHTUNG und Leistungsgrenzen (Migrationsschritt 53, Konzept 7.2) —
        /// <c>null</c> heißt „Vorbelegung", also das verhaltensneutrale Ein-Zonen-Modell
        /// ohne Leistungsgrenzen. EIN Parameter statt neun: Die Werte gehören fachlich
        /// zusammen, und nur der Speicherdialog setzt sie überhaupt.
        /// </param>
        public static int ProjektPufferAnlegen(int idProjekt, string bezeichner, string hersteller,
                                               string speichertyp, int volumenLiter, double verluste,
                                               double investitionskosten, string verwendung,
                                               int? vorlauf, int? ruecklauf,
                                               double schwelleEin, double schwelleAus,
                                               double schwelleAusNachrang, int entladeprio,
                                               double schwelleReserve = ProjektPuffer.SCHWELLE_RESERVE_DEFAULT,
                                               bool? nutzungHeizung = null,
                                               bool? nutzungBrauchwasser = null,
                                               bool? nutzungProzess = null,
                                               Schichtdaten schicht = null)
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
                                      schwelleEin, schwelleAus, schwelleAusNachrang, entladeprio,
                                      schwelleReserve)) < 0)
                return -1;

            // KLASSEN-SET (Migrationsschritt 49). Eigenes, zielgenaues UPDATE statt einer
            // Erweiterung von SQL_PUFFER_INSERT_VOLL: Auf einer Datenbank ohne
            // Schemastand 49 liesse eine erweiterte Spaltenliste das Anlegen des GANZEN
            // Speichers scheitern - wegen dreier Flags, deren Wert sich ohnehin aus der
            // mitgeschriebenen Verwendung ableiten laesst.
            KlassenSetSchreiben(neueId, KlassenSetBestimmen(verwendung, nutzungHeizung,
                                                            nutzungBrauchwasser, nutzungProzess));

            // SCHICHTUNG (Migrationsschritt 53) - eigenes, zielgenaues UPDATE aus
            // demselben Grund wie das Klassen-Set darüber. Ohne Angabe geschieht nichts:
            // Die Vorbelegung ist genau das, was die Leseseite ohne Spalten liefert.
            SchichtdatenSchreiben(neueId, schicht ?? new Schichtdaten());

            // Anlagenzeile nachtragen - eine je Projekt + Bezeichner (Regel R4 der
            // Migration) UND eine je Projekt + Gerät (Teil A der Anlagenzeilen-
            // Eindeutigkeit). Die zweite Bedingung ist die belastbarere: Sie prüft den
            // Fremdschlüssel ID_PUFFER, also genau das, was der Eindeutigkeitsindex
            // sperrt; die Namensprüfung hängt am Bezeichner, der sich ändern kann.
            if (!AnlagenzeileVorhanden(idProjekt, name) &&
                !Anlagenzeilen.ZeileVorhanden(Anlagenzeilen.SPALTE_PUFFER, idProjekt, neueId))
                StilleDb.NonQuery(ProjektPuffer.SQL_ANLAGENZEILE_INSERT,
                                  ProjektPuffer.AnlagenzeileParameter(idProjekt, name, neueId));

            return neueId;
        }

        /// <summary>
        /// Ändert einen vorhandenen Projekt-Puffer (Konzept 4.3).
        /// Die drei Klassen-Set-Flags verhalten sich wie in
        /// <see cref="ProjektPufferAnlegen"/>: <c>null</c> heißt „aus
        /// <paramref name="verwendung"/> ableiten".
        /// </summary>
        /// <param name="schicht">
        /// SCHICHTUNG und Leistungsgrenzen (Migrationsschritt 53) — <c>null</c> heißt
        /// hier „unverändert lassen", NICHT „auf Vorbelegung setzen" (Begründung an der
        /// Schreibstelle unten).
        /// </param>
        public static bool ProjektPufferAendern(int idPuffer, int idProjekt, string bezeichner,
                                                string hersteller, string speichertyp, int volumenLiter,
                                                double verluste, double investitionskosten,
                                                string verwendung, int? vorlauf, int? ruecklauf,
                                                double schwelleEin, double schwelleAus,
                                                double schwelleAusNachrang, int entladeprio,
                                                double schwelleReserve = ProjektPuffer.SCHWELLE_RESERVE_DEFAULT,
                                                bool? nutzungHeizung = null,
                                                bool? nutzungBrauchwasser = null,
                                                bool? nutzungProzess = null,
                                                Schichtdaten schicht = null)
        {
            if (idPuffer <= 0 || string.IsNullOrEmpty(bezeichner)) return false;

            string alterName = StilleDb.Text(StilleDb.Scalar(
                "SELECT Bezeichner FROM Tab_Pufferspeicher WHERE ID = ?",
                StilleDb.Par("@id", DbParamTyp.Integer, idPuffer)));

            string name = string.Equals(alterName, bezeichner, StringComparison.Ordinal)
                ? bezeichner
                : EindeutigerBezeichner(idProjekt, bezeichner, idPuffer);

            if (StilleDb.NonQuery(ProjektPuffer.SQL_PUFFER_UPDATE_VOLL,
                                  ProjektPuffer.PufferParameterVollUpdate(
                                      idPuffer, name, hersteller, speichertyp, volumenLiter,
                                      verluste, investitionskosten, verwendung,
                                      vorlauf, ruecklauf,
                                      schwelleEin, schwelleAus, schwelleAusNachrang, entladeprio,
                                      schwelleReserve)) < 0)
                return false;

            // KLASSEN-SET (Migrationsschritt 49) - dieselbe Begründung für das eigene,
            // zielgenaue UPDATE wie in ProjektPufferAnlegen.
            KlassenSetSchreiben(idPuffer, KlassenSetBestimmen(verwendung, nutzungHeizung,
                                                              nutzungBrauchwasser, nutzungProzess));

            // SCHICHTUNG (Migrationsschritt 53) - dieselbe Begründung für das eigene,
            // zielgenaue UPDATE wie beim Klassen-Set.
            //
            // null heißt hier AUSDRÜCKLICH "nicht anfassen" und nicht "auf Vorbelegung
            // setzen": Beim ANLEGEN ist die Vorbelegung der richtige Startwert, beim
            // ÄNDERN wäre sie stiller Datenverlust - ein Aufrufer, der nur das Volumen
            // ändert, löschte sonst die gepflegte Schichtung mit.
            if (schicht != null) SchichtdatenSchreiben(idPuffer, schicht);

            // Die Anlagenzeile führt denselben Bezeichner - sonst greift
            // ProjektWaisenEntfernen zu (Abgleich läuft über den Namen).
            if (!string.Equals(alterName, name, StringComparison.Ordinal) && alterName.Length > 0)
            {
                StilleDb.NonQuery(
                    "UPDATE Tab_Energieanlagen SET Bezeichner = ? " +
                    "WHERE ID_Projekt = ? AND ID_Type = ? AND Bezeichner = ?",
                    StilleDb.Par("@neu", DbParamTyp.VarWChar, name),
                    StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt),
                    StilleDb.Par("@typ", DbParamTyp.Integer, ProjektPuffer.TYP_PUFFER),
                    StilleDb.Par("@alt", DbParamTyp.VarWChar, alterName));

                // Und die Alt-Zuordnung: Z_ProjektPufferSp.Pufferspeicher ist eine
                // TEXTreferenz. Sie wird HIER weiter nachgeführt, obwohl die Tabelle seit
                // Migrationsschritt 51 stillgelegt ist (Konzept Kapitel 15) - die Zeilen
                // bleiben in der Datenbank stehen, und ein Bezeichner, der dort auf einen
                // umbenannten Speicher zeigt, wäre ein stiller Datenwiderspruch.
                //
                // Der ursprüngliche Grund ist mit Paket L entfallen: Bis dahin legte das
                // nächste "Speichern" bei stehengebliebenem Altnamen einen DUPLIKAT-PUFFER
                // an - Z_ProjektPufferSpCtrl.Insert löste den Namen über GetProjektId auf,
                // fand ihn nach dem Umbenennen nicht mehr und rief CopyFromStamm, das eine
                // zweite Projektkopie unter dem ALTEN Namen erzeugte (reproduziert im
                // Review zu Paket 2). Diese Klasse gibt es nicht mehr.
                //
                // Schlüssel ist die ID_Pufferspeicher, nicht der Name: sie ist seit der
                // Migration Pflichtspalte mit erzwungener Beziehung und trifft genau die
                // Zeilen dieses Speichers - gleichnamige Zeilen anderer Speicher bleiben
                // unangetastet.
                StilleDb.NonQuery(
                    "UPDATE Z_ProjektPufferSp SET Pufferspeicher = ? WHERE ID_Pufferspeicher = ?",
                    StilleDb.Par("@neu", DbParamTyp.VarWChar, name),
                    StilleDb.Par("@id", DbParamTyp.Integer, idPuffer));
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
                    StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt),
                    StilleDb.Par("@bez", DbParamTyp.VarWChar, kandidat),
                    StilleDb.Par("@aus", DbParamTyp.Integer, idAusnahme)));

                if (treffer == 0) return kandidat;
            }

            return basis + " (" + DateTime.Now.ToString("HHmmss") + ")";
        }

        /// <summary>
        /// Anlagen, die den Puffer als Quelle oder Senke referenzieren — Grundlage für
        /// die Blockade beim Entfernen (Konzept 5.2, Konsistenzregel). Je Treffer ein
        /// fertiger Anzeigetext.
        ///
        /// PAKET PARALLELVERBUND: Eine VERBUNDMITGLIEDSCHAFT (<c>Z_AnlagePufferVerbund</c>)
        /// ist eine Referenz wie jede andere und erscheint hier mit derselben Wirkung. Sie
        /// muss es sein: Verschwindet ein Mitglied stillschweigend, sinkt die gerechnete
        /// Kapazität des Verbunds, ohne dass es jemand erfährt. In der Datenbank sichert
        /// das zusätzlich die restriktive Beziehung <c>FK_Verbund_Puffer</c> ab; hier steht
        /// die Aussage, die der Anwender liest.
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
                StilleDb.Par("@a", DbParamTyp.Integer, idPuffer),
                StilleDb.Par("@b", DbParamTyp.Integer, idPuffer),
                StilleDb.Par("@c", DbParamTyp.Integer, idPuffer));
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

            // PAKET PARALLELVERBUND: die Verbundmitgliedschaften. Eigene Abfrage statt
            // eines vierten OR-Glieds oben - die Zuordnung steht in einer ANDEREN Tabelle,
            // und ein OUTER JOIN nur für den Löschhinweis wäre die teurere Lösung für
            // dasselbe Ergebnis. Die Rolle heißt „Verbundmitglied", damit der Anwender in
            // der Blockademeldung sofort sieht, dass es nicht um Haupt- oder Zweitsenke
            // geht.
            foreach (KeyValuePair<int, string> m in
                     AnlagePufferVerbundCtrl.MitgliedschaftenAufPuffer(idPuffer))
            {
                string zeile = m.Value + " - Verbundmitglied";
                if (!treffer.Contains(zeile)) treffer.Add(zeile);
            }

            // PAKET S1 (Migrationsschritt 50): die Zeilen der SENKENLISTE.
            //
            // Eigene Abfrage aus demselben Grund wie beim Verbund - die Zuordnung steht
            // in einer anderen Tabelle. Sie muss hier stehen: Ab Schritt 50 ist
            // Z_AnlageSenke.ID_Puffer der Ort, an dem eine dritte, vierte, n-te
            // Puffersenke ueberhaupt nur noch abgelegt werden KANN; die beiden
            // WS_*-Slots oben sehen davon nichts. Ohne diesen Block meldete die
            // Blockade "keine Referenzen", waehrend die restriktive Beziehung
            // FK_AnlageSenke_Puffer das Loeschen anschliessend abweist - der Anwender
            // bekaeme einen Datenbankfehler statt einer Erklaerung.
            //
            // Die Rolle nennt den RANG statt "Haupt-/Zweitsenke": In einer Liste
            // beliebiger Laenge ist die Position die Aussage.
            DataTable senken = StilleDb.Tabelle(
                "SELECT a.Bezeichner, a.ID_Type, s.Rang " +
                "FROM [" + Z_AnlageSenkeCtrl.TABLE + "] s " +
                "INNER JOIN Tab_Energieanlagen a ON a.ID = s.ID_Anlage " +
                "WHERE s.ID_Puffer = ? ORDER BY a.Bezeichner, s.Rang",
                StilleDb.Par("@s", DbParamTyp.Integer, idPuffer));

            if (senken != null)
            {
                foreach (DataRow r in senken.Rows)
                {
                    string bezeichner = StilleDb.Text(StilleDb.Feld(r, "Bezeichner"));
                    string erzeuger = Ladeordnung.ErzeugerName(StilleDb.Zahl(StilleDb.Feld(r, "ID_Type")));
                    int rang = StilleDb.Zahl(StilleDb.Feld(r, "Rang"));

                    string zeile = bezeichner + " (" + erzeuger + ") - Senke Rang " + rang;
                    if (!treffer.Contains(zeile)) treffer.Add(zeile);
                }
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
                StilleDb.Par("@id", DbParamTyp.Integer, idPuffer)));

            // Alt-Zuordnungen dieses Speichers zuerst - sie hängen über
            // Z_ProjektPufferSp.ID_Pufferspeicher am Puffer.
            StilleDb.NonQuery("DELETE FROM Z_ProjektPufferSp WHERE ID_Pufferspeicher = ?",
                              StilleDb.Par("@id", DbParamTyp.Integer, idPuffer));

            // Anlagenzeile (ID_Type = 12) des Speichers
            if (bezeichner.Length > 0)
                StilleDb.NonQuery(
                    "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? AND Bezeichner = ?",
                    StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt),
                    StilleDb.Par("@typ", DbParamTyp.Integer, ProjektPuffer.TYP_PUFFER),
                    StilleDb.Par("@bez", DbParamTyp.VarWChar, bezeichner));

            ReferenzenLoesen(new List<int> { idPuffer });

            bool ok = StilleDb.NonQuery("DELETE FROM Tab_Pufferspeicher WHERE ID = ?",
                                        StilleDb.Par("@id", DbParamTyp.Integer, idPuffer)) >= 0;

            // Waisen aufräumen (B0-6a) - Projektkopien ohne Anlagenzeile
            new PufferSpCtrl().ProjektWaisenEntfernen(idProjekt);
            return ok;
        }

        // =================================================================================
        //  KLASSEN-SET (Migrationsschritt 49, Paket K2)
        //  Konzept Brauchwasser/Heizung/Pufferspeicher § 6.1, Entscheidung
        //  F5-Alternative / L6 vom 27.08.2026
        // =================================================================================
        //
        // Tab_Pufferspeicher.Verwendung war EINWERTIG: Heizung, Brauchwasser oder
        // "Kombi" für beides. Ein Prozessspeicher liess sich gar nicht ausdruecken, und
        // {Heizung, Prozess} erst recht nicht. An ihre Stelle treten drei unabhaengige
        // Ja/Nein-Flags; "Kombi" ist nur noch der ANZEIGENAME des Sets
        // {Heizung, Brauchwasser}.
        //
        // Verwendung bleibt als LESE-ALTLAST stehen und wird beim Speichern als
        // ABGELEITETER Altwert mitgeschrieben - bis zum Paket S2 lesen Anzeigen,
        // Auswahllisten und Meldungen sie noch (WaermesenkeClass.WirksameVerwendung,
        // Ladeordnung.Entladereihenfolge, SpeicherKarte). Fuehrend sind die Flags.
        //
        // DIE EINE ABLEITUNGSREGEL steht in KlassenSetAusVerwendung und wird von JEDEM
        // Schreiber benutzt, der Verwendung setzt. Wer sie umgeht, erzeugt eine zweite
        // Wahrheit ueber dieselbe Frage.

        /// <summary>
        /// Das Klassen-Set eines Pufferspeichers — welche der drei Kanäle er bedient
        /// (Migrationsschritt 49, Konzept 6.1). Unveränderlich; die Ableitungen
        /// <see cref="Verwendung"/> und <see cref="HatAltEntsprechung"/> hängen daran.
        /// </summary>
        public sealed class KlassenSet
        {
            public readonly bool Heizung;
            public readonly bool Brauchwasser;
            public readonly bool Prozess;

            public KlassenSet(bool heizung, bool brauchwasser, bool prozess)
            {
                Heizung = heizung;
                Brauchwasser = brauchwasser;
                Prozess = prozess;
            }

            /// <summary>
            /// Das LEERE Set — ein Speicher, den niemand entlädt. Fachlich kein
            /// gültiger Zustand (Konzept 6.1: „Mindestens ein Flag muss gesetzt
            /// sein"); überall, wo er auftritt, heißt er „noch nicht gesetzt" und löst
            /// die Rückfallregel aus.
            /// </summary>
            public bool Leer
            {
                get { return !Heizung && !Brauchwasser && !Prozess; }
            }

            /// <summary>
            /// Der abgeleitete ALTWERT für <c>Tab_Pufferspeicher.Verwendung</c>.
            ///
            /// <para>Drei Sets haben eine genaue Entsprechung — {H} → „Heizung",
            /// {B} → „Brauchwasser", {H, B} → „Kombi". Für die übrigen ({P}, {H, P},
            /// {B, P}, {H, B, P}, {}) gibt es keinen Altwert; sie bekommen den
            /// NÄCHSTLIEGENDEN: Enthält das Set beide Wasserkanäle, ist das „Kombi";
            /// enthält es nur Brauchwasser, „Brauchwasser"; sonst „Heizung". Damit
            /// bleibt der Altwert genau so gut, wie er sein kann — die Prozessklasse
            /// kennt er nicht, und „Heizung" ist überall der Rückfall für Unbekanntes
            /// (WaermesenkeClass.NormalisierteVerwendung).</para>
            /// </summary>
            public string Verwendung
            {
                get
                {
                    if (Heizung && Brauchwasser) return WaermesenkeClass.VERWENDUNG_KOMBI;
                    if (Brauchwasser) return WaermesenkeClass.VERWENDUNG_BRAUCHWASSER;
                    return WaermesenkeClass.VERWENDUNG_HEIZUNG;
                }
            }

            /// <summary>
            /// true, wenn <see cref="Verwendung"/> das Set VERLUSTFREI abbildet — also
            /// genau für {H}, {B} und {H, B}. Sonst ist der Altwert nur der
            /// nächstliegende, und die Oberfläche weist darauf hin.
            /// </summary>
            public bool HatAltEntsprechung
            {
                get { return !Prozess && !Leer; }
            }
        }

        /// <summary>
        /// DIE Ableitungsregel Verwendung → Klassen-Set (Konzept 6.1, DML-Migration
        /// Schritt 49): <c>Heizung</c> → {H}, <c>Brauchwasser</c> → {B},
        /// <c>Kombi</c> → {H, B}; <c>Quelle</c>, Leerwert, <c>null</c> und jeder
        /// unbekannte Wert → {H}.
        ///
        /// <para>Die Schreibweise ist gleichgültig: Normalisiert wird über
        /// <see cref="WaermesenkeClass.NormalisierteVerwendung"/> — dieselbe Stelle, die
        /// auch der Rechenkern benutzt, und dieselbe Toleranz, die die DML des
        /// Migrationsschritts über <c>UCase</c> hat.</para>
        /// </summary>
        public static KlassenSet KlassenSetAusVerwendung(string verwendung)
        {
            string wirksam = WaermesenkeClass.NormalisierteVerwendung(verwendung);

            bool brauchwasser =
                string.Equals(wirksam, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                              StringComparison.OrdinalIgnoreCase) ||
                string.Equals(wirksam, WaermesenkeClass.VERWENDUNG_KOMBI,
                              StringComparison.OrdinalIgnoreCase);

            bool heizung = !string.Equals(wirksam, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                          StringComparison.OrdinalIgnoreCase);

            return new KlassenSet(heizung, brauchwasser, false);
        }

        /// <summary>Kurzform für Aufrufer, die nur den Altwert brauchen.</summary>
        public static string VerwendungAusKlassenSet(bool heizung, bool brauchwasser, bool prozess)
        {
            return new KlassenSet(heizung, brauchwasser, prozess).Verwendung;
        }

        /// <summary>
        /// Das Klassen-Set einer GELESENEN Puffer-Zeile — spaltentolerant.
        ///
        /// <para><b>Zwei Lücken, eine Regel.</b> Fehlen die Spalten (Datenbank noch
        /// nicht auf Schemastand 49), liest die Methode nichts und das Set bliebe leer.
        /// Und selbst wenn sie da sind, kann es leer sein: Access belegt eine per
        /// <c>ADD COLUMN … YESNO</c> angehängte Spalte mit FALSCH, nicht mit NULL —
        /// eine Zeile, die ein Schreibweg ohne Flag-Vorsorge angelegt hat, trägt
        /// deshalb drei Nullen statt einer Lücke. Beide Fälle bedeuten dasselbe: „noch
        /// nicht gesetzt". Beide fallen auf <see cref="KlassenSetAusVerwendung"/>
        /// zurück, und damit auf exakt das Verhalten vor Schritt 49.</para>
        /// </summary>
        public static KlassenSet KlassenSetAusZeile(DataRow r)
        {
            if (r == null) return new KlassenSet(true, false, false);

            KlassenSet gelesen = new KlassenSet(
                FlagLesen(r, SchemaKatalog.SPALTE_PSP_NUTZUNG_HEIZUNG),
                FlagLesen(r, SchemaKatalog.SPALTE_PSP_NUTZUNG_BRAUCHWASSER),
                FlagLesen(r, SchemaKatalog.SPALTE_PSP_NUTZUNG_PROZESS));

            return gelesen.Leer
                ? KlassenSetAusVerwendung(Text(ColOrNull(r, "Verwendung")))
                : gelesen;
        }

        /// <summary>Ein Ja/Nein-Feld; fehlende Spalte, NULL und Unlesbares ergeben false.</summary>
        private static bool FlagLesen(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte)) return false;

            object v = r[spalte];
            if (v == null || v == DBNull.Value) return false;

            try { return Convert.ToBoolean(v); }
            catch { return false; }
        }

        /// <summary>
        /// Das Klassen-Set EINES Puffers aus der Datenbank; nie <c>null</c>. Ein
        /// unbekannter Puffer liefert die Vorbelegung {Heizung} — dieselbe Antwort wie
        /// eine leere Verwendung.
        ///
        /// Still (nur <c>Console.WriteLine</c>): Die Methode hängt an denselben Wegen
        /// wie <see cref="TemperaturenLesen"/> und muss dialogfrei bleiben.
        /// </summary>
        public static KlassenSet KlassenSetLesen(int idPuffer)
        {
            if (idPuffer <= 0) return new KlassenSet(true, false, false);

            DataTable dt = StilleDb.Tabelle(
                "SELECT * FROM Tab_Pufferspeicher WHERE ID = ?",
                StilleDb.Par("@id", DbParamTyp.Integer, idPuffer));

            if (dt == null || dt.Rows.Count == 0) return new KlassenSet(true, false, false);

            return KlassenSetAusZeile(dt.Rows[0]);
        }

        /// <summary>
        /// Die Klassen-Sets ALLER Puffer eines Projekts in EINER Abfrage; nie
        /// <c>null</c>, Schlüssel ist die Puffer-ID (PAKET S2).
        ///
        /// <para>Das Gegenstück zu <see cref="KlassenSetLesen"/> für Aufrufer, die
        /// ohnehin alle Speicher eines Projekts brauchen: die Speicherauswahl des
        /// Senkendialogs und das Schemamodell. Ein Aufruf je Speicher wäre bei Projekt
        /// 1023 der Referenzmenge — über 80 Pufferkopien aus wiederholtem „Projekt
        /// duplizieren" — eine Abfrage je Listeneintrag beim Öffnen des Dialogs.</para>
        ///
        /// <para>Abgeleitet wird über dieselbe eine Regel
        /// (<see cref="KlassenSetAusZeile"/>), also mit demselben Rückfall auf
        /// <c>Verwendung</c>, wenn die Flags des Schemastands 49 fehlen. Still und
        /// dialogfrei wie die Einzelfassung.</para>
        /// </summary>
        public static Dictionary<int, KlassenSet> KlassenSetsJeProjekt(int idProjekt)
        {
            Dictionary<int, KlassenSet> sets = new Dictionary<int, KlassenSet>();
            if (idProjekt <= 0) return sets;

            DataTable dt = StilleDb.Tabelle(
                "SELECT * FROM Tab_Pufferspeicher WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
            if (dt == null) return sets;

            foreach (DataRow r in dt.Rows)
            {
                int id = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                if (id <= 0 || sets.ContainsKey(id)) continue;
                sets[id] = KlassenSetAusZeile(r);
            }

            return sets;
        }

        /// <summary>
        /// Das zu schreibende Klassen-Set aus Altwert UND ausdrücklicher Vorgabe: Jedes
        /// Flag, das der Aufrufer setzt, gewinnt; die übrigen kommen aus
        /// <paramref name="verwendung"/>. Ein LEERES Ergebnis wird auf {Heizung}
        /// gehoben — ein Speicher, den niemand entlädt, darf nicht entstehen
        /// (Konzept 6.1). Der Dialog fängt das vorher mit einer Meldung ab; diese Zeile
        /// ist das Netz für die programmatischen Wege.
        /// </summary>
        public static KlassenSet KlassenSetBestimmen(string verwendung, bool? nutzungHeizung,
                                                     bool? nutzungBrauchwasser, bool? nutzungProzess)
        {
            KlassenSet ausAltwert = KlassenSetAusVerwendung(verwendung);

            bool h = nutzungHeizung ?? ausAltwert.Heizung;
            bool b = nutzungBrauchwasser ?? ausAltwert.Brauchwasser;
            bool p = nutzungProzess ?? ausAltwert.Prozess;

            if (!h && !b && !p) h = true;

            return new KlassenSet(h, b, p);
        }

        /// <summary>
        /// Schreibt das Klassen-Set an eine Puffer-Zeile — ein EIGENES, zielgenaues
        /// UPDATE (Muster <c>KonfigurationCtrl.ExtrapolationErlaubtSchreiben</c>).
        ///
        /// <para><b>Warum nicht in die Spaltenlisten von
        /// <c>ProjektPuffer.SQL_PUFFER_INSERT_VOLL</c> und
        /// <c>SQL_PUFFER_UPDATE_VOLL</c>.</b> Auf einer Datenbank ohne Schemastand 49
        /// scheiterte damit das Anlegen bzw. Ändern des GANZEN Speichers — wegen
        /// dreier Flags, deren Wert sich aus der mitgeschriebenen Verwendung ohnehin
        /// ableiten lässt. Hier scheitert im schlimmsten Fall nur das Nachtragen, und
        /// die Leseseite fällt geordnet auf die Ableitung zurück.</para>
        ///
        /// <para>Vorher läuft die einmalige Spaltenvorsorge
        /// (<see cref="StelleKlassenSetSpaltenSicher"/>). Dialogfrei; Rückgabe false,
        /// wenn die Spalten fehlen oder keine Zeile getroffen wurde.</para>
        /// </summary>
        public static bool KlassenSetSchreiben(int idPuffer, KlassenSet set)
        {
            if (idPuffer <= 0 || set == null) return false;
            if (!StelleKlassenSetSpaltenSicher()) return false;

            return StillNonQuery(
                "UPDATE Tab_Pufferspeicher SET " +
                "[" + SchemaKatalog.SPALTE_PSP_NUTZUNG_HEIZUNG + "] = ?, " +
                "[" + SchemaKatalog.SPALTE_PSP_NUTZUNG_BRAUCHWASSER + "] = ?, " +
                "[" + SchemaKatalog.SPALTE_PSP_NUTZUNG_PROZESS + "] = ? " +
                "WHERE ID = ?",
                StilleDb.Par("@h", DbParamTyp.Boolean, set.Heizung),
                StilleDb.Par("@b", DbParamTyp.Boolean, set.Brauchwasser),
                StilleDb.Par("@p", DbParamTyp.Boolean, set.Prozess),
                StilleDb.Par("@id", DbParamTyp.Integer, idPuffer)) > 0;
        }

        /// <summary>Bequemlichkeitsfassung von <see cref="KlassenSetSchreiben(int,KlassenSet)"/>.</summary>
        public static bool KlassenSetSchreiben(int idPuffer, bool heizung, bool brauchwasser,
                                               bool prozess)
        {
            return KlassenSetSchreiben(idPuffer, new KlassenSet(heizung, brauchwasser, prozess));
        }

        /// <summary>Ergebnis der einmaligen Spaltenvorsorge (siehe <see cref="StelleKlassenSetSpaltenSicher"/>).</summary>
        private static bool? _klassenSetSpaltenBereit;

        /// <summary>
        /// Tolerante Vorsorge für die drei Klassen-Set-Spalten (Migrationsschritt 49)
        /// unmittelbar vor dem SCHREIBEN — dasselbe Muster wie
        /// <c>Z_ProjektGebGanglinieCtrl.StelleKanalSpalteSicher</c> für Schritt 48.
        ///
        /// <para>Die Leseseite braucht sie nicht: <see cref="KlassenSetAusZeile"/> prüft
        /// die Spaltennamen und fällt auf die Verwendung zurück. Der Schreibweg dagegen
        /// nennt die Spalten ausgeschrieben.</para>
        ///
        /// <para>Eigene Verbindung statt <c>DataRepository</c>, weil eine Vorsorge kein
        /// Bedienschritt ist und keine MessageBox zeigen darf. Einmal je Prozess. Die
        /// Leseprobe am Ende ist Nachweis statt Annahme — das <c>ALTER</c> schluckt
        /// jeden Fehler, auch einen echten.</para>
        ///
        /// <para>Kollidiert NICHT mit dem Migrationsschritt: Dessen DML setzt das Set
        /// nur dort, wo alle drei Flags falsch sind. Eine Zeile, die diese Vorsorge
        /// angelegt und der Dialog schon gefüllt hat, bleibt unangetastet.</para>
        /// </summary>
        public static bool StelleKlassenSetSpaltenSicher()
        {
            if (_klassenSetSpaltenBereit.HasValue) return _klassenSetSpaltenBereit.Value;

            string sH = SchemaKatalog.SPALTE_PSP_NUTZUNG_HEIZUNG;
            string sB = SchemaKatalog.SPALTE_PSP_NUTZUNG_BRAUCHWASSER;
            string sP = SchemaKatalog.SPALTE_PSP_NUTZUNG_PROZESS;

            bool ok = false;
            try
            {
                // ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht. Statt blind zu
                // ALTERn und den Fehler zu schlucken, entscheidet eine VORABPROBE ueber
                // die Schema-Auskunft (S4c/S4d vorgezogen); der Spaltentyp YESNO wird
                // dabei nach SQLite uebersetzt.
                HashSet<string> vorhanden = StilleDb.SpaltenNamen(SchemaKatalog.TAB_PUFFERSPEICHER);

                foreach (string spalte in new[] { sH, sB, sP })
                {
                    if (vorhanden != null && vorhanden.Contains(spalte)) continue;
                    StilleDb.NonQuery(StilleDb.AlterTableAddColumn(
                        SchemaKatalog.TAB_PUFFERSPEICHER, spalte, "YESNO"));
                }

                // Nachweis statt Annahme - erst diese Leseprobe belegt, dass alle drei
                // Spalten da sind.
                ok = StillProbe(
                    "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_PUFFERSPEICHER +
                    "] WHERE [" + sH + "] = FALSE AND [" + sB + "] = FALSE " +
                    "AND [" + sP + "] = FALSE");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Klassen-Set-Spalten nicht verfuegbar: " + ex.Message);
                ok = false;
            }

            _klassenSetSpaltenBereit = ok;
            return ok;
        }

        // =================================================================================
        //  SCHICHTUNG UND LEISTUNGSGRENZEN (Migrationsschritt 53, Paket P1)
        //  Konzept Brauchwasser/Heizung/Pufferspeicher § 7.2 und § 6.3
        // =================================================================================
        //
        // Neun Spalten an Tab_Pufferspeicher, alle mit VERHALTENSNEUTRALER Vorbelegung:
        //
        //   Schichten_Anzahl    LONG    1     Ein-Zonen-Modell wie im Bestand
        //   Hoehe               DOUBLE  NULL  Hoehe aus dem H/D-Verhaeltnis 2,5
        //   Lambda_Eff          DOUBLE  NULL  1,5 W/(m*K)
        //   T_Nutz_BW           DOUBLE  NULL  Ruecklauf (RL_eff)
        //   Entnahme_Heizung    DOUBLE  NULL  Kanal-Default
        //   Entnahme_BW         DOUBLE  NULL  Kanal-Default
        //   Entnahme_Prozess    DOUBLE  NULL  Kanal-Default
        //   Ladeleistung_Max    DOUBLE  0     unbegrenzt
        //   Entladeleistung_Max DOUBLE  0     unbegrenzt
        //
        // DIE SPALTENNAMEN KOMMEN AUS SchemaKatalog - dieselbe Quelle, aus der Schritt 53
        // sie anlegt (Muster KlassenSetSchreiben mit den Flags des Schrittes 49). Eine
        // zweite Namensliste hier waere eine zweite Wahrheit ueber dasselbe Schema, und
        // eine Abweichung faende niemand: Der Dialog schriebe in eine selbst angelegte
        // Spalte, die Engine laese die der Migration.
        //
        // SPALTENTOLERANZ ist die tragende Eigenschaft dieses Blocks: Auf einer Datenbank
        // ohne Schritt 53 liest SchichtdatenAusZeile schlicht nichts und liefert die
        // Vorbelegung; der Dialog zeigt dann N = 1 und leere Felder, der
        // Warnkriterienkatalog meldet weder W4 noch W6. Nichts scheitert, nichts aendert
        // sich am gerechneten Ergebnis.

        /// <summary>
        /// Die Schicht- und Leistungsparameter EINER Puffer-Zeile (Schritt 53).
        ///
        /// <para>Veränderlich, anders als <see cref="KlassenSet"/>: Der Dialog baut das
        /// Objekt Feld für Feld aus seinen Eingabefeldern auf, und ein
        /// Neun-Parameter-Konstruktor wäre an der Aufrufstelle nicht mehr lesbar.</para>
        /// </summary>
        public sealed class Schichtdaten
        {
            /// <summary>Schichtenzahl 1…10; 1 = Ein-Zonen-Speicher.</summary>
            public int Schichten = SCHICHTEN_DEFAULT;

            /// <summary>Behälterhöhe [m]; <c>null</c> = aus H/D 2,5.</summary>
            public double? Hoehe;

            /// <summary>Effektive vertikale Wärmeleitfähigkeit [W/(m·K)]; <c>null</c> = 1,5.</summary>
            public double? LambdaEff;

            /// <summary>Mindest-Nutztemperatur Brauchwasser [°C]; <c>null</c> = RL_eff.</summary>
            public double? TNutzBW;

            /// <summary>Entnahmehöhe Heizung 0…1; <c>null</c> = Default.</summary>
            public double? EntnahmeHeizung;

            /// <summary>Entnahmehöhe Brauchwasser 0…1; <c>null</c> = Default.</summary>
            public double? EntnahmeBW;

            /// <summary>Entnahmehöhe Prozesswärme 0…1; <c>null</c> = Default.</summary>
            public double? EntnahmeProzess;

            /// <summary>Größte Ladeleistung [kW]; 0 = unbegrenzt.</summary>
            public double LadeleistungMax;

            /// <summary>Größte Entladeleistung [kW]; 0 = unbegrenzt.</summary>
            public double EntladeleistungMax;

            /// <summary>true, sobald der Speicher mehr als eine Schicht führt.</summary>
            public bool Geschichtet
            {
                get { return Schichten > SCHICHTEN_DEFAULT; }
            }

            /// <summary>
            /// true, wenn NICHTS gepflegt ist — das verhaltensneutrale Ein-Zonen-Modell
            /// ohne Leistungsgrenzen. Nur in diesem Zustand darf
            /// <see cref="SchichtdatenSchreiben"/> auf die Spaltenvorsorge verzichten:
            /// Es gibt dann nichts abzulegen, was der Rückfall nicht ohnehin liefert.
            /// </summary>
            public bool IstVorbelegung
            {
                get
                {
                    return Schichten <= SCHICHTEN_DEFAULT &&
                           !Hoehe.HasValue && !LambdaEff.HasValue && !TNutzBW.HasValue &&
                           !EntnahmeHeizung.HasValue && !EntnahmeBW.HasValue &&
                           !EntnahmeProzess.HasValue &&
                           LadeleistungMax <= 0 && EntladeleistungMax <= 0;
                }
            }

            /// <summary>Überträgt die Werte in ein <see cref="PufferSpModel"/>.</summary>
            public void NachModell(PufferSpModel m)
            {
                if (m == null) return;

                m.Schichten_Anzahl = Schichten;
                m.Hoehe = Hoehe;
                m.Lambda_Eff = LambdaEff;
                m.T_Nutz_BW = TNutzBW;
                m.Entnahme_Heizung = EntnahmeHeizung;
                m.Entnahme_BW = EntnahmeBW;
                m.Entnahme_Prozess = EntnahmeProzess;
                m.Ladeleistung_Max = LadeleistungMax;
                m.Entladeleistung_Max = EntladeleistungMax;
            }
        }

        /// <summary>
        /// Die Schichtdaten einer GELESENEN Puffer-Zeile — spaltentolerant; nie
        /// <c>null</c>.
        ///
        /// <para>Fehlt eine Spalte (Schritt 53 noch nicht gelaufen) oder steht dort NULL,
        /// bleibt es beim Vorbelegungswert. Eine <c>Schichten_Anzahl</c> unter 1 wird auf
        /// 1 gehoben und eine über <see cref="PufferSpModel.SCHICHTEN_MAX"/> auf das
        /// Maximum gekappt — Altdaten und programmatische Schreibwege sollen das
        /// Rechenwerk nicht mit einer 0- oder 500-Schichten-Vorgabe erreichen.</para>
        /// </summary>
        public static Schichtdaten SchichtdatenAusZeile(DataRow r)
        {
            Schichtdaten d = new Schichtdaten();
            if (r == null) return d;

            int? n = ZahlOderNull(r, SchemaKatalog.SPALTE_PSP_SCHICHTEN_ANZAHL);
            if (n.HasValue) d.Schichten = SchichtenKlemmen(n.Value);

            d.Hoehe = KommazahlOderNull(r, SchemaKatalog.SPALTE_PSP_HOEHE);
            d.LambdaEff = KommazahlOderNull(r, SchemaKatalog.SPALTE_PSP_LAMBDA_EFF);
            d.TNutzBW = KommazahlOderNull(r, SchemaKatalog.SPALTE_PSP_T_NUTZ_BW);
            d.EntnahmeHeizung = KommazahlOderNull(r, SchemaKatalog.SPALTE_PSP_ENTNAHME_HEIZUNG);
            d.EntnahmeBW = KommazahlOderNull(r, SchemaKatalog.SPALTE_PSP_ENTNAHME_BW);
            d.EntnahmeProzess = KommazahlOderNull(r, SchemaKatalog.SPALTE_PSP_ENTNAHME_PROZESS);
            d.LadeleistungMax = KommazahlOderNull(r, SchemaKatalog.SPALTE_PSP_LADELEISTUNG_MAX) ?? 0;
            d.EntladeleistungMax = KommazahlOderNull(r, SchemaKatalog.SPALTE_PSP_ENTLADELEISTUNG_MAX) ?? 0;

            return d;
        }

        /// <summary>Hält die Schichtenzahl im zulässigen Band 1…10.</summary>
        public static int SchichtenKlemmen(int n)
        {
            if (n < SCHICHTEN_DEFAULT) return SCHICHTEN_DEFAULT;
            if (n > SCHICHTEN_MAX) return SCHICHTEN_MAX;
            return n;
        }

        /// <summary>
        /// Die Schichtdaten EINES Puffers aus der Datenbank; nie <c>null</c>. Ein
        /// unbekannter Puffer liefert die Vorbelegung. Still wie
        /// <see cref="KlassenSetLesen"/>.
        /// </summary>
        public static Schichtdaten SchichtdatenLesen(int idPuffer)
        {
            if (idPuffer <= 0) return new Schichtdaten();

            DataTable dt = StilleDb.Tabelle(
                "SELECT * FROM Tab_Pufferspeicher WHERE ID = ?",
                StilleDb.Par("@id", DbParamTyp.Integer, idPuffer));

            if (dt == null || dt.Rows.Count == 0) return new Schichtdaten();

            return SchichtdatenAusZeile(dt.Rows[0]);
        }

        /// <summary>
        /// Die Schichtenzahl ALLER Puffer eines Projekts in EINER Abfrage; nie
        /// <c>null</c>, Schlüssel ist die Puffer-ID.
        ///
        /// <para>Für Aufrufer, die ohnehin alle Speicher eines Projekts zeigen — die
        /// Speicherkarten der Konfigurationsseite. Dieselbe Begründung wie bei
        /// <see cref="KlassenSetsJeProjekt"/>: Bei Projekt 1023 der Referenzmenge wären
        /// Einzelabfragen über 80 Rundreisen für einen Seitenaufbau.</para>
        ///
        /// <para>Aufgenommen werden nur Speicher mit <c>N &gt; 1</c> — allein danach
        /// fragen die Aufrufer, und eine Abbildung mit 80 Einsern wäre nur Ballast.</para>
        /// </summary>
        public static Dictionary<int, int> SchichtenJeProjekt(int idProjekt)
        {
            Dictionary<int, int> schichten = new Dictionary<int, int>();
            if (idProjekt <= 0) return schichten;

            DataTable dt = StilleDb.Tabelle(
                "SELECT * FROM Tab_Pufferspeicher WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
            if (dt == null) return schichten;

            foreach (DataRow r in dt.Rows)
            {
                int id = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                if (id <= 0 || schichten.ContainsKey(id)) continue;

                int n = SchichtdatenAusZeile(r).Schichten;
                if (n > SCHICHTEN_DEFAULT) schichten[id] = n;
            }

            return schichten;
        }

        /// <summary>
        /// Schreibt die Schichtdaten an eine Puffer-Zeile — ein EIGENES, zielgenaues
        /// UPDATE, aus demselben Grund wie <see cref="KlassenSetSchreiben"/>: Auf einer
        /// Datenbank ohne Schemastand 53 ließe eine erweiterte Spaltenliste in
        /// <c>ProjektPuffer.SQL_PUFFER_UPDATE_VOLL</c> das Speichern des GANZEN Speichers
        /// scheitern.
        ///
        /// <para><b>Ohne Anlass keine Spalte.</b> Trägt <paramref name="daten"/> nichts
        /// als die Vorbelegung (<see cref="Schichtdaten.IstVorbelegung"/>) und fehlen die
        /// Spalten noch, geschieht NICHTS — der Rückfall beim Lesen liefert ohnehin
        /// dieselben Werte. Erst eine echte Angabe legt die Spalten an. Damit verändert
        /// das bloße Öffnen und Speichern eines beliebigen Puffers das Schema einer noch
        /// nicht migrierten Datenbank nicht.</para>
        /// </summary>
        public static bool SchichtdatenSchreiben(int idPuffer, Schichtdaten daten)
        {
            if (idPuffer <= 0 || daten == null) return false;

            if (daten.IstVorbelegung && !SchichtSpaltenVorhanden()) return true;
            if (!StelleSchichtSpaltenSicher()) return false;

            return StillNonQuery(
                "UPDATE Tab_Pufferspeicher SET " +
                "[" + SchemaKatalog.SPALTE_PSP_SCHICHTEN_ANZAHL + "] = ?, " +
                "[" + SchemaKatalog.SPALTE_PSP_HOEHE + "] = ?, " +
                "[" + SchemaKatalog.SPALTE_PSP_LAMBDA_EFF + "] = ?, " +
                "[" + SchemaKatalog.SPALTE_PSP_T_NUTZ_BW + "] = ?, " +
                "[" + SchemaKatalog.SPALTE_PSP_ENTNAHME_HEIZUNG + "] = ?, " +
                "[" + SchemaKatalog.SPALTE_PSP_ENTNAHME_BW + "] = ?, " +
                "[" + SchemaKatalog.SPALTE_PSP_ENTNAHME_PROZESS + "] = ?, " +
                "[" + SchemaKatalog.SPALTE_PSP_LADELEISTUNG_MAX + "] = ?, " +
                "[" + SchemaKatalog.SPALTE_PSP_ENTLADELEISTUNG_MAX + "] = ? " +
                "WHERE ID = ?",
                StilleDb.Par("@n", DbParamTyp.Integer, SchichtenKlemmen(daten.Schichten)),
                Zahlpar("@h", daten.Hoehe),
                Zahlpar("@l", daten.LambdaEff),
                Zahlpar("@t", daten.TNutzBW),
                Zahlpar("@eh", daten.EntnahmeHeizung),
                Zahlpar("@eb", daten.EntnahmeBW),
                Zahlpar("@ep", daten.EntnahmeProzess),
                StilleDb.Par("@lp", DbParamTyp.Double, daten.LadeleistungMax),
                StilleDb.Par("@ep2", DbParamTyp.Double, daten.EntladeleistungMax),
                StilleDb.Par("@id", DbParamTyp.Integer, idPuffer)) > 0;
        }

        /// <summary>Ein nullbarer Kommazahl-Parameter; <c>null</c> geht als DBNull.</summary>
        private static DbParam Zahlpar(string name, double? wert)
        {
            DbParam p = new DbParam(name, DbParamTyp.Double);
            p.Wert = wert.HasValue ? (object)wert.Value : DBNull.Value;
            return p;
        }

        /// <summary>Ergebnis der reinen Leseprobe (siehe <see cref="SchichtSpaltenVorhanden"/>).</summary>
        private static bool? _schichtSpaltenVorhanden;

        /// <summary>Ergebnis der Vorsorge mit Anlegen (siehe <see cref="StelleSchichtSpaltenSicher"/>).</summary>
        private static bool? _schichtSpaltenBereit;

        /// <summary>
        /// REINE LESEPROBE: Führt die Datenbank die Schichtspalten schon? Legt NICHTS an.
        ///
        /// <para>Der Unterschied zu <see cref="StelleSchichtSpaltenSicher"/> ist der
        /// Zweck: Diese Probe beantwortet „darf ich schreiben, ohne das Schema
        /// anzufassen?" und hält damit eine noch nicht migrierte Datenbank unverändert,
        /// solange niemand eine Schichtangabe macht. Einmal je Prozess.</para>
        /// </summary>
        public static bool SchichtSpaltenVorhanden()
        {
            if (_schichtSpaltenVorhanden.HasValue) return _schichtSpaltenVorhanden.Value;

            bool ok = false;
            try
            {
                // ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht.
                ok = SchichtSpaltenProbe();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Schichtspalten nicht pruefbar: " + ex.Message);
                ok = false;
            }

            _schichtSpaltenVorhanden = ok;
            return ok;
        }

        /// <summary>
        /// Tolerante Vorsorge für die neun Schichtspalten (Migrationsschritt 53)
        /// unmittelbar vor dem SCHREIBEN — dasselbe Muster und dieselbe Begründung wie
        /// <see cref="StelleKlassenSetSpaltenSicher"/> für Schritt 49.
        ///
        /// <para>Die Leseseite braucht sie nicht: <see cref="SchichtdatenAusZeile"/> prüft
        /// die Spaltennamen und fällt auf die Vorbelegung zurück. Der Schreibweg dagegen
        /// nennt die Spalten ausgeschrieben.</para>
        ///
        /// <para>Kollidiert NICHT mit dem Migrationsschritt: Dessen <c>ALTER TABLE</c>
        /// schluckt eine bereits vorhandene Spalte (Muster
        /// <c>SchemaMigration.Schritt_49_Klassenset</c>), und seine verhaltensneutrale
        /// Vorbelegung trifft dieselben Werte, die hier ohnehin stehen.</para>
        ///
        /// <para>Eigene Verbindung statt <c>DataRepository</c>, weil eine Vorsorge kein
        /// Bedienschritt ist und keine MessageBox zeigen darf. Einmal je Prozess. Die
        /// Leseprobe am Ende ist Nachweis statt Annahme — das <c>ALTER</c> schluckt jeden
        /// Fehler, auch einen echten.</para>
        /// </summary>
        public static bool StelleSchichtSpaltenSicher()
        {
            if (_schichtSpaltenBereit.HasValue) return _schichtSpaltenBereit.Value;

            bool ok = false;
            try
            {
                // ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht, Vorabprobe
                // statt geschlucktem ALTER-Fehler (S4c/S4d vorgezogen).
                HashSet<string> vorhanden = StilleDb.SpaltenNamen(SchemaKatalog.TAB_PUFFERSPEICHER);

                foreach (KeyValuePair<string, string> spalte in SchichtSpaltenTypen())
                {
                    if (vorhanden != null && vorhanden.Contains(spalte.Key)) continue;
                    StilleDb.NonQuery(StilleDb.AlterTableAddColumn(
                        SchemaKatalog.TAB_PUFFERSPEICHER, spalte.Key, spalte.Value));
                }

                ok = SchichtSpaltenProbe();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Schichtspalten nicht verfuegbar: " + ex.Message);
                ok = false;
            }

            _schichtSpaltenBereit = ok;
            if (ok) _schichtSpaltenVorhanden = true;   // die Leseprobe ist mit beantwortet
            return ok;
        }

        /// <summary>Spaltenname → Access-Typ, in der Reihenfolge des Spaltenvertrags.</summary>
        private static List<KeyValuePair<string, string>> SchichtSpaltenTypen()
        {
            List<KeyValuePair<string, string>> liste = new List<KeyValuePair<string, string>>();
            liste.Add(new KeyValuePair<string, string>(SchemaKatalog.SPALTE_PSP_SCHICHTEN_ANZAHL, "LONG"));
            liste.Add(new KeyValuePair<string, string>(SchemaKatalog.SPALTE_PSP_HOEHE, "DOUBLE"));
            liste.Add(new KeyValuePair<string, string>(SchemaKatalog.SPALTE_PSP_LAMBDA_EFF, "DOUBLE"));
            liste.Add(new KeyValuePair<string, string>(SchemaKatalog.SPALTE_PSP_T_NUTZ_BW, "DOUBLE"));
            liste.Add(new KeyValuePair<string, string>(SchemaKatalog.SPALTE_PSP_ENTNAHME_HEIZUNG, "DOUBLE"));
            liste.Add(new KeyValuePair<string, string>(SchemaKatalog.SPALTE_PSP_ENTNAHME_BW, "DOUBLE"));
            liste.Add(new KeyValuePair<string, string>(SchemaKatalog.SPALTE_PSP_ENTNAHME_PROZESS, "DOUBLE"));
            liste.Add(new KeyValuePair<string, string>(SchemaKatalog.SPALTE_PSP_LADELEISTUNG_MAX, "DOUBLE"));
            liste.Add(new KeyValuePair<string, string>(SchemaKatalog.SPALTE_PSP_ENTLADELEISTUNG_MAX, "DOUBLE"));
            return liste;
        }

        /// <summary>
        /// Nachweis statt Annahme: EIN <c>SELECT</c> über alle neun Spalten. Schlägt er
        /// fehl, fehlt mindestens eine — dann bleibt der Schreibweg aus, und die Leseseite
        /// arbeitet mit der Vorbelegung weiter.
        /// </summary>
        private static bool SchichtSpaltenProbe()
        {
            List<string> namen = new List<string>();
            foreach (KeyValuePair<string, string> s in SchichtSpaltenTypen())
                namen.Add("[" + s.Key + "]");

            // ARBEITSPAKET S4b: SELECT TOP 1 -> LIMIT 1 (S5 vorgezogen).
            if (!StillProbe("SELECT " + string.Join(", ", namen.ToArray()) +
                            " FROM [" + SchemaKatalog.TAB_PUFFERSPEICHER + "] LIMIT 1"))
            {
                Console.WriteLine("Schichtspalten-Leseprobe fehlgeschlagen.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Leseprobe: true, wenn die Abfrage AUSFUEHRBAR ist - unabhaengig davon, ob sie
        /// eine Zeile oder einen Wert liefert. ARBEITSPAKET S4b: Genau diese
        /// Unterscheidung leistet <c>StilleDb.Scalar</c> NICHT (dort ist null sowohl
        /// "Fehler" als auch "kein Wert"), deshalb steht sie hier.
        /// </summary>
        private static bool StillProbe(string sql, params DbParam[] parameter)
        {
            try
            {
                using (SqliteConnection conn = StilleDb.OeffneVerbindung())
                using (SqliteCommand cmd = DataRepository.ErzeugeKommando(conn, null, sql, parameter))
                {
                    cmd.ExecuteScalar();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Pufferspeicher-Leseprobe fehlgeschlagen: " + ex.Message);
                return false;
            }
        }

        /// <summary>Ein ganzzahliges Feld; fehlende Spalte, NULL und Unlesbares ergeben <c>null</c>.</summary>
        private static int? ZahlOderNull(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte)) return null;

            object v = r[spalte];
            if (v == null || v == DBNull.Value) return null;

            try { return Convert.ToInt32(v); }
            catch { return null; }
        }

        /// <summary>Ein Kommazahl-Feld; fehlende Spalte, NULL und Unlesbares ergeben <c>null</c>.</summary>
        private static double? KommazahlOderNull(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte)) return null;

            object v = r[spalte];
            if (v == null || v == DBNull.Value) return null;

            try { return Convert.ToDouble(v); }
            catch { return null; }
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
                // ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht. Der Leser
                // bleibt ein Leser (zwei Spalten einer Zeile), die Meldung bleibt
                // dieselbe - deshalb innen umgestellt statt ueber StilleDb.
                using (SqliteConnection conn = StilleDb.OeffneVerbindung())
                using (SqliteCommand cmd = DataRepository.ErzeugeKommando(
                           conn, null, ProjektPuffer.SQL_PUFFER_TEMPERATUREN,
                           new[] { new DbParam("@id", idPuffer) }))
                using (SqliteDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return false;
                    if (r.IsDBNull(0) || r.IsDBNull(1)) return false;

                    vorlauf = Convert.ToInt32(r.GetValue(0));
                    ruecklauf = Convert.ToInt32(r.GetValue(1));
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

        // PAKET A1: SetTemperaturen und TemperaturenLoeschen sind ENTFALLEN.
        //
        // Beide gehoerten zur Nachfuehrung der Betriebstemperaturen aus der Alt-Zuordnung
        // Z_ProjektPufferSp an die Puffer-Zeile ("fuehrende Ablage", Etappe 4): Beim
        // Speichern der Konfiguration uebertrug Form_Simulation_Config die Werte der
        // fuehrenden Waermepumpen-Zuordnung an den Speicher, und das Leeren beider Zellen
        // setzte sie dort wieder auf NULL. Ihr einziger Aufrufer ist mit dem Abriss der
        // Alt-Zuordnung gegangen.
        //
        // Tab_Pufferspeicher ist jetzt die EINZIGE Ablage der Betriebstemperaturen;
        // gepflegt werden sie in Form_PufferSp_Projekt, das seinen eigenen Schreibweg hat.
        // Migrationsschritt 51 hat die Werte der Alt-Zuordnung einmalig uebernommen.

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

            // ARBEITSPAKET S4b: SELECT TOP 1 -> LIMIT 1 (S5 vorgezogen).
            object v = StillScalar(
                "SELECT Gesamtvolumen FROM Tab_Pufferspeicher " +
                "WHERE ID_Projekt = ? AND Bezeichner = ? ORDER BY ID LIMIT 1",
                new DbParam("@idProj", idProjekt),
                new DbParam("@bez", ProjektPuffer.BEZ_PENDELSPEICHER));

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
                    new DbParam("@vol", liter),
                    new DbParam("@id", idPuffer)) >= 0;
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

            // KLASSEN-SET (Migrationsschritt 49): SQL_PUFFER_INSERT schreibt
            // ProjektPuffer.VERWENDUNG_HEIZUNG - das Set wird aus genau diesem Wert
            // abgeleitet statt hier ein zweites Mal festgelegt. Eine Wahrheit je Frage.
            KlassenSetSchreiben(neueId, KlassenSetAusVerwendung(ProjektPuffer.VERWENDUNG_HEIZUNG));

            // Anlagenzeile nachtragen, damit der Speicher im Projektbaum erscheint -
            // dieselbe Regel wie R4 der Migration (eine Zeile je Projekt+Bezeichner).
            // Wie in ProjektPufferAnlegen: Name UND Geräteverweis prüfen - eine Zeile je
            // Projekt und Gerät (Teil A der Anlagenzeilen-Eindeutigkeit).
            if (!AnlagenzeileVorhanden(idProjekt, ProjektPuffer.BEZ_PENDELSPEICHER) &&
                !Anlagenzeilen.ZeileVorhanden(Anlagenzeilen.SPALTE_PUFFER, idProjekt, neueId))
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
            // ARBEITSPAKET S4b: SELECT TOP 1 -> LIMIT 1 (S5 vorgezogen).
            object v = StillScalar(
                "SELECT ID FROM Tab_Pufferspeicher " +
                "WHERE ID_Projekt = ? AND Bezeichner = ? ORDER BY ID LIMIT 1",
                new DbParam("@idProj", idProjekt),
                new DbParam("@bez", ProjektPuffer.BEZ_PENDELSPEICHER));

            if (v == null || v == DBNull.Value) return 0;
            try { return Convert.ToInt32(v); }
            catch { return 0; }
        }

        private static bool AnlagenzeileVorhanden(int idProjekt, string bezeichner)
        {
            object v = StillScalar(
                "SELECT COUNT(*) FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type = ? AND Bezeichner = ?",
                new DbParam("@idProj", idProjekt),
                new DbParam("@typ", ProjektPuffer.TYP_PUFFER),
                new DbParam("@bez", bezeichner ?? ""));

            if (v == null || v == DBNull.Value) return false;
            try { return Convert.ToInt32(v) > 0; }
            catch { return false; }
        }

        /// <summary>
        /// Skalare Abfrage OHNE Dialog. DataRepository zeigt im Fehlerfall eine
        /// MessageBox - im Engine-Pfad waere das ein haengender Lauf (der Referenzlauf
        /// braucht dafuer eigens einen Dialogwaechter).
        ///
        /// ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht. Innen umgestellt und
        /// NICHT auf <see cref="StilleDb.Scalar"/> zurueckgefuehrt, aus zwei Gruenden:
        /// der eigene Meldungstext, und die Rueckgabe - diese Fassung reicht
        /// <c>DBNull</c> DURCH (die Aufrufer pruefen darauf), StilleDb macht null daraus.
        /// </summary>
        private static object StillScalar(string sql, params DbParam[] parameter)
        {
            try
            {
                using (SqliteConnection conn = StilleDb.OeffneVerbindung())
                using (SqliteCommand cmd = DataRepository.ErzeugeKommando(conn, null, sql, parameter))
                {
                    return cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Pufferspeicher-Abfrage fehlgeschlagen: " + ex.Message);
                return null;
            }
        }

        /// <summary>Schreibende Anweisung ohne Dialog; -1 bei Fehler.
        /// ARBEITSPAKET S4b: innen umgestellt - eigener Meldungstext wie oben.</summary>
        private static int StillNonQuery(string sql, params DbParam[] parameter)
        {
            try
            {
                using (SqliteConnection conn = StilleDb.OeffneVerbindung())
                using (SqliteCommand cmd = DataRepository.ErzeugeKommando(conn, null, sql, parameter))
                {
                    return cmd.ExecuteNonQuery();
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
                new DbParam("@idProj", idProjekt)));
        }

        /// <summary>Liefert die IDs der Puffer-Zeilen, die ein Filter trifft.</summary>
        private static List<int> BetroffeneIds(string sql, params DbParam[] parameter)
        {
            List<int> ids = new List<int>();
            try
            {
                // ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht; Meldungstext
                // und Rueckgabe (leere Liste im Fehlerfall) bleiben.
                using (SqliteConnection conn = StilleDb.OeffneVerbindung())
                using (SqliteCommand cmd = DataRepository.ErzeugeKommando(conn, null, sql, parameter))
                using (SqliteDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        if (!r.IsDBNull(0)) ids.Add(Convert.ToInt32(r.GetValue(0)));
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
        ///
        /// PAKET PARALLELVERBUND: Die Verbundzeilen werden GELOESCHT, nicht genullt - eine
        /// Zuordnungszeile ohne Puffer hat keine Bedeutung (dieselbe Behandlung wie
        /// Z_ProjektPufferSp in ProjektPufferEntfernen). Ohne diesen Schritt scheiterte das
        /// DELETE FROM Tab_Pufferspeicher an der restriktiven Beziehung FK_Verbund_Puffer,
        /// genau wie es ohne das Nullen von WS_ID_Puffer scheitern wuerde.
        ///
        /// PAKET S1 (Migrationsschritt 50): Die Zeilen der SENKENLISTE werden nach der
        /// Regel behandelt, die WaermesenkeClass.Normalisieren beim Lesen ohnehin
        /// anwendet - und deshalb GETRENNT nach Rang:
        ///
        ///   Rang 1  wird auf 'Heizkreis' NORMALISIERT (Ziel, Ladeprio, Ladegrenze und
        ///           PV-Prioritaet zurueckgesetzt, ID_Puffer geleert). Das ist Regel N5:
        ///           "Puffer-Ziel ohne Puffer -> Heizkreis". Geloescht werden darf die
        ///           Zeile nicht - Rang 1 ist Pflicht (Konzept § 5.1), und ohne sie
        ///           faende die Engine gar keine Senke mehr.
        ///   Rang >= 2 wird GELOESCHT. Auch das ist Bestandsverhalten: Eine Zweitsenke
        ///           ohne Puffer raeumt Normalisieren heute vollstaendig ab
        ///           (Ziel2 = ""), sie ist danach keine Senke mehr.
        ///
        /// Beides ist wirkungsgleich mit dem, was der Lauf ohnehin gerechnet haette -
        /// das Loesen aendert also nur die ABLAGE, nicht das Ergebnis.
        /// </summary>
        private static void ReferenzenLoesen(List<int> pufferIds)
        {
            if (pufferIds == null || pufferIds.Count == 0) return;

            AnlagePufferVerbundCtrl.ReferenzenEntfernen(pufferIds);

            string liste = string.Join(",", pufferIds);

            // S1: die Senkenliste. STILL wie die Spalten-Schleife unten - fehlt die
            // Tabelle (Schritt 50 noch nicht gelaufen), gibt es dort auch nichts zu
            // loesen, und das Loeschen des Puffers darf daran nicht scheitern.
            try
            {
                StilleDb.NonQuery(
                    "UPDATE [" + Z_AnlageSenkeCtrl.TABLE + "] " +
                    "SET Ziel = '" + DbWerte.WS_ZIEL_HEIZKREIS + "', ID_Puffer = NULL, " +
                    "    Ladeprio = 0, Ladeprio_PV = 0, Ladegrenze = 0 " +
                    "WHERE Rang = 1 AND ID_Puffer IN (" + liste + ")");

                StilleDb.NonQuery(
                    "DELETE FROM [" + Z_AnlageSenkeCtrl.TABLE + "] " +
                    "WHERE Rang > 1 AND ID_Puffer IN (" + liste + ")");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Senkenzeilen auf Pufferspeicher nicht geloest: " + ex.Message);
            }
            try
            {
                // ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht, je Spalte ein
                // eigener Aufruf (KEINE Transaktion darum - auch bisher stand hier
                // keine). Der Ausnahmetext steht in StillNonQuery, die Zuordnung zur
                // Spalte in der Zeile darunter.
                foreach (string spalte in PUFFER_REFERENZEN)
                {
                    // z. B. Spalte noch nicht angelegt - kein Grund, das Loeschen zu stoppen
                    if (StillNonQuery(
                            "UPDATE Tab_Energieanlagen SET [" + spalte + "] = NULL " +
                            "WHERE [" + spalte + "] IN (" + liste + ")") < 0)
                        Console.WriteLine("Referenz " + spalte + " nicht geloest.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Referenzen auf Pufferspeicher konnten nicht geloest werden: " + ex.Message);
            }
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
