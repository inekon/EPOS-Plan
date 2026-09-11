using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Die Dringlichkeit eines Vorprüfungshinweises.</summary>
    public enum FlottenHinweisStufe
    {
        /// <summary>Sachhinweis; der Lauf ist trotzdem sinnvoll.</summary>
        Hinweis = 0,

        /// <summary>Warnung; der Lauf liefert voraussichtlich kein brauchbares Ergebnis.</summary>
        Warnung = 1
    }

    /// <summary>
    /// Die sprachneutrale Kennung eines Vorprüfungshinweises. Die Oberfläche hängt an ihr
    /// ihren Abhilfeknopf auf (Konzept Stromspeicher-Dialoge 2.2 Punkt 2).
    /// </summary>
    public enum FlottenHinweisKennung
    {
        /// <summary>Das Peak-Ziel liegt unter dem Maximum der Tagesminima, und Netzladung ist verboten.</summary>
        PeakZielUnterTagesminimum = 0,

        /// <summary>Das Peak-Ziel liegt über der Referenzspitze; die Kappung bleibt wirkungslos.</summary>
        PeakZielUeberReferenzspitze = 1,

        /// <summary>Der jährliche Betriebsaufwand liegt unter einem Tausendstel der Investition.</summary>
        BetriebskostenSehrNiedrig = 2,

        /// <summary>Der Start-Ladezustand steht auf dem SoC-Minimum; am Anfang des Zeitraums ist nichts zu entladen.</summary>
        StartSoCAufMinimum = 3,

        /// <summary>Die Flotte hat im ganzen Zeitraum weder geladen noch entladen.</summary>
        FlotteArbeitslos = 4
    }

    /// <summary>Ein Vorprüfungshinweis zu einer Flottenstudie.</summary>
    public sealed class FlottenHinweis
    {
        /// <summary>Die sprachneutrale Kennung.</summary>
        public FlottenHinweisKennung Kennung { get; set; }

        /// <summary>Die Dringlichkeit.</summary>
        public FlottenHinweisStufe Stufe { get; set; }

        /// <summary>Der Text im Klartext.</summary>
        public string Text { get; set; } = "";
    }

    /// <summary>
    /// Vorprüfung einer Flottenstudie VOR dem Lauf und Deutung der Diagnose NACH dem Lauf
    /// (Konzept Stromspeicher-Dialoge 2.4 Punkte 3 und 6, Anwenderentscheid SD‑Q4,
    /// Aufgabe #183).
    /// </summary>
    /// <remarks>
    /// Die Prüfung ist eine reine Funktion: Sie liest Zeitreihe, Konfiguration, die
    /// aufgelösten Kostensätze und — sofern schon vorhanden — die Diagnose des Laufs, und
    /// gibt eine Liste von Hinweisen zurück. Sie ändert nichts und rechnet nichts nach.
    /// Die Verdrahtung in der Oberfläche (Banner, Abhilfeknöpfe) ist Sache des Pakets P3.
    /// </remarks>
    public static class FlottenPlausibilitaet
    {
        /// <summary>
        /// Der Anteil der Investition, unter dem ein jährlicher Betriebsaufwand als
        /// auffällig klein gilt (Konzept 1.3: „Betrieb 1,00 €/a" bei 15 000 € Investition).
        /// </summary>
        public const double BetriebskostenSchwelleAnteil = 0.001;

        /// <summary>
        /// Prüft eine Flottenstudie und liefert die Hinweise in fester Reihenfolge.
        /// </summary>
        /// <param name="eingang">Die Standortzeitreihen; ohne sie entfallen die beiden Peak-Ziel-Prüfungen.</param>
        /// <param name="konfiguration">Die Flotte samt Betriebsoptionen.</param>
        /// <param name="kosten">Die aufgelösten Kostensätze; <c>null</c> = mit den Sätzen der Einheiten rechnen.</param>
        /// <param name="diagnose">Die Diagnose eines bereits gerechneten Laufs; <c>null</c> vor dem Lauf.</param>
        /// <returns>Die Hinweise; eine leere Liste, wenn nichts zu beanstanden ist.</returns>
        public static List<FlottenHinweis> Pruefe(
            FlottenEingang eingang,
            FlottenStudieKonfiguration konfiguration,
            SpeicherKostensaetze kosten,
            FlottenDiagnose diagnose = null)
        {
            var hinweise = new List<FlottenHinweis>();
            if (konfiguration?.Optionen == null) return hinweise;
            CultureInfo k = CultureInfo.CurrentCulture;

            PruefePeakZiel(hinweise, eingang, konfiguration, k);
            PruefeBetriebskosten(hinweise, konfiguration, kosten, k);
            PruefeStartSoC(hinweise, konfiguration, diagnose);
            PruefeDiagnose(hinweise, diagnose, k);
            return hinweise;
        }

        /// <summary>Beide Prüfungen des Peak-Ziels gegen die Referenzzeitreihe.</summary>
        /// <param name="hinweise">Die Sammelliste.</param>
        /// <param name="eingang">Die Standortzeitreihen; <c>null</c> oder leer = keine Prüfung.</param>
        /// <param name="konfiguration">Die Flotte samt Betriebsoptionen.</param>
        /// <param name="k">Die Kultur der Zahlenformatierung.</param>
        private static void PruefePeakZiel(List<FlottenHinweis> hinweise, FlottenEingang eingang,
            FlottenStudieKonfiguration konfiguration, CultureInfo k)
        {
            if (konfiguration.Optionen.WirtschaftlicherPeakZielwertKw is not double h) return;
            if (eingang?.Istwerte == null || eingang.Istwerte.Count == 0) return;

            FlottenPeakZielVorschlag lage = FlottenPeakZiel.Vorschlag(eingang.Istwerte, konfiguration);
            if (!konfiguration.Optionen.NetzladungErlaubt && h < lage.TagesminimumMaxKw)
                hinweise.Add(new FlottenHinweis
                {
                    Kennung = FlottenHinweisKennung.PeakZielUnterTagesminimum,
                    Stufe = FlottenHinweisStufe.Warnung,
                    Text = string.Format(k,
                        "Peak-Ziel {0} kW liegt unter dem Maximum der Tagesminima ({1} kW): Unter dieses Ziel " +
                        "fällt die Last an mindestens einem Tag nie. Ohne freigegebene Netzladung lädt die " +
                        "Flotte dann nicht. Vorschlag aus der Referenz: {2} kW.",
                        h.ToString("0.#", k), lage.TagesminimumMaxKw.ToString("0.#", k),
                        lage.PeakZielKw.ToString("0.#", k))
                });

            if (h > lage.ReferenzspitzeKw)
                hinweise.Add(new FlottenHinweis
                {
                    Kennung = FlottenHinweisKennung.PeakZielUeberReferenzspitze,
                    Stufe = FlottenHinweisStufe.Hinweis,
                    Text = string.Format(k,
                        "Peak-Ziel {0} kW liegt über der Referenzspitze ({1} kW): Die Kappung bleibt wirkungslos, " +
                        "weil die Last das Ziel nie überschreitet.",
                        h.ToString("0.#", k), lage.ReferenzspitzeKw.ToString("0.#", k))
                });
        }

        /// <summary>Die Größenordnungsprüfung des jährlichen Betriebsaufwands gegen die Investition.</summary>
        /// <param name="hinweise">Die Sammelliste.</param>
        /// <param name="konfiguration">Die Flotte, aus der Größen und eigene Sätze stammen.</param>
        /// <param name="kosten">Die aufgelösten Kostensätze für Einheiten ohne eigene Kostenvorgabe.</param>
        /// <param name="k">Die Kultur der Zahlenformatierung.</param>
        private static void PruefeBetriebskosten(List<FlottenHinweis> hinweise,
            FlottenStudieKonfiguration konfiguration, SpeicherKostensaetze kosten, CultureInfo k)
        {
            if (konfiguration.Einheiten == null || konfiguration.Einheiten.Count == 0) return;
            double investition = 0, betrieb = 0;
            foreach (FlottenEinheit e in konfiguration.Einheiten)
            {
                double leistung = Math.Max(e.LadeleistungKw, e.EntladeleistungKw);
                bool ausSaetzen = !e.EigeneKosten && kosten != null;
                investition += (ausSaetzen ? 0 : e.InvestitionEuro)
                    + (ausSaetzen ? kosten.InvestEurProKwh : e.InvestitionEuroProKWh) * e.KapazitaetKWh
                    + (ausSaetzen ? kosten.InvestEurProKw : e.InvestitionEuroProKw) * leistung;
                betrieb += (ausSaetzen ? 0 : e.JaehrlicheFixeOpexEuro)
                    + (ausSaetzen ? kosten.BetriebEurProKwhJahr : e.JaehrlicheOpexEuroProKWhKapazitaet) * e.KapazitaetKWh
                    + (ausSaetzen ? kosten.BetriebEurProKwJahr : e.JaehrlicheOpexEuroProKw) * leistung;
            }
            if (!double.IsFinite(investition) || !double.IsFinite(betrieb) || investition <= 0) return;
            if (betrieb >= BetriebskostenSchwelleAnteil * investition) return;

            hinweise.Add(new FlottenHinweis
            {
                Kennung = FlottenHinweisKennung.BetriebskostenSehrNiedrig,
                Stufe = FlottenHinweisStufe.Hinweis,
                Text = string.Format(k,
                    "Der jährliche Betriebsaufwand der Flotte beträgt {0} € bei {1} € Investition — weniger als " +
                    "{2} %. Bitte die Kostensätze prüfen: Sie sind absolute Beträge, kein Prozentsatz der Investition.",
                    betrieb.ToString("0.##", k), investition.ToString("0.##", k),
                    (BetriebskostenSchwelleAnteil * 100).ToString("0.###", k))
            });
        }

        /// <summary>Der Hinweis zum Start-Ladezustand (Anwenderentscheid SD‑Q4).</summary>
        /// <remarks>
        /// Der Produktivstandard bleibt das SoC-Minimum (AP0); geändert wird nichts. Der
        /// Hinweis erscheint, wenn die Diagnose eine arbeitslose Flotte meldet ODER der
        /// Start-Ladezustand auf dem Minimum steht und gegen eine Lastspitze gefahren wird.
        /// </remarks>
        /// <param name="hinweise">Die Sammelliste.</param>
        /// <param name="konfiguration">Die Flotte samt Betriebsoptionen.</param>
        /// <param name="diagnose">Die Diagnose eines bereits gerechneten Laufs oder <c>null</c>.</param>
        private static void PruefeStartSoC(List<FlottenHinweis> hinweise,
            FlottenStudieKonfiguration konfiguration, FlottenDiagnose diagnose)
        {
            if (konfiguration.Einheiten == null || konfiguration.Einheiten.Count == 0) return;
            bool aufMinimum = konfiguration.Optionen.Betriebsziel == FlottenBetriebsziel.PeakShaving &&
                konfiguration.Einheiten.Any(x => x.SocStart <= x.SocMin + 1e-9);
            if (!aufMinimum && diagnose?.Arbeitslos != true) return;

            hinweise.Add(new FlottenHinweis
            {
                Kennung = FlottenHinweisKennung.StartSoCAufMinimum,
                Stufe = FlottenHinweisStufe.Hinweis,
                Text = "Start-Ladezustand = SoC-Minimum (Produktivstandard): am 1. Januar ist nichts zu entladen. " +
                    "Eine Spitze in den ersten Stunden des Zeitraums kann die Flotte deshalb nicht kappen."
            });
        }

        /// <summary>Der Befund „arbeitslose Flotte" aus der Diagnose des gerechneten Laufs.</summary>
        /// <param name="hinweise">Die Sammelliste.</param>
        /// <param name="diagnose">Die Diagnose des Laufs oder <c>null</c>.</param>
        /// <param name="k">Die Kultur der Zahlenformatierung.</param>
        private static void PruefeDiagnose(List<FlottenHinweis> hinweise, FlottenDiagnose diagnose, CultureInfo k)
        {
            if (diagnose?.Arbeitslos != true) return;
            hinweise.Add(new FlottenHinweis
            {
                Kennung = FlottenHinweisKennung.FlotteArbeitslos,
                Stufe = FlottenHinweisStufe.Warnung,
                Text = "Die Flotte hat im gesamten Zeitraum weder geladen noch entladen. " + Gruende(diagnose, k)
            });
        }

        /// <summary>Fasst die gezählten Diagnosegründe in einem Satz zusammen.</summary>
        /// <param name="diagnose">Die Diagnose des Laufs.</param>
        /// <param name="k">Die Kultur der Zahlenformatierung.</param>
        /// <returns>Die Aufzählung der Gründe mit ihren Zahlen; ein Ersatzsatz, wenn keiner gezählt wurde.</returns>
        public static string Gruende(FlottenDiagnose diagnose, CultureInfo k)
        {
            if (diagnose == null || diagnose.Gruende.Count == 0) return "Es wurde kein Grund gezählt.";
            k ??= CultureInfo.CurrentCulture;
            string[] teile = diagnose.Gruende.Select(x => string.Format(k, "{0} ({1} von {2} Intervallen, {3} %)",
                Benennung(x.Grund), x.Intervalle.ToString(k), diagnose.IntervalleGesamt.ToString(k),
                (x.Anteil * 100).ToString("0.#", k))).ToArray();
            return "Gezählte Gründe: " + string.Join("; ", teile) + ".";
        }

        /// <summary>Die deutsche Benennung eines Diagnosegrunds.</summary>
        /// <param name="grund">Der sprachneutrale Grund.</param>
        /// <returns>Der Anzeigetext.</returns>
        public static string Benennung(FlottenDiagnoseGrund grund) => grund switch
        {
            FlottenDiagnoseGrund.LastUeberPeakZiel => "Last über dem Peak-Ziel",
            FlottenDiagnoseGrund.LadedeckelDurchPeakregel => "Ladedeckel 0 durch die Peak-Regel",
            FlottenDiagnoseGrund.LadedeckelDurchNetzladeverbot => "Ladedeckel 0 durch das Netzladeverbot",
            FlottenDiagnoseGrund.EntladeanforderungOhneEnergie => "Entladeanforderung bei leerem Speicher",
            FlottenDiagnoseGrund.KeineEntladeanforderung => "keine Entladeanforderung",
            FlottenDiagnoseGrund.KeineLadeanforderung => "keine Ladeanforderung",
            _ => grund.ToString()
        };
    }
}
