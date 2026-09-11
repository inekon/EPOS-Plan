using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Eingabeprüfung der Speicherparameter (W11b‑B‑28, seit W11b‑B‑29 je Regel
    /// einzeln) — reine Funktionen über den Werten des Blocks „Stromspeicher", ohne
    /// Datenbank und ohne Oberfläche.
    ///
    /// <para><b>Warum sie im Kern liegen.</b> Der Schreibweg selbst steht in der
    /// Windows-Hülle (<c>SimulationErgebnisHuelle.SpeicherfeldSchreiben</c>), und
    /// <c>WindowsFormsApplication1</c> hat kein Testprojekt. Die Regeln, um die es geht,
    /// sind aber Fachregeln und keine Fensterlogik: Ein SoC-Band braucht eine untere und
    /// eine obere Kante, eine Gerätegröße ist positiv, und ein Zins ist nicht negativ.
    /// Hier stehen sie an einer Stelle und sind geprüft; die Hülle ruft sie und schreibt
    /// erst danach.</para>
    ///
    /// <para><b>Je Regel eine Funktion</b> (W11b‑B‑29). Seit der Block jedes Feld SOFORT
    /// schreibt (Anwenderentscheid 1 vom 10.09.2026), gibt es keinen Satz mehr, den man
    /// als Ganzes prüfen könnte: Es kommt EIN Feld an, und geprüft wird die Regel, die
    /// zu ihm gehört — das SoC-Band gegen den jeweils anderen GESPEICHERTEN Wert, die
    /// Gerätegröße gegen den anderen Gerätewert, die vier Wirtschaftswerte je für sich.
    /// Ein Verstoss weist ab, und es geht nichts in die Datenbank.</para>
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
        /// Das SoC-Band: <c>0 ≤ min &lt; max ≤ 100</c>. <c>null</c> heißt „in Ordnung";
        /// sonst steht der Grund im Text, und der Aufrufer schreibt nichts.
        /// </summary>
        /// <remarks>
        /// Die strenge Ungleichung in der Mitte ist Absicht: <c>min = max</c> wäre ein
        /// Speicher ohne nutzbaren Hub, und die Engine teilte später durch die
        /// Bandbreite. Geprüft wird immer das GANZE Band — wer eine Kante eingibt, gibt
        /// sie gegen die andere, gespeicherte ein.
        /// </remarks>
        /// <param name="soCMinProzent">Ladezustand minimal [%].</param>
        /// <param name="soCMaxProzent">Ladezustand maximal [%].</param>
        public static string SoCBand(double soCMinProzent, double soCMaxProzent)
        {
            if (Unbrauchbar(soCMinProzent) || Unbrauchbar(soCMaxProzent)
                || soCMinProzent < SOC_MIN || soCMaxProzent > SOC_MAX
                || soCMinProzent >= soCMaxProzent)
                return MyResource.Resource.SP_PARAM_MSG_SOC_BAND;

            return null;
        }

        /// <summary>
        /// Die Gerätegröße: Kapazität UND Leistung größer als null. <c>null</c> heißt
        /// „in Ordnung".
        /// </summary>
        /// <remarks>
        /// Geprüft wird nur, wenn der Block die Gerätegröße überhaupt schreiben darf
        /// (genau eine SP-Anlage im Projekt, <c>StromspeicherSimCtrl.UebernehmeAuslegung</c>);
        /// sonst ist sie eine Anzeige, die niemand ändern kann, und eine Sperre daran
        /// wäre eine Sackgasse. Diese Bedingung liegt beim Aufrufer — hier steht nur die
        /// Regel.
        /// </remarks>
        /// <param name="kapazitaetKwh">Nennkapazität [kWh].</param>
        /// <param name="leistungKw">Lade-/Entladeleistung [kW].</param>
        public static string Geraet(double kapazitaetKwh, double leistungKw)
        {
            if (Unbrauchbar(kapazitaetKwh) || Unbrauchbar(leistungKw)
                || kapazitaetKwh <= 0.0 || leistungKw <= 0.0)
                return MyResource.Resource.SP_PARAM_MSG_GERAET_POSITIV;

            return null;
        }

        /// <summary>
        /// Einer der vier Wirtschaftswerte (Nutzungsdauer, Kapitalzins, Leistungspreis,
        /// Netzladeaufschlag): null ist erlaubt, negativ nicht. <c>null</c> heißt „in
        /// Ordnung".
        /// </summary>
        /// <remarks>
        /// Ein Projekt ohne Leistungspreis ist ein gültiger Fall — deshalb ist die Grenze
        /// null und nicht „größer als null". Die Meldung nennt alle vier Werte: Sie steht
        /// in der Statuszeile des Blocks, in dem sie nebeneinander stehen.
        /// </remarks>
        /// <param name="wert">Der eingegebene Wert.</param>
        public static string NichtNegativ(double wert)
        {
            if (Unbrauchbar(wert) || wert < 0.0)
                return MyResource.Resource.SP_PARAM_MSG_NEGATIV;

            return null;
        }

        /// <summary>NaN und Unendlich sind keine Eingaben, sondern Rechenunfaelle.</summary>
        private static bool Unbrauchbar(double wert)
            => double.IsNaN(wert) || double.IsInfinity(wert);
    }
}
