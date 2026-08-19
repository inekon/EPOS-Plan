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
    /// Der Baustein RECHNET NICHT selbst: er zeigt die Ergebnisse, die der
    /// Berichtslauf zuvor gerechnet hat (BerichtsDatenSammler.SammleFuerBericht,
    /// Schritt b — frische Simulation, dann WirtschaftlichkeitCtrl.Berechne). Sie
    /// liegen am Berichtsbaum (BerichtsDaten.Wirtschaftlichkeit) und sind zugleich
    /// nach Tab_ErgebnisWirtschaftlichkeit persistiert; Reiter, Word und Excel zeigen
    /// damit dieselben Zahlen. Nur wenn die Rechnung dieses Laufs ausblieb (Fehler),
    /// wird auf den persistierten Stand zurückgefallen und das ausgewiesen.
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

            // Quelle sind die Zahlen DIESES Berichtslaufs; der persistierte Stand ist
            // nur das Rückfallnetz, falls die Rechnung des Laufs scheiterte.
            bool ausDiesemLauf = daten.Wirtschaftlichkeit.Count > 0;
            List<WirtschaftlichkeitErgebnis> alle = ausDiesemLauf
                ? daten.Wirtschaftlichkeit
                : provider.LadeErgebnisse(ids);

            if (alle.Count == 0)
            {
                k.Hinweis("Für diese Vergleichsgruppe konnte keine Wirtschaftlichkeit berechnet " +
                          "werden" +
                          (daten.WirtschaftlichkeitFehler != null
                           ? " (" + daten.WirtschaftlichkeitFehler + ")" : "") +
                          ". Kostenpositionen (Tab_ProjektWerte) und die Parameter im Bereich " +
                          "Berichte & Kosten → Wirtschaftlichkeit prüfen.");
                return;
            }
            if (!ausDiesemLauf)
                k.Hinweis("⚠ Die Wirtschaftlichkeitsrechnung dieses Berichtslaufs ist " +
                          "fehlgeschlagen" +
                          (daten.WirtschaftlichkeitFehler != null
                           ? " (" + daten.WirtschaftlichkeitFehler + ")" : "") +
                          " — gezeigt wird der zuletzt gespeicherte Stand.");

            // ---------------- Methodik + Parameternachweis (Normanforderung) ----------------
            WirtschaftlichkeitParameter p = provider.LadeParameter(daten.IdStamm);
            k.Text("Bewertung nach der Kapitalwertmethode in Anlehnung an DIN EN 17463 (ValERI): " +
                   "alle Zahlungsströme der Projekte werden über den Betrachtungszeitraum auf den " +
                   "Entscheidungszeitpunkt abgezinst. Referenz (Unterlassensalternative) ist das " +
                   "Stammprojekt — der Kapitalwert einer Variante ist der Barwert der Differenz-" +
                   "Zahlungsströme Variante − Stamm; ein positiver Wert bedeutet: die Variante ist " +
                   "über den Betrachtungszeitraum wirtschaftlicher als der Stamm.");
            TarifParameter tarifP = provider.LadeTarif(daten.IdStamm);
            k.Hinweis("Parameter dieses Rechenlaufs: " + p.Nachweis(k.Kultur) +
                      " · " + tarifP.Nachweis(k.Kultur) +
                      " · Restwert linear · Ersatzbeschaffungen nominal konstant. " +
                      "Energie-/Strompreise aus der Kostenmaske des jeweiligen Projekts; " +
                      "Investitions- und Betriebskosten aus den Kostenpositionen (Tab_ProjektWerte). " +
                      "Rechenstand: " + alle[0].Zeitstempel.ToString("dd.MM.yyyy HH:mm", k.Kultur) + ".");

            // Aktualität gegen den Simulationsstand prüfen. Nach der verbindlichen
            // Kette (Simulation → Wirtschaftlichkeit) darf hier nichts mehr auflaufen;
            // die Prüfung bleibt als Netz, falls doch etwas dazwischenkam.
            var veraltet = new List<string>();
            foreach (VariantenDaten v in daten.Varianten)
            {
                WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(x =>
                    x.IdProjekt == v.IdProjekt && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                if (e == null || (e.Fehlgrund == null && !provider.ErgebnisAktuell(e)))
                    veraltet.Add(v.IstStamm ? "Stamm" : v.Anzeige);
            }
            if (veraltet.Count > 0)
                k.Hinweis("⚠ Für " + string.Join(", ", veraltet) + " passt das " +
                          "Wirtschaftlichkeits-Ergebnis nicht zum Simulationslauf dieses " +
                          "Berichts — Bericht erneut erstellen.");

            // ---------------- Vergleichstabelle (Szenario Erwartet) ----------------
            k.Ueberschrift2("Kennzahlen im Szenario „Erwartet“");
            SchreibeVergleich(k, daten, alle, WirtschaftlichkeitSzenario.ERWARTET);

            // ---------------- Kapitalwert-Verlauf (Phase 11) ----------------
            SchreibeVerlauf(k, daten, provider, p, tarifP);

            // ---------------- Szenarienübersicht (Worst / Erwartet / Best) ----------------
            k.Ueberschrift2("Szenarien Worst / Erwartet / Best");
            k.Hinweis("Szenariowerte aus den Best-/Worst-Case-Feldern der Kostenpositionen " +
                      "(Betrag und Nutzungsdauer); nicht gepflegte Felder übernehmen den Erwartungswert.");
            SchreibeSzenarien(k, daten, alle);

            // ---------------- Sensitivitätsanalyse (W2, Normanforderung) ----------------
            List<SensitivitaetZeile> sens = provider.LadeSensitivitaet(ids);
            if (sens.Count > 0)
            {
                k.Ueberschrift2("Sensitivitätsanalyse (Szenario „Erwartet“)");
                k.Hinweis("Kapitalwert der Variante gegenüber dem Stamm bei Veränderung je eines " +
                          "Einflussparameters; Zins und Preissteigerung wirken auf beide Projekte, " +
                          "Investitions- und Energiekosten-Ausschlag nur auf die Variante.");
                SchreibeSensitivitaet(k, daten, sens);
            }

            // ---------------- Strommengen-Matrix + Tarif (W3) ----------------
            Dictionary<int, StromMatrix> matrizen = provider.LadeStromMatrix(ids);
            if (matrizen.Count > 0)
            {
                k.Ueberschrift2("Strommengen nach Tarifzonen");
                k.Hinweis("Stundenweise Zuordnung aus der In-Memory-Simulation (Referenzjahr 2026 " +
                          "für die Wochentage). KWK-Aufteilung: Eigenstrom = min(BHKW-Erzeugung, " +
                          "Strombedarf) je Stunde — dokumentierte Näherung (W3).");
                SchreibeMatrix(k, daten, matrizen);
            }

            // ---------------- Emissionsbilanz (W3) ----------------
            if (p.IdKraftwerkspark > 0)
            {
                k.Ueberschrift2("Emissionsbilanz — gekoppelte vs. getrennte Erzeugung");
                ReferenzkesselInfo rk = provider.LiesReferenzkessel(daten.IdStamm);
                k.Hinweis("Referenz (getrennt): dieselbe Brennstoff-Wärme im Referenzkessel (η = " +
                          p.RefKesselWirkungsgrad.ToString("N0", k.Kultur) + " %" +
                          (rk != null && rk.Gefunden
                           ? ", aus dem Stammprojekt: " + rk.Bezeichner +
                             (rk.BrennstoffName.Length > 0 ? ", " + rk.BrennstoffName : "")
                           : ", Vorgabewert — kein Heizkessel im Stammprojekt") +
                          ") und derselbe KWK-Strom im Kraftwerkspark, inkl. Netzverluste " +
                          "(Konzept Kap. 2.8).");
                SchreibeEmissionsbilanz(k, daten, p, alle, provider);
            }

            // Unvollständige Rechnungen ausweisen (keine stillen Lücken).
            foreach (WirtschaftlichkeitErgebnis e in alle.Where(x =>
                         x.Szenario == WirtschaftlichkeitSzenario.ERWARTET &&
                         (x.Fehlgrund != null || x.Hinweis != null)))
            {
                VariantenDaten v = daten.Varianten.FirstOrDefault(x => x.IdProjekt == e.IdProjekt);
                string name = v == null ? ("Projekt " + e.IdProjekt) : (v.IstStamm ? "Stamm" : v.Anzeige);
                if (e.Fehlgrund != null) k.Hinweis("⚠ " + name + ": " + e.Fehlgrund);
                if (e.Hinweis != null) k.Hinweis("⚠ " + name + ": " + e.Hinweis);
            }
        }

        // ------------------------------------------------------------- Verlauf (Phase 11)

        /// <summary>Kapitalwert-Verlauf über den Betrachtungszeitraum als Diagramme
        /// (Differenz zur Stamm-Referenz + absolute kumulierte Barwerte). Die Reihen
        /// werden aus den Berichtsdaten frisch gerechnet (Szenario Erwartet, T aus
        /// den Parametern) — derselbe Rechenkern wie der Verlaufs-Dialog.</summary>
        private static void SchreibeVerlauf(WordKontext k, BerichtsDaten daten,
                                            WirtschaftlichkeitCtrl provider,
                                            WirtschaftlichkeitParameter p,
                                            TarifParameter tarifP)
        {
            // Konsistenz-Gate (Review 11): sind Tarif oder KWKG aktiv, hängen die
            // Zahlungsreihen an den Stundenreihen. Wurde der Bericht OHNE Zeitreihen
            // gesammelt (Baustein „Ergebnisse je Variante" abgewählt), würde das
            // Diagramm andere Zahlen zeigen als die Tabellen darüber → entfallen
            // lassen und offen begründen (keine stillen Widersprüche).
            bool zeitreihenNoetig = (tarifP != null && tarifP.Aktiv) ||
                                    p.KwkgBonus > 0 || p.KwkgBonusEinspeisung > 0;
            if (zeitreihenNoetig &&
                daten.Varianten.Any(v => v.Fehler == null && v.Zeitreihen == null))
            {
                k.Ueberschrift2("Kapitalwert-Verlauf über den Betrachtungszeitraum");
                k.Hinweis("Diagramm entfällt: Tarifstruktur/KWKG benötigen Stundenreihen, " +
                          "der Bericht wurde aber ohne Zeitreihen erzeugt — Baustein " +
                          "„Ergebnisse je Variante“ aktivieren und den Bericht erneut erstellen.");
                return;
            }

            WirtschaftlichkeitVerlauf verlauf;
            try
            {
                verlauf = provider.BerechneVerlauf(daten, p, p.Betrachtungszeitraum,
                                                   WirtschaftlichkeitSzenario.ERWARTET);
            }
            catch { return; }
            if (verlauf == null || verlauf.Absolut.All(s => s.Kumuliert == null)) return;

            k.Ueberschrift2("Kapitalwert-Verlauf über den Betrachtungszeitraum");
            k.Hinweis("Kumulierte diskontierte Zahlungsströme je Jahr (Szenario „Erwartet“). " +
                      "Ohne Restwert — Nettobarwert = Endwert + Restwert-Barwert. " +
                      "Der Schnitt der Differenzlinie mit der Nulllinie ist die " +
                      "dynamische Amortisation. Aus den Berichtsdaten gerechnet, " +
                      "derselbe Rechenkern wie Reiter und Verlaufs-Dialog.");

            if (verlauf.Differenz.Any(s => s.Kumuliert != null))
                k.Bild(ChartRenderer.KapitalwertVerlauf(
                    "Differenz zur Stamm-Referenz",
                    ChartRenderer.VerlaufsReihen(verlauf.Differenz, false), null),
                    620, 310);
            else
                k.Hinweis("Differenzdiagramm entfällt — für das Stammprojekt konnte keine " +
                          "Zahlungsreihe gerechnet werden (siehe Hinweise am Kapitelende).");
            k.Bild(ChartRenderer.KapitalwertVerlauf(
                "Kumulierte Barwerte je Projekt",
                ChartRenderer.VerlaufsReihen(verlauf.Absolut, true), null),
                620, 310);
        }

        // ------------------------------------------------------------- Tabellen

        private static void SchreibeVergleich(WordKontext k, BerichtsDaten daten,
                                              List<WirtschaftlichkeitErgebnis> alle, string szenario)
        {
            // BEHG-/KWKG-/IRR-Zeilen nur, wenn sie im Datenbestand vorkommen (nie „0"-Zeilen).
            bool mitBehg = alle.Any(x => x.CO2AbgabeJahr > 0);
            bool mitKwkg = alle.Any(x => x.KwkgErloesJahr1 > 0);
            bool mitIrr = alle.Any(x => x.IRR.HasValue);

            var zeilen = new List<Zeile>();
            zeilen.Add(new Zeile("Investition I₀ [€]",            (e, kk) => kk.FW(e.Investition, "N0")));
            zeilen.Add(new Zeile("Betriebskosten [€/a]",          (e, kk) => kk.FW(e.BetriebskostenJahr, "N0")));
            zeilen.Add(new Zeile("Energiekosten [€/a]",           (e, kk) => kk.FW(e.EnergiekostenJahr, "N0")));
            if (alle.Any(x => x.StromkostenTarif.HasValue))
                zeilen.Add(new Zeile("Stromkosten Tarif [€/a]",   (e, kk) => kk.FW(e.StromkostenTarif, "N0")));
            if (mitBehg)
                zeilen.Add(new Zeile("CO₂-Abgabe BEHG [€/a]",     (e, kk) => kk.FW(e.CO2AbgabeJahr, "N0")));
            zeilen.Add(new Zeile("Einspeiseerlös [€/a]",          (e, kk) => kk.FW(e.EinspeiseerloesJahr, "N0")));
            if (mitKwkg)
                zeilen.Add(new Zeile("KWKG-Erlös Jahr 1 [€/a]",   (e, kk) => kk.FW(e.KwkgErloesJahr1, "N0")));
            // ETAPPE E2 (L6): die Bemessungsgrundlage der KWKG-Deckelung — ELEKTRISCHE
            // Vollbenutzungsstunden, leistungsgewichtet. Nur, wenn sie im Datenbestand
            // vorkommt (nie eine „0"-Zeile), genau wie die Zeilen darüber.
            if (alle.Any(x => x.KwkgVbhElektrisch > 0))
                zeilen.Add(new Zeile("Vbh elektrisch (KWKG-Basis) [h/a]",
                                     (e, kk) => e.KwkgVbhElektrisch > 0 ? kk.FW(e.KwkgVbhElektrisch, "N0") : "—"));
            // ETAPPE E4: die drei Steuergutschriften, nach derselben Regel wie oben —
            // nur, wenn sie im Datenbestand vorkommen (nie eine „0"-Zeile).
            if (alle.Any(x => x.EnergiesteuerJahr1 > 0))
                zeilen.Add(new Zeile(MyResource.Resource.WIRT_ZEILE_ENERGIESTEUER,
                                     (e, kk) => kk.FW(e.EnergiesteuerJahr1, "N0")));
            if (alle.Any(x => x.StromsteuerBefreiungJahr1 > 0))
                zeilen.Add(new Zeile(MyResource.Resource.WIRT_ZEILE_STROMST_BEFREIUNG,
                                     (e, kk) => kk.FW(e.StromsteuerBefreiungJahr1, "N0")));
            if (alle.Any(x => x.StromsteuerEntlastungJahr1 > 0))
                zeilen.Add(new Zeile(MyResource.Resource.WIRT_ZEILE_STROMST_ENTLASTUNG,
                                     (e, kk) => kk.FW(e.StromsteuerEntlastungJahr1, "N0")));
            // ETAPPE E5: vermiedene Kosten nach der Differenzmethode. Der Leistungsanteil
            // ist regelmäßig NEGATIV — die Bedingung prüft deshalb auf „ungleich 0".
            if (alle.Any(x => x.VermiedenGesamtJahr != 0 || x.VermiedenArbeitJahr != 0))
            {
                zeilen.Add(new Zeile(MyResource.Resource.WIRT_ZEILE_VERMIEDEN_ARBEIT,
                                     (e, kk) => kk.FW(e.VermiedenArbeitJahr, "N0")));
                zeilen.Add(new Zeile(MyResource.Resource.WIRT_ZEILE_VERMIEDEN_LEISTUNG,
                                     (e, kk) => kk.FW(e.VermiedenLeistungJahr, "N0")));
                zeilen.Add(new Zeile(MyResource.Resource.WIRT_ZEILE_VERMIEDEN_GESAMT,
                                     (e, kk) => kk.FW(e.VermiedenGesamtJahr, "N0")));
            }
            if (alle.Any(x => x.AufschlagJahr != 0))
                zeilen.Add(new Zeile(MyResource.Resource.WIRT_ZEILE_AUFSCHLAG,
                                     (e, kk) => kk.FW(e.AufschlagJahr, "N0")));
            zeilen.Add(new Zeile("Restwert (Barwert) [€]",        (e, kk) => kk.FW(e.RestwertBarwert, "N0")));
            zeilen.Add(new Zeile("Nettobarwert über T [€]",       (e, kk) => kk.FW(e.Kapitalwert, "N0")));
            zeilen.Add(new Zeile("Kapitalwert vs. Stamm [€]",     (e, kk) => e.IstStamm ? "—" : kk.FW(e.KapitalwertDiff, "N0")));
            zeilen.Add(new Zeile("Annuität des KW [€/a]",         (e, kk) => e.IstStamm ? "—" : kk.FW(e.AnnuitaetKW, "N0")));
            zeilen.Add(new Zeile("Amortisation [a]",              (e, kk) => e.IstStamm ? "—" : kk.FW(e.AmortisationJahre, "N1")));
            if (mitIrr)
                zeilen.Add(new Zeile("Interner Zinsfuß [%]",      (e, kk) => e.IstStamm ? "—" : kk.FW(e.IRR, "N1")));
            zeilen.Add(new Zeile("Wärmegestehungskosten [€/kWh]", (e, kk) => kk.FW(e.Gestehungskosten, "N3")));

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

        /// <summary>Strommengen-Matrix: je Projekt eine Tabelle Zone × Mengenart [MWh].</summary>
        private static void SchreibeMatrix(WordKontext k, BerichtsDaten daten,
                                           Dictionary<int, StromMatrix> matrizen)
        {
            foreach (VariantenDaten v in daten.Varianten)
            {
                if (!matrizen.ContainsKey(v.IdProjekt)) continue;
                StromMatrix m = matrizen[v.IdProjekt];

                k.Ueberschrift3((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);

                int wLabel = 2400;
                int wCol = (WordBerichtGenerator.INHALT_B - wLabel) / 4;
                int[] w = { wLabel, wCol, wCol, wCol, wCol };

                Table t = k.NeueTabelle(w);
                var kopf = new TableRow();
                kopf.Append(k.Zelle("Zone", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
                kopf.Append(k.Zelle("Netzbezug [MWh]", w[1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("PV-Einspeisung [MWh]", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("KWK-Eigenstrom [MWh]", w[3], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("KWK-Einspeisung [MWh]", w[4], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                t.Append(kopf);

                foreach (string zone in StromMatrix.Zonen)
                {
                    StromMatrix.Zone z = m.Hole(zone);
                    if (z == null) continue;
                    var tr = new TableRow();
                    tr.Append(k.Zelle(zone, w[0], false, null, JustificationValues.Left));
                    tr.Append(k.Zelle(k.F(z.BezugMWh, 1), w[1], false, null, JustificationValues.Right));
                    tr.Append(k.Zelle(k.F(z.EinspeisungPvMWh, 1), w[2], false, null, JustificationValues.Right));
                    tr.Append(k.Zelle(k.F(z.KwkEigenMWh, 1), w[3], false, null, JustificationValues.Right));
                    tr.Append(k.Zelle(k.F(z.KwkEinspeisungMWh, 1), w[4], false, null, JustificationValues.Right));
                    t.Append(tr);
                }
                k.Fuege(t);
                k.Hinweis("Jahres-Bezugsspitze: " + k.F(m.MaxBezugKW, 0) + " kW (Basis der Leistungspreis-Staffel).");
            }
        }

        /// <summary>Emissionsbilanz je Projekt: Schadstoff × gekoppelt/getrennt/Vermeidung.</summary>
        private static void SchreibeEmissionsbilanz(WordKontext k, BerichtsDaten daten,
                                                    WirtschaftlichkeitParameter p,
                                                    List<WirtschaftlichkeitErgebnis> alle,
                                                    WirtschaftlichkeitCtrl provider)
        {
            foreach (VariantenDaten v in daten.Varianten)
            {
                // Nur bei aktuellem Wirtschaftlichkeits-Ergebnis — sonst stünden
                // zwei Rechenstände in einem Kapitel (Review Phase 8).
                WirtschaftlichkeitErgebnis erw = alle.FirstOrDefault(x =>
                    x.IdProjekt == v.IdProjekt && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                if (erw == null || !provider.ErgebnisAktuell(erw))
                {
                    k.Hinweis("⚠ " + (v.IstStamm ? "Stamm" : v.Anzeige) +
                              ": Emissionsbilanz entfällt — das Wirtschaftlichkeits-Ergebnis " +
                              "passt nicht zum Simulationslauf dieses Berichts.");
                    continue;
                }
                EmissionsBilanz b = EmissionsBilanzRechner.Berechne(v.IdProjekt, p);
                if (b == null) continue;

                k.Ueberschrift3((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);
                if (b.Hinweis != null) { k.Hinweis("⚠ " + b.Hinweis); }
                if (!b.CO2GekoppeltT.HasValue && !b.CO2GetrenntT.HasValue) continue;

                k.Hinweis("Kraftwerkspark: " + b.ParkName);

                int wLabel = 2800;
                int wCol = (WordBerichtGenerator.INHALT_B - wLabel) / 3;
                int[] w = { wLabel, wCol, wCol, wCol };

                Table t = k.NeueTabelle(w);
                var kopf = new TableRow();
                kopf.Append(k.Zelle("Schadstoff", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
                kopf.Append(k.Zelle("Gekoppelt (System)", w[1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("Getrennt (Referenz)", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("Vermeidung", w[3], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                t.Append(kopf);

                Action<string, double?, double?> zeile = (label, gek, getr) =>
                {
                    var tr = new TableRow();
                    tr.Append(k.Zelle(label, w[0], false, null, JustificationValues.Left));
                    tr.Append(k.Zelle(k.FW(gek, "N1"), w[1], false, null, JustificationValues.Right));
                    tr.Append(k.Zelle(k.FW(getr, "N1"), w[2], false, null, JustificationValues.Right));
                    string diff = (gek.HasValue && getr.HasValue) ? k.F(getr.Value - gek.Value, 1) : "—";
                    tr.Append(k.Zelle(diff, w[3], false, null,
                        diff == "—" ? JustificationValues.Center : JustificationValues.Right));
                    t.Append(tr);
                };
                zeile("CO₂ [t/a]", b.CO2GekoppeltT, b.CO2GetrenntT);
                zeile("SO₂ [kg/a]", b.SO2GekoppeltKg, b.SO2GetrenntKg);
                zeile("NOx [kg/a]", b.NOxGekoppeltKg, b.NOxGetrenntKg);
                k.Fuege(t);
                k.Beschriftung(" ");
            }
        }

        /// <summary>Sensitivitätstabellen: je Variante 4 Parameterzeilen (−Δ · Basis · +Δ → KW).</summary>
        private static void SchreibeSensitivitaet(WordKontext k, BerichtsDaten daten,
                                                  List<SensitivitaetZeile> sens)
        {
            foreach (VariantenDaten v in daten.Varianten.Where(x => !x.IstStamm))
            {
                List<SensitivitaetZeile> zeilen = sens.Where(x => x.IdProjekt == v.IdProjekt).ToList();
                if (zeilen.Count == 0) continue;

                k.Ueberschrift3("Variante — " + v.Anzeige);

                int wLabel = 3600;
                int wCol = (WordBerichtGenerator.INHALT_B - wLabel) / 3;
                int[] w = { wLabel, wCol, wCol, wCol };

                Table t = k.NeueTabelle(w);
                var kopf = new TableRow();
                kopf.Append(k.Zelle("Parameter", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
                kopf.Append(k.Zelle("KW bei −Δ [€]", w[1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("KW Basis [€]", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("KW bei +Δ [€]", w[3], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                t.Append(kopf);

                foreach (SensitivitaetZeile z in zeilen)
                {
                    var tr = new TableRow();
                    tr.Append(k.Zelle(z.Parameter, w[0], false, null, JustificationValues.Left));
                    string[] werte = { k.FW(z.KwMinus, "N0"), k.FW(z.KwBasis, "N0"), k.FW(z.KwPlus, "N0") };
                    for (int i = 0; i < 3; i++)
                        tr.Append(k.Zelle(werte[i], w[i + 1], false,
                            i == 1 ? WordBerichtGenerator.STAMM_FILL : null,
                            werte[i] == "—" ? JustificationValues.Center : JustificationValues.Right));
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
