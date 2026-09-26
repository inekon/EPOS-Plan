using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using S = DocumentFormat.OpenXml.Spreadsheet;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die ausführliche Excel-Vorlage</b> (Anwenderauftrag BV-E9 „Auch Excel soll eine Vorlage sein mit allen
    /// Konfigurationselementen“; Konzept Berichtsvorlagen 4.4, 7.1–7.4) — eine mitgelieferte Mappe je Sprache
    /// (<c>Berichtsvorlage_Excel_Ausfuehrlich.xlsx</c>, <c>…_en.xlsx</c>), die jedes Konfigurationselement einer Excel-Vorlage
    /// einmal zeigt und in einer Zellnotiz erklärt:
    /// <list type="bullet">
    /// <item>Deckblatt: Zellplatzhalter jeder Art (Text, Zahl, Datum, Liste, Prozent in einer Prozentzelle, Parameter als Anteil,
    /// <c>|mit grund</c>, <c>|stellen n</c>, Text im Satz), Namen <c>EPOS.*</c> und <c>EPOS_*</c> auf eine Zelle und als
    /// Konstante, zwei Formeln — auf einen reservierten Parameternamen und auf eine eigene Zelle (<c>FullCalculationOnLoad</c>);</item>
    /// <item>die Blattmarken aller sieben erzeugten Blätter in der Folge der Standardmappe;</item>
    /// <item>Blatt „Auswertung“: Tabellen als erzeugter Bereich (listentauglich zugleich Excel-Tabelle), die drei Tabellen mit
    /// reiner Excel-Quelle und ein Bildplatzhalter, der ein Excel-Diagramm wird;</item>
    /// <item>Blatt „Eigene Diagramme“: eine Excel-Tabelle <c>EPOS_tabelle__wirtschaft__szenarien</c> mit einem eigenen
    /// Säulendiagramm darauf und ein eigenes Liniendiagramm auf den Namen <c>EPOS.reihe.*</c>;</item>
    /// <item>das Musterblatt <c>blatt.detail</c> mit <c>stand.*</c>-Werten, einer Tabelle je Stand als Zellmarke, einer als
    /// Excel-Tabelle <c>EPOS_stand__tabelle__monatswerte</c> und einem Diagramm je Stand.</item>
    /// </list>
    /// Erzeugt vom Werkzeug <c>Werkzeuge/Berichtsvorlage</c> (Modus <c>excel-ausfuehrlich</c>, Sammelbefehl <c>alle</c>); die
    /// Aktualitätswache vergleicht die mitgelieferte Datei mit einer frisch erzeugten (<c>BerichtsvorlagenCtrl.Inhaltsschluessel</c>).
    /// </summary>
    public static class ExcelAusfuehrlich
    {
        /// <summary>Die Art der Vorlage in <c>EPOS.Vorlage</c>.</summary>
        public const string VORLAGENART = "ausfuehrlich-excel";

        /// <summary>Die Blätter in ihrer Folge (Namen der Marken sind sprachneutral).</summary>
        internal const string BLATT_UEBERSICHT = "blatt.uebersicht", BLATT_VERGLEICH = "blatt.vergleich",
                              BLATT_WIRTSCHAFT = "blatt.wirtschaftlichkeit", BLATT_VERLAUF = "blatt.verlauf",
                              BLATT_DETAIL = "blatt.detail", BLATT_CHECKLISTE = "blatt.checkliste",
                              BLATT_DIAGRAMMDATEN = "blatt.diagrammdaten";

        /// <summary>Die Excel-Tabelle mit dem eigenen Säulendiagramm.</summary>
        internal const string TABELLE_SZENARIEN = "EPOS_tabelle__wirtschaft__szenarien";

        /// <summary>Die Excel-Tabelle je Stand auf dem Musterblatt.</summary>
        internal const string TABELLE_MONATSWERTE = "EPOS_stand__tabelle__monatswerte";

        /// <summary>Die Reihennamen des eigenen Liniendiagramms.</summary>
        internal const string REIHE_MONATE = "EPOS.reihe.monate", REIHE_WAERME = "EPOS.reihe.waermebedarf.monate";

        /// <summary>
        /// <b>Erzeugt die ausführliche Excel-Vorlage</b> auf Deutsch oder Englisch — die Bytes einer <c>.xlsx</c> mit
        /// <c>EPOS.Katalogfassung</c> (<paramref name="fassung"/>), <c>EPOS.Vorlage</c> = <see cref="VORLAGENART"/> und
        /// <c>EPOS.Sprache</c>.
        /// </summary>
        public static byte[] Erzeuge(bool englisch, int fassung = Vorlagenfeldkatalog.KATALOGFASSUNG)
        {
            var m = new Excelmappenbau(englisch);
            byte[] bytes;
            string eigene;
            using (var wb = new XLWorkbook())
            {
                Deckblatt(wb, m);
                Marke(wb, m, BLATT_UEBERSICHT, nameof(R.BV_XLA_N_MARKE));
                Marke(wb, m, BLATT_VERGLEICH, nameof(R.BV_XLA_N_MARKE));
                Auswertung(wb, m);
                eigene = EigeneDiagramme(wb, m);
                Marke(wb, m, BLATT_WIRTSCHAFT, nameof(R.BV_XLA_N_MARKE_WIRTSCHAFT));
                Marke(wb, m, BLATT_VERLAUF, nameof(R.BV_XLA_N_MARKE));
                Musterblatt(wb, m);
                Marke(wb, m, BLATT_CHECKLISTE, nameof(R.BV_XLA_N_MARKE_CHECKLISTE));
                Marke(wb, m, BLATT_DIAGRAMMDATEN, nameof(R.BV_XLA_N_MARKE_DIAGRAMMDATEN));
                m.Eigenschaften(wb, fassung, VORLAGENART);
                bytes = m.Speichere(wb);
            }
            bytes = Diagramme(bytes, eigene, englisch);
            return Excelmappenbau.Festschreiben(bytes);
        }

        // =====================================================================
        //  Deckblatt
        // =====================================================================

        private static void Deckblatt(XLWorkbook wb, Excelmappenbau m)
        {
            IXLWorksheet ws = m.Blatt(wb, nameof(R.BV_XLA_BLATT_DECKBLATT));
            ws.Column(1).Width = 44;
            ws.Column(2).Width = 34;
            ws.Column(3).Width = 14;

            IXLCell titel = ws.Cell(1, 1);
            titel.Value = "{{bericht.titel}}";
            titel.Style.Font.Bold = true;
            titel.Style.Font.FontSize = 18;
            m.Notiz(titel, nameof(R.BV_XLA_N_TEXT));
            ws.Cell(2, 1).Value = "{{bericht.untertitel}}";
            ws.Cell(2, 1).Style.Font.FontSize = 13;

            // Beschriftung · Wert wie das Deckblatt des Wortberichts — die Beschriftungen als text.* in der Sprache des Berichts.
            string[,] tafel =
            {
                { "{{text.kunde}}", "{{projekt.kunde}}" },
                { "{{text.bearbeiter}}", "{{projekt.bearbeiter}}" },
                { "{{text.ersteller}}", "{{ersteller.firma}}" },
                { "{{text.varianten}}", "{{bericht.varianten.liste}}" },
                { "{{text.datum}}", "{{bericht.datum}}" },
            };
            for (int i = 0; i < tafel.GetLength(0); i++)
            {
                ws.Cell(4 + i, 1).Value = tafel[i, 0];
                ws.Cell(4 + i, 1).Style.Font.Bold = true;
                ws.Cell(4 + i, 2).Value = tafel[i, 1];
            }
            m.Notiz(ws.Cell(4, 1), nameof(R.BV_XLA_N_FESTTEXT));
            m.Notiz(ws.Cell(7, 2), nameof(R.BV_XLA_N_LISTE));
            m.Notiz(ws.Cell(8, 2), nameof(R.BV_XLA_N_DATUM));

            // Zahlen: Zahl mit Kernformat, Prozent in einer Prozentzelle, Parameter als Anteil, |stellen n, |mit grund.
            int r = 10;
            m.Abschnitt(ws, r++, m.T(nameof(R.BV_XLA_ABSCHNITT_ZAHLEN)));
            ws.Cell(r, 1).Value = m.T(nameof(R.BV_XLA_ANZAHL));
            ws.Cell(r, 2).Value = "{{bericht.varianten.anzahl}}";
            m.Notiz(ws.Cell(r, 2), nameof(R.BV_XLA_N_ZAHL));
            r++;
            ws.Cell(r, 1).Value = "{{kennzahl.energie.waermebedarf.beschriftung}}";
            ws.Cell(r, 2).Value = "{{stamm.kennzahl.energie.waermebedarf}}";
            ws.Cell(r, 3).Value = "{{kennzahl.energie.waermebedarf.einheit}}";
            m.Notiz(ws.Cell(r, 1), nameof(R.BV_XLA_N_BESCHRIFTUNG));
            int zeileWaerme = r;
            r++;
            ws.Cell(r, 1).Value = "{{kennzahl.energie.wp_deckung.beschriftung}}";
            ws.Cell(r, 2).Value = "{{stamm.kennzahl.energie.wp_deckung}}";
            ws.Cell(r, 2).Style.NumberFormat.Format = "0.0%";
            m.Notiz(ws.Cell(r, 2), nameof(R.BV_XLA_N_PROZENT));
            r++;
            ws.Cell(r, 1).Value = "{{kennzahl.eff.jaz.beschriftung}}";
            ws.Cell(r, 2).Value = "{{stamm.kennzahl.eff.jaz|stellen 2}}";
            m.Notiz(ws.Cell(r, 2), nameof(R.BV_XLA_N_STELLEN));
            r++;
            ws.Cell(r, 1).Value = m.T(nameof(R.BV_XLA_ZINS));
            ws.Cell(r, 2).Value = "{{wirtschaft.parameter.zins}}";
            m.Notiz(ws.Cell(r, 2), nameof(R.BV_XLA_N_PARAMETER));
            r++;
            ws.Cell(r, 1).Value = m.T(nameof(R.BV_XLA_BESTE));
            ws.Cell(r, 2).Value = "{{wirtschaft.beste.anzeige}}";
            r++;
            ws.Cell(r, 1).Value = m.T(nameof(R.BV_XLA_BESTE_KAPITALWERT));
            ws.Cell(r, 2).Value = "{{wirtschaft.beste.kapitalwert_diff|mit grund}}";
            m.Notiz(ws.Cell(r, 2), nameof(R.BV_XLA_N_MIT_GRUND));
            r += 2;

            // Text im Satz.
            ws.Cell(r, 1).Value = "{{text.erstellt_mit}} {{ersteller.programm}} {{ersteller.version}}";
            m.Notiz(ws.Cell(r, 1), nameof(R.BV_XLA_N_SATZ));
            r += 2;

            // Namen: auf eine Zelle (Punkt- und Unterstrichform) und als Konstante.
            m.Abschnitt(ws, r++, m.T(nameof(R.BV_XLA_ABSCHNITT_NAMEN)));
            ws.Cell(r, 1).Value = m.T(nameof(R.BV_XLA_NAME_PROJEKT));
            wb.DefinedNames.Add("EPOS.projekt.name", ws.Range(r, 2, r, 2));
            m.Notiz(ws.Cell(r, 2), nameof(R.BV_XLA_N_NAME_PUNKT));
            r++;
            ws.Cell(r, 1).Value = m.T(nameof(R.BV_XLA_NAME_KLIMA));
            wb.DefinedNames.Add("EPOS_projekt__klimaregion", ws.Range(r, 2, r, 2));
            m.Notiz(ws.Cell(r, 2), nameof(R.BV_XLA_N_NAME_UNTERSTRICH));
            r++;
            ws.Cell(r, 1).Value = m.T(nameof(R.BV_XLA_NAME_KONSTANTE));
            ws.Cell(r, 2).Value = "EPOS.projekt.simulationsstand";
            ws.Cell(r, 2).Style.Font.FontColor = XLColor.FromHtml("#7F7F7F");
            wb.DefinedNames.Add("EPOS.projekt.simulationsstand", "=\"\"");
            m.Notiz(ws.Cell(r, 1), nameof(R.BV_XLA_N_NAME_KONSTANTE));
            r += 2;

            // Formeln: auf einen reservierten Parameternamen der Formelmappe und auf eine eigene Zelle.
            m.Abschnitt(ws, r++, m.T(nameof(R.BV_XLA_ABSCHNITT_FORMELN)));
            ws.Cell(r, 1).Value = m.T(nameof(R.BV_XLA_FORMEL_ZINS));
            ws.Cell(r, 2).FormulaA1 = "IFERROR(Zins_i*100,\"\")";
            ws.Cell(r, 2).Style.NumberFormat.Format = "0.00";
            m.Notiz(ws.Cell(r, 2), nameof(R.BV_XLA_N_FORMEL_RESERVIERT));
            r++;
            ws.Cell(r, 1).Value = m.T(nameof(R.BV_XLA_FORMEL_EIGEN));
            ws.Cell(r, 2).FormulaA1 = "IF(ISNUMBER(B" + zeileWaerme + "),B" + zeileWaerme + "*1000,\"\")";
            ws.Cell(r, 2).Style.NumberFormat.Format = "#,##0";
            m.Notiz(ws.Cell(r, 2), nameof(R.BV_XLA_N_FORMEL_EIGEN));
            r += 2;

            ws.Cell(r, 1).Value = "{{bericht.warnungen}}";
            ws.Cell(r, 1).Style.Alignment.WrapText = true;
            m.Notiz(ws.Cell(r, 1), nameof(R.BV_XLA_N_WARNUNGEN));
        }

        /// <summary>Ein Blatt nur mit seiner Blattmarke in A1 — das erzeugte Blatt tritt an seine Stelle.</summary>
        private static void Marke(XLWorkbook wb, Excelmappenbau m, string schluessel, string notiz)
        {
            IXLWorksheet ws = wb.Worksheets.Add(schluessel);
            ws.Cell(1, 1).Value = "{{" + schluessel + "}}";
            m.Notiz(ws.Cell(1, 1), notiz);
        }

        // =====================================================================
        //  Auswertung: Tabellen und ein Bildplatzhalter
        // =====================================================================

        private static void Auswertung(XLWorkbook wb, Excelmappenbau m)
        {
            IXLWorksheet ws = m.Blatt(wb, nameof(R.BV_XLA_BLATT_AUSWERTUNG));
            m.Titel(ws.Cell(1, 1), m.T(nameof(R.BV_XLA_BLATT_AUSWERTUNG)));
            ws.Column(1).Width = 30;
            var bloecke = new (string Schluessel, string Notiz)[]
            {
                ("tabelle.varianten", nameof(R.BV_XLA_N_TABELLE_LISTE)),
                ("tabelle.komponenten.matrix", nameof(R.BV_XLA_N_TABELLE_BEREICH)),
                ("tabelle.wirtschaft.parameter", nameof(R.BV_XLA_N_TABELLE_EXCEL)),
                ("tabelle.wirtschaft.verlauf", nameof(R.BV_XLA_N_TABELLE_EXCEL)),
            };
            int r = 3;
            foreach ((string schluessel, string notiz) in bloecke)
            {
                Beschreibung(ws, r++, m, schluessel);
                ws.Cell(r, 1).Value = "{{" + schluessel + "}}";
                m.Notiz(ws.Cell(r, 1), notiz);
                r += 2;
            }
            Beschreibung(ws, r++, m, "bild.wirtschaft.spanne");
            ws.Cell(r, 1).Value = "{{bild.wirtschaft.spanne}}";
            m.Notiz(ws.Cell(r, 1), nameof(R.BV_XLA_N_BILD));
        }

        private static void Beschreibung(IXLWorksheet ws, int r, Excelmappenbau m, string schluessel)
        {
            Vorlagenfeld f = Vorlagenfeldkatalog.Finde(schluessel);
            if (f != null) m.Beschreibung(ws, r, f);
        }

        // =====================================================================
        //  Eigene Diagramme auf einer Excel-Tabelle und auf Reihennamen
        // =====================================================================

        /// <summary>Das Blatt der eigenen Diagramme; die Diagramme selbst legt <see cref="Diagramme"/> über das SDK an.</summary>
        private static string EigeneDiagramme(XLWorkbook wb, Excelmappenbau m)
        {
            IXLWorksheet ws = m.Blatt(wb, nameof(R.BV_XLA_BLATT_DIAGRAMME));
            // Die Excel-Tabelle EPOS_tabelle__wirtschaft__szenarien: Kopf und eine Musterzeile — EPOS füllt sie Zeile für Zeile.
            string[] kopf =
            {
                m.T(nameof(R.BV_XLA_SP_VERSION)), m.T(nameof(R.BV_XLA_SP_UNGUENSTIG)), m.T(nameof(R.BV_XLA_SP_ERWARTET)),
                m.T(nameof(R.BV_XLA_SP_GUENSTIG)), m.T(nameof(R.BV_XLA_SP_SPANNE)), m.T(nameof(R.BV_XLA_SP_AMORTISATION)),
                m.T(nameof(R.BV_XLA_SP_EINSTUFUNG)),
            };
            for (int j = 0; j < kopf.Length; j++) ws.Cell(1, j + 1).Value = kopf[j];
            ws.Cell(2, 1).Value = m.T(nameof(R.BV_XLA_MUSTERZEILE));
            for (int j = 2; j <= 5; j++) ws.Cell(2, j).Value = 0.0;
            IXLTable t = ws.Range(1, 1, 2, kopf.Length).CreateTable(TABELLE_SZENARIEN);
            t.Theme = XLTableTheme.TableStyleLight9;
            m.Notiz(ws.Cell(1, 1), nameof(R.BV_XLA_N_EXCELTABELLE));

            // Die Punkte der Reihennamen: zwölf Monate — EPOS setzt das RefersTo der Namen auf das Blatt „Diagrammdaten“.
            for (int i = 0; i < 12; i++)
            {
                ws.Cell(30 + i, 1).Value = m.Kultur.DateTimeFormat.AbbreviatedMonthNames[i];
                ws.Cell(30 + i, 2).Value = 0.0;
            }
            ws.Cell(29, 1).Value = m.T(nameof(R.BV_XLA_REIHEN));
            m.Notiz(ws.Cell(29, 1), nameof(R.BV_XLA_N_REIHEN));
            wb.DefinedNames.Add(REIHE_MONATE, ws.Range(30, 1, 41, 1));
            wb.DefinedNames.Add(REIHE_WAERME, ws.Range(30, 2, 41, 2));
            ws.Column(1).Width = 26;
            return ws.Name;
        }

        /// <summary>
        /// Legt über das OpenXML SDK die beiden eigenen Diagramme an: Säulen „Erwartet je Version“ auf der Excel-Tabelle und eine
        /// Linie „Wärmebedarf je Monat“ auf den Reihennamen (Reihenbezüge <c>[0]!EPOS.reihe.*</c>).
        /// </summary>
        private static byte[] Diagramme(byte[] bytes, string blatt, bool englisch)
        {
            var strom = new MemoryStream();
            strom.Write(bytes, 0, bytes.Length);
            strom.Position = 0;
            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(strom, true))
            {
                S.Sheet sheet = doc.WorkbookPart.Workbook.Sheets.Elements<S.Sheet>().First(s => s.Name == blatt);
                var teil = (WorksheetPart)doc.WorkbookPart.GetPartById(sheet.Id);

                var saeulen = new Exceldiagramm("eigen.saeulen", ExcelVorlagentexte.T(englisch, nameof(R.BV_XLA_DIAGRAMM_SAEULEN)))
                {
                    Kategorienkopf = ExcelVorlagentexte.T(englisch, nameof(R.BV_XLA_SP_VERSION)),
                };
                saeulen.Kategorien.Add(ExcelVorlagentexte.T(englisch, nameof(R.BV_XLA_MUSTERZEILE)));
                Excelreihe reihe = saeulen.Reihe(ExcelVorlagentexte.T(englisch, nameof(R.BV_XLA_SP_ERWARTET)),
                                                 new double?[] { 0.0 }, Excelreihenart.Saeule, "4472C4");
                var bereich = new Datenbereich(blatt, 1, 2, 1, 1);
                bereich.Spalten[reihe] = 3;
                Exceldiagrammschreiber.Setze(teil, new Diagrammanker(8, 0, Diagrammanker.SPALTEN, Diagrammanker.ZEILEN), saeulen, bereich, englisch);

                var linie = new Exceldiagramm("eigen.linie", ExcelVorlagentexte.T(englisch, nameof(R.BV_XLA_DIAGRAMM_LINIE)))
                {
                    Kategorienkopf = ExcelVorlagentexte.T(englisch, nameof(R.BV_XLA_REIHEN)),
                };
                for (int i = 0; i < 12; i++) linie.Kategorien.Add(BerichtTexte.KulturFuer(englisch).DateTimeFormat.AbbreviatedMonthNames[i]);
                Excelreihe l = linie.Reihe(ExcelVorlagentexte.T(englisch, nameof(R.BV_XLA_REIHE_WAERME)),
                                           Enumerable.Repeat((double?)0.0, 12), Excelreihenart.Linie, "C00000");
                var b2 = new Datenbereich(blatt, 29, 30, 12, 1);
                b2.Spalten[l] = 2;
                ChartPart teil2 = Exceldiagrammschreiber.Setze(teil, new Diagrammanker(8, 28, Diagrammanker.SPALTEN, Diagrammanker.ZEILEN),
                                                               linie, b2, englisch);
                string q = "'" + blatt.Replace("'", "''") + "'!";
                foreach (C.Formula f in teil2.ChartSpace.Descendants<C.Formula>())
                {
                    if (f.Text == q + "$B$30:$B$41") f.Text = "[0]!" + REIHE_WAERME;
                    else if (f.Text == q + "$A$30:$A$41") f.Text = "[0]!" + REIHE_MONATE;
                }
                teil2.ChartSpace.Save();
            }
            return strom.ToArray();
        }

        // =====================================================================
        //  Musterblatt blatt.detail
        // =====================================================================

        private static void Musterblatt(XLWorkbook wb, Excelmappenbau m)
        {
            IXLWorksheet ws = wb.Worksheets.Add(BLATT_DETAIL);
            ws.Cell(1, 1).Value = "{{blatt.detail}}";
            m.Notiz(ws.Cell(1, 1), nameof(R.BV_XLA_N_MUSTERBLATT));
            ws.Column(1).Width = 40;
            ws.Column(2).Width = 22;

            ws.Cell(2, 1).Value = "{{stand.anzeige}}";
            ws.Cell(2, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Style.Font.FontSize = 14;
            m.Notiz(ws.Cell(2, 1), nameof(R.BV_XLA_N_STAND));
            ws.Cell(3, 1).Value = "{{stand.rolle}}";
            ws.Cell(3, 2).Value = "{{stand.simulationsstand}}";
            ws.Cell(5, 1).Value = "{{kennzahl.energie.waermebedarf.beschriftung}}";
            ws.Cell(5, 2).Value = "{{stand.kennzahl.energie.waermebedarf}}";
            ws.Cell(6, 1).Value = "{{kennzahl.eff.jaz.beschriftung}}";
            ws.Cell(6, 2).Value = "{{stand.kennzahl.eff.jaz|stellen 2}}";
            ws.Cell(7, 1).Value = m.T(nameof(R.BV_XLA_KAPITALWERT_STAND));
            ws.Cell(7, 2).Value = "{{stand.wirtschaft.kapitalwert_diff|mit grund}}";

            Beschreibung(ws, 9, m, "stand.tabelle.kennzahlen");
            ws.Cell(10, 1).Value = "{{stand.tabelle.kennzahlen}}";
            m.Notiz(ws.Cell(10, 1), nameof(R.BV_XLA_N_TABELLE_STAND));

            Beschreibung(ws, 12, m, "stand.tabelle.monatswerte");
            ws.Cell(13, 1).Value = m.T(nameof(R.BV_XLA_SP_MONAT));
            ws.Cell(13, 2).Value = m.T(nameof(R.BV_XLA_SP_WERT));
            ws.Cell(14, 1).Value = m.T(nameof(R.BV_XLA_MUSTERZEILE));
            ws.Cell(14, 2).Value = 0.0;
            IXLTable t = ws.Range(13, 1, 14, 2).CreateTable(TABELLE_MONATSWERTE);
            t.Theme = XLTableTheme.TableStyleLight9;
            m.Notiz(ws.Cell(13, 1), nameof(R.BV_XLA_N_EXCELTABELLE_STAND));

            Beschreibung(ws, 16, m, "stand.bild.strombilanz_monate");
            ws.Cell(17, 1).Value = "{{stand.bild.strombilanz_monate}}";
            m.Notiz(ws.Cell(17, 1), nameof(R.BV_XLA_N_BILD_STAND));
        }
    }
}
