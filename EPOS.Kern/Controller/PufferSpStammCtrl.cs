using System;
using System.Collections.Generic;
using System.Data;

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
                new DbParam("@id", id));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        /// <summary>
        /// <b>Der Schreibweg des Katalogimports</b> (iU9-W13.0e): Duplikatpruefung und
        /// Einfuegen in EINER Transaktion.
        ///
        /// <para><b>Was sich gegenueber dem Bestand aendert.</b> Nur die Klammer.
        /// <c>Form_PufferSp_einlesen.UebernehmeEintrag</c> rief <see cref="Exists"/>
        /// und <see cref="InsertFrom"/> nacheinander ueber ZWEI Verbindungen; wer
        /// dazwischen denselben Bezeichner anlegte, bekam ihn zweimal. Konzept 6.3
        /// verlangt die Klammer ausdruecklich („Pruefung und Schreiben je Eintrag
        /// klammern; heute nur beim Heizkessel der Fall").</para>
        /// </summary>
        public VdiUebernahmeErgebnis ImportUebernehmen(PufferSpModel model, string nameOverride = null)
        {
            if (model == null) return VdiUebernahmeErgebnis.Fehler;

            try
            {
                string bezeichner = nameOverride ?? model.Name;

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
                            (ID, Bezeichner, Hersteller, Speichertyp, Bereitschaftsverluste, Gesamtvolumen, Investitionskosten, ReadOnly)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

                    DbParam[] ps = {
                        new DbParam("@id", neueId),
                        new DbParam("@bez", bezeichner ?? ""),
                        new DbParam("@her", (object)(model.Firma ?? "")),
                        new DbParam("@typ", (object)(model.Speichertyp ?? "")),
                        new DbParam("@bbv", model.Betriebsbereitschaftverlust),
                        new DbParam("@vol", model.Gesamtvolumen),
                        new DbParam("@inv", model.Investitionskosten),
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
                Console.WriteLine("Fehler bei der Übernahme des Pufferspeichers: " + ex.Message);
                return VdiUebernahmeErgebnis.Fehler;
            }
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

            DbParam[] ps = {
                new DbParam("@id", neueId),
                new DbParam("@bez", this.Name ?? ""),
                new DbParam("@her", (object)(this.Firma ?? "")),
                new DbParam("@typ", (object)(this.Speichertyp ?? "")),
                new DbParam("@ver", this.Betriebsbereitschaftverlust),
                new DbParam("@vol", this.Gesamtvolumen),
                new DbParam("@inv", this.Investitionskosten),
                new DbParam("@ro", false)
            };

            bool ok = DataRepository.ExecuteSQL(sql, ps);
            if (ok) this.ID = neueId;
            return ok;
        }

        public bool Update()
        {
            if (IsReadOnlyStatic(this.Name))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt");
                return false;
            }

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Hersteller = ?, Speichertyp = ?, Bereitschaftsverluste = ?,
                            Investitionskosten = ?, Gesamtvolumen = ?
                          WHERE Bezeichner = ?";

            DbParam[] ps = {
                new DbParam("@her", (object)(this.Firma ?? "")),
                new DbParam("@typ", (object)(this.Speichertyp ?? "")),
                new DbParam("@ver", this.Betriebsbereitschaftverlust),
                new DbParam("@inv", this.Investitionskosten),
                new DbParam("@vol", this.Gesamtvolumen),
                new DbParam("@bez", this.Name ?? "")
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

            DbParam[] ps = {
                new DbParam("@her", (object)(this.Firma ?? "")),
                new DbParam("@typ", (object)(this.Speichertyp ?? "")),
                new DbParam("@ver", this.Betriebsbereitschaftverlust),
                new DbParam("@vol", this.Gesamtvolumen),
                new DbParam("@id", id)
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
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt");
                return false;
            }

            string sql = "DELETE FROM [" + TABLE + "] WHERE ID = ?";
            return DataRepository.ExecuteSQL(sql, new DbParam("@id", id));
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

        // =================================================================================
        // W6.0c - Volumen- und Herstellerfilter der beiden Pufferspeicherdialoge
        // =================================================================================

        /// <summary>Eine Zeile der Katalogliste: Primaerschluessel und Bezeichner.</summary>
        public sealed record KatalogZeile(int Id, string Bezeichner);

        // =================================================================================
        // W10a.0b - die Katalogliste des PROJEKTdialogs
        // =================================================================================

        /// <summary>
        /// Eine Zeile der Katalogliste des Projektdialogs — die sieben Felder, die
        /// <c>Form_PufferSp_Projekt</c> aus dem Auslieferungskatalog uebernimmt.
        /// </summary>
        /// <param name="Id">Primaerschluessel im Katalog.</param>
        /// <param name="Bezeichner">Der Name, den die Klappliste zeigt.</param>
        /// <param name="Hersteller">Uebernahmefeld beim Speichern.</param>
        /// <param name="Speichertyp">Uebernahmefeld beim Speichern (leer = Bestand behalten).</param>
        /// <param name="Gesamtvolumen">Liter — fuellt das Volumenfeld.</param>
        /// <param name="Bereitschaftsverluste">kWh/24h — fuellt das Verlustfeld.</param>
        /// <param name="Investitionskosten">Uebernahmefeld beim Speichern.</param>
        public sealed record Katalogzeile(int Id, string Bezeichner, string Hersteller,
                                          string Speichertyp, int Gesamtvolumen,
                                          double Bereitschaftsverluste, double Investitionskosten);

        /// <summary>
        /// Der vollstaendige Auslieferungskatalog fuer den PROJEKTdialog, nach Bezeichner
        /// sortiert.
        ///
        /// <para><b>iU9‑W10a.0b (Befund W10‑B27).</b> Die Abfrage stand als inline-SQL in
        /// <c>Form_PufferSp_Projekt.KatalogLaden</c> :1139-1141 — in einer MASKE, wo der
        /// SQL-Dialektpruefer sie zwar findet, aber niemand sie wiederverwenden kann. Der
        /// Wortlaut ist unveraendert uebernommen, einschliesslich der Sortierung.</para>
        ///
        /// <para><b>Warum nicht <see cref="Filtern"/>.</b> Jene Methode liefert Id und
        /// Bezeichner fuer die gefilterte KATALOGverwaltung; der Projektdialog filtert
        /// nicht, uebernimmt dafuer aber fuenf weitere Felder in seine Eingabefelder.
        /// Zwei Fragen, zwei Abfragen.</para>
        /// </summary>
        public static IReadOnlyList<Katalogzeile> Katalogzeilen()
        {
            var liste = new List<Katalogzeile>();

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, Bezeichner, Hersteller, Speichertyp, Gesamtvolumen, Bereitschaftsverluste, " +
                "Investitionskosten FROM [" + TABLE + "] ORDER BY Bezeichner");
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                liste.Add(new Katalogzeile(
                    StilleDb.Zahl(StilleDb.Feld(r, "ID")),
                    StilleDb.Text(StilleDb.Feld(r, "Bezeichner")),
                    StilleDb.Text(StilleDb.Feld(r, "Hersteller")),
                    StilleDb.Text(StilleDb.Feld(r, "Speichertyp")),
                    StilleDb.Zahl(StilleDb.Feld(r, "Gesamtvolumen")),
                    StilleDb.Kommazahl(StilleDb.Feld(r, "Bereitschaftsverluste")),
                    StilleDb.Kommazahl(StilleDb.Feld(r, "Investitionskosten"))));
            }
            return liste;
        }

        /// <summary>
        /// SQL-Praedikat je Volumenstufe, Index 0 = „Alle".
        /// </summary>
        /// <remarks>
        /// <para>
        /// Umgezogen aus <c>WindowsFormsApplication1/Views/Pufferspeicher/PufferSpFilter.cs</c>
        /// (Paket 9 / L5) - dort hing die Tabelle an einer <c>ComboBox</c> und war damit
        /// fuer eine Razor-Komponente unerreichbar. Der Wortlaut der sechs Praedikate ist
        /// unveraendert, einschliesslich der NULL-Absicherung in Stufe 0:
        /// </para>
        /// <para>
        /// Der Bestandsausdruck <c>Gesamtvolumen Like '%'</c> wandelt die Zahl in Text und
        /// vergleicht; fuer <c>NULL</c> ergibt das wieder <c>NULL</c> - der Satz fiele aus
        /// „Alle" heraus. Ein Katalogsatz ohne gepflegtes Gesamtvolumen (etwa aus einem
        /// VDI-3805-Import) waere damit unsichtbar, ohne dass irgendwo eine Meldung
        /// erscheint. Die Klammer ist noetig, weil das Praedikat mit <c>and</c> an den
        /// Herstellerfilter gehaengt wird.
        /// </para>
        /// </remarks>
        public static readonly string[] VOLUMEN_SQL =
        {
            "(Gesamtvolumen IS NULL OR Gesamtvolumen Like '%')",
            "Gesamtvolumen <100",
            "Gesamtvolumen >=100 and Gesamtvolumen <200",
            "Gesamtvolumen >=200 and Gesamtvolumen <500",
            "Gesamtvolumen >=500 and Gesamtvolumen <1000",
            "Gesamtvolumen >=1000"
        };

        /// <summary>
        /// Die Anzeigetexte der sechs Filterstufen in derselben Reihenfolge wie
        /// <see cref="VOLUMEN_SQL"/> - der Index ist der Steuerwert.
        /// </summary>
        public static IReadOnlyList<string> VolumenTexte()
        {
            return new[]
            {
                MyResource.Resource.PSP_FILTER_ALLE,
                MyResource.Resource.PSP_FILTER_BIS_100L,
                MyResource.Resource.PSP_FILTER_100_BIS_200L,
                MyResource.Resource.PSP_FILTER_200_BIS_500L,
                MyResource.Resource.PSP_FILTER_500_BIS_1000L,
                MyResource.Resource.PSP_FILTER_UEBER_1000L
            };
        }

        /// <summary>
        /// Die Hersteller des Katalogs - die Auswahlliste <c>comboBox_Hersteller</c>.
        /// </summary>
        /// <remarks>
        /// <c>Form_PufferSp_Load</c> baute sie ueber <c>ReadAll</c> und
        /// <c>FindStringExact</c> zusammen, also ueber die Oberflaeche. Hier macht es die
        /// Datenbank; die Reihenfolge ist damit stabil statt an der Katalogsortierung
        /// haengend.
        /// </remarks>
        public static IReadOnlyList<string> Hersteller()
        {
            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Hersteller FROM " + TABLE + " GROUP BY Hersteller ORDER BY Hersteller");
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
            {
                string h = row["Hersteller"] == DBNull.Value ? "" : row["Hersteller"].ToString();
                if (h.Length > 0) liste.Add(h);
            }
            return liste;
        }

        /// <summary>
        /// Die Katalogliste, eingeengt auf Hersteller und Volumenstufe.
        /// </summary>
        /// <param name="hersteller">
        /// Leer, <c>null</c> und <c>PSP_FILTER_ALLE</c> heben die Einengung auf - dieselbe
        /// Regel wie <c>PufferSpFilter.HerstellerSql</c>.
        /// </param>
        /// <param name="volumenstufe">Index in <see cref="VOLUMEN_SQL"/>; alles ausserhalb
        /// gilt als 0 („Alle").</param>
        /// <remarks>
        /// Aus <c>Form_PufferSp.SetFilter</c> (Z. 300). Der Herstellername kommt als
        /// <see cref="DbParam"/> statt als eingesetzter Text mit verdoppeltem Hochkomma -
        /// dieselbe Wirkung, aber ohne Zeichenketten-Arithmetik.
        /// </remarks>
        public IReadOnlyList<KatalogZeile> Filtern(string hersteller, int volumenstufe)
        {
            if (volumenstufe < 0 || volumenstufe >= VOLUMEN_SQL.Length) volumenstufe = 0;
            string szFilterVolumen = VOLUMEN_SQL[volumenstufe];

            string h = (hersteller ?? "").Trim();
            bool alle = h.Length == 0 ||
                        string.Equals(h, MyResource.Resource.PSP_FILTER_ALLE,
                                      StringComparison.OrdinalIgnoreCase);

            string sql = alle
                ? "SELECT ID, Bezeichner FROM " + TABLE + " WHERE " + szFilterVolumen + " ORDER BY Bezeichner"
                : "SELECT ID, Bezeichner FROM " + TABLE + " WHERE Hersteller = ? and " + szFilterVolumen + " ORDER BY Bezeichner";

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
        /// Die sechs Anzeigefelder eines Speichers - der Detailblock des Projektdialogs.
        /// Die Zahlen kommen bereits als Text mit einer Nachkommastelle
        /// (<c>Form_PufferSp.FeldText</c>).
        /// </summary>
        public sealed record SpeicherDetail(string Bezeichner, string Hersteller, string Typ,
                                            string Bereitschaftsverluste, string Gesamtvolumen,
                                            string Investitionskosten);

        /// <summary>Die Feldliste beider Detailabfragen - je Tabelle dieselbe.</summary>
        internal const string DETAIL_FELDER =
            "SELECT Bezeichner, Hersteller, Speichertyp, Bereitschaftsverluste, " +
            "Gesamtvolumen, Investitionskosten FROM ";

        /// <summary>
        /// Die Anzeigefelder eines KATALOGsatzes ueber seinen Primaerschluessel;
        /// <c>null</c>, wenn es ihn nicht gibt.
        /// </summary>
        public static SpeicherDetail Detail(int id)
        {
            DataTable dt = DataRepository.GetDataTable(
                DETAIL_FELDER + TABLE + " WHERE ID=?", new DbParam("@id", id));
            return AusZeile(dt);
        }

        /// <summary>
        /// Die Anzeigefelder eines Katalogsatzes ueber seinen Bezeichner; <c>null</c>,
        /// wenn es ihn nicht gibt (<c>listBox_PufferSp_DB_SelectedIndexChanged</c>).
        /// </summary>
        public static SpeicherDetail Detail(string szName)
        {
            DataTable dt = DataRepository.GetDataTable(
                DETAIL_FELDER + TABLE + " WHERE Bezeichner=? ORDER BY ID",
                new DbParam("@nam", szName ?? ""));
            return AusZeile(dt);
        }

        /// <summary>
        /// Erste Zeile als <see cref="SpeicherDetail"/>; die Zahlen mit einer
        /// Nachkommastelle wie <c>Form_PufferSp.FeldText</c>.
        /// </summary>
        internal static SpeicherDetail AusZeile(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return null;
            DataRow row = dt.Rows[0];
            return new SpeicherDetail(
                FeldText(row, "Bezeichner"),
                FeldText(row, "Hersteller"),
                FeldText(row, "Speichertyp"),
                FeldText(row, "Bereitschaftsverluste"),
                FeldText(row, "Gesamtvolumen"),
                FeldText(row, "Investitionskosten"));
        }

        /// <summary>
        /// Feldwert als Text; NULL und fehlende Spalte ergeben eine leere Zeichenkette,
        /// Fliesskommazahlen bekommen eine Nachkommastelle. Wortgleich aus
        /// <c>Form_PufferSp.FeldText</c> uebernommen - die Oberflaeche zeigte diese Felder
        /// nur an und rechnete nicht mit ihnen.
        /// </summary>
        internal static string FeldText(DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte)) return "";
            object wert = row[spalte];
            if (wert == null || wert == DBNull.Value) return "";

            if (wert is double d) return d.ToString("0.0");
            if (wert is float f) return f.ToString("0.0");
            if (wert is decimal m) return m.ToString("0.0");

            return wert.ToString();
        }
    }
}
