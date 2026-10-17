using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // TEILKATALOGE DES BAUTEILKATALOGS (BK-7, Entwurf 2.3 Regel 12: "Der Name des Teilkatalogs geht in
    // die Quelle").
    //
    // DIE DATEIEN: Katalog.csv fuehrt die Teilkataloge (Kennung und Name), Katalog_Inhalt.csv ordnet ihnen
    // die Saetze zu. Ihre Spaltennamen nennt das Feldinventar nicht; der Leser verlaesst sich deshalb auf
    // keine Verweisspalte, sondern nur auf die Kennungen selbst (GUID sind eindeutig):
    //   - Katalog.csv: Kennung aus "UId" (sonst der ersten Spalte, die auf "UId" endet), Name aus der
    //     ersten vorhandenen Spalte von NAMENSSPALTEN.
    //   - Katalog_Inhalt.csv: Jede Zeile wird nach Kennungen durchsucht. Eine Kennung aus Katalog.csv ist
    //     ihr Teilkatalog, eine Kennung eines Satzes der Quelle (Baustoff, Aufbau oder deren Basiszeile)
    //     ihr Inhalt, eine Kennung einer anderen Zeile von Katalog_Inhalt.csv ihr Elternknoten. Traegt die
    //     Zeile selbst keinen Teilkatalog, gilt der naechste Elternknoten mit einem (Baum des Katalogs).
    //   - Katalog_Zuordnung.csv (BK-7b, Vertrag des Exports, Kopf UId;CatalogContentUId;CatalogUId;BasisUId;
    //     DimensionUId;StateType;SortNum): liegt sie vor, gilt sie vorrangig. BasisUId trifft beim Baustoff die
    //     Basiszeile (Basis_Auszug.csv.UId, deren MaterialUId auf ihn zeigt), beim Aufbau dessen BasisUId;
    //     DimensionUId trifft die Kennung UId eines Aufbaus. Massgeblich ist CatalogUId, auch wenn der
    //     Inhaltsknoten (CatalogContentUId) in einem anderen Teilkatalog haengt - die Abweichungen zaehlt das
    //     Protokoll. Steht ein Satz in zwei Teilkatalogen, nennt die Quelle beide (wie bei der zusammengefuehrten
    //     Dublette); die Vorschau zaehlt diese Saetze. Fehlt die Datei, gilt der Weg ueber Katalog_Inhalt.csv.
    // Fehlt eine Datei oder Spalte oder trifft keine Zeile einen Satz, steht ein benannter Hinweis im
    // Protokoll und der Import laeuft ohne Teilkatalognamen weiter.
    //
    // DIE QUELLE: "<Praefix>, Datenstand JJJJ-MM, Teilkatalog <Name>" bzw. ", Teilkataloge <A> / <B>",
    // wenn ein Satz in mehreren Teilkatalogen steht oder in der Quelle mit Saetzen anderer Teilkataloge
    // zusammengefuehrt wurde; nie laenger als BauteilkatalogSchema.QUELLE_MAX_ZEICHEN.
    // ====================================================================================

    /// <summary>Die Zuordnung der Sätze der Quelle zu ihren Teilkatalogen (BK-7).</summary>
    public static class BauteilkatalogTeilkataloge
    {
        /// <summary>Die Spalten, die in <c>Katalog.csv</c> als Name gelesen werden — die erste vorhandene gilt.</summary>
        public static readonly IReadOnlyList<string> NAMENSSPALTEN =
            new[] { "ShortDesc", "Name", "Description", "Caption", "Title", "Bezeichnung", "LongDesc" };

        /// <summary>Höchstzahl der Elternschritte im Baum von <c>Katalog_Inhalt.csv</c>.</summary>
        public const int TIEFE_MAX = 32;

        /// <summary>Die Höchstlänge eines Teilkatalognamens in der Quelle.</summary>
        public const int NAME_MAX_ZEICHEN = 80;

        /// <summary>Die Spalten von <c>Katalog_Zuordnung.csv</c> (Vertrag des Exports, BK-7b).</summary>
        public const string SPALTE_KATALOG = "CatalogUId", SPALTE_INHALT = "CatalogContentUId",
                            SPALTE_BASIS = "BasisUId", SPALTE_DIMENSION = "DimensionUId";

        /// <summary>Das Ergebnis der Zuordnung.</summary>
        internal sealed class Ergebnis
        {
            /// <summary>Satzkennung → Namen ihrer Teilkataloge.</summary>
            internal Dictionary<string, SortedSet<string>> Saetze = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            /// <summary>Die Teilkataloge mit Kennung und Name in <c>Katalog.csv</c>.</summary>
            internal int InQuelle;
            /// <summary>Wahr, wenn die Zuordnung aus <c>Katalog_Zuordnung.csv</c> stammt (BK-7b).</summary>
            internal bool AusZuordnung;
        }

        /// <summary>
        /// Ordnet die Sätze zu. <paramref name="ziele"/> bildet eine Kennung (Satz oder Basiszeile) auf die
        /// Kennungen der Sätze ab, die sie meint. Liegt <c>Katalog_Zuordnung.csv</c> vor und taugt sie, gilt sie
        /// (BK-7b); sonst der Weg über <c>Katalog_Inhalt.csv</c> (BK-7).
        /// </summary>
        internal static Ergebnis Zuordnen(BauteilkatalogQuelle q,
                                          IReadOnlyDictionary<string, List<string>> ziele,
                                          List<string> protokoll)
        {
            var e = new Ergebnis();
            BauteilkatalogTabelle tk = q.Tabelle(BauteilkatalogQuelle.DATEI_KATALOG);
            BauteilkatalogTabelle ti = q.Tabelle(BauteilkatalogQuelle.DATEI_KATALOG_INHALT);
            BauteilkatalogTabelle tz = q.Tabelle(BauteilkatalogQuelle.DATEI_KATALOG_ZUORDNUNG);
            if (tk == null) return e;
            Dictionary<string, string> namen = Namen(tk, protokoll);
            if (namen == null) return e;
            e.InQuelle = namen.Count;
            InhaltBaum baum = ti == null ? null : new InhaltBaum(ti);

            if (tz != null && AusZuordnung(tz, baum, namen, ziele, e.Saetze, protokoll))
            {
                e.AusZuordnung = true;
                return e;
            }
            if (baum == null) return e;

            int zeilenMitSatz = 0, ohneKatalog = 0;
            for (int i = 0; i < baum.Zeilen.Count; i++)
            {
                var saetze = new HashSet<string>(StringComparer.Ordinal);
                foreach (string v in baum.Zeilen[i].Verweise)
                    if (ziele.TryGetValue(v, out List<string> s)) saetze.UnionWith(s);
                if (saetze.Count == 0) continue;
                zeilenMitSatz++;
                List<string> kataloge = baum.KatalogeDer(i, namen);
                if (kataloge.Count == 0) { ohneKatalog++; continue; }
                foreach (string satz in saetze)
                    foreach (string k in kataloge) Eintragen(e.Saetze, satz, namen[k]);
            }
            if (zeilenMitSatz == 0)
                protokoll.Add(BauteilkatalogQuelle.DATEI_KATALOG_INHALT + ": keine Zeile trifft einen Satz der Quelle, ohne Teilkatalognamen weiter");
            else if (ohneKatalog > 0)
                protokoll.Add(BauteilkatalogQuelle.DATEI_KATALOG_INHALT + ": Zeilen mit Satz, aber ohne Teilkatalog: " + Zahl(ohneKatalog));
            return e;
        }

        /// <summary>Die Teilkataloge aus <c>Katalog.csv</c>: Kennung → Name; <c>null</c> mit Hinweis, wenn keiner taugt.</summary>
        private static Dictionary<string, string> Namen(BauteilkatalogTabelle tk, List<string> protokoll)
        {
            string kennSpalte = tk.Spalten.ContainsKey("UId")
                ? "UId"
                : tk.Spalten.OrderBy(s => s.Value).Select(s => s.Key).FirstOrDefault(s => s.EndsWith("UId", StringComparison.OrdinalIgnoreCase));
            string nameSpalte = NAMENSSPALTEN.FirstOrDefault(s => tk.Spalten.ContainsKey(s));
            if (kennSpalte == null || nameSpalte == null)
            {
                protokoll.Add(BauteilkatalogQuelle.DATEI_KATALOG + ": Spalte fehlt (" +
                              (kennSpalte == null ? "Kennung UId" : "Name: " + string.Join(", ", NAMENSSPALTEN)) +
                              "), ohne Teilkatalognamen weiter");
                return null;
            }
            var namen = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string[] z in tk.Zeilen)
            {
                string k = tk.Kennung(z, kennSpalte);
                string n = tk.Text(z, nameSpalte);
                if (k == null || string.IsNullOrEmpty(n)) continue;
                namen[k] = n.Length <= NAME_MAX_ZEICHEN ? n : n.Substring(0, NAME_MAX_ZEICHEN).TrimEnd();
            }
            if (namen.Count == 0)
            {
                protokoll.Add(BauteilkatalogQuelle.DATEI_KATALOG + ": kein Teilkatalog mit Kennung und Name, ohne Teilkatalognamen weiter");
                return null;
            }
            return namen;
        }

        /// <summary>
        /// Der Weg über <c>Katalog_Zuordnung.csv</c> (BK-7b): je Zeile trifft <c>BasisUId</c> oder
        /// <c>DimensionUId</c> über <paramref name="ziele"/> die Sätze, <c>CatalogUId</c> nennt den Teilkatalog.
        /// Weicht <c>CatalogUId</c> vom Teilkatalog des Inhaltsknotens (<c>CatalogContentUId</c>) ab, gilt
        /// <c>CatalogUId</c>; die Abweichungen werden gezählt. Rückgabe <c>false</c> mit Hinweis, wenn die Datei
        /// nicht taugt — dann gilt der Weg über <c>Katalog_Inhalt.csv</c>.
        /// </summary>
        private static bool AusZuordnung(BauteilkatalogTabelle tz, InhaltBaum baum, Dictionary<string, string> namen,
                                         IReadOnlyDictionary<string, List<string>> ziele,
                                         Dictionary<string, SortedSet<string>> saetze, List<string> protokoll)
        {
            string datei = BauteilkatalogQuelle.DATEI_KATALOG_ZUORDNUNG;
            var fehlt = new[] { SPALTE_KATALOG, SPALTE_BASIS, SPALTE_DIMENSION }.Where(s => !tz.Spalten.ContainsKey(s)).ToList();
            if (fehlt.Count > 0)
            {
                protokoll.Add(datei + ": Spalte fehlt (" + string.Join(", ", fehlt) + "), Teilkataloge aus " +
                              BauteilkatalogQuelle.DATEI_KATALOG_INHALT);
                return false;
            }
            bool mitInhalt = baum != null && tz.Spalten.ContainsKey(SPALTE_INHALT);
            int mitSatz = 0, ohneName = 0, abweichend = 0;
            foreach (string[] z in tz.Zeilen)
            {
                var treffer = new HashSet<string>(StringComparer.Ordinal);
                foreach (string spalte in new[] { SPALTE_BASIS, SPALTE_DIMENSION })
                {
                    string k = tz.Kennung(z, spalte);
                    if (k != null && ziele.TryGetValue(k, out List<string> s)) treffer.UnionWith(s);
                }
                if (treffer.Count == 0) continue;
                mitSatz++;
                string katalog = tz.Kennung(z, SPALTE_KATALOG);
                if (katalog == null || !namen.TryGetValue(katalog, out string name)) { ohneName++; continue; }
                if (mitInhalt)
                {
                    string knoten = tz.Kennung(z, SPALTE_INHALT);
                    if (knoten != null && baum.Index(knoten) is int i)
                    {
                        List<string> imBaum = baum.KatalogeDer(i, namen);
                        if (imBaum.Count > 0 && !imBaum.Contains(katalog)) abweichend++;
                    }
                }
                foreach (string satz in treffer) Eintragen(saetze, satz, name);
            }
            protokoll.Add(datei + ": Teilkataloge aus dieser Datei; " + Zahl(mitSatz) + " von " + Zahl(tz.Anzahl) +
                          " Zeilen treffen einen Satz der Quelle");
            if (mitSatz == 0)
                protokoll.Add(datei + ": keine Zeile trifft einen Satz der Quelle, ohne Teilkatalognamen weiter");
            if (ohneName > 0)
                protokoll.Add(datei + ": Zeilen mit Satz, aber ohne benannten Teilkatalog: " + Zahl(ohneName));
            if (abweichend > 0)
                protokoll.Add(datei + ": CatalogUId weicht vom Teilkatalog des Inhaltsknotens ab, CatalogUId gilt: " + Zahl(abweichend));
            return true;
        }

        private static void Eintragen(Dictionary<string, SortedSet<string>> saetze, string satz, string name)
        {
            if (!saetze.TryGetValue(satz, out SortedSet<string> menge))
                saetze[satz] = menge = new SortedSet<string>(StringComparer.Ordinal);
            menge.Add(name);
        }

        private static string Zahl(int n) => n.ToString("N0", CultureInfo.InvariantCulture);

        /// <summary>
        /// Der Baum von <c>Katalog_Inhalt.csv</c>: je Zeile die eigene Kennung und alle übrigen Kennungen; die
        /// Teilkataloge einer Zeile sind ihre eigenen, sonst die des nächsten Elternknotens.
        /// </summary>
        private sealed class InhaltBaum
        {
            internal readonly List<(string Eigen, List<string> Verweise)> Zeilen;
            private readonly Dictionary<string, int> _nachEigen = new Dictionary<string, int>(StringComparer.Ordinal);

            internal InhaltBaum(BauteilkatalogTabelle ti)
            {
                bool mitEigener = ti.Spalten.TryGetValue("UId", out int eigenIndex);
                Zeilen = new List<(string, List<string>)>(ti.Anzahl);
                foreach (string[] z in ti.Zeilen)
                {
                    string eigen = mitEigener ? BauteilkatalogQuelle.Kennung(z[eigenIndex]) : null;
                    var verweise = new List<string>();
                    for (int i = 0; i < z.Length; i++)
                    {
                        if (mitEigener && i == eigenIndex) continue;
                        string f = z[i];
                        if (f.Length < 32 || f.Length > 38) continue;
                        string k = BauteilkatalogQuelle.Kennung(f);
                        if (k != null) verweise.Add(k);
                    }
                    if (eigen != null && !_nachEigen.ContainsKey(eigen)) _nachEigen[eigen] = Zeilen.Count;
                    Zeilen.Add((eigen, verweise));
                }
            }

            /// <summary>Die Zeile mit der eigenen Kennung <paramref name="eigen"/>, sonst <c>null</c>.</summary>
            internal int? Index(string eigen) => _nachEigen.TryGetValue(eigen, out int i) ? i : (int?)null;

            internal List<string> KatalogeDer(int start, Dictionary<string, string> namen)
            {
                var besucht = new HashSet<int> { start };
                var ebene = new List<int> { start };
                for (int tiefe = 0; tiefe <= TIEFE_MAX && ebene.Count > 0; tiefe++)
                {
                    var treffer = ebene.SelectMany(i => Zeilen[i].Verweise).Where(namen.ContainsKey).Distinct().ToList();
                    if (treffer.Count > 0) return treffer;
                    var naechste = new List<int>();
                    foreach (int i in ebene)
                        foreach (string v in Zeilen[i].Verweise)
                            if (_nachEigen.TryGetValue(v, out int e) && besucht.Add(e)) naechste.Add(e);
                    ebene = naechste;
                }
                return new List<string>();
            }
        }

        /// <summary>
        /// Die Quelle eines Satzes: <paramref name="quelle"/> ergänzt um seine Teilkataloge — höchstens
        /// <see cref="BauteilkatalogSchema.QUELLE_MAX_ZEICHEN"/> Zeichen, überzählige Namen als „u. a.“.
        /// </summary>
        public static string QuelleMit(string quelle, IEnumerable<string> teilkataloge)
        {
            List<string> namen = (teilkataloge ?? Enumerable.Empty<string>()).Where(n => !string.IsNullOrWhiteSpace(n))
                                                                             .Distinct(StringComparer.Ordinal).ToList();
            if (namen.Count == 0) return quelle;
            const string MEHR = " u. a.";
            string kopf = quelle + (namen.Count == 1 ? ", Teilkatalog " : ", Teilkataloge ");
            int max = BauteilkatalogSchema.QUELLE_MAX_ZEICHEN;
            string s = kopf;
            for (int i = 0; i < namen.Count; i++)
            {
                string naechste = s + (i == 0 ? "" : " / ") + namen[i];
                bool letzte = i == namen.Count - 1;
                if (naechste.Length + (letzte ? 0 : MEHR.Length) > max)
                {
                    if (i == 0) return quelle.Length <= max ? quelle : quelle.Substring(0, max);
                    return s + MEHR;
                }
                s = naechste;
            }
            return s;
        }
    }
}
