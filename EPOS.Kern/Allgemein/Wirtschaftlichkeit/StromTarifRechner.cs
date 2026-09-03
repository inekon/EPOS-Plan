using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // ETAPPE E5 — Strombezug, Reststrom und Einspeisung nach der DIFFERENZMETHODE
    // (Konzept_BHKW_Kosten_Erloese.md, Abschnitt 4.3; Analyse der Altanwendung,
    // Abschnitte 2.3 und 8).
    //
    //   Bezugskosten ohne BHKW = Arbeit(Bedarf,    Bezugstarif)    + Leistung(Bedarf,    Modell)
    //   Reststromkosten        = Arbeit(Restbezug, Reststromtarif) + Leistung(Restbezug, Modell)
    //   Vermiedene Kosten      = Bezugskosten ohne BHKW − Reststromkosten
    //   Einspeiseerlös         = Einspeisemenge × Einspeisepreis
    //
    // REINE FUNKTIONEN ÜBER DTOs (Leitentscheidung L9, Vorbild SpeicherEngine/
    // Aufschlagsmodell.cs): kein Datenbankzugriff, keine Oberfläche, sprachneutrale
    // Schlüssel. Die Herleitungstexte entstehen mit einer übergebenen Kultur.
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Eine Stufe der Leistungspreisstaffel: <b>kumulierte Obergrenze</b> in kW mit
    /// Sommer- und Winterpreis in €/kW·a.
    ///
    /// <para><b>Kumuliert, nicht als Breite — das ist die erste der vier Fallen des
    /// Altkatalogs.</b> `DB-TARIF.XLS` speichert Stufen<i>breiten</i>: „500/1500/6000"
    /// bedeutet dort Grenzen bei 500, 2.000 und 8.000 kW, weil die Staffelroutine
    /// kumulativ aufsummiert. Hier steht in <see cref="ObergrenzeKW"/> die Grenze
    /// selbst. Wer alte Tarifsätze übernimmt, muss umrechnen.</para>
    ///
    /// <para><b>Die vierte Stufe wird geführt</b> (zweite Falle): Im Altkatalog war ihre
    /// Speicherzeile auskommentiert, sie war stumm der unbegrenzte Rest. Eine
    /// Obergrenze ≤ 0 bedeutet hier ausdrücklich „nach oben offen".</para>
    /// </summary>
    public class LeistungsStufe
    {
        /// <summary>Kumulierte Obergrenze [kW]; ≤ 0 = nach oben offen (letzte Stufe).</summary>
        public double ObergrenzeKW;
        /// <summary>Preis in der Sommerspanne [€/kW·a].</summary>
        public double PreisSommer;
        /// <summary>Preis in der Winterspanne [€/kW·a].</summary>
        public double PreisWinter;

        public LeistungsStufe() { }
        public LeistungsStufe(double obergrenzeKW, double preisSommer, double preisWinter)
        { ObergrenzeKW = obergrenzeKW; PreisSommer = preisSommer; PreisWinter = preisWinter; }

        /// <summary>true, wenn die Stufe überhaupt einen Preis führt.</summary>
        public bool Gepflegt { get { return PreisSommer != 0 || PreisWinter != 0; } }
    }

    /// <summary>
    /// Eine Tarifrolle (Bezug ohne BHKW, Reststrom mit BHKW oder Einspeisung):
    /// Durchschnitts-Arbeitspreis, Grundpreis und — bei den beiden Bezugsrollen —
    /// das Leistungspreismodell.
    ///
    /// <para><b>HT/NT entfällt</b> (Leitentscheidung L10, Nutzervorgabe): Je Rolle gilt
    /// EIN Durchschnitts-Arbeitspreis. Genau das tut die Altanwendung in
    /// <c>Durchschitt_eintragen</c> bereits, obwohl sie vier Preise führt. Die
    /// Leistungspreise bleiben vollständig erhalten — der Leistungsanteil der
    /// vermiedenen Kosten ist regelmäßig negativ und damit ergebnisrelevant.</para>
    /// </summary>
    public class TarifRolle
    {
        /// <summary>Sprachneutraler Rollenschlüssel (nur für Herleitungstexte).</summary>
        public string Rolle = "";

        /// <summary>Durchschnitts-Arbeitspreis [€/kWh].</summary>
        public double ArbeitspreisEurKWh;

        /// <summary>Grundpreis [€/a].</summary>
        public double GrundpreisEurJahr;

        /// <summary>
        /// Leistungspreismodell, Steuerwert aus <c>DbWerte.LEISTUNGSMODELL_*</c>.
        /// Ein leerer Wert wird wie <c>MONATLICH</c> behandelt.
        /// </summary>
        public string Leistungsmodell = DbWerte.LEISTUNGSMODELL_MONATLICH;

        /// <summary>Monatlicher Leistungspreis [€/kW·Monat] (Modell MONATLICH).</summary>
        public double MonatspreisEurKWMonat;

        /// <summary>Die vier Staffelstufen (Modelle STAFFEL und JAHRESHOECHSTLAST).</summary>
        public List<LeistungsStufe> Stufen = new List<LeistungsStufe>();
    }

    /// <summary>Ergebnis einer Rollenrechnung [€/a], aufgeteilt in Arbeit und Leistung.</summary>
    public class TarifKosten
    {
        public double ArbeitEur;
        public double LeistungEur;
        public double GrundpreisEur;
        public double SummeEur { get { return ArbeitEur + LeistungEur + GrundpreisEur; } }
    }

    /// <summary>
    /// Ergebnis der Differenzmethode für ein Projekt und ein Jahr [€/a].
    /// Alle Beträge netto.
    /// </summary>
    public class StromErloesErgebnis
    {
        /// <summary>Bezugskosten OHNE die Anlage (Referenz) [€/a].</summary>
        public TarifKosten Bezug = new TarifKosten();

        /// <summary>Reststromkosten MIT der Anlage [€/a].</summary>
        public TarifKosten Reststrom = new TarifKosten();

        /// <summary>Vermiedene Kosten, Arbeitsanteil [€/a].</summary>
        public double VermiedenArbeitEur;

        /// <summary>
        /// Vermiedene Kosten, Leistungsanteil [€/a] — <b>regelmäßig negativ</b>, weil der
        /// Reststrom-Leistungspreis über dem Bezugs-Leistungspreis liegt. Das ist kein
        /// Fehler, sondern die Kernaussage der Rechnung, und wird deshalb als eigene
        /// Zeile ausgewiesen (Konzept 4.3; im Beispiel der Altanwendung −341 €).
        /// </summary>
        public double VermiedenLeistungEur;

        /// <summary>Vermiedene Kosten gesamt [€/a] (Arbeit + Leistung + Grundpreisdifferenz).</summary>
        public double VermiedenGesamtEur;

        /// <summary>Einspeiseerlös [€/a] (Menge × Einspeisepreis, ohne KWK-Zuschlag).</summary>
        public double EinspeiseerloesEur;

        /// <summary>Bewertete Einspeisemenge [MWh/a] (PV-Überschuss + KWK-Einspeisung).</summary>
        public double EinspeisungMWh;

        /// <summary>Herleitungszeilen im Klartext (Nachweis statt stiller Zahl).</summary>
        public List<string> Herleitung = new List<string>();
    }

    /// <summary>Mengen- und Lastangaben eines Jahres — die Eingabeseite der Rechnung.</summary>
    public class StromErloesEingabe
    {
        /// <summary>Strombedarf OHNE die Anlage [MWh/a] (Referenzmenge).</summary>
        public double BedarfMWh;

        /// <summary>Tatsächlicher Netzbezug MIT der Anlage [MWh/a].</summary>
        public double RestbezugMWh;

        /// <summary>Eingespeiste Menge [MWh/a] (PV-Überschuss + KWK-Einspeisung).</summary>
        public double EinspeisungMWh;

        /// <summary>Lastbild des Bedarfs (Jahres-, Sommer-, Winter- und Monatsmaxima) [kW].</summary>
        public StromMatrix.Lastbild LastBedarf;

        /// <summary>Lastbild des Restbezugs [kW].</summary>
        public StromMatrix.Lastbild LastRestbezug;
    }

    /// <summary>
    /// Die Rechenkette „Strom und Erlöse" der Etappe E5 — reine Funktionen ohne
    /// Datenbankzugriff (L9).
    /// </summary>
    public static class StromTarifRechner
    {
        /// <summary>
        /// Leistungskosten einer Rolle [€/a] nach dem gewählten Modell.
        ///
        /// <list type="bullet">
        /// <item><c>MONATLICH</c>: Σ über zwölf Monate aus Monatsmaximum × Monatspreis.</item>
        /// <item><c>STAFFEL</c>: Sommer- und Wintermaximum getrennt durch die vierstufige
        /// Staffel geschickt, mit dem jeweiligen Saisonpreis je Stufe.</item>
        /// <item><c>JAHRESHOECHSTLAST</c>: nur das Jahresmaximum, mit den WINTERpreisen
        /// der Staffel bewertet (die Jahresspitze fällt in der Regel in die
        /// Winterspanne; ein davon abweichender Sommerpreis wäre nicht zuordenbar).</item>
        /// </list>
        ///
        /// <para><b>Kein versteckter Modellschalter</b> (dritte Falle des Altkatalogs):
        /// Dort führte ein Sommerpreis von 0 dazu, dass nur das Jahresmaximum gestaffelt
        /// wurde — bei 22 von 28 Tarifsätzen. Hier ist ein Preis von 0 ein Preis von 0.
        /// </para>
        ///
        /// <para><b>Kein stiller Vorrang</b> (vierte Abweichung): Die Altanwendung ließ
        /// den Monatspreis die Staffel überstimmen („hat Vorrang"). Hier entscheidet
        /// allein das gewählte Modell.</para>
        /// </summary>
        public static double Leistungskosten(TarifRolle rolle, StromMatrix.Lastbild last)
        {
            if (rolle == null || last == null) return 0;
            string modell = string.IsNullOrEmpty(rolle.Leistungsmodell)
                          ? DbWerte.LEISTUNGSMODELL_MONATLICH : rolle.Leistungsmodell;

            if (string.Equals(modell, DbWerte.LEISTUNGSMODELL_MONATLICH, StringComparison.Ordinal))
                return last.SummeMonatsmaxima * rolle.MonatspreisEurKWMonat;

            if (string.Equals(modell, DbWerte.LEISTUNGSMODELL_JAHRESHOECHSTLAST, StringComparison.Ordinal))
                return Staffelbetrag(rolle.Stufen, last.MaxJahr, true);

            if (string.Equals(modell, DbWerte.LEISTUNGSMODELL_STAFFEL, StringComparison.Ordinal))
                return Staffelbetrag(rolle.Stufen, last.MaxSommer, false)
                     + Staffelbetrag(rolle.Stufen, last.MaxWinter, true);

            // Unbekannter Steuerwert: wie MONATLICH behandeln statt still 0 zu liefern —
            // dieselbe Richtung wie bei den übrigen Steuerwerten des Moduls.
            return last.SummeMonatsmaxima * rolle.MonatspreisEurKWMonat;
        }

        /// <summary>
        /// Staffelbetrag [€/a] für EINE Höchstlast: Jede Stufe bekommt den Anteil der
        /// Last, der zwischen ihrer Untergrenze (= Obergrenze der Vorstufe) und ihrer
        /// eigenen Obergrenze liegt.
        ///
        /// <para>Eine Obergrenze ≤ 0 gilt als „nach oben offen" und nimmt den ganzen
        /// Rest auf; danach ist die Staffel zu Ende. Stufen hinter einer offenen Stufe
        /// bleiben deshalb wirkungslos — das ist gewollt und in der Maske sichtbar.</para>
        /// </summary>
        public static double Staffelbetrag(List<LeistungsStufe> stufen, double lastKW, bool winter)
        {
            if (stufen == null || stufen.Count == 0 || lastKW <= 0) return 0;

            double summe = 0, unten = 0;
            foreach (LeistungsStufe s in stufen)
            {
                if (s == null) continue;
                bool offen = s.ObergrenzeKW <= 0;
                double oben = offen ? lastKW : s.ObergrenzeKW;
                if (oben <= unten) { if (offen) break; else continue; }

                double anteil = Math.Min(lastKW, oben) - unten;
                if (anteil <= 0) break;                      // Last unterhalb dieser Stufe
                summe += anteil * (winter ? s.PreisWinter : s.PreisSommer);
                unten = oben;
                if (offen || unten >= lastKW) break;
            }
            return summe;
        }

        /// <summary>Kosten EINER Rolle [€/a] aus Menge und Lastbild.</summary>
        public static TarifKosten Rollenkosten(TarifRolle rolle, double mengeMWh,
                                               StromMatrix.Lastbild last)
        {
            var k = new TarifKosten();
            if (rolle == null) return k;
            k.ArbeitEur = mengeMWh * 1000.0 * rolle.ArbeitspreisEurKWh;
            k.LeistungEur = Leistungskosten(rolle, last);
            k.GrundpreisEur = rolle.GrundpreisEurJahr;
            return k;
        }

        /// <summary>
        /// Die vollständige Kette: vermiedene Kosten nach der Differenzmethode plus
        /// Einspeiseerlös.
        ///
        /// <para><b>Warum die Differenzmethode und nicht „Eigenverbrauch × Arbeitspreis".</b>
        /// Beide Wege stehen in der Altanwendung nebeneinander; die Python-Fassung
        /// bewertet den Eigenverbrauch mit EINEM Preis, der VBA-Code bildet die Differenz
        /// zweier Tarife — und überschreibt die Python-Werte dreißig Zeilen später:
        /// <c>einsparung_arbeit(0) = KostenArbeitStrombezug − KostenArbeitReststrombezug</c>.
        /// Genau dieser Index füllt den Ergebnisdialog (Analyse, Abschnitt 8). Die
        /// Differenzmethode ist damit belegt.</para>
        /// </summary>
        public static StromErloesErgebnis Rechne(StromErloesEingabe eingabe,
                                                 TarifRolle bezug, TarifRolle reststrom,
                                                 TarifRolle einspeisung, CultureInfo kultur)
        {
            var r = new StromErloesErgebnis();
            if (eingabe == null) return r;
            if (kultur == null) kultur = CultureInfo.CurrentCulture;

            r.Bezug = Rollenkosten(bezug, eingabe.BedarfMWh, eingabe.LastBedarf);
            r.Reststrom = Rollenkosten(reststrom, eingabe.RestbezugMWh, eingabe.LastRestbezug);

            r.VermiedenArbeitEur = r.Bezug.ArbeitEur - r.Reststrom.ArbeitEur;
            r.VermiedenLeistungEur = r.Bezug.LeistungEur - r.Reststrom.LeistungEur;
            r.VermiedenGesamtEur = r.Bezug.SummeEur - r.Reststrom.SummeEur;

            r.EinspeisungMWh = eingabe.EinspeisungMWh;
            r.EinspeiseerloesEur = einspeisung == null ? 0
                : eingabe.EinspeisungMWh * 1000.0 * einspeisung.ArbeitspreisEurKWh
                  + einspeisung.GrundpreisEurJahr;

            r.Herleitung.Add(
                "Bezug ohne Anlage: " + eingabe.BedarfMWh.ToString("N1", kultur) + " MWh × " +
                (bezug == null ? 0 : bezug.ArbeitspreisEurKWh).ToString("N4", kultur) + " €/kWh = " +
                r.Bezug.ArbeitEur.ToString("N2", kultur) + " € Arbeit, " +
                r.Bezug.LeistungEur.ToString("N2", kultur) + " € Leistung (" +
                Modelltext(bezug) + ")");
            r.Herleitung.Add(
                "Reststrom mit Anlage: " + eingabe.RestbezugMWh.ToString("N1", kultur) + " MWh × " +
                (reststrom == null ? 0 : reststrom.ArbeitspreisEurKWh).ToString("N4", kultur) + " €/kWh = " +
                r.Reststrom.ArbeitEur.ToString("N2", kultur) + " € Arbeit, " +
                r.Reststrom.LeistungEur.ToString("N2", kultur) + " € Leistung (" +
                Modelltext(reststrom) + ")");
            r.Herleitung.Add(
                "Vermiedene Kosten: Arbeit " + r.VermiedenArbeitEur.ToString("N2", kultur) +
                " € + Leistung " + r.VermiedenLeistungEur.ToString("N2", kultur) +
                " € = " + r.VermiedenGesamtEur.ToString("N2", kultur) + " €/a" +
                (r.VermiedenLeistungEur < 0
                    ? " (der negative Leistungsanteil ist der Regelfall: der Reststromtarif " +
                      "ist teurer als der Bezugstarif)"
                    : ""));
            if (r.EinspeiseerloesEur != 0 || r.EinspeisungMWh > 0)
                r.Herleitung.Add(
                    "Einspeiseerlös: " + r.EinspeisungMWh.ToString("N1", kultur) + " MWh × " +
                    (einspeisung == null ? 0 : einspeisung.ArbeitspreisEurKWh).ToString("N4", kultur) +
                    " €/kWh = " + r.EinspeiseerloesEur.ToString("N2", kultur) + " €/a " +
                    "(ohne KWK-Zuschlag — der steht in eigener Zeile)");
            return r;
        }

        /// <summary>Klartext des Leistungspreismodells für die Herleitung.</summary>
        private static string Modelltext(TarifRolle rolle)
        {
            string m = rolle == null || string.IsNullOrEmpty(rolle.Leistungsmodell)
                     ? DbWerte.LEISTUNGSMODELL_MONATLICH : rolle.Leistungsmodell;
            if (string.Equals(m, DbWerte.LEISTUNGSMODELL_STAFFEL, StringComparison.Ordinal))
                return "Staffel, Sommer- und Wintermaximum getrennt";
            if (string.Equals(m, DbWerte.LEISTUNGSMODELL_JAHRESHOECHSTLAST, StringComparison.Ordinal))
                return "Jahreshöchstlast, Staffel mit Winterpreisen";
            return "monatlicher Leistungspreis, Σ zwölf Monatsmaxima";
        }
    }
}
