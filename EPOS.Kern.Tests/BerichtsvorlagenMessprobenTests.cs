using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using WindowsFormsApplication1;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using S = DocumentFormat.OpenXml.Spreadsheet;
using W = DocumentFormat.OpenXml.Wordprocessing;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// BV-E0 — die MESSPROBEN des Konzepts Berichtsvorlagen
    /// (<c>Dokumentation/aktuell/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md</c>,
    /// Abschnitt 13), als Tests festgehalten.
    ///
    /// <para><b>Was eine Probe ist.</b> Jede beantwortet eine Frage des Konzepts an den
    /// Paketen, wie das Haus sie führt: ClosedXML 0.105.1 und DocumentFormat.OpenXml 3.5.1
    /// (<c>Directory.Packages.props</c>). Der Kommentar je Fall nennt die Frage und das
    /// Messergebnis vom 25.09.2026; die Zusicherungen halten das GEMESSENE Verhalten fest —
    /// auch dort, wo es ein Mangel ist. Wird ein Fall nach einem Paketwechsel rot, hat sich
    /// das Verhalten geändert: Dann ist die Folgerung im Konzept zu prüfen, nicht die
    /// Zusicherung nachzuziehen.</para>
    ///
    /// <para><b>Rahmen.</b> Dateien entstehen nur im Temp-Ordner und werden weggeräumt;
    /// keine Datenbank, kein getauschter Dienst. Geprüft wird mit dem
    /// <see cref="OpenXmlValidator"/> für Office 2016.</para>
    ///
    /// <para><b>Nicht hier:</b> die TIPPPROBE (wie Word <c>{{</c> beim Tippen in Runs
    /// zerlegt, was Anführungszeichen und Autokorrektur daraus machen) — ohne echtes Word
    /// nicht messbar —, und der Nachweis mit einer echten, in Word 365 angelegten Vorlage
    /// (Probe 5 baut die Stildatei nach).</para>
    /// </summary>
    public class BerichtsvorlagenMessprobenTests
    {
        private const string CT_XLSX = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml";
        private const string CT_XLTX = "application/vnd.openxmlformats-officedocument.spreadsheetml.template.main+xml";
        private const string CT_DOCX = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";
        private const string CT_DOTX = "application/vnd.openxmlformats-officedocument.wordprocessingml.template.main+xml";

        /// <summary>Die berechnete Spalte der Probevorlage (Probe 2), wie Excel sie ablegt.</summary>
        private const string FORMEL_DOPPELT = "Tabelle1[[#This Row],[Wert]]*2";

        // =====================================================================
        //  Probe 1 — Excel-Namen mit Punkt
        // =====================================================================

        /// <summary>
        /// PROBE 1 — Excel-Namen mit Punkt (Konzept 4.4, Zeile „Name“, und 5.5).
        ///
        /// <para><b>Frage:</b> Tragen mappenweite Namen wie
        /// <c>EPOS.stamm.kennzahl.eff.jaz</c> ihre Punkte durch Speichern und Laden — mit
        /// ClosedXML und mit dem SDK gelesen —, und übersteht <c>EPOS.CO2</c> den Weg,
        /// dessen Rest einem Zellbezug gleicht? Sonst gälte der Rückfall
        /// <c>EPOS_&lt;schlüssel&gt;</c> mit <c>__</c> für den Punkt.</para>
        ///
        /// <para><b>Gemessen:</b> Ja. ClosedXML legt beide Namen an und schreibt sie als
        /// mappenweite <c>definedName</c> (ohne <c>localSheetId</c>) mit dem Bezug
        /// <c>Stamm!$B$1:$B$1</c> bzw. <c>Stamm!$B$2:$B$2</c>; ClosedXML und SDK lesen
        /// dieselben Namen und Bezüge zurück, der Validator meldet 0 Fehler. Eine Formel
        /// <c>EPOS.CO2*2</c> übersteht den Weg, und ClosedXML rechnet sie (246,8). Der
        /// Rückfall mit <c>__</c> wird nicht gebraucht. (In Excel selbst ist die Datei nicht
        /// geöffnet worden; Excels Namensregel lässt Punkte ausdrücklich zu.)</para>
        ///
        /// <para><b>Nebenbefund:</b> ClosedXML nimmt auch den nackten Namen <c>CO2</c>
        /// ohne Widerspruch an, obwohl er in Excel ein Zellbezug ist (Spalte CO, Zeile 2),
        /// und der Validator merkt es nicht. Den Präfix <c>EPOS.</c> muss EPOS selbst
        /// durchsetzen — weder Paket noch Validator tun es.</para>
        /// </summary>
        [Fact]
        public void Probe1_Excel_Namen_mit_Punkt_ueberstehen_Speichern_und_Laden()
        {
            const string JAZ = "EPOS.stamm.kennzahl.eff.jaz";
            const string CO2 = "EPOS.CO2";

            string ordner = TempOrdner();
            try
            {
                string pfad = Path.Combine(ordner, "namen.xlsx");
                using (var wb = new XLWorkbook())
                {
                    IXLWorksheet ws = wb.Worksheets.Add("Stamm");
                    ws.Cell("A1").Value = "JAZ";
                    ws.Cell("B1").Value = 4.2;
                    ws.Cell("A2").Value = "CO2";
                    ws.Cell("B2").Value = 123.4;
                    wb.DefinedNames.Add(JAZ, ws.Range("B1"));
                    wb.DefinedNames.Add(CO2, ws.Range("B2"));
                    ws.Cell("C2").FormulaA1 = CO2 + "*2";
                    wb.SaveAs(pfad);
                }

                using (var wb = new XLWorkbook(pfad))
                {
                    Assert.True(wb.DefinedNames.TryGetValue(JAZ, out IXLDefinedName jaz),
                                JAZ + " ist beim Laden mit ClosedXML verloren gegangen.");
                    Assert.Equal("Stamm!$B$1:$B$1", jaz.RefersTo);
                    Assert.Equal(4.2, jaz.Ranges.First().FirstCell().Value.GetNumber(), 12);

                    Assert.True(wb.DefinedNames.TryGetValue(CO2, out IXLDefinedName co2),
                                CO2 + " ist beim Laden mit ClosedXML verloren gegangen.");
                    Assert.Equal("Stamm!$B$2:$B$2", co2.RefersTo);

                    IXLCell formel = wb.Worksheet("Stamm").Cell("C2");
                    Assert.Equal(CO2 + "*2", formel.FormulaA1);
                    Assert.Equal(246.8, formel.Value.GetNumber(), 9);
                }

                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(pfad, false))
                {
                    Dictionary<string, S.DefinedName> namen = doc.WorkbookPart.Workbook.DefinedNames
                        .Elements<S.DefinedName>().ToDictionary(n => n.Name.Value, n => n);
                    Assert.Equal("Stamm!$B$1:$B$1", namen[JAZ].Text);
                    Assert.Null(namen[JAZ].LocalSheetId);
                    Assert.Equal("Stamm!$B$2:$B$2", namen[CO2].Text);
                    Assert.Null(namen[CO2].LocalSheetId);
                    Assert.Empty(Validierungsfehler(doc));
                }

                // Nebenbefund: der nackte Zellbezug als Name — ClosedXML wirft nicht,
                // der Validator schweigt.
                string nackt = Path.Combine(ordner, "nackt.xlsx");
                using (var wb = new XLWorkbook())
                {
                    IXLWorksheet ws = wb.Worksheets.Add("Stamm");
                    ws.Cell("B2").Value = 123.4;
                    wb.DefinedNames.Add("CO2", ws.Range("B2"));
                    wb.SaveAs(nackt);
                }
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(nackt, false))
                {
                    Assert.Contains(doc.WorkbookPart.Workbook.DefinedNames.Elements<S.DefinedName>(),
                                    n => n.Name == "CO2");
                    Assert.Empty(Validierungsfehler(doc));
                }
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Probe 2 — ClosedXML-Rundlauf einer Vorlage
        // =====================================================================

        /// <summary>
        /// PROBE 2 — der ClosedXML-Rundlauf einer Vorlage (Konzept 7.4; BV-E8 „nur nach
        /// grünen Messproben“).
        ///
        /// <para><b>Frage:</b> Was überlebt, wenn ClosedXML eine in Excel gebaute Vorlage
        /// lädt, eine Zelle ändert, Zeilen in eine Excel-Tabelle einfügt
        /// (<c>InsertRowsBelow</c>) und speichert: das Diagramm, die Tabelle samt
        /// berechneter Spalte, ein definierter Name, das gespeicherte Ergebnis
        /// <c>&lt;v&gt;</c> einer Vorlagenformel, <c>fullCalcOnLoad</c>? Die Vorlage baut
        /// das SDK so, wie Excel sie ablegt: Formeln MIT <c>&lt;v&gt;</c>, die berechnete
        /// Spalte mit <c>calculatedColumnFormula</c>, ein Balkendiagramm auf dem Bereich.</para>
        ///
        /// <para><b>Gemessen</b> (Validator vor und nach jedem Lauf 0 Fehler):</para>
        /// <list type="bullet">
        ///   <item><description>DIAGRAMM: bleibt — Teil, Zeichnungsverweis und Reihenformel
        ///     unverändert.</description></item>
        ///   <item><description>TABELLE: bleibt und wächst mit <c>InsertRowsBelow(2)</c> auf
        ///     <c>A1:C6</c>. Die BERECHNETE SPALTE geht dabei verloren:
        ///     <c>calculatedColumnFormula</c> fehlt danach, und die neuen Zeilen tragen in
        ///     der Spalte keine Formel. Ohne Einfügen — nur eine geänderte Zelle — bleibt sie
        ///     stehen; es ist das Einfügen, das sie kostet.</description></item>
        ///   <item><description>NAME: bleibt mit unverändertem Bezug.</description></item>
        ///   <item><description><c>&lt;v&gt;</c>: geht verloren, auch ganz ohne Änderung.
        ///     ClosedXML liest das gespeicherte Ergebnis (60) als Zwischenwert, schreibt die
        ///     Formel aber ohne <c>&lt;v&gt;</c> zurück (dasselbe für die Formeln der
        ///     berechneten Spalte; vgl. <see cref="FormelmappeClosedXmlBefundTests"/>).</description></item>
        ///   <item><description><c>fullCalcOnLoad</c>: setzt ClosedXML nicht von sich aus —
        ///     <c>XLWorkbook.FullCalculationOnLoad</c> steht nach dem Laden auf
        ///     <c>false</c>, das <c>calcPr</c> der Vorlage bleibt ohne das Merkmal. Wer
        ///     <c>FullCalculationOnLoad = true</c> setzt, bekommt es geschrieben.</description></item>
        /// </list>
        ///
        /// <para><b>Folgerung:</b> Die Frage aus 7.4 ist mit „nein“ beantwortet — jede
        /// Vorlagenformel verliert ihr <c>&lt;v&gt;</c>, eine Vorschau ohne Rechenweg zeigt
        /// sie also leer, nicht veraltet. <c>fullCalcOnLoad</c> muss EPOS setzen, sobald die
        /// Vorlage eine Formel trägt (7.4); ClosedXML tut es nicht von selbst. Eine Liste
        /// darf eine Excel-Tabelle mit berechneter Spalte nicht über <c>InsertRowsBelow</c>
        /// wachsen lassen, ohne Formeln und <c>calculatedColumnFormula</c> selbst
        /// nachzutragen (BV-E8).</para>
        /// </summary>
        [Fact]
        public void Probe2_ClosedXML_Rundlauf_einer_Vorlage_mit_Diagramm_Tabelle_Name_und_Formel()
        {
            string ordner = TempOrdner();
            try
            {
                string vorlage = Path.Combine(ordner, "vorlage.xlsx");
                VorlageMitSdkBauen(vorlage);
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(vorlage, false))
                {
                    Assert.Empty(Validierungsfehler(doc));
                    WorksheetPart teil = BlattTeil(doc, "Daten");
                    Assert.Equal(FORMEL_DOPPELT, Spalte(teil, "Doppelt").CalculatedColumnFormula?.Text);
                    Assert.Equal("60", Zelle(teil, "F1").CellValue?.Text);
                    Assert.Single(teil.DrawingsPart.ChartParts);
                }

                // (a) unverändert laden und speichern
                string unveraendert = Path.Combine(ordner, "unveraendert.xlsx");
                using (var wb = new XLWorkbook(vorlage)) wb.SaveAs(unveraendert);

                // (b) eine Zelle ändern, fullCalcOnLoad anfordern
                string zelle = Path.Combine(ordner, "zelle.xlsx");
                using (var wb = new XLWorkbook(vorlage))
                {
                    wb.Worksheet("Daten").Cell("B2").Value = 11;
                    wb.FullCalculationOnLoad = true;
                    wb.SaveAs(zelle);
                }

                // (c) der Fall der Frage: Zelle ändern, zwei Zeilen in die Tabelle, speichern
                string eingefuegt = Path.Combine(ordner, "eingefuegt.xlsx");
                using (var wb = new XLWorkbook(vorlage))
                {
                    Assert.False(wb.FullCalculationOnLoad);
                    IXLWorksheet ws = wb.Worksheet("Daten");
                    Assert.Equal("SUM(B2:B4)", ws.Cell("F1").FormulaA1);
                    Assert.Equal(60.0, ws.Cell("F1").CachedValue.GetNumber(), 12);

                    ws.Cell("B2").Value = 11;
                    IXLTable tabelle = ws.Table("Tabelle1");
                    tabelle.InsertRowsBelow(2);
                    Assert.Equal("A1:C6", tabelle.RangeAddress.ToString());
                    ws.Cell("A5").Value = "Apr";
                    ws.Cell("B5").Value = 40;
                    ws.Cell("A6").Value = "Mai";
                    ws.Cell("B6").Value = 50;
                    Assert.False(ws.Cell("C5").HasFormula);
                    wb.SaveAs(eingefuegt);
                }

                foreach (string datei in new[] { unveraendert, zelle, eingefuegt })
                {
                    using SpreadsheetDocument doc = SpreadsheetDocument.Open(datei, false);
                    string was = Path.GetFileName(datei) + ": ";
                    Assert.Empty(Validierungsfehler(doc));
                    WorksheetPart teil = BlattTeil(doc, "Daten");

                    // Diagramm und Name überleben jeden Lauf.
                    Assert.Single(teil.Worksheet.Elements<S.Drawing>());
                    ChartPart diagramm = Assert.Single(teil.DrawingsPart.ChartParts);
                    Assert.Equal("Daten!$B$2:$B$4", Reihenformel<C.Values>(diagramm));
                    S.DefinedName name = Assert.Single(doc.WorkbookPart.Workbook.DefinedNames.Elements<S.DefinedName>());
                    Assert.Equal("EPOS.probe.werte", name.Name.Value);
                    Assert.Equal("Daten!$B$2:$B$4", name.Text);

                    // <v> der Vorlagenformel und der berechneten Spalte: immer verloren.
                    Assert.Equal("SUM(B2:B4)", Zelle(teil, "F1").CellFormula?.Text);
                    Assert.True(Zelle(teil, "F1").CellValue == null, was + "ClosedXML behält jetzt <v> — 7.4 prüfen.");
                    Assert.Equal(FORMEL_DOPPELT, Zelle(teil, "C2").CellFormula?.Text);
                    Assert.Null(Zelle(teil, "C2").CellValue);
                }

                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(unveraendert, false))
                {
                    WorksheetPart teil = BlattTeil(doc, "Daten");
                    Assert.Equal("A1:C4", Tabelle(teil).Reference.Value);
                    Assert.Equal(FORMEL_DOPPELT, Spalte(teil, "Doppelt").CalculatedColumnFormula?.Text);
                    Assert.Null(doc.WorkbookPart.Workbook.CalculationProperties?.FullCalculationOnLoad);
                }

                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(zelle, false))
                {
                    WorksheetPart teil = BlattTeil(doc, "Daten");
                    Assert.Equal("A1:C4", Tabelle(teil).Reference.Value);
                    Assert.Equal(FORMEL_DOPPELT, Spalte(teil, "Doppelt").CalculatedColumnFormula?.Text);
                    Assert.True(doc.WorkbookPart.Workbook.CalculationProperties?.FullCalculationOnLoad?.Value == true);
                }

                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(eingefuegt, false))
                {
                    WorksheetPart teil = BlattTeil(doc, "Daten");
                    Assert.Equal("A1:C6", Tabelle(teil).Reference.Value);
                    Assert.True(Spalte(teil, "Doppelt").CalculatedColumnFormula == null,
                                "ClosedXML behält die berechnete Spalte jetzt auch beim Einfügen — BV-E8 prüfen.");
                    Assert.Null(Zelle(teil, "C5")?.CellFormula);
                    Assert.Null(Zelle(teil, "C6")?.CellFormula);
                    Assert.Equal("11", Zelle(teil, "B2").CellValue?.Text);
                    Assert.Null(doc.WorkbookPart.Workbook.CalculationProperties?.FullCalculationOnLoad);
                }
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Probe 3 — .xltx und .dotx
        // =====================================================================

        /// <summary>
        /// PROBE 3 — <c>.xltx</c> und <c>.dotx</c> (Konzept 6.1 „Formate“, 7.1 „Mappe“).
        ///
        /// <para><b>Frage:</b> Lässt sich eine Excel-Vorlage <c>.xltx</c> über das SDK
        /// (<c>SpreadsheetDocument.Open</c> + <c>ChangeDocumentType(Workbook)</c>) zur Mappe
        /// umstellen und danach mit ClosedXML laden; wird eine Word-Vorlage <c>.dotx</c> über
        /// <c>ChangeDocumentType(Document)</c> zum Dokument; sind die Ergebnisse
        /// schemagültig?</para>
        ///
        /// <para><b>Gemessen:</b> Ja für beide. Die Umstellung setzt den Inhaltstyp des
        /// Hauptteils auf <c>…sheet.main+xml</c> bzw. <c>…document.main+xml</c>; ClosedXML liest
        /// die umgestellte Mappe samt Werten, der Text des Dokuments bleibt; der Validator
        /// meldet 0 Fehler für die Vorlagen wie für die umgestellten Dateien.</para>
        ///
        /// <para><b>Nebenbefund:</b> ClosedXML lädt eine <c>.xltx</c> auch DIREKT; was
        /// <c>SaveAs</c> daraus macht, hängt am Ladeweg. Vom PFAD geladen, stellt
        /// <c>SaveAs("….xlsx")</c> nach der Zielendung auf <c>…sheet.main+xml</c> um; aus
        /// einem STROM geladen — der Weg des Konzepts, <c>new XLWorkbook(stream)</c> (7.1) —
        /// trägt dieselbe <c>.xlsx</c> weiter <c>…template.main+xml</c>, Endung und
        /// Inhaltstyp widersprechen sich. <c>XLWorkbook.OpenFromTemplate</c> stellt selbst
        /// um. Für den Stromweg ist die Umstellung über das SDK vor dem Laden damit
        /// Pflicht.</para>
        /// </summary>
        [Fact]
        public void Probe3_Xltx_und_Dotx_werden_ueber_das_SDK_zu_Mappe_und_Dokument()
        {
            string ordner = TempOrdner();
            try
            {
                // ---- Excel
                string xltx = Path.Combine(ordner, "vorlage.xltx");
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(xltx, SpreadsheetDocumentType.Template))
                {
                    WorkbookPart wbp = doc.AddWorkbookPart();
                    wbp.Workbook = new S.Workbook();
                    WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();
                    wsp.Worksheet = new S.Worksheet(new S.SheetData(
                        new S.Row(
                            new S.Cell { CellReference = "A1", DataType = S.CellValues.InlineString, InlineString = new S.InlineString(new S.Text("Vorlage")) },
                            new S.Cell { CellReference = "B1", CellValue = new S.CellValue(42) })
                        { RowIndex = 1 }));
                    wbp.Workbook.Append(new S.Sheets(new S.Sheet { Name = "Bericht", SheetId = 1, Id = wbp.GetIdOfPart(wsp) }));
                }
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(xltx, false))
                {
                    Assert.Equal(SpreadsheetDocumentType.Template, doc.DocumentType);
                    Assert.Equal(CT_XLTX, doc.WorkbookPart.ContentType);
                    Assert.Empty(Validierungsfehler(doc));
                }

                string xlsx = Path.Combine(ordner, "umgestellt.xlsx");
                File.Copy(xltx, xlsx);
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(xlsx, true))
                    doc.ChangeDocumentType(SpreadsheetDocumentType.Workbook);
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(xlsx, false))
                {
                    Assert.Equal(SpreadsheetDocumentType.Workbook, doc.DocumentType);
                    Assert.Equal(CT_XLSX, doc.WorkbookPart.ContentType);
                    Assert.Empty(Validierungsfehler(doc));
                }
                using (var wb = new XLWorkbook(xlsx))
                {
                    Assert.Equal("Vorlage", wb.Worksheet(1).Cell("A1").Value.GetText());
                    Assert.Equal(42.0, wb.Worksheet(1).Cell("B1").Value.GetNumber(), 12);
                }

                // Nebenbefund: ClosedXML lädt die .xltx auch ohne Umstellung. Vom PFAD
                // geladen stellt SaveAs nach der Zielendung um ...
                string vomPfad = Path.Combine(ordner, "vom_pfad.xlsx");
                using (var wb = new XLWorkbook(xltx))
                {
                    Assert.Equal("Vorlage", wb.Worksheet(1).Cell("A1").Value.GetText());
                    wb.SaveAs(vomPfad);
                }
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(vomPfad, false))
                    Assert.Equal(CT_XLSX, doc.WorkbookPart.ContentType);

                // ... aus einem STROM geladen bleibt es eine Vorlage, auch unter .xlsx.
                string ausStrom = Path.Combine(ordner, "aus_strom.xlsx");
                using (FileStream strom = File.OpenRead(xltx))
                using (var wb = new XLWorkbook(strom))
                {
                    Assert.Equal("Vorlage", wb.Worksheet(1).Cell("A1").Value.GetText());
                    wb.SaveAs(ausStrom);
                }
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(ausStrom, false))
                    Assert.Equal(CT_XLTX, doc.WorkbookPart.ContentType);

                string ueberVorlage = Path.Combine(ordner, "ueber_vorlage.xlsx");
                using (XLWorkbook wb = XLWorkbook.OpenFromTemplate(xltx)) wb.SaveAs(ueberVorlage);
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(ueberVorlage, false))
                    Assert.Equal(CT_XLSX, doc.WorkbookPart.ContentType);

                // ---- Word
                const string TEXT = "Vorlage {{projekt.kunde}}";
                string dotx = Path.Combine(ordner, "vorlage.dotx");
                using (WordprocessingDocument doc = WordprocessingDocument.Create(dotx, WordprocessingDocumentType.Template))
                {
                    MainDocumentPart main = doc.AddMainDocumentPart();
                    main.Document = new W.Document(new W.Body(new W.Paragraph(new W.Run(new W.Text(TEXT)))));
                }
                using (WordprocessingDocument doc = WordprocessingDocument.Open(dotx, false))
                {
                    Assert.Equal(WordprocessingDocumentType.Template, doc.DocumentType);
                    Assert.Equal(CT_DOTX, doc.MainDocumentPart.ContentType);
                    Assert.Empty(Validierungsfehler(doc));
                }

                string docx = Path.Combine(ordner, "umgestellt.docx");
                File.Copy(dotx, docx);
                using (WordprocessingDocument doc = WordprocessingDocument.Open(docx, true))
                    doc.ChangeDocumentType(WordprocessingDocumentType.Document);
                using (WordprocessingDocument doc = WordprocessingDocument.Open(docx, false))
                {
                    Assert.Equal(WordprocessingDocumentType.Document, doc.DocumentType);
                    Assert.Equal(CT_DOCX, doc.MainDocumentPart.ContentType);
                    Assert.Equal(TEXT, doc.MainDocumentPart.Document.Body.InnerText);
                    Assert.Empty(Validierungsfehler(doc));
                }
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Probe 4 — Inhaltssteuerelement, Alternativtext, zerlegter Platzhalter
        // =====================================================================

        /// <summary>
        /// PROBE 4 — Inhaltssteuerelement und Alternativtext in Word (Konzept 4.2, 4.3,
        /// 6.5) — nur FINDEN, keine Engine.
        ///
        /// <para><b>Frage:</b> Lassen sich die drei Platzhalterformen einer Word-Vorlage über
        /// das SDK sicher finden: ein Inhaltssteuerelement mit <c>w:tag</c> =
        /// <c>projekt.kunde</c> (als Text-SDT im Satz und als Block-SDT), ein Bild mit
        /// <c>wp:docPr/@descr</c> = <c>{{bild.vergleich.balken.eff.jaz}}</c> und ein Absatz,
        /// dessen <c>{{projekt.kunde}}</c> auf drei Runs verteilt ist?</para>
        ///
        /// <para><b>Gemessen:</b> Ja, alle drei. Über das Tag kommen genau zwei
        /// Steuerelemente, ein <c>SdtRun</c> im Absatz „Kunde: …“ und ein <c>SdtBlock</c> im
        /// Rumpf; über <c>descr</c> genau ein Bild samt seinem Absatz; der zerlegte
        /// Platzhalter steht in KEINEM einzelnen <c>w:t</c>, wohl aber im zusammengesetzten
        /// Text seines Absatzes (drei Runs, einer davon fett). Der Validator meldet 0 Fehler.
        /// Der Normalisierer der Engine (BV-E1) muss also absatzweise über die Runs lesen;
        /// ein Suchen je <c>w:t</c> fände den Platzhalter nicht.</para>
        /// </summary>
        [Fact]
        public void Probe4_Inhaltssteuerelement_Alternativtext_und_zerlegter_Platzhalter_lassen_sich_finden()
        {
            const string TAG = "projekt.kunde";
            const string MARKE = "{{projekt.kunde}}";
            const string BILD = "{{bild.vergleich.balken.eff.jaz}}";

            string ordner = TempOrdner();
            try
            {
                string pfad = Path.Combine(ordner, "platzhalter.docx");
                using (WordprocessingDocument doc = WordprocessingDocument.Create(pfad, WordprocessingDocumentType.Document))
                {
                    MainDocumentPart main = doc.AddMainDocumentPart();
                    var sect = new W.SectionProperties();
                    var body = new W.Body(sect);
                    main.Document = new W.Document(body);
                    var k = new WordKontext(main, body, sect);

                    // 1) Text-SDT im Satz
                    k.Fuege(new W.Paragraph(
                        new W.Run(new W.Text("Kunde: ") { Space = SpaceProcessingModeValues.Preserve }),
                        new W.SdtRun(
                            new W.SdtProperties(new W.SdtAlias { Val = "Kunde" }, new W.Tag { Val = TAG }, new W.SdtContentText()),
                            new W.SdtContentRun(new W.Run(new W.Text("Musterkunde")))),
                        new W.Run(new W.Text(", Stand heute.") { Space = SpaceProcessingModeValues.Preserve })));

                    // 2) Block-SDT
                    k.Fuege(new W.SdtBlock(
                        new W.SdtProperties(new W.SdtAlias { Val = "Kunde (Block)" }, new W.Tag { Val = TAG }),
                        new W.SdtContentBlock(new W.Paragraph(new W.Run(new W.Text("Musterkunde"))))));

                    // 3) Bild mit dem Schlüssel im Alternativtext
                    k.Bild(Convert.FromBase64String(PNG_1X1), 100, 80);
                    body.Descendants<DW.DocProperties>().Single().Description = BILD;

                    // 4) der Platzhalter auf drei Runs verteilt, der mittlere fett
                    k.Fuege(new W.Paragraph(
                        new W.Run(new W.Text("{{proj")),
                        new W.Run(new W.RunProperties(new W.Bold()), new W.Text("ekt.kun")),
                        new W.Run(new W.Text("de}}"))));
                }

                using (WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false))
                {
                    Assert.Empty(Validierungsfehler(doc));
                    W.Body body = doc.MainDocumentPart.Document.Body;

                    List<W.SdtElement> steuerelemente = body.Descendants<W.SdtElement>()
                        .Where(s => s.SdtProperties?.GetFirstChild<W.Tag>()?.Val?.Value == TAG).ToList();
                    Assert.Equal(2, steuerelemente.Count);
                    W.SdtRun imSatz = Assert.Single(steuerelemente.OfType<W.SdtRun>());
                    Assert.StartsWith("Kunde: ", imSatz.Ancestors<W.Paragraph>().First().InnerText);
                    W.SdtBlock alsBlock = Assert.Single(steuerelemente.OfType<W.SdtBlock>());
                    Assert.Same(body, alsBlock.Parent);

                    DW.DocProperties bild = Assert.Single(body.Descendants<DW.DocProperties>(),
                        d => d.Description?.Value == BILD);
                    Assert.NotNull(bild.Ancestors<W.Paragraph>().FirstOrDefault());

                    Assert.DoesNotContain(body.Descendants<W.Text>(), t => t.Text.Contains(MARKE));
                    W.Paragraph zerlegt = Assert.Single(body.Elements<W.Paragraph>(),
                        p => string.Concat(p.Descendants<W.Text>().Select(t => t.Text)).Contains(MARKE));
                    Assert.Equal(3, zerlegt.Elements<W.Run>().Count());
                    Assert.Equal(MARKE, zerlegt.InnerText);
                }
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Probe 5 — Vorlage aus deutschem Word 365 (synthetisch)
        // =====================================================================

        /// <summary>
        /// PROBE 5 — Vorlage aus deutschem Word 365, synthetisch (Konzept 6.2).
        ///
        /// <para><b>Frage:</b> Was geschieht, wenn <see cref="WordKontext"/> in eine Vorlage
        /// schreibt, deren Stile die übersetzten IDs des deutschen Word tragen
        /// (<c>berschrift1</c>, <c>berschrift2</c>, <c>Titel</c>, <c>Untertitel</c>,
        /// <c>Standard</c>; <c>w:name</c> „heading 1“, „heading 2“, „Title“, „Subtitle“,
        /// „Normal“) — und lassen sich die Rollen über <c>w:name</c> auflösen?</para>
        ///
        /// <para><b>Gemessen:</b> <c>WordKontext.MitStil("Heading1", …)</c> setzt
        /// <c>pStyle = Heading1</c> — einen Stil, den das Dokument nicht kennt; ebenso
        /// <c>Heading2</c>, <c>Title</c> und <c>Normal</c> (Befund 6.2: Word zeigt solche Absätze als
        /// Standardtext, das Inhaltsverzeichnis bleibt leer). Der Validator meldet 0 Fehler —
        /// er prüft keine Stilverweise, das muss der Vorlagenprüfer selbst tun. Über
        /// <c>w:name</c> ohne Rücksicht auf Groß- und Kleinschreibung lösen sich alle Rollen
        /// auf: heading 1 → <c>berschrift1</c>, heading 2 → <c>berschrift2</c>, title →
        /// <c>Titel</c>, subtitle → <c>Untertitel</c>, normal → <c>Standard</c>; der
        /// Standardabsatz ebenso über <c>w:default="1"</c>.</para>
        ///
        /// <para><b>Offen:</b> Der Nachweis mit einem ECHTEN, im deutschen Word 365 angelegten
        /// Dokument steht aus — die Stildatei hier ist nachgebaut.</para>
        /// </summary>
        [Fact]
        public void Probe5_Deutsche_Stil_IDs_WordKontext_setzt_Stile_die_es_nicht_gibt_Rollen_loesen_sich_ueber_den_Namen()
        {
            string ordner = TempOrdner();
            try
            {
                string pfad = Path.Combine(ordner, "deutsch.docx");
                using (WordprocessingDocument doc = WordprocessingDocument.Create(pfad, WordprocessingDocumentType.Document))
                {
                    MainDocumentPart main = doc.AddMainDocumentPart();
                    main.Document = new W.Document(new W.Body(new W.SectionProperties()));
                    StyleDefinitionsPart stile = main.AddNewPart<StyleDefinitionsPart>();
                    stile.Styles = new W.Styles(
                        DeutscherStil("Standard", "Normal", null, standard: true),
                        DeutscherStil("berschrift1", "heading 1", 0),
                        DeutscherStil("berschrift2", "heading 2", 1),
                        DeutscherStil("Titel", "Title", null),
                        DeutscherStil("Untertitel", "Subtitle", null));
                }

                using (WordprocessingDocument doc = WordprocessingDocument.Open(pfad, true))
                {
                    MainDocumentPart main = doc.MainDocumentPart;
                    W.Body body = main.Document.Body;
                    var k = new WordKontext(main, body, body.Elements<W.SectionProperties>().Last());
                    k.Titel("Probebericht");
                    k.MitStil("Heading1", "Einleitung");
                    k.Ueberschrift2("Ergebnisse");
                    k.Text("Fließtext");
                    main.Document.Save();
                }

                using (WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false))
                {
                    Assert.Empty(Validierungsfehler(doc));

                    List<W.Style> stile = doc.MainDocumentPart.StyleDefinitionsPart.Styles.Elements<W.Style>().ToList();
                    List<string> gesetzt = doc.MainDocumentPart.Document.Body.Descendants<W.ParagraphStyleId>()
                        .Select(p => p.Val.Value).ToList();
                    Assert.Equal(new[] { "Title", "Heading1", "Heading2", "Normal" }, gesetzt);
                    foreach (string id in gesetzt)
                        Assert.DoesNotContain(stile, s => s.StyleId?.Value == id);

                    string Rolle(string name) => stile.FirstOrDefault(s =>
                        string.Equals(s.StyleName?.Val?.Value, name, StringComparison.OrdinalIgnoreCase))?.StyleId?.Value;

                    Assert.Equal("berschrift1", Rolle("heading 1"));
                    Assert.Equal("berschrift2", Rolle("heading 2"));
                    Assert.Equal("Titel", Rolle("title"));
                    Assert.Equal("Untertitel", Rolle("subtitle"));
                    Assert.Equal("Standard", Rolle("normal"));
                    Assert.Equal("Standard", stile.Single(s =>
                        s.Type?.Value == W.StyleValues.Paragraph && s.Default?.Value == true).StyleId.Value);
                }
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Probe 6 — Diagramm per SDK in eine ClosedXML-Mappe (BV-Q11)
        // =====================================================================

        /// <summary>
        /// PROBE 6 — ein Excel-Diagramm per SDK in eine ClosedXML-Mappe (Anwenderentscheid
        /// BV-Q11 vom 25.09.2026: Excel-Diagramme entstehen als Excel-eigene Diagramme aus den
        /// Zahlen der Tabellen; ClosedXML legt keine Diagramme an, das SDK kann es). Die
        /// entscheidende Probe für die Standardmappe.
        ///
        /// <para><b>Frage:</b> Trägt der Weg — ClosedXML schreibt die Zahlen, das SDK setzt ein
        /// Balkendiagramm (<c>DrawingsPart</c>, <c>ChartPart</c>, <c>TwoCellAnchor</c>) auf den
        /// Bereich —, und übersteht das Diagramm einen weiteren ClosedXML-Lauf, der eine Zahl
        /// ändert? Eine Reihe, Kategorien aus der ersten Spalte.</para>
        ///
        /// <para><b>Gemessen:</b> Ja. Nach dem SDK-Schritt meldet der Validator 0 Fehler. Nach
        /// Laden, Ändern (B3: 20 → 25) und Speichern mit ClosedXML stehen Diagrammteil,
        /// Zeichnungsteil, Anker und das <c>drawing</c>-Element des Blatts unverändert; die
        /// Reihe zeigt weiter auf <c>Daten!$B$2:$B$5</c>, die Kategorien auf
        /// <c>Daten!$A$2:$A$5</c>, der Reihenname auf <c>Daten!$B$1</c>; der Validator meldet
        /// wieder 0 Fehler. Den ZWISCHENSPEICHER der Reihe (<c>numCache</c>) fasst ClosedXML
        /// nicht an: Er zeigt nach dem Lauf weiter 20, die Zelle 25 — wer nur den Speicher
        /// liest, sieht den alten Stand (Cache-Nachtrag: BV-E8).</para>
        /// </summary>
        [Fact]
        public void Probe6_Diagramm_per_SDK_in_einer_ClosedXML_Mappe_uebersteht_den_Rundlauf()
        {
            string[] monate = { "Jan", "Feb", "Mär", "Apr" };
            double[] werte = { 10, 20, 30, 40 };

            string ordner = TempOrdner();
            try
            {
                string pfad = Path.Combine(ordner, "standardmappe.xlsx");
                using (var wb = new XLWorkbook())
                {
                    IXLWorksheet ws = wb.Worksheets.Add("Daten");
                    ws.Cell("A1").Value = "Monat";
                    ws.Cell("B1").Value = "Wärme [MWh]";
                    for (int i = 0; i < monate.Length; i++)
                    {
                        ws.Cell(i + 2, 1).Value = monate[i];
                        ws.Cell(i + 2, 2).Value = werte[i];
                    }
                    wb.SaveAs(pfad);
                }

                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(pfad, true))
                    BalkendiagrammEinfuegen(BlattTeil(doc, "Daten"), "Daten!$B$1", "Wärme [MWh]",
                                            "Daten!$A$2:$A$5", monate, "Daten!$B$2:$B$5", werte);

                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(pfad, false))
                {
                    Assert.Empty(Validierungsfehler(doc));
                    Assert.Single(BlattTeil(doc, "Daten").DrawingsPart.ChartParts);
                }

                using (var wb = new XLWorkbook(pfad))
                {
                    wb.Worksheet("Daten").Cell("B3").Value = 25;
                    wb.Save();
                }

                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(pfad, false))
                {
                    Assert.Empty(Validierungsfehler(doc));
                    WorksheetPart teil = BlattTeil(doc, "Daten");
                    Assert.Single(teil.Worksheet.Elements<S.Drawing>());
                    Assert.NotNull(teil.DrawingsPart);
                    Assert.Single(teil.DrawingsPart.WorksheetDrawing.Elements<Xdr.TwoCellAnchor>());
                    ChartPart diagramm = Assert.Single(teil.DrawingsPart.ChartParts);
                    Assert.Single(diagramm.ChartSpace.Descendants<C.BarChartSeries>());

                    Assert.Equal("Daten!$B$2:$B$5", Reihenformel<C.Values>(diagramm));
                    Assert.Equal("Daten!$A$2:$A$5", Reihenformel<C.CategoryAxisData>(diagramm));
                    Assert.Equal("Daten!$B$1", Reihenformel<C.SeriesText>(diagramm));

                    Assert.Equal("25", Zelle(teil, "B3").CellValue?.Text);
                    Assert.Equal("20", diagramm.ChartSpace.Descendants<C.Values>().Single()
                                              .Descendants<C.NumericPoint>().ElementAt(1).NumericValue.Text);
                }
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Bauhelfer
        // =====================================================================

        /// <summary>Ein PNG von einem Pixel — der Inhalt des Bildes spielt keine Rolle.</summary>
        private const string PNG_1X1 =
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

        /// <summary>
        /// Die Vorlage der Probe 2, gebaut wie Excel sie ablegt: Blatt „Daten“ mit der
        /// Excel-Tabelle <c>Tabelle1</c> (<c>A1:C4</c>; Monat, Wert und die berechnete Spalte
        /// Doppelt, Formeln mit <c>&lt;v&gt;</c>), der Vorlagenformel <c>F1 = SUM(B2:B4)</c> mit
        /// <c>&lt;v&gt;60&lt;/v&gt;</c>, dem mappenweiten Namen <c>EPOS.probe.werte</c>, einem
        /// <c>calcPr</c> ohne <c>fullCalcOnLoad</c> und einem Balkendiagramm auf
        /// <c>B2:B4</c>.
        /// </summary>
        private static void VorlageMitSdkBauen(string pfad)
        {
            using SpreadsheetDocument doc = SpreadsheetDocument.Create(pfad, SpreadsheetDocumentType.Workbook);
            WorkbookPart wbp = doc.AddWorkbookPart();
            wbp.Workbook = new S.Workbook();

            WorkbookStylesPart stile = wbp.AddNewPart<WorkbookStylesPart>();
            stile.Stylesheet = new S.Stylesheet(
                new S.Fonts(new S.Font(new S.FontSize { Val = 11 }, new S.FontName { Val = "Calibri" })) { Count = 1 },
                new S.Fills(new S.Fill(new S.PatternFill { PatternType = S.PatternValues.None }),
                            new S.Fill(new S.PatternFill { PatternType = S.PatternValues.Gray125 })) { Count = 2 },
                new S.Borders(new S.Border(new S.LeftBorder(), new S.RightBorder(), new S.TopBorder(),
                                           new S.BottomBorder(), new S.DiagonalBorder())) { Count = 1 },
                new S.CellStyleFormats(new S.CellFormat { NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0 }) { Count = 1 },
                new S.CellFormats(new S.CellFormat { NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0, FormatId = 0 }) { Count = 1 },
                new S.CellStyles(new S.CellStyle { Name = "Standard", FormatId = 0, BuiltinId = 0 }) { Count = 1 });

            string[] texte = { "Monat", "Wert", "Doppelt", "Jan", "Feb", "Mär", "Summe" };
            SharedStringTablePart sst = wbp.AddNewPart<SharedStringTablePart>();
            sst.SharedStringTable = new S.SharedStringTable(texte.Select(t => new S.SharedStringItem(new S.Text(t))))
            {
                Count = (uint)texte.Length,
                UniqueCount = (uint)texte.Length
            };

            WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();
            wsp.Worksheet = new S.Worksheet(
                new S.SheetData(
                    new S.Row(Text("A1", 0), Text("B1", 1), Text("C1", 2), Text("E1", 6), Formel("F1", "SUM(B2:B4)", 60)) { RowIndex = 1 },
                    new S.Row(Text("A2", 3), Zahl("B2", 10), Formel("C2", FORMEL_DOPPELT, 20)) { RowIndex = 2 },
                    new S.Row(Text("A3", 4), Zahl("B3", 20), Formel("C3", FORMEL_DOPPELT, 40)) { RowIndex = 3 },
                    new S.Row(Text("A4", 5), Zahl("B4", 30), Formel("C4", FORMEL_DOPPELT, 60)) { RowIndex = 4 }),
                new S.PageMargins { Left = 0.7, Right = 0.7, Top = 0.75, Bottom = 0.75, Header = 0.3, Footer = 0.3 });

            TableDefinitionPart tdp = wsp.AddNewPart<TableDefinitionPart>();
            tdp.Table = new S.Table(
                new S.AutoFilter { Reference = "A1:C4" },
                new S.TableColumns(
                    new S.TableColumn { Id = 1, Name = "Monat" },
                    new S.TableColumn { Id = 2, Name = "Wert" },
                    new S.TableColumn(new S.CalculatedColumnFormula(FORMEL_DOPPELT)) { Id = 3, Name = "Doppelt" })
                { Count = 3 },
                new S.TableStyleInfo
                {
                    Name = "TableStyleMedium2", ShowFirstColumn = false, ShowLastColumn = false,
                    ShowRowStripes = true, ShowColumnStripes = false
                })
            { Id = 1, Name = "Tabelle1", DisplayName = "Tabelle1", Reference = "A1:C4", TotalsRowShown = false };
            wsp.Worksheet.Append(new S.TableParts(new S.TablePart { Id = wsp.GetIdOfPart(tdp) }) { Count = 1 });
            wsp.Worksheet.Save();

            wbp.Workbook.Append(new S.Sheets(new S.Sheet { Name = "Daten", SheetId = 1, Id = wbp.GetIdOfPart(wsp) }));
            wbp.Workbook.Append(new S.DefinedNames(new S.DefinedName("Daten!$B$2:$B$4") { Name = "EPOS.probe.werte" }));
            wbp.Workbook.Append(new S.CalculationProperties { CalculationId = 191029 });
            wbp.Workbook.Save();

            BalkendiagrammEinfuegen(wsp, "Daten!$B$1", "Wert", "Daten!$A$2:$A$4", new[] { "Jan", "Feb", "Mär" },
                                    "Daten!$B$2:$B$4", new double[] { 10, 20, 30 });
        }

        private static S.Cell Text(string adresse, int index) =>
            new S.Cell { CellReference = adresse, DataType = S.CellValues.SharedString, CellValue = new S.CellValue(index) };

        private static S.Cell Zahl(string adresse, double wert) =>
            new S.Cell { CellReference = adresse, CellValue = new S.CellValue(wert) };

        private static S.Cell Formel(string adresse, string formel, double gespeichert) =>
            new S.Cell { CellReference = adresse, CellFormula = new S.CellFormula(formel), CellValue = new S.CellValue(gespeichert) };

        /// <summary>
        /// Setzt ein Säulendiagramm (gruppiert, eine Reihe) mit Zwischenspeichern auf das
        /// Blatt: <c>ChartPart</c> im <c>DrawingsPart</c>, verankert über
        /// <c>TwoCellAnchor</c> (D2:K17), das <c>drawing</c>-Element an seiner Schemastelle
        /// im Blatt. Genau so viel, wie Excel für ein gültiges Diagramm braucht.
        /// </summary>
        private static void BalkendiagrammEinfuegen(WorksheetPart wsp, string namensFormel, string reihenname,
                                                    string kategorienFormel, string[] kategorien,
                                                    string werteFormel, double[] werte)
        {
            const uint KATEGORIEACHSE = 500000001u;
            const uint WERTEACHSE = 500000002u;

            DrawingsPart zeichnung = wsp.DrawingsPart ?? wsp.AddNewPart<DrawingsPart>();
            if (zeichnung.WorksheetDrawing == null) zeichnung.WorksheetDrawing = new Xdr.WorksheetDrawing();
            ChartPart teil = zeichnung.AddNewPart<ChartPart>();

            var zahlen = new C.NumberingCache(new C.FormatCode("General"), new C.PointCount { Val = (uint)werte.Length });
            for (int i = 0; i < werte.Length; i++)
                zahlen.Append(new C.NumericPoint(new C.NumericValue(werte[i].ToString(CultureInfo.InvariantCulture))) { Index = (uint)i });

            var saeulen = new C.BarChart(
                new C.BarDirection { Val = C.BarDirectionValues.Column },
                new C.BarGrouping { Val = C.BarGroupingValues.Clustered },
                new C.VaryColors { Val = false },
                new C.BarChartSeries(
                    new C.Index { Val = 0u },
                    new C.Order { Val = 0u },
                    new C.SeriesText(new C.StringReference(new C.Formula(namensFormel), Textspeicher(new[] { reihenname }))),
                    new C.InvertIfNegative { Val = false },
                    new C.CategoryAxisData(new C.StringReference(new C.Formula(kategorienFormel), Textspeicher(kategorien))),
                    new C.Values(new C.NumberReference(new C.Formula(werteFormel), zahlen))),
                new C.GapWidth { Val = (UInt16Value)150 },
                new C.AxisId { Val = KATEGORIEACHSE },
                new C.AxisId { Val = WERTEACHSE });

            var flaeche = new C.PlotArea(
                new C.Layout(),
                saeulen,
                new C.CategoryAxis(
                    new C.AxisId { Val = KATEGORIEACHSE },
                    new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax }),
                    new C.Delete { Val = false },
                    new C.AxisPosition { Val = C.AxisPositionValues.Bottom },
                    new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
                    new C.CrossingAxis { Val = WERTEACHSE },
                    new C.Crosses { Val = C.CrossesValues.AutoZero },
                    new C.AutoLabeled { Val = true },
                    new C.LabelAlignment { Val = C.LabelAlignmentValues.Center },
                    new C.LabelOffset { Val = (UInt16Value)100 }),
                new C.ValueAxis(
                    new C.AxisId { Val = WERTEACHSE },
                    new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax }),
                    new C.Delete { Val = false },
                    new C.AxisPosition { Val = C.AxisPositionValues.Left },
                    new C.MajorGridlines(),
                    new C.NumberingFormat { FormatCode = "General", SourceLinked = true },
                    new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
                    new C.CrossingAxis { Val = KATEGORIEACHSE },
                    new C.Crosses { Val = C.CrossesValues.AutoZero },
                    new C.CrossBetween { Val = C.CrossBetweenValues.Between }));

            var raum = new C.ChartSpace(
                new C.EditingLanguage { Val = "de-DE" },
                new C.RoundedCorners { Val = false },
                new C.Chart(
                    new C.AutoTitleDeleted { Val = true },
                    flaeche,
                    new C.PlotVisibleOnly { Val = true },
                    new C.DisplayBlanksAs { Val = C.DisplayBlanksAsValues.Gap }));
            raum.AddNamespaceDeclaration("c", "http://schemas.openxmlformats.org/drawingml/2006/chart");
            raum.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
            raum.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            teil.ChartSpace = raum;

            uint kennung = 2;
            foreach (Xdr.NonVisualDrawingProperties nv in zeichnung.WorksheetDrawing.Descendants<Xdr.NonVisualDrawingProperties>())
                if (nv.Id != null && nv.Id.Value >= kennung) kennung = nv.Id.Value + 1;

            zeichnung.WorksheetDrawing.Append(new Xdr.TwoCellAnchor(
                new Xdr.FromMarker(new Xdr.ColumnId("3"), new Xdr.ColumnOffset("0"), new Xdr.RowId("1"), new Xdr.RowOffset("0")),
                new Xdr.ToMarker(new Xdr.ColumnId("10"), new Xdr.ColumnOffset("0"), new Xdr.RowId("16"), new Xdr.RowOffset("0")),
                new Xdr.GraphicFrame(
                    new Xdr.NonVisualGraphicFrameProperties(
                        new Xdr.NonVisualDrawingProperties { Id = kennung, Name = "Diagramm " + (kennung - 1) },
                        new Xdr.NonVisualGraphicFrameDrawingProperties()),
                    new Xdr.Transform(new A.Offset { X = 0L, Y = 0L }, new A.Extents { Cx = 0L, Cy = 0L }),
                    new A.Graphic(new A.GraphicData(new C.ChartReference { Id = zeichnung.GetIdOfPart(teil) })
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart" }))
                { Macro = "" },
                new Xdr.ClientData()));
            zeichnung.WorksheetDrawing.Save();

            // Das drawing-Element steht im Blatt VOR legacyDrawing … tableParts, extLst
            // (Reihenfolge von CT_Worksheet).
            S.Worksheet blatt = wsp.Worksheet;
            if (!blatt.Elements<S.Drawing>().Any())
            {
                var verweis = new S.Drawing { Id = wsp.GetIdOfPart(zeichnung) };
                OpenXmlElement danach = blatt.ChildElements.FirstOrDefault(e =>
                    e is S.LegacyDrawing || e is S.LegacyDrawingHeaderFooter || e is S.DrawingHeaderFooter ||
                    e is S.Picture || e is S.OleObjects || e is S.Controls || e is S.WebPublishItems ||
                    e is S.TableParts || e is S.WorksheetExtensionList);
                if (danach != null) blatt.InsertBefore(verweis, danach);
                else blatt.Append(verweis);
            }
            blatt.Save();
        }

        private static C.StringCache Textspeicher(string[] texte)
        {
            var speicher = new C.StringCache(new C.PointCount { Val = (uint)texte.Length });
            for (int i = 0; i < texte.Length; i++)
                speicher.Append(new C.StringPoint(new C.NumericValue(texte[i])) { Index = (uint)i });
            return speicher;
        }

        /// <summary>Ein Absatzstil, wie ihn das deutsche Word ablegt: übersetzte ID, englischer Name.</summary>
        private static W.Style DeutscherStil(string id, string name, int? gliederungsebene, bool standard = false)
        {
            var stil = new W.Style { Type = W.StyleValues.Paragraph, StyleId = id };
            if (standard) stil.Default = true;
            stil.Append(new W.StyleName { Val = name });
            if (!standard) stil.Append(new W.BasedOn { Val = "Standard" });
            stil.Append(new W.PrimaryStyle());
            if (gliederungsebene != null)
                stil.Append(new W.StyleParagraphProperties(new W.OutlineLevel { Val = gliederungsebene.Value }));
            return stil;
        }

        // =====================================================================
        //  Lesehelfer
        // =====================================================================

        private static WorksheetPart BlattTeil(SpreadsheetDocument doc, string name)
        {
            S.Sheet blatt = doc.WorkbookPart.Workbook.Sheets.Elements<S.Sheet>().Single(s => s.Name == name);
            return (WorksheetPart)doc.WorkbookPart.GetPartById(blatt.Id);
        }

        private static S.Cell Zelle(WorksheetPart teil, string adresse) =>
            teil.Worksheet.Descendants<S.Cell>().FirstOrDefault(c => c.CellReference?.Value == adresse);

        private static S.Table Tabelle(WorksheetPart teil) => teil.TableDefinitionParts.Single().Table;

        private static S.TableColumn Spalte(WorksheetPart teil, string name) =>
            Tabelle(teil).TableColumns.Elements<S.TableColumn>().Single(c => c.Name == name);

        /// <summary>Die Formel unter dem Reihenteil <typeparamref name="T"/> der einzigen Reihe.</summary>
        private static string Reihenformel<T>(ChartPart teil) where T : OpenXmlElement =>
            teil.ChartSpace.Descendants<C.BarChartSeries>().Single().GetFirstChild<T>()
                .Descendants<C.Formula>().Single().Text;

        /// <summary>Die Befunde des Validators (Office 2016) als lesbare Zeilen.</summary>
        private static List<string> Validierungsfehler(OpenXmlPackage doc) =>
            new OpenXmlValidator(FileFormatVersions.Office2016).Validate(doc)
                .Select(f => f.Description + " @ " + f.Path?.XPath + " (" + f.Part?.Uri + ")")
                .ToList();

        private static string TempOrdner()
        {
            string ordner = Path.Combine(Path.GetTempPath(),
                                         "epos-bv-e0-messprobe-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            return ordner;
        }

        private static void Aufraeumen(string ordner)
        {
            try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }
    }
}
