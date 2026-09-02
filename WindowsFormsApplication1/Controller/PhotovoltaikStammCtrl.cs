using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

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
                             I_Mpp, I_Kurzschluss, alpha_SC, beta_OC, gamma_PMP, T_NOCT, Laenge, Breite, Modulkosten, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

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
                MessageBox.Show("Der Katalogeintrag \"" + (szName ?? "") + "\" wurde nicht gefunden.",
                    "Nicht gefunden", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }

            if (ids.Count > 1)
            {
                MessageBox.Show("Der Name \"" + (szName ?? "") + "\" ist im Katalog " + ids.Count +
                    "-mal vergeben. Es ist deshalb nicht entscheidbar, welcher Eintrag gemeint ist - " +
                    aktion + " wurde nichts.",
                    "Name mehrdeutig", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // Umbenennen darf keinen bereits vergebenen Namen treffen - sonst legte
            // ausgerechnet die Korrektur eine neue Dublette an. Greift nur bei echter
            // Umbenennung, sonst sperrte sie das Speichern einer Bestandsdublette aus.
            List<int> gleicheNamen = IdsMitBezeichner(this.m_szName);
            if (gleicheNamen.Count > 0 && !gleicheNamen.Contains(id))
            {
                MessageBox.Show("Ein anderer Katalogeintrag trägt bereits den Namen \"" +
                    (this.m_szName ?? "") + "\". Bitte einen eindeutigen Namen vergeben.",
                    "Name bereits vergeben", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Bezeichner = ?, Firma = ?, Beschreibung = ?, Leistung = ?, Wirkungsgrad = ?,
                            U_Mpp = ?, U_Leerlauf = ?, I_Mpp = ?, I_Kurzschluss = ?,
                            alpha_SC = ?, beta_OC = ?, gamma_PMP = ?, T_NOCT = ?,
                            Laenge = ?, Breite = ?, Modulkosten = ?
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
                            Laenge = ?, Breite = ?
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
                new DbParam("@id", id)
            };

            return DataRepository.ExecuteSQL(sql, ps);
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
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        }

        private PhotovoltaikModel MapRowToModel(DataRow row)
        {
            PhotovoltaikModel m = new PhotovoltaikModel();
            FillFromRow(m, row);
            return m;
        }
    }
}
