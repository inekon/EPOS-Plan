using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Berichtsvorlage
{
    /// <summary>
    /// Die Prüfungen, die beide Modi vor dem Schreiben ziehen: der
    /// <see cref="OpenXmlValidator"/> in jeder Fassung von Office 2007 bis 2021 (dieselbe
    /// Liste wie <c>EPOS.Kern.Tests/WordBerichtSvgWacheTests.cs</c>) und die Stilregeln der
    /// Vorlage.
    /// </summary>
    internal static class Pruefung
    {
        /// <summary>Office 2007 bis 2021 — jede Fassung, die der Validator bis dahin kennt.</summary>
        internal static readonly FileFormatVersions[] Fassungen =
        {
            FileFormatVersions.Office2007, FileFormatVersions.Office2010,
            FileFormatVersions.Office2013, FileFormatVersions.Office2016,
            FileFormatVersions.Office2019, FileFormatVersions.Office2021
        };

        /// <summary>
        /// Die acht Stil-IDs, die der <c>WordBerichtGenerator</c> über
        /// <c>WordKontext.MitStil</c> anspricht (<c>Titel</c>, <c>Untertitel</c>,
        /// <c>Ueberschrift1–3</c>, <c>Text</c>, <c>Hinweis</c>, <c>Beschriftung</c>).
        /// </summary>
        internal static readonly string[] Pflichtstile =
        {
            "Title", "Subtitle", "Heading1", "Heading2", "Heading3", "Normal", "Hinweis", "Beschriftung"
        };

        /// <summary>
        /// Die Überschriften, die das Inhaltsverzeichnis des Berichts sammelt
        /// (<c>TOC \o "1-3"</c> in <c>WordKontext.TocFeld</c>) — sie müssen eine
        /// Gliederungsebene tragen.
        /// </summary>
        internal static readonly string[] Gliederungsstile = { "Heading1", "Heading2", "Heading3" };

        /// <summary>
        /// Das Absatzformat der Kapitelüberschriften einer Vorlage (Konzept 5.3, 6.2, Anhang B.3):
        /// eine Überschrift in diesem Format unmittelbar vor einem Kapitelplatzhalter entfällt
        /// mit dem Kapitel. Auf Heading1 aufgebaut, Gliederungsebene 1 (<c>w:outlineLvl 0</c>),
        /// damit das Inhaltsverzeichnis (<c>TOC \o "1-3" \u</c>) sie sammelt.
        /// </summary>
        internal const string KAPITELKOPF_ID = "EPOSKapitelkopf";
        internal const string KAPITELKOPF_NAME = "EPOS Kapitelkopf";

        /// <summary>Validiert in jeder Fassung, schreibt den Befund und gibt die Fehlersumme zurück.</summary>
        internal static int Validieren(string pfad, TextWriter aus) => Validieren(pfad, aus, false);

        /// <summary>
        /// Wie <see cref="Validieren(string, TextWriter)"/>; mit <paramref name="mustertabelle"/> zählt in Office 2007 der
        /// Befund zum Alternativtext einer Tabelle (<c>w:tblDescription</c>, Word kennt ihn erst ab 2010) nicht — nur er,
        /// und nur, wenn er <c>{{muster.tabelle}}</c> trägt (<see cref="IstAusnahmeMustertabelle"/>). Die Engine liest
        /// die Mustertabelle über diesen Alternativtext und entfernt sie; der Bericht bleibt in jeder Fassung gültig.
        /// </summary>
        internal static int Validieren(string pfad, TextWriter aus, bool mustertabelle)
        {
            int summe = 0;
            using (WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false))
            {
                foreach (FileFormatVersions fassung in Fassungen)
                {
                    List<ValidationErrorInfo> alle = new OpenXmlValidator(fassung).Validate(doc).ToList();
                    List<ValidationErrorInfo> fehler = alle.Where(f => !(mustertabelle && IstAusnahmeMustertabelle(fassung, f))).ToList();
                    summe += fehler.Count;
                    aus.WriteLine("    " + fassung.ToString().PadRight(11) + " " + fehler.Count + " Fehler"
                                  + (alle.Count > fehler.Count ? " (dazu " + (alle.Count - fehler.Count) + "× Alternativtext der Mustertabelle, erst ab Word 2010)" : ""));
                    foreach (ValidationErrorInfo f in fehler.Take(5))
                        aus.WriteLine("      - " + f.Description + " @ " + f.Part?.Uri + " " + f.Path?.XPath);
                }
            }
            return summe;
        }

        /// <summary>
        /// Der eine Befund, den eine Vorlage mit Mustertabelle in Office 2007 trägt: <c>w:tblDescription</c> als
        /// unerwartetes Kind von <c>w:tblPr</c>, und die Tabelle trägt den Alternativtext <c>{{muster.tabelle}}</c>.
        /// </summary>
        internal static bool IstAusnahmeMustertabelle(FileFormatVersions fassung, ValidationErrorInfo f)
        {
            if (fassung != FileFormatVersions.Office2007) return false;
            if (!(f.Node is TableProperties tp)) return false;
            if (!(f.Description ?? "").Contains(":tblDescription", StringComparison.Ordinal)) return false;
            return tp.GetFirstChild<TableDescription>()?.Val?.Value == "{{muster.tabelle}}";
        }

        /// <summary>
        /// Die Stilregeln der Vorlage: keine doppelte ID, alle Pflichtstile vorhanden,
        /// Überschrift 1–3 mit <c>w:outlineLvl</c>, das Absatzformat „EPOS Kapitelkopf“
        /// vorhanden und wie festgelegt. Leere Liste = in Ordnung.
        /// </summary>
        internal static List<string> Stilbefunde(Styles stile)
        {
            var befunde = new List<string>();
            if (stile == null)
            {
                befunde.Add("Die Datei hat keinen Stilteil (word/styles.xml).");
                return befunde;
            }

            foreach (List<Style> gruppe in Stilbereinigung.DoppelteGruppen(stile))
                befunde.Add("Stil-ID „" + gruppe[0].StyleId.Value + "“ ist " + gruppe.Count + "-mal definiert.");

            foreach (string id in Pflichtstile)
            {
                Style s = Finde(stile, id);
                if (s == null) { befunde.Add("Pflichtstil „" + id + "“ fehlt."); continue; }
                if (Gliederungsstile.Contains(id) && s.StyleParagraphProperties?.OutlineLevel == null)
                    befunde.Add("Überschriftenstil „" + id + "“ trägt keine Gliederungsebene (w:outlineLvl).");
            }

            Style kopf = Finde(stile, KAPITELKOPF_ID);
            if (kopf == null)
                befunde.Add("Absatzformat „" + KAPITELKOPF_NAME + "“ (" + KAPITELKOPF_ID + ") fehlt — „bereinigen“ ergänzt es.");
            else if (kopf.StyleName?.Val?.Value != KAPITELKOPF_NAME || kopf.BasedOn?.Val?.Value != "Heading1"
                     || kopf.StyleParagraphProperties?.OutlineLevel?.Val?.Value != 0)
                befunde.Add("Absatzformat „" + KAPITELKOPF_NAME + "“ weicht ab (Name, basedOn Heading1 oder Gliederungsebene 1).");
            return befunde;
        }

        internal static Style Finde(Styles stile, string id)
            => stile.Elements<Style>().FirstOrDefault(s => s.StyleId?.Value == id);
    }
}
