using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Excel-Engine der Berichtsvorlagen</b> (Konzept Berichtsvorlagen 4.4, 4.7, 4.10, 7.1–7.4; Etappe BV-E7) —
    /// füllt eine Excel-Vorlage des Anwenders mit den Werten des Laufs und setzt die heute erzeugten Blätter an ihre
    /// Blattmarken. Ohne Vorlage bleibt alles, wie es ist: Die Mappe entsteht dann im Code
    /// (<see cref="ExcelBerichtGenerator.Erzeuge"/>, Konzept 7.1 „die Standard-.xlsx entsteht im Code“).
    ///
    /// <para><b>Ablauf.</b> (1) Die Bytes werden über das SDK auf eine Arbeitsmappe umgestellt, falls sie eine
    /// <c>.xltx</c> sind; Makros (<c>.xlsm</c>, <c>.xltm</c>, VBA-Projekt) lehnt die Engine ab
    /// (<see cref="NotSupportedException"/>). (2) ClosedXML lädt die Mappe (<c>new XLWorkbook(stream)</c>); eine
    /// Ladeausnahme wird ein benannter Fehler (<see cref="InvalidDataException"/>). (3) Reservierte Namen der Formelmappe
    /// werden entfernt, Blattmarken und gleichnamige Anwenderblätter umbenannt — sonst würfe <c>Worksheets.Add</c>.
    /// (4) Die Zellplatzhalter der Anwenderblätter werden gefüllt: allein in der Zelle ein typisierter Wert (Zahl als
    /// Zahl, Datum als Datum, Format der Zielzelle nur bei „Standard“ gesetzt, Prozentregel 7.1), im Satz Text.
    /// (5) Die erzeugten Blätter entstehen über dieselben Blattbauer wie ohne Vorlage
    /// (<see cref="ExcelBerichtGenerator.SchreibeBlaetter"/>) in Calibri 11 — ausdrücklich, damit sie keine
    /// Vorlagenschrift erben —, treten an die Stelle ihrer Blattmarke (heutiger Name) oder hängen hinten an; eine Marke
    /// ohne Inhalt entfällt mit ihrem Blatt. Das Musterblatt <c>blatt.detail</c> wird je Stand geklont und mit den
    /// <c>stand.*</c>-Platzhaltern gefüllt. (6) Die Namen <c>EPOS.*</c>/<c>EPOS_*</c> bekommen ihren Wert — in ihre
    /// Zelle bzw. als Konstante über <c>RefersTo</c>. (7) Trägt die Vorlage Formeln, rechnet Excel beim Öffnen neu
    /// (<c>FullCalculationOnLoad</c>). (8) Der Paketvergleich nennt, was ClosedXML beim Füllen verlor.</para>
    ///
    /// <para><b>Tabellen und Diagramme (BV-E8):</b> <c>{{tabelle.…}}</c> allein in einer Zelle wird ein erzeugter Bereich —
    /// listentauglich als Excel-Tabelle <c>EPOS_&lt;name&gt;</c> —, eine Excel-Tabelle <c>EPOS_&lt;name&gt;</c> der Vorlage wird
    /// Zeile für Zeile gefüllt und wächst, <c>{{bild.…}}</c> allein in einer Zelle wird das Excel-Diagramm des Bildes an dieser
    /// Zelle, Namen <c>EPOS.reihe.*</c> zeigen auf Rasterreihen des Stammprojekts. Die erzeugten Blätter tragen ihre Diagramme
    /// wie ohne Vorlage; die Zahlen stehen im Blatt „Diagrammdaten“ (Marke <c>blatt.diagrammdaten</c>). Diagramme der
    /// Vorlage behalten ihre Bezüge, auf gewachsene Tabellen nachgezogen, mit neuem Zwischenspeicher (<see cref="Diagrammplan"/>).
    /// Die Engine liest nur <see cref="BerichtsDaten"/> (Wache <c>BerichtSchreiberOhneDatenbankWacheTests</c>).</para>
    /// </summary>
    public sealed class ExcelVorlagenfueller
    {
        /// <summary>Die Schrift der erzeugten Blätter (Konzept 7.1: ausdrücklich, nie aus der Vorlage).</summary>
        public const string SCHRIFT = "Calibri";

        /// <summary>Die Schriftgröße der erzeugten Blätter.</summary>
        public const double SCHRIFTGROESSE = 11;

        /// <summary>Der Inhaltstyp eines Tabellenblatts — Blätter entfallen mit ihrer Marke, das ist kein Verlust.</summary>
        private const string TYP_BLATT = "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml";

        /// <summary>
        /// Füllt die Vorlage <paramref name="vorlage"/> und schreibt die Mappe nach <paramref name="zielDatei"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Keine Berichtsdaten, oder die Vorlage ist leer (Parameter <c>vorlage</c>).</exception>
        /// <exception cref="InvalidDataException">Keine Excel-Arbeitsmappe, oder ClosedXML kann sie nicht laden.</exception>
        /// <exception cref="NotSupportedException">Eine Vorlage mit Makros.</exception>
        public Fuellergebnis Fuelle(byte[] vorlage, BerichtsDaten daten, BerichtsKonfiguration konfig,
                                    Erstellerangaben ersteller, string zielDatei)
        {
            if (daten == null || daten.Varianten == null || daten.Varianten.Count == 0)
                throw new ArgumentException("Keine Berichtsdaten vorhanden.");
            bool englisch = BerichtTexte.Englisch;

            byte[] arbeit = ExcelVorlagenmappe.Normalisiere(vorlage, englisch, out bool warVorlage);
            Dictionary<string, int> vorher = ExcelVorlagenmappe.Paketinhalt(arbeit);
            var ergebnis = new Fuellergebnis(zielDatei, englisch);
            if (warVorlage) ergebnis.Hinweis(ExcelVorlagentexte.T(englisch, nameof(R.BV_XL_LAUF_XLTX)));

            ExcelBerichtGenerator.GrafikModulSicherstellen();
            var formeln = new Formelregister();
            Berichtswerte werte = Berichtswerte.Aus(daten, konfig, englisch, ersteller);

            // BV-E8 (Konzept 7.4): die Excel-Diagramme — der erzeugten Blätter wie ohne Vorlage, dazu die der Bildplatzhalter.
            var diagramme = new Diagrammplan(daten, englisch, werte.Wirtschaft);

            using (XLWorkbook wb = ExcelVorlagenmappe.Lade(arbeit, englisch))
            {
                ExcelVorlagenmappe mappe = ExcelVorlagenmappe.Lies(wb);
                new Sitzung(wb, mappe, werte, ergebnis, englisch, diagramme).Fuelle(daten, konfig, formeln);
                diagramme.Festhalten();

                // Konzept 7.4: sobald die Vorlage irgendeine Formel trägt, rechnet Excel beim Öffnen neu — ClosedXML
                // verliert das zwischengespeicherte Ergebnis (<v>) jeder Formel (Messprobe 2 von BV-E0).
                if (mappe.Formeln > 0)
                    ergebnis.Hinweis(ExcelVorlagentexte.T(englisch, nameof(R.BV_XL_LAUF_FORMELN), mappe.Formeln));
                if (mappe.Formeln > 0 || formeln.Anzahl > 0) wb.FullCalculationOnLoad = true;
                wb.SaveAs(zielDatei);
            }

            // Die Ergebnisse der Formelmappe nachtragen (wie ohne Vorlage, ExcelBerichtGenerator.Erzeuge).
            formeln.Nachtragen(zielDatei);

            // BV-E8: die Diagramme über das SDK, nach ClosedXML und dem Nachtrag (wie ohne Vorlage).
            diagramme.Anlegen(zielDatei);

            // Konzept 7.4, Paketschutz: was ClosedXML beim Füllen verlor, steht mit Namen in der Laufmeldung.
            try
            {
                List<string> verluste = ExcelVorlagenmappe.Verluste(vorher, ExcelVorlagenmappe.Paketinhalt(File.ReadAllBytes(zielDatei)),
                                                                    englisch, new[] { TYP_BLATT });
                if (verluste.Count > 0)
                    ergebnis.Warnung(ExcelVorlagentexte.T(englisch, nameof(R.BV_XL_LAUF_VERLUST), string.Join(", ", verluste)));
            }
            catch (Exception)
            {
                // Der Vergleich ist Auskunft; die Mappe steht.
            }
            return ergebnis;
        }

        /// <summary>
        /// Die Standardmappe als Vorlage (Konzept 7.2: „Die Standardvorlage trägt nur Blattmarken in heutiger Folge“) —
        /// sechs Blätter mit je einer Blattmarke in A1. Als Datei der Auslieferung gibt es sie nicht (die Standard-Mappe
        /// entsteht im Code, 7.1); sie ist der Nachweis, dass der Vorlagenweg dieselbe Mappe baut wie der Weg ohne Vorlage,
        /// und liegt als Ausgangspunkt eigener Excel-Vorlagen im Musterordner des Vorlagenordners
        /// (<see cref="BerichtsvorlagenCtrl.DATEI_EXCEL_STANDARD"/>, Anwenderentscheid BV-E7-6).
        /// </summary>
        public static byte[] Standardmappe()
        {
            using (var wb = new XLWorkbook())
            {
                int n = 1;
                foreach (string marke in ExcelVorlagenmappe.Blattmarken.Keys)
                {
                    IXLWorksheet ws = wb.Worksheets.Add("Blatt" + n.ToString(CultureInfo.InvariantCulture));
                    ws.Cell(1, 1).Value = "{{" + marke + "}}";
                    n++;
                }
                using (var strom = new MemoryStream())
                {
                    wb.SaveAs(strom);
                    return strom.ToArray();
                }
            }
        }

        // =====================================================================
        //  Die Sitzung eines Füllens
        // =====================================================================

        private sealed class Sitzung
        {
            private readonly XLWorkbook _wb;
            private readonly ExcelVorlagenmappe _mappe;
            private readonly Berichtswerte _werte;
            private readonly Fuellergebnis _e;
            private readonly bool _englisch;
            private readonly Diagrammplan _diagramme;

            private readonly Dictionary<ExcelBerichtGenerator.Blattart, List<IXLWorksheet>> _erzeugt =
                new Dictionary<ExcelBerichtGenerator.Blattart, List<IXLWorksheet>>();

            /// <summary>Die Tabellen an Zellmarken, die noch zu schreiben sind (BV-E8) — nach allen Zellen eines Blattes, von unten.</summary>
            private readonly List<Bereichsauftrag> _bereiche = new List<Bereichsauftrag>();

            /// <summary>Die Datenbereiche der Namen <c>EPOS.reihe.*</c>, bis ihr <c>RefersTo</c> gesetzt ist.</summary>
            private readonly Dictionary<Excelnamensfund, Diagrammplan.Block> _reihen = new Dictionary<Excelnamensfund, Diagrammplan.Block>();

            internal Sitzung(XLWorkbook wb, ExcelVorlagenmappe mappe, Berichtswerte werte, Fuellergebnis e, bool englisch,
                             Diagrammplan diagramme)
            {
                _wb = wb;
                _mappe = mappe;
                _werte = werte;
                _e = e;
                _englisch = englisch;
                _diagramme = diagramme;
            }

            private string T(string schluessel, params object[] argumente)
            {
                return ExcelVorlagentexte.T(_englisch, schluessel, argumente);
            }

            internal void Fuelle(BerichtsDaten daten, BerichtsKonfiguration konfig, Formelregister formeln)
            {
                if (_mappe.Zellen.Count == 0 && _mappe.Marken.Count == 0 && _mappe.DoppelteMarken.Count == 0 && _mappe.Namen.Count == 0
                    && _mappe.Tabellen.Count == 0)
                {
                    _e.OhnePlatzhalter = true;
                    _e.Hinweis(T(nameof(R.BV_XL_LAUF_OHNE_PLATZHALTER)));
                }

                EntferneReservierteNamen();
                MarkenBeiseite();
                GleichnamigeUmbenennen();

                // BV-E8: Diagramme der Vorlage behalten ihre Bezüge nur, wenn EPOS sie nachzieht (Messprobe 2 von BV-E0).
                if (_diagramme != null) _diagramme.VorlageNachfuehren = true;

                // Die Zellplatzhalter der Anwenderblätter (nicht der Marken- und Musterblätter).
                var markenblaetter = new HashSet<IXLWorksheet>(_mappe.Marken.Select(m => m.Blatt));
                foreach (Excelzellfund f in _mappe.Zellen.Where(z => !markenblaetter.Contains(z.Zelle.Worksheet)))
                    FuelleZelle(f.Zelle, f.Text, f.Marken, _werte, false,
                                ExcelVorlagentexte.Zelle(_englisch, f.Zelle.Worksheet.Name, f.Adresse));
                SchreibeBereiche();
                FuelleTabellen();
                PlaneReihen();
                foreach (Excelblattmarke doppelt in _mappe.DoppelteMarken)
                {
                    IXLCell a1 = doppelt.Blatt.Cell(1, 1);
                    Gelb(a1);
                    _e.Unbekannt(new Fuellbefund("{{" + doppelt.Schluessel + "}}",
                                                 ExcelVorlagentexte.Zelle(_englisch, doppelt.Blatt.Name, "A1"),
                                                 Fuellbefundart.Doppelt, T(nameof(R.BV_XL_GRUND_DOPPELT))));
                }

                ErzeugeBlaetter(daten, konfig, formeln);
                SetzeAnDieMarken();
                FuelleNamen();
            }

            // ------------------------------------------------------------ Namen, Marken, Blattnamen

            /// <summary>Konzept 7.4: Die Namen der Formelmappe sind reserviert — eine Vorlage, die sie führt, lehnt der
            /// Prüfer ab; die Engine entfernt sie, sonst scheiterte die Formelmappe an ihnen.</summary>
            private void EntferneReservierteNamen()
            {
                foreach (Excelnamensfund n in _mappe.Namen.Where(n => n.Reserviert))
                {
                    try
                    {
                        if (n.Bereich == null) _wb.DefinedNames.Delete(n.Name.Name);
                        else n.Bereich.DefinedNames.Delete(n.Name.Name);
                    }
                    catch (Exception)
                    {
                        // steht im Satz darunter
                    }
                    _e.Warnung(T(nameof(R.BV_XL_LAUF_RESERVIERT), n.Name.Name));
                }
            }

            /// <summary>Die Markenblätter bekommen Namen, die kein erzeugtes Blatt trägt — sie werden ohnehin ersetzt.</summary>
            private void MarkenBeiseite()
            {
                int i = 1;
                foreach (Excelblattmarke m in _mappe.Marken)
                {
                    string name;
                    do { name = "EPOS~" + i.ToString(CultureInfo.InvariantCulture); i++; }
                    while (_wb.Worksheets.Any(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)));
                    m.Blatt.Name = name;
                    if (!m.Leer && !m.IstMuster)
                        _e.Warnung(T(nameof(R.BV_XL_LAUF_NICHT_LEER), m.Name, "{{" + m.Schluessel + "}}"));
                }
            }

            /// <summary>Ein Anwenderblatt, das heißt wie ein erzeugtes Blatt, bekommt einen freien Namen (der Prüfer meldet
            /// es vorher als Fehler) — sonst würfe <c>Worksheets.Add</c>.</summary>
            private void GleichnamigeUmbenennen()
            {
                var feste = new HashSet<string>(ExcelBerichtGenerator.FesteBlattnamen().Values, StringComparer.OrdinalIgnoreCase);
                var marken = new HashSet<IXLWorksheet>(_mappe.Marken.Select(m => m.Blatt));
                foreach (IXLWorksheet ws in _wb.Worksheets.ToList())
                {
                    if (marken.Contains(ws) || !feste.Contains(ws.Name)) continue;
                    string alt = ws.Name;
                    string basis = alt.Length > 20 ? alt.Substring(0, 20) : alt;
                    string neu = basis + " (" + T(nameof(R.BV_XL_BLATT_VORLAGE)) + ")";
                    if (neu.Length > 31) neu = neu.Substring(0, 31);
                    int n = 2;
                    while (_wb.Worksheets.Any(w => string.Equals(w.Name, neu, StringComparison.OrdinalIgnoreCase)))
                        neu = basis + " (" + n++.ToString(CultureInfo.InvariantCulture) + ")";
                    ws.Name = neu;
                    _e.Warnung(T(nameof(R.BV_XL_LAUF_UMBENANNT), alt, neu));
                }
            }

            // ------------------------------------------------------------ Erzeugte Blätter

            private void ErzeugeBlaetter(BerichtsDaten daten, BerichtsKonfiguration konfig, Formelregister formeln)
            {
                Excelblattmarke muster = _mappe.Muster;

                // Konzept 7.1: Die erzeugten Blätter setzen Calibri 11 ausdrücklich — neue Blätter nehmen die Schrift der
                // Mappe, und die Vermessung (AdjustToContents) liefe sonst mit einer Vorlagenschrift außerhalb der
                // Rückfallkette. Danach gilt wieder, was die Mappe trug.
                string schrift = _wb.Style.Font.FontName;
                double groesse = _wb.Style.Font.FontSize;
                _wb.Style.Font.FontName = SCHRIFT;
                _wb.Style.Font.FontSize = SCHRIFTGROESSE;
                try
                {
                    ExcelBerichtGenerator.SchreibeBlaetter(_wb, daten, konfig, formeln,
                        (art, stand) =>
                        {
                            if (art != ExcelBerichtGenerator.Blattart.Detail || muster == null) return true;
                            Klone(muster, stand);
                            return false;
                        },
                        (art, stand, blaetter) =>
                        {
                            foreach (IXLWorksheet ws in blaetter) Liste(art).Add(ws);
                        },
                        _diagramme);

                    // BV-E8: die Zahlen der Diagramme — zuletzt, wenn alle Diagramme geplant sind (auch die der Musterblätter).
                    IXLWorksheet daten2 = _diagramme?.SchreibeDatenblatt(_wb);
                    if (daten2 != null) Liste(ExcelBerichtGenerator.Blattart.Diagrammdaten).Add(daten2);
                }
                finally
                {
                    _wb.Style.Font.FontName = schrift;
                    _wb.Style.Font.FontSize = groesse;
                }

                // Gemessen (ClosedXML 0.105.1): Eine Zelle, deren Schrift der Standardschrift gleicht, schreibt ClosedXML mit
                // dem Format 0 — und das trägt beim Speichern die Schrift 0 der Vorlage (Aptos). Der Zeichensatz macht die
                // Schrift der erzeugten Blätter unterscheidbar, ohne sie zu ändern: Sie bleiben Calibri 11.
                foreach (IXLWorksheet ws in _erzeugt.Where(p => p.Key != ExcelBerichtGenerator.Blattart.Detail || muster == null)
                                                    .SelectMany(p => p.Value))
                {
                    ws.Style.Font.FontName = SCHRIFT;
                    ws.Style.Font.FontCharSet = XLFontCharSet.Ansi;
                }
            }

            private List<IXLWorksheet> Liste(ExcelBerichtGenerator.Blattart art)
            {
                if (!_erzeugt.TryGetValue(art, out List<IXLWorksheet> l)) _erzeugt[art] = l = new List<IXLWorksheet>();
                return l;
            }

            // ------------------------------------------------------------ Tabellen und Diagramme (BV-E8)

            /// <summary>Eine Tabelle an einer Zellmarke, die nach allen Zellen ihres Blattes geschrieben wird.</summary>
            private sealed class Bereichsauftrag
            {
                internal IXLWorksheet Blatt;
                internal int Zeile, Spalte;
                internal Vorlagenfeld Feld;
                internal Berichtstabelle Tabelle;
                internal string Leertext;
            }

            /// <summary>
            /// Konzept 7.3: die Tabellen der Zellmarken als erzeugte Bereiche — je Blatt von unten nach oben, je Zeile einmal so
            /// viele Zeilen eingefügt, wie die längste Tabelle der Zeile braucht (Inhalte darunter wandern mit). Eine
            /// listentaugliche Tabelle wird eine Excel-Tabelle <c>EPOS_&lt;name&gt;</c>.
            /// </summary>
            private void SchreibeBereiche()
            {
                foreach (IGrouping<IXLWorksheet, Bereichsauftrag> blatt in _bereiche.GroupBy(b => b.Blatt).ToList())
                    foreach (IGrouping<int, Bereichsauftrag> zeile in blatt.GroupBy(b => b.Zeile).OrderByDescending(g => g.Key))
                    {
                        int mehr = zeile.Max(b => Excelbereiche.Zeilen(b.Tabelle)) - 1;
                        if (mehr > 0) blatt.Key.Row(zeile.Key).InsertRowsBelow(mehr);
                        foreach (Bereichsauftrag b in zeile)
                        {
                            b.Blatt.Cell(b.Zeile, b.Spalte).Value = Blank.Value;
                            Excelbereiche.Schreibe(b.Blatt, b.Zeile, b.Spalte, b.Tabelle, b.Feld.Schluessel, b.Leertext);
                        }
                    }
                _bereiche.Clear();
            }

            /// <summary>
            /// Konzept 7.3, 7.4: die Excel-Tabellen <c>EPOS_&lt;name&gt;</c> der Vorlage — Zeile für Zeile gefüllt, sie wachsen
            /// oder schrumpfen; ihre Diagramme zieht der Diagrammplan nach. Eine Tabelle, die nicht listentauglich ist (Spalten je
            /// Stand, Gruppenzeilen), bleibt unverändert und steht mit Warnung im Ergebnis.
            /// </summary>
            private void FuelleTabellen()
            {
                foreach (Exceltabellenfund f in _mappe.Tabellen)
                {
                    string fundort = ExcelVorlagentexte.Tabelle(_englisch, f.Tabelle.Name);
                    Platzhalter p = Platzhaltersyntax.Lies(f.Schluessel ?? "");
                    Vorlagenfeld feld = p.Art == Platzhalterart.Feld ? Vorlagenfeldkatalog.Finde(p.Schluessel) : null;
                    if (feld == null || feld.Art != Vorlagenfeldart.Tabelle)
                    {
                        Stehen("{{" + f.Schluessel + "}}", feld == null ? Excelstelle.Unbekannt : Excelstelle.FalscheArt, fundort);
                        continue;
                    }
                    if (feld.Kontext == Vorlagenfeldkontext.Stand || feld.Kontext == Vorlagenfeldkontext.Gebaeude)
                    {
                        Stehen("{{" + f.Schluessel + "}}", Excelstelle.Kontext, fundort);
                        continue;
                    }
                    Platzhalterwert wert = Vorlagenfeldkatalog.Loese(feld, _werte, p.Angaben);
                    Zaehle(feld, wert, p, fundort);
                    Berichtstabelle t = wert.Tabelle;
                    if (t == null || t.Kopf == null || (!t.Listentauglich && t.Zeilen.Count > 0))
                    {
                        _e.Warnung(T(nameof(R.BV_XL_LAUF_NICHT_LISTE), "{{" + feld.Schluessel + "}}", f.Tabelle.Name));
                        continue;
                    }
                    try { _diagramme?.Gewachsen(Excelbereiche.Fuelle(f.Tabelle, t)); }
                    catch (Exception ex) { _e.Warnung(T(nameof(R.BV_XL_LAUF_AUSNAHME), "{{" + feld.Schluessel + "}}", fundort, ex.Message)); }
                }
            }

            /// <summary>Die Namen <c>EPOS.reihe.*</c> bekommen einen Datenbereich im Blatt „Diagrammdaten“ (vor dessen Schreiben).</summary>
            private void PlaneReihen()
            {
                if (_diagramme == null) return;
                foreach (Excelnamensfund n in _mappe.Namen.Where(n => !n.Reserviert && Excelreihen.IstReihe(n.Schluessel)))
                {
                    Exceldiagramm d = Excelreihen.Daten(_werte.Daten, n.Schluessel, _englisch);
                    Diagrammplan.Block b = d == null ? null : _diagramme.Datenbereich(Excelreihen.Kennung(n.Schluessel), d);
                    if (b != null) _reihen[n] = b;
                }
            }

            /// <summary>Ein Bildplatzhalter allein in einer Zelle: das Excel-Diagramm des Bildes an dieser Zelle.</summary>
            private void SetzeDiagramm(IXLCell zelle, Vorlagenfeld feld, Platzhalter p, Berichtswerte w, string fundort)
            {
                Platzhalterwert wert = Vorlagenfeldkatalog.Loese(feld, w, p.Angaben);
                Zaehle(feld, wert, p, fundort);
                if (!wert.IstLeer && _diagramme != null && _diagramme.AnZelle(zelle, feld.Schluessel, w.LaufenderStand)) return;
                bool mitGrund = p.Angaben.Any(a => a.Art == Formatangabeart.MitGrund);
                if (mitGrund && wert.IstLeer && wert.Text.Length > 0) zelle.Value = wert.Text;
                else zelle.Value = Blank.Value;
                if (!wert.IstLeer) _e.Hinweis(T(nameof(R.BV_XL_LAUF_KEIN_DIAGRAMM), p.Normalform, fundort));
            }

            /// <summary>Ein Tabellenplatzhalter allein in einer Zelle: vorgemerkt als erzeugter Bereich (<see cref="SchreibeBereiche"/>).</summary>
            private void MerkeBereich(IXLCell zelle, Vorlagenfeld feld, Platzhalter p, Berichtswerte w, string fundort)
            {
                Platzhalterwert wert = Vorlagenfeldkatalog.Loese(feld, w, p.Angaben);
                Zaehle(feld, wert, p, fundort);
                bool mitGrund = p.Angaben.Any(a => a.Art == Formatangabeart.MitGrund);
                _bereiche.Add(new Bereichsauftrag
                {
                    Blatt = zelle.Worksheet,
                    Zeile = zelle.Address.RowNumber,
                    Spalte = zelle.Address.ColumnNumber,
                    Feld = feld,
                    Tabelle = wert.IstLeer ? null : wert.Tabelle,
                    Leertext = mitGrund && wert.IstLeer ? wert.Text : "",
                });
            }

            /// <summary>Konzept 7.2: Das Musterblatt je Stand geklont (<c>CopyTo</c>), benannt wie das heutige
            /// Detailblatt, mit dem Stand als Kontext gefüllt; die Blattmarke in A1 wird geleert.</summary>
            private void Klone(Excelblattmarke muster, VariantenDaten stand)
            {
                IXLWorksheet klon = muster.Blatt.CopyTo(ExcelBerichtGenerator.BlattName(_wb, stand));
                klon.Cell(1, 1).Value = Blank.Value;
                Berichtswerte w = _werte.MitStand(stand);
                foreach (IXLCell zelle in klon.CellsUsed(XLCellsUsedOptions.Contents).ToList())
                {
                    if (zelle.HasFormula || !zelle.Value.IsText) continue;
                    string text = zelle.Value.GetText();
                    if (text.IndexOf("{{", StringComparison.Ordinal) < 0) continue;
                    List<Platzhalter> marken = Platzhaltersyntax.Finde(text).ToList();
                    if (marken.Count == 0) continue;
                    FuelleZelle(zelle, text, marken, w, true, ExcelVorlagentexte.Zelle(_englisch, klon.Name, zelle.Address.ToString()));
                }
                SchreibeBereiche();
                Liste(ExcelBerichtGenerator.Blattart.Detail).Add(klon);
            }

            /// <summary>
            /// Konzept 7.2: Jedes erzeugte Blatt tritt an die Stelle seiner Blattmarke (in der Folge seiner Entstehung),
            /// das Markenblatt entfällt; eine Marke ohne Inhalt entfällt mit ihrem Blatt. Blätter ohne Marke bleiben
            /// hinten angehängt.
            /// </summary>
            private void SetzeAnDieMarken()
            {
                foreach (Excelblattmarke m in _mappe.Marken)
                {
                    _erzeugt.TryGetValue(m.Art, out List<IXLWorksheet> neu);
                    if (neu == null || neu.Count == 0)
                    {
                        _e.Hinweis(T(nameof(R.BV_XL_LAUF_ENTFAELLT), "{{" + m.Schluessel + "}}", m.Name));
                        m.Blatt.Delete();
                        continue;
                    }
                    foreach (IXLWorksheet ws in neu) ws.Position = m.Blatt.Position;
                    m.Blatt.Delete();
                }
            }

            // ------------------------------------------------------------ Zellen

            /// <summary>Füllt eine Zelle: eine Marke allein → typisierter Wert, sonst Textersetzung im Satz.</summary>
            private void FuelleZelle(IXLCell zelle, string text, IReadOnlyList<Platzhalter> marken, Berichtswerte w,
                                     bool aufMuster, string fundort)
            {
                if (marken.Count == 1 && string.Equals(text.Trim(), marken[0].Roh, StringComparison.Ordinal))
                {
                    Platzhalter p = marken[0];
                    Vorlagenfeld feld = Vorlagenfeldkatalog.Finde(p.Schluessel);
                    Excelstelle stelle = ExcelVorlagenmappe.Beurteile(p, feld, aufMuster, true);
                    if (stelle != Excelstelle.Gut)
                    {
                        Gelb(zelle);
                        Stehen(p.Normalform, stelle, fundort);
                        return;
                    }
                    if (feld.Art == Vorlagenfeldart.Bild) { SetzeDiagramm(zelle, feld, p, w, fundort); return; }
                    if (feld.Art == Vorlagenfeldart.Tabelle) { MerkeBereich(zelle, feld, p, w, fundort); return; }
                    Schreibe(zelle, feld, p, Vorlagenfeldkatalog.Loese(feld, w, p.Angaben), fundort);
                    return;
                }

                var satz = new System.Text.StringBuilder();
                int pos = 0;
                bool stehen = false;
                foreach (Platzhalter p in marken)
                {
                    if (p.Position < pos) continue;
                    satz.Append(text, pos, p.Position - pos);
                    Vorlagenfeld feld = p.Art == Platzhalterart.Feld ? Vorlagenfeldkatalog.Finde(p.Schluessel) : null;
                    Excelstelle stelle = ExcelVorlagenmappe.Beurteile(p, feld, aufMuster, false);
                    if (stelle == Excelstelle.Gut)
                    {
                        Platzhalterwert wert = Vorlagenfeldkatalog.Loese(feld, w, p.Angaben);
                        Zaehle(feld, wert, p, fundort);
                        satz.Append(wert.Text);
                    }
                    else
                    {
                        satz.Append(p.Roh);
                        Stehen(p.Normalform, stelle, fundort);
                        stehen = true;
                    }
                    pos = p.Position + p.Laenge;
                }
                if (pos < text.Length) satz.Append(text, pos, text.Length - pos);
                zelle.Value = satz.ToString();
                if (stehen) Gelb(zelle);
            }

            /// <summary>Ein typisierter Wert (Konzept 4.6, 7.1): Zahl als Zahl, Datum als Datum, Liste zeilenweise, Text
            /// als Text; ohne Wert eine leere Zelle (4.10), mit <c>|mit grund</c> der Leerwert samt Grund.</summary>
            private void Schreibe(IXLCell zelle, Vorlagenfeld feld, Platzhalter p, Platzhalterwert wert, string fundort)
            {
                Zaehle(feld, wert, p, fundort);
                if (wert.IstLeer)
                {
                    bool mitGrund = p.Angaben.Any(a => a.Art == Formatangabeart.MitGrund);
                    if (mitGrund && wert.Text.Length > 0) zelle.Value = wert.Text;
                    else zelle.Value = Blank.Value;
                    return;
                }

                switch (feld.Art)
                {
                    case Vorlagenfeldart.Zahl:
                        SchreibeZahl(zelle, feld, p, wert.Zahl ?? 0.0, fundort);
                        return;
                    case Vorlagenfeldart.Datum:
                        {
                            // Vor dem Wert gefragt: ClosedXML setzt beim Zuweisen eines Datums selbst ein Datumsformat.
                            bool standard = IstStandard(zelle);
                            zelle.Value = wert.Datum ?? DateTime.MinValue;
                            if (standard) SetzeDatumsformat(zelle, feld, p);
                            return;
                        }
                    case Vorlagenfeldart.Liste:
                        zelle.Value = string.Join("\n", wert.Zeilen);
                        return;
                    default:
                        zelle.Value = wert.Text;
                        return;
                }
            }

            private void SchreibeZahl(IXLCell zelle, Vorlagenfeld feld, Platzhalter p, double zahl, string fundort)
            {
                bool standard = IstStandard(zelle);
                bool prozent = IstProzenteinheit(feld);
                bool anteil = false;
                if (prozent && !standard && IstProzentformat(zelle))
                {
                    // Konzept 7.1: Kennzahlen mit „%“ sind 0–100; eine Zielzelle mit Prozentformat bekommt den Anteil.
                    anteil = true;
                    _e.Hinweis(T(nameof(R.BV_XL_LAUF_PROZENT), fundort));
                }
                else if (prozent && standard && IstParameter(feld))
                {
                    // Anhang A: die Parameter der Wirtschaftlichkeit stehen in Excel als Anteil (wie in der Formelmappe).
                    anteil = true;
                }
                zelle.Value = anteil ? zahl / 100.0 : zahl;
                if (standard)
                    zelle.Style.NumberFormat.Format = anteil ? Prozentformat(feld, p) : Zahlformat(feld, p);
            }

            // ------------------------------------------------------------ Namen

            /// <summary>
            /// Die Namen <c>EPOS.*</c>/<c>EPOS_*</c> (Konzept 4.4, 7.4): verweist der Name auf eine Zelle, bekommt die Zelle den
            /// typisierten Wert; verweist er auf keinen Bereich, wird sein <c>RefersTo</c> der Wert als Konstante. Ein Bereich
            /// aus mehreren Zellen (Reihen, BV-E8) bleibt unberührt und steht im Ergebnis.
            /// </summary>
            private void FuelleNamen()
            {
                foreach (Excelnamensfund n in _mappe.Namen.Where(n => !n.Reserviert))
                {
                    string fundort = ExcelVorlagentexte.Name(_englisch, n.Name.Name);
                    if (Excelreihen.IstReihe(n.Schluessel))
                    {
                        FuelleReihe(n, fundort);
                        continue;
                    }
                    Platzhalter p = Platzhaltersyntax.Lies(n.Schluessel);
                    Vorlagenfeld feld = p.Art == Platzhalterart.Feld ? Vorlagenfeldkatalog.Finde(p.Schluessel) : null;
                    Excelstelle stelle = ExcelVorlagenmappe.Beurteile(p, feld, false, true, alsName: true);
                    if (stelle != Excelstelle.Gut)
                    {
                        Stehen("{{" + n.Schluessel + "}}", stelle, fundort);
                        continue;
                    }

                    List<IXLRange> bereiche;
                    try { bereiche = n.Name.Ranges.ToList(); }
                    catch (Exception) { bereiche = new List<IXLRange>(); }

                    Platzhalterwert wert = Vorlagenfeldkatalog.Loese(feld, _werte, p.Angaben);
                    if (bereiche.Count == 1 && bereiche[0].RangeAddress.FirstAddress.Equals(bereiche[0].RangeAddress.LastAddress))
                    {
                        Schreibe(bereiche[0].FirstCell(), feld, p, wert, fundort);
                    }
                    else if (bereiche.Count == 0)
                    {
                        Zaehle(feld, wert, p, fundort);
                        try { n.Name.RefersTo = Konstante(feld, wert); }
                        catch (Exception ex) { _e.Warnung(T(nameof(R.BV_XL_LAUF_AUSNAHME), n.Name.Name, fundort, ex.Message)); }
                    }
                    else
                    {
                        _e.Unbekannt(new Fuellbefund("{{" + n.Schluessel + "}}", fundort, Fuellbefundart.NichtUnterstuetzt,
                                                     T(nameof(R.BV_XL_GRUND_BEREICH))));
                    }
                }
            }

            /// <summary>
            /// Ein Name <c>EPOS.reihe.*</c> (Konzept 7.4): sein <c>RefersTo</c> zeigt danach auf die Zahlen im Blatt
            /// „Diagrammdaten“ — ein Diagramm der Vorlage auf dem Namen zeigt sie. Ohne Reihe (unbekannt oder im Lauf nicht
            /// erhoben) bleibt der Name, wie er war.
            /// </summary>
            private void FuelleReihe(Excelnamensfund n, string fundort)
            {
                if (!Excelreihen.Lies(n.Schluessel, out _, out _))
                {
                    _e.Unbekannt(new Fuellbefund("EPOS." + n.Schluessel, fundort, Fuellbefundart.Unbekannt,
                                                 T(nameof(R.BV_XL_GRUND_REIHE), Excelreihen.Liste())));
                    return;
                }
                _e.Ersetzt++;
                if (!_reihen.TryGetValue(n, out Diagrammplan.Block b))
                {
                    _e.Leer(n.Schluessel);
                    _e.Hinweis(T(nameof(R.BV_XL_LAUF_REIHE_LEER), n.Name.Name));
                    return;
                }
                try { n.Name.RefersTo = Excelreihen.Bezug(b); }
                catch (Exception ex) { _e.Warnung(T(nameof(R.BV_XL_LAUF_AUSNAHME), n.Name.Name, fundort, ex.Message)); }
            }

            /// <summary>Der Wert eines Namens ohne Zelle als Konstante für <c>RefersTo</c>.</summary>
            private static string Konstante(Vorlagenfeld feld, Platzhalterwert wert)
            {
                if (!wert.IstLeer && feld.Art == Vorlagenfeldart.Zahl && wert.Zahl.HasValue)
                {
                    double z = IstParameter(feld) ? wert.Zahl.Value / 100.0 : wert.Zahl.Value;
                    return "=" + z.ToString("R", CultureInfo.InvariantCulture);
                }
                if (!wert.IstLeer && feld.Art == Vorlagenfeldart.Datum && wert.Datum.HasValue)
                    return "=" + wert.Datum.Value.ToOADate().ToString("R", CultureInfo.InvariantCulture);
                string text = wert.IstLeer ? "" : (feld.Art == Vorlagenfeldart.Liste ? string.Join("\n", wert.Zeilen) : wert.Text);
                return "=\"" + text.Replace("\"", "\"\"") + "\"";
            }

            // ------------------------------------------------------------ Befunde

            private void Zaehle(Vorlagenfeld feld, Platzhalterwert wert, Platzhalter p, string fundort)
            {
                _e.Ersetzt++;
                _e.Aufgeloest(feld.Schluessel, feld.Kontext);
                if (wert.IstLeer) _e.Leer(feld.Schluessel);
                if (wert.Ausnahme != null) _e.Warnung(T(nameof(R.BV_XL_LAUF_AUSNAHME), p.Normalform, fundort, wert.Ausnahme));
            }

            private void Stehen(string normalform, Excelstelle stelle, string fundort)
            {
                Fuellbefundart art;
                string grund;
                switch (stelle)
                {
                    case Excelstelle.Unbekannt:
                        art = Fuellbefundart.Unbekannt;
                        grund = WordVorlagentexte.T(WordVorlagentexte.GRUND_UNBEKANNT, _englisch);
                        break;
                    case Excelstelle.Spaeter:
                        art = Fuellbefundart.NichtUnterstuetzt;
                        grund = WordVorlagentexte.T(WordVorlagentexte.GRUND_NICHT_UNTERSTUETZT, _englisch);
                        break;
                    case Excelstelle.Block:
                        art = Fuellbefundart.Block;
                        grund = T(nameof(R.BV_XL_GRUND_BLOCK));
                        break;
                    case Excelstelle.Kontext:
                    case Excelstelle.Gebaeude:
                        art = Fuellbefundart.Kontext;
                        grund = T(nameof(R.BV_XL_GRUND_KONTEXT));
                        break;
                    case Excelstelle.OhneExcel:
                        art = Fuellbefundart.FalscheStelle;
                        grund = T(nameof(R.BV_XL_GRUND_OHNE_EXCEL));
                        break;
                    case Excelstelle.Blattort:
                        art = Fuellbefundart.FalscheStelle;
                        grund = T(nameof(R.BV_XL_GRUND_BLATTORT));
                        break;
                    default:
                        art = Fuellbefundart.FalscheStelle;
                        grund = WordVorlagentexte.T(WordVorlagentexte.GRUND_FALSCHE_STELLE, _englisch);
                        break;
                }
                _e.Unbekannt(new Fuellbefund(normalform, fundort, art, grund));
            }

            /// <summary>Ein Platzhalter, der stehen bleibt, ist gelb hinterlegt (Konzept 4.10) — wie in Word.</summary>
            private static void Gelb(IXLCell zelle)
            {
                zelle.Style.Fill.BackgroundColor = XLColor.Yellow;
            }
        }

        // =====================================================================
        //  Formate (Konzept 7.1)
        // =====================================================================

        /// <summary>Trägt die Zelle das Zahlenformat „Standard“? Nur dann setzt EPOS das Format des Kerns.</summary>
        internal static bool IstStandard(IXLCell zelle)
        {
            IXLNumberFormat f = zelle.Style.NumberFormat;
            string format = f.Format ?? "";
            return (f.NumberFormatId == 0 && format.Length == 0) || string.Equals(format, "General", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Trägt die Zelle ein Prozentformat (eingebaut 9/10 oder ein Format mit „%“ außerhalb von Anführungszeichen)?</summary>
        internal static bool IstProzentformat(IXLCell zelle)
        {
            IXLNumberFormat f = zelle.Style.NumberFormat;
            if (f.NumberFormatId == 9 || f.NumberFormatId == 10) return true;
            string format = f.Format ?? "";
            bool zitat = false;
            foreach (char c in format)
            {
                if (c == '"') zitat = !zitat;
                else if (c == '%' && !zitat) return true;
            }
            return false;
        }

        /// <summary>Eine Zahl in Prozent (Einheit „%“, Konzept 7.1: 0–100)?</summary>
        internal static bool IstProzenteinheit(Vorlagenfeld feld)
        {
            return string.Equals((feld?.Einheit ?? "").Trim(), "%", StringComparison.Ordinal);
        }

        /// <summary>Ein Parameter der Wirtschaftlichkeit in Prozent — in Excel ein Anteil (Anhang A, BV-E4-3).</summary>
        internal static bool IstParameter(Vorlagenfeld feld)
        {
            return feld != null && IstProzenteinheit(feld) &&
                   feld.Schluessel.StartsWith("wirtschaft.parameter.", StringComparison.Ordinal);
        }

        /// <summary>Die Nachkommastellen: <c>|stellen n</c>, sonst das Katalogformat (<c>N0</c> … <c>N9</c>).</summary>
        internal static int Stellen(Vorlagenfeld feld, Platzhalter p)
        {
            foreach (Formatangabe a in p?.Angaben ?? Array.Empty<Formatangabe>())
                if (a.Art == Formatangabeart.Stellen && a.Zahl.HasValue) return a.Zahl.Value;
            string format = feld?.Format ?? "N0";
            if (format.Length >= 2 && (format[0] == 'N' || format[0] == 'F' || format[0] == 'n' || format[0] == 'f') &&
                int.TryParse(format.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                return Math.Max(0, Math.Min(n, Platzhaltersyntax.STELLEN_MAX));
            return 0;
        }

        /// <summary>Das Excel-Zahlenformat des Kernformats: <c>N1</c> → <c>#,##0.0</c>.</summary>
        internal static string Zahlformat(Vorlagenfeld feld, Platzhalter p)
        {
            int n = Stellen(feld, p);
            return n == 0 ? "#,##0" : "#,##0." + new string('0', n);
        }

        /// <summary>Das Prozentformat eines Anteils mit den Stellen des Kernformats: <c>N2</c> → <c>0.00%</c>.</summary>
        internal static string Prozentformat(Vorlagenfeld feld, Platzhalter p)
        {
            int n = Stellen(feld, p);
            return n == 0 ? "0%" : "0." + new string('0', n) + "%";
        }

        /// <summary>Das Datumsformat nach Katalogformat und Angabe: kurz (eingebaut 14), mit Zeit (22), lang.</summary>
        private static void SetzeDatumsformat(IXLCell zelle, Vorlagenfeld feld, Platzhalter p)
        {
            string format = feld.Format ?? "d";
            foreach (Formatangabe a in p.Angaben)
            {
                if (a.Art == Formatangabeart.Datum) format = "d";
                else if (a.Art == Formatangabeart.DatumLang) format = "D";
                else if (a.Art == Formatangabeart.DatumMitZeit) format = "g";
            }
            if (format == "g") zelle.Style.NumberFormat.NumberFormatId = 22;
            else if (format == "D") zelle.Style.NumberFormat.Format = BerichtTexte.Englisch ? "mmmm d, yyyy" : "d. mmmm yyyy";
            else zelle.Style.NumberFormat.NumberFormatId = 14;
        }
    }
}
