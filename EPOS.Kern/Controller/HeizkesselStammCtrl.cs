using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // Controller fuer die Stammdaten-Tabelle Tab_Heizkessel_STAMM.
    // Analog zu BHKWStammCtrl, aber fuer Heizkessel:
    //   - Tabelle = Tab_Heizkessel_STAMM (globaler Katalog)
    //   - DB-Spalte "Bezeichner" wird auf das Model-Feld Name abgebildet
    //   - liest/schreibt das Feld ReadOnly
    //   - Update() und Delete() verweigern die Aenderung schreibgeschuetzter Datensaetze
    //   - Insert() vergibt eine explizite ID (MAX+1) und setzt ReadOnly = false
    // Alle DB-Zugriffe laufen ueber DataRepository.
    public class HeizkesselStammCtrl : HeizkesselModel
    {
        public const string TABLE = "Tab_Heizkessel_STAMM";

        // --- Kompatibilitaets-Layer nach vereinbarter Schablone ---
        private List<HeizkesselModel> _internalList = new List<HeizkesselModel>();
        private bool _hasSingleData = false;

        public int rows => _internalList.Count > 0 ? _internalList.Count : (_hasSingleData ? 1 : 0);
        public List<HeizkesselModel> items => _internalList;

        // Zuletzt gelesener ReadOnly-Zustand (bei ReadSingle gesetzt)
        public bool m_bReadOnly = false;

        // Stammdaten-Listen (Dropdowns)
        public List<string> Brennstoffart = new List<string>();
        public List<string> Brennstoffart_Gruppe = new List<string>();

        public HeizkesselStammCtrl()
        {
            LoadMetaData();
        }

        private void LoadMetaData()
        {
            DataTable dtG = DataRepository.GetDataTable("SELECT Gruppe FROM Tab_BrennstoffKategorien ORDER BY ID");
            Brennstoffart_Gruppe.Clear();
            foreach (DataRow r in dtG.Rows) Brennstoffart_Gruppe.Add(r["Gruppe"].ToString());

            DataTable dtS = DataRepository.GetDataTable("SELECT Bezeichner FROM Tab_Brennstoff_Stamm ORDER BY ID");
            Brennstoffart.Clear();
            foreach (DataRow r in dtS.Rows) Brennstoffart.Add(r["Bezeichner"].ToString());
        }

        // --- SCHEMA-VORSORGE ---

        /// <summary>
        /// Stellt die Spalte <c>Wartungskosten_Einheit</c> in <c>Tab_Heizkessel</c> und
        /// <c>Tab_Heizkessel_STAMM</c> sicher (Migrationsschritt 15) — die tolerante
        /// Rückfallebene für den Fall, dass die Migration nie angestoßen wurde.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum es sie braucht.</b> <see cref="SchemaKatalog.Alle"/> ist ausdrücklich
        /// der Umfang der SIMULATIONS-Eingabespalten; die Wartungseinheit gehört zum
        /// Kostenmodul und steht deshalb nicht darin (Begründung dort). Ohne die Spalte
        /// scheitern aber <see cref="Insert"/>, <see cref="Update"/> und
        /// <c>HeizkesselCtrl.CopyFromStamm</c> sichtbar — genau das Fehlerbild, das
        /// <c>StromAufschlagCtrl.StelleSpaltenSicher</c> für Schritt 12 abfängt.
        /// </para>
        /// <para>
        /// <b>Ohne Dialog, Schema je Tabelle.</b> Beides übernommen aus der korrigierten
        /// Fassung von <c>StromAufschlagCtrl.StelleSpaltenSicher</c> (Commit 87483b4):
        /// Eine Vorsorge ist kein Bedienschritt und darf keine MessageBox zeigen, deshalb
        /// <see cref="StilleDb"/> statt <c>DataRepository.ExecuteSQL</c>;
        /// und das Schema wird je Tabelle gelesen, sonst greift die Existenzprüfung für
        /// die zweite Tabelle nie und das <c>ALTER TABLE</c> läuft bei jedem Aufruf erneut.
        /// Echte Fehler bleiben sichtbar — sie melden sich beim folgenden Schreibzugriff.
        /// </para>
        /// <para>
        /// ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht, Schemaprobe statt
        /// <c>GetOleDbSchemaTable</c> (S4c vorgezogen), SQLite-Spaltentypen statt
        /// Access-Typen (S4d vorgezogen).
        /// </para>
        /// </remarks>
        public static void StelleSpaltenSicher()
        {
            try
            {
                Dictionary<string, HashSet<string>> schemaJeTabelle =
                    new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (SchemaSpalte s in SchemaKatalog.Schritt15_KesselWartungseinheit)
                {
                    HashSet<string> vorhanden;
                    if (!schemaJeTabelle.TryGetValue(s.Tabelle, out vorhanden))
                    {
                        vorhanden = StilleDb.SpaltenNamen(s.Tabelle);
                        schemaJeTabelle[s.Tabelle] = vorhanden;
                    }

                    if (vorhanden == null) continue;          // Tabelle fehlt - nicht unsere Aufgabe
                    if (vorhanden.Contains(s.Name)) continue;

                    if (StilleDb.NonQuery(StilleDb.AlterTableAddColumn(
                            s.Tabelle, s.Name, s.TypDefinition)) < 0)
                    {
                        Protokoll(s.Tabelle + "." + s.Name + ": Spalte konnte nicht angelegt werden.");
                        continue;
                    }

                    // Frisch angelegte Spalte ist NULL. Dieselbe Vorbelegung wie
                    // Migrationsschritt 15b - sonst haetten die Bestandszeilen
                    // einen Betrag ohne Einheit.
                    if (StilleDb.NonQuery(
                            "UPDATE [" + s.Tabelle + "] SET [" + s.Name + "] = ? " +
                            "WHERE [" + s.Name + "] IS NULL OR [" + s.Name + "] = ''",
                            new DbParam("@e", DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR)) < 0)
                        Protokoll(s.Tabelle + "." + s.Name + ": Vorbelegung fehlgeschlagen.");
                }
            }
            catch (Exception ex)
            {
                Protokoll(ex.Message);
            }
        }

        /// <summary>Protokolliert einen Vorsorge-Fehlschlag, ohne den Anwender zu stören.</summary>
        private static void Protokoll(string meldung)
        {
            try { Console.WriteLine("HeizkesselStammCtrl.StelleSpaltenSicher: " + meldung); }
            catch { }
        }

        // --- READ ---

        public void ReadAll(string filter = "")
        {
            _internalList.Clear();
            _hasSingleData = false;

            string sql = "SELECT * FROM [" + TABLE + "]";
            if (!string.IsNullOrEmpty(filter)) sql += " WHERE " + filter;
            sql += " ORDER BY Bezeichner";

            DataTable dt = DataRepository.GetDataTable(sql);
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    _internalList.Add(MapRowToModel(row));
                }
            }
        }

        /// <summary>
        /// Laedt den Katalogsatz zum Bezeichner. Bei mehrfach vergebenem Bezeichner die
        /// Zeile mit der KLEINSTEN ID; wie viele es insgesamt waren, sagt
        /// <see cref="AnzahlMitBezeichner"/>.
        /// </summary>
        /// <remarks>
        /// Das <c>ORDER BY ID</c> ist der Grund, warum diese Methode ueberhaupt eine
        /// Zusage machen kann: Ohne Sortierung bestimmt die ACE-Engine die Reihenfolge,
        /// und zwei Lesewege auf dieselbe Tabelle koennen VERSCHIEDENE Zeilen liefern.
        /// Genau das war in <c>Form_Heizkessel_Bearbeiten.SetControls</c> angelegt, das
        /// die Anzeigefelder aus einem <c>RecordSet</c> und die Wartungskosten aus diesem
        /// Controller fuellte - bei einer Dublette konnte die Maske Werte aus ZWEI Zeilen
        /// mischen. Die kleinste ID ist dieselbe Wahl, die Migrationsschritt 17 fuer
        /// Anlagendubletten trifft: Sie ist die zuerst angelegte Zeile.
        /// </remarks>
        public void ReadSingle(string name)
        {
            _internalList.Clear();
            _hasSingleData = false;

            string sql = "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ? ORDER BY ID";
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@nam", name));

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                FillModelFromRow(this, row);
                _internalList.Add(MapRowToModel(row));
                _hasSingleData = true;
            }
        }

        /// <summary>Laedt den Katalogsatz ueber seinen Primaerschluessel.</summary>
        public void ReadById(int id)
        {
            _internalList.Clear();
            _hasSingleData = false;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE ID = ?",
                new DbParam("@id", id));

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                FillModelFromRow(this, row);
                _internalList.Add(MapRowToModel(row));
                _hasSingleData = true;
            }
        }

        /// <summary>
        /// Wie viele Katalogsaetze diesen Bezeichner tragen. 0, 1 oder - im Bestand
        /// leider vorhanden - mehr.
        /// </summary>
        /// <remarks>
        /// <c>Tab_Heizkessel_STAMM</c> hat auf <c>Bezeichner</c> keinen eindeutigen
        /// Index. Gemessen am 18.08.2026 auf einer Kopie der Produktivdatenbank:
        /// 21 Zeilen, davon 16 auf acht doppelt vergebene Bezeichner verteilt. Jeder
        /// Weg, der einen Kessel ueber den Namen adressiert, muss diesen Fall kennen.
        /// </remarks>
        public static int AnzahlMitBezeichner(string name)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@nam", name ?? ""));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        public bool Exists(string name)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@nam", name ?? ""));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        // ReadOnly-Pruefung (Instanz)
        public bool IsReadOnly(string name)
        {
            return IsReadOnlyStatic(name);
        }

        // ReadOnly-Pruefung (statisch, fuer die UI-Guards)
        public static bool IsReadOnlyStatic(string name)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@nam", name ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        /// <summary>
        /// ReadOnly-Pruefung fuer GENAU eine Zeile. Die Variante ueber den Bezeichner
        /// beantwortet bei einer Dublette die Frage nach der falschen Zeile - sie kann
        /// den Schutz also sowohl faelschlich melden als auch faelschlich uebergehen.
        /// </summary>
        public static bool IsReadOnlyById(int id)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE ID = ?",
                new DbParam("@id", id));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        // --- SAVE ---

        /// <summary>
        /// Anlegen oder Aendern - entschieden an der ID, nicht am Namen.
        /// </summary>
        /// <remarks>
        /// Frueher entschied <see cref="Exists"/>: Ein neuer Kessel, dessen Name schon im
        /// Katalog stand, landete damit im <see cref="Update"/>-Zweig und ueberschrieb den
        /// fremden Eintrag. Die ID sagt dagegen zweifelsfrei, ob dieses Modell aus der
        /// Datenbank stammt.
        /// </remarks>
        public bool Save()
        {
            return this.ID > 0 ? Update() : Insert();
        }

        /// <summary>
        /// <b>Der Schreibweg des Katalogimports</b> (iU9-W13.0e): Duplikatpruefung und
        /// Einfuegen in EINER Transaktion.
        ///
        /// <para><b>Warum es das hier gibt.</b> Bis Welle 13 stand dieser Weg in
        /// <c>Form_Heizkessel_einlesen</c> — ein 19-spaltiges INSERT mit
        /// <c>MAX(ID)+1</c> und 19 <c>DbParam</c> IM FORMULAR, der einzige
        /// Schreibweg der Welle, der nicht im Kern lag (Befund W13-B16). Der Rumpf
        /// ist woertlich uebernommen; nur die <c>MessageBox</c> ist zum
        /// Rueckgabewert geworden, damit der Aufrufer die Meldung waehlt.</para>
        ///
        /// <para>Die Spaltenliste bleibt die des Imports: <c>Wartungskosten_Einheit</c>,
        /// <c>Brennwert</c>, <c>Vorlauf</c> und <c>Ruecklauf</c> schreibt er NICHT
        /// (anders als <see cref="Insert"/>) — sie sind Anwenderfelder.</para>
        /// </summary>
        /// <param name="model">Die Importwerte; <c>Name</c> ist der Bezeichner.</param>
        /// <param name="nameOverride">Beim Umbenennen der vom Anwender vergebene Bezeichner.</param>
        public VdiUebernahmeErgebnis ImportUebernehmen(HeizkesselModel model, string nameOverride = null)
        {
            if (model == null) return VdiUebernahmeErgebnis.Fehler;

            try
            {
                string bezeichner = nameOverride ?? model.Name;

                using (DbVorgang v = DataRepository.Vorgang())
                {
                    // Zweite Verteidigungslinie hinter der Vorpruefung des
                    // Konfliktdialogs - sie prueft auch den Umbenennen-Namen.
                    object anzahl = v.Skalar(
                        "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ?",
                        new DbParam("?", bezeichner ?? ""));
                    if (Convert.ToInt32(anzahl) > 0)
                    {
                        v.Rollback();
                        return VdiUebernahmeErgebnis.Duplikat;
                    }

                    object mx = v.Skalar("SELECT MAX(ID) FROM [" + TABLE + "]");
                    int neueId = (mx == null || mx == DBNull.Value) ? 1 : Convert.ToInt32(mx) + 1;

                    string sql = @"INSERT INTO [" + TABLE + @"]
                            (ID, Bezeichner, Beschreibung, Firma, Ptherm, Brennstoff, Wirkungsgrad_Gas, Wirkungsgrad_Öl,
                             Investitionskosten, Raumbedarf, Wartungskosten, Nutzungsdauer, CO2, SO2, NOx, CO, Staub,
                             Betriebsbereitschaftverlust, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                    DbParam[] ps = {
                        new DbParam("@id", neueId),
                        new DbParam("@nam", (object)bezeichner ?? DBNull.Value),
                        new DbParam("@bes", (object)model.Beschreibung ?? DBNull.Value),
                        new DbParam("@fir", (object)model.Firma ?? DBNull.Value),
                        new DbParam("@pth", model.Ptherm),
                        new DbParam("@bre", model.Brennstoff),
                        new DbParam("@wgg", model.Wirkungsgrad_Gas),
                        new DbParam("@wgo", model.Wirkungsgrad_Oel),
                        new DbParam("@inv", model.Investitionskosten),
                        new DbParam("@rau", model.Raumbedarf),
                        new DbParam("@war", model.Wartungskosten),
                        new DbParam("@nut", model.Nutzungsdauer),
                        new DbParam("@co2", model.CO2),
                        new DbParam("@so2", model.SO2),
                        new DbParam("@nox", model.NOx),
                        new DbParam("@co", model.CO),
                        new DbParam("@sta", model.Staub),
                        new DbParam("@bbv", model.Betriebsbereitschaftverlust),
                        new DbParam("@ro", false)
                    };

                    v.Ausfuehren(sql, ps);
                    v.Commit();
                    this.ID = neueId;
                    return VdiUebernahmeErgebnis.Gespeichert;
                }
            }
            catch (Exception ex)
            {
                // DbVorgang.Dispose rollt beim Verlassen des using zurueck, wenn
                // kein Commit gesehen wurde - ein eigener Rollback ist unnoetig.
                Console.WriteLine("Fehler bei Heizkessel Übernehmen: " + ex.Message);
                return VdiUebernahmeErgebnis.Fehler;
            }
        }

        public bool Insert()
        {
            int neueId = DataRepository.GetMaxID(TABLE) + 1;

            string sql = @"INSERT INTO [" + TABLE + @"]
                            (ID, Bezeichner, Beschreibung, Firma, Ptherm, Brennstoff,
                             Wirkungsgrad_Gas, Wirkungsgrad_Öl, Investitionskosten, Raumbedarf,
                             Wartungskosten, Wartungskosten_Einheit, Nutzungsdauer, CO2, SO2, NOx, CO, Staub,
                             Betriebsbereitschaftverlust, Brennwert, Vorlauf, Ruecklauf, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            DbParam[] ps = {
                new DbParam("@id", neueId),
                new DbParam("@bez", this.Name ?? ""),
                new DbParam("@bes", this.Beschreibung ?? ""),
                new DbParam("@fir", this.Firma ?? ""),
                new DbParam("@pth", this.Ptherm),
                new DbParam("@bre", this.Brennstoff),
                new DbParam("@wgg", this.Wirkungsgrad_Gas),
                new DbParam("@wgo", this.Wirkungsgrad_Oel),
                new DbParam("@inv", this.Investitionskosten),
                new DbParam("@rau", this.Raumbedarf),
                new DbParam("@war", this.Wartungskosten),
                new DbParam("@wae", HeizkesselCtrl.Einheit(this.Wartungskosten_Einheit)),
                new DbParam("@nut", this.Nutzungsdauer),
                new DbParam("@co2", this.CO2),
                new DbParam("@so2", this.SO2),
                new DbParam("@nox", this.NOx),
                new DbParam("@co", this.CO),
                new DbParam("@sta", this.Staub),
                new DbParam("@bbv", this.Betriebsbereitschaftverlust),
                new DbParam("@brn", this.Brennwert),
                new DbParam("@vl", this.Vorlauf),
                new DbParam("@tl", this.Ruecklauf),
                new DbParam("@ro", false)
            };

            bool ok = DataRepository.ExecuteSQL(sql, ps);
            if (ok) this.ID = neueId;
            return ok;
        }

        /// <summary>
        /// Schreibt den Datensatz zurueck, adressiert ueber <see cref="HeizkesselModel.ID"/>.
        /// Voraussetzung: Das Modell wurde ueber <see cref="ReadSingle"/> oder
        /// <see cref="ReadById"/> geladen, traegt also eine ID.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum die ID statt des Bezeichners.</b> Bis zum 18.08.2026 endete diese
        /// Anweisung auf <c>WHERE Bezeichner = ?</c>. Auf <c>Bezeichner</c> liegt aber
        /// kein eindeutiger Index, und der Bestand fuehrt acht doppelt vergebene
        /// Bezeichner (16 von 21 Zeilen, gemessen 18.08.2026) - ein Speichervorgang
        /// aenderte damit stillschweigend ZWEI Katalogsaetze, von denen der Anwender nur
        /// einen gesehen hatte. Die ID ist der Primaerschluessel und trifft genau die
        /// Zeile, die der Dialog angezeigt hat. Das ist zugleich die Projektregel aus
        /// <c>CLAUDE.md</c>: "bei neuen Beziehungen IDs verwenden".
        /// </para>
        /// <para>
        /// <b>Der Bezeichner wandert in den SET-Teil.</b> Solange er der Filter war,
        /// konnte er sich nicht aendern: Ein im Dialog geaenderter Name stand im WHERE,
        /// traf keine Zeile - und <see cref="DataRepository.ExecuteSQL"/> meldet auch
        /// bei NULL betroffenen Zeilen Erfolg, der Dialog quittierte also "Datensatz
        /// gespeichert", ohne etwas geschrieben zu haben. Mit dem ID-Filter wirkt das
        /// Umbenennen; <see cref="BezeichnerBelegt"/> haelt davor die Eindeutigkeit
        /// wenigstens fuer neue Namen aufrecht, damit hier keine weitere Dublette
        /// entsteht.
        /// </para>
        /// </remarks>
        /// <remarks>
        /// <para>
        /// <b>Seit iU9-W6.1 zweigeteilt.</b> Die drei Ablehnungsgruende stehen in
        /// <see cref="UpdateMitGrund"/> und kommen dort als Text ZURUECK; diese Methode
        /// zeigt ihn wie bisher ueber <c>Meldung.*</c>. Grund: Der Katalogeditor ist
        /// seither eine Razor-Komponente und hat keine <c>MessageBox</c> - er braucht den
        /// Grund als Rueckgabe. Es bleibt bei genau EINER Regel, nur mit zwei Arten, sie
        /// zu erfahren.
        /// </para>
        /// </remarks>
        public bool Update()
        {
            (bool ok, string grund) = UpdateMitGrund();
            if (!ok && !string.IsNullOrEmpty(grund))
            {
                // Die Titelzeile haengt am Grund - wie im Bestand: fehlende ID war eine
                // Warnung, die beiden fachlichen Ablehnungen ein Hinweis.
                if (this.ID <= 0) Meldung.Warnung(grund, "Speichern nicht möglich");
                else if (IsReadOnlyById(this.ID)) Meldung.Hinweis(grund, "Schreibgeschützt");
                else Meldung.Hinweis(grund, "Name bereits vergeben");
            }
            return ok;
        }

        /// <summary>Die UPDATE-Anweisung selbst, ohne Pruefungen.</summary>
        private bool Schreiben()
        {
            string sql = @"UPDATE [" + TABLE + @"] SET
                            Bezeichner = ?, Beschreibung = ?, Firma = ?, Ptherm = ?, Brennstoff = ?,
                            Wirkungsgrad_Gas = ?, Wirkungsgrad_Öl = ?, Investitionskosten = ?,
                            Raumbedarf = ?, Wartungskosten = ?, Wartungskosten_Einheit = ?, Nutzungsdauer = ?,
                            CO2 = ?, SO2 = ?, NOx = ?, CO = ?, Staub = ?,
                            Betriebsbereitschaftverlust = ?, Brennwert = ?, Vorlauf=?, Ruecklauf=?
                          WHERE ID = ?";

            DbParam[] ps = {
                new DbParam("@bez", this.Name ?? ""),
                new DbParam("@bes", this.Beschreibung ?? ""),
                new DbParam("@fir", this.Firma ?? ""),
                new DbParam("@pth", this.Ptherm),
                new DbParam("@bre", this.Brennstoff),
                new DbParam("@wgg", this.Wirkungsgrad_Gas),
                new DbParam("@wgo", this.Wirkungsgrad_Oel),
                new DbParam("@inv", this.Investitionskosten),
                new DbParam("@rau", this.Raumbedarf),
                new DbParam("@war", this.Wartungskosten),
                new DbParam("@wae", HeizkesselCtrl.Einheit(this.Wartungskosten_Einheit)),
                new DbParam("@nut", this.Nutzungsdauer),
                new DbParam("@co2", this.CO2),
                new DbParam("@so2", this.SO2),
                new DbParam("@nox", this.NOx),
                new DbParam("@co", this.CO),
                new DbParam("@sta", this.Staub),
                new DbParam("@bbv", this.Betriebsbereitschaftverlust),
                new DbParam("@brn", this.Brennwert),
                new DbParam("@vl", this.Vorlauf),
                new DbParam("@rl", this.Ruecklauf),
                new DbParam("@id", this.ID)
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        /// <summary>
        /// Import-Ueberschreiben (Dublettenkonzept 4.2): aktualisiert GENAU die Felder,
        /// die der VDI-Import liefert, adressiert per ID. Vom Anwender gepflegte Felder
        /// (Bezeichner, Beschreibung, Investitionskosten, Wartungskosten(_Einheit),
        /// Nutzungsdauer, Brennwert, Vorlauf, Ruecklauf, ReadOnly) bleiben unangetastet -
        /// der Import befuellt sie nicht.
        /// </summary>
        /// <remarks>
        /// Bewusst OHNE ReadOnly-Sperre: Das Ueberschreiben eines ReadOnly-Satzes ist
        /// erlaubt und wird vorher im Konfliktdialog bestaetigt (Entscheidung 9.2 -
        /// erlauben mit Hinweis).
        /// </remarks>
        public bool UpdateImport(int id)
        {
            if (id <= 0) return false;

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Firma = ?, Ptherm = ?, Brennstoff = ?,
                            Wirkungsgrad_Gas = ?, Wirkungsgrad_Öl = ?, Raumbedarf = ?,
                            CO2 = ?, SO2 = ?, NOx = ?, CO = ?, Staub = ?,
                            Betriebsbereitschaftverlust = ?
                          WHERE ID = ?";

            DbParam[] ps = {
                new DbParam("@fir", this.Firma ?? ""),
                new DbParam("@pth", this.Ptherm),
                new DbParam("@bre", this.Brennstoff),
                new DbParam("@wgg", this.Wirkungsgrad_Gas),
                new DbParam("@wgo", this.Wirkungsgrad_Oel),
                new DbParam("@rau", this.Raumbedarf),
                new DbParam("@co2", this.CO2),
                new DbParam("@so2", this.SO2),
                new DbParam("@nox", this.NOx),
                new DbParam("@co", this.CO),
                new DbParam("@sta", this.Staub),
                new DbParam("@bbv", this.Betriebsbereitschaftverlust),
                new DbParam("@id", id)
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        /// <summary>
        /// Der Bezeichner, wie er aktuell IN DER DATENBANK unter dieser ID steht - die
        /// Vergleichsgrundlage dafuer, ob ein Speichervorgang eine Umbenennung ist.
        /// </summary>
        public static string GespeicherterBezeichner(int id)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM [" + TABLE + "] WHERE ID = ?",
                new DbParam("@id", id));
            return (v != null && v != DBNull.Value) ? Convert.ToString(v) : "";
        }

        /// <summary>
        /// Traegt ein ANDERER Katalogsatz als <paramref name="eigeneId"/> bereits diesen
        /// Bezeichner?
        /// </summary>
        public static bool BezeichnerBelegt(string name, int eigeneId)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ? AND ID <> ?",
                new DbParam("@nam", name ?? ""),
                new DbParam("@id", eigeneId));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        /// <summary>
        /// Loescht den Katalogsatz zum Bezeichner - aber nur, wenn er eindeutig ist.
        /// </summary>
        /// <remarks>
        /// Dieselbe Anweisung endete bis zum 18.08.2026 auf <c>WHERE Bezeichner = ?</c>
        /// und loeschte bei einem doppelt vergebenen Namen BEIDE Zeilen. Das ist die
        /// schaerfere Auspraegung desselben Fehlers wie in <see cref="Update"/>, nur
        /// unwiederbringlich. Weil die aufrufenden Listen (<c>Form_Heizkessel_Admin</c>,
        /// <c>Form_Heizkessel</c>) nur den Namen fuehren, laesst sich die gemeinte Zeile
        /// hier nicht bestimmen - deshalb wird der mehrdeutige Fall gemeldet und NICHT
        /// geloescht. Wer gezielt eine Zeile entfernen will, nimmt <see cref="Delete(int)"/>.
        /// </remarks>
        public bool Delete(string name)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID FROM [" + TABLE + "] WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@nam", name ?? ""));

            int anzahl = (dt != null) ? dt.Rows.Count : 0;

            if (anzahl == 0)
            {
                Meldung.Hinweis("Der Katalogeintrag \"" + (name ?? "") + "\" wurde nicht gefunden.",
                    "Nicht gefunden");
                return false;
            }

            if (anzahl > 1)
            {
                Meldung.Warnung("Der Name \"" + (name ?? "") + "\" ist im Katalog " + anzahl +
                    "-mal vergeben. Es ist deshalb nicht entscheidbar, welcher Eintrag gemeint ist - " +
                    "es wurde nichts gelöscht.",
                    "Name mehrdeutig");
                return false;
            }

            return Delete(Convert.ToInt32(dt.Rows[0]["ID"]));
        }

        /// <summary>Loescht GENAU den Katalogsatz mit dieser ID.</summary>
        public bool Delete(int id)
        {
            if (IsReadOnlyById(id))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt");
                return false;
            }

            string sql = "DELETE FROM [" + TABLE + "] WHERE ID = ?";
            return DataRepository.ExecuteSQL(sql, new DbParam("@id", id));
        }

        // --- MAPPING ---

        private void FillModelFromRow(HeizkesselModel target, DataRow row)
        {
            target.ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0;
            target.Name = row["Bezeichner"]?.ToString() ?? "";
            target.Firma = row["Firma"]?.ToString() ?? "";
            target.Beschreibung = row["Beschreibung"]?.ToString() ?? "";
            target.Ptherm = row["Ptherm"] != DBNull.Value ? Convert.ToDouble(row["Ptherm"]) : 0.0;
            target.Brennstoff = row["Brennstoff"] != DBNull.Value ? Convert.ToInt32(row["Brennstoff"]) : 0;
            target.Wirkungsgrad_Gas = row["Wirkungsgrad_Gas"] != DBNull.Value ? Convert.ToDouble(row["Wirkungsgrad_Gas"]) : 0.0;
            target.Wirkungsgrad_Oel = row["Wirkungsgrad_Öl"] != DBNull.Value ? Convert.ToDouble(row["Wirkungsgrad_Öl"]) : 0.0;
            target.Investitionskosten = row["Investitionskosten"] != DBNull.Value ? Convert.ToDouble(row["Investitionskosten"]) : 0.0;
            target.Raumbedarf = row["Raumbedarf"] != DBNull.Value ? Convert.ToDouble(row["Raumbedarf"]) : 0.0;
            target.Wartungskosten = row["Wartungskosten"] != DBNull.Value ? Convert.ToDouble(row["Wartungskosten"]) : 0.0;
            // Spaltenprüfung, weil eine nicht migrierte Datenbank die Spalte noch nicht führt.
            target.Wartungskosten_Einheit = HeizkesselCtrl.Einheit(
                row.Table.Columns.Contains(SchemaKatalog.SPALTE_KESSEL_WARTUNG_EINHEIT)
                    ? row[SchemaKatalog.SPALTE_KESSEL_WARTUNG_EINHEIT] as string : null);
            target.Nutzungsdauer = row["Nutzungsdauer"] != DBNull.Value ? Convert.ToDouble(row["Nutzungsdauer"]) : 0.0;
            target.CO2 = row["CO2"] != DBNull.Value ? Convert.ToDouble(row["CO2"]) : 0.0;
            target.SO2 = row["SO2"] != DBNull.Value ? Convert.ToDouble(row["SO2"]) : 0.0;
            target.NOx = row["NOx"] != DBNull.Value ? Convert.ToDouble(row["NOx"]) : 0.0;
            target.CO = row["CO"] != DBNull.Value ? Convert.ToDouble(row["CO"]) : 0.0;
            target.Staub = row["Staub"] != DBNull.Value ? Convert.ToDouble(row["Staub"]) : 0.0;
            target.Betriebsbereitschaftverlust = row["Betriebsbereitschaftverlust"] != DBNull.Value ? Convert.ToDouble(row["Betriebsbereitschaftverlust"]) : 0.0;
            target.Brennwert = row["Brennwert"] != DBNull.Value ? Convert.ToBoolean(row["Brennwert"]) : false;
            target.Vorlauf = row["Vorlauf"] != DBNull.Value ? Convert.ToInt32(row["Vorlauf"]) : 0;
            target.Ruecklauf = row["Ruecklauf"] != DBNull.Value ? Convert.ToInt32(row["Ruecklauf"]) : 0;

            if (ReferenceEquals(target, this))
            {
                this.m_bReadOnly = row.Table.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);
            }
        }

        private HeizkesselModel MapRowToModel(DataRow row)
        {
            HeizkesselModel m = new HeizkesselModel();
            FillModelFromRow(m, row);
            return m;
        }

        // =================================================================================
        // W6.0c - der KATALOGFILTER des Projektdialogs
        // =================================================================================

        /// <summary>
        /// Eine Zeile der Katalogliste: Primaerschluessel und Bezeichner.
        /// </summary>
        /// <remarks>
        /// Der Vorlaeufer <c>Form_Heizkessel.SetFilter</c> legte nur den Bezeichner in die
        /// <c>ListBox</c> und suchte die Id beim Hinzufuegen ueber
        /// <c>DataRepository.GetIdByName</c> nach. <c>Tab_Heizkessel_STAMM</c> hat auf
        /// <c>Bezeichner</c> aber keinen eindeutigen Index (siehe
        /// <see cref="AnzahlMitBezeichner"/>) - bei einer Dublette entschied die Reihenfolge
        /// der Engine, welcher Kessel aufgenommen wurde. Die Id kommt deshalb mit der Zeile.
        /// </remarks>
        public sealed record KatalogZeile(int Id, string Bezeichner);

        /// <summary>
        /// Die sechs Leistungsstufen des Filters, Index 0 = „Alle".
        /// </summary>
        /// <remarks>
        /// Die Praedikate sind zeichengleich aus <c>Form_Heizkessel.SetFilter</c>
        /// uebernommen. Was sich aendert, ist allein der STEUERWERT: Bis Welle 6 verglich
        /// die Maske den ANGEZEIGTEN Text der editierbaren <c>ComboBox</c> gegen deutsche
        /// Literale - mit lokalisierten Eintraegen haette das in keiner anderen Sprache
        /// mehr getroffen (dieselbe Fehlerklasse wie B0-10 im Pufferspeicher, dort in
        /// Paket 9 auf den Index umgestellt). Jetzt entscheidet der Index.
        /// </remarks>
        public static readonly string[] LEISTUNG_SQL =
        {
            "Ptherm Like '%'",
            "Ptherm <50",
            "Ptherm >=50 and Ptherm <200",
            "Ptherm >=200 and Ptherm <500",
            "Ptherm >=500 and Ptherm <1000",
            "Ptherm >=1000"
        };

        /// <summary>
        /// Die Katalogliste des Projektdialogs, eingeengt auf Brennstoffgruppe und
        /// Leistungsstufe.
        /// </summary>
        /// <param name="gruppe">
        /// Eintrag aus <see cref="Brennstoffart_Gruppe"/>. Leer, <c>null</c>, „Alle" und
        /// jeder unbekannte Wert heben die Einengung auf - Bestandsverhalten.
        /// </param>
        /// <param name="leistungsstufe">Index in <see cref="LEISTUNG_SQL"/>; alles
        /// ausserhalb gilt als 0 („Alle").</param>
        /// <remarks>
        /// <para>
        /// Die Gruppenkette ist WORTGLEICH aus <c>Form_Heizkessel.SetFilter</c> (Z. 665-676)
        /// uebernommen, samt ihrer beiden Ungenauigkeiten (Regel F3 - eine stille Reparatur
        /// waere eine Fachaenderung; Befund W6-O-1 des Protokolls):
        /// </para>
        /// <list type="bullet">
        /// <item>Die Kette kennt „Sonstige", <c>Tab_BrennstoffKategorien</c> fuehrt aber
        /// „Sonstige Energieträger" - der Eintrag trifft nie, und die Liste zeigt dann
        /// alle Brennstoffe der gewaehlten Leistungsstufe.</item>
        /// <item>„Sonstige" ist auf <c>Brennstoff=23</c> abgebildet; 23 ist im Katalog
        /// aber Fernwaerme. <c>Form_BHKWEing.BuildFilter</c> bildet dieselben drei Gruppen
        /// auf 23/24/25 ab und liegt damit richtig.</item>
        /// </list>
        /// <para>
        /// Ebenfalls Bestand: Die Gruppen „Fernwärme" und „Wasserstoff" stehen in der
        /// Kette gar nicht und heben die Einengung deshalb auf.
        /// </para>
        /// </remarks>
        public IReadOnlyList<KatalogZeile> Filtern(string gruppe, int leistungsstufe)
        {
            if (leistungsstufe < 0 || leistungsstufe >= LEISTUNG_SQL.Length) leistungsstufe = 0;
            string szFilterLeistung = LEISTUNG_SQL[leistungsstufe];

            string szFilter = "";
            string g = gruppe ?? "";
            if (g == "Gas") szFilter = "(Brennstoff >=1 and Brennstoff <=5) or Brennstoff=14";
            else if (g == "Öl") szFilter = "(Brennstoff >=6 and Brennstoff <=9) or (Brennstoff >=18 and Brennstoff <=22)";
            else if (g == "Koks") szFilter = "Brennstoff=10";
            else if (g == "Kohle") szFilter = "Brennstoff=11";
            else if (g == "Holz") szFilter = "Brennstoff=12";
            else if (g == "Tierische Fette") szFilter = "Brennstoff=17";
            else if (g == "Strom") szFilter = "Brennstoff=13";
            else if (g == "Pellets") szFilter = "Brennstoff=15";
            else if (g == "Rapsöl") szFilter = "Brennstoff=16";
            else if (g == "Sonstige") szFilter = "Brennstoff=23";
            else if (g == "Alle") szFilter = "Brennstoff Like '%'";

            string sql = szFilter == ""
                ? "SELECT ID, Bezeichner FROM [" + TABLE + "] WHERE " + szFilterLeistung + " ORDER BY Bezeichner"
                : "SELECT ID, Bezeichner FROM [" + TABLE + "] WHERE " + szFilter + " and " + szFilterLeistung + " ORDER BY Bezeichner";

            var liste = new List<KatalogZeile>();
            DataTable dt = DataRepository.GetDataTable(sql);
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
            {
                if (row["ID"] == null || row["ID"] == DBNull.Value) continue;
                liste.Add(new KatalogZeile(Convert.ToInt32(row["ID"]),
                                           row["Bezeichner"] == DBNull.Value ? "" : row["Bezeichner"].ToString()));
            }
            return liste;
        }

        /// <summary>
        /// Der Primaerschluessel zum Bezeichner, 0 wenn es keinen gibt.
        /// </summary>
        /// <remarks>
        /// Ersetzt <c>DataRepository.GetIdByName(TABLE, "Bezeichner", name)</c> in den
        /// Aufrufern. <c>ORDER BY ID</c> macht die Wahl bei einer Dublette benennbar - es
        /// ist die zuerst angelegte Zeile, dieselbe Wahl wie in <see cref="ReadSingle"/>.
        /// </remarks>
        public static int IdZu(string name)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM [" + TABLE + "] WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@nam", name ?? ""));
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        // =================================================================================
        // W6.1 - der EINE Schreibeinstieg des Katalogeditors
        // =================================================================================

        /// <summary>
        /// Was ein Speicherversuch des Katalogeditors ergeben hat.
        /// </summary>
        /// <param name="Ok">Wurde geschrieben?</param>
        /// <param name="Meldung">
        /// Der Grund im Klartext - bei Erfolg die Bestaetigung, sonst die Ablehnung.
        /// Bereits lokalisiert; die Oberflaeche zeigt ihn als Banner.
        /// </param>
        /// <param name="Name">
        /// Der Bezeichner, unter dem der Satz jetzt steht. Nach einer Umbenennung ist das
        /// der NEUE Name - der Aufrufer waehlt damit die Zeile in seiner Liste wieder aus.
        /// </param>
        public sealed record SpeicherErgebnis(bool Ok, string Meldung, string Name);

        /// <summary>
        /// Schreibt den geladenen Katalogsatz zurueck - der Weg des Knopfes
        /// „Überschreiben".
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Zusammengefasst aus zwei Orten.</b> Bis iU9-W6.1 stand die Dublettenbremse
        /// (<c>SELECT COUNT(*) … WHERE Bezeichner = ?</c>) in
        /// <c>Form_Heizkessel_Bearbeiten.btn_Ueberschreiben_Click</c> und die drei
        /// Ablehnungsgruende in <see cref="Update"/>, das sie ueber <c>Meldung.*</c>
        /// selbst zeigte. Eine Razor-Komponente hat keine <c>MessageBox</c>: Der Grund
        /// muss zurueckkommen, nicht erscheinen.
        /// </para>
        /// <para>
        /// <b>Die Bremse bleibt.</b> <c>Tab_Heizkessel_STAMM</c> fuehrt auf
        /// <c>Bezeichner</c> keinen eindeutigen Schluessel. Zwar adressiert
        /// <see cref="Update"/> seit dem 18.08.2026 ueber die ID und traefe die
        /// Dublette gar nicht mehr - die Meldung bleibt trotzdem, weil sonst
        /// unerklaerlich bliebe, warum derselbe Name in der Auswahlliste mehrfach steht.
        /// </para>
        /// </remarks>
        public static SpeicherErgebnis Ueberschreiben(HeizkesselModel daten)
        {
            if (daten == null)
                return new SpeicherErgebnis(false, Text("HZKK_MSG_FEHLER",
                    "Fehler beim Überschreiben des Datensatzes!"), "");

            try
            {
                // Tab_Heizkessel_STAMM fuehrt keinen eindeutigen Schluessel auf Bezeichner.
                // Bei einer Dublette abbrechen, statt unbemerkt zu arbeiten, wo der
                // Anwender nur einen von zwei Saetzen gesehen hat (gleiche Bremse wie in
                // Form_Heizkessel_Admin).
                object anz = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ?",
                    new DbParam("@nam", GespeicherterBezeichner(daten.ID)));
                int nAnzahl = (anz == null || anz == DBNull.Value) ? 0 : Convert.ToInt32(anz);
                if (nAnzahl > 1)
                    return new SpeicherErgebnis(false,
                        string.Format(MyResource.Resource.ADM_MEHRDEUTIG_TEXT,
                                      GespeicherterBezeichner(daten.ID), nAnzahl), "");

                var ctrl = new HeizkesselStammCtrl();
                ctrl.Uebernehmen(daten);

                (bool ok, string grund) = ctrl.UpdateMitGrund();
                if (!ok) return new SpeicherErgebnis(false, grund, "");

                return new SpeicherErgebnis(true,
                    Text("HZKK_MSG_GESPEICHERT", "Datensatz gespeichert"), ctrl.Name);
            }
            catch
            {
                return new SpeicherErgebnis(false, Text("HZKK_MSG_FEHLER",
                    "Fehler beim Überschreiben des Datensatzes!"), "");
            }
        }

        /// <summary>
        /// Legt einen neuen Katalogsatz an - der Weg der Knoepfe „Speichern" (Modus NEU)
        /// und „Speichern unter".
        /// </summary>
        /// <remarks>
        /// Fasst <c>Form_Heizkessel_Bearbeiten.Insert</c> zusammen: erst
        /// <see cref="Exists"/>, dann <see cref="Insert"/>. Der Vorlaeufer meldete beide
        /// Ausgaenge ununterscheidbar als „Name existiert bereits oder Datenbankfehler!";
        /// hier sagt der Grund, welcher der beiden es war.
        /// </remarks>
        public static SpeicherErgebnis Anlegen(HeizkesselModel daten, string name)
        {
            if (daten == null || string.IsNullOrWhiteSpace(name))
                return new SpeicherErgebnis(false, Text("HZKK_MSG_NAME_FEHLT",
                    "Bitte einen gültigen Namen eingeben!"), "");

            try
            {
                var ctrl = new HeizkesselStammCtrl();
                ctrl.Uebernehmen(daten);
                ctrl.Name = name.Trim();
                ctrl.ID = 0;                       // Insert vergibt die Id selbst

                if (ctrl.Exists(ctrl.Name))
                    return new SpeicherErgebnis(false, Text("HZKK_MSG_NAME_BELEGT",
                        "Name existiert bereits!"), "");

                if (!ctrl.Insert())
                    return new SpeicherErgebnis(false, Text("HZKK_MSG_FEHLER_ANLEGEN",
                        "Fehler beim Speichern des Datensatzes!"), "");

                return new SpeicherErgebnis(true,
                    Text("HZKK_MSG_ANGELEGT", "Datensatz erfolgreich neu angelegt."), ctrl.Name);
            }
            catch
            {
                return new SpeicherErgebnis(false, Text("HZKK_MSG_FEHLER_ANLEGEN",
                    "Fehler beim Speichern des Datensatzes!"), "");
            }
        }

        /// <summary>
        /// Die drei Ablehnungsgruende von <see cref="Update"/> als RUECKGABE statt als
        /// Meldung. <see cref="Update"/> ruft die Methode und zeigt den Grund selbst -
        /// so gibt es weiterhin genau eine Regel, aber zwei Arten, sie zu erfahren.
        /// </summary>
        internal (bool Ok, string Grund) UpdateMitGrund()
        {
            if (this.ID <= 0)
                return (false, "Der Datensatz kann nicht gespeichert werden, weil er ohne Datenbank-ID " +
                               "geladen wurde. Bitte den Kessel erneut aus der Liste auswählen.");

            // ReadOnly-Schutz: schreibgeschuetzte Stammdatensaetze duerfen nicht geaendert werden.
            if (IsReadOnlyById(this.ID))
                return (false, "Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.");

            // Umbenennen darf keinen bereits vergebenen Namen treffen - sonst legte
            // ausgerechnet die Korrektur eine neue Dublette an. Die Pruefung greift NUR
            // bei einer echten Umbenennung; ohne diese Einschraenkung liesse sich eine der
            // 16 Bestandszeilen mit doppeltem Bezeichner gar nicht mehr speichern.
            if (!string.Equals(GespeicherterBezeichner(this.ID), this.Name ?? "", StringComparison.Ordinal)
                && BezeichnerBelegt(this.Name, this.ID))
                return (false, "Ein anderer Katalogeintrag trägt bereits den Namen \"" + (this.Name ?? "") +
                               "\". Bitte einen eindeutigen Namen vergeben.");

            return (Schreiben(), "");
        }

        /// <summary>Uebernimmt die 21 Felder eines Modells in diesen Controller.</summary>
        private void Uebernehmen(HeizkesselModel m)
        {
            this.ID = m.ID;
            this.Name = m.Name;
            this.Beschreibung = m.Beschreibung;
            this.Firma = m.Firma;
            this.Ptherm = m.Ptherm;
            this.Brennstoff = m.Brennstoff;
            this.Wirkungsgrad_Gas = m.Wirkungsgrad_Gas;
            this.Wirkungsgrad_Oel = m.Wirkungsgrad_Oel;
            this.Investitionskosten = m.Investitionskosten;
            this.Raumbedarf = m.Raumbedarf;
            this.Wartungskosten = m.Wartungskosten;
            this.Wartungskosten_Einheit = m.Wartungskosten_Einheit;
            this.Nutzungsdauer = m.Nutzungsdauer;
            this.CO2 = m.CO2;
            this.SO2 = m.SO2;
            this.NOx = m.NOx;
            this.CO = m.CO;
            this.Staub = m.Staub;
            this.Betriebsbereitschaftverlust = m.Betriebsbereitschaftverlust;
            this.Brennwert = m.Brennwert;
            this.Vorlauf = m.Vorlauf;
            this.Ruecklauf = m.Ruecklauf;
        }

        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        /// <summary>
        /// Die BRENNSTOFFARTEN, die die Heizkessel EINES Projekts fuehren
        /// (iU9-W11a.2; SQL woertlich aus <c>Form_Simulation_Detail
        /// .KesselBrennstoffartenLesen</c>, Z. 1194-1221).
        ///
        /// <para><b>Wozu.</b> Die Heizkessel-Ergebnisseite blendet eine der zehn
        /// Brennstoffzeilen ein, wenn ihr Jahreswert &gt; 0 ist ODER ein Kessel des
        /// Projekts diesen Brennstoff fuehrt. Der zweite Teil ist diese Abfrage — die
        /// einzige echte Fachabfrage jener Maske und damit ein Kern-Kandidat.</para>
        ///
        /// <para><b>Der Verbund laeuft ueber den Bezeichner</b>, nicht ueber eine Id:
        /// <c>Tab_Heizkessel.Bezeichner = Tab_Energieanlagen.Bezeichner</c>. Das ist die
        /// Textverknuepfung des Altschemas (Wurzel-CLAUDE.md, „Namenskonventionen");
        /// der Wortlaut bleibt unveraendert, damit die Anzeige dieselbe bleibt.</para>
        ///
        /// <para>Dialogfrei ueber <see cref="StilleDb"/> wie
        /// <see cref="ErgebnisPraesenz"/>: Schlaegt die Abfrage fehl, bleibt die Menge
        /// leer, und der Aufrufer faellt auf die vollstaendige Anzeige zurueck.</para>
        /// </summary>
        public static HashSet<int> BrennstoffartenJeProjekt(int idProjekt)
        {
            HashSet<int> arten = new HashSet<int>();
            if (idProjekt <= 0) return arten;

            DataTable dt = StilleDb.Tabelle(
                "SELECT DISTINCT k.Brennstoff FROM Tab_Heizkessel AS k " +
                "INNER JOIN Tab_Energieanlagen AS a ON k.Bezeichner = a.Bezeichner " +
                "WHERE k.ID_Projekt = ? AND a.ID_Projekt = ? AND a.ID_Type = ?",
                StilleDb.Par("@proj1", DbParamTyp.Integer, idProjekt),
                StilleDb.Par("@proj2", DbParamTyp.Integer, idProjekt),
                StilleDb.Par("@typ", DbParamTyp.Integer, WizardItemClass.KESSEL_TYP));

            if (dt == null) return arten;

            foreach (DataRow r in dt.Rows)
            {
                int a = StilleDb.Zahl(StilleDb.Feld(r, "Brennstoff"), -1);
                if (a >= 0) arten.Add(a);
            }
            return arten;
        }
    }
}
