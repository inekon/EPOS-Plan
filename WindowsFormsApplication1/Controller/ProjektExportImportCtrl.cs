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
    public class ProjektExportImportCtrl
    {
        private const string FORMAT = "wp-projekt";
        private const int FORMAT_VER = 1;
        private const int SCHEMA_VER = 29;

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
        {
            int srcId = _dup.GetProjektId(projektName);
            if (srcId <= 0) { MessageBox.Show("Projekt '" + projektName + "' nicht gefunden."); return false; }

            var tx = DataRepository.BeginTransaction();
            OleDbConnection conn = tx.Item1; OleDbTransaction trans = tx.Item2;
            try
            {
                var plan = _dup.ErmittlePlan(conn, trans);
                var manifestTabellen = new List<TabMeta>();
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
                    int i = 0;
                    foreach (var s in plan)
                    {
                        fortschritt?.Report(new ProjektDuplizierenCtrl.Fortschritt
                        { Aktuell = i++, Gesamt = plan.Count, Tabelle = s.Tabelle });

                        DataTable dt = DataRepository.GetDataTable(
                            "SELECT * FROM [" + s.Tabelle + "] WHERE " + string.Format(s.Filter, srcId));
                        if (dt == null || dt.Rows.Count == 0) continue;

                        WriteEntry(zip, "data/" + s.Tabelle + ".json", RowsToJson(dt));
                        manifestTabellen.Add(new TabMeta { name = s.Tabelle, pk = s.Pk });

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
                        schemaVersion = SCHEMA_VER,
                        exportedUtc = DateTime.UtcNow.ToString("o"),
                        sourceProject = projektName,
                        tables = manifestTabellen,
                        catalogs = katalogMeta,
                        fill = fuellMeta
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
            var catalogRows = new Dictionary<string, List<Dictionary<string, JsonElement>>>();
            var fillRows = new Dictionary<string, List<Dictionary<string, JsonElement>>>();

            using (var zip = ZipFile.OpenRead(quellPfad))
            {
                man = JsonSerializer.Deserialize<Manifest>(ReadEntry(zip, "manifest.json"));
                if (man == null || man.format != FORMAT) { fehler = "Kein gültiges Projektpaket."; return -1; }
                foreach (var t in man.tables)
                    tableRows[t.name] = LiesZeilen(ReadEntry(zip, "data/" + t.name + ".json"));
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

            var tx = DataRepository.BeginTransaction();
            OleDbConnection conn = tx.Item1; OleDbTransaction trans = tx.Item2;
            try
            {
                _fks = LiesFremdschluessel(conn);
                _projektTabellen = new HashSet<string>(man.tables.Select(x => x.name), StringComparer.OrdinalIgnoreCase);
                _fkKeys = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                int schritt = 0;
                int gesamt = man.tables.Count + (man.catalogs?.Count > 0 ? 1 : 0) + (ueberschreibId > 0 ? 1 : 0);

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

                // 2) Offsets je Projekt-Tabelle (nur vorhandene Tabellen).
                var offset = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                foreach (var t in man.tables)
                    if (ZielSpalten(t.name) != null && tableRows[t.name].Count > 0)
                        offset[t.name] = BerechneOffset(conn, trans, t.name, t.pk, tableRows[t.name]);

                // 3) Einfügen in FK-Reihenfolge, Spalten-Schnittmenge, alles umschlüsseln.
                foreach (var t in man.tables)
                {
                    fortschritt?.Report(new ProjektDuplizierenCtrl.Fortschritt { Aktuell = schritt++, Gesamt = gesamt, Tabelle = t.name });

                    Dictionary<string, Type> zielTypen = ZielTypen(t.name);
                    if (zielTypen == null) continue;                       // Tabelle in Ziel-DB nicht (mehr) vorhanden
                    if (!offset.ContainsKey(t.name)) continue;

                    // FK-Spalten dieser Tabelle je Spaltenname (für Verwaisten-Behandlung).
                    var fkByCol = new Dictionary<string, Fk>(StringComparer.OrdinalIgnoreCase);
                    if (_fks != null && _fks.TryGetValue(t.name, out var flist))
                        foreach (var f in flist) fkByCol[f.Col] = f;

                    foreach (var row in tableRows[t.name])
                    {
                        var cols = new List<string>(); var ph = new List<string>(); var ps = new List<OleDbParameter>();
                        int i = 0;
                        foreach (var kv in row)
                        {
                            if (!zielTypen.ContainsKey(kv.Key)) continue;  // Spalte gibt es im Ziel nicht -> überspringen
                            object val = (t.name.Equals("Tab_Projekt", StringComparison.OrdinalIgnoreCase) &&
                                          kv.Key.Equals("Projektname", StringComparison.OrdinalIgnoreCase))
                                ? ziel
                                : Umschluessele(t.name, kv.Key, t.pk, kv.Value, offset, katMap);
                            // Verwaister Fremdschlüssel (kein passender Elterndatensatz) -> NULL.
                            // Betrifft leere UND nicht-leere Werte, deren Ziel-Zeile fehlt (RI wurde
                            // ohne Altdatenprüfung aktiviert). Projekt-Eigentabellen sind ausgenommen,
                            // da deren Eltern erst in dieser Transaktion (versetzt) entstehen.
                            if (fkByCol.TryGetValue(kv.Key, out var fk) && val != null && !(val is DBNull))
                            {
                                string sv = Convert.ToString(val);
                                if (string.IsNullOrWhiteSpace(sv))
                                    val = null;   // leerer Verweis = kein Verweis -> IMMER NULL (auch bei Projekttabellen)
                                else if (!_projektTabellen.Contains(fk.RefTab) &&
                                         !LadeElternSchluessel(conn, trans, fk.RefTab, fk.RefCol).Contains(sv))
                                    val = null;   // verwaister Katalogverweis -> NULL (Projekt-Eltern entstehen erst in der Transaktion)
                            }
                            cols.Add("[" + kv.Key + "]"); string p = "@p" + (i++); ph.Add(p);
                            ps.Add(MacheParam(p, val, zielTypen[kv.Key]));
                        }
                        if (cols.Count == 0) continue;
                        using (var c = new OleDbCommand(
                            "INSERT INTO [" + t.name + "] (" + string.Join(",", cols) + ") VALUES (" + string.Join(",", ph) + ")",
                            conn, trans))
                        {
                            c.Parameters.AddRange(ps.ToArray());
                            try { c.ExecuteNonQuery(); }
                            catch (Exception ex) { throw new Exception("\r\n" + VolleDiagnose(t.name, cols, ps, zielTypen, conn, trans) + ":: " + ex.Message, ex); }
                        }
                    }
                }

                trans.Commit();
                fortschritt?.Report(new ProjektDuplizierenCtrl.Fortschritt { Aktuell = gesamt, Gesamt = gesamt, Tabelle = "" });

                string projPk = man.tables.First(x => x.name.Equals("Tab_Projekt", StringComparison.OrdinalIgnoreCase)).pk;
                long altProjId = tableRows["Tab_Projekt"][0][projPk].GetInt64();
                return offset.ContainsKey("Tab_Projekt") ? (int)(altProjId + offset["Tab_Projekt"]) : -1;
            }
            catch (Exception ex)
            {
                try { trans.Rollback(); } catch { }
                fehler = ex.Message; return -1;
            }
            finally { try { trans.Dispose(); } catch { } try { conn.Dispose(); } catch { } }
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

        private bool FkExistiert(OleDbConnection conn, OleDbTransaction trans, string refTab, string refCol, object v)
        {
            try
            {
                using (var c = new OleDbCommand("SELECT COUNT(*) FROM [" + refTab + "] WHERE [" + refCol + "] = ?", conn, trans))
                { c.Parameters.Add(new OleDbParameter("@v", v)); return Convert.ToInt32(c.ExecuteScalar()) > 0; }
            }
            catch
            {
                try
                {
                    object cnt = DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM [" + refTab + "] WHERE [" + refCol + "] = ?", new OleDbParameter("@v", v));
                    return Convert.ToInt32(cnt) > 0;
                }
                catch { return true; }   // unbekannt -> nicht fälschlich als Fehler markieren
            }
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

        // Prüft bei einem INSERT-Fehler gezielt jeden Fremdschlüssel der Zeile: existiert der
        // referenzierte Datensatz im Elterntisch? Nennt die genaue Spalte/Wert/Zieltabelle.
        // Liest auf frischer Verbindung (committete Daten), unabhängig von der Import-Transaktion.
        private string FkDiagnose(string tab, List<string> cols, List<OleDbParameter> ps)
        {
            if (_fks == null || !_fks.TryGetValue(tab, out var list)) return "";
            var sb = new StringBuilder();
            foreach (var fk in list)
            {
                // Eltern, die in DIESER Transaktion erst angelegt werden (Projekt-Eigentabellen),
                // sind auf frischer Verbindung noch nicht sichtbar -> nicht als "fehlt" melden.
                if (_projektTabellen != null && _projektTabellen.Contains(fk.RefTab)) continue;
                int idx = cols.FindIndex(c => c.Trim('[', ']', ' ').Equals(fk.Col, StringComparison.OrdinalIgnoreCase));
                if (idx < 0 || idx >= ps.Count) continue;
                object v = ps[idx].Value;
                if (v == null || v == DBNull.Value) continue;
                try
                {
                    object cnt = DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM [" + fk.RefTab + "] WHERE [" + fk.RefCol + "] = ?",
                        new OleDbParameter("@v", v));
                    if (Convert.ToInt32(cnt) == 0)
                        sb.Append(fk.Col).Append("=").Append(v).Append(" fehlt in ")
                          .Append(fk.RefTab).Append("[").Append(fk.RefCol).Append("]; ");
                }
                catch { }
            }
            return sb.Length == 0 ? "" : "  [FK-Prüfung: " + sb.ToString().TrimEnd() + "]";
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
        private class KatMeta { public string name { get; set; } public string pk { get; set; } public string[] naturalKey { get; set; } }
    }
}
