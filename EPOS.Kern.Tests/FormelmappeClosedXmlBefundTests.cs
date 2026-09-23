using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E8b (Vorarbeit der Formelmappe, Konzept § 2.11.6 „Vor Stufe 0 zu klären") —
    /// der <b>Befund zu ClosedXML</b>, als Wache festgehalten.
    ///
    /// <para><b>Gemessen am 23.09.2026 mit ClosedXML 0.105.1:</b></para>
    /// <list type="number">
    ///   <item><description>Eine Formel wird OHNE zwischengespeichertes Ergebnis abgelegt —
    ///     die Zelle trägt <c>&lt;f&gt;</c>, aber kein <c>&lt;v&gt;</c>. Das gilt auch
    ///     nach <c>RecalculateAllFormulas()</c>: Die Rechnung bleibt im Speicher, die Datei
    ///     sieht sie nicht. Nur <c>SaveOptions.EvaluateFormulasBeforeSaving</c> schreibt
    ///     Ergebnisse — mit 15 Stellen und mit den Lücken der eigenen Rechenmaschine.</description></item>
    ///   <item><description>Die Rechenmaschine von ClosedXML kennt die Finanzfunktionen der
    ///     Formelmappe nicht: <c>NPV</c> (NBW), <c>PMT</c> (RMZ) und <c>IRR</c> (IKV) ergeben
    ///     <c>#NAME?</c>. Grundrechenarten, <c>SUM</c>, Potenzen, <c>IF</c>/<c>COUNTIF</c>,
    ///     <c>MIN</c>, <c>ROUND</c> rechnet sie.</description></item>
    /// </list>
    ///
    /// <para><b>Der Entscheid daraus</b> (<c>Formelmappe</c>): Die Formeln schreibt
    /// ClosedXML, das ERGEBNIS jeder Formelzelle trägt EPOS nach dem Speichern selbst ein —
    /// die Zahl des Rechenkerns, mit voller Stellenzahl —, und die Mappe verlangt beim
    /// Öffnen die volle Neuberechnung (<c>fullCalcOnLoad</c>). Excel (gemessen: Microsoft
    /// 365, Version 16) rechnet damit beim Öffnen selbst und kommt auf dieselben Werte;
    /// ein Betrachter ohne Rechenmaschine zeigt die eingetragenen Zahlen statt leerer
    /// Zellen.</para>
    ///
    /// <para><b>Warum eine Wache:</b> Ändert eine neue ClosedXML-Fassung eines der beiden
    /// Verhalten, wird dieser Fall rot — dann ist der Entscheid neu zu prüfen (etwa ob der
    /// Nachtrag der Ergebnisse entfallen kann). Die Fälle brauchen keine Datenbank.</para>
    /// </summary>
    public class FormelmappeClosedXmlBefundTests
    {
        [Fact]
        public void ClosedXML_legt_Formeln_ohne_zwischengespeichertes_Ergebnis_ab()
        {
            string ordner = TempOrdner();
            try
            {
                string ohne = Path.Combine(ordner, "ohne.xlsx");
                string mit = Path.Combine(ordner, "mit_recalc.xlsx");
                using (XLWorkbook wb = Probe()) wb.SaveAs(ohne);
                using (XLWorkbook wb = Probe())
                {
                    wb.RecalculateAllFormulas();
                    wb.SaveAs(mit);
                }

                foreach (string datei in new[] { ohne, mit })
                {
                    Cell zelle = Formelzelle(datei, "C1");
                    Assert.NotNull(zelle.CellFormula);
                    Assert.True(zelle.CellValue == null,
                        Path.GetFileName(datei) + ": ClosedXML legt jetzt ein Ergebnis ab — " +
                        "Entscheid der Formelmappe (Ergebnisse selbst nachtragen) prüfen.");
                }
            }
            finally { Aufraeumen(ordner); }
        }

        [Fact]
        public void ClosedXML_rechnet_NPV_PMT_und_IRR_nicht()
        {
            using XLWorkbook wb = Probe();
            wb.RecalculateAllFormulas();
            IXLWorksheet ws = wb.Worksheet(1);

            // Was sie rechnet: Summe und Potenz.
            Assert.Equal(XLDataType.Number, ws.Cell("C1").Value.Type);
            Assert.Equal(-90000.0 + 30000.0 + 40000.0 + 50000.0, ws.Cell("C1").Value.GetNumber(), 6);
            Assert.Equal(Math.Pow(1.05, -2), ws.Cell("C2").Value.GetNumber(), 12);

            // Was sie nicht rechnet: die drei Finanzfunktionen der Formelmappe.
            foreach (string adresse in new[] { "C3", "C4", "C5" })
                Assert.True(ws.Cell(adresse).Value.Type == XLDataType.Error,
                    adresse + " (" + ws.Cell(adresse).FormulaA1 + "): ClosedXML rechnet die Funktion " +
                    "jetzt — Entscheid der Formelmappe prüfen.");
        }

        // =====================================================================

        private static XLWorkbook Probe()
        {
            var wb = new XLWorkbook();
            IXLWorksheet ws = wb.Worksheets.Add("Probe");
            ws.Cell("A1").Value = 0.05;
            ws.Cell("B1").Value = -90000.0;
            ws.Cell("B2").Value = 30000.0;
            ws.Cell("B3").Value = 40000.0;
            ws.Cell("B4").Value = 50000.0;
            ws.Cell("C1").FormulaA1 = "SUM(B1:B4)";
            ws.Cell("C2").FormulaA1 = "(1+A1)^-2";
            ws.Cell("C3").FormulaA1 = "NPV(A1,B2:B4)+B1";
            ws.Cell("C4").FormulaA1 = "PMT(A1,3,90000)";
            ws.Cell("C5").FormulaA1 = "IRR(B1:B4)";
            return wb;
        }

        private static Cell Formelzelle(string datei, string adresse)
        {
            using SpreadsheetDocument doc = SpreadsheetDocument.Open(datei, false);
            WorksheetPart teil = doc.WorkbookPart.WorksheetParts.First();
            Cell zelle = teil.Worksheet.Descendants<Cell>().First(c => c.CellReference != null && c.CellReference.Value == adresse);
            return (Cell)zelle.CloneNode(true);
        }

        private static string TempOrdner()
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-e8b-befund-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        private static void Aufraeumen(string ordner)
        {
            try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }
    }
}
