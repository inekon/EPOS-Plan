using System;
using System.Collections.Generic;
using System.Globalization;
using ClosedXML.Excel;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E6 (Konzept § 2.13 (5), Mockup Kategorie 8, Anhangzeile U13) — das
    /// <b>Excel-Blatt „Verlauf"</b>: je Jahr eine Zeile, je Stand und Szenario eine Spalte —
    /// dieselben Linien, die das Bild zeichnet (kumulierter Barwert der Differenz zur
    /// Referenz, ohne Restwert), in einer Spaltengruppe je Szenario (Ungünstig · Erwartet ·
    /// Günstig), darunter je Spalte der Nulldurchgang, der Restwert-Barwert am Horizontende
    /// und die Kapitalwertdifferenz, die sich mit ihm ergibt.
    ///
    /// <para><b>Zwei Wege, ein Blatt:</b> der Knopf „Verlauf nach Excel…" der Seite schreibt
    /// eine eigene Mappe mit nur diesem Blatt (<see cref="SchreibeMappe"/>), der
    /// Tabellenbericht hängt dasselbe Blatt an (<see cref="SchreibeBlatt"/>). Beide lesen
    /// dasselbe Sammelmodell (<see cref="WirtschaftlichkeitVerlaufSzenarien"/>) und
    /// dieselbe Regel für den Nulldurchgang (<see cref="KapitalwertRechner.Nulldurchgang"/>).</para>
    ///
    /// <para><b>Werte bleiben Zahlen</b> (Konzept Kap. 9): Die Zellen tragen echte Zahlen mit
    /// Zellformat; ein fehlender Nulldurchgang steht als „—" und nie als 0. Die Spaltenbreiten
    /// sind fest — das Blatt braucht keine Textvermessung und damit keine Schrift des
    /// Systems.</para>
    /// </summary>
    public static class VerlaufExcel
    {
        private static readonly XLColor KOPF = XLColor.FromHtml("#D9E1F2");
        private static readonly XLColor GRUPPE = XLColor.FromHtml("#EAEDED");
        private static readonly XLColor SUMME = XLColor.FromHtml("#F2F2F2");

        /// <summary>Erste Zeile der Spaltengruppen (Szenarionamen).</summary>
        public const int ZEILE_GRUPPEN = 4;

        /// <summary>Kopfzeile der Tabelle („Jahr" und die Namen der Stände).</summary>
        public const int ZEILE_KOPF = 5;

        /// <summary>Die Zeile des Jahres 0.</summary>
        public const int ZEILE_JAHR0 = 6;

        /// <summary>
        /// Schreibt eine eigene Mappe mit NUR dem Blatt „Verlauf" — der Weg des Knopfes
        /// „Verlauf nach Excel…". Die Datei wird überschrieben.
        /// </summary>
        /// <param name="pfad">Zieldatei (.xlsx).</param>
        /// <param name="verlauf">Die drei Läufe.</param>
        /// <param name="texte">Beschriftungen; <c>null</c> = die Vorgabe.</param>
        /// <param name="unterzeile">Zweite Zeile unter dem Titel (Referenz, Horizont); leer = keine.</param>
        /// <param name="nurStaende">Nur diese Stände; <c>null</c> = alle.</param>
        /// <param name="nurSzenarien">Nur diese Szenarien (Persistenzwerte); <c>null</c> = alle drei.</param>
        public static void SchreibeMappe(string pfad, WirtschaftlichkeitVerlaufSzenarien verlauf,
                                         VerlaufBlattTexte texte, string unterzeile,
                                         ICollection<int> nurStaende = null,
                                         ICollection<string> nurSzenarien = null)
        {
            if (string.IsNullOrEmpty(pfad)) throw new ArgumentException("Kein Zielpfad.", nameof(pfad));
            using (var wb = new XLWorkbook())
            {
                SchreibeBlatt(wb, verlauf, texte, unterzeile, nurStaende, nurSzenarien);
                wb.SaveAs(pfad);
            }
        }

        /// <summary>
        /// Hängt das Blatt „Verlauf" an die Mappe an und liefert es zurück. Ohne einen
        /// einzigen Stand mit Linie trägt es nur Titel und Hinweis.
        /// </summary>
        public static IXLWorksheet SchreibeBlatt(XLWorkbook wb, WirtschaftlichkeitVerlaufSzenarien verlauf,
                                                 VerlaufBlattTexte texte, string unterzeile,
                                                 ICollection<int> nurStaende = null,
                                                 ICollection<string> nurSzenarien = null)
        {
            if (wb == null) throw new ArgumentNullException(nameof(wb));
            texte = texte ?? new VerlaufBlattTexte();
            CultureInfo kultur = BerichtTexte.Kultur;

            IXLWorksheet ws = wb.Worksheets.Add(Blattname(wb, texte.Blatt));
            ws.Cell(1, 1).Value = texte.Titel;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            if (!string.IsNullOrEmpty(unterzeile))
            {
                ws.Cell(2, 1).Value = unterzeile;
                ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            }
            ws.Column(1).Width = 46;

            List<Spalte> spalten = Spalten(verlauf, texte, nurStaende, nurSzenarien);
            if (spalten.Count == 0)
            {
                ws.Cell(ZEILE_GRUPPEN, 1).Value = texte.KeineReihe;
                return ws;
            }

            // ---- Kopf: Spaltengruppe je Szenario, darunter die Stände ----
            ws.Cell(ZEILE_KOPF, 1).Value = texte.Jahr;
            int c = 2;
            string gruppe = null;
            int gruppeVon = 2;
            foreach (Spalte s in spalten)
            {
                if (!string.Equals(gruppe, s.Szenario, StringComparison.Ordinal))
                {
                    if (gruppe != null) Gruppenkopf(ws, gruppeVon, c - 1, texte.Szenarioname(gruppe));
                    gruppe = s.Szenario;
                    gruppeVon = c;
                }
                ws.Cell(ZEILE_KOPF, c).Value = s.Stand;
                ws.Column(c).Width = 18;
                c++;
            }
            Gruppenkopf(ws, gruppeVon, c - 1, texte.Szenarioname(gruppe));
            int letzte = c - 1;
            ws.Range(ZEILE_KOPF, 1, ZEILE_KOPF, letzte).Style.Font.Bold = true;
            ws.Range(ZEILE_KOPF, 1, ZEILE_KOPF, letzte).Style.Fill.BackgroundColor = KOPF;

            // ---- je Jahr eine Zeile ----
            int jahre = 0;
            foreach (Spalte s in spalten) jahre = Math.Max(jahre, s.Werte.Length - 1);
            for (int t = 0; t <= jahre; t++)
            {
                int r = ZEILE_JAHR0 + t;
                ws.Cell(r, 1).Value = t;
                for (int i = 0; i < spalten.Count; i++)
                {
                    double[] w = spalten[i].Werte;
                    if (t >= w.Length) continue;
                    ws.Cell(r, i + 2).Value = w[t];
                    ws.Cell(r, i + 2).Style.NumberFormat.Format = "#,##0";
                }
            }

            // ---- je Spalte: Nulldurchgang, Restwert, Kapitalwertdifferenz ----
            int z0 = ZEILE_JAHR0 + jahre + 1;
            ws.Cell(z0, 1).Value = texte.Nulldurchgang;
            ws.Cell(z0 + 1, 1).Value = texte.Restwert;
            ws.Cell(z0 + 2, 1).Value = texte.Kapitalwert;
            for (int i = 0; i < spalten.Count; i++)
            {
                Spalte s = spalten[i];
                IXLCell zelle = ws.Cell(z0, i + 2);
                if (s.Nulldurchgang.HasValue)
                {
                    zelle.Value = Math.Round(s.Nulldurchgang.Value, 2);
                    zelle.Style.NumberFormat.Format = "0.00";
                }
                else
                {
                    zelle.Value = texte.KeinDurchgang;
                    zelle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }

                ws.Cell(z0 + 1, i + 2).Value = s.Restwert;
                ws.Cell(z0 + 1, i + 2).Style.NumberFormat.Format = "#,##0";
                ws.Cell(z0 + 2, i + 2).Value = s.Werte[s.Werte.Length - 1] + s.Restwert;
                ws.Cell(z0 + 2, i + 2).Style.NumberFormat.Format = "#,##0";
            }
            ws.Range(z0, 1, z0 + 2, letzte).Style.Fill.BackgroundColor = SUMME;
            ws.Range(z0 + 2, 1, z0 + 2, letzte).Style.Font.Bold = true;

            ws.SheetView.Freeze(ZEILE_KOPF, 1);
            return ws;
        }

        /// <summary>
        /// Die Spalten des Blattes in Zeichenreihenfolge: je Szenario (Ungünstig · Erwartet ·
        /// Günstig) die Stände in der Reihenfolge der Gruppe — dieselben Linien, die das Bild
        /// zeichnet.
        /// </summary>
        internal static List<Spalte> Spalten(WirtschaftlichkeitVerlaufSzenarien verlauf,
                                             VerlaufBlattTexte texte,
                                             ICollection<int> nurStaende, ICollection<string> nurSzenarien)
        {
            var liste = new List<Spalte>();
            if (verlauf == null) return liste;
            List<KeyValuePair<int, string>> staende = verlauf.Versionen();
            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
            {
                if (nurSzenarien != null && !nurSzenarien.Contains(s)) continue;
                foreach (KeyValuePair<int, string> v in staende)
                {
                    if (nurStaende != null && !nurStaende.Contains(v.Key)) continue;
                    VerlaufSerie d = verlauf.Differenz(v.Key, s);
                    if (d == null || d.Kumuliert == null || d.Kumuliert.Length == 0) continue;
                    liste.Add(new Spalte
                    {
                        Szenario = s,
                        Stand = v.Value ?? "",
                        Werte = d.Kumuliert,
                        Restwert = d.RestwertBarwert,
                        Nulldurchgang = KapitalwertRechner.Nulldurchgang(d.Kumuliert)
                    });
                }
            }
            return liste;
        }

        /// <summary>Der Kopf einer Spaltengruppe: der Szenarioname über ihren Spalten.</summary>
        private static void Gruppenkopf(IXLWorksheet ws, int von, int bis, string name)
        {
            if (bis < von) return;
            ws.Cell(ZEILE_GRUPPEN, von).Value = name;
            IXLRange bereich = ws.Range(ZEILE_GRUPPEN, von, ZEILE_GRUPPEN, bis);
            if (bis > von) bereich.Merge();
            bereich.Style.Font.Bold = true;
            bereich.Style.Fill.BackgroundColor = GRUPPE;
            bereich.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        /// <summary>Der Blattname — eindeutig in der Mappe (Excel vergleicht ohne
        /// Groß-/Kleinschreibung), höchstens 31 Zeichen, ohne die verbotenen Zeichen.</summary>
        private static string Blattname(XLWorkbook wb, string wunsch)
        {
            string name = string.IsNullOrEmpty(wunsch) ? "Verlauf" : wunsch;
            foreach (char z in new[] { '[', ']', ':', '*', '?', '/', '\\' }) name = name.Replace(z, '_');
            if (name.Length > 28) name = name.Substring(0, 28);
            string basis = name;
            int n = 2;
            while (wb.Worksheets.Contains(name)) name = basis + " " + n++;
            return name;
        }

        /// <summary>Eine Spalte des Blattes: ein Stand in einem Szenario.</summary>
        internal sealed class Spalte
        {
            public string Szenario = "";
            public string Stand = "";
            public double[] Werte = new double[0];
            public double Restwert;
            public double? Nulldurchgang;
        }
    }

    /// <summary>
    /// ETAPPE E6 — die Beschriftungen des Blattes „Verlauf". Die Vorgabe ist deutsch;
    /// Seite und Bericht nehmen <see cref="AusRessourcen"/>.
    /// </summary>
    public sealed class VerlaufBlattTexte
    {
        /// <summary>Name des Blattes (<c>WIRT_VERL_BLATT</c>).</summary>
        public string Blatt { get; set; } = "Verlauf";

        /// <summary>Erste Zeile (<c>WIRT_VERL_BLATT_TITEL</c>).</summary>
        public string Titel { get; set; }
            = "Kumulierter Barwert der Differenz zur Referenz je Jahr [€] — ohne Restwert";

        /// <summary>Kopf der ersten Spalte (<c>WIRT_MJ_JAHR</c>).</summary>
        public string Jahr { get; set; } = "Jahr";

        /// <summary>Zeile des Nulldurchgangs (<c>WIRT_VERL_ZEILE_NULL</c>).</summary>
        public string Nulldurchgang { get; set; } = "Nulldurchgang (dynamische Amortisation) [a]";

        /// <summary>Zeile des Restwerts (<c>WIRT_VERL_ZEILE_RESTWERT</c>).</summary>
        public string Restwert { get; set; } = "Restwert-Barwert am Horizontende [€]";

        /// <summary>Zeile der Kapitalwertdifferenz (<c>WIRT_VERL_ZEILE_KW</c>).</summary>
        public string Kapitalwert { get; set; } = "Kapitalwertdifferenz = Endwert + Restwert-Barwert [€]";

        /// <summary>Zelle ohne Nulldurchgang im Horizont.</summary>
        public string KeinDurchgang { get; set; } = "—";

        /// <summary>Hinweis ohne einen einzigen Stand mit Linie (<c>WIRT_VERL_KEINE_REIHE</c>).</summary>
        public string KeineReihe { get; set; } = "Keine berechenbaren Reihen.";

        /// <summary>Die Szenarionamen (<c>WIRT_SZEN_WORST</c>, <c>…_ERWARTET</c>, <c>…_BEST</c>).</summary>
        public string Worst { get; set; } = "Ungünstig";

        /// <inheritdoc cref="Worst"/>
        public string Erwartet { get; set; } = "Erwartet";

        /// <inheritdoc cref="Worst"/>
        public string Best { get; set; } = "Günstig";

        /// <summary>Der Anzeigename eines Szenarios (Persistenzwert).</summary>
        public string Szenarioname(string szenario)
        {
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.WORST, StringComparison.Ordinal)) return Worst;
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.BEST, StringComparison.Ordinal)) return Best;
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.ERWARTET, StringComparison.Ordinal)) return Erwartet;
            return szenario ?? "";
        }

        /// <summary>Dieselben Texte in der Oberflächensprache (<c>MyResource</c>).</summary>
        public static VerlaufBlattTexte AusRessourcen()
        {
            return new VerlaufBlattTexte
            {
                Blatt = MyResource.Resource.WIRT_VERL_BLATT,
                Titel = MyResource.Resource.WIRT_VERL_BLATT_TITEL,
                Jahr = MyResource.Resource.WIRT_MJ_JAHR,
                Nulldurchgang = MyResource.Resource.WIRT_VERL_ZEILE_NULL,
                Restwert = MyResource.Resource.WIRT_VERL_ZEILE_RESTWERT,
                Kapitalwert = MyResource.Resource.WIRT_VERL_ZEILE_KW,
                KeineReihe = MyResource.Resource.WIRT_VERL_KEINE_REIHE,
                Worst = MyResource.Resource.WIRT_SZEN_WORST,
                Erwartet = MyResource.Resource.WIRT_SZEN_ERWARTET,
                Best = MyResource.Resource.WIRT_SZEN_BEST
            };
        }
    }
}
