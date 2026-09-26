using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Prüfer der Excel-Vorlagen</b> (Konzept Berichtsvorlagen 4.4, 4.7, 4.10, 7.1–7.4, 8.5; Etappe BV-E7) — dieselben
    /// Regeln, nach denen der <see cref="ExcelVorlagenfueller"/> füllt (<see cref="ExcelVorlagenmappe.Beurteile"/>), als
    /// <see cref="Pruefbefund"/> mit Fundort („Blatt „Deckblatt“, Zelle B3“, „Name „EPOS.projekt.kunde““) und „Was tun“.
    ///
    /// <para><b>Schnell</b> (Laden der Seite, Vorlagenwechsel, vor dem Start): Größen, Format, Makros, unbekannte
    /// Schlüssel (mit Vorschlag), Schlüssel ohne Ausgabe Excel, Tabellen und Bilder (erst BV-E8), Blöcke, Kontextverstöße
    /// (<c>stand.*</c> außerhalb des Musterblatts <c>blatt.detail</c>, <c>gebaeude.*</c>), Formatangaben, Blattmarken
    /// (nicht in A1, Blatt nicht leer, doppelt, Bezüge darauf), gleichnamige Anwenderblätter, reservierte Namen, EPOS-Namen
    /// auf Bereichen, Formeln der Vorlage (Hinweis: Vorschauen zeigen alte Werte), Sprache aus <c>custom.xml</c>.
    /// <b>Voll</b> (Hinzufügen, „Prüfen“) zusätzlich die Endung und der <b>Paketschutz</b>: Probe-Laden und -Speichern mit
    /// ClosedXML und Vergleich nach Inhaltstyp mit Positivliste der erwarteten Verluste — verlorene Teile mit Namen.</para>
    /// </summary>
    public static class ExcelVorlagenpruefer
    {
        /// <summary>Endungen, die als Excel-Vorlage abgelehnt werden (Konzept 7.1).</summary>
        private static readonly string[] AbgelehnteEndungen = { ".xls", ".xlt", ".xlsm", ".xltm", ".xlsb", ".xlam", ".ods" };

        /// <summary>Prüft eine Excel-Vorlage gegen den laufenden Platzhalterkatalog.</summary>
        public static Pruefbefund Pruefe(byte[] vorlage, Pruefstufe stufe, Pruefkontext kontext)
        {
            return Pruefe(vorlage, stufe, kontext, Vorlagenkatalogsicht.Standard, Pruefgrenzen.Standard);
        }

        /// <summary>Prüft gegen einen übergebenen Katalog und übergebene Grenzen (Prüfstand).</summary>
        internal static Pruefbefund Pruefe(byte[] vorlage, Pruefstufe stufe, Pruefkontext kontext,
                                           Vorlagenkatalogsicht katalog, Pruefgrenzen grenzen)
        {
            kontext ??= new Pruefkontext();
            grenzen ??= Pruefgrenzen.Standard;
            var s = new Sitzung(stufe, kontext, katalog ?? Vorlagenkatalogsicht.Standard);
            string summe = Vorlagenpruefer.Pruefsumme(vorlage);

            if (stufe == Pruefstufe.Voll) s.PruefeEndung(kontext.Dateiname);

            if (vorlage == null || vorlage.Length == 0)
                return s.Unlesbar(s.T(nameof(R.VF_PRUEF_GRUND_LEER)), summe);
            if (ExcelVorlagenmappe.IstOle(vorlage))
            {
                s.Format(s.T(nameof(R.BV_XL_PRUEF_FORMAT_XLS)));
                return s.Befund(summe, false);
            }
            if (!ExcelVorlagenmappe.IstZip(vorlage))
                return s.Unlesbar(s.T(nameof(R.BV_XL_PRUEF_GRUND_KEIN_EXCEL)), summe);

            // Die Sicherheitsgrenzen VOR dem Öffnen (Konzept 8.5) — in beiden Stufen.
            if (vorlage.LongLength > grenzen.Datei)
            {
                s.Groesse(nameof(R.VF_PRUEF_GROESSE), vorlage.LongLength, grenzen.Datei);
                return s.Befund(summe, false);
            }
            long entpackt;
            try { entpackt = ExcelVorlagenmappe.Entpackt(vorlage); }
            catch (Exception ex) { return s.Unlesbar(ex.Message, summe); }
            if (entpackt > grenzen.Entpackt)
            {
                s.Groesse(nameof(R.VF_PRUEF_GROESSE_ENTPACKT), entpackt, grenzen.Entpackt);
                return s.Befund(summe, false);
            }

            s.LiesEigenschaften(vorlage);

            byte[] arbeit;
            try
            {
                arbeit = ExcelVorlagenmappe.Normalisiere(vorlage, kontext.Englisch, out _);
            }
            catch (NotSupportedException)
            {
                s.Format(".xlsm");
                s.Makros();
                return s.Befund(summe, false);
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is ArgumentException)
            {
                return s.Unlesbar(ex.Message, summe);
            }

            XLWorkbook wb;
            try { wb = ExcelVorlagenmappe.Lade(arbeit, kontext.Englisch); }
            catch (InvalidDataException ex) { return s.UnlesbarLaden(ex.InnerException?.Message ?? ex.Message, summe); }

            using (wb)
            {
                ExcelVorlagenmappe mappe;
                try { mappe = ExcelVorlagenmappe.Lies(wb); }
                catch (Exception ex) { return s.UnlesbarLaden(ex.Message, summe); }

                s.PruefeZellen(mappe);
                s.PruefeMarken(wb, mappe);
                s.PruefeBlattnamen(wb, mappe);
                s.PruefeNamen(mappe);
                s.PruefeTabellen(mappe);
                s.PruefeFormeln(mappe);
                if (stufe == Pruefstufe.Voll) s.PruefePaket(wb, arbeit);
                return s.Befund(summe, true);
            }
        }

        // =====================================================================
        //  Die Sitzung einer Prüfung
        // =====================================================================

        private sealed class Sitzung
        {
            private readonly Pruefstufe _stufe;
            private readonly Pruefkontext _kontext;
            private readonly Vorlagenkatalogsicht _katalog;
            private readonly List<Pruefmeldung> _meldungen = new List<Pruefmeldung>();
            private readonly HashSet<string> _gesehen = new HashSet<string>(StringComparer.Ordinal);
            private readonly List<Vorlagenfund> _funde = new List<Vorlagenfund>();
            private readonly List<string> _schluessel = new List<string>();
            private readonly List<string> _unbekannte = new List<string>();
            private int _anzahl;
            private bool _formatGemeldet;
            private int? _fassung;
            private string _sprache;

            internal Sitzung(Pruefstufe stufe, Pruefkontext kontext, Vorlagenkatalogsicht katalog)
            {
                _stufe = stufe;
                _kontext = kontext;
                _katalog = katalog;
            }

            private bool Englisch { get { return _kontext.Englisch; } }

            internal string T(string schluessel, params object[] argumente)
            {
                return ExcelVorlagentexte.T(Englisch, schluessel, argumente);
            }

            private string Datei { get { return T(nameof(R.VF_PRUEF_ORT_DATEI)); } }

            private void Melde(Befundstufe stufe, string kennung, string text, string fundort, string wasTun,
                               string marke = null, string vorschlag = null)
            {
                if (!_gesehen.Add(kennung + "|" + text + "|" + fundort)) return;
                _meldungen.Add(new Pruefmeldung(stufe, kennung, text, fundort, wasTun, marke, vorschlag));
            }

            // ------------------------------------------------------------ Datei

            internal Pruefbefund Unlesbar(string grund, string summe)
            {
                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_UNLESBAR), T(nameof(R.VF_PRUEF_UNLESBAR), grund), Datei,
                      T(nameof(R.BV_XL_PRUEF_UNLESBAR_TUN)));
                return Befund(summe, false);
            }

            internal Pruefbefund UnlesbarLaden(string grund, string summe)
            {
                Melde(Befundstufe.Fehler, nameof(R.BV_XL_FEHLER_LADEN), T(nameof(R.BV_XL_FEHLER_LADEN), grund), Datei,
                      T(nameof(R.BV_XL_PRUEF_UNLESBAR_TUN)));
                return Befund(summe, false);
            }

            internal void Format(string format)
            {
                if (_formatGemeldet) return;
                _formatGemeldet = true;
                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_FORMAT), T(nameof(R.VF_PRUEF_FORMAT), format), Datei,
                      T(nameof(R.BV_XL_PRUEF_FORMAT_TUN)));
            }

            internal void Makros()
            {
                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_MAKROS), T(nameof(R.VF_PRUEF_MAKROS)), Datei,
                      T(nameof(R.BV_XL_PRUEF_MAKROS_TUN)));
            }

            internal void PruefeEndung(string dateiname)
            {
                if (string.IsNullOrWhiteSpace(dateiname)) return;
                string endung;
                try { endung = Path.GetExtension(dateiname.Trim()).ToLowerInvariant(); }
                catch (ArgumentException) { return; }
                if (AbgelehnteEndungen.Contains(endung)) Format(endung);
            }

            internal void Groesse(string kennung, long groesse, long grenze)
            {
                Melde(Befundstufe.Fehler, kennung, T(kennung, Megabyte(groesse), Megabyte(grenze)), Datei,
                      T(nameof(R.BV_XL_PRUEF_GROESSE_TUN)));
            }

            private string Megabyte(long bytes)
            {
                double mb = bytes / (1024.0 * 1024.0);
                return mb.ToString(mb >= 10 ? "0" : "0.0", BerichtTexte.KulturFuer(Englisch));
            }

            /// <summary>Katalogfassung und Sprache aus <c>docProps/custom.xml</c> (Konzept 4.9, 5.6) — wie in Word.</summary>
            internal void LiesEigenschaften(byte[] vorlage)
            {
                try
                {
                    using (var strom = new MemoryStream(vorlage, false))
                    using (SpreadsheetDocument doc = SpreadsheetDocument.Open(strom, false))
                    {
                        OpenXmlElement wurzel = doc.CustomFilePropertiesPart?.RootElement;
                        if (wurzel == null) return;
                        foreach (OpenXmlElement e in wurzel.ChildElements)
                        {
                            string name = (e.GetAttributes().FirstOrDefault(a => a.LocalName == "name").Value ?? "").Trim();
                            string wert = (e.InnerText ?? "").Trim();
                            if (string.Equals(name, Vorlagenpruefer.EIGENSCHAFT_KATALOGFASSUNG, StringComparison.OrdinalIgnoreCase))
                            {
                                if (int.TryParse(wert, NumberStyles.Integer, CultureInfo.InvariantCulture, out int f)) _fassung = f;
                            }
                            else if (string.Equals(name, Vorlagenpruefer.EIGENSCHAFT_SPRACHE, StringComparison.OrdinalIgnoreCase))
                            {
                                _sprache = wert.Length > 0 ? wert : null;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Eine unlesbare Eigenschaftsdatei ist kein Grund, die Vorlage abzulehnen.
                }
            }

            private bool SpracheAbweichend
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(_sprache)) return false;
                    bool vorlageEnglisch = _sprache.Trim().StartsWith("en", StringComparison.OrdinalIgnoreCase);
                    bool vorlageDeutsch = _sprache.Trim().StartsWith("de", StringComparison.OrdinalIgnoreCase);
                    return (vorlageEnglisch && !Englisch) || (vorlageDeutsch && Englisch);
                }
            }

            // ------------------------------------------------------------ Platzhalter

            /// <summary>Die Zellplatzhalter der Anwenderblätter und des Musterblatts.</summary>
            internal void PruefeZellen(ExcelVorlagenmappe mappe)
            {
                var ersetzt = new HashSet<IXLWorksheet>(mappe.Marken.Where(m => !m.IstMuster).Select(m => m.Blatt));
                foreach (Excelzellfund f in mappe.Zellen)
                {
                    if (ersetzt.Contains(f.Zelle.Worksheet)) continue;
                    string fundort = ExcelVorlagentexte.Zelle(Englisch, f.Blattname, f.Adresse);
                    bool allein = f.Allein;
                    foreach (Platzhalter p in f.Marken)
                        Pruefe(p, fundort, mappe.AufMuster(f), allein, Fundquelle.Text);
                }
            }

            /// <summary>Ein Platzhalter an seiner Stelle — die Regel des Füllers (<see cref="ExcelVorlagenmappe.Beurteile"/>).</summary>
            private void Pruefe(Platzhalter p, string fundort, bool aufMuster, bool allein, Fundquelle quelle, bool alsName = false)
            {
                _anzahl++;
                Vorlagenfeld feld = p.Art == Platzhalterart.Feld ? _katalog.Finde(p.Schluessel) : null;
                var fund = new Vorlagenfund(p, null, quelle, null, null, null) { Feld = feld };
                _funde.Add(fund);
                string normiert = Platzhaltersyntax.NormiereSchluessel(p.Schluessel);
                if (p.Art == Platzhalterart.Feld && normiert.Length > 0 && !_schluessel.Contains(normiert)) _schluessel.Add(normiert);

                Excelstelle stelle = ExcelVorlagenmappe.Beurteile(p, feld, aufMuster, allein, alsName);
                switch (stelle)
                {
                    case Excelstelle.Unbekannt:
                        {
                            if (p.Art == Platzhalterart.Feld && normiert.Length > 0 && !_unbekannte.Contains(normiert)) _unbekannte.Add(normiert);
                            Vorlagenfeld naechster = p.Art == Platzhalterart.Feld ? _katalog.Naechster(p.Schluessel, Vorlagenpruefer.VORSCHLAG_ABSTAND) : null;
                            string vorschlag = naechster == null ? null : "{{" + naechster.Schluessel + "}}";
                            Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_UNBEKANNT), T(nameof(R.VF_PRUEF_UNBEKANNT), p.Normalform), fundort,
                                  vorschlag != null ? T(nameof(R.VF_PRUEF_VORSCHLAG_TUN), vorschlag) : T(nameof(R.VF_PRUEF_UNBEKANNT_TUN)),
                                  p.Normalform, vorschlag);
                            return;
                        }
                    case Excelstelle.OhneExcel:
                    case Excelstelle.FalscheArt:
                        Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_ORT),
                              T(nameof(R.VF_PRUEF_ORT), p.Normalform, Artname(feld.Art),
                                T(stelle == Excelstelle.OhneExcel || allein ? nameof(R.BV_XL_STELLE_EXCEL) : nameof(R.VF_PRUEF_STELLE_SATZ))),
                              fundort, T(stelle == Excelstelle.OhneExcel ? nameof(R.BV_XL_PRUEF_OHNE_EXCEL_TUN) : nameof(R.BV_XL_PRUEF_ORT_TUN)),
                              p.Normalform);
                        return;
                    case Excelstelle.Spaeter:
                        Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_SPAETER), T(nameof(R.VF_PRUEF_SPAETER), p.Normalform), fundort,
                              T(nameof(R.BV_XL_PRUEF_SPAETER_TUN)), p.Normalform);
                        return;
                    case Excelstelle.Block:
                        Melde(Befundstufe.Fehler, nameof(R.BV_XL_PRUEF_BLOCK), T(nameof(R.BV_XL_PRUEF_BLOCK), p.Normalform), fundort,
                              T(nameof(R.BV_XL_PRUEF_BLOCK_TUN), "{{blatt.detail}}"), p.Normalform);
                        return;
                    case Excelstelle.Kontext:
                        Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_KONTEXT_STAND),
                              T(nameof(R.VF_PRUEF_KONTEXT_STAND), p.Normalform, T(nameof(R.BV_XL_PRUEF_MUSTERBLATT), "{{blatt.detail}}")),
                              fundort, T(nameof(R.BV_XL_PRUEF_KONTEXT_TUN), "{{blatt.detail}}"), p.Normalform);
                        return;
                    case Excelstelle.Gebaeude:
                        Melde(Befundstufe.Fehler, nameof(R.BV_XL_PRUEF_GEBAEUDE), T(nameof(R.BV_XL_PRUEF_GEBAEUDE), p.Normalform), fundort,
                              T(nameof(R.BV_XL_PRUEF_GEBAEUDE_TUN)), p.Normalform);
                        return;
                    case Excelstelle.Blattort:
                        Melde(Befundstufe.Fehler, nameof(R.BV_XL_PRUEF_BLATT_ORT), T(nameof(R.BV_XL_PRUEF_BLATT_ORT), p.Normalform), fundort,
                              T(nameof(R.BV_XL_PRUEF_BLATT_ORT_TUN)), p.Normalform);
                        return;
                }

                // Gut: die Formatangaben (Konzept 4.8) — eine unbekannte oder unpassende Angabe übergeht der Füller.
                foreach (Formatangabe a in p.Angaben)
                {
                    if (!a.IstBekannt)
                        Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_ANGABE_UNBEKANNT), T(nameof(R.VF_PRUEF_ANGABE_UNBEKANNT), a.Roh, p.Normalform),
                              fundort, T(nameof(R.VF_PRUEF_ANGABE_TUN_ENTFERNEN)), p.Normalform);
                    else if (!a.PasstZu(feld.Art))
                        Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_ANGABE_UNPASSEND),
                              T(nameof(R.VF_PRUEF_ANGABE_UNPASSEND), a.Normalform, p.Normalform, Artname(feld.Art)),
                              fundort, T(nameof(R.VF_PRUEF_ANGABE_TUN_ENTFERNEN)), p.Normalform);
                }
            }

            private string Artname(Vorlagenfeldart art)
            {
                switch (art)
                {
                    case Vorlagenfeldart.Zahl: return T(nameof(R.VF_PRUEF_ART_ZAHL));
                    case Vorlagenfeldart.Datum: return T(nameof(R.VF_PRUEF_ART_DATUM));
                    case Vorlagenfeldart.Tabelle: return T(nameof(R.VF_PRUEF_ART_TABELLE));
                    case Vorlagenfeldart.Bild: return T(nameof(R.VF_PRUEF_ART_BILD));
                    case Vorlagenfeldart.Liste: return T(nameof(R.VF_PRUEF_ART_LISTE));
                    case Vorlagenfeldart.Kapitel: return T(nameof(R.VF_PRUEF_ART_KAPITEL));
                    case Vorlagenfeldart.Schalter: return T(nameof(R.VF_PRUEF_ART_SCHALTER));
                    case Vorlagenfeldart.Blatt: return T(nameof(R.VF_PRUEF_ART_BLATT));
                    default: return T(nameof(R.VF_PRUEF_ART_TEXT));
                }
            }

            // ------------------------------------------------------------ Blattmarken

            /// <summary>Konzept 7.2: doppelte Marken, Marken auf Blättern mit Inhalt, Bezüge auf Markenblätter.</summary>
            internal void PruefeMarken(XLWorkbook wb, ExcelVorlagenmappe mappe)
            {
                foreach (Excelblattmarke m in mappe.Marken.Concat(mappe.DoppelteMarken))
                {
                    _anzahl++;
                    string marke = "{{" + m.Schluessel + "}}";
                    Platzhalter p = Platzhaltersyntax.Lies(marke);
                    _funde.Add(new Vorlagenfund(p, null, Fundquelle.Text, null, null, null) { Feld = _katalog.Finde(m.Schluessel) });
                    if (!_schluessel.Contains(m.Schluessel)) _schluessel.Add(m.Schluessel);
                }

                foreach (Excelblattmarke m in mappe.DoppelteMarken)
                    Melde(Befundstufe.Fehler, nameof(R.BV_XL_PRUEF_BLATT_DOPPELT), T(nameof(R.BV_XL_PRUEF_BLATT_DOPPELT), "{{" + m.Schluessel + "}}"),
                          ExcelVorlagentexte.Zelle(Englisch, m.Name, "A1"), T(nameof(R.BV_XL_PRUEF_BLATT_DOPPELT_TUN)), "{{" + m.Schluessel + "}}");

                foreach (Excelblattmarke m in mappe.Marken.Where(m => !m.Leer && !m.IstMuster))
                    Melde(Befundstufe.Warnung, nameof(R.BV_XL_PRUEF_BLATT_NICHT_LEER),
                          T(nameof(R.BV_XL_PRUEF_BLATT_NICHT_LEER), m.Name, "{{" + m.Schluessel + "}}"),
                          ExcelVorlagentexte.Blatt(Englisch, m.Name), T(nameof(R.BV_XL_PRUEF_BLATT_NICHT_LEER_TUN)), "{{" + m.Schluessel + "}}");

                // Bezüge des Anwenders auf ein Markenblatt zeigen nach dem Füllen ins Leere: das Blatt wird ersetzt
                // (heutiger Name) oder entfällt ohne Inhalt.
                var namen = mappe.Marken.Select(m => m.Name).ToList();
                if (namen.Count == 0) return;
                var markenblaetter = new HashSet<IXLWorksheet>(mappe.Marken.Select(m => m.Blatt));
                foreach (string name in namen)
                {
                    var stellen = new List<string>();
                    foreach (IXLWorksheet ws in wb.Worksheets.Where(w => !markenblaetter.Contains(w)))
                        foreach (IXLCell z in ws.CellsUsed(XLCellsUsedOptions.Contents))
                            if (z.HasFormula && Bezieht(z.FormulaA1, name))
                                stellen.Add(ExcelVorlagentexte.Zelle(Englisch, ws.Name, z.Address.ToString()));
                    foreach (IXLDefinedName n in wb.DefinedNames)
                    {
                        string bezug;
                        try { bezug = n.RefersTo ?? ""; } catch (Exception) { bezug = ""; }
                        if (Bezieht(bezug, name)) stellen.Add(ExcelVorlagentexte.Name(Englisch, n.Name));
                    }
                    if (stellen.Count > 0)
                        Melde(Befundstufe.Warnung, nameof(R.BV_XL_PRUEF_BLATT_BEZUG),
                              T(nameof(R.BV_XL_PRUEF_BLATT_BEZUG), name, string.Join("; ", stellen.Take(3)) + (stellen.Count > 3 ? " …" : "")),
                              ExcelVorlagentexte.Blatt(Englisch, name), T(nameof(R.BV_XL_PRUEF_BLATT_BEZUG_TUN)));
                }
            }

            /// <summary>Nennt der Formeltext das Blatt (<c>Name!</c> oder <c>'Name'!</c>)?</summary>
            private static bool Bezieht(string formel, string blatt)
            {
                if (string.IsNullOrEmpty(formel)) return false;
                string zitiert = "'" + blatt.Replace("'", "''") + "'!";
                return formel.IndexOf(zitiert, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       formel.IndexOf(blatt + "!", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            /// <summary>Konzept 7.2: Ein Anwenderblatt, das heißt wie ein erzeugtes Blatt, ist ein Fehler (<c>Worksheets.Add</c> würfe).</summary>
            internal void PruefeBlattnamen(XLWorkbook wb, ExcelVorlagenmappe mappe)
            {
                var markenblaetter = new HashSet<IXLWorksheet>(mappe.Marken.Select(m => m.Blatt));
                var marken = ExcelVorlagenmappe.Blattmarken.ToDictionary(p => p.Value, p => p.Key);
                IReadOnlyDictionary<ExcelBerichtGenerator.Blattart, string> feste = ExcelBerichtGenerator.FesteBlattnamen();
                foreach (IXLWorksheet ws in wb.Worksheets.Where(w => !markenblaetter.Contains(w)))
                    foreach (KeyValuePair<ExcelBerichtGenerator.Blattart, string> f in feste)
                        if (string.Equals(ws.Name, f.Value, StringComparison.OrdinalIgnoreCase))
                            Melde(Befundstufe.Fehler, nameof(R.BV_XL_PRUEF_BLATTNAME), T(nameof(R.BV_XL_PRUEF_BLATTNAME), ws.Name),
                                  ExcelVorlagentexte.Blatt(Englisch, ws.Name), T(nameof(R.BV_XL_PRUEF_BLATTNAME_TUN), "{{" + marken[f.Key] + "}}"));
            }

            // ------------------------------------------------------------ Namen

            internal void PruefeNamen(ExcelVorlagenmappe mappe)
            {
                foreach (Excelnamensfund n in mappe.Namen)
                {
                    string fundort = ExcelVorlagentexte.Name(Englisch, n.Name.Name);
                    if (n.Reserviert)
                    {
                        Melde(Befundstufe.Fehler, nameof(R.BV_XL_PRUEF_RESERVIERT), T(nameof(R.BV_XL_PRUEF_RESERVIERT), n.Name.Name), fundort,
                              T(nameof(R.BV_XL_PRUEF_RESERVIERT_TUN)));
                        continue;
                    }
                    // BV-E8: ein Name EPOS.reihe.* zeigt nach dem Füllen auf eine Rasterreihe des Stammprojekts.
                    if (Excelreihen.IstReihe(n.Schluessel))
                    {
                        _anzahl++;
                        if (!Excelreihen.Lies(n.Schluessel, out _, out _))
                            Melde(Befundstufe.Fehler, nameof(R.BV_XL_PRUEF_REIHE), T(nameof(R.BV_XL_PRUEF_REIHE), n.Name.Name), fundort,
                                  T(nameof(R.BV_XL_PRUEF_REIHE_TUN), Excelreihen.Liste()));
                        continue;
                    }
                    Platzhalter p = Platzhaltersyntax.Lies(n.Schluessel);
                    Pruefe(p, fundort, false, true, Fundquelle.Text, alsName: true);

                    int zellen = 0;
                    try
                    {
                        List<IXLRange> bereiche = n.Name.Ranges.ToList();
                        zellen = bereiche.Count > 1 ? 2 : bereiche.Sum(b => b.RowCount() * b.ColumnCount());
                    }
                    catch (Exception) { zellen = 0; }
                    if (zellen > 1)
                        Melde(Befundstufe.Fehler, nameof(R.BV_XL_PRUEF_NAME_BEREICH), T(nameof(R.BV_XL_PRUEF_NAME_BEREICH), n.Name.Name), fundort,
                              T(nameof(R.BV_XL_PRUEF_NAME_BEREICH_TUN)));
                }
            }

            /// <summary>
            /// BV-E8 (Konzept 7.3): die Excel-Tabellen <c>EPOS_&lt;name&gt;</c> — der Name nennt eine Tabelle des Katalogs, die
            /// keinen Stand braucht; Tabellen je Stand stehen auf dem Musterblatt — als Zellmarke oder als Excel-Tabelle (BV-E9).
            /// </summary>
            private static bool AufMuster(ExcelVorlagenmappe mappe, Exceltabellenfund t)
            {
                Excelblattmarke m = mappe.Muster;
                return m != null && string.Equals(t.Blattname, m.Name, StringComparison.OrdinalIgnoreCase);
            }

            internal void PruefeTabellen(ExcelVorlagenmappe mappe)
            {
                foreach (Exceltabellenfund t in mappe.Tabellen)
                {
                    _anzahl++;
                    string fundort = ExcelVorlagentexte.Tabelle(Englisch, t.Tabelle.Name);
                    Vorlagenfeld feld = t.Schluessel == null ? null : _katalog.Finde(t.Schluessel);
                    if (feld != null && !_schluessel.Contains(feld.Schluessel)) _schluessel.Add(feld.Schluessel);
                    if (feld == null || feld.Art != Vorlagenfeldart.Tabelle)
                        Melde(Befundstufe.Fehler, nameof(R.BV_XL_PRUEF_TABELLE), T(nameof(R.BV_XL_PRUEF_TABELLE), t.Tabelle.Name), fundort,
                              T(nameof(R.BV_XL_PRUEF_TABELLE_TUN)));
                    // BV-E9: eine Tabelle je Stand auf dem Musterblatt — jeder Klon füllt sie mit seinem Stand.
                    else if ((feld.Kontext == Vorlagenfeldkontext.Stand && !AufMuster(mappe, t)) || feld.Kontext == Vorlagenfeldkontext.Gebaeude)
                        Melde(Befundstufe.Fehler, nameof(R.BV_XL_PRUEF_TABELLE_STAND), T(nameof(R.BV_XL_PRUEF_TABELLE_STAND), t.Tabelle.Name), fundort,
                              T(nameof(R.BV_XL_PRUEF_TABELLE_STAND_TUN), "{{" + feld.Schluessel + "}}", "{{blatt.detail}}"));
                }
            }

            // ------------------------------------------------------------ Formeln, Paket

            /// <summary>Konzept 7.4: Formeln der Vorlage verlieren ihr zwischengespeichertes Ergebnis — ein Hinweis.</summary>
            internal void PruefeFormeln(ExcelVorlagenmappe mappe)
            {
                if (mappe.Formeln > 0)
                    Melde(Befundstufe.Hinweis, nameof(R.BV_XL_PRUEF_FORMELN), T(nameof(R.BV_XL_PRUEF_FORMELN), mappe.Formeln), Datei,
                          T(nameof(R.BV_XL_PRUEF_FORMELN_TUN)));
                if (mappe.Zellen.Count == 0 && mappe.Marken.Count == 0 && mappe.DoppelteMarken.Count == 0 && mappe.Namen.Count == 0
                    && mappe.Tabellen.Count == 0)
                    Melde(Befundstufe.Hinweis, nameof(R.BV_XL_LAUF_OHNE_PLATZHALTER), T(nameof(R.BV_XL_LAUF_OHNE_PLATZHALTER)), Datei,
                          T(nameof(R.BV_XL_PRUEF_OHNE_PLATZHALTER_TUN), "{{blatt.vergleich}}"));
                if (SpracheAbweichend)
                    Melde(Befundstufe.Warnung, nameof(R.VF_PRUEF_SPRACHE),
                          T(nameof(R.VF_PRUEF_SPRACHE),
                            T(_sprache.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? nameof(R.VF_PRUEF_SPRACHE_EN) : nameof(R.VF_PRUEF_SPRACHE_DE)),
                            T(Englisch ? nameof(R.VF_PRUEF_SPRACHE_EN) : nameof(R.VF_PRUEF_SPRACHE_DE))),
                          T(nameof(R.VF_PRUEF_ORT_EIGENSCHAFTEN)), T(nameof(R.VF_PRUEF_SPRACHE_TUN)));
            }

            /// <summary>
            /// Konzept 7.4, <b>Paketschutz</b>: Probe-Laden (geschehen) und -Speichern mit ClosedXML, Vergleich nach
            /// Inhaltstyp mit Positivliste der erwarteten Verluste (<c>calcChain</c>, Druckereinstellungen); was fehlt,
            /// steht mit Namen da (Pivot, Formen, Steuerelemente …). Eine Ausnahme beim Speichern ist ein benannter Fehler.
            /// </summary>
            internal void PruefePaket(XLWorkbook wb, byte[] arbeit)
            {
                byte[] probe;
                try
                {
                    using (var strom = new MemoryStream())
                    {
                        wb.SaveAs(strom);
                        probe = strom.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Melde(Befundstufe.Fehler, nameof(R.BV_XL_PRUEF_SPEICHERN), T(nameof(R.BV_XL_PRUEF_SPEICHERN), ex.Message), Datei,
                          T(nameof(R.BV_XL_PRUEF_UNLESBAR_TUN)));
                    return;
                }
                try
                {
                    List<string> verluste = ExcelVorlagenmappe.Verluste(ExcelVorlagenmappe.Paketinhalt(arbeit),
                                                                        ExcelVorlagenmappe.Paketinhalt(probe), Englisch);
                    if (verluste.Count > 0)
                        Melde(Befundstufe.Warnung, nameof(R.BV_XL_PRUEF_VERLUST), T(nameof(R.BV_XL_PRUEF_VERLUST), string.Join(", ", verluste)),
                              Datei, T(nameof(R.BV_XL_PRUEF_VERLUST_TUN)));
                }
                catch (Exception ex)
                {
                    Melde(Befundstufe.Fehler, nameof(R.BV_XL_PRUEF_SPEICHERN), T(nameof(R.BV_XL_PRUEF_SPEICHERN), ex.Message), Datei,
                          T(nameof(R.BV_XL_PRUEF_UNLESBAR_TUN)));
                }
            }

            // ------------------------------------------------------------ Befund

            internal Pruefbefund Befund(string summe, bool lesbar)
            {
                bool wirtschaft = _schluessel.Any(k => Vorlagenpruefer.Wirtschaftsbereiche.Any(b => k.StartsWith(b, StringComparison.Ordinal))) ||
                                  _schluessel.Contains("blatt.wirtschaftlichkeit") || _schluessel.Contains("blatt.verlauf") ||
                                  _schluessel.Contains("blatt.checkliste");
                return new Pruefbefund(_stufe, _meldungen, _funde, _anzahl, _schluessel.OrderBy(k => k, StringComparer.Ordinal).ToList(),
                                       _unbekannte, _fassung, _sprache, SpracheAbweichend, false, wirtschaft, null, summe, lesbar, 0);
            }
        }
    }
}
