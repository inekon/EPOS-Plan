using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kataloge des Zapfprofilgenerators im Projekttransfer</b> (Umsetzungskonzept
    /// Zapfprofilgenerator, Abschnitt 3.2, Stufe Z0, Posten P9).
    ///
    /// <para><b>Was reist.</b> Zone, Wohnungstyp und Projektzeile zeigen auf
    /// unveränderliche Katalogversionen: <c>ID_Nutzungsart</c>, <c>ID_Tagesgangsatz</c>,
    /// <c>ID_Bedarfstag</c>, <c>ID_Ausstattung</c> (<see cref="KATALOG_SPALTE_ZU_TABELLE"/>).
    /// Die Köpfe reisen unter <c>catalogs/</c> und werden am Ziel über ihren natürlichen
    /// Schlüssel wiedergefunden (<c>Bezeichner</c>, <c>Katalogversion</c>) bzw.
    /// (<c>Art</c>, <c>Schluessel</c>, <c>Katalogversion</c>). Zu jeder Nutzungsart reist
    /// ihr Tagesgangsatz mit (er ist NOT NULL an ihr), und zu Tagesgangsatz und Bedarfstag
    /// ihre Kindzeilen unter <c>catalogchildren/</c>.</para>
    ///
    /// <para><b>Fehlt die Zeile am Ziel, wird sie mitgenommen</b> — als EIGENE Zeile mit
    /// <c>Status = 'IMPORT'</c>, <c>ReadOnly = 0</c>, ohne <c>ID_Vorlage</c>, mit ihrem
    /// Tagesgangsatz bzw. ihren Kindzeilen, und der Bericht nennt sie. Das hält die
    /// Rechnung des Projekts fest (die Version reist mit dem Projekt, Frage ZU12) und
    /// hängt die Zone nie still um. Der Weg über die Original-Id (<c>fill/</c>) taugt für
    /// diese Kataloge nicht: Er fände am Ziel eine FREMDE Zeile gleicher Id und zeigte
    /// still darauf (gemessen vor diesem Posten). Eine mitgenommene Zeile löscht die
    /// Auslieferungsvorlage wieder (<c>Werkzeuge/Auslieferungsvorlage</c>, Konzept 6 (b)).</para>
    ///
    /// <para><b>Benannte Ablehnung.</b> Lässt sich eine fehlende Nutzungsart nicht
    /// mitnehmen, weil ihr Tagesgangsatz nicht im Paket steht (ein Paket, das nicht dieser
    /// Weg geschrieben hat), bricht der Import mit Namen ab und ändert nichts.</para>
    /// </summary>
    public partial class ProjektExportImportCtrl
    {
        /// <summary>Paketordner der Kindzeilen mitreisender Katalogköpfe.</summary>
        private const string KINDER_PRAEFIX = "catalogchildren/";

        /// <summary>Kindtabelle → (Kopftabelle, Verweisspalte).</summary>
        private static readonly (string Kind, string Kopf, string Spalte)[] TWW_KINDER =
        {
            (TwwSchema.TAB_TWW_TAGESGANG_STAMM, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, "ID_Tagesgangsatz"),
            (TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, "ID_Bedarfstag"),
        };

        /// <summary>Die Tww-Kataloge, die über den natürlichen Schlüssel reisen.</summary>
        private static readonly HashSet<string> TWW_KATALOGE = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, TwwSchema.TAB_TWW_DIN4708_WERT_STAMM
        };

        /// <summary>Kopftabelle → (alte Id → neue Id) der in DIESEM Import mitgenommenen Köpfe.</summary>
        private Dictionary<string, Dictionary<long, long>> _twwMitgenommen =
            new Dictionary<string, Dictionary<long, long>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Berichtszeilen der Mitnahme; <see cref="ImportierenIntern"/> hängt sie an.</summary>
        private List<string> _twwBericht = new List<string>();

        private class KindMeta
        {
            public string name { get; set; }
            public string parent { get; set; }
            public string parentColumn { get; set; }
            public string pk { get; set; }
        }

        private static bool IstTwwKatalog(string tabelle) => tabelle != null && TWW_KATALOGE.Contains(tabelle);

        private void TwwZuruecksetzen()
        {
            _twwMitgenommen = new Dictionary<string, Dictionary<long, long>>(StringComparer.OrdinalIgnoreCase);
            _twwBericht = new List<string>();
        }

        // =================================================================================
        //  EXPORT
        // =================================================================================

        /// <summary>
        /// Ergänzt die Katalogverweise um die Tagesgangsätze der verwiesenen Nutzungsarten —
        /// ohne sie ließe sich eine am Ziel fehlende Nutzungsart nicht mitnehmen.
        /// </summary>
        private static void TwwKatalogRefsErgaenzen(Dictionary<string, HashSet<long>> katalogRefs)
        {
            if (!katalogRefs.TryGetValue(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, out HashSet<long> nutzungen) ||
                nutzungen.Count == 0)
                return;

            if (!katalogRefs.TryGetValue(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, out HashSet<long> saetze))
                katalogRefs[TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM] = saetze = new HashSet<long>();

            foreach (long id in nutzungen.OrderBy(x => x))
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT ID_Tagesgangsatz FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?", new DbParam("@id", id));
                if (o != null && o != DBNull.Value && Convert.ToInt64(o) > 0) saetze.Add(Convert.ToInt64(o));
            }
        }

        /// <summary>
        /// Die Katalogverweise in Auflösungsreihenfolge: der Tagesgangsatz VOR der
        /// Nutzungsart, die auf ihn zeigt; sonst die Reihenfolge des Exports (stabil).
        /// </summary>
        private static IEnumerable<KeyValuePair<string, HashSet<long>>> TwwReihenfolge(
            Dictionary<string, HashSet<long>> katalogRefs)
        {
            return katalogRefs.OrderBy(kv =>
                string.Equals(kv.Key, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, StringComparison.OrdinalIgnoreCase) ? 0 : 1);
        }

        /// <summary>
        /// Schreibt die Kindzeilen der mitreisenden Tagesgangsätze und Bedarfstage unter
        /// <c>catalogchildren/</c>; <c>null</c>, wenn es keine gibt (das Manifest bleibt dann
        /// wie bei jedem Paket ohne Zapfprofil).
        /// </summary>
        private static List<KindMeta> TwwKinderSchreiben(ZipArchive zip, Dictionary<string, HashSet<long>> katalogRefs)
        {
            var meta = new List<KindMeta>();
            foreach (var k in TWW_KINDER)
            {
                if (!katalogRefs.TryGetValue(k.Kopf, out HashSet<long> koepfe) || koepfe.Count == 0) continue;

                DataTable alle = null;
                foreach (long id in koepfe.OrderBy(x => x))
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT * FROM [" + k.Kind + "] WHERE [" + k.Spalte + "] = ? ORDER BY ID", new DbParam("@id", id));
                    if (dt == null) continue;
                    if (alle == null) alle = dt; else alle.Merge(dt);
                }
                if (alle == null || alle.Rows.Count == 0) continue;

                WriteEntry(zip, KINDER_PRAEFIX + k.Kind + ".json", RowsToJson(alle));
                meta.Add(new KindMeta { name = k.Kind, parent = k.Kopf, parentColumn = k.Spalte, pk = "ID" });
            }
            return meta.Count == 0 ? null : meta;
        }

        // =================================================================================
        //  IMPORT
        // =================================================================================

        /// <summary>
        /// Legt eine am Ziel fehlende Tww-Katalogzeile als eigene Zeile an: Status
        /// <c>IMPORT</c>, <c>ReadOnly = 0</c>, <c>ID_Vorlage</c> leer, der Tagesgangsatz einer
        /// Nutzungsart umgeschlüsselt. Liefert die neue Id.
        /// </summary>
        private long TwwZeileMitnehmen(DbVorgang v, KatMeta k, Dictionary<string, JsonElement> row,
            Dictionary<string, long> katMap, Dictionary<string, Type> zielTypen)
        {
            string name = TwwName(k, row);
            var cs = row.Keys.Where(x => !x.Equals(k.pk, StringComparison.OrdinalIgnoreCase)
                                         && zielTypen.ContainsKey(x)).ToList();
            var cps = new List<DbParam>();
            for (int n = 0; n < cs.Count; n++)
            {
                string spalte = cs[n];
                object wert = JsonToObject(row[spalte]);

                if (spalte.Equals("Status", StringComparison.OrdinalIgnoreCase)) wert = TwwSchema.STATUS_IMPORT;
                else if (spalte.Equals("ReadOnly", StringComparison.OrdinalIgnoreCase)) wert = 0L;
                else if (spalte.Equals("ID_Vorlage", StringComparison.OrdinalIgnoreCase)) wert = null;
                else if (spalte.Equals("ID_Tagesgangsatz", StringComparison.OrdinalIgnoreCase) && wert != null)
                {
                    string schluessel = TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + "||" + Convert.ToInt64(wert);
                    if (!katMap.TryGetValue(schluessel, out long satz))
                        throw new Exception(
                            "Die Katalogzeile " + name + " aus " + k.name + " fehlt am Ziel und kann nicht " +
                            "mitgenommen werden: ihr Tagesgangsatz steht nicht im Paket. Import abgelehnt, nichts geändert.");
                    wert = satz;
                }
                cps.Add(MacheParam("@c" + n, wert, TypVon(zielTypen, spalte)));
            }

            long neuId;
            try
            {
                neuId = v.EinfuegenUndId("INSERT INTO [" + k.name + "] (" +
                    string.Join(",", cs.Select(x => "[" + x + "]")) + ") VALUES (" +
                    string.Join(",", cs.Select(_ => "?")) + ")", cps.ToArray());
            }
            catch (Exception ex)
            {
                throw new Exception(Diagnose("Katalog-Mitnahme " + k.name, cs, cps, zielTypen) + " :: " + ex.Message, ex);
            }

            if (!_twwMitgenommen.TryGetValue(k.name, out var map))
                _twwMitgenommen[k.name] = map = new Dictionary<long, long>();
            map[row[k.pk].GetInt64()] = neuId;

            _twwBericht.Add("Katalogzeile " + name + " (" + k.name + ") fehlte am Ziel und wurde mit Status IMPORT " +
                            "mitgenommen.");
            return neuId;
        }

        /// <summary>
        /// Spielt die Kindzeilen (Tagesgänge, Ereignisse) der in diesem Import mitgenommenen
        /// Köpfe ein, umgeschlüsselt auf den neuen Kopf. Kinder eines am Ziel GEFUNDENEN
        /// Kopfs bleiben liegen — der Kopf bringt dort seine eigenen mit.
        /// </summary>
        private void TwwKinderEinspielen(DbVorgang v, List<KindMeta> kinder,
            Dictionary<string, List<Dictionary<string, JsonElement>>> kindRows, Dictionary<string, long> katMap)
        {
            if (kinder == null) return;
            foreach (KindMeta k in kinder)
            {
                if (!_twwMitgenommen.TryGetValue(k.parent, out var koepfe) || koepfe.Count == 0) continue;
                if (!kindRows.TryGetValue(k.name, out var zeilen)) continue;
                Dictionary<string, Type> zielTypen = ZielTypen(k.name);
                if (zielTypen == null) continue;

                foreach (var row in zeilen)
                {
                    if (!row.TryGetValue(k.parentColumn, out JsonElement elternJe) ||
                        elternJe.ValueKind != JsonValueKind.Number) continue;
                    if (!koepfe.TryGetValue(elternJe.GetInt64(), out long neuerKopf)) continue;

                    var cs = row.Keys.Where(x => !x.Equals(k.pk, StringComparison.OrdinalIgnoreCase)
                                                 && zielTypen.ContainsKey(x)).ToList();
                    var cps = new List<DbParam>();
                    for (int n = 0; n < cs.Count; n++)
                    {
                        object wert = cs[n].Equals(k.parentColumn, StringComparison.OrdinalIgnoreCase)
                            ? neuerKopf
                            : JsonToObject(row[cs[n]]);
                        cps.Add(MacheParam("@c" + n, wert, TypVon(zielTypen, cs[n])));
                    }
                    try
                    {
                        v.Ausfuehren("INSERT INTO [" + k.name + "] (" +
                            string.Join(",", cs.Select(x => "[" + x + "]")) + ") VALUES (" +
                            string.Join(",", cs.Select(_ => "?")) + ")", cps.ToArray());
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(Diagnose("Katalog-Mitnahme " + k.name, cs, cps, zielTypen) + " :: " + ex.Message, ex);
                    }
                }
            }
        }

        /// <summary>„Bezeichner“ (Katalogversion) bzw. „Art/Schluessel“ (Katalogversion) für den Bericht.</summary>
        private static string TwwName(KatMeta k, Dictionary<string, JsonElement> row)
        {
            var teile = new List<string>();
            string version = null;
            foreach (string s in k.naturalKey ?? new string[0])
            {
                string w = row.TryGetValue(s, out JsonElement je) ? Convert.ToString(JsonToObject(je)) : "";
                if (s.Equals("Katalogversion", StringComparison.OrdinalIgnoreCase)) version = w;
                else teile.Add(w);
            }
            return "„" + string.Join(" / ", teile) + "“" +
                   (string.IsNullOrEmpty(version) ? "" : " (Katalogversion " + version + ")");
        }
    }
}
