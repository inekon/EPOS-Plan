using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Controller fuer die PROJEKTGERAETE der Waermepumpen (<c>Tab_WP</c>) samt der
    /// Kopie eines Stammsatzes in ein Projekt.
    ///
    /// <para><b>Seit iU9-W7.0a im Kern.</b> Die Klasse lag bis dahin in
    /// <c>WindowsFormsApplication1\Controller</c> und war <c>partial</c>: Ein zweiter
    /// Teil (<c>WPCtrl.WinForms.cs</c>) trug die Methode <c>FillListBox(ListBox)</c>.
    /// Ein partieller Typ geht nicht ueber die Assemblygrenze (Lehre aus
    /// <c>WizardSeite</c>) — und der Umzug ist noetig, weil die Huellen der
    /// Waermepumpen-Dialoge (W7.3 bis W7.5) den PROJEKTSTAND lesen muessen, ohne
    /// WinForms zu kennen.</para>
    ///
    /// <para><b><c>FillListBox</c> ist ersatzlos entfallen.</b> Die Methode hatte im
    /// gesamten Bestand keinen einzigen Aufrufer; die gleichnamige Methode in
    /// <see cref="KlimaregionStammCtrl"/> gehoert einer anderen Klasse und bleibt, wo
    /// sie ist. Sie in die Anwendung zurueckzuspiegeln haette eine
    /// Erweiterungsmethode gekostet, die niemand ruft.</para>
    /// </summary>
    class WPCtrl : WPModel
    {
        /// <summary>
        /// Eine Kennlinienzeile in die PROJEKTtabelle schreiben — EINE Anweisung für
        /// beide Wege (Katalogkopie und Nachholknopf).
        ///
        /// <para><b><c>ID_Projekt</c> kommt aus der Wärmepumpe, nicht vom Aufrufer.</b>
        /// Bis Schemaschritt 96 blieb die Spalte beim Schreiben ausgelassen und fiel auf
        /// ihren <c>DEFAULT 0</c> zurück — daher die 1.446 Zeilen der Testdatenbank, die
        /// an einer echten Wärmepumpe hingen und trotzdem auf kein Projekt zeigten. Seit
        /// die Spalte einen Fremdschlüssel auf <c>Tab_Projekt</c> trägt, wiese die
        /// Datenbank diese 0 ab; die Unterabfrage holt die Projektnummer dort, wo sie
        /// ohnehin steht, statt sie durch den Aufrufer zu reichen und irgendwo zu
        /// vergessen. <c>Tab_Kenndaten</c> ist die Projekttabelle — der Katalog liegt in
        /// <c>Tab_Kenndaten_STAMM</c>.</para>
        /// </summary>
        internal const string SQL_KENNLINIE_EINFUEGEN =
            "INSERT INTO Tab_Kenndaten (ID, ID_Projekt, ID_WP, Vorlauf, Temperatur, COP, Ptherm) " +
            "VALUES (?, (SELECT w.ID_Projekt FROM Tab_WP w WHERE w.ID = ?), ?, ?, ?, ?, ?)";

        private List<WPModel> _internalList = new List<WPModel>();
        public int rows => _internalList.Count;
        public new List<WPModel> items => _internalList;

        public WPCtrl()
        {
        }

        public bool Delete()
        {
            try
            {
                string sql = "DELETE FROM Tab_WP WHERE Bezeichner = ?";
                DbParam[] ps = { new DbParam("@nam", WPName ?? (object)DBNull.Value) };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Delete: " + ex.Message);
                return false;
            }
        }

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
                        // Parametrisierter INSERT-Befehl
                        string insertSql = @"INSERT INTO Tab_WP 
                                            (
                                                Bezeichner, ID_Projekt, Firma, Beschreibung, Typ, 
                                                Baujahr, Aufstellung, Nennleistung, maxPTherm, 
                                                Heizung, Regelung, Modulkosten, Bauart, Kuehlleistung
                                            ) 
                                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                        DbParam[] ps = {
                            new DbParam("@nam", WPName ?? (object)DBNull.Value),
                            new DbParam("@proj", ID_Projekt),
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
                            new DbParam("@kuehl", Kuehlleistung)
                        };

                        // ARBEITSPAKET S4e: Einfuegen und ID-Rueckgabe in EINEM Aufruf auf der
                        // Verbindung des Vorgangs. Frueher stand SELECT @@IDENTITY NACH dem
                        // Commit auf derselben, noch offenen Verbindung; jetzt liefert der
                        // Einfuegeaufruf die ID unmittelbar VOR dem Commit. Verbindung und
                        // Wert sind dieselben, nur der Lesezeitpunkt liegt eine Anweisung
                        // frueher - anders ist die ID nach dem Commit nicht mehr zu haben.
                        int neueId = v.EinfuegenUndId(insertSql, ps);

                        v.Commit(); // Schreibt die Daten jetzt unwiderruflich in die Datenbank

                        if (neueId > 0)
                        {
                            ID = neueId;
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Insert: " + ex.Message);
                return false;
            }
        }

        public void ReadAll(string filter = "")
        {
            string sql = string.IsNullOrEmpty(filter)
                ? "SELECT * FROM Tab_WP ORDER BY Bezeichner"
                : "SELECT * FROM Tab_WP WHERE " + filter;

            DataTable dt = DataRepository.GetDataTable(sql, null);
            MapDataTableToItems(dt);
        }

        public void ReadAll_MitMinMaxVorlauf(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql, null);
            MapDataTableToItems(dt);
        }

        public void ReadSingle(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql, null);
            _internalList.Clear(); // Liste leeren bei ReadSingle

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (row["ID"] != DBNull.Value) ID = Convert.ToInt32(row["ID"]);
                if (row["Bezeichner"] != DBNull.Value) WPName = row["Bezeichner"].ToString();
                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value) ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);
                if (row["Firma"] != DBNull.Value) Firma = row["Firma"].ToString();
                if (row["Beschreibung"] != DBNull.Value) Beschreibung = row["Beschreibung"].ToString();
                if (row["Typ"] != DBNull.Value) Typ = row["Typ"].ToString();
                if (row["Baujahr"] != DBNull.Value) Baujahr = Convert.ToInt32(row["Baujahr"]);
                if (row["Aufstellung"] != DBNull.Value) Aufstellung = row["Aufstellung"].ToString();
                if (row["Nennleistung"] != DBNull.Value) Nennleistung = Convert.ToInt32(row["Nennleistung"]);
                if (row["Heizung"] != DBNull.Value) Heizung = Convert.ToInt32(row["Heizung"]);
                if (row["Regelung"] != DBNull.Value) Regelung = row["Regelung"].ToString();
                if (row["Modulkosten"] != DBNull.Value) Modulkosten = Convert.ToInt32(row["Modulkosten"]);
                if (dt.Columns.Contains("Kuehlleistung") && row["Kuehlleistung"] != DBNull.Value) Kuehlleistung = Convert.ToDouble(row["Kuehlleistung"]);
                if (dt.Columns.Contains("Bauart") && row["Bauart"] != DBNull.Value) Bauart = row["Bauart"].ToString();

                // Bei ReadSingle fügen wir diese Instanz (this) als Kopie hinzu, damit rows auf 1 springt
                _internalList.Add(this);
            }
        }

        #region --- PROJEKTGERAET SCHREIBEN (Tab_WP) ---

        /// <summary>
        /// Was ein Schreibversuch am PROJEKTGERAET ergeben hat — derselbe Zuschnitt wie
        /// <see cref="WPStammCtrl.SpeicherErgebnis"/>.
        /// </summary>
        /// <param name="Ok">Wurde geschrieben?</param>
        /// <param name="Meldung">Der Grund im Klartext, bereits lokalisiert.</param>
        /// <param name="Name">Der Bezeichner des Geraets — er aendert sich hier nie.</param>
        public sealed record SpeicherErgebnis(bool Ok, string Meldung, string Name);

        /// <summary>
        /// Die STAMMFELDER, die der Anlagendialog am Projektgeraet bearbeitet
        /// (Anwenderentscheid 16.09.2026).
        /// </summary>
        /// <remarks>
        /// <para><b>Der Bezeichner steht nicht darin.</b> Er ist im Anlagendialog
        /// unveraenderlich und zugleich die einzige Klammer zwischen Projektkopie und
        /// Katalogsatz (<c>Tab_WP</c> fuehrt kein <c>ID_Stamm</c>) — wer ihn aendern
        /// koennte, zerrisse sie.</para>
        /// <para><b><c>null</c> heisst „unveraendert lassen"</b>, wie bei
        /// <c>HeizkesselStammCtrl.AnzeigefelderHeizkessel</c>: Der Schreibweg liest den
        /// Satz, legt die mitgegebenen Felder darueber und schreibt zurueck; ein
        /// ausgelassenes Feld wuerde sonst als 0 oder leer ueber einen gepflegten Wert
        /// laufen.</para>
        /// </remarks>
        /// <param name="Firma">Hersteller.</param>
        /// <param name="Beschreibung">Beschreibung.</param>
        /// <param name="Typ">Typ (Waermequelle).</param>
        /// <param name="Regelung">Regelung / Leistungsstufen.</param>
        /// <param name="Aufstellung">Aufstellungsart.</param>
        /// <param name="Baujahr">Baujahr.</param>
        /// <param name="Nennleistung">Nennleistung [kW].</param>
        /// <param name="Heizung">Leistung des Heizstabs [kW] (Spalte <c>Tab_WP.Heizung</c>).</param>
        /// <param name="Kuehlleistung">
        /// Kuehlleistung [kW] (Spalte <c>Tab_WP.Kuehlleistung</c>, <c>REAL</c>).
        ///
        /// <para><b>Sie ist eine KOMMAZAHL und keine ganze</b> — anders als Nennleistung
        /// und Heizstableistung, die als <c>INTEGER</c> stehen. Ein <c>int?</c> an dieser
        /// Stelle schnitte 5,5 kW auf 5 kW ab, und zwar still; die Uebernahme in den
        /// Katalog (<see cref="WPStammCtrl.UebernehmenAusProjekt"/>) traegt die Spalte
        /// unveraendert mit.</para>
        /// </param>
        public sealed record ProjektgeraetFelder(string Firma = null,
                                                 string Beschreibung = null,
                                                 string Typ = null,
                                                 string Regelung = null,
                                                 string Aufstellung = null,
                                                 int? Baujahr = null,
                                                 int? Nennleistung = null,
                                                 int? Heizung = null,
                                                 double? Kuehlleistung = null);

        /// <summary>Das kleinste zulaessige Baujahr.</summary>
        public const int BAUJAHR_KLEINSTES = 1900;

        /// <summary>
        /// Das groesste zulaessige Baujahr: das naechste Kalenderjahr — ein Geraet darf
        /// bestellt und mit dem kommenden Baujahr geplant sein.
        /// </summary>
        /// <remarks>
        /// Der Stammdialog bietet eine geschlossene Klappliste der letzten zehn Jahre
        /// an, laesst einen gespeicherten Wert ausserhalb der Liste aber stehen
        /// (<c>WaermepumpeStammFelder.Baujahreintraege</c>). Eine Liste ist keine
        /// Wertpruefung; der Kern zieht deshalb den weiten Rahmen, der einen Zahlendreher
        /// („20025") faengt, ohne einen Altbestand abzulehnen.
        /// </remarks>
        public static int BaujahrGroesstes => DateTime.Now.Year + 1;

        /// <summary>
        /// Schreibt die STAMMFELDER in die PROJEKTKOPIE eines Geraets — der Speicherweg
        /// des Waermepumpen-Anlagendialogs (Anwenderentscheid 16.09.2026).
        /// </summary>
        /// <remarks>
        /// <para><b>Warum nicht in den Katalog.</b> Die Felder Hersteller, Beschreibung,
        /// Typ, Regelung, Aufstellung, Baujahr, Nennleistung, Heizstableistung und
        /// Kuehlleistung standen
        /// im Anlagendialog bisher fuer den KATALOGSATZ. Wer sie dort aenderte, aenderte
        /// sie fuer jedes andere Projekt mit. Sie gehoeren zur Anlage dieses Projekts —
        /// also in <c>Tab_WP</c>, und der Rechenweg liest genau diese Zeile
        /// (<c>SimulationWaermepumpe.ModuleAufbauen</c>). In den Katalog kommt der Stand
        /// nur auf ausdruecklichen Zuruf ueber
        /// <see cref="WPStammCtrl.UebernehmenAusProjekt"/>.</para>
        ///
        /// <para><b>Adressiert wird ueber ID UND ID_Projekt, nie ueber den
        /// Bezeichner.</b> <c>Tab_WP.Bezeichner</c> ist NICHT eindeutig: Dieselbe
        /// Waermepumpe steht in der Testdatenbank in bis zu vier Projekten unter
        /// demselben Namen. Genau daran starb die Vorgaengermethode <c>WPCtrl.Update()</c>
        /// — sie filterte <c>WHERE Bezeichner = ?</c> OHNE Projekt und haette beim ersten
        /// Aufruf die gleichnamigen Geraete ALLER Projekte ueberschrieben. Sie hatte im
        /// ganzen Bestand keinen Aufrufer und ist mit diesem Auftrag entfallen; der
        /// Projektfilter hier ist ihr Ersatz und zugleich die Lehre daraus.</para>
        ///
        /// <para><b>Read-modify-write.</b> Was der Aufrufer nicht mitgibt
        /// (<c>null</c>), wird aus dem gelesenen Satz zurueckgeschrieben — auch
        /// <c>NULL</c> bleibt <c>NULL</c>.</para>
        /// </remarks>
        /// <param name="idWp">Die Projektkopie (<c>Tab_WP.ID</c>).</param>
        /// <param name="idProjekt">Das Projekt (<c>Tab_WP.ID_Projekt</c>).</param>
        /// <param name="felder">Die zu schreibenden Felder; <c>null</c> = unveraendert.</param>
        public static SpeicherErgebnis ProjektgeraetSchreiben(int idWp, int idProjekt,
                                                              ProjektgeraetFelder felder)
        {
            if (felder == null || idWp <= 0 || idProjekt <= 0)
                return new SpeicherErgebnis(false, Text("WP_PROJ_MSG_FEHLER",
                    "Die Projektdaten konnten nicht gespeichert werden."), "");

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner, Firma, Beschreibung, Typ, Baujahr, Aufstellung, " +
                    "Nennleistung, Heizung, Regelung, Kuehlleistung " +
                    "FROM Tab_WP WHERE ID = ? AND ID_Projekt = ?",
                    new DbParam("@id", idWp), new DbParam("@proj", idProjekt));

                if (dt == null || dt.Rows.Count == 0)
                    return new SpeicherErgebnis(false, NichtGefunden(idWp), "");

                DataRow satz = dt.Rows[0];
                string bezeichner = Spaltentext(satz, "Bezeichner");

                string grund = FelderPruefen(felder);
                if (!string.IsNullOrEmpty(grund))
                    return new SpeicherErgebnis(false, grund, bezeichner);

                bool ok = DataRepository.ExecuteSQL(
                    "UPDATE Tab_WP SET Firma = ?, Beschreibung = ?, Typ = ?, Regelung = ?, " +
                    "Aufstellung = ?, Baujahr = ?, Nennleistung = ?, Heizung = ?, " +
                    "Kuehlleistung = ? WHERE ID = ? AND ID_Projekt = ?",
                    new DbParam("@fir", Uebernommen(felder.Firma, satz, "Firma")),
                    new DbParam("@bes", Uebernommen(felder.Beschreibung, satz, "Beschreibung")),
                    new DbParam("@typ", Uebernommen(felder.Typ, satz, "Typ")),
                    new DbParam("@reg", Uebernommen(felder.Regelung, satz, "Regelung")),
                    new DbParam("@auf", Uebernommen(felder.Aufstellung, satz, "Aufstellung")),
                    new DbParam("@bau", Uebernommen(felder.Baujahr, satz, "Baujahr")),
                    new DbParam("@nen", Uebernommen(felder.Nennleistung, satz, "Nennleistung")),
                    new DbParam("@hei", Uebernommen(felder.Heizung, satz, "Heizung")),
                    new DbParam("@kue", Uebernommen(felder.Kuehlleistung, satz, "Kuehlleistung")),
                    new DbParam("@id", idWp),
                    new DbParam("@proj", idProjekt));

                return ok
                    ? new SpeicherErgebnis(true, Text("WP_PROJ_MSG_GESPEICHERT",
                        "Projektgerät gespeichert"), bezeichner)
                    : new SpeicherErgebnis(false, Text("WP_PROJ_MSG_FEHLER",
                        "Die Projektdaten konnten nicht gespeichert werden."), bezeichner);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Schreiben des Projektgeraets: " + ex.Message);
                return new SpeicherErgebnis(false, Text("WP_PROJ_MSG_FEHLER",
                    "Die Projektdaten konnten nicht gespeichert werden."), "");
            }
        }

        /// <summary>
        /// Fuehrt das Projekt zu dieser Geraete-Id eine eigene KOPIE?
        /// (<c>Tab_WP.ID = ? AND ID_Projekt = ?</c>)
        /// </summary>
        /// <remarks>
        /// Die Frage entscheidet in der Oberflaeche ueber die WEICHE SPERRE zweier Knoepfe
        /// des Anlagendialogs: Vor dem ersten Speichern traegt <c>ID_WP</c> die KATALOG-Id
        /// — es gibt dann weder Projektkennlinien zu bearbeiten noch eine Projektzeile in
        /// den Katalog zu uebernehmen. Der Dialog fragt lieber vorher, als hinterher eine
        /// benannte Ablehnung zu zeigen.
        /// </remarks>
        public static bool ProjektgeraetVorhanden(int idWp, int idProjekt)
        {
            if (idWp <= 0 || idProjekt <= 0) return false;

            try
            {
                object v = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_WP WHERE ID = ? AND ID_Projekt = ?",
                    new DbParam("@id", idWp), new DbParam("@proj", idProjekt));
                return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei der Suche nach dem Projektgeraet: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Die benannte Ablehnung, wenn es die Projektkopie nicht gibt — mit dem
        /// haeufigsten Grund zuerst.
        /// </summary>
        /// <remarks>
        /// Die Anlagenzeile fuehrt in <c>ID_WP</c> bis zum ersten Speichern die
        /// KATALOG-Id (<c>Tab_WP_STAMM.ID</c>, siehe
        /// <see cref="WaermepumpeGeraeteCtrl"/>); erst der Speicherweg der Verwaltung
        /// legt die Kopie ueber <see cref="CopyFromStamm(int,int)"/> an. Wer in diesem
        /// Zustand speichert, bekommt den Grund genannt statt „Datensatz nicht
        /// gefunden".
        /// </remarks>
        internal static string NichtGefunden(int idWp)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + WPStammCtrl.TABLE + " WHERE ID = ?",
                new DbParam("@id", idWp));
            bool istKatalogId = v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;

            return istKatalogId
                ? Text("WP_PROJ_MSG_NICHT_GESPEICHERT",
                       "Die Anlage ist noch nicht gespeichert; die Projektdaten entstehen mit dem ersten Speichern.")
                : Text("WP_PROJ_MSG_KEIN_SATZ",
                       "Das Gerät steht nicht in diesem Projekt.");
        }

        /// <summary>
        /// Prueft die Zahlenfelder; der Rueckgabewert ist der Ablehnungsgrund im
        /// Klartext oder <c>null</c>. Erst pruefen, dann schreiben — ein halb
        /// uebernommener Satz entsteht so gar nicht.
        /// </summary>
        private static string FelderPruefen(ProjektgeraetFelder f)
        {
            if (f.Nennleistung.HasValue && f.Nennleistung.Value < 0)
                return Negativ(Text("WPS_LBL_NENNLEISTUNG", "Nennleistung"));

            if (f.Heizung.HasValue && f.Heizung.Value < 0)
                return Negativ(Text("WPS_LBL_HEIZSTAB", "Heizstab"));

            if (f.Kuehlleistung.HasValue && f.Kuehlleistung.Value < 0)
                return Negativ(Text("WPS_LBL_KUEHLLEISTUNG", "Kühlleistung"));

            if (f.Baujahr.HasValue &&
                (f.Baujahr.Value < BAUJAHR_KLEINSTES || f.Baujahr.Value > BaujahrGroesstes))
                return string.Format(
                    Text("KBROW_MSG_WERT_BEREICH", "„{0}“ muss zwischen {1} und {2} liegen."),
                    Text("WPS_LBL_BAUJAHR", "Baujahr"),
                    BAUJAHR_KLEINSTES.ToString(),
                    BaujahrGroesstes.ToString());

            return null;
        }

        /// <summary>Die Ablehnung eines negativen Wertes, im Wortlaut des Aufklappers.</summary>
        private static string Negativ(string feldname)
            => string.Format(Text("KBROW_MSG_WERT_NEGATIV", "„{0}“ darf nicht negativ sein."),
                             feldname);

        /// <summary>Der neue Text, sonst der gelesene Wert (<c>NULL</c> bleibt <c>NULL</c>).</summary>
        private static object Uebernommen(string neu, DataRow satz, string spalte)
            => neu ?? Spaltenwert(satz, spalte);

        /// <summary>Die neue Zahl, sonst der gelesene Wert (<c>NULL</c> bleibt <c>NULL</c>).</summary>
        private static object Uebernommen(int? neu, DataRow satz, string spalte)
            => neu.HasValue ? (object)neu.Value : Spaltenwert(satz, spalte);

        /// <summary>Die neue Kommazahl, sonst der gelesene Wert (<c>NULL</c> bleibt <c>NULL</c>).</summary>
        private static object Uebernommen(double? neu, DataRow satz, string spalte)
            => neu.HasValue ? (object)neu.Value : Spaltenwert(satz, spalte);

        /// <summary>Der rohe Spaltenwert; fehlende Spalte und <c>null</c> ergeben <c>DBNull</c>.</summary>
        private static object Spaltenwert(DataRow satz, string spalte)
        {
            object v = satz.Table.Columns.Contains(spalte) ? satz[spalte] : DBNull.Value;
            return v ?? DBNull.Value;
        }

        /// <summary>Der Spaltenwert als Text; fehlende Spalte und <c>NULL</c> ergeben „".</summary>
        private static string Spaltentext(DataRow satz, string spalte)
        {
            object v = Spaltenwert(satz, spalte);
            return v == DBNull.Value ? "" : v.ToString();
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

        #region --- STAMM -> PROJEKT KOPIE ---

        /// <summary>
        /// Projekt-WP-ID (<c>Tab_WP.ID</c>) zu einem KATALOGSATZ im Projekt, oder 0 —
        /// der Weg über die Id statt über den Namen (Anwenderentscheid 16.09.2026,
        /// Schemaschritt 80).
        /// </summary>
        /// <remarks>
        /// <para><b>Warum es diesen Weg neben <see cref="GetProjektId"/> gibt.</b>
        /// <c>Tab_WP.Bezeichner</c> ist nicht eindeutig, und ein umbenannter Katalogsatz
        /// findet seine eigene Projektkopie über den Namen gar nicht mehr. Die Spalte
        /// <c>ID_Stamm</c> ist die Klammer, die eine Umbenennung überlebt.</para>
        ///
        /// <para><b><c>ID_Stamm</c> darf NULL sein</b> — von Hand angelegte Geräte, ein
        /// gelöschter Katalogsatz, ein beim Nachtrag mehrdeutiger Name. Wer hier 0
        /// bekommt, fragt deshalb weiter über den Namen; das ist keine Ausnahme, sondern
        /// der Regelfall des Altbestands.</para>
        /// </remarks>
        public int GetProjektIdZuStamm(int stammId, int idProjekt)
        {
            if (stammId <= 0 || idProjekt <= 0) return 0;

            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_WP WHERE " + WaermepumpeKatalogverweis.SPALTE +
                " = ? AND ID_Projekt = ? ORDER BY ID",
                new DbParam("@stamm", stammId),
                new DbParam("@proj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Projekt-WP-ID (Tab_WP.ID) zu einem Bezeichner im Projekt, oder 0.
        public int GetProjektId(string szBezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_WP WHERE Bezeichner = ? AND ID_Projekt = ?",
                new DbParam("@bez", szBezeichner ?? ""),
                new DbParam("@proj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Loescht einen Projekt-WP (per Bezeichner + Projekt) samt Kennlinien.
        public bool DeleteFromProjekt(string szBezeichner, int idProjekt)
        {
            int id = GetProjektId(szBezeichner, idProjekt);
            if (id > 0)
            {
                DataRepository.ExecuteSQL("DELETE FROM Tab_Kenndaten WHERE ID_WP = ?", new DbParam("@id", id));
                DataRepository.ExecuteSQL("DELETE FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = ?", new DbParam("@id", id));
            }
            return DataRepository.ExecuteSQL("DELETE FROM Tab_WP WHERE Bezeichner = ? AND ID_Projekt = ?",
                new DbParam("@bez", szBezeichner ?? ""), new DbParam("@proj", idProjekt));
        }

        // Komfort-Ueberladung: kopiert per Bezeichner aus den Stammdaten ins Projekt.
        public int CopyFromStamm(string szBezeichner, int idProjekt)
        {
            int stammId = DataRepository.GetIdByName(WPStammCtrl.TABLE, "Bezeichner", szBezeichner);
            if (stammId <= 0) return -1;
            return CopyFromStamm(stammId, idProjekt);
        }

        // Kopiert einen Stamm-WP (Tab_WP_STAMM) samt Kennlinien (Tab_Kenndaten_STAMM /
        // Tab_Kenndaten_Kuehlung_STAMM) in die Projekt-Tabellen, sofern fuer das Projekt noch nicht
        // vorhanden. Setzt ID_Projekt und remappt die Kennlinien auf die neue Projekt-WP-ID.
        // Rueckgabe: Projekt-WP-ID (Tab_WP.ID) des kopierten ODER bereits vorhandenen Satzes, -1 bei Fehler.
        public int CopyFromStamm(int stammId, int idProjekt)
        {
            try
            {
                DataTable head = DataRepository.GetDataTable(
                    "SELECT * FROM " + WPStammCtrl.TABLE + " WHERE ID = ?", new DbParam("@id", stammId));
                if (head == null || head.Rows.Count == 0) return -1;
                DataRow sHead = head.Rows[0];
                string bez = sHead["Bezeichner"].ToString();

                // Die vorhandene Kopie: ZUERST über den Katalogverweis (Schemaschritt
                // 80), erst danach über den Namen. Umgekehrt fände ein umbenannter
                // Katalogsatz seine eigene Kopie nicht und legte eine zweite an; der
                // Namensweg bleibt für jede Kopie ohne Verweis (ID_Stamm NULL).
                int vorhanden = GetProjektIdZuStamm(stammId, idProjekt);
                if (vorhanden <= 0) vorhanden = GetProjektId(bez, idProjekt);
                if (vorhanden > 0) return vorhanden;

                DataTable cw = DataRepository.GetDataTable(
                    "SELECT * FROM " + WPStammCtrl.CURVE   + " WHERE ID_WP = ? ORDER BY ID", new DbParam("@id", stammId));
                DataTable ck = DataRepository.GetDataTable(
                    "SELECT * FROM " + WPStammCtrl.CURVE_K + " WHERE ID_WP = ? ORDER BY ID", new DbParam("@id", stammId));

                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        int neueId;
                        {
                            object m = v.Skalar("SELECT Max(ID) FROM Tab_WP");
                            neueId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                        }

                        // ID_Stamm seit Schemaschritt 80: Die Kopie merkt sich, aus
                        // welchem Katalogsatz sie stammt - eine Umbenennung des Satzes
                        // zerreisst die Klammer dann nicht mehr.
                        string hsql = @"INSERT INTO Tab_WP
                        (ID, ID_Projekt, ID_Stamm, Bezeichner, Firma, Beschreibung, Typ, Baujahr, Aufstellung,
                         Nennleistung, maxPtherm, Heizung, Regelung, Modulkosten, Laenge, Breite, Hoehe,
                         Gewicht, Raum, Kuehlleistung, Bauart)
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                        {
                            List<DbParam> p = new List<DbParam>();
                            p.Add(new DbParam("@id", neueId));
                            p.Add(new DbParam("@proj", idProjekt));
                            p.Add(new DbParam("@stamm", stammId));
                            p.Add(P(sHead, "Bezeichner"));
                            p.Add(P(sHead, "Firma"));
                            p.Add(P(sHead, "Beschreibung"));
                            p.Add(P(sHead, "Typ"));
                            p.Add(P(sHead, "Baujahr"));
                            p.Add(P(sHead, "Aufstellung"));
                            p.Add(P(sHead, "Nennleistung"));
                            p.Add(P(sHead, "maxPtherm"));
                            p.Add(P(sHead, "Heizung"));
                            p.Add(P(sHead, "Regelung"));
                            p.Add(P(sHead, "Modulkosten"));
                            p.Add(P(sHead, "Laenge"));
                            p.Add(P(sHead, "Breite"));
                            p.Add(P(sHead, "Hoehe"));
                            p.Add(P(sHead, "Gewicht"));
                            p.Add(P(sHead, "Raum"));
                            p.Add(P(sHead, "Kuehlleistung"));
                            p.Add(P(sHead, "Bauart"));
                            v.Ausfuehren(hsql, p.ToArray());
                        }

                        // Kennlinien Waerme (ID explizit MAX+1, ID_WP = neue Projekt-WP-ID)
                        if (cw != null && cw.Rows.Count > 0)
                        {
                            int cid;
                            { object m = v.Skalar("SELECT Max(ID) FROM Tab_Kenndaten"); cid = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1; }
                            foreach (DataRow r in cw.Rows)
                            {
                                {
                                    List<DbParam> p = new List<DbParam>();
                                    p.Add(new DbParam("@id", cid++));
                                    p.Add(new DbParam("@pj", neueId));
                                    p.Add(new DbParam("@wp", neueId));
                                    p.Add(P(r, "Vorlauf"));
                                    p.Add(P(r, "Temperatur"));
                                    p.Add(P(r, "COP"));
                                    p.Add(P(r, "Ptherm"));
                                    v.Ausfuehren(SQL_KENNLINIE_EINFUEGEN, p.ToArray());
                                }
                            }
                        }

                        // Kennlinien Kuehlung
                        if (ck != null && ck.Rows.Count > 0)
                        {
                            int ckid;
                            { object m = v.Skalar("SELECT Max(ID) FROM Tab_Kenndaten_Kuehlung"); ckid = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1; }
                            foreach (DataRow r in ck.Rows)
                            {
                                {
                                    List<DbParam> p = new List<DbParam>();
                                    p.Add(new DbParam("@id", ckid++));
                                    p.Add(new DbParam("@wp", neueId));
                                    p.Add(P(r, "Vorlauf"));
                                    p.Add(P(r, "Temperatur"));
                                    p.Add(P(r, "COP"));
                                    p.Add(P(r, "Pkuehl"));
                                    p.Add(P(r, "Last"));
                                    v.Ausfuehren("INSERT INTO Tab_Kenndaten_Kuehlung (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) VALUES (?, ?, ?, ?, ?, ?, ?)", p.ToArray());
                                }
                            }
                        }

                        v.Commit();
                        return neueId;
                    }
                    catch (Exception ex)
                    {
                        try { v.Rollback(); } catch { }
                        Console.WriteLine("Fehler beim Kopieren des WP aus den Stammdaten: " + ex.Message);
                        return -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Kopieren des WP aus den Stammdaten: " + ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Holt die KENNLINIEN einer bereits vorhandenen Gerätekopie aus dem
        /// Stammkatalog nach — <b>Befund W7‑B‑3</b> der Windows-Abnahme V2 vom
        /// 07.09.2026, der Knopf „Kennlinien aus dem Katalog übernehmen".
        ///
        /// <para><b>Warum es diesen Weg neben <see cref="CopyFromStamm(int,int)"/>
        /// gibt.</b> Jene Methode legt die Gerätekopie SAMT Kennlinien an und kehrt bei
        /// einer bereits vorhandenen Kopie sofort mit deren Id zurück
        /// (<c>if (vorhanden &gt; 0) return vorhanden;</c>) — sie rührt die Kennlinien
        /// dann nicht mehr an. Ein Gerät, dessen Kopie ohne Kennlinien entstanden ist
        /// (Altbestand, eine abgebrochene Kopie, ein Katalogsatz, der seine
        /// Stützstellen erst später bekam), bleibt damit für immer ohne — und der Lauf
        /// bricht mit <c>SIMENG_WP_KEINE_KENNDATEN</c> ab.</para>
        ///
        /// <para><b>Was schon da ist, bleibt unangetastet.</b> Geschrieben wird nur in
        /// eine Tabelle, die für dieses Gerät KEINE Zeile führt; die vom Anwender
        /// gepflegten Stützstellen einer vorhandenen Kopie kann der Knopf damit nicht
        /// überschreiben. Wärme und Kühlung werden getrennt betrachtet — es gibt
        /// Geräte mit dem einen und ohne das andere.</para>
        /// </summary>
        /// <param name="projektWpId">Die Gerätekopie (<c>Tab_WP.ID</c>).</param>
        /// <returns>
        /// Die Zahl der geschriebenen Stützstellen (Wärme und Kühlung zusammen);
        /// 0, wenn es nichts zu holen gab (kein Katalogsatz gleichen Bezeichners oder
        /// bereits vollständig), −1 bei einem Fehler — dann ist nichts geschrieben.
        /// </returns>
        public int KennlinienAusKatalog(int projektWpId)
        {
            if (projektWpId <= 0) return -1;

            try
            {
                DataTable kopf = DataRepository.GetDataTable(
                    "SELECT Bezeichner, " + WaermepumpeKatalogverweis.SPALTE +
                    " FROM Tab_WP WHERE ID = ?", new DbParam("@id", projektWpId));
                if (kopf == null || kopf.Rows.Count == 0) return -1;

                string bez = kopf.Rows[0]["Bezeichner"] == DBNull.Value
                    ? "" : kopf.Rows[0]["Bezeichner"].ToString();

                // Der KATALOGVERWEIS zuerst (Schemaschritt 80), der Name als Rückfall.
                // Sonst holte der Knopf die Stützstellen eines gleichnamigen Fremdsatzes,
                // sobald jemand den eigenen Katalogsatz umbenannt hat.
                int stammId = Verweis(kopf.Rows[0]);
                if (stammId <= 0)
                    stammId = DataRepository.GetIdByName(WPStammCtrl.TABLE, "Bezeichner", bez);
                if (stammId <= 0) return 0;

                bool waermeFehlt = Zeilenzahl("SELECT COUNT(*) FROM Tab_Kenndaten WHERE ID_WP = ?", projektWpId) == 0;
                bool kuehlFehlt = Zeilenzahl("SELECT COUNT(*) FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = ?", projektWpId) == 0;
                if (!waermeFehlt && !kuehlFehlt) return 0;

                DataTable cw = waermeFehlt
                    ? DataRepository.GetDataTable(
                        "SELECT * FROM " + WPStammCtrl.CURVE + " WHERE ID_WP = ? ORDER BY ID",
                        new DbParam("@id", stammId))
                    : null;
                DataTable ck = kuehlFehlt
                    ? DataRepository.GetDataTable(
                        "SELECT * FROM " + WPStammCtrl.CURVE_K + " WHERE ID_WP = ? ORDER BY ID",
                        new DbParam("@id", stammId))
                    : null;

                if ((cw == null || cw.Rows.Count == 0) && (ck == null || ck.Rows.Count == 0)) return 0;

                int geschrieben = 0;
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        // Id-Vergabe wie in CopyFromStamm: EINMAL Max(ID) je Tabelle,
                        // danach hochzaehlen - innerhalb des Vorgangs gelesen.
                        if (cw != null && cw.Rows.Count > 0)
                        {
                            int cid;
                            { object m = v.Skalar("SELECT Max(ID) FROM Tab_Kenndaten"); cid = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1; }
                            foreach (DataRow r in cw.Rows)
                            {
                                List<DbParam> p = new List<DbParam>();
                                p.Add(new DbParam("@id", cid++));
                                p.Add(new DbParam("@pj", projektWpId));
                                p.Add(new DbParam("@wp", projektWpId));
                                p.Add(P(r, "Vorlauf"));
                                p.Add(P(r, "Temperatur"));
                                p.Add(P(r, "COP"));
                                p.Add(P(r, "Ptherm"));
                                v.Ausfuehren(SQL_KENNLINIE_EINFUEGEN, p.ToArray());
                                geschrieben++;
                            }
                        }

                        if (ck != null && ck.Rows.Count > 0)
                        {
                            int ckid;
                            { object m = v.Skalar("SELECT Max(ID) FROM Tab_Kenndaten_Kuehlung"); ckid = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1; }
                            foreach (DataRow r in ck.Rows)
                            {
                                List<DbParam> p = new List<DbParam>();
                                p.Add(new DbParam("@id", ckid++));
                                p.Add(new DbParam("@wp", projektWpId));
                                p.Add(P(r, "Vorlauf"));
                                p.Add(P(r, "Temperatur"));
                                p.Add(P(r, "COP"));
                                p.Add(P(r, "Pkuehl"));
                                p.Add(P(r, "Last"));
                                v.Ausfuehren("INSERT INTO Tab_Kenndaten_Kuehlung (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) VALUES (?, ?, ?, ?, ?, ?, ?)", p.ToArray());
                                geschrieben++;
                            }
                        }

                        v.Commit();
                        return geschrieben;
                    }
                    catch (Exception ex)
                    {
                        try { v.Rollback(); } catch { }
                        Console.WriteLine("Fehler beim Nachholen der WP-Kennlinien: " + ex.Message);
                        return -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Nachholen der WP-Kennlinien: " + ex.Message);
                return -1;
            }
        }

        /// <summary>Zeilenzahl einer Zaehlabfrage mit einer Geraete-Id.</summary>
        /// <summary>
        /// Der Katalogverweis einer Projektzeile (<c>Tab_WP.ID_Stamm</c>) als Zahl;
        /// fehlende Spalte, NULL und 0 ergeben gleichermaßen 0 — „an keinem
        /// Katalogsatz". Die Fallunterscheidung steht hier und nicht bei jedem
        /// Aufrufer.
        /// </summary>
        internal static int Verweis(DataRow satz)
        {
            if (satz == null) return 0;
            if (!satz.Table.Columns.Contains(WaermepumpeKatalogverweis.SPALTE)) return 0;

            object v = satz[WaermepumpeKatalogverweis.SPALTE];
            if (v == null || v == DBNull.Value) return 0;

            try { return Convert.ToInt32(v); }
            catch { return 0; }
        }

        private static int Zeilenzahl(string sql, int id)
        {
            object v = DataRepository.ExecuteScalar(sql, new DbParam("@id", id));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Parameter aus Spaltenwert (DBNull, falls Spalte fehlt).
        private static DbParam P(DataRow row, string col)
        {
            object v = row.Table.Columns.Contains(col) ? row[col] : DBNull.Value;
            return new DbParam("@" + col, v ?? DBNull.Value);
        }

        #endregion

        // Mappt die DataTable direkt in die dynamische Liste
        private void MapDataTableToItems(DataTable dt)
        {
            _internalList.Clear(); // Alte Einträge aus der Liste löschen

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                WPModel item = new WPModel();

                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.ID = Convert.ToInt32(row["ID"]);
                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value) item.ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);
                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) item.WPName = row["Bezeichner"].ToString();
                if (dt.Columns.Contains("Firma") && row["Firma"] != DBNull.Value) item.Firma = row["Firma"].ToString();
                if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) item.Beschreibung = row["Beschreibung"].ToString();
                if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value) item.Typ = row["Typ"].ToString();
                if (dt.Columns.Contains("Baujahr") && row["Baujahr"] != DBNull.Value) item.Baujahr = Convert.ToInt32(row["Baujahr"]);
                if (dt.Columns.Contains("Aufstellung") && row["Aufstellung"] != DBNull.Value) item.Aufstellung = row["Aufstellung"].ToString();
                if (dt.Columns.Contains("Nennleistung") && row["Nennleistung"] != DBNull.Value) item.Nennleistung = Convert.ToInt32(row["Nennleistung"]);
                if (dt.Columns.Contains("maxPTherm") && row["maxPTherm"] != DBNull.Value) item.maxPTherm = Convert.ToInt32(row["maxPTherm"]);
                if (dt.Columns.Contains("Heizung") && row["Heizung"] != DBNull.Value) item.Heizung = Convert.ToInt32(row["Heizung"]);
                if (dt.Columns.Contains("Regelung") && row["Regelung"] != DBNull.Value) item.Regelung = row["Regelung"].ToString();
                if (dt.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value) item.Modulkosten = Convert.ToInt32(row["Modulkosten"]);
                if (dt.Columns.Contains("Kuehlleistung") && row["Kuehlleistung"] != DBNull.Value) item.Kuehlleistung = Convert.ToDouble(row["Kuehlleistung"]);
                if (dt.Columns.Contains("Bauart") && row["Bauart"] != DBNull.Value) item.Bauart = row["Bauart"].ToString();

                // Für erweiterte Abfragen (ReadAll_MitMinMaxVorlauf)
                if (dt.Columns.Contains("Max") && row["Max"] != DBNull.Value) item.MaxVorlauf = Convert.ToInt32(row["Max"]);
                if (dt.Columns.Contains("Min") && row["Min"] != DBNull.Value) item.MinVorlauf = Convert.ToInt32(row["Min"]);

                _internalList.Add(item); // Dynamisch zur Liste hinzufügen
            }
        }
    }
}
