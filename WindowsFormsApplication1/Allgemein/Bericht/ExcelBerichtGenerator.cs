using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ClosedXML.Excel;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Excel-Ausgabe des Variantenberichts (Konzept Kap. 9) über ClosedXML —
    /// datenorientiert, kein Berichtslayout-Nachbau, kein installiertes Office nötig.
    ///
    /// Aufbau der Mappe:
    ///  - Blatt „Übersicht":  Projektstammdaten, Variantenliste mit Simulationsstand,
    ///    Komponenten-Matrix (Gewerke × Varianten).
    ///  - Blatt „Vergleich":  komplette Kennzahlen-Vergleichstabelle — Kennzahlen als
    ///    Zeilen (nach den 4 Gruppen), Varianten als Spalten (Stamm zuerst), ECHTE
    ///    Zahlenwerte mit Zellformat, fixierte Köpfe, Autofilter; rechts Δ%-Block.
    ///  - je Variante ein Detailblatt (abwählbar über Baustein „Ergebnisse je
    ///    Variante"): Kennzahlen, Erzeuger-Module, Brennstoffmengen, Monatswerte.
    /// Fehlende Werte bleiben LEER (nie 0) — Konzept Kap. 5/9.
    /// </summary>
    public class ExcelBerichtGenerator
    {
        private static readonly XLColor KOPF = XLColor.FromHtml("#D9E1F2");
        private static readonly XLColor STAMM = XLColor.FromHtml("#F2F2F2");
        private static readonly XLColor GRUPPE = XLColor.FromHtml("#EAEDED");

        // =============================================================== Schriftrückfall
        //
        // PAKET iU7-4 (Entscheidung iF19) — die SCHRIFTFALLE VON ClosedXML.
        //
        // Diese Mappe ruft viermal Columns().AdjustToContents(...). Die Spaltenbreite
        // ist dort keine Schätzung, sondern eine echte Textvermessung: ClosedXML fragt
        // dafür sein Grafikmodul, und das ist SixLabors.Fonts. Das Modul braucht die
        // Schrift der Zelle als DATEI — und die Zellen dieser Mappe tragen die
        // Vorgabeschrift von Excel, also Calibri. Auf Windows liegt sie mit Office
        // vor; auf Linux, in der CI und auf iOS nicht.
        //
        // BEFUND VOM 03.09.2026 (gemessen, nicht angenommen): ClosedXML 0.105.1 bringt
        // eine eigene Schrift EINGEBETTET mit — das Feld heißt dort "CarlitoBare" —
        // und vermisst damit weiter, wenn eine Familie fehlt. Ein Lauf ohne Calibri
        // hat auf einem Linux mit 22 installierten Familien NICHT geworfen; auch eine
        // ausdrücklich unsinnige Rückfallschrift fing ClosedXML selbst ab. Die Falle
        // ist in DIESER Fassung also entschärft.
        //
        // Genau deshalb übersteuert diese Stelle NICHT blind:
        //
        //   1. Ist Calibri (Windows) oder das metrisch gleiche Carlito da, wird das
        //      Grafikmodul ausdrücklich darauf festgelegt. Das ist die Schrift, mit der
        //      bisher schon vermessen wurde — auf Windows ändert sich nichts.
        //   2. Sonst wird zuerst GEPRÜFT, ob ClosedXML von sich aus messen kann. Kann
        //      es das (eingebettetes Carlito), bleibt seine Vorgabe stehen: Carlito
        //      passt metrisch zu Calibri, jede Systemschrift wäre schlechter und die
        //      Spaltenbreiten liefen gegenüber Windows auseinander.
        //   3. Erst wenn diese Messprobe wirklich wirft — der Fall, den iF19 im Blick
        //      hatte, etwa auf einem Abbild ohne jede Schrift oder mit einer künftigen
        //      ClosedXML-Fassung ohne eingebettete Schrift — wird die erste vorhandene
        //      Familie aus der Liste unten gesetzt. Ein Bericht soll dann eine etwas
        //      andere Spaltenbreite haben und nicht gar keine Datei.
        //
        // Die Reihenfolge der Liste ist dieselbe, die der ChartRenderer für seine
        // Diagramme benutzt — Tabelle und Diagramm desselben Berichts sollen nicht in
        // verschiedenen Schriften vermessen werden.

        /// <summary>Gesuchte Schriftfamilien in dieser Reihenfolge; die erste vorhandene gewinnt.</summary>
        private static readonly string[] SCHRIFT_RUECKFALL =
        { "Calibri", "Carlito", "Liberation Sans", "DejaVu Sans", "Arial" };

        /// <summary>Die Familien, die zu Calibri metrisch passen — nur sie dürfen die
        /// Vorgabe von ClosedXML ohne weitere Prüfung übersteuern.</summary>
        private static readonly string[] SCHRIFT_METRIKGLEICH = { "Calibri", "Carlito" };

        private static readonly object _grafikSchloss = new object();
        private static bool _grafikGesetzt;

        /// <summary>
        /// Legt das Grafikmodul von ClosedXML fest, falls nötig — einmal je Prozess,
        /// vor der ersten Arbeitsmappe. Siehe den Block darüber.
        /// </summary>
        private static void GrafikModulSicherstellen()
        {
            lock (_grafikSchloss)
            {
                if (_grafikGesetzt) return;
                _grafikGesetzt = true;      // auch ein Fehlschlag wird nicht wiederholt

                try
                {
                    string schrift = RueckfallSchrift();

                    // 1. Calibri/Carlito vorhanden -> ausdrücklich darauf festlegen.
                    if (schrift != null && Array.IndexOf(SCHRIFT_METRIKGLEICH, schrift) >= 0)
                    { Setze(schrift); return; }

                    // 2. Kommt ClosedXML mit seiner eigenen (eingebetteten) Schrift
                    //    zurecht, bleibt es dabei.
                    if (MessprobeLaeuft()) return;

                    // 3. Nur im Notfall auf eine Systemschrift ausweichen.
                    if (schrift != null) Setze(schrift);
                }
                catch
                {
                    // Ein Bericht darf nicht an der Spaltenbreite scheitern.
                }
            }
        }

        private static void Setze(string schriftfamilie)
        {
            LoadOptions.DefaultGraphicEngine =
                new ClosedXML.Graphics.DefaultGraphicEngine(schriftfamilie);
        }

        /// <summary>
        /// Vermisst eine Wegwerf-Zelle mit der Vorgabeschrift der Mappe. Läuft sie
        /// durch, kann ClosedXML auf diesem System messen und braucht keine Hilfe.
        /// </summary>
        private static bool MessprobeLaeuft()
        {
            try
            {
                using (var wb = new XLWorkbook())
                {
                    IXLWorksheet ws = wb.Worksheets.Add("Probe");
                    ws.Cell(1, 1).Value = "Spaltenbreite";
                    ws.Columns().AdjustToContents(1, 60);
                }
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Erste vorhandene Familie aus <see cref="SCHRIFT_RUECKFALL"/>; ersatzweise
        /// irgendeine installierte Familie. <c>null</c>, wenn das System gar keine
        /// Schrift führt — dann bleibt es bei der Vorgabe von ClosedXML.
        /// </summary>
        private static string RueckfallSchrift()
        {
            SixLabors.Fonts.FontFamily familie;
            foreach (string name in SCHRIFT_RUECKFALL)
                if (SixLabors.Fonts.SystemFonts.Collection.TryGet(name, out familie))
                    return name;

            foreach (SixLabors.Fonts.FontFamily f in SixLabors.Fonts.SystemFonts.Families)
                return f.Name;

            return null;
        }

        public string Erzeuge(BerichtsDaten daten, BerichtsKonfiguration konfig, string zielDatei)
        {
            if (daten == null || daten.Varianten.Count == 0)
                throw new ArgumentException("Keine Berichtsdaten vorhanden.");

            GrafikModulSicherstellen();

            using (var wb = new XLWorkbook())
            {
                BlattUebersicht(wb, daten);
                BlattVergleich(wb, daten);

                // Phase 6: Kapitalwert-Ergebnisse dieses Berichtslaufs (gleiche Quelle
                // wie der Word-Baustein — BerichtsDaten.Wirtschaftlichkeit, ersatzweise
                // der persistierte Stand aus Tab_ErgebnisWirtschaftlichkeit).
                if (konfig != null && konfig.IstAktiv(BerichtsKonfiguration.B_WIRTSCHAFT))
                    BlattWirtschaftlichkeit(wb, daten);

                if (konfig == null || konfig.IstAktiv(BerichtsKonfiguration.B_ERGEBNISSE))
                    foreach (VariantenDaten v in daten.Varianten)
                        BlattDetail(wb, v);

                wb.SaveAs(zielDatei);
            }
            return zielDatei;
        }

        // ------------------------------------------------------------- Übersicht

        private static void BlattUebersicht(XLWorkbook wb, BerichtsDaten daten)
        {
            IXLWorksheet ws = wb.Worksheets.Add("Übersicht");
            VariantenDaten stamm = daten.Varianten.FirstOrDefault(x => x.IstStamm);

            int r = 1;
            ws.Cell(r, 1).Value = "EPOS-Plan — Variantenvergleich";
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 1).Style.Font.FontSize = 14;
            r += 2;

            Action<string, string> zeile = (label, wert) =>
            {
                ws.Cell(r, 1).Value = label;
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Cell(r, 2).Value = wert ?? "";
                r++;
            };
            zeile("Projekt", daten.Stammprojektname);
            zeile("Kunde", stamm != null && stamm.Projekt != null ? stamm.Projekt.m_szKunde : "");
            zeile("Bearbeiter", stamm != null && stamm.Projekt != null ? stamm.Projekt.m_szBearbeiter : "");
            zeile("Klimaregion", stamm != null && stamm.Details != null ? stamm.Details.KlimaregionName : "");
            zeile("Berichtsdatum", daten.ErstelltAm.ToString("dd.MM.yyyy HH:mm"));
            r++;

            // Variantenliste.
            ws.Cell(r, 1).Value = "Rolle";
            ws.Cell(r, 2).Value = "Bezeichner";
            ws.Cell(r, 3).Value = "Projektname";
            ws.Cell(r, 4).Value = "Simulation vom";
            ws.Cell(r, 5).Value = "Hinweis";
            KopfZeile(ws, r, 5);
            r++;
            foreach (VariantenDaten v in daten.Varianten)
            {
                ws.Cell(r, 1).Value = v.IstStamm ? "Stamm" : "Variante";
                ws.Cell(r, 2).Value = v.IstStamm ? "(Stammprojekt)" : v.Variantenname;
                ws.Cell(r, 3).Value = v.Projektname;
                if (v.SimulationsStand.HasValue)
                {
                    ws.Cell(r, 4).Value = v.SimulationsStand.Value;
                    ws.Cell(r, 4).Style.DateFormat.Format = "dd.MM.yyyy hh:mm";
                }
                ws.Cell(r, 5).Value = v.Fehler != null ? "Fehler: " + v.Fehler
                    : v.FrischSimuliert ? "neu gerechnet"
                    : v.ErgebnisVeraltet ? "älter als Projektänderung" : "";
                if (v.IstStamm) ws.Range(r, 1, r, 5).Style.Fill.BackgroundColor = STAMM;
                r++;
            }
            r++;

            // Komponenten-Matrix.
            ws.Cell(r, 1).Value = "Gewerk";
            for (int i = 0; i < daten.Varianten.Count; i++)
                ws.Cell(r, 2 + i).Value = daten.Varianten[i].IstStamm ? "Stamm" : daten.Varianten[i].Anzeige;
            KopfZeile(ws, r, 1 + daten.Varianten.Count);
            r++;
            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
            {
                ws.Cell(r, 1).Value = g.Key;
                for (int i = 0; i < daten.Varianten.Count; i++)
                {
                    ProjektDetails d = daten.Varianten[i].Details;
                    int n = (d != null && d.KomponentenAnzahl.ContainsKey(g.Key)) ? d.KomponentenAnzahl[g.Key] : 0;
                    ws.Cell(r, 2 + i).Value = n == 0 ? "—" : (n == 1 ? "✓" : "✓ (" + n + ")");
                    ws.Cell(r, 2 + i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    if (daten.Varianten[i].IstStamm) ws.Cell(r, 2 + i).Style.Fill.BackgroundColor = STAMM;
                }
                r++;
            }

            ws.Columns().AdjustToContents(1, 60);
        }

        // ------------------------------------------------------------- Vergleich

        private static void BlattVergleich(XLWorkbook wb, BerichtsDaten daten)
        {
            IXLWorksheet ws = wb.Worksheets.Add("Vergleich");
            // E5/F7: CO₂-Zeilen nach dem gerechneten Modus beschriften.
            List<Kennzahl> katalog =
                KennzahlenKatalog.Alle(EmissionsAusweis.ModusAusVarianten(daten.Varianten));
            VariantenDaten stamm = daten.Varianten.FirstOrDefault(x => x.IstStamm);
            List<VariantenDaten> varianten = daten.Varianten.Where(x => !x.IstStamm).ToList();

            int projSpalten = daten.Varianten.Count;
            int deltaStart = 4 + projSpalten;   // Δ%-Block rechts neben den Wertspalten

            // Kopfzeile.
            ws.Cell(1, 1).Value = "Gruppe";
            ws.Cell(1, 2).Value = "Kennzahl";
            ws.Cell(1, 3).Value = "Einheit";
            for (int i = 0; i < projSpalten; i++)
                ws.Cell(1, 4 + i).Value = daten.Varianten[i].IstStamm ? "Stamm" : daten.Varianten[i].Anzeige;
            for (int i = 0; i < varianten.Count; i++)
                ws.Cell(1, deltaStart + i).Value = "Δ% " + varianten[i].Anzeige;
            KopfZeile(ws, 1, deltaStart + varianten.Count - 1);

            int r = 2;
            foreach (string gruppe in new[] { KennzahlenKatalog.GR_ENERGIE, KennzahlenKatalog.GR_EFFIZIENZ,
                                              KennzahlenKatalog.GR_EMISSION, KennzahlenKatalog.GR_KOSTEN })
            {
                var zeilen = katalog.Where(x => x.Gruppe == gruppe)
                    .Where(x => daten.Varianten.Any(v =>
                        v.Kennzahlen.ContainsKey(x.Schluessel) && v.Kennzahlen[x.Schluessel].HasValue))
                    .ToList();
                if (zeilen.Count == 0) continue;

                foreach (Kennzahl kz in zeilen)
                {
                    ws.Cell(r, 1).Value = gruppe;
                    ws.Cell(r, 2).Value = kz.Label(BerichtTexte.Englisch);
                    ws.Cell(r, 3).Value = kz.Einheit;

                    double? stammWert = Wert(stamm, kz.Schluessel);
                    for (int i = 0; i < projSpalten; i++)
                    {
                        VariantenDaten v = daten.Varianten[i];
                        double? wert = Wert(v, kz.Schluessel);
                        IXLCell zelle = ws.Cell(r, 4 + i);
                        if (wert.HasValue)
                        {
                            zelle.Value = wert.Value;                     // echter Zahlenwert
                            zelle.Style.NumberFormat.Format = Format(kz.Format);
                        }
                        if (v.IstStamm) zelle.Style.Fill.BackgroundColor = STAMM;
                    }
                    for (int i = 0; i < varianten.Count; i++)
                    {
                        double? wert = Wert(varianten[i], kz.Schluessel);
                        if (kz.DeltaAnzeigen && stammWert.HasValue && wert.HasValue
                            && Math.Abs(stammWert.Value) > 1e-9)
                        {
                            IXLCell zelle = ws.Cell(r, deltaStart + i);
                            zelle.Value = (wert.Value - stammWert.Value) / Math.Abs(stammWert.Value) * 100.0;
                            zelle.Style.NumberFormat.Format = "+#,##0.0;−#,##0.0;±0,0";
                        }
                    }
                    r++;
                }
            }

            var tabelle = ws.Range(1, 1, r - 1, deltaStart + Math.Max(varianten.Count, 1) - 1);
            tabelle.SetAutoFilter();
            ws.SheetView.Freeze(1, 3);
            ws.Columns().AdjustToContents(1, 45);
        }

        // ------------------------------------------------------------- Wirtschaftlichkeit (Phase 6)

        /// <summary>
        /// Blatt „Wirtschaftlichkeit": Kennzahlen der Kapitalwertmethode je Szenario
        /// (Zeilenblöcke Worst/Erwartet/Best; Spalten = Stamm + Varianten) aus der
        /// Rechnung dieses Berichtslaufs — identische Quelle wie der Word-Baustein.
        /// Echte Zahlenwerte, fehlende Werte bleiben leer.
        /// </summary>
        private static void BlattWirtschaftlichkeit(XLWorkbook wb, BerichtsDaten daten)
        {
            var provider = new WirtschaftlichkeitCtrl();
            List<int> ids = daten.Varianten.Select(v => v.IdProjekt).ToList();
            bool ausDiesemLauf = daten.Wirtschaftlichkeit.Count > 0;
            List<WirtschaftlichkeitErgebnis> alle = ausDiesemLauf
                ? daten.Wirtschaftlichkeit
                : provider.LadeErgebnisse(ids);

            IXLWorksheet ws = wb.Worksheets.Add(BerichtTexte.T("Wirtschaftlichkeit"));
            int r = 1;
            ws.Cell(r, 1).Value = BerichtTexte.T("Wirtschaftlichkeit") + " — " +
                                  BerichtTexte.T("Kapitalwertmethode (DIN EN 17463)");
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 1).Style.Font.FontSize = 14;
            r += 1;

            if (alle.Count == 0)
            {
                ws.Cell(r + 1, 1).Value = BerichtTexte.T(
                    "Wirtschaftlichkeit konnte für diesen Bericht nicht berechnet werden — " +
                    "Kostenpositionen und Parameter prüfen.") +
                    (daten.WirtschaftlichkeitFehler != null
                     ? " (" + daten.WirtschaftlichkeitFehler + ")" : "");
                ws.Columns().AdjustToContents();
                return;
            }

            WirtschaftlichkeitParameter p = provider.LadeParameter(daten.IdStamm);
            TarifParameter tarifP = provider.LadeTarif(daten.IdStamm);
            ws.Cell(r, 1).Value = p.Nachweis(BerichtTexte.Kultur) +
                // ETAPPE E7 (Divergenz D1): Der TARIFnachweis stand bisher nur im
                // Word-Bericht. Er nennt Modell, Arbeitspreise und Preisstand — ohne ihn
                // ist die Stromkostenzeile im Excel-Blatt nicht nachvollziehbar.
                " · " + tarifP.Nachweis(BerichtTexte.Kultur) +
                // LEITENTSCHEIDUNGEN L12/L13: derselbe Ausweis wie im Word-Bericht —
                // Rechtsstand der Emissionsbewertung und Konvention der Biomasse.
                " · " + BilanzKonvention.Bestimme(p, new GesetzKatalog()).Ausweis(BerichtTexte.Kultur) +
                " · " + BerichtTexte.T("Referenz: Stammprojekt · Restwert linear") +
                " · " + BerichtTexte.T("Rechenstand") + ": " +
                alle[0].Zeitstempel.ToString("dd.MM.yyyy HH:mm", BerichtTexte.Kultur);
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r++;
            if (!ausDiesemLauf)
            {
                ws.Cell(r, 1).Value = BerichtTexte.T(
                    "⚠ Die Wirtschaftlichkeitsrechnung dieses Berichtslaufs ist fehlgeschlagen — " +
                    "gezeigt wird der zuletzt gespeicherte Stand.");
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#C00000");
                r++;
            }

            // ETAPPE E7 (Divergenz D3): Die Aktualitätsprüfung gegen den Simulationsstand
            // gab es bisher nur in Word. Ein Excel-Nutzer sah nicht, dass die Zahlen zu
            // einem anderen Lauf gehören als der Bericht.
            var veraltet = new List<string>();
            foreach (VariantenDaten v in daten.Varianten)
            {
                WirtschaftlichkeitErgebnis ea = alle.FirstOrDefault(x =>
                    x.IdProjekt == v.IdProjekt && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                if (ea == null || (ea.Fehlgrund == null && !provider.ErgebnisAktuell(ea)))
                    veraltet.Add(v.IstStamm ? "Stamm" : v.Anzeige);
            }
            if (veraltet.Count > 0)
            {
                ws.Cell(r, 1).Value = string.Format(MyResource.Resource.WIRT_ERGEBNIS_VERALTET,
                                                    string.Join(", ", veraltet));
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#C00000");
                r++;
            }
            r++;

            // ETAPPE E7: EINE Zeilendefinition für Word, Excel und Ergebnisreiter
            // (WirtschaftlichkeitZeilen). Die Liste stand bis dahin dreimal im Code.
            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Kennzahlen(alle, tarifP);

            // Der Zeitbezug der €/a-Werte steht einmal über der Tabelle statt in vier
            // Zeilentiteln (E7).
            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_ZEILE_JAHR1;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r += 2;

            foreach (string szenario in new[] { WirtschaftlichkeitSzenario.ERWARTET,
                                                WirtschaftlichkeitSzenario.BEST,
                                                WirtschaftlichkeitSzenario.WORST })
            {
                var block = alle.Where(x => x.Szenario == szenario).ToList();
                if (block.Count == 0) continue;

                ws.Cell(r, 1).Value = BerichtTexte.T("Szenario") + ": " + szenario;
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Range(r, 1, r, 1 + daten.Varianten.Count).Style.Fill.BackgroundColor = GRUPPE;
                r++;

                int kopfZeile = r;
                int stammSpalte = -1;
                ws.Cell(r, 1).Value = BerichtTexte.T("Kennzahl");
                int c = 2;
                foreach (VariantenDaten v in daten.Varianten)
                {
                    ws.Cell(r, c).Value = v.IstStamm ? "Stamm" : v.Anzeige;
                    if (v.IstStamm) stammSpalte = c;
                    c++;
                }
                ws.Range(kopfZeile, 1, kopfZeile, c - 1).Style.Font.Bold = true;
                ws.Range(kopfZeile, 1, kopfZeile, c - 1).Style.Fill.BackgroundColor = KOPF;
                r++;

                foreach (WirtZeile z in zeilen)
                {
                    // Zeile komplett ausblenden, wenn kein Projekt einen Wert liefert
                    // (z. B. BEHG/KWKG deaktiviert) — Konvention „nie 0-Zeilen".
                    bool hatWert = false;
                    foreach (VariantenDaten v in daten.Varianten)
                    {
                        WirtschaftlichkeitErgebnis pe = block.FirstOrDefault(x => x.IdProjekt == v.IdProjekt);
                        if (pe == null) continue;
                        if (z.IstText ? !string.IsNullOrEmpty(z.Text(pe)) : z.ExcelWert(pe).HasValue)
                        { hatWert = true; break; }
                    }
                    if (!hatWert) continue;

                    // Der Titel kommt aus MyResource — kein BerichtTexte.T() darüber.
                    ws.Cell(r, 1).Value = z.Titel;
                    c = 2;
                    foreach (VariantenDaten v in daten.Varianten)
                    {
                        WirtschaftlichkeitErgebnis e = block.FirstOrDefault(x => x.IdProjekt == v.IdProjekt);
                        if (e != null && z.IstText)
                        {
                            // Textzeile (Herkunft der Steuersätze, E7).
                            string t = z.Text(e);
                            if (!string.IsNullOrEmpty(t)) ws.Cell(r, c).Value = t;
                        }
                        else
                        {
                            // Wertspalten bleiben NUMERISCH: Beim Stamm bleibt die Zelle
                            // einer Differenzkennzahl leer, statt „(Referenz)" zu tragen —
                            // sonst wären Filter und Diagramme des Blattes hinüber. Das
                            // ist der eine bewusst verbliebene Unterschied zu Word und
                            // Reiter (Divergenz D5).
                            double? wert = e == null ? (double?)null : z.ExcelWert(e);
                            if (wert.HasValue)
                            {
                                ws.Cell(r, c).Value = wert.Value;
                                ws.Cell(r, c).Style.NumberFormat.Format = z.ExcelFormat;
                            }
                        }
                        c++;
                    }
                    r++;
                }
                if (stammSpalte > 0)
                    ws.Range(kopfZeile + 1, stammSpalte, r - 1, stammSpalte)
                      .Style.Fill.BackgroundColor = STAMM;

                // Fehlgründe unter dem Block ausweisen.
                foreach (WirtschaftlichkeitErgebnis e in block.Where(x => x.Fehlgrund != null))
                {
                    VariantenDaten v = daten.Varianten.FirstOrDefault(x => x.IdProjekt == e.IdProjekt);
                    ws.Cell(r, 1).Value = "⚠ " + (v == null ? ("Projekt " + e.IdProjekt)
                                                            : (v.IstStamm ? "Stamm" : v.Anzeige)) +
                                          ": " + e.Fehlgrund;
                    ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#B22222");
                    r++;
                }
                r++;   // Leerzeile zwischen den Szenarien
            }

            // ---------------- Hinweise dieses Laufs (ETAPPE E7, Divergenz D2) ----------------
            //
            // e.Hinweis erschien in Excel BISHER NIRGENDS. Darin stehen sämtliche
            // Begründungen der Etappen E2 bis E6: warum eine Gutschrift 0 ist, welcher
            // Aufschlagssatz angesetzt wurde, welche Anlage am Stichtag scheitert, wie
            // die vermiedenen Kosten entstehen. Ein Excel-Nutzer erfuhr davon nichts.
            // Ausgegeben wird EINMAL aus dem Szenario „Erwartet" — wie in Word; die
            // Texte sind über die Szenarien gleich, und dreimal derselbe Absatz wäre
            // Lärm.
            var mitHinweis = alle.Where(x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET &&
                                             !string.IsNullOrEmpty(x.Hinweis)).ToList();
            if (mitHinweis.Count > 0 || daten.Warnungen.Count > 0)
            {
                ws.Cell(r, 1).Value = MyResource.Resource.WIRT_NACHWEIS_TITEL;
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Range(r, 1, r, 1 + daten.Varianten.Count).Style.Fill.BackgroundColor = GRUPPE;
                r++;
                foreach (WirtschaftlichkeitErgebnis e in mitHinweis)
                {
                    VariantenDaten v = daten.Varianten.FirstOrDefault(x => x.IdProjekt == e.IdProjekt);
                    ws.Cell(r, 1).Value = "⚠ " + (v == null ? ("Projekt " + e.IdProjekt)
                                                            : (v.IstStamm ? "Stamm" : v.Anzeige)) +
                                          ": " + e.Hinweis;
                    ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                    r++;
                }
                // ETAPPE E7 (Divergenz D6): die Warnungen des Berichtslaufs standen
                // ebenfalls nur im Word-Anhang.
                if (daten.Warnungen.Count > 0)
                {
                    ws.Cell(r, 1).Value = MyResource.Resource.WIRT_NACHWEIS_LAUFHINWEISE;
                    ws.Cell(r, 1).Style.Font.Bold = true;
                    r++;
                    foreach (string wtext in daten.Warnungen)
                    {
                        ws.Cell(r, 1).Value = "• " + wtext;
                        ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                        r++;
                    }
                }
                r++;
            }

            // ---------------- KWK-Zuschlag je Modul (E6 → E7) ----------------
            r = BlattKwkgModule(ws, daten, alle, r);

            // ---------------- Betriebskosten nach Kostenarten (E3 → E7) ----------------
            r = BlattBetriebskosten(ws, daten, alle, r);

            // ---------------- Kapitalwert-Verlauf (Phase 11, Szenario Erwartet) ----------------
            // Jahresreihen frisch aus den Berichtsdaten gerechnet (T aus den Parametern);
            // dieselben Werte wie die Diagramme in Word und im Verlaufs-Dialog.
            // Konsistenz-Gate wie im Word-Baustein (Review 11): sind Tarif/KWKG aktiv,
            // aber keine Stundenreihen im Berichtslauf, entfällt der Block mit Hinweis.
            bool zeitreihenNoetig = (tarifP != null && tarifP.Aktiv) ||
                                    p.KwkgBonus > 0 || p.KwkgBonusEinspeisung > 0;
            int rStart = r;
            WirtschaftlichkeitVerlauf verlaufFuerMehrjahres = null;
            try
            {
                if (zeitreihenNoetig &&
                    daten.Varianten.Any(v => v.Fehler == null && v.Zeitreihen == null))
                {
                    ws.Cell(r, 1).Value = BerichtTexte.T(
                        "Kapitalwert-Verlauf entfällt: Bericht ohne Stundenreihen erzeugt (Baustein „Ergebnisse je Variante“ aktivieren).");
                    ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                    r += 2;
                }
                else
                {
                    WirtschaftlichkeitVerlauf verlauf = provider.BerechneVerlauf(
                        daten, p, p.Betrachtungszeitraum, WirtschaftlichkeitSzenario.ERWARTET);
                    verlaufFuerMehrjahres = verlauf;   // E7: Grundlage der Mehrjahrestabelle
                    var mitReihe = verlauf.Absolut.Where(s => s.Kumuliert != null).ToList();
                    var mitDiff = verlauf.Differenz.Where(x => x.Kumuliert != null).ToList();
                    if (mitReihe.Count > 0)
                    {
                        ws.Cell(r, 1).Value = BerichtTexte.T("Kapitalwert-Verlauf (kumulierte Barwerte, ohne Restwert) [€]");
                        ws.Cell(r, 1).Style.Font.Bold = true;
                        ws.Range(r, 1, r, 1 + mitReihe.Count + mitDiff.Count)
                          .Style.Fill.BackgroundColor = GRUPPE;
                        r++;

                        ws.Cell(r, 1).Value = BerichtTexte.T("Jahr");
                        int cv = 2;
                        foreach (VerlaufSerie s in mitReihe)
                        { ws.Cell(r, cv).Value = s.Anzeige; cv++; }
                        foreach (VerlaufSerie s in mitDiff)
                        { ws.Cell(r, cv).Value = "Δ " + s.Anzeige + " − Stamm"; cv++; }
                        ws.Range(r, 1, r, cv - 1).Style.Font.Bold = true;
                        ws.Range(r, 1, r, cv - 1).Style.Fill.BackgroundColor = KOPF;
                        r++;

                        for (int t = 0; t <= verlauf.Jahre; t++)
                        {
                            ws.Cell(r, 1).Value = t;
                            cv = 2;
                            foreach (VerlaufSerie s in mitReihe)
                            {
                                ws.Cell(r, cv).Value = s.Kumuliert[t];
                                ws.Cell(r, cv).Style.NumberFormat.Format = "#,##0";
                                cv++;
                            }
                            foreach (VerlaufSerie s in mitDiff)
                            {
                                ws.Cell(r, cv).Value = s.Kumuliert[t];
                                ws.Cell(r, cv).Style.NumberFormat.Format = "#,##0";
                                cv++;
                            }
                            r++;
                        }
                        ws.Cell(r, 1).Value = BerichtTexte.T(
                            "Ohne Restwert — Nettobarwert = Endwert + Restwert-Barwert.");
                        ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                        r += 2;
                    }
                }
            }
            catch
            {
                // Halb geschriebenen Block räumen, damit keine Reste unter den
                // Folgeblöcken stehen bleiben (Review-Verifikation 11).
                try { ws.Range(rStart, 1, r + 1, 2 * daten.Varianten.Count + 1).Clear(XLClearOptions.All); }
                catch { }
                r = rStart;
                verlaufFuerMehrjahres = null;
            }

            // ---------------- Mehrjahresübersicht der Zahlungsströme (E7) ----------------
            r = BlattMehrjahres(ws, daten, verlaufFuerMehrjahres, alle, r);

            // ---------------- Sensitivitätsanalyse (W2, Szenario Erwartet) ----------------
            List<SensitivitaetZeile> sens = provider.LadeSensitivitaet(ids);
            if (sens.Count > 0)
            {
                ws.Cell(r, 1).Value = BerichtTexte.T("Sensitivitätsanalyse (Szenario „Erwartet“)");
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = GRUPPE;
                r++;

                foreach (VariantenDaten v in daten.Varianten.Where(x => !x.IstStamm))
                {
                    var zeilenSens = sens.Where(x => x.IdProjekt == v.IdProjekt).ToList();
                    if (zeilenSens.Count == 0) continue;

                    ws.Cell(r, 1).Value = BerichtTexte.T("Variante") + ": " + v.Anzeige;
                    ws.Cell(r, 1).Style.Font.Bold = true;
                    r++;

                    ws.Cell(r, 1).Value = BerichtTexte.T("Parameter");
                    ws.Cell(r, 2).Value = BerichtTexte.T("KW bei −Δ [€]");
                    ws.Cell(r, 3).Value = BerichtTexte.T("KW Basis [€]");
                    ws.Cell(r, 4).Value = BerichtTexte.T("KW bei +Δ [€]");
                    ws.Range(r, 1, r, 4).Style.Font.Bold = true;
                    ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = KOPF;
                    r++;

                    foreach (SensitivitaetZeile z in zeilenSens)
                    {
                        ws.Cell(r, 1).Value = z.Parameter;
                        double?[] werte = { z.KwMinus, z.KwBasis, z.KwPlus };
                        for (int i = 0; i < 3; i++)
                            if (werte[i].HasValue)
                            {
                                ws.Cell(r, 2 + i).Value = werte[i].Value;
                                ws.Cell(r, 2 + i).Style.NumberFormat.Format = "#,##0";
                            }
                        r++;
                    }
                    r++;
                }
            }

            // ---------------- Strommengen-Matrix (W3) ----------------
            Dictionary<int, StromMatrix> matrizen = provider.LadeStromMatrix(ids);
            if (matrizen.Count > 0)
            {
                ws.Cell(r, 1).Value = BerichtTexte.T("Strommengen nach Tarifzonen [MWh]");
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Range(r, 1, r, 5).Style.Fill.BackgroundColor = GRUPPE;
                r++;
                foreach (VariantenDaten v in daten.Varianten)
                {
                    if (!matrizen.ContainsKey(v.IdProjekt)) continue;
                    StromMatrix m = matrizen[v.IdProjekt];

                    ws.Cell(r, 1).Value = (v.IstStamm ? "Stamm" : v.Anzeige) +
                        " — " + BerichtTexte.T("Bezugsspitze") + " " + m.MaxBezugKW.ToString("N0", BerichtTexte.Kultur) + " kW";
                    ws.Cell(r, 1).Style.Font.Bold = true;
                    r++;

                    ws.Cell(r, 1).Value = BerichtTexte.T("Zone");
                    // ETAPPE E7: „Bedarf ohne Anlage" — seit E5 gerechnet und
                    // persistiert, in beiden Matrixausgaben aber ungenutzt.
                    ws.Cell(r, 2).Value = MyResource.Resource.WIRT_MATRIX_BEDARF;
                    ws.Cell(r, 3).Value = BerichtTexte.T("Netzbezug [MWh]");
                    ws.Cell(r, 4).Value = BerichtTexte.T("PV-Einspeisung [MWh]");
                    ws.Cell(r, 5).Value = BerichtTexte.T("KWK-Eigenstrom [MWh]");
                    ws.Cell(r, 6).Value = BerichtTexte.T("KWK-Einspeisung [MWh]");
                    ws.Range(r, 1, r, 6).Style.Font.Bold = true;
                    ws.Range(r, 1, r, 6).Style.Fill.BackgroundColor = KOPF;
                    r++;
                    foreach (string zone in StromMatrix.Zonen)
                    {
                        StromMatrix.Zone z = m.Hole(zone);
                        if (z == null) continue;
                        ws.Cell(r, 1).Value = zone;
                        double[] werte = { z.BedarfMWh, z.BezugMWh, z.EinspeisungPvMWh,
                                           z.KwkEigenMWh, z.KwkEinspeisungMWh };
                        for (int i = 0; i < werte.Length; i++)
                        {
                            ws.Cell(r, 2 + i).Value = werte[i];
                            ws.Cell(r, 2 + i).Style.NumberFormat.Format = "#,##0.0";
                        }
                        r++;
                    }
                    r++;
                }
                ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MATRIX_BEDARF_HINWEIS;
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                r += 2;
            }

            // ---------------- Emissionsbilanz (W3) ----------------
            if (p.IdKraftwerkspark > 0)
            {
                ws.Cell(r, 1).Value = BerichtTexte.T("Emissionsbilanz — gekoppelte vs. getrennte Erzeugung");
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = GRUPPE;
                r++;
                foreach (VariantenDaten v in daten.Varianten)
                {
                    // Nur wenn das persistierte Ergebnis zum Simulationslauf passt —
                    // sonst stünden zwei Rechenstände in einem Blatt (Review Phase 8).
                    WirtschaftlichkeitErgebnis erw = alle.FirstOrDefault(x =>
                        x.IdProjekt == v.IdProjekt && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                    if (erw == null || !provider.ErgebnisAktuell(erw)) continue;
                    EmissionsBilanz b = EmissionsBilanzRechner.Berechne(v.IdProjekt, p);
                    if (b == null || (!b.CO2GekoppeltT.HasValue && !b.CO2GetrenntT.HasValue)) continue;

                    ws.Cell(r, 1).Value = (v.IstStamm ? "Stamm" : v.Anzeige) +
                        (b.Konvention == null || b.Konvention.Stromgutschrift ? " — " + b.ParkName : "");
                    ws.Cell(r, 1).Style.Font.Bold = true;
                    r++;

                    // LEITENTSCHEIDUNGEN L12/L13 — derselbe Ausweis wie im Word-Bericht.
                    if (b.Konvention != null)
                    {
                        ws.Cell(r, 1).Value = b.Konvention.Ausweis(BerichtTexte.Kultur);
                        ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                        r++;
                        if (b.Konvention.OhneGutschrift || b.Konvention.Substitution)
                        {
                            ws.Cell(r, 1).Value = b.Konvention.OhneGutschrift
                                ? MyResource.Resource.BILANZ_HINWEIS_DIN
                                : MyResource.Resource.BILANZ_HINWEIS_SUBSTITUTION;
                            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                            r++;
                        }
                    }

                    ws.Cell(r, 1).Value = BerichtTexte.T("Schadstoff");
                    ws.Cell(r, 2).Value = BerichtTexte.T("Gekoppelt (System)");
                    ws.Cell(r, 3).Value = BerichtTexte.T("Getrennt (Referenz)");
                    ws.Cell(r, 4).Value = BerichtTexte.T("Vermeidung");
                    ws.Range(r, 1, r, 4).Style.Font.Bold = true;
                    ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = KOPF;
                    r++;

                    Action<string, double?, double?> bz = (label, gek, getr) =>
                    {
                        ws.Cell(r, 1).Value = label;
                        if (gek.HasValue) { ws.Cell(r, 2).Value = gek.Value; ws.Cell(r, 2).Style.NumberFormat.Format = "#,##0.0"; }
                        if (getr.HasValue) { ws.Cell(r, 3).Value = getr.Value; ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.0"; }
                        if (gek.HasValue && getr.HasValue)
                        { ws.Cell(r, 4).Value = getr.Value - gek.Value; ws.Cell(r, 4).Style.NumberFormat.Format = "#,##0.0"; }
                        r++;
                    };
                    bz(EmissionsAusweis.BilanzZeile(b.Modus), b.CO2GekoppeltT, b.CO2GetrenntT);
                    bz("SO₂ [kg/a]", b.SO2GekoppeltKg, b.SO2GetrenntKg);
                    bz("NOx [kg/a]", b.NOxGekoppeltKg, b.NOxGetrenntKg);
                    // Die beiden Teilbeträge aus einer WAHL: das biogene
                    // Verbrennungs-CO₂ steckt in der gekoppelten Spalte, die Gutschrift
                    // des KWK-Stroms in der getrennten. Eine Vermeidungsspalte hätte für
                    // sie keine Bedeutung, deshalb je nur die zutreffende Spalte.
                    if (b.CO2BiogenT > 0)
                        bz(MyResource.Resource.BILANZ_ZEILE_BIOGEN, b.CO2BiogenT, null);
                    if (b.CO2GutschriftStromT > 0)
                        bz(MyResource.Resource.BILANZ_ZEILE_GUTSCHRIFT, null, b.CO2GutschriftStromT);
                    r++;
                }
            }

            ws.Column(1).Width = 32;
            // Spaltenbreiten: die Mehrjahresübersicht (E7) ist mit bis zu 13 Spalten der
            // breiteste Block des Blattes.
            for (int i = 2; i <= Math.Max(14, 2 * daten.Varianten.Count); i++) ws.Column(i).Width = 18;
            ws.SheetView.FreezeRows(2);
        }

        // ------------------------------------------------- Mehrjahresübersicht (E7)

        /// <summary>
        /// ETAPPE E7 — je Projekt eine Tabelle mit den Jahren 0…T als Zeilen und den
        /// Positionen des Zahlungsstroms als Spalten. Inhaltlich dieselbe Tabelle wie im
        /// Word-Bericht; beide bauen auf <see cref="Mehrjahresbild"/> auf.
        /// </summary>
        private static int BlattMehrjahres(IXLWorksheet ws, BerichtsDaten daten,
                                           WirtschaftlichkeitVerlauf verlauf,
                                           List<WirtschaftlichkeitErgebnis> alle, int r)
        {
            if (verlauf == null || verlauf.Absolut.All(s => s.Bild == null)) return r;

            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MJ_TITEL;
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Range(r, 1, r, 14).Style.Fill.BackgroundColor = GRUPPE;
            r++;
            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MJ_HINWEIS;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r += 2;

            foreach (VariantenDaten v in daten.Varianten)
            {
                VerlaufSerie serie = verlauf.Absolut.FirstOrDefault(s => s.IdProjekt == v.IdProjekt);
                Mehrjahresbild bild = Mehrjahresbild.Baue(serie);

                ws.Cell(r, 1).Value = (v.IstStamm ? "Stamm" : v.Anzeige);
                ws.Cell(r, 1).Style.Font.Bold = true;
                r++;
                if (bild == null)
                {
                    ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MJ_ENTFAELLT +
                        (serie != null && serie.Fehlgrund != null ? " (" + serie.Fehlgrund + ")" : "");
                    ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                    r += 2;
                    continue;
                }

                int spalten = bild.Spalten.Count;
                ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MJ_JAHR;
                for (int i = 0; i < spalten; i++) ws.Cell(r, 2 + i).Value = bild.Spalten[i].Titel;
                ws.Range(r, 1, r, 1 + spalten).Style.Font.Bold = true;
                ws.Range(r, 1, r, 1 + spalten).Style.Fill.BackgroundColor = KOPF;
                r++;

                for (int jahr = 0; jahr <= bild.Jahre; jahr++)
                {
                    ws.Cell(r, 1).Value = jahr;
                    for (int i = 0; i < spalten; i++)
                    {
                        ws.Cell(r, 2 + i).Value = bild.Spalten[i].Wert(jahr);
                        ws.Cell(r, 2 + i).Style.NumberFormat.Format = "#,##0";
                        if (bild.Spalten[i].IstSumme)
                            ws.Cell(r, 2 + i).Style.Fill.BackgroundColor = STAMM;
                    }
                    r++;
                }

                // Abschlusszeile mit dem Restwert-Barwert im Jahr T; sie schließt die
                // kumulierte Spalte auf den Nettobarwert auf.
                ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MJ_RESTWERT_T;
                ws.Cell(r, 1).Style.Font.Bold = true;
                for (int i = 0; i < spalten; i++)
                {
                    if (bild.Spalten[i].Schluessel == "BARWERT")
                        ws.Cell(r, 2 + i).Value = bild.RestwertBarwert;
                    else if (bild.Spalten[i].Schluessel == "KUMULIERT")
                        ws.Cell(r, 2 + i).Value = bild.Kapitalwert;
                    else continue;
                    ws.Cell(r, 2 + i).Style.NumberFormat.Format = "#,##0";
                    ws.Cell(r, 2 + i).Style.Font.Bold = true;
                    ws.Cell(r, 2 + i).Style.Fill.BackgroundColor = STAMM;
                }
                r++;

                ws.Cell(r, 1).Value = string.Format(MyResource.Resource.WIRT_MJ_PROBE,
                    bild.KumuliertT.ToString("N0", BerichtTexte.Kultur),
                    bild.RestwertBarwert.ToString("N0", BerichtTexte.Kultur),
                    bild.Kapitalwert.ToString("N0", BerichtTexte.Kultur));
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                r++;

                // Nachweisblock: vermiedene Kosten und Aufschlagsbetrag stehen
                // AUSSERHALB des Zahlungsstroms — beide stecken schon in anderen
                // Positionen, eine eigene Zeile wäre eine Doppelzählung.
                WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(x =>
                    x.IdProjekt == v.IdProjekt && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                bool vermieden = e != null &&
                                 (e.VermiedenGesamtJahr != 0 || e.VermiedenArbeitJahr != 0);
                if (e != null && (vermieden || e.AufschlagJahr != 0))
                {
                    ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MJ_NACHWEIS_TITEL;
                    ws.Cell(r, 1).Style.Font.Bold = true;
                    r++;
                    ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MJ_NACHWEIS_HINWEIS;
                    ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                    r++;
                    Action<string, double> nz = (label, wert) =>
                    {
                        ws.Cell(r, 1).Value = label;
                        ws.Cell(r, 2).Value = wert;
                        ws.Cell(r, 2).Style.NumberFormat.Format = "#,##0";
                        r++;
                    };
                    if (vermieden)
                    {
                        nz(MyResource.Resource.WIRT_ZEILE_VERMIEDEN_ARBEIT, e.VermiedenArbeitJahr);
                        nz(MyResource.Resource.WIRT_ZEILE_VERMIEDEN_LEISTUNG, e.VermiedenLeistungJahr);
                        nz(MyResource.Resource.WIRT_ZEILE_VERMIEDEN_GESAMT, e.VermiedenGesamtJahr);
                    }
                    if (e.AufschlagJahr != 0)
                        nz(MyResource.Resource.WIRT_ZEILE_AUFSCHLAG, e.AufschlagJahr);
                }
                r++;
            }
            return r;
        }

        // ------------------------------------------------- KWK-Zuschlag je Modul (E7)

        /// <summary>ETAPPE E7 — eine Zeile je BHKW-Modul (Übergabepunkt 1 aus E6).</summary>
        private static int BlattKwkgModule(IXLWorksheet ws, BerichtsDaten daten,
                                           List<WirtschaftlichkeitErgebnis> alle, int r)
        {
            var mitModulen = alle.Where(x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET &&
                                             x.KwkgModule != null && x.KwkgModule.Count > 0).ToList();
            if (mitModulen.Count == 0) return r;

            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_KWKG_MODUL_TITEL;
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Range(r, 1, r, 10).Style.Fill.BackgroundColor = GRUPPE;
            r++;
            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_KWKG_MODUL_HINWEIS;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r += 2;

            string[] kopf =
            {
                MyResource.Resource.WIRT_KWKG_SP_MODUL,
                MyResource.Resource.WIRT_KWKG_SP_PEL,
                MyResource.Resource.WIRT_KWKG_SP_VBH,
                MyResource.Resource.WIRT_KWKG_SP_SATZ_EIGEN,
                MyResource.Resource.WIRT_KWKG_SP_SATZ_EINSP,
                MyResource.Resource.WIRT_KWKG_SP_SATZQUELLE,
                MyResource.Resource.WIRT_KWKG_SP_DECKEL,
                MyResource.Resource.WIRT_KWKG_SP_KONTINGENT,
                MyResource.Resource.WIRT_KWKG_SP_BEGINN,
                MyResource.Resource.WIRT_KWKG_SP_JAHR1,
                MyResource.Resource.WIRT_KWKG_SP_ERSCHOEPFT
            };

            foreach (VariantenDaten v in daten.Varianten)
            {
                WirtschaftlichkeitErgebnis e = mitModulen.FirstOrDefault(x => x.IdProjekt == v.IdProjekt);
                if (e == null) continue;

                ws.Cell(r, 1).Value = (v.IstStamm ? "Stamm" : v.Anzeige);
                ws.Cell(r, 1).Style.Font.Bold = true;
                r++;

                for (int i = 0; i < kopf.Length; i++) ws.Cell(r, 1 + i).Value = kopf[i];
                ws.Range(r, 1, r, kopf.Length).Style.Font.Bold = true;
                ws.Range(r, 1, r, kopf.Length).Style.Fill.BackgroundColor = KOPF;
                r++;

                foreach (KwkgModulNachweis m in e.KwkgModule)
                {
                    ws.Cell(r, 1).Value = m.Bezeichner;
                    Zahl(ws, r, 2, m.PelKW, "#,##0");
                    Zahl(ws, r, 3, m.VbhElektrisch, "#,##0");
                    Zahl(ws, r, 4, m.SatzEigenCt, "#,##0.00");
                    Zahl(ws, r, 5, m.SatzEinspeisungCt, "#,##0.00");
                    ws.Cell(r, 6).Value = m.SatzAusAnlage
                        ? MyResource.Resource.WIRT_KWKG_SATZ_QUELLE_ANLAGE
                        : MyResource.Resource.WIRT_KWKG_SATZ_QUELLE_PROJEKT;
                    if (m.JahresdeckelH > 0) Zahl(ws, r, 7, m.JahresdeckelH, "#,##0");
                    else ws.Cell(r, 7).Value = MyResource.Resource.WIRT_KWKG_DECKEL_STAFFEL;
                    Zahl(ws, r, 8, m.KontingentH, "#,##0");
                    ws.Cell(r, 9).Value = m.Foerderbeginn;
                    Zahl(ws, r, 10, m.Jahr1Eur, "#,##0");
                    if (m.ErschoepftAbJahr > 0) ws.Cell(r, 11).Value = m.ErschoepftAbJahr;
                    else ws.Cell(r, 11).Value = MyResource.Resource.WIRT_KWKG_ERSCHOEPFT_NIE;
                    r++;
                }

                foreach (KwkgModulNachweis m in e.KwkgModule)
                {
                    if (m.HerleitungEigen.Length == 0 && m.HerleitungEinspeisung.Length == 0) continue;
                    ws.Cell(r, 1).Value = string.Format(MyResource.Resource.WIRT_KWKG_HERLEITUNG_ZEILE,
                                                        m.Bezeichner, m.HerleitungEigen,
                                                        m.HerleitungEinspeisung);
                    ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                    r++;
                }
                r++;
            }
            return r;
        }

        // --------------------------------------- Betriebskosten nach Kostenarten (E7)

        /// <summary>
        /// ETAPPE E7 — die Betriebskostenpositionen nach Kostenart der VDI 2067, je
        /// Position mit Bemessungsart und Herleitung. Zweck der E3-Spalte
        /// <c>Kostenart</c>.
        /// </summary>
        private static int BlattBetriebskosten(IXLWorksheet ws, BerichtsDaten daten,
                                               List<WirtschaftlichkeitErgebnis> alle, int r)
        {
            var mitPositionen = alle.Where(x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET &&
                                                x.Betriebskosten != null &&
                                                x.Betriebskosten.Count > 0).ToList();
            if (mitPositionen.Count == 0) return r;

            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_BK_TITEL;
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Range(r, 1, r, 6).Style.Fill.BackgroundColor = GRUPPE;
            r++;
            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_BK_HINWEIS;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r += 2;

            foreach (VariantenDaten v in daten.Varianten)
            {
                WirtschaftlichkeitErgebnis e = mitPositionen.FirstOrDefault(x => x.IdProjekt == v.IdProjekt);
                if (e == null) continue;

                ws.Cell(r, 1).Value = (v.IstStamm ? "Stamm" : v.Anzeige);
                ws.Cell(r, 1).Style.Font.Bold = true;
                r++;

                ws.Cell(r, 1).Value = MyResource.Resource.WIRT_BK_SP_POSITION;
                ws.Cell(r, 2).Value = MyResource.Resource.WIRT_BK_SP_GRUPPE;
                ws.Cell(r, 3).Value = MyResource.Resource.WIRT_BK_SP_BEMESSUNG;
                ws.Cell(r, 4).Value = MyResource.Resource.WIRT_BK_SP_HERLEITUNG;
                ws.Cell(r, 5).Value = MyResource.Resource.WIRT_BK_SP_BETRAG;
                ws.Range(r, 1, r, 5).Style.Font.Bold = true;
                ws.Range(r, 1, r, 5).Style.Fill.BackgroundColor = KOPF;
                r++;

                double summe = 0;
                foreach (string art in WirtschaftlichkeitZeilen.Kostenarten)
                {
                    List<KostenPositionNachweis> block = e.Betriebskosten
                        .Where(x => string.Equals(x.Kostenart ?? "", art, StringComparison.Ordinal))
                        .ToList();
                    if (block.Count == 0) continue;

                    ws.Cell(r, 1).Value = WirtschaftlichkeitZeilen.KostenartText(art);
                    ws.Range(r, 1, r, 5).Style.Font.Bold = true;
                    ws.Range(r, 1, r, 5).Style.Fill.BackgroundColor = STAMM;
                    r++;

                    foreach (KostenPositionNachweis n in block)
                    {
                        string herleitung = WirtschaftlichkeitZeilen.Herleitung(n, BerichtTexte.Kultur);
                        if (herleitung.Length == 0 && n.SzenarioGepflegt)
                            herleitung = MyResource.Resource.WIRT_BK_SZENARIOWERT;

                        ws.Cell(r, 1).Value = n.Bezeichnung;
                        ws.Cell(r, 2).Value = n.Gruppe;
                        ws.Cell(r, 3).Value = WirtschaftlichkeitZeilen.BemessungText(n.Bemessung);
                        ws.Cell(r, 4).Value = herleitung;
                        Zahl(ws, r, 5, n.BetragJahr, "#,##0");
                        r++;
                        summe += n.BetragJahr;
                    }
                }

                ws.Cell(r, 1).Value = MyResource.Resource.WIRT_BK_SUMME;
                Zahl(ws, r, 5, summe, "#,##0");
                ws.Range(r, 1, r, 5).Style.Font.Bold = true;
                ws.Range(r, 1, r, 5).Style.Fill.BackgroundColor = KOPF;
                r++;

                if (e.BetriebskostenJahr.HasValue &&
                    Math.Abs(summe - e.BetriebskostenJahr.Value) > 0.5)
                {
                    ws.Cell(r, 1).Value = string.Format(MyResource.Resource.WIRT_BK_ABWEICHUNG,
                        summe.ToString("N2", BerichtTexte.Kultur),
                        e.BetriebskostenJahr.Value.ToString("N2", BerichtTexte.Kultur));
                    ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#B22222");
                    r++;
                }
                r++;
            }
            return r;
        }

        /// <summary>Zahlwert mit Format in eine Zelle (E7-Hilfe).</summary>
        private static void Zahl(IXLWorksheet ws, int zeile, int spalte, double wert, string format)
        {
            ws.Cell(zeile, spalte).Value = wert;
            ws.Cell(zeile, spalte).Style.NumberFormat.Format = format;
        }

        // ------------------------------------------------------------- Detailblatt

        private static void BlattDetail(XLWorkbook wb, VariantenDaten v)
        {
            IXLWorksheet ws = wb.Worksheets.Add(BlattName(wb, v));

            int r = 1;
            ws.Cell(r, 1).Value = (v.IstStamm ? "Stamm — " : "Variante — ") + v.Projektname;
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 1).Style.Font.FontSize = 13;
            r++;
            ws.Cell(r, 1).Value = "Simulationsstand";
            if (v.SimulationsStand.HasValue)
            {
                ws.Cell(r, 2).Value = v.SimulationsStand.Value;
                ws.Cell(r, 2).Style.DateFormat.Format = "dd.MM.yyyy hh:mm";
            }
            r += 2;

            if (v.Fehler != null)
            {
                ws.Cell(r, 1).Value = "Fehler: " + v.Fehler;
                return;
            }

            // Kennzahlen (alle verfügbaren, echte Werte).
            ws.Cell(r, 1).Value = "Gruppe";
            ws.Cell(r, 2).Value = "Kennzahl";
            ws.Cell(r, 3).Value = "Wert";
            ws.Cell(r, 4).Value = "Einheit";
            KopfZeile(ws, r, 4);
            r++;
            // E5/F7: Das Detailblatt zeigt EINE Variante - ihr eigener Modus beschriftet.
            foreach (Kennzahl kz in KennzahlenKatalog.Alle(v.EmissionsModus))
            {
                double? wert = Wert(v, kz.Schluessel);
                if (!wert.HasValue) continue;
                ws.Cell(r, 1).Value = kz.Gruppe;
                ws.Cell(r, 2).Value = kz.Label(BerichtTexte.Englisch);
                ws.Cell(r, 3).Value = wert.Value;
                ws.Cell(r, 3).Style.NumberFormat.Format = Format(kz.Format);
                ws.Cell(r, 4).Value = kz.Einheit;
                r++;
            }
            r++;

            // Erzeuger-Module.
            r = ModulBlock(ws, r, v.Ergebnis);

            // Brennstoffmengen.
            if (v.Brennstoffmengen != null && v.Brennstoffmengen.Rows.Count > 0)
            {
                ws.Cell(r, 1).Value = "Brennstoffmengen";
                ws.Cell(r, 1).Style.Font.Bold = true;
                r++;
                ws.Cell(r, 1).Value = "Erzeuger";
                ws.Cell(r, 2).Value = "Bezeichner";
                ws.Cell(r, 3).Value = "Menge";
                KopfZeile(ws, r, 3);
                r++;
                foreach (DataRow zeile in v.Brennstoffmengen.Rows)
                {
                    ws.Cell(r, 1).Value = zeile["Erzeuger"] != DBNull.Value ? zeile["Erzeuger"].ToString() : "";
                    ws.Cell(r, 2).Value = zeile["Bezeichner"] != DBNull.Value ? zeile["Bezeichner"].ToString() : "";
                    ws.Cell(r, 3).Value = zeile["Menge"] != DBNull.Value ? zeile["Menge"].ToString() : "";
                    r++;
                }
                r++;
            }

            // Monatswerte aus dem frischen Simulationslauf (nur wenn Zeitreihen vorliegen).
            if (v.Zeitreihen != null)
                r = MonatsBlock(ws, r, v.Zeitreihen);

            ws.SheetView.Freeze(1, 0);
            ws.Columns().AdjustToContents(1, 45);
        }

        private static int ModulBlock(IXLWorksheet ws, int r, ErgebnisModel m)
        {
            if (m == null) return r;

            ws.Cell(r, 1).Value = "Erzeuger — Einzelauflistung (Module)";
            ws.Cell(r, 1).Style.Font.Bold = true;
            r++;
            ws.Cell(r, 1).Value = "Erzeuger";
            ws.Cell(r, 2).Value = "Wärme [MWh/a]";
            ws.Cell(r, 3).Value = "Strom [MWh/a]";
            ws.Cell(r, 4).Value = "Energieträger";
            ws.Cell(r, 5).Value = "Verbrauch [MWh/a]";
            KopfZeile(ws, r, 5);
            r++;

            Action<string, double?, double?, string, double?> zeile = (name, waerme, strom, traeger, verbrauch) =>
            {
                ws.Cell(r, 1).Value = name;
                if (waerme.HasValue) { ws.Cell(r, 2).Value = waerme.Value; ws.Cell(r, 2).Style.NumberFormat.Format = "#,##0"; }
                if (strom.HasValue) { ws.Cell(r, 3).Value = strom.Value; ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0"; }
                ws.Cell(r, 4).Value = traeger ?? "";
                if (verbrauch.HasValue) { ws.Cell(r, 5).Value = verbrauch.Value; ws.Cell(r, 5).Style.NumberFormat.Format = "#,##0"; }
                r++;
            };

            if (m.Waermepumpe != null)
                foreach (ErgebnisWaermepumpeModulModel mo in m.Waermepumpe.Module)
                    zeile(Leer(mo.Modul, "Wärmepumpe"), mo.Waermeproduktion, null, "Strom", mo.Stromverbrauch + mo.Heizstab);
            if (m.BHKW != null)
                foreach (ErgebnisBHKWModulModel mo in m.BHKW.Module)
                    zeile(Leer(mo.Modul, "BHKW"), mo.Waermeproduktion, mo.Stromproduktion,
                          mo.Brennstoff, mo.Verbrauch > 0 ? (double?)mo.Verbrauch : null);
            if (m.Heizkessel != null)
                foreach (ErgebnisHeizkesselModulModel mo in m.Heizkessel.Module)
                    zeile(Leer(mo.Modul, "Spitzenkessel"),
                          mo.Waermeproduktion > 0 ? mo.Waermeproduktion : mo.Waerme_Gas + mo.Waerme_Oel,
                          null, mo.Brennstoff, mo.Verbrauch > 0 ? (double?)mo.Verbrauch : null);
            if (m.Solarthermie != null)
                foreach (ErgebnisSolarthermieModulModel mo in m.Solarthermie.Module)
                    zeile(Leer(mo.Modul, "Solarthermie"), mo.Waermeproduktion, null, null, null);
            if (m.Photovoltaik != null)
                foreach (ErgebnisPhotovoltaikModulModel mo in m.Photovoltaik.Module)
                    zeile(Leer(mo.Modul, "Photovoltaik"), null, mo.Stromproduktion, null, null);

            return r + 1;
        }

        private static int MonatsBlock(IXLWorksheet ws, int r, ZeitreihenSatz z)
        {
            var spalten = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(ZeitreihenSatz.WAERMEBEDARF, "Wärmebedarf"),
                new KeyValuePair<string, string>(ZeitreihenSatz.WP_WAERME, "Wärmepumpe"),
                new KeyValuePair<string, string>(ZeitreihenSatz.BHKW_WAERME, "BHKW-Wärme"),
                new KeyValuePair<string, string>(ZeitreihenSatz.KESSEL_WAERME, "Spitzenkessel"),
                new KeyValuePair<string, string>(ZeitreihenSatz.SOLAR_WAERME, "Solarthermie"),
                new KeyValuePair<string, string>(ZeitreihenSatz.STROMBEDARF, "Strombedarf"),
                new KeyValuePair<string, string>(ZeitreihenSatz.PV_GENUTZT, "PV-Eigenverbrauch"),
                new KeyValuePair<string, string>(ZeitreihenSatz.BHKW_STROM, "BHKW-Strom"),
                new KeyValuePair<string, string>(ZeitreihenSatz.PV_UEBERSCHUSS, "Einspeisung"),
                new KeyValuePair<string, string>(ZeitreihenSatz.NETZBEZUG, "Netzbezug"),
            }.Where(s => z.Hat(s.Key)).ToList();
            if (spalten.Count == 0) return r;

            ws.Cell(r, 1).Value = "Monatswerte [MWh] (aus dem Simulationslauf dieses Berichts)";
            ws.Cell(r, 1).Style.Font.Bold = true;
            r++;
            ws.Cell(r, 1).Value = "Monat";
            for (int s = 0; s < spalten.Count; s++) ws.Cell(r, 2 + s).Value = spalten[s].Value;
            KopfZeile(ws, r, 1 + spalten.Count);
            r++;

            string[] monate = { "Januar", "Februar", "März", "April", "Mai", "Juni",
                                "Juli", "August", "September", "Oktober", "November", "Dezember" };
            var werte = spalten.Select(s => ChartRenderer.MonatsSummenMWh(z.Hole(s.Key))).ToList();
            for (int m = 0; m < 12; m++)
            {
                ws.Cell(r, 1).Value = monate[m];
                for (int s = 0; s < spalten.Count; s++)
                {
                    ws.Cell(r, 2 + s).Value = werte[s][m];
                    ws.Cell(r, 2 + s).Style.NumberFormat.Format = "#,##0.0";
                }
                r++;
            }
            return r + 1;
        }

        // ------------------------------------------------------------- Helfer

        private static void KopfZeile(IXLWorksheet ws, int zeile, int bisSpalte)
        {
            var rng = ws.Range(zeile, 1, zeile, bisSpalte);
            rng.Style.Font.Bold = true;
            rng.Style.Fill.BackgroundColor = KOPF;
        }

        private static double? Wert(VariantenDaten v, string schluessel)
        {
            if (v == null) return null;
            return v.Kennzahlen.ContainsKey(schluessel) ? v.Kennzahlen[schluessel] : null;
        }

        // Katalogformat ("N0"/"N1"/"N2") → Excel-Zellformat.
        private static string Format(string katalogFormat)
        {
            switch (katalogFormat)
            {
                case "N0": return "#,##0";
                case "N1": return "#,##0.0";
                case "N2": return "#,##0.00";
                default: return "#,##0.0";
            }
        }

        private static string Leer(string s, string fallback)
        { return string.IsNullOrWhiteSpace(s) ? fallback : s.Trim(); }

        // Eindeutiger, gültiger Blattname (max. 31 Zeichen, ohne []:*?/\).
        private static string BlattName(XLWorkbook wb, VariantenDaten v)
        {
            string name = v.IstStamm ? "Stamm" : v.Anzeige;
            foreach (char c in new[] { '[', ']', ':', '*', '?', '/', '\\' }) name = name.Replace(c, '_');
            if (name.Length > 28) name = name.Substring(0, 28);
            string basis = name;
            int n = 2;
            while (wb.Worksheets.Any(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)))
                name = basis + " " + n++;
            return name;
        }
    }
}
