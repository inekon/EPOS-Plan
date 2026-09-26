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
    ///
    /// <para><b>ETAPPE BV-E3 (Konzept Berichtsvorlagen 5.1): Er liest nur noch den Wertesatz.</b>
    /// Parameter, Tarif, Nachweiszeilen, Bilanzkonvention, Aktualität, Kennzahltafel, KWKG-Lage,
    /// Verlauf, Szenarioannahmen, Strommatrix, Referenzkessel, Erzeuger und Emissionsbilanz stehen in
    /// <see cref="BerichtsDaten.Wirtschaft"/> — ermittelt vom Sammler über dieselben Aufrufe, die bis
    /// hierher in diesem Baustein standen. Beim Schreiben wird die Datenbank nicht berührt.</para>
    /// </summary>
    public class WirtschaftlichkeitBaustein : IBerichtsBaustein
    {
        public string Schluessel { get { return BerichtsKonfiguration.B_WIRTSCHAFT; } }
        public string Titel { get { return "Wirtschaftlichkeit"; } }

        /// <summary>Die Überschrift des Kapitels (Überschrift 1) — Schlüssel der Übersetzung in
        /// <see cref="BerichtTexte"/>; dieselbe Quelle hat <see cref="Berichtskapitel.Ueberschrift"/>.</summary>
        public const string UEBERSCHRIFT = "Wirtschaftlichkeit";

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            k.Ueberschrift1(UEBERSCHRIFT);

            // BV-E3: der Wertesatz des Laufs — ohne Sammler (Proben) bildet ihn Von Teil für Teil.
            WirtschaftsBerichtswerte werte = WirtschaftsBerichtswerte.Von(daten);

            // Quelle sind die Zahlen DIESES Berichtslaufs; der persistierte Stand ist
            // nur das Rückfallnetz, falls die Rechnung des Laufs scheiterte.
            bool ausDiesemLauf = werte.AusDiesemLauf;
            List<WirtschaftlichkeitErgebnis> alle = werte.Ergebnisse;

            if (alle.Count == 0)
            {
                k.Hinweis(TextOhneErgebnis(daten));
                return;
            }
            if (!ausDiesemLauf)
                k.Hinweis(TextRueckfall(daten));

            // ---------------- Methodik + Parameternachweis (Normanforderung) ----------------
            WirtschaftlichkeitParameter p = werte.Parameter;

            // ETAPPE E5 Teil b: die BEWERTUNG dieses Laufs — Bandbreite mit Einstufungen,
            // Vorschlag, Hinweistext, Deklarationen, Nutzungsdauer-Hinweise, Stände ohne
            // Nachweis und Sensitivität. Der Sammler legt sie an den Baum; wer den Baustein
            // ohne Sammler ruft (Proben, Rückfall), bekommt sie aus denselben Kernmethoden
            // (BV-E3: über den Wertesatz). Word bildet keine dieser Tafeln mehr selbst.
            WirtschaftlichkeitBewertung bewertung = werte.Bewertung;
            k.Text(METHODIK);
            k.Hinweis(Parameterzeile(werte, k.Kultur));

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
            SchreibeValeriAusweise(k, daten, werte, bewertung);

            // Aktualität gegen den Simulationsstand prüfen. Nach der verbindlichen
            // Kette (Simulation → Wirtschaftlichkeit) darf hier nichts mehr auflaufen;
            // die Prüfung bleibt als Netz, falls doch etwas dazwischenkam.
            string veraltet = TextVeraltet(daten, werte, null);
            if (veraltet != null) k.HinweisRoh(veraltet);

            // ---------------- Vergleichstabelle (Szenario Erwartet) ----------------
            k.Ueberschrift2("Kennzahlen im Szenario „Erwartet“");
            // ETAPPE E7: Der Zeitbezug steht im Tabellenkopf statt in vier von
            // zweiundzwanzig Zeilentiteln — erst dadurch passt derselbe Schlüssel in
            // Kennzahlen- UND Mehrjahrestabelle.
            k.HinweisRoh(MyResource.Resource.WIRT_ZEILE_JAHR1);
            SchreibeVergleich(k, daten, alle, WirtschaftlichkeitSzenario.ERWARTET, werte);

            // ---------------- KWK-Zuschlag je Modul (E6 → E7) ----------------
            SchreibeKwkgModule(k, daten, alle);

            // ---------------- Betriebskosten nach Kostenarten (E3 → E7) ----------------
            SchreibeBetriebskosten(k, daten, alle);

            // ---------------- Kapitalwert-Verlauf + Mehrjahresübersicht ----------------
            // ETAPPE E7: Beide Blöcke leben von derselben Verlaufsrechnung; sie läuft
            // deshalb genau einmal. ETAPPE E6: Sie rechnet alle drei Szenarien (drei
            // vollständige Läufe ohne Speichern); die Mehrjahrestabelle nimmt daraus den
            // Erwartungsfall — Zahl für Zahl der bisherige Einzellauf.
            WirtschaftlichkeitVerlaufSzenarien verlauf = HoleVerlauf(k, werte);
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
            SchreibeSzenarien(k, bewertung, p, daten, werte);

            // ---- ETAPPE W5‑B‑12 (VALERI-Lücke G6): die nicht monetären Wirkungen ----
            //
            // Unmittelbar NACH dem Vorschlag zur Entscheidung: Erst die Zahl mit ihrer
            // Bandbreite und der Empfehlung, dann das, was die Zahl nicht fassen kann.
            // DIN EN 17463 verlangt beides nebeneinander.
            //
            // OHNE GEPFLEGTE WIRKUNG ENTFÄLLT DER GANZE BLOCK — Überschrift eingeschlossen.
            // Eine leere Überschrift wäre keine Aussage, sondern eine Lücke mit Titel.
            //
            // ETAPPE E17 (V‑G11, DIN EN 17463 6.1 und 8.2): statt des Freitexts die TABELLE
            // „Nicht monetarisierbare Wirkungen" — Kategorie, Beschreibung, Dauer, Wirkung auf
            // Organisation, Mitarbeiter und Umwelt, Beurteilung. Quelle ist die Wirkungsliste
            // der Bewertung (Stammprojekt); keine Zahl daraus fließt in den Kapitalwert.
            SchreibeNichtMonetaereWirkungen(k, bewertung.Wirkungen);

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
            Dictionary<int, StromMatrix> matrizen = werte.Strommatrizen;
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
                ReferenzkesselInfo rk = werte.Referenzkessel;
                k.Hinweis("Referenz (getrennt): dieselbe Brennstoff-Wärme im Referenzkessel (η = " +
                          p.RefKesselWirkungsgrad.ToString("N0", k.Kultur) + " %" +
                          (rk != null && rk.Gefunden
                           ? ", aus dem Stammprojekt: " + rk.Bezeichner +
                             (rk.BrennstoffName.Length > 0 ? ", " + rk.BrennstoffName : "")
                           : ", Vorgabewert — kein Heizkessel im Stammprojekt") +
                          ") und derselbe KWK-Strom im Kraftwerkspark, inkl. Netzverluste " +
                          "(Konzept Kap. 2.8).");
                SchreibeEmissionsbilanz(k, daten, p, alle, werte);
            }

            // Unvollständige Rechnungen ausweisen (keine stillen Lücken).
            foreach (string zeile in Rechnungszeilen(daten, alle, null, true, true)) k.Hinweis(zeile);
        }

        // ------------------------------------------------------------- Texte (BV-E4)
        //
        // Die Sätze, die dieser Baustein schreibt und die der Platzhalterkatalog als Einzelwerte führt
        // (wirtschaft.methodik, wirtschaft.parameternachweis, wirtschaft.warnungen, …) — EINE Stelle für
        // beide, damit Kapitel und Platzhalter denselben Wortlaut tragen (Konzept Berichtsvorlagen 4.11).

        /// <summary>Der Methodiksatz (Normanforderung) — zugleich Schlüssel der Übersetzung.</summary>
        internal const string METHODIK =
            "Bewertung nach der Kapitalwertmethode in Anlehnung an DIN EN 17463 (ValERI): " +
            "alle Zahlungsströme der Projekte werden über den Betrachtungszeitraum auf den " +
            "Entscheidungszeitpunkt abgezinst. Referenz (Unterlassensalternative) ist das " +
            "Stammprojekt — der Kapitalwert einer Variante ist der Barwert der Differenz-" +
            "Zahlungsströme Variante − Stamm; ein positiver Wert bedeutet: die Variante ist " +
            "über den Betrachtungszeitraum wirtschaftlicher als der Stamm.";

        /// <summary>Der Satz, wenn für die Gruppe keine Wirtschaftlichkeit vorliegt.</summary>
        internal static string TextOhneErgebnis(BerichtsDaten daten)
        {
            return "Für diese Vergleichsgruppe konnte keine Wirtschaftlichkeit berechnet " +
                   "werden" +
                   (daten.WirtschaftlichkeitFehler != null
                    ? " (" + daten.WirtschaftlichkeitFehler + ")" : "") +
                   ". Kostenpositionen (Tab_ProjektWerte) und die Parameter im Bereich " +
                   "Berichte & Kosten → Wirtschaftlichkeit prüfen.";
        }

        /// <summary>Der Satz beim Rückfall auf den gespeicherten Stand.</summary>
        internal static string TextRueckfall(BerichtsDaten daten)
        {
            return "⚠ Die Wirtschaftlichkeitsrechnung dieses Berichtslaufs ist " +
                   "fehlgeschlagen" +
                   (daten.WirtschaftlichkeitFehler != null
                    ? " (" + daten.WirtschaftlichkeitFehler + ")" : "") +
                   " — gezeigt wird der zuletzt gespeicherte Stand.";
        }

        /// <summary>
        /// Die Nachweiszeile „Parameter dieses Rechenlaufs: …“ samt Rechenstand. Nur mit Ergebnissen fragen.
        /// </summary>
        internal static string Parameterzeile(WirtschaftsBerichtswerte werte, System.Globalization.CultureInfo kultur)
        {
            WirtschaftlichkeitParameter p = werte.Parameter;
            List<WirtschaftlichkeitErgebnis> alle = werte.Ergebnisse;
            TarifParameter tarifP = werte.Tarif;
            // LEITENTSCHEIDUNGEN L12/L13 — der Ausweis der Bilanzierungsregeln gehört in
            // dieselbe Nachweiszeile: Er sagt, nach welchem Rechtsstand die Emissionen
            // bewertet sind und mit welcher Konvention die Biomasse.
            return "Parameter dieses Rechenlaufs: " + werte.Parameternachweis(kultur) +
                   " · " + tarifP.Nachweis(kultur) +
                   " · " + werte.Bilanzkonvention.Ausweis(kultur) +
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
                   "Rechenstand: " + alle[0].Zeitstempel.ToString("dd.MM.yyyy HH:mm", kultur) + ".";
        }

        /// <summary>Die Warnung einer Zelle der Kennzahltafel: „⚠ Name — Zeile: Warnung“.</summary>
        internal static string Zellwarnung(VariantenDaten v, WirtZeile z, string warnung)
        {
            return "⚠ " + (v.IstStamm ? "Stamm" : v.Anzeige) + " — " + z.Titel + ": " + warnung;
        }

        /// <summary>Der Name eines Stands in den Hinweisen: „Stamm“ oder sein Anzeigename.</summary>
        internal static string Standname(BerichtsDaten daten, int idProjekt)
        {
            VariantenDaten v = daten.Varianten.FirstOrDefault(x => x.IdProjekt == idProjekt);
            return v == null ? ("Projekt " + idProjekt) : (v.IstStamm ? "Stamm" : v.Anzeige);
        }

        /// <summary>
        /// Der Satz „Ergebnis veraltet …“ über die Stände, deren Ergebnis „Erwartet“ fehlt oder nicht zum
        /// Simulationslauf passt; <c>null</c> = keiner. <paramref name="nur"/> schränkt auf einen Stand ein.
        /// </summary>
        internal static string TextVeraltet(BerichtsDaten daten, WirtschaftsBerichtswerte werte, VariantenDaten nur)
        {
            List<WirtschaftlichkeitErgebnis> alle = werte.Ergebnisse;
            var veraltet = new List<string>();
            foreach (VariantenDaten v in daten.Varianten)
            {
                if (nur != null && v.IdProjekt != nur.IdProjekt) continue;
                WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(x =>
                    x.IdProjekt == v.IdProjekt && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                if (e == null || (e.Fehlgrund == null && !werte.ErgebnisAktuell(e)))
                    veraltet.Add(v.IstStamm ? "Stamm" : v.Anzeige);
            }
            return veraltet.Count == 0 ? null
                : string.Format(MyResource.Resource.WIRT_ERGEBNIS_VERALTET, string.Join(", ", veraltet));
        }

        /// <summary>
        /// Die Zeilen „⚠ Name: Fehlgrund“ und „⚠ Name: Hinweis“ der Ergebnisse „Erwartet“ (unvollständige
        /// Rechnungen, keine stillen Lücken); <paramref name="nur"/> schränkt auf einen Stand ein.
        /// </summary>
        internal static List<string> Rechnungszeilen(BerichtsDaten daten, List<WirtschaftlichkeitErgebnis> alle,
                                                     VariantenDaten nur, bool fehlgruende, bool hinweise)
        {
            var zeilen = new List<string>();
            foreach (WirtschaftlichkeitErgebnis e in alle.Where(x =>
                         x.Szenario == WirtschaftlichkeitSzenario.ERWARTET &&
                         (x.Fehlgrund != null || x.Hinweis != null)))
            {
                if (nur != null && e.IdProjekt != nur.IdProjekt) continue;
                string name = Standname(daten, e.IdProjekt);
                if (fehlgruende && e.Fehlgrund != null) zeilen.Add("⚠ " + name + ": " + e.Fehlgrund);
                if (hinweise && e.Hinweis != null) zeilen.Add("⚠ " + name + ": " + e.Hinweis);
            }
            return zeilen;
        }

        // ------------------------------------------------------------- Verlauf (Phase 11)

        /// <summary>Kapitalwert-Verlauf über den Betrachtungszeitraum als Diagramme
        /// (Differenz zur Referenz in allen drei Szenarien + absolute kumulierte Barwerte
        /// je Version). Die Reihen werden aus den Berichtsdaten frisch gerechnet (ETAPPE E6:
        /// drei vollständige Läufe ohne Speichern, T aus den Parametern) — derselbe
        /// Rechenkern wie der Abschnitt „Verlauf" der Seite.</summary>
        private static WirtschaftlichkeitVerlaufSzenarien HoleVerlauf(WordKontext k, WirtschaftsBerichtswerte werte)
        {
            // Konsistenz-Gate (Review 11): sind Tarif oder KWKG aktiv, hängen die
            // Zahlungsreihen an den Stundenreihen. Wurde der Bericht OHNE Zeitreihen
            // gesammelt (Baustein „Ergebnisse je Variante" abgewählt), würde das
            // Diagramm andere Zahlen zeigen als die Tabellen darüber → entfallen
            // lassen und offen begründen (keine stillen Widersprüche).
            // ETAPPE BK1: Der KWKG-Zweig fragt KwkgAktivierung — die EINE Regel, die
            // auch der Rechenkern zieht. Die Projektsätze entscheiden nicht mehr.
            // Q11 (E7b): nur ein WIRKSAMER Tarifsatz (Rollentarif) braucht die Reihen.
            // BV-E3: Die Regel steht im Wertesatz (VerlaufEntfaellt) — dieselbe für Word und Excel.
            if (werte.VerlaufEntfaellt)
            {
                k.Ueberschrift2("Kapitalwert-Verlauf über den Betrachtungszeitraum");
                k.Hinweis("Diagramm entfällt: Tarifstruktur/KWKG benötigen Stundenreihen, " +
                          "der Bericht wurde aber ohne Zeitreihen erzeugt — Baustein " +
                          "„Ergebnisse je Variante“ aktivieren und den Bericht erneut erstellen.");
                return null;
            }

            // ETAPPE E9a (Schritt B): jedes Szenario über SEINEN Betrachtungszeitraum —
            // die Linie endet, wo ihr Kapitalwert steht, und die Gliederung der Brücke
            // passt zum Ergebnis. Ohne gepflegten Zeitraum Zahl für Zahl der Verlauf
            // über T. BV-E3: gerechnet hat ihn der Sammler (null = die Rechnung scheiterte).
            return werte.Verlauf;
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
                // BV-E5: dasselbe Modell wie bild.wirtschaft.kapitalwert_szenarien (Berichtsbilder).
                Zeichnung.Zeichenmodell dreier = Sicher(() => Berichtsbilder.KapitalwertSzenarien(verlauf));
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
            // BV-E5: dasselbe Modell wie bild.wirtschaft.barwerte_kumuliert (Berichtsbilder).
            k.Bild(Sicher(() => Berichtsbilder.BarwerteKumuliert(verlauf)), 620, 310);
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
            // BV-E5: dasselbe Modell wie bild.wirtschaft.bruecke (Berichtsbilder) — ohne Verlauf, Leitversion
            // oder passende Gliederung entfällt die Bildstelle samt Überschrift.
            Zeichnung.Zeichenmodell bild = Sicher(() => Berichtsbilder.Bruecke(daten, verlauf, alle, p, bewertung, k.Kultur));
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
                // BV-E5: dasselbe Modell wie stand.bild.zahlungsstrom (Berichtsbilder).
                Zeichnung.Zeichenmodell strom = Sicher(() => Berichtsbilder.Zahlungsstrom(bild, v.Anzeige, k.Kultur));
                if (strom != null) k.Bild(strom, 620, strom.Hoehe / 2);

                // BV-E5: dieselbe Tafel wie {{stand.tabelle.mehrjahres}}.
                k.Fuege(WordTabellenschreiber.Direkt(k, Berichtstabellen.Mehrjahrestafel(bild, k.Kultur)));

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

            // BV-E5: dieselbe Tafel wie {{stand.tabelle.vermiedene_kosten}}.
            k.Fuege(WordTabellenschreiber.Direkt(k, Berichtstabellen.VermiedeneKosten(idProjekt, alle, k.Kultur)));
            k.Abstand();
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

                // Elf Spalten, gleich denen des Excel-Blattes (mit dem zweiten Fall sechzehn) — BV-E5: dieselbe
                // Tafel wie {{stand.tabelle.kwkg_module}}; darunter die Herleitung der Sätze nach § 7.
                Berichtstabelle kwkg = Berichtstabellen.KwkgModule(v, alle, k.Kultur);
                k.Fuege(WordTabellenschreiber.Direkt(k, kwkg));
                foreach (string herleitung in kwkg.Hinweise) k.HinweisRoh(herleitung);
                k.Abstand();
            }
        }

        // ------------------------------------------------- Betriebskosten nach Kostenart (E7)

        /// <summary>
        /// ETAPPE E7 — die Betriebskostenpositionen, gegliedert nach der Kostenart der
        /// VDI 2067, je Position mit Bemessungsart und Herleitung Menge × Einheitpreis.
        /// Das ist der Zweck, für den Etappe E3 die Spalte <c>Kostenart</c> angelegt hat.
        /// </summary>
        /// <summary>
        /// ETAPPE E17 (V‑G11) — die Tabelle „Nicht monetarisierbare Wirkungen": Überschrift
        /// (<c>WIRT_NM_TITEL</c>), ein Hinweis zu Skalen und Regel, dann je Wirkung eine
        /// Zeile. Ohne benannte Wirkung entfällt der Block.
        /// </summary>
        internal static void SchreibeNichtMonetaereWirkungen(WordKontext k, IReadOnlyList<ProjektWirkung> wirkungen)
        {
            List<ProjektWirkung> zeilen = (wirkungen ?? new List<ProjektWirkung>())
                .Where(w => w != null && !string.IsNullOrWhiteSpace(w.Beschreibung)).ToList();
            if (zeilen.Count == 0) return;

            k.Ueberschrift2Roh(MyResource.Resource.WIRT_NM_TITEL);
            k.HinweisRoh(MyResource.Resource.WIRT_NM_TABELLE_HINWEIS);

            // BV-E5: dieselbe Tafel wie {{tabelle.wirtschaft.nicht_monetaer}}. Über den Einfügeanker wie jede andere
            // Tabelle: Mit Vorlage steht am Ende des Rumpfs deren Abschnittsangabe (w:sectPr), und ein bloßes Anhängen
            // setzte die Tafel dahinter — ungültig in jeder Office-Fassung (Konzept Berichtsvorlagen 2.4).
            k.Fuege(WordTabellenschreiber.Direkt(k, Berichtstabellen.NichtMonetaer(zeilen, k.Kultur)));
        }

        private static void SchreibeBetriebskosten(WordKontext k, BerichtsDaten daten,
                                                   List<WirtschaftlichkeitErgebnis> alle)
        {
            var mitPositionen = alle.Where(x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET &&
                                                x.Betriebskosten != null &&
                                                x.Betriebskosten.Count > 0).ToList();
            if (mitPositionen.Count == 0) return;

            k.Ueberschrift2Roh(MyResource.Resource.WIRT_BK_TITEL);
            k.HinweisRoh(MyResource.Resource.WIRT_BK_HINWEIS);

            foreach (VariantenDaten v in daten.Varianten)
            {
                WirtschaftlichkeitErgebnis e = mitPositionen.FirstOrDefault(x => x.IdProjekt == v.IdProjekt);
                if (e == null) continue;

                k.Ueberschrift3((v.IstStamm ? "Stamm — " : "Variante — ") + v.Anzeige);

                // BV-E5: dieselbe Tafel wie {{stand.tabelle.betriebskosten}}; die Probe gegen die Zahl, mit der die
                // Kapitalwertrechnung gerechnet hat (Positionen des ersten Jahres gegen die Betriebskosten p. a., E8c),
                // steht darunter.
                Berichtstabelle bk = Berichtstabellen.Betriebskosten(v, alle, k.Kultur);
                k.Fuege(WordTabellenschreiber.Direkt(k, bk));
                foreach (string abweichung in bk.Hinweise) k.HinweisRoh(abweichung);
                k.Abstand();
            }
        }

        // ------------------------------------------------------------- Tabellen

        private static void SchreibeVergleich(WordKontext k, BerichtsDaten daten,
                                              List<WirtschaftlichkeitErgebnis> alle, string szenario,
                                              WirtschaftsBerichtswerte werte)
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
            // BV-E3: die Zeilen gegen diese Referenz aus dem Wertesatz (WirtschaftlichkeitZeilen.Kennzahlen).
            // BV-E5: die Tafel steht als Berichtstabelle (Berichtstabellen.Wirtschaftskennzahlen) — dieselbe wie
            // {{tabelle.wirtschaft.kennzahlen}}; Blockteilung nur in Sicht 1 (Konzept § 2.15), in Sicht 2 genau A | B.
            VariantenDaten stamm = daten.Varianten.FirstOrDefault(v => v.IstStamm);
            if (stamm == null) return;
            Berichtstabelle tafel = Berichtstabellen.Wirtschaftskennzahlen(daten, werte, szenario, BerichtTexte.Englisch, k.Kultur);
            if (tafel.Spalten.Count == 0) return;
            bool paar = daten.Sicht != null && daten.Sicht.IstPaar;

            foreach (IReadOnlyList<int> block in tafel.Bloecke())
            {
                if (paar)
                {
                    int idErste = block.Select(i => tafel.Spalten[i].IdProjekt).FirstOrDefault(id => id.HasValue) ?? 0;
                    k.HinweisRoh(Referenzwahl.Deklarationszeile(
                        Referenzwahl.Name(daten.Varianten.FirstOrDefault(v => v.IdProjekt == idErste)),
                        Referenzwahl.Name(daten.Varianten.FirstOrDefault(
                            v => daten.IdGruppenreferenz > 0
                               ? v.IdProjekt == daten.IdGruppenreferenz : v.IstStamm))));
                }

                k.Fuege(WordTabellenschreiber.Direkt(k, tafel, block));

                // ETAPPE E5 (V‑A, Befund A2): die WARNUNG einer Zelle — heute die
                // Mehrdeutigkeit des Zinsfußes bei mehr als einem Vorzeichenwechsel. Die
                // Zelle bleibt die Zahl; die Warnung steht unter der Tafel, je Stand einmal.
                foreach (Tabellenzeile z in tafel.Zeilen)
                    foreach (int i in block)
                        if (i < z.Zellen.Count && z.Zellen[i].Warnung != null) k.HinweisRoh(z.Zellen[i].Warnung);
                k.Abstand();
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
                // BV-E5: dieselbe Tafel wie {{stand.tabelle.strommengen}}.
                k.Fuege(WordTabellenschreiber.Direkt(k, Berichtstabellen.Strommengen(v, matrizen, BerichtTexte.Englisch, k.Kultur)));
                k.Hinweis(string.Format(k.Kultur, MyResource.Resource.WIRT_MATRIX_STUNDENSPITZE,
                                        k.F(m.MaxBezugKW, 0)));
                k.HinweisRoh(MyResource.Resource.WIRT_MATRIX_BEDARF_HINWEIS);
            }
        }

        /// <summary>Emissionsbilanz je Projekt: Schadstoff × gekoppelt/getrennt/Vermeidung.</summary>
        private static void SchreibeEmissionsbilanz(WordKontext k, BerichtsDaten daten,
                                                    WirtschaftlichkeitParameter p,
                                                    List<WirtschaftlichkeitErgebnis> alle,
                                                    WirtschaftsBerichtswerte werte)
        {
            foreach (VariantenDaten v in daten.Varianten)
            {
                // Nur bei aktuellem Wirtschaftlichkeits-Ergebnis — sonst stünden
                // zwei Rechenstände in einem Kapitel (Review Phase 8).
                WirtschaftlichkeitErgebnis erw = alle.FirstOrDefault(x =>
                    x.IdProjekt == v.IdProjekt && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                if (erw == null || !werte.ErgebnisAktuell(erw))
                {
                    k.Hinweis("⚠ " + (v.IstStamm ? "Stamm" : v.Anzeige) +
                              ": Emissionsbilanz entfällt — das Wirtschaftlichkeits-Ergebnis " +
                              "passt nicht zum Simulationslauf dieses Berichts.");
                    continue;
                }
                // BV-E3: gerechnet hat sie der Sammler (EmissionsBilanzRechner.Berechne mit p).
                EmissionsBilanz b = werte.Emissionsbilanz(v.IdProjekt);
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

                // BV-E5: dieselbe Tafel wie {{stand.tabelle.emissionsbilanz}}.
                k.Fuege(WordTabellenschreiber.Direkt(k, Berichtstabellen.Emissionsbilanz(b, BerichtTexte.Englisch, k.Kultur)));

                // Die beiden Teilbeträge, die aus einer WAHL stammen und in den Zahlen
                // oben stecken — als Zeilen unter der Tabelle statt in ihr: Eine
                // Differenzspalte „Vermeidung" hätte für sie keine Bedeutung.
                if (b.CO2BiogenT > 0)
                    k.Hinweis(MyResource.Resource.BILANZ_ZEILE_BIOGEN + ": " +
                              k.F(b.CO2BiogenT, 1) + " (im gekoppelten System enthalten)");
                if (b.CO2GutschriftStromT > 0)
                    k.Hinweis(MyResource.Resource.BILANZ_ZEILE_GUTSCHRIFT + ": " +
                              k.F(b.CO2GutschriftStromT, 1) + " (in der getrennten Referenz enthalten)");
                k.Abstand();
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

                // BV-E5: dieselbe Tafel wie {{stand.tabelle.sensitivitaet}}; die Steigung ist eine Ableitung der
                // beiden Randwerte, ohne stetige Stufe (Wegfall des KWKG-Zuschlags) „—".
                k.Fuege(WordTabellenschreiber.Direkt(k, Berichtstabellen.Sensitivitaet(v, zeilen, BerichtTexte.Englisch, k.Kultur)));
                k.Abstand();
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
                                              WirtschaftlichkeitParameter p, BerichtsDaten daten,
                                              WirtschaftsBerichtswerte werte)
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

            // W5‑B‑11: sechs Spalten statt fünf; ETAPPE E2 (G8): sieben mit der Spanne — die Beschriftungsspalte gibt
            // die Breite ab, die Einstufung und Spanne brauchen. ETAPPE E2 (G8): die REFERENZZEILE nennt den Stand, gegen
            // den jede Δ-Zahl gerechnet ist; in ihren Δ-Spalten steht „(Referenz)“ — eine 0 wäre eine gerechnete Zahl.
            // E5 (Q4): die SPANNE ist der Betrag aus größtem und kleinstem Szenariowert; W5‑B‑11 (G9), E5 (U5): die
            // Einstufung ist derselbe Text wie auf der Karte. BV-E5: dieselbe Tafel wie {{tabelle.wirtschaft.szenarien}}.
            Berichtstabelle t = Berichtstabellen.Szenarien(bewertung, BerichtTexte.Englisch, k.Kultur);
            k.Fuege(WordTabellenschreiber.Direkt(k, t));
            k.HinweisRoh(string.Format(k.Kultur, MyResource.Resource.WIRT_SZ_DELTA_FUSS,
                                       band.Referenzname));

            // ---- ETAPPE E6 (Nachtrag E5b, Frage (4)): das SPANNENBILD neben der Tafel ----
            // Dieselbe Bandbreite als Balken je Version (Mockup valeri-f2): der Balken vom
            // kleinsten bis zum größten Szenariowert, der Erwartungsfall als Punkt, die
            // Referenz als Nulllinie — dasselbe Modell wie auf der Seite. Die Anzeigegröße
            // folgt der Bildhöhe, die mit den Versionen wächst.
            // BV-E5: dasselbe Modell wie bild.wirtschaft.spanne (Berichtsbilder).
            Zeichnung.Zeichenmodell spanne = Sicher(() => Berichtsbilder.Spanne(band));
            if (spanne != null) k.Bild(spanne, 620, spanne.Hoehe / 2);

            // ---- W5‑B‑11 (G8): die ANNAHMEN der Bandbreite, je Szenario eine Zeile ----
            //
            // Der Nachweis nennt den WIRKSAMEN Satz (i, p_E, p_B, Investition, Erträge,
            // Nutzungsdauer) und seine Herkunft: „Vorgaben", solange niemand ein Feld
            // gepflegt hat, sonst „gepflegte Werte". Dieselben Ressourcen wie Dialog und
            // Seite — drei Formulierungen derselben Auskunft wären drei Wahrheiten.
            SchreibeSzenarioAnnahmen(k, p, daten, werte);

            // ---- ETAPPE E9b (U10, E9b‑Q3): der Ausweis unter den Annahmen -------------
            // „n von m Parametern szenariert" samt der gepflegten Größen — an der Stelle
            // des Hinweistexts, den die Pflege in den Dialogen überflüssig macht
            // (Konzept § 2.11.7); derselbe Satz wie unter der Annahmentafel der Seite.
            if (!string.IsNullOrEmpty(bewertung.Szenarioabdeckung))
                k.HinweisRoh(bewertung.Szenarioabdeckung);

            // ---- W5‑B‑11 (G9): der Vorschlag zur Entscheidung ----------------------
            // ETAPPE E2: Er nennt die REFERENZ beim Namen. Seit § 2.9 rechnet ΔKW gegen
            // die gewählte Referenz, nicht mehr fest gegen den Stamm — „gegenüber dem
            // Stammprojekt" war bei gewählter Variantenreferenz schlicht falsch. Seit E5
            // Teil b ist es der Satz der Bewertung — derselbe wie auf der Seite.
            if (!string.IsNullOrEmpty(bewertung.Vorschlagstext)) k.TextRoh(bewertung.Vorschlagstext);
            k.Abstand();
        }

        /// <summary>
        /// ETAPPE W5‑B‑11 (G8): die Annahmenzeile je Szenario — Best und Worst, in
        /// dieser Reihenfolge. Erwartet bekommt keine: Es IST der Projektparametersatz,
        /// und der steht bereits in „Parameter dieses Rechenlaufs".
        /// </summary>
        private static void SchreibeSzenarioAnnahmen(WordKontext k, WirtschaftlichkeitParameter p,
                                                     BerichtsDaten daten, WirtschaftsBerichtswerte werte)
        {
            if (p == null) return;
            foreach (string sz in new[] { WirtschaftlichkeitSzenario.WORST,
                                          WirtschaftlichkeitSzenario.BEST })
            {
                string annahmen = Annahmenzeile(p, sz, k.Kultur);
                if (annahmen == null) continue;
                k.HinweisRoh(annahmen);

                // ETAPPE E9a (Norm 9 c): die gepflegten Trägerpreise des Szenarios je Stand —
                // nur, wo einer gepflegt ist; „wie Erwartet" wird nicht wiederholt.
                // BV-E3: aus dem Wertesatz (TraegerpreisSzenario.Nachweiszeile über die Stände).
                string preise = daten != null ? werte.Traegerpreiszeile(sz, k.Kultur) : null;
                if (!string.IsNullOrEmpty(preise)) k.HinweisRoh(preise);
            }
        }

        /// <summary>
        /// Die Annahmenzeile eines Szenarios (Günstig, Ungünstig): der wirksame Satz und seine Herkunft;
        /// <c>null</c> ohne Satz. Dieselbe Zeile führt der Katalog als <c>wirtschaft.szenario.&lt;s&gt;.annahmen</c>.
        /// </summary>
        internal static string Annahmenzeile(WirtschaftlichkeitParameter p, string sz, System.Globalization.CultureInfo kultur)
        {
            SzenarioSatz satz = p?.SatzFuer(sz);
            if (satz == null) return null;
            string name = sz == WirtschaftlichkeitSzenario.BEST
                        ? MyResource.Resource.WIRT_SZEN_BEST
                        : MyResource.Resource.WIRT_SZEN_WORST;
            string muster = satz.NurVorgaben
                          ? MyResource.Resource.WPAR_SZ_HERKUNFT_VORGABE
                          : MyResource.Resource.WPAR_SZ_HERKUNFT_GEPFLEGT;
            return string.Format(kultur, muster, name, satz.Nachweis(p, kultur));
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
                                                   WirtschaftsBerichtswerte werte,
                                                   WirtschaftlichkeitBewertung bewertung)
        {
            foreach (string zeile in Valerizeilen(werte, bewertung)) k.HinweisRoh(zeile);
            foreach (string zeile in Deklarationszeilen(bewertung)) k.HinweisRoh(zeile);
        }

        /// <summary>Die VALERI-Ausweise vor den Deklarationen: Zeitraum, Nutzungsdauer, Eigennutzung (PV),
        /// Vereinfachungen — dieselben Zeilen führt der Katalog als <c>wirtschaft.valeri_hinweise</c>.</summary>
        internal static List<string> Valerizeilen(WirtschaftsBerichtswerte werte, WirtschaftlichkeitBewertung bewertung)
        {
            var zeilen = new List<string>();
            NutzungsdauerHinweise nutzungsdauer = bewertung.Nutzungsdauer ?? new NutzungsdauerHinweise();
            if (!string.IsNullOrEmpty(nutzungsdauer.Zeitraumzeile)) zeilen.Add(nutzungsdauer.Zeitraumzeile);
            foreach (string zeile in nutzungsdauer.Zeilen) zeilen.Add(zeile);

            try
            {
                WirtschaftlichkeitCtrl.ErzeugerFlags flags = werte.Erzeuger;   // BV-E3: ErzeugerDerGruppe im Sammler
                if (flags != null && flags.Photovoltaik)
                    zeilen.Add(ValeriAusweis.EigennutzungHerleitung());
            }
            catch { }

            zeilen.Add(ValeriAusweis.Vereinfachungen());
            return zeilen;
        }

        /// <summary>Die Deklarationen in EINER Zeile und die Nr.-31-Zeile — im Katalog
        /// <c>wirtschaft.deklarationen</c>.</summary>
        internal static List<string> Deklarationszeilen(WirtschaftlichkeitBewertung bewertung)
        {
            var zeilen = new List<string>();
            // ETAPPE E5 (V‑A): die Deklarationen in EINER Zeile — nominal · Steuern ·
            // Restwert · Risiko, die Risikozeile nach Q5 mit „keine benannt", wenn kein
            // Text gepflegt ist.
            var deklarationen = new List<string>();
            if (bewertung.Deklarationen != null)
                foreach (ValeriDeklaration d in bewertung.Deklarationen)
                    if (d != null && !string.IsNullOrEmpty(d.Text)) deklarationen.Add(d.Text);
            if (deklarationen.Count > 0) zeilen.Add(string.Join(" · ", deklarationen.ToArray()));

            // ETAPPE E5 (Nr. 31): Stände, deren Zeilen keinen Nachweisumschlag tragen.
            string nachweis = WirtschaftlichkeitBewertung.Nachweiszeile(bewertung.OhneNachweis);
            if (!string.IsNullOrEmpty(nachweis)) zeilen.Add(nachweis);
            return zeilen;
        }
    }
}
