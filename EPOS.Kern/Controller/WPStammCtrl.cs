using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // Controller fuer die Waermepumpen-STAMMDATEN (Tab_WP_STAMM) samt Kennlinien-Stammtabellen
    // (Tab_Kenndaten_STAMM / Tab_Kenndaten_Kuehlung_STAMM).
    // Schluessel = ID, Namensfeld = Bezeichner (im WPModel weiterhin als WPName gefuehrt).
    // Neues Feld ReadOnly: schreibgeschuetzte Stammdatensaetze koennen nicht ueberschrieben/geloescht werden.
    // Wird von den Waermepumpen-Dialogen verwendet: seit iU9-W7 von den Huellen
    // WaermepumpeStammHuelle, WaermepumpeAnlageHuelle und WaermepumpenHuelle, dazu
    // seit iU9-W13 vom Katalogimport ueber WaermepumpeImportSatz. Alle DB-Zugriffe
    // laufen ueber DataRepository.
    class WPStammCtrl : WPModel
    {
        public const string TABLE     = "Tab_WP_STAMM";
        public const string CURVE     = "Tab_Kenndaten_STAMM";
        public const string CURVE_K   = "Tab_Kenndaten_Kuehlung_STAMM";

        private List<WPModel> _internalList = new List<WPModel>();
        public int rows => _internalList.Count;
        public new List<WPModel> items => _internalList;

        public WPStammCtrl()
        {
        }

        #region --- READ ---

        // filter z.B. "ID=5" oder "Bezeichner='...'"; leer = alle (nach Bezeichner sortiert).
        public void ReadAll(string filter = "")
        {
            string sql = string.IsNullOrEmpty(filter)
                ? "SELECT * FROM " + TABLE + " ORDER BY Bezeichner"
                : "SELECT * FROM " + TABLE + " WHERE " + filter;

            DataTable dt = DataRepository.GetDataTable(sql, null);
            MapDataTableToItems(dt);
        }

        public void ReadAll_MitMinMaxVorlauf(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql, null);
            MapDataTableToItems(dt);
        }

        // ReadSingle mit komplettem SQL (Aufrufer uebergeben "select * from Tab_WP_STAMM where Bezeichner='...'").
        public void ReadSingle(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql, null);
            _internalList.Clear();

            if (dt != null && dt.Rows.Count > 0)
            {
                MapRowToThis(dt.Rows[0]);
                _internalList.Add(this);
            }
        }

        public bool IsReadOnly(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM " + TABLE + " WHERE Bezeichner = ?",
                new DbParam("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        public bool Exists(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + TABLE + " WHERE Bezeichner = ?",
                new DbParam("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        /// <summary>
        /// Die Zeilen des WAERMEPUMPEN-KATALOGS (iU9-W7.0b) — der ganze Stammkatalog,
        /// angereichert um kleinsten und groessten Vorlauf je Geraet.
        ///
        /// <para><b>Herkunft.</b> Woertlich aus <c>WPDataCtrl.ReadAll</c>, das bis
        /// hierher AM ENDE der Formulardatei <c>Form_WPFilterAuswahl.cs</c> stand
        /// (Z. 281-323). Zwei Abfragen wie dort: der Katalog ueber
        /// <see cref="ReadAll()"/> (nach Bezeichner sortiert), dazu EINE Gruppenabfrage
        /// ueber alle Kennlinien. Je Geraet einzeln zu fragen waere bei einigen hundert
        /// Stammsaetzen genau das, was diese eine Abfrage vermeidet.</para>
        ///
        /// <para><b>Ohne Kennlinien bleibt es bei 0/0.</b> Ein Stammsatz ohne Zeile in
        /// <c>Tab_Kenndaten_STAMM</c> steht mit Vorlauf 0 im Katalog — er faellt damit
        /// aus jedem Bereichsfilter mit einer Untergrenze &gt; 0 heraus. Das ist das
        /// Verhalten des Vorlaeufers.</para>
        /// </summary>
        // ==================================================================
        // OHNE WIRT SEIT STUFE S2.2 (Anwenderentscheid W14a-E-10, Frage Q5)
        //
        // Diese Methode fuellte den WaermepumpenKatalogDialog mit den elf
        // Merkmalen seiner Filterleiste. Seit S2.2 holt er seine Zeilen ueber
        // Katalogfilterzeilen() - denselben Weg wie die Verwaltung, dieselben
        // neun Spalten. Gemessen am 07.09.2026 ruft nur noch
        // WaermepumpenKatalogTests hierher; sie bleibt als Gegenprobe stehen,
        // zusammen mit WaermepumpenKatalogFilter und WaermepumpenKatalogZeile.
        // ==================================================================
        public IReadOnlyList<WaermepumpenKatalogZeile> KatalogZeilen()
        {
            ReadAll();   // alle Stamm-WP (Tab_WP_STAMM), sortiert nach Bezeichner

            var kleinster = new Dictionary<int, int>();
            var groesster = new Dictionary<int, int>();

            DataTable dtv = DataRepository.GetDataTable(
                "SELECT ID_WP, Min(Vorlauf) AS MinV, Max(Vorlauf) AS MaxV FROM " + CURVE + " GROUP BY ID_WP");
            if (dtv != null)
            {
                foreach (DataRow r in dtv.Rows)
                {
                    if (r["ID_WP"] == DBNull.Value) continue;
                    int idWp = Convert.ToInt32(r["ID_WP"]);
                    kleinster[idWp] = r["MinV"] != DBNull.Value ? Convert.ToInt32(r["MinV"]) : 0;
                    groesster[idWp] = r["MaxV"] != DBNull.Value ? Convert.ToInt32(r["MaxV"]) : 0;
                }
            }

            var liste = new List<WaermepumpenKatalogZeile>();
            foreach (WPModel m in _internalList)
            {
                liste.Add(new WaermepumpenKatalogZeile(
                    Hersteller: m.Firma,
                    Bezeichnung: m.WPName,
                    Bauart: m.Bauart,
                    Aufstellung: m.Aufstellung,
                    MaxVorlauf: groesster.ContainsKey(m.ID) ? groesster[m.ID] : 0,
                    MinVorlauf: kleinster.ContainsKey(m.ID) ? kleinster[m.ID] : 0,
                    MaxLeistung: m.Nennleistung,
                    ElZuheizung: m.Heizung,
                    Funktionsprinzip: m.Typ,
                    Regelung: m.Regelung,
                    Auslegung: m.Kuehlleistung > 0
                        ? WaermepumpenKatalogZeile.AUSLEGUNG_HEIZEN_KUEHLEN
                        : WaermepumpenKatalogZeile.AUSLEGUNG_HEIZEN));
            }
            return liste;
        }

        // =================================================================================
        // W14a-E-10 / S1.5 - die Zeilen der KATALOGVERWALTUNG mit ihren Parameterspalten
        // =================================================================================

        // --- DUPLIZIEREN (Konzept Administrationsdialoge, Entscheid AD-Q11) ---

        /// <summary>
        /// <b>„Duplizieren…"</b>: kopiert den Wärmepumpensatz <paramref name="id"/> als EIGENEN
        /// Satz unter <paramref name="neuerName"/> — <c>ReadOnly = 0</c>, alle übrigen
        /// Spalten wie im Original (Entscheid <b>AD-Q11</b> vom 23.09.2026:
        /// Auslieferungssätze werden nie überschrieben; wer einen ändern will, dupliziert
        /// ihn).
        /// </summary>
        /// <remarks>
        /// Die Regel steht einmal in <see cref="Katalogkopie.Duplizieren"/>; hier steht nur,
        /// WELCHE Tabelle es ist — und dass die Kennlinien (<c>Tab_Kenndaten_STAMM</c>,
        /// <c>Tab_Kenndaten_Kuehlung_STAMM</c>) im selben Vorgang mitkommen.
        /// </remarks>
        public static Katalogkopie.Ergebnis Duplizieren(int id, string neuerName)
        {
            return Katalogkopie.Duplizieren(TABLE, id, neuerName,
                new Katalogkopie.Kindtabelle(CURVE, "ID_WP"),
                new Katalogkopie.Kindtabelle(CURVE_K, "ID_WP"));
        }

        /// <summary>
        /// <b>Die Zeilen der Stammverwaltung</b> (Anwenderentscheid W14a-E-10,
        /// Konzept_Katalogfilter 4.3 und S1.5) — NEUN Spalten: Hersteller, Modell,
        /// Quelle, Nennleistung, VL min, VL max, Zuheizung, Kuehlleistung und COP bei A2/W35.
        ///
        /// <para><b>Der Kern des Entscheids.</b> „Das Schema des Dialogs sollte immer
        /// gleich aussehen (Waermepumpe aehnlich wie PV-Module und Heizkessel)" — bis
        /// hierher zeigte die Stammliste EINE Spalte (den Bezeichner), waehrend der
        /// vollstaendige Filter des Hauses in einer UEBERLAGERUNG „Modul-Katalog…" sass,
        /// die der Anwender beim Oeffnen gar nicht sieht (Befund 1.2/4).</para>
        ///
        /// <para><b>Drei abgeleitete Groessen aus den Kennlinien</b>, alle aus EINER
        /// Gruppenabfrage bzw. einem Punktzugriff auf <see cref="CURVE"/> — je Geraet
        /// einzeln zu fragen waere bei einigen hundert Stammsaetzen genau das, was
        /// <see cref="KatalogZeilen"/> schon vermeidet:</para>
        /// <list type="bullet">
        ///   <item><b>VL min / VL max</b> = <c>Min/Max(Vorlauf)</c> je <c>ID_WP</c>. Ohne
        ///     Kennlinie bleibt es beim Halbgeviertstrich — der Vorlaeufer schrieb dort
        ///     0/0 und liess den Satz aus jedem Bereichsfilter mit Untergrenze &gt; 0
        ///     fallen; als LEERWERT ist derselbe Satz wenigstens sichtbar.</item>
        ///   <item><b>COP A2/W35</b> = <c>COP</c> bei <c>Vorlauf = 35</c> und
        ///     <c>Temperatur = 2</c> — die Guetezahl, nach der ein Planer waehlt. Sie
        ///     steht als Kennlinienpunkt da und nirgends als Spalte.</item>
        ///   <item><b>Kuehlleistung</b> = <c>Tab_WP_STAMM.Kuehlleistung</c> [kW]. Bis zum
        ///     16.09.2026 stand hier das Kennzeichen „Kuehlen" (<c>Kuehlleistung &gt; 0</c>,
        ///     „Ja"/„Nein"); die Zahl sagt dasselbe und nennt die Leistung, und der
        ///     Schalter „nur mit Kuehlfunktion" setzt sie auf <c>&gt;0</c>.</item>
        /// </list>
        ///
        /// <para><b>Die Bauart fehlt bewusst</b>: 45 von 51 Saetzen fuehren sie leer
        /// (Anhang A des Konzepts). Eine Spalte, die fast immer leer ist, kostet Breite
        /// und traegt nichts.</para>
        /// </summary>
        public IReadOnlyList<Katalogfilterzeile> Katalogfilterzeilen()
        {
            var liste = new List<Katalogfilterzeile>();

            var kleinster = new Dictionary<int, double?>();
            var groesster = new Dictionary<int, double?>();
            DataTable dtv = StilleDb.Tabelle(
                "SELECT ID_WP, Min(Vorlauf) AS MinV, Max(Vorlauf) AS MaxV FROM " + CURVE +
                " GROUP BY ID_WP");
            if (dtv != null)
            {
                foreach (DataRow r in dtv.Rows)
                {
                    int idWp = Katalogfeld.Ganzzahl(r, "ID_WP");
                    if (idWp <= 0) continue;
                    kleinster[idWp] = Katalogfeld.Zahl(r, "MinV");
                    groesster[idWp] = Katalogfeld.Zahl(r, "MaxV");
                }
            }

            var cop = new Dictionary<int, double?>();
            DataTable dtc = StilleDb.Tabelle(
                "SELECT ID_WP, COP FROM " + CURVE + " WHERE Vorlauf = 35 AND Temperatur = 2");
            if (dtc != null)
            {
                foreach (DataRow r in dtc.Rows)
                {
                    int idWp = Katalogfeld.Ganzzahl(r, "ID_WP");
                    if (idWp <= 0 || cop.ContainsKey(idWp)) continue;
                    cop[idWp] = Katalogfeld.Zahl(r, "COP");
                }
            }

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, Bezeichner, Firma, Typ, Nennleistung, Heizung, Kuehlleistung, " +
                "ReadOnly FROM " + TABLE + " ORDER BY Bezeichner");
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                int id = Katalogfeld.Ganzzahl(r, "ID");
                string bezeichner = Katalogfeld.Text(r, "Bezeichner");
                double? kuehl = Katalogfeld.Zahl(r, "Kuehlleistung");

                var zeile = new Katalogfilterzeile(id, bezeichner)
                {
                    Geschuetzt = Katalogfeld.Kennzeichen(r, "ReadOnly")
                };

                liste.Add(zeile
                    .MitText(Katalogfilterprofil.SpHersteller, Katalogfeld.Text(r, "Firma"))
                    .MitText(Katalogfilterprofil.SpBezeichner, bezeichner)
                    .MitText(Katalogfilterprofil.SpQuelle, Katalogfeld.Text(r, "Typ"))
                    .MitZahl(Katalogfilterprofil.SpNennleistung,
                             Katalogfeld.Zahl(r, "Nennleistung"), 1)
                    .MitZahl(Katalogfilterprofil.SpVlMin,
                             kleinster.ContainsKey(id) ? kleinster[id] : null, 0)
                    .MitZahl(Katalogfilterprofil.SpVlMax,
                             groesster.ContainsKey(id) ? groesster[id] : null, 0)
                    .MitZahl(Katalogfilterprofil.SpZuheizung, Katalogfeld.Zahl(r, "Heizung"), 1)
                    // 16.09.2026: die KUEHLLEISTUNG als Zahl. Das Kennzeichen „Kuehlen"
                    // (Kuehlleistung > 0) sagte dasselbe mit weniger Auskunft; ein NICHT
                    // gepflegter Wert bleibt der Halbgeviertstrich und faellt damit aus
                    // jedem Bereichsfilter - genau wie bei VL min/max.
                    .MitZahl(Katalogfilterprofil.SpKuehlleistung, kuehl, 1)
                    .MitZahl(Katalogfilterprofil.SpCop, cop.ContainsKey(id) ? cop[id] : null, 2));
            }
            return liste;
        }

        /// <summary>
        /// Der Name des Projekts, das diese Waermepumpe VERWENDET — oder <c>null</c>,
        /// wenn keines sie verwendet (iU9-W7.0e).
        ///
        /// <para>Woertlich aus <c>Form_WP.btn_Loeschen_Click</c> (Z. 442-449): Der
        /// Verbund <c>Tab_Projekt</c> × <c>Tab_Energieanlagen</c> ueber den BEZEICHNER
        /// der Anlage. Ist er belegt, lehnt der Dialog das Loeschen ab und nennt das
        /// Projekt.</para>
        ///
        /// <para><b>Der Bezeichner ist nicht eindeutig</b> — der Vorlaeufer nahm die
        /// ERSTE Zeile, die der Lesezeiger lieferte, und das bleibt so (Regel F3). Der
        /// Name dient der Meldung, nicht der Zuordnung; fuer die Sperre selbst genuegt,
        /// DASS es ein Projekt gibt.</para>
        /// </summary>
        public string GesperrtDurchProjekt(string wpName)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Tab_Projekt.ID, Tab_Projekt.Projektname FROM Tab_Projekt " +
                "INNER JOIN Tab_Energieanlagen ON Tab_Projekt.ID = Tab_Energieanlagen.ID_Projekt " +
                "WHERE Tab_Energieanlagen.Bezeichner = ?",
                new DbParam("@bez", wpName ?? ""));

            if (dt == null || dt.Rows.Count == 0) return null;
            object name = dt.Rows[0]["Projektname"];
            return name == DBNull.Value ? "" : name.ToString();
        }

        #endregion

        #region --- ADMIN WRITE (Tab_WP_STAMM) ---

        // Aktualisiert einen Stammdatensatz (per Bezeichner). Schreibgeschuetzte Saetze werden abgelehnt.
        public bool Update()
        {
            if (IsReadOnly(WPName))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt");
                return false;
            }
            try
            {
                string sql = @"UPDATE " + TABLE + @"
                               SET Firma = ?, Beschreibung = ?, Typ = ?, Baujahr = ?, Aufstellung = ?,
                                   Nennleistung = ?, maxPtherm = ?, Heizung = ?, Regelung = ?, Modulkosten = ?
                               WHERE Bezeichner = ?";
                DbParam[] ps = {
                    new DbParam("@fir", Firma ?? (object)DBNull.Value),
                    new DbParam("@bes", Beschreibung ?? (object)DBNull.Value),
                    new DbParam("@typ", Typ ?? (object)DBNull.Value),
                    new DbParam("@bau", Baujahr),
                    new DbParam("@auf", Aufstellung ?? (object)DBNull.Value),
                    new DbParam("@nen", Nennleistung),
                    new DbParam("@max", maxPTherm),
                    new DbParam("@hei", Heizung),
                    new DbParam("@reg", Regelung ?? (object)DBNull.Value),
                    new DbParam("@mod", Modulkosten),
                    new DbParam("@nam", WPName ?? (object)DBNull.Value)
                };
                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex) { Console.WriteLine("Fehler bei Update (STAMM): " + ex.Message); return false; }
        }

        /// <summary>
        /// Was ein Speicherversuch des Waermepumpen-Stammdialogs ergeben hat
        /// (iU9-W7.0e; derselbe Zuschnitt wie
        /// <see cref="HeizkesselStammCtrl.SpeicherErgebnis"/>).
        /// </summary>
        /// <param name="Ok">Wurde geschrieben?</param>
        /// <param name="Meldung">Der Grund im Klartext, bereits lokalisiert.</param>
        /// <param name="Name">Der Bezeichner, unter dem der Satz jetzt steht — damit
        /// waehlt der Dialog die Zeile in seiner neu geladenen Liste wieder aus.</param>
        public sealed record SpeicherErgebnis(bool Ok, string Meldung, string Name);

        /// <summary>
        /// Schreibt einen Stammsatz — der Weg von <c>Form_WP.btn_Speichern_Click</c>
        /// (Z. 372-428), ohne <c>MessageBox</c>.
        ///
        /// <para><b>Drei Ausgaenge wie bisher.</b> Schreibgeschuetzt (nur beim Aendern),
        /// gespeichert, oder Fehler. Der Vorlaeufer zeigte alle drei als schlichte
        /// Meldung; hier stehen sie im Ergebnis und werden zum Banner.</para>
        ///
        /// <para><b>Der Datensatz kommt VOLLSTAENDIG herein.</b> Der Vorlaeufer las vor
        /// dem Schreiben mit <c>ReadSingle</c> den Satz nach, weil das Formular
        /// <c>maxPtherm</c>, <c>Bauart</c> und <c>Kuehlleistung</c> nicht bearbeitet —
        /// ohne das haette <c>Update</c> sie genullt. Diese Felder traegt jetzt der
        /// uebergebene <paramref name="daten"/>-Satz, den die Huelle aus der gewaehlten
        /// Zeile aufbaut. Beim ANLEGEN ist er leer statt aus dem zuvor markierten Satz
        /// gefuellt — siehe Abweichung A-6 des Protokolls W7.</para>
        /// </summary>
        /// <param name="daten">Der zu schreibende Satz.</param>
        /// <param name="neu"><c>true</c> = anlegen (<c>Insert</c>), sonst aendern (<c>Update</c>).</param>
        public SpeicherErgebnis Speichern(WPModel daten, bool neu)
        {
            if (daten == null)
                return new SpeicherErgebnis(false, Text("WPS_MSG_FEHLER",
                    "Speicherung nicht möglich, Fehler aufgetreten!"), "");

            string name = (daten.WPName ?? "").Trim();
            if (name.Length == 0)
                return new SpeicherErgebnis(false, Text("WPS_MSG_NAME_FEHLT",
                    "Bitte einen Namen für die Wärmepumpe eingeben!"), "");

            if (!neu && IsReadOnly(name))
                return new SpeicherErgebnis(false, Text("WPS_MSG_READONLY_SPEICHERN",
                    "Diese Wärmepumpe ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden."), "");

            if (neu && Exists(name))
                return new SpeicherErgebnis(false, Text("WPS_MSG_NAME_BELEGT",
                    "Name existiert bereits!"), "");

            // Der Controller IST das Modell (er erbt WPModel) - Update und Insert lesen
            // ihre Werte von sich selbst.
            ID = daten.ID;
            WPName = name;
            Firma = daten.Firma;
            Beschreibung = daten.Beschreibung;
            Typ = daten.Typ;
            Baujahr = daten.Baujahr;
            Aufstellung = daten.Aufstellung;
            Nennleistung = daten.Nennleistung;
            maxPTherm = daten.maxPTherm;
            Heizung = daten.Heizung;
            Regelung = daten.Regelung;
            Modulkosten = daten.Modulkosten;
            Bauart = daten.Bauart;
            Kuehlleistung = daten.Kuehlleistung;

            bool ok;
            try { ok = neu ? Insert() : Update(); }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern der Wärmepumpe: " + ex.Message);
                ok = false;
            }

            return ok
                ? new SpeicherErgebnis(true, Text("WPS_MSG_GESPEICHERT", "Gespeichert"), name)
                : new SpeicherErgebnis(false, Text("WPS_MSG_FEHLER",
                    "Speicherung nicht möglich, Fehler aufgetreten!"), "");
        }

        /// <summary>Ressourcentext mit deutschem Rueckfall (Drei-Schichten-Regel).</summary>
        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        // Loescht einen Stammdatensatz (per Bezeichner) samt Kennlinien, sofern nicht schreibgeschuetzt.
        public bool Delete()
        {
            if (IsReadOnly(WPName))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt");
                return false;
            }
            try
            {
                int id = DataRepository.GetIdByName(TABLE, "Bezeichner", WPName);
                if (id > 0)
                {
                    DataRepository.ExecuteSQL("DELETE FROM " + CURVE   + " WHERE ID_WP = ?", new DbParam("@id", id));
                    DataRepository.ExecuteSQL("DELETE FROM " + CURVE_K + " WHERE ID_WP = ?", new DbParam("@id", id));
                }
                return DataRepository.ExecuteSQL("DELETE FROM " + TABLE + " WHERE Bezeichner = ?",
                    new DbParam("@nam", WPName ?? (object)DBNull.Value));
            }
            catch (Exception ex) { Console.WriteLine("Fehler bei Delete (STAMM): " + ex.Message); return false; }
        }

        /// <summary>
        /// <b>Der Schreibweg des Katalogimports</b> (iU9-W13.0e): Duplikatpruefung,
        /// Stammsatz UND beide Kennlinientabellen in EINER Transaktion.
        ///
        /// <para><b>Was sich gegenueber dem Bestand aendert.</b>
        /// <c>Form_WP_einlesen.UebernehmeEintrag</c> schrieb DREI Tabellen ohne
        /// Transaktion und kompensierte mit einer <c>finally</c>-Aufraeumklammer:
        /// Scheiterte ein Kennlinien-Insert, wurde der schon angelegte Stammsatz
        /// wieder geloescht (Befund W13-B33). Der Grund war, dass die Controller
        /// ueber getrennte Verbindungen schrieben — mit dieser Methode nicht mehr.
        /// Der Zwilling <see cref="UeberschreibeMitKennlinien"/> zeigte laengst,
        /// dass es geht; hier steht dieselbe Klammer fuer die Neuanlage.</para>
        ///
        /// <para>Die ID des Stammsatzes ist ein AutoWert und kommt aus
        /// <c>DbVorgang.EinfuegenUndId</c> — dieselbe Anweisung wie in
        /// <see cref="Insert"/>, nur ohne zwischenzeitliches Commit.</para>
        /// </summary>
        public VdiUebernahmeErgebnis ImportMitKennlinien(
            string nameOverride,
            IList<(int Vorlauf, int Temperatur, double COP, double Ptherm)> kenndaten,
            IList<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)> kuehlung)
        {
            string bezeichner = nameOverride ?? WPName;

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    object anzahl = v.Skalar(
                        "SELECT COUNT(*) FROM " + TABLE + " WHERE Bezeichner = ?",
                        new DbParam("?", bezeichner ?? ""));
                    if (Convert.ToInt32(anzahl) > 0)
                    {
                        v.Rollback();
                        return VdiUebernahmeErgebnis.Duplikat;
                    }

                    string sql = @"INSERT INTO " + TABLE + @"
                            (Bezeichner, Firma, Beschreibung, Typ, Baujahr, Aufstellung, Nennleistung,
                             maxPtherm, Heizung, Regelung, Modulkosten, Bauart, Kuehlleistung, ReadOnly)
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    DbParam[] ps = {
                        new DbParam("@nam", (object)bezeichner ?? DBNull.Value),
                        new DbParam("@fir", Firma ?? (object)DBNull.Value),
                        new DbParam("@bes", Beschreibung ?? (object)DBNull.Value),
                        new DbParam("@typ", Typ ?? (object)DBNull.Value),
                        new DbParam("@bau", Baujahr),
                        new DbParam("@auf", Aufstellung ?? (object)DBNull.Value),
                        new DbParam("@nen", Nennleistung),
                        new DbParam("@max", maxPTherm),
                        new DbParam("@hei", Heizung),
                        new DbParam("@reg", Regelung ?? (object)DBNull.Value),
                        new DbParam("@mod", Modulkosten),
                        new DbParam("@bart", Bauart ?? (object)DBNull.Value),
                        new DbParam("@kuehl", Kuehlleistung),
                        new DbParam("@ro", false)
                    };

                    int neueId = v.EinfuegenUndId(sql, ps);
                    if (neueId <= 0)
                    {
                        v.Rollback();
                        return VdiUebernahmeErgebnis.Fehler;
                    }

                    if (kenndaten != null && kenndaten.Count > 0)
                    {
                        int naechsteId;
                        {
                            object m = v.Skalar("SELECT MAX(ID) FROM " + CURVE);
                            naechsteId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                        }

                        foreach (var k in kenndaten)
                        {
                            v.Ausfuehren(
                                "INSERT INTO " + CURVE + " (ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm, ReadOnly) VALUES (?, ?, ?, ?, ?, ?, ?)",
                                new DbParam("@id", DbParamTyp.Integer) { Wert = naechsteId++ },
                                new DbParam("@wp", DbParamTyp.Integer) { Wert = neueId },
                                new DbParam("@vor", DbParamTyp.Integer) { Wert = k.Vorlauf },
                                new DbParam("@tem", DbParamTyp.Integer) { Wert = k.Temperatur },
                                new DbParam("@cop", DbParamTyp.Double) { Wert = k.COP },
                                new DbParam("@pth", DbParamTyp.Double) { Wert = k.Ptherm },
                                new DbParam("@ro", DbParamTyp.Boolean) { Wert = false });
                        }
                    }

                    if (kuehlung != null && kuehlung.Count > 0)
                    {
                        int naechsteId;
                        {
                            object m = v.Skalar("SELECT MAX(ID) FROM " + CURVE_K);
                            naechsteId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                        }

                        foreach (var k in kuehlung)
                        {
                            v.Ausfuehren(
                                "INSERT INTO " + CURVE_K + " (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) VALUES (?, ?, ?, ?, ?, ?, ?)",
                                new DbParam("@id", DbParamTyp.Integer) { Wert = naechsteId++ },
                                new DbParam("@wp", DbParamTyp.Integer) { Wert = neueId },
                                new DbParam("@vor", DbParamTyp.Integer) { Wert = k.Vorlauf },
                                new DbParam("@tem", DbParamTyp.Integer) { Wert = k.Temperatur },
                                new DbParam("@cop", DbParamTyp.Double) { Wert = k.COP },
                                new DbParam("@pk", DbParamTyp.Double) { Wert = k.Pkuehl },
                                new DbParam("@last", DbParamTyp.Integer) { Wert = k.Last });
                        }
                    }

                    v.Commit();
                    ID = neueId;
                    WPName = bezeichner;
                    return VdiUebernahmeErgebnis.Gespeichert;
                }
            }
            catch (Exception ex)
            {
                // Kein Aufraeumen noetig: Ohne Commit rollt DbVorgang.Dispose den
                // ganzen Vorgang zurueck - Stammsatz UND Kennlinien.
                Console.WriteLine("Fehler beim WP-Import '" + bezeichner + "': " + ex.Message);
                return VdiUebernahmeErgebnis.Fehler;
            }
        }

        // Legt einen neuen Stammdatensatz an (Import). ReadOnly = false. Die ID ist ein
        // AutoWert und wird vom Einfuegeaufruf des Vorgangs zurueckgeliefert (S4e).
        public bool Insert()
        {
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    // Der innere Block haelt nur den Einzug: das INSERT-SQL steht als
                    // @"…"-Literal, dessen Zeilenumbrueche und Einrueckungen INHALT der
                    // Zeichenkette sind. Sie bleiben mit S4e Zeichen fuer Zeichen stehen.
                    {
                        string sql = @"INSERT INTO " + TABLE + @"
                            (Bezeichner, Firma, Beschreibung, Typ, Baujahr, Aufstellung, Nennleistung,
                             maxPtherm, Heizung, Regelung, Modulkosten, Bauart, Kuehlleistung, ReadOnly,
                             Kuehlbetrieb, Kuehl_Vorlauf, Kuehl_Hilfsstromanteil)
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                        DbParam[] ps = {
                            new DbParam("@nam", WPName ?? (object)DBNull.Value),
                            new DbParam("@fir", Firma ?? (object)DBNull.Value),
                            new DbParam("@bes", Beschreibung ?? (object)DBNull.Value),
                            new DbParam("@typ", Typ ?? (object)DBNull.Value),
                            new DbParam("@bau", Baujahr),
                            new DbParam("@auf", Aufstellung ?? (object)DBNull.Value),
                            new DbParam("@nen", Nennleistung),
                            new DbParam("@max", maxPTherm),
                            new DbParam("@hei", Heizung),
                            new DbParam("@reg", Regelung ?? (object)DBNull.Value),
                            new DbParam("@mod", Modulkosten),
                            new DbParam("@bart", Bauart ?? (object)DBNull.Value),
                            new DbParam("@kuehl", Kuehlleistung),
                            new DbParam("@ro", false),
                            // KU-S3 (Schemaschritt 114): NULL-treu, nie 0 fuer ein leeres Feld
                            new DbParam("@kbet", Kuehlbetrieb),
                            ProjektPuffer.Par("@kvor", DbParamTyp.Integer,
                                KuehlVorlauf.HasValue ? (object)KuehlVorlauf.Value : null),
                            ProjektPuffer.Par("@khs", DbParamTyp.Double,
                                KuehlHilfsstromanteil.HasValue ? (object)KuehlHilfsstromanteil.Value : null)
                        };

                        // ARBEITSPAKET S4e: Einfuegen und ID-Rueckgabe in EINEM Aufruf auf der
                        // Verbindung des Vorgangs (frueher SELECT @@IDENTITY nach dem Commit
                        // auf derselben Verbindung - gleicher Wert, nur eine Anweisung frueher).
                        int neueId = v.EinfuegenUndId(sql, ps);

                        v.Commit();
                        if (neueId > 0) ID = neueId;
                    }
                    return true;
                }
            }
            catch (Exception ex) { Console.WriteLine("Fehler bei Insert (STAMM): " + ex.Message); return false; }
        }

        // Kennlinien-Import (Waerme) in die STAMM-Tabelle. ID explizit (MAX+1), ReadOnly = false.
        public bool InsertKenndatenStamm(int idWp, int vorlauf, int temperatur, double cop, double ptherm)
        {
            object m = DataRepository.ExecuteScalar("SELECT Max(ID) FROM " + CURVE);
            int id = (m == null || m == DBNull.Value) ? 1 : Convert.ToInt32(m) + 1;
            string sql = System.FormattableString.Invariant(
                $@"INSERT INTO {CURVE} (ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm, ReadOnly)
                   VALUES ({id}, {idWp}, {vorlauf}, {temperatur}, {cop}, {ptherm}, FALSE)");
            return DataRepository.ExecuteSQL(sql);
        }

        // Kennlinien-Import (Kuehlung) in die STAMM-Tabelle. ID explizit (MAX+1).
        public bool InsertKenndatenKuehlungStamm(int idWp, int vorlauf, int temperatur, double cop, double pkuehl, int last)
        {
            object m = DataRepository.ExecuteScalar("SELECT Max(ID) FROM " + CURVE_K);
            int id = (m == null || m == DBNull.Value) ? 1 : Convert.ToInt32(m) + 1;
            string sql = System.FormattableString.Invariant(
                $@"INSERT INTO {CURVE_K} (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last])
                   VALUES ({id}, {idWp}, {vorlauf}, {temperatur}, {cop}, {pkuehl}, {last})");
            return DataRepository.ExecuteSQL(sql);
        }

        // SQL und Parameter des Import-Updates - EINE Stelle fuer UpdateImport und
        // UeberschreibeMitKennlinien, damit die Feldliste nicht auseinanderlaufen kann.
        private string ImportUpdateSql()
        {
            return @"UPDATE [" + TABLE + @"] SET
                        Firma = ?, Typ = ?, Baujahr = ?, Aufstellung = ?,
                        Nennleistung = ?, maxPtherm = ?, Heizung = ?, Regelung = ?,
                        Bauart = ?, Kuehlleistung = ?
                      WHERE ID = ?";
        }

        private DbParam[] ImportUpdateParameter(int id)
        {
            return new[] {
                new DbParam("@fir", Firma ?? (object)DBNull.Value),
                new DbParam("@typ", Typ ?? (object)DBNull.Value),
                new DbParam("@bau", Baujahr),
                new DbParam("@auf", Aufstellung ?? (object)DBNull.Value),
                new DbParam("@nen", Nennleistung),
                new DbParam("@max", maxPTherm),
                new DbParam("@hei", Heizung),
                new DbParam("@reg", Regelung ?? (object)DBNull.Value),
                new DbParam("@bart", Bauart ?? (object)DBNull.Value),
                new DbParam("@kuehl", Kuehlleistung),
                new DbParam("@id", id)
            };
        }

        /// <summary>
        /// Import-Ueberschreiben (Dublettenkonzept 4.2): aktualisiert GENAU die Felder,
        /// die der VDI-Import liefert, adressiert per ID. Vom Anwender gepflegte Felder
        /// (Bezeichner, Beschreibung, Modulkosten, ReadOnly) bleiben unangetastet.
        /// </summary>
        /// <remarks>
        /// Bewusst OHNE ReadOnly-Sperre: Das Ueberschreiben eines ReadOnly-Satzes ist
        /// erlaubt und wird vorher im Konfliktdialog bestaetigt (Entscheidung 9.2 -
        /// erlauben mit Hinweis).
        /// </remarks>
        public bool UpdateImport(int id)
        {
            if (id <= 0) return false;
            return DataRepository.ExecuteSQL(ImportUpdateSql(), ImportUpdateParameter(id));
        }

        /// <summary>
        /// Import-Ueberschreiben samt Kennlinien in EINER Transaktion (Dublettenkonzept 4.2):
        /// dasselbe Stammsatz-Update wie <see cref="UpdateImport"/>, danach werden die
        /// Kennlinien (Waerme und Kuehlung) geloescht und durch die neuen Importzeilen
        /// ersetzt. <paramref name="kuehlung"/> darf leer sein.
        /// </summary>
        /// <remarks>
        /// Bewusst OHNE ReadOnly-Sperre: Das Ueberschreiben eines ReadOnly-Satzes ist
        /// erlaubt und wird vorher im Konfliktdialog bestaetigt (Entscheidung 9.2 -
        /// erlauben mit Hinweis). Transaktionsmuster wie
        /// <c>StromganglinieStammCtrl.ImportGanglinie</c>.
        /// </remarks>
        public bool UeberschreibeMitKennlinien(int id,
            IList<(int Vorlauf, int Temperatur, double COP, double Ptherm)> kenndaten,
            IList<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)> kuehlung)
        {
            if (id <= 0) return false;

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    // (1) Stammsatz aktualisieren - identisches UPDATE wie UpdateImport
                    v.Ausfuehren(ImportUpdateSql(), ImportUpdateParameter(id));

                    // (2) Alte Kennlinien beider Tabellen entfernen
                    {
                        List<DbParam> p = new List<DbParam>();
                        p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = id });
                        v.Ausfuehren("DELETE FROM " + CURVE + " WHERE ID_WP = ?", p.ToArray());
                    }
                    {
                        List<DbParam> p = new List<DbParam>();
                        p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = id });
                        v.Ausfuehren("DELETE FROM " + CURVE_K + " WHERE ID_WP = ?", p.ToArray());
                    }

                    // (3) Neue Kennlinien einfuegen. Die ID wird je Tabelle EINMAL als MAX+1
                    //     innerhalb der Transaktion ermittelt und fortlaufend hochgezaehlt.
                    if (kenndaten != null && kenndaten.Count > 0)
                    {
                        int naechsteId;
                        {
                            object m = v.Skalar("SELECT MAX(ID) FROM " + CURVE);
                            naechsteId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                        }

                        foreach (var k in kenndaten)
                        {
                            v.Ausfuehren(
                                "INSERT INTO " + CURVE + " (ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm, ReadOnly) VALUES (?, ?, ?, ?, ?, ?, ?)",
                                new DbParam("@id", DbParamTyp.Integer) { Wert = naechsteId++ },
                                new DbParam("@wp", DbParamTyp.Integer) { Wert = id },
                                new DbParam("@vor", DbParamTyp.Integer) { Wert = k.Vorlauf },
                                new DbParam("@tem", DbParamTyp.Integer) { Wert = k.Temperatur },
                                new DbParam("@cop", DbParamTyp.Double) { Wert = k.COP },
                                new DbParam("@pth", DbParamTyp.Double) { Wert = k.Ptherm },
                                new DbParam("@ro", DbParamTyp.Boolean) { Wert = false });
                        }
                    }

                    // Kuehlung: Tabelle hat KEIN ReadOnly, dafuer ID_Projekt - das bleibt
                    // beim Stamm-Import bewusst leer.
                    if (kuehlung != null && kuehlung.Count > 0)
                    {
                        int naechsteId;
                        {
                            object m = v.Skalar("SELECT MAX(ID) FROM " + CURVE_K);
                            naechsteId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                        }

                        foreach (var k in kuehlung)
                        {
                            v.Ausfuehren(
                                "INSERT INTO " + CURVE_K + " (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) VALUES (?, ?, ?, ?, ?, ?, ?)",
                                new DbParam("@id", DbParamTyp.Integer) { Wert = naechsteId++ },
                                new DbParam("@wp", DbParamTyp.Integer) { Wert = id },
                                new DbParam("@vor", DbParamTyp.Integer) { Wert = k.Vorlauf },
                                new DbParam("@tem", DbParamTyp.Integer) { Wert = k.Temperatur },
                                new DbParam("@cop", DbParamTyp.Double) { Wert = k.COP },
                                new DbParam("@pk", DbParamTyp.Double) { Wert = k.Pkuehl },
                                new DbParam("@last", DbParamTyp.Integer) { Wert = k.Last });
                        }
                    }

                    v.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    DataRepository.FehlerMelden("Fehler beim Überschreiben der Wärmepumpe (Stammdaten): " + ex.Message);
                    return false;
                }
            }
        }

        #endregion

        #region --- PROJEKT -> KATALOG UEBERNAHME ---

        /// <summary>
        /// Die FACHSPALTEN, die eine Uebernahme aus dem Projekt in den Katalog traegt —
        /// alles ausser dem Bezeichner (dem Schluessel) und <c>ReadOnly</c> (dem
        /// Kennzeichen der Auslieferung). <c>Tab_WP</c> fuehrt dieselben Spalten unter
        /// denselben Namen; deshalb reicht EINE Liste fuer Lesen und Schreiben.
        /// </summary>
        private const string UEBERNAHME_SPALTEN =
            "Firma, Beschreibung, Typ, Baujahr, Aufstellung, Nennleistung, maxPtherm, " +
            "Heizung, Regelung, Modulkosten, Laenge, Breite, Hoehe, Gewicht, Raum, " +
            "Kuehlleistung, Bauart, Kuehlbetrieb, Kuehl_Vorlauf, Kuehl_Hilfsstromanteil";

        /// <summary>
        /// Was eine Uebernahme VORFINDEN wird — damit die Oberflaeche ihre Rueckfrage
        /// konkret stellen kann, statt allgemein zu warnen.
        /// </summary>
        /// <param name="Bezeichner">Der Name des Projektgeraets; „" = es gibt den Satz nicht.</param>
        /// <param name="KatalogsatzVorhanden">Steht im Katalog bereits ein Satz gleichen Bezeichners?</param>
        /// <param name="ReadOnly">Ist dieser Katalogsatz ein Auslieferungssatz? Dann wird er nicht ueberschrieben.</param>
        /// <param name="AnzahlProjekteMitKopie">
        /// Wie viele ANDERE Projekte fuehren eine eigene Kopie desselben Geraets? Sie
        /// aendern sich durch die Uebernahme NICHT — der Satz, den sie tragen, ist ihrer.
        /// </param>
        /// <param name="KatalogBezeichner">
        /// Der Name des VERKNUEPFTEN Katalogsatzes (Schemaschritt 80); leer, wenn es
        /// keinen gibt.
        ///
        /// <para>Er ist normalerweise gleich <paramref name="Bezeichner"/> — aber eben
        /// nur normalerweise: Wer den Katalogsatz umbenannt hat, bekommt hier den NEUEN
        /// Namen und im Bezeichner den der Projektkopie. Die Oberflaeche sagt dann
        /// „Katalogsatz ‚Alt' (jetzt ‚Neu')" statt stillschweigend einen zweiten Satz
        /// anzulegen.</para>
        /// </param>
        public sealed record UebernahmeVorschauSatz(string Bezeichner,
                                                    bool KatalogsatzVorhanden,
                                                    bool ReadOnly,
                                                    int AnzahlProjekteMitKopie,
                                                    string KatalogBezeichner = "");

        /// <summary>
        /// Der Katalogsatz einer Projektkopie: ueber den VERWEIS
        /// (<c>Tab_WP.ID_Stamm</c>, Schemaschritt 80), sonst ueber den Bezeichner.
        ///
        /// <para><b>Der Rueckfall ist kein Notnagel, sondern der Altbestand.</b>
        /// <c>ID_Stamm</c> ist nullbar: von Hand angelegte Geraete, ein geloeschter
        /// Katalogsatz und jede Kopie, deren Name beim Nachtrag mehrdeutig war, tragen
        /// keinen Verweis. Erst wenn auch der Verweis ins Leere zeigt, gilt wieder der
        /// Name.</para>
        ///
        /// <para>Die Zeilen tragen <c>ID</c>, <c>ReadOnly</c> und <c>Bezeichner</c> —
        /// den Namen braucht die Vorschau, um eine Umbenennung benennen zu koennen.
        /// Ueber den Namen koennen es MEHRERE Zeilen sein (der Katalog fuehrt keinen
        /// eindeutigen Schluessel darauf); ueber den Verweis ist es immer genau eine.</para>
        /// </summary>
        private static DataTable KatalogsatzZu(int stammId, string bezeichner)
        {
            if (stammId > 0)
            {
                DataTable ueberVerweis = DataRepository.GetDataTable(
                    "SELECT ID, ReadOnly, Bezeichner FROM " + TABLE + " WHERE ID = ?",
                    new DbParam("@id", stammId));
                if (ueberVerweis != null && ueberVerweis.Rows.Count > 0) return ueberVerweis;
            }

            return DataRepository.GetDataTable(
                "SELECT ID, ReadOnly, Bezeichner FROM " + TABLE +
                " WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@bez", bezeichner ?? ""));
        }

        /// <summary>
        /// Was die Uebernahme eines Projektgeraets in den Katalog antreffen wird
        /// (Anwenderentscheid 16.09.2026) — gelesen, nicht geschrieben.
        /// </summary>
        /// <remarks>
        /// Die Oberflaeche formuliert daraus ihre Warnung („Katalogsatz ‚X' wird
        /// ueberschrieben; N weitere Projekte fuehren bereits eine Kopie — sie aendern
        /// sich nicht") und fragt ERST DANN. Die Zahl zaehlt ANDERE Projekte, das eigene
        /// also nicht.
        /// </remarks>
        /// <param name="idWp">Die Projektkopie (<c>Tab_WP.ID</c>).</param>
        /// <param name="idProjekt">Das Projekt (<c>Tab_WP.ID_Projekt</c>).</param>
        public static UebernahmeVorschauSatz UebernahmeVorschau(int idWp, int idProjekt)
        {
            var leer = new UebernahmeVorschauSatz("", false, false, 0);
            if (idWp <= 0 || idProjekt <= 0) return leer;

            try
            {
                DataTable kopf = DataRepository.GetDataTable(
                    "SELECT Bezeichner, " + WaermepumpeKatalogverweis.SPALTE +
                    " FROM Tab_WP WHERE ID = ? AND ID_Projekt = ?",
                    new DbParam("@id", idWp), new DbParam("@proj", idProjekt));
                if (kopf == null || kopf.Rows.Count == 0) return leer;

                DataRow kopie = kopf.Rows[0];
                if (kopie["Bezeichner"] == DBNull.Value) return leer;

                string bezeichner = kopie["Bezeichner"].ToString();

                // DER KATALOGSATZ: erst über den Verweis (Schemaschritt 80), dann über
                // den Namen. Der Verweis überlebt eine Umbenennung des Katalogsatzes -
                // der Name nicht, und genau darin lag der Fehler, den der Entscheid
                // abstellt.
                DataTable dt = KatalogsatzZu(WPCtrl.Verweis(kopie), bezeichner);
                bool vorhanden = dt != null && dt.Rows.Count > 0;
                bool geschuetzt = vorhanden && dt.Rows[0]["ReadOnly"] != DBNull.Value &&
                                  Convert.ToBoolean(dt.Rows[0]["ReadOnly"]);
                string katalogname = vorhanden && dt.Rows[0]["Bezeichner"] != DBNull.Value
                    ? dt.Rows[0]["Bezeichner"].ToString() : "";

                object n = DataRepository.ExecuteScalar(
                    "SELECT COUNT(DISTINCT ID_Projekt) FROM Tab_WP " +
                    "WHERE Bezeichner = ? AND ID_Projekt <> ?",
                    new DbParam("@bez", bezeichner), new DbParam("@proj", idProjekt));
                int andere = (n == null || n == DBNull.Value) ? 0 : Convert.ToInt32(n);

                return new UebernahmeVorschauSatz(bezeichner, vorhanden, geschuetzt, andere,
                                                  katalogname);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei der Uebernahmevorschau: " + ex.Message);
                return leer;
            }
        }

        /// <summary>
        /// Traegt den Stand einer PROJEKTKOPIE in den KATALOG — der Knopf „In Stamm
        /// uebernehmen" des Anlagendialogs (Anwenderentscheid 16.09.2026).
        /// </summary>
        /// <remarks>
        /// <para><b>Die Klammer ist der Bezeichner.</b> <c>Tab_WP</c> fuehrt kein
        /// <c>ID_Stamm</c>; Projektkopie und Katalogsatz kennen einander nur ueber den
        /// Namen. Also gilt: gleicher Bezeichner vorhanden → UPDATE, keiner → INSERT als
        /// neuer Katalogsatz. Ein umbenannter Katalogsatz ist damit ein NEUER — die
        /// Meldung sagt, was geschah.</para>
        ///
        /// <para><b>Auslieferungssaetze bleiben stehen.</b> Ein <c>ReadOnly</c>-Satz
        /// gehoert zur Auslieferung; eine Ueberschreibung ginge beim naechsten
        /// Datenbank-Update ohnehin verloren. Anders als der VDI-Import (Entscheidung 9.2
        /// des Dublettenkonzepts) darf dieser Weg sie deshalb nicht anfassen — er lehnt
        /// benannt ab.</para>
        ///
        /// <para><b>Andere Projekte aendern sich nicht.</b> Geschrieben wird allein der
        /// Katalogsatz. Die Kopien anderer Projekte bleiben, wie sie sind; ihre Zahl
        /// nennt <see cref="UebernahmeVorschau"/> vor der Rueckfrage.</para>
        ///
        /// <para><b><paramref name="mitKennlinien"/> ERSETZT die Katalogstuetzstellen</b>
        /// — beide Tabellen, Waerme wie Kuehlung, wie
        /// <see cref="UeberschreibeMitKennlinien"/> es beim Import tut: loeschen,
        /// neu einfuegen. Der Katalogsatz fuehrt danach genau die Kennlinien der
        /// Projektkopie, auch wenn sie leer ist.</para>
        ///
        /// <para>Alles in EINER Transaktion: Scheitert eine Kennlinienzeile, bleibt auch
        /// der Kopf, wie er war.</para>
        /// </remarks>
        /// <param name="idWp">Die Projektkopie (<c>Tab_WP.ID</c>).</param>
        /// <param name="idProjekt">Das Projekt (<c>Tab_WP.ID_Projekt</c>).</param>
        /// <param name="mitKennlinien">Auch die Stuetzstellen uebernehmen?</param>
        public static SpeicherErgebnis UebernehmenAusProjekt(int idWp, int idProjekt,
                                                             bool mitKennlinien)
        {
            if (idWp <= 0 || idProjekt <= 0)
                return new SpeicherErgebnis(false, Text("WP_STAMM_UEBERNAHME_MSG_FEHLER",
                    "Die Übernahme in den Katalog ist fehlgeschlagen."), "");

            try
            {
                DataTable quelle = DataRepository.GetDataTable(
                    "SELECT Bezeichner, " + WaermepumpeKatalogverweis.SPALTE + ", " +
                    UEBERNAHME_SPALTEN +
                    " FROM Tab_WP WHERE ID = ? AND ID_Projekt = ?",
                    new DbParam("@id", idWp), new DbParam("@proj", idProjekt));
                if (quelle == null || quelle.Rows.Count == 0)
                    return new SpeicherErgebnis(false, WPCtrl.NichtGefunden(idWp), "");

                DataRow satz = quelle.Rows[0];
                string bezeichner = satz["Bezeichner"] == DBNull.Value
                    ? "" : satz["Bezeichner"].ToString();

                // (1) Der Katalogsatz: ueber den VERWEIS (Schemaschritt 80), sonst ueber
                //     den Bezeichner. Ueber den Verweis ist es immer genau EINER - eine
                //     Umbenennung des Katalogsatzes legt damit keinen zweiten mehr an.
                //     Ueber den Namen koennen es mehrere sein: Tab_WP_STAMM fuehrt auf
                //     Bezeichner keinen eindeutigen Schluessel, und ein Update traefe
                //     dann BEIDE Saetze (Klammer wie
                //     HeizkesselStammCtrl.AnzeigefelderSchreiben).
                DataTable katalog = KatalogsatzZu(WPCtrl.Verweis(satz), bezeichner);
                int anzahl = katalog == null ? 0 : katalog.Rows.Count;

                if (anzahl > 1)
                    return new SpeicherErgebnis(false,
                        string.Format(MyResource.Resource.ADM_MEHRDEUTIG_TEXT, bezeichner, anzahl),
                        bezeichner);

                int katalogId = 0;
                if (anzahl == 1)
                {
                    DataRow k = katalog.Rows[0];
                    katalogId = k["ID"] != DBNull.Value ? Convert.ToInt32(k["ID"]) : 0;

                    bool geschuetzt = k["ReadOnly"] != DBNull.Value && Convert.ToBoolean(k["ReadOnly"]);
                    if (geschuetzt)
                        return new SpeicherErgebnis(false,
                            Text("IMP_KONFLIKT_HINWEIS_READONLY",
                                 "Auslieferungssatz: Eine Überschreibung geht beim nächsten Datenbank-Update verloren.") +
                            " " +
                            Text("WP_STAMM_UEBERNAHME_MSG_READONLY",
                                 "Auslieferungssätze werden nicht überschrieben."),
                            bezeichner);
                }

                // (2) Die Stuetzstellen der Projektkopie - VOR der Transaktion gelesen,
                //     wie in CopyFromStamm.
                DataTable pw = null, pk = null;
                if (mitKennlinien)
                {
                    pw = DataRepository.GetDataTable(
                        "SELECT Vorlauf, Temperatur, COP, Ptherm FROM Tab_Kenndaten " +
                        "WHERE ID_WP = ? ORDER BY ID", new DbParam("@id", idWp));
                    pk = DataRepository.GetDataTable(
                        "SELECT Vorlauf, Temperatur, COP, Pkuehl, [Last] FROM Tab_Kenndaten_Kuehlung " +
                        "WHERE ID_WP = ? ORDER BY ID", new DbParam("@id", idWp));
                }

                bool neu = anzahl == 0;

                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        if (neu)
                        {
                            var p = new List<DbParam>();
                            p.Add(new DbParam("@bez", (object)bezeichner ?? DBNull.Value));
                            p.AddRange(Uebernahmewerte(satz));
                            p.Add(new DbParam("@ro", DbParamTyp.Boolean) { Wert = false });

                            katalogId = v.EinfuegenUndId(
                                "INSERT INTO " + TABLE + " (Bezeichner, " + UEBERNAHME_SPALTEN +
                                ", ReadOnly) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                                p.ToArray());
                            if (katalogId <= 0) throw new InvalidOperationException(
                                "Der Katalogsatz konnte nicht angelegt werden.");
                        }
                        else
                        {
                            var p = new List<DbParam>(Uebernahmewerte(satz));
                            p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = katalogId });

                            v.Ausfuehren(
                                "UPDATE " + TABLE + " SET Firma = ?, Beschreibung = ?, Typ = ?, " +
                                "Baujahr = ?, Aufstellung = ?, Nennleistung = ?, maxPtherm = ?, " +
                                "Heizung = ?, Regelung = ?, Modulkosten = ?, Laenge = ?, Breite = ?, " +
                                "Hoehe = ?, Gewicht = ?, Raum = ?, Kuehlleistung = ?, Bauart = ?, " +
                                "Kuehlbetrieb = ?, Kuehl_Vorlauf = ?, Kuehl_Hilfsstromanteil = ? " +
                                "WHERE ID = ?",
                                p.ToArray());
                        }

                        // DIE KLAMMER FESTZIEHEN (Schemaschritt 80). Nach einem INSERT
                        // gibt es den Katalogsatz erst seit dieser Zeile - ohne den
                        // Verweis faende die Projektkopie ihn beim naechsten Mal wieder
                        // nur ueber den Namen. Und wo der Satz ueber den NAMEN gefunden
                        // wurde (Kopie ohne Verweis, Altbestand), entsteht die Klammer
                        // hier: Danach traegt die Umbenennung des Katalogsatzes nichts
                        // mehr aus.
                        v.Ausfuehren(
                            "UPDATE Tab_WP SET " + WaermepumpeKatalogverweis.SPALTE + " = ? " +
                            "WHERE ID = ? AND ID_Projekt = ?",
                            new DbParam("@stamm", DbParamTyp.Integer) { Wert = katalogId },
                            new DbParam("@id", DbParamTyp.Integer) { Wert = idWp },
                            new DbParam("@proj", DbParamTyp.Integer) { Wert = idProjekt });

                        if (mitKennlinien)
                        {
                            v.Ausfuehren("DELETE FROM " + CURVE + " WHERE ID_WP = ?",
                                new DbParam("@id", DbParamTyp.Integer) { Wert = katalogId });
                            v.Ausfuehren("DELETE FROM " + CURVE_K + " WHERE ID_WP = ?",
                                new DbParam("@id", DbParamTyp.Integer) { Wert = katalogId });

                            if (pw != null && pw.Rows.Count > 0)
                            {
                                int id;
                                {
                                    object m = v.Skalar("SELECT Max(ID) FROM " + CURVE);
                                    id = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                                }
                                foreach (DataRow r in pw.Rows)
                                    v.Ausfuehren(
                                        "INSERT INTO " + CURVE +
                                        " (ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm, ReadOnly) VALUES (?, ?, ?, ?, ?, ?, ?)",
                                        new DbParam("@id", DbParamTyp.Integer) { Wert = id++ },
                                        new DbParam("@wp", DbParamTyp.Integer) { Wert = katalogId },
                                        new DbParam("@vor", Feldwert(r, "Vorlauf")),
                                        new DbParam("@tem", Feldwert(r, "Temperatur")),
                                        new DbParam("@cop", Feldwert(r, "COP")),
                                        new DbParam("@pth", Feldwert(r, "Ptherm")),
                                        new DbParam("@ro", DbParamTyp.Boolean) { Wert = false });
                            }

                            if (pk != null && pk.Rows.Count > 0)
                            {
                                int id;
                                {
                                    object m = v.Skalar("SELECT Max(ID) FROM " + CURVE_K);
                                    id = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                                }
                                foreach (DataRow r in pk.Rows)
                                    v.Ausfuehren(
                                        "INSERT INTO " + CURVE_K +
                                        " (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) VALUES (?, ?, ?, ?, ?, ?, ?)",
                                        new DbParam("@id", DbParamTyp.Integer) { Wert = id++ },
                                        new DbParam("@wp", DbParamTyp.Integer) { Wert = katalogId },
                                        new DbParam("@vor", Feldwert(r, "Vorlauf")),
                                        new DbParam("@tem", Feldwert(r, "Temperatur")),
                                        new DbParam("@cop", Feldwert(r, "COP")),
                                        new DbParam("@pk", Feldwert(r, "Pkuehl")),
                                        new DbParam("@last", Feldwert(r, "Last")));
                            }
                        }

                        v.Commit();
                    }
                    catch (Exception ex)
                    {
                        try { v.Rollback(); } catch { }
                        Console.WriteLine("Fehler bei der Übernahme in den Katalog: " + ex.Message);
                        return new SpeicherErgebnis(false, Text("WP_STAMM_UEBERNAHME_MSG_FEHLER",
                            "Die Übernahme in den Katalog ist fehlgeschlagen."), bezeichner);
                    }
                }

                return new SpeicherErgebnis(true, string.Format(
                    neu
                        ? Text("WP_STAMM_UEBERNAHME_MSG_ANGELEGT", "Katalogsatz „{0}“ neu angelegt.")
                        : Text("WP_STAMM_UEBERNAHME_MSG_UEBERSCHRIEBEN", "Katalogsatz „{0}“ überschrieben."),
                    bezeichner), bezeichner);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei der Übernahme in den Katalog: " + ex.Message);
                return new SpeicherErgebnis(false, Text("WP_STAMM_UEBERNAHME_MSG_FEHLER",
                    "Die Übernahme in den Katalog ist fehlgeschlagen."), "");
            }
        }

        /// <summary>
        /// Die Werte der zwanzig Fachspalten in der Reihenfolge von
        /// <see cref="UEBERNAHME_SPALTEN"/> — EINE Stelle fuer INSERT und UPDATE, damit
        /// Spaltenliste und Werte nicht auseinanderlaufen koennen.
        /// </summary>
        private static DbParam[] Uebernahmewerte(DataRow satz)
        {
            return new[]
            {
                new DbParam("@fir", Feldwert(satz, "Firma")),
                new DbParam("@bes", Feldwert(satz, "Beschreibung")),
                new DbParam("@typ", Feldwert(satz, "Typ")),
                new DbParam("@bau", Feldwert(satz, "Baujahr")),
                new DbParam("@auf", Feldwert(satz, "Aufstellung")),
                new DbParam("@nen", Feldwert(satz, "Nennleistung")),
                new DbParam("@max", Feldwert(satz, "maxPtherm")),
                new DbParam("@hei", Feldwert(satz, "Heizung")),
                new DbParam("@reg", Feldwert(satz, "Regelung")),
                new DbParam("@mod", Feldwert(satz, "Modulkosten")),
                new DbParam("@lae", Feldwert(satz, "Laenge")),
                new DbParam("@bre", Feldwert(satz, "Breite")),
                new DbParam("@hoe", Feldwert(satz, "Hoehe")),
                new DbParam("@gew", Feldwert(satz, "Gewicht")),
                new DbParam("@rau", Feldwert(satz, "Raum")),
                new DbParam("@kue", Feldwert(satz, "Kuehlleistung")),
                new DbParam("@bart", Feldwert(satz, "Bauart")),
                // KU-S3 (Schemaschritt 114): dieselben Namen in Tab_WP und Tab_WP_STAMM
                new DbParam("@kbet", Feldwert(satz, KuehlungSchema.SPALTE_ERZEUGER_KUEHLBETRIEB)),
                new DbParam("@kvor", Feldwert(satz, KuehlungSchema.SPALTE_KUEHL_VORLAUF)),
                new DbParam("@khs", Feldwert(satz, KuehlungSchema.SPALTE_KUEHL_HILFSSTROMANTEIL))
            };
        }

        /// <summary>Der rohe Spaltenwert; fehlende Spalte und <c>null</c> ergeben <c>DBNull</c>.</summary>
        private static object Feldwert(DataRow satz, string spalte)
        {
            object v = satz.Table.Columns.Contains(spalte) ? satz[spalte] : DBNull.Value;
            return v ?? DBNull.Value;
        }

        #endregion

        #region --- MAPPING ---

        private void MapDataTableToItems(DataTable dt)
        {
            _internalList.Clear();
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
            {
                WPModel item = new WPModel();
                FillModel(item, dt, row);
                _internalList.Add(item);
            }
        }

        private void MapRowToThis(DataRow row)
        {
            FillModel(this, row.Table, row);
        }

        private void FillModel(WPModel item, DataTable dt, DataRow row)
        {
            if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.ID = Convert.ToInt32(row["ID"]);
            if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) item.WPName = row["Bezeichner"].ToString();
            if (dt.Columns.Contains("Firma") && row["Firma"] != DBNull.Value) item.Firma = row["Firma"].ToString();
            if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) item.Beschreibung = row["Beschreibung"].ToString();
            if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value) item.Typ = row["Typ"].ToString();
            if (dt.Columns.Contains("Baujahr") && row["Baujahr"] != DBNull.Value) item.Baujahr = Convert.ToInt32(row["Baujahr"]);
            if (dt.Columns.Contains("Aufstellung") && row["Aufstellung"] != DBNull.Value) item.Aufstellung = row["Aufstellung"].ToString();
            if (dt.Columns.Contains("Nennleistung") && row["Nennleistung"] != DBNull.Value) item.Nennleistung = Convert.ToInt32(row["Nennleistung"]);
            if (dt.Columns.Contains("maxPtherm") && row["maxPtherm"] != DBNull.Value) item.maxPTherm = Convert.ToInt32(row["maxPtherm"]);
            if (dt.Columns.Contains("Heizung") && row["Heizung"] != DBNull.Value) item.Heizung = Convert.ToInt32(row["Heizung"]);
            if (dt.Columns.Contains("Regelung") && row["Regelung"] != DBNull.Value) item.Regelung = row["Regelung"].ToString();
            if (dt.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value) item.Modulkosten = Convert.ToInt32(row["Modulkosten"]);
            if (dt.Columns.Contains("Kuehlleistung") && row["Kuehlleistung"] != DBNull.Value) item.Kuehlleistung = Convert.ToDouble(row["Kuehlleistung"]);
            if (dt.Columns.Contains("Bauart") && row["Bauart"] != DBNull.Value) item.Bauart = row["Bauart"].ToString();
            if (dt.Columns.Contains("Max") && row["Max"] != DBNull.Value) item.MaxVorlauf = Convert.ToInt32(row["Max"]);
            if (dt.Columns.Contains("Min") && row["Min"] != DBNull.Value) item.MinVorlauf = Convert.ToInt32(row["Min"]);
            item.m_bReadOnly = dt.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);
            WPCtrl.KuehlfelderLesen(dt, row, item);   // KU-S3, NULL-treu (Schemaschritt 114)
        }

        #endregion
    }
}
