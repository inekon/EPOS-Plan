using System;
using System.Data;

namespace WindowsFormsApplication1
{
    // Controller fuer die Stammdaten-Tabelle Tab_Solarkollektoren_STAMM (globaler Kollektor-Katalog).
    // Analog zu HeizkesselStammCtrl / PhotovoltaikStammCtrl:
    //   - Tabelle = Tab_Solarkollektoren_STAMM
    //   - DB-Spalte Bezeichner wird auf m_szKollektorname abgebildet, Investitionskosten auf m_Kosten
    //   - liest/schreibt das Feld ReadOnly
    //   - InsertFrom() vergibt eine explizite ID (MAX+1) und setzt ReadOnly = false
    //   - Update()/Delete() verweigern schreibgeschuetzte Datensaetze
    // Alle DB-Zugriffe laufen ueber DataRepository.
    public class SolarkollektorenStammCtrl : SolarkollektorenModel
    {
        public const string TABLE = "Tab_Solarkollektoren_STAMM";

        public bool m_bReadOnly = false;

        public void ReadAll(string szFilter = "")
        {
            string sql = "SELECT * FROM [" + TABLE + "]";
            if (!string.IsNullOrEmpty(szFilter)) sql += " WHERE " + szFilter;
            sql += " ORDER BY Bezeichner";

            DataTable dt = DataRepository.GetDataTable(sql);

            items = new SolarkollektorenModel[1000];
            rows = 0;

            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (rows >= items.Length) break;
                    items[rows] = MapRowToModel(row);
                    rows += 1;
                }
            }
        }

        public void ReadSingle(string szName)
        {
            string sql = "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ?";
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@bez", szName ?? (object)DBNull.Value));

            rows = 0;
            if (dt != null && dt.Rows.Count > 0)
            {
                FillFromRow(this, dt.Rows[0]);
                this.m_bReadOnly = ReadOnlyOf(dt.Rows[0]);
                items = new SolarkollektorenModel[1] { this };
                rows = 1;
            }
        }

        /// <summary>
        /// Die Id eines Katalogsatzes zu seinem Bezeichner (iU9-W7.0e) — dieselbe
        /// Auskunft, die <c>Form_SolarKollektoren.btn_Hinzzu_Click</c> ueber
        /// <c>DataRepository.GetIdByName</c> holte (Z. 199). Sie steht hier, damit die
        /// Huelle nicht die Tabellenkonstante nach aussen tragen muss.
        /// </summary>
        /// <returns>0, wenn es den Namen im Katalog nicht gibt.</returns>
        public static int IdZu(string szName)
        {
            return DataRepository.GetIdByName(TABLE, "Bezeichner", szName);
        }

        /// <summary>
        /// Ein Katalogsatz ueber seine ID (iU9-W7.0e) — der Weg, auf dem
        /// <c>btn_Hinzzu_Click</c> Vor- und Ruecklauf des Stammsatzes in die neue
        /// Projektzeile uebernimmt (Z. 214-224).
        ///
        /// <para><b>Vorlauf und Ruecklauf werden ganzzahlig gelesen.</b> Der Vorlaeufer
        /// tat das ueber eine eigene Hilfsmethode <c>IntCol</c>, die zwei Spaltennamen
        /// probierte — „Ruecklauf" in ASCII und „Rücklauf" mit Umlaut. Die
        /// Doppelschreibung stammt aus dem Access-Bestand; die Abbildung
        /// <see cref="MapRowToModel"/> dieser Klasse kennt sie bereits.</para>
        /// </summary>
        /// <returns><c>null</c>, wenn es den Satz nicht gibt.</returns>
        public static SolarkollektorenModel ReadById(int id)
        {
            if (id <= 0) return null;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE ID = ?", new DbParam("@id", id));
            if (dt == null || dt.Rows.Count == 0) return null;

            var ctrl = new SolarkollektorenStammCtrl();
            return ctrl.MapRowToModel(dt.Rows[0]);
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
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@bez", szName ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        public bool InsertFrom(SolarkollektorenModel m)
        {
            if (m != null) CopyFrom(m);

            int neueId = DataRepository.GetMaxID(TABLE) + 1;

            string sql = @"INSERT INTO [" + TABLE + @"]
                            (ID, Bezeichner, Firma, Beschreibung, Kollektortyp, Modulflaeche, Aperturflaeche,
                             h0, k1, k2, Kdir, Kdfu, Investitionskosten, Vorlauf, Ruecklauf, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            DbParam[] ps = {
                new DbParam("@id", neueId),
                new DbParam("@bez", this.m_szKollektorname ?? ""),
                new DbParam("@fir", (object)(this.m_szFirma ?? "")),
                new DbParam("@bes", (object)(this.m_szBeschreibung ?? "")),
                new DbParam("@typ", (object)(this.m_szKollektortyp ?? "")),
                new DbParam("@mfl", this.m_Modulfläche),
                new DbParam("@afl", this.m_Aperturfläche),
                new DbParam("@h0", this.m_h0),
                new DbParam("@k1", this.m_k1),
                new DbParam("@k2", this.m_k2),
                new DbParam("@kdir", this.m_Kdir),
                new DbParam("@kdfu", this.m_Kdfu),
                new DbParam("@inv", this.m_Kosten),
                new DbParam("@vor", (int)this.m_Vorlauf),
                new DbParam("@rue", (int)this.m_Ruecklauf),
                new DbParam("@ro", false)
            };

            bool ok = DataRepository.ExecuteSQL(sql, ps);
            if (ok) this.m_ID = neueId;
            return ok;
        }

        // Aktualisiert den Datensatz (Schluessel = Bezeichner = this.m_szKollektorname).
        // Vorlauf/Ruecklauf werden bewusst NICHT ueberschrieben (nicht im Editor vorhanden).
        public bool UpdateFrom(SolarkollektorenModel m)
        {
            if (m != null) CopyFrom(m);

            if (IsReadOnlyStatic(this.m_szKollektorname))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt");
                return false;
            }

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Firma = ?, Beschreibung = ?, Kollektortyp = ?, Modulflaeche = ?, Aperturflaeche = ?,
                            h0 = ?, k1 = ?, k2 = ?, Kdir = ?, Kdfu = ?, Investitionskosten = ?,
                            Vorlauf = ?, Ruecklauf = ?
                          WHERE Bezeichner = ?";

            DbParam[] ps = {
                new DbParam("@fir", (object)(this.m_szFirma ?? "")),
                new DbParam("@bes", (object)(this.m_szBeschreibung ?? "")),
                new DbParam("@typ", (object)(this.m_szKollektortyp ?? "")),
                new DbParam("@mfl", this.m_Modulfläche),
                new DbParam("@afl", this.m_Aperturfläche),
                new DbParam("@h0", this.m_h0),
                new DbParam("@k1", this.m_k1),
                new DbParam("@k2", this.m_k2),
                new DbParam("@kdir", this.m_Kdir),
                new DbParam("@kdfu", this.m_Kdfu),
                new DbParam("@inv", this.m_Kosten),
                new DbParam("vl", this.m_Vorlauf),
                new DbParam("rl", this.m_Ruecklauf),
                new DbParam("@bez", this.m_szKollektorname ?? "")
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        /// <summary>
        /// Import-Ueberschreiben (Dublettenkonzept 4.2): aktualisiert GENAU die Felder,
        /// die der VDI-Import liefert, adressiert per ID. Vom Anwender gepflegte Felder
        /// (Bezeichner, Beschreibung, Investitionskosten, ReadOnly) bleiben unangetastet.
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
                            Firma = ?, Kollektortyp = ?, Modulflaeche = ?, Aperturflaeche = ?,
                            h0 = ?, k1 = ?, k2 = ?, Kdir = ?, Kdfu = ?,
                            Vorlauf = ?, Ruecklauf = ?
                          WHERE ID = ?";

            DbParam[] ps = {
                new DbParam("@fir", (object)(this.m_szFirma ?? "")),
                new DbParam("@typ", (object)(this.m_szKollektortyp ?? "")),
                new DbParam("@mfl", this.m_Modulfläche),
                new DbParam("@afl", this.m_Aperturfläche),
                new DbParam("@h0", this.m_h0),
                new DbParam("@k1", this.m_k1),
                new DbParam("@k2", this.m_k2),
                new DbParam("@kdir", this.m_Kdir),
                new DbParam("@kdfu", this.m_Kdfu),
                new DbParam("@vor", (int)this.m_Vorlauf),
                new DbParam("@rue", (int)this.m_Ruecklauf),
                new DbParam("@id", id)
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Delete(string szName)
        {
            if (IsReadOnlyStatic(szName))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt");
                return false;
            }

            string sql = "DELETE FROM [" + TABLE + "] WHERE Bezeichner = ?";
            return DataRepository.ExecuteSQL(sql, new DbParam("@bez", szName ?? ""));
        }

        // --- MAPPING ---

        private void CopyFrom(SolarkollektorenModel m)
        {
            this.m_szKollektorname = m.m_szKollektorname;
            this.m_szFirma = m.m_szFirma;
            this.m_szBeschreibung = m.m_szBeschreibung;
            this.m_szKollektortyp = m.m_szKollektortyp;
            this.m_Modulfläche = m.m_Modulfläche;
            this.m_Aperturfläche = m.m_Aperturfläche;
            this.m_h0 = m.m_h0;
            this.m_k1 = m.m_k1;
            this.m_k2 = m.m_k2;
            this.m_Kdir = m.m_Kdir;
            this.m_Kdfu = m.m_Kdfu;
            this.m_Kosten = m.m_Kosten;
            this.m_Vorlauf = m.m_Vorlauf;
            this.m_Ruecklauf = m.m_Ruecklauf;
        }

        private static bool ReadOnlyOf(DataRow row)
        {
            return row.Table.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);
        }

        private static double D(DataRow row, string col)
        {
            return (row.Table.Columns.Contains(col) && row[col] != DBNull.Value) ? Convert.ToDouble(row[col]) : 0.0;
        }

        private static string S(DataRow row, string col)
        {
            return (row.Table.Columns.Contains(col) && row[col] != DBNull.Value) ? row[col].ToString() : "";
        }

        private static void FillFromRow(SolarkollektorenModel m, DataRow row)
        {
            if (row.Table.Columns.Contains("ID") && row["ID"] != DBNull.Value) m.m_ID = Convert.ToInt32(row["ID"]);
            m.m_szKollektorname = S(row, "Bezeichner");
            m.m_szFirma = S(row, "Firma");
            m.m_szBeschreibung = S(row, "Beschreibung");
            m.m_szKollektortyp = S(row, "Kollektortyp");
            m.m_Modulfläche = D(row, "Modulflaeche");
            m.m_Aperturfläche = D(row, "Aperturflaeche");
            m.m_h0 = D(row, "h0");
            m.m_k1 = D(row, "k1");
            m.m_k2 = D(row, "k2");
            m.m_Kdir = D(row, "Kdir");
            m.m_Kdfu = D(row, "Kdfu");
            m.m_Kosten = D(row, "Investitionskosten");
            m.m_Vorlauf = D(row, "Vorlauf");
            m.m_Ruecklauf = D(row, "Ruecklauf");
        }

        private SolarkollektorenModel MapRowToModel(DataRow row)
        {
            SolarkollektorenModel m = new SolarkollektorenModel();
            FillFromRow(m, row);
            return m;
        }
    }
}
