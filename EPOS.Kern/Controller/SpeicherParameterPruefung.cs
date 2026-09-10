using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Eingabeprüfung der Speicherparameter (W11b‑B‑28) — eine reine Funktion über
    /// den Werten des Blocks „Stromspeicher", ohne Datenbank und ohne Oberfläche.
    ///
    /// <para><b>Warum sie im Kern liegt.</b> Der Schreibweg selbst steht in der
    /// Windows-Hülle (<c>SimulationErgebnisHuelle.SpeicherparameterSchreiben</c>), und
    /// <c>WindowsFormsApplication1</c> hat kein Testprojekt. Die Regeln, um die es geht,
    /// sind aber Fachregeln und keine Fensterlogik: Ein SoC-Band braucht eine untere und
    /// eine obere Kante, eine Gerätegröße ist positiv, und ein Zins ist nicht negativ.
    /// Hier stehen sie an einer Stelle und sind geprüft; die Hülle ruft sie und schreibt
    /// erst danach.</para>
    ///
    /// <para><b>Kulturfrei.</b> Es wird gerechnet und verglichen, nicht formatiert oder
    /// zerlegt — die Zahlen kommen bereits als <c>double</c> an. Nur der Meldungstext
    /// kommt aus <c>MyResource</c> und folgt damit der Oberflächensprache des Fadens.</para>
    /// </summary>
    public static class SpeicherParameterPruefung
    {
        /// <summary>Untere Kante des SoC-Bandes [%].</summary>
        public const double SOC_MIN = 0.0;

        /// <summary>Obere Kante des SoC-Bandes [%].</summary>
        public const double SOC_MAX = 100.0;

        /// <summary>
        /// Prüft einen Satz Speicherparameter. <c>null</c> heißt „in Ordnung"; sonst
        /// steht der Grund im Text, und der Aufrufer schreibt nichts.
        /// </summary>
        /// <param name="soCMinProzent">Ladezustand minimal [%].</param>
        /// <param name="soCMaxProzent">Ladezustand maximal [%].</param>
        /// <param name="geraetegroesseAenderbar">
        /// Darf der Block Kapazität und Leistung überhaupt schreiben? Nur dann werden
        /// sie geprüft — sonst sind sie ein angezeigtes Gerätedatum, und ein Projekt
        /// ohne gepflegten Speicher soll an der Prüfung nicht hängenbleiben.
        /// </param>
        /// <param name="kapazitaetKwh">Nennkapazität [kWh].</param>
        /// <param name="leistungKw">Lade-/Entladeleistung [kW].</param>
        /// <param name="nutzungsdauerJahre">Nutzungsdauer [a].</param>
        /// <param name="kapitalzinsProzent">Kapitalzins [%].</param>
        /// <param name="leistungspreisEurProKwA">Leistungspreis [EUR/(kW·a)].</param>
        /// <param name="netzladeaufschlagCtProKwh">Netzladeaufschlag [ct/kWh].</param>
        /// <returns><c>null</c> = gültig, sonst der Meldungstext.</returns>
        public static string Pruefen(double soCMinProzent, double soCMaxProzent,
                                     bool geraetegroesseAenderbar,
                                     double kapazitaetKwh, double leistungKw,
                                     double nutzungsdauerJahre, double kapitalzinsProzent,
                                     double leistungspreisEurProKwA,
                                     double netzladeaufschlagCtProKwh)
        {
            // DAS BAND: 0 <= min < max <= 100. Die strenge Ungleichung in der Mitte ist
            // Absicht - min = max waere ein Speicher ohne nutzbaren Hub, und die
            // Engine teilte spaeter durch die Bandbreite.
            if (Unbrauchbar(soCMinProzent) || Unbrauchbar(soCMaxProzent)
                || soCMinProzent < SOC_MIN || soCMaxProzent > SOC_MAX
                || soCMinProzent >= soCMaxProzent)
                return MyResource.Resource.SP_PARAM_MSG_SOC_BAND;

            // DIE GERAETEGROESSE nur, wenn der Block sie auch schreibt (genau eine
            // SP-Anlage im Projekt); sonst ist sie eine Anzeige, die niemand aendern
            // kann, und eine Sperre daran waere eine Sackgasse.
            if (geraetegroesseAenderbar
                && (Unbrauchbar(kapazitaetKwh) || Unbrauchbar(leistungKw)
                    || kapazitaetKwh <= 0.0 || leistungKw <= 0.0))
                return MyResource.Resource.SP_PARAM_MSG_GERAET_POSITIV;

            // DIE VIER WIRTSCHAFTSWERTE duerfen null sein (ein Projekt ohne
            // Leistungspreis ist ein gueltiger Fall), aber nicht negativ.
            if (Negativ(nutzungsdauerJahre) || Negativ(kapitalzinsProzent)
                || Negativ(leistungspreisEurProKwA) || Negativ(netzladeaufschlagCtProKwh))
                return MyResource.Resource.SP_PARAM_MSG_NEGATIV;

            return null;
        }

        /// <summary>NaN und Unendlich sind keine Eingaben, sondern Rechenunfaelle.</summary>
        private static bool Unbrauchbar(double wert)
            => double.IsNaN(wert) || double.IsInfinity(wert);

        private static bool Negativ(double wert) => Unbrauchbar(wert) || wert < 0.0;
    }
}
