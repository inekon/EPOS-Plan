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

        /// <summary>
        /// Ordnet die Sätze zu. <paramref name="ziele"/> bildet eine Kennung (Satz oder Basiszeile) auf die
        /// Kennungen der Sätze ab, die sie meint. Rückgabe: Satzkennung → Namen ihrer Teilkataloge.
        /// </summary>
        internal static Dictionary<string, SortedSet<string>> Zuordnen(BauteilkatalogQuelle q,
                                                                        IReadOnlyDictionary<string, List<string>> ziele,
                                                                        List<string> protokoll)
        {
            var ergebnis = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            BauteilkatalogTabelle tk = q.Tabelle(BauteilkatalogQuelle.DATEI_KATALOG);
            BauteilkatalogTabelle ti = q.Tabelle(BauteilkatalogQuelle.DATEI_KATALOG_INHALT);
            if (tk == null || ti == null) return ergebnis;

            string kennSpalte = tk.Spalten.ContainsKey("UId")
                ? "UId"
                : tk.Spalten.OrderBy(s => s.Value).Select(s => s.Key).FirstOrDefault(s => s.EndsWith("UId", StringComparison.OrdinalIgnoreCase));
            string nameSpalte = NAMENSSPALTEN.FirstOrDefault(s => tk.Spalten.ContainsKey(s));
            if (kennSpalte == null || nameSpalte == null)
            {
                protokoll.Add(BauteilkatalogQuelle.DATEI_KATALOG + ": Spalte fehlt (" +
                              (kennSpalte == null ? "Kennung UId" : "Name: " + string.Join(", ", NAMENSSPALTEN)) +
                              "), ohne Teilkatalognamen weiter");
                return ergebnis;
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
                return ergebnis;
            }

            // Je Zeile von Katalog_Inhalt.csv: eigene Kennung und alle übrigen Kennungen.
            bool mitEigener = ti.Spalten.TryGetValue("UId", out int eigenIndex);
            var zeilen = new List<(string Eigen, List<string> Verweise)>(ti.Anzahl);
            var nachEigen = new Dictionary<string, int>(StringComparer.Ordinal);
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
                if (eigen != null && !nachEigen.ContainsKey(eigen)) nachEigen[eigen] = zeilen.Count;
                zeilen.Add((eigen, verweise));
            }

            // Die Teilkataloge einer Zeile: eigene, sonst die des nächsten Elternknotens.
            List<string> KatalogeDer(int start)
            {
                var besucht = new HashSet<int> { start };
                var ebene = new List<int> { start };
                for (int tiefe = 0; tiefe <= TIEFE_MAX && ebene.Count > 0; tiefe++)
                {
                    var treffer = ebene.SelectMany(i => zeilen[i].Verweise).Where(namen.ContainsKey).Distinct().ToList();
                    if (treffer.Count > 0) return treffer;
                    var naechste = new List<int>();
                    foreach (int i in ebene)
                        foreach (string v in zeilen[i].Verweise)
                            if (nachEigen.TryGetValue(v, out int e) && besucht.Add(e)) naechste.Add(e);
                    ebene = naechste;
                }
                return new List<string>();
            }

            int zeilenMitSatz = 0, ohneKatalog = 0;
            for (int i = 0; i < zeilen.Count; i++)
            {
                var saetze = new HashSet<string>(StringComparer.Ordinal);
                foreach (string v in zeilen[i].Verweise)
                    if (ziele.TryGetValue(v, out List<string> s)) saetze.UnionWith(s);
                if (saetze.Count == 0) continue;
                zeilenMitSatz++;
                List<string> kataloge = KatalogeDer(i);
                if (kataloge.Count == 0) { ohneKatalog++; continue; }
                foreach (string satz in saetze)
                {
                    if (!ergebnis.TryGetValue(satz, out SortedSet<string> menge))
                        ergebnis[satz] = menge = new SortedSet<string>(StringComparer.Ordinal);
                    foreach (string k in kataloge) menge.Add(namen[k]);
                }
            }
            if (zeilenMitSatz == 0)
                protokoll.Add(BauteilkatalogQuelle.DATEI_KATALOG_INHALT + ": keine Zeile trifft einen Satz der Quelle, ohne Teilkatalognamen weiter");
            else if (ohneKatalog > 0)
                protokoll.Add(BauteilkatalogQuelle.DATEI_KATALOG_INHALT + ": Zeilen mit Satz, aber ohne Teilkatalog: " +
                              ohneKatalog.ToString("N0", CultureInfo.InvariantCulture));
            return ergebnis;
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
