using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // Controller fuer die Stammdaten-Tabelle Tab_PV_STAMM (globaler PV-Modul-Katalog).
    // Analog zu HeizkesselStammCtrl / PufferSpStammCtrl:
    //   - Tabelle = Tab_PV_STAMM
    //   - DB-Spalte Bezeichner wird auf m_szName abgebildet, gamma_PMP auf m_Temp_Coeff_Pmax
    //   - liest/schreibt das Feld ReadOnly
    //   - Insert() vergibt eine explizite ID (MAX+1) und setzt ReadOnly = false
    //   - Update()/Delete() verweigern schreibgeschuetzte Datensaetze
    // Alle DB-Zugriffe laufen ueber DataRepository.
    public class PhotovoltaikStammCtrl : PhotovoltaikModel
    {
        public const string TABLE = "Tab_PV_STAMM";

        private List<PhotovoltaikModel> _internalList = new List<PhotovoltaikModel>();
        public int rows => _internalList.Count;
        public new List<PhotovoltaikModel> items => _internalList;

        public bool m_bReadOnly = false;

        public void ReadAll(string filter = "")
        {
            string sql = "SELECT * FROM [" + TABLE + "]";
            if (!string.IsNullOrEmpty(filter)) sql += " WHERE " + filter;
            sql += " ORDER BY Bezeichner";

            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();
            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                _internalList.Add(MapRowToModel(row));
            }
        }

        /// <summary>
        /// Laedt den Katalogsatz zum Bezeichner. Bei mehrfach vergebenem Bezeichner die
        /// Zeile mit der KLEINSTEN ID - dieselbe Zusage wie
        /// <c>HeizkesselStammCtrl.ReadSingle</c>, und aus demselben Grund: Ohne
        /// <c>ORDER BY</c> bestimmt die ACE-Engine die Reihenfolge, zwei Lesewege koennen
        /// dann verschiedene Zeilen liefern.
        /// </summary>
        public void ReadSingle(string szName)
        {
            _internalList.Clear();
            string sql = "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ? ORDER BY ID";
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@bez", szName ?? (object)DBNull.Value));

            if (dt != null && dt.Rows.Count > 0)
            {
                FillFromRow(this, dt.Rows[0]);
                this.m_bReadOnly = ReadOnlyOf(dt.Rows[0]);
                _internalList.Add(this);
            }
        }

        public bool Exists(string szName)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@bez", szName ?? ""));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        public static bool IsReadOnlyStatic(string szName)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@bez", szName ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        /// <summary>
        /// ReadOnly-Pruefung fuer GENAU eine Zeile. Die Variante ueber den Bezeichner
        /// beantwortet bei einer Dublette die Frage nach der falschen Zeile.
        /// </summary>
        public static bool IsReadOnlyById(int id)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE ID = ?",
                new DbParam("@id", id));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        public bool InsertFrom(PhotovoltaikModel m)
        {
            if (m != null) CopyFrom(m);
            return Insert();
        }

        public bool UpdateFrom(PhotovoltaikModel m, string szKey)
        {
            if (m != null) CopyFrom(m);
            return Update(szKey);
        }

        public bool Insert()
        {
            int neueId = DataRepository.GetMaxID(TABLE) + 1;

            string sql = @"INSERT INTO [" + TABLE + @"]
                            (ID, Bezeichner, Firma, Beschreibung, Leistung, Wirkungsgrad, U_Mpp, U_Leerlauf,
                             I_Mpp, I_Kurzschluss, alpha_SC, beta_OC, gamma_PMP, T_NOCT, Laenge, Breite, Modulkosten,
                             Technologie, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            DbParam[] ps = {
                new DbParam("@id", neueId),
                new DbParam("@bez", this.m_szName ?? ""),
                new DbParam("@fir", (object)(this.m_szFirma ?? "")),
                new DbParam("@bes", (object)(this.m_szBeschreibung ?? "")),
                new DbParam("@lei", this.m_Leistung),
                new DbParam("@wir", this.m_Wirkungsgrad),
                new DbParam("@ump", this.m_U_Mpp),
                new DbParam("@ule", this.m_U_Leerlauf),
                new DbParam("@imp", this.m_I_Mpp),
                new DbParam("@iks", this.m_I_Kurzschluss),
                new DbParam("@asc", this.m_alpha_SC),
                new DbParam("@boc", this.m_beta_OC),
                new DbParam("@gam", this.m_Temp_Coeff_Pmax),
                new DbParam("@noc", this.m_T_NOCT),
                new DbParam("@lae", this.m_Laenge),
                new DbParam("@bre", this.m_Breite),
                new DbParam("@mod", this.m_Modulkosten),
                Tec(),
                new DbParam("@ro", false)
            };

            bool ok = DataRepository.ExecuteSQL(sql, ps);
            if (ok) this.m_ID = neueId;
            return ok;
        }

        /// <summary>
        /// Die IDs aller Katalogsaetze zu diesem Bezeichner, aufsteigend.
        /// </summary>
        /// <remarks>
        /// <c>Tab_PV_STAMM</c> hat auf <c>Bezeichner</c> keinen eindeutigen Index. Gemessen
        /// am 18.08.2026 auf einer Kopie der Produktivdatenbank: 11 Zeilen, davon 10 auf
        /// fuenf doppelt vergebene Namen verteilt - alle fuenf Paare in JEDER Spalte ausser
        /// der ID gleich, also ein zweiter Importlauf. Migrationsschritt 18 raeumt sie weg;
        /// bis dahin (und nach einem erneuten Doppelimport) muss jeder Weg, der ueber den
        /// Namen adressiert, den Fall kennen.
        /// </remarks>
        public static List<int> IdsMitBezeichner(string szName)
        {
            List<int> ids = new List<int>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID FROM [" + TABLE + "] WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@bez", szName ?? ""));

            if (dt != null)
                foreach (DataRow r in dt.Rows)
                    if (r["ID"] != DBNull.Value) ids.Add(Convert.ToInt32(r["ID"]));

            return ids;
        }

        /// <summary>
        /// Loest <paramref name="szName"/> auf GENAU eine ID auf. Rueckgabe 0, wenn es
        /// keinen oder mehr als einen Treffer gibt; im mehrdeutigen Fall mit Meldung.
        /// </summary>
        /// <remarks>
        /// Der mehrdeutige Fall wird bewusst NICHT geraten. Die aufrufende Liste
        /// (<c>Form_AdminPV</c>) fuehrt nur den Namen, die gemeinte Zeile ist daraus nicht
        /// bestimmbar - und ein Schreibzugriff ueber den Bezeichner traefe beide. Genau
        /// dieselbe Entscheidung wie in <c>HeizkesselStammCtrl.Delete(string)</c>.
        /// </remarks>
        private static int EindeutigeId(string szName, string aktion)
        {
            List<int> ids = IdsMitBezeichner(szName);

            if (ids.Count == 0)
            {
                Meldung.Hinweis("Der Katalogeintrag \"" + (szName ?? "") + "\" wurde nicht gefunden.",
                    "Nicht gefunden");
                return 0;
            }

            if (ids.Count > 1)
            {
                Meldung.Warnung("Der Name \"" + (szName ?? "") + "\" ist im Katalog " + ids.Count +
                    "-mal vergeben. Es ist deshalb nicht entscheidbar, welcher Eintrag gemeint ist - " +
                    aktion + " wurde nichts.",
                    "Name mehrdeutig");
                return 0;
            }

            return ids[0];
        }

        // Aktualisiert den Datensatz. szKey ist der urspruengliche Bezeichner;
        // this.m_szName darf einen neuen Bezeichner tragen (Umbenennung).
        public bool Update(string szKey)
        {
            int id = EindeutigeId(szKey, "geändert");
            return id > 0 && Update(id);
        }

        /// <summary>
        /// Schreibt GENAU den Katalogsatz mit dieser ID zurueck.
        /// </summary>
        /// <remarks>
        /// Bis zum 18.08.2026 endete das UPDATE auf <c>WHERE Bezeichner = ?</c> und
        /// aenderte bei einem doppelt vergebenen Namen ZWEI Katalogsaetze zugleich -
        /// derselbe Befund wie bei <c>HeizkesselStammCtrl.Update</c>, und mit fuenf
        /// betroffenen Paaren hier sogar der groessere Anteil des Katalogs.
        /// </remarks>
        public bool Update(int id)
        {
            if (id <= 0) return false;

            if (IsReadOnlyById(id))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt");
                return false;
            }

            // Umbenennen darf keinen bereits vergebenen Namen treffen - sonst legte
            // ausgerechnet die Korrektur eine neue Dublette an. Greift nur bei echter
            // Umbenennung, sonst sperrte sie das Speichern einer Bestandsdublette aus.
            List<int> gleicheNamen = IdsMitBezeichner(this.m_szName);
            if (gleicheNamen.Count > 0 && !gleicheNamen.Contains(id))
            {
                Meldung.Hinweis("Ein anderer Katalogeintrag trägt bereits den Namen \"" +
                    (this.m_szName ?? "") + "\". Bitte einen eindeutigen Namen vergeben.",
                    "Name bereits vergeben");
                return false;
            }

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Bezeichner = ?, Firma = ?, Beschreibung = ?, Leistung = ?, Wirkungsgrad = ?,
                            U_Mpp = ?, U_Leerlauf = ?, I_Mpp = ?, I_Kurzschluss = ?,
                            alpha_SC = ?, beta_OC = ?, gamma_PMP = ?, T_NOCT = ?,
                            Laenge = ?, Breite = ?, Modulkosten = ?, Technologie = ?
                          WHERE ID = ?";

            DbParam[] ps = {
                new DbParam("@bez", this.m_szName ?? ""),
                new DbParam("@fir", (object)(this.m_szFirma ?? "")),
                new DbParam("@bes", (object)(this.m_szBeschreibung ?? "")),
                new DbParam("@lei", this.m_Leistung),
                new DbParam("@wir", this.m_Wirkungsgrad),
                new DbParam("@ump", this.m_U_Mpp),
                new DbParam("@ule", this.m_U_Leerlauf),
                new DbParam("@imp", this.m_I_Mpp),
                new DbParam("@iks", this.m_I_Kurzschluss),
                new DbParam("@asc", this.m_alpha_SC),
                new DbParam("@boc", this.m_beta_OC),
                new DbParam("@gam", this.m_Temp_Coeff_Pmax),
                new DbParam("@noc", this.m_T_NOCT),
                new DbParam("@lae", this.m_Laenge),
                new DbParam("@bre", this.m_Breite),
                new DbParam("@mod", this.m_Modulkosten),
                Tec(),
                new DbParam("@id", id)
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        /// <summary>
        /// Import-Ueberschreiben (Dublettenkonzept 4.2): aktualisiert GENAU die Felder,
        /// die der Import liefert, adressiert per ID. Vom Anwender gepflegte Felder
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

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Firma = ?, Leistung = ?, Wirkungsgrad = ?,
                            U_Mpp = ?, U_Leerlauf = ?, I_Mpp = ?, I_Kurzschluss = ?,
                            alpha_SC = ?, beta_OC = ?, gamma_PMP = ?, T_NOCT = ?,
                            Laenge = ?, Breite = ?, Technologie = ?
                          WHERE ID = ?";

            DbParam[] ps = {
                new DbParam("@fir", (object)(this.m_szFirma ?? "")),
                new DbParam("@lei", this.m_Leistung),
                new DbParam("@wir", this.m_Wirkungsgrad),
                new DbParam("@ump", this.m_U_Mpp),
                new DbParam("@ule", this.m_U_Leerlauf),
                new DbParam("@imp", this.m_I_Mpp),
                new DbParam("@iks", this.m_I_Kurzschluss),
                new DbParam("@asc", this.m_alpha_SC),
                new DbParam("@boc", this.m_beta_OC),
                new DbParam("@gam", this.m_Temp_Coeff_Pmax),
                new DbParam("@noc", this.m_T_NOCT),
                new DbParam("@lae", this.m_Laenge),
                new DbParam("@bre", this.m_Breite),
                Tec(),
                new DbParam("@id", id)
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        /// <summary>
        /// Parameter fuer <c>Technologie</c> (E2.3): LEER bleibt NULL. Eine leere
        /// Zeichenkette waere eine dritte Aussage neben "nicht gepflegt" und einem der
        /// fuenf Persistenzwerte - und die Dublettenpruefung vergleicht die Spalte mit.
        /// </summary>
        private DbParam Tec()
        {
            return new DbParam("@tec", string.IsNullOrEmpty(this.m_Technologie)
                                           ? DBNull.Value : (object)this.m_Technologie);
        }

        /// <summary>
        /// Loescht den Katalogsatz zum Bezeichner - aber nur, wenn er eindeutig ist.
        /// Dieselbe Anweisung loeschte bis zum 18.08.2026 bei einem doppelt vergebenen
        /// Namen BEIDE Zeilen.
        /// </summary>
        public bool Delete(string szName)
        {
            int id = EindeutigeId(szName, "gelöscht");
            return id > 0 && Delete(id);
        }

        /// <summary>Loescht GENAU den Katalogsatz mit dieser ID.</summary>
        public bool Delete(int id)
        {
            if (id <= 0) return false;

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

        private void CopyFrom(PhotovoltaikModel m)
        {
            this.m_szName = m.m_szName;
            this.m_szFirma = m.m_szFirma;
            this.m_szBeschreibung = m.m_szBeschreibung;
            this.m_Leistung = m.m_Leistung;
            this.m_Wirkungsgrad = m.m_Wirkungsgrad;
            this.m_U_Mpp = m.m_U_Mpp;
            this.m_U_Leerlauf = m.m_U_Leerlauf;
            this.m_I_Mpp = m.m_I_Mpp;
            this.m_I_Kurzschluss = m.m_I_Kurzschluss;
            this.m_alpha_SC = m.m_alpha_SC;
            this.m_beta_OC = m.m_beta_OC;
            this.m_Temp_Coeff_Pmax = m.m_Temp_Coeff_Pmax;
            this.m_T_NOCT = m.m_T_NOCT;
            this.m_Laenge = m.m_Laenge;
            this.m_Breite = m.m_Breite;
            this.m_Modulkosten = m.m_Modulkosten;
            this.m_Technologie = m.m_Technologie;
        }

        private static bool ReadOnlyOf(DataRow row)
        {
            return row.Table.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);
        }

        private static double D(DataRow row, string col)
        {
            return (row.Table.Columns.Contains(col) && row[col] != DBNull.Value) ? Convert.ToDouble(row[col]) : 0.0;
        }

        private static void FillFromRow(PhotovoltaikModel m, DataRow row)
        {
            if (row.Table.Columns.Contains("ID") && row["ID"] != DBNull.Value) m.m_ID = Convert.ToInt32(row["ID"]);
            if (row.Table.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) m.m_szName = row["Bezeichner"].ToString();
            if (row.Table.Columns.Contains("Firma") && row["Firma"] != DBNull.Value) m.m_szFirma = row["Firma"].ToString();
            if (row.Table.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) m.m_szBeschreibung = row["Beschreibung"].ToString();
            m.m_Leistung = D(row, "Leistung");
            m.m_Wirkungsgrad = D(row, "Wirkungsgrad");
            m.m_U_Mpp = D(row, "U_Mpp");
            m.m_U_Leerlauf = D(row, "U_Leerlauf");
            m.m_I_Mpp = D(row, "I_Mpp");
            m.m_I_Kurzschluss = D(row, "I_Kurzschluss");
            m.m_alpha_SC = D(row, "alpha_SC");
            m.m_beta_OC = D(row, "beta_OC");
            m.m_Temp_Coeff_Pmax = D(row, "gamma_PMP");
            m.m_T_NOCT = D(row, "T_NOCT");
            m.m_Laenge = D(row, "Laenge");
            m.m_Breite = D(row, "Breite");
            m.m_Modulkosten = D(row, "Modulkosten");
            // E2.3: fehlende Spalte und NULL sind derselbe Fall - Technologie unbekannt.
            if (row.Table.Columns.Contains("Technologie") && row["Technologie"] != DBNull.Value)
                m.m_Technologie = row["Technologie"].ToString();
        }

        private PhotovoltaikModel MapRowToModel(DataRow row)
        {
            PhotovoltaikModel m = new PhotovoltaikModel();
            FillFromRow(m, row);
            return m;
        }

        // =================================================================================
        // W6.0c - Herstellerfilter des Projektdialogs
        // =================================================================================

        /// <summary>Eine Zeile der Katalogliste: Primaerschluessel und Bezeichner.</summary>
        public sealed record KatalogZeile(int Id, string Bezeichner);

        /// <summary>
        /// Die Hersteller des Katalogs in Anzeigereihenfolge - die Auswahlliste
        /// <c>comboBox_Hersteller</c>.
        /// </summary>
        /// <remarks>
        /// Zeichengleich <c>Form_PV_Load</c> (Z. 69):
        /// <c>SELECT Firma FROM Tab_PV_STAMM GROUP BY Firma ORDER BY Firma</c>. Das
        /// <c>GROUP BY</c> statt <c>DISTINCT</c> ist Bestand und bleibt - es tut hier
        /// dasselbe.
        /// </remarks>
        public static IReadOnlyList<string> Hersteller()
        {
            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Firma FROM " + TABLE + " GROUP BY Firma ORDER BY Firma");
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
                liste.Add(row["Firma"] == DBNull.Value ? "" : row["Firma"].ToString());
            return liste;
        }

        /// <summary>
        /// Die Katalogliste, eingeengt auf einen Hersteller.
        /// </summary>
        /// <param name="hersteller">
        /// Eintrag aus <see cref="Hersteller"/>. Leer, <c>null</c> und „Alle" heben die
        /// Einengung auf.
        /// </param>
        /// <remarks>
        /// <para>
        /// Aus <c>Form_PV.SetFilter</c> (Z. 215-233). Zwei Dinge aendern sich, beide
        /// notwendig:
        /// </para>
        /// <list type="bullet">
        /// <item>Der Herstellername kommt als <see cref="DbParam"/> statt als eingesetzter
        /// Text. Der Bestand baute <c>Firma='…'</c> zusammen, ohne das Hochkomma zu
        /// verdoppeln - ein Herstellername mit Apostroph zerriss das Praedikat
        /// (der Pufferspeicherfilter verdoppelte es wenigstens).</item>
        /// <item><c>ORDER BY Bezeichner</c> steht jetzt da. Der Bestand sortierte hier
        /// NICHT, waehrend die Erstbefuellung ueber <see cref="ReadAll"/> sortiert kam -
        /// die Liste sprang beim ersten Filtern in eine andere Reihenfolge.</item>
        /// </list>
        /// </remarks>
        public IReadOnlyList<KatalogZeile> Filtern(string hersteller)
        {
            string h = (hersteller ?? "").Trim();
            bool alle = h.Length == 0 || h == "Alle";

            string sql = alle
                ? "SELECT ID, Bezeichner FROM " + TABLE + " ORDER BY Bezeichner"
                : "SELECT ID, Bezeichner FROM " + TABLE + " WHERE Firma = ? ORDER BY Bezeichner";

            var liste = new List<KatalogZeile>();
            DataTable dt = alle
                ? DataRepository.GetDataTable(sql)
                : DataRepository.GetDataTable(sql, new DbParam("@firma", h));
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
        /// Die Anzeigefelder eines Katalogmoduls - der Detailblock des Projektdialogs.
        /// </summary>
        /// <param name="Bezeichner">Modulname.</param>
        /// <param name="Beschreibung">Freitext.</param>
        /// <param name="Firma">Hersteller.</param>
        /// <param name="Leistung">Leistung EINES Moduls in WATT - die Katalogspalte
        /// <c>Leistung</c>, die der Katalogdialog „Nennleistung (Pmax)" mit der
        /// Einheit W nennt (W6-O-5).</param>
        /// <remarks>
        /// <para><b>Die dreizehn weiteren Felder kamen mit dem Anwenderwunsch W6-E-1</b>
        /// (Windows-Abnahme 05.09.2026: „optional sollten beim ausgewaehlten PV-Modul
        /// alle Eigenschaften/Parameter angezeigt werden"). Sie sind <c>double?</c>, weil
        /// <c>Tab_PV_STAMM</c> sie NULL fuehren darf - und weil eine NULL etwas anderes
        /// ist als eine gemessene 0. Fuer die ANZEIGE fallen beide zusammen
        /// (siehe <see cref="Parameterzeilen"/>), fuer die Rechnung nicht.</para>
        /// </remarks>
        public sealed record ModulDetail(string Bezeichner, string Beschreibung,
                                         string Firma, double Leistung,
                                         double? Wirkungsgrad = null, double? UMpp = null,
                                         double? ULeerlauf = null, double? IMpp = null,
                                         double? IKurzschluss = null, double? AlphaSc = null,
                                         double? BetaOc = null, double? GammaPmp = null,
                                         double? TNoct = null, double? Laenge = null,
                                         double? Breite = null, double? Modulkosten = null,
                                         string Technologie = "");

        /// <summary>
        /// Die Anzeigefelder zum Bezeichner; <c>null</c>, wenn es keinen Satz gibt.
        /// </summary>
        /// <remarks>
        /// <para>Fasst die drei zeichengleichen <c>RecordSet</c>-Bloecke von
        /// <c>listBox_Auswahl_SelectedIndexChanged</c> (Z. 163),
        /// <c>listBox_DB_SelectedIndexChanged</c> (Z. 191) und
        /// <c>UpdateGesamtleistung</c> (Z. 314) zusammen. <c>ORDER BY ID</c> macht die Wahl
        /// bei einem doppelt vergebenen Bezeichner benennbar.</para>
        /// <para><b>W6-E-1:</b> Die Spaltenliste ist um die dreizehn uebrigen
        /// Katalogfelder gewachsen - EIN Lesevorgang statt eines zweiten daneben. Der
        /// Vorlaeufer las an dieser Stelle ohnehin <c>select *</c>.</para>
        /// </remarks>
        public static ModulDetail Detail(string szName)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Bezeichner, Beschreibung, Firma, Leistung, Wirkungsgrad, U_Mpp, U_Leerlauf, " +
                "I_Mpp, I_Kurzschluss, alpha_SC, beta_OC, gamma_PMP, T_NOCT, Laenge, Breite, " +
                "Modulkosten, Technologie FROM " + TABLE +
                " WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@nam", szName ?? ""));
            if (dt == null || dt.Rows.Count == 0) return null;

            DataRow r = dt.Rows[0];
            return new ModulDetail(
                r["Bezeichner"] == DBNull.Value ? "" : r["Bezeichner"].ToString(),
                r["Beschreibung"] == DBNull.Value ? "" : r["Beschreibung"].ToString(),
                r["Firma"] == DBNull.Value ? "" : r["Firma"].ToString(),
                r["Leistung"] == DBNull.Value ? 0 : Convert.ToDouble(r["Leistung"]),
                Wert(r, "Wirkungsgrad"), Wert(r, "U_Mpp"), Wert(r, "U_Leerlauf"),
                Wert(r, "I_Mpp"), Wert(r, "I_Kurzschluss"), Wert(r, "alpha_SC"),
                Wert(r, "beta_OC"), Wert(r, "gamma_PMP"), Wert(r, "T_NOCT"),
                Wert(r, "Laenge"), Wert(r, "Breite"), Wert(r, "Modulkosten"),
                r.Table.Columns.Contains("Technologie") && r["Technologie"] != DBNull.Value
                    ? r["Technologie"].ToString() : "");
        }

        /// <summary>Eine Zahl der Katalogzeile; <c>null</c> bei NULL oder fehlender Spalte.</summary>
        private static double? Wert(DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte) || row[spalte] == DBNull.Value) return null;
            return Convert.ToDouble(row[spalte]);
        }

        // =================================================================================
        // W6-E-1 - alle Modulparameter des gewaehlten Katalogsatzes
        // =================================================================================

        /// <summary>
        /// Eine Zeile des Parameterblocks: sprachneutraler Schluessel, uebersetzte
        /// Beschriftung, fertig formatierter Wert und die Einheit dahinter.
        /// </summary>
        public sealed record ModulParameter(string Schluessel, string Bezeichnung,
                                            string Wert, string Einheit);

        /// <summary>
        /// Was ein nicht gepflegter Wert anzeigt. Der Katalog fuehrt „nicht gepflegt"
        /// als 0 (<c>ModulKatalogProfil</c>: „leer = 0 = nicht gepflegt") und als NULL;
        /// beides ist dasselbe und darf nicht als gemessene Null erscheinen.
        /// </summary>
        public const string PARAMETER_LEER = "–";

        /// <summary>
        /// <b>Alle Eigenschaften eines PV-Moduls als Anzeigezeilen</b> (Anwenderwunsch
        /// W6-E-1, Windows-Abnahme 05.09.2026).
        /// </summary>
        /// <param name="d">Der Katalogsatz aus <see cref="Detail"/>; <c>null</c> = leere Liste.</param>
        /// <remarks>
        /// <para><b>Beschriftung und Einheit kommen aus DERSELBEN Quelle wie der
        /// Katalogdialog</b> — <see cref="ModulKatalogProfil"/> in der Auspraegung
        /// <see cref="ModulKatalogArt.Photovoltaik"/>. Es gibt fuer einen Modulwert genau
        /// einen Text im Haus; ein zweiter liefe beim ersten Fachwechsel auseinander.
        /// Die zwei Temperaturkoeffizienten <c>alpha_SC</c> und <c>beta_OC</c> fuehrt der
        /// Katalogdialog NICHT (er kann sie nicht pflegen, siehe
        /// <see cref="SpeichernAus"/>); ihre Beschriftungen stehen deshalb dort, wo sie
        /// der Bestand fuehrt — im Modulimport (W13, <c>PVIMP_LBL_*</c>), samt Einheit
        /// im Text.</para>
        /// <para><b>Die Zahlen sehen aus wie im Katalogdialog:</b> der Wirkungsgrad mit
        /// zwei Nachkommastellen (<c>PvAdminHuelle.Anzeige</c>), alles Uebrige roh in der
        /// Kultur des Anwenders. Wer beide Masken nebeneinanderlegt, liest dieselben
        /// Ziffern.</para>
        /// </remarks>
        public static IReadOnlyList<ModulParameter> Parameterzeilen(ModulDetail d)
        {
            var zeilen = new List<ModulParameter>();
            if (d == null) return zeilen;

            ModulKatalogProfil profil = ModulKatalogProfil.Finde(ModulKatalogArt.Photovoltaik, Uebersetzt);

            zeilen.Add(Aus(profil, ModulKatalogProfil.FeldWirkungsgrad, Zahl(d.Wirkungsgrad, "F2")));
            zeilen.Add(Aus(profil, ModulKatalogProfil.FeldUMpp, Zahl(d.UMpp)));
            zeilen.Add(Aus(profil, ModulKatalogProfil.FeldULeerlauf, Zahl(d.ULeerlauf)));
            zeilen.Add(Aus(profil, ModulKatalogProfil.FeldIMpp, Zahl(d.IMpp)));
            zeilen.Add(Aus(profil, ModulKatalogProfil.FeldIKurzschluss, Zahl(d.IKurzschluss)));

            // Die zwei Koeffizienten des Imports - Einheit steht im Beschriftungstext.
            zeilen.Add(new ModulParameter("ALPHA_SC", Text("PVIMP_LBL_ALPHA_ISC", "alpha_SC:"),
                                          Zahl(d.AlphaSc), ""));
            zeilen.Add(new ModulParameter("BETA_OC", Text("PVIMP_LBL_BETA_VOC", "beta_OC:"),
                                          Zahl(d.BetaOc), ""));

            zeilen.Add(Aus(profil, ModulKatalogProfil.FeldTempKoeff, Zahl(d.GammaPmp)));
            zeilen.Add(Aus(profil, ModulKatalogProfil.FeldTNoct, Zahl(d.TNoct)));
            zeilen.Add(Aus(profil, ModulKatalogProfil.FeldLaenge, Zahl(d.Laenge)));
            zeilen.Add(Aus(profil, ModulKatalogProfil.FeldBreite, Zahl(d.Breite)));
            zeilen.Add(Aus(profil, ModulKatalogProfil.FeldModulkosten, Zahl(d.Modulkosten)));
            zeilen.Add(Aus(profil, ModulKatalogProfil.FeldTechnologie, TechnologieText(d.Technologie)));

            return zeilen;
        }

        /// <summary>Baut eine Zeile aus dem Katalogfeld gleichen Schluessels.</summary>
        private static ModulParameter Aus(ModulKatalogProfil profil, string schluessel, string wert)
        {
            foreach (ModulKatalogFeld f in profil.Felder)
                if (f.Schluessel == schluessel)
                    return new ModulParameter(schluessel, f.Bezeichnung, wert, f.Einheit);

            return new ModulParameter(schluessel, schluessel, wert, "");
        }

        /// <summary>
        /// Der Anzeigetext einer Zahl; NULL und 0 heissen beide „nicht gepflegt".
        /// </summary>
        private static string Zahl(double? wert, string format = null)
        {
            if (!wert.HasValue || wert.Value == 0.0) return PARAMETER_LEER;

            return format == null
                ? wert.Value.ToString(System.Globalization.CultureInfo.CurrentCulture)
                : wert.Value.ToString(format, System.Globalization.CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Die Zelltechnologie im Klartext — dieselben fuenf Texte wie im Katalogdialog.
        /// Ein unbekannter Code wird GEZEIGT, nicht verschluckt: Er steht so in der
        /// Datenbank, und wer ihn sucht, muss ihn lesen koennen.
        /// </summary>
        private static string TechnologieText(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return PARAMETER_LEER;

            foreach (var o in ModulKatalogProfil.Technologien(Uebersetzt))
                if (string.Equals(o.Wert, code, StringComparison.Ordinal)) return o.Text;

            return code;
        }

        /// <summary>Uebersetzt einen Beschriftungsschluessel; unbekannt = der Schluessel selbst.</summary>
        private static string Uebersetzt(string schluessel) => Text(schluessel, schluessel);

        /// <summary>
        /// Der Bezeichner eines Katalogmoduls ueber seinen Primaerschluessel; leer, wenn
        /// es ihn nicht gibt.
        /// </summary>
        /// <remarks>
        /// Der Vorlaeufer <c>Form_PV.btn_Hinzu_Click</c> nahm den Namen aus der ListBox
        /// und suchte damit den Satz; die Zeile fuehrt seit iU9-W6.5 ihre Id, und die
        /// ist eindeutig - der Katalog kann gleichnamige Module fuehren.
        /// </remarks>
        public static string BezeichnerZu(int id)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM " + TABLE + " WHERE ID = ?", new DbParam("@id", id));
            return (v == null || v == DBNull.Value) ? "" : v.ToString();
        }

        // =================================================================================
        // W14a.0e - der EINE Schreibeinstieg des Modulkatalogs
        // =================================================================================

        /// <summary>
        /// Was ein Speicherversuch des Modulkatalogs ergeben hat — dieselbe Form wie
        /// <c>HeizkesselStammCtrl.SpeicherErgebnis</c> (W6.0).
        /// </summary>
        public sealed record SpeicherErgebnis(bool Ok, string Meldung, string Name);

        /// <summary>
        /// Schreibt den Modulsatz — der Weg des Knopfes „Speichern"
        /// (<c>Form_AdminPV.btn_Speichern_Click</c> Z. 58-134).
        /// </summary>
        /// <param name="daten">Die dreizehn Felder der Maske.</param>
        /// <param name="neu">
        /// <c>true</c> nach „Neu…": anlegen statt aendern (Bestandsfeld <c>m_Neu</c>).
        /// </param>
        /// <param name="schluessel">
        /// Der urspruengliche Bezeichner — der WHERE-Schluessel des UPDATE. Der Bestand
        /// nahm dafuer <c>listBox_PV.Text</c> (Z. 118).
        /// </param>
        /// <remarks>
        /// <para><b>Befund W14-B33 behoben.</b> Der Vorlaeufer meldete den Erfolg des
        /// UPDATE, hatte aber KEINEN <c>else</c>-Zweig: Ein fehlgeschlagenes Update
        /// schwieg. Jetzt kommt in beiden Faellen ein Ergebnis mit Text zurueck.</para>
        /// <para>Der <c>Exists</c>-Vorabtest beim Anlegen ist woertlich uebernommen
        /// (Z. 104), einschliesslich seiner Meldung.</para>
        /// </remarks>
        public static SpeicherErgebnis SpeichernAus(PhotovoltaikModel daten, bool neu, string schluessel)
        {
            if (daten == null || string.IsNullOrWhiteSpace(daten.m_szName))
                return new SpeicherErgebnis(false,
                    MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG, "");

            try
            {
                var ctrl = new PhotovoltaikStammCtrl();

                if (neu)
                {
                    if (ctrl.Exists(daten.m_szName))
                        return new SpeicherErgebnis(false,
                            MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT, "");

                    if (!ctrl.InsertFrom(daten))
                        return new SpeicherErgebnis(false,
                            MyResource.Resource.PSP_MELDUNG_SPEICHERN_FEHLER, "");

                    return new SpeicherErgebnis(true,
                        MyResource.Resource.PSP_MELDUNG_DATENSATZ_GESPEICHERT, daten.m_szName);
                }

                // PV-Katalog-Koeffizienten (mit Merge 5 aus Form_AdminPV nachgezogen): Die
                // Katalogmaske fuehrt alpha_SC und beta_OC nicht. Ein Update aus ihr traegt
                // deshalb die GESPEICHERTEN Koeffizienten weiter, statt sie mit 0 zu
                // ueberschreiben - genau das loeschte bis dahin bei jedem Speichern eines
                // CEC-Moduls seine Temperaturkoeffizienten.
                var bestand = new PhotovoltaikStammCtrl();
                bestand.ReadSingle(schluessel ?? daten.m_szName);
                if (bestand.rows > 0)
                {
                    if (daten.m_alpha_SC == 0.0) daten.m_alpha_SC = bestand.items[0].m_alpha_SC;
                    if (daten.m_beta_OC == 0.0) daten.m_beta_OC = bestand.items[0].m_beta_OC;
                }

                if (!ctrl.UpdateFrom(daten, schluessel ?? daten.m_szName))
                    return new SpeicherErgebnis(false,
                        MyResource.Resource.PSP_MELDUNG_SPEICHERN_FEHLER, "");

                return new SpeicherErgebnis(true,
                    MyResource.Resource.PSP_MELDUNG_DATENSATZ_GESPEICHERT, daten.m_szName);
            }
            catch (Exception ex)
            {
                return new SpeicherErgebnis(false,
                    string.Format(MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message), "");
            }
        }

        /// <summary>
        /// Loescht ein Katalogmodul und sagt, warum es nicht ging.
        /// </summary>
        /// <remarks>
        /// Der Vorlaeufer (<c>Form_AdminPV.btn_Loeschen_Click</c> Z. 221-242) loeschte
        /// OHNE Rueckfrage (Befund W14-B35) und schluckte jede Ausnahme still (Z. 239).
        /// Die Rueckfrage stellt jetzt die Oberflaeche, der Grund kommt von hier.
        /// </remarks>
        public static SpeicherErgebnis Loeschen(string szName)
        {
            if (string.IsNullOrWhiteSpace(szName))
                return new SpeicherErgebnis(false,
                    MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG, "");

            try
            {
                var ctrl = new PhotovoltaikStammCtrl();
                if (!ctrl.Delete(szName))
                    return new SpeicherErgebnis(false, Text("KBROW_MSG_LOESCHEN_FEHLER",
                        "Der Datensatz konnte nicht gelöscht werden."), "");

                return new SpeicherErgebnis(true, "", szName);
            }
            catch (Exception ex)
            {
                return new SpeicherErgebnis(false,
                    string.Format(MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message), "");
            }
        }

        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
