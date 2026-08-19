using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

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
        /// eigene <see cref="OleDbConnection"/> statt <c>DataRepository.ExecuteSQL</c>;
        /// und das Schema wird je Tabelle gelesen, sonst greift die Existenzprüfung für
        /// die zweite Tabelle nie und das <c>ALTER TABLE</c> läuft bei jedem Aufruf erneut.
        /// Echte Fehler bleiben sichtbar — sie melden sich beim folgenden Schreibzugriff.
        /// </para>
        /// </remarks>
        public static void StelleSpaltenSicher()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    Dictionary<string, HashSet<string>> schemaJeTabelle =
                        new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                    foreach (SchemaSpalte s in SchemaKatalog.Schritt15_KesselWartungseinheit)
                    {
                        HashSet<string> vorhanden;
                        if (!schemaJeTabelle.TryGetValue(s.Tabelle, out vorhanden))
                        {
                            vorhanden = SpaltenNamen(conn, s.Tabelle);
                            schemaJeTabelle[s.Tabelle] = vorhanden;
                        }

                        if (vorhanden == null) continue;          // Tabelle fehlt - nicht unsere Aufgabe
                        if (vorhanden.Contains(s.Name)) continue;

                        try
                        {
                            using (OleDbCommand cmd = new OleDbCommand(
                                "ALTER TABLE [" + s.Tabelle + "] ADD COLUMN [" + s.Name + "] " +
                                s.TypDefinition, conn))
                                cmd.ExecuteNonQuery();

                            // Frisch angelegte Spalte ist NULL. Dieselbe Vorbelegung wie
                            // Migrationsschritt 15b - sonst haetten die Bestandszeilen
                            // einen Betrag ohne Einheit.
                            using (OleDbCommand cmd = new OleDbCommand(
                                "UPDATE [" + s.Tabelle + "] SET [" + s.Name + "] = ? " +
                                "WHERE [" + s.Name + "] IS NULL OR [" + s.Name + "] = ''", conn))
                            {
                                cmd.Parameters.Add(new OleDbParameter("@e", DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR));
                                cmd.ExecuteNonQuery();
                            }
                        }
                        catch (Exception ex)
                        {
                            Protokoll(s.Tabelle + "." + s.Name + ": " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Protokoll(ex.Message);
            }
        }

        /// <summary>
        /// Die Spaltennamen einer Tabelle, oder <c>null</c>, wenn es die Tabelle nicht
        /// gibt bzw. das Schema nicht lesbar ist.
        /// </summary>
        private static HashSet<string> SpaltenNamen(OleDbConnection conn, string tabelle)
        {
            try
            {
                DataTable cols = conn.GetOleDbSchemaTable(
                    OleDbSchemaGuid.Columns, new object[] { null, null, tabelle, null });

                if (cols == null || cols.Rows.Count == 0) return null;

                HashSet<string> namen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (DataRow r in cols.Rows) namen.Add(Convert.ToString(r["COLUMN_NAME"]));
                return namen;
            }
            catch { return null; }
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
            DataTable dt = DataRepository.GetDataTable(sql, new OleDbParameter("@nam", name));

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
                new OleDbParameter("@id", id));

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
                new OleDbParameter("@nam", name ?? ""));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        public bool Exists(string name)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new OleDbParameter("@nam", name ?? ""));
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
                new OleDbParameter("@nam", name ?? ""));
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
                new OleDbParameter("@id", id));
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

            OleDbParameter[] ps = {
                new OleDbParameter("@id", neueId),
                new OleDbParameter("@bez", this.Name ?? ""),
                new OleDbParameter("@bes", this.Beschreibung ?? ""),
                new OleDbParameter("@fir", this.Firma ?? ""),
                new OleDbParameter("@pth", this.Ptherm),
                new OleDbParameter("@bre", this.Brennstoff),
                new OleDbParameter("@wgg", this.Wirkungsgrad_Gas),
                new OleDbParameter("@wgo", this.Wirkungsgrad_Oel),
                new OleDbParameter("@inv", this.Investitionskosten),
                new OleDbParameter("@rau", this.Raumbedarf),
                new OleDbParameter("@war", this.Wartungskosten),
                new OleDbParameter("@wae", HeizkesselCtrl.Einheit(this.Wartungskosten_Einheit)),
                new OleDbParameter("@nut", this.Nutzungsdauer),
                new OleDbParameter("@co2", this.CO2),
                new OleDbParameter("@so2", this.SO2),
                new OleDbParameter("@nox", this.NOx),
                new OleDbParameter("@co", this.CO),
                new OleDbParameter("@sta", this.Staub),
                new OleDbParameter("@bbv", this.Betriebsbereitschaftverlust),
                new OleDbParameter("@brn", this.Brennwert),
                new OleDbParameter("@vl", this.Vorlauf),
                new OleDbParameter("@tl", this.Ruecklauf),
                new OleDbParameter("@ro", false)
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
                MessageBox.Show("Der Datensatz kann nicht gespeichert werden, weil er ohne Datenbank-ID " +
                    "geladen wurde. Bitte den Kessel erneut aus der Liste auswählen.",
                    "Speichern nicht möglich", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // ReadOnly-Schutz: schreibgeschuetzte Stammdatensaetze duerfen nicht geaendert werden.
            if (IsReadOnlyById(this.ID))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show("Ein anderer Katalogeintrag trägt bereits den Namen \"" + (this.Name ?? "") +
                    "\". Bitte einen eindeutigen Namen vergeben.",
                    "Name bereits vergeben", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Bezeichner = ?, Beschreibung = ?, Firma = ?, Ptherm = ?, Brennstoff = ?,
                            Wirkungsgrad_Gas = ?, Wirkungsgrad_Öl = ?, Investitionskosten = ?,
                            Raumbedarf = ?, Wartungskosten = ?, Wartungskosten_Einheit = ?, Nutzungsdauer = ?,
                            CO2 = ?, SO2 = ?, NOx = ?, CO = ?, Staub = ?,
                            Betriebsbereitschaftverlust = ?, Brennwert = ?, Vorlauf=?, Ruecklauf=?
                          WHERE ID = ?";

            OleDbParameter[] ps = {
                new OleDbParameter("@bez", this.Name ?? ""),
                new OleDbParameter("@bes", this.Beschreibung ?? ""),
                new OleDbParameter("@fir", this.Firma ?? ""),
                new OleDbParameter("@pth", this.Ptherm),
                new OleDbParameter("@bre", this.Brennstoff),
                new OleDbParameter("@wgg", this.Wirkungsgrad_Gas),
                new OleDbParameter("@wgo", this.Wirkungsgrad_Oel),
                new OleDbParameter("@inv", this.Investitionskosten),
                new OleDbParameter("@rau", this.Raumbedarf),
                new OleDbParameter("@war", this.Wartungskosten),
                new OleDbParameter("@wae", HeizkesselCtrl.Einheit(this.Wartungskosten_Einheit)),
                new OleDbParameter("@nut", this.Nutzungsdauer),
                new OleDbParameter("@co2", this.CO2),
                new OleDbParameter("@so2", this.SO2),
                new OleDbParameter("@nox", this.NOx),
                new OleDbParameter("@co", this.CO),
                new OleDbParameter("@sta", this.Staub),
                new OleDbParameter("@bbv", this.Betriebsbereitschaftverlust),
                new OleDbParameter("@brn", this.Brennwert),
                new OleDbParameter("@vl", this.Vorlauf),
                new OleDbParameter("@rl", this.Ruecklauf),
                new OleDbParameter("@id", this.ID)
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
                new OleDbParameter("@id", id));
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
                new OleDbParameter("@nam", name ?? ""),
                new OleDbParameter("@id", eigeneId));
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
                new OleDbParameter("@nam", name ?? ""));

            int anzahl = (dt != null) ? dt.Rows.Count : 0;

            if (anzahl == 0)
            {
                MessageBox.Show("Der Katalogeintrag \"" + (name ?? "") + "\" wurde nicht gefunden.",
                    "Nicht gefunden", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (anzahl > 1)
            {
                MessageBox.Show("Der Name \"" + (name ?? "") + "\" ist im Katalog " + anzahl +
                    "-mal vergeben. Es ist deshalb nicht entscheidbar, welcher Eintrag gemeint ist - " +
                    "es wurde nichts gelöscht.",
                    "Name mehrdeutig", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return Delete(Convert.ToInt32(dt.Rows[0]["ID"]));
        }

        /// <summary>Loescht GENAU den Katalogsatz mit dieser ID.</summary>
        public bool Delete(int id)
        {
            if (IsReadOnlyById(id))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string sql = "DELETE FROM [" + TABLE + "] WHERE ID = ?";
            return DataRepository.ExecuteSQL(sql, new OleDbParameter("@id", id));
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
