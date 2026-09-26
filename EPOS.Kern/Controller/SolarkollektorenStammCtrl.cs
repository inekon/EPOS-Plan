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
        /// Ein Katalogsatz ueber seine ID (iU9-W7.0e) — der Weg, auf dem die Huelle
        /// beim Aufnehmen den Stammsatz der neuen Projektzeile nachschlaegt.
        ///
        /// <para><b>Der Katalog fuehrt keine Temperaturen.</b> Vor- und Ruecklauf eines
        /// Solarkollektors haben keinen Rechenweg (Ertrag mit fester Speichertemperatur,
        /// <c>SimulationSolarthermie.Kollektorfelder_Lesen</c>); die Spalten sind mit
        /// Schemaschritt <see cref="SolarkollektorTemperaturen.SCHRITT"/> entfallen.</para>
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
                             h0, k1, k2, Kdir, Kdfu, Investitionskosten, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

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
                             h0, k1, k2, Kdir, Kdfu, Investitionskosten, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

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
                new DbParam("@ro", false)
            };

            bool ok = DataRepository.ExecuteSQL(sql, ps);
            if (ok) this.m_ID = neueId;
            return ok;
        }

        // Aktualisiert den Datensatz (Schluessel = Bezeichner = this.m_szKollektorname).
        // Der Katalog fuehrt keine Vor- und Ruecklauftemperatur (Schemaschritt
        // SolarkollektorTemperaturen.SCHRITT) - sie hatten keinen Rechenweg.
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
                            h0 = ?, k1 = ?, k2 = ?, Kdir = ?, Kdfu = ?, Investitionskosten = ?
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
                            h0 = ?, k1 = ?, k2 = ?, Kdir = ?, Kdfu = ?
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

        // =================================================================================
        // W14a-E-10 / S1.5 - die Zeilen der KATALOGVERWALTUNG mit ihren Parameterspalten
        // =================================================================================

        // --- DUPLIZIEREN (Konzept Administrationsdialoge, Entscheid AD-Q11) ---

        /// <summary>
        /// <b>„Duplizieren…"</b>: kopiert den Solarkollektorsatz <paramref name="id"/> als EIGENEN
        /// Satz unter <paramref name="neuerName"/> — <c>ReadOnly = 0</c>, alle übrigen
        /// Spalten wie im Original (Entscheid <b>AD-Q11</b> vom 23.09.2026:
        /// Auslieferungssätze werden nie überschrieben; wer einen ändern will, dupliziert
        /// ihn).
        /// </summary>
        /// <remarks>
        /// Die Regel steht einmal in <see cref="Katalogkopie.Duplizieren"/>; hier steht nur,
        /// WELCHE Tabelle es ist.
        /// </remarks>
        public static Katalogkopie.Ergebnis Duplizieren(int id, string neuerName)
        {
            return Katalogkopie.Duplizieren(TABLE, id, neuerName);
        }

        /// <summary>
        /// <b>„Schloss setzen…" / „Schloss aufheben…"</b> (Entscheid AD-Q15): schaltet das
        /// Auslieferungskennzeichen der Sätze <paramref name="ids"/> — nur den Kopfsatz, kein
        /// Wert ändert sich. Die Regel steht einmal in
        /// <see cref="Auslieferungskennzeichen.SetzenInTabelle"/>; hier steht nur die Tabelle.
        /// </summary>
        public static Auslieferungskennzeichen.Ergebnis SchlossSetzen(IReadOnlyList<int> ids, bool gesperrt)
            => Auslieferungskennzeichen.SetzenInTabelle(TABLE, ids, gesperrt);

        /// <summary>
        /// <b>Die Zeilen der Katalogverwaltung</b> (Anwenderentscheid W14a-E-10,
        /// Konzept_Katalogfilter 4.4 und S1.5) — SECHS Spalten: Bezeichner, Hersteller,
        /// Kollektortyp, Aperturflaeche, η₀ und k1.
        ///
        /// <para><b>Der erste Filter dieses Katalogs ueberhaupt.</b> Die Auspraegung
        /// kannte bis hierher <c>KatalogFilterArt.Keiner</c> — keine Klappliste, keine
        /// Suche, eine Namensspalte. Sieben Saetze halten das aus; nach einem
        /// VDI-3805-Import sind es Hunderte.</para>
        ///
        /// <para><b>Gerechnet wird mit der APERTUR</b>, nicht mit der Modulflaeche
        /// (<c>SimulationSolarthermie.cs:232</c>) — deshalb steht sie in der Spalte.
        /// η₀ ist der Konversionsfaktor <c>h0</c> (<c>:242</c>), k1 der lineare
        /// Verlustbeiwert (<c>:243</c>).</para>
        /// </summary>
        public static IReadOnlyList<Katalogfilterzeile> Katalogfilterzeilen()
        {
            var liste = new List<Katalogfilterzeile>();

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, Bezeichner, Firma, Kollektortyp, Aperturflaeche, h0, k1, ReadOnly FROM [" +
                TABLE + "] ORDER BY Bezeichner");
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                string bezeichner = Katalogfeld.Text(r, "Bezeichner");

                var zeile = new Katalogfilterzeile(Katalogfeld.Ganzzahl(r, "ID"), bezeichner)
                {
                    Geschuetzt = Katalogfeld.Kennzeichen(r, "ReadOnly")
                };

                liste.Add(zeile
                    .MitText(Katalogfilterprofil.SpBezeichner, bezeichner)
                    .MitText(Katalogfilterprofil.SpHersteller, Katalogfeld.Text(r, "Firma"))
                    .MitText(Katalogfilterprofil.SpKollektortyp, Katalogfeld.Text(r, "Kollektortyp"))
                    .MitZahl(Katalogfilterprofil.SpApertur, Katalogfeld.Zahl(r, "Aperturflaeche"), 2)
                    .MitZahl(Katalogfilterprofil.SpEtaNull, Katalogfeld.Zahl(r, "h0"), 3)
                    .MitZahl(Katalogfilterprofil.SpK1, Katalogfeld.Zahl(r, "k1"), 3));
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

            // W14a-B78 ABGELOEST am 15.09.2026: Hier stand "" - die leere
            // „Kollektorfläche" des Vorlaeufers. Das war richtig, solange der Block nur
            // ANZEIGTE; mit dem Speicherweg wuerde die Leere beim ersten Speichern die
            // gespeicherte Modulflaeche auf 0 setzen. Gezeigt wird deshalb der Wert.
            werte[KatalogBrowserProfil.FeldModulflaeche] = Feld(r, "Modulflaeche");
            werte[KatalogBrowserProfil.FeldAperturflaeche] = Feld(r, "Aperturflaeche");

            // --- Der volle Feldbestand (Anwenderentscheid 15.09.2026) ---
            werte[KatalogBrowserProfil.FeldH0] = Feld(r, "h0");
            werte[KatalogBrowserProfil.FeldK1] = Feld(r, "k1");
            werte[KatalogBrowserProfil.FeldK2] = Feld(r, "k2");
            werte[KatalogBrowserProfil.FeldKdir] = Feld(r, "Kdir");
            werte[KatalogBrowserProfil.FeldKdiff] = Feld(r, "Kdfu");
            werte[KatalogBrowserProfil.FeldInvestitionskosten] = Feld(r, "Investitionskosten");

            return werte;
        }

        /// <summary>Feldwert als Text; fehlende Spalte und <c>NULL</c> ergeben „".</summary>
        private static string Feld(DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte)) return "";
            object v = row[spalte];
            return (v == null || v == DBNull.Value) ? "" : v.ToString();
        }

        // =================================================================================
        // Der Speicherweg des Katalog-Aufklappers (Anwenderentscheid 15.09.2026)
        // =================================================================================

        /// <summary>Das Ergebnis eines Schreibversuchs — wie in den sechs Nachbarn.</summary>
        /// <param name="Ok">Wurde geschrieben?</param>
        /// <param name="Meldung">Der Grund im Klartext, bereits lokalisiert.</param>
        /// <param name="Name">Der Bezeichner, unter dem der Satz jetzt steht.</param>
        public sealed record SpeicherErgebnis(bool Ok, string Meldung, string Name);

        /// <summary>
        /// Die elf editierbaren Felder eines Katalogsatzes — jede fachliche Spalte
        /// ausser dem Bezeichner, der der Schluessel des <c>UPDATE</c> ist.
        /// </summary>
        /// <remarks>
        /// Anders als bei Heizkessel und BHKW sind hier ALLE Felder Pflicht: Der
        /// Datensatz ist neu, es gibt keinen Aufrufer aus der Zeit davor, der eine
        /// Leerstelle brauchte.
        /// </remarks>
        public sealed record AnzeigefelderSolarkollektor(string Kollektortyp, string Firma,
                                                         string Beschreibung,
                                                         double Modulflaeche,
                                                         double Aperturflaeche,
                                                         double H0, double K1, double K2,
                                                         double Kdir, double Kdiff,
                                                         double Investitionskosten);

        /// <summary>
        /// Schreibt die Anzeigefelder in den Katalogsatz zurueck — der Weg des Knopfes
        /// „Speichern" im Aufklapper „Alle Daten anzeigen".
        /// </summary>
        /// <remarks>
        /// <para><b>Muster <c>HeizkesselStammCtrl.AnzeigefelderSchreiben</c></b>, in
        /// derselben Reihenfolge: Dublettenklammer, Satz VOLLSTAENDIG lesen, nur die
        /// angezeigten Felder aendern, schreiben. Der Grund einer Ablehnung kommt als
        /// Text zurueck statt als Meldungsfenster — eine Razor-Komponente hat keines.</para>
        /// <para><b>Der Schreibschutz wird VOR dem Schreiben gefragt</b>, nicht in
        /// <see cref="UpdateFrom"/> abgewartet: Jene Methode zeigt ihn ueber
        /// <c>Meldung.Hinweis</c>, und ein Fenster ist hier keine Antwort. Dieselbe
        /// Reihenfolge wie in <c>PufferSpStammCtrl.Ueberschreiben</c>.</para>
        /// </remarks>
        public static SpeicherErgebnis AnzeigefelderSchreiben(string bezeichner,
                                                              AnzeigefelderSolarkollektor felder)
        {
            if (string.IsNullOrWhiteSpace(bezeichner) || felder == null)
                return new SpeicherErgebnis(false, Text("SKK_MSG_FEHLER",
                    "Fehler beim Überschreiben des Datensatzes!"), "");

            try
            {
                // Dublettenklammer: UpdateFrom filtert auf den Bezeichner, und der
                // Katalog fuehrt darauf keinen eindeutigen Schluessel. Bei einer
                // Dublette wuerden beide Saetze zugleich ueberschrieben.
                object anz = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ?",
                    new DbParam("@bez", bezeichner));
                int anzahl = (anz == null || anz == DBNull.Value) ? 0 : Convert.ToInt32(anz);
                if (anzahl > 1)
                    return new SpeicherErgebnis(false,
                        string.Format(MyResource.Resource.ADM_MEHRDEUTIG_TEXT, bezeichner, anzahl), "");

                if (IsReadOnlyStatic(bezeichner))
                    return new SpeicherErgebnis(false, Text("SKK_MSG_SCHUTZ",
                        "Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden."), "");

                var schreiber = new SolarkollektorenStammCtrl();
                schreiber.ReadSingle(bezeichner);
                if (schreiber.rows == 0)
                    return new SpeicherErgebnis(false, Text("SKK_MSG_FEHLER",
                        "Fehler beim Überschreiben des Datensatzes!"), "");

                string verstoss = FelderUebernehmen(schreiber, felder);
                if (!string.IsNullOrEmpty(verstoss))
                    return new SpeicherErgebnis(false, verstoss, "");

                if (!schreiber.UpdateFrom(null))
                    return new SpeicherErgebnis(false, Text("SKK_MSG_FEHLER",
                        "Fehler beim Überschreiben des Datensatzes!"), "");

                return new SpeicherErgebnis(true,
                    Text("SKK_MSG_GESPEICHERT", "Datensatz gespeichert"),
                    schreiber.m_szKollektorname);
            }
            catch
            {
                return new SpeicherErgebnis(false, Text("SKK_MSG_FEHLER",
                    "Fehler beim Überschreiben des Datensatzes!"), "");
            }
        }

        /// <summary>
        /// Prueft die Werte und traegt sie in den GELESENEN Satz ein; der Rueckgabewert
        /// ist der Ablehnungsgrund im Klartext oder <c>null</c>. Erst pruefen, dann
        /// setzen — nach einer Ablehnung steht kein halb geaenderter Satz im Speicher.
        /// </summary>
        private static string FelderUebernehmen(SolarkollektorenStammCtrl satz,
                                                AnzeigefelderSolarkollektor f)
        {
            const KatalogBrowserArt art = KatalogBrowserArt.Solarkollektoren;

            string grund = KatalogFeldPruefung.ErsterGrund(
                KatalogFeldPruefung.NichtNegativ(art, KatalogBrowserProfil.FeldModulflaeche,
                                                 f.Modulflaeche),
                KatalogFeldPruefung.NichtNegativ(art, KatalogBrowserProfil.FeldAperturflaeche,
                                                 f.Aperturflaeche),
                KatalogFeldPruefung.NichtNegativ(art, KatalogBrowserProfil.FeldInvestitionskosten,
                                                 f.Investitionskosten),

                // h0 ist der KONVERSIONSFAKTOR und liegt zwischen 0 und 1 - wer dort
                // 76,1 eintraegt, meint Prozent und soll es hoeren
                // (SimulationSolarthermie.cs:242).
                KatalogFeldPruefung.ImBereich(art, KatalogBrowserProfil.FeldH0, f.H0, 0, 1),

                // Kdir und Kdiff sind EINFALLSWINKELKORREKTUREN und duerfen ueber 1
                // liegen: Ein Vakuumroehrenkollektor sammelt schraeg mehr als senkrecht
                // (der Auslieferungskatalog fuehrt 1,27). Nur negativ geht nicht.
                KatalogFeldPruefung.NichtNegativ(art, KatalogBrowserProfil.FeldKdir, f.Kdir),
                KatalogFeldPruefung.NichtNegativ(art, KatalogBrowserProfil.FeldKdiff, f.Kdiff),

                KatalogFeldPruefung.NichtNegativ(art, KatalogBrowserProfil.FeldK1, f.K1),
                KatalogFeldPruefung.NichtNegativ(art, KatalogBrowserProfil.FeldK2, f.K2));
            if (!string.IsNullOrEmpty(grund)) return grund;

            satz.m_szKollektortyp = f.Kollektortyp ?? "";
            satz.m_szFirma = f.Firma ?? "";
            satz.m_szBeschreibung = f.Beschreibung ?? "";
            satz.m_Modulfläche = f.Modulflaeche;
            satz.m_Aperturfläche = f.Aperturflaeche;
            satz.m_h0 = f.H0;
            satz.m_k1 = f.K1;
            satz.m_k2 = f.K2;
            satz.m_Kdir = f.Kdir;
            satz.m_Kdfu = f.Kdiff;
            satz.m_Kosten = f.Investitionskosten;

            return null;
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
