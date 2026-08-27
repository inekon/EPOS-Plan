using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dupliziert ein komplettes Projekt (alle projektbezogenen Datensaetze) generisch per SQL.
    ///
    /// GENERISCHE TABELLENERKENNUNG:
    ///  - Die Liste der zu kopierenden Tabellen wird NICHT fest gepflegt, sondern zur Laufzeit aus
    ///    dem DB-Schema ermittelt: jede Basistabelle, die
    ///      * NICHT auf _STAMM endet,
    ///      * NICHT in der festen Katalog-Ausnahmeliste steht, und
    ///      * eine Spalte ID_Projekt bzw. ProjektID hat  ->  wird automatisch mitkopiert.
    ///  - ZUSAETZLICH werden ueber den echten FK-Graphen (erzwungene Beziehungen) auch Tabellen OHNE
    ///    eigenes ID_Projekt automatisch erkannt, sobald sie per Fremdschluessel an einer bereits
    ///    kopierten Tabelle haengen (iterativ bis Fixpunkt -> auch Enkel/Urenkel). Ihr Filter wird aus
    ///    dem Elternfilter zusammengesetzt. Eine neue Detailtabelle (z. B. mit FK auf Tab_DBTagV) wird
    ///    damit ohne Code-Aenderung mitkopiert.
    ///  - Neu hinzugefuegte Projekt-Tabellen werden damit automatisch beruecksichtigt.
    ///
    /// FESTE AUSNAHMEN (bewusst hart kodiert):
    ///  - KATALOG_TABELLEN : globale Kataloge, die nie dupliziert werden.
    ///  - KATALOG_SPALTEN  : ID-Spalten, die auf Kataloge zeigen und NICHT versetzt werden.
    ///  - FK_MAP / FK_OVERRIDE : interne Fremdschluessel -> Zieltabelle (unregelmaessige Namen).
    ///  - KINDER : Tabellen ohne (verlaessliches) ID_Projekt, die ueber einen Eltern-FK gefiltert
    ///             werden (z. B. Tab_Kenndaten ueber ID_WP, *Daten ueber ID_Ganglinie).
    ///
    /// KOPIER-VERFAHREN (kompakter, tabellen-eigener Offset):
    ///  - offset(T) = MAX(T) - MIN(Quellzeilen) + 1  -> neue IDs liegen kompakt hinter dem Maximum.
    ///  - Jede ID-Spalte wird mit dem Offset IHRER Zieltabelle versetzt (PK->self, ID_Projekt->Tab_Projekt,
    ///    FK->referenzierte Tabelle). 0/NULL bleiben unveraendert (IIF([c] > 0, ...)).
    ///  - Alle Operationen sind reines SQL (INSERT ... SELECT).
    ///
    /// Aufruf:  new ProjektDuplizierenCtrl().Duplizieren("Quelle", "Neu");
    /// </summary>
    public class ProjektDuplizierenCtrl
    {
        // ---- FESTE AUSNAHMEN ----

        private static readonly HashSet<string> KATALOG_TABELLEN = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Tab_BrennstoffKategorien", "Tab_Typ_Energieanlagen",
            "Tab_KostenGruppenKatalog", "Tab_KostenKomponente", "Tab_Kostenfaktor",
            "energy_carrier", "energy_conversion", "pricing_model",
            "Tab_Applikation",         // anwendungsweit, kein Projektbezug
            "Einfügefehler"            // Access-Fehlertabelle
            // ETAPPE K6 (HF1, Migrationsschritt 29): "Tab_KostenKategorie" und
            // "energy_unit" sind hier entfallen — beide Tabellen gibt es nicht mehr.
            // Die Liste ist eine AUSSCHLUSSliste gegen den schema-getriebenen Kopierlauf
            // (GetOleDbSchemaTable, :225-262); ein Eintrag für eine gedroppte Tabelle
            // wäre ab jetzt nur noch irreführend: Er behauptete, es gäbe einen Katalog,
            // der vor dem Duplizieren geschützt werden muss.
        };

        // Tabellen, die trotz Projektbezug NICHT dupliziert werden (feste Ausnahme).
        // - energy_price wird wieder mitkopiert (projekteigene Preise). Voraussetzung
        //   ist, dass der eindeutige Index "unq_price_date" in Access um ID_Projekt erweitert wurde
        //   (dann: ID_Projekt, carrier_id, valid_from). Andernfalls kollidiert die Kopie mit dem
        //   Quellprojekt und energy_price muss hier wieder eingetragen werden.
        // - Berichtskonfiguration gilt JE STAMMPROJEKT (BerichtCtrl.Lade faellt ohne Zeile
        //   auf die Standardkonfiguration zurueck) - eine Kopie fuer das Zielprojekt ist
        //   fachlich ueberfluessig. Sie war ausserdem der Ausloeser des Duplizier-Abbruchs
        //   vom 21.08.2026: Die Tabelle haengt an keiner Loeschweitergabe, ein geloeschtes
        //   Projekt hinterlaesst also eine verwaiste Konfigzeile. Die Kopie zielt auf
        //   MAX(Tab_Projekt.ID)+1 - genau die ProjektID, die so eine Waise noch belegt -
        //   und scheitert dann am eindeutigen Index UQ_BerichtKonfigProj (ProjektID).
        //   ProjektCtrl.Delete raeumt die Konfigzeile seither mit ab; der Ausschluss hier
        //   macht das Duplizieren zusaetzlich gegen Altwaisen im Bestand unempfindlich.
        private static readonly HashSet<string> AUSNAHME_TABELLEN = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Berichtskonfiguration"
        };

        // ID-Spalten, die auf Katalog-/Stammdaten zeigen und NICHT versetzt werden duerfen.
        private static readonly HashSet<string> KATALOG_SPALTEN = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ID_Type", "ID_Stamm", "StammID", "KomponentenID", "KategorieID",
            "carrier_id", "ID_Energieträger", "ID_Umrechnung", "ID_Brennstoff"
        };

        // Interne Fremdschluessel mit eindeutigem Zielnamen (Spalte -> Zieltabelle).
        private static readonly Dictionary<string, string> FK_MAP = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"ID_WP","Tab_WP"}, {"ID_SP","Tab_Stromspeicher"}, {"ID_PV","Tab_PV"},
            {"ID_Solar","Tab_Solarkollektoren"}, {"ID_Kessel","Tab_Heizkessel"}, {"ID_BHKW","Tab_BHKW"},
            {"ID_PUFFER","Tab_Pufferspeicher"}, {"ID_Pufferspeicher","Tab_Pufferspeicher"},
            // Quellen-/Senken-Modell (Konzept 5.3): die drei neuen Puffer-Referenzen in
            // Tab_Energieanlagen. Seit Schritt 4 der SchemaMigration sind das echte
            // Access-Beziehungen, die _echteFks ohnehin erkennt - der Eintrag hier ist
            // Guertel und Hosentraeger fuer Datenbanken, in denen die Migration (noch)
            // nicht gelaufen ist. Ohne Versatz zeigten Varianten auf die Speicher des
            // Quellprojekts; das faellt erst im Ergebnis auf.
            {"WS_ID_Puffer","Tab_Pufferspeicher"}, {"WS_ID_Puffer2","Tab_Pufferspeicher"},
            {"WQ_ID_Puffer","Tab_Pufferspeicher"},
            {"ID_Klimaregion","Tab_Klimaregion"}, {"ID_ProjektGebaeude","Z_ProjektGebaeude"},
            {"ID_Gebaeude","Tab_Gebaeude"}, {"ID_TagV","Tab_DBTagV"},
            {"ID_Stromverbraucher","Tab_Stromverbraucher"}, {"ID_Prozesswaerme","Tab_Prozesswaerme"},
            {"ID_Brauchwasser","Tab_Brauchwasser"},
            // Ä20: Anlagenbezug der Kostenpositionen (Tab_ProjektWerte.ID_Anlage,
            // Migrationsschritt 45). Ohne Versatz zeigten die Positionen einer
            // Variante auf die Anlagen des QUELLprojekts und stünden dort als
            // „ohne Anlagenzuordnung“ da.
            {"ID_Anlage","Tab_Energieanlagen"}
        };

        // Mehrdeutige FK-Spalten (gleicher Name, verschiedene Zieltabellen) -> je Tabelle aufgeloest.
        private static readonly Dictionary<string, Dictionary<string, string>> FK_OVERRIDE =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            {"Z_ProjektWaermebedarf",   new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"ID_Ganglinie","Tab_Waermebedarf"}}},
            {"Z_ProjektStromganglinie", new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"ID_Ganglinie","Tab_Stromganglinie"}}},
            {"Z_ProjektSolarganglinie", new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"ID_Ganglinie","Tab_Solarganglinie"}}},
            {"Tab_WaermebedarfDaten",   new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"ID_Ganglinie","Tab_Waermebedarf"}}},
            {"Tab_StromganglinieDaten", new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"ID_Ganglinie","Tab_Stromganglinie"}}},
            {"Tab_SolarganglinieDaten", new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"ID_Ganglinie","Tab_Solarganglinie"}}},
        };

        // Kind-Tabellen (kein verlaessliches ID_Projekt) -> Sonderfilter ueber den Eltern-FK. {0} = Quell-Projekt-ID.
        private static readonly Dictionary<string, string> KINDER = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"Tab_Kenndaten",          "ID_WP IN (SELECT ID FROM Tab_WP WHERE ID_Projekt = {0})"},
            {"Tab_Kenndaten_Kuehlung", "ID_WP IN (SELECT ID FROM Tab_WP WHERE ID_Projekt = {0})"},
            {"Tab_DBTagV",             "ID_Gebaeude IN (SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {0})"},
            {"Tab_DBTagVDaten",        "ID_TagV IN (SELECT ID FROM Tab_DBTagV WHERE ID_Gebaeude IN (SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = {0}))"},
            {"Tab_WaermebedarfDaten",  "ID_Ganglinie IN (SELECT ID FROM Tab_Waermebedarf WHERE ID_Projekt = {0})"},
            {"Tab_StromganglinieDaten","ID_Ganglinie IN (SELECT ID FROM Tab_Stromganglinie WHERE ID_Projekt = {0})"},
            {"Tab_SolarganglinieDaten","ID_Ganglinie IN (SELECT ID FROM Tab_Solarganglinie WHERE ID_Projekt = {0})"},
            {"Tab_Stromverbrauchertyp","ID_Stromverbraucher IN (SELECT ID FROM Tab_Stromverbraucher WHERE ID_Projekt = {0})"},
        };

        // Echte, in Access deklarierte Fremdschluessel: Key "Tabelle||Spalte" -> referenzierte Tabelle.
        // Wird zur Laufzeit aus dem Schema gelesen und deckt exakt die erzwungene referentielle
        // Integritaet ab. Hat Vorrang vor FK_MAP/FK_OVERRIDE (die nur Fallback fuer nicht deklarierte
        // Beziehungen sind).
        private Dictionary<string, string> _echteFks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ---- interne Struktur einer zu kopierenden Tabelle ----
        internal class Spec
        {
            public string Tabelle;
            public string Pk;
            public string Filter;      // {0} = Quell-Projekt-ID
            public string NameSpalte;  // nur Tab_Projekt
            public List<string> Cols;  // Spalten (einmalig gelesen, fuer Sortierung + INSERT)
        }

        public int GetProjektId(string projektname)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Projekt WHERE Projektname = ?",
                new OleDbParameter("@n", projektname ?? ""));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        /// <summary>
        /// Fortschrittsmeldung waehrend des Kopierens (pro Tabelle). Aktuell = Anzahl bereits
        /// kopierter Tabellen, Gesamt = Anzahl zu kopierender Tabellen, Tabelle = gerade laufende.
        /// </summary>
        public class Fortschritt
        {
            public int Aktuell;
            public int Gesamt;
            public string Tabelle;
        }

        /// <summary>
        /// Dupliziert 'quelleName' unter dem neuen Namen 'neuerName'.
        /// Optional meldet 'fortschritt' den Kopierfortschritt pro Tabelle (fuer eine ProgressBar).
        /// Rueckgabe: neue Projekt-ID, oder -1 bei Fehler.
        /// </summary>
        public int Duplizieren(string quelleName, string neuerName, IProgress<Fortschritt> fortschritt = null)
        {
            if (string.IsNullOrWhiteSpace(quelleName) || string.IsNullOrWhiteSpace(neuerName))
            { MessageBox.Show("Quell- und Zielprojektname duerfen nicht leer sein."); return -1; }

            int srcId = GetProjektId(quelleName);
            if (srcId <= 0) { MessageBox.Show("Quellprojekt '" + quelleName + "' wurde nicht gefunden."); return -1; }
            if (GetProjektId(neuerName) > 0) { MessageBox.Show("Es existiert bereits ein Projekt mit dem Namen '" + neuerName + "'."); return -1; }

            OleDbConnection conn = null;
            OleDbTransaction trans = null;
            try
            {
                var tx = DataRepository.BeginTransaction();
                conn = tx.Item1;
                trans = tx.Item2;

                // 1) Tabellen generisch ermitteln.
                List<Spec> specs = ErmittlePlan(conn, trans);
                var copySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (Spec s in specs) copySet.Add(s.Tabelle);

                // 2) Offsets bestimmen (MAX ueber alle / MIN ueber Quellzeilen).
                var offset = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                foreach (Spec s in specs)
                {
                    long o;
                    if (BerechneOffset(conn, trans, s, srcId, out o)) offset[s.Tabelle] = o;
                }
                if (!offset.ContainsKey("Tab_Projekt"))
                { try { trans.Rollback(); } catch { } MessageBox.Show("Projekt konnte nicht gelesen werden."); return -1; }

                // 3) Kopieren. Nur Tabellen mit Quellzeilen (offset vorhanden) werden gezaehlt/gemeldet.
                var zuKopieren = new List<Spec>();
                foreach (Spec s in specs)
                    if (offset.ContainsKey(s.Tabelle)) zuKopieren.Add(s);

                int gesamt = zuKopieren.Count;
                for (int i = 0; i < zuKopieren.Count; i++)
                {
                    Spec s = zuKopieren[i];
                    if (fortschritt != null)
                        fortschritt.Report(new Fortschritt { Aktuell = i, Gesamt = gesamt, Tabelle = s.Tabelle });

                    string sql = BaueInsertSql(s, srcId, offset, copySet);
                    if (sql == null) continue;
                    using (OleDbCommand c = new OleDbCommand(sql, conn, trans))
                    {
                        if (s.NameSpalte != null) c.Parameters.Add(new OleDbParameter("@name", neuerName));
                        c.ExecuteNonQuery();
                    }
                }
                if (fortschritt != null)
                    fortschritt.Report(new Fortschritt { Aktuell = gesamt, Gesamt = gesamt, Tabelle = "" });

                trans.Commit();

                // Ä24: Geräteanker der kopierten Kostenpositionen auf die
                // KOPIERTEN Geräte umstellen. Der generische Lauf versetzt
                // ID_Anlage (FK_MAP); ID_AnlageGeraet kann er nicht versetzen —
                // die Zieltabelle hängt an der Komponente. Ohne den Nachzug
                // zeigten die Anker auf die Geräte des QUELLprojekts, und der
                // erste Anlagen-Wizard-Lauf der Kopie löste die Zuordnungen
                // (Befund 27.08.2026: WP-Positionen der Varianten 1038/1039).
                int neuId = (int)(srcId + offset["Tab_Projekt"]);
                try { KostenProjektPositionenCtrl.AnkerNachziehen(neuId); } catch { }
                return neuId;
            }
            catch (Exception ex)
            {
                if (trans != null) { try { trans.Rollback(); } catch { } }
                MessageBox.Show("Fehler beim Duplizieren des Projekts: " + ex.Message);
                return -1;
            }
            finally
            {
                // Deterministische Freigabe: Transaktion und Verbindung immer schliessen/entsorgen,
                // auch im Fehlerfall. (BeginTransaction kann selbst werfen -> dann sind beide null.)
                if (trans != null) { try { trans.Dispose(); } catch { } }
                if (conn != null) { try { conn.Close(); } catch { } try { conn.Dispose(); } catch { } }
            }
        }

        // Ermittelt die zu kopierenden Tabellen generisch aus dem Schema.
        internal List<Spec> ErmittlePlan(OleDbConnection conn, OleDbTransaction trans)
        {
            // Zuerst die deklarierten Beziehungen einlesen (fuer Reihenfolge + Offset-Ziel).
            _echteFks = LiesEchteFks(conn, trans);

            var plan = new Dictionary<string, Spec>(StringComparer.OrdinalIgnoreCase);
            var uebrig = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase); // Tabellen ohne eigenen Projektbezug -> evtl. FK-gebundene Kinder

            // Tab_Projekt als Wurzel (Name wird ersetzt).
            plan["Tab_Projekt"] = new Spec { Tabelle = "Tab_Projekt", Pk = "ID", Filter = "ID = {0}", NameSpalte = "Projektname", Cols = Spalten("Tab_Projekt") };

            DataTable tabs = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
            foreach (DataRow row in tabs.Rows)
            {
                string name = row["TABLE_NAME"].ToString();
                if (Ausgeschlossen(name)) continue;
                if (string.Equals(name, "Tab_Projekt", StringComparison.OrdinalIgnoreCase)) continue;

                List<string> cols = Spalten(name);
                if (cols == null || cols.Count == 0) continue;

                if (KINDER.ContainsKey(name))
                {
                    plan[name] = new Spec { Tabelle = name, Pk = ErmittlePk(cols), Filter = KINDER[name], Cols = cols };
                }
                else if (Enthaelt(cols, "ID_Projekt"))
                {
                    plan[name] = new Spec { Tabelle = name, Pk = ErmittlePk(cols), Filter = "[ID_Projekt] = {0}", Cols = cols };
                }
                else if (Enthaelt(cols, "ProjektID"))
                {
                    plan[name] = new Spec { Tabelle = name, Pk = ErmittlePk(cols), Filter = "[ProjektID] = {0}", Cols = cols };
                }
                else
                {
                    // Kein eigener Projektbezug: koennte ueber einen Fremdschluessel an einer kopierten
                    // Tabelle haengen (z. B. eine neue Detailtabelle mit FK auf Tab_DBTagV) -> unten pruefen.
                    uebrig[name] = cols;
                }
            }

            // Auto-Erkennung FK-gebundener Kind-Tabellen (ohne eigenes ID_Projekt), iterativ bis Fixpunkt.
            // Eine uebrige Tabelle wird mitkopiert, sobald sie einen ERZWUNGENEN Fremdschluessel auf eine
            // bereits im Plan stehende Tabelle hat. Ihr Filter wird aus dem Elternfilter zusammengesetzt,
            // sodass genau die zum Projekt gehoerenden Zeilen kopiert werden (auch Enkel/Urenkel).
            // (Erfordert echte FKs aus _echteFks; ohne diese greifen nur ID_Projekt-Tabellen + feste KINDER.)
            bool neuHinzugefuegt = true;
            while (neuHinzugefuegt)
            {
                neuHinzugefuegt = false;
                foreach (KeyValuePair<string, List<string>> kv in new List<KeyValuePair<string, List<string>>>(uebrig))
                {
                    string name = kv.Key;
                    List<string> cols = kv.Value;

                    string fkCol = null, eltern = null;
                    foreach (string col in cols)
                    {
                        string p;
                        if (_echteFks.TryGetValue(name + "||" + col, out p) &&
                            plan.ContainsKey(p) &&
                            !string.Equals(p, name, StringComparison.OrdinalIgnoreCase))
                        { fkCol = col; eltern = p; break; }
                    }
                    if (fkCol == null) continue;

                    Spec pSpec = plan[eltern];
                    string filter = "[" + fkCol + "] IN (SELECT [" + pSpec.Pk + "] FROM [" + eltern + "] WHERE " + pSpec.Filter + ")";
                    plan[name] = new Spec { Tabelle = name, Pk = ErmittlePk(cols), Filter = filter, Cols = cols };
                    uebrig.Remove(name);
                    neuHinzugefuegt = true;
                }
            }

            var alle = new List<Spec>(plan.Values);

            // Nach Fremdschluessel-Abhaengigkeiten sortieren: referenzierte Tabelle zuerst,
            // damit erzwungene Beziehungen (referentielle Integritaet) beim INSERT erfuellt sind.
            return Sortiere(alle);
        }

        /// <summary>
        /// Liest die in Access deklarierten Fremdschluessel (erzwungene Beziehungen) aus dem Schema.
        /// Rueckgabe: "FK_Tabelle||FK_Spalte" -> PK_Tabelle (referenzierte Tabelle).
        /// Faellt bei nicht unterstuetztem Schema-Rowset auf eine leere Map zurueck.
        /// </summary>
        private Dictionary<string, string> LiesEchteFks(OleDbConnection conn, OleDbTransaction trans)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 1) Bevorzugt ueber das Standard-Schema-Rowset.
            try
            {
                DataTable fk = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, null);
                if (fk != null)
                {
                    foreach (DataRow r in fk.Rows)
                    {
                        string fkT = r["FK_TABLE_NAME"] != DBNull.Value ? r["FK_TABLE_NAME"].ToString() : null;
                        string fkC = r["FK_COLUMN_NAME"] != DBNull.Value ? r["FK_COLUMN_NAME"].ToString() : null;
                        string pkT = r["PK_TABLE_NAME"] != DBNull.Value ? r["PK_TABLE_NAME"].ToString() : null;
                        if (!string.IsNullOrEmpty(fkT) && !string.IsNullOrEmpty(fkC) && !string.IsNullOrEmpty(pkT))
                            map[fkT + "||" + fkC] = pkT;
                    }
                }
            }
            catch { /* Provider unterstuetzt das Rowset nicht */ }

            if (map.Count > 0) return map;

            // 2) Fallback: Access-Systemtabelle MSysRelationships direkt lesen (dieselben Beziehungen).
            //    szObject = FK-Tabelle, szColumn = FK-Spalte, szReferencedObject = PK-Tabelle,
            //    grbit & 2 (dbRelationDontEnforce) gesetzt = NICHT erzwungen -> ueberspringen.
            //    (Erfordert Leserecht auf Systemobjekte; schlaegt es fehl, greift die FK_MAP-Heuristik.)
            try
            {
                using (var cmd = new OleDbCommand(
                    "SELECT szObject, szColumn, szReferencedObject, grbit FROM MSysRelationships", conn, trans))
                using (OleDbDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        string fkT = rd["szObject"] != DBNull.Value ? rd["szObject"].ToString() : null;
                        string fkC = rd["szColumn"] != DBNull.Value ? rd["szColumn"].ToString() : null;
                        string pkT = rd["szReferencedObject"] != DBNull.Value ? rd["szReferencedObject"].ToString() : null;
                        int grbit = rd["grbit"] != DBNull.Value ? Convert.ToInt32(rd["grbit"]) : 0;
                        if ((grbit & 2) != 0) continue; // nicht erzwungen
                        if (!string.IsNullOrEmpty(fkT) && !string.IsNullOrEmpty(fkC) && !string.IsNullOrEmpty(pkT))
                            map[fkT + "||" + fkC] = pkT;
                    }
                }
            }
            catch { /* kein Zugriff auf MSysRelationships -> Fallback auf FK_MAP/FK_OVERRIDE */ }

            return map;
        }

        /// <summary>
        /// Topologische Sortierung der Kopier-Specs: jede Tabelle wird NACH allen Tabellen einsortiert,
        /// auf die sie verweist, damit die referenzierten (Eltern-)Datensaetze bereits existieren,
        /// bevor ein Kind eingefuegt wird.
        ///
        /// Fuer die REIHENFOLGE zaehlen NUR die tatsaechlich ERZWUNGENEN Beziehungen (aus dem
        /// Foreign_Keys-Schema) - genau diese werfen die RI-Fehler. Nicht erzwungene Fallback-FKs
        /// (FK_MAP) wuerden sonst Schein-Zyklen erzeugen (z. B. Tab_Gebaeude &lt;-&gt; Z_ProjektGebaeude).
        /// Nur wenn keine echten FKs vorliegen (Provider liefert das Rowset nicht), wird die
        /// FK_MAP-Heuristik als Fallback verwendet.
        ///
        /// Bei einem echten Zyklus wird er schonend aufgebrochen: es wird der Knoten mit den
        /// wenigsten offenen Abhaengigkeiten und - bei Gleichstand - den meisten Abhaengigen
        /// (also der "Elternteil") zuerst platziert.
        /// </summary>
        private List<Spec> Sortiere(List<Spec> specs)
        {
            var copySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Spec s in specs) copySet.Add(s.Tabelle);

            var deps = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            if (_echteFks.Count > 0)
            {
                // Nur erzwungene Beziehungen als Reihenfolge-Abhaengigkeit.
                foreach (Spec s in specs) deps[s.Tabelle] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, string> kv in _echteFks)
                {
                    int sep = kv.Key.IndexOf("||", StringComparison.Ordinal);
                    if (sep <= 0) continue;
                    string fkT = kv.Key.Substring(0, sep);
                    string pkT = kv.Value;
                    if (!deps.ContainsKey(fkT)) continue;                                   // FK-Tabelle wird nicht kopiert
                    if (!copySet.Contains(pkT)) continue;                                   // Ziel wird nicht kopiert (Katalog/_STAMM)
                    if (string.Equals(fkT, pkT, StringComparison.OrdinalIgnoreCase)) continue; // Selbstbezug
                    deps[fkT].Add(pkT);
                }
            }
            else
            {
                // Fallback: FK_MAP-Heuristik (kann Schein-Zyklen enthalten, wird unten aufgebrochen).
                foreach (Spec s in specs) deps[s.Tabelle] = Abhaengigkeiten(s, copySet);
            }

            var result = new List<Spec>();
            var erledigt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rest = new List<Spec>(specs);

            while (rest.Count > 0)
            {
                // 1) Alle Knoten platzieren, deren Abhaengigkeiten erfuellt sind (in Eingangsreihenfolge).
                int platziert = 0;
                for (int i = 0; i < rest.Count; i++)
                {
                    Spec s = rest[i];
                    bool bereit = true;
                    foreach (string d in deps[s.Tabelle])
                        if (!erledigt.Contains(d)) { bereit = false; break; }
                    if (!bereit) continue;

                    result.Add(s);
                    erledigt.Add(s.Tabelle);
                    rest.RemoveAt(i);
                    i--;
                    platziert++;
                }
                if (platziert > 0) continue;

                // 2) Zyklus: Elternteil zuerst -> wenigste offene Abhaengigkeiten, bei Gleichstand
                //    die meisten Abhaengigen (Knoten, auf den die meisten anderen verweisen).
                int best = 0, bestOffen = int.MaxValue, bestAbh = -1;
                for (int i = 0; i < rest.Count; i++)
                {
                    int offen = 0;
                    foreach (string d in deps[rest[i].Tabelle])
                        if (!erledigt.Contains(d)) offen++;

                    int abhaengige = 0;
                    foreach (Spec other in rest)
                        if (deps[other.Tabelle].Contains(rest[i].Tabelle)) abhaengige++;

                    if (offen < bestOffen || (offen == bestOffen && abhaengige > bestAbh))
                    {
                        bestOffen = offen; bestAbh = abhaengige; best = i;
                    }
                }
                result.Add(rest[best]);
                erledigt.Add(rest[best].Tabelle);
                rest.RemoveAt(best);
            }
            return result;
        }

        /// <summary>
        /// Liefert die Tabellen, von denen 's' per Fremdschluessel abhaengt (die also VORHER kopiert
        /// sein muessen). Nur Ziele, die selbst mitkopiert werden (copySet) und nicht 's' selbst sind.
        /// </summary>
        private HashSet<string> Abhaengigkeiten(Spec s, HashSet<string> copySet)
        {
            var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (s.Cols == null) return deps;
            foreach (string col in s.Cols)
            {
                string ziel = ErmittleZieltabelle(s.Tabelle, col, s.Pk);
                if (ziel == null) continue;
                if (string.Equals(ziel, s.Tabelle, StringComparison.OrdinalIgnoreCase)) continue; // PK -> self
                if (copySet.Contains(ziel)) deps.Add(ziel);
            }
            return deps;
        }

        private static bool Ausgeschlossen(string name)
        {
            if (name == null) return true;
            string n = name.ToLowerInvariant();
            if (n.EndsWith("_stamm")) return true;
            if (n.StartsWith("msys") || n.StartsWith("~") || n.StartsWith("f_")) return true;
            if (KATALOG_TABELLEN.Contains(name)) return true;
            if (AUSNAHME_TABELLEN.Contains(name)) return true;
            return false;
        }

        private static bool Enthaelt(List<string> cols, string col)
        {
            foreach (string c in cols) if (string.Equals(c, col, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string ErmittlePk(List<string> cols)
        {
            foreach (string cand in new[] { "ID", "id", "ID_Z" }) if (Enthaelt(cols, cand)) return cand;
            return cols[0];
        }

        // Spalten einer Tabelle (in DB-Reihenfolge).
        private List<string> Spalten(string tabelle)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable("SELECT * FROM [" + tabelle + "] WHERE 1 = 0");
                if (dt == null) return null;
                var list = new List<string>();
                foreach (DataColumn dc in dt.Columns) list.Add(dc.ColumnName);
                return list;
            }
            catch { return null; }
        }

        // offset(T) = MAX(T.pk) - MIN(pk der Quellzeilen) + 1.
        private bool BerechneOffset(OleDbConnection conn, OleDbTransaction trans, Spec s, int srcId, out long offset)
        {
            offset = 0;
            string where = string.Format(s.Filter, srcId);
            object oMax, oMin;
            try
            {
                using (var cMax = new OleDbCommand("SELECT MAX([" + s.Pk + "]) FROM [" + s.Tabelle + "]", conn, trans))
                    oMax = cMax.ExecuteScalar();
                using (var cMin = new OleDbCommand("SELECT MIN([" + s.Pk + "]) FROM [" + s.Tabelle + "] WHERE " + where, conn, trans))
                    oMin = cMin.ExecuteScalar();
            }
            catch { return false; }

            if (oMin == null || oMin == DBNull.Value) return false; // keine Quellzeilen
            long max = (oMax != null && oMax != DBNull.Value) ? Convert.ToInt64(oMax) : 0;
            long min = Convert.ToInt64(oMin);
            offset = max - min + 1;
            if (offset < 1) offset = 1;
            return true;
        }

        // Baut das generische INSERT ... SELECT.
        private string BaueInsertSql(Spec s, int srcId, Dictionary<string, long> offset, HashSet<string> copySet)
        {
            List<string> cols = s.Cols ?? Spalten(s.Tabelle);
            if (cols == null || cols.Count == 0) return null;

            var colList = new List<string>();
            var exprs = new List<string>();
            foreach (string col in cols)
            {
                colList.Add("[" + col + "]");

                if (s.NameSpalte != null && string.Equals(col, s.NameSpalte, StringComparison.OrdinalIgnoreCase))
                {
                    exprs.Add("?"); // neuer Projektname
                    continue;
                }

                string ziel = ErmittleZieltabelle(s.Tabelle, col, s.Pk);
                if (ziel != null && offset.ContainsKey(ziel) && copySet.Contains(ziel))
                    exprs.Add("IIF([" + col + "] > 0, [" + col + "] + " + offset[ziel] + ", [" + col + "])");
                else
                    exprs.Add("[" + col + "]");
            }

            string where = string.Format(s.Filter, srcId);
            return "INSERT INTO [" + s.Tabelle + "] (" + string.Join(", ", colList) + ") " +
                   "SELECT " + string.Join(", ", exprs) + " FROM [" + s.Tabelle + "] WHERE " + where;
        }

        // Liefert die Zieltabelle, deren Offset auf diese Spalte anzuwenden ist (oder null = nicht versetzen).
        internal string ErmittleZieltabelle(string tabelle, string col, string pk)
        {
            if (string.Equals(col, pk, StringComparison.OrdinalIgnoreCase)) return tabelle;              // PK -> self
            if (string.Equals(col, "ID_Projekt", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(col, "ProjektID", StringComparison.OrdinalIgnoreCase)) return "Tab_Projekt";
            if (KATALOG_SPALTEN.Contains(col)) return null;                                              // Katalog -> nicht versetzen

            // 1) Deklarierte Access-Beziehung (erzwungene ref. Integritaet) hat Vorrang.
            string real;
            if (_echteFks.TryGetValue(tabelle + "||" + col, out real)) return real;

            // 2) Fallback: handgepflegte Zuordnung fuer nicht deklarierte Beziehungen.
            Dictionary<string, string> ov;
            if (FK_OVERRIDE.TryGetValue(tabelle, out ov) && ov.ContainsKey(col)) return ov[col];         // mehrdeutig -> je Tabelle
            string ziel;
            if (FK_MAP.TryGetValue(col, out ziel)) return ziel;                                          // interner FK
            return null;                                                                                 // unbekannt -> unveraendert lassen
        }

        public static int ZeigeExportImportDialog(IWin32Window owner = null)
        {
            using (var dlg = new Form_ProjektExportImport())
            {
                return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.ImportierteProjektId : -1;
            }
        }
    }
}
