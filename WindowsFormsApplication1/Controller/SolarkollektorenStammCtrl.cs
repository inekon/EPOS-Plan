using System;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

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
            DataTable dt = DataRepository.GetDataTable(sql, new OleDbParameter("@bez", szName ?? (object)DBNull.Value));

            rows = 0;
            if (dt != null && dt.Rows.Count > 0)
            {
                FillFromRow(this, dt.Rows[0]);
                this.m_bReadOnly = ReadOnlyOf(dt.Rows[0]);
                items = new SolarkollektorenModel[1] { this };
                rows = 1;
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

        public bool InsertFrom(SolarkollektorenModel m)
        {
            if (m != null) CopyFrom(m);

            int neueId = DataRepository.GetMaxID(TABLE) + 1;

            string sql = @"INSERT INTO [" + TABLE + @"]
                            (ID, Bezeichner, Firma, Beschreibung, Kollektortyp, Modulflaeche, Aperturflaeche,
                             h0, k1, k2, Kdir, Kdfu, Investitionskosten, Vorlauf, Ruecklauf, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            OleDbParameter[] ps = {
                new OleDbParameter("@id", neueId),
                new OleDbParameter("@bez", this.m_szKollektorname ?? ""),
                new OleDbParameter("@fir", (object)(this.m_szFirma ?? "")),
                new OleDbParameter("@bes", (object)(this.m_szBeschreibung ?? "")),
                new OleDbParameter("@typ", (object)(this.m_szKollektortyp ?? "")),
                new OleDbParameter("@mfl", this.m_Modulfläche),
                new OleDbParameter("@afl", this.m_Aperturfläche),
                new OleDbParameter("@h0", this.m_h0),
                new OleDbParameter("@k1", this.m_k1),
                new OleDbParameter("@k2", this.m_k2),
                new OleDbParameter("@kdir", this.m_Kdir),
                new OleDbParameter("@kdfu", this.m_Kdfu),
                new OleDbParameter("@inv", this.m_Kosten),
                new OleDbParameter("@vor", (int)this.m_Vorlauf),
                new OleDbParameter("@rue", (int)this.m_Ruecklauf),
                new OleDbParameter("@ro", false)
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
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Firma = ?, Beschreibung = ?, Kollektortyp = ?, Modulflaeche = ?, Aperturflaeche = ?,
                            h0 = ?, k1 = ?, k2 = ?, Kdir = ?, Kdfu = ?, Investitionskosten = ?,
                            Vorlauf = ?, Ruecklauf = ?
                          WHERE Bezeichner = ?";

            OleDbParameter[] ps = {
                new OleDbParameter("@fir", (object)(this.m_szFirma ?? "")),
                new OleDbParameter("@bes", (object)(this.m_szBeschreibung ?? "")),
                new OleDbParameter("@typ", (object)(this.m_szKollektortyp ?? "")),
                new OleDbParameter("@mfl", this.m_Modulfläche),
                new OleDbParameter("@afl", this.m_Aperturfläche),
                new OleDbParameter("@h0", this.m_h0),
                new OleDbParameter("@k1", this.m_k1),
                new OleDbParameter("@k2", this.m_k2),
                new OleDbParameter("@kdir", this.m_Kdir),
                new OleDbParameter("@kdfu", this.m_Kdfu),
                new OleDbParameter("@inv", this.m_Kosten),
                new OleDbParameter("vl", this.m_Vorlauf),
                new OleDbParameter("rl", this.m_Ruecklauf),
                new OleDbParameter("@bez", this.m_szKollektorname ?? "")
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Delete(string szName)
        {
            if (IsReadOnlyStatic(szName))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            string sql = "DELETE FROM [" + TABLE + "] WHERE Bezeichner = ?";
            return DataRepository.ExecuteSQL(sql, new OleDbParameter("@bez", szName ?? ""));
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
