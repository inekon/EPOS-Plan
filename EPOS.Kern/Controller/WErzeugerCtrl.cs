using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // Die Klasse ist partial: Der PROJEKT-LOESCHWEG (Delete) steht in
    // WErzeugerCtrl.Aufraeumen.cs, weil er als einziger Teil den Aufraeumlauf
    // GeraeteWaisen ruft. Beide Haelften liegen im Kern.
    partial class WErzeugerCtrl : WErzeugerModel
    {
        private List<WErzeugerModel> _internalList = new List<WErzeugerModel>();
        public int rows => _internalList.Count;
        public new List<WErzeugerModel> items => _internalList;

        public WErzeugerCtrl()
        {
        }

        /// <summary>
        /// Ändert die Grunddaten einer Anlagenzeile.
        ///
        /// <para>
        /// Bewusst NICHT auf den vollen Spaltensatz erweitert: Ein UPDATE lässt die
        /// nicht genannten Spalten unverändert stehen - es ist also nicht verlustbehaftet
        /// und damit keine zweite Wahrheit über die Quellen-/Senken-Konfiguration. Die
        /// pflegen <c>WaermesenkeClass.Schreiben</c> und
        /// <c>WaermequelleClass.WertSchreiben</c> gezielt je Anlage. Verlustbehaftet ist
        /// allein der Weg Löschen + Neuanlegen - der geht über
        /// <see cref="Insert"/> bzw. <c>WizardCtrl.SQL_ANLAGE_INSERT</c>.
        /// </para>
        /// </summary>
        public bool Update()
        {
            try
            {
                string sql = @"UPDATE Tab_Energieanlagen 
                               SET ID_Projekt = ?, Bezeichner = ?, ID_Type = ?, ID_WP = ?, Betriebsart = ?, 
                                   Sperrung = ?, Sperrzeit_von = ?, Sperrzeit_bis = ?, Vorlauf = ?, Rücklauf = ?,
                                   Bivalenter_Betrieb = ?, Abschaltpunkt = ?, Nutzungszeit = ?, ID_SP = ?, ID_PV = ?, ID_Solar = ?
                               WHERE ID = ?";

                DbParam[] ps = {
                    new DbParam("@idProj", ID_Projekt),
                    new DbParam("@bez", Bezeichner ?? (object)DBNull.Value),
                    new DbParam("@idType", ID_Type),
                    new DbParam("@idWp", ID_WP),
                    new DbParam("@betr", Betriebsart ?? (object)DBNull.Value),
                    new DbParam("@sperr", Sperrung),
                    new DbParam("@von", Sperrzeit_von),
                    new DbParam("@bis", Sperrzeit_bis),
                    new DbParam("@vor", Vorlauf),
                    new DbParam("@rue", Ruecklauf),
                    new DbParam("@biv", Bivalenter_Betrieb),
                    new DbParam("@absch", Abschaltpunkt),
                    new DbParam("@nutz", Nutzungszeit),
                    new DbParam("@idSp", ID_SP),
                    new DbParam("@idPv", ID_PV),
                    new DbParam("@idSol", ID_Solar),
                    new DbParam("@id", ID) // Die ID am Ende bestimmt die WHERE-Klausel
                };

                bool ok = DataRepository.ExecuteSQL(sql, ps);
                if (ok) { StromTraegerNachziehen(); ProjektGeaendert(); }
                return ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Update: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Legt eine Anlagenzeile an - über DIESELBE Anweisung wie der Wizard-Weg
        /// (<see cref="AnlagenSql.SQL_ANLAGE_INSERT"/>).
        ///
        /// <para>
        /// Die frühere eigene Fassung führte 21 der 57 Spalten und schrieb
        /// <c>WS_Ladeprio*</c>/<c>WS_Ladegrenze*</c> mit hartkodierter 0. Das war die
        /// zweite Halbwahrheit über denselben Datensatz: zwei Einfügewege mit
        /// unterschiedlichem Spaltensatz. Die 0 („nach Vorgabe" bzw. „nicht gesetzt",
        /// Konzept 3.4, Paket-4-Review Punkt 9) steckt jetzt in der Vorbelegung von
        /// <see cref="WErzeugerModel"/> und gilt damit für JEDEN Einfügeweg; die
        /// Fremdschlüsselspalten (<c>WS_ID_Puffer</c> &amp; Co.) bleiben dort NULL.
        /// </para>
        /// </summary>
        #region --- SCHMALES UPDATE DER ANLAGENKONFIGURATION ---

        /// <summary>
        /// Was ein Schreibversuch an der Anlagenkonfiguration ergeben hat — derselbe
        /// Zuschnitt wie <see cref="WPCtrl.SpeicherErgebnis"/>.
        /// </summary>
        /// <param name="Ok">Wurde geschrieben?</param>
        /// <param name="Meldung">Der Grund im Klartext, bereits lokalisiert.</param>
        /// <param name="Name">Der Bezeichner der Anlage — er aendert sich hier nie.</param>
        public sealed record SpeicherErgebnis(bool Ok, string Meldung, string Name);

        /// <summary>
        /// Die Felder der WAERMEPUMPEN-KONFIGURATION an der Anlagenzeile
        /// (Anwenderentscheid 16.09.2026) — genau die, die der Konfigurationsknopf der
        /// Karte zeigt.
        /// </summary>
        /// <remarks>
        /// <para><b><c>null</c> heisst „unveraendert lassen"</b>, wie bei
        /// <see cref="WPCtrl.ProjektgeraetFelder"/>: Der Schreibweg liest den Satz, legt
        /// die mitgegebenen Felder darueber und schreibt zurueck. Ein ausgelassenes Feld
        /// wuerde sonst als 0 oder leer ueber einen gepflegten Wert laufen.</para>
        ///
        /// <para><b>Der Bezeichner steht nicht darin</b> und die Geraeteverweise
        /// (<c>ID_WP</c> &amp; Co.) auch nicht — das ist keine Konfiguration, sondern die
        /// Identitaet der Zeile. Wer sie aendert, geht weiter ueber den Speicherweg des
        /// Assistenten.</para>
        /// </remarks>
        /// <param name="Heizstab">
        /// „Mit Heizstab" — seit dem Entscheid der EINE Schalter, den der Lauf je
        /// Waermepumpe liest (<c>SimulationWaermepumpe.ModuleAufbauen</c>).
        /// </param>
        /// <param name="Sperrung">Sperrzeit beachten?</param>
        /// <param name="SperrzeitVon">Beginn der Sperrzeit [h].</param>
        /// <param name="SperrzeitBis">Ende der Sperrzeit [h].</param>
        /// <param name="BivalenterBetrieb">Bivalenter Betrieb?</param>
        /// <param name="Betriebsart">
        /// <c>Tab_Energieanlagen.Betriebsart</c> (monovalent / bivalent-parallel /
        /// bivalent-alternativ); die Engine vergleicht sie ZEICHENGLEICH.
        /// </param>
        /// <param name="Abschaltpunkt">Abschaltpunkt bzw. Bivalenztemperatur [Grad C].</param>
        /// <param name="IdCarrier">
        /// Der Energietraeger der Anlage (<c>Tab_Energieanlagen.ID_Carrier</c>).
        /// <c>null</c> heisst wie ueberall „nicht anfassen"; <b>0 heisst ausdruecklich
        /// „kein Energietraeger"</b> und wird als 0 geschrieben. Die Spalte fuehrt im
        /// Bestand beide Schreibweisen fuer „keiner" (NULL und 0), und der lesende Code
        /// behandelt sie gleich (SchemaKatalog, Schritt 8).
        /// </param>
        /// <param name="KuehlIdCarrier">
        /// Der Stromtraeger des Kaeltestroms (<c>Tab_Energieanlagen.Kuehl_ID_Carrier</c>, KU-S3;
        /// K9, E33). <c>null</c> heisst „nicht anfassen"; <b>0 (oder kleiner) heisst „wie
        /// Heizbetrieb"</b> und wird als NULL geschrieben - die Spalte kennt keine 0, sie steht
        /// unter einer Beziehung auf <c>energy_carrier.id</c>. Ein Traeger, den es nicht gibt,
        /// faellt ebenso auf NULL (<see cref="AnlagenSql.TraegerVerweisOderNull"/>).
        /// </param>
        /// <param name="KuehlEigenerZaehler">
        /// Die Abrechnungsart des Kaeltestroms (<c>Tab_Energieanlagen.Kuehl_EigenerZaehler</c>,
        /// Schemaschritt 115; E34). <c>null</c> heisst „nicht anfassen"; <b><c>true</c> = eigener
        /// Zaehler</b> (geschrieben als 1), <b><c>false</c> = anteilig am Netzbezug</b>, die Vorgabe
        /// — geschrieben als NULL, nie als 0 (<see cref="AnlagenSql.EigenerZaehlerOderNull"/>).
        /// </param>
        public sealed record KonfigurationFelder(bool? Heizstab = null,
                                                 bool? Sperrung = null,
                                                 int? SperrzeitVon = null,
                                                 int? SperrzeitBis = null,
                                                 bool? BivalenterBetrieb = null,
                                                 string Betriebsart = null,
                                                 double? Abschaltpunkt = null,
                                                 int? IdCarrier = null,
                                                 int? KuehlIdCarrier = null,
                                                 bool? KuehlEigenerZaehler = null);

        /// <summary>
        /// Schreibt die Konfigurationsfelder EINER Anlagenzeile — der Speicherweg des
        /// Konfigurationsknopfs (Anwenderentscheid 16.09.2026).
        /// </summary>
        /// <remarks>
        /// <para><b>Warum es diesen Weg neben <see cref="Update"/> gibt.</b>
        /// <see cref="Update"/> fuehrt weder <c>Heizstab</c> noch <c>ID_Carrier</c> — die
        /// zwei Felder, um die es hier vor allem geht. Und der uebliche Speicherweg der
        /// Oberflaeche ist LOESCHEN + NEUANLEGEN
        /// (<c>WizardCtrl.Del_Projekt_Waermeerzeuger</c> + <c>Add_WP_Waermeerzeuger</c>):
        /// Er vergibt neue Anlagen-Ids, und jede Zuordnung, die an der alten Id haengt —
        /// Kostenzeilen, Straenge, Senken —, muss gerettet werden. Fuer die Aenderung von
        /// acht Feldern ist das der falsche Preis.</para>
        ///
        /// <para><b>Adressiert wird ueber <c>ID</c> UND <c>ID_Projekt</c></b>, nie ueber
        /// den Bezeichner — dieselbe Lehre wie bei
        /// <see cref="WPCtrl.ProjektgeraetSchreiben"/>: Anlagenbezeichner sind nicht
        /// eindeutig.</para>
        ///
        /// <para><b>Read-modify-write.</b> Was der Aufrufer nicht mitgibt (<c>null</c>),
        /// wird aus dem gelesenen Satz zurueckgeschrieben — auch <c>NULL</c> bleibt
        /// <c>NULL</c>. Die Ids der Zeile bleiben, wie sie sind.</para>
        ///
        /// <para><b>Der Stromtraeger wird nachgezogen</b> wie in <see cref="Update"/>:
        /// Wer den Heizstab einschaltet, hebt die Anlage in die elektrische Welt, und das
        /// Projekt braucht dann seinen Stromtraeger (ET-2). Ein Fehlschlag dabei bricht
        /// das Speichern nicht ab.</para>
        /// </remarks>
        /// <param name="idAnlage">Die Anlagenzeile (<c>Tab_Energieanlagen.ID</c>).</param>
        /// <param name="idProjekt">Das Projekt (<c>Tab_Energieanlagen.ID_Projekt</c>).</param>
        /// <param name="felder">Die zu schreibenden Felder; <c>null</c> = unveraendert.</param>
        public static SpeicherErgebnis KonfigurationSchreiben(int idAnlage, int idProjekt,
                                                              KonfigurationFelder felder)
        {
            if (felder == null || idAnlage <= 0 || idProjekt <= 0)
                return new SpeicherErgebnis(false, Text("ANL_KONFIG_MSG_FEHLER",
                    "Die Konfiguration der Anlage konnte nicht gespeichert werden."), "");

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, Heizstab, Sperrung, Sperrzeit_von, Sperrzeit_bis, " +
                    "Bivalenter_Betrieb, Betriebsart, Abschaltpunkt, [" +
                    SchemaKatalog.SPALTE_ID_CARRIER + "], [" +
                    KuehlungSchema.SPALTE_KUEHL_ID_CARRIER + "], [" +
                    KuehlungSchema.SPALTE_KUEHL_EIGENER_ZAEHLER + "] " +
                    "FROM Tab_Energieanlagen WHERE ID = ? AND ID_Projekt = ?",
                    new DbParam("@id", idAnlage), new DbParam("@proj", idProjekt));

                if (dt == null || dt.Rows.Count == 0)
                    return new SpeicherErgebnis(false,
                        string.Format(Text("ANL_KONFIG_MSG_NICHT_GEFUNDEN",
                            "Die Anlage {0} wurde nicht gefunden."), idAnlage), "");

                DataRow satz = dt.Rows[0];
                string bezeichner = Feldtext(satz, "Bezeichner");

                // Die Ja/Nein-Spalten sind NOT NULL mit CHECK (spalte IN (0,1)) - sie
                // bekommen deshalb IMMER einen Wert, nie DBNull. Die uebrigen sind
                // NULL-treu: Was nicht mitkommt, wird zurueckgeschrieben, wie es stand.
                bool ok = DataRepository.ExecuteSQL(
                    "UPDATE Tab_Energieanlagen SET Heizstab = ?, Sperrung = ?, " +
                    "Sperrzeit_von = ?, Sperrzeit_bis = ?, Bivalenter_Betrieb = ?, " +
                    "Betriebsart = ?, Abschaltpunkt = ?, [" +
                    SchemaKatalog.SPALTE_ID_CARRIER + "] = ?, [" +
                    KuehlungSchema.SPALTE_KUEHL_ID_CARRIER + "] = ?, [" +
                    KuehlungSchema.SPALTE_KUEHL_EIGENER_ZAEHLER + "] = ? " +
                    "WHERE ID = ? AND ID_Projekt = ?",
                    new DbParam("@stab", Uebernommen(felder.Heizstab, satz, "Heizstab")),
                    new DbParam("@sperr", Uebernommen(felder.Sperrung, satz, "Sperrung")),
                    ProjektPuffer.Par("@svon", DbParamTyp.Integer,
                        Uebernommen(felder.SperrzeitVon, satz, "Sperrzeit_von")),
                    ProjektPuffer.Par("@sbis", DbParamTyp.Integer,
                        Uebernommen(felder.SperrzeitBis, satz, "Sperrzeit_bis")),
                    new DbParam("@biv", Uebernommen(felder.BivalenterBetrieb, satz,
                                                    "Bivalenter_Betrieb")),
                    ProjektPuffer.Par("@art", DbParamTyp.VarWChar,
                        Uebernommen(felder.Betriebsart, satz, "Betriebsart")),
                    ProjektPuffer.Par("@ab", DbParamTyp.Double,
                        Uebernommen(felder.Abschaltpunkt, satz, "Abschaltpunkt")),
                    ProjektPuffer.Par("@carrier", DbParamTyp.Integer,
                        Uebernommen(felder.IdCarrier, satz, SchemaKatalog.SPALTE_ID_CARRIER)),
                    ProjektPuffer.Par("@kuehlcarrier", DbParamTyp.Integer,
                        KuehlTraeger(felder.KuehlIdCarrier, satz, bezeichner)),
                    ProjektPuffer.Par("@kuehlzaehler", DbParamTyp.Integer,
                        KuehlAbrechnung(felder.KuehlEigenerZaehler, satz)),
                    new DbParam("@id", idAnlage),
                    new DbParam("@proj", idProjekt));

                if (!ok)
                    return new SpeicherErgebnis(false, Text("ANL_KONFIG_MSG_FEHLER",
                        "Die Konfiguration der Anlage konnte nicht gespeichert werden."),
                        bezeichner);

                // ET-2 wie in Update(): Wer den Heizstab einschaltet, hebt die Anlage in
                // die elektrische Welt. Idempotent, und ein Fehlschlag bricht nichts ab.
                try
                {
                    bool elektrisch = Ja(Uebernommen(felder.Heizstab, satz, "Heizstab"));
                    if (elektrisch) ProjektEnergietraegerCtrl.StromTraegerSicherstellen(idProjekt);
                }
                catch { }

                return new SpeicherErgebnis(true, Text("ANL_KONFIG_MSG_GESPEICHERT",
                    "Konfiguration gespeichert"), bezeichner);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Schreiben der Anlagenkonfiguration: " + ex.Message);
                return new SpeicherErgebnis(false, Text("ANL_KONFIG_MSG_FEHLER",
                    "Die Konfiguration der Anlage konnte nicht gespeichert werden."), "");
            }
        }

        /// <summary>
        /// Der Stromtraeger der Kuehlung fuer <see cref="KonfigurationSchreiben"/> (KU-S3): <c>null</c>
        /// = der gelesene Wert (NULL bleibt NULL), kleiner gleich 0 = NULL („wie Heizbetrieb"),
        /// sonst der Traeger - sofern es ihn gibt (<see cref="AnlagenSql.TraegerVerweisOderNull"/>).
        /// </summary>
        private static object KuehlTraeger(int? neu, DataRow satz, string bezeichner)
        {
            if (!neu.HasValue)
                return Wertoderleer(Feldwert(satz, KuehlungSchema.SPALTE_KUEHL_ID_CARRIER));
            return AnlagenSql.TraegerVerweisOderNull(neu.Value, bezeichner);
        }

        /// <summary>
        /// Die Abrechnungsart des Kaeltestroms fuer <see cref="KonfigurationSchreiben"/>
        /// (Schemaschritt 115; E34): <c>null</c> = der gelesene Wert (NULL bleibt NULL), sonst 1 fuer
        /// den eigenen Zaehler und NULL fuer „anteilig" (<see cref="AnlagenSql.EigenerZaehlerOderNull"/>).
        /// </summary>
        private static object KuehlAbrechnung(bool? neu, DataRow satz)
        {
            if (!neu.HasValue)
                return Wertoderleer(Feldwert(satz, KuehlungSchema.SPALTE_KUEHL_EIGENER_ZAEHLER));
            return AnlagenSql.EigenerZaehlerOderNull(neu.Value);
        }

        /// <summary>Der neue Wahrheitswert, sonst der gelesene (0/1, nie <c>NULL</c>).</summary>
        private static object Uebernommen(bool? neu, DataRow satz, string spalte)
            => neu.HasValue ? (object)neu.Value : (object)Ja(Feldwert(satz, spalte));

        /// <summary>Die neue Zahl, sonst der gelesene Wert (<c>NULL</c> bleibt <c>NULL</c>).</summary>
        private static object Uebernommen(int? neu, DataRow satz, string spalte)
            => neu.HasValue ? (object)neu.Value : Wertoderleer(Feldwert(satz, spalte));

        /// <summary>Die neue Kommazahl, sonst der gelesene Wert (<c>NULL</c> bleibt <c>NULL</c>).</summary>
        private static object Uebernommen(double? neu, DataRow satz, string spalte)
            => neu.HasValue ? (object)neu.Value : Wertoderleer(Feldwert(satz, spalte));

        /// <summary>Der neue Text, sonst der gelesene Wert (<c>NULL</c> bleibt <c>NULL</c>).</summary>
        private static object Uebernommen(string neu, DataRow satz, string spalte)
            => neu ?? Wertoderleer(Feldwert(satz, spalte));

        /// <summary><c>DBNull</c> wird zu <c>null</c> — so, wie <c>ProjektPuffer.Par</c> es erwartet.</summary>
        private static object Wertoderleer(object v)
            => (v == null || v == DBNull.Value) ? null : v;

        /// <summary>Der rohe Spaltenwert; fehlende Spalte und <c>null</c> ergeben <c>DBNull</c>.</summary>
        private static object Feldwert(DataRow satz, string spalte)
        {
            object v = satz.Table.Columns.Contains(spalte) ? satz[spalte] : DBNull.Value;
            return v ?? DBNull.Value;
        }

        /// <summary>Der Spaltenwert als Text; fehlende Spalte und <c>NULL</c> ergeben „".</summary>
        private static string Feldtext(DataRow satz, string spalte)
        {
            object v = Feldwert(satz, spalte);
            return v == DBNull.Value ? "" : v.ToString();
        }

        /// <summary>Der Spaltenwert als Wahrheitswert; <c>NULL</c> und Unlesbares ergeben <c>false</c>.</summary>
        private static bool Ja(object v)
        {
            if (v == null || v == DBNull.Value) return false;
            try { return Convert.ToBoolean(v); }
            catch { return false; }
        }

        /// <summary>Ressourcentext mit deutschem Rueckfall (Drei-Schichten-Regel).</summary>
        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        #endregion

        public bool Insert()
        {
            try
            {
                bool ok = DataRepository.ExecuteSQL(AnlagenSql.SQL_ANLAGE_INSERT,
                                                    AnlagenSql.AnlagenParameter(ID_Projekt, this));
                if (ok) { StromTraegerNachziehen(); SenkenAnlegen(); ProjektGeaendert(); }
                return ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Insert: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// AENDERUNGSDATUM (Anwenderbefund 22.09.2026): Eine geschriebene Anlagenzeile
        /// ändert die Eingangsgrößen der Simulation — das gespeicherte Ergebnis ist danach
        /// älter als das Projekt. Die Marke sitzt im SCHREIBCONTROLLER, nicht in der Kachel:
        /// Bis hierher setzten nur vier der zwölf Kacheln das Datum, und die Kachel
        /// „Wärmepumpe“ war keine davon.
        /// </summary>
        private void ProjektGeaendert()
        {
            if (ID_Projekt <= 0) return;
            MerkmalUebernahmeCtrl.MarkiereProjektGeaendert(ID_Projekt);
        }

        /// <summary>
        /// ET‑2 (Anwenderbefund 08.09.2026): Nach dem Schreiben einer Anlage, die Strom
        /// verwendet, bekommt das Projekt seinen Stromträger — bis hierher tat das nur der
        /// Assistent. Idempotent; ein Fehlschlag bricht das Speichern der Anlage nicht ab.
        ///
        /// <para><b>Keine eigene Bedingung</b> (Anwenderentscheide 22.09.2026): Ob das
        /// Projekt einen Stromträger braucht, beantwortet allein
        /// <see cref="ProjektEnergietraegerCtrl.BrauchtStromTraeger"/> — sie wird in
        /// <see cref="ProjektEnergietraegerCtrl.StromTraegerSicherstellen"/> gestellt und
        /// kennt auch Elektrokessel, BHKW und Hilfsenergie, die diese Anlagenzeile allein
        /// nicht verrät.</para>
        /// </summary>
        private void StromTraegerNachziehen()
        {
            if (ID_Projekt <= 0) return;
            try { ProjektEnergietraegerCtrl.StromTraegerSicherstellen(ID_Projekt); }
            catch { }
        }

        /// <summary>
        /// SENKEN BEIM ANLEGEN (Anwenderentscheid 23.09.2026): Eine eben eingefügte
        /// Wärmeerzeugeranlage bekommt ihre Senkenzeilen aus den Bedarfskanälen des
        /// Projekts (<see cref="Senkenvorbelegung"/>) — dieselbe Regel wie im Speicherweg
        /// <c>WizardCtrl.Add_WP_Waermeerzeuger</c>. Die neue Id ist der größte AutoWert des
        /// Projekts und Typs. Best effort: Ohne Zeile gilt die Vorbelegung.
        /// </summary>
        private void SenkenAnlegen()
        {
            if (ID_Projekt <= 0 || !Senkenvorbelegung.IstWaermeerzeuger(ID_Type)) return;
            try
            {
                object neu = DataRepository.ExecuteScalar(
                    "SELECT MAX(ID) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                    new DbParam("@p", ID_Projekt), new DbParam("@t", ID_Type));
                if (neu == null || neu == DBNull.Value) return;
                Senkenvorbelegung.Anlegen(ID_Projekt, new[] { Convert.ToInt32(neu) }, false);
            }
            catch { }
        }

        public void ReadAllFilter(string filter = "")
        {
            string sql;
            if (string.IsNullOrEmpty(filter))
            {
                sql = "SELECT * FROM Tab_Energieanlagen ORDER BY Bezeichner";
            }
            else
            {
                sql = "SELECT * FROM Tab_Energieanlagen WHERE " + filter;
            }

            DataTable dt = DataRepository.GetDataTable(sql, null);
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                WErzeugerModel item = new WErzeugerModel();
                AusZeile(dt, row, item);
                _internalList.Add(item);
            }
        }

        public void ReadSingle(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql, null);

            // "rows" Variable spiegelt im Single-Modus die Existenz wider (0 oder 1)
            if (dt != null && dt.Rows.Count > 0)
            {
                AusZeile(dt, dt.Rows[0], this);
            }
        }

        /// <summary>
        /// Zieht die ANLAGENZEILE eines Erzeugermodells aus der Datenbank nach
        /// (iU9-W7.0e) — woertlich aus <c>Wizard_WPItem.AnlagenzeileNachziehen</c>
        /// (Z. 466-510, Aenderung Ä25 vom 27.08.2026).
        ///
        /// <para><b>Wozu.</b> Die Kostenzeile der Waermepumpenmaske haengt an
        /// <c>item.ID</c> (Anlagenzeile) und <c>item.ID_Projekt</c>. Beide traegt das
        /// Listenobjekt nicht in jedem Einstieg: Ein Eintrag aus „Neu…" startet mit 0/0,
        /// und der Del+Add-Speicherweg schreibt seit Ä24 zwar die frische Anlagen-Id
        /// zurueck, <c>ID_Projekt</c> aber NICHT. Ohne dieses Nachziehen blieb „Kosten
        /// bearbeiten…" auch bei laengst gespeicherter Anlage gesperrt.</para>
        ///
        /// <para><b>Zwei Wege, in dieser Reihenfolge.</b> (1) Ueber eine gueltige
        /// <c>ID</c> am Modell: Die Zeile selbst nennt ihr Projekt — das heilt den
        /// Ä24-Fall. (2) Sonst ueber den GERAETEANKER: Projekt-Geraetekopie
        /// <c>Tab_WP.ID</c> → <c>Tab_Energieanlagen.ID_WP</c> desselben Projekts. Der
        /// Verbund mit <c>Tab_WP</c> haelt Stammkatalog-Ids heraus (eine Katalogzeile
        /// traegt kein <c>ID_Projekt</c> dieses Projekts — dieselbe Pruefung wie
        /// <c>WizardCtrl.PufferGehoertZuProjekt</c>). Findet auch das nichts, ist die
        /// Anlage wirklich noch nicht gespeichert.</para>
        ///
        /// <para><b>Die Ids stehen als LITERALE im Verbund.</b> ACE bindet positionale
        /// Parameter dort nicht verlaesslich (Ä21-Befund); der Wortlaut bleibt deshalb
        /// unveraendert. Es sind ausschliesslich <c>int</c>-Werte aus der Datenbank.</para>
        /// </summary>
        /// <param name="modell">Die Anlagenzeile; <c>ID</c> und <c>ID_Projekt</c> werden
        /// bei Erfolg an Ort und Stelle gesetzt.</param>
        /// <param name="projektRueckfall">
        /// Das GEOEFFNETE Projekt. Der Vorlaeufer holte es sich aus
        /// <c>Program.startfrm.m_ID_Projekt</c> — im Kern ist <c>Program.*</c> verboten,
        /// deshalb reicht die Huelle es herein.
        /// </param>
        /// <returns><c>true</c>, wenn <c>ID</c> und <c>ID_Projekt</c> danach eine
        /// GESPEICHERTE Anlagenzeile bezeichnen.</returns>
        public static bool AnlagenzeileNachziehen(WErzeugerModel modell, int projektRueckfall)
        {
            if (modell == null) return false;
            try
            {
                // (1) Gueltige Anlagen-Id am Listenobjekt.
                if (modell.ID > 0)
                {
                    object p = DataRepository.ExecuteScalar(
                        "SELECT ID_Projekt FROM Tab_Energieanlagen WHERE ID = ?",
                        new DbParam("@id", modell.ID));
                    if (p != null && p != DBNull.Value && Convert.ToInt32(p) > 0)
                    {
                        modell.ID_Projekt = Convert.ToInt32(p);
                        return true;
                    }
                }

                // (2) Sonst ueber den Geraeteanker.
                int projekt = modell.ID_Projekt;
                if (projekt <= 0) projekt = projektRueckfall;
                if (projekt <= 0 || modell.ID_WP <= 0) return false;

                object a = DataRepository.ExecuteScalar(
                    "SELECT MIN(a.ID) FROM Tab_Energieanlagen AS a " +
                    "INNER JOIN Tab_WP AS g ON a.ID_WP = g.ID " +
                    "WHERE a.ID_Projekt = " + projekt +
                    " AND g.ID_Projekt = " + projekt +
                    " AND a.ID_WP = " + modell.ID_WP);
                if (a == null || a == DBNull.Value) return false;

                modell.ID = Convert.ToInt32(a);
                modell.ID_Projekt = projekt;
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Überträgt eine Zeile aus <c>Tab_Energieanlagen</c> in ein Modell - die EINE
        /// Leseabbildung für <see cref="ReadAllFilter"/> und <see cref="ReadSingle"/>.
        ///
        /// <para>
        /// Beide Methoden hatten dieselbe Spaltenliste zweimal ausgeschrieben und waren
        /// bereits auseinandergelaufen (in <c>ReadAllFilter</c> stand bei <c>Azimut</c>
        /// ein nicht kurzschließendes <c>&amp;</c>, das bei fehlender Spalte geworfen
        /// hätte). Eine Abbildung bedeutet: Was gelesen wird, wird auch geschrieben -
        /// die Symmetrie zu <c>WizardCtrl.SQL_ANLAGE_INSERT</c> ist an einer
        /// einzigen Stelle prüfbar.
        /// </para>
        ///
        /// <para>
        /// ZWEI ZUWEISUNGSMUSTER, mit Absicht. Die 30 Bestandsspalten behalten das
        /// bisherige Muster „vorhanden und nicht NULL ⇒ übernehmen", sonst bleibt die
        /// Vorbelegung stehen. Die 27 Spalten der Quellen-/Senken-Konfiguration werden
        /// dagegen AUSDRÜCKLICH zugewiesen, auch mit <c>null</c>: Ihre Vorbelegung ist
        /// bei den Ladeprioritäten die 0, und ein NULL aus der Datenbank würde sonst
        /// beim Zurückschreiben zur 0 - der Roundtrip muss beide Zustände
        /// unterscheiden (Konzept 3.4).
        /// </para>
        /// </summary>
        private static void AusZeile(DataTable dt, DataRow row, WErzeugerModel item)
        {
            // --- Bestandsspalten ------------------------------------------------------
            if (Belegt(dt, row, "ID")) item.ID = Convert.ToInt32(row["ID"]);
            if (Belegt(dt, row, "ID_Projekt")) item.ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);
            if (Belegt(dt, row, "Bezeichner")) item.Bezeichner = row["Bezeichner"].ToString();
            if (Belegt(dt, row, "ID_Type")) item.ID_Type = Convert.ToInt32(row["ID_Type"]);
            if (Belegt(dt, row, "ID_WP")) item.ID_WP = Convert.ToInt32(row["ID_WP"]);
            if (Belegt(dt, row, "Betriebsart")) item.Betriebsart = row["Betriebsart"].ToString();
            if (Belegt(dt, row, "Sperrung")) item.Sperrung = Convert.ToBoolean(row["Sperrung"]);
            if (Belegt(dt, row, "Sperrzeit_von")) item.Sperrzeit_von = Convert.ToInt32(row["Sperrzeit_von"]);
            if (Belegt(dt, row, "Sperrzeit_bis")) item.Sperrzeit_bis = Convert.ToInt32(row["Sperrzeit_bis"]);
            if (Belegt(dt, row, "Vorlauf")) item.Vorlauf = Convert.ToInt32(row["Vorlauf"]);
            if (Belegt(dt, row, "Rücklauf")) item.Ruecklauf = Convert.ToInt32(row["Rücklauf"]);
            if (Belegt(dt, row, "Bivalenter_Betrieb")) item.Bivalenter_Betrieb = Convert.ToBoolean(row["Bivalenter_Betrieb"]);
            if (Belegt(dt, row, "Abschaltpunkt")) item.Abschaltpunkt = Convert.ToDouble(row["Abschaltpunkt"]);
            if (Belegt(dt, row, "Nutzungszeit")) item.Nutzungszeit = Convert.ToInt32(row["Nutzungszeit"]);
            if (Belegt(dt, row, "ID_SP")) item.ID_SP = Convert.ToInt32(row["ID_SP"]);
            if (Belegt(dt, row, "ID_PV")) item.ID_PV = Convert.ToInt32(row["ID_PV"]);
            if (Belegt(dt, row, "ID_Solar")) item.ID_Solar = Convert.ToInt32(row["ID_Solar"]);
            if (Belegt(dt, row, "Heizstab")) item.Heizstab = Convert.ToBoolean(row["Heizstab"]);
            if (Belegt(dt, row, "Volumen")) item.Volumen = Convert.ToDouble(row["Volumen"]);
            if (Belegt(dt, row, "rendeMix")) item.rendeMix = Convert.ToBoolean(row["rendeMix"]);
            if (Belegt(dt, row, "Solaranteil")) item.Solaranteil = Convert.ToInt32(row["Solaranteil"]);
            if (Belegt(dt, row, "ID_Kessel")) item.ID_Kessel = Convert.ToInt32(row["ID_Kessel"]);
            if (Belegt(dt, row, "ID_BHKW")) item.ID_BHKW = Convert.ToInt32(row["ID_BHKW"]);
            if (Belegt(dt, row, "Grenzleistung")) item.Grenzleistung = Convert.ToDouble(row["Grenzleistung"]);
            if (Belegt(dt, row, "Kollektormodulanzahl")) item.Kollektormodulanzahl = Convert.ToInt32(row["Kollektormodulanzahl"]);
            if (Belegt(dt, row, "PV_Leistung")) item.PV_Leistung = Convert.ToDouble(row["PV_Leistung"]);
            if (Belegt(dt, row, "Neigung")) item.m_Neigung = Convert.ToInt32(row["Neigung"]);
            if (Belegt(dt, row, "Azimut")) item.m_Azimut = Convert.ToInt32(row["Azimut"]);
            if (Belegt(dt, row, "ID_PUFFER")) item.ID_PUFFER = Convert.ToInt32(row["ID_PUFFER"]);
            // Ohne diese Zeile verliert der Bearbeiten-Zweig des Wizards den Energietraeger:
            // WizardParent.LoadWEFromDB uebernimmt ID_Carrier aus diesen Modellen, und
            // Del_Projekt_Waermeerzeuger + Add_WP_Waermeerzeuger schrieben die Anlagen
            // anschliessend mit ID_Carrier = 0 zurueck.
            // Über den ROHWERT, damit NULL nicht zur 0 wird (siehe WErzeugerModel).
            item.ID_CarrierRoh = Zahl(dt, row, "ID_Carrier");
            // KU-S3 (Schemaschritt 114; K9, E33): der Stromtraeger der Kuehlung, NULL = wie
            // Heizbetrieb - ausdruecklich auch mit null; eine fehlende Spalte gilt wie NULL.
            item.Kuehl_ID_Carrier = Zahl(dt, row, KuehlungSchema.SPALTE_KUEHL_ID_CARRIER);
            // Schemaschritt 115 (E34): die Abrechnungsart des Kaeltestroms, NULL = anteilig am
            // Netzbezug (Vorgabe) - NULL bleibt null, 1 wird true, 0 false; eine fehlende Spalte
            // gilt wie NULL.
            int? zaehler = Zahl(dt, row, KuehlungSchema.SPALTE_KUEHL_EIGENER_ZAEHLER);
            item.Kuehl_EigenerZaehler = zaehler.HasValue ? (bool?)(zaehler.Value != 0) : null;

            // --- Quellen-/Senken-Konfiguration (ausdrücklich, auch mit null) -----------
            item.Prioritaet = Zahl(dt, row, "Prioritaet");
            item.BM_Typ = Text(dt, row, "BM_Typ");

            item.WQ_Typ = Text(dt, row, "WQ_Typ");
            item.WQ_Temp = Kommazahl(dt, row, "WQ_Temp");
            item.WQ_Monatswerte = Text(dt, row, "WQ_Monatswerte");
            item.WQ_Wochenwerte = Text(dt, row, "WQ_Wochenwerte");
            item.WQ_CSV = Text(dt, row, "WQ_CSV");
            item.WQ_Puffer = Text(dt, row, "WQ_Puffer");
            item.WQ_ID_Puffer = Zahl(dt, row, "WQ_ID_Puffer");
            item.WQ_Spreizung = Kommazahl(dt, row, "WQ_Spreizung");
            item.WQ_Regeneration = Kommazahl(dt, row, "WQ_Regeneration");
            item.WQ_Unbegrenzt = JaNein(dt, row, "WQ_Unbegrenzt");
            item.WQ_Tiefe = Kommazahl(dt, row, "WQ_Tiefe");
            item.WQ_Flaeche = Kommazahl(dt, row, "WQ_Flaeche");
            item.WQ_Anzahl = Zahl(dt, row, "WQ_Anzahl");
            item.WQ_Bodentyp = Text(dt, row, "WQ_Bodentyp");
            item.WQ_Quellsystem = Text(dt, row, "WQ_Quellsystem");

            item.WS_Typ = Text(dt, row, "WS_Typ");
            item.WS_Ziel = Text(dt, row, "WS_Ziel");
            item.WS_ID_Puffer = Zahl(dt, row, "WS_ID_Puffer");
            item.WS_Ladeprio = Zahl(dt, row, "WS_Ladeprio");
            item.WS_Ladegrenze = Kommazahl(dt, row, "WS_Ladegrenze");
            item.WS_Ladeprio_PV = Zahl(dt, row, "WS_Ladeprio_PV");
            item.WS_Ziel2 = Text(dt, row, "WS_Ziel2");
            item.WS_ID_Puffer2 = Zahl(dt, row, "WS_ID_Puffer2");
            item.WS_Ladeprio2 = Zahl(dt, row, "WS_Ladeprio2");
            item.WS_Ladegrenze2 = Kommazahl(dt, row, "WS_Ladegrenze2");

            // --- PV-Anlagenparameter (Paket A, Stufe E1.3) ----------------------------
            // Ausdruecklich auch mit null - NULL heisst hier "nie gepflegt, es gilt der
            // Vorgabewert" (0,95 bzw. 0 %) und darf beim Zurueckschreiben nicht zur 0
            // werden. Belegt() behandelt eine FEHLENDE Spalte wie NULL; eine Datenbank
            // vor Migrationsschritt 62 laeuft damit unveraendert weiter.
            item.PV_WrWirkungsgrad = Kommazahl(dt, row, "PV_WrWirkungsgrad");
            item.PV_Systemverluste = Kommazahl(dt, row, "PV_Systemverluste");

            // --- PV-Modellwahl und Wechselrichter (Paket B, Stufe E2) ----------------
            // Ebenfalls ausdruecklich mit null. Bei PV_Modell traegt NULL die Aussage
            // "vereinfachtes Modell" - eine Datenbank vor Migrationsschritt 63 laeuft
            // damit unveraendert weiter und rechnet den Paket-A-Weg.
            item.PV_Modell = Text(dt, row, "PV_Modell");
            item.PV_WrNennleistungKw = Kommazahl(dt, row, "PV_WrNennleistungKw");
            item.PV_WrEta10 = Kommazahl(dt, row, "PV_WrEta10");
            item.PV_WrEta50 = Kommazahl(dt, row, "PV_WrEta50");
            item.PV_WrEta100 = Kommazahl(dt, row, "PV_WrEta100");

            // --- Der sichtbare Wechselrichterweg (W6-E-3, Stufe S2) -------------------
            // Ebenfalls ausdruecklich mit null. NULL traegt hier die Aussage
            // "vereinfacht" und ist zugleich "nie gewaehlt" - eine Datenbank vor
            // Migrationsschritt 66 laeuft damit unveraendert weiter.
            item.PV_Wechselrichterweg = Text(dt, row, SchemaKatalog.SPALTE_EA_PV_WECHSELRICHTERWEG);
        }

        /// <summary>Spalte vorhanden UND nicht NULL - eine fehlende Spalte gilt wie NULL.</summary>
        private static bool Belegt(DataTable dt, DataRow row, string spalte)
        {
            return dt.Columns.Contains(spalte) && row[spalte] != DBNull.Value;
        }

        /// <summary>Text oder <c>null</c>; der Unterschied NULL/Leerstring bleibt erhalten.</summary>
        private static string Text(DataTable dt, DataRow row, string spalte)
        {
            return Belegt(dt, row, spalte) ? row[spalte].ToString() : null;
        }

        private static int? Zahl(DataTable dt, DataRow row, string spalte)
        {
            return Belegt(dt, row, spalte) ? (int?)Convert.ToInt32(row[spalte]) : null;
        }

        private static double? Kommazahl(DataTable dt, DataRow row, string spalte)
        {
            return Belegt(dt, row, spalte) ? (double?)Convert.ToDouble(row[spalte]) : null;
        }

        /// <summary>Ja/Nein-Feld; Access kennt dort kein NULL, fehlende Spalte = false.</summary>
        private static bool JaNein(DataTable dt, DataRow row, string spalte)
        {
            return Belegt(dt, row, spalte) && Convert.ToBoolean(row[spalte]);
        }

        /// <summary>Eine Anlagenzeile, so weit eine Liste sie braucht: Id und Name.</summary>
        public sealed record AnlagenZeile(int Id, string Bezeichner);

        /// <summary>
        /// Alle Anlagenzeilen EINES Projekts von EINEM Typ, in Anlagenreihenfolge
        /// (iU9-W11a.2).
        ///
        /// <para><b>Drei Aufrufer, eine Abfrage.</b> Bis hierher stand sie zweimal im
        /// Bestand: als <c>SELECT ID, Bezeichner … ORDER BY ID</c> im
        /// Variantenvergleich (Z. 363-378) und als <c>SELECT COUNT(*)</c> in
        /// <c>Form_Simulation_Detail.SpVariantenzahl</c> (Z. 7217-7236). Die Zaehlung ist
        /// die <c>Count</c>-Eigenschaft dieser Liste — eine zweite Abfrage braucht es
        /// dafuer nicht.</para>
        ///
        /// <para><c>ORDER BY ID</c> ist Fachkonzept 7.3: Dieselbe Reihenfolge fuehren die
        /// Uebersicht des Hauptformulars und <c>ReadAllByProjekt</c>, damit eine Variante
        /// in allen drei Ansichten an derselben Stelle steht.</para>
        ///
        /// <para>Wirft nicht — bei einem Fehler kommt eine leere Liste zurueck.</para>
        /// </summary>
        public static List<AnlagenZeile> AnlagenJeTyp(int idProjekt, int idType)
        {
            List<AnlagenZeile> zeilen = new List<AnlagenZeile>();
            if (idProjekt <= 0) return zeilen;

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner FROM Tab_Energieanlagen " +
                    "WHERE ID_Projekt = ? AND ID_Type = ? ORDER BY ID",
                    new DbParam("@proj", idProjekt),
                    new DbParam("@typ", idType));

                if (dt == null) return zeilen;

                foreach (DataRow r in dt.Rows)
                    zeilen.Add(new AnlagenZeile(
                        Convert.ToInt32(r["ID"]),
                        r["Bezeichner"] != DBNull.Value ? r["Bezeichner"].ToString() : ""));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Anlagen des Projekts konnten nicht gelesen werden: " + ex.Message);
            }

            return zeilen;
        }

        /// <summary>
        /// Alle Anlagen EINES Projekts von EINEM Typ als VOLLE Modelle (iU9-W11a.2) —
        /// der parametrisierte Ersatz fuer
        /// <c>ReadAllFilter("ID_Projekt=" + p + " and ID_Type=" + t)</c>.
        ///
        /// <para>Zwei Aufrufer im Bestand: der Doppelklick auf die Modulliste der
        /// Waermepumpenseite (<c>Form_Simulation_Detail</c> Z. 5053) und die
        /// Speicherkapazitaet des Dashboards (<c>TabNavigationManager</c> Z. 142). Beide
        /// bauten den WHERE-Zweig als Zeichenkette zusammen.</para>
        ///
        /// <para><b>Ohne <c>ORDER BY</c></b> — wie der Vorlaeufer: <c>ReadAllFilter</c>
        /// sortiert nur den filterlosen Fall.</para>
        /// </summary>
        public static List<WErzeugerModel> ModelleJeTyp(int idProjekt, int idType)
        {
            WErzeugerCtrl ctrl = new WErzeugerCtrl();
            ctrl.LesenJeTyp(idProjekt, idType);
            return new List<WErzeugerModel>(ctrl.items);
        }

        /// <summary>
        /// Fuellt DIESES Steuerobjekt mit den Anlagen eines Projekts und Typs —
        /// parametrisiert. Siehe <see cref="ModelleJeTyp"/>.
        /// </summary>
        public void LesenJeTyp(int idProjekt, int idType)
        {
            _internalList.Clear();

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                new DbParam("@proj", idProjekt),
                new DbParam("@typ", idType));

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                WErzeugerModel item = new WErzeugerModel();
                AusZeile(dt, row, item);
                _internalList.Add(item);
            }
        }

        /// <summary>
        /// Der ANZEIGENAME einer Anlagenzeile (iU9-W11a.2; woertlich aus
        /// <c>Form_Simulation_Detail.SpVariantenname</c>, Z. 6410-6423).
        ///
        /// <para>Die Variantentabelle selbst fuehrt keinen Namen — der Name gehoert zur
        /// Anlage (Fachkonzept 7.3). Rueckgabe <c>null</c>, wenn die Zeile keinen
        /// Bezeichner fuehrt oder nicht gelesen werden kann; der Aufrufer nimmt dann die
        /// Id. Der Anzeigename ist Beiwerk — der Status erscheint auch ohne ihn.</para>
        ///
        /// <para><b>Warum nicht schlicht <c>Bezeichner</c>.</b> Die Arbeitsanweisung
        /// nennt den Namen so; er ist hier aber vergeben: <c>WErzeugerCtrl</c> erbt von
        /// <c>WErzeugerModel</c>, und dort ist <c>Bezeichner</c> ein FELD. Eine
        /// gleichnamige statische Methode bricht den Bau (CS0019 an jeder Lesestelle des
        /// Feldes).</para>
        /// </summary>
        public static string AnlagenBezeichner(int idAnlage)
        {
            if (idAnlage <= 0) return null;

            try
            {
                object wert = DataRepository.ExecuteScalar(
                    "SELECT Bezeichner FROM Tab_Energieanlagen WHERE ID = ?",
                    new DbParam("@id", idAnlage));

                if (wert != null && wert != DBNull.Value && wert.ToString().Length > 0)
                    return wert.ToString();
            }
            catch { /* Anzeigename ist Beiwerk */ }

            return null;
        }
    }
}
