using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Baut Excel-Vorlagen für die Tests der Etappe BV-E7 (Konzept Berichtsvorlagen 7) — mit ClosedXML, nachbearbeitet über
    /// das SDK, wo ClosedXML es nicht kann (Dokumenttyp <c>.xltx</c>/<c>.xlsm</c>, Standardschrift der Mappe, Formen).
    /// Alles im Speicher.
    /// </summary>
    internal static class Excelprobe
    {
        /// <summary>Eine Mappe aus <paramref name="bau"/> als Bytes.</summary>
        internal static byte[] Mappe(Action<XLWorkbook> bau)
        {
            using (var wb = new XLWorkbook())
            {
                bau(wb);
                if (wb.Worksheets.Count == 0) wb.Worksheets.Add("Leer");
                using (var strom = new MemoryStream())
                {
                    wb.SaveAs(strom);
                    return strom.ToArray();
                }
            }
        }

        /// <summary>Dieselben Bytes über das SDK nachbearbeitet.</summary>
        internal static byte[] Bearbeite(byte[] mappe, Action<SpreadsheetDocument> bearbeite)
        {
            var strom = new MemoryStream();
            strom.Write(mappe, 0, mappe.Length);
            strom.Position = 0;
            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(strom, true)) bearbeite(doc);
            return strom.ToArray();
        }

        /// <summary>Als Excel-Vorlage (<c>.xltx</c>, Inhaltstyp template.main).</summary>
        internal static byte[] AlsVorlage(byte[] mappe)
        {
            return Bearbeite(mappe, d => d.ChangeDocumentType(SpreadsheetDocumentType.Template));
        }

        /// <summary>Als Mappe mit Makros (<c>.xlsm</c>).</summary>
        internal static byte[] MitMakros(byte[] mappe)
        {
            return Bearbeite(mappe, d => d.ChangeDocumentType(SpreadsheetDocumentType.MacroEnabledWorkbook));
        }

        /// <summary>Setzt die Standardschrift der Mappe (Schrift 0 und Formatvorlage „Standard“) — etwa Aptos Narrow wie Office 365.</summary>
        internal static byte[] MitStandardschrift(byte[] mappe, string schrift)
        {
            return Bearbeite(mappe, d =>
            {
                Stylesheet stile = d.WorkbookPart.WorkbookStylesPart.Stylesheet;
                foreach (Font f in stile.Fonts.Elements<Font>())
                {
                    FontName name = f.GetFirstChild<FontName>();
                    if (name != null) name.Val = schrift;
                }
                stile.Save();
            });
        }

        /// <summary>Hängt an das erste Blatt eine Zeichnung mit einer Form (<c>xdr:sp</c>) — ClosedXML verliert sie.</summary>
        internal static byte[] MitForm(byte[] mappe)
        {
            return Bearbeite(mappe, d =>
            {
                WorksheetPart blatt = d.WorkbookPart.WorksheetParts.First();
                DrawingsPart zeichnung = blatt.AddNewPart<DrawingsPart>();
                using (var s = new StreamWriter(zeichnung.GetStream(FileMode.Create)))
                    s.Write("<xdr:wsDr xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\" " +
                            "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\">" +
                            "<xdr:twoCellAnchor><xdr:from><xdr:col>3</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>3</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:from>" +
                            "<xdr:to><xdr:col>6</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>8</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:to>" +
                            "<xdr:sp macro=\"\" textlink=\"\"><xdr:nvSpPr><xdr:cNvPr id=\"2\" name=\"Pfeil 1\"/><xdr:cNvSpPr/></xdr:nvSpPr>" +
                            "<xdr:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"1000000\" cy=\"500000\"/></a:xfrm>" +
                            "<a:prstGeom prst=\"rightArrow\"><a:avLst/></a:prstGeom></xdr:spPr></xdr:sp><xdr:clientData/></xdr:twoCellAnchor></xdr:wsDr>");
                string id = blatt.GetIdOfPart(zeichnung);
                Worksheet ws = blatt.Worksheet;
                var drawing = new Drawing { Id = id };
                // Schemafolge: drawing steht hinter pageMargins/pageSetup/headerFooter, vor legacyDrawing/tableParts/extLst.
                OpenXmlElement danach = ws.Elements<LegacyDrawing>().Cast<OpenXmlElement>()
                    .Concat(ws.Elements<TableParts>()).Concat(ws.Elements<WorksheetExtensionList>()).FirstOrDefault();
                if (danach != null) ws.InsertBefore(drawing, danach);
                else ws.Append(drawing);
                ws.Save();
            });
        }

        /// <summary>Setzt eine Dokumenteigenschaft in <c>custom.xml</c> (etwa <c>EPOS.Sprache</c>).</summary>
        internal static byte[] MitEigenschaft(byte[] mappe, string name, string wert)
        {
            return Bearbeite(mappe, d =>
            {
                CustomFilePropertiesPart teil = d.CustomFilePropertiesPart ?? d.AddCustomFilePropertiesPart();
                teil.Properties = new DocumentFormat.OpenXml.CustomProperties.Properties();
                var p = new DocumentFormat.OpenXml.CustomProperties.CustomDocumentProperty
                {
                    FormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}", PropertyId = 2, Name = name,
                };
                p.Append(new DocumentFormat.OpenXml.VariantTypes.VTLPWSTR(wert));
                teil.Properties.Append(p);
                teil.Properties.Save();
            });
        }

        /// <summary>Die Zahl der Formen in allen Zeichnungen einer Datei.</summary>
        internal static int Formen(string pfad)
        {
            using SpreadsheetDocument doc = SpreadsheetDocument.Open(pfad, false);
            return doc.WorkbookPart.WorksheetParts.Where(w => w.DrawingsPart != null)
                      .Sum(w => w.DrawingsPart.RootElement.Descendants().Count(e => e.LocalName == "sp"));
        }

        /// <summary>Steht <c>fullCalcOnLoad</c> in der Mappe?</summary>
        internal static bool VolleNeuberechnung(string pfad)
        {
            using SpreadsheetDocument doc = SpreadsheetDocument.Open(pfad, false);
            CalculationProperties calc = doc.WorkbookPart.Workbook.GetFirstChild<CalculationProperties>();
            return calc?.FullCalculationOnLoad?.Value == true;
        }

        /// <summary>Der Dokumenttyp einer Datei.</summary>
        internal static SpreadsheetDocumentType Typ(string pfad)
        {
            using SpreadsheetDocument doc = SpreadsheetDocument.Open(pfad, false);
            return doc.DocumentType;
        }
    }
}
