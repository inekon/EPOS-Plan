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
    /// Konfigurationselementen“, Nachtrag „die detaillierten Komponenten, damit der Benutzer die Vorlage ändern kann“;
    /// Konzept Berichtsvorlagen 4.4, 7.1–7.4) — eine mitgelieferte Mappe je Sprache (<c>Berichtsvorlage_Excel_Ausfuehrlich.xlsx</c>,
    /// <c>…_en.xlsx</c>), die jedes Konfigurationselement einer Excel-Vorlage zeigt und jedes erzeugte Blatt des
    /// Standardberichts, das sich aus Einzelelementen bauen lässt, so nachbildet — jedes Element mit einer Zellnotiz:
    /// <list type="bullet">
    /// <item>Deckblatt: Zellplatzhalter jeder Art (Text, Zahl, Datum, Liste, Prozent in einer Prozentzelle, Parameter als Anteil,
    /// <c>|mit grund</c>, <c>|stellen n</c>, Text im Satz), Namen <c>EPOS.*</c> und <c>EPOS_*</c> auf eine Zelle und als
    /// Konstante, zwei Formeln — auf einen reservierten Parameternamen und auf eine eigene Zelle (<c>FullCalculationOnLoad</c>);</item>
    /// <item>„Projektübersicht“ (Blatt Übersicht): Titel, Kopfwerte, <c>tabelle.varianten</c>, <c>tabelle.komponenten.matrix</c>,
    /// Diagramm <c>stamm.bild.speichertemperaturen</c>;</item>
    /// <item>„Variantenvergleich“ (Blatt Vergleich): <c>tabelle.vergleich.liste</c> und die vier Vergleichsbalken;</item>
    /// <item>die Blattmarke der Formelmappe <c>blatt.wirtschaftlichkeit</c> — lebende Formeln und eine datenabhängige
    /// Gegenrechnung lassen sich nicht aus Einzelelementen bauen; daneben „Auswertung“ mit ihren Tafeln und vier Diagrammen
    /// als Werte;</item>
    /// <item>„Kapitalwertverlauf“ (Blatt Verlauf): <c>tabelle.wirtschaft.verlauf</c>;</item>
    /// <item>das Musterblatt <c>blatt.detail</c> (Detailblatt je Stand): Kopf, <c>stand.tabelle.kennzahlen.liste</c>, Erzeuger,
    /// Brennstoffmengen, die Tafeln der Wirtschaftlichkeit je Stand, die Excel-Tabelle <c>EPOS_stand__tabelle__monatswerte</c>
    /// und sieben Diagramme je Stand (die vier des Detailblatts, die Deckungskreise des Vergleichs, der Zahlungsstrom);</item>
    /// <item>die Blattmarken <c>blatt.checkliste</c> (die Stellen der Checkliste entstehen erst aus der gefüllten Mappe) und
    /// <c>blatt.diagrammdaten</c> (die Zahlen aller Diagramme);</item>
    /// <item>„Eigene Diagramme“: eine Excel-Tabelle <c>EPOS_tabelle__wirtschaft__szenarien</c> mit einem eigenen
    /// Säulendiagramm darauf und ein eigenes Liniendiagramm auf den Namen <c>EPOS.reihe.*</c>.</item>
    /// </list>
    /// Die Mappe trägt <c>EPOS.Blattanhang</c> = <c>nein</c> (<see cref="ExcelVorlagenmappe.EIGENSCHAFT_BLATTANHANG"/>): Die
    /// nachgebildeten Blätter entstehen nicht zusätzlich. Erzeugt vom Werkzeug <c>Werkzeuge/Berichtsvorlage</c> (Modus
    /// <c>excel-ausfuehrlich</c>, Sammelbefehl <c>alle</c>); die Aktualitätswache vergleicht die mitgelieferte Datei mit einer
    /// frisch erzeugten (<c>BerichtsvorlagenCtrl.Inhaltsschluessel</c>).
    /// </summary>
    public static class ExcelAusfuehrlich
    {
        /// <summary>Die Art der Vorlage in <c>EPOS.Vorlage</c>.</summary>
        public const string VORLAGENART = "ausfuehrlich-excel";

        /// <summary>Die Blattmarken, die die Vorlage behält (Namen der Marken sind sprachneutral).</summary>
        internal const string BLATT_WIRTSCHAFT = "blatt.wirtschaftlichkeit", BLATT_DETAIL = "blatt.detail",
                              BLATT_CHECKLISTE = "blatt.checkliste", BLATT_DIAGRAMMDATEN = "blatt.diagrammdaten";

        /// <summary>
        /// Die erzeugten Blätter des Standardberichts und ihre Entsprechung in dieser Vorlage: ein nachgebildetes Blatt (Ressource
        /// seines Namens) oder die Blattmarke, die bleibt.
        /// </summary>
        internal static readonly IReadOnlyList<(ExcelBerichtGenerator.Blattart Art, string Blattressource, string Marke)> Entsprechungen = new[]
        {
            (ExcelBerichtGenerator.Blattart.Uebersicht, nameof(R.BV_XLA_BLATT_UEBERSICHT), (string)null),
            (ExcelBerichtGenerator.Blattart.Vergleich, nameof(R.BV_XLA_BLATT_VERGLEICH), null),
            (ExcelBerichtGenerator.Blattart.Wirtschaftlichkeit, null, BLATT_WIRTSCHAFT),
            (ExcelBerichtGenerator.Blattart.Verlauf, nameof(R.BV_XLA_BLATT_VERLAUF), null),
            (ExcelBerichtGenerator.Blattart.Detail, null, BLATT_DETAIL),
            (ExcelBerichtGenerator.Blattart.Checkliste, null, BLATT_CHECKLISTE),
            (ExcelBerichtGenerator.Blattart.Diagrammdaten, null, BLATT_DIAGRAMMDATEN),
        };

        /// <summary>Die Diagramme des Detailblatts (wie <c>ExcelBerichtGenerator.PlaneDiagramme</c>).</summary>
        internal static readonly IReadOnlyList<string> Detaildiagramme = new[]
        {
            "stand.bild.waerme_jahresverlauf", "stand.bild.waerme_dauerlinie", "stand.bild.strombilanz_monate", "stand.bild.speicherverlauf",
        };

        /// <summary>Die Excel-Tabelle mit dem eigenen Säulendiagramm.</summary>
        internal const string TABELLE_SZENARIEN = "EPOS_tabelle__wirtschaft__szenarien";

        /// <summary>Die Excel-Tabelle je Stand auf dem Musterblatt.</summary>
        internal const string TABELLE_MONATSWERTE = "EPOS_stand__tabelle__monatswerte";

        /// <summary>Die Reihennamen des eigenen Liniendiagramms.</summary>
        internal const string REIHE_MONATE = "EPOS.reihe.monate", REIHE_WAERME = "EPOS.reihe.waermebedarf.monate";

        /// <summary>
        /// <b>Erzeugt die ausführliche Excel-Vorlage</b> auf Deutsch oder Englisch — die Bytes einer <c>.xlsx</c> mit
        /// <c>EPOS.Katalogfassung</c> (<paramref name="fassung"/>), <c>EPOS.Vorlage</c> = <see cref="VORLAGENART"/>,
        /// <c>EPOS.Sprache</c> und <c>EPOS.Blattanhang</c> = <c>nein</c>.
        /// </summary>
        public static byte[] Erzeuge(bool englisch, int fassung = Vorlagenfeldkatalog.KATALOGFASSUNG)
        {
            var m = new Excelmappenbau(englisch);
            byte[] bytes;
            string eigene;
            using (var wb = new XLWorkbook())
            {
                Deckblatt(wb, m);
                Uebersicht(wb, m);
                Vergleich(wb, m);
                Marke(wb, m, BLATT_WIRTSCHAFT, nameof(R.BV_XLA_N_MARKE_WIRTSCHAFT));
                Auswertung(wb, m);
                Verlauf(wb, m);
                Musterblatt(wb, m);
                Marke(wb, m, BLATT_CHECKLISTE, nameof(R.BV_XLA_N_MARKE_CHECKLISTE));
                eigene = EigeneDiagramme(wb, m);
                Marke(wb, m, BLATT_DIAGRAMMDATEN, nameof(R.BV_XLA_N_MARKE_DIAGRAMMDATEN));
                m.Eigenschaften(wb, fassung, VORLAGENART);
                wb.CustomProperties.Add(ExcelVorlagenmappe.EIGENSCHAFT_BLATTANHANG, englisch ? "no" : "nein");
                bytes = m.Speichere(wb);
            }
            bytes = Diagramme(bytes, eigene, englisch);
            return Excelmappenbau.Festschreiben(bytes);
        }

        // =====================================================================
        //  Elemente: Platzhalter mit Notiz
        // =====================================================================

        /// <summary>
        /// Ein Element: der Platzhalter <paramref name="platzhalter"/> in der Zelle, als Notiz die Beschreibung des Katalogs zu
        /// <paramref name="schluessel"/> und der Satz <paramref name="wie"/> (wie man es ändert).
        /// </summary>
        private static IXLCell Element(IXLCell zelle, Excelmappenbau m, string schluessel, string wie, string platzhalter = null,
                                       string zusatz = null)
        {
            zelle.Value = platzhalter ?? "{{" + schluessel + "}}";
            Vorlagenfeld f = Vorlagenfeldkatalog.Finde(schluessel);
            string beschreibung = f == null ? "" : Vorlagenfeldkatalog.Beschreibung(f, m.Englisch);
            string text = (beschreibung.Length > 0 ? beschreibung + " " : "") + m.T(wie);
            if (zusatz != null) text += " " + m.T(zusatz);
            m.NotizText(zelle, text);
            return zelle;
        }

        /// <summary>Eine fette Beschriftung über einem Element.</summary>
        private static void Beschriftung(IXLCell zelle, Excelmappenbau m, string ressource)
        {
            zelle.Value = m.T(ressource);
            zelle.Style.Font.Bold = true;
        }

        /// <summary>Diagramme untereinander ab Zeile <paramref name="r"/>, je im Abstand eines Diagramms; gibt die Zeile danach zurück.</summary>
        private static int Diagrammfolge(IXLWorksheet ws, Excelmappenbau m, int r, IEnumerable<(string Schluessel, string Notiz)> bilder)
        {
            foreach ((string schluessel, string notiz) in bilder)
            {
                Element(ws.Cell(r, 1), m, schluessel, nameof(R.BV_XLA_N_WIE_BILD), zusatz: notiz);
                r += ExcelBaukasten.DIAGRAMMABSTAND;
            }
            return r;
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
            m.Notiz(ws.Cell(2, 1), nameof(R.BV_XLA_N_AUFBAU));

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
        //  Projektübersicht (Blatt „Übersicht“)
        // =====================================================================

        private static void Uebersicht(XLWorkbook wb, Excelmappenbau m)
        {
            IXLWorksheet ws = m.Blatt(wb, nameof(R.BV_XLA_BLATT_UEBERSICHT));
            m.Titel(ws.Cell(1, 1), m.T(nameof(R.BV_XLA_TITEL_UEBERSICHT)));
            m.Notiz(ws.Cell(1, 1), nameof(R.BV_XLA_N_UEBERSICHT));

            // Die Kopfwerte wie im erzeugten Blatt: Beschriftung fett, daneben der Wert.
            var kopf = new (string Beschriftung, string Schluessel, string Platzhalter)[]
            {
                (nameof(R.BV_XLA_PROJEKT), "projekt.name", null),
                (nameof(R.BV_XLA_KUNDE), "projekt.kunde", null),
                (nameof(R.BV_XLA_BEARBEITER), "projekt.bearbeiter", null),
                (nameof(R.BV_XLA_KLIMAREGION), "projekt.klimaregion", null),
                (nameof(R.BV_XLA_BERICHTSDATUM), "bericht.datum", "{{bericht.datum|datum mit zeit}}"),
            };
            int r = 3;
            foreach ((string beschriftung, string schluessel, string platzhalter) in kopf)
            {
                Beschriftung(ws.Cell(r, 1), m, beschriftung);
                Element(ws.Cell(r, 2), m, schluessel, platzhalter == null ? nameof(R.BV_XLA_N_KOPFWERT) : nameof(R.BV_XLA_N_DATUM_ZEIT),
                        platzhalter);
                r++;
            }

            Element(ws.Cell(9, 1), m, "tabelle.varianten", nameof(R.BV_XLA_N_TABELLE_LISTE));
            Element(ws.Cell(11, 1), m, "tabelle.komponenten.matrix", nameof(R.BV_XLA_N_TABELLE_BEREICH));
            Diagrammfolge(ws, m, 13, new[] { ("stamm.bild.speichertemperaturen", (string)null) });

            ws.Column(1).Width = 22;
            ws.Column(2).Width = 30;
            ws.Column(3).Width = 26;
            ws.Column(4).Width = 18;
            ws.Column(5).Width = 30;
        }

        // =====================================================================
        //  Variantenvergleich (Blatt „Vergleich“)
        // =====================================================================

        private static void Vergleich(XLWorkbook wb, Excelmappenbau m)
        {
            IXLWorksheet ws = m.Blatt(wb, nameof(R.BV_XLA_BLATT_VERGLEICH));
            Element(ws.Cell(1, 1), m, "tabelle.vergleich.liste", nameof(R.BV_XLA_N_VERGLEICHSLISTE));
            Diagrammfolge(ws, m, 3, Berichtsbilder.Balkenkennzahlen.Select((k, i) =>
                (Exceldiagrammquellen.VERGLEICH_BALKEN + k, i == 0 ? nameof(R.BV_XLA_N_BILD_BALKEN) : (string)null)));
            ws.SheetView.Freeze(1, 3);
            ws.Column(1).Width = 16;
            ws.Column(2).Width = 42;
            ws.Column(3).Width = 12;
            for (int c = 4; c <= 10; c++) ws.Column(c).Width = 14;
        }

        // =====================================================================
        //  Auswertung: die Wirtschaftlichkeit in Einzelelementen (Werte der Formelmappe)
        // =====================================================================

        private static void Auswertung(XLWorkbook wb, Excelmappenbau m)
        {
            IXLWorksheet ws = m.Blatt(wb, nameof(R.BV_XLA_BLATT_AUSWERTUNG));
            m.Titel(ws.Cell(1, 1), m.T(nameof(R.BV_XLA_TITEL_AUSWERTUNG)));
            m.Notiz(ws.Cell(1, 1), nameof(R.BV_XLA_N_AUSWERTUNG));
            ws.Column(1).Width = 44;

            var bloecke = new (string Beschriftung, string Schluessel, string Wie)[]
            {
                (nameof(R.BV_XLA_ABSCHNITT_PARAMETER), "tabelle.wirtschaft.parameter", nameof(R.BV_XLA_N_TABELLE_EXCEL)),
                (nameof(R.BV_XLA_ABSCHNITT_ERWARTET), "tabelle.wirtschaft.kennzahlen", nameof(R.BV_XLA_N_WIE_TABELLE)),
                (nameof(R.BV_XLA_ABSCHNITT_GUENSTIG), "tabelle.wirtschaft.kennzahlen.guenstig", nameof(R.BV_XLA_N_WIE_TABELLE)),
                (nameof(R.BV_XLA_ABSCHNITT_UNGUENSTIG), "tabelle.wirtschaft.kennzahlen.unguenstig", nameof(R.BV_XLA_N_WIE_TABELLE)),
                (nameof(R.BV_XLA_ABSCHNITT_BANDBREITE), "tabelle.wirtschaft.szenarien", nameof(R.BV_XLA_N_WIE_TABELLE)),
                (nameof(R.BV_XLA_ABSCHNITT_VORSCHLAG), "wirtschaft.vorschlag", nameof(R.BV_XLA_N_KOPFWERT)),
                (nameof(R.BV_XLA_ABSCHNITT_NICHT_MONETAER), "tabelle.wirtschaft.nicht_monetaer", nameof(R.BV_XLA_N_WIE_TABELLE)),
                (nameof(R.BV_XLA_ABSCHNITT_HINWEISE), "wirtschaft.hinweise", nameof(R.BV_XLA_N_KOPFWERT)),
            };
            int r = 3;
            foreach ((string beschriftung, string schluessel, string wie) in bloecke)
            {
                Beschriftung(ws.Cell(r++, 1), m, beschriftung);
                IXLCell c = Element(ws.Cell(r++, 1), m, schluessel, wie);
                if (!schluessel.StartsWith("tabelle.", StringComparison.Ordinal)) c.Style.Alignment.WrapText = true;
                r++;
            }
            // Die Warnungen der Rechnung unter den Hinweisen.
            Element(ws.Cell(r - 1, 1), m, "wirtschaft.warnungen", nameof(R.BV_XLA_N_KOPFWERT)).Style.Alignment.WrapText = true;
            r++;
            Diagrammfolge(ws, m, r, Exceldiagrammquellen.Wirtschaftsbilder.Select(k => (k, (string)null)));
        }

        // =====================================================================
        //  Kapitalwertverlauf (Blatt „Verlauf“)
        // =====================================================================

        private static void Verlauf(XLWorkbook wb, Excelmappenbau m)
        {
            IXLWorksheet ws = m.Blatt(wb, nameof(R.BV_XLA_BLATT_VERLAUF));
            m.Titel(ws.Cell(1, 1), m.T(nameof(R.WIRT_VERL_BLATT_TITEL)));
            Element(ws.Cell(3, 1), m, "tabelle.wirtschaft.verlauf", nameof(R.BV_XLA_N_VERLAUF));
            ws.Column(1).Width = 14;
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
        //  Musterblatt blatt.detail (Detailblatt je Stand)
        // =====================================================================

        private static void Musterblatt(XLWorkbook wb, Excelmappenbau m)
        {
            IXLWorksheet ws = wb.Worksheets.Add(BLATT_DETAIL);
            ws.Cell(1, 1).Value = "{{blatt.detail}}";
            m.Notiz(ws.Cell(1, 1), nameof(R.BV_XLA_N_MUSTERBLATT));
            ws.Column(1).Width = 40;
            ws.Column(2).Width = 22;
            for (int c = 3; c <= 6; c++) ws.Column(c).Width = 16;

            // Kopf wie im Detailblatt: „Stamm — Projekt“, Simulationsstand, der Fehler des Laufs.
            ws.Cell(2, 1).Value = "{{stand.rolle}} — {{stand.projektname}}";
            ws.Cell(2, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Style.Font.FontSize = 13;
            m.Notiz(ws.Cell(2, 1), nameof(R.BV_XLA_N_STAND));
            ws.Cell(3, 1).Value = m.T(nameof(R.BV_XLA_SIMULATIONSSTAND));
            Element(ws.Cell(3, 2), m, "stand.simulationsstand", nameof(R.BV_XLA_N_KOPFWERT));
            Element(ws.Cell(4, 1), m, "stand.fehler", nameof(R.BV_XLA_N_KOPFWERT)).Style.Font.FontColor = XLColor.FromHtml("#C00000");

            Element(ws.Cell(6, 1), m, "stand.tabelle.kennzahlen.liste", nameof(R.BV_XLA_N_TABELLE_STAND));
            Beschriftung(ws.Cell(8, 1), m, nameof(R.BV_XLA_ABSCHNITT_ERZEUGER));
            Element(ws.Cell(9, 1), m, "stand.tabelle.erzeuger", nameof(R.BV_XLA_N_TABELLE_STAND));
            Beschriftung(ws.Cell(11, 1), m, nameof(R.BV_XLA_ABSCHNITT_BRENNSTOFF));
            Element(ws.Cell(12, 1), m, "stand.tabelle.brennstoffmengen", nameof(R.BV_XLA_N_TABELLE_STAND));

            // Die Tafeln der Formelmappe je Stand als Werte.
            Beschriftung(ws.Cell(14, 1), m, nameof(R.BV_XLA_ABSCHNITT_WIRTSCHAFT_STAND));
            m.Notiz(ws.Cell(14, 1), nameof(R.BV_XLA_N_WIRTSCHAFT_STAND));
            Element(ws.Cell(15, 1), m, "stand.tabelle.betriebskosten", nameof(R.BV_XLA_N_TABELLE_STAND));
            Element(ws.Cell(17, 1), m, "stand.tabelle.kwkg_module", nameof(R.BV_XLA_N_TABELLE_STAND));
            Element(ws.Cell(19, 1), m, "stand.tabelle.mehrjahres", nameof(R.BV_XLA_N_TABELLE_STAND));

            // Die Monatswerte als Excel-Tabelle je Stand: Kopf und eine Musterzeile — sie wächst beim Füllen. Sie steht unter
            // allen anderen Tabellen: Eine Excel-Tabelle wächst nur in ihren eigenen Spalten.
            Beschriftung(ws.Cell(21, 1), m, nameof(R.BV_XLA_ABSCHNITT_MONATE));
            ws.Cell(22, 1).Value = m.T(nameof(R.BV_XLA_SP_MONAT));
            ws.Cell(22, 2).Value = m.T(nameof(R.BV_XLA_SP_WERT));
            ws.Cell(23, 1).Value = m.T(nameof(R.BV_XLA_MUSTERZEILE));
            ws.Cell(23, 2).Value = 0.0;
            IXLTable t = ws.Range(22, 1, 23, 2).CreateTable(TABELLE_MONATSWERTE);
            t.Theme = XLTableTheme.TableStyleLight9;
            m.Notiz(ws.Cell(22, 1), nameof(R.BV_XLA_N_EXCELTABELLE_STAND));

            // Die Diagramme je Stand: die vier des Detailblatts, die Deckungskreise des Vergleichs, der Zahlungsstrom.
            Beschriftung(ws.Cell(25, 1), m, nameof(R.BV_XLA_ABSCHNITT_DIAGRAMME));
            var bilder = Detaildiagramme.Select(k => (k, (string)null)).ToList();
            bilder.Add(("stand.bild.deckung_waerme", nameof(R.BV_XLA_N_BILD_DECKUNG)));
            bilder.Add(("stand.bild.deckung_strom", null));
            bilder.Add(("stand.bild.zahlungsstrom", null));
            Diagrammfolge(ws, m, 26, bilder);
        }
    }
}
