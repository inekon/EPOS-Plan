using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Reiner Rechenkern der Kapitalwertmethode (DIN EN 17463 / ValERI; Konzept
    /// Kap. 5.1) — ohne DB- und UI-Abhängigkeit, dadurch gegen Referenzwerte
    /// testbar (goetz_test.XLS, VALERI_Vorlage_V7).
    ///
    ///   KW = −I₀ + Σ_{t=1..T} (E_t − A_t) / (1+i)^t + RW_T / (1+i)^T
    ///
    /// Regeln der Ausbaustufe W1:
    ///  - Ersatzbeschaffung: Position mit Nutzungsdauer n &lt; T wird in t = n, 2n, …
    ///    (t &lt; T) nominal unverändert erneut beschafft (keine Preissteigerung auf
    ///    Investitionen — Vereinfachung W1, im Bericht ausgewiesen).
    ///  - Restwert linear: je Position Investition × Restnutzungsdauer/Nutzungsdauer
    ///    zum Zeitpunkt T, abgezinst (Entscheidung 11.08.2026).
    ///  - Nutzungsdauer &lt; 1 a wird wie n = T behandelt (keine Ersatzbeschaffung,
    ///    kein Restwert) — betrifft Positionen ohne (sinnvoll) gepflegte Nutzungsdauer.
    ///  - Betriebskosten steigen mit p_B, Energiekosten mit p_E [%/a];
    ///    Einspeiseerlöse bleiben nominal konstant (feste Vergütung, W1).
    ///
    /// Erweiterungen der Stufe W2 (Phase 7):
    ///  - BEHG-CO₂-Abgabe: Jahr-1-Betrag [€/a], steigt mit p_E (CO₂-Preispfad).
    ///  - Zusätzliche nominale Erlösreihen je Jahr (KWKG-Bonus mit Vbh-Kontingent,
    ///    ab Etappe E4 zusätzlich die Steuergutschriften — die Jahreslogik baut der
    ///    Aufrufer, hier wird nur abgezinst).
    ///  - Nominalreihe + nominaler Restwert im Zahlungsbild → interner Zinsfuß
    ///    (IRR) der Differenzreihe per Bisektion.
    ///
    /// <para><b>ETAPPE E4 (Leitentscheidung L1): benannte Reihen statt EINER Reihe.</b>
    /// Bis dahin nahm <see cref="Rechne"/> genau ein <c>double[] zusatzErloesJeJahr</c>
    /// entgegen — die KWKG-Reihe. Mit den drei Steuergutschriften gibt es erstmals mehr
    /// als eine jahresscharfe Erlösreihe; der Parameter ist deshalb auf eine Liste
    /// benannter Reihen umgestellt (<see cref="ErloesReihe"/>). Die Rechnung selbst
    /// ändert sich nicht: Abgezinst wird die SUMME der Reihen je Jahr, und eine Liste
    /// mit genau der KWKG-Reihe liefert Wert für Wert dasselbe wie vorher. Die Namen
    /// werden im Bericht (Etappe E7) gebraucht, um die Gutschriften einzeln
    /// auszuweisen.</para>
    ///
    /// <para><b>ETAPPE E7: das Zahlungsbild gibt die Einzelpositionen zurück.</b> Bis
    /// dahin verließen dieses Verfahren nur Summen — die Jahresreihen von Betrieb,
    /// Energie, CO₂-Abgabe, Ersatzbeschaffung, Einspeiseerlös und den benannten
    /// Erlösreihen entstanden hier, gingen in die Nettoreihe ein und waren danach nicht
    /// mehr zu haben. Die Mehrjahrestabelle des Berichts braucht sie einzeln; sie stehen
    /// deshalb jetzt am <see cref="Zahlungsbild"/>. <b>Rein additiv</b> — der Rechenweg
    /// der Summen ist unverändert, und die Referenzprobe belegt das.</para>
    /// </summary>
    public static class KapitalwertRechner
    {
        /// <summary>
        /// Eine BENANNTE jahresscharfe Erlösreihe (Etappe E4, Leitentscheidung L1) —
        /// nominal, unabgezinst, Index 1…T; Index 0 bleibt unbenutzt (dort steht in den
        /// Zahlungsreihen die Investition).
        ///
        /// <para><b>Warum benannt.</b> Seit E4 gibt es vier solcher Reihen: den
        /// KWK-Zuschlag und die drei Steuergutschriften. Sie werden zwar gemeinsam
        /// abgezinst, müssen im Bericht (Etappe E7) und in der Sensitivität aber
        /// einzeln adressierbar bleiben — das Novellen-Szenario streicht zum Beispiel
        /// genau die KWKG-Reihe und lässt die Steuergutschriften stehen.</para>
        ///
        /// <para><b>Der Name ist ein Schlüssel, kein Anzeigetext</b> (Drei-Schichten-Regel):
        /// sprachneutral, ASCII, eingefroren. Die Anzeigetexte stehen in
        /// <c>MyResource.Resource.WIRT_REIHE_*</c>.</para>
        /// </summary>
        public sealed class ErloesReihe
        {
            /// <summary>KWK-Zuschlag nach KWKG 2025 (Phase 9 / Etappe E2).</summary>
            public const string KWKG = "KWKG_ZUSCHLAG";

            /// <summary>Energiesteuer-Entlastung nach § 53 bzw. § 53a EnergieStG (E4).</summary>
            public const string ENERGIESTEUER = "ENERGIESTEUER_GUTSCHRIFT";

            /// <summary>Stromsteuer-Befreiung nach § 9 Abs. 1 Nr. 3 StromStG (E4).</summary>
            public const string STROMSTEUER_BEFREIUNG = "STROMSTEUER_BEFREIUNG";

            /// <summary>Stromsteuer-Entlastung nach § 9b StromStG (E4).</summary>
            public const string STROMSTEUER_ENTLASTUNG = "STROMSTEUER_ENTLASTUNG";

            /// <summary>
            /// ETAPPE K6 — pauschale Vorauszahlung nach § 9 KWKG für Anlagen bis
            /// 2 kW<sub>el</sub>. Die einzige Reihe des Programms, deren Betrag im
            /// <b>Index 0</b> steht: Das Gesetz zahlt einmalig aus, binnen zwei Monaten
            /// nach der Zulassung, und ersetzt damit die laufende Abrechnung. Sie ist
            /// deshalb ein Erlös im Jahr 0 — nicht, wie in der Altanwendung, eine
            /// Minderung der Investition.
            /// </summary>
            public const string KWKG_PAUSCHALE = "KWKG_PAUSCHALE";

            /// <summary>
            /// ETAPPE P4 (PV-Konzept § 4.6): die jahresscharfe PV-Einspeisevergütung
            /// aus dem Vergütungsdialog (<c>PvErloesRechner</c>). Ersetzt bei aktivem
            /// Dialog den PV-Anteil des konstanten Einspeiseerlöses — nach Ablauf der
            /// Vergütungsdauer fällt die Reihe auf den Marktwert (Direktvermarktung)
            /// bzw. 0 (feste EV) zurück; die § 51a-Gutschrift liegt im letzten
            /// Vergütungsjahr.
            /// </summary>
            public const string PV_VERGUETUNG = "PV_VERGUETUNG";

            public ErloesReihe(string name, double[] jeJahr)
            {
                Name = name ?? "";
                JeJahr = jeJahr;
            }

            /// <summary>Sprachneutraler Schlüssel der Reihe.</summary>
            public string Name { get; private set; }

            /// <summary>Nominale Jahresbeträge [€/a]; Index 1…T, <c>null</c> = keine Reihe.</summary>
            public double[] JeJahr { get; private set; }

            /// <summary>Betrag des ersten Betrachtungsjahres [€/a]; 0, wenn die Reihe leer ist.</summary>
            public double Jahr1
            {
                get { return JeJahr != null && JeJahr.Length > 1 ? JeJahr[1] : 0; }
            }

            /// <summary>Wert des Jahres t [€/a]; außerhalb der Reihe 0.</summary>
            public double Wert(int t)
            {
                return JeJahr != null && t >= 0 && t < JeJahr.Length ? JeJahr[t] : 0;
            }
        }

        /// <summary>Eine Investitionsposition (Tab_ProjektWerte, Kategorie 1, Szenariowert).</summary>
        public class InvestPosition
        {
            public double Betrag;          // [€]
            public double Nutzungsdauer;   // [a]; < 1 → wie Betrachtungszeitraum

            /// <summary>
            /// ETAPPE KD6 (Konzept Kostendialoge § 11, FK10): Startzeitpunkt der
            /// Investition. ≤ 1 = t0 (Bestand, zeichengleicher Rechenweg); Jahr
            /// X ≥ 2 = die Zahlung fällt erst im Jahr X (abgezinst über die
            /// Nominalreihe), Nutzungsdauer/Ersatz zählen ab X. Der Startzeitpunkt
            /// VERSCHIEBT die Zahlung, er indexiert sie nicht (keine
            /// Preissteigerung auf Investitionen).
            /// </summary>
            public int StartJahr;
        }

        /// <summary>Zahlungsstrombild eines Projekts über den Betrachtungszeitraum.</summary>
        public class Zahlungsbild
        {
            public double Investition;          // I₀ [€] — NACH Zuschussabzug

            /// <summary>ETAPPE KD6 (§ 11): Summe der Positionen mit Startjahr ≥ 2 [€]
            /// — sie zahlen über die Jahresreihe, nicht über I₀ (reiner Ausweis).</summary>
            public double InvestitionVerschoben;

            // ---- ETAPPE K5 — der Investitionszuschuss (Konzept § 7.4, L7) ----

            /// <summary>
            /// Summe der Investitionspositionen VOR Abzug des Zuschusses [€]. Die
            /// Bezugsgröße jeder prozentualen Betriebskostenbemessung („% der
            /// Investitionssumme") und die Zahl, die der Bericht als „Investition"
            /// ausweist.
            /// </summary>
            public double InvestitionBrutto;

            /// <summary>
            /// Tatsächlich ANGESETZTER Zuschuss [€], positiv. Er ist auf
            /// <see cref="InvestitionBrutto"/> geklemmt: Ein Zuschuss über der
            /// Investitionssumme ergäbe ein negatives I₀ — also eine Zahlung, die das
            /// Projekt im Jahr 0 einbringt. Das ist keine Investitionsrechnung mehr,
            /// sondern eine Fehleingabe, und sie wird als solche gemeldet statt
            /// gerechnet.
            /// </summary>
            public double Zuschuss;

            /// <summary>
            /// Übersteigender Teil eines zu hohen Zuschusses [€], 0 im Regelfall.
            /// Größer als 0 heißt: Der Anwender hat mehr Zuschuss erfasst als
            /// Investition — der Aufrufer setzt daraufhin einen Hinweis.
            /// </summary>
            public double ZuschussUeberhang;

            public double BarwertAusgaben;      // Betrieb + Energie + Ersatz [€]
            public double BarwertEinnahmen;     // Erlöse [€]
            public double RestwertBarwert;      // [€]
            public double Kapitalwert;          // KW [€]

            /// <summary>Barwert-Zahlungsreihe: Index 0 = −I₀, Index t = Barwert des
            /// Netto-Zahlungsstroms im Jahr t OHNE Restwert (für Amortisation).</summary>
            public double[] BarwertReihe;

            /// <summary>Nominale Zahlungsreihe (unabgezinst), gleicher Aufbau —
            /// Grundlage des internen Zinsfußes (W2).</summary>
            public double[] NominalReihe;

            /// <summary>Restwert zum Zeitpunkt T, unabgezinst (für den IRR).</summary>
            public double RestwertNominal;

            // ---- ETAPPE E7 — der Rückgabekanal der EINZELPOSITIONEN ----
            //
            // Bis E7 gab dieses Bild nur die SUMMEN heraus: eine Nettoreihe, eine
            // Barwertreihe, vier Barwertskalare. Die Jahresreihen der einzelnen
            // Positionen entstanden in der Schleife unten, gingen in die Summe ein und
            // wurden verworfen — vom KWK-Zuschlag überlebte allein der Wert des Jahres 1.
            // Eine Mehrjahrestabelle nach Positionen war damit nicht baubar; es fehlte
            // nicht ein Formatierer, sondern der Kanal.
            //
            // Die Felder sind REIN ADDITIV: Sie werden nur befüllt, nie gelesen, und
            // der Rechenweg der Summen bleibt Zeichen für Zeichen der von vorher
            // (siehe den Kommentar in der Jahresschleife). Nominal, unabgezinst,
            // Index 1…T; Index 0 bleibt leer — dort steht die Investition.

            /// <summary>Betriebskosten je Jahr [€], mit p_B fortgeschrieben.</summary>
            public double[] BetriebJeJahr;

            /// <summary>Energiekosten je Jahr [€] OHNE CO₂-Abgabe, mit p_E fortgeschrieben.</summary>
            public double[] EnergieJeJahr;

            /// <summary>CO₂-Abgabe nach BEHG je Jahr [€], mit p_E fortgeschrieben.</summary>
            public double[] BehgJeJahr;

            /// <summary>Ersatzbeschaffungen je Jahr [€] (Index 0…T; nominal konstant).</summary>
            public double[] ErsatzJeJahr;

            /// <summary>Einspeiseerlös je Jahr [€] (nominal konstant, feste Vergütung).</summary>
            public double[] EinspeiseerloesJeJahr;

            /// <summary>Die benannten Erlösreihen, wie sie hereingereicht wurden —
            /// KWK-Zuschlag und die drei Steuergutschriften (E4). <c>null</c> = keine.
            /// Erst hierdurch wird das Auslaufen des KWK-Zuschlags im Bericht
            /// sichtbar.</summary>
            public IList<ErloesReihe> ErloesReihen;

            /// <summary>Nominaler Jahresbetrag einer benannten Reihe [€]; 0, wenn die
            /// Reihe fehlt.</summary>
            public double ReihenWert(string name, int t)
            {
                if (ErloesReihen == null || name == null) return 0;
                double summe = 0;
                foreach (ErloesReihe r in ErloesReihen)
                    if (r != null && string.Equals(r.Name, name, StringComparison.Ordinal))
                        summe += r.Wert(t);
                return summe;
            }

            /// <summary>true, wenn die Reihe überhaupt einen Betrag ungleich 0 führt.</summary>
            public bool HatReihe(string name)
            {
                if (ErloesReihen == null || name == null) return false;
                foreach (ErloesReihe r in ErloesReihen)
                    if (r != null && string.Equals(r.Name, name, StringComparison.Ordinal) &&
                        r.JeJahr != null)
                        for (int t = 1; t < r.JeJahr.Length; t++)
                            if (r.JeJahr[t] != 0) return true;
                return false;
            }
        }

        /// <summary>Annuitätenfaktor a(i,n); i als Dezimalzahl (0,03), n in Jahren.</summary>
        public static double Annuitaet(double i, double n)
        {
            if (n <= 0) return 0;
            if (Math.Abs(i) < 1e-12) return 1.0 / n;
            double q = Math.Pow(1.0 + i, n);
            return i * q / (q - 1.0);
        }

        /// <summary>
        /// Absolutes Zahlungsbild eines Projekts. Kostenreihen in €/a (Jahr-1-Werte),
        /// Zins/Preissteigerungen in Prozent. energieJahr = null → Energiekosten
        /// unbestimmbar; der Aufrufer setzt dann Fehlgrund und lässt KW leer.
        /// </summary>
        /// <param name="zusatzErloesReihen">
        /// Benannte jahresscharfe Erlösreihen (Etappe E4, L1) — KWK-Zuschlag und
        /// Steuergutschriften. <c>null</c> oder leer = keine. Abgezinst wird die SUMME
        /// je Jahr; eine Liste mit genau der KWKG-Reihe rechnet Wert für Wert wie der
        /// frühere Parameter <c>double[] zusatzErloesJeJahr</c>.
        /// </param>
        /// <param name="zuschuss">
        /// ETAPPE K5 (Konzept § 7.4, L7): Investitionszuschuss [€], positiv erfasst.
        /// Er mindert I₀ <b>einmalig</b> und wird deshalb NICHT als
        /// <see cref="InvestPosition"/> hereingereicht: Eine Position bekäme über ihre
        /// Nutzungsdauer eine Ersatzbeschaffung und einen Restwert, und beides ist bei
        /// einer Förderzahlung sinnlos (die Altanwendung tat genau das, mit einer
        /// zufälligen Nutzungsdauer — Konzept Anhang A(e)). Der Abzug geschieht deshalb
        /// NACH der Positionsschleife: Ersatzreihe und Restwert entstehen aus den
        /// Bruttobeträgen und bleiben vom Zuschuss unberührt. 0 = kein Zuschuss.
        /// </param>
        /// <param name="behgJeJahr">
        /// ETAPPE K6 (Konzept § 8.3, Entscheidung E5): die CO₂-Abgabe
        /// <b>jahresscharf</b> [€], Index 1…T — der jahresgenaue Preispfad des
        /// Gesetzeskatalogs. <c>null</c> = wie bisher: <paramref name="behgJahr"/> wird
        /// mit der Energiepreissteigerung fortgeschrieben. Der Ausdruck für die Ausgaben
        /// bleibt in diesem Fall ZEICHENGLEICH der Fassung vor K6 — insbesondere bleibt
        /// <c>(energieJahr + behgJahr)</c> EINE Klammer, sonst verschöbe sich das
        /// Ergebnis in der letzten Stelle (Warnung aus Etappe E7).
        /// </param>
        public static Zahlungsbild Rechne(List<InvestPosition> investitionen,
                                          double betriebJahr, double energieJahr, double erloesJahr,
                                          double zinsProzent, int jahre,
                                          double preisstBetriebProzent, double preisstEnergieProzent,
                                          double behgJahr = 0,
                                          IList<ErloesReihe> zusatzErloesReihen = null,
                                          double zuschuss = 0,
                                          double[] behgJeJahr = null,
                                          IList<KeyValuePair<double, int>> betriebAbJahr = null)
        {
            double i = zinsProzent / 100.0;
            double pB = preisstBetriebProzent / 100.0;
            double pE = preisstEnergieProzent / 100.0;
            int T = Math.Max(1, jahre);

            var z = new Zahlungsbild
            {
                BarwertReihe = new double[T + 1],
                NominalReihe = new double[T + 1],
                // ETAPPE E7 — Rückgabekanal der Einzelpositionen (rein additiv).
                BetriebJeJahr = new double[T + 1],
                EnergieJeJahr = new double[T + 1],
                BehgJeJahr = new double[T + 1],
                EinspeiseerloesJeJahr = new double[T + 1],
                ErloesReihen = zusatzErloesReihen
            };

            // ---------------- Investition t=0 + Ersatzbeschaffungen + Restwert ----------------
            double[] ersatzJeJahr = new double[T + 1];
            z.ErsatzJeJahr = ersatzJeJahr;                    // E7: dieselbe Reihe, nicht kopiert
            double restwertT = 0;

            if (investitionen != null)
            {
                foreach (InvestPosition pos in investitionen)
                {
                    if (pos == null || pos.Betrag == 0) continue;
                    // Nutzungsdauern < 1 a sind fachlich nicht sinnvoll → wie T behandeln
                    // (verhindert zugleich exzessive Ersatz-Schleifen bei Fehleingaben).
                    double n = pos.Nutzungsdauer >= 1.0 ? pos.Nutzungsdauer : T;

                    // ETAPPE KD6 (§ 11, FK10): Positionen mit Startjahr X ≥ 2 zahlen
                    // erst im Jahr X — über die Jahresreihe (dort wird abgezinst),
                    // NICHT über I₀. Ersatzkette und Restwert zählen ab X. Für
                    // StartJahr ≤ 1 bleibt der Rechenweg Zeichen für Zeichen der
                    // von vorher.
                    int start = pos.StartJahr > 1 ? pos.StartJahr : 0;
                    if (start > T)
                    {
                        // Investition außerhalb des Betrachtungszeitraums: keine
                        // Zahlung, kein Ersatz, kein Restwert — nur Ausweis.
                        z.InvestitionVerschoben += pos.Betrag;
                        continue;
                    }

                    int letzteBeschaffung;
                    if (start == 0)
                    {
                        z.Investition += pos.Betrag;
                        letzteBeschaffung = 0;
                    }
                    else
                    {
                        z.InvestitionVerschoben += pos.Betrag;
                        ersatzJeJahr[start] += pos.Betrag;
                        letzteBeschaffung = start;
                    }

                    // Ersatz auf ganze Jahre gerundet: tj = round(start + k·n),
                    // 1 ≤ tj < T (im letzten Betrachtungsjahr wird nicht mehr ersetzt).
                    for (double t = start + n; ; t += n)
                    {
                        int tj = (int)Math.Round(t);
                        if (tj >= T) break;
                        if (tj >= 1) { ersatzJeJahr[tj] += pos.Betrag; letzteBeschaffung = tj; }
                    }

                    // Linearer Restwert der letzten Beschaffung zum Zeitpunkt T
                    // (konsistent zum gerundeten Buchungsjahr).
                    double alter = T - letzteBeschaffung;
                    double rest = n - alter;
                    if (rest > 1e-9) restwertT += pos.Betrag * (rest / n);
                }
            }

            // ---------------- ETAPPE K5: Zuschuss mindert I₀ einmalig ----------------
            // Der Abzug steht NACH der Positionsschleife und wirkt deshalb ausschließlich
            // auf z.Investition — ersatzJeJahr und restwertT sind zu diesem Zeitpunkt
            // fertig und bleiben Bruttogrößen. Genau das ist der Unterschied zur
            // Altanwendung: Dort war der Zuschuss eine Position mit (zufälliger)
            // Nutzungsdauer und erzeugte damit Ersatzbeschaffungen und einen Restwert
            // auf Geld, das nie ersetzt werden muss.
            z.InvestitionBrutto = z.Investition;
            if (zuschuss > 0)
            {
                // Klemme auf die Investitionssumme: Ein negatives I₀ wäre eine Einzahlung
                // im Jahr 0 - rechnerisch möglich, fachlich eine Fehleingabe. Der
                // Überhang wird ausgewiesen, damit der Aufrufer ihn melden kann, statt
                // ihn stillschweigend zu verschlucken.
                z.Zuschuss = Math.Min(zuschuss, z.Investition);
                z.ZuschussUeberhang = zuschuss - z.Zuschuss;
                z.Investition -= z.Zuschuss;
            }

            // ---------------- ETAPPE K6: Einmalzahlung im Jahr 0 ----------------
            // Index 0 einer benannten Erlösreihe ist eine EINMALZAHLUNG zum Zeitpunkt der
            // Investition — heute nur die Pauschale des § 9 KWKG. Sie wird nicht
            // abgezinst (t = 0) und mindert NICHT die Investition: I₀ bleibt, was die
            // Anlage kostet, und die Zahlung steht als Einnahme daneben.
            //
            // ADDITIV: Jede Reihe vor K6 führt in Index 0 eine 0, damit ist einmalT0 dort
            // 0 und beide Startwerte bleiben Zeichen für Zeichen die von vorher.
            double einmalT0 = 0;
            if (zusatzErloesReihen != null)
                foreach (ErloesReihe reihe in zusatzErloesReihen)
                    if (reihe != null) einmalT0 += reihe.Wert(0);

            // ---------------- Jahresreihe abzinsen ----------------
            z.BarwertReihe[0] = -z.Investition + einmalT0;
            z.NominalReihe[0] = -z.Investition + einmalT0;
            z.BarwertEinnahmen += einmalT0;
            for (int t = 1; t <= T; t++)
            {
                double faktor = Math.Pow(1.0 + i, -t);
                // ACHTUNG: Der Ausdruck für ausgaben bleibt ZEICHENGLEICH der Fassung vor
                // Etappe E7 — insbesondere bleibt (energieJahr + behgJahr) EINE Klammer.
                // Die getrennten Reihen darunter sind Ausweis und gehen NICHT in die
                // Summe ein; sonst verschöbe sich das Ergebnis in der letzten Stelle.
                //
                // ETAPPE K6: Nur wenn eine jahresscharfe CO₂-Reihe hereingereicht wurde,
                // tritt der zweite Zweig an ihre Stelle. Ohne sie (behgJeJahr = null)
                // läuft der Bestandsausdruck unverändert — deshalb zwei Zweige und
                // nicht eine umgeformte Zeile.
                double behgT = behgJeJahr != null && t < behgJeJahr.Length ? behgJeJahr[t] : 0;

                // ETAPPE KD6 (§ 11, FK10): Betriebskosten von Positionen mit
                // Startjahr X laufen erst ab t ≥ X — mit derselben Preissteigerung
                // ab t0 (der Betrag ist heutiges Preisniveau, gezahlt ab X). Ohne
                // solche Positionen ist betriebT == betriebJahr (bitgleich) und der
                // Ausdruck rechnet Zeichen für Zeichen wie vorher.
                double betriebT = betriebJahr;
                if (betriebAbJahr != null)
                    foreach (KeyValuePair<double, int> vb in betriebAbJahr)
                        if (t >= vb.Value) betriebT += vb.Key;

                double ausgaben = behgJeJahr == null
                    ? betriebT * Math.Pow(1.0 + pB, t - 1)
                      + (energieJahr + behgJahr) * Math.Pow(1.0 + pE, t - 1)
                      + ersatzJeJahr[t]
                    : betriebT * Math.Pow(1.0 + pB, t - 1)
                      + energieJahr * Math.Pow(1.0 + pE, t - 1) + behgT
                      + ersatzJeJahr[t];
                double einnahmen = erloesJahr;   // feste Einspeisevergütung, nominal konstant
                if (zusatzErloesReihen != null)
                    foreach (ErloesReihe reihe in zusatzErloesReihen)
                        if (reihe != null) einnahmen += reihe.Wert(t);   // KWKG + Steuern (E4)

                // ETAPPE E7 — Einzelpositionen für die Mehrjahrestabelle.
                z.BetriebJeJahr[t] = betriebT * Math.Pow(1.0 + pB, t - 1);
                z.EnergieJeJahr[t] = energieJahr * Math.Pow(1.0 + pE, t - 1);
                z.BehgJeJahr[t] = behgJeJahr == null ? behgJahr * Math.Pow(1.0 + pE, t - 1) : behgT;
                z.EinspeiseerloesJeJahr[t] = erloesJahr;

                z.BarwertAusgaben += ausgaben * faktor;
                z.BarwertEinnahmen += einnahmen * faktor;
                z.BarwertReihe[t] = (einnahmen - ausgaben) * faktor;
                z.NominalReihe[t] = einnahmen - ausgaben;
            }

            z.RestwertNominal = restwertT;
            z.RestwertBarwert = restwertT * Math.Pow(1.0 + i, -T);
            z.Kapitalwert = -z.Investition - z.BarwertAusgaben + z.BarwertEinnahmen + z.RestwertBarwert;
            return z;
        }

        /// <summary>
        /// Interner Zinsfuß [%] der Differenzreihe Variante − Stamm (inkl. Restwert-
        /// differenz im letzten Jahr): Nullstelle von KW(r) per Bisektion in
        /// (−99 %, 1000 %). null = kein Vorzeichenwechsel (keine klassische
        /// Investitionsreihe) oder keine Konvergenz.
        /// </summary>
        public static double? InternerZinsfuss(Zahlungsbild variante, Zahlungsbild stamm)
        {
            if (variante == null || stamm == null ||
                variante.NominalReihe == null || stamm.NominalReihe == null) return null;
            int T = Math.Min(variante.NominalReihe.Length, stamm.NominalReihe.Length) - 1;

            double[] fluss = new double[T + 1];
            for (int t = 0; t <= T; t++) fluss[t] = variante.NominalReihe[t] - stamm.NominalReihe[t];
            fluss[T] += variante.RestwertNominal - stamm.RestwertNominal;

            Func<double, double> kw = r =>
            {
                double summe = 0;
                for (int t = 0; t <= T; t++) summe += fluss[t] / Math.Pow(1.0 + r, t);
                return summe;
            };

            double lo = -0.99, hi = 10.0;
            double fLo = kw(lo), fHi = kw(hi);
            if (double.IsNaN(fLo) || double.IsNaN(fHi) || fLo * fHi > 0) return null;

            for (int iter = 0; iter < 200; iter++)
            {
                double mid = (lo + hi) / 2.0, fMid = kw(mid);
                if (Math.Abs(fMid) < 1e-6 || (hi - lo) < 1e-9) return Math.Round(mid * 100.0, 2);
                if (fLo * fMid <= 0) { hi = mid; fHi = fMid; } else { lo = mid; fLo = fMid; }
            }
            return Math.Round((lo + hi) / 2.0 * 100.0, 2);
        }

        /// <summary>
        /// Dynamische Amortisation der DIFFERENZ Variante − Stamm (Referenz = Stamm,
        /// Entscheidung 11.08.2026): erstes Jahr, in dem der kumulierte Barwert der
        /// Differenz-Zahlungsreihe ≥ 0 wird — ohne Restwert, mit linearer
        /// Interpolation im Jahr. null = amortisiert sich im Betrachtungszeitraum nie.
        /// </summary>
        public static double? AmortisationDifferenz(Zahlungsbild variante, Zahlungsbild stamm)
        {
            if (variante == null || stamm == null) return null;
            int T = Math.Min(variante.BarwertReihe.Length, stamm.BarwertReihe.Length) - 1;

            double kum = variante.BarwertReihe[0] - stamm.BarwertReihe[0];   // −ΔI₀
            if (kum >= 0)
            {
                // Keine Mehrinvestition: „amortisiert ab Jahr 0" gilt nur, wenn die
                // Variante auch über den Zeitraum netto nicht schlechter fährt.
                double summe = kum;
                for (int t = 1; t <= T; t++) summe += variante.BarwertReihe[t] - stamm.BarwertReihe[t];
                return summe >= 0 ? (double?)0 : null;
            }
            for (int t = 1; t <= T; t++)
            {
                double zufluss = variante.BarwertReihe[t] - stamm.BarwertReihe[t];
                if (kum + zufluss >= 0 && zufluss > 0)
                    return (t - 1) + (-kum / zufluss);                       // Interpolation im Jahr t
                kum += zufluss;
            }
            return null;
        }
    }
}
