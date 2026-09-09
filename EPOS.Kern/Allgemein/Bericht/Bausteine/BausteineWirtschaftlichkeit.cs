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
            // LEITENTSCHEIDUNGEN L12/L13 — der Ausweis der Bilanzierungsregeln gehört in
            // dieselbe Nachweiszeile: Er sagt, nach welchem Rechtsstand die Emissionen
            // bewertet sind und mit welcher Konvention die Biomasse.
            k.Hinweis("Parameter dieses Rechenlaufs: " + p.Nachweis(k.Kultur) +
                      " · " + tarifP.Nachweis(k.Kultur) +
                      " · " + BilanzKonvention.Bestimme(p, new GesetzKatalog()).Ausweis(k.Kultur) +
                      " · Restwert linear · Ersatzbeschaffungen nominal konstant. " +
                      "Energie-/Strompreise aus der Kostenmaske des jeweiligen Projekts; " +
                      "Investitions- und Betriebskosten aus den Kostenpositionen (Tab_ProjektWerte). " +
                      "Rechenstand: " + alle[0].Zeitstempel.ToString("dd.MM.yyyy HH:mm", k.Kultur) + ".");

            // ---------------- ETAPPE W5‑B‑11 (09.09.2026) — die VALERI-Ausweise ----------
            //
            // Drei Sätze, die keine Zahl ändern und ohne die der Bericht unvollständig
            // ist (DIN EN 17463 verlangt die Offenlegung der Annahmen):
            //   G7  — warum dieser Betrachtungszeitraum? Erst das Verhältnis von T zu den
            //          Nutzungsdauern sagt, ob ein Restwert am Ende steht und ob
            //          zwischendurch ersetzt wird.
            //   G10 — Eigenverbrauchsquote und Einspeiseanteil sind ABGELEITET, nicht
            //          angenommen. Ohne den Satz hält der Leser sie für eine Schätzung.
            //   G1/G3/G5 — die drei bewusst NICHT umgesetzten Lücken, benannt statt
            //          verschwiegen (Anwenderentscheid 09.09.2026).
            SchreibeValeriAusweise(k, daten, provider, p);

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
                k.HinweisRoh(string.Format(MyResource.Resource.WIRT_ERGEBNIS_VERALTET,
                                           string.Join(", ", veraltet)));

            // ---------------- Vergleichstabelle (Szenario Erwartet) ----------------
            k.Ueberschrift2("Kennzahlen im Szenario „Erwartet“");
            // ETAPPE E7: Der Zeitbezug steht im Tabellenkopf statt in vier von
            // zweiundzwanzig Zeilentiteln — erst dadurch passt derselbe Schlüssel in
            // Kennzahlen- UND Mehrjahrestabelle.
            k.HinweisRoh(MyResource.Resource.WIRT_ZEILE_JAHR1);
            SchreibeVergleich(k, daten, alle, WirtschaftlichkeitSzenario.ERWARTET, tarifP);

            // ---------------- KWK-Zuschlag je Modul (E6 → E7) ----------------
            SchreibeKwkgModule(k, daten, alle);

            // ---------------- Betriebskosten nach Kostenarten (E3 → E7) ----------------
            SchreibeBetriebskosten(k, daten, alle);

            // ---------------- Kapitalwert-Verlauf + Mehrjahresübersicht ----------------
            // ETAPPE E7: Beide Blöcke leben von derselben Verlaufsrechnung; sie läuft
            // deshalb genau einmal.
            WirtschaftlichkeitVerlauf verlauf = HoleVerlauf(k, daten, provider, p, tarifP);
            SchreibeVerlauf(k, verlauf);
            SchreibeMehrjahres(k, daten, verlauf, alle);

            // ---------------- Szenarienübersicht (Worst / Erwartet / Best) ----------------
            k.Ueberschrift2("Szenarien Worst / Erwartet / Best");
            // ETAPPE W5‑B‑11 (G8): Der Satz nannte bis hierher nur die ZEILENwerte —
            // seit W5‑B‑9 gibt es die zweite Quelle, den pauschalen Parametersatz je
            // Szenario, und seit W5‑B‑11 zieht er auch die investitionsgekoppelten
            // Betriebskosten mit (G11). Ein Bericht, der nur eine der beiden Quellen
            // nennt, erklärt seine eigenen Zahlen nicht.
            k.HinweisRoh(MyResource.Resource.WIRT_SZ_QUELLEN);
            SchreibeSzenarien(k, daten, alle, p);

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
        private static WirtschaftlichkeitVerlauf HoleVerlauf(WordKontext k, BerichtsDaten daten,
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
                return null;
            }

            try
            {
                return provider.BerechneVerlauf(daten, p, p.Betrachtungszeitraum,
                                                WirtschaftlichkeitSzenario.ERWARTET);
            }
            catch { return null; }
        }

        private static void SchreibeVerlauf(WordKontext k, WirtschaftlichkeitVerlauf verlauf)
        {
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

        // ------------------------------------------------------- Mehrjahresübersicht (E7)

        /// <summary>
        /// ETAPPE E7 — die Mehrjahrestabelle: je Projekt eine Tabelle mit den Jahren 0…T
        /// als Zeilen und den Positionen des Zahlungsstroms als Spalten.
        ///
        /// <para><b>Was sie zeigt, das der Bericht bisher verschwieg.</b> Erstens das
        /// <b>Auslaufen des KWK-Zuschlags</b>: Die Spalte fällt in dem Jahr auf 0, in dem
        /// das Vollbenutzungsstunden-Kontingent erschöpft ist — im bisherigen
        /// „KWKG-Erlös Jahr 1" war davon nichts zu sehen. Zweitens, dass die
        /// Steuergutschriften auf dem heutigen Rechtsstand <b>flach</b> verlaufen.
        /// Drittens die <b>auseinanderlaufenden Preissteigerungssätze</b> für Betrieb und
        /// Energie.</para>
        ///
        /// <para><b>Layout.</b> Jahre als Zeilen, Positionen als Spalten — bei T = 20
        /// passen 21 Jahresspalten nicht auf A4. Spalten ohne einen einzigen Betrag
        /// entfallen (dieselbe Konvention wie bei den Kennzahlzeilen), die Schrift ist
        /// schmaler als in den übrigen Tabellen.</para>
        /// </summary>
        private static void SchreibeMehrjahres(WordKontext k, BerichtsDaten daten,
                                               WirtschaftlichkeitVerlauf verlauf,
                                               List<WirtschaftlichkeitErgebnis> alle)
        {
            if (verlauf == null || verlauf.Absolut.All(s => s.Bild == null)) return;

            k.Ueberschrift2Roh(MyResource.Resource.WIRT_MJ_TITEL);
            k.HinweisRoh(MyResource.Resource.WIRT_MJ_HINWEIS);

            foreach (VariantenDaten v in daten.Varianten)
            {
                VerlaufSerie serie = verlauf.Absolut.FirstOrDefault(s => s.IdProjekt == v.IdProjekt);
                k.Ueberschrift3((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);

                Mehrjahresbild bild = Mehrjahresbild.Baue(serie);
                if (bild == null)
                {
                    k.HinweisRoh(MyResource.Resource.WIRT_MJ_ENTFAELLT +
                                 (serie != null && serie.Fehlgrund != null
                                  ? " (" + serie.Fehlgrund + ")" : ""));
                    continue;
                }

                int wJahr = 620;
                int wCol = (WordBerichtGenerator.INHALT_B - wJahr) / bild.Spalten.Count;
                var w = new List<int> { wJahr };
                for (int i = 0; i < bild.Spalten.Count; i++) w.Add(wCol);

                Table t = k.NeueTabelle(w.ToArray());
                var kopf = new TableRow();
                kopf.Append(k.Zelle(MyResource.Resource.WIRT_MJ_JAHR, w[0], true,
                                    WordBerichtGenerator.HEAD_FILL, JustificationValues.Left,
                                    false, WordBerichtGenerator.SCHRIFT_TABELLE_SCHMAL));
                for (int i = 0; i < bild.Spalten.Count; i++)
                    kopf.Append(k.Zelle(bild.Spalten[i].Titel, w[i + 1], true,
                                        WordBerichtGenerator.HEAD_FILL, JustificationValues.Center,
                                        false, WordBerichtGenerator.SCHRIFT_TABELLE_SCHMAL));
                t.Append(kopf);

                for (int jahr = 0; jahr <= bild.Jahre; jahr++)
                {
                    var tr = new TableRow();
                    tr.Append(k.Zelle(jahr.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                      w[0], false, null, JustificationValues.Left,
                                      false, WordBerichtGenerator.SCHRIFT_TABELLE_SCHMAL));
                    for (int i = 0; i < bild.Spalten.Count; i++)
                    {
                        double wert = bild.Spalten[i].Wert(jahr);
                        tr.Append(k.Zelle(wert == 0 ? "—" : k.F(wert, 0), w[i + 1], false,
                                          bild.Spalten[i].IstSumme ? WordBerichtGenerator.STAMM_FILL : null,
                                          wert == 0 ? JustificationValues.Center : JustificationValues.Right,
                                          false, WordBerichtGenerator.SCHRIFT_TABELLE_SCHMAL));
                    }
                    t.Append(tr);
                }

                // Abschlusszeile: der Restwert-Barwert im Jahr T. Er ist kein Jahres-
                // zahlungsstrom, schließt die kumulierte Spalte aber auf den
                // Nettobarwert auf — die Tabelle prüft sich damit selbst.
                var abschluss = new TableRow();
                abschluss.Append(k.Zelle(MyResource.Resource.WIRT_MJ_RESTWERT_T, w[0], true, null,
                                         JustificationValues.Left, false,
                                         WordBerichtGenerator.SCHRIFT_TABELLE_SCHMAL));
                for (int i = 0; i < bild.Spalten.Count; i++)
                {
                    string txt = "—";
                    if (bild.Spalten[i].Schluessel == "BARWERT") txt = k.F(bild.RestwertBarwert, 0);
                    else if (bild.Spalten[i].Schluessel == "KUMULIERT") txt = k.F(bild.Kapitalwert, 0);
                    abschluss.Append(k.Zelle(txt, w[i + 1], true,
                                             WordBerichtGenerator.STAMM_FILL,
                                             txt == "—" ? JustificationValues.Center : JustificationValues.Right,
                                             false, WordBerichtGenerator.SCHRIFT_TABELLE_SCHMAL));
                }
                t.Append(abschluss);
                k.Fuege(t);

                k.HinweisRoh(string.Format(MyResource.Resource.WIRT_MJ_PROBE,
                                           k.F(bild.KumuliertT, 0),
                                           k.F(bild.RestwertBarwert, 0),
                                           k.F(bild.Kapitalwert, 0)));

                // Nachweisblock: vermiedene Kosten und Aufschlagsbetrag. Sie stehen
                // ausdrücklich AUSSERHALB der Tabelle — beide stecken bereits in anderen
                // Positionen, eine eigene Zahlungszeile wäre eine Doppelzählung.
                SchreibeNachweisblock(k, alle, v.IdProjekt);
            }
        }

        /// <summary>Vermiedene Kosten und Aufschlagsbetrag als benannter Nachweis (E7).</summary>
        private static void SchreibeNachweisblock(WordKontext k,
                                                  List<WirtschaftlichkeitErgebnis> alle, int idProjekt)
        {
            WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(x =>
                x.IdProjekt == idProjekt && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
            if (e == null) return;
            bool vermieden = e.VermiedenGesamtJahr != 0 || e.VermiedenArbeitJahr != 0;
            if (!vermieden && e.AufschlagJahr == 0) return;

            k.Ueberschrift3Roh(MyResource.Resource.WIRT_MJ_NACHWEIS_TITEL);
            k.HinweisRoh(MyResource.Resource.WIRT_MJ_NACHWEIS_HINWEIS);

            int wLabel = 5200, wWert = WordBerichtGenerator.INHALT_B - wLabel;
            Table t = k.NeueTabelle(new[] { wLabel, wWert });
            Action<string, double> zeile = (label, wert) =>
            {
                var tr = new TableRow();
                tr.Append(k.Zelle(label, wLabel, false, null, JustificationValues.Left, false,
                                  WordBerichtGenerator.SCHRIFT_TABELLE));
                tr.Append(k.Zelle(k.F(wert, 0), wWert, false, null, JustificationValues.Right, false,
                                  WordBerichtGenerator.SCHRIFT_TABELLE));
                t.Append(tr);
            };
            if (vermieden)
            {
                zeile(MyResource.Resource.WIRT_ZEILE_VERMIEDEN_ARBEIT, e.VermiedenArbeitJahr);
                zeile(MyResource.Resource.WIRT_ZEILE_VERMIEDEN_LEISTUNG, e.VermiedenLeistungJahr);
                zeile(MyResource.Resource.WIRT_ZEILE_VERMIEDEN_GESAMT, e.VermiedenGesamtJahr);
            }
            if (e.AufschlagJahr != 0)
                zeile(MyResource.Resource.WIRT_ZEILE_AUFSCHLAG, e.AufschlagJahr);
            k.Fuege(t);
            k.Beschriftung(" ");
        }

        // ------------------------------------------------------- KWK-Zuschlag je Modul (E7)

        /// <summary>
        /// ETAPPE E7 — eine Zeile je BHKW-Modul der KWKG-Rechnung (Übergabepunkt 1 aus
        /// E6). Bis dahin stand dieselbe Auskunft als Aufzählung in einer Hinweiszeile,
        /// die bei drei Modulen unlesbar wird.
        /// </summary>
        private static void SchreibeKwkgModule(WordKontext k, BerichtsDaten daten,
                                               List<WirtschaftlichkeitErgebnis> alle)
        {
            var mitModulen = alle.Where(x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET &&
                                             x.KwkgModule != null && x.KwkgModule.Count > 0).ToList();
            if (mitModulen.Count == 0) return;

            k.Ueberschrift2Roh(MyResource.Resource.WIRT_KWKG_MODUL_TITEL);
            k.HinweisRoh(MyResource.Resource.WIRT_KWKG_MODUL_HINWEIS);

            foreach (VariantenDaten v in daten.Varianten)
            {
                WirtschaftlichkeitErgebnis e = mitModulen.FirstOrDefault(x => x.IdProjekt == v.IdProjekt);
                if (e == null) continue;

                k.Ueberschrift3((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);

                // Elf Spalten, gleich denen des Excel-Blattes — Word und Excel sollen
                // dieselbe Tabelle zeigen, nicht zwei verschieden beschnittene.
                int wName = 1655;
                int wCol = (WordBerichtGenerator.INHALT_B - wName) / 10;
                var w = new List<int> { wName };
                for (int i = 0; i < 10; i++) w.Add(wCol);

                string[] kopfTexte =
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

                Table t = k.NeueTabelle(w.ToArray());
                var kopf = new TableRow();
                for (int i = 0; i < kopfTexte.Length; i++)
                    kopf.Append(k.Zelle(kopfTexte[i], w[i], true, WordBerichtGenerator.HEAD_FILL,
                                        i == 0 ? JustificationValues.Left : JustificationValues.Center,
                                        false, WordBerichtGenerator.SCHRIFT_TABELLE_SCHMAL));
                t.Append(kopf);

                foreach (KwkgModulNachweis m in e.KwkgModule)
                {
                    string[] werte =
                    {
                        m.Bezeichner,
                        k.F(m.PelKW, 0),
                        k.F(m.VbhElektrisch, 0),
                        k.F(m.SatzEigenCt, 2),
                        k.F(m.SatzEinspeisungCt, 2),
                        m.SatzAusAnlage ? MyResource.Resource.WIRT_KWKG_SATZ_QUELLE_ANLAGE
                                        : MyResource.Resource.WIRT_KWKG_SATZ_QUELLE_PROJEKT,
                        m.JahresdeckelH > 0 ? k.F(m.JahresdeckelH, 0)
                                            : MyResource.Resource.WIRT_KWKG_DECKEL_STAFFEL,
                        k.F(m.KontingentH, 0),
                        m.Foerderbeginn.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        k.F(m.Jahr1Eur, 0),
                        m.ErschoepftAbJahr > 0
                            ? m.ErschoepftAbJahr.ToString(System.Globalization.CultureInfo.InvariantCulture)
                            : MyResource.Resource.WIRT_KWKG_ERSCHOEPFT_NIE
                    };
                    var tr = new TableRow();
                    for (int i = 0; i < werte.Length; i++)
                        tr.Append(k.Zelle(werte[i], w[i], false, null,
                                          i == 0 ? JustificationValues.Left : JustificationValues.Right,
                                          false, WordBerichtGenerator.SCHRIFT_TABELLE_SCHMAL));
                    t.Append(tr);
                }
                k.Fuege(t);

                // Die Herleitung des angesetzten Satzes nach § 7 — Tranchen, nicht Klasse.
                foreach (KwkgModulNachweis m in e.KwkgModule)
                    if (m.HerleitungEigen.Length > 0 || m.HerleitungEinspeisung.Length > 0)
                        k.HinweisRoh(string.Format(MyResource.Resource.WIRT_KWKG_HERLEITUNG_ZEILE,
                                                   m.Bezeichner, m.HerleitungEigen,
                                                   m.HerleitungEinspeisung));
                k.Beschriftung(" ");
            }
        }

        // ------------------------------------------------- Betriebskosten nach Kostenart (E7)

        /// <summary>
        /// ETAPPE E7 — die Betriebskostenpositionen, gegliedert nach der Kostenart der
        /// VDI 2067, je Position mit Bemessungsart und Herleitung Menge × Einheitpreis.
        /// Das ist der Zweck, für den Etappe E3 die Spalte <c>Kostenart</c> angelegt hat.
        /// </summary>
        private static void SchreibeBetriebskosten(WordKontext k, BerichtsDaten daten,
                                                   List<WirtschaftlichkeitErgebnis> alle)
        {
            var mitPositionen = alle.Where(x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET &&
                                                x.Betriebskosten != null &&
                                                x.Betriebskosten.Count > 0).ToList();
            if (mitPositionen.Count == 0) return;

            k.Ueberschrift2Roh(MyResource.Resource.WIRT_BK_TITEL);
            k.HinweisRoh(MyResource.Resource.WIRT_BK_HINWEIS);

            int wPos = 2700, wGruppe = 1500, wBem = 1600, wHerl = 2400;
            int wBetrag = WordBerichtGenerator.INHALT_B - wPos - wGruppe - wBem - wHerl;
            int[] w = { wPos, wGruppe, wBem, wHerl, wBetrag };

            foreach (VariantenDaten v in daten.Varianten)
            {
                WirtschaftlichkeitErgebnis e = mitPositionen.FirstOrDefault(x => x.IdProjekt == v.IdProjekt);
                if (e == null) continue;

                k.Ueberschrift3((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);

                Table t = k.NeueTabelle(w);
                var kopf = new TableRow();
                kopf.Append(k.Zelle(MyResource.Resource.WIRT_BK_SP_POSITION, w[0], true,
                                    WordBerichtGenerator.HEAD_FILL, JustificationValues.Left, false,
                                    WordBerichtGenerator.SCHRIFT_TABELLE));
                kopf.Append(k.Zelle(MyResource.Resource.WIRT_BK_SP_GRUPPE, w[1], true,
                                    WordBerichtGenerator.HEAD_FILL, JustificationValues.Left, false,
                                    WordBerichtGenerator.SCHRIFT_TABELLE));
                kopf.Append(k.Zelle(MyResource.Resource.WIRT_BK_SP_BEMESSUNG, w[2], true,
                                    WordBerichtGenerator.HEAD_FILL, JustificationValues.Left, false,
                                    WordBerichtGenerator.SCHRIFT_TABELLE));
                kopf.Append(k.Zelle(MyResource.Resource.WIRT_BK_SP_HERLEITUNG, w[3], true,
                                    WordBerichtGenerator.HEAD_FILL, JustificationValues.Left, false,
                                    WordBerichtGenerator.SCHRIFT_TABELLE));
                kopf.Append(k.Zelle(MyResource.Resource.WIRT_BK_SP_BETRAG, w[4], true,
                                    WordBerichtGenerator.HEAD_FILL, JustificationValues.Center, false,
                                    WordBerichtGenerator.SCHRIFT_TABELLE));
                t.Append(kopf);

                double summe = 0;
                foreach (string art in WirtschaftlichkeitZeilen.Kostenarten)
                {
                    List<KostenPositionNachweis> block = e.Betriebskosten
                        .Where(x => string.Equals(x.Kostenart ?? "", art, StringComparison.Ordinal))
                        .ToList();
                    if (block.Count == 0) continue;

                    // Gruppenzeile der Kostenart über die volle Breite der ersten Spalte.
                    var gz = new TableRow();
                    gz.Append(k.Zelle(WirtschaftlichkeitZeilen.KostenartText(art), w[0], true,
                                      WordBerichtGenerator.STAMM_FILL, JustificationValues.Left, false,
                                      WordBerichtGenerator.SCHRIFT_TABELLE));
                    for (int i = 1; i < w.Length; i++)
                        gz.Append(k.Zelle("", w[i], true, WordBerichtGenerator.STAMM_FILL,
                                          JustificationValues.Left, false,
                                          WordBerichtGenerator.SCHRIFT_TABELLE));
                    t.Append(gz);

                    foreach (KostenPositionNachweis n in block)
                    {
                        string herleitung = WirtschaftlichkeitZeilen.Herleitung(n, k.Kultur);
                        if (herleitung.Length == 0 && n.SzenarioGepflegt)
                            herleitung = MyResource.Resource.WIRT_BK_SZENARIOWERT;

                        var tr = new TableRow();
                        tr.Append(k.Zelle(n.Bezeichnung, w[0], false, null, JustificationValues.Left,
                                          false, WordBerichtGenerator.SCHRIFT_TABELLE));
                        tr.Append(k.Zelle(n.Gruppe, w[1], false, null, JustificationValues.Left,
                                          false, WordBerichtGenerator.SCHRIFT_TABELLE));
                        tr.Append(k.Zelle(WirtschaftlichkeitZeilen.BemessungText(n.Bemessung), w[2],
                                          false, null, JustificationValues.Left, false,
                                          WordBerichtGenerator.SCHRIFT_TABELLE));
                        tr.Append(k.Zelle(herleitung, w[3], false, null, JustificationValues.Left,
                                          false, WordBerichtGenerator.SCHRIFT_TABELLE));
                        tr.Append(k.Zelle(k.F(n.BetragJahr, 0), w[4], false, null,
                                          JustificationValues.Right, false,
                                          WordBerichtGenerator.SCHRIFT_TABELLE));
                        t.Append(tr);
                        summe += n.BetragJahr;
                    }
                }

                var sz = new TableRow();
                sz.Append(k.Zelle(MyResource.Resource.WIRT_BK_SUMME, w[0], true,
                                  WordBerichtGenerator.HEAD_FILL, JustificationValues.Left, false,
                                  WordBerichtGenerator.SCHRIFT_TABELLE));
                for (int i = 1; i < 4; i++)
                    sz.Append(k.Zelle("", w[i], true, WordBerichtGenerator.HEAD_FILL,
                                      JustificationValues.Left, false,
                                      WordBerichtGenerator.SCHRIFT_TABELLE));
                sz.Append(k.Zelle(k.F(summe, 0), w[4], true, WordBerichtGenerator.HEAD_FILL,
                                  JustificationValues.Right, false,
                                  WordBerichtGenerator.SCHRIFT_TABELLE));
                t.Append(sz);
                k.Fuege(t);

                // Probe gegen die Zahl, mit der die Kapitalwertrechnung gerechnet hat.
                if (e.BetriebskostenJahr.HasValue &&
                    Math.Abs(summe - e.BetriebskostenJahr.Value) > 0.5)
                    k.HinweisRoh(string.Format(MyResource.Resource.WIRT_BK_ABWEICHUNG,
                                               k.F(summe, 2),
                                               k.F(e.BetriebskostenJahr.Value, 2)));
                k.Beschriftung(" ");
            }
        }

        // ------------------------------------------------------------- Tabellen

        private static void SchreibeVergleich(WordKontext k, BerichtsDaten daten,
                                              List<WirtschaftlichkeitErgebnis> alle, string szenario,
                                              TarifParameter tarif)
        {
            // ETAPPE E7: EINE Zeilendefinition für Word, Excel und Ergebnisreiter.
            // Bis dahin stand dieselbe Liste dreimal im Code; die Zahlen liefen nicht
            // auseinander, das Drumherum aber schon.
            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Kennzahlen(alle, tarif);

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

                foreach (WirtZeile z in zeilen)
                {
                    var tr = new TableRow();
                    // Der Titel kommt aus MyResource und ist damit bereits in der
                    // Berichtssprache — er darf NICHT noch einmal durch BerichtTexte.T().
                    tr.Append(k.Zelle(z.Titel, w[0], false, null, JustificationValues.Left,
                                      false, WordBerichtGenerator.SCHRIFT_TABELLE));
                    for (int i = 0; i < spalten.Count; i++)
                    {
                        WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(x =>
                            x.IdProjekt == spalten[i].IdProjekt && x.Szenario == szenario);
                        string txt = z.Anzeige(e, k.Kultur);
                        tr.Append(k.Zelle(txt, w[i + 1], false,
                            spalten[i].IstStamm ? WordBerichtGenerator.STAMM_FILL : null,
                            z.IstText ? JustificationValues.Left
                                      : (txt == "—" ? JustificationValues.Center : JustificationValues.Right),
                            false, WordBerichtGenerator.SCHRIFT_TABELLE));
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

                // ETAPPE E7: fünfte Mengenspalte „Bedarf ohne Anlage". Sie wird seit E5
                // gerechnet und persistiert, war aber in keiner der beiden Matrixausgaben
                // zu sehen — dabei ist sie die Bezugsgröße der vermiedenen Kosten.
                int wLabel = 2000;
                int wCol = (WordBerichtGenerator.INHALT_B - wLabel) / 5;
                int[] w = { wLabel, wCol, wCol, wCol, wCol, wCol };

                Table t = k.NeueTabelle(w);
                var kopf = new TableRow();
                kopf.Append(k.Zelle("Zone", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
                kopf.Append(k.Zelle(MyResource.Resource.WIRT_MATRIX_BEDARF, w[1], true,
                                    WordBerichtGenerator.HEAD_FILL, JustificationValues.Center,
                                    false, WordBerichtGenerator.SCHRIFT_TABELLE));
                kopf.Append(k.Zelle("Netzbezug [MWh]", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("PV-Einspeisung [MWh]", w[3], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("KWK-Eigenstrom [MWh]", w[4], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("KWK-Einspeisung [MWh]", w[5], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                t.Append(kopf);

                foreach (string zone in StromMatrix.Zonen)
                {
                    StromMatrix.Zone z = m.Hole(zone);
                    if (z == null) continue;
                    var tr = new TableRow();
                    tr.Append(k.Zelle(zone, w[0], false, null, JustificationValues.Left));
                    tr.Append(k.Zelle(k.F(z.BedarfMWh, 1), w[1], false, null, JustificationValues.Right));
                    tr.Append(k.Zelle(k.F(z.BezugMWh, 1), w[2], false, null, JustificationValues.Right));
                    tr.Append(k.Zelle(k.F(z.EinspeisungPvMWh, 1), w[3], false, null, JustificationValues.Right));
                    tr.Append(k.Zelle(k.F(z.KwkEigenMWh, 1), w[4], false, null, JustificationValues.Right));
                    tr.Append(k.Zelle(k.F(z.KwkEinspeisungMWh, 1), w[5], false, null, JustificationValues.Right));
                    t.Append(tr);
                }
                k.Fuege(t);
                k.Hinweis("Jahres-Bezugsspitze: " + k.F(m.MaxBezugKW, 0) + " kW (Basis der Leistungspreis-Staffel).");
                k.HinweisRoh(MyResource.Resource.WIRT_MATRIX_BEDARF_HINWEIS);
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

                // LEITENTSCHEIDUNGEN L12/L13 — die Regeln, nach denen diese Tabelle
                // gerechnet ist, stehen ÜBER ihr. Der Kraftwerkspark wird nur genannt,
                // wenn er tatsächlich eine Gutschrift trägt.
                if (b.Konvention == null || b.Konvention.Stromgutschrift)
                    k.Hinweis("Kraftwerkspark: " + b.ParkName);
                if (b.Konvention != null)
                {
                    k.Hinweis(b.Konvention.Ausweis(k.Kultur));
                    if (b.Konvention.OhneGutschrift) k.HinweisRoh(MyResource.Resource.BILANZ_HINWEIS_DIN);
                    if (b.Konvention.Substitution) k.HinweisRoh(MyResource.Resource.BILANZ_HINWEIS_SUBSTITUTION);
                    if (b.CO2BiogenT > 0) k.HinweisRoh(MyResource.Resource.BILANZ_HINWEIS_BIOMASSE);
                }

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
                // E5/F7: Der Zeilentitel nennt den Modus, in dem die Zahl entstand.
                zeile(EmissionsAusweis.BilanzZeile(b.Modus), b.CO2GekoppeltT, b.CO2GetrenntT);
                zeile("SO₂ [kg/a]", b.SO2GekoppeltKg, b.SO2GetrenntKg);
                zeile("NOx [kg/a]", b.NOxGekoppeltKg, b.NOxGetrenntKg);
                k.Fuege(t);

                // Die beiden Teilbeträge, die aus einer WAHL stammen und in den Zahlen
                // oben stecken — als Zeilen unter der Tabelle statt in ihr: Eine
                // Differenzspalte „Vermeidung" hätte für sie keine Bedeutung.
                if (b.CO2BiogenT > 0)
                    k.Hinweis(MyResource.Resource.BILANZ_ZEILE_BIOGEN + ": " +
                              k.F(b.CO2BiogenT, 1) + " (im gekoppelten System enthalten)");
                if (b.CO2GutschriftStromT > 0)
                    k.Hinweis(MyResource.Resource.BILANZ_ZEILE_GUTSCHRIFT + ": " +
                              k.F(b.CO2GutschriftStromT, 1) + " (in der getrennten Referenz enthalten)");
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

        /// <summary>
        /// Szenarienübersicht: je Variante die Kapitalwert-DIFFERENZ zum Stamm in
        /// W/E/B, die Amortisation und — seit W5‑B‑11 — die Einstufung.
        ///
        /// <para><b>ETAPPE W5‑B‑11 (G8/G9, 09.09.2026).</b> Die drei Wertspalten führten
        /// schon immer <see cref="WirtschaftlichkeitErgebnis.KapitalwertDiff"/>, waren
        /// aber mit „KW Worst" überschrieben — also mit dem Namen einer anderen Größe
        /// (der absolute Kapitalwert steht in der Kennzahlentabelle darüber). Die
        /// Köpfe sagen jetzt ΔKW und stehen als Ressourcen; darunter erklärt eine
        /// Fußzeile, was Δ heißt und wann „—" erscheint. Die neue Spalte
        /// „Einstufung" zeigt die Grundlage des Vorschlags, der unter der Tabelle
        /// steht — sonst müsste der Leser die Regel aus drei Zahlen selbst
        /// zurückrechnen.</para>
        ///
        /// <para>Unter der Tabelle stehen die ANNAHMEN von Best und Worst (der
        /// wirksame Parametersatz mit seiner Herkunft) und der Vorschlag zur
        /// Entscheidung. Beides ist Ausgabe; gerechnet wird hier nichts.</para>
        /// </summary>
        private static void SchreibeSzenarien(WordKontext k, BerichtsDaten daten,
                                              List<WirtschaftlichkeitErgebnis> alle,
                                              WirtschaftlichkeitParameter p)
        {
            List<VariantenDaten> varianten = daten.Varianten.Where(v => !v.IstStamm).ToList();
            if (varianten.Count == 0)
            {
                k.Hinweis("Keine Varianten ausgewählt — die Szenarienübersicht entfällt.");
                return;
            }

            // W5‑B‑11: sechs Spalten statt fünf. Die Summe bleibt INHALT_B — die
            // Beschriftungsspalte gibt die Breite ab, die die Einstufung braucht.
            int wLabel = 2455;
            int wCol = (WordBerichtGenerator.INHALT_B - wLabel) / 5;
            int[] w = { wLabel, wCol, wCol, wCol, wCol, wCol };

            List<VariantenEmpfehlung> urteile = WirtschaftlichkeitEmpfehlung.Einstufungen(alle);

            Table t = k.NeueTabelle(w);
            var kopf = new TableRow();
            kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_SZ_SP_VARIANTE, w[0], JustificationValues.Left));
            kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_SZ_SP_WORST, w[1], JustificationValues.Center));
            kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_SZ_SP_ERWARTET, w[2], JustificationValues.Center));
            kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_SZ_SP_BEST, w[3], JustificationValues.Center));
            kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_SZ_SP_AMORT, w[4], JustificationValues.Center));
            kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_EMPF_SPALTE, w[5], JustificationValues.Center));
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

                // W5‑B‑11 (G9): die Einstufung derselben Variante. „—", solange kein
                // Erwartet-Ergebnis vorliegt — ein Urteil ohne Zahl gibt es nicht.
                VariantenEmpfehlung u = urteile.FirstOrDefault(x => x.IdProjekt == v.IdProjekt);
                tr.Append(k.Zelle(u == null ? "—" : u.StufeText, w[5], false, null,
                    u == null ? JustificationValues.Center : JustificationValues.Left));
                t.Append(tr);
            }
            k.Fuege(t);
            k.HinweisRoh(MyResource.Resource.WIRT_SZ_DELTA_FUSS);

            // ---- W5‑B‑11 (G8): die ANNAHMEN der Bandbreite, je Szenario eine Zeile ----
            //
            // Der Nachweis nennt den WIRKSAMEN Satz (i, p_E, p_B, Investition, Erträge,
            // Nutzungsdauer) und seine Herkunft: „Vorgaben", solange niemand ein Feld
            // gepflegt hat, sonst „gepflegte Werte". Dieselben Ressourcen wie Dialog und
            // Seite — drei Formulierungen derselben Auskunft wären drei Wahrheiten.
            SchreibeSzenarioAnnahmen(k, p);

            // ---- W5‑B‑11 (G9): der Vorschlag zur Entscheidung ----------------------
            string vorschlag = WirtschaftlichkeitEmpfehlung.Vorschlagstext(urteile, k.Kultur);
            if (!string.IsNullOrEmpty(vorschlag)) k.TextRoh(vorschlag);
            k.Beschriftung(" ");
        }

        /// <summary>W5‑B‑11: Kopfzelle mit einem Text, der bereits aus
        /// <c>MyResource</c> kommt — er darf nicht noch einmal durch
        /// <c>BerichtTexte.T()</c> laufen (Etappe E7, Doppelübersetzung).</summary>
        private static TableCell Kopfzelle(WordKontext k, string text, int breite,
                                           JustificationValues just)
        {
            return k.Zelle(text, breite, true, WordBerichtGenerator.HEAD_FILL, just, false,
                           WordBerichtGenerator.SCHRIFT_TABELLE);
        }

        /// <summary>
        /// ETAPPE W5‑B‑11 (G8): die Annahmenzeile je Szenario — Best und Worst, in
        /// dieser Reihenfolge. Erwartet bekommt keine: Es IST der Projektparametersatz,
        /// und der steht bereits in „Parameter dieses Rechenlaufs".
        /// </summary>
        private static void SchreibeSzenarioAnnahmen(WordKontext k, WirtschaftlichkeitParameter p)
        {
            if (p == null) return;
            foreach (string sz in new[] { WirtschaftlichkeitSzenario.WORST,
                                          WirtschaftlichkeitSzenario.BEST })
            {
                SzenarioSatz satz = p.SatzFuer(sz);
                if (satz == null) continue;
                string name = sz == WirtschaftlichkeitSzenario.BEST
                            ? MyResource.Resource.WIRT_SZEN_BEST
                            : MyResource.Resource.WIRT_SZEN_WORST;
                string muster = satz.NurVorgaben
                              ? MyResource.Resource.WPAR_SZ_HERKUNFT_VORGABE
                              : MyResource.Resource.WPAR_SZ_HERKUNFT_GEPFLEGT;
                k.HinweisRoh(string.Format(k.Kultur, muster, name, satz.Nachweis(p, k.Kultur)));
            }
        }

        /// <summary>
        /// ETAPPE W5‑B‑11 (Anwenderentscheid 09.09.2026): die drei VALERI-Ausweise
        /// unter dem Parameternachweis — Nutzungsdauer-Abgleich (G7),
        /// Herleitung der Eigennutzung (G10) und die offengelegten Vereinfachungen
        /// (G1/G3/G5).
        ///
        /// <para><b>Alles reine Ausgabe.</b> Gelesen werden die Investitionspositionen
        /// des ERWARTET-Laufs — dieselbe Liste, mit der der Kapitalwert gerechnet hat.
        /// Ein Lesefehler darf den Bericht nicht kippen; die Zeile entfällt dann
        /// still, denn sie ist Beiwerk, kein Ergebnis.</para>
        /// </summary>
        private static void SchreibeValeriAusweise(WordKontext k, BerichtsDaten daten,
                                                   WirtschaftlichkeitCtrl provider,
                                                   WirtschaftlichkeitParameter p)
        {
            try
            {
                var positionen = new List<KapitalwertRechner.InvestPosition>();
                foreach (VariantenDaten v in daten.Varianten)
                    positionen.AddRange(WirtschaftlichkeitCtrl.LiesInvestitionen(
                        v.IdProjekt, WirtschaftlichkeitSzenario.ERWARTET));
                string zeitraum = NutzungsdauerAbgleich.Hinweis(p.Betrachtungszeitraum,
                                                                positionen, k.Kultur);
                if (!string.IsNullOrEmpty(zeitraum)) k.HinweisRoh(zeitraum);
            }
            catch { }

            try
            {
                WirtschaftlichkeitCtrl.ErzeugerFlags flags = provider.ErzeugerDerGruppe(daten.IdStamm);
                if (flags != null && flags.Photovoltaik)
                    k.HinweisRoh(ValeriAusweis.EigennutzungHerleitung());
            }
            catch { }

            k.HinweisRoh(ValeriAusweis.Vereinfachungen());
        }
    }
}
