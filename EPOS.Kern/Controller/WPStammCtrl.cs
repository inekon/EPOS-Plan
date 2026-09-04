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
                             maxPtherm, Heizung, Regelung, Modulkosten, Bauart, Kuehlleistung, ReadOnly)
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
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
                            new DbParam("@ro", false)
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
        }

        #endregion
    }
}
