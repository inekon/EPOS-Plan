using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    // Controller fuer die Stammdaten-Tabelle Tab_Pufferspeicher_STAMM (globaler Katalog).
    // Analog zu HeizkesselStammCtrl / StromspeicherStammCtrl:
    //   - Tabelle = Tab_Pufferspeicher_STAMM
    //   - DB-Spalten Bezeichner/Hersteller/Bereitschaftsverluste werden auf die Model-Felder
    //     Name/Firma/Betriebsbereitschaftverlust abgebildet
    //   - liest/schreibt das Feld ReadOnly
    //   - Insert() vergibt eine explizite ID (MAX+1) und setzt ReadOnly = false
    //   - Update()/Delete() verweigern schreibgeschuetzte Datensaetze
    // Alle DB-Zugriffe laufen ueber DataRepository.
    public class PufferSpStammCtrl : PufferSpModel
    {
        public const string TABLE = "Tab_Pufferspeicher_STAMM";

        private List<PufferSpModel> _internalList = new List<PufferSpModel>();
        public int rows => _internalList.Count;
        public List<PufferSpModel> items => _internalList;

        public bool m_bReadOnly = false;

        public void ReadAll(string filter = "")
        {
            string sql = "SELECT * FROM [" + TABLE + "]";
            if (!string.IsNullOrEmpty(filter)) sql += " WHERE " + filter;

            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();
            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                _internalList.Add(MapRowToModel(row));
            }
        }

        public bool Exists(string szName)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szName ?? ""));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        public static bool IsReadOnlyStatic(string szName)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szName ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        /// <summary>
        /// Schreibschutz des Katalogeintrags mit der angegebenen STAMM-ID.
        /// </summary>
        /// <remarks>
        /// V0-9: eindeutige Fassung von <see cref="IsReadOnlyStatic(string)"/>. Bei
        /// gleichnamigen Katalogeinträgen liefert die Namensfassung den Schreibschutz
        /// irgendeines Treffers, nicht den der gemeinten Zeile.
        /// </remarks>
        public static bool IsReadOnlyStatic(int id)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE ID = ?",
                new OleDbParameter("@id", id));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        // Uebernimmt die Werte aus einem Model und legt einen neuen Stammdatensatz an.
        public bool InsertFrom(PufferSpModel m)
        {
            if (m != null)
            {
                this.Name = m.Name;
                this.Firma = m.Firma;
                this.Speichertyp = m.Speichertyp;
                this.Betriebsbereitschaftverlust = m.Betriebsbereitschaftverlust;
                this.Gesamtvolumen = m.Gesamtvolumen;
                this.Investitionskosten = m.Investitionskosten;
            }
            return Insert();
        }

        // Uebernimmt die Werte aus einem Model und aktualisiert den Datensatz (Schluessel = Name).
        public bool UpdateFrom(PufferSpModel m)
        {
            if (m != null)
            {
                this.Name = m.Name;
                this.Firma = m.Firma;
                this.Speichertyp = m.Speichertyp;
                this.Betriebsbereitschaftverlust = m.Betriebsbereitschaftverlust;
                this.Gesamtvolumen = m.Gesamtvolumen;
                this.Investitionskosten = m.Investitionskosten;
            }
            return Update();
        }

        public bool Insert()
        {
            int neueId = DataRepository.GetMaxID(TABLE) + 1;

            string sql = @"INSERT INTO [" + TABLE + @"]
                            (ID, Bezeichner, Hersteller, Speichertyp, Bereitschaftsverluste, Gesamtvolumen, Investitionskosten, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

            OleDbParameter[] ps = {
                new OleDbParameter("@id", neueId),
                new OleDbParameter("@bez", this.Name ?? ""),
                new OleDbParameter("@her", (object)(this.Firma ?? "")),
                new OleDbParameter("@typ", (object)(this.Speichertyp ?? "")),
                new OleDbParameter("@ver", this.Betriebsbereitschaftverlust),
                new OleDbParameter("@vol", this.Gesamtvolumen),
                new OleDbParameter("@inv", this.Investitionskosten),
                new OleDbParameter("@ro", false)
            };

            bool ok = DataRepository.ExecuteSQL(sql, ps);
            if (ok) this.ID = neueId;
            return ok;
        }

        public bool Update()
        {
            if (IsReadOnlyStatic(this.Name))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Hersteller = ?, Speichertyp = ?, Bereitschaftsverluste = ?,
                            Investitionskosten = ?, Gesamtvolumen = ?
                          WHERE Bezeichner = ?";

            OleDbParameter[] ps = {
                new OleDbParameter("@her", (object)(this.Firma ?? "")),
                new OleDbParameter("@typ", (object)(this.Speichertyp ?? "")),
                new OleDbParameter("@ver", this.Betriebsbereitschaftverlust),
                new OleDbParameter("@inv", this.Investitionskosten),
                new OleDbParameter("@vol", this.Gesamtvolumen),
                new OleDbParameter("@bez", this.Name ?? "")
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        /// <summary>
        /// Import-Ueberschreiben (Dublettenkonzept 4.2): aktualisiert GENAU die Felder,
        /// die der VDI-Import liefert, adressiert per ID. Vom Anwender gepflegte Felder
        /// (Bezeichner, Investitionskosten, ReadOnly) bleiben unangetastet.
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
                            Hersteller = ?, Speichertyp = ?, Bereitschaftsverluste = ?, Gesamtvolumen = ?
                          WHERE ID = ?";

            OleDbParameter[] ps = {
                new OleDbParameter("@her", (object)(this.Firma ?? "")),
                new OleDbParameter("@typ", (object)(this.Speichertyp ?? "")),
                new OleDbParameter("@ver", this.Betriebsbereitschaftverlust),
                new OleDbParameter("@vol", this.Gesamtvolumen),
                new OleDbParameter("@id", id)
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        /// <summary>
        /// Löscht den Katalogeintrag mit der angegebenen STAMM-ID.
        /// </summary>
        /// <remarks>
        /// V0-9: Gelöscht wird über die ID der ausgewählten Zeile statt über den
        /// Bezeichner. Der Katalog kann gleichnamige Einträge enthalten - die
        /// Eingabemasken verhindern nur neue Dubletten über die Oberfläche, der
        /// VDI-3805-Import legt sie durchaus an -, und "WHERE Bezeichner = ?" hat dann
        /// ALLE Namensvettern auf einmal getilgt. Die B0-8-Rückfrage im Dialog schützt
        /// nur vor dem versehentlichen Auslösen, nicht vor dem Mehrfachtreffer.
        /// </remarks>
        public bool Delete(int id)
        {
            if (id <= 0) return false;

            if (IsReadOnlyStatic(id))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string sql = "DELETE FROM [" + TABLE + "] WHERE ID = ?";
            return DataRepository.ExecuteSQL(sql, new OleDbParameter("@id", id));
        }

        /// <summary>
        /// Löschung über den Bezeichner - Zugang für Aufrufer, die keine ID zur Hand
        /// haben (Katalogdialog der Administration).
        /// </summary>
        /// <remarks>
        /// V0-9: Der Name wird zuerst auf GENAU EINE ID aufgelöst; gelöscht wird dann
        /// über <see cref="Delete(int)"/>. Damit trifft auch dieser Weg bei
        /// gleichnamigen Katalogeinträgen nur noch einen Datensatz statt alle. Neuer
        /// Code reicht die ID der ausgewählten Zeile durch und ruft <see cref="Delete(int)"/>.
        /// </remarks>
        public bool Delete(string szName)
        {
            return Delete(DataRepository.GetIdByName(TABLE, "Bezeichner", szName ?? ""));
        }

        private PufferSpModel MapRowToModel(DataRow row)
        {
            PufferSpModel m = new PufferSpModel();
            if (row.Table.Columns.Contains("ID") && row["ID"] != DBNull.Value) m.ID = Convert.ToInt32(row["ID"]);
            if (row.Table.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) m.Name = row["Bezeichner"].ToString();
            if (row.Table.Columns.Contains("Hersteller") && row["Hersteller"] != DBNull.Value) m.Firma = row["Hersteller"].ToString();
            if (row.Table.Columns.Contains("Speichertyp") && row["Speichertyp"] != DBNull.Value) m.Speichertyp = row["Speichertyp"].ToString();
            if (row.Table.Columns.Contains("Bereitschaftsverluste") && row["Bereitschaftsverluste"] != DBNull.Value) m.Betriebsbereitschaftverlust = Convert.ToDouble(row["Bereitschaftsverluste"]);
            if (row.Table.Columns.Contains("Gesamtvolumen") && row["Gesamtvolumen"] != DBNull.Value) m.Gesamtvolumen = Convert.ToInt32(row["Gesamtvolumen"]);
            if (row.Table.Columns.Contains("Investitionskosten") && row["Investitionskosten"] != DBNull.Value) m.Investitionskosten = Convert.ToDouble(row["Investitionskosten"]);
            return m;
        }
    }
}
