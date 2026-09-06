using System;
using System.Collections.Generic;
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

        /// <summary>
        /// <b>Der Schreibweg des Katalogimports</b> (iU9-W13.0e): Duplikatpruefung und
        /// Einfuegen in EINER Transaktion.
        ///
        /// <para><b>Was sich gegenueber dem Bestand aendert.</b> Die Klammer — und
        /// dass die Pruefung hier steht statt als konkateniertes inline-SQL in
        /// <c>Form_SolarKollektoren_einlesen:225</c>. <see cref="Exists"/> gab es
        /// bereits, die Maske rief es nur nicht.</para>
        /// </summary>
        public VdiUebernahmeErgebnis ImportUebernehmen(SolarkollektorenModel model, string nameOverride = null)
        {
            if (model == null) return VdiUebernahmeErgebnis.Fehler;

            try
            {
                string bezeichner = nameOverride ?? model.m_szKollektorname;

                using (DbVorgang v = DataRepository.Vorgang())
                {
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
                            (ID, Bezeichner, Firma, Beschreibung, Kollektortyp, Modulflaeche, Aperturflaeche,
                             h0, k1, k2, Kdir, Kdfu, Investitionskosten, Vorlauf, Ruecklauf, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                    DbParam[] ps = {
                        new DbParam("@id", neueId),
                        new DbParam("@bez", bezeichner ?? ""),
                        new DbParam("@fir", (object)(model.m_szFirma ?? "")),
                        new DbParam("@bes", (object)(model.m_szBeschreibung ?? "")),
                        new DbParam("@typ", (object)(model.m_szKollektortyp ?? "")),
                        new DbParam("@mfl", model.m_Modulfläche),
                        new DbParam("@afl", model.m_Aperturfläche),
                        new DbParam("@h0", model.m_h0),
                        new DbParam("@k1", model.m_k1),
                        new DbParam("@k2", model.m_k2),
                        new DbParam("@kdir", model.m_Kdir),
                        new DbParam("@kdfu", model.m_Kdfu),
                        new DbParam("@inv", model.m_Kosten),
                        new DbParam("@vor", (int)model.m_Vorlauf),
                        new DbParam("@rue", (int)model.m_Ruecklauf),
                        new DbParam("@ro", false)
                    };

                    v.Ausfuehren(sql, ps);
                    v.Commit();
                    this.m_ID = neueId;
                    return VdiUebernahmeErgebnis.Gespeichert;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei der Übernahme des Solarkollektors: " + ex.Message);
                return VdiUebernahmeErgebnis.Fehler;
            }
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
        //
        // BERICHTIGT AM 06.09.2026 (W6-E-4): Hier stand "Vorlauf/Ruecklauf werden
        // bewusst NICHT ueberschrieben (nicht im Editor vorhanden)". Das stimmt seit
        // dem Umzug des Editors nach EPOS.UI nicht mehr - SolarkollektorKatalogDialog
        // fuehrt beide Felder (leer erlaubt, leer = 0), die SET-Liste unten schreibt
        // sie, und ImportUebernehmen/InsertFrom tun dasselbe. Der Katalog ist die Quelle
        // der Vorbelegung aus AnlagenTemperaturen.
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

        // =================================================================================
        // W14a.0c - Katalogliste und Detailblock des Katalogbrowsers
        // =================================================================================

        /// <summary>
        /// Eine Zeile der Katalogliste — die fuenf Werte, aus denen
        /// <c>Form_SolarKollektorenAdmin.SetDBList</c> (Z. 89-99) seine zweispaltige
        /// Rasterzeile baut.
        /// </summary>
        /// <param name="Id">Primaerschluessel im Katalog.</param>
        /// <param name="Bezeichner">Erste Spalte.</param>
        /// <param name="Firma">Erste Zeile der Spalte „Eigenschaften".</param>
        /// <param name="Kollektortyp">Zweite Zeile.</param>
        /// <param name="Aperturflaeche">Dritte Zeile [m²].</param>
        public sealed record KatalogZeile(int Id, string Bezeichner, string Firma,
                                          string Kollektortyp, double Aperturflaeche);

        /// <summary>
        /// Der vollstaendige Kollektorkatalog, nach Bezeichner sortiert — die Liste des
        /// Katalogbrowsers.
        /// </summary>
        /// <remarks>
        /// <para>Der Vorlaeufer nahm <see cref="ReadAll"/> und baute die zweite Spalte in
        /// der Maske zusammen (Z. 96), samt der beiden deutschen Literale
        /// „Kollektortyp: " und „Aperturfläche: " IM DATENSTROM. Hier kommen die Werte,
        /// die Beschriftungen stehen als <see cref="KatalogBrowserProfil.Zeilenbauplan"/>
        /// im Profil und damit im Textkatalog.</para>
        /// <para>Diese Auspraegung kennt KEINEN Filter — <c>SetDBList(szFilter)</c> wurde
        /// von allen drei Aufrufern leer gelassen (Befund W14-B18); der Parameter faellt
        /// deshalb ersatzlos weg.</para>
        /// </remarks>
        public static IReadOnlyList<KatalogZeile> KatalogZeilen()
        {
            var liste = new List<KatalogZeile>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, Firma, Kollektortyp, Aperturflaeche FROM [" + TABLE +
                "] ORDER BY Bezeichner");
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
            {
                if (row["ID"] == null || row["ID"] == DBNull.Value) continue;
                liste.Add(new KatalogZeile(
                    Convert.ToInt32(row["ID"]),
                    Feld(row, "Bezeichner"),
                    Feld(row, "Firma"),
                    Feld(row, "Kollektortyp"),
                    row["Aperturflaeche"] == DBNull.Value ? 0 : Convert.ToDouble(row["Aperturflaeche"])));
            }
            return liste;
        }

        /// <summary>
        /// Die acht Anzeigefelder eines Katalogsatzes, bereits als Text — der Detailblock
        /// von <c>Form_SolarKollektorenAdmin.dataGridView1_Click</c> (Z. 101-123).
        /// <c>null</c>, wenn es den Bezeichner nicht gibt.
        /// </summary>
        /// <remarks>
        /// <para>Der Vorlaeufer baute sein SQL per Textverkettung (Z. 107, Befund
        /// W14-B12); hier steht <see cref="DbParam"/>. Die Zahlen kommen ROH wie im
        /// Bestand (<c>rs.Read(...).ToString()</c>), ohne Format.</para>
        /// <para><b>Befund W14a-B78 (Feldkarte, 04.09.2026).</b> Die Maske hat ZWEI
        /// Flaechenfelder: <c>textBox_Kollektor_A</c> („Kollektorfläche") und
        /// <c>textBox_Modul_A</c> („Aperturfläche"). Das erste wird im ganzen Bestand nie
        /// gefuellt; das zweite bekommt in Z. 117 die Modulflaeche und in Z. 118 sofort
        /// danach die Aperturflaeche (Befund W14-B15). Woertlich uebernommen heisst das:
        /// <see cref="KatalogBrowserProfil.FeldModulflaeche"/> („Kollektorfläche") bleibt
        /// LEER, <see cref="KatalogBrowserProfil.FeldAperturflaeche"/> traegt die
        /// Aperturflaeche. Entscheide E-2 und E-11.</para>
        /// </remarks>
        public static IReadOnlyDictionary<string, string> KatalogsatzAnzeige(string szName)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@bez", szName ?? ""));
            if (dt == null || dt.Rows.Count == 0) return null;

            DataRow r = dt.Rows[0];
            var werte = new Dictionary<string, string>(StringComparer.Ordinal);

            werte[KatalogBrowserProfil.FeldBezeichner] = Feld(r, "Bezeichner");
            werte[KatalogBrowserProfil.FeldKollektortyp] = Feld(r, "Kollektortyp");
            werte[KatalogBrowserProfil.FeldFirma] = Feld(r, "Firma");
            werte[KatalogBrowserProfil.FeldBeschreibung] = Feld(r, "Beschreibung");

            // W14a-B78: bleibt leer, genau wie im Bestand.
            werte[KatalogBrowserProfil.FeldModulflaeche] = "";
            werte[KatalogBrowserProfil.FeldAperturflaeche] = Feld(r, "Aperturflaeche");
            werte[KatalogBrowserProfil.FeldVorlauf] = Feld(r, "Vorlauf");
            werte[KatalogBrowserProfil.FeldRuecklauf] = Feld(r, "Ruecklauf");

            return werte;
        }

        /// <summary>Feldwert als Text; fehlende Spalte und <c>NULL</c> ergeben „".</summary>
        private static string Feld(DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte)) return "";
            object v = row[spalte];
            return (v == null || v == DBNull.Value) ? "" : v.ToString();
        }
    }
}
