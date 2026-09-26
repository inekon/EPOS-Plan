using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Excel-Baukasten</b> (Konzept Berichtsvorlagen 6.3 Nr. 3, 7; Etappe BV-E9) — eine Excel-Mappe, die der Kern <b>aus dem
    /// Katalog erzeugt</b>, das Gegenstück zu <see cref="WordBaukasten"/>: jeder Eintrag mit Ausgabe Excel und
    /// <see cref="Vorlagenfeld.Seit"/> ≤ Fassung, der in einer Excel-Vorlage stehen darf (<see cref="ExcelVorlagenmappe.Beurteile"/>),
    /// genau einmal, gegliedert nach Kontext:
    /// <list type="bullet">
    /// <item>Blatt „Baukasten“: Bericht, Installation, Stamm und Gruppe — je Eintrag Beschreibung, Schlüssel (grau, ohne Klammern)
    /// und der Zellplatzhalter allein in seiner Zelle (typisierter Wert);</item>
    /// <item>Blatt „Tabellen“: die Tabellen als Zellmarke — gefüllt ein erzeugter Bereich, listentauglich eine Excel-Tabelle
    /// <c>EPOS_&lt;name&gt;</c>;</item>
    /// <item>Blatt „Diagramme“: die Bilder als Zellmarke, je im Abstand eines Diagramms;</item>
    /// <item>Blatt „Namen“: die Reihennamen <c>EPOS.reihe.*</c> und ein Name auf eine Zelle;</item>
    /// <item>die Blattmarken aller erzeugten Blätter, <c>blatt.detail</c> als Musterblatt mit allen Einträgen je Stand.</item>
    /// </list>
    /// Nicht in Excel: Kapitel, Schalter und Blöcke (Excel kennt keine Blocksyntax), Werte je Gebäude, der Paarvergleich und die
    /// Mustertabelle. Gefüllt ergibt der Baukasten eine Probemappe mit jedem Wert, ohne Prüferfehler und ohne übrige
    /// Platzhalter. Plattformfrei, ohne Datenbank; zweimal erzeugt ist der Inhalt gleich (<c>BerichtsvorlagenCtrl.Inhaltsschluessel</c>).
    /// </summary>
    public static class ExcelBaukasten
    {
        /// <summary>Die Art des Baukastens in <c>EPOS.Vorlage</c>.</summary>
        public const string VORLAGENART = "baukasten-excel";

        /// <summary>Der Abstand zweier Diagrammplatzhalter in Zeilen (ein Diagramm ist 20 Zeilen hoch).</summary>
        internal const int DIAGRAMMABSTAND = Diagrammanker.ZEILEN + 2;

        /// <summary>Die Folge der Arten auf dem Blatt „Baukasten“.</summary>
        private static readonly Vorlagenfeldart[] Wertarten =
        {
            Vorlagenfeldart.Text, Vorlagenfeldart.Zahl, Vorlagenfeldart.Datum, Vorlagenfeldart.Liste,
        };

        /// <summary>
        /// Die Einträge des Excel-Baukastens der Fassung <paramref name="fassung"/>: Ausgabe Excel, in einer Excel-Vorlage
        /// füllbar — auf dem Musterblatt die Einträge je Stand —, dazu die Blattmarken; ohne Paarvergleich.
        /// </summary>
        public static IReadOnlyList<Vorlagenfeld> Eintraege(int fassung = Vorlagenfeldkatalog.KATALOGFASSUNG)
        {
            return Vorlagenfeldkatalog.Alle.Where(f => f != null && f.Seit <= fassung && Passt(f)).ToList();
        }

        /// <summary>Steht der Eintrag im Excel-Baukasten?</summary>
        internal static bool Passt(Vorlagenfeld f)
        {
            if ((f.Ausgaben & Vorlagenausgabe.Excel) == 0) return false;
            if (Vorlagenpruefer.IstPaarschluessel(f.Schluessel)) return false;
            if (f.Art == Vorlagenfeldart.Blatt) return true;
            Platzhalter p = Platzhaltersyntax.Lies(f.Schluessel);
            return ExcelVorlagenmappe.Beurteile(p, f, f.Kontext == Vorlagenfeldkontext.Stand, true) == Excelstelle.Gut;
        }

        /// <summary>
        /// <b>Erzeugt den Excel-Baukasten</b> der Katalogfassung <paramref name="fassung"/> auf Deutsch oder Englisch — die Bytes
        /// einer <c>.xlsx</c> mit <c>EPOS.Katalogfassung</c>, <c>EPOS.Vorlage</c> = <see cref="VORLAGENART"/> und
        /// <c>EPOS.Sprache</c> in <c>custom.xml</c>.
        /// </summary>
        public static byte[] Erzeuge(bool englisch, int fassung = Vorlagenfeldkatalog.KATALOGFASSUNG)
        {
            if (fassung < 1) throw new ArgumentOutOfRangeException(nameof(fassung), fassung, "Katalogfassung ab 1");
            IReadOnlyList<Vorlagenfeld> alle = Eintraege(fassung);
            var m = new Excelmappenbau(englisch);

            List<Vorlagenfeld> werte = alle.Where(f => f.Kontext != Vorlagenfeldkontext.Stand && Array.IndexOf(Wertarten, f.Art) >= 0).ToList();
            List<Vorlagenfeld> tabellen = alle.Where(f => f.Kontext != Vorlagenfeldkontext.Stand && f.Art == Vorlagenfeldart.Tabelle).ToList();
            List<Vorlagenfeld> bilder = alle.Where(f => f.Kontext != Vorlagenfeldkontext.Stand && f.Art == Vorlagenfeldart.Bild).ToList();
            List<Vorlagenfeld> stand = alle.Where(f => f.Kontext == Vorlagenfeldkontext.Stand).ToList();
            List<Vorlagenfeld> blaetter = alle.Where(f => f.Art == Vorlagenfeldart.Blatt).ToList();

            using (var wb = new XLWorkbook())
            {
                // ---- Blatt „Baukasten“: die Werte nach Kontext ----
                IXLWorksheet ws = m.Blatt(wb, nameof(R.VF_XL_BAUKASTEN_BLATT));
                m.Titel(ws.Cell(1, 1), m.T(nameof(R.VF_XL_BAUKASTEN_TITEL)));
                m.Hinweis(ws.Cell(2, 1), m.T(nameof(R.VF_XL_BAUKASTEN_EINLEITUNG), fassung, alle.Count));
                int r = 4;
                foreach (IGrouping<Vorlagenfeldkontext, Vorlagenfeld> kontext in werte.GroupBy(f => f.Kontext).OrderBy(g => Kontextfolge(g.Key)))
                {
                    m.Abschnitt(ws, r++, m.Kontexttitel(kontext.Key));
                    m.Kopf(ws, r++);
                    foreach (Vorlagenfeld f in kontext.OrderBy(f => Array.IndexOf(Wertarten, f.Art)))
                        m.Eintrag(ws, r++, f);
                    r++;
                }
                ws.Column(1).Width = 70;
                ws.Column(2).Width = 42;
                ws.Column(3).Width = 36;

                // ---- Blatt „Tabellen“ ----
                IXLWorksheet wt = m.Blatt(wb, nameof(R.VF_XL_BAUKASTEN_BLATT_TABELLEN));
                m.Titel(wt.Cell(1, 1), m.T(nameof(R.VF_XL_BAUKASTEN_BLATT_TABELLEN)));
                m.Hinweis(wt.Cell(2, 1), m.T(nameof(R.VF_XL_BAUKASTEN_HINWEIS_TABELLEN)));
                r = 4;
                foreach (Vorlagenfeld f in tabellen)
                {
                    m.Beschreibung(wt, r++, f);
                    wt.Cell(r++, 1).Value = Marke(f);
                    r++;
                }
                wt.Column(1).Width = 40;

                // ---- Blatt „Diagramme“ ----
                IXLWorksheet wd = m.Blatt(wb, nameof(R.VF_XL_BAUKASTEN_BLATT_DIAGRAMME));
                m.Titel(wd.Cell(1, 1), m.T(nameof(R.VF_XL_BAUKASTEN_BLATT_DIAGRAMME)));
                m.Hinweis(wd.Cell(2, 1), m.T(nameof(R.VF_XL_BAUKASTEN_HINWEIS_DIAGRAMME)));
                r = 4;
                foreach (Vorlagenfeld f in bilder)
                {
                    m.Beschreibung(wd, r, f);
                    wd.Cell(r + 1, 1).Value = Marke(f);
                    r += DIAGRAMMABSTAND + 1;
                }

                // ---- Blatt „Namen“: Reihennamen und ein Name auf eine Zelle ----
                IXLWorksheet wn = m.Blatt(wb, nameof(R.VF_XL_BAUKASTEN_BLATT_NAMEN));
                m.Titel(wn.Cell(1, 1), m.T(nameof(R.VF_XL_BAUKASTEN_BLATT_NAMEN)));
                m.Hinweis(wn.Cell(2, 1), m.T(nameof(R.VF_XL_BAUKASTEN_HINWEIS_NAMEN)));
                r = 4;
                foreach (string name in Reihennamen())
                {
                    wn.Cell(r, 1).Value = name;
                    wn.Cell(r, 2).Value = m.T(nameof(R.VF_XL_BAUKASTEN_REIHE));
                    wb.DefinedNames.Add(name, wn.Range(r, 3, r, 3));
                    r++;
                }
                r++;
                Vorlagenfeld projektname = Vorlagenfeldkatalog.Finde("projekt.name");
                if (projektname != null && projektname.Seit <= fassung)
                {
                    string name = Vorlagenfeldkatalog.ExcelName(projektname.Schluessel);
                    wn.Cell(r, 1).Value = name;
                    wn.Cell(r, 2).Value = m.T(nameof(R.VF_XL_BAUKASTEN_NAME_ZELLE));
                    wb.DefinedNames.Add(name, wn.Range(r, 3, r, 3));
                }
                wn.Column(1).Width = 44;
                wn.Column(2).Width = 60;

                // ---- Blattmarken, blatt.detail als Musterblatt ----
                foreach (Vorlagenfeld b in blaetter)
                {
                    IXLWorksheet mb = wb.Worksheets.Add(b.Schluessel);
                    mb.Cell(1, 1).Value = Marke(b);
                    if (b.Schluessel != "blatt.detail") continue;

                    mb.Cell(2, 1).Value = m.T(nameof(R.VF_XL_BAUKASTEN_HINWEIS_MUSTER));
                    mb.Cell(2, 1).Style.Font.Italic = true;
                    r = 4;
                    List<Vorlagenfeld> standwerte = stand.Where(f => Array.IndexOf(Wertarten, f.Art) >= 0)
                                                         .OrderBy(f => Array.IndexOf(Wertarten, f.Art)).ToList();
                    m.Kopf(mb, r++);
                    foreach (Vorlagenfeld f in standwerte) m.Eintrag(mb, r++, f);
                    r++;
                    foreach (Vorlagenfeld f in stand.Where(f => f.Art == Vorlagenfeldart.Tabelle))
                    {
                        m.Beschreibung(mb, r++, f);
                        mb.Cell(r++, 1).Value = Marke(f);
                        r++;
                    }
                    foreach (Vorlagenfeld f in stand.Where(f => f.Art == Vorlagenfeldart.Bild))
                    {
                        m.Beschreibung(mb, r, f);
                        mb.Cell(r + 1, 1).Value = Marke(f);
                        r += DIAGRAMMABSTAND + 1;
                    }
                    mb.Column(1).Width = 70;
                    mb.Column(2).Width = 42;
                    mb.Column(3).Width = 36;
                }

                m.Eigenschaften(wb, fassung, VORLAGENART);
                return m.Speichere(wb);
            }
        }

        /// <summary>Die Reihennamen des Baukastens: die drei Achsen, je Reihe die Monatssummen, dazu Tage und Stunden des Wärmebedarfs.</summary>
        internal static IEnumerable<string> Reihennamen()
        {
            const string P = Vorlagenfeldkatalog.PRAEFIX_EXCEL + Excelreihen.PRAEFIX;
            yield return P + "monate";
            yield return P + "tage";
            yield return P + "stunden";
            foreach (string reihe in Excelreihen.Reihen) yield return P + reihe.ToLowerInvariant() + ".monate";
            yield return P + ZeitreihenSatz.WAERMEBEDARF.ToLowerInvariant() + ".tage";
            yield return P + ZeitreihenSatz.WAERMEBEDARF.ToLowerInvariant();
        }

        internal static string Marke(Vorlagenfeld f) { return "{{" + f.Schluessel + "}}"; }

        private static int Kontextfolge(Vorlagenfeldkontext k)
        {
            switch (k)
            {
                case Vorlagenfeldkontext.Bericht: return 0;
                case Vorlagenfeldkontext.Installation: return 1;
                case Vorlagenfeldkontext.Stamm: return 2;
                case Vorlagenfeldkontext.Gruppe: return 3;
                default: return 4;
            }
        }
    }

    /// <summary>
    /// Der Bau einer mitgelieferten Excel-Mappe (Baukasten, ausführliche Vorlage): Blätter, Beschriftungen, Notizen in der
    /// Sprache der Mappe, Eigenschaften in <c>custom.xml</c> und das Speichern mit festen Zeitstempeln.
    /// </summary>
    internal sealed class Excelmappenbau
    {
        /// <summary>Der feste Zeitstempel der Pakete — so schreibt jeder Lauf dieselben Bytes für ClosedXML-Teile.</summary>
        internal static readonly DateTime ZEITSTEMPEL = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly XLColor GRAU = XLColor.FromHtml("#7F7F7F");

        internal Excelmappenbau(bool englisch)
        {
            Englisch = englisch;
            Kultur = BerichtTexte.KulturFuer(englisch);
        }

        internal bool Englisch { get; }

        internal CultureInfo Kultur { get; }

        internal string T(string schluessel, params object[] argumente)
        {
            return ExcelVorlagentexte.T(Englisch, schluessel, argumente);
        }

        /// <summary>Ein neues Blatt mit dem Namen aus der Ressource.</summary>
        internal IXLWorksheet Blatt(XLWorkbook wb, string ressource)
        {
            string name = T(ressource);
            if (name.Length > 31) name = name.Substring(0, 31);
            return wb.Worksheets.Add(name);
        }

        internal void Titel(IXLCell c, string text)
        {
            c.Value = text;
            c.Style.Font.Bold = true;
            c.Style.Font.FontSize = 14;
        }

        internal void Hinweis(IXLCell c, string text)
        {
            c.Value = text;
            c.Style.Font.Italic = true;
            c.Style.Font.FontColor = GRAU;
        }

        internal void Abschnitt(IXLWorksheet ws, int r, string text)
        {
            ws.Cell(r, 1).Value = text;
            ws.Range(r, 1, r, 3).Style.Font.Bold = true;
            ws.Range(r, 1, r, 3).Style.Fill.BackgroundColor = ExcelBerichtGenerator.GRUPPE;
        }

        /// <summary>Die Kopfzeile Beschreibung · Schlüssel · Platzhalter.</summary>
        internal void Kopf(IXLWorksheet ws, int r)
        {
            ws.Cell(r, 1).Value = T(nameof(R.VF_XL_BAUKASTEN_SP_BESCHREIBUNG));
            ws.Cell(r, 2).Value = T(nameof(R.VF_XL_BAUKASTEN_SP_SCHLUESSEL));
            ws.Cell(r, 3).Value = T(nameof(R.VF_XL_BAUKASTEN_SP_PLATZHALTER));
            ws.Range(r, 1, r, 3).Style.Font.Bold = true;
            ws.Range(r, 1, r, 3).Style.Fill.BackgroundColor = ExcelBerichtGenerator.KOPF;
        }

        /// <summary>Ein Eintrag: Beschreibung, Schlüssel (grau, ohne Klammern) und der Platzhalter allein in Spalte C.</summary>
        internal void Eintrag(IXLWorksheet ws, int r, Vorlagenfeld f)
        {
            ws.Cell(r, 1).Value = WordBaukasten.Entschaerft(Vorlagenfeldkatalog.Beschreibung(f, Englisch));
            ws.Cell(r, 1).Style.Alignment.WrapText = true;
            ws.Cell(r, 2).Value = f.Schluessel;
            ws.Cell(r, 2).Style.Font.FontColor = GRAU;
            ws.Cell(r, 3).Value = ExcelBaukasten.Marke(f);
        }

        /// <summary>Die Beschreibungszeile über einer Tabelle oder einem Diagramm: Beschreibung und Schlüssel.</summary>
        internal void Beschreibung(IXLWorksheet ws, int r, Vorlagenfeld f)
        {
            ws.Cell(r, 1).Value = WordBaukasten.Entschaerft(Vorlagenfeldkatalog.Beschreibung(f, Englisch)) + " · " + f.Schluessel;
            ws.Cell(r, 1).Style.Font.Italic = true;
            ws.Cell(r, 1).Style.Font.FontColor = GRAU;
        }

        internal string Kontexttitel(Vorlagenfeldkontext k)
        {
            string t = T("VF_KATALOG_KONTEXT_" + k.ToString().ToUpperInvariant());
            return t.Length > 0 && t != "VF_KATALOG_KONTEXT_" + k.ToString().ToUpperInvariant()
                ? char.ToUpper(t[0], Kultur) + t.Substring(1)
                : k.ToString();
        }

        /// <summary>Eine Notiz an der Zelle — nie mit doppelten Klammern, damit gefüllt kein Platzhalter übrig bleibt.</summary>
        internal void Notiz(IXLCell c, string ressource, params object[] argumente)
        {
            string text = WordBaukasten.Entschaerft(T(ressource, argumente));
            IXLComment k = c.CreateComment();
            k.Author = "EPOS-Plan";
            k.AddText(text);
            k.Style.Size.SetWidth(48).Size.SetHeight(Math.Max(4, Math.Min(24, text.Length / 36 + 2)));
        }

        /// <summary>Katalogfassung, Art der Vorlage und Sprache in <c>custom.xml</c> (Konzept 4.9, 5.6).</summary>
        internal void Eigenschaften(XLWorkbook wb, int fassung, string art)
        {
            wb.CustomProperties.Add(Vorlagenpruefer.EIGENSCHAFT_KATALOGFASSUNG, fassung.ToString(CultureInfo.InvariantCulture));
            wb.CustomProperties.Add(WordBaukasten.EIGENSCHAFT_VORLAGE, art);
            wb.CustomProperties.Add(Vorlagenpruefer.EIGENSCHAFT_SPRACHE, Englisch ? "en" : "de");
            wb.Properties.Author = "EPOS-Plan";
            wb.Properties.Created = ZEITSTEMPEL;
            wb.Properties.Modified = ZEITSTEMPEL;
        }

        /// <summary>Speichert die Mappe in den Speicher.</summary>
        internal byte[] Speichere(XLWorkbook wb)
        {
            using (var strom = new MemoryStream())
            {
                wb.SaveAs(strom);
                return strom.ToArray();
            }
        }

        /// <summary>Setzt die Zeitstempel aller Einträge des Pakets auf <see cref="ZEITSTEMPEL"/> (wiederholbare Bytes).</summary>
        internal static byte[] Festschreiben(byte[] paket)
        {
            using (var ein = new MemoryStream(paket, false))
            using (var zipEin = new ZipArchive(ein, ZipArchiveMode.Read))
            using (var aus = new MemoryStream())
            {
                using (var zipAus = new ZipArchive(aus, ZipArchiveMode.Create, true))
                {
                    foreach (ZipArchiveEntry e in zipEin.Entries)
                    {
                        ZipArchiveEntry neu = zipAus.CreateEntry(e.FullName, CompressionLevel.Optimal);
                        neu.LastWriteTime = new DateTimeOffset(ZEITSTEMPEL);
                        using (Stream q = e.Open())
                        using (Stream z = neu.Open())
                            q.CopyTo(z);
                    }
                }
                return aus.ToArray();
            }
        }
    }
}
