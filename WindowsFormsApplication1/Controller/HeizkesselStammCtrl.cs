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
        public bool Update()
        {
            if (this.ID <= 0)
            {
                Meldung.Warnung("Der Datensatz kann nicht gespeichert werden, weil er ohne Datenbank-ID " +
                    "geladen wurde. Bitte den Kessel erneut aus der Liste auswählen.",
                    "Speichern nicht möglich");
                return false;
            }

            // ReadOnly-Schutz: schreibgeschuetzte Stammdatensaetze duerfen nicht geaendert werden.
            if (IsReadOnlyById(this.ID))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt");
                return false;
            }

            // Umbenennen darf keinen bereits vergebenen Namen treffen - sonst legte
            // ausgerechnet die Korrektur eine neue Dublette an.
            //
            // Die Pruefung greift NUR bei einer echten Umbenennung. Ohne diese
            // Einschraenkung sperrte sie genau den Fall aus, um den es hier geht: Eine
            // der 16 Bestandszeilen mit doppeltem Bezeichner liesse sich nicht mehr
            // speichern, weil der eigene, unveraenderte Name ja bereits einer anderen
            // Zeile gehoert. Bearbeiten muss moeglich bleiben - verboten ist nur, eine
            // WEITERE Dublette zu erzeugen.
            if (!string.Equals(GespeicherterBezeichner(this.ID), this.Name ?? "", StringComparison.Ordinal)
                && BezeichnerBelegt(this.Name, this.ID))
            {
                Meldung.Hinweis("Ein anderer Katalogeintrag trägt bereits den Namen \"" + (this.Name ?? "") +
                    "\". Bitte einen eindeutigen Namen vergeben.",
                    "Name bereits vergeben");
                return false;
            }

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
    }
}
