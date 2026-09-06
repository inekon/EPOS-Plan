using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Controller des WECHSELRICHTERKATALOGS <c>Tab_Wechselrichter_STAMM</c>
    /// (Stufe S1 des <c>Konzept_Wechselrichter_EPOS-Plan.md</c>, Anwenderentscheid
    /// <b>W6‑E‑2</b> vom 06.09.2026).
    ///
    /// <para>Muster ist <see cref="PhotovoltaikStammCtrl"/>, Zeile für Zeile:
    /// <c>ReadAll</c>/<c>ReadSingle</c>, ein <c>Insert</c> mit ausdrücklicher ID
    /// (MAX+1) und <c>ReadOnly = false</c>, ein <c>Update</c>/<c>Delete</c> auf die ID
    /// mit ReadOnly-Sperre, dazu die Wege der Verwaltung
    /// (<see cref="Filtern"/>, <see cref="Hersteller"/>, <see cref="SpeichernAus"/>,
    /// <see cref="Loeschen"/>) und der eine Import-Schreibweg
    /// <see cref="UpdateImport"/>.</para>
    ///
    /// <para><b>Alles über <see cref="DbParam"/>, nichts verkettet.</b> Der
    /// Herstellerfilter der Verwaltung geht als Parameter hinein — der Vorläufer der
    /// PV-Seite baute <c>Firma='…'</c> zusammen, ohne das Hochkomma zu verdoppeln
    /// (Befund zu <c>PhotovoltaikStammCtrl.Filtern</c>).</para>
    ///
    /// <para><b>Keine Rechenwirkung.</b> In Stufe S1 liest kein Rechenweg diese
    /// Tabelle; sie wird gepflegt und importiert, mehr nicht. Der Referenzlauf bleibt
    /// byte-gleich.</para>
    /// </summary>
    public class WechselrichterStammCtrl : WechselrichterModel
    {
        /// <summary>Die Stammtabelle.</summary>
        public const string TABLE = SchemaKatalog.TAB_WECHSELRICHTER_STAMM;

        private readonly List<WechselrichterModel> _liste = new List<WechselrichterModel>();

        /// <summary>Zahl der gelesenen Sätze.</summary>
        public int rows => _liste.Count;

        /// <summary>Die gelesenen Sätze.</summary>
        public List<WechselrichterModel> items => _liste;

        // =================================================================
        //  Lesen
        // =================================================================

        /// <summary>Alle Katalogsätze, nach Bezeichner sortiert.</summary>
        public void ReadAll()
        {
            _liste.Clear();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] ORDER BY Bezeichner");
            if (dt == null) return;

            foreach (DataRow row in dt.Rows) _liste.Add(AusZeile(row));
        }

        /// <summary>
        /// Der Katalogsatz zum Bezeichner. Bei mehrfach vergebenem Namen die Zeile mit
        /// der KLEINSTEN Id — dieselbe Zusage wie
        /// <c>PhotovoltaikStammCtrl.ReadSingle</c>, und aus demselben Grund: Ohne
        /// <c>ORDER BY</c> bestimmt die Engine die Reihenfolge.
        /// </summary>
        public void ReadSingle(string szName)
        {
            _liste.Clear();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@bez", szName ?? ""));
            if (dt == null || dt.Rows.Count == 0) return;

            FuelleAus(this, dt.Rows[0]);
            _liste.Add(this);
        }

        /// <summary>Gibt es einen Katalogsatz mit diesem Bezeichner?</summary>
        public bool Exists(string szName)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@bez", szName ?? ""));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        /// <summary>ReadOnly-Prüfung für GENAU eine Zeile.</summary>
        public static bool IsReadOnlyById(int id)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE ID = ?", new DbParam("@id", id));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        /// <summary>Die Ids aller Katalogsätze zu einem Bezeichner, aufsteigend.</summary>
        public static List<int> IdsMitBezeichner(string szName)
        {
            var ids = new List<int>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID FROM [" + TABLE + "] WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@bez", szName ?? ""));
            if (dt == null) return ids;

            foreach (DataRow r in dt.Rows)
                if (r["ID"] != DBNull.Value) ids.Add(Convert.ToInt32(r["ID"]));
            return ids;
        }

        /// <summary>Der Bezeichner zu einer Id; leer, wenn es sie nicht gibt.</summary>
        public static string BezeichnerZu(int id)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM [" + TABLE + "] WHERE ID = ?", new DbParam("@id", id));
            return (v == null || v == DBNull.Value) ? "" : v.ToString();
        }

        // =================================================================
        //  Der Herstellerfilter der Verwaltung (Konzept 6)
        // =================================================================

        /// <summary>Eine Zeile der Katalogliste: Primärschlüssel und Bezeichner.</summary>
        public sealed record KatalogZeile(int Id, string Bezeichner);

        /// <summary>
        /// Die Hersteller des Katalogs in Anzeigereihenfolge — die Auswahlliste des
        /// Herstellerfilters. Bauart wie <c>PhotovoltaikStammCtrl.Hersteller</c>.
        /// </summary>
        /// <remarks>
        /// <b>Der Wechselrichterkatalog führt ihn, die Photovoltaik nicht</b>
        /// (Konzept 6): Die CEC-Wechselrichterliste bringt über zweitausend Geräte von
        /// rund hundert Herstellern; ohne Einengung ist die Liste nicht bedienbar.
        /// </remarks>
        public static IReadOnlyList<string> Hersteller()
        {
            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Firma FROM [" + TABLE + "] GROUP BY Firma ORDER BY Firma");
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
            {
                string f = row["Firma"] == DBNull.Value ? "" : row["Firma"].ToString();
                if (f.Length > 0) liste.Add(f);
            }
            return liste;
        }

        /// <summary>
        /// Die Katalogliste, wahlweise auf einen Hersteller eingeengt.
        /// </summary>
        /// <param name="hersteller">
        /// Leer, <c>null</c> und „Alle" heben die Einengung auf — derselbe Steuerwert
        /// wie bei <c>PhotovoltaikStammCtrl.Filtern</c>.
        /// </param>
        public IReadOnlyList<KatalogZeile> Filtern(string hersteller)
        {
            string h = (hersteller ?? "").Trim();
            bool alle = h.Length == 0 || h == "Alle";

            string sql = alle
                ? "SELECT ID, Bezeichner FROM [" + TABLE + "] ORDER BY Bezeichner"
                : "SELECT ID, Bezeichner FROM [" + TABLE + "] WHERE Firma = ? ORDER BY Bezeichner";

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

        // =================================================================
        //  Schreiben
        // =================================================================

        /// <summary>Übernimmt die Fachwerte und legt den Satz an.</summary>
        public bool InsertFrom(WechselrichterModel m)
        {
            UebernimmVon(m);
            return Insert();
        }

        /// <summary>Übernimmt die Fachwerte und schreibt den Satz zurück.</summary>
        public bool UpdateFrom(WechselrichterModel m, string szKey)
        {
            UebernimmVon(m);
            return Update(szKey);
        }

        /// <summary>
        /// Legt den Katalogsatz an — ausdrückliche Id (MAX+1) und
        /// <c>ReadOnly = false</c>, wie <c>PhotovoltaikStammCtrl.Insert</c>.
        /// </summary>
        public bool Insert()
        {
            int neueId = DataRepository.GetMaxID(TABLE) + 1;

            string sql = "INSERT INTO [" + TABLE + "] " +
                         "(ID, Bezeichner, Firma, Beschreibung, " + string.Join(", ", WechselrichterSchema.Fachspalten) +
                         ", ReadOnly) VALUES (?, ?, ?, ?, " + Fragezeichen(WechselrichterSchema.Fachspalten.Length) + ", ?)";

            var ps = new List<DbParam>
            {
                new DbParam("@id", neueId),
                new DbParam("@bez", m_szName ?? ""),
                new DbParam("@fir", Text(m_szFirma)),
                new DbParam("@bes", Text(m_szBeschreibung))
            };
            ps.AddRange(Fachparameter());
            ps.Add(new DbParam("@ro", false));

            bool ok = DataRepository.ExecuteSQL(sql, ps.ToArray());
            if (ok) m_ID = neueId;
            return ok;
        }

        /// <summary>
        /// Schreibt den Satz zurück. <paramref name="szKey"/> ist der ursprüngliche
        /// Bezeichner; <see cref="WechselrichterModel.m_szName"/> darf einen neuen
        /// tragen (Umbenennung).
        /// </summary>
        public bool Update(string szKey)
        {
            int id = EindeutigeId(szKey);
            return id > 0 && Update(id);
        }

        /// <summary>Schreibt GENAU den Katalogsatz mit dieser Id zurück.</summary>
        public bool Update(int id)
        {
            if (id <= 0) return false;

            if (IsReadOnlyById(id))
            {
                Meldung.Hinweis(Text(MyResource.Resource.WRK_MSG_SCHREIBGESCHUETZT),
                                Text(MyResource.Resource.WRK_TITEL_SCHREIBGESCHUETZT));
                return false;
            }

            // Umbenennen darf keinen bereits vergebenen Namen treffen - sonst legte
            // ausgerechnet die Korrektur eine neue Dublette an (wortgleich
            // PhotovoltaikStammCtrl.Update).
            List<int> gleicheNamen = IdsMitBezeichner(m_szName);
            if (gleicheNamen.Count > 0 && !gleicheNamen.Contains(id))
            {
                Meldung.Hinweis(
                    string.Format(MyResource.Resource.WRK_MSG_NAME_VERGEBEN, m_szName ?? ""),
                    Text(MyResource.Resource.WRK_TITEL_NAME_VERGEBEN));
                return false;
            }

            var sql = "UPDATE [" + TABLE + "] SET Bezeichner = ?, Firma = ?, Beschreibung = ?, " +
                      Zuweisungen(WechselrichterSchema.Fachspalten) + " WHERE ID = ?";

            var ps = new List<DbParam>
            {
                new DbParam("@bez", m_szName ?? ""),
                new DbParam("@fir", Text(m_szFirma)),
                new DbParam("@bes", Text(m_szBeschreibung))
            };
            ps.AddRange(Fachparameter());
            ps.Add(new DbParam("@id", id));

            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        /// <summary>
        /// Import-Überschreiben (Dublettenkonzept 4.2): aktualisiert GENAU die Felder,
        /// die der Import liefert, adressiert per Id. Vom Anwender gepflegte Felder
        /// (<c>Bezeichner</c>, <c>Beschreibung</c>, <c>Kosten</c>, <c>ReadOnly</c>)
        /// bleiben unangetastet.
        /// </summary>
        /// <remarks>
        /// Bewusst OHNE ReadOnly-Sperre — wie <c>PhotovoltaikStammCtrl.UpdateImport</c>:
        /// Das Überschreiben eines Auslieferungssatzes ist erlaubt und wird vorher im
        /// Konfliktdialog bestätigt.
        /// </remarks>
        public bool UpdateImport(int id)
        {
            if (id <= 0) return false;

            string[] spalten = Importspalten();
            string sql = "UPDATE [" + TABLE + "] SET Firma = ?, " + Zuweisungen(spalten) + " WHERE ID = ?";

            var ps = new List<DbParam> { new DbParam("@fir", Text(m_szFirma)) };
            foreach (string spalte in spalten) ps.Add(Fachparameter(spalte));
            ps.Add(new DbParam("@id", id));

            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        /// <summary>
        /// Die Fachspalten, die der Import schreibt — alle außer <c>Kosten</c>: Das ist
        /// ein Anwenderfeld, genau wie <c>Modulkosten</c> beim PV-Modul, und steht aus
        /// demselben Grund in <c>AusschlussSpalten</c> der Dublettenprüfung
        /// (Konzept 5.4).
        /// </summary>
        internal static string[] Importspalten()
        {
            var liste = new List<string>();
            foreach (string s in WechselrichterSchema.Fachspalten)
                if (s != WechselrichterSchema.SPALTE_KOSTEN) liste.Add(s);
            return liste.ToArray();
        }

        /// <summary>Löscht den Katalogsatz zum Bezeichner — nur, wenn er eindeutig ist.</summary>
        public bool Delete(string szName)
        {
            int id = EindeutigeId(szName);
            return id > 0 && Delete(id);
        }

        /// <summary>Löscht GENAU den Katalogsatz mit dieser Id.</summary>
        public bool Delete(int id)
        {
            if (id <= 0) return false;

            if (IsReadOnlyById(id))
            {
                Meldung.Hinweis(Text(MyResource.Resource.WRK_MSG_SCHREIBGESCHUETZT_LOESCHEN),
                                Text(MyResource.Resource.WRK_TITEL_SCHREIBGESCHUETZT));
                return false;
            }

            return DataRepository.ExecuteSQL("DELETE FROM [" + TABLE + "] WHERE ID = ?",
                                             new DbParam("@id", id));
        }

        // =================================================================
        //  Der EINE Schreibeinstieg der Verwaltung (Muster W14a.0e)
        // =================================================================

        /// <summary>
        /// Was ein Speicherversuch ergeben hat — dieselbe Form wie
        /// <c>PhotovoltaikStammCtrl.SpeicherErgebnis</c>.
        /// </summary>
        public sealed record SpeicherErgebnis(bool Ok, string Meldung, string Name);

        /// <summary>
        /// Schreibt den Katalogsatz — der Weg des Knopfes „Speichern" der Verwaltung.
        /// </summary>
        /// <param name="daten">Die Felder der Maske.</param>
        /// <param name="neu"><c>true</c> nach „Neu…": anlegen statt ändern.</param>
        /// <param name="schluessel">Der ursprüngliche Bezeichner — der WHERE-Schlüssel.</param>
        /// <remarks>
        /// <b>Die Plausibilität läuft VOR dem Schreiben</b> (Konzept 6): Ein
        /// Kopierfehler wie der von 2026 im PV-Katalog
        /// (<c>alpha_SC</c>/<c>beta_OC</c>/<c>T_NOCT</c>) soll hier gar nicht erst
        /// entstehen können. Ein Fehler sperrt, eine Warnung nicht — sie steht in der
        /// Meldung des Erfolgsfalls.
        /// </remarks>
        public static SpeicherErgebnis SpeichernAus(WechselrichterModel daten, bool neu, string schluessel)
        {
            if (daten == null || string.IsNullOrWhiteSpace(daten.m_szName))
                return new SpeicherErgebnis(false, MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG, "");

            WechselrichterPlausibilitaet.Befund pruefung = WechselrichterPlausibilitaet.Pruefe(daten);
            if (!pruefung.Ok)
                return new SpeicherErgebnis(false, WechselrichterPlausibilitaet.Meldung(pruefung), "");

            try
            {
                var ctrl = new WechselrichterStammCtrl();

                if (neu)
                {
                    if (ctrl.Exists(daten.m_szName))
                        return new SpeicherErgebnis(false, MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT, "");

                    if (!ctrl.InsertFrom(daten))
                        return new SpeicherErgebnis(false, MyResource.Resource.PSP_MELDUNG_SPEICHERN_FEHLER, "");
                }
                else if (!ctrl.UpdateFrom(daten, schluessel ?? daten.m_szName))
                {
                    return new SpeicherErgebnis(false, MyResource.Resource.PSP_MELDUNG_SPEICHERN_FEHLER, "");
                }

                string meldung = MyResource.Resource.PSP_MELDUNG_DATENSATZ_GESPEICHERT;
                if (pruefung.Warnungen.Count > 0)
                    meldung += " " + WechselrichterPlausibilitaet.Meldung(pruefung);

                return new SpeicherErgebnis(true, meldung, daten.m_szName);
            }
            catch (Exception ex)
            {
                return new SpeicherErgebnis(false,
                    string.Format(MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message), "");
            }
        }

        /// <summary>Löscht einen Katalogsatz und sagt, warum es nicht ging.</summary>
        public static SpeicherErgebnis Loeschen(string szName)
        {
            if (string.IsNullOrWhiteSpace(szName))
                return new SpeicherErgebnis(false, MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG, "");

            try
            {
                if (!new WechselrichterStammCtrl().Delete(szName))
                    return new SpeicherErgebnis(false, MyResource.Resource.KBROW_MSG_LOESCHEN_FEHLER, "");

                return new SpeicherErgebnis(true, "", szName);
            }
            catch (Exception ex)
            {
                return new SpeicherErgebnis(false,
                    string.Format(MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message), "");
            }
        }

        // =================================================================
        //  Hilfsmittel
        // =================================================================

        /// <summary>
        /// Löst einen Bezeichner auf GENAU eine Id auf; 0, wenn es keinen oder mehr als
        /// einen Treffer gibt. Der mehrdeutige Fall wird bewusst NICHT geraten —
        /// dieselbe Entscheidung wie in <c>PhotovoltaikStammCtrl.EindeutigeId</c>.
        /// </summary>
        private static int EindeutigeId(string szName)
        {
            List<int> ids = IdsMitBezeichner(szName);

            if (ids.Count == 0)
            {
                Meldung.Hinweis(
                    string.Format(MyResource.Resource.WRK_MSG_NICHT_GEFUNDEN, szName ?? ""),
                    Text(MyResource.Resource.WRK_TITEL_NICHT_GEFUNDEN));
                return 0;
            }

            if (ids.Count > 1)
            {
                Meldung.Warnung(
                    string.Format(MyResource.Resource.WRK_MSG_NAME_MEHRDEUTIG, szName ?? "", ids.Count),
                    Text(MyResource.Resource.WRK_TITEL_NAME_MEHRDEUTIG));
                return 0;
            }

            return ids[0];
        }

        /// <summary>Die Fachwerte als Parameterliste, in der Reihenfolge des Schemas.</summary>
        private List<DbParam> Fachparameter()
        {
            var ps = new List<DbParam>();
            foreach (string spalte in WechselrichterSchema.Fachspalten) ps.Add(Fachparameter(spalte));
            return ps;
        }

        /// <summary>
        /// Der Parameter EINER Fachspalte. <c>null</c> bleibt <c>DBNull</c> — „nicht
        /// gepflegt" ist bei einem Wechselrichter etwas anderes als 0 (siehe
        /// <see cref="WechselrichterModel"/>).
        /// </summary>
        private DbParam Fachparameter(string spalte)
        {
            return new DbParam("@" + spalte, Wert(spalte) ?? DBNull.Value);
        }

        /// <summary>Der Wert einer Fachspalte als Objekt; <c>null</c> = NULL.</summary>
        private object Wert(string spalte)
        {
            switch (spalte)
            {
                case WechselrichterSchema.SPALTE_P_AC_NENN: return m_P_AC_Nenn;
                case WechselrichterSchema.SPALTE_S_AC_MAX: return m_S_AC_Max;
                case WechselrichterSchema.SPALTE_P_DC_MAX: return m_P_DC_Max;
                case WechselrichterSchema.SPALTE_U_MPP_MIN: return m_U_Mpp_Min;
                case WechselrichterSchema.SPALTE_U_MPP_MAX: return m_U_Mpp_Max;
                case WechselrichterSchema.SPALTE_U_DC_MAX: return m_U_Dc_Max;
                case WechselrichterSchema.SPALTE_U_START: return m_U_Start;
                case WechselrichterSchema.SPALTE_I_DC_MAX: return m_I_Dc_Max;
                case WechselrichterSchema.SPALTE_ANZAHL_MPPT: return m_Anzahl_Mppt;
                case WechselrichterSchema.SPALTE_STRAENGE_JE_MPPT: return m_Straenge_Je_Mppt;
                case WechselrichterSchema.SPALTE_ETA05: return m_Eta05;
                case WechselrichterSchema.SPALTE_ETA10: return m_Eta10;
                case WechselrichterSchema.SPALTE_ETA20: return m_Eta20;
                case WechselrichterSchema.SPALTE_ETA30: return m_Eta30;
                case WechselrichterSchema.SPALTE_ETA50: return m_Eta50;
                case WechselrichterSchema.SPALTE_ETA100: return m_Eta100;
                case WechselrichterSchema.SPALTE_ETA_EURO: return m_Eta_Euro;
                case WechselrichterSchema.SPALTE_ETA_MAX: return m_Eta_Max;
                case WechselrichterSchema.SPALTE_P_STANDBY: return m_P_Standby;
                case WechselrichterSchema.SPALTE_P_NACHT: return m_P_Nacht;
                case WechselrichterSchema.SPALTE_KOSTEN: return m_Kosten;
                case WechselrichterSchema.SPALTE_SANDIA_PDCO: return m_Sandia_Pdco;
                case WechselrichterSchema.SPALTE_SANDIA_VDCO: return m_Sandia_Vdco;
                case WechselrichterSchema.SPALTE_SANDIA_PSO: return m_Sandia_Pso;
                case WechselrichterSchema.SPALTE_SANDIA_C0: return m_Sandia_C0;
                case WechselrichterSchema.SPALTE_SANDIA_C1: return m_Sandia_C1;
                case WechselrichterSchema.SPALTE_SANDIA_C2: return m_Sandia_C2;
                case WechselrichterSchema.SPALTE_SANDIA_C3: return m_Sandia_C3;
                case WechselrichterSchema.SPALTE_HERKUNFT:
                    return string.IsNullOrWhiteSpace(m_Herkunft) ? null : m_Herkunft;
            }
            return null;
        }

        /// <summary>„<c>Spalte = ?</c>", kommagetrennt — für UPDATE.</summary>
        private static string Zuweisungen(string[] spalten)
        {
            var teile = new List<string>(spalten.Length);
            foreach (string s in spalten) teile.Add("[" + s + "] = ?");
            return string.Join(", ", teile);
        }

        /// <summary>„?, ?, …" — für INSERT.</summary>
        private static string Fragezeichen(int anzahl)
        {
            var teile = new List<string>(anzahl);
            for (int i = 0; i < anzahl; i++) teile.Add("?");
            return string.Join(", ", teile);
        }

        private static string Text(string wert) => wert ?? "";

        // =================================================================
        //  Abbildung Zeile → Modell (Muster PhotovoltaikStammCtrl.FillFromRow)
        // =================================================================

        /// <summary>Ein Katalogsatz aus einer Datenbankzeile.</summary>
        internal static WechselrichterModel AusZeile(DataRow row)
        {
            var m = new WechselrichterModel();
            FuelleAus(m, row);
            return m;
        }

        /// <summary>
        /// Füllt ein Modell aus einer Zeile. Fehlende Spalte und NULL sind derselbe
        /// Fall — „nicht gepflegt".
        /// </summary>
        internal static void FuelleAus(WechselrichterModel m, DataRow row)
        {
            if (m == null || row == null) return;

            m.m_ID = Ganz(row, "ID") ?? 0;
            m.m_ID_Projekt = Ganz(row, "ID_Projekt") ?? 0;
            m.m_szName = Zeichen(row, "Bezeichner");
            m.m_szFirma = Zeichen(row, "Firma");
            m.m_szBeschreibung = Zeichen(row, "Beschreibung");

            m.m_P_AC_Nenn = Zahl(row, WechselrichterSchema.SPALTE_P_AC_NENN);
            m.m_S_AC_Max = Zahl(row, WechselrichterSchema.SPALTE_S_AC_MAX);
            m.m_P_DC_Max = Zahl(row, WechselrichterSchema.SPALTE_P_DC_MAX);
            m.m_U_Mpp_Min = Zahl(row, WechselrichterSchema.SPALTE_U_MPP_MIN);
            m.m_U_Mpp_Max = Zahl(row, WechselrichterSchema.SPALTE_U_MPP_MAX);
            m.m_U_Dc_Max = Zahl(row, WechselrichterSchema.SPALTE_U_DC_MAX);
            m.m_U_Start = Zahl(row, WechselrichterSchema.SPALTE_U_START);
            m.m_I_Dc_Max = Zahl(row, WechselrichterSchema.SPALTE_I_DC_MAX);
            m.m_Anzahl_Mppt = Ganz(row, WechselrichterSchema.SPALTE_ANZAHL_MPPT);
            m.m_Straenge_Je_Mppt = Ganz(row, WechselrichterSchema.SPALTE_STRAENGE_JE_MPPT);
            m.m_Eta05 = Zahl(row, WechselrichterSchema.SPALTE_ETA05);
            m.m_Eta10 = Zahl(row, WechselrichterSchema.SPALTE_ETA10);
            m.m_Eta20 = Zahl(row, WechselrichterSchema.SPALTE_ETA20);
            m.m_Eta30 = Zahl(row, WechselrichterSchema.SPALTE_ETA30);
            m.m_Eta50 = Zahl(row, WechselrichterSchema.SPALTE_ETA50);
            m.m_Eta100 = Zahl(row, WechselrichterSchema.SPALTE_ETA100);
            m.m_Eta_Euro = Zahl(row, WechselrichterSchema.SPALTE_ETA_EURO);
            m.m_Eta_Max = Zahl(row, WechselrichterSchema.SPALTE_ETA_MAX);
            m.m_P_Standby = Zahl(row, WechselrichterSchema.SPALTE_P_STANDBY);
            m.m_P_Nacht = Zahl(row, WechselrichterSchema.SPALTE_P_NACHT);
            m.m_Kosten = Zahl(row, WechselrichterSchema.SPALTE_KOSTEN);
            m.m_Sandia_Pdco = Zahl(row, WechselrichterSchema.SPALTE_SANDIA_PDCO);
            m.m_Sandia_Vdco = Zahl(row, WechselrichterSchema.SPALTE_SANDIA_VDCO);
            m.m_Sandia_Pso = Zahl(row, WechselrichterSchema.SPALTE_SANDIA_PSO);
            m.m_Sandia_C0 = Zahl(row, WechselrichterSchema.SPALTE_SANDIA_C0);
            m.m_Sandia_C1 = Zahl(row, WechselrichterSchema.SPALTE_SANDIA_C1);
            m.m_Sandia_C2 = Zahl(row, WechselrichterSchema.SPALTE_SANDIA_C2);
            m.m_Sandia_C3 = Zahl(row, WechselrichterSchema.SPALTE_SANDIA_C3);

            string herkunft = Zeichen(row, WechselrichterSchema.SPALTE_HERKUNFT);
            m.m_Herkunft = herkunft.Length == 0 ? null : herkunft;

            m.m_bReadOnly = row.Table.Columns.Contains(WechselrichterSchema.SPALTE_READONLY)
                            && row[WechselrichterSchema.SPALTE_READONLY] != DBNull.Value
                            && Convert.ToBoolean(row[WechselrichterSchema.SPALTE_READONLY]);
        }

        private static string Zeichen(DataRow row, string spalte)
        {
            return (row.Table.Columns.Contains(spalte) && row[spalte] != DBNull.Value)
                ? row[spalte].ToString() : "";
        }

        private static double? Zahl(DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte) || row[spalte] == DBNull.Value) return null;
            return Convert.ToDouble(row[spalte]);
        }

        private static int? Ganz(DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte) || row[spalte] == DBNull.Value) return null;
            return Convert.ToInt32(row[spalte]);
        }
    }
}
