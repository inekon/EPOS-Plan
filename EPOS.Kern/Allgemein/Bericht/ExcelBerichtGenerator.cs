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
        // ETAPPE E8b: intern statt privat — die Blöcke der Formelmappe (ExcelFormelmappe)
        // zeichnen in denselben Farben.
        internal static readonly XLColor KOPF = XLColor.FromHtml("#D9E1F2");
        internal static readonly XLColor STAMM = XLColor.FromHtml("#F2F2F2");
        internal static readonly XLColor GRUPPE = XLColor.FromHtml("#EAEDED");

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
        internal static void GrafikModulSicherstellen()
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

            // ETAPPE E8b (Formelmappe): das Register der Formelzellen — jede Formel, die ein
            // Block schreibt, merkt sich hier ihr Ergebnis; nach dem Speichern wird es
            // eingetragen (Befund E8b/0: ClosedXML legt keine Ergebnisse ab).
            var formeln = new Formelregister();

            using (var wb = new XLWorkbook())
            {
                SchreibeBlaetter(wb, daten, konfig, formeln, null, null);

                // ETAPPE E8b: Eine Mappe mit Formeln verlangt beim Öffnen die volle
                // Neuberechnung — Excel und LibreOffice rechnen dann selbst.
                if (formeln.Anzahl > 0) wb.FullCalculationOnLoad = true;
                wb.SaveAs(zielDatei);
            }

            // ETAPPE E8b: die Ergebnisse der Formelzellen nachtragen — dieselben Zahlen, die
            // die Zellen als Werte trugen; ein Betrachter ohne Rechenmaschine zeigt sie.
            formeln.Nachtragen(zielDatei);
            return zielDatei;
        }

        // ------------------------------------------------------------- Die erzeugten Blätter

        /// <summary>
        /// Die erzeugten Blätter der Mappe (Konzept Berichtsvorlagen 7.2): Übersicht, Vergleich,
        /// Wirtschaftlichkeit (Formelmappe), Verlauf, je Stand ein Detailblatt, Checkliste — in dieser
        /// Folge.
        /// </summary>
        internal enum Blattart
        {
            Uebersicht,
            Vergleich,
            Wirtschaftlichkeit,
            Verlauf,
            Detail,
            Checkliste,
        }

        /// <summary>
        /// Legt die erzeugten Blätter in der heutigen Folge und unter den heutigen Bedingungen an
        /// (Übersicht und Vergleich immer; Wirtschaftlichkeit und Checkliste nur mit
        /// <see cref="BerichtsKonfiguration.B_WIRTSCHAFT"/>, Verlauf nur mit Verlauf, je Stand ein
        /// Detailblatt nur mit <see cref="BerichtsKonfiguration.B_ERGEBNISSE"/>). Ohne Rückrufe ist das
        /// genau der Weg ohne Vorlage (<see cref="Erzeuge"/>). Der Füller einer Excel-Vorlage
        /// (<see cref="ExcelVorlagenfueller"/>, BV-E7) hört mit: <paramref name="erzeugen"/> fragt vor
        /// jedem Blatt, das entstünde (beim Detailblatt je Stand) — <c>false</c> heißt, der Füller baut
        /// es selbst (Musterblatt); <paramref name="erzeugt"/> meldet danach die neuen Blätter.
        /// Wird ein Blatt nicht gefragt, hat es in diesem Bericht keinen Inhalt.
        /// </summary>
        internal static void SchreibeBlaetter(XLWorkbook wb, BerichtsDaten daten, BerichtsKonfiguration konfig,
                                              Formelregister formeln, Func<Blattart, VariantenDaten, bool> erzeugen,
                                              Action<Blattart, VariantenDaten, IReadOnlyList<IXLWorksheet>> erzeugt)
        {
            Action<Blattart, VariantenDaten, Action> blatt = (art, stand, schreibe) =>
            {
                if (erzeugen != null && !erzeugen(art, stand)) return;
                var vorher = new HashSet<IXLWorksheet>(wb.Worksheets);
                schreibe();
                if (erzeugt != null) erzeugt(art, stand, wb.Worksheets.Where(w => !vorher.Contains(w)).ToList());
            };

            blatt(Blattart.Uebersicht, null, () => BlattUebersicht(wb, daten));
            blatt(Blattart.Vergleich, null, () => BlattVergleich(wb, daten, formeln));

            // Phase 6: Kapitalwert-Ergebnisse dieses Berichtslaufs (gleiche Quelle
            // wie der Word-Baustein — BerichtsDaten.Wirtschaftlichkeit, ersatzweise
            // der persistierte Stand aus Tab_ErgebnisWirtschaftlichkeit).
            if (konfig != null && konfig.IstAktiv(BerichtsKonfiguration.B_WIRTSCHAFT))
            {
                WirtschaftlichkeitVerlaufSzenarien verlauf = null;
                blatt(Blattart.Wirtschaftlichkeit, null, () => verlauf = BlattWirtschaftlichkeit(wb, daten, formeln));

                // ETAPPE E6 (U13): das Blatt „Verlauf" — je Jahr eine Zeile, je Variante
                // und Szenario eine Spalte, dieselben Linien wie das Dreierbild des
                // Wortberichts und dasselbe Blatt, das der Knopf „Verlauf nach Excel…"
                // der Seite schreibt. Ohne Verlauf (keine Linie, Zeitreihen fehlen)
                // entfällt es — der Grund steht im Blatt „Wirtschaftlichkeit".
                if (verlauf != null && !verlauf.Leer)
                    blatt(Blattart.Verlauf, null, () =>
                        VerlaufExcel.SchreibeBlatt(wb, verlauf, VerlaufBlattTexte.AusRessourcen(),
                            string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_VERL_STATUS,
                                          verlauf.Jahre) + "."));
            }

            if (konfig == null || konfig.IstAktiv(BerichtsKonfiguration.B_ERGEBNISSE))
                foreach (VariantenDaten v in daten.Varianten)
                    blatt(Blattart.Detail, v, () => BlattDetail(wb, v));

            // ETAPPE E8b (U43, Konzept V‑G12): die Anhang-E-Checkliste als letztes Blatt —
            // nur mit dem Baustein „Wirtschaftlichkeit", auf dessen Blöcke sie verweist.
            if (konfig != null && konfig.IstAktiv(BerichtsKonfiguration.B_WIRTSCHAFT))
                blatt(Blattart.Checkliste, null, () => AnhangECheckliste.SchreibeExcel(wb, AnhangECheckliste.AusBericht(daten)));
        }

        /// <summary>
        /// Die festen Namen der erzeugten Blätter in der Sprache des Laufs — die Blätter, deren
        /// <c>Worksheets.Add</c> an einem gleichnamigen Anwenderblatt scheitern würde (der Verlauf
        /// und die Detailblätter weichen selbst auf einen freien Namen aus).
        /// </summary>
        internal static IReadOnlyDictionary<Blattart, string> FesteBlattnamen()
        {
            return new Dictionary<Blattart, string>
            {
                [Blattart.Uebersicht] = "Übersicht",
                [Blattart.Vergleich] = "Vergleich",
                [Blattart.Wirtschaftlichkeit] = BerichtTexte.T("Wirtschaftlichkeit"),
                [Blattart.Checkliste] = AnhangECheckliste.Blattname,
            };
        }

        /// <summary>Der Name des Verlaufsblatts in der Sprache des Laufs (vor dem Ausweichen auf einen freien Namen).</summary>
        internal static string Verlaufsblattname()
        {
            return VerlaufBlattTexte.AusRessourcen().Blatt;
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

        private static void BlattVergleich(XLWorkbook wb, BerichtsDaten daten, Formelregister formeln)
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
            // KU2 Welle 3: die Gruppe „Kälte“ zwischen Effizienz und Emissionen (KennzahlenKatalog.GRUPPEN).
            foreach (string gruppe in KennzahlenKatalog.GRUPPEN)
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
                            double delta = (wert.Value - stammWert.Value) / Math.Abs(stammWert.Value) * 100.0;
                            zelle.Value = delta;
                            zelle.Style.NumberFormat.Format = "+#,##0.0;−#,##0.0;±0,0";

                            // ETAPPE E8b, Stufe 3 (Konzept § 2.11.6): der Δ%-Block als
                            // Zellbezug auf die beiden Wertspalten derselben Zeile.
                            ExcelFormelmappe.Deltazelle(zelle, r, 4 + daten.Varianten.IndexOf(varianten[i]),
                                                        4 + daten.Varianten.IndexOf(stamm),
                                                        wert.Value, stammWert.Value, delta, formeln);
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
        /// <summary>
        /// Das Blatt „Wirtschaftlichkeit". ETAPPE E6: Es liefert den Verlauf mit drei
        /// Szenarien zurück, den es für seinen Verlaufsblock gerechnet hat — das Blatt
        /// „Verlauf" schreibt dieselben Linien, ohne ein zweites Mal zu rechnen.
        /// <c>null</c> = kein Verlauf (keine Ergebnisse, Zeitreihen fehlen, Rechenfehler).
        /// </summary>
        private static WirtschaftlichkeitVerlaufSzenarien BlattWirtschaftlichkeit(XLWorkbook wb, BerichtsDaten daten,
                                                                                  Formelregister formeln)
        {
            // BV-E3 (Konzept Berichtsvorlagen 5.1): Das Blatt liest den WERTESATZ des Laufs
            // (BerichtsDaten.Wirtschaft) — dieselben Teile wie der Wortbericht, vom Sammler EINMAL
            // über dieselben Aufrufe ermittelt; beim Schreiben wird die Datenbank nicht berührt.
            WirtschaftsBerichtswerte w = WirtschaftsBerichtswerte.Von(daten);
            bool ausDiesemLauf = w.AusDiesemLauf;
            List<WirtschaftlichkeitErgebnis> alle = w.Ergebnisse;

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
                return null;
            }

            WirtschaftlichkeitParameter p = w.Parameter;
            TarifParameter tarifP = w.Tarif;

            // ETAPPE E5 Teil b: die BEWERTUNG dieses Laufs — Bandbreite mit Einstufungen,
            // Vorschlag, Hinweistext, Deklarationen, Nutzungsdauer-Hinweise, Stände ohne
            // Nachweis und Sensitivität; dieselbe, die der Wortbericht liest. Ohne Sammler
            // (Proben, Rückfall) entsteht sie aus denselben Kernmethoden.
            WirtschaftlichkeitBewertung bewertung = w.Bewertung;

            ws.Cell(r, 1).Value = w.Parameternachweis(BerichtTexte.Kultur) +
                // ETAPPE E7 (Divergenz D1): Der TARIFnachweis stand bisher nur im
                // Word-Bericht. Er nennt Modell, Arbeitspreise und Preisstand — ohne ihn
                // ist die Stromkostenzeile im Excel-Blatt nicht nachvollziehbar.
                " · " + tarifP.Nachweis(BerichtTexte.Kultur) +
                // LEITENTSCHEIDUNGEN L12/L13: derselbe Ausweis wie im Word-Bericht —
                // Rechtsstand der Emissionsbewertung und Konvention der Biomasse.
                " · " + w.Bilanzkonvention.Ausweis(BerichtTexte.Kultur) +
                " · " + BerichtTexte.T("Referenz: Stammprojekt · Restwert linear") +
                " · " + BerichtTexte.T("Rechenstand") + ": " +
                alle[0].Zeitstempel.ToString("dd.MM.yyyy HH:mm", BerichtTexte.Kultur);
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r++;

            // ETAPPE E8b, Stufe 0 (Konzept § 2.11.6): der Parameterblock aus ECHTEN Zellen,
            // unmittelbar unter seiner Prosazeile — je Szenario ein Satz, die Spalte
            // „Erwartet" benannt. Alles darunter wandert um
            // ExcelFormelmappe.PARAMETERBLOCK_ZEILEN Zeilen; keine Zahl ändert sich.
            // ETAPPE E9a: mit Zeitraum, Menge und Erlössätzen je Szenario und — nur wo
            // gepflegt — den Trägerpreisen der Stände.
            // ETAPPE E15 (V‑G7): mit Risiko die Risikozeilen, ihre Formeln über das Register.
            // BV-E3: die gepflegten Trägerpreise der Stände aus dem Wertesatz (Traegerpreissatz.Lies).
            r = ExcelFormelmappe.Parameterblock(ws, r, p, daten.Varianten, formeln, w.Traegerpreise);

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
                if (ea == null || (ea.Fehlgrund == null && !w.ErgebnisAktuell(ea)))
                    veraltet.Add(v.IstStamm ? "Stamm" : v.Anzeige);
            }
            if (veraltet.Count > 0)
            {
                ws.Cell(r, 1).Value = string.Format(MyResource.Resource.WIRT_ERGEBNIS_VERALTET,
                                                    string.Join(", ", veraltet));
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#C00000");
                r++;
            }

            // ETAPPE E2 (VALERI-Lücke G7, Befund A5): der Betrachtungszeitraum gegen die
            // Nutzungsdauern. Er stand in Word und auf der Seite, im Excel-Blatt nicht —
            // DIN EN 17463 verlangt die Begründung des Zeitraums in JEDER Ausgabe, und
            // die Zahl T steht oben im Parameternachweis ohne jede Einordnung.
            // Derselbe Aufruf wie im Wortbericht (NutzungsdauerAbgleich.Hinweis).
            //
            // ETAPPE E5 Teil b (U39): Zeile und die Hinweiszeilen „k von n Positionen ohne
            // Nutzungsdauer" kommen aus der Bewertung — derselbe Kern-Controller wie auf
            // der Seite und im Wortbericht; je Hinweis eine Zeile.
            NutzungsdauerHinweise nutzungsdauer = bewertung.Nutzungsdauer ?? new NutzungsdauerHinweise();
            var nutzungsdauerZeilen = new List<string>();
            if (!string.IsNullOrEmpty(nutzungsdauer.Zeitraumzeile))
                nutzungsdauerZeilen.Add(nutzungsdauer.Zeitraumzeile);
            nutzungsdauerZeilen.AddRange(nutzungsdauer.Zeilen);
            foreach (string zeile in nutzungsdauerZeilen)
            {
                ws.Cell(r, 1).Value = zeile;
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                r++;
            }
            r++;

            // ETAPPE E7: EINE Zeilendefinition für Word, Excel und Ergebnisreiter
            // (WirtschaftlichkeitZeilen). Die Liste stand bis dahin dreimal im Code.
            // ETAPPE B7: eine Sichtbarkeitsregel für alle drei Ausgaben (Sichtbare).
            // KONZEPT § 2.9 und § 2.15 (VG-Q4): Das BLATT FOLGT DER SICHT - so wie es
            // den Haekchen folgt. In Sicht 2 stehen A und B mit A als Referenz, in
            // Sicht 1 alle Staende gegen die Referenz der Gruppe. Dieselbe
            // Zeilendefinition wie in Word und auf der Seite.
            int idReferenz = w.IdReferenzTafel;
            var spalten = new List<VariantenDaten>();
            if (daten.Sicht != null && daten.Sicht.IstPaar)
                foreach (int id in daten.Sicht.Spalten(null))
                {
                    VariantenDaten sv = daten.Varianten.FirstOrDefault(x => x.IdProjekt == id);
                    if (sv != null) spalten.Add(sv);
                }
            if (spalten.Count == 0) spalten.AddRange(daten.Varianten);

            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Sichtbare(w.Zeilen(idReferenz), alle);

            // KONZEPT § 2.15 (VG-Q4): In Sicht 2 sagt die DEKLARATIONSZEILE, wogegen
            // gerechnet wurde - die Norm laesst den Vergleich zweier Massnahmen zu
            // (8.1.2), verlangt aber die Benennung.
            if (daten.Sicht != null && daten.Sicht.IstPaar && spalten.Count > 0)
            {
                VariantenDaten gruppe = daten.Varianten.FirstOrDefault(
                    v => daten.IdGruppenreferenz > 0
                       ? v.IdProjekt == daten.IdGruppenreferenz : v.IstStamm);
                ws.Cell(r, 1).Value = Referenzwahl.Deklarationszeile(
                    Referenzwahl.Name(spalten[0]), Referenzwahl.Name(gruppe));
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                r++;
            }

            // Der Zeitbezug der €/a-Werte steht einmal über der Tabelle statt in vier
            // Zeilentiteln (E7).
            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_ZEILE_JAHR1;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r += 2;

            // ETAPPE E8b, Stufe 2: die Lage des Blocks „Erwartet" (Zeile je Kennzahl, Spalte je
            // Stand) — seine Kennzahlen bekommen nach den Mehrjahrestabellen ihre Formeln.
            var lagen = new Dictionary<string, KennzahlBlock>(StringComparer.Ordinal);

            foreach (string szenario in new[] { WirtschaftlichkeitSzenario.ERWARTET,
                                                WirtschaftlichkeitSzenario.BEST,
                                                WirtschaftlichkeitSzenario.WORST })
            {
                var block = alle.Where(x => x.Szenario == szenario).ToList();
                if (block.Count == 0) continue;
                // ETAPPE E14 (E14‑Q2 a): jeder Block merkt sich seine Lage — auch Günstig und
                // Ungünstig bekommen ihre Kennzahlen als Formeln auf die Tabellen ihres Szenarios.
                KennzahlBlock lage = lagen[szenario] = new KennzahlBlock();

                // E5‑Q2: der Anzeigename des Szenarios (Ungünstig / Erwartet / Günstig),
                // nicht der gespeicherte Schlüssel.
                ws.Cell(r, 1).Value = BerichtTexte.T("Szenario") + ": " + VerlaufZeilen.Szenarioname(szenario);
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Range(r, 1, r, 1 + spalten.Count).Style.Fill.BackgroundColor = GRUPPE;
                r++;

                // ETAPPE W5‑B‑11 (Anwenderentscheid 09.09.2026, G8): die ANNAHMEN des
                // Szenarios unmittelbar unter seiner Blöcküberschrift — der wirksame
                // Parametersatz und seine Herkunft (Vorgaben/gepflegt). Erwartet bekommt
                // keine Zeile: Es IST der Projektparametersatz, und der steht oben.
                SzenarioSatz satz = p.SatzFuer(szenario);
                if (satz != null)
                {
                    string name = szenario == WirtschaftlichkeitSzenario.BEST
                                ? MyResource.Resource.WIRT_SZEN_BEST
                                : MyResource.Resource.WIRT_SZEN_WORST;
                    ws.Cell(r, 1).Value = string.Format(BerichtTexte.Kultur,
                        satz.NurVorgaben ? MyResource.Resource.WPAR_SZ_HERKUNFT_VORGABE
                                         : MyResource.Resource.WPAR_SZ_HERKUNFT_GEPFLEGT,
                        name, satz.Nachweis(p, BerichtTexte.Kultur));
                    ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                    r++;

                    // ETAPPE E9a (Norm 9 c): die gepflegten Trägerpreise des Szenarios je
                    // Stand — nur, wo einer gepflegt ist (sonst keine Zeile, das Blatt bleibt).
                    string preise = w.Traegerpreiszeile(szenario, BerichtTexte.Kultur);
                    if (!string.IsNullOrEmpty(preise))
                    {
                        ws.Cell(r, 1).Value = preise;
                        ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                        r++;
                    }
                }

                int kopfZeile = r;
                int stammSpalte = -1;
                ws.Cell(r, 1).Value = BerichtTexte.T("Kennzahl");
                int c = 2;
                foreach (VariantenDaten v in spalten)
                {
                    ws.Cell(r, c).Value = v.IstStamm ? "Stamm" : v.Anzeige;
                    // Hinterlegt wird die REFERENZ - ohne gewaehlte wie bisher der Stamm.
                    if (idReferenz > 0 ? v.IdProjekt == idReferenz : v.IstStamm) stammSpalte = c;
                    if (lage != null) lage.Spalten[v.IdProjekt] = c;
                    c++;
                }
                ws.Range(kopfZeile, 1, kopfZeile, c - 1).Style.Font.Bold = true;
                ws.Range(kopfZeile, 1, kopfZeile, c - 1).Style.Fill.BackgroundColor = KOPF;
                r++;

                foreach (WirtZeile z in zeilen)
                {
                    // ETAPPE B7: Hier stand bis dahin eine ZWEITE Sichtbarkeitsprüfung
                    // (über den Szenarioblock). Sie ist entfallen — die Zeilenliste ist
                    // bereits gefiltert, und zwar nach derselben Regel wie in Word und
                    // im Reiter. Eine Zeile, die nur ein Szenario füllt, fehlte hier
                    // sonst in den beiden anderen Blöcken.

                    // Der Titel kommt aus MyResource — kein BerichtTexte.T() darüber.
                    ws.Cell(r, 1).Value = (z.Einzug > 0 ? "    " : "") + z.Titel;
                    if (lage != null) lage.Zeilen[z.Schluessel] = r;
                    if (z.IstUeberschrift || z.IstSumme)
                        ws.Cell(r, 1).Style.Font.Bold = true;
                    if (z.IstUeberschrift)
                    {
                        // Eine Überschrift trägt in den Wertspalten nichts — sie bleiben
                        // leer, damit Filter und Diagramme des Blattes numerisch bleiben.
                        ws.Range(r, 1, r, Math.Max(1, spalten.Count + 1))
                          .Style.Fill.BackgroundColor = KOPF;
                        r++;
                        continue;
                    }
                    c = 2;
                    foreach (VariantenDaten v in spalten)
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
                                if (lage != null) lage.Werte[(r, c)] = wert.Value;   // E14: Bandbreite
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

            // ---------------- ETAPPE E2 (G8): Bandbreite mit Spanne und Referenzzeile --
            //
            // Die drei Szenarioblöcke darüber zeigen jede Kennzahl je Szenario; was
            // fehlte, war die ZUSAMMENSCHAU — ΔKW in Worst/Erwartet/Best nebeneinander,
            // die Spanne dazwischen und die Zeile des Standes, gegen den gerechnet
            // wurde. Word führt dieselbe Tafel (BausteineWirtschaftlichkeit); gerechnet
            // wird nichts Neues. ETAPPE E5 Teil b: Die Tafel liest die Bandbreite der
            // Bewertung — Spanne als Betrag aus größtem und kleinstem Wert (Q4), die
            // Einstufungen wie auf den Karten, in Sicht 2 gegen A (Q6).
            r = BandbreitenTafel(ws, r, bewertung, lagen, formeln);

            // ---------------- ETAPPE W5‑B‑11 (G9): Vorschlag zur Entscheidung ----------
            //
            // EINE Zelle unter den drei Szenarioblöcken — dieselbe Regel und derselbe
            // Satz wie in Word und auf der Seite (WirtschaftlichkeitEmpfehlung). Leer
            // bleibt sie, solange keine Variante ein Erwartet-Ergebnis gegenüber der
            // Referenz hat; ein Vorschlag ohne Zahlen wäre eine Behauptung.
            // ETAPPE E2 (G9): Er nennt die Referenz beim Namen. ETAPPE E5 Teil b: der Satz
            // der Bewertung.
            string empfehlung = bewertung.Vorschlagstext;
            if (!string.IsNullOrEmpty(empfehlung))
            {
                ws.Cell(r, 1).Value = empfehlung;
                ws.Cell(r, 1).Style.Font.Bold = true;
                r += 2;
            }

            // ---------------- ETAPPE W5‑B‑12 (G6): nicht monetäre Wirkungen ----------
            //
            // EINE Zelle UNTER der Vorschlagszelle — dieselbe Stelle wie in Word (dort
            // Überschrift + Absatz nach dem Vorschlag). Ohne gepflegten Text entfällt
            // sie ganz: Eine Zeile „Nicht monetäre Wirkungen:" ohne Inhalt wäre die
            // Behauptung, es gäbe keine.
            //
            // ETAPPE E17 (V‑G11, DIN EN 17463 6.1 und 8.2): statt der Freitextzelle die TABELLE
            // „Nicht monetarisierbare Wirkungen" an derselben Stelle — Titelzeile, Kopf, je
            // Wirkung eine Zeile. Reine Werte, keine Formel: Die Beurteilung ist Anzeige, sie
            // fließt in keine Zelle der Rechnung.
            r = NichtMonetaereWirkungenTafel(ws, r, bewertung.Wirkungen);

            // ---------------- Hinweise dieses Laufs (ETAPPE E7, Divergenz D2) ----------------
            //
            // e.Hinweis erschien in Excel BISHER NIRGENDS. Darin stehen sämtliche
            // Begründungen der Etappen E2 bis E6: warum eine Gutschrift 0 ist, welcher
            // Preiszerlegung angesetzt wurde, welche Anlage am Stichtag scheitert, wie
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
            // ETAPPE E8b, Stufe 3: bemessene Positionen als Menge × Satz.
            r = BlattBetriebskosten(ws, daten, alle, r, formeln);

            // ---------------- Kapitalwert-Verlauf (Phase 11, Szenario Erwartet) ----------------
            // Jahresreihen frisch aus den Berichtsdaten gerechnet (T aus den Parametern);
            // dieselben Werte wie die Diagramme in Word und im Verlaufs-Dialog.
            // Konsistenz-Gate wie im Word-Baustein (Review 11): sind Tarif/KWKG aktiv,
            // aber keine Stundenreihen im Berichtslauf, entfällt der Block mit Hinweis.
            // ETAPPE BK1: dieselbe EINE Regel wie im Word-Baustein und im Rechenkern.
            // Q11 (E7b): nur ein WIRKSAMER Tarifsatz (Rollentarif) braucht die Reihen.
            // BV-E3: Die Regel steht im Wertesatz (VerlaufEntfaellt) — dieselbe wie im Wortbericht.
            int rStart = r;
            // E7/E14: der Verlauf je Zeitraum ist zugleich die Grundlage der Mehrjahrestabellen.
            WirtschaftlichkeitVerlaufSzenarien verlaufSzenarien = null;
            try
            {
                if (w.VerlaufEntfaellt)
                {
                    ws.Cell(r, 1).Value = BerichtTexte.T(
                        "Kapitalwert-Verlauf entfällt: Bericht ohne Stundenreihen erzeugt (Baustein „Ergebnisse je Variante“ aktivieren).");
                    ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                    r += 2;
                }
                else
                {
                    // ETAPPE E6 (U13): alle drei Szenarien — drei vollständige Läufe ohne
                    // Speichern. Die Mehrjahrestabelle nimmt daraus den Erwartungsfall, Zahl
                    // für Zahl der bisherige Einzellauf.
                    // ETAPPE E9a (Schritt B): jedes Szenario über SEINEN Betrachtungszeitraum;
                    // die Tabelle läuft bis zum längsten, Jahre jenseits von T_s bleiben leer.
                    // BV-E3: gerechnet hat ihn der Sammler; scheiterte die Rechnung, entfällt der
                    // Block wie bei einem Fehler hier (Fang unten).
                    WirtschaftlichkeitVerlaufSzenarien drei = w.Verlauf
                        ?? throw new InvalidOperationException("Der Kapitalwertverlauf ließ sich nicht rechnen.");
                    WirtschaftlichkeitVerlauf verlauf = drei.Lauf(WirtschaftlichkeitSzenario.ERWARTET);
                    verlaufSzenarien = drei;           // E6: Grundlage des Blattes „Verlauf" und (E7/E14) der Mehrjahrestabellen
                    var mitReihe = verlauf.Absolut.Where(s => s.Kumuliert != null).ToList();
                    var mitDiff = verlauf.Differenz.Where(x => x.Kumuliert != null).ToList();
                    if (mitReihe.Count > 0)
                    {
                        // ETAPPE E6 (Konzept § 2.13 (5)): JE SZENARIO EINE SPALTENGRUPPE —
                        // Ungünstig · Erwartet · Günstig, in jeder dieselben Spalten wie
                        // bisher (je Projekt, dann die Differenzen), darüber der Name des
                        // Szenarios. Die Spalten eines Projekts, das in einem Szenario keine
                        // Reihe hat, bleiben leer.
                        var gruppen = new List<WirtschaftlichkeitVerlauf>();
                        foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
                            if (drei.Lauf(s) != null) gruppen.Add(drei.Lauf(s));
                        int breite = mitReihe.Count + mitDiff.Count;
                        int letzte = 1 + gruppen.Count * breite;

                        ws.Cell(r, 1).Value = BerichtTexte.T("Kapitalwert-Verlauf (kumulierte Barwerte, ohne Restwert) [€]");
                        ws.Cell(r, 1).Style.Font.Bold = true;
                        ws.Range(r, 1, r, letzte).Style.Fill.BackgroundColor = GRUPPE;
                        r++;

                        int cg = 2;
                        foreach (WirtschaftlichkeitVerlauf lauf in gruppen)
                        {
                            ws.Cell(r, cg).Value = VerlaufZeilen.Szenarioname(lauf.Szenario);
                            IXLRange kopf = ws.Range(r, cg, r, cg + breite - 1);
                            if (breite > 1) kopf.Merge();
                            kopf.Style.Font.Bold = true;
                            kopf.Style.Fill.BackgroundColor = GRUPPE;
                            kopf.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cg += breite;
                        }
                        r++;

                        // Der Δ-Kopf nennt die REFERENZ, gegen die der Verlauf rechnet (in
                        // Sicht 2 A, sonst die Gruppenreferenz) — mit dem Namen ihrer
                        // Spalte daneben, nicht fest „Stamm".
                        string referenz = Referenzspalte(verlauf);
                        ws.Cell(r, 1).Value = BerichtTexte.T("Jahr");
                        int cv = 2;
                        foreach (WirtschaftlichkeitVerlauf lauf in gruppen)
                        {
                            foreach (VerlaufSerie s in mitReihe)
                            { ws.Cell(r, cv).Value = s.Anzeige; cv++; }
                            foreach (VerlaufSerie s in mitDiff)
                            { ws.Cell(r, cv).Value = "Δ " + s.Anzeige + " − " + referenz; cv++; }
                        }
                        ws.Range(r, 1, r, cv - 1).Style.Font.Bold = true;
                        ws.Range(r, 1, r, cv - 1).Style.Fill.BackgroundColor = KOPF;
                        r++;

                        // ETAPPE E9a: bis zum längsten Zeitraum der drei Läufe — eine Zelle
                        // jenseits des Zeitraums ihres Szenarios bleibt leer (Verlaufszelle).
                        for (int t = 0; t <= drei.Jahre; t++)
                        {
                            ws.Cell(r, 1).Value = t;
                            cv = 2;
                            foreach (WirtschaftlichkeitVerlauf lauf in gruppen)
                            {
                                foreach (VerlaufSerie s in mitReihe)
                                {
                                    Verlaufszelle(ws.Cell(r, cv), lauf.Absolut, s.IdProjekt, t);
                                    cv++;
                                }
                                foreach (VerlaufSerie s in mitDiff)
                                {
                                    Verlaufszelle(ws.Cell(r, cv), lauf.Differenz, s.IdProjekt, t);
                                    cv++;
                                }
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
                try { ws.Range(rStart, 1, r + 1, 6 * daten.Varianten.Count + 1).Clear(XLClearOptions.All); }
                catch { }
                r = rStart;
                verlaufSzenarien = null;
            }

            // ---------------- Mehrjahresübersicht der Zahlungsströme (E7) ----------------
            // ETAPPE E8b, Stufe 1: Die Tabellen rechnen in Formeln auf den Parameterblock;
            // ihre Lage merkt sich die Liste — die Kennzahlen der Stufe 2 beziehen sich darauf.
            // ETAPPE E14 (E14‑Q1 a): je Szenario eine Tabelle je Stand — Erwartet an seiner
            // Stelle, darunter Günstig und Ungünstig, jede mit den Eingangswerten ihres Laufs
            // (Verlauf je Zeitraum) und den Namen ihrer Spalte im Parameterblock; die Jahreszeilen
            // reichen in allen drei bis zum längsten Zeitraum.
            var tafelnJeSzenario = new Dictionary<string, List<MehrjahresTafel>>(StringComparer.Ordinal);
            if (verlaufSzenarien != null)
                foreach (string szenario in new[] { WirtschaftlichkeitSzenario.ERWARTET,
                                                    WirtschaftlichkeitSzenario.BEST,
                                                    WirtschaftlichkeitSzenario.WORST })
                {
                    WirtschaftlichkeitVerlauf lauf = verlaufSzenarien.Lauf(szenario);
                    if (lauf == null) continue;
                    var tafeln = new List<MehrjahresTafel>();
                    r = BlattMehrjahres(ws, daten, lauf, alle, r, p.FuerSzenario(szenario), szenario,
                                        verlaufSzenarien.Jahre, formeln, tafeln);
                    tafelnJeSzenario[szenario] = tafeln;
                }

            // ETAPPE E8b, Stufe 2 (Konzept § 2.11.6): die Kennzahlen in Formeln — Nettobarwert
            // über NBW, Differenz als Zellbezug, Annuität über RMZ auf die Tabellen; interner
            // Zinsfuß (IKV) und Amortisation über die Differenzreihe Variante − Referenz, die
            // jede Tabelle einer Variante rechts bekommt. Gegen dieselbe Referenz wie der
            // Verlauf, aus dem die Tabellen stehen.
            foreach (KeyValuePair<string, List<MehrjahresTafel>> kv in tafelnJeSzenario)
            {
                KennzahlBlock lageS;
                if (!lagen.TryGetValue(kv.Key, out lageS)) continue;
                ExcelFormelmappe.Kennzahlen(ws, lageS, kv.Value, verlaufSzenarien.Lauf(kv.Key), alle,
                                            p.FuerSzenario(kv.Key), kv.Key, formeln);
            }

            // ---------------- Sensitivitätsanalyse (W2, Szenario Erwartet) ----------------
            // ETAPPE E5 Teil b (V‑A, V‑G6): die Zeilen der Bewertung dieses Laufs (in
            // Sicht 2 gegen A) samt Steigung — die Wertspalte bleibt NUMERISCH, die Einheit
            // steht daneben als Text (€/%-Pkt. oder €/%).
            List<SensitivitaetZeile> sens = bewertung.Sensitivitaet ?? new List<SensitivitaetZeile>();
            if (sens.Count > 0)
            {
                ws.Cell(r, 1).Value = BerichtTexte.T("Sensitivitätsanalyse (Szenario „Erwartet“)");
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Range(r, 1, r, 6).Style.Fill.BackgroundColor = GRUPPE;
                r++;

                foreach (VariantenDaten v in daten.Varianten)
                {
                    var zeilenSens = sens.Where(x => x.IdProjekt == v.IdProjekt).ToList();
                    if (zeilenSens.Count == 0) continue;

                    ws.Cell(r, 1).Value = BerichtTexte.T(v.IstStamm ? "Stamm" : "Variante") + ": " + v.Anzeige;
                    ws.Cell(r, 1).Style.Font.Bold = true;
                    r++;

                    ws.Cell(r, 1).Value = BerichtTexte.T("Parameter");
                    ws.Cell(r, 2).Value = BerichtTexte.T("KW bei −Δ [€]");
                    ws.Cell(r, 3).Value = BerichtTexte.T("KW Basis [€]");
                    ws.Cell(r, 4).Value = BerichtTexte.T("KW bei +Δ [€]");
                    ws.Cell(r, 5).Value = MyResource.Resource.WIRT_SENS_SP_STEIGUNG;
                    ws.Cell(r, 6).Value = MyResource.Resource.WIRT_SENS_SP_EINHEIT;
                    ws.Range(r, 1, r, 6).Style.Font.Bold = true;
                    ws.Range(r, 1, r, 6).Style.Fill.BackgroundColor = KOPF;
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
                        // Ohne stetige Stufe (Wegfall des KWKG-Zuschlags) keine Steigung —
                        // beide Zellen bleiben leer.
                        if (z.Steigung.HasValue)
                        {
                            ws.Cell(r, 5).Value = z.Steigung.Value;
                            ws.Cell(r, 5).Style.NumberFormat.Format = "#,##0.00";
                            ws.Cell(r, 6).Value = z.SteigungEinheit;
                        }
                        r++;
                    }
                    r++;
                }
            }

            // ---------------- Strommengen-Matrix (W3) ----------------
            // Q11 (E7b): keine Tarifzonen mehr — eine Jahreszeile je Projekt.
            Dictionary<int, StromMatrix> matrizen = w.Strommatrizen;
            if (matrizen.Count > 0)
            {
                ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MATRIX_TITEL + " [MWh]";
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Range(r, 1, r, 5).Style.Fill.BackgroundColor = GRUPPE;
                r++;
                foreach (VariantenDaten v in daten.Varianten)
                {
                    if (!matrizen.ContainsKey(v.IdProjekt)) continue;
                    StromMatrix m = matrizen[v.IdProjekt];

                    ws.Cell(r, 1).Value = (v.IstStamm ? "Stamm" : v.Anzeige) +
                        " — " + MyResource.Resource.WIRT_MATRIX_STUNDENLAST + " " +
                        m.MaxBezugKW.ToString("N0", BerichtTexte.Kultur) + " kW";
                    ws.Cell(r, 1).Style.Font.Bold = true;
                    r++;

                    ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MATRIX_ZEITRAUM;
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
                    ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MATRIX_JAHR;
                    double[] werte = { m.BedarfGesamtMWh, m.BezugGesamtMWh, m.EinspeisungPvGesamtMWh,
                                       m.KwkEigenGesamtMWh, m.KwkEinspeisungGesamtMWh };
                    for (int i = 0; i < werte.Length; i++)
                    {
                        ws.Cell(r, 2 + i).Value = werte[i];
                        ws.Cell(r, 2 + i).Style.NumberFormat.Format = "#,##0.0";
                    }
                    r++;
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
                    if (erw == null || !w.ErgebnisAktuell(erw)) continue;
                    EmissionsBilanz b = w.Emissionsbilanz(v.IdProjekt);   // BV-E3: EmissionsBilanzRechner im Sammler
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

            // ---------------- ETAPPE E5 Teil b: Bewertung nach DIN EN 17463 ----------------
            //
            // Die Deklarationen der Bewertung (nominal · Steuern · Restwert · Risiko, die
            // Risikozeile nach Q5 ohne gepflegten Text „keine benannt") und die Nr.-31-Zeile.
            // Der Block steht am ENDE des Blattes: Er ist Ausweis, keine Tafel, und die
            // Ankerzeilen der Tafeln darüber bleiben, wo sie sind.
            var deklarationen = new List<string>();
            if (bewertung.Deklarationen != null)
                foreach (ValeriDeklaration d in bewertung.Deklarationen)
                    if (d != null && !string.IsNullOrEmpty(d.Text)) deklarationen.Add(d.Text);
            string nachweis = WirtschaftlichkeitBewertung.Nachweiszeile(bewertung.OhneNachweis);
            if (deklarationen.Count > 0 || !string.IsNullOrEmpty(nachweis))
            {
                ws.Cell(r, 1).Value = MyResource.Resource.WPAR_G_BEWERTUNG;
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = GRUPPE;
                r++;
                foreach (string d in deklarationen)
                {
                    ws.Cell(r, 1).Value = d;
                    ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                    r++;
                }
                if (!string.IsNullOrEmpty(nachweis))
                {
                    ws.Cell(r, 1).Value = nachweis;
                    ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                    r++;
                }
                r++;
            }

            ws.Column(1).Width = 32;
            // Spaltenbreiten: die Mehrjahresübersicht (E7) ist mit bis zu 13 Spalten der
            // breiteste Block des Blattes. ETAPPE E6: Der Verlauf trägt je Szenario eine
            // Spaltengruppe — drei Gruppen aus Projekten und Differenzen.
            // ETAPPE E8b: Die Formelmappe hängt rechts an die Mehrjahrestabellen Hilfs- und
            // Differenzspalten an — auch sie bekommen die Breite der Zahlenspalten.
            int breit = Math.Max(14, 6 * daten.Varianten.Count);
            IXLColumn letzteSpalte = ws.LastColumnUsed();
            if (letzteSpalte != null) breit = Math.Max(breit, letzteSpalte.ColumnNumber());
            for (int i = 2; i <= breit; i++) ws.Column(i).Width = 18;
            ws.SheetView.FreezeRows(2);
            return verlaufSzenarien;
        }

        /// <summary>
        /// ETAPPE E6 — eine Zelle des Verlaufsblocks: der kumulierte Barwert des Projekts
        /// <paramref name="idProjekt"/> im Jahr <paramref name="t"/> aus DIESER Spaltengruppe.
        /// Ohne Reihe bleibt die Zelle leer (nie 0 — Konzept Kap. 5/9).
        /// </summary>
        private static void Verlaufszelle(IXLCell zelle, List<VerlaufSerie> reihen, int idProjekt, int t)
        {
            foreach (VerlaufSerie s in reihen)
            {
                if (s == null || s.IdProjekt != idProjekt || s.Kumuliert == null || t >= s.Kumuliert.Length)
                    continue;
                zelle.Value = s.Kumuliert[t];
                zelle.Style.NumberFormat.Format = "#,##0";
                return;
            }
        }

        /// <summary>
        /// ETAPPE E6 — der Name der Referenz eines Verlaufs, so wie ihre Spalte im selben
        /// Block ihn trägt (<see cref="WirtschaftlichkeitVerlauf.IdReferenz"/>). Ohne
        /// aufgelöste Referenz der Stamm — dann rechnet der Verlauf gegen ihn.
        /// </summary>
        private static string Referenzspalte(WirtschaftlichkeitVerlauf verlauf)
        {
            VerlaufSerie referenz = null;
            if (verlauf != null)
                referenz = verlauf.Absolut.FirstOrDefault(s => s != null && verlauf.IdReferenz > 0 &&
                                                               s.IdProjekt == verlauf.IdReferenz)
                        ?? verlauf.Absolut.FirstOrDefault(s => s != null && s.IstStamm);
            return referenz != null && !string.IsNullOrEmpty(referenz.Anzeige)
                 ? referenz.Anzeige : BerichtTexte.T("Stamm");
        }

        // ------------------------------------------------- Mehrjahresübersicht (E7)

        /// <summary>
        /// ETAPPE E7 — je Projekt eine Tabelle mit den Jahren 0…T als Zeilen und den
        /// Positionen des Zahlungsstroms als Spalten. Inhaltlich dieselbe Tabelle wie im
        /// Word-Bericht; beide bauen auf <see cref="Mehrjahresbild"/> auf.
        /// </summary>
        private static int BlattMehrjahres(IXLWorksheet ws, BerichtsDaten daten,
                                           WirtschaftlichkeitVerlauf verlauf,
                                           List<WirtschaftlichkeitErgebnis> alle, int r,
                                           WirtschaftlichkeitParameter ps, string szenario, int jahreTabelle,
                                           Formelregister formeln, List<MehrjahresTafel> tafeln)
        {
            if (verlauf == null || verlauf.Absolut.All(s => s.Bild == null)) return r;
            bool erwartet = string.Equals(szenario, WirtschaftlichkeitSzenario.ERWARTET, StringComparison.Ordinal);

            if (erwartet)
            {
                ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MJ_TITEL;
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Range(r, 1, r, 14).Style.Fill.BackgroundColor = GRUPPE;
                r++;
                ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MJ_HINWEIS;
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                r += 2;
            }
            else
            {
                // ETAPPE E14 (E14‑Q1 a): dieselbe Tabelle je Stand für Günstig und Ungünstig —
                // Eingangswerte aus dem Lauf des Szenarios, Formeln auf seine Spalte des
                // Parameterblocks, Zeilen bis zum längsten Zeitraum der drei Szenarien.
                string name = VerlaufZeilen.Szenarioname(szenario);
                ws.Cell(r, 1).Value = string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_FM_MJ_SZENARIO_TITEL,
                                                    name, ps.Betrachtungszeitraum);
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Range(r, 1, r, 14).Style.Fill.BackgroundColor = GRUPPE;
                r++;
                ws.Cell(r, 1).Value = string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_FM_MJ_SZENARIO_HINWEIS,
                                                    name, ExcelFormelmappe.Anhang(szenario), ps.Betrachtungszeitraum);
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                r += 2;
            }

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
                int kopfZeile = r;   // ETAPPE E8b: Anker der Formeln (Jahr 0 steht darunter)
                ws.Cell(r, 1).Value = MyResource.Resource.WIRT_MJ_JAHR;
                for (int i = 0; i < spalten; i++) ws.Cell(r, 2 + i).Value = bild.Spalten[i].Titel;
                ws.Range(r, 1, r, 1 + spalten).Style.Font.Bold = true;
                ws.Range(r, 1, r, 1 + spalten).Style.Fill.BackgroundColor = KOPF;
                r++;

                // ETAPPE E14 (E14‑Q1 a): Jahreszeilen bis zum längsten Zeitraum der drei
                // Szenarien; jenseits von T_s bleiben die Positionen leer.
                int zeilenJahre = Math.Max(bild.Jahre, jahreTabelle);
                for (int jahr = 0; jahr <= zeilenJahre; jahr++)
                {
                    ws.Cell(r, 1).Value = jahr;
                    if (jahr > bild.Jahre) { r++; continue; }
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

                // ETAPPE E8b, Stufe 1 (Konzept § 2.11.6): dieselbe Tabelle in Formeln —
                // Energie als Fortschreibung, Betrieb als zwei Terme über Hilfsspalten,
                // Netto als Zeilensumme, Barwert, Laufsumme, Abschluss. Die Zellen tragen
                // danach die Formel UND (über das Register) die Zahl, die hier stand.
                MehrjahresTafel tafel = ExcelFormelmappe.Mehrjahrestabelle(ws, kopfZeile, v.IdProjekt,
                                                                           bild, serie, ps, szenario,
                                                                           zeilenJahre, formeln);
                if (tafel != null) tafeln.Add(tafel);

                ws.Cell(r, 1).Value = string.Format(MyResource.Resource.WIRT_MJ_PROBE,
                    bild.KumuliertT.ToString("N0", BerichtTexte.Kultur),
                    bild.RestwertBarwert.ToString("N0", BerichtTexte.Kultur),
                    bild.Kapitalwert.ToString("N0", BerichtTexte.Kultur));
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                r++;

                // Nachweisblock: die vermiedenen Kosten stehen AUSSERHALB des
                // Zahlungsstroms — sie stecken schon in anderen Positionen, eine eigene
                // Zeile wäre eine Doppelzählung.
                WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(x =>
                    x.IdProjekt == v.IdProjekt && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                // ETAPPE E14: der Nachweisblock steht einmal, beim Erwartungsfall.
                bool vermieden = erwartet && e != null &&
                                 (e.VermiedenGesamtJahr != 0 || e.VermiedenArbeitJahr != 0);
                if (vermieden)
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
                }
                r++;
            }
            return r;
        }

        // ------------------------------------- Nicht monetarisierbare Wirkungen (E17)

        /// <summary>
        /// ETAPPE E17 (V‑G11) — die Tafel „Nicht monetarisierbare Wirkungen": Titelzeile
        /// (<c>WIRT_NM_TITEL</c>, fett), Hinweis, Kopf (Kategorie, Beschreibung, Dauer,
        /// Organisation, Mitarbeiter, Umwelt, Beurteilung), je Wirkung eine Zeile; die
        /// Beurteilung als Zahl (leer = nicht beurteilt). Ohne benannte Wirkung entfällt die
        /// Tafel. Rückgabe: die nächste freie Zeile (eine Leerzeile Abstand).
        /// </summary>
        internal static int NichtMonetaereWirkungenTafel(IXLWorksheet ws, int r, IReadOnlyList<ProjektWirkung> wirkungen)
        {
            List<ProjektWirkung> zeilen = (wirkungen ?? new List<ProjektWirkung>())
                .Where(w => w != null && !string.IsNullOrWhiteSpace(w.Beschreibung)).ToList();
            if (zeilen.Count == 0) return r;

            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_NM_TITEL;
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Range(r, 1, r, 7).Style.Fill.BackgroundColor = GRUPPE;
            r++;
            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_NM_TABELLE_HINWEIS;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r++;

            string[] kopf =
            {
                MyResource.Resource.WIRT_NM_SP_KATEGORIE, MyResource.Resource.WIRT_NM_SP_BESCHREIBUNG,
                MyResource.Resource.WIRT_NM_SP_DAUER, MyResource.Resource.WIRT_NM_SP_ORGANISATION,
                MyResource.Resource.WIRT_NM_SP_MITARBEITER, MyResource.Resource.WIRT_NM_SP_UMWELT,
                MyResource.Resource.WIRT_NM_SP_BEURTEILUNG
            };
            for (int c = 0; c < kopf.Length; c++)
            {
                ws.Cell(r, c + 1).Value = kopf[c];
                ws.Cell(r, c + 1).Style.Font.Bold = true;
                ws.Cell(r, c + 1).Style.Fill.BackgroundColor = KOPF;
            }
            r++;

            foreach (ProjektWirkung z in zeilen)
            {
                ws.Cell(r, 1).Value = NichtMonetaereWirkungen.KategorieText(z.Kategorie);
                ws.Cell(r, 2).Value = z.Beschreibung.Trim();
                ws.Cell(r, 2).Style.Alignment.WrapText = true;
                ws.Cell(r, 3).Value = NichtMonetaereWirkungen.DauerText(z.Dauer);
                ws.Cell(r, 4).Value = NichtMonetaereWirkungen.WirkungText(z.WirkungOrganisation);
                ws.Cell(r, 5).Value = NichtMonetaereWirkungen.WirkungText(z.WirkungMitarbeiter);
                ws.Cell(r, 6).Value = NichtMonetaereWirkungen.WirkungText(z.WirkungUmwelt);
                int? b = z.Beurteilung;
                if (b.HasValue) ws.Cell(r, 7).Value = b.Value;
                else ws.Cell(r, 7).Value = MyResource.Resource.WIRT_NM_NICHT_BEURTEILT;
                r++;
            }
            return r + 1;
        }

        // ------------------------------------------------- Bandbreite (E2, G8)

        /// <summary>
        /// ETAPPE E2 (VALERI-Lücke G8) — die <b>Bandbreitentafel</b> des Excel-Blatts:
        /// je Variante ΔKW in Ungünstig / Erwartet / Günstig (E5‑Q2), die <b>Spanne</b>
        /// (größter minus kleinster der drei Werte, E5‑Q4),
        /// die Amortisation und die Einstufung, darüber die <b>Referenzzeile</b>.
        ///
        /// <para>Dieselbe Tafel führt der Wortbericht
        /// (<c>BausteineWirtschaftlichkeit.SchreibeSzenarien</c>) — Spalten, Reihenfolge
        /// und Fußzeile stammen aus denselben Ressourcen. Gerechnet wird nichts: Die
        /// Δ-Werte stehen in den Ergebnissen, die Spanne ist ihre Differenz.</para>
        ///
        /// <para>Die Wertspalten bleiben NUMERISCH (Divergenz D5) — die Referenzzeile
        /// trägt ihren Text deshalb nur in der Beschriftungsspalte, ihre Δ-Zellen
        /// bleiben leer statt „(Referenz)" zu tragen.</para>
        ///
        /// <para><b>ETAPPE E5 Teil b:</b> Die Tafel liest die Bandbreite der Bewertung
        /// (<see cref="WirtschaftlichkeitBewertung.Bandbreite"/>) statt sie selbst zu
        /// bilden — dieselben Zeilen, Spannen und Einstufungen wie Seite und Wortbericht.
        /// Unter dem Fußtext steht der Hinweistext der Szenarien (U10).</para>
        /// </summary>
        private static int BandbreitenTafel(IXLWorksheet ws, int r, WirtschaftlichkeitBewertung bewertung,
                                            Dictionary<string, KennzahlBlock> lagen, Formelregister formeln)
        {
            WirtschaftlichkeitBandbreite band = bewertung.Bandbreite ?? new WirtschaftlichkeitBandbreite();
            if (band.Leer) return r;

            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_SZ_BANDBREITE_TITEL;
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Range(r, 1, r, 7).Style.Fill.BackgroundColor = GRUPPE;
            r++;

            string[] kopf =
            {
                MyResource.Resource.WIRT_SZ_SP_VARIANTE,
                MyResource.Resource.WIRT_SZ_SP_WORST,
                MyResource.Resource.WIRT_SZ_SP_ERWARTET,
                MyResource.Resource.WIRT_SZ_SP_BEST,
                MyResource.Resource.WIRT_SZ_SP_SPANNE,
                MyResource.Resource.WIRT_SZ_SP_AMORT,
                MyResource.Resource.WIRT_EMPF_SPALTE,
            };
            for (int i = 0; i < kopf.Length; i++)
            {
                ws.Cell(r, i + 1).Value = kopf[i];
                ws.Cell(r, i + 1).Style.Font.Bold = true;
            }
            r++;

            // Die Referenzzeile — der Stand, gegen den jede Δ-Zahl gerechnet ist.
            ws.Cell(r, 1).Value = band.Referenzname;
            ws.Range(r, 1, r, 7).Style.Fill.BackgroundColor = STAMM;
            r++;

            foreach (BandbreitenZeile z in band.Zeilen)
            {
                ws.Cell(r, 1).Value = z.IstStamm ? BerichtTexte.T("Stamm") : z.Anzeige;

                Betrag(ws, r, 2, z.Worst);
                Betrag(ws, r, 3, z.Erwartet);
                Betrag(ws, r, 4, z.Best);
                Betrag(ws, r, 5, z.Spanne);

                // ETAPPE E14 (Stufe 2): ΔKW je Szenario als Zellbezug auf die Kennzahltafel des
                // Szenarios, die Spanne als MAX − MIN der drei — gegengerechnet wie jede Formel.
                ExcelFormelmappe.Bandbreitenzeile(ws, r, z, lagen, formeln);

                if (z.AmortisationJahre.HasValue)
                {
                    ws.Cell(r, 6).Value = z.AmortisationJahre.Value;
                    ws.Cell(r, 6).Style.NumberFormat.Format = "#,##0.0";
                }

                // ETAPPE E5 (U5): derselbe Stufentext wie auf der Empfehlungskarte.
                if (z.Urteil != null) ws.Cell(r, 7).Value = z.Urteil.StufeText;
                r++;
            }

            ws.Cell(r, 1).Value = string.Format(BerichtTexte.Kultur,
                                                MyResource.Resource.WIRT_SZ_DELTA_FUSS,
                                                band.Referenzname);
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r++;

            // ETAPPE E9b (U10, E9b‑Q3): der Ausweis „n von m Parametern szenariert" an der
            // Stelle des Hinweistexts (Konzept § 2.11.7) — derselbe Satz wie unter der
            // Annahmentafel der Seite und im Wortbericht.
            if (!string.IsNullOrEmpty(bewertung.Szenarioabdeckung))
            {
                ws.Cell(r, 1).Value = bewertung.Szenarioabdeckung;
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
                r++;
            }
            r++;
            return r;
        }

        /// <summary>Eine €-Zelle der Bandbreitentafel; ohne Wert bleibt sie leer (D5).</summary>
        private static void Betrag(IXLWorksheet ws, int zeile, int spalte, double? wert)
        {
            if (!wert.HasValue) return;
            ws.Cell(zeile, spalte).Value = wert.Value;
            ws.Cell(zeile, spalte).Style.NumberFormat.Format = "#,##0";
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

                // ETAPPE E7c (E7c1-Q7): dieselben fünf Fall-2-Spalten wie die Word-Tafel,
                // und wie dort nur, wenn ein Modul dieser Variante Fall 2 rechnet.
                bool mitFall2 = KwkgFall2Spalten.Noetig(e.KwkgModule);
                string[] kopfV = mitFall2 ? kopf.Concat(KwkgFall2Spalten.Kopf()).ToArray() : kopf;
                for (int i = 0; i < kopfV.Length; i++) ws.Cell(r, 1 + i).Value = kopfV[i];
                ws.Range(r, 1, r, kopfV.Length).Style.Font.Bold = true;
                ws.Range(r, 1, r, kopfV.Length).Style.Fill.BackgroundColor = KOPF;
                r++;

                foreach (KwkgModulNachweis m in e.KwkgModule)
                {
                    ws.Cell(r, 1).Value = m.Bezeichner;
                    Zahl(ws, r, 2, m.PelKW, "#,##0");
                    Zahl(ws, r, 3, m.VbhElektrisch, "#,##0");
                    // AUFTRAG #351 (U26): vier Nachkommastellen wie im Satzfeld und
                    // im Word-Bericht — das Zellformat kommt aus dem Kern.
                    Zahl(ws, r, 4, m.SatzEigenCt, KwkgSatzHerkunft.EXCELFORMAT);
                    Zahl(ws, r, 5, m.SatzEinspeisungCt, KwkgSatzHerkunft.EXCELFORMAT);
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
                    if (mitFall2)
                    {
                        // Die Wertspalten bleiben numerisch; eine fehlende Größe bleibt leer.
                        ws.Cell(r, 12).Value = KwkgFall2Spalten.Fall(m);
                        ZahlOderLeer(ws, r, 13, KwkgFall2Spalten.Stromkennzahl(m),
                                     KwkgFall2Spalten.EXCELFORMAT_SIGMA);
                        ZahlOderLeer(ws, r, 14, KwkgFall2Spalten.NutzwaermeMWh(m),
                                     KwkgFall2Spalten.EXCELFORMAT_MWH);
                        ZahlOderLeer(ws, r, 15, KwkgFall2Spalten.KwkStromMWh(m),
                                     KwkgFall2Spalten.EXCELFORMAT_MWH);
                        ZahlOderLeer(ws, r, 16, KwkgFall2Spalten.KuerzungMWh(m),
                                     KwkgFall2Spalten.EXCELFORMAT_MWH);
                    }
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
                                               List<WirtschaftlichkeitErgebnis> alle, int r,
                                               Formelregister formeln)
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

                int kopfZeile = r;   // ETAPPE E8b: Köpfe „Menge" und „Satz" folgen, wenn es bemessene Zeilen gibt
                ws.Cell(r, 1).Value = MyResource.Resource.WIRT_BK_SP_POSITION;
                ws.Cell(r, 2).Value = MyResource.Resource.WIRT_BK_SP_GRUPPE;
                ws.Cell(r, 3).Value = MyResource.Resource.WIRT_BK_SP_BEMESSUNG;
                ws.Cell(r, 4).Value = MyResource.Resource.WIRT_BK_SP_HERLEITUNG;
                ws.Cell(r, 5).Value = MyResource.Resource.WIRT_BK_SP_BETRAG;
                ws.Range(r, 1, r, 5).Style.Font.Bold = true;
                ws.Range(r, 1, r, 5).Style.Fill.BackgroundColor = KOPF;
                r++;

                double summe = 0;
                // ETAPPE E8c (E8b‑Q3, Lesart b): die Probe unten vergleicht nur die Positionen,
                // die im ersten Jahr zahlen — eine Position mit späterem Startjahr steht in der
                // Summe, aber nicht in den angesetzten Betriebskosten p. a.
                double summeErstesJahr = 0;
                bool bemessen = false;
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
                        // ETAPPE E8c (E8b‑Q3): Herleitung oder Szenariokennzeichen, bei einem
                        // späteren Startjahr mit „ab Jahr X".
                        string herleitung = WirtschaftlichkeitZeilen.HerleitungZeile(n, BerichtTexte.Kultur);

                        ws.Cell(r, 1).Value = n.Bezeichnung;
                        ws.Cell(r, 2).Value = n.Gruppe;
                        // ETAPPE E8c (E8b‑Q2): jede Bemessungsart mit ihrem Namen im Gewerk.
                        ws.Cell(r, 3).Value = WirtschaftlichkeitZeilen.BemessungText(n.Bemessung, n.Komponente);
                        ws.Cell(r, 4).Value = herleitung;
                        Zahl(ws, r, 5, n.BetragJahr, "#,##0");
                        // ETAPPE E8b, Stufe 3 (Konzept § 2.11.6): eine bemessene Position
                        // trägt Menge und Satz in eigenen Spalten, der Betrag ist ihr Produkt.
                        if (ExcelFormelmappe.Betriebskostenzeile(ws, r, n, formeln)) bemessen = true;
                        r++;
                        summe += n.BetragJahr;
                        if (WirtschaftlichkeitZeilen.LaeuftImErstenJahr(n)) summeErstesJahr += n.BetragJahr;
                    }
                }
                if (bemessen) ExcelFormelmappe.BetriebskostenKopf(ws, kopfZeile);

                ws.Cell(r, 1).Value = MyResource.Resource.WIRT_BK_SUMME;
                Zahl(ws, r, 5, summe, "#,##0");
                ws.Range(r, 1, r, 5).Style.Font.Bold = true;
                ws.Range(r, 1, r, 5).Style.Fill.BackgroundColor = KOPF;
                // ETAPPE E8b, Stufe 3: die Summe als Spaltensumme der Beträge darüber.
                ExcelFormelmappe.BetriebskostenSumme(ws, r, kopfZeile + 1, r - 1, summe, summe, formeln);
                r++;

                // Probe gegen die Zahl, mit der die Kapitalwertrechnung gerechnet hat — die
                // Positionen des ersten Jahres gegen die Betriebskosten p. a. (E8c).
                string abweichung = WirtschaftlichkeitZeilen.GliederungAbweichung(
                    summeErstesJahr, e.BetriebskostenJahr, BerichtTexte.Kultur);
                if (abweichung.Length > 0)
                {
                    ws.Cell(r, 1).Value = abweichung;
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

        /// <summary>Wie <see cref="Zahl"/>; ohne Wert bleibt die Zelle leer (E7c-Hilfe).</summary>
        private static void ZahlOderLeer(IXLWorksheet ws, int zeile, int spalte, double? wert,
                                         string format)
        {
            if (wert.HasValue) Zahl(ws, zeile, spalte, wert.Value, format);
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
            };
            if (z.Hat(ZeitreihenSatz.NETZEINSPEISUNG))
            {
                // Flottenbericht: Komponenten und kontrollierende Gesamtsumme stehen
                // getrennt nebeneinander. Keine davon fließt hier erneut in Kosten ein.
                spalten.Add(new KeyValuePair<string, string>(ZeitreihenSatz.PV_UEBERSCHUSS, "PV-Einspeisung"));
                spalten.Add(new KeyValuePair<string, string>(ZeitreihenSatz.BHKW_UEBERSCHUSS, "BHKW-Einspeisung"));
                spalten.Add(new KeyValuePair<string, string>(ZeitreihenSatz.BATTERIE_EINSPEISUNG, "Batterie-Einspeisung"));
                spalten.Add(new KeyValuePair<string, string>(ZeitreihenSatz.NETZEINSPEISUNG, "Netzeinspeisung gesamt"));
                spalten.Add(new KeyValuePair<string, string>(ZeitreihenSatz.PV_ABREGELUNG, "PV-Abregelung"));
            }
            else
            {
                // Bestandspfad und sein bisheriger Bericht bleiben unverändert.
                spalten.Add(new KeyValuePair<string, string>(ZeitreihenSatz.PV_UEBERSCHUSS, "Einspeisung"));
            }
            spalten.Add(new KeyValuePair<string, string>(ZeitreihenSatz.NETZBEZUG, "Netzbezug"));
            spalten = spalten.Where(s => z.Hat(s.Key)).ToList();
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
        internal static string BlattName(XLWorkbook wb, VariantenDaten v)
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
