using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using S = DocumentFormat.OpenXml.Spreadsheet;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Diagramme der Anwendervorlage nach dem Füllen</b> (Konzept Berichtsvorlagen 7.4; Messproben 2 und 6 von BV-E0;
    /// Etappe BV-E8). ClosedXML behält die Diagramme einer Vorlage samt Reihenformeln, verschiebt aber keinen Bezug, wenn eine
    /// Tabelle wächst, und trägt ihren Zwischenspeicher nicht nach. Beides geschieht hier über das SDK auf dem gespeicherten
    /// Paket:
    /// <list type="number">
    /// <item>Eine Reihe, die genau auf die Datenzeilen (oder Kopf und Datenzeilen) einer gewachsenen Excel-Tabelle
    /// <c>EPOS_&lt;name&gt;</c> zeigte, zeigt danach auf deren neue Datenzeilen.</item>
    /// <item>Jede Reihe mit einem einfachen Bezug oder einem Namen (etwa <c>EPOS.reihe.*</c>) bekommt ihren Zwischenspeicher
    /// aus den Zellen der Mappe — so zeigt auch eine Vorschau ohne Rechenwerk die neuen Werte. Trägt eine Zelle des Bezugs
    /// eine Formel ohne Ergebnis, bleibt der Speicher, wie er war (Excel rechnet beim Öffnen neu).</item>
    /// </list>
    /// </summary>
    internal static class Vorlagendiagramme
    {
        private static readonly Regex BEZUG = new Regex(
            @"^(?:'(?<q>(?:[^']|'')+)'|(?<b>[^'!\[\]]+))!\$?(?<s1>[A-Za-z]{1,3})\$?(?<z1>\d+)(?::\$?(?<s2>[A-Za-z]{1,3})\$?(?<z2>\d+))?$");

        /// <summary>Zieht die Diagramme aller Blätter nach.</summary>
        internal static void Nachfuehren(WorkbookPart mappe, IReadOnlyDictionary<string, WorksheetPart> teile,
                                         IReadOnlyList<Tabellenwachstum> wachstum)
        {
            var zellen = new Zelllesung(mappe, teile);
            var namen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (S.DefinedName n in mappe.Workbook.DefinedNames?.Elements<S.DefinedName>() ?? Enumerable.Empty<S.DefinedName>())
                if (n.Name?.Value != null && n.LocalSheetId == null) namen[n.Name.Value] = n.Text;

            foreach (WorksheetPart t in teile.Values)
            {
                if (t.DrawingsPart == null) continue;
                foreach (ChartPart c in t.DrawingsPart.ChartParts.ToList()) Bearbeite(c, wachstum, zellen, namen);
            }
        }

        private static void Bearbeite(ChartPart c, IReadOnlyList<Tabellenwachstum> wachstum, Zelllesung zellen,
                                      Dictionary<string, string> namen)
        {
            if (c.ChartSpace == null) return;
            foreach (C.Formula f in c.ChartSpace.Descendants<C.Formula>().ToList())
            {
                string neu = Wachse(f.Text, wachstum);
                if (neu != null) f.Text = neu;
            }
            foreach (C.NumberReference r in c.ChartSpace.Descendants<C.NumberReference>().ToList())
            {
                List<Zellwert> werte = Werte(r.Formula?.Text, zellen, namen);
                if (werte == null) continue;
                string format = r.NumberingCache?.FormatCode?.Text ?? "General";
                var speicher = Exceldiagrammschreiber.Zahlspeicher(werte.Select(w => w.Zahl).ToList(), werte.Count, format);
                if (r.NumberingCache != null) r.NumberingCache.Remove();
                r.Formula.InsertAfterSelf(speicher);
            }
            foreach (C.StringReference r in c.ChartSpace.Descendants<C.StringReference>().ToList())
            {
                List<Zellwert> werte = Werte(r.Formula?.Text, zellen, namen);
                if (werte == null) continue;
                var speicher = Exceldiagrammschreiber.Textspeicher(werte.Select(w => w.Text).ToList());
                if (r.StringCache != null) r.StringCache.Remove();
                r.Formula.InsertAfterSelf(speicher);
            }
            c.ChartSpace.Save();
        }

        /// <summary>Der nachgezogene Bezug einer Reihe auf eine gewachsene Tabelle; <c>null</c> = unverändert.</summary>
        internal static string Wachse(string formel, IReadOnlyList<Tabellenwachstum> wachstum)
        {
            if (string.IsNullOrEmpty(formel) || wachstum == null || wachstum.Count == 0) return null;
            if (!Zerlege(formel, out string blatt, out int s1, out int z1, out int s2, out int z2)) return null;
            foreach (Tabellenwachstum w in wachstum)
            {
                if (w.Blattname == null || !string.Equals(w.Blattname, blatt, StringComparison.OrdinalIgnoreCase)) continue;
                if (s1 < w.Spalte1 || s2 > w.Spalte2) continue;
                if ((z1 == w.Kopfzeile + 1 || z1 == w.Kopfzeile) && z2 == w.AlteLetzte && w.NeueLetzte != w.AlteLetzte)
                    return Datenbereich.Bezug(blatt, s1, z1, s2, w.NeueLetzte);
            }
            return null;
        }

        /// <summary>Die Werte hinter einer Reihenformel — ein einfacher Bezug oder ein Name darauf; <c>null</c> = nicht lesbar.</summary>
        private static List<Zellwert> Werte(string formel, Zelllesung zellen, Dictionary<string, string> namen)
        {
            if (string.IsNullOrEmpty(formel)) return null;
            string bezug = formel;
            if (!Zerlege(bezug, out _, out _, out _, out _, out _))
            {
                // Ein Name: „Mappe!EPOS.reihe.x“, „[0]!EPOS.reihe.x“ oder „EPOS.reihe.x“.
                string name = formel.Contains('!') ? formel.Substring(formel.LastIndexOf('!') + 1) : formel;
                if (!namen.TryGetValue(name, out bezug)) return null;
                bezug = (bezug ?? "").TrimStart('=');
            }
            if (!Zerlege(bezug, out string blatt, out int s1, out int z1, out int s2, out int z2)) return null;
            if ((long)(s2 - s1 + 1) * (z2 - z1 + 1) > 1_048_576) return null;
            var liste = new List<Zellwert>();
            for (int z = z1; z <= z2; z++)
                for (int s = s1; s <= s2; s++)
                {
                    Zellwert w = zellen.Lies(blatt, s, z);
                    if (w == null) return null;             // kein Blatt dieses Namens
                    if (w.OffeneFormel) return null;        // Excel rechnet neu — der alte Speicher bleibt
                    liste.Add(w);
                }
            return liste;
        }

        internal static bool Zerlege(string bezug, out string blatt, out int s1, out int z1, out int s2, out int z2)
        {
            blatt = null;
            s1 = z1 = s2 = z2 = 0;
            Match m = BEZUG.Match(bezug ?? "");
            if (!m.Success) return false;
            blatt = m.Groups["q"].Success ? m.Groups["q"].Value.Replace("''", "'") : m.Groups["b"].Value;
            s1 = Spalte(m.Groups["s1"].Value);
            z1 = int.Parse(m.Groups["z1"].Value, CultureInfo.InvariantCulture);
            s2 = m.Groups["s2"].Success ? Spalte(m.Groups["s2"].Value) : s1;
            z2 = m.Groups["z2"].Success ? int.Parse(m.Groups["z2"].Value, CultureInfo.InvariantCulture) : z1;
            if (s2 < s1) (s1, s2) = (s2, s1);
            if (z2 < z1) (z1, z2) = (z2, z1);
            return true;
        }

        private static int Spalte(string buchstaben)
        {
            int n = 0;
            foreach (char c in buchstaben.ToUpperInvariant()) n = n * 26 + (c - 'A' + 1);
            return n;
        }

        /// <summary>Der Wert einer Zelle, wie ihn der Zwischenspeicher braucht.</summary>
        internal sealed class Zellwert
        {
            internal double? Zahl;
            internal string Text;
            internal bool OffeneFormel;
        }

        /// <summary>Liest Zellwerte aus den Blättern des Pakets (gemeinsame Zeichenketten, Inline-Text, Zahlen, Formeln).</summary>
        private sealed class Zelllesung
        {
            private readonly WorkbookPart _mappe;
            private readonly IReadOnlyDictionary<string, WorksheetPart> _teile;
            private readonly Dictionary<string, Dictionary<string, S.Cell>> _blaetter =
                new Dictionary<string, Dictionary<string, S.Cell>>(StringComparer.OrdinalIgnoreCase);
            private List<string> _texte;

            internal Zelllesung(WorkbookPart mappe, IReadOnlyDictionary<string, WorksheetPart> teile)
            {
                _mappe = mappe;
                _teile = teile;
            }

            internal Zellwert Lies(string blatt, int spalte, int zeile)
            {
                if (!_blaetter.TryGetValue(blatt, out Dictionary<string, S.Cell> zellen))
                {
                    if (!_teile.TryGetValue(blatt, out WorksheetPart t)) return null;
                    zellen = new Dictionary<string, S.Cell>(StringComparer.OrdinalIgnoreCase);
                    foreach (S.Cell c in t.Worksheet.Descendants<S.Cell>())
                        if (c.CellReference?.Value != null) zellen[c.CellReference.Value] = c;
                    _blaetter[blatt] = zellen;
                }
                string adresse = Datenbereich.Spaltenname(spalte) + zeile.ToString(CultureInfo.InvariantCulture);
                if (!zellen.TryGetValue(adresse, out S.Cell zelle)) return new Zellwert();
                string roh = zelle.CellValue?.Text;
                if (zelle.CellFormula != null && roh == null) return new Zellwert { OffeneFormel = true };

                S.CellValues typ = zelle.DataType?.Value ?? S.CellValues.Number;
                if (typ == S.CellValues.SharedString && int.TryParse(roh, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                    return new Zellwert { Text = Gemeinsam(i) };
                if (typ == S.CellValues.InlineString) return new Zellwert { Text = zelle.InlineString?.InnerText ?? "" };
                if (typ == S.CellValues.String || typ == S.CellValues.Error) return new Zellwert { Text = roh ?? "" };
                if (typ == S.CellValues.Boolean) return new Zellwert { Zahl = roh == "1" ? 1.0 : 0.0, Text = roh == "1" ? "TRUE" : "FALSE" };
                if (roh != null && double.TryParse(roh, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    return new Zellwert { Zahl = d, Text = d.ToString("R", CultureInfo.InvariantCulture) };
                return new Zellwert();
            }

            private string Gemeinsam(int i)
            {
                _texte ??= _mappe.SharedStringTablePart?.SharedStringTable?.Elements<S.SharedStringItem>()
                                .Select(x => x.InnerText).ToList() ?? new List<string>();
                return i >= 0 && i < _texte.Count ? _texte[i] : "";
            }
        }
    }
}
