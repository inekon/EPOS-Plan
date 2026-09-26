using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wie eine Excel-Tabelle der Vorlage gewachsen oder geschrumpft ist (Konzept Berichtsvorlagen 7.3, 7.4): das Blatt,
    /// die Spalten, die erste Datenzeile und die letzte vorher und nachher. Die Diagramme der Vorlage, deren Reihen genau auf
    /// die Datenzeilen zeigten, zieht der <see cref="Diagrammplan"/> über das SDK nach — ClosedXML verschiebt ihre Bezüge
    /// nicht (Messprobe 2 von BV-E0).
    /// </summary>
    internal sealed class Tabellenwachstum
    {
        internal Tabellenwachstum(IXLWorksheet blatt, int kopfzeile, int alteLetzte, int neueLetzte, int spalte1, int spalte2)
        {
            Blatt = blatt;
            Kopfzeile = kopfzeile;
            AlteLetzte = alteLetzte;
            NeueLetzte = neueLetzte;
            Spalte1 = spalte1;
            Spalte2 = spalte2;
        }

        internal IXLWorksheet Blatt { get; }

        /// <summary>Der Name des Blattes beim Speichern.</summary>
        internal string Blattname { get; set; }

        internal int Kopfzeile { get; }

        internal int AlteLetzte { get; }

        internal int NeueLetzte { get; }

        internal int Spalte1 { get; }

        internal int Spalte2 { get; }
    }

    /// <summary>
    /// <b>Listen und Bereiche in Excel</b> (Konzept Berichtsvorlagen 5.4, 7.3; Etappe BV-E8): eine <see cref="Berichtstabelle"/>
    /// als erzeugter Bereich an einer Zellmarke <c>{{tabelle.…}}</c> — eine listentaugliche als Excel-Tabelle
    /// <c>EPOS_&lt;name&gt;</c>, eine mit Stand-Spalten oder Gruppenzeilen als formatierter Bereich — und das Füllen einer
    /// Excel-Tabelle <c>EPOS_&lt;name&gt;</c> der Vorlage (Zeile für Zeile, sie wächst oder schrumpft). Zahlen bleiben
    /// Zahlen im Format der Tabelle, ein Leerwert „—“ wird eine leere Zelle (nie 0).
    /// </summary>
    internal static class Excelbereiche
    {
        /// <summary>Die Vorsilbe der Excel-Tabellen (Konzept 4.4, 5.5).</summary>
        internal const string PRAEFIX_TABELLE = "EPOS_";

        /// <summary>Wie viele Zeilen der Bereich einer Tabelle belegt: Kopf, Zeilen, Hinweise darunter.</summary>
        internal static int Zeilen(Berichtstabelle t)
        {
            if (t == null || t.IstLeer) return 1;
            return (t.Kopf != null ? 1 : 0) + t.Zeilen.Count + t.Hinweise.Count;
        }

        /// <summary>Der Name der Excel-Tabelle eines Schlüssels: <c>EPOS_tabelle__varianten</c>.</summary>
        internal static string Tabellenname(string schluessel)
        {
            return PRAEFIX_TABELLE + (schluessel ?? "").Replace(".", "__");
        }

        /// <summary>
        /// Schreibt die Tabelle ab <paramref name="zeile"/>/<paramref name="spalte"/> — die Zeilen darunter müssen frei sein
        /// (der Aufrufer fügt sie ein). Eine leere Tabelle schreibt ihren Grund. Eine listentaugliche wird eine Excel-Tabelle
        /// mit einem freien Namen <c>EPOS_&lt;name&gt;</c>; gibt deren Namen zurück, sonst <c>null</c>.
        /// </summary>
        internal static string Schreibe(IXLWorksheet ws, int zeile, int spalte, Berichtstabelle t, string schluessel, string leertext)
        {
            if (t == null || t.IstLeer)
            {
                ws.Cell(zeile, spalte).Value = leertext ?? "";
                return null;
            }

            int r = zeile;
            if (t.Kopf != null)
            {
                for (int j = 0; j < t.Kopf.Zellen.Count; j++)
                {
                    IXLCell c = ws.Cell(r, spalte + j);
                    c.Value = t.Kopf.Zellen[j].Text ?? "";
                    c.Style.Font.Bold = true;
                    c.Style.Fill.BackgroundColor = ExcelBerichtGenerator.KOPF;
                    c.Style.Alignment.WrapText = true;
                }
                r++;
            }
            int ersteDaten = r;
            foreach (Tabellenzeile z in t.Zeilen)
            {
                for (int j = 0; j < z.Zellen.Count; j++) Zelle(ws.Cell(r, spalte + j), z.Zellen[j], z.Rolle);
                r++;
            }
            int letzteDaten = r - 1;
            foreach (string h in t.Hinweise)
            {
                ws.Cell(r, spalte).Value = h;
                ws.Cell(r, spalte).Style.Font.Italic = true;
                r++;
            }

            if (!t.Listentauglich || t.Kopf == null || letzteDaten < ersteDaten) return null;
            string name = FreierTabellenname(ws.Workbook, Tabellenname(schluessel));
            IXLRange bereich = ws.Range(zeile, spalte, letzteDaten, spalte + t.Kopf.Zellen.Count - 1);
            IXLTable tabelle = bereich.CreateTable(name);
            tabelle.Theme = XLTableTheme.TableStyleLight9;
            return name;
        }

        /// <summary>
        /// Füllt die Excel-Tabelle <paramref name="tabelle"/> der Vorlage mit einer listentauglichen Tabelle: Kopf aus der
        /// Tabelle, eine Datenzeile je Zeile — die Tabelle wächst über <c>InsertRowsBelow</c> (Inhalte darunter wandern mit),
        /// schrumpft durch Leeren der übrigen Zeilen und passt ihre Spaltenzahl an. Gibt das Wachstum zurück.
        /// </summary>
        internal static Tabellenwachstum Fuelle(IXLTable tabelle, Berichtstabelle t)
        {
            IXLWorksheet ws = tabelle.Worksheet;
            IXLRangeAddress alt = tabelle.RangeAddress;
            int kopf = alt.FirstAddress.RowNumber, s1 = alt.FirstAddress.ColumnNumber;
            int alteLetzte = alt.LastAddress.RowNumber, alteSpalten = alt.LastAddress.ColumnNumber - s1 + 1;
            int alteZeilen = alteLetzte - kopf;
            int n = Math.Max(1, t.Zeilen.Count), m = t.Kopf.Zellen.Count;

            if (n > alteZeilen) tabelle.InsertRowsBelow(n - alteZeilen);
            else if (n < alteZeilen)
                ws.Range(kopf + n + 1, s1, alteLetzte, s1 + alteSpalten - 1).Clear(XLClearOptions.Contents);
            if (n != alteZeilen || m != alteSpalten) tabelle.Resize(kopf, s1, kopf + n, s1 + m - 1);

            for (int j = 0; j < m; j++) ws.Cell(kopf, s1 + j).Value = t.Kopf.Zellen[j].Text ?? "";
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    IXLCell c = ws.Cell(kopf + 1 + i, s1 + j);
                    if (i < t.Zeilen.Count && j < t.Zeilen[i].Zellen.Count) Zelle(c, t.Zeilen[i].Zellen[j], t.Zeilen[i].Rolle, false);
                    else c.Value = Blank.Value;
                }
            return new Tabellenwachstum(ws, kopf, alteLetzte, kopf + n, s1, s1 + m - 1);
        }

        /// <summary>Eine Zelle: Zahl mit Format (und Einheit), Leerwert leer, sonst Text; Rollen der Direktformatierung.</summary>
        private static void Zelle(IXLCell c, Tabellenzelle z, Tabellenrolle zeilenrolle, bool formatiert = true)
        {
            if (z.Zahl.HasValue && double.IsFinite(z.Zahl.Value))
            {
                c.Value = z.Zahl.Value;
                string format = Exceldiagrammquellen.Zahlformat(z.Format);
                if (!string.IsNullOrWhiteSpace(z.Einheit)) format += " \"" + z.Einheit.Replace("\"", "") + "\"";
                c.Style.NumberFormat.Format = format;
            }
            else if (z.IstLeer) c.Value = Blank.Value;
            else c.Value = z.Text ?? "";

            if (!formatiert) return;
            if (z.Fett || (zeilenrolle & (Tabellenrolle.Gruppe | Tabellenrolle.Summe)) != 0) c.Style.Font.Bold = true;
            if ((zeilenrolle & Tabellenrolle.Gruppe) != 0) c.Style.Fill.BackgroundColor = ExcelBerichtGenerator.GRUPPE;
            else if (z.Hinterlegung == Tabellenhinterlegung.Stamm) c.Style.Fill.BackgroundColor = ExcelBerichtGenerator.STAMM;
            else if (z.Hinterlegung == Tabellenhinterlegung.Kopf) c.Style.Fill.BackgroundColor = ExcelBerichtGenerator.KOPF;
            if (z.Ausrichtung == Tabellenausrichtung.Rechts) c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            else if (z.Ausrichtung == Tabellenausrichtung.Mitte) c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        /// <summary>Ein Tabellenname, den die Mappe noch nicht trägt (Tabellen und Namen teilen einen Namensraum).</summary>
        private static string FreierTabellenname(IXLWorkbook wb, string basis)
        {
            var belegt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (IXLWorksheet w in wb.Worksheets)
                foreach (IXLTable t in w.Tables) belegt.Add(t.Name);
            foreach (IXLDefinedName d in wb.DefinedNames) belegt.Add(d.Name);
            string name = basis;
            for (int i = 2; belegt.Contains(name); i++) name = basis + "_" + i.ToString(CultureInfo.InvariantCulture);
            return name;
        }
    }
}
