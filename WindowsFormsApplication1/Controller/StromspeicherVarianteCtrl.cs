using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Zugriff auf Tab_StromspeicherVariante - die Betriebsfuehrung je Speichervariante
    // (Fachkonzept Stromspeicher 7.3, angelegt von SchemaMigration Schritt 11b).
    //
    // Durchgaengig ueber DataRepository mit ?-Parametern (CLAUDE.md: RecordSet ist
    // Altbestand und fuer neuen Code ausgeschlossen). IDs explizit ueber MAX(ID)+1 wie
    // im uebrigen Projekt; Schreibzugriffe sind ZIELGENAUE UPDATEs ueber die ID, damit
    // eine Aenderung an einem Feld nie den ganzen Satz mitschreibt.
    //
    // Lesen ist durchgaengig NAMENSBASIERT mit Columns.Contains-Wache: auf einer
    // Datenbank, deren Migration noch nicht durchgelaufen ist, liefert der Controller
    // dann die Vorbelegung des Modells statt einer Ausnahme.
    // ---------------------------------------------------------------------------
    public class StromspeicherVarianteCtrl
    {
        public const string TABLE = "Tab_StromspeicherVariante";

        private const string TAB_ANLAGEN = "Tab_Energieanlagen";

        private List<StromspeicherVarianteModel> _internalList = new List<StromspeicherVarianteModel>();
        public int rows => _internalList.Count;
        public List<StromspeicherVarianteModel> items => _internalList;

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>
        /// Die Variante EINER Speicheranlage (1:1). Rueckgabe null, wenn die Anlage
        /// noch keine Variante hat - der Aufrufer arbeitet dann mit einem frischen
        /// <see cref="StromspeicherVarianteModel"/> und dessen Vorbelegung.
        /// </summary>
        public StromspeicherVarianteModel ReadByEnergieanlage(int idEnergieanlage)
        {
            if (idEnergieanlage <= 0) return null;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE ID_Energieanlage = ? ORDER BY ID LIMIT 1",
                new OleDbParameter("@anl", idEnergieanlage));

            _internalList.Clear();
            if (dt == null || dt.Rows.Count == 0) return null;

            StromspeicherVarianteModel m = AusZeile(dt.Rows[0]);
            _internalList.Add(m);
            return m;
        }

        /// <summary>
        /// Die als aktiv markierte Variante eines Projekts (Fachkonzept 5.5/7.3) - sie
        /// speist Uebersichtsanzeige und Gesamtsimulation.
        ///
        /// Der Join ueber <c>Tab_Energieanlagen</c> ist noetig, weil die Variante selbst
        /// kein Projekt kennt: Ihr Projektbezug haengt an der Anlagenzeile, und genau so
        /// bleibt er beim Kopieren oder Loeschen eines Projekts widerspruchsfrei.
        ///
        /// Rueckgabe null, wenn das Projekt keine Speicheranlage oder keine aktive
        /// Variante fuehrt. Bei mehreren aktiven Zeilen - was
        /// <see cref="SetzeAktiv"/> ausschliesst, eine von Hand bearbeitete Datenbank
        /// aber hergeben koennte - gilt die kleinste ID.
        /// </summary>
        public StromspeicherVarianteModel ReadAktiveVariante(int idProjekt)
        {
            if (idProjekt <= 0) return null;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT v.* FROM [" + TABLE + "] AS v " +
                "INNER JOIN " + TAB_ANLAGEN + " AS a ON v.ID_Energieanlage = a.ID " +
                "WHERE a.ID_Projekt = ? AND v.Aktiv = TRUE ORDER BY v.ID LIMIT 1",
                new OleDbParameter("@proj", idProjekt));

            _internalList.Clear();
            if (dt == null || dt.Rows.Count == 0) return null;

            StromspeicherVarianteModel m = AusZeile(dt.Rows[0]);
            _internalList.Add(m);
            return m;
        }

        /// <summary>
        /// Alle Speichervarianten eines Projekts in Anlagenreihenfolge - die Grundlage
        /// der Vergleichstabelle (Fachkonzept 7.3). Fuellt zusaetzlich
        /// <see cref="items"/>.
        /// </summary>
        public List<StromspeicherVarianteModel> ReadAllByProjekt(int idProjekt)
        {
            _internalList.Clear();
            if (idProjekt <= 0) return _internalList;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT v.* FROM [" + TABLE + "] AS v " +
                "INNER JOIN " + TAB_ANLAGEN + " AS a ON v.ID_Energieanlage = a.ID " +
                "WHERE a.ID_Projekt = ? ORDER BY a.ID, v.ID",
                new OleDbParameter("@proj", idProjekt));

            if (dt == null) return _internalList;

            foreach (DataRow r in dt.Rows) _internalList.Add(AusZeile(r));
            return _internalList;
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>
        /// Legt die Variante einer Anlage an (ID nach dem MAX(ID)+1-Hausmuster) und
        /// traegt die vergebene ID in das Modell zurueck.
        ///
        /// KEIN Duplikat: Fuehrt die Anlage bereits eine Variante, liefert die Methode
        /// deren ID und schreibt nichts - dieselbe Zusage wie
        /// <c>StromspeicherCtrl.CopyFromStamm</c> und wie Migrationsschritt 11d.
        /// Rueckgabe -1 bei Fehler.
        /// </summary>
        public int Insert(StromspeicherVarianteModel m)
        {
            if (m == null || m.ID_Energieanlage <= 0) return -1;

            StelleTabelleSicher();

            StromspeicherVarianteModel vorhanden = ReadByEnergieanlage(m.ID_Energieanlage);
            if (vorhanden != null) { m.ID = vorhanden.ID; return vorhanden.ID; }

            int neueId = DataRepository.GetMaxID(TABLE) + 1;

            string sql = "INSERT INTO [" + TABLE + "] (ID, ID_Energieanlage, Betriebsart, " +
                "PV_Zulaessig, BHKW_Ueberschuss_Zulaessig, BHKW_Stromgefuehrt, Netzentladung, " +
                "SoC_Min_Prozent, SoC_Max_Prozent, Berechnungsart, Preisquelle, " +
                "Kompatibilitaetsmodus, Kapitalzins, Nutzungsdauer, L_P, A_Netzlade, " +
                "Aktiv, Ladeschwellwert, ID_Preisreihe, ID_Kostenprofil, Aufschlag_Anwenden) " +
                "VALUES (?,?,?, ?,?,?,?, ?,?, ?,?, ?,?,?,?,?, ?,?, ?,?,?)";

            OleDbParameter[] ps = {
                new OleDbParameter("@id", neueId),
                new OleDbParameter("@anl", m.ID_Energieanlage),
                Txt("@bart", m.Betriebsart, DbWerte.SP_BETRIEBSART_GRUENSTROM),
                new OleDbParameter("@pv", m.PV_Zulaessig),
                new OleDbParameter("@bhkw", m.BHKW_Ueberschuss_Zulaessig),
                new OleDbParameter("@bhkwstrom", m.BHKW_Stromgefuehrt),
                new OleDbParameter("@netzent", m.Netzentladung),
                new OleDbParameter("@socmin", m.SoC_Min_Prozent),
                new OleDbParameter("@socmax", m.SoC_Max_Prozent),
                Txt("@rart", m.Berechnungsart, DbWerte.SP_BERECHNUNG_DAUERNUTZUNG),
                Txt("@pquelle", m.Preisquelle, DbWerte.SP_PREISQUELLE_FIXPREIS),
                new OleDbParameter("@kompat", m.Kompatibilitaetsmodus),
                new OleDbParameter("@zins", m.Kapitalzins),
                new OleDbParameter("@nutz", m.Nutzungsdauer),
                new OleDbParameter("@lp", m.L_P),
                new OleDbParameter("@anetz", m.A_Netzlade),
                new OleDbParameter("@aktiv", m.Aktiv),
                new OleDbParameter("@schwelle", m.Ladeschwellwert),
                // AP4: Preisquellen-Verweise. 0 heisst "nicht gewaehlt" und wird als
                // NULL abgelegt - dieselbe FK-Regel wie im Spaltenkatalog.
                new OleDbParameter("@preisreihe", OleDbType.Integer)
                    { Value = m.ID_Preisreihe > 0 ? (object)m.ID_Preisreihe : DBNull.Value },
                new OleDbParameter("@kostenprofil", OleDbType.Integer)
                    { Value = m.ID_Kostenprofil > 0 ? (object)m.ID_Kostenprofil : DBNull.Value },
                new OleDbParameter("@aufschlag", m.Aufschlag_Anwenden)
            };

            if (!DataRepository.ExecuteSQL(sql, ps)) return -1;

            m.ID = neueId;
            return neueId;
        }

        /// <summary>
        /// Schreibt eine vorhandene Variante zurueck. Zielgenau ueber die ID; das Feld
        /// <c>Aktiv</c> bleibt bewusst AUSSEN VOR - es ist eine Eigenschaft des
        /// Projekts ("genau eine aktive Variante") und wird ausschliesslich ueber
        /// <see cref="SetzeAktiv"/> gesetzt, damit ein Speichern der Parameterseite nie
        /// zwei aktive Varianten hinterlassen kann.
        /// </summary>
        public bool Update(StromspeicherVarianteModel m)
        {
            if (m == null || m.ID <= 0) return false;

            string sql = "UPDATE [" + TABLE + "] SET " +
                "ID_Energieanlage = ?, Betriebsart = ?, " +
                "PV_Zulaessig = ?, BHKW_Ueberschuss_Zulaessig = ?, BHKW_Stromgefuehrt = ?, " +
                "Netzentladung = ?, SoC_Min_Prozent = ?, SoC_Max_Prozent = ?, " +
                "Berechnungsart = ?, Preisquelle = ?, Kompatibilitaetsmodus = ?, " +
                "Kapitalzins = ?, Nutzungsdauer = ?, L_P = ?, A_Netzlade = ?, " +
                "Ladeschwellwert = ?, ID_Preisreihe = ?, ID_Kostenprofil = ?, " +
                "Aufschlag_Anwenden = ? WHERE ID = ?";

            OleDbParameter[] ps = {
                new OleDbParameter("@anl", m.ID_Energieanlage),
                Txt("@bart", m.Betriebsart, DbWerte.SP_BETRIEBSART_GRUENSTROM),
                new OleDbParameter("@pv", m.PV_Zulaessig),
                new OleDbParameter("@bhkw", m.BHKW_Ueberschuss_Zulaessig),
                new OleDbParameter("@bhkwstrom", m.BHKW_Stromgefuehrt),
                new OleDbParameter("@netzent", m.Netzentladung),
                new OleDbParameter("@socmin", m.SoC_Min_Prozent),
                new OleDbParameter("@socmax", m.SoC_Max_Prozent),
                Txt("@rart", m.Berechnungsart, DbWerte.SP_BERECHNUNG_DAUERNUTZUNG),
                Txt("@pquelle", m.Preisquelle, DbWerte.SP_PREISQUELLE_FIXPREIS),
                new OleDbParameter("@kompat", m.Kompatibilitaetsmodus),
                new OleDbParameter("@zins", m.Kapitalzins),
                new OleDbParameter("@nutz", m.Nutzungsdauer),
                new OleDbParameter("@lp", m.L_P),
                new OleDbParameter("@anetz", m.A_Netzlade),
                new OleDbParameter("@schwelle", m.Ladeschwellwert),
                new OleDbParameter("@preisreihe", OleDbType.Integer)
                    { Value = m.ID_Preisreihe > 0 ? (object)m.ID_Preisreihe : DBNull.Value },
                new OleDbParameter("@kostenprofil", OleDbType.Integer)
                    { Value = m.ID_Kostenprofil > 0 ? (object)m.ID_Kostenprofil : DBNull.Value },
                new OleDbParameter("@aufschlag", m.Aufschlag_Anwenden),
                new OleDbParameter("@id", m.ID)
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        /// <summary>
        /// Macht GENAU EINE Variante zur aktiven Variante ihres Projekts: erst alle
        /// Varianten des Projekts zuruecksetzen, dann die gewaehlte setzen. Zwei
        /// zielgenaue UPDATEs statt eines Rundumschlags - so bleibt der Zustand auch
        /// dann eindeutig, wenn der zweite Schritt scheitert (keine aktive Variante ist
        /// ein handhabbarer Zustand, zwei aktive nicht).
        /// </summary>
        public bool SetzeAktiv(int idProjekt, int idVariante)
        {
            if (idProjekt <= 0 || idVariante <= 0) return false;

            DataRepository.ExecuteSQL(
                "UPDATE [" + TABLE + "] SET Aktiv = FALSE WHERE ID_Energieanlage IN " +
                "(SELECT ID FROM " + TAB_ANLAGEN + " WHERE ID_Projekt = ?)",
                new OleDbParameter("@proj", idProjekt));

            return DataRepository.ExecuteSQL(
                "UPDATE [" + TABLE + "] SET Aktiv = TRUE WHERE ID = ?",
                new OleDbParameter("@id", idVariante));
        }

        public bool Delete(int idVariante)
        {
            if (idVariante <= 0) return false;
            return DataRepository.ExecuteSQL("DELETE FROM [" + TABLE + "] WHERE ID = ?",
                new OleDbParameter("@id", idVariante));
        }

        /// <summary>
        /// Raeumt die Variante einer geloeschten Anlage ab. Normalerweise erledigt das
        /// die Loeschweitergabe der Beziehung FK_SpVariante_Anlage; auf einer Datenbank,
        /// auf der sie nicht angelegt werden konnte (Migrationsprotokoll), muss es der
        /// Aufrufer selbst tun - sonst zeigt die Waise wegen der MAX(ID)+1-Vergabe
        /// spaeter auf eine FREMDE Anlage.
        /// </summary>
        public bool DeleteByEnergieanlage(int idEnergieanlage)
        {
            if (idEnergieanlage <= 0) return false;
            return DataRepository.ExecuteSQL("DELETE FROM [" + TABLE + "] WHERE ID_Energieanlage = ?",
                new OleDbParameter("@anl", idEnergieanlage));
        }

        // =====================================================================
        // Abbildung und Rueckfallebene
        // =====================================================================

        private static StromspeicherVarianteModel AusZeile(DataRow r)
        {
            StromspeicherVarianteModel m = new StromspeicherVarianteModel();

            m.ID = I(r, "ID");
            m.ID_Energieanlage = I(r, "ID_Energieanlage");

            // Leerer Text heisst "nicht gepflegt" - dann bleibt die Vorbelegung des
            // Modells stehen, statt eine leere Betriebsart in die Engine zu tragen.
            m.Betriebsart = S(r, "Betriebsart", m.Betriebsart);
            m.Berechnungsart = S(r, "Berechnungsart", m.Berechnungsart);
            m.Preisquelle = S(r, "Preisquelle", m.Preisquelle);

            m.PV_Zulaessig = B(r, "PV_Zulaessig");
            m.BHKW_Ueberschuss_Zulaessig = B(r, "BHKW_Ueberschuss_Zulaessig");
            m.BHKW_Stromgefuehrt = B(r, "BHKW_Stromgefuehrt");
            m.Netzentladung = B(r, "Netzentladung");
            m.Kompatibilitaetsmodus = B(r, "Kompatibilitaetsmodus");
            m.Aktiv = B(r, "Aktiv");

            m.SoC_Min_Prozent = D(r, "SoC_Min_Prozent", m.SoC_Min_Prozent);
            m.SoC_Max_Prozent = D(r, "SoC_Max_Prozent", m.SoC_Max_Prozent);
            m.Kapitalzins = D(r, "Kapitalzins", m.Kapitalzins);
            m.Nutzungsdauer = D(r, "Nutzungsdauer", m.Nutzungsdauer);
            m.L_P = D(r, "L_P", 0.0);
            m.A_Netzlade = D(r, "A_Netzlade", 0.0);
            m.Ladeschwellwert = D(r, "Ladeschwellwert", 0.0);

            // AP4 (Migrationsschritt 12a). Auf einer noch nicht migrierten Datenbank
            // fehlen die drei Spalten - dann bleibt die Vorbelegung des Modells stehen,
            // also "keine Reihe gewaehlt" und "Aufschlaege anwenden".
            m.ID_Preisreihe = I(r, "ID_Preisreihe");
            m.ID_Kostenprofil = I(r, "ID_Kostenprofil");
            if (r.Table.Columns.Contains("Aufschlag_Anwenden"))
                m.Aufschlag_Anwenden = B(r, "Aufschlag_Anwenden");

            return m;
        }

        private static bool _tabelleGeprueft;

        /// <summary>
        /// Rueckfallebene fuer Datenbanken, deren SchemaMigration (Schritt 11b) noch
        /// nicht gelaufen ist: legt die Tabelle samt Index und Loeschweitergabe an.
        /// Muster und Begruendung wie <c>ErgebnisCtrl.StellePufferTabelleSicher</c> -
        /// jeder Schritt einzeln abgesichert, damit ein Fehlschlag die uebrigen nicht
        /// mitreisst.
        ///
        /// ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht; Schemaprobe statt
        /// <c>GetOleDbSchemaTable</c> (S4c vorgezogen); SQLite-DDL statt der
        /// Access-Konstanten aus <see cref="SchemaMigration"/> (S4d vorgezogen).
        ///
        /// ZWEI UNVERMEIDBARE ABWEICHUNGEN GEGENUEBER DER ACCESS-FASSUNG:
        ///   - Die Anweisungen kommen NICHT mehr aus <see cref="SchemaMigration"/>. Die
        ///     Konstanten dort sind Access-DDL (LONG/YESNO/TEXT(n)) und gehoeren zum
        ///     eingefrorenen Alt-Zweig (Arbeitspaket S6). Massgeblich ist hier
        ///     <c>sql\schema\001_grundschema.sql</c>; ein zweiter Spaltensatz entsteht
        ///     dadurch nicht - beide beschreiben dieselbe Tabelle.
        ///   - SQLite kann einen Fremdschluessel nach dem CREATE TABLE nicht nachruesten.
        ///     Die Loeschweitergabe steht deshalb IM CREATE TABLE statt in einem eigenen
        ///     ALTER; scheitert das CREATE, gibt es auch keine Beziehung - genau wie
        ///     bisher.
        /// </summary>
        public static void StelleTabelleSicher()
        {
            if (_tabelleGeprueft) return;
            _tabelleGeprueft = true;

            try
            {
                if (StilleDb.TabelleVorhanden(TABLE)) return;   // vorhanden

                string ddl =
                    "CREATE TABLE IF NOT EXISTS [" + TABLE + "] (" +
                    "\"ID\" INTEGER PRIMARY KEY, " +
                    "\"ID_Energieanlage\" INTEGER, " +
                    "\"Betriebsart\" TEXT CHECK (length(\"Betriebsart\") <= 50), " +
                    "\"PV_Zulaessig\" INTEGER NOT NULL DEFAULT 0 CHECK (\"PV_Zulaessig\" IN (0,1)), " +
                    "\"BHKW_Ueberschuss_Zulaessig\" INTEGER NOT NULL DEFAULT 0 CHECK (\"BHKW_Ueberschuss_Zulaessig\" IN (0,1)), " +
                    "\"BHKW_Stromgefuehrt\" INTEGER NOT NULL DEFAULT 0 CHECK (\"BHKW_Stromgefuehrt\" IN (0,1)), " +
                    "\"Netzentladung\" INTEGER NOT NULL DEFAULT 0 CHECK (\"Netzentladung\" IN (0,1)), " +
                    "\"SoC_Min_Prozent\" REAL, " +
                    "\"SoC_Max_Prozent\" REAL, " +
                    "\"Berechnungsart\" TEXT CHECK (length(\"Berechnungsart\") <= 50), " +
                    "\"Preisquelle\" TEXT CHECK (length(\"Preisquelle\") <= 50), " +
                    "\"Kompatibilitaetsmodus\" INTEGER NOT NULL DEFAULT 0 CHECK (\"Kompatibilitaetsmodus\" IN (0,1)), " +
                    "\"Kapitalzins\" REAL, " +
                    "\"Nutzungsdauer\" REAL, " +
                    "\"L_P\" REAL, " +
                    "\"A_Netzlade\" REAL, " +
                    "\"Aktiv\" INTEGER NOT NULL DEFAULT 0 CHECK (\"Aktiv\" IN (0,1)), " +
                    "\"Ladeschwellwert\" REAL, " +
                    "FOREIGN KEY (\"ID_Energieanlage\") REFERENCES \"Tab_Energieanlagen\" (\"ID\") ON DELETE CASCADE)";

                // ohne Tabelle sind Index und Beziehung sinnlos
                if (StilleDb.NonQuery(ddl) < 0) return;

                StilleDb.NonQuery("CREATE INDEX IF NOT EXISTS \"idx_SpVariante\" " +
                                  "ON [" + TABLE + "] (\"ID_Energieanlage\")");
            }
            catch { /* best effort - ein echter Fehler faellt beim naechsten Zugriff auf */ }
        }

        /// <summary>Textparameter mit Rueckfall auf den Vorgabewert statt auf NULL.</summary>
        private static OleDbParameter Txt(string name, string wert, string vorgabe)
        {
            return new OleDbParameter(name, string.IsNullOrEmpty(wert) ? vorgabe : wert);
        }

        private static int I(DataRow r, string col)
        { return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? Convert.ToInt32(r[col]) : 0; }

        private static double D(DataRow r, string col, double vorgabe)
        { return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? Convert.ToDouble(r[col]) : vorgabe; }

        private static bool B(DataRow r, string col)
        { return r.Table.Columns.Contains(col) && r[col] != DBNull.Value && Convert.ToBoolean(r[col]); }

        private static string S(DataRow r, string col, string vorgabe)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return vorgabe;
            string s = r[col].ToString();
            return string.IsNullOrEmpty(s) ? vorgabe : s;
        }
    }
}
