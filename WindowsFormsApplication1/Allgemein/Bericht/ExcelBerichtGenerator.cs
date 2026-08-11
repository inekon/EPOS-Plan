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

        public string Erzeuge(BerichtsDaten daten, BerichtsKonfiguration konfig, string zielDatei)
        {
            if (daten == null || daten.Varianten.Count == 0)
                throw new ArgumentException("Keine Berichtsdaten vorhanden.");

            using (var wb = new XLWorkbook())
            {
                BlattUebersicht(wb, daten);
                BlattVergleich(wb, daten);

                // Phase 6: persistierte Kapitalwert-Ergebnisse (gleiche Quelle wie
                // Reiter und Word-Baustein — Tab_ErgebnisWirtschaftlichkeit).
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
            List<Kennzahl> katalog = KennzahlenKatalog.Alle();
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
        /// (Zeilenblöcke Worst/Erwartet/Best; Spalten = Stamm + Varianten) aus den
        /// persistierten Ergebnissen. Echte Zahlenwerte, fehlende Werte bleiben leer.
        /// </summary>
        private static void BlattWirtschaftlichkeit(XLWorkbook wb, BerichtsDaten daten)
        {
            var provider = new WirtschaftlichkeitCtrl();
            List<int> ids = daten.Varianten.Select(v => v.IdProjekt).ToList();
            List<WirtschaftlichkeitErgebnis> alle = provider.LadeErgebnisse(ids);

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
                    "Noch keine Wirtschaftlichkeitsberechnung gespeichert — im Bereich " +
                    "Berichte & Kosten → Wirtschaftlichkeit berechnen.");
                ws.Columns().AdjustToContents();
                return;
            }

            WirtschaftlichkeitParameter p = provider.LadeParameter(daten.IdStamm);
            ws.Cell(r, 1).Value = p.Nachweis(BerichtTexte.Kultur) +
                " · " + BerichtTexte.T("Referenz: Stammprojekt · Restwert linear") +
                " · " + BerichtTexte.T("Rechenstand") + ": " +
                alle[0].Zeitstempel.ToString("dd.MM.yyyy HH:mm", BerichtTexte.Kultur);
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r += 2;

            // Kennzahl-Zeilen: Label, Format, Wertzugriff (null = leer).
            var zeilen = new List<Tuple<string, string, Func<WirtschaftlichkeitErgebnis, double?>>>
            {
                Tuple.Create("Investition I₀ [€]", "#,##0", (Func<WirtschaftlichkeitErgebnis, double?>)(e => (double?)e.Investition)),
                Tuple.Create("Betriebskosten [€/a]", "#,##0", (Func<WirtschaftlichkeitErgebnis, double?>)(e => e.BetriebskostenJahr)),
                Tuple.Create("Energiekosten [€/a]", "#,##0", (Func<WirtschaftlichkeitErgebnis, double?>)(e => e.EnergiekostenJahr)),
                Tuple.Create("Einspeiseerlös [€/a]", "#,##0", (Func<WirtschaftlichkeitErgebnis, double?>)(e => (double?)e.EinspeiseerloesJahr)),
                Tuple.Create("Restwert (Barwert) [€]", "#,##0", (Func<WirtschaftlichkeitErgebnis, double?>)(e => (double?)e.RestwertBarwert)),
                Tuple.Create("Nettobarwert über T [€]", "#,##0", (Func<WirtschaftlichkeitErgebnis, double?>)(e => e.Kapitalwert)),
                Tuple.Create("Kapitalwert vs. Stamm [€]", "#,##0", (Func<WirtschaftlichkeitErgebnis, double?>)(e => e.IstStamm ? null : e.KapitalwertDiff)),
                Tuple.Create("Annuität des KW [€/a]", "#,##0", (Func<WirtschaftlichkeitErgebnis, double?>)(e => e.IstStamm ? null : e.AnnuitaetKW)),
                Tuple.Create("Amortisation [a]", "#,##0.0", (Func<WirtschaftlichkeitErgebnis, double?>)(e => e.IstStamm ? null : e.AmortisationJahre)),
                Tuple.Create("Wärmegestehungskosten [€/kWh]", "#,##0.000", (Func<WirtschaftlichkeitErgebnis, double?>)(e => e.Gestehungskosten))
            };

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

                foreach (var z in zeilen)
                {
                    ws.Cell(r, 1).Value = BerichtTexte.T(z.Item1);
                    c = 2;
                    foreach (VariantenDaten v in daten.Varianten)
                    {
                        WirtschaftlichkeitErgebnis e = block.FirstOrDefault(x => x.IdProjekt == v.IdProjekt);
                        double? wert = e == null ? (double?)null : z.Item3(e);
                        if (wert.HasValue)
                        {
                            ws.Cell(r, c).Value = wert.Value;
                            ws.Cell(r, c).Style.NumberFormat.Format = z.Item2;
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

            ws.Column(1).Width = 32;
            for (int i = 2; i <= 1 + daten.Varianten.Count; i++) ws.Column(i).Width = 16;
            ws.SheetView.FreezeRows(2);
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
            foreach (Kennzahl kz in KennzahlenKatalog.Alle())
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
