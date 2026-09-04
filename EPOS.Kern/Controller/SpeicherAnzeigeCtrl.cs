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
        /// <para><b>Wörtlich übernommen, samt Lücke:</b> Der Vorläufer kennt
        /// Nachtnutzung und Dauernutzung, NICHT aber
        /// <c>DbWerte.SP_BERECHNUNG_ARBITRAGE</c> — die Preissteuerung erscheint dort
        /// mit ihrem Persistenzwert. Das ist keine Portfrage und bleibt so
        /// (offener Punkt im W11a-Protokoll).</para>
        /// </summary>
        public static string BerechnungsartText(string wert)
        {
            if (wert == DbWerte.SP_BERECHNUNG_NACHTNUTZUNG)
                return MyResource.Resource.SP_BERECHNUNG_ANZEIGE_NACHTNUTZUNG;
            if (wert == DbWerte.SP_BERECHNUNG_DAUERNUTZUNG)
                return MyResource.Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG;
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
