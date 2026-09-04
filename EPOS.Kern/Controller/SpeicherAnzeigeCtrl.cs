using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die drei ANZEIGEÜBERSETZUNGEN des Stromspeichers — eine Stelle statt drei
    /// (iU9-W11a.5, Befund W11-B42).
    ///
    /// <para><b>Woher sie kommen.</b> <c>BetriebsartText</c>, <c>BerechnungsartText</c>
    /// und <c>AmortisationText</c> standen in
    /// <c>Form_SpeicherVariantenVergleich</c> (:529-562), <c>AmortisationText</c>
    /// zusätzlich in <c>Form_SpeicherOptimierung</c> (:1119) und — unter dem Namen
    /// <c>SpAmortisationstext</c> — in <c>Form_Simulation_Detail</c> (:7502). Sie
    /// übersetzen Persistenzwerte (<c>DbWerte.SP_*</c>, deutsch und eingefroren) bzw.
    /// einen Engine-Zustand in Anzeigetexte — genau die Schichtgrenze, für die es die
    /// Drei-Schichten-Regel gibt.</para>
    ///
    /// <para><b>Es war eine VIERFACHUNG, keine Dreifachung.</b> Die vierte Fassung stand
    /// in <c>Form_Simulation_Config.Karten.cs</c> und ist mit iU9‑W10b nach
    /// <c>SimulationKonfigHuelle</c> gewandert. Sie wich ab: Sie kannte die
    /// Preissteuerung, die drei anderen nicht. Beim Zusammenführen der beiden Wellen
    /// ist ihr Wissen hierher gezogen und die Hülle ruft diese Methoden — vier Kopien,
    /// eine Wahrheit (W11a‑O‑4).</para>
    ///
    /// <para><b>Zwei Ressourcenpaare für denselben Text.</b> Der Bestand führt
    /// <c>OPT_AMORT_NIE</c>/<c>OPT_AMORT_UEBER</c> (Optimierung, Variantenvergleich) UND
    /// <c>SP_ERG_NICHT_AMORTISIERBAR</c>/<c>SP_ERG_UEBER_NUTZUNGSDAUER</c>
    /// (Ergebnisseite). Beide Paare tragen in BEIDEN Sprachen denselben Wortlaut
    /// („nicht amortisierbar" / „&gt; Nutzungsdauer" bzw. „not amortisable" /
    /// „&gt; service life") — nachgeprüft am Katalog. Genommen ist hier das
    /// <c>SP_ERG_*</c>-Paar; das andere bleibt für seine übrigen Verwender stehen.</para>
    ///
    /// <para><b>Eine Formatabweichung.</b> Die Ergebnisseite formatierte die Jahre mit
    /// <c>"N1"</c>, Optimierung und Variantenvergleich mit <c>"0.0"</c>. Beide liefern
    /// dieselbe Zeichenkette, solange die Amortisationszeit unter 1 000 Jahren bleibt —
    /// erst darüber setzt <c>"N1"</c> ein Tausendertrennzeichen. Genommen ist
    /// <c>"N1"</c>. Ein Fall, in dem sich das auswirkt, ist fachlich ausgeschlossen (die
    /// Engine meldet über der Nutzungsdauer einen Zustand, keine Zahl).</para>
    /// </summary>
    public static class SpeicherAnzeigeCtrl
    {
        /// <summary>
        /// Betriebsart als Anzeigetext. Unbekannte Werte kommen unverändert zurück —
        /// besser der Persistenzwert als gar nichts.
        /// </summary>
        public static string BetriebsartText(string wert)
        {
            if (wert == DbWerte.SP_BETRIEBSART_GRAUSTROM)
                return MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRAUSTROM;
            if (wert == DbWerte.SP_BETRIEBSART_GRUENSTROM)
                return MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRUENSTROM;
            return wert ?? "";
        }

        /// <summary>
        /// Berechnungsart als Anzeigetext.
        ///
        /// <para><b>Die Lücke ist beim Zusammenführen mit W10b geschlossen worden.</b>
        /// Drei der vier Fassungen des Bestands (Variantenvergleich,
        /// Auslegungsoptimierung, Ergebnisseite) kannten nur Nachtnutzung und
        /// Dauernutzung; die Preissteuerung erschien dort mit ihrem Persistenzwert
        /// „Arbitrage". Die VIERTE — <c>Form_Simulation_Config.BerechnungsartAnzeige</c>,
        /// mit iU9‑W10b nach <c>SimulationKonfigHuelle</c> gewandert — kannte sie. Diese
        /// Fassung ist die vollständigere und steht jetzt hier; alle vier Aufrufer
        /// bekommen damit denselben Text (W11a‑O‑4).</para>
        ///
        /// <para>Ein unbekannter Wert kommt weiterhin unverändert zurück. Die vierte
        /// Fassung fiel dort auf „Dauernutzung" zurück — das ist eine Behauptung über
        /// Daten, die man nicht kennt; der Persistenzwert ist ehrlicher. Alle vier
        /// Schreiber setzen ohnehin nur <c>DbWerte.SP_BERECHNUNG_*</c>.</para>
        /// </summary>
        public static string BerechnungsartText(string wert)
        {
            if (wert == DbWerte.SP_BERECHNUNG_NACHTNUTZUNG)
                return MyResource.Resource.SP_BERECHNUNG_ANZEIGE_NACHTNUTZUNG;
            if (wert == DbWerte.SP_BERECHNUNG_DAUERNUTZUNG)
                return MyResource.Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG;
            if (wert == DbWerte.SP_BERECHNUNG_ARBITRAGE)
                return MyResource.Resource.SP_BERECHNUNG_ANZEIGE_ARBITRAGE;
            return wert ?? "";
        }

        /// <summary>
        /// Amortisationszeit als Text: die Jahre, oder der Klartext des Sonderfalls
        /// (Fachkonzept 7.1 — die V7-Mappe schrieb beides in dieselbe Zelle, die Engine
        /// trennt Zustand und Zahl).
        /// </summary>
        public static string AmortisationText(Amortisation a)
        {
            switch (a.Status)
            {
                case AmortisationStatus.NichtAmortisierbar:
                    return MyResource.Resource.SP_ERG_NICHT_AMORTISIERBAR;
                case AmortisationStatus.UeberNutzungsdauer:
                    return MyResource.Resource.SP_ERG_UEBER_NUTZUNGSDAUER;
                default:
                    return a.Jahre.ToString("N1", CultureInfo.CurrentCulture);
            }
        }
    }
}
