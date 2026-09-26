using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>Wie ein Platzhalter an seiner Stelle einer Excel-Vorlage steht (Konzept Berichtsvorlagen 4.4, 4.7).</summary>
    internal enum Excelstelle
    {
        /// <summary>Füllbar: ein Wert (Text, Zahl, Datum, Liste) an erlaubter Stelle.</summary>
        Gut,

        /// <summary>Den Schlüssel kennt der Katalog nicht.</summary>
        Unbekannt,

        /// <summary>Der Eintrag hat keine Ausgabe Excel (Kapitel, Logo, Word-Tabellen und -Bilder).</summary>
        OhneExcel,

        /// <summary>Tabelle oder Diagramm — in Excel erst mit BV-E8.</summary>
        Spaeter,

        /// <summary>Die Art passt nicht an die Stelle: ein Schalter, eine Liste oder ein Blatt im Satz.</summary>
        FalscheArt,

        /// <summary>Eine Blockmarke — Excel kennt keine Blocksyntax (Konzept 4.4).</summary>
        Block,

        /// <summary>Ein Wert je Stand außerhalb des Musterblatts <c>blatt.detail</c> (Konzept 4.7).</summary>
        Kontext,

        /// <summary>Ein Wert je Gebäude — in Excel nur als Listenzeile (BV-E8).</summary>
        Gebaeude,

        /// <summary>Eine Blattmarke, die nicht allein in A1 steht.</summary>
        Blattort,
    }

    /// <summary>Eine Blattmarke <c>{{blatt.&lt;name&gt;}}</c> in A1 eines Blattes (Konzept 7.2).</summary>
    internal sealed class Excelblattmarke
    {
        internal Excelblattmarke(IXLWorksheet blatt, string schluessel, ExcelBerichtGenerator.Blattart art, bool leer)
        {
            Blatt = blatt;
            Name = blatt.Name;
            Schluessel = schluessel;
            Art = art;
            Leer = leer;
        }

        /// <summary>Das markierte Blatt der Vorlage.</summary>
        internal IXLWorksheet Blatt { get; }

        /// <summary>Der Name des Blattes in der Vorlage.</summary>
        internal string Name { get; }

        /// <summary>Der Schlüssel der Marke, etwa <c>blatt.vergleich</c>.</summary>
        internal string Schluessel { get; }

        /// <summary>Welches erzeugte Blatt an seine Stelle tritt.</summary>
        internal ExcelBerichtGenerator.Blattart Art { get; }

        /// <summary>Steht außer der Marke nichts auf dem Blatt?</summary>
        internal bool Leer { get; }

        /// <summary>
        /// Ein Musterblatt (Konzept 7.2): <c>blatt.detail</c> auf einem Blatt, das weitere Zellen trägt — es wird je
        /// Stand geklont, statt durch die erzeugten Detailblätter ersetzt zu werden.
        /// </summary>
        internal bool IstMuster { get { return Art == ExcelBerichtGenerator.Blattart.Detail && !Leer; } }
    }

    /// <summary>Eine Zelle einer Excel-Vorlage mit Platzhaltern (Konzept 4.4).</summary>
    internal sealed class Excelzellfund
    {
        internal Excelzellfund(IXLCell zelle, string text, IReadOnlyList<Platzhalter> marken)
        {
            Zelle = zelle;
            Blattname = zelle.Worksheet.Name;
            Adresse = zelle.Address.ToString();
            Text = text;
            Marken = marken;
        }

        internal IXLCell Zelle { get; }

        internal string Blattname { get; }

        internal string Adresse { get; }

        internal string Text { get; }

        internal IReadOnlyList<Platzhalter> Marken { get; }

        /// <summary>Steht genau eine Marke allein in der Zelle (Leerraum außen herum zählt nicht)? Dann ein typisierter Wert.</summary>
        internal bool Allein
        {
            get { return Marken.Count == 1 && string.Equals(Text.Trim(), Marken[0].Roh, StringComparison.Ordinal); }
        }
    }

    /// <summary>Ein Name der Vorlage — ein EPOS-Name (<c>EPOS.*</c>, <c>EPOS_*</c>) oder ein reservierter (Konzept 4.4, 7.4).</summary>
    internal sealed class Excelnamensfund
    {
        internal Excelnamensfund(IXLDefinedName name, IXLWorksheet bereich, string schluessel, bool reserviert)
        {
            Name = name;
            Bereich = bereich;
            Schluessel = schluessel;
            Reserviert = reserviert;
        }

        internal IXLDefinedName Name { get; }

        /// <summary>Das Blatt eines blattweiten Namens; <c>null</c> = mappenweit.</summary>
        internal IXLWorksheet Bereich { get; }

        /// <summary>Der Platzhalterschlüssel des Namens; <c>null</c> bei einem reservierten Namen.</summary>
        internal string Schluessel { get; }

        /// <summary>Gehört der Name der Formelmappe (<see cref="Vorlagenfeldkatalog.ReservierteExcelNamen"/>)?</summary>
        internal bool Reserviert { get; }
    }

    /// <summary>
    /// <b>Was eine Excel-Vorlage trägt</b> (Konzept Berichtsvorlagen 4.4, 7.1–7.4; Etappe BV-E7) — gelesen EINMAL aus
    /// der geladenen Mappe, für Prüfer und Füller gleich: Blattmarken, Zellen mit Platzhaltern, EPOS-Namen,
    /// reservierte Namen und die Zahl der Formeln. Dazu die Paketarbeit vor und nach ClosedXML: die Umstellung einer
    /// <c>.xltx</c> auf eine Arbeitsmappe (Messprobe 3 von BV-E0), die Ablehnung von Makros und der Paketvergleich
    /// mit Positivliste der erwarteten Verluste (Konzept 7.4).
    /// </summary>
    internal sealed class ExcelVorlagenmappe
    {
        /// <summary>Die Vorsilbe eines Platzhalternamens ohne Punkte (Konzept 5.5): <c>EPOS_projekt__kunde</c>.</summary>
        internal const string PRAEFIX_UNTERSTRICH = "EPOS_";

        /// <summary>Inhaltstypen, deren Verlust beim Laden und Speichern erwartet ist (Konzept 7.4).</summary>
        internal static readonly IReadOnlyList<string> ErwarteteVerluste = new[]
        {
            "application/vnd.openxmlformats-officedocument.spreadsheetml.calcChain+xml",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.printerSettings",
        };

        /// <summary>Die Schlüssel der Blattmarken und die Blätter, die an ihre Stelle treten (Konzept 7.2).</summary>
        internal static readonly IReadOnlyDictionary<string, ExcelBerichtGenerator.Blattart> Blattmarken =
            new Dictionary<string, ExcelBerichtGenerator.Blattart>(StringComparer.Ordinal)
            {
                ["blatt.uebersicht"] = ExcelBerichtGenerator.Blattart.Uebersicht,
                ["blatt.vergleich"] = ExcelBerichtGenerator.Blattart.Vergleich,
                ["blatt.wirtschaftlichkeit"] = ExcelBerichtGenerator.Blattart.Wirtschaftlichkeit,
                ["blatt.verlauf"] = ExcelBerichtGenerator.Blattart.Verlauf,
                ["blatt.detail"] = ExcelBerichtGenerator.Blattart.Detail,
                ["blatt.checkliste"] = ExcelBerichtGenerator.Blattart.Checkliste,
                ["blatt.diagrammdaten"] = ExcelBerichtGenerator.Blattart.Diagrammdaten,
            };

        private ExcelVorlagenmappe()
        {
        }

        /// <summary>Die Blattmarken in Blattfolge — je Art die erste; weitere stehen in <see cref="DoppelteMarken"/>.</summary>
        internal List<Excelblattmarke> Marken { get; } = new List<Excelblattmarke>();

        /// <summary>Blattmarken, deren Art schon ein früheres Blatt trägt.</summary>
        internal List<Excelblattmarke> DoppelteMarken { get; } = new List<Excelblattmarke>();

        /// <summary>Die Zellen mit Platzhaltern — ohne die A1-Zellen der Blattmarken.</summary>
        internal List<Excelzellfund> Zellen { get; } = new List<Excelzellfund>();

        /// <summary>Die EPOS-Namen und die reservierten Namen.</summary>
        internal List<Excelnamensfund> Namen { get; } = new List<Excelnamensfund>();

        /// <summary>Wie viele Zellen eine Formel tragen.</summary>
        internal int Formeln { get; private set; }

        /// <summary>Das Musterblatt <c>blatt.detail</c>; <c>null</c> ohne.</summary>
        internal Excelblattmarke Muster { get { return Marken.FirstOrDefault(m => m.IstMuster); } }

        /// <summary>Liegt die Zelle auf dem Musterblatt?</summary>
        internal bool AufMuster(Excelzellfund f)
        {
            Excelblattmarke m = Muster;
            return m != null && string.Equals(f.Blattname, m.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Liest eine geladene Mappe.</summary>
        internal static ExcelVorlagenmappe Lies(XLWorkbook wb)
        {
            var m = new ExcelVorlagenmappe();
            var gesehen = new HashSet<ExcelBerichtGenerator.Blattart>();
            foreach (IXLWorksheet ws in wb.Worksheets)
            {
                Excelblattmarke marke = null;
                var zellen = new List<Excelzellfund>();
                int weitere = 0;
                foreach (IXLCell zelle in ws.CellsUsed(XLCellsUsedOptions.Contents))
                {
                    if (zelle.HasFormula)
                    {
                        m.Formeln++;
                        weitere++;
                        continue;
                    }
                    string text = zelle.Value.IsText ? zelle.Value.GetText() : null;
                    bool a1 = zelle.Address.RowNumber == 1 && zelle.Address.ColumnNumber == 1;
                    if (text == null || text.IndexOf("{{", StringComparison.Ordinal) < 0)
                    {
                        if (!zelle.Value.IsBlank) weitere++;
                        continue;
                    }
                    List<Platzhalter> marken = Platzhaltersyntax.Finde(text).ToList();
                    if (marken.Count == 0)
                    {
                        weitere++;
                        continue;
                    }
                    if (a1 && marken.Count == 1 && string.Equals(text.Trim(), marken[0].Roh, StringComparison.Ordinal)
                        && marken[0].Art == Platzhalterart.Feld
                        && Blattmarken.TryGetValue(Platzhaltersyntax.NormiereSchluessel(marken[0].Schluessel),
                                                   out ExcelBerichtGenerator.Blattart art))
                    {
                        marke = new Excelblattmarke(ws, Platzhaltersyntax.NormiereSchluessel(marken[0].Schluessel), art, true);
                        continue;
                    }
                    weitere++;
                    zellen.Add(new Excelzellfund(zelle, text, marken));
                }

                if (marke != null)
                {
                    marke = new Excelblattmarke(ws, marke.Schluessel, marke.Art, weitere == 0);
                    if (gesehen.Add(marke.Art)) m.Marken.Add(marke);
                    else m.DoppelteMarken.Add(marke);
                }
                m.Zellen.AddRange(zellen);
            }
            m.LiesNamen(wb);
            return m;
        }

        private void LiesNamen(XLWorkbook wb)
        {
            var reserviert = new HashSet<string>(Vorlagenfeldkatalog.ReservierteExcelNamen, StringComparer.OrdinalIgnoreCase);
            foreach (IXLDefinedName n in wb.DefinedNames.ToList()) Nimm(n, null, reserviert);
            foreach (IXLWorksheet ws in wb.Worksheets)
                foreach (IXLDefinedName n in ws.DefinedNames.ToList()) Nimm(n, ws, reserviert);
        }

        private void Nimm(IXLDefinedName n, IXLWorksheet bereich, HashSet<string> reserviert)
        {
            string name = n?.Name ?? "";
            if (reserviert.Contains(name))
            {
                Namen.Add(new Excelnamensfund(n, bereich, null, true));
                return;
            }
            string schluessel = SchluesselAusName(name);
            if (schluessel != null) Namen.Add(new Excelnamensfund(n, bereich, schluessel, false));
        }

        /// <summary>
        /// Der Platzhalterschlüssel eines Excel-Namens (Konzept 4.4, 5.5): <c>EPOS.projekt.kunde</c> → <c>projekt.kunde</c>,
        /// <c>EPOS_projekt__kunde</c> → <c>projekt.kunde</c>; <c>null</c> für einen Namen ohne EPOS-Vorsilbe.
        /// </summary>
        internal static string SchluesselAusName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (name.StartsWith(Vorlagenfeldkatalog.PRAEFIX_EXCEL, StringComparison.OrdinalIgnoreCase))
                return Platzhaltersyntax.NormiereSchluessel(name.Substring(Vorlagenfeldkatalog.PRAEFIX_EXCEL.Length));
            if (name.StartsWith(PRAEFIX_UNTERSTRICH, StringComparison.OrdinalIgnoreCase))
                return Platzhaltersyntax.NormiereSchluessel(name.Substring(PRAEFIX_UNTERSTRICH.Length).Replace("__", "."));
            return null;
        }

        /// <summary>
        /// Wie ein Platzhalter an seiner Stelle steht — die EINE Regel für Prüfer und Füller. <paramref name="allein"/>:
        /// allein in der Zelle bzw. als Name (typisierter Wert); sonst im Satz (Textersetzung).
        /// </summary>
        internal static Excelstelle Beurteile(Platzhalter p, Vorlagenfeld feld, bool aufMuster, bool allein)
        {
            if (p.IstBlockmarke || p.Art != Platzhalterart.Feld) return p.Art == Platzhalterart.Unbekannt ? Excelstelle.Unbekannt : Excelstelle.Block;
            if (feld == null) return Excelstelle.Unbekannt;
            if (feld.Art == Vorlagenfeldart.Blatt) return Excelstelle.Blattort;
            if ((feld.Ausgaben & Vorlagenausgabe.Excel) == 0) return Excelstelle.OhneExcel;
            if (feld.Art == Vorlagenfeldart.Tabelle || feld.Art == Vorlagenfeldart.Bild) return Excelstelle.Spaeter;
            if (feld.Art == Vorlagenfeldart.Kapitel || feld.Art == Vorlagenfeldart.Schalter) return Excelstelle.FalscheArt;
            if (feld.Art == Vorlagenfeldart.Liste && !allein) return Excelstelle.FalscheArt;
            if (feld.Kontext == Vorlagenfeldkontext.Gebaeude) return Excelstelle.Gebaeude;
            if (feld.Kontext == Vorlagenfeldkontext.Stand && !aufMuster) return Excelstelle.Kontext;
            return Excelstelle.Gut;
        }

        // =====================================================================
        //  Paket: Umstellung, Makros, Inhalt, Vergleich
        // =====================================================================

        /// <summary>
        /// Stellt eine Excel-Vorlage (<c>.xltx</c>) über das SDK auf eine Arbeitsmappe um (Messprobe 3 von BV-E0: aus
        /// einem Strom geladen, speicherte ClosedXML sie sonst als Vorlage weiter) und lehnt Makros ab. Liefert die
        /// Bytes, die ClosedXML lädt; <paramref name="warVorlage"/> sagt, ob umgestellt wurde.
        /// </summary>
        /// <exception cref="ArgumentException">Leere Vorlage (Parameter <c>vorlage</c>).</exception>
        /// <exception cref="InvalidDataException">Keine Excel-Arbeitsmappe.</exception>
        /// <exception cref="NotSupportedException">Makros (<c>.xlsm</c>, <c>.xltm</c>, VBA-Projekt).</exception>
        internal static byte[] Normalisiere(byte[] vorlage, bool englisch, out bool warVorlage)
        {
            warVorlage = false;
            if (vorlage == null || vorlage.Length == 0)
                throw new ArgumentException(ExcelVorlagentexte.T(englisch, nameof(R.VF_PRUEF_GRUND_LEER)), "vorlage");
            if (!IstZip(vorlage))
                throw new InvalidDataException(ExcelVorlagentexte.T(englisch, nameof(R.BV_XL_PRUEF_GRUND_KEIN_EXCEL)));

            var strom = new MemoryStream();
            strom.Write(vorlage, 0, vorlage.Length);
            strom.Position = 0;
            bool keinExcel = false, makros = false;
            try
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(strom, true))
                {
                    if (doc.WorkbookPart?.Workbook == null) keinExcel = true;
                    else if (HatMakros(doc)) makros = true;
                    else if (doc.DocumentType == SpreadsheetDocumentType.Template)
                    {
                        doc.ChangeDocumentType(SpreadsheetDocumentType.Workbook);
                        warVorlage = true;
                    }
                }
            }
            catch (Exception ex) when (ex is OpenXmlPackageException || ex is FileFormatException || ex is InvalidDataException ||
                                       ex is IOException || ex is InvalidOperationException || ex is System.Xml.XmlException)
            {
                throw new InvalidDataException(ExcelVorlagentexte.T(englisch, nameof(R.BV_XL_FEHLER_LADEN), ex.Message), ex);
            }
            if (keinExcel) throw new InvalidDataException(ExcelVorlagentexte.T(englisch, nameof(R.BV_XL_PRUEF_GRUND_KEIN_EXCEL)));
            if (makros) throw new NotSupportedException(ExcelVorlagentexte.T(englisch, nameof(R.BV_XL_FEHLER_MAKROS)));
            return strom.ToArray();
        }

        /// <summary>Trägt das Paket Makros — Dokumenttyp mit Makros oder ein VBA-Projekt?</summary>
        internal static bool HatMakros(SpreadsheetDocument doc)
        {
            return doc.DocumentType == SpreadsheetDocumentType.MacroEnabledWorkbook ||
                   doc.DocumentType == SpreadsheetDocumentType.MacroEnabledTemplate ||
                   doc.DocumentType == SpreadsheetDocumentType.AddIn ||
                   doc.WorkbookPart?.VbaProjectPart != null;
        }

        /// <summary>Lädt die (umgestellten) Bytes in ClosedXML; eine Ladeausnahme wird ein benannter Fehler.</summary>
        internal static XLWorkbook Lade(byte[] arbeit, bool englisch)
        {
            try
            {
                return new XLWorkbook(new MemoryStream(arbeit, false));
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(ExcelVorlagentexte.T(englisch, nameof(R.BV_XL_FEHLER_LADEN), ex.Message), ex);
            }
        }

        /// <summary>Ein Zip-Paket (<c>PK\3\4</c>)?</summary>
        internal static bool IstZip(byte[] b)
        {
            return b != null && b.Length >= 4 && b[0] == (byte)'P' && b[1] == (byte)'K' && b[2] == 3 && b[3] == 4;
        }

        /// <summary>OLE-Verbunddatei: Excel 97–2003 oder ein verschlüsseltes OOXML-Paket.</summary>
        internal static bool IstOle(byte[] b)
        {
            return b != null && b.Length >= 8 && b[0] == 0xD0 && b[1] == 0xCF && b[2] == 0x11 && b[3] == 0xE0 &&
                   b[4] == 0xA1 && b[5] == 0xB1 && b[6] == 0x1A && b[7] == 0xE1;
        }

        /// <summary>Die Summe der entpackten Größen aus dem Zentralverzeichnis.</summary>
        internal static long Entpackt(byte[] paket)
        {
            long summe = 0;
            using (var strom = new MemoryStream(paket, false))
            using (var zip = new ZipArchive(strom, ZipArchiveMode.Read))
                foreach (ZipArchiveEntry e in zip.Entries) summe += Math.Max(0L, e.Length);
            return summe;
        }

        /// <summary>
        /// Der Inhalt eines Pakets für den Paketvergleich (Konzept 7.4): je Inhaltstyp die Zahl der Teile, dazu die Formen
        /// in den Zeichnungen (<c>xdr:sp</c>, <c>xdr:grpSp</c>, <c>xdr:cxnSp</c>) — ClosedXML verliert Formen, auch wenn
        /// die Zeichnung selbst bleibt.
        /// </summary>
        internal static Dictionary<string, int> Paketinhalt(byte[] paket)
        {
            var typen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using (var strom = new MemoryStream(paket, false))
            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(strom, false))
            {
                var gesehen = new HashSet<Uri>();
                var stapel = new Stack<OpenXmlPartContainer>();
                stapel.Push(doc);
                while (stapel.Count > 0)
                {
                    OpenXmlPartContainer c = stapel.Pop();
                    foreach (IdPartPair paar in c.Parts)
                    {
                        OpenXmlPart teil = paar.OpenXmlPart;
                        if (!gesehen.Add(teil.Uri)) continue;
                        Zaehle(typen, teil.ContentType, 1);
                        if (teil is DrawingsPart zeichnung)
                        {
                            int formen = 0;
                            try
                            {
                                formen = zeichnung.RootElement?.Descendants()
                                    .Count(e => e.LocalName == "sp" || e.LocalName == "grpSp" || e.LocalName == "cxnSp") ?? 0;
                            }
                            catch (Exception) { formen = 0; }
                            if (formen > 0) Zaehle(typen, TYP_FORMEN, formen);
                        }
                        else if (teil is WorksheetPart blatt)
                        {
                            int steuer = 0;
                            try
                            {
                                steuer = blatt.RootElement?.Descendants()
                                    .Count(e => e.LocalName == "control" || e.LocalName == "oleObject") ?? 0;
                            }
                            catch (Exception) { steuer = 0; }
                            if (steuer > 0) Zaehle(typen, TYP_STEUERELEMENTE, steuer);
                        }
                        stapel.Push(teil);
                    }
                }
            }
            return typen;
        }

        /// <summary>Der Pseudotyp der Formen im Paketinhalt.</summary>
        internal const string TYP_FORMEN = "epos/formen";

        /// <summary>Der Pseudotyp der Steuerelemente und OLE-Objekte eines Blattes (<c>control</c>, <c>oleObject</c>).</summary>
        internal const string TYP_STEUERELEMENTE = "epos/steuerelemente";

        private static void Zaehle(Dictionary<string, int> typen, string typ, int zahl)
        {
            typen.TryGetValue(typ ?? "", out int n);
            typen[typ ?? ""] = n + zahl;
        }

        /// <summary>
        /// Die verlorenen Teile: je Inhaltstyp, der nachher seltener vorkommt als vorher, der Name des Teils (ohne die
        /// erwarteten Verluste und die übergebenen Ausnahmen), zusammengefasst je Namen.
        /// </summary>
        internal static List<string> Verluste(Dictionary<string, int> vorher, Dictionary<string, int> nachher, bool englisch,
                                              IEnumerable<string> ausnahmen = null)
        {
            var aus = new HashSet<string>(ErwarteteVerluste.Concat(ausnahmen ?? Enumerable.Empty<string>()), StringComparer.OrdinalIgnoreCase);
            var namen = new List<string>();
            foreach (KeyValuePair<string, int> p in vorher.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (aus.Contains(p.Key)) continue;
                nachher.TryGetValue(p.Key, out int n);
                if (n >= p.Value) continue;
                string name = Teilname(p.Key, englisch);
                if (!namen.Contains(name)) namen.Add(name);
            }
            return namen;
        }

        /// <summary>Der Name eines Teils zu seinem Inhaltstyp (Konzept 7.4: „Pivot, Formen, Steuerelemente, VBA“).</summary>
        internal static string Teilname(string typ, bool englisch)
        {
            string t = (typ ?? "").ToLowerInvariant();
            string schluessel =
                t == TYP_FORMEN ? nameof(R.BV_XL_TEIL_FORMEN)
                : t == TYP_STEUERELEMENTE ? nameof(R.BV_XL_TEIL_STEUERELEMENTE)
                : t.Contains("pivot") ? nameof(R.BV_XL_TEIL_PIVOT)
                : t.Contains("vbaproject") ? nameof(R.BV_XL_TEIL_MAKROS)
                : t.Contains("controlproperties") || t.Contains("activex") ? nameof(R.BV_XL_TEIL_STEUERELEMENTE)
                : t.Contains("drawingml.chart") || t.Contains("chartsheet") || t.Contains("chartstyle") || t.Contains("chartcolorstyle") ? nameof(R.BV_XL_TEIL_DIAGRAMME)
                : t.Contains("slicer") || t.Contains("timeline") ? nameof(R.BV_XL_TEIL_DATENSCHNITTE)
                : t.Contains("querytable") || t.Contains("connections") ? nameof(R.BV_XL_TEIL_VERBINDUNGEN)
                : t.Contains("externallink") ? nameof(R.BV_XL_TEIL_EXTERN)
                : t.StartsWith("image/", StringComparison.Ordinal) ? nameof(R.BV_XL_TEIL_BILDER)
                : t.Contains("comments") ? nameof(R.BV_XL_TEIL_KOMMENTARE)
                : t.Contains("vmldrawing") || t.Contains("drawing+xml") ? nameof(R.BV_XL_TEIL_ZEICHNUNGEN)
                : null;
            return schluessel == null ? typ : ExcelVorlagentexte.T(englisch, schluessel);
        }
    }

    /// <summary>
    /// Die Texte der Excel-Engine und des Excel-Prüfers aus <c>MyResource</c> in der Sprache des Berichts. Ersetzt wird
    /// nur <c>{0}</c>, <c>{1}</c> …, nicht über <c>string.Format</c> — Argumente zitieren Platzhalter in doppelten
    /// Klammern, die stehen bleiben müssen.
    /// </summary>
    internal static class ExcelVorlagentexte
    {
        internal static string T(bool englisch, string schluessel, params object[] argumente)
        {
            CultureInfo kultur = BerichtTexte.KulturFuer(englisch);
            string muster = null;
            try { muster = R.ResourceManager.GetString(schluessel, kultur); }
            catch (Exception) { muster = null; }
            muster ??= schluessel;
            if (argumente == null) return muster;
            for (int i = 0; i < argumente.Length; i++)
                muster = muster.Replace("{" + i.ToString(CultureInfo.InvariantCulture) + "}",
                                        Convert.ToString(argumente[i], kultur) ?? "");
            return muster;
        }

        /// <summary>Der Fundort einer Zelle: „Blatt „Deckblatt“, Zelle B3“.</summary>
        internal static string Zelle(bool englisch, string blatt, string adresse)
        {
            return T(englisch, nameof(R.BV_XL_ORT_ZELLE), blatt, adresse);
        }

        /// <summary>Der Fundort eines Namens: „Name „EPOS.projekt.kunde““.</summary>
        internal static string Name(bool englisch, string name)
        {
            return T(englisch, nameof(R.BV_XL_ORT_NAME), name);
        }

        /// <summary>Der Fundort eines Blattes: „Blatt „Vergleich““.</summary>
        internal static string Blatt(bool englisch, string blatt)
        {
            return T(englisch, nameof(R.BV_XL_ORT_BLATT), blatt);
        }
    }
}
