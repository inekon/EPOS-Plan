using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using S = DocumentFormat.OpenXml.Spreadsheet;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Plan der Excel-Diagramme einer Mappe</b> (Konzept Berichtsvorlagen 7.2–7.4, Entscheid BV-Q11; Etappe BV-E8).
    /// Er entsteht in zwei Schritten, weil ClosedXML keine Diagramme anlegen kann (Messprobe 6 von BV-E0):
    /// <list type="number">
    /// <item><b>Mit ClosedXML</b>, während die Blätter entstehen: Jedes Diagramm bekommt einen Datenbereich im Blatt
    /// <see cref="BLATTNAME"/> (Blöcke nebeneinander: Titel, Bezug, Kopfzeile, Zahlen; eine leere Spalte dazwischen) und
    /// einen Platz — auf einem erzeugten Blatt rechts neben dessen Inhalt untereinander, in einer Vorlage an der Zelle des
    /// Bildplatzhalters. <see cref="SchreibeDatenblatt"/> schreibt das Blatt, wenn es wenigstens ein Diagramm gibt.</item>
    /// <item><b>Mit dem OpenXML SDK</b> auf dem gespeicherten Paket (<see cref="Anlegen"/>), nach dem Nachtrag der
    /// Formelergebnisse: je Platz ein natives Diagramm (<see cref="Exceldiagrammschreiber"/>) mit Reihenbezügen auf die
    /// Zellen des Datenbereichs.</item>
    /// </list>
    /// Gleiche Diagramme teilen sich einen Datenbereich (derselbe Schlüssel für denselben Stand). Die bestehenden Blätter
    /// bleiben Zelle für Zelle, wie sie sind — die Diagramme stehen in ihrer Zeichnungsebene.
    /// </summary>
    internal sealed class Diagrammplan
    {
        /// <summary>Der Name des Datenblatts — fest wie „Übersicht“ und „Vergleich“, damit Bezüge darauf in jeder Sprache halten.</summary>
        internal const string BLATTNAME = "Diagrammdaten";

        /// <summary>Zeile des Titels, des Bezugs, der Spaltenköpfe und die erste Datenzeile eines Blocks.</summary>
        private const int ZEILE_TITEL = 1, ZEILE_BEZUG = 2, ZEILE_KOPF = 3, ZEILE_DATEN = 4;

        /// <summary>Der Abstand zweier Diagramme untereinander in Zeilen.</summary>
        private const int ZEILENABSTAND = 2;

        private readonly List<Block> _bloecke = new List<Block>();
        private readonly Dictionary<string, Block> _nachSchluessel = new Dictionary<string, Block>(StringComparer.Ordinal);
        private readonly List<Platz> _plaetze = new List<Platz>();
        private readonly Dictionary<IXLWorksheet, int> _naechsteZeile = new Dictionary<IXLWorksheet, int>();
        private readonly Dictionary<IXLWorksheet, int> _spalte = new Dictionary<IXLWorksheet, int>();
        private int _naechsteSpalte = 1;
        private IXLWorksheet _datenblatt;

        internal Diagrammplan(BerichtsDaten daten, bool englisch, WirtschaftsBerichtswerte wirtschaft = null)
        {
            Kontext = new Diagrammkontext(daten, englisch, wirtschaft);
        }

        /// <summary>Was die Quellen lesen.</summary>
        internal Diagrammkontext Kontext { get; }

        /// <summary>Die geplanten Diagramme in Planfolge.</summary>
        internal IReadOnlyList<Platz> Plaetze { get { return _plaetze; } }

        /// <summary>Gibt es ein Diagramm oder einen Datenbereich?</summary>
        internal bool Leer { get { return _bloecke.Count == 0; } }

        /// <summary>Das geschriebene Datenblatt; <c>null</c> vor <see cref="SchreibeDatenblatt"/> oder ohne Diagramm.</summary>
        internal IXLWorksheet Datenblatt { get { return _datenblatt; } }

        // =====================================================================
        //  Planen
        // =====================================================================

        /// <summary>
        /// Plant das Diagramm des Bildschlüssels auf dem erzeugten Blatt <paramref name="ziel"/> — rechts neben seinem Inhalt,
        /// unter dem vorigen Diagramm desselben Blattes. <c>false</c>, wenn es kein Diagramm gibt (kein Modell, keine Reihen).
        /// </summary>
        internal bool AufBlatt(IXLWorksheet ziel, string schluessel, VariantenDaten stand = null)
        {
            Block block = Datenbereich(schluessel, stand);
            if (block == null) return false;
            if (!_spalte.TryGetValue(ziel, out int spalte))
            {
                // Nullbasiert: die Spalte hinter einer leeren Spalte rechts des Inhalts, ab der zweiten Zeile.
                spalte = (ziel.LastColumnUsed()?.ColumnNumber() ?? 0) + 1;
                _spalte[ziel] = spalte;
                _naechsteZeile[ziel] = 1;
            }
            int zeile = _naechsteZeile[ziel];
            _naechsteZeile[ziel] = zeile + Diagrammanker.ZEILEN + ZEILENABSTAND;
            _plaetze.Add(new Platz(block, ziel, new Diagrammanker(spalte, zeile, Diagrammanker.SPALTEN, Diagrammanker.ZEILEN)));
            return true;
        }

        /// <summary>
        /// Plant das Diagramm an einer Zelle (Bildplatzhalter einer Vorlage): links oben an der Zelle, Vorgabegröße.
        /// <c>false</c>, wenn es kein Diagramm gibt. Die Zelle trägt bis <see cref="Festhalten"/> eine Marke — erzeugte Bereiche
        /// darüber verschieben sie noch, und erst dann steht ihr Platz fest.
        /// </summary>
        internal bool AnZelle(IXLCell zelle, string schluessel, VariantenDaten stand = null)
        {
            Block block = Datenbereich(schluessel, stand);
            if (block == null) return false;
            string marke = MARKE + (++_marken).ToString(CultureInfo.InvariantCulture) + "\u2060";
            zelle.Value = marke;
            _plaetze.Add(new Platz(block, zelle.Worksheet, default) { Marke = marke });
            return true;
        }

        /// <summary>Die Marke einer Diagrammzelle bis zum Festhalten (Wortverbinder, damit kein Anwendertext so heißt).</summary>
        private const string MARKE = "\u2060EPOS-Diagramm-";

        private int _marken;

        private readonly List<Tabellenwachstum> _wachstum = new List<Tabellenwachstum>();

        /// <summary>Merkt eine gewachsene Excel-Tabelle der Vorlage vor (die Diagramme darauf zieht <see cref="Anlegen"/> nach).</summary>
        internal void Gewachsen(Tabellenwachstum w)
        {
            if (w != null) _wachstum.Add(w);
        }

        /// <summary>
        /// Trägt die Mappe Diagramme der Vorlage? Dann zieht <see cref="Anlegen"/> ihre Bezüge auf gewachsene Tabellen nach und
        /// trägt ihre Zwischenspeicher aus den gefüllten Zellen neu ein.
        /// </summary>
        internal bool VorlageNachfuehren { get; set; }

        /// <summary>
        /// Der Datenbereich des Diagramms (einmal je Schlüssel und Stand); <c>null</c>, wenn es kein Diagramm gibt. Die Spalten
        /// stehen fest, sobald der Block geplant ist — auch bevor das Blatt geschrieben ist.
        /// </summary>
        internal Block Datenbereich(string schluessel, VariantenDaten stand = null)
        {
            string kennung = schluessel + "|" + (Exceldiagrammquellen.JeStand(schluessel) && stand != null
                ? stand.IdProjekt.ToString(CultureInfo.InvariantCulture) : "");
            if (_nachSchluessel.TryGetValue(kennung, out Block da)) return da;
            Exceldiagramm d = Exceldiagrammquellen.Baue(schluessel, Kontext, stand);
            if (d == null) return null;
            return Neu(kennung, d);
        }

        /// <summary>Ein Datenbereich ohne Diagramm (Reihen für Namen <c>EPOS.reihe.*</c>).</summary>
        internal Block Datenbereich(string kennung, Exceldiagramm d)
        {
            if (_nachSchluessel.TryGetValue(kennung, out Block da)) return da;
            return d == null ? null : Neu(kennung, d);
        }

        private Block Neu(string kennung, Exceldiagramm d)
        {
            var bereich = new Datenbereich(BLATTNAME, ZEILE_KOPF, ZEILE_DATEN, Math.Max(1, d.Zeilen), _naechsteSpalte);
            int spalte = _naechsteSpalte + 1;
            foreach (Excelreihe r in d.Spaltenfolge()) bereich.Spalten[r] = spalte++;
            _naechsteSpalte = spalte + 1;          // eine leere Spalte bis zum nächsten Block
            var block = new Block(d, bereich);
            _bloecke.Add(block);
            _nachSchluessel[kennung] = block;
            return block;
        }

        // =====================================================================
        //  Das Datenblatt (ClosedXML)
        // =====================================================================

        /// <summary>
        /// Schreibt das Blatt <see cref="BLATTNAME"/> hinten an die Mappe — nur, wenn es einen Datenbereich gibt. Eine Zelle
        /// je Wert: Zahlen als Zahlen im Format des Diagramms, eine Lücke als leere Zelle (nie 0). Gibt das Blatt zurück.
        /// </summary>
        internal IXLWorksheet SchreibeDatenblatt(XLWorkbook wb)
        {
            if (_bloecke.Count == 0) return null;
            IXLWorksheet ws = wb.Worksheets.Add(FreierName(wb));
            foreach (Block b in _bloecke) b.Bereich.Blatt = ws.Name;

            foreach (Block b in _bloecke) Schreibe(ws, b);
            ws.SheetView.FreezeRows(ZEILE_KOPF);
            _datenblatt = ws;
            return ws;
        }

        private static string FreierName(XLWorkbook wb)
        {
            string name = BLATTNAME;
            int n = 2;
            while (wb.Worksheets.Any(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)))
                name = BLATTNAME + " (" + n++.ToString(CultureInfo.InvariantCulture) + ")";
            return name;
        }

        private void Schreibe(IXLWorksheet ws, Block b)
        {
            Exceldiagramm d = b.Diagramm;
            Datenbereich x = b.Bereich;
            int k = x.Kategorienspalte;

            ws.Cell(ZEILE_TITEL, k).Value = d.Titel;
            ws.Cell(ZEILE_TITEL, k).Style.Font.Bold = true;
            if (!string.IsNullOrEmpty(d.Bezug))
            {
                ws.Cell(ZEILE_BEZUG, k).Value = d.Bezug;
                ws.Cell(ZEILE_BEZUG, k).Style.Font.Italic = true;
            }

            IXLCell kopf = ws.Cell(ZEILE_KOPF, k);
            kopf.Value = d.Kategorienkopf;
            foreach (KeyValuePair<Excelreihe, int> s in x.Spalten)
                ws.Cell(ZEILE_KOPF, s.Value).Value = s.Key.Name;
            IXLRange kopfzeile = ws.Range(ZEILE_KOPF, k, ZEILE_KOPF, x.LetzteSpalte);
            kopfzeile.Style.Font.Bold = true;
            kopfzeile.Style.Fill.BackgroundColor = ExcelBerichtGenerator.KOPF;
            kopfzeile.Style.Alignment.WrapText = true;
            foreach (KeyValuePair<Excelreihe, int> s in x.Spalten.Where(p => p.Key.Hilfsreihe))
                ws.Cell(ZEILE_KOPF, s.Value).Style.Fill.BackgroundColor = ExcelBerichtGenerator.GRUPPE;

            for (int i = 0; i < x.Zeilen; i++)
            {
                if (i >= d.Kategorien.Count) break;
                object kat = d.Kategorien[i];
                if (kat is double zahl) ws.Cell(ZEILE_DATEN + i, k).Value = zahl;
                else ws.Cell(ZEILE_DATEN + i, k).Value = Convert.ToString(kat, CultureInfo.InvariantCulture) ?? "";
            }
            foreach (KeyValuePair<Excelreihe, int> s in x.Spalten)
            {
                double?[] werte = s.Key.Werte;
                for (int i = 0; i < werte.Length && i < x.Zeilen; i++)
                    if (werte[i].HasValue) ws.Cell(ZEILE_DATEN + i, s.Value).Value = werte[i].Value;
                ws.Range(ZEILE_DATEN, s.Value, ZEILE_DATEN + x.Zeilen - 1, s.Value).Style.NumberFormat.Format = d.Zahlformat;
                ws.Column(s.Value).Width = 14;
            }
            ws.Column(k).Width = d.Kategorien.All(c => c is double) ? 10 : 22;
        }

        // =====================================================================
        //  Die Diagramme (OpenXML SDK)
        // =====================================================================

        /// <summary>
        /// Legt die geplanten Diagramme in der gespeicherten Mappe an. Die Namen der Blätter liest es vorher aus der geladenen
        /// Mappe (<see cref="Festhalten"/>), weil sie sich bis zum Speichern noch ändern können (Blattmarken).
        /// </summary>
        internal void Anlegen(string datei)
        {
            if (_plaetze.Count == 0 && !VorlageNachfuehren) return;
            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(datei, true))
            {
                WorkbookPart mappe = doc.WorkbookPart;
                var teile = new Dictionary<string, WorksheetPart>(StringComparer.OrdinalIgnoreCase);
                foreach (S.Sheet s in mappe.Workbook.Descendants<S.Sheet>())
                    if (s.Name?.Value != null && s.Id?.Value != null && mappe.GetPartById(s.Id.Value) is WorksheetPart t)
                        teile[s.Name.Value] = t;

                // Zuerst die Diagramme der Vorlage (die eigenen tragen schon die richtigen Bezüge und Zahlen).
                if (VorlageNachfuehren) Vorlagendiagramme.Nachfuehren(mappe, teile, _wachstum);

                foreach (Platz p in _plaetze)
                {
                    if (p.Blattname == null || !teile.TryGetValue(p.Blattname, out WorksheetPart ziel)) continue;
                        if (_datenblatt == null) continue;
                    p.Block.Bereich.Blatt = _datenblattname ?? p.Block.Bereich.Blatt;
                    Exceldiagrammschreiber.Setze(ziel, p.Anker, p.Block.Diagramm, p.Block.Bereich, Kontext.Englisch);
                }
            }
        }

        private string _datenblattname;

        /// <summary>Hält die Namen der Blätter fest — unmittelbar vor dem Speichern mit ClosedXML.</summary>
        internal void Festhalten()
        {
            _datenblattname = _datenblatt?.Name;
            foreach (Platz p in _plaetze)
            {
                try
                {
                    p.Blattname = p.Ziel.Name;
                    if (p.Marke != null) p.Anker = Markenanker(p);
                }
                catch (Exception) { p.Blattname = null; }       // ein entferntes Blatt
            }
            foreach (Tabellenwachstum w in _wachstum)
            {
                try { w.Blattname = w.Blatt.Name; }
                catch (Exception) { w.Blattname = null; }
            }
        }

        /// <summary>Der Platz einer Diagrammzelle: die Zelle mit der Marke; sie wird geleert. Ohne Zelle entfällt das Diagramm.</summary>
        private static Diagrammanker Markenanker(Platz p)
        {
            IXLCell zelle = p.Ziel.CellsUsed(XLCellsUsedOptions.Contents)
                             .FirstOrDefault(c => c.Value.IsText && c.Value.GetText() == p.Marke);
            if (zelle == null)
            {
                p.Blattname = null;
                return default;
            }
            zelle.Value = Blank.Value;
            return new Diagrammanker(zelle.Address.ColumnNumber - 1, zelle.Address.RowNumber - 1,
                                     Diagrammanker.SPALTEN, Diagrammanker.ZEILEN);
        }

        // =====================================================================
        //  Typen
        // =====================================================================

        /// <summary>Ein Diagramm mit seinem Datenbereich.</summary>
        internal sealed class Block
        {
            internal Block(Exceldiagramm diagramm, Datenbereich bereich)
            {
                Diagramm = diagramm;
                Bereich = bereich;
            }

            internal Exceldiagramm Diagramm { get; }

            internal Datenbereich Bereich { get; }
        }

        /// <summary>Ein geplantes Diagramm: Block, Zielblatt und Anker.</summary>
        internal sealed class Platz
        {
            internal Platz(Block block, IXLWorksheet ziel, Diagrammanker anker)
            {
                Block = block;
                Ziel = ziel;
                Anker = anker;
            }

            internal Block Block { get; }

            internal IXLWorksheet Ziel { get; }

            internal Diagrammanker Anker { get; set; }

            /// <summary>Die Marke der Diagrammzelle einer Vorlage bis zum Festhalten; <c>null</c> auf einem erzeugten Blatt.</summary>
            internal string Marke { get; set; }

            /// <summary>Der Name des Zielblatts beim Speichern (<see cref="Festhalten"/>).</summary>
            internal string Blattname { get; set; }
        }
    }
}
