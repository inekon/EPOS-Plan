using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Exportiert ein Projekt in eine portable .wpx-Datei (ZIP + JSON) und importiert es wieder –
    /// auch in eine ANDERE Access-DB. Nutzt den generischen Plan aus ProjektDuplizierenCtrl.
    ///
    /// Dafür in ProjektDuplizierenCtrl von private auf internal stellen (oder Fassaden anlegen):
    ///   class Spec, ErmittlePlan(conn,trans), ErmittleZieltabelle(tab,col,pk).
    ///
    /// SCHEMA-DRIFT: Der Import ist tolerant. Es wird nur die SCHNITTMENGE aus exportierten und
    /// tatsächlich vorhandenen Spalten eingefügt; Tabellen, die es in der Ziel-DB nicht (mehr)
    /// gibt, werden übersprungen. Neue Ziel-Spalten müssen nullable/mit Default sein.
    /// </summary>
    // =====================================================================================
    //  VERWANDTSCHAFT MIT DEM MIGRATIONS-TOOL (AccessMigration, eigenständiges Tool)
    // -------------------------------------------------------------------------------------
    //  Diese Klasse und das Migrations-Tool lösen dasselbe Grundproblem – Access-Daten mit
    //  AutoWert-Surrogatschlüsseln zwischen Datenbanken bewegen, ohne Fremdschlüssel zu
    //  zerreißen –, nur in unterschiedlichem Umfang:
    //    * Migrations-Tool:  ganze DB (alt -> neue Versions-Vorlage), Kataloge INKLUSIVE.
    //    * Diese Klasse:     EIN Projekt in eine .wpx-Datei und zurück; Kataloge werden
    //                        NICHT mitkopiert, sondern im Ziel per Name wiedergefunden.
    //
    //  Gleiche Konzepte, gleiche Fallen – wer eines pflegt, sollte das andere kennen:
    //    * Natürlicher Schlüssel statt Autowert-ID:
    //        hier  KATALOG_NATURALKEY / KATALOG_SPALTE_ZU_TABELLE (fest verdrahtet)
    //        Tool  "matchColumns" in migration.config.json (+ automatische Ableitung aus
    //              den Unique-Indizes des Schemas; die JSON listet nur Ausnahmen).
    //    * FK-Umschlüsselung alt_ID -> neu_ID über den natürlichen Schlüssel des Elterns.
    //    * Original-IDs beibehalten, damit Verweise auflösen:
    //        hier  "fill"-Verfahren (FuelleKatalog)      Tool  "preserveIdTables".
    //    * Wertkonvertierung auf den Zielspaltentyp:
    //        hier  Passe()                               Tool  ValueCoercion.Coerce().
    //    * Verwaiste/fehlerhafte Zeilen überspringen statt abzubrechen:
    //        hier  Selbstheilung (NulleVerwaisteFks + Retry)  Tool  "skipRowsOnError".
    //    * AUTOWERT-Zähler nach dem Einfügen expliziter IDs nachziehen (sonst Fehler 3022):
    //        hier  ReseedAutoWerte()                     Tool  AutoNumberReseeder.
    //    * Umbenennungen (Tabelle/Feld) über Software-Updates hinweg:
    //        hier  (noch) nicht behandelt                Tool  "tableRenames"/"columnRenames".
    //
    //  KÜNFTIGE VEREINHEITLICHUNG (bewusst noch NICHT umgesetzt):
    //    Die fest verdrahteten Listen könnten durch dieselbe migration.config.json ersetzt
    //    werden (matchColumns = natürliche Schlüssel, tableRenames/columnRenames = Alias-Map),
    //    ergänzt um Laufzeit-Ableitung aus Schema-Beziehungen (GetOleDbSchemaTable) und
    //    Unique-Indizes. Bis dahin gelten die Listen unten als Fallback.
    // =====================================================================================
    public class ProjektExportImportCtrl
    {
        private const string FORMAT = "wp-projekt";
        // T3: Version 2 = Varianten-Baeume (projects/<i>/data/) + variantLinks.
        private const int FORMAT_VER = 2;

        private readonly ProjektDuplizierenCtrl _dup = new ProjektDuplizierenCtrl();

        // Cache: Zieltabelle -> (Spaltenname -> .NET-Datentyp). null = Tabelle nicht vorhanden.
        private readonly Dictionary<string, Dictionary<string, Type>> _typCache =
            new Dictionary<string, Dictionary<string, Type>>(StringComparer.OrdinalIgnoreCase);

        // In Access definierte Fremdschlüssel (für gezielte FK-Diagnose beim Import).
        private Dictionary<string, List<Fk>> _fks;
        // Projekt-Eigentabellen dieses Imports (in derselben Transaktion angelegt -> aus FK-Diagnose ausblenden).
        private HashSet<string> _projektTabellen;
        // Cache: "Zieltabelle||Schlüsselspalte" -> vorhandene Elternschlüssel (als Text).
        private Dictionary<string, HashSet<string>> _fkKeys;

        /// <summary>Verhalten, wenn der Zielname bereits existiert.</summary>
        public enum BeiVorhandenem { Abbrechen, Ueberschreiben, NeuerName }

        /// <summary>T4: Zeilenbericht des letzten Imports (Projekte, Varianten,
        /// Verknüpfungen, Hinweise) — der Dialog zeigt und sichert ihn.</summary>
        public List<string> LetzterBericht { get; private set; } = new List<string>();

        // ---- PROJEKT-SPEZIFISCH ------------------------------------------------------------
        private static readonly Dictionary<string, string> KATALOG_SPALTE_ZU_TABELLE =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "carrier_id",       "energy_carrier" },
            { "ID_Energieträger", "energy_carrier" },
            { "ID_Umrechnung",    "energy_conversion" },
            { "ID_Brennstoff",    "Tab_Brennstoff_Stamm" },
            { "ID_Type",          "Tab_Typ_Energieanlagen" },
            { "KomponentenID",    "Tab_KostenKomponente" },
            // TODO: bei Bedarf ID_Stamm / StammID / KategorieID ergänzen.
        };

        private static readonly Dictionary<string, string[]> KATALOG_NATURALKEY =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "energy_carrier",         new[] { "name" } },              // eindeutig über den Namen
            { "energy_conversion",      new[] { "id_brennstoff","from_unit","to_unit" } },
            { "Tab_Brennstoff_Stamm",   new[] { "Bezeichner" } },
            { "Tab_Typ_Energieanlagen", new[] { "Bezeichner" } },
            { "Tab_KostenKomponente",   new[] { "Komponente" } },
        };
        // -----------------------------------------------------------------------------------

        // ===================================================================================
        //  EXPORT
        // ===================================================================================
        public bool Exportieren(string projektName, string zielPfad,
            IProgress<ProjektDuplizierenCtrl.Fortschritt> fortschritt = null)
            => Exportieren(projektName, null, zielPfad, fortschritt);

        /// <summary>
        /// T3 (Konzept Projekttransfer): Export mit Varianten. Jede gewählte
        /// Variante reist als eigener Projektbaum unter <c>projects/&lt;i&gt;/data/</c>;
        /// die <c>Tab_Variante</c>-Verknüpfungen reisen NICHT als Tabellenzeilen
        /// (ihr <c>ID_ProjektRef</c> wäre über Paketgrenzen nicht versetzbar),
        /// sondern als <c>variantLinks</c> im Manifest und werden beim Import
        /// neu geschrieben.
        /// </summary>
        public bool Exportieren(string projektName, List<string> variantenProjekte, string zielPfad,
            IProgress<ProjektDuplizierenCtrl.Fortschritt> fortschritt = null)
        {
            int srcId = _dup.GetProjektId(projektName);
            if (srcId <= 0) { MessageBox.Show("Projekt '" + projektName + "' nicht gefunden."); return false; }

            var tx = DataRepository.BeginTransaction();
            OleDbConnection conn = tx.Item1; OleDbTransaction trans = tx.Item2;
            try
            {
                var plan = _dup.ErmittlePlan(conn, trans);
                List<TabMeta> manifestTabellen;
                var katalogRefs = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase);

                // Projekt-Tabellen (werden kopiert) und konfigurierte Natural-Key-Kataloge.
                var copySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in plan) copySet.Add(s.Tabelle);
                var konfigurierteKataloge = new HashSet<string>(KATALOG_SPALTE_ZU_TABELLE.Values, StringComparer.OrdinalIgnoreCase);

                // Echte Access-Beziehungen: FK-Spalte je Tabelle -> (Zieltabelle, Ziel-PK).
                var fks = LiesFremdschluessel(conn);
                // Referenzierte Zeilen aus NICHT kopierten Katalogen, die per Original-ID
                // aufgefüllt werden (Zieltabelle -> (PK-Spalte, benötigte IDs)).
                var fuellRefs = new Dictionary<string, KeyValuePair<string, HashSet<long>>>(StringComparer.OrdinalIgnoreCase);

                using (var zipStream = new FileStream(zielPfad, FileMode.Create))
                using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    // T3: EIN Baum-Schreiber für Stamm und Varianten — gleicher Plan,
                    // eigener Projektfilter, eigenes Zip-Präfix. Tab_Variante bleibt
                    // draußen (Verknüpfung = variantLinks im Manifest; eine mitreisende
                    // Zeile hätte ein nicht versetzbares ID_ProjektRef).
                    List<TabMeta> BaumSchreiben(int projektId, string prefix)
                    {
                        var tabellen = new List<TabMeta>();
                        int i = 0;
                        foreach (var s in plan)
                        {
                            fortschritt?.Report(new ProjektDuplizierenCtrl.Fortschritt
                            { Aktuell = i++, Gesamt = plan.Count, Tabelle = s.Tabelle });

                            if (s.Tabelle.Equals("Tab_Variante", StringComparison.OrdinalIgnoreCase)) continue;

                            DataTable dt = DataRepository.GetDataTable(
                                "SELECT * FROM [" + s.Tabelle + "] WHERE " + string.Format(s.Filter, projektId));
                            if (dt == null || dt.Rows.Count == 0) continue;

                            WriteEntry(zip, prefix + s.Tabelle + ".json", RowsToJson(dt));
                            tabellen.Add(new TabMeta { name = s.Tabelle, pk = s.Pk });

                            foreach (DataColumn c in dt.Columns)
                                if (KATALOG_SPALTE_ZU_TABELLE.TryGetValue(c.ColumnName, out string katTab))
                                {
                                    if (!katalogRefs.TryGetValue(katTab, out var ids))
                                        katalogRefs[katTab] = ids = new HashSet<long>();
                                    foreach (DataRow r in dt.Rows)
                                        if (r[c] != DBNull.Value) { long v = Convert.ToInt64(r[c]); if (v > 0) ids.Add(v); }
                                }

                            // Generisch: jede echte FK-Spalte, deren Zieltabelle NICHT kopiert wird
                            // und NICHT als Natural-Key-Katalog konfiguriert ist -> Original-ID auffüllen.
                            if (fks.TryGetValue(s.Tabelle, out var tabFks))
                                foreach (var fk in tabFks)
                                {
                                    if (copySet.Contains(fk.RefTab) || konfigurierteKataloge.Contains(fk.RefTab)) continue;
                                    if (!dt.Columns.Contains(fk.Col)) continue;
                                    if (!fuellRefs.TryGetValue(fk.RefTab, out var eintrag))
                                        fuellRefs[fk.RefTab] = eintrag = new KeyValuePair<string, HashSet<long>>(fk.RefCol, new HashSet<long>());
                                    foreach (DataRow r in dt.Rows)
                                        if (r[fk.Col] != DBNull.Value)
                                        { long v; if (long.TryParse(Convert.ToString(r[fk.Col]), out v) && v > 0) eintrag.Value.Add(v); }
                                }
                        }
                        return tabellen;
                    }

                    manifestTabellen = BaumSchreiben(srcId, "data/");

                    // T3: Varianten-Bäume + Verknüpfungen fürs Manifest.
                    var varMetas = new List<VarMeta>();
                    var links = new List<LinkMeta>();
                    DataTable eigenerLink = DataRepository.GetDataTable(
                        "SELECT v.Variantenname, p.Projektname FROM Tab_Variante AS v INNER JOIN Tab_Projekt AS p " +
                        "ON v.ID_ProjektRef = p.ID WHERE v.ID_Projekt = " + srcId);
                    if (eigenerLink != null && eigenerLink.Rows.Count > 0)
                        links.Add(new LinkMeta
                        {
                            projekt = projektName,
                            stamm = Convert.ToString(eigenerLink.Rows[0]["Projektname"]),
                            variantenname = Convert.ToString(eigenerLink.Rows[0]["Variantenname"])
                        });
                    int lauf = 0;
                    foreach (string vName in variantenProjekte ?? new List<string>())
                    {
                        int vid = _dup.GetProjektId(vName);
                        if (vid <= 0 || vid == srcId) continue;
                        var vTabellen = BaumSchreiben(vid, "projects/" + lauf + "/data/");
                        varMetas.Add(new VarMeta { name = vName, tables = vTabellen });
                        DataTable lnk = DataRepository.GetDataTable(
                            "SELECT Variantenname FROM Tab_Variante WHERE ID_Projekt = " + vid);
                        links.Add(new LinkMeta
                        {
                            projekt = vName,
                            stamm = projektName,
                            variantenname = (lnk != null && lnk.Rows.Count > 0)
                                ? Convert.ToString(lnk.Rows[0]["Variantenname"]) : vName
                        });
                        lauf++;
                    }

                    var katalogMeta = new List<KatMeta>();
                    foreach (var kv in katalogRefs)
                    {
                        if (kv.Value.Count == 0) continue;
                        string katTab = kv.Key, pk = "id";
                        DataTable dt = DataRepository.GetDataTable(
                            "SELECT * FROM [" + katTab + "] WHERE [" + pk + "] IN (" + string.Join(",", kv.Value) + ")");
                        if (dt == null || dt.Rows.Count == 0) continue;
                        WriteEntry(zip, "catalogs/" + katTab + ".json", RowsToJson(dt));
                        katalogMeta.Add(new KatMeta
                        {
                            name = katTab,
                            pk = pk,
                            naturalKey = KATALOG_NATURALKEY.TryGetValue(katTab, out var nk) ? nk : new[] { pk }
                        });
                    }

                    // Auffüll-Kataloge exportieren (mit echter PK-Spalte).
                    var fuellMeta = new List<KatMeta>();
                    foreach (var kv in fuellRefs)
                    {
                        if (kv.Value.Value.Count == 0) continue;
                        string katTab = kv.Key, pk = kv.Value.Key;
                        DataTable dt = DataRepository.GetDataTable(
                            "SELECT * FROM [" + katTab + "] WHERE [" + pk + "] IN (" + string.Join(",", kv.Value.Value) + ")");
                        if (dt == null || dt.Rows.Count == 0) continue;
                        WriteEntry(zip, "fill/" + katTab + ".json", RowsToJson(dt));
                        fuellMeta.Add(new KatMeta { name = katTab, pk = pk, naturalKey = new[] { pk } });
                    }

                    var manifest = new Manifest
                    {
                        format = FORMAT,
                        formatVersion = FORMAT_VER,
                        // B2 (Konzept Projekttransfer T2): der echte Migrationsstand —
                        // der Import lehnt Pakete mit anderem Stand ab.
                        schemaVersion = SchemaMigration.ZIEL_VERSION,
                        exportedUtc = DateTime.UtcNow.ToString("o"),
                        sourceProject = projektName,
                        tables = manifestTabellen,
                        catalogs = katalogMeta,
                        fill = fuellMeta,
                        variants = varMetas,
                        variantLinks = links
                    };
                    WriteEntry(zip, "manifest.json",
                        JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
                }
                fortschritt?.Report(new ProjektDuplizierenCtrl.Fortschritt { Aktuell = plan.Count, Gesamt = plan.Count, Tabelle = "" });
                trans.Rollback();
                return true;
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                MessageBox.Show("Fehler beim Export: " + ex.Message); return false;
            }
            finally { try { trans.Dispose(); } catch { } try { conn.Dispose(); } catch { } }
        }

        // ===================================================================================
        //  IMPORT
        // ===================================================================================
        public int Importieren(string quellPfad, string gewuenschterName, BeiVorhandenem modus,
            IProgress<ProjektDuplizierenCtrl.Fortschritt> fortschritt, out string fehler)
        {
            fehler = null;
            Manifest man;
            var tableRows = new Dictionary<string, List<Dictionary<string, JsonElement>>>();
            var variantRows = new List<Dictionary<string, List<Dictionary<string, JsonElement>>>>();
            var catalogRows = new Dictionary<string, List<Dictionary<string, JsonElement>>>();
            var fillRows = new Dictionary<string, List<Dictionary<string, JsonElement>>>();

            using (var zip = ZipFile.OpenRead(quellPfad))
            {
                man = JsonSerializer.Deserialize<Manifest>(ReadEntry(zip, "manifest.json"));
                if (man == null || man.format != FORMAT) { fehler = "Kein gültiges Projektpaket."; return -1; }

                // B2 (Konzept Projekttransfer T2): Schemastände müssen übereinstimmen —
                // die Datenmigrationen laufen datenbankweit genau einmal, ein Paket mit
                // anderem Stand schleuste still Altdaten ein. schemaVersion 0 = Altpaket
                // (vor T2 exportiert) und bleibt zugelassen.
                if (man.schemaVersion != 0 && man.schemaVersion != SchemaMigration.ZIEL_VERSION)
                {
                    fehler = "Das Paket wurde mit Schemastand " + man.schemaVersion +
                             " exportiert, dieser Rechner arbeitet mit Stand " +
                             SchemaMigration.ZIEL_VERSION +
                             ". Bitte beide Rechner auf denselben Programmstand bringen " +
                             "und das Projekt neu exportieren.";
                    return -1;
                }
                foreach (var t in man.tables)
                    tableRows[t.name] = LiesZeilen(ReadEntry(zip, "data/" + t.name + ".json"));
                // T3: Varianten-Baeume (Paketformat V2, projects/<i>/data/...).
                for (int vi = 0; vi < (man.variants?.Count ?? 0); vi++)
                {
                    var vr = new Dictionary<string, List<Dictionary<string, JsonElement>>>();
                    foreach (var t in man.variants[vi].tables)
                        vr[t.name] = LiesZeilen(ReadEntry(zip, "projects/" + vi + "/data/" + t.name + ".json"));
                    variantRows.Add(vr);
                }
                foreach (var k in man.catalogs ?? new List<KatMeta>())
                    catalogRows[k.name] = LiesZeilen(ReadEntry(zip, "catalogs/" + k.name + ".json"));
                foreach (var k in man.fill ?? new List<KatMeta>())
                    fillRows[k.name] = LiesZeilen(ReadEntry(zip, "fill/" + k.name + ".json"));
            }

            // Zielnamen / Konfliktbehandlung bestimmen.
            string ziel = string.IsNullOrWhiteSpace(gewuenschterName) ? man.sourceProject : gewuenschterName;
            int existierId = _dup.GetProjektId(ziel);
            int ueberschreibId = 0;
            if (existierId > 0)
            {
                if (modus == BeiVorhandenem.Abbrechen)
                { fehler = "Ein Projekt '" + ziel + "' existiert bereits."; return -1; }
                if (modus == BeiVorhandenem.NeuerName) ziel = EindeutigerName(ziel);
                else ueberschreibId = existierId;   // Ueberschreiben
            }

            // Beziehungen auf einer FRISCHEN Verbindung lesen (vor der Transaktion), damit das
            // Schema-Rowset zuverlässig kommt und die FK-Behandlung sicher greift.
            _fks = null;
            try
            {
                using (var schemaConn = new OleDbConnection(DataRepository.GetConnectionString()))
                { schemaConn.Open(); _fks = LiesFremdschluessel(schemaConn); }
            }
            catch { _fks = null; }

            var tx = DataRepository.BeginTransaction();
            OleDbConnection conn = tx.Item1; OleDbTransaction trans = tx.Item2;
            try
            {
                if (_fks == null || _fks.Count == 0) _fks = LiesFremdschluessel(conn);   // Fallback über Transaktionsverbindung

                // B1 (Konzept Projekttransfer T1): Die Umschlüsselung fragt
                // ErmittleZieltabelle des Duplizierers — dessen Beziehungswissen
                // lud bisher nur der EXPORT (ErmittlePlan). Ein reiner Import
                // ließ damit jede Beziehung außerhalb der FK_MAP unversetzt
                // (Befund „Tab_Ergebnis[ID] FEHLT"). Jetzt wird es hier geladen.
                try { _dup.BeziehungenLaden(conn, trans); } catch { }

                _projektTabellen = new HashSet<string>(man.tables.Select(x => x.name), StringComparer.OrdinalIgnoreCase);
                _fkKeys = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                int schritt = 0;
                int gesamt = man.tables.Count + (man.catalogs?.Count > 0 ? 1 : 0) + (ueberschreibId > 0 ? 1 : 0);
                for (int vi = 0; vi < (man.variants?.Count ?? 0); vi++)
                    gesamt += man.variants[vi].tables.Count;

                // 0) Vorhandenes Projekt löschen (Überschreiben).
                if (ueberschreibId > 0)
                {
                    fortschritt?.Report(new ProjektDuplizierenCtrl.Fortschritt { Aktuell = schritt++, Gesamt = gesamt, Tabelle = "(altes Projekt entfernen)" });
                    LoescheProjekt(conn, trans, ueberschreibId);
                }

                // 1) Kataloge auflösen -> katMap.
                var katMap = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                if (man.catalogs?.Count > 0)
                {
                    fortschritt?.Report(new ProjektDuplizierenCtrl.Fortschritt { Aktuell = schritt++, Gesamt = gesamt, Tabelle = "(Kataloge)" });
                    foreach (var k in man.catalogs)
                        if (ZielSpalten(k.name) != null)
                            LoeseKatalogAuf(conn, trans, k, catalogRows[k.name], katMap);
                }

                // 1b) Referenzierte Katalogzeilen mit Original-ID auffüllen (falls im Ziel fehlend).
                //     Sichert die referenzielle Integrität für nicht kopierte Katalogtabellen
                //     (z. B. Tab_KostenGruppenKatalog über KategorieID). Keine Umschlüsselung.
                if (man.fill?.Count > 0)
                {
                    fortschritt?.Report(new ProjektDuplizierenCtrl.Fortschritt { Aktuell = schritt, Gesamt = gesamt, Tabelle = "(Referenzdaten)" });
                    foreach (var k in man.fill)
                        if (fillRows.ContainsKey(k.name))
                            FuelleKatalog(conn, trans, k, fillRows[k.name]);
                }

                // 2)+3) Stamm-Projektbaum einfügen (Offsets + Umschlüsselung in
                // BaumEinfuegen — T3: derselbe Weg trägt auch die Varianten).
                var berichte = new List<string>();
                var nameZuId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int neueProjektId = BaumEinfuegen(conn, trans, man.tables, tableRows, ziel,
                                                  katMap, fortschritt, ref schritt, gesamt);
                if (neueProjektId > 0) nameZuId[man.sourceProject ?? ziel] = neueProjektId;
                berichte.Add("Projekt \u201E" + ziel + "\u201C importiert (" + man.tables.Count + " Tabellen).");

                // T3: Varianten-Bäume — der gewählte Konfliktmodus gilt für alle (TF2).
                for (int vi = 0; vi < (man.variants?.Count ?? 0); vi++)
                {
                    string vQuelle = man.variants[vi].name;
                    string vZiel = vQuelle;
                    int vExist = _dup.GetProjektId(vZiel);
                    if (vExist > 0)
                    {
                        if (modus == BeiVorhandenem.Abbrechen)
                            throw new Exception("Ein Projekt '" + vZiel + "' existiert bereits (Variante des Pakets) - Import abgebrochen, nichts geändert.");
                        if (modus == BeiVorhandenem.NeuerName) vZiel = EindeutigerName(vZiel);
                        else
                        {
                            fortschritt?.Report(new ProjektDuplizierenCtrl.Fortschritt { Aktuell = schritt, Gesamt = gesamt, Tabelle = "(alte Variante entfernen)" });
                            LoescheProjekt(conn, trans, vExist);
                        }
                    }
                    int vId = BaumEinfuegen(conn, trans, man.variants[vi].tables, variantRows[vi], vZiel,
                                            katMap, fortschritt, ref schritt, gesamt);
                    if (vId > 0) nameZuId[vQuelle] = vId;
                    berichte.Add("Variante \u201E" + vZiel + "\u201C importiert (" + man.variants[vi].tables.Count + " Tabellen).");
                }

                // T3: Verknüpfungen wiederherstellen — Tab_Variante reist nicht als
                // Tabellenzeile (ID_ProjektRef nicht versetzbar), sondern als Manifest-Link.
                foreach (var link in man.variantLinks ?? new List<LinkMeta>())
                {
                    try
                    {
                        int pId = nameZuId.TryGetValue(link.projekt ?? "", out int p1) ? p1 : 0;
                        int sId = nameZuId.TryGetValue(link.stamm ?? "", out int s1) ? s1 : ProjektIdInTrans(conn, trans, link.stamm);
                        if (pId <= 0 || sId <= 0 || pId == sId)
                        {
                            berichte.Add("Hinweis: Verknüpfung \u201E" + link.projekt + "\u201C -> \u201E" + link.stamm +
                                         "\u201C nicht herstellbar (Stamm nicht im Paket und nicht am Ziel) - das Projekt steht eigenständig.");
                            continue;
                        }
                        int neuVid;
                        using (var c = new OleDbCommand("SELECT MAX(ID) FROM Tab_Variante", conn, trans))
                        { object m = c.ExecuteScalar(); neuVid = ((m == null || m == DBNull.Value) ? 0 : Convert.ToInt32(m)) + 1; }
                        using (var c = new OleDbCommand(
                            "INSERT INTO Tab_Variante (ID, ID_Projekt, ID_ProjektRef, Variantenname) VALUES (?, ?, ?, ?)", conn, trans))
                        {
                            c.Parameters.AddWithValue("@id", neuVid);
                            c.Parameters.AddWithValue("@p", pId);
                            c.Parameters.AddWithValue("@r", sId);
                            c.Parameters.AddWithValue("@n", (object)(link.variantenname ?? "") ?? DBNull.Value);
                            c.ExecuteNonQuery();
                        }
                        berichte.Add("Als Variante \u201E" + (string.IsNullOrEmpty(link.variantenname) ? link.projekt : link.variantenname) + "\u201C verknüpft.");
                    }
                    catch (Exception exLink)
                    { berichte.Add("Hinweis: Verknüpfung \u201E" + link.projekt + "\u201C fehlgeschlagen: " + exLink.Message); }
                }

                trans.Commit();
                fortschritt?.Report(new ProjektDuplizierenCtrl.Fortschritt { Aktuell = gesamt, Gesamt = gesamt, Tabelle = "" });

                // Nach dem Import die AutoWert-Zähler der kopierten Tabellen auf MAX+1 setzen.
                // Beim Einfügen expliziter (versetzter) IDs führt ACE den Zähler NICHT nach – die
                // Anwendung bekäme sonst beim nächsten regulären Insert eine bereits vergebene ID
                // (Fehler 3022 "doppelter Schlüssel"). Läuft nach dem Commit, ohne den Import zu gefährden.
                var alleTabellen = new List<TabMeta>(man.tables);
                for (int vi = 0; vi < (man.variants?.Count ?? 0); vi++)
                    alleTabellen.AddRange(man.variants[vi].tables);
                try { ReseedAutoWerte(alleTabellen); } catch { }

                // B3 (Konzept Projekttransfer T2): Die komponentenabhängigen Kostenanker
                // (Tab_ProjektWerte.ID_AnlageGeraet -> Tab_WP/Tab_Heizkessel/... je
                // Komponente) kann die generische FK-Umschlüsselung nicht kennen — sie
                // kämen mit den Geräte-IDs des QUELLrechners an, und die Ä21-Selbst-
                // heilung löste die Zuordnungen beim ersten UI-Aufbau ehrlich auf
                // („ohne Anlagenzuordnung", das Ä24-Befundbild). Aus den bereits
                // umgeschlüsselten, gültigen Anlagenzuordnungen neu ableiten —
                // derselbe Baustein wie im Duplizierer seit Ä24. T3: je Projektbaum.
                foreach (var kvp in nameZuId)
                    try { KostenProjektPositionenCtrl.AnkerNachziehen(kvp.Value); } catch { }

                LetzterBericht = berichte;
                return neueProjektId;
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                fehler = ex.Message; return -1;
            }
            finally { try { trans.Dispose(); } catch { } try { conn.Dispose(); } catch { } }
        }

        // ---- T3: EIN Projektbaum (Tabellenliste + Zeilen) unter zielName einfügen ----------
        // Liefert die neue Projekt-Id (Tab_Projekt-PK + Offset) oder -1. Wirft bei
        // Einfügefehlern (der Aufrufer rollt die gesamte Transaktion zurück).
        private int BaumEinfuegen(OleDbConnection conn, OleDbTransaction trans,
            List<TabMeta> tabellen, Dictionary<string, List<Dictionary<string, JsonElement>>> rows,
            string zielName, Dictionary<string, long> katMap,
            IProgress<ProjektDuplizierenCtrl.Fortschritt> fortschritt, ref int schritt, int gesamt)
        {
            // Je Baum: die FK-Vorablogik unterscheidet „reist mit" von „Zielbestand".
            _projektTabellen = new HashSet<string>(tabellen.Select(x => x.name), StringComparer.OrdinalIgnoreCase);

            // Offsets je Projekt-Tabelle (nur vorhandene Tabellen).
            var offset = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in tabellen)
                if (ZielSpalten(t.name) != null && rows[t.name].Count > 0)
                    offset[t.name] = BerechneOffset(conn, trans, t.name, t.pk, rows[t.name]);

            // Einfügen in FK-Reihenfolge, Spalten-Schnittmenge, alles umschlüsseln.
            foreach (var t in tabellen)
            {
                fortschritt?.Report(new ProjektDuplizierenCtrl.Fortschritt { Aktuell = schritt++, Gesamt = gesamt, Tabelle = t.name });

                Dictionary<string, Type> zielTypen = ZielTypen(t.name);
                if (zielTypen == null) continue;                       // Tabelle in Ziel-DB nicht (mehr) vorhanden
                if (!offset.ContainsKey(t.name)) continue;

                // FK-Spalten dieser Tabelle je Spaltenname (für Verwaisten-Behandlung).
                var fkByCol = new Dictionary<string, Fk>(StringComparer.OrdinalIgnoreCase);
                if (_fks != null && _fks.TryGetValue(t.name, out var flist))
                    foreach (var f in flist) fkByCol[f.Col] = f;

                foreach (var row in rows[t.name])
                {
                    var colNames = new List<string>(); var cols = new List<string>(); var ph = new List<string>();
                    var vals = new List<object>(); var typen = new List<Type>();
                    int i = 0;
                    foreach (var kv in row)
                    {
                        if (!zielTypen.ContainsKey(kv.Key)) continue;  // Spalte gibt es im Ziel nicht -> überspringen
                        object val = (t.name.Equals("Tab_Projekt", StringComparison.OrdinalIgnoreCase) &&
                                      kv.Key.Equals("Projektname", StringComparison.OrdinalIgnoreCase))
                            ? zielName
                            : Umschluessele(t.name, kv.Key, t.pk, kv.Value, offset, katMap);
                        // Vorab-Behandlung verwaister/leerer FK-Werte (siehe unten auch die Selbstheilung).
                        if (fkByCol.TryGetValue(kv.Key, out var fk) && val != null && !(val is DBNull))
                        {
                            string sv = Convert.ToString(val);
                            if (string.IsNullOrWhiteSpace(sv))
                                val = null;   // leerer Verweis = kein Verweis -> NULL
                            else if (_projektTabellen.Contains(fk.RefTab) && !offset.ContainsKey(fk.RefTab))
                                // B1-Randfall: Die Elterntabelle gehört zum Paket, kam aber
                                // OHNE Zeilen mit (Export überspringt leere Tabellen) — der
                                // Verweis kann nur auf Fremdes im Ziel zeigen. Ehrlich lösen
                                // statt still einen fremden Datensatz zu referenzieren.
                                val = null;
                            else if (!_projektTabellen.Contains(fk.RefTab))
                            {
                                var set = LadeElternSchluessel(conn, trans, fk.RefTab, fk.RefCol);
                                if (!set.Contains(sv))
                                {
                                    if (val is string && StelleTextKatalogSicher(conn, trans, fk.RefTab, fk.RefCol, sv))
                                        set.Add(sv);   // Namen anlernen, Wert bleibt
                                    else
                                        val = null;
                                }
                            }
                        }
                        colNames.Add(kv.Key); cols.Add("[" + kv.Key + "]"); ph.Add("@p" + (i++));
                        vals.Add(val); typen.Add(zielTypen[kv.Key]);
                    }
                    if (cols.Count == 0) continue;

                    Exception err = FuehreInsertAus(t.name, cols, ph, vals, typen, conn, trans);
                    if (err != null)
                    {
                        // SELBSTHEILUNG: tatsächlich verwaiste FK-Werte (in der Transaktion geprüft)
                        // nullen und den INSERT genau einmal wiederholen. Unabhängig von der Vorab-Logik.
                        int genullt = NulleVerwaisteFks(t.name, colNames, vals, conn, trans);
                        if (genullt > 0) err = FuehreInsertAus(t.name, cols, ph, vals, typen, conn, trans);
                        if (err != null)
                            throw new Exception("\r\n" + VolleDiagnoseWerte(t.name, colNames, vals, typen, conn, trans) + ":: " + err.Message, err);
                    }
                }
            }

            string projPk = tabellen.First(x => x.name.Equals("Tab_Projekt", StringComparison.OrdinalIgnoreCase)).pk;
            long altProjId = rows["Tab_Projekt"][0][projPk].GetInt64();
            return offset.ContainsKey("Tab_Projekt") ? (int)(altProjId + offset["Tab_Projekt"]) : -1;
        }

        // T3: Projekt-Id INNERHALB der Import-Transaktion nachschlagen (der frisch
        // eingefügte Stamm ist für DataRepository-Verbindungen noch unsichtbar).
        private int ProjektIdInTrans(OleDbConnection conn, OleDbTransaction trans, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            try
            {
                using (var c = new OleDbCommand("SELECT ID FROM Tab_Projekt WHERE Projektname = ?", conn, trans))
                {
                    c.Parameters.AddWithValue("@n", name);
                    object o = c.ExecuteScalar();
                    return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
                }
            }
            catch { return 0; }
        }

        // ---- AutoWert-Zähler nachziehen ----------------------------------------------------
        // Setzt für die kopierten Projekt-Tabellen den AutoWert-Zähler auf MAX(pk)+1.
        // Nur ECHTE AutoWert-Spalten werden angefasst (über ADOX erkannt) – sonst würde
        // ALTER ... COUNTER eine manuelle Long-Spalte fälschlich in einen AutoWert umwandeln.
        // Ohne ADOX (leere Erkennung) wird bewusst NICHTS geändert.
        private void ReseedAutoWerte(List<TabMeta> tabellen)
        {
            HashSet<string> autoSpalten = LiesAutoWertSpalten();
            if (autoSpalten.Count == 0) return;   // keine sichere Erkennung -> nichts anfassen

            // Eigene Verbindung: Fehler landen in unserem catch, KEINE MessageBox von DataRepository.
            using (var conn = new OleDbConnection(DataRepository.GetConnectionString()))
            {
                try { conn.Open(); } catch { return; }
                foreach (var t in tabellen)
                {
                    if (string.IsNullOrEmpty(t.pk)) continue;
                    if (!autoSpalten.Contains(t.name + "||" + t.pk)) continue;   // nur echte AutoWerte
                    try
                    {
                        long max = 0;
                        using (var c = new OleDbCommand("SELECT MAX([" + t.pk + "]) FROM [" + t.name + "]", conn))
                        { object o = c.ExecuteScalar(); if (o != null && o != DBNull.Value) max = Convert.ToInt64(o); }
                        // Zähler auf MAX+1 setzen. Schlägt bei beziehungsgebundenen Eltern-Spalten
                        // fehl (z. B. Tab_Projekt.ID) – dann bleibt der Zähler unverändert (still abgefangen).
                        using (var c = new OleDbCommand(
                            "ALTER TABLE [" + t.name + "] ALTER COLUMN [" + t.pk + "] COUNTER(" + (max + 1) + ",1)", conn))
                            c.ExecuteNonQuery();
                    }
                    catch { }
                }
            }
        }

        // Liest die AutoWert-Spalten der DB über ADOX (spät gebunden -> keine feste Projekt-
        // referenz nötig). Rückgabe "Tabelle||Spalte". Leeres Set = ADOX nicht verfügbar.
        private HashSet<string> LiesAutoWertSpalten()
        {
            var res = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            object catObj = null;
            try
            {
                Type tCat = Type.GetTypeFromProgID("ADOX.Catalog");
                if (tCat == null) return res;
                catObj = Activator.CreateInstance(tCat);
                dynamic cat = catObj;
                cat.ActiveConnection = DataRepository.GetConnectionString();
                foreach (dynamic tbl in cat.Tables)
                {
                    string typ = Convert.ToString(tbl.Type);
                    if (!string.Equals(typ, "TABLE", StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (dynamic col in tbl.Columns)
                    {
                        try
                        {
                            object v = col.Properties["Autoincrement"].Value;
                            if (v is bool b && b) res.Add(Convert.ToString(tbl.Name) + "||" + Convert.ToString(col.Name));
                        }
                        catch { }
                    }
                }
            }
            catch { res.Clear(); }
            finally
            {
                try { if (catObj != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(catObj); } catch { }
            }
            return res;
        }

        // ---- Umschlüsselung ----------------------------------------------------------------
        private object Umschluessele(string tab, string col, string pk, JsonElement je,
            Dictionary<string, long> offset, Dictionary<string, long> katMap)
        {
            object raw = JsonToObject(je);
            if (raw == null) return DBNull.Value;

            if (col.Equals(pk, StringComparison.OrdinalIgnoreCase))
                return Convert.ToInt64(raw) + offset[tab];
            if (col.Equals("ID_Projekt", StringComparison.OrdinalIgnoreCase) ||
                col.Equals("ProjektID", StringComparison.OrdinalIgnoreCase))
                return offset.ContainsKey("Tab_Projekt") ? Convert.ToInt64(raw) + offset["Tab_Projekt"] : raw;

            if (KATALOG_SPALTE_ZU_TABELLE.TryGetValue(col, out string katTab))
            {
                long v = Convert.ToInt64(raw);
                if (v <= 0) return raw;
                return katMap.TryGetValue(katTab + "||" + v, out long neu) ? neu : (object)v;
            }

            string ziel = _dup.ErmittleZieltabelle(tab, col, pk);
            if (ziel != null && offset.ContainsKey(ziel))
            { long v = Convert.ToInt64(raw); return v > 0 ? v + offset[ziel] : (object)v; }

            // B1-Gürtel zur Hosenträger-Beziehungsabfrage: Kommt das Schema-Rowset
            // leer zurück (bekannte Lotterie) und kennt auch die FK_MAP die Spalte
            // nicht, greift die Namenskonvention GEGEN DIE PAKET-TABELLEN:
            // ID_<X> -> Tab_<X> wird nur versetzt, wenn Tab_<X> im Paket mitreist
            // (offset vorhanden) — dieselbe Konvention, auf der die FK_MAP beruht,
            // hier aber auf den transportierten Tabellensatz begrenzt.
            if (ziel == null && col.StartsWith("ID_", StringComparison.OrdinalIgnoreCase))
            {
                string kandidat = "Tab_" + col.Substring(3);
                if (offset.ContainsKey(kandidat))
                { long v = Convert.ToInt64(raw); return v > 0 ? v + offset[kandidat] : (object)v; }
            }
            return raw;
        }

        // ---- Referenzierte Katalogzeilen unter ihrer Original-ID auffüllen ------------------
        // Fügt fehlende Zeilen mit exakt derselben PK ein, sodass FK-Verweise gültig bleiben,
        // ohne die IDs zu verändern. Vorhandene Zeilen werden nicht angetastet.
        private void FuelleKatalog(OleDbConnection conn, OleDbTransaction trans, KatMeta k,
            List<Dictionary<string, JsonElement>> rows)
        {
            Dictionary<string, Type> zielTypen = ZielTypen(k.name);
            if (zielTypen == null || !zielTypen.ContainsKey(k.pk)) return;
            foreach (var row in rows)
            {
                if (!row.ContainsKey(k.pk)) continue;
                object pkRoh = JsonToObject(row[k.pk]);

                using (var c = new OleDbCommand(
                    "SELECT COUNT(*) FROM [" + k.name + "] WHERE [" + k.pk + "] = @id", conn, trans))
                {
                    c.Parameters.Add(MacheParam("@id", pkRoh, TypVon(zielTypen, k.pk)));
                    if (Convert.ToInt32(c.ExecuteScalar()) > 0) continue;   // schon vorhanden -> nichts tun
                }

                var cs = row.Keys.Where(x => zielTypen.ContainsKey(x)).ToList();
                if (cs.Count == 0) continue;
                var cps = new List<OleDbParameter>();
                using (var ins = new OleDbCommand("INSERT INTO [" + k.name + "] (" +
                       string.Join(",", cs.Select(x => "[" + x + "]")) + ") VALUES (" +
                       string.Join(",", cs.Select((_, n) => "@c" + n)) + ")", conn, trans))
                {
                    for (int n = 0; n < cs.Count; n++)
                        cps.Add(MacheParam("@c" + n, JsonToObject(row[cs[n]]), TypVon(zielTypen, cs[n])));
                    ins.Parameters.AddRange(cps.ToArray());
                    try { ins.ExecuteNonQuery(); }
                    catch (Exception ex) { throw new Exception(Diagnose("Referenzdaten " + k.name, cs, cps, zielTypen) + " :: " + ex.Message, ex); }
                }
            }
        }

        // ---- Katalog wiederfinden / anlegen ------------------------------------------------
        private void LoeseKatalogAuf(OleDbConnection conn, OleDbTransaction trans, KatMeta k,
            List<Dictionary<string, JsonElement>> rows, Dictionary<string, long> katMap)
        {
            Dictionary<string, Type> zielTypen = ZielTypen(k.name);
            if (zielTypen == null) return;
            foreach (var row in rows)
            {
                long altId = row[k.pk].GetInt64();
                var wo = new List<string>(); var ps = new List<OleDbParameter>(); int i = 0;
                foreach (var key in k.naturalKey)
                {
                    string p = "@k" + (i++); wo.Add("[" + key + "] = " + p);
                    ps.Add(MacheParam(p, JsonToObject(row[key]), TypVon(zielTypen, key)));
                }

                long neuId;
                using (var c = new OleDbCommand(
                    "SELECT [" + k.pk + "] FROM [" + k.name + "] WHERE " + string.Join(" AND ", wo), conn, trans))
                {
                    c.Parameters.AddRange(ps.ToArray());
                    object found;
                    try { found = c.ExecuteScalar(); }
                    catch (Exception ex) { throw new Exception(Diagnose("Katalog-Suche " + k.name, new List<string>(k.naturalKey), ps, zielTypen) + " :: " + ex.Message, ex); }
                    if (found != null && found != DBNull.Value) neuId = Convert.ToInt64(found);
                    else
                    {
                        var cs = row.Keys.Where(x => !x.Equals(k.pk, StringComparison.OrdinalIgnoreCase)
                                                     && zielTypen.ContainsKey(x)).ToList();
                        var cps = new List<OleDbParameter>();
                        using (var ins = new OleDbCommand("INSERT INTO [" + k.name + "] (" +
                               string.Join(",", cs.Select(x => "[" + x + "]")) + ") VALUES (" +
                               string.Join(",", cs.Select((_, n) => "@c" + n)) + ")", conn, trans))
                        {
                            for (int n = 0; n < cs.Count; n++)
                                cps.Add(MacheParam("@c" + n, JsonToObject(row[cs[n]]), TypVon(zielTypen, cs[n])));
                            ins.Parameters.AddRange(cps.ToArray());
                            try { ins.ExecuteNonQuery(); }
                            catch (Exception ex) { throw new Exception(Diagnose("Katalog-INSERT " + k.name, cs, cps, zielTypen) + " :: " + ex.Message, ex); }
                        }
                        using (var id = new OleDbCommand("SELECT @@IDENTITY", conn, trans))
                            neuId = Convert.ToInt64(id.ExecuteScalar());
                    }
                }
                katMap[k.name + "||" + altId] = neuId;
            }
        }

        // ---- Vorhandenes Projekt löschen (Überschreiben) -----------------------------------
        private void LoescheProjekt(OleDbConnection conn, OleDbTransaction trans, int projektId)
        {
            var plan = _dup.ErmittlePlan(conn, trans);
            plan.Reverse();  // Kinder zuerst löschen (Plan ist Eltern-zuerst sortiert)
            foreach (var s in plan)
            {
                if (ZielSpalten(s.Tabelle) == null) continue;
                using (var c = new OleDbCommand(
                    "DELETE FROM [" + s.Tabelle + "] WHERE " + string.Format(s.Filter, projektId), conn, trans))
                    c.ExecuteNonQuery();
            }
        }

        // ---- Helfer -----------------------------------------------------------------------
        private string EindeutigerName(string basis)
        {
            for (int n = 2; n < 1000; n++)
            { string kand = basis + " (" + n + ")"; if (_dup.GetProjektId(kand) <= 0) return kand; }
            return basis + " (" + Guid.NewGuid().ToString("N").Substring(0, 6) + ")";
        }

        // Spalten der ZIEL-Tabelle (null = Tabelle existiert nicht).
        private HashSet<string> ZielSpalten(string tabelle)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable("SELECT * FROM [" + tabelle + "] WHERE 1 = 0");
                if (dt == null) return null;
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (DataColumn c in dt.Columns) set.Add(c.ColumnName);
                return set;
            }
            catch { return null; }
        }

        private long BerechneOffset(OleDbConnection conn, OleDbTransaction trans, string tab, string pk,
            List<Dictionary<string, JsonElement>> rows)
        {
            long max;
            using (var c = new OleDbCommand("SELECT MAX([" + pk + "]) FROM [" + tab + "]", conn, trans))
            { object o = c.ExecuteScalar(); max = (o != null && o != DBNull.Value) ? Convert.ToInt64(o) : 0; }
            long min = rows.Min(r => r[pk].GetInt64());
            long off = max - min + 1; return off < 1 ? 1 : off;
        }

        private static void WriteEntry(ZipArchive zip, string path, string content)
        {
            var e = zip.CreateEntry(path, CompressionLevel.Optimal);
            using (var w = new StreamWriter(e.Open(), new UTF8Encoding(false))) w.Write(content);
        }
        private static string ReadEntry(ZipArchive zip, string path)
        {
            var e = zip.GetEntry(path); if (e == null) return null;
            using (var r = new StreamReader(e.Open(), Encoding.UTF8)) return r.ReadToEnd();
        }
        // Deserialisiert Zeilen und macht die Spaltennamen case-insensitiv,
        // da Access-Spalten unabhängig von der Groß-/Kleinschreibung sind, JSON-Keys aber nicht.
        private static List<Dictionary<string, JsonElement>> LiesZeilen(string json)
        {
            var roh = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json)
                      ?? new List<Dictionary<string, JsonElement>>();
            var res = new List<Dictionary<string, JsonElement>>(roh.Count);
            foreach (var r in roh)
            {
                var ci = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in r) ci[kv.Key] = kv.Value;
                res.Add(ci);
            }
            return res;
        }

        private static string RowsToJson(DataTable dt)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (DataRow r in dt.Rows)
            {
                var d = new Dictionary<string, object>();
                foreach (DataColumn c in dt.Columns) d[c.ColumnName] = r[c] == DBNull.Value ? null : r[c];
                list.Add(d);
            }
            return JsonSerializer.Serialize(list);
        }
        // Access/Jet kennt keinen 64-Bit-Ganzzahltyp. Int64 an eine "Long Integer"-Spalte
        // zu binden scheitert mit "data value could not be converted". Daher Int64 -> Int32
        // (falls es passt), sonst -> Double; null -> DBNull.
        private static object AlsDbWert(object v)
        {
            if (v == null) return DBNull.Value;
            if (v is long l)
                return (l >= int.MinValue && l <= int.MaxValue) ? (object)(int)l : (object)(double)l;
            return v;
        }

        private static object JsonToObject(JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.Null: return null;
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Number: return je.TryGetInt64(out long l) ? (object)l : je.GetDouble();
                default:
                    // Strings bleiben Strings; die Umwandlung in DateTime/Zahl macht Passe()
                    // anhand des tatsächlichen Zieltyps – kein Raten mehr.
                    return je.GetString();
            }
        }

        // Spalten der ZIEL-Tabelle mit .NET-Typ (gecacht). null = Tabelle existiert nicht.
        private Dictionary<string, Type> ZielTypen(string tabelle)
        {
            if (_typCache.TryGetValue(tabelle, out var vorhanden)) return vorhanden;
            Dictionary<string, Type> map = null;
            try
            {
                DataTable dt = DataRepository.GetDataTable("SELECT * FROM [" + tabelle + "] WHERE 1 = 0");
                if (dt != null)
                {
                    map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                    foreach (DataColumn c in dt.Columns) map[c.ColumnName] = c.DataType;
                }
            }
            catch { map = null; }
            _typCache[tabelle] = map;
            return map;
        }

        // Baut eine aussagekräftige Fehlermeldung: welche Spalte hat welchen Zieltyp und
        // welchen tatsächlichen Wert-Typ bekommen. So lässt sich der Konflikt sofort erkennen.
        private static string Diagnose(string ctx, List<string> spalten, List<OleDbParameter> ps, Dictionary<string, Type> typen)
        {
            var sb = new StringBuilder(ctx + " -> ");
            for (int q = 0; q < spalten.Count && q < ps.Count; q++)
            {
                string name = spalten[q].Trim('[', ']', ' ');
                Type zt = (typen != null && typen.TryGetValue(name, out var t)) ? t : null;
                object v = ps[q].Value;
                string vt = (v == null || v == DBNull.Value) ? "NULL" : v.GetType().Name;
                sb.Append(name).Append("(Ziel=").Append(zt != null ? zt.Name : "?").Append(",Wert=").Append(vt).Append(") ");
            }
            return sb.ToString();
        }

        // Erzeugt einen OleDbParameter, dessen Wert in den Zielspaltentyp konvertiert UND
        // dessen OleDbType passend gesetzt ist. Ohne expliziten OleDbType bindet ADO.NET eine
        // DateTime als DBTimeStamp – Access erwartet aber "Date", was ACE-Fehler 3464
        // ("Datentypenkonflikt in Kriterienausdruck") auslöst, obwohl die .NET-Typen passen.
        private static OleDbParameter MacheParam(string name, object rohWert, Type ziel)
        {
            object w = Passe(rohWert, ziel);
            var p = new OleDbParameter(name, w);
            Type t = ziel == null ? null : (Nullable.GetUnderlyingType(ziel) ?? ziel);
            if (t == typeof(DateTime)) p.OleDbType = OleDbType.Date;
            else if (t == typeof(bool)) p.OleDbType = OleDbType.Boolean;
            else if (t == typeof(byte) || t == typeof(short) || t == typeof(int) || t == typeof(long))
                p.OleDbType = OleDbType.Integer;
            else if (t == typeof(double) || t == typeof(float)) p.OleDbType = OleDbType.Double;
            else if (t == typeof(decimal)) p.OleDbType = OleDbType.Decimal;
            else if (t == typeof(Guid)) p.OleDbType = OleDbType.Guid;
            // String bewusst ohne expliziten Typ: ADO.NET wählt VarWChar/LongVarWChar passend
            // zur Länge, sonst würden Memo-Felder (> 255 Zeichen) abgeschnitten.
            return p;
        }

        // Führt den INSERT aus – ZWEISTUFIG:
        //  1) mit Parametern (sicher für Text/Memo/Datum, z. B. Tab_Projekt),
        //  2) scheitert das, als Fallback mit LITERALEN Werten. Grund: Access/ACE lehnt in manchen
        //     Fällen (z. B. expliziter AutoWert-PK) den gebundenen Parameter ab, akzeptiert aber
        //     denselben Wert als Literal (genau wie die Duplizierung via INSERT ... SELECT).
        // Gibt null bei Erfolg zurück, sonst die (erste) Ausnahme.
        private Exception FuehreInsertAus(string tab, List<string> cols, List<string> ph,
            List<object> vals, List<Type> typen, OleDbConnection conn, OleDbTransaction trans)
        {
            Exception ersteAusnahme;
            // 1) Versuch mit Parametern.
            try
            {
                using (var c = new OleDbCommand(
                    "INSERT INTO [" + tab + "] (" + string.Join(",", cols) + ") VALUES (" + string.Join(",", ph) + ")",
                    conn, trans))
                {
                    for (int q = 0; q < ph.Count; q++) c.Parameters.Add(MacheParam(ph[q], vals[q], typen[q]));
                    c.ExecuteNonQuery();
                }
                return null;
            }
            catch (Exception ex) { ersteAusnahme = ex; }

            // 2) Fallback mit literalen Werten.
            try
            {
                var lit = new List<string>();
                for (int q = 0; q < cols.Count; q++)
                    lit.Add(AlsSqlLiteral(q < vals.Count ? vals[q] : null, q < typen.Count ? typen[q] : null));
                string sql = "INSERT INTO [" + tab + "] (" + string.Join(", ", cols) + ") VALUES (" +
                             string.Join(", ", lit) + ")";
                using (var c = new OleDbCommand(sql, conn, trans))
                    c.ExecuteNonQuery();
                return null;
            }
            catch { return ersteAusnahme; }   // beide Wege gescheitert -> ersten Fehler melden
        }

        // Formatiert einen Wert als Access-SQL-Literal (für den Literal-Fallback).
        private static string AlsSqlLiteral(object v, Type ziel)
        {
            if (v == null || v == DBNull.Value) return "NULL";
            if (v is bool b) return b ? "True" : "False";
            if (v is DateTime dt) return "#" + dt.ToString("MM/dd/yyyy HH:mm:ss") + "#";  // US-Format = ACE-sicher
            if (v is string s) return "'" + s.Replace("'", "''") + "'";
            if (v is double || v is float || v is decimal || v is long || v is int || v is short || v is byte)
                return Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
            return "'" + Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture).Replace("'", "''") + "'";
        }

        // Setzt jeden FK-Wert der Zeile, dessen Elterndatensatz (in der Transaktion geprüft) fehlt,
        // auf NULL. Projektinterne Ziele bleiben unangetastet (Eltern entstehen erst in der Transaktion).
        // Gibt die Anzahl genullter Werte zurück.
        private int NulleVerwaisteFks(string tab, List<string> colNames, List<object> vals,
            OleDbConnection conn, OleDbTransaction trans)
        {
            if (_fks == null || !_fks.TryGetValue(tab, out var list)) return 0;
            int n = 0;
            foreach (var fk in list)
            {
                int idx = colNames.FindIndex(c => c.Equals(fk.Col, StringComparison.OrdinalIgnoreCase));
                if (idx < 0) continue;
                object v = vals[idx];
                if (v == null || v == DBNull.Value) continue;
                string sv = Convert.ToString(v);
                if (string.IsNullOrWhiteSpace(sv)) { vals[idx] = null; n++; continue; }
                if (_projektTabellen.Contains(fk.RefTab)) continue;
                if (!FkExistiert(conn, trans, fk.RefTab, fk.RefCol, v)) { vals[idx] = null; n++; }
            }
            return n;
        }

        // Diagnose auf Basis der Wertliste (nach evtl. Nullung), delegiert an VolleDiagnose.
        private string VolleDiagnoseWerte(string tab, List<string> colNames, List<object> vals,
            List<Type> typen, OleDbConnection conn, OleDbTransaction trans)
        {
            var cols = new List<string>(); var ps = new List<OleDbParameter>();
            var typMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            for (int q = 0; q < colNames.Count; q++)
            {
                cols.Add("[" + colNames[q] + "]");
                ps.Add(MacheParam("@d" + q, vals[q], typen[q]));
                typMap[colNames[q]] = typen[q];
            }
            return VolleDiagnose(tab, cols, ps, typMap, conn, trans);
        }

        // Vollständige Fehlerdiagnose für einen fehlgeschlagenen Zeilen-INSERT: Tabelle, jede
        // Spalte mit Zieltyp/Werttyp/echtem Wert, und für jede Access-Beziehung, ob der Eltern-
        // datensatz existiert – geprüft IN der laufenden Transaktion (sieht in-txn-Eltern).
        private string VolleDiagnose(string tab, List<string> cols, List<OleDbParameter> ps,
            Dictionary<string, Type> typen, OleDbConnection conn, OleDbTransaction trans)
        {
            var sb = new StringBuilder();
            sb.Append("INSERT ").Append(tab).Append("  – Spalten:\r\n");
            for (int q = 0; q < cols.Count && q < ps.Count; q++)
            {
                string name = cols[q].Trim('[', ']', ' ');
                Type zt = (typen != null && typen.TryGetValue(name, out var t)) ? t : null;
                object v = ps[q].Value;
                bool leer = (v == null || v == DBNull.Value);
                string vs = leer ? "NULL" : Convert.ToString(v);
                if (vs != null && vs.Length > 40) vs = vs.Substring(0, 40) + "…";
                sb.Append("  ").Append(name).Append(" [Ziel=").Append(zt != null ? zt.Name : "?")
                  .Append(", Wert=").Append(leer ? "NULL" : v.GetType().Name).Append("] = ").Append(vs).Append("\r\n");
            }
            if (_fks != null && _fks.TryGetValue(tab, out var list) && list.Count > 0)
            {
                sb.Append("Beziehungen / FK-Prüfung (in Transaktion):\r\n");
                foreach (var fk in list)
                {
                    int idx = cols.FindIndex(c => c.Trim('[', ']', ' ').Equals(fk.Col, StringComparison.OrdinalIgnoreCase));
                    object v = (idx >= 0 && idx < ps.Count) ? ps[idx].Value : null;
                    bool leer = (v == null || v == DBNull.Value);
                    string status = leer ? "NULL (ok)"
                        : (FkExistiert(conn, trans, fk.RefTab, fk.RefCol, v) ? "vorhanden" : ">>> FEHLT <<<");
                    sb.Append("  ").Append(fk.Col).Append("=").Append(leer ? "NULL" : Convert.ToString(v))
                      .Append(" -> ").Append(fk.RefTab).Append("[").Append(fk.RefCol).Append("] : ").Append(status).Append("\r\n");
                }
            }
            else sb.Append("(keine Access-Beziehungen für ").Append(tab).Append(" gefunden)\r\n");
            return sb.ToString();
        }

        // Existiert ein referenzierter Elterndatensatz? Prüft NUR gegen den gecachten
        // Elternschlüssel-Satz (keine Einzelabfrage, keine zweite Verbindung -> keine Locks/Hänger).
        private bool FkExistiert(OleDbConnection conn, OleDbTransaction trans, string refTab, string refCol, object v)
        {
            if (v == null || v == DBNull.Value) return true;
            var set = LadeElternSchluessel(conn, trans, refTab, refCol);
            return set.Contains(Convert.ToString(v));
        }

        // Stellt sicher, dass ein Text-Schlüssel im (globalen) Katalog existiert: legt ihn per
        // Name an, falls er fehlt (Insert-if-not-exists, wie die vorhandene "Lern"-Logik).
        // So bleiben Verweise per Name auch beim Import in eine andere DB gültig.
        private bool StelleTextKatalogSicher(OleDbConnection conn, OleDbTransaction trans, string refTab, string refCol, string wert)
        {
            try
            {
                using (var ins = new OleDbCommand("INSERT INTO [" + refTab + "] ([" + refCol + "]) VALUES (?)", conn, trans))
                {
                    var p = new OleDbParameter("@v", OleDbType.VarWChar) { Value = wert };
                    ins.Parameters.Add(p);
                    ins.ExecuteNonQuery();
                }
                return true;
            }
            catch { return false; }   // z. B. weitere NOT-NULL-Spalten ohne Default -> Aufrufer nullt den Verweis
        }

        // Lädt (einmalig, gecacht) die vorhandenen Schlüsselwerte einer Elterntabelle als Text.
        // Auf der Import-Transaktion, damit zuvor aufgefüllte/aufgelöste Katalogzeilen enthalten sind.
        private HashSet<string> LadeElternSchluessel(OleDbConnection conn, OleDbTransaction trans, string refTab, string refCol)
        {
            string key = refTab + "||" + refCol;
            if (_fkKeys.TryGetValue(key, out var s)) return s;
            s = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var c = new OleDbCommand("SELECT [" + refCol + "] FROM [" + refTab + "]", conn, trans))
                using (var rd = c.ExecuteReader())
                    while (rd.Read())
                        if (!rd.IsDBNull(0)) s.Add(Convert.ToString(rd.GetValue(0)));
            }
            catch { }
            _fkKeys[key] = s;
            return s;
        }

        private static Type TypVon(Dictionary<string, Type> map, string spalte) =>
            (map != null && map.TryGetValue(spalte, out var t)) ? t : null;

        // Wandelt den Wert in den tatsächlichen Zielspaltentyp um. So werden Datentyp-
        // konflikte (Int64->Long Integer, String->DateTime, Text mit Zahl usw.) vermieden.
        private static object Passe(object v, Type ziel)
        {
            if (v == null) return DBNull.Value;
            if (ziel == null) return AlsDbWert(v);
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                Type t = Nullable.GetUnderlyingType(ziel) ?? ziel;
                // Leerstring in einer nicht-Text-Spalte -> NULL (sonst Datentypkonflikt).
                if (t != typeof(string) && v is string sv && string.IsNullOrWhiteSpace(sv))
                    return DBNull.Value;
                if (t == typeof(string)) return Convert.ToString(v, ci);
                if (t == typeof(DateTime)) return (v is DateTime) ? v : Convert.ToDateTime(v, ci);
                if (t == typeof(bool)) return (v is bool) ? v : Convert.ToBoolean(v, ci);
                if (t == typeof(byte) || t == typeof(short) || t == typeof(int) || t == typeof(long))
                    return Convert.ToInt32(v, ci);          // Access kennt max. "Long Integer" (Int32)
                if (t == typeof(decimal)) return Convert.ToDecimal(v, ci);
                if (t == typeof(float) || t == typeof(double)) return Convert.ToDouble(v, ci);
                if (t == typeof(Guid)) return (v is Guid) ? v : Guid.Parse(Convert.ToString(v, ci));
                return Convert.ChangeType(v, t, ci);
            }
            catch { return AlsDbWert(v); }
        }

        // ---- DTOs -------------------------------------------------------------------------
        private class Manifest
        {
            public string format { get; set; }
            public int formatVersion { get; set; }
            public int schemaVersion { get; set; }
            public string exportedUtc { get; set; }
            public string sourceProject { get; set; }
            public List<TabMeta> tables { get; set; }
            public List<KatMeta> catalogs { get; set; }
            public List<KatMeta> fill { get; set; }   // per Original-ID aufzufüllende Katalogzeilen
            public List<VarMeta> variants { get; set; }        // T3: Varianten-Bäume (projects/<i>/data/)
            public List<LinkMeta> variantLinks { get; set; }   // T3: Stamm-Verknüpfungen (statt Tab_Variante-Zeilen)
        }
        private class Fk { public string Col; public string RefTab; public string RefCol; }

        // Liest die in Access definierten Fremdschlüssel: je Kindtabelle die Liste der
        // FK-Spalten mit Zieltabelle und Ziel-PK-Spalte. Leer, falls keine RI definiert ist.
        private Dictionary<string, List<Fk>> LiesFremdschluessel(OleDbConnection conn)
        {
            var res = new Dictionary<string, List<Fk>>(StringComparer.OrdinalIgnoreCase);
            DataTable dt;
            try { dt = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, null); }
            catch { return res; }
            if (dt == null) return res;
            foreach (DataRow r in dt.Rows)
            {
                string kind = Convert.ToString(r["FK_TABLE_NAME"]);
                string kindCol = Convert.ToString(r["FK_COLUMN_NAME"]);
                string ziel = Convert.ToString(r["PK_TABLE_NAME"]);
                string zielCol = Convert.ToString(r["PK_COLUMN_NAME"]);
                if (string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(kindCol)) continue;
                if (!res.TryGetValue(kind, out var l)) res[kind] = l = new List<Fk>();
                l.Add(new Fk { Col = kindCol, RefTab = ziel, RefCol = zielCol });
            }
            return res;
        }

        private class TabMeta { public string name { get; set; } public string pk { get; set; } }
        private class VarMeta { public string name { get; set; } public List<TabMeta> tables { get; set; } }
        private class LinkMeta { public string projekt { get; set; } public string stamm { get; set; } public string variantenname { get; set; } }
        private class KatMeta { public string name { get; set; } public string pk { get; set; } public string[] naturalKey { get; set; } }
    }
}
