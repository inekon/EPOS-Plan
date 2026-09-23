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

            // ETAPPE E5 Teil b: die BEWERTUNG dieses Laufs — Bandbreite mit Einstufungen,
            // Vorschlag, Hinweistext, Deklarationen, Nutzungsdauer-Hinweise, Stände ohne
            // Nachweis und Sensitivität. Der Sammler legt sie an den Baum; wer den Baustein
            // ohne Sammler ruft (Proben, Rückfall), bekommt sie aus denselben Kernmethoden.
            // Word bildet keine dieser Tafeln mehr selbst.
            WirtschaftlichkeitBewertung bewertung = daten.Bewertung
                ?? WirtschaftlichkeitBewertung.FuerBericht(daten, alle, p, k.Kultur);
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
                      // ETAPPE W5‑B‑12: „Ersatzbeschaffungen nominal konstant" war bis
                      // hierher richtig und ist es jetzt nur noch bei p_I = 0. Der Satz
                      // sagt deshalb, was TATSÄCHLICH gerechnet wurde — der wirksame
                      // Satz selbst steht mit seiner Herkunft im Parameternachweis davor.
                      " · Restwert linear · " +
                      (p.PreisInvestWirksam != 0
                          ? "Ersatzbeschaffungen preisindiziert mit p_I (VDI 2067). "
                          : "Ersatzbeschaffungen nominal konstant (p_I = 0). ") +
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
            SchreibeValeriAusweise(k, daten, provider, bewertung);

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
            // deshalb genau einmal. ETAPPE E6: Sie rechnet alle drei Szenarien (drei
            // vollständige Läufe ohne Speichern); die Mehrjahrestabelle nimmt daraus den
            // Erwartungsfall — Zahl für Zahl der bisherige Einzellauf.
            WirtschaftlichkeitVerlaufSzenarien verlauf = HoleVerlauf(k, daten, provider, p, tarifP);
            SchreibeVerlauf(k, verlauf);
            // ETAPPE E8a (U41): das Brückenbild zur Kapitalwertdifferenz — aus DENSELBEN drei
            // Läufen, neben den Bildern des Verlaufs und vor den Jahresreihen der Mehrjahrestafel
            // (im Mockup steht die Brücke unter der Gliederung, vor dem Zahlungsstrom).
            SchreibeBruecke(k, daten, verlauf, alle, p, bewertung);
            SchreibeMehrjahres(k, daten, verlauf == null ? null : verlauf.Lauf(WirtschaftlichkeitSzenario.ERWARTET), alle);

            // ---------------- Szenarienübersicht (Ungünstig / Erwartet / Günstig) ----------------
            // ETAPPE E6 (Befund Nach #434 (d), Entscheid E5‑Q2): Die Überschrift nennt die
            // Szenarien mit ihren Namen aus MyResource — bis hierher stand „Worst / Erwartet
            // / Best" fest im Quelltext, ohne Schlüssel und ohne Übersetzung.
            k.Ueberschrift2Roh(string.Format(k.Kultur, MyResource.Resource.WIRT_SZ_UEBERSCHRIFT,
                                             MyResource.Resource.WIRT_SZEN_WORST,
                                             MyResource.Resource.WIRT_SZEN_ERWARTET,
                                             MyResource.Resource.WIRT_SZEN_BEST));
            // ETAPPE W5‑B‑11 (G8): Der Satz nannte bis hierher nur die ZEILENwerte —
            // seit W5‑B‑9 gibt es die zweite Quelle, den pauschalen Parametersatz je
            // Szenario, und seit W5‑B‑11 zieht er auch die investitionsgekoppelten
            // Betriebskosten mit (G11). Ein Bericht, der nur eine der beiden Quellen
            // nennt, erklärt seine eigenen Zahlen nicht.
            k.HinweisRoh(MyResource.Resource.WIRT_SZ_QUELLEN);
            SchreibeSzenarien(k, bewertung, p);

            // ---- ETAPPE W5‑B‑12 (VALERI-Lücke G6): die nicht monetären Wirkungen ----
            //
            // Unmittelbar NACH dem Vorschlag zur Entscheidung: Erst die Zahl mit ihrer
            // Bandbreite und der Empfehlung, dann das, was die Zahl nicht fassen kann.
            // DIN EN 17463 verlangt beides nebeneinander.
            //
            // OHNE GEPFLEGTEN TEXT ENTFÄLLT DER GANZE BLOCK — Überschrift eingeschlossen.
            // Eine leere Überschrift wäre keine Aussage, sondern eine Lücke mit Titel.
            if (p != null && !string.IsNullOrWhiteSpace(p.NichtMonetaer))
            {
                k.Ueberschrift2Roh(MyResource.Resource.WIRT_NM_TITEL);
                k.TextRoh(p.NichtMonetaer.Trim());
            }

            // ---------------- Sensitivitätsanalyse (W2, Normanforderung) ----------------
            // ETAPPE E5 Teil b (V‑A): aus der Bewertung des Laufs — in Sicht 2 also gegen A —
            // und mit der Steigungsspalte (€ je %-Punkt bzw. je %).
            List<SensitivitaetZeile> sens = bewertung.Sensitivitaet ?? new List<SensitivitaetZeile>();
            if (sens.Count > 0)
            {
                k.Ueberschrift2("Sensitivitätsanalyse (Szenario „Erwartet“)");
                k.Hinweis("Kapitalwert der Variante gegenüber dem Stamm bei Veränderung je eines " +
                          "Einflussparameters; Zins und Preissteigerung wirken auf beide Projekte, " +
                          "Investitions- und Energiekosten-Ausschlag nur auf die Variante.");
                SchreibeSensitivitaet(k, daten, sens);
            }

            // ---------------- Strommengen-Matrix (W3) ----------------
            // Q11 (E7b): keine Tarifzonen mehr — eine Jahreszeile je Projekt.
            Dictionary<int, StromMatrix> matrizen = provider.LadeStromMatrix(ids);
            if (matrizen.Count > 0)
            {
                k.Ueberschrift2(MyResource.Resource.WIRT_MATRIX_TITEL);
                k.Hinweis(MyResource.Resource.WIRT_MATRIX_HERKUNFT);
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
        /// (Differenz zur Referenz in allen drei Szenarien + absolute kumulierte Barwerte
        /// je Version). Die Reihen werden aus den Berichtsdaten frisch gerechnet (ETAPPE E6:
        /// drei vollständige Läufe ohne Speichern, T aus den Parametern) — derselbe
        /// Rechenkern wie der Abschnitt „Verlauf" der Seite.</summary>
        private static WirtschaftlichkeitVerlaufSzenarien HoleVerlauf(WordKontext k, BerichtsDaten daten,
                                                                      WirtschaftlichkeitCtrl provider,
                                                                      WirtschaftlichkeitParameter p,
                                                                      TarifParameter tarifP)
        {
            // Konsistenz-Gate (Review 11): sind Tarif oder KWKG aktiv, hängen die
            // Zahlungsreihen an den Stundenreihen. Wurde der Bericht OHNE Zeitreihen
            // gesammelt (Baustein „Ergebnisse je Variante" abgewählt), würde das
            // Diagramm andere Zahlen zeigen als die Tabellen darüber → entfallen
            // lassen und offen begründen (keine stillen Widersprüche).
            // ETAPPE BK1: Der KWKG-Zweig fragt KwkgAktivierung — die EINE Regel, die
            // auch der Rechenkern zieht. Die Projektsätze entscheiden nicht mehr.
            // Q11 (E7b): nur ein WIRKSAMER Tarifsatz (Rollentarif) braucht die Reihen.
            bool zeitreihenNoetig = (tarifP != null && tarifP.Wirksam) ||
                                    KwkgAktivierung.IstAktiv(daten.IdStamm,
                                        daten.Varianten.Select(x => x.IdProjekt));
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
                return provider.BerechneVerlaufSzenarien(daten, p, p.Betrachtungszeitraum);
            }
            catch { return null; }
        }

        private static void SchreibeVerlauf(WordKontext k, WirtschaftlichkeitVerlaufSzenarien verlauf)
        {
            WirtschaftlichkeitVerlauf erwartet = verlauf == null
                ? null : verlauf.Lauf(WirtschaftlichkeitSzenario.ERWARTET);
            if (erwartet == null || erwartet.Absolut.All(s => s.Kumuliert == null)) return;

            k.Ueberschrift2("Kapitalwert-Verlauf über den Betrachtungszeitraum");
            k.HinweisRoh(MyResource.Resource.WIRT_VERL_WORT_HINWEIS);

            // ETAPPE E6 (Konzept § 2.13 (5), U13): das DREIERBILD — der kumulierte Barwert der
            // Differenz zur Referenz in allen drei Szenarien, Farbe = Variante, Strichart =
            // Szenario, die Legende zweigeteilt, der Nulldurchgang je Linie markiert. Es
            // tritt an die Stelle des Differenzbildes im Erwartungsfall: dessen Linien sind
            // die durchgezogenen des Dreierbildes. Das Bildmaß wächst mit der Legende; die
            // Anzeigegröße folgt ihm, damit nichts verzerrt.
            if (!verlauf.Leer)
            {
                ChartRenderer.VerlaufSzenarienTexte texte = ChartRenderer.VerlaufSzenarienTexte.AusRessourcen();
                Zeichnung.Zeichenmodell dreier = Sicher(() => ChartRenderer.KapitalwertSzenarienModell(
                    MyResource.Resource.WIRT_VERL_BILD,
                    ChartRenderer.VerlaufsReihenSzenarien(verlauf, texte), texte,
                    MyResource.Resource.WIRT_VERL_FUSS));
                if (dreier != null) k.Bild(dreier, 620, dreier.Hoehe / 2);

                // Dieselben Zeilen wie unter dem Bild der Seite (VerlaufZeilen).
                string nulldurchgaenge = VerlaufZeilen.Nulldurchgaenge(verlauf, null, null, k.Kultur);
                if (!string.IsNullOrEmpty(nulldurchgaenge)) k.HinweisRoh(nulldurchgaenge);
                string restwerte = VerlaufZeilen.Restwerte(verlauf, null, null, k.Kultur);
                if (!string.IsNullOrEmpty(restwerte)) k.HinweisRoh(restwerte);
            }
            else
                k.Hinweis("Differenzdiagramm entfällt — für das Stammprojekt konnte keine " +
                          "Zahlungsreihe gerechnet werden (siehe Hinweise am Kapitelende).");
            // AUFTRAG U18 (Anwenderentscheid 18.09.2026): Das zweite Verlaufsbild steht
            // an GENAU EINEM Ort — hier im Wortbericht. Seine Legende nennt jede Version
            // mit Namen und Farbe; die Stammlinie ist die Bezugsgröße und keine Version
            // und wird deshalb gestrichelt gezeichnet, damit sie auch im
            // Schwarz-Weiß-Ausdruck von den Versionen zu trennen ist. ETAPPE E6: Es zeigt
            // den Erwartungsfall, unverändert.
            //
            // ETAPPE E2 — DER VORBEHALT ZUM „EINEN ORT": Gemeint ist der BERICHT. Der
            // Excel-Bericht führt den Verlauf als ZAHLEN statt als Bild (Blatt
            // „Wirtschaftlichkeit" und Blatt „Verlauf"). „An genau einem Ort" heißt also:
            // EIN erzeugtes Bild im Berichtsweg, nicht „nirgends sonst im Programm".
            k.Bild(Sicher(() => ChartRenderer.KapitalwertVerlaufModell(
                "Kumulierte Barwerte je Version",
                ChartRenderer.VerlaufsReihen(erwartet.Absolut, true, true), null)),
                620, 310);
        }

        /// <summary>
        /// ETAPPE E8a (U41, Mockup Kategorie 8 „Von der Investition zur Kapitalwertdifferenz"):
        /// das <b>Brückenbild</b> — die Leitversion gegen die Referenz des Laufs im
        /// Erwartungsfall, je Bestandteil der Beitrag zur Kapitalwertdifferenz. Die Zahlen sind
        /// die Gliederungen der drei Läufe, die der Verlauf darüber zeichnet, abgeglichen gegen
        /// die Ergebnisse des Laufs (<see cref="Zahlungsgliederungen"/>); dieselbe Leitversion
        /// und dieselben Texte wie auf der Seite. Ohne Verlauf, ohne Leitversion oder ohne
        /// passende Gliederung entfällt die Bildstelle.
        /// </summary>
        private static void SchreibeBruecke(WordKontext k, BerichtsDaten daten,
                                            WirtschaftlichkeitVerlaufSzenarien verlauf,
                                            List<WirtschaftlichkeitErgebnis> alle,
                                            WirtschaftlichkeitParameter p,
                                            WirtschaftlichkeitBewertung bewertung)
        {
            if (verlauf == null || p == null || bewertung == null || bewertung.Bandbreite == null) return;
            Zahlungsgliederungen satz = Zahlungsgliederungen.Aus(verlauf, p, alle);
            int idReferenz = bewertung.Bandbreite.IdReferenz;
            int leit = Zahlungsgliederungen.Leitversion(alle, daten.Varianten.Select(v => v.IdProjekt), idReferenz);
            string erwartet = WirtschaftlichkeitSzenario.ERWARTET;
            Zahlungsgliederung stand = satz.Von(leit, erwartet), referenz = satz.Von(idReferenz, erwartet);
            if (leit == 0 || leit == idReferenz || stand == null || referenz == null) return;

            VariantenDaten v = daten.Varianten.FirstOrDefault(x => x.IdProjekt == leit);
            string name = v == null ? "" : (v.IstStamm ? "Stamm" : v.Anzeige);
            ChartRenderer.BrueckenTexte texte = ChartRenderer.BrueckenTexte.Fuer(
                name, bewertung.Bandbreite.Referenzname, MyResource.Resource.WIRT_SZEN_ERWARTET, stand, k.Kultur);
            Zeichnung.Zeichenmodell bild = Sicher(() => ChartRenderer.KapitalwertBrueckeModell(
                ChartRenderer.Brueckenschritt.Aus(stand, referenz), texte));
            if (bild == null) return;

            k.Ueberschrift2Roh(MyResource.Resource.WIRT_BR_TITEL);
            k.Bild(bild, 620, bild.Hoehe / 2);
        }

        /// <summary>
        /// Dieselbe Klammer wie in den Bausteinen „Ergebnisse" und „Vergleich": Ein
        /// Diagrammfehler lässt die Bildstelle aus, statt den ganzen Bericht zu
        /// reißen. <c>WordKontext.Bild</c> übergeht das <c>null</c>.
        /// </summary>
        private static Zeichnung.Zeichenmodell Sicher(Func<Zeichnung.Zeichenmodell> f)
        {
            try { return f(); } catch { return null; }
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
        ///
        /// <para><b>ETAPPE E8a (U42):</b> Über jeder Tafel steht ihr Zahlungsstrombild — die
        /// Positionsspalten als gestapelte Jahresbalken (<see cref="ChartRenderer.ZahlungsstromModell"/>).</para>
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

                // ETAPPE E8a (U42, Anwenderentscheid E8a‑Q1, Lesart a): das Zahlungsstrombild
                // über der Tafel — dieselben Spalten als gestapelte Jahresbalken, Ausgaben nach
                // unten, Ersatzjahre markiert; dasselbe Bild wie in Block 2 der Seite.
                Zeichnung.Zeichenmodell strom = Sicher(() => ChartRenderer.ZahlungsstromModell(
                    ChartRenderer.Zahlungsstromreihe.Aus(bild), ChartRenderer.Zahlungsstromreihe.Ersatzjahre(bild),
                    ChartRenderer.ZahlungsstromTexte.Fuer(v.Anzeige, MyResource.Resource.WIRT_SZEN_ERWARTET, k.Kultur)));
                if (strom != null) k.Bild(strom, 620, strom.Hoehe / 2);

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
            if (!vermieden) return;

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
                // ETAPPE E7c (E7c1-Q7): Rechnet ein Modul den zweiten Fall des § 2 Nr. 16
                // KWKG, kommen fünf Spalten dazu (Fall, σ, Nutzwärme, KWK-Strom, Kürzung) —
                // sonst bleibt die Tafel, wie sie war (Fall 1 überall, nichts zu zeigen).
                // Die Namensspalte gibt dann Breite ab; die übrigen bleiben gleich breit.
                bool mitFall2 = KwkgFall2Spalten.Noetig(e.KwkgModule);
                int spalten = mitFall2 ? 16 : 11;
                int wName = mitFall2 ? 1255 : 1655;
                int wCol = (WordBerichtGenerator.INHALT_B - wName) / (spalten - 1);
                var w = new List<int> { wName };
                for (int i = 0; i < spalten - 1; i++) w.Add(wCol);

                var kopfTexte = new List<string>
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
                if (mitFall2) kopfTexte.AddRange(KwkgFall2Spalten.Kopf());

                Table t = k.NeueTabelle(w.ToArray());
                var kopf = new TableRow();
                for (int i = 0; i < kopfTexte.Count; i++)
                    kopf.Append(k.Zelle(kopfTexte[i], w[i], true, WordBerichtGenerator.HEAD_FILL,
                                        i == 0 ? JustificationValues.Left : JustificationValues.Center,
                                        false, WordBerichtGenerator.SCHRIFT_TABELLE_SCHMAL));
                t.Append(kopf);

                foreach (KwkgModulNachweis m in e.KwkgModule)
                {
                    var werte = new List<string>
                    {
                        m.Bezeichner,
                        k.F(m.PelKW, 0),
                        k.F(m.VbhElektrisch, 0),
                        // AUFTRAG #351 (U26): dieselbe Stellenzahl wie das Satzfeld
                        // des Dialogs — ein Format, das im Kern steht.
                        k.F(m.SatzEigenCt, KwkgSatzHerkunft.NACHKOMMASTELLEN),
                        k.F(m.SatzEinspeisungCt, KwkgSatzHerkunft.NACHKOMMASTELLEN),
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
                    if (mitFall2) werte.AddRange(KwkgFall2Spalten.Werte(m, k.Kultur));
                    var tr = new TableRow();
                    for (int i = 0; i < werte.Count; i++)
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

        /// <summary>
        /// KONZEPT § 2.15 — die Spaltenblöcke der Kennzahltafel. In Sicht 2 ist es
        /// GENAU EIN Block mit A und B; die Blockteilung samt wiederholter Stammspalte
        /// gilt nur für Sicht 1, wo beliebig viele Stände nebeneinander stehen können.
        /// </summary>
        private static List<List<VariantenDaten>> Bloecke(WordKontext k, BerichtsDaten daten)
        {
            if (daten.Sicht == null || !daten.Sicht.IstPaar) return k.VariantenBloecke(daten);

            var paar = new List<VariantenDaten>();
            foreach (int id in daten.Sicht.Spalten(null))
            {
                VariantenDaten v = daten.Varianten.FirstOrDefault(x => x.IdProjekt == id);
                if (v != null) paar.Add(v);
            }
            return new List<List<VariantenDaten>> { paar };
        }

        private static void SchreibeVergleich(WordKontext k, BerichtsDaten daten,
                                              List<WirtschaftlichkeitErgebnis> alle, string szenario,
                                              TarifParameter tarif)
        {
            // ETAPPE E7: EINE Zeilendefinition für Word, Excel und Ergebnisreiter.
            // Bis dahin stand dieselbe Liste dreimal im Code; die Zahlen liefen nicht
            // auseinander, das Drumherum aber schon.
            // ETAPPE B7: Die SICHTBARKEIT entscheidet seither dieselbe Regel wie im
            // Reiter und im Excel-Blatt (WirtschaftlichkeitZeilen.Sichtbare) — bis
            // dahin filterte Word gar nicht, der Reiter über die gewählten Spalten und
            // Excel über den Szenarioblock. Drei Regeln, drei mögliche Tabellen.
            // KONZEPT § 2.9 und § 2.15 (VG‑Q4): Der BERICHT FOLGT DER SICHT — so wie er
            // den Häkchen folgt. In Sicht 2 druckt er A | B mit A als Referenz und die
            // Deklarationszeile darüber; in Sicht 1 alle Stände gegen die Referenz der
            // Gruppe. Beides entsteht aus DERSELBEN Zeilendefinition; einen zweiten
            // Zeilenkatalog gibt es nicht.
            int idReferenz = daten.Sicht != null && daten.Sicht.IstPaar
                           ? daten.Sicht.IdA : daten.IdGruppenreferenz;
            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Sichtbare(
                WirtschaftlichkeitZeilen.Kennzahlen(alle, tarif, idReferenz), alle);

            VariantenDaten stamm = daten.Varianten.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return;

            // Welche SPALTE die Referenzhinterlegung trägt — die gewählte Referenz,
            // sonst wie bisher der Stamm.
            int idRefSpalte = idReferenz > 0 ? idReferenz : stamm.IdProjekt;

            foreach (List<VariantenDaten> block in Bloecke(k, daten))
            {
                var spalten = new List<VariantenDaten>();
                if (daten.Sicht != null && daten.Sicht.IstPaar) spalten.AddRange(block);
                else { spalten.Add(stamm); spalten.AddRange(block); }
                if (spalten.Count == 0) continue;

                if (daten.Sicht != null && daten.Sicht.IstPaar)
                    k.HinweisRoh(Referenzwahl.Deklarationszeile(
                        Referenzwahl.Name(spalten[0]),
                        Referenzwahl.Name(daten.Varianten.FirstOrDefault(
                            v => daten.IdGruppenreferenz > 0
                               ? v.IdProjekt == daten.IdGruppenreferenz : v.IstStamm))));

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
                    //
                    // ETAPPE B7: Die Rubrik trägt Überschriften, Unterzeilen und eine
                    // Summe. Word hat kein Aufklappmuster, also steht der Einzug im
                    // Text („    davon …") und die Überschrift fett — dieselbe Ordnung
                    // wie im Reiter, mit den Mitteln der Tabelle.
                    //
                    // ETAPPE E5 (V‑A, Entscheid V‑3, Q3): Amortisation und Zinsfuß tragen
                    // das Label „nachrichtlich (Anhang C)" am Titel — dieselbe Einordnung
                    // wie Kachel und Kennzahltafel der Seite. Die Zahl bleibt, wie sie ist.
                    string titel = (z.Einzug > 0 ? "    " : "") + z.Titel +
                                   (z.Nachrichtlich ? " — " + ValeriAusweis.NachrichtlichLabel() : "");
                    tr.Append(k.Zelle(titel, w[0], z.IstUeberschrift || z.IstSumme,
                                      z.IstUeberschrift ? WordBerichtGenerator.HEAD_FILL : null,
                                      JustificationValues.Left,
                                      false, WordBerichtGenerator.SCHRIFT_TABELLE));
                    for (int i = 0; i < spalten.Count; i++)
                    {
                        WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(x =>
                            x.IdProjekt == spalten[i].IdProjekt && x.Szenario == szenario);
                        string txt = z.IstUeberschrift ? "" : z.Anzeige(e, k.Kultur);
                        tr.Append(k.Zelle(txt, w[i + 1], z.IstSumme,
                            z.IstUeberschrift ? WordBerichtGenerator.HEAD_FILL
                            : spalten[i].IdProjekt == idRefSpalte ? WordBerichtGenerator.STAMM_FILL : null,
                            z.IstText ? JustificationValues.Left
                                      : (txt == "—" ? JustificationValues.Center : JustificationValues.Right),
                            false, WordBerichtGenerator.SCHRIFT_TABELLE));
                    }
                    t.Append(tr);
                }
                k.Fuege(t);

                // ETAPPE E5 (V‑A, Befund A2): die WARNUNG einer Zelle — heute die
                // Mehrdeutigkeit des Zinsfußes bei mehr als einem Vorzeichenwechsel. Die
                // Zelle bleibt die Zahl; die Warnung steht unter der Tafel, je Stand einmal.
                foreach (WirtZeile z in zeilen)
                    for (int i = 0; i < spalten.Count; i++)
                    {
                        WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(x =>
                            x.IdProjekt == spalten[i].IdProjekt && x.Szenario == szenario);
                        string warnung = z.Warnung(e);
                        if (string.IsNullOrEmpty(warnung)) continue;
                        k.HinweisRoh("⚠ " + (spalten[i].IstStamm ? "Stamm" : spalten[i].Anzeige) +
                                     " — " + z.Titel + ": " + warnung);
                    }
                k.Beschriftung(" ");
            }
        }

        /// <summary>
        /// Strommengen-Matrix: je Projekt eine Tabelle mit der Jahreszeile × Mengenart
        /// [MWh]. Q11 (E7b): Die vier Zeilen der Tarifzonen Winter/Sommer × HT/NT sind
        /// entfallen — es gibt keinen Zeitzonentarif mehr, also auch keine Zone, die eine
        /// Menge trüge.
        /// </summary>
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
                kopf.Append(k.Zelle(MyResource.Resource.WIRT_MATRIX_ZEITRAUM, w[0], true,
                                    WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
                kopf.Append(k.Zelle(MyResource.Resource.WIRT_MATRIX_BEDARF, w[1], true,
                                    WordBerichtGenerator.HEAD_FILL, JustificationValues.Center,
                                    false, WordBerichtGenerator.SCHRIFT_TABELLE));
                kopf.Append(k.Zelle("Netzbezug [MWh]", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("PV-Einspeisung [MWh]", w[3], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("KWK-Eigenstrom [MWh]", w[4], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("KWK-Einspeisung [MWh]", w[5], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                t.Append(kopf);

                var tr = new TableRow();
                tr.Append(k.Zelle(MyResource.Resource.WIRT_MATRIX_JAHR, w[0], false, null, JustificationValues.Left));
                tr.Append(k.Zelle(k.F(m.BedarfGesamtMWh, 1), w[1], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(k.F(m.BezugGesamtMWh, 1), w[2], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(k.F(m.EinspeisungPvGesamtMWh, 1), w[3], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(k.F(m.KwkEigenGesamtMWh, 1), w[4], false, null, JustificationValues.Right));
                tr.Append(k.Zelle(k.F(m.KwkEinspeisungGesamtMWh, 1), w[5], false, null, JustificationValues.Right));
                t.Append(tr);
                k.Fuege(t);
                k.Hinweis(string.Format(k.Kultur, MyResource.Resource.WIRT_MATRIX_STUNDENSPITZE,
                                        k.F(m.MaxBezugKW, 0)));
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

        /// <summary>
        /// Sensitivitätstabellen: je Stand außer der Referenz die Parameterzeilen
        /// (−Δ · Basis · +Δ → KW) und — ETAPPE E5 Teil b (V‑A, V‑G6) — die
        /// <b>Steigung</b> je %-Punkt bzw. je %. Die Zeilen kommen aus der Bewertung des
        /// Laufs (<see cref="WirtschaftlichkeitBewertung.Sensitivitaet"/>); welche Stände
        /// Zeilen tragen, sagt sie — ist eine Variante die Referenz, auch der Stamm.
        /// </summary>
        private static void SchreibeSensitivitaet(WordKontext k, BerichtsDaten daten,
                                                  List<SensitivitaetZeile> sens)
        {
            foreach (VariantenDaten v in daten.Varianten)
            {
                List<SensitivitaetZeile> zeilen = sens.Where(x => x.IdProjekt == v.IdProjekt).ToList();
                if (zeilen.Count == 0) continue;

                k.Ueberschrift3((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);

                int wLabel = 3300;
                int wCol = (WordBerichtGenerator.INHALT_B - wLabel) / 4;
                int[] w = { wLabel, wCol, wCol, wCol, wCol };

                Table t = k.NeueTabelle(w);
                var kopf = new TableRow();
                kopf.Append(k.Zelle("Parameter", w[0], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Left));
                kopf.Append(k.Zelle("KW bei −Δ [€]", w[1], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("KW Basis [€]", w[2], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(k.Zelle("KW bei +Δ [€]", w[3], true, WordBerichtGenerator.HEAD_FILL, JustificationValues.Center));
                kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_SENS_SP_STEIGUNG, w[4], JustificationValues.Center));
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
                    // Die Steigung ist eine Ableitung der beiden Randwerte; ohne stetige
                    // Stufe (Wegfall des KWKG-Zuschlags) gibt es keine — dann „—".
                    string steigung = z.Steigung.HasValue
                        ? k.FW(z.Steigung, "N2") + " " + z.SteigungEinheit : "—";
                    tr.Append(k.Zelle(steigung, w[4], false, null,
                        steigung == "—" ? JustificationValues.Center : JustificationValues.Right,
                        false, WordBerichtGenerator.SCHRIFT_TABELLE));
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
        ///
        /// <para><b>ETAPPE E5 Teil b:</b> Die Tafel entsteht aus der Bandbreite der
        /// Bewertung (<see cref="WirtschaftlichkeitBewertung.Bandbreite"/>) — dieselben
        /// Zeilen, Spannen (Betrag aus größtem und kleinstem Wert, Q4) und Einstufungen
        /// wie die Karten und die Bandbreite der Seite. Unter den Annahmen steht der
        /// Hinweistext (U10), dann der Vorschlag derselben Bewertung.</para>
        /// </summary>
        private static void SchreibeSzenarien(WordKontext k, WirtschaftlichkeitBewertung bewertung,
                                              WirtschaftlichkeitParameter p)
        {
            // ETAPPE E2 (G8/G9): die REFERENZ der Gruppe — in der Paarsicht der Stand A,
            // sonst die Gruppenreferenz, ohne Wahl der Stamm. Sie bekommt eine eigene
            // Zeile und steht deshalb nicht noch einmal in der Variantenliste. Seit E5
            // Teil b löst sie die Bewertung auf, nicht dieser Baustein.
            WirtschaftlichkeitBandbreite band = bewertung.Bandbreite ?? new WirtschaftlichkeitBandbreite();
            if (band.Leer)
            {
                k.Hinweis("Keine Varianten ausgewählt — die Szenarienübersicht entfällt.");
                return;
            }

            // W5‑B‑11: sechs Spalten statt fünf; ETAPPE E2 (G8): sieben mit der Spanne.
            // Die Summe bleibt INHALT_B — die Beschriftungsspalte gibt die Breite ab,
            // die Einstufung und Spanne brauchen.
            int wLabel = 2100;
            int wCol = (WordBerichtGenerator.INHALT_B - wLabel) / 6;
            int[] w = { wLabel, wCol, wCol, wCol, wCol, wCol, wCol };

            Table t = k.NeueTabelle(w);
            var kopf = new TableRow();
            kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_SZ_SP_VARIANTE, w[0], JustificationValues.Left));
            kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_SZ_SP_WORST, w[1], JustificationValues.Center));
            kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_SZ_SP_ERWARTET, w[2], JustificationValues.Center));
            kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_SZ_SP_BEST, w[3], JustificationValues.Center));
            kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_SZ_SP_SPANNE, w[4], JustificationValues.Center));
            kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_SZ_SP_AMORT, w[5], JustificationValues.Center));
            kopf.Append(Kopfzelle(k, MyResource.Resource.WIRT_EMPF_SPALTE, w[6], JustificationValues.Center));
            t.Append(kopf);

            // ---- ETAPPE E2 (G8): die REFERENZZEILE --------------------------------
            //
            // Sie nennt den Stand, gegen den jede Δ-Zahl der Tabelle gerechnet ist. Ohne
            // sie musste der Leser aus der Fußzeile erschließen, welcher Stand fehlt —
            // und seit § 2.9 ist das nicht mehr zwingend der Stamm. In ihren eigenen
            // Δ-Spalten steht „(Referenz)", dieselbe Anzeige wie in der
            // Kennzahlentabelle (WirtZeile.StammAnzeige) — eine 0 wäre eine gerechnete
            // Zahl, und gerechnet ist hier nichts.
            var refZeile = new TableRow();
            refZeile.Append(k.Zelle(band.Referenzname, w[0], true,
                                    WordBerichtGenerator.STAMM_FILL, JustificationValues.Left));
            for (int i = 1; i <= 4; i++)
                refZeile.Append(k.Zelle(MyResource.Resource.WIRT_ZEILE_STAMM_REFERENZ, w[i], false,
                                        WordBerichtGenerator.STAMM_FILL, JustificationValues.Center));
            for (int i = 5; i <= 6; i++)
                refZeile.Append(k.Zelle("—", w[i], false,
                                        WordBerichtGenerator.STAMM_FILL, JustificationValues.Center));
            t.Append(refZeile);

            foreach (BandbreitenZeile z in band.Zeilen)
            {
                var tr = new TableRow();
                tr.Append(k.Zelle(z.Anzeige, w[0], false, null, JustificationValues.Left));
                int spalte = 1;
                foreach (double? wert in new[] { z.Worst, z.Erwartet, z.Best })
                {
                    string txt = k.FW(wert, "N0");
                    tr.Append(k.Zelle(txt, w[spalte], false, null,
                        txt == "—" ? JustificationValues.Center : JustificationValues.Right));
                    spalte++;
                }

                // ETAPPE E2 (G8), E5 (Q4): die SPANNE des Modells — der Betrag aus größtem
                // und kleinstem Szenariowert. Fehlt Worst oder Best, bleibt sie „—": Eine
                // Spanne aus einer Zahl gibt es nicht.
                string sp = k.FW(z.Spanne, "N0");
                tr.Append(k.Zelle(sp, w[4], false, null,
                    sp == "—" ? JustificationValues.Center : JustificationValues.Right));

                string am = k.FW(z.AmortisationJahre, "N1");
                tr.Append(k.Zelle(am, w[5], false, null,
                    am == "—" ? JustificationValues.Center : JustificationValues.Right));

                // W5‑B‑11 (G9), E5 (U5): die Einstufung — derselbe Text wie auf der Karte.
                // „—", solange kein Erwartet-Ergebnis vorliegt — ein Urteil ohne Zahl gibt
                // es nicht.
                VariantenEmpfehlung u = z.Urteil;
                tr.Append(k.Zelle(u == null ? "—" : u.StufeText, w[6], false, null,
                    u == null ? JustificationValues.Center : JustificationValues.Left));
                t.Append(tr);
            }
            k.Fuege(t);
            k.HinweisRoh(string.Format(k.Kultur, MyResource.Resource.WIRT_SZ_DELTA_FUSS,
                                       band.Referenzname));

            // ---- ETAPPE E6 (Nachtrag E5b, Frage (4)): das SPANNENBILD neben der Tafel ----
            // Dieselbe Bandbreite als Balken je Version (Mockup valeri-f2): der Balken vom
            // kleinsten bis zum größten Szenariowert, der Erwartungsfall als Punkt, die
            // Referenz als Nulllinie — dasselbe Modell wie auf der Seite. Die Anzeigegröße
            // folgt der Bildhöhe, die mit den Versionen wächst.
            Zeichnung.Zeichenmodell spanne = Sicher(() => ChartRenderer.KapitalwertSpanneModell(
                ChartRenderer.Spannenbalken.Aus(band), band.Referenzname,
                ChartRenderer.SpannenTexte.AusRessourcen()));
            if (spanne != null) k.Bild(spanne, 620, spanne.Hoehe / 2);

            // ---- W5‑B‑11 (G8): die ANNAHMEN der Bandbreite, je Szenario eine Zeile ----
            //
            // Der Nachweis nennt den WIRKSAMEN Satz (i, p_E, p_B, Investition, Erträge,
            // Nutzungsdauer) und seine Herkunft: „Vorgaben", solange niemand ein Feld
            // gepflegt hat, sonst „gepflegte Werte". Dieselben Ressourcen wie Dialog und
            // Seite — drei Formulierungen derselben Auskunft wären drei Wahrheiten.
            SchreibeSzenarioAnnahmen(k, p);

            // ---- ETAPPE E5 (U10): der Hinweistext unter den Annahmen -------------------
            // Was ein Szenario heute variiert und was nicht — derselbe Text wie unter der
            // Annahmentafel der Seite.
            if (!string.IsNullOrEmpty(bewertung.Szenariohinweis))
                k.HinweisRoh(bewertung.Szenariohinweis);

            // ---- W5‑B‑11 (G9): der Vorschlag zur Entscheidung ----------------------
            // ETAPPE E2: Er nennt die REFERENZ beim Namen. Seit § 2.9 rechnet ΔKW gegen
            // die gewählte Referenz, nicht mehr fest gegen den Stamm — „gegenüber dem
            // Stammprojekt" war bei gewählter Variantenreferenz schlicht falsch. Seit E5
            // Teil b ist es der Satz der Bewertung — derselbe wie auf der Seite.
            if (!string.IsNullOrEmpty(bewertung.Vorschlagstext)) k.TextRoh(bewertung.Vorschlagstext);
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
        ///
        /// <para><b>ETAPPE E5 Teil b:</b> Zeitraumzeile und die Hinweiszeilen „k von n
        /// Positionen ohne Nutzungsdauer" (U39) kommen aus der Bewertung — derselbe
        /// Kern-Controller wie auf der Seite. Unter den Vereinfachungen stehen die
        /// Deklarationen der Bewertung nach DIN EN 17463 (V‑A) und, wo Ergebniszeilen
        /// keinen Nachweis tragen, die Nr.-31-Zeile.</para>
        /// </summary>
        private static void SchreibeValeriAusweise(WordKontext k, BerichtsDaten daten,
                                                   WirtschaftlichkeitCtrl provider,
                                                   WirtschaftlichkeitBewertung bewertung)
        {
            NutzungsdauerHinweise nutzungsdauer = bewertung.Nutzungsdauer ?? new NutzungsdauerHinweise();
            if (!string.IsNullOrEmpty(nutzungsdauer.Zeitraumzeile)) k.HinweisRoh(nutzungsdauer.Zeitraumzeile);
            foreach (string zeile in nutzungsdauer.Zeilen) k.HinweisRoh(zeile);

            try
            {
                WirtschaftlichkeitCtrl.ErzeugerFlags flags = provider.ErzeugerDerGruppe(daten.IdStamm);
                if (flags != null && flags.Photovoltaik)
                    k.HinweisRoh(ValeriAusweis.EigennutzungHerleitung());
            }
            catch { }

            k.HinweisRoh(ValeriAusweis.Vereinfachungen());

            // ETAPPE E5 (V‑A): die Deklarationen in EINER Zeile — nominal · Steuern ·
            // Restwert · Risiko, die Risikozeile nach Q5 mit „keine benannt", wenn kein
            // Text gepflegt ist.
            var deklarationen = new List<string>();
            if (bewertung.Deklarationen != null)
                foreach (ValeriDeklaration d in bewertung.Deklarationen)
                    if (d != null && !string.IsNullOrEmpty(d.Text)) deklarationen.Add(d.Text);
            if (deklarationen.Count > 0) k.HinweisRoh(string.Join(" · ", deklarationen.ToArray()));

            // ETAPPE E5 (Nr. 31): Stände, deren Zeilen keinen Nachweisumschlag tragen.
            string nachweis = WirtschaftlichkeitBewertung.Nachweiszeile(bewertung.OhneNachweis);
            if (!string.IsNullOrEmpty(nachweis)) k.HinweisRoh(nachweis);
        }
    }
}
