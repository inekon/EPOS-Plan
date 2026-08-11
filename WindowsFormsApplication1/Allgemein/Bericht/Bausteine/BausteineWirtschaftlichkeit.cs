using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Baustein 7: Wirtschaftlichkeit (Konzept_Wirtschaftlichkeit.md Kap. 5/6;
    /// Phase 6 = Ausbaustufe W1, Kapitalwertmethode nach DIN EN 17463).
    ///
    /// Der Baustein RECHNET NICHT selbst: er liest die im Reiter
    /// „Wirtschaftlichkeit" persistierten Ergebnisse (Tab_ErgebnisWirtschaftlichkeit,
    /// IWirtschaftlichkeitProvider) — Reiter, Word und Excel zeigen damit garantiert
    /// identische Zahlen. Liegen keine (oder veraltete) Ergebnisse vor, erscheint
    /// ein Hinweis mit dem Weg zum Reiter.
    /// </summary>
    public class WirtschaftlichkeitBaustein : IBerichtsBaustein
    {
        public string Schluessel { get { return BerichtsKonfiguration.B_WIRTSCHAFT; } }
        public string Titel { get { return "Wirtschaftlichkeit"; } }

        /// <summary>Kennzahl-Zeilen der Vergleichstabelle (Reihenfolge wie im Reiter).</summary>
        private class Zeile
        {
            public string Label;
            public Func<WirtschaftlichkeitErgebnis, WordKontext, string> Wert;
            public Zeile(string label, Func<WirtschaftlichkeitErgebnis, WordKontext, string> wert)
            { Label = label; Wert = wert; }
        }

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            k.Ueberschrift1("Wirtschaftlichkeit");

            var provider = new WirtschaftlichkeitCtrl();
            List<int> ids = daten.Varianten.Select(v => v.IdProjekt).ToList();
            List<WirtschaftlichkeitErgebnis> alle = provider.LadeErgebnisse(ids);

            if (alle.Count == 0)
            {
                k.Hinweis("Für diese Vergleichsgruppe wurde noch keine Wirtschaftlichkeit berechnet. " +
                          "Berechnung im Bereich Berichte & Kosten → Wirtschaftlichkeit ausführen, " +
                          "anschließend den Bericht erneut erstellen.");
                return;
            }

            // ---------------- Methodik + Parameternachweis (Normanforderung) ----------------
            WirtschaftlichkeitParameter p = provider.LadeParameter(daten.IdStamm);
            k.Text("Bewertung nach der Kapitalwertmethode in Anlehnung an DIN EN 17463 (ValERI): " +
                   "alle Zahlungsströme der Projekte werden über den Betrachtungszeitraum auf den " +
                   "Entscheidungszeitpunkt abgezinst. Referenz (Unterlassensalternative) ist das " +
                   "Stammprojekt — der Kapitalwert einer Variante ist der Barwert der Differenz-" +
                   "Zahlungsströme Variante − Stamm; ein positiver Wert bedeutet: die Variante ist " +
                   "über den Betrachtungszeitraum wirtschaftlicher als der Stamm.");
            k.Hinweis("Parameter dieses Rechenlaufs: " + p.Nachweis(k.Kultur) +
                      " · Restwert linear · Ersatzbeschaffungen nominal konstant (Stufe W1). " +
                      "Energie-/Strompreise aus der Kostenmaske des jeweiligen Projekts; " +
                      "Investitions- und Betriebskosten aus den Kostenpositionen (Tab_ProjektWerte). " +
                      "Rechenstand: " + alle[0].Zeitstempel.ToString("dd.MM.yyyy HH:mm", k.Kultur) + ".");

            // Aktualität gegen den Simulationsstand prüfen.
            var veraltet = new List<string>();
            foreach (VariantenDaten v in daten.Varianten)
            {
                WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(x =>
                    x.IdProjekt == v.IdProjekt && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                if (e == null || (e.Fehlgrund == null && !provider.ErgebnisAktuell(e)))
                    veraltet.Add(v.IstStamm ? "Stamm" : v.Anzeige);
            }
            if (veraltet.Count > 0)
                k.Hinweis("⚠ Für " + string.Join(", ", veraltet) + " passt das gespeicherte " +
                          "Wirtschaftlichkeits-Ergebnis nicht (mehr) zum aktuellen Simulationslauf — " +
                          "im Reiter Wirtschaftlichkeit neu berechnen.");

            // ---------------- Vergleichstabelle (Szenario Erwartet) ----------------
            k.Ueberschrift2("Kennzahlen im Szenario „Erwartet“");
            SchreibeVergleich(k, daten, alle, WirtschaftlichkeitSzenario.ERWARTET);

            // ---------------- Szenarienübersicht (Worst / Erwartet / Best) ----------------
            k.Ueberschrift2("Szenarien Worst / Erwartet / Best");
            k.Hinweis("Szenariowerte aus den Best-/Worst-Case-Feldern der Kostenpositionen " +
                      "(Betrag und Nutzungsdauer); nicht gepflegte Felder übernehmen den Erwartungswert.");
            SchreibeSzenarien(k, daten, alle);

            // Unvollständige Rechnungen ausweisen (keine stillen Lücken).
            foreach (WirtschaftlichkeitErgebnis e in alle.Where(x =>
                         x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.Fehlgrund != null))
            {
                VariantenDaten v = daten.Varianten.FirstOrDefault(x => x.IdProjekt == e.IdProjekt);
                k.Hinweis("⚠ " + (v == null ? ("Projekt " + e.IdProjekt) : (v.IstStamm ? "Stamm" : v.Anzeige)) +
                          ": " + e.Fehlgrund);
            }
        }

        // ------------------------------------------------------------- Tabellen

        private static void SchreibeVergleich(WordKontext k, BerichtsDaten daten,
                                              List<WirtschaftlichkeitErgebnis> alle, string szenario)
        {
            var zeilen = new List<Zeile>
            {
                new Zeile("Investition I₀ [€]",            (e, kk) => kk.FW(e.Investition, "N0")),
                new Zeile("Betriebskosten [€/a]",          (e, kk) => kk.FW(e.BetriebskostenJahr, "N0")),
                new Zeile("Energiekosten [€/a]",           (e, kk) => kk.FW(e.EnergiekostenJahr, "N0")),
                new Zeile("Einspeiseerlös [€/a]",          (e, kk) => kk.FW(e.EinspeiseerloesJahr, "N0")),
                new Zeile("Restwert (Barwert) [€]",        (e, kk) => kk.FW(e.RestwertBarwert, "N0")),
                new Zeile("Nettobarwert über T [€]",       (e, kk) => kk.FW(e.Kapitalwert, "N0")),
                new Zeile("Kapitalwert vs. Stamm [€]",     (e, kk) => e.IstStamm ? "—" : kk.FW(e.KapitalwertDiff, "N0")),
                new Zeile("Annuität des KW [€/a]",         (e, kk) => e.IstStamm ? "—" : kk.FW(e.AnnuitaetKW, "N0")),
                new Zeile("Amortisation [a]",              (e, kk) => e.IstStamm ? "—" : kk.FW(e.AmortisationJahre, "N1")),
                new Zeile("Wärmegestehungskosten [€/kWh]", (e, kk) => kk.FW(e.Gestehungskosten, "N3"))
            };

            VariantenDaten stamm = daten.Varianten.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return;

            foreach (List<VariantenDaten> block in k.VariantenBloecke(daten))
            {
                var spalten = new List<VariantenDaten> { stamm };
                spalten.AddRange(block);

                int wLabel = 3100;
                int wCol = (WordBerichtGenerator.INHALT_B - wLabel) / spalten.Count;
                var w = new List<int> { wLabel };
                for (int i = 0; i < spalten.Count; i++) w.Add(wCol);

                Table t = k.NeueTabelle(w.ToArray());
                var kopf = new TableRow();
                kopf.Append(k.Zelle("Kennzahl", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
                for (int i = 0; i < spalten.Count; i++)
                    kopf.Append(k.Zelle(spalten[i].IstStamm ? "Stamm" : spalten[i].Anzeige,
                        w[i + 1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                t.Append(kopf);

                foreach (Zeile z in zeilen)
                {
                    var tr = new TableRow();
                    tr.Append(k.Zelle(BerichtTexte.T(z.Label), w[0], false, null, JustificationValues.Left));
                    for (int i = 0; i < spalten.Count; i++)
                    {
                        WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(x =>
                            x.IdProjekt == spalten[i].IdProjekt && x.Szenario == szenario);
                        string txt = e == null ? "—" : z.Wert(e, k);
                        tr.Append(k.Zelle(txt, w[i + 1], false,
                            spalten[i].IstStamm ? WordBerichtGenerator.STAMM_FILL : null,
                            txt == "—" ? JustificationValues.Center : JustificationValues.Right));
                    }
                    t.Append(tr);
                }
                k.Fuege(t);
                k.Beschriftung(" ");
            }
        }

        /// <summary>Szenarienübersicht: je Variante der Kapitalwert vs. Stamm in W/E/B.</summary>
        private static void SchreibeSzenarien(WordKontext k, BerichtsDaten daten,
                                              List<WirtschaftlichkeitErgebnis> alle)
        {
            List<VariantenDaten> varianten = daten.Varianten.Where(v => !v.IstStamm).ToList();
            if (varianten.Count == 0)
            {
                k.Hinweis("Keine Varianten ausgewählt — die Szenarienübersicht entfällt.");
                return;
            }

            int wLabel = 3100;
            int wCol = (WordBerichtGenerator.INHALT_B - wLabel) / 4;
            int[] w = { wLabel, wCol, wCol, wCol, wCol };

            Table t = k.NeueTabelle(w);
            var kopf = new TableRow();
            kopf.Append(k.Zelle("Variante", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
            kopf.Append(k.Zelle("KW Worst [€]", w[1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            kopf.Append(k.Zelle("KW Erwartet [€]", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            kopf.Append(k.Zelle("KW Best [€]", w[3], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            kopf.Append(k.Zelle("Amortisation [a]", w[4], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
            t.Append(kopf);

            foreach (VariantenDaten v in varianten)
            {
                var tr = new TableRow();
                tr.Append(k.Zelle(v.Anzeige, w[0], false, null, JustificationValues.Left));
                int spalte = 1;
                foreach (string sz in new[] { WirtschaftlichkeitSzenario.WORST,
                                              WirtschaftlichkeitSzenario.ERWARTET,
                                              WirtschaftlichkeitSzenario.BEST })
                {
                    WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(x =>
                        x.IdProjekt == v.IdProjekt && x.Szenario == sz);
                    string txt = e == null ? "—" : k.FW(e.KapitalwertDiff, "N0");
                    tr.Append(k.Zelle(txt, w[spalte], false, null,
                        txt == "—" ? JustificationValues.Center : JustificationValues.Right));
                    spalte++;
                }
                WirtschaftlichkeitErgebnis erw = alle.FirstOrDefault(x =>
                    x.IdProjekt == v.IdProjekt && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                string am = erw == null ? "—" : k.FW(erw.AmortisationJahre, "N1");
                tr.Append(k.Zelle(am, w[4], false, null,
                    am == "—" ? JustificationValues.Center : JustificationValues.Right));
                t.Append(tr);
            }
            k.Fuege(t);
            k.Beschriftung(" ");
        }
    }
}
