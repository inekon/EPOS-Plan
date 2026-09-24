using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

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
    /// ihr Tagesgangsatz mit (er ist NOT NULL an ihr), und zu Tagesgangsatz, Bedarfstag und
    /// Nutzungsart ihre Kindzeilen unter <c>catalogchildren/</c> — Tagesgänge, Ereignisse und
    /// die Zapfkategorien (Schemaschritt T2). Eine Kindtabelle, die die Zieldatenbank nicht
    /// führt (Stand vor 115), bleibt beim Import liegen.</para>
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
    /// <para><b>Mitnahme nur bei Bedarf.</b> Ein Tagesgangsatz, der nur als Abhängigkeit
    /// einer Nutzungsart reist (keine Zone verweist direkt auf ihn), wird am Ziel nicht auf
    /// Vorrat angelegt: Er wird vorgemerkt und erst mitgenommen, wenn eine mitgenommene
    /// Nutzungsart ihn braucht. Findet der Import die Nutzungsart am Ziel, bleibt er
    /// liegen.</para>
    ///
    /// <para><b>Benannte Ablehnung.</b> Lässt sich eine fehlende Nutzungsart nicht
    /// mitnehmen, weil ihr Tagesgangsatz nicht im Paket steht (ein Paket, das nicht dieser
    /// Weg geschrieben hat), bricht der Import mit Namen ab und ändert nichts. Ebenso, wenn
    /// eine Zone, ein Wohnungstyp oder die Projektzeile auf eine Tww-Katalogzeile zeigt, die
    /// das Paket gar nicht führt, oder das Paket einen Tww-Katalog unter <c>fill/</c>
    /// (Original-Id) trägt.</para>
    ///
    /// <para><b>Interne Spalten reisen nicht.</b> <c>Beleg</c> (interne Sekundärquelle,
    /// Konzept 6 (e)) und <c>Freigabe</c> (Vier-Augen-Vermerk der Katalogpflege) bleiben im
    /// Paket leer; eine mitgenommene Zeile trägt beide leer.</para>
    ///
    /// <para><b>Inhaltsvergleich namensgleicher Zeilen (ZU17, N6).</b> Findet der Import am
    /// Ziel eine Zeile mit demselben natürlichen Schlüssel, die NICHT zur Auslieferung gehört
    /// (<c>EIGEN</c> oder <c>IMPORT</c> — beide sind beim Anwender änderbar), vergleicht er
    /// ihre Werte mit dem Paket (<see cref="TwwInhaltGleich"/>): alle Wertgruppen des Kopfes —
    /// ohne Schlüssel, Status, <c>ReadOnly</c>, Vorlage, interne Spalten und ohne die
    /// Provenienzspalten, die den Wert beschreiben, nicht ihn —, den Tagesgangsatz einer
    /// Nutzungsart über seine Tagesgänge (nicht über seine Id) und die Kindzeilen (Tagesgänge,
    /// Ereignisse) als Menge. Gleich: Die Zone zeigt auf die Zielzeile. Abweichend: Die Zeile
    /// kommt als NEUE Version mit Status <c>IMPORT</c> und dem Zusatz
    /// <c>„ (Import n)“</c> im Bezeichner — beim DIN-4708-Wert im <c>Schluessel</c> —, n die
    /// kleinste freie Zahl ab 1; trägt eine frühere Version „(Import n)“ schon denselben
    /// Inhalt, wird sie wiederverwendet. Die Zielzeile bleibt unberührt, der Bericht nennt
    /// beides. Eine Auslieferungszeile ist eine unveränderliche Version und wird nicht
    /// verglichen.</para>
    ///
    /// <para><b>Schemaschritt 121 (T3).</b> Die Laufangaben der Auslegung an <c>Tab_TwwProjekt</c>
    /// (Erzeugerart, Werkstoff, Personen, Bezug des Füllstands) und die Bezugsart am Bedarfstag
    /// reisen mit ihrer Zeile — Projektzeile bzw. Katalogkopf — und zählen im Inhaltsvergleich.
    /// Führt die Zieldatenbank die Spalten noch nicht (Stand vor 121), läuft der Import durch wie
    /// bei einer Kindtabelle vor 115; der Bericht nennt je Spalte, wie viele Werte liegen bleiben
    /// (<see cref="TwwSchritt121Melden"/>) — benannt, nie still.</para>
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
            (TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "ID_Nutzungsart"),
        };

        /// <summary>
        /// Die Verwaltungsspalten einer Kindzeile mit eigenem Stand (die Zapfkategorien): Sie
        /// tragen den Inhalt nicht und werden bei der Mitnahme wie am Kopf gesetzt —
        /// <c>Status = 'IMPORT'</c>, <c>ReadOnly = 0</c>.
        /// </summary>
        private static readonly string[] TWW_KIND_VERWALTUNG = { "Status", "ReadOnly" };

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

        /// <summary>„Tabelle||Id“ jeder Tww-Katalogzeile, auf die eine Projektzeile des Pakets DIREKT zeigt.</summary>
        private HashSet<string> _twwDirekt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Am Ziel fehlende Tagesgangsätze, die nur als Abhängigkeit reisen (alte Id → Zeile).</summary>
        private Dictionary<long, (KatMeta Meta, Dictionary<string, JsonElement> Zeile, Dictionary<string, Type> Typen)>
            _twwVorgemerkt = new Dictionary<long, (KatMeta, Dictionary<string, JsonElement>, Dictionary<string, Type>)>();

        /// <summary>Die internen Spalten der Tww-Kataloge, die nie in ein Paket gehen.</summary>
        private static readonly string[] TWW_INTERN = { "Beleg", "Freigabe" };

        /// <summary>
        /// Die Kindtabellen des Pakets für Inhaltsvergleich (ZU17) und Einspielen — nie die
        /// Angaben des Manifests selbst, sondern die festen Einträge aus <see cref="TWW_KINDER"/>,
        /// die das Manifest nennt (<see cref="TwwManifestPruefen"/>, N8).
        /// </summary>
        private List<KindMeta> _twwKinderMeta = new List<KindMeta>();

        /// <summary>Die Kindzeilen des Pakets je Kindtabelle — für den Inhaltsvergleich (ZU17).</summary>
        private Dictionary<string, List<Dictionary<string, JsonElement>>> _twwKindRows =
            new Dictionary<string, List<Dictionary<string, JsonElement>>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Spalten, die den Inhalt nicht tragen: Verwaltung, Vorlage, interne Spalten (ZU17).</summary>
        private static readonly HashSet<string> TWW_NICHT_VERGLICHEN = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ID", "Status", "ReadOnly", "ID_Vorlage", "Beleg", "Freigabe", "Katalogversion"
        };

        /// <summary>Die Provenienzspalten — sie beschreiben einen Wert, sie sind keiner (ZU17).</summary>
        private static readonly Regex TWW_PROVENIENZ = new Regex(
            "^(Bedarf_|Jahresgang_|Wochengang_)?(Quelle|Ausgabe|Version|Herkunftsart)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>Der Zusatz einer abweichenden namensgleichen Version: „ (Import n)“ (ZU17).</summary>
        private static string TwwZusatz(int n) => " (Import " + n.ToString(CultureInfo.InvariantCulture) + ")";

        private class KindMeta
        {
            public string name { get; set; }
            public string parent { get; set; }
            public string parentColumn { get; set; }
            public string pk { get; set; }
        }

        private static bool IstTwwKatalog(string tabelle) => tabelle != null && TWW_KATALOGE.Contains(tabelle);

        /// <summary>Jede Katalogtabelle des Zapfprofilgenerators (<c>Tab_Tww*_STAMM</c>), auch Kinder und Parameter.</summary>
        private static bool IstTwwStamm(string tabelle) =>
            tabelle != null && tabelle.StartsWith("Tab_Tww", StringComparison.OrdinalIgnoreCase) &&
            tabelle.EndsWith("_STAMM", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Prüft die Tww-Angaben des Manifests, bevor etwas geschrieben wird (ZU17, N8): Ein
        /// Tww-Katalog muss Primärschlüssel <c>ID</c> und genau den natürlichen Schlüssel aus
        /// <see cref="KATALOG_NATURALKEY"/> tragen, eine Kindtabelle muss mit Name, Kopf und
        /// Verweisspalte in <see cref="TWW_KINDER"/> stehen. Die Bezeichner, die in SQL-Texte
        /// eingesetzt werden, kommen damit aus diesen festen Tabellen, nie aus dem Paket.
        /// <paramref name="kinder"/> sind die festen Einträge der genannten Kindtabellen;
        /// <paramref name="uebergangen"/> nimmt je Kindtabelle des Pakets, die diese Datenbank nicht
        /// führt (die Zapfkategorien vor Schritt 115), eine Berichtszeile auf — der Import übergeht
        /// sie benannt.
        /// </summary>
        private static bool TwwManifestPruefen(List<KatMeta> kataloge, List<KindMeta> manifestKinder,
                                               out List<KindMeta> kinder, out string fehler, List<string> uebergangen)
        {
            kinder = new List<KindMeta>();
            fehler = null;
            foreach (KatMeta k in kataloge ?? new List<KatMeta>())
            {
                if (k == null || !IstTwwKatalog(k.name)) continue;
                string[] soll = KATALOG_NATURALKEY[k.name];
                if (!string.Equals(k.pk, "ID", StringComparison.OrdinalIgnoreCase)
                    || k.naturalKey == null || !k.naturalKey.SequenceEqual(soll, StringComparer.OrdinalIgnoreCase))
                {
                    fehler = "Das Paket beschreibt den Katalog " + k.name + " anders als dieses Programm (Schlüssel). " +
                             "Import abgelehnt, nichts geändert.";
                    return false;
                }
            }
            foreach (KindMeta m in manifestKinder ?? new List<KindMeta>())
            {
                var fest = TWW_KINDER.Where(t => m != null
                    && string.Equals(t.Kind, m.name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(t.Kopf, m.parent, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(t.Spalte, m.parentColumn, StringComparison.OrdinalIgnoreCase)
                    && string.Equals("ID", m.pk, StringComparison.OrdinalIgnoreCase)).ToList();
                if (fest.Count != 1)
                {
                    fehler = "Das Paket führt eine unbekannte Kindtabelle " + (m?.name ?? "(ohne Namen)") +
                             " der Tww-Kataloge. Import abgelehnt, nichts geändert.";
                    return false;
                }
                if (kinder.Any(x => string.Equals(x.name, fest[0].Kind, StringComparison.OrdinalIgnoreCase))) continue;
                // Eine Kindtabelle, die diese Datenbank noch nicht führt (die Zapfkategorien vor
                // Schritt 115), kann weder verglichen noch eingespielt werden — sie bleibt liegen,
                // und der Bericht nennt sie.
                if (!DataRepository.TabelleVorhanden(fest[0].Kind))
                {
                    uebergangen?.Add("Die Kindzeilen " + fest[0].Kind + " des Pakets sind übergangen — diese Datenbank " +
                                     "führt die Tabelle nicht (älterer Schemastand); das Projekt ist ohne sie importiert.");
                    continue;
                }
                kinder.Add(new KindMeta { name = fest[0].Kind, parent = fest[0].Kopf, parentColumn = fest[0].Spalte, pk = "ID" });
            }
            return true;
        }

        private void TwwZuruecksetzen()
        {
            _twwMitgenommen = new Dictionary<string, Dictionary<long, long>>(StringComparer.OrdinalIgnoreCase);
            _twwBericht = new List<string>();
            _twwDirekt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _twwVorgemerkt = new Dictionary<long, (KatMeta, Dictionary<string, JsonElement>, Dictionary<string, Type>)>();
            _twwKinderMeta = new List<KindMeta>();
            _twwKindRows = new Dictionary<string, List<Dictionary<string, JsonElement>>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Die benannte Ablehnung einer Tww-Katalogzeile, die das Paket nicht führt.</summary>
        private static string TwwFehltImPaket(string tabelle, string spalte, long id, string katalog) =>
            "Eine Zeile aus " + tabelle + " verweist über " + spalte + " auf die Katalogzeile Id " + id + " aus " +
            katalog + ", die das Paket nicht führt - Import abgelehnt, nichts geändert.";

        /// <summary>
        /// Meldet für jede Spalte des Schemaschritts 121 (<see cref="TwwSchema.SpaltenT3"/>), die das
        /// Paket mit einem Wert trägt, die Zieldatenbank aber nicht führt (Stand vor 121), eine
        /// Berichtszeile: Der Import läuft durch, der Wert bleibt liegen. Die Vorgabe der Spalte
        /// (<c>Personen_Auto</c> = 1) zählt nicht als Wert, der verloren ginge.
        /// </summary>
        private void TwwSchritt121Melden(IEnumerable<Dictionary<string, List<Dictionary<string, JsonElement>>>> baeume,
                                         Dictionary<string, List<Dictionary<string, JsonElement>>> katalogzeilen)
        {
            var quellen = new List<Dictionary<string, List<Dictionary<string, JsonElement>>>>(baeume ?? Enumerable.Empty<Dictionary<string, List<Dictionary<string, JsonElement>>>>());
            if (katalogzeilen != null) quellen.Add(katalogzeilen);
            foreach (TwwSpalte s in TwwSchema.SpaltenT3)
            {
                int belegt = 0;
                foreach (var baum in quellen)
                    if (baum != null && baum.TryGetValue(s.Tabelle, out List<Dictionary<string, JsonElement>> zeilen) && zeilen != null)
                        belegt += zeilen.Count(z => z.TryGetValue(s.Name, out JsonElement w) && w.ValueKind != JsonValueKind.Null
                                                    && !(s.Name == TwwSchema.SPALTE_PERSONEN_AUTO && w.ValueKind == JsonValueKind.Number
                                                         && w.TryGetInt64(out long a) && a == 1));
                if (belegt == 0) continue;
                HashSet<string> ziel = ZielSpalten(s.Tabelle);
                if (ziel == null || ziel.Contains(s.Name)) continue;
                _twwBericht.Add("Die Zieldatenbank fuehrt " + s.Tabelle + "." + s.Name + " noch nicht (Schemastand vor 121): " +
                                belegt.ToString(CultureInfo.InvariantCulture) + " Wert(e) bleiben beim Import liegen.");
            }
        }

        /// <summary>Entfernt die internen Spalten (<see cref="TWW_INTERN"/>) aus einer Tww-Katalogtabelle vor dem Schreiben.</summary>
        private static void TwwInterneSpaltenEntfernen(string tabelle, DataTable dt)
        {
            if (dt == null || !IstTwwStamm(tabelle)) return;
            foreach (string s in TWW_INTERN)
                if (dt.Columns.Contains(s)) dt.Columns.Remove(s);
        }

        /// <summary>
        /// Merkt sich, auf welche Tww-Katalogzeilen die Projektzeilen des Pakets DIREKT zeigen
        /// (Zone, Wohnungstyp, Projektzeile) — nur diese werden ohne Bedarf mitgenommen.
        /// </summary>
        private void TwwDirekteVerweiseSammeln(IEnumerable<Dictionary<string, List<Dictionary<string, JsonElement>>>> baeume)
        {
            foreach (var baum in baeume)
                foreach (var zeilen in baum.Values)
                    foreach (var row in zeilen)
                        foreach (var kv in row)
                        {
                            if (!KATALOG_SPALTE_ZU_TABELLE.TryGetValue(kv.Key, out string katTab) || !IstTwwKatalog(katTab)) continue;
                            if (kv.Value.ValueKind != JsonValueKind.Number || !kv.Value.TryGetInt64(out long id) || id <= 0) continue;
                            _twwDirekt.Add(katTab + "||" + id);
                        }
        }

        /// <summary>
        /// Ein am Ziel fehlender Tagesgangsatz, auf den keine Projektzeile direkt zeigt, wird
        /// nur vorgemerkt (<c>true</c>) — eine mitgenommene Nutzungsart holt ihn bei Bedarf.
        /// </summary>
        private bool TwwVormerken(KatMeta k, Dictionary<string, JsonElement> row, Dictionary<string, Type> zielTypen)
        {
            if (!string.Equals(k.name, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, StringComparison.OrdinalIgnoreCase)) return false;
            long altId = row[k.pk].GetInt64();
            if (_twwDirekt.Contains(k.name + "||" + altId)) return false;
            _twwVorgemerkt[altId] = (k, row, zielTypen);
            return true;
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
                if (!DataRepository.TabelleVorhanden(k.Kind)) continue;   // Stand vor 115: keine Zapfkategorien

                DataTable alle = null;
                foreach (long id in koepfe.OrderBy(x => x))
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT * FROM [" + k.Kind + "] WHERE [" + k.Spalte + "] = ? ORDER BY ID", new DbParam("@id", id));
                    if (dt == null) continue;
                    if (alle == null) alle = dt; else alle.Merge(dt);
                }
                if (alle == null || alle.Rows.Count == 0) continue;

                TwwInterneSpaltenEntfernen(k.Kind, alle);
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

            // ZU17: Steht der natürliche Schlüssel am Ziel schon (mit anderem Inhalt — sonst
            // wären wir nicht hier), kommt die Zeile als neue Version „… (Import n)“; eine
            // frühere Version mit demselben Inhalt wird wiederverwendet.
            string namensspalte = TwwNamensspalte(k);
            string neuerName = null;
            if (TwwSchluesselId(v, k, row, null, zielTypen).HasValue)
            {
                string basis = Convert.ToString(JsonToObject(row[namensspalte]), CultureInfo.InvariantCulture);
                for (int n = 1; neuerName == null; n++)
                {
                    string kandidat = basis + TwwZusatz(n);
                    long? vorhanden = TwwSchluesselId(v, k, row, kandidat, zielTypen);
                    if (!vorhanden.HasValue) { neuerName = kandidat; break; }
                    if (TwwInhaltGleich(v, k, row, vorhanden.Value, katMap))
                    {
                        _twwBericht.Add("Katalogzeile " + name + " (" + k.name + ") weicht vom namensgleichen Eintrag am " +
                                        "Ziel ab und entspricht der schon mitgenommenen Version „" + kandidat +
                                        "“ - das Projekt zeigt auf diese.");
                        return vorhanden.Value;
                    }
                }
            }

            var cs = row.Keys.Where(x => !x.Equals(k.pk, StringComparison.OrdinalIgnoreCase)
                                         && zielTypen.ContainsKey(x)).ToList();
            var cps = new List<DbParam>();
            for (int n = 0; n < cs.Count; n++)
            {
                string spalte = cs[n];
                object wert = JsonToObject(row[spalte]);

                if (neuerName != null && spalte.Equals(namensspalte, StringComparison.OrdinalIgnoreCase)) wert = neuerName;
                else if (spalte.Equals("Status", StringComparison.OrdinalIgnoreCase)) wert = TwwSchema.STATUS_IMPORT;
                else if (spalte.Equals("ReadOnly", StringComparison.OrdinalIgnoreCase)) wert = 0L;
                else if (spalte.Equals("ID_Vorlage", StringComparison.OrdinalIgnoreCase)) wert = null;
                else if (TWW_INTERN.Contains(spalte, StringComparer.OrdinalIgnoreCase)) wert = null;
                else if (spalte.Equals("ID_Tagesgangsatz", StringComparison.OrdinalIgnoreCase) && wert != null)
                {
                    long altSatz = Convert.ToInt64(wert);
                    string schluessel = TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + "||" + altSatz;
                    if (!katMap.TryGetValue(schluessel, out long satz))
                    {
                        // Nur als Abhängigkeit gereist und am Ziel fehlend: jetzt gebraucht.
                        if (!_twwVorgemerkt.TryGetValue(altSatz, out var vm))
                            throw new Exception(
                                "Die Katalogzeile " + name + " aus " + k.name + " fehlt am Ziel und kann nicht " +
                                "mitgenommen werden: ihr Tagesgangsatz steht nicht im Paket. Import abgelehnt, nichts geändert.");
                        _twwVorgemerkt.Remove(altSatz);
                        satz = TwwZeileMitnehmen(v, vm.Meta, vm.Zeile, katMap, vm.Typen);
                        katMap[schluessel] = satz;
                    }
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

            _twwBericht.Add(neuerName == null
                ? "Katalogzeile " + name + " (" + k.name + ") fehlte am Ziel und wurde mit Status IMPORT mitgenommen."
                : "Katalogzeile " + name + " (" + k.name + ") weicht vom namensgleichen Eintrag am Ziel ab und wurde " +
                  "als neue Version „" + neuerName + "“ mit Status IMPORT mitgenommen; der Eintrag am Ziel bleibt unverändert.");
            return neuId;
        }

        // =================================================================================
        //  ZU17 — Inhaltsvergleich namensgleicher Zeilen
        // =================================================================================

        /// <summary>Die Spalte, die den Zusatz „ (Import n)“ trägt: <c>Schluessel</c> beim DIN-4708-Wert, sonst <c>Bezeichner</c>.</summary>
        private static string TwwNamensspalte(KatMeta k) =>
            string.Equals(k.name, TwwSchema.TAB_TWW_DIN4708_WERT_STAMM, StringComparison.OrdinalIgnoreCase)
                ? "Schluessel" : "Bezeichner";

        /// <summary>
        /// Die Id der Zielzeile mit dem natürlichen Schlüssel der Paketzeile —
        /// mit <paramref name="name"/> an Stelle ihres Bezeichners (bzw. Schlüssels), wenn gesetzt.
        /// </summary>
        private long? TwwSchluesselId(DbVorgang v, KatMeta k, Dictionary<string, JsonElement> row, string name,
                                      Dictionary<string, Type> zielTypen)
        {
            string namensspalte = TwwNamensspalte(k);
            var wo = new List<string>();
            var ps = new List<DbParam>();
            foreach (string key in k.naturalKey)
            {
                object wert = name != null && key.Equals(namensspalte, StringComparison.OrdinalIgnoreCase)
                    ? name : JsonToObject(row[key]);
                wo.Add("[" + key + "] = ?");
                ps.Add(MacheParam("@k" + ps.Count, wert, TypVon(zielTypen, key)));
            }
            object o = v.Skalar("SELECT [" + k.pk + "] FROM [" + k.name + "] WHERE " + string.Join(" AND ", wo), ps.ToArray());
            return o == null || o == DBNull.Value ? (long?)null : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Trägt die Zielzeile <paramref name="zielId"/> denselben Inhalt wie die Paketzeile? Eine
        /// Auslieferungszeile gilt als gleich (unveränderliche Version). Verglichen werden die
        /// Wertspalten des Kopfes, der Tagesgangsatz einer Nutzungsart über seine Tagesgänge und
        /// die Kindzeilen als Menge.
        /// </summary>
        private bool TwwInhaltGleich(DbVorgang v, KatMeta k, Dictionary<string, JsonElement> row, long zielId,
                                     Dictionary<string, long> katMap)
        {
            DataTable dt = v.Lese("SELECT * FROM [" + k.name + "] WHERE [" + k.pk + "] = ?", new DbParam("@id", zielId));
            if (dt == null || dt.Rows.Count == 0) return false;
            DataRow ziel = dt.Rows[0];
            if (dt.Columns.Contains("Status")
                && string.Equals(Convert.ToString(ziel["Status"], CultureInfo.InvariantCulture), TwwSchema.STATUS_AUSLIEFERUNG,
                                 StringComparison.Ordinal))
                return true;

            foreach (var kv in row)
            {
                string s = kv.Key;
                if (!dt.Columns.Contains(s) || TwwNichtVerglichen(k, s)) continue;
                object paket = JsonToObject(kv.Value);
                if (s.Equals("ID_Tagesgangsatz", StringComparison.OrdinalIgnoreCase))
                {
                    // Der Satz zählt über seinen INHALT (die vier Tagesgänge), nicht über seine Id:
                    // Die Zielzeile ist gleich, wenn ihr Satz dieselben Tagesgänge trägt wie der
                    // Satz des Pakets — gleich, unter welchem Namen er am Ziel steht.
                    object ist = ziel[s];
                    bool istLeer = ist == null || ist == DBNull.Value;
                    if (paket == null) { if (istLeer) continue; return false; }
                    if (istLeer) return false;
                    KindMeta gaenge = _twwKinderMeta.FirstOrDefault(m =>
                        string.Equals(m.parent, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, StringComparison.OrdinalIgnoreCase));
                    if (gaenge == null || !TwwKinderGleich(v, gaenge, Convert.ToInt64(paket, CultureInfo.InvariantCulture),
                                                           Convert.ToInt64(ist, CultureInfo.InvariantCulture)))
                        return false;
                    continue;
                }
                if (TwwNorm(paket) != TwwNorm(ziel[s])) return false;
            }

            long altId = row[k.pk].GetInt64();
            foreach (KindMeta km in _twwKinderMeta)
                if (string.Equals(km.parent, k.name, StringComparison.OrdinalIgnoreCase) && !TwwKinderGleich(v, km, altId, zielId))
                    return false;
            return true;
        }

        /// <summary>Stimmen die Kindzeilen eines Paketkopfs und einer Zielzeile als Menge überein?</summary>
        private bool TwwKinderGleich(DbVorgang v, KindMeta km, long altId, long zielId)
        {
            DataTable dt = v.Lese("SELECT * FROM [" + km.name + "] WHERE [" + km.parentColumn + "] = ?",
                                  new DbParam("@id", zielId));
            var spalten = new List<string>();
            if (dt != null)
                foreach (DataColumn c in dt.Columns)
                    if (!c.ColumnName.Equals(km.pk, StringComparison.OrdinalIgnoreCase)
                        && !c.ColumnName.Equals(km.parentColumn, StringComparison.OrdinalIgnoreCase)
                        && !TWW_PROVENIENZ.IsMatch(c.ColumnName)
                        && !TWW_INTERN.Contains(c.ColumnName, StringComparer.OrdinalIgnoreCase)
                        && !TWW_KIND_VERWALTUNG.Contains(c.ColumnName, StringComparer.OrdinalIgnoreCase))
                        spalten.Add(c.ColumnName);
            spalten.Sort(StringComparer.OrdinalIgnoreCase);

            var ziel = new List<string>();
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                    ziel.Add(string.Join(";", spalten.Select(s => s + "=" + TwwNorm(r[s]))));

            var paket = new List<string>();
            if (_twwKindRows.TryGetValue(km.name, out var zeilen))
                foreach (var r in zeilen)
                {
                    if (!r.TryGetValue(km.parentColumn, out JsonElement e) || e.ValueKind != JsonValueKind.Number
                        || e.GetInt64() != altId) continue;
                    paket.Add(string.Join(";", spalten.Select(s =>
                        s + "=" + TwwNorm(r.TryGetValue(s, out JsonElement je) ? JsonToObject(je) : null))));
                }

            ziel.Sort(StringComparer.Ordinal);
            paket.Sort(StringComparer.Ordinal);
            return ziel.SequenceEqual(paket, StringComparer.Ordinal);
        }

        /// <summary>Verwaltungs-, Schlüssel-, interne und Provenienzspalten tragen den Inhalt nicht.</summary>
        private static bool TwwNichtVerglichen(KatMeta k, string spalte) =>
            spalte.Equals(k.pk, StringComparison.OrdinalIgnoreCase)
            || TWW_NICHT_VERGLICHEN.Contains(spalte)
            || (k.naturalKey ?? new string[0]).Contains(spalte, StringComparer.OrdinalIgnoreCase)
            || TWW_PROVENIENZ.IsMatch(spalte);

        /// <summary>Ein Wert in vergleichbarer Textform: Zahlen (auch Wahrheitswerte) als double „R“, Text mit Vorsatz, leer als ∅.</summary>
        private static string TwwNorm(object w)
        {
            if (w == null || w == DBNull.Value) return "∅";
            switch (w)
            {
                case bool b: return b ? "1" : "0";
                case string s: return "s:" + s;
                case byte _: case short _: case int _: case long _: case float _: case double _: case decimal _:
                    return Convert.ToDouble(w, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture);
                default: return "s:" + Convert.ToString(w, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Spielt die Kindzeilen (Tagesgänge, Ereignisse, Zapfkategorien) der in diesem Import
        /// mitgenommenen Köpfe ein, umgeschlüsselt auf den neuen Kopf. Kinder eines am Ziel
        /// GEFUNDENEN Kopfs bleiben liegen — der Kopf bringt dort seine eigenen mit. Eine
        /// Kindzeile mit eigenem Stand (Zapfkategorie) kommt wie ihr Kopf als
        /// <c>Status = 'IMPORT'</c>, <c>ReadOnly = 0</c>, ohne internen Beleg.
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
                        string spalte = cs[n];
                        object wert;
                        if (spalte.Equals(k.parentColumn, StringComparison.OrdinalIgnoreCase)) wert = neuerKopf;
                        else if (spalte.Equals("Status", StringComparison.OrdinalIgnoreCase)) wert = TwwSchema.STATUS_IMPORT;
                        else if (spalte.Equals("ReadOnly", StringComparison.OrdinalIgnoreCase)) wert = 0L;
                        else if (TWW_INTERN.Contains(spalte, StringComparer.OrdinalIgnoreCase)) wert = null;
                        else wert = JsonToObject(row[spalte]);
                        cps.Add(MacheParam("@c" + n, wert, TypVon(zielTypen, spalte)));
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
